using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace whatlanCar;

public sealed class PolicyInference : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly int _width;
    private readonly int _height;
    private readonly int _channels;
    private readonly bool _useDepth;
    private readonly bool _useMinimap;
    private readonly float[] _inputBuffer;
    private readonly DenseTensor<float> _inputTensor;
    private readonly List<NamedOnnxValue> _inputs;

    public bool UseDepth => _useDepth;
    public bool UseMinimap => _useMinimap;

    public PolicyInference(string modelPath)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("策略模型不存在，请先训练生成 policy.onnx。", modelPath);
        }

        var metadataPath = Path.ChangeExtension(modelPath, ".json");
        var metadata = File.Exists(metadataPath)
            ? JsonSerializer.Deserialize<PolicyMetadata>(File.ReadAllText(metadataPath)) ?? new PolicyMetadata()
            : new PolicyMetadata();

        _width = metadata.ImgWidth > 0 ? metadata.ImgWidth : 160;
        _height = metadata.ImgHeight > 0 ? metadata.ImgHeight : 120;
        _channels = metadata.InChannels > 0 ? metadata.InChannels : 9;
        _useDepth = metadata.UseDepth;
        _useMinimap = metadata.UseMinimap;

        _session = new InferenceSession(modelPath, CreateSessionOptions());
        _inputName = _session.InputNames.FirstOrDefault()
            ?? throw new InvalidOperationException("策略模型没有输入。");

        _inputBuffer = new float[1 * _channels * _height * _width];
        _inputTensor = new DenseTensor<float>(_inputBuffer, new[] { 1, _channels, _height, _width });
        _inputs = new List<NamedOnnxValue>(1)
        {
            NamedOnnxValue.CreateFromTensor(_inputName, _inputTensor)
        };
    }

    public PolicyAction Predict(Mat frameBgr, Mat depthBgr, Mat minimapBgr)
    {
        return PredictDetailed(frameBgr, depthBgr, minimapBgr).Action;
    }

    public PolicyPrediction PredictDetailed(Mat frameBgr, Mat depthBgr, Mat minimapBgr)
    {
        FillInput(frameBgr, depthBgr, minimapBgr);
        using var results = _session.Run(_inputs);
        var outputs = results.ToDictionary(x => x.Name, x => x.AsTensor<float>());

        var moveTensor = GetOutput(outputs, "move_logits");
        var turnTensor = GetOutput(outputs, "turn_logits");
        var move = ArgMax(moveTensor, 3);
        var turn = ArgMax(turnTensor, 3);
        var moveProb = Softmax(moveTensor, 3);
        var turnProb = Softmax(turnTensor, 3);
        var jumpLogit = GetOutput(outputs, "jump_logit").FirstOrDefault();
        var jump = Sigmoid(jumpLogit) > 0.5f;

        var action = new PolicyAction(
            move == 1,
            move == 2,
            turn == 1,
            turn == 2,
            jump);
        return new PolicyPrediction(
            action,
            move,
            turn,
            moveProb[move],
            turnProb[turn],
            BestMargin(turnProb, turn));
    }

    private void FillInput(Mat frameBgr, Mat depthBgr, Mat minimapBgr)
    {
        Array.Clear(_inputBuffer);
        var channelOffset = 0;
        CopyImageToInput(frameBgr, channelOffset);
        channelOffset += 3;

        if (_useDepth)
        {
            CopyImageToInput(depthBgr, channelOffset);
            channelOffset += 3;
        }

        if (_useMinimap)
        {
            CopyImageToInput(minimapBgr, channelOffset);
        }
    }

    private void CopyImageToInput(Mat bgrImage, int channelOffset)
    {
        using var resized = new Mat();
        using var rgb = new Mat();
        Cv2.Resize(bgrImage, resized, new OpenCvSharp.Size(_width, _height), interpolation: InterpolationFlags.Area);
        Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                var pixel = rgb.At<Vec3b>(y, x);
                var pixelIndex = y * _width + x;
                _inputBuffer[(channelOffset + 0) * _height * _width + pixelIndex] = pixel.Item0 / 255.0f;
                _inputBuffer[(channelOffset + 1) * _height * _width + pixelIndex] = pixel.Item1 / 255.0f;
                _inputBuffer[(channelOffset + 2) * _height * _width + pixelIndex] = pixel.Item2 / 255.0f;
            }
        }
    }

    private static Tensor<float> GetOutput(Dictionary<string, Tensor<float>> outputs, string name)
    {
        return outputs.TryGetValue(name, out var tensor)
            ? tensor
            : outputs.Values.First();
    }

    private static int ArgMax(Tensor<float> tensor, int count)
    {
        var bestIndex = 0;
        var bestValue = float.NegativeInfinity;
        for (var i = 0; i < count; i++)
        {
            var value = tensor.GetValue(i);
            if (value > bestValue)
            {
                bestValue = value;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static float Sigmoid(float value)
    {
        return 1.0f / (1.0f + MathF.Exp(-value));
    }

    private static float[] Softmax(Tensor<float> tensor, int count)
    {
        var values = new float[count];
        var max = float.NegativeInfinity;
        for (var i = 0; i < count; i++)
        {
            values[i] = tensor.GetValue(i);
            max = MathF.Max(max, values[i]);
        }

        var sum = 0.0f;
        for (var i = 0; i < count; i++)
        {
            values[i] = MathF.Exp(values[i] - max);
            sum += values[i];
        }

        if (sum <= 0)
        {
            return values;
        }

        for (var i = 0; i < count; i++)
        {
            values[i] /= sum;
        }

        return values;
    }

    private static float BestMargin(float[] probabilities, int bestIndex)
    {
        var runnerUp = 0.0f;
        for (var i = 0; i < probabilities.Length; i++)
        {
            if (i != bestIndex)
            {
                runnerUp = MathF.Max(runnerUp, probabilities[i]);
            }
        }

        return probabilities[bestIndex] - runnerUp;
    }

    private static SessionOptions CreateSessionOptions()
    {
        return new SessionOptions
        {
            LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_WARNING,
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL
        };
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    private sealed class PolicyMetadata
    {
        [JsonPropertyName("img_width")]
        public int ImgWidth { get; set; }

        [JsonPropertyName("img_height")]
        public int ImgHeight { get; set; }

        [JsonPropertyName("in_channels")]
        public int InChannels { get; set; }

        [JsonPropertyName("use_depth")]
        public bool UseDepth { get; set; } = true;

        [JsonPropertyName("use_minimap")]
        public bool UseMinimap { get; set; } = true;
    }
}

public readonly record struct PolicyAction(bool Forward, bool Back, bool Left, bool Right, bool Jump)
{
    public override string ToString()
    {
        var move = Forward ? "W" : Back ? "S" : "-";
        var turn = Left ? "左" : Right ? "右" : "-";
        var jump = Jump ? "J" : "-";
        return $"{move}/{turn}/{jump}";
    }
}

public readonly record struct PolicyPrediction(
    PolicyAction Action,
    int MoveLabel,
    int TurnLabel,
    float MoveConfidence,
    float TurnConfidence,
    float TurnMargin);
