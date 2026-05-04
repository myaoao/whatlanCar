using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace whatlanCar;

public sealed class DepthAnythingInference : IDisposable
{
    private readonly int _inputSize;
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly float[] _inputBuffer;
    private readonly DenseTensor<float> _inputTensor;
    private readonly List<NamedOnnxValue> _inputs;
    private long _totalInferenceMs;
    private int _inferenceCount;

    public string ExecutionProvider { get; private set; } = "CPU";

    public double AverageInferenceMs => _inferenceCount == 0 ? 0 : (double)_totalInferenceMs / _inferenceCount;

    public DepthAnythingInference(string modelPath, int inputSize = 518)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("Depth Anything model was not found.", modelPath);
        }

        _inputSize = inputSize;
        _session = CreateSession(modelPath);
        _inputName = _session.InputNames.FirstOrDefault()
            ?? throw new InvalidOperationException("Model input name was not found.");

        _inputBuffer = new float[1 * 3 * _inputSize * _inputSize];
        _inputTensor = new DenseTensor<float>(_inputBuffer, new[] { 1, 3, _inputSize, _inputSize });
        _inputs = new List<NamedOnnxValue>(1)
        {
            NamedOnnxValue.CreateFromTensor(_inputName, _inputTensor)
        };
    }

    private InferenceSession CreateSession(string modelPath)
    {
        // 优先使用 TensorRT/CUDA，失败时自动回退 CPU，保证没有 GPU 环境也能启动。
        try
        {
            using var options = CreateSessionOptions();
            options.AppendExecutionProvider_Tensorrt(0);
            var session = new InferenceSession(modelPath, options);
            ExecutionProvider = "TensorRT";
            return session;
        }
        catch
        {
        }

        try
        {
            using var options = CreateSessionOptions();
            options.AppendExecutionProvider_CUDA(0);
            var session = new InferenceSession(modelPath, options);
            ExecutionProvider = "CUDA";
            return session;
        }
        catch
        {
        }

        using var cpuOptions = CreateSessionOptions();
        var cpuSession = new InferenceSession(modelPath, cpuOptions);
        ExecutionProvider = "CPU";
        return cpuSession;
    }

    private static SessionOptions CreateSessionOptions()
    {
        return new SessionOptions
        {
            LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_WARNING,
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            ExecutionMode = ExecutionMode.ORT_PARALLEL,
            EnableMemoryPattern = true,
            EnableCpuMemArena = true
        };
    }

    public Mat PredictDepth(Mat bgrImage, out long inferenceMs, bool resizeToOriginal = false)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Mat? resized = null;
        Mat? depthOutput = null;

        try
        {
            var modelInput = bgrImage;
            if (bgrImage.Width != _inputSize || bgrImage.Height != _inputSize)
            {
                resized = new Mat();
                Cv2.Resize(bgrImage, resized, new OpenCvSharp.Size(_inputSize, _inputSize), interpolation: InterpolationFlags.Area);
                modelInput = resized;
            }

            // 将 BGR 图像归一化到模型需要的 NCHW float 输入。
            FillInputTensor(modelInput);

            using var results = _session.Run(_inputs);
            var tensor = results.First().AsTensor<float>();
            var dimensions = tensor.Dimensions.ToArray();
            var outputHeight = dimensions.Length >= 2 ? dimensions[^2] : _inputSize;
            var outputWidth = dimensions.Length >= 1 ? dimensions[^1] : _inputSize;

            depthOutput = new Mat(outputHeight, outputWidth, MatType.CV_32FC1);
            CopyTensorToMat(tensor, depthOutput, outputHeight * outputWidth);

            Mat finalDepth;
            if (resizeToOriginal && (outputWidth != bgrImage.Width || outputHeight != bgrImage.Height))
            {
                finalDepth = new Mat();
                Cv2.Resize(depthOutput, finalDepth, new OpenCvSharp.Size(bgrImage.Width, bgrImage.Height), interpolation: InterpolationFlags.Area);
                depthOutput.Dispose();
                depthOutput = null;
            }
            else
            {
                finalDepth = depthOutput;
                depthOutput = null;
            }

            stopwatch.Stop();
            inferenceMs = stopwatch.ElapsedMilliseconds;
            _totalInferenceMs += inferenceMs;
            _inferenceCount++;

            return finalDepth;
        }
        finally
        {
            resized?.Dispose();
            depthOutput?.Dispose();
        }
    }

    public static Mat CreateVisualization(Mat depthMap)
    {
        using var normalized = new Mat();
        Cv2.Normalize(depthMap, normalized, 0, 255, NormTypes.MinMax, MatType.CV_8UC1);

        var colored = new Mat();
        Cv2.ApplyColorMap(normalized, colored, ColormapTypes.Inferno);
        return colored;
    }

    public static double CalculateObstaclePercent(Mat depthMap)
    {
        var groundStartY = (int)(depthMap.Height * 0.4);
        var (_, groundPercentile30) = GetGroundPercentiles(depthMap, groundStartY);

        var obstacleCount = 0;
        var totalPixels = depthMap.Width * depthMap.Height;

        unsafe
        {
            var srcPtr = (float*)depthMap.DataPointer;
            var step = (int)(depthMap.Step() / sizeof(float));

            for (var y = 0; y < depthMap.Height; y++)
            {
                var rowPtr = srcPtr + y * step;
                for (var x = 0; x < depthMap.Width; x++)
                {
                    if (rowPtr[x] >= groundPercentile30)
                    {
                        obstacleCount++;
                    }
                }
            }
        }

        return (double)obstacleCount / totalPixels * 100.0;
    }

    public static double CalculateCenterPathObstaclePercent(Mat depthMap)
    {
        var groundStartY = (int)(depthMap.Height * 0.4);
        var (_, groundPercentile30) = GetGroundPercentiles(depthMap, groundStartY);

        var left = (int)(depthMap.Width * 0.34);
        var right = (int)(depthMap.Width * 0.66);
        var top = (int)(depthMap.Height * 0.34);
        var bottom = (int)(depthMap.Height * 0.88);
        var obstacleCount = 0;
        var totalPixels = Math.Max(1, (right - left) * (bottom - top));

        unsafe
        {
            var srcPtr = (float*)depthMap.DataPointer;
            var step = (int)(depthMap.Step() / sizeof(float));

            for (var y = top; y < bottom; y++)
            {
                var rowPtr = srcPtr + y * step;
                for (var x = left; x < right; x++)
                {
                    if (rowPtr[x] >= groundPercentile30)
                    {
                        obstacleCount++;
                    }
                }
            }
        }

        return (double)obstacleCount / totalPixels * 100.0;
    }

    public static DepthSceneStats AnalyzeScene(Mat depthMap, double focusDarkThresholdPercent)
    {
        double minVal;
        double maxVal;
        Cv2.MinMaxLoc(depthMap, out minVal, out maxVal);

        var range = maxVal - minVal;
        if (range <= double.Epsilon)
        {
            return new DepthSceneStats(100, 0, 100, 100, true);
        }

        var darkThreshold = minVal + range * 0.08;
        var brightThreshold = minVal + range * 0.45;
        var darkCount = 0;
        var brightCount = 0;
        var topDarkCount = 0;
        var topPixelCount = 0;
        var focusDarkCount = 0;
        var focusPixelCount = 0;
        var totalPixels = depthMap.Width * depthMap.Height;
        var topBottom = depthMap.Height / 2;
        var focusLeft = (int)(depthMap.Width * 0.22);
        var focusRight = (int)(depthMap.Width * 0.78);
        var focusTop = (int)(depthMap.Height * 0.12);
        var focusBottom = (int)(depthMap.Height * 0.68);

        unsafe
        {
            var srcPtr = (float*)depthMap.DataPointer;
            var step = (int)(depthMap.Step() / sizeof(float));

            for (var y = 0; y < depthMap.Height; y++)
            {
                var rowPtr = srcPtr + y * step;
                for (var x = 0; x < depthMap.Width; x++)
                {
                    var depth = rowPtr[x];
                    if (depth <= darkThreshold)
                    {
                        darkCount++;
                    }

                    if (depth >= brightThreshold)
                    {
                        brightCount++;
                    }

                    if (y < topBottom)
                    {
                        topPixelCount++;
                        if (depth <= darkThreshold)
                        {
                            topDarkCount++;
                        }
                    }

                    if (x >= focusLeft && x < focusRight && y >= focusTop && y < focusBottom)
                    {
                        focusPixelCount++;
                        if (depth <= darkThreshold)
                        {
                            focusDarkCount++;
                        }
                    }
                }
            }
        }

        var darkPercent = (double)darkCount / totalPixels * 100.0;
        var brightPercent = (double)brightCount / totalPixels * 100.0;
        var topDarkPercent = topPixelCount == 0 ? 0 : (double)topDarkCount / topPixelCount * 100.0;
        var focusDarkPercent = focusPixelCount == 0 ? 0 : (double)focusDarkCount / focusPixelCount * 100.0;
        var topDarkThresholdPercent = Math.Min(95.0, focusDarkThresholdPercent + 6.0);
        var fullDarkThresholdPercent = Math.Min(95.0, focusDarkThresholdPercent + 4.0);
        var likelyInvalidPassScene = focusDarkPercent >= focusDarkThresholdPercent
            || (topDarkPercent >= topDarkThresholdPercent && darkPercent >= 60.0)
            || (darkPercent >= fullDarkThresholdPercent && brightPercent <= 18.0);

        return new DepthSceneStats(darkPercent, brightPercent, topDarkPercent, focusDarkPercent, likelyInvalidPassScene);
    }

    private static (float Percentile5, float Percentile30) GetGroundPercentiles(Mat depthMap, int groundStartY)
    {
        // 参考原深度图测试项目：取画面下半部分作地面候选区域，用分位数构建通行阈值。
        var groundPixels = depthMap.Width * (depthMap.Height - groundStartY);
        var groundDepths = new float[groundPixels];
        var index = 0;

        unsafe
        {
            var srcPtr = (float*)depthMap.DataPointer;
            var step = (int)(depthMap.Step() / sizeof(float));

            for (var y = groundStartY; y < depthMap.Height; y++)
            {
                var rowPtr = srcPtr + y * step;
                for (var x = 0; x < depthMap.Width; x++)
                {
                    groundDepths[index++] = rowPtr[x];
                }
            }
        }

        Array.Sort(groundDepths);
        return (groundDepths[(int)(groundDepths.Length * 0.05)], groundDepths[(int)(groundDepths.Length * 0.30)]);
    }

    private void FillInputTensor(Mat bgrImage)
    {
        const float scale = 1.0f / 255.0f;
        const float meanR = 0.485f;
        const float meanG = 0.456f;
        const float meanB = 0.406f;
        const float stdR = 0.229f;
        const float stdG = 0.224f;
        const float stdB = 0.225f;

        var planeSize = _inputSize * _inputSize;

        unsafe
        {
            fixed (float* inputPtr = _inputBuffer)
            {
                var rPtr = inputPtr;
                var gPtr = inputPtr + planeSize;
                var bPtr = inputPtr + planeSize * 2;
                var srcPtr = (byte*)bgrImage.DataPointer;
                var step = bgrImage.Step();

                for (var y = 0; y < _inputSize; y++)
                {
                    var rowPtr = srcPtr + y * step;
                    var rowOffset = y * _inputSize;

                    for (var x = 0; x < _inputSize; x++)
                    {
                        var srcOffset = x * 3;
                        var dstOffset = rowOffset + x;
                        var b = rowPtr[srcOffset] * scale;
                        var g = rowPtr[srcOffset + 1] * scale;
                        var r = rowPtr[srcOffset + 2] * scale;

                        rPtr[dstOffset] = (r - meanR) / stdR;
                        gPtr[dstOffset] = (g - meanG) / stdG;
                        bPtr[dstOffset] = (b - meanB) / stdB;
                    }
                }
            }
        }
    }

    private static void CopyTensorToMat(Tensor<float> tensor, Mat destination, int totalPixels)
    {
        if (tensor is DenseTensor<float> denseTensor)
        {
            var outputSpan = denseTensor.Buffer.Span;
            unsafe
            {
                fixed (float* srcPtr = outputSpan)
                {
                    var dstPtr = (float*)destination.DataPointer;
                    var copyCount = Math.Min(totalPixels, outputSpan.Length);
                    Buffer.MemoryCopy(srcPtr, dstPtr, totalPixels * sizeof(float), copyCount * sizeof(float));
                }
            }

            return;
        }

        var outputData = tensor.ToArray();
        unsafe
        {
            fixed (float* srcPtr = outputData)
            {
                var dstPtr = (float*)destination.DataPointer;
                var copyCount = Math.Min(totalPixels, outputData.Length);
                Buffer.MemoryCopy(srcPtr, dstPtr, totalPixels * sizeof(float), copyCount * sizeof(float));
            }
        }
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}

public readonly record struct DepthSceneStats(
    double DarkPercent,
    double BrightPercent,
    double TopDarkPercent,
    double FocusDarkPercent,
    bool LikelyInvalidPassScene);
