"""
Depth Anything 深度图测试/推理参考版。

依赖:
    pip install onnxruntime-gpu opencv-python numpy

用法:
    python depth_test_reference.py --image test.png
    python depth_test_reference.py --image test.png --model data/depth_anything_vits14.onnx

说明:
    这个文件提取自 whatlanCar 当前的深度推理逻辑，只保留模型推理、显示图生成、
    障碍比例、前方暗区和是否可通行判断，方便单独参考。
"""

from __future__ import annotations

import argparse
import time
from dataclasses import dataclass
from pathlib import Path

import cv2
import numpy as np
import onnxruntime as ort


DEFAULT_MODEL = Path(__file__).resolve().parent / "bin" / "Debug" / "net10.0-windows" / "data" / "depth_anything_vits14.onnx"


@dataclass(frozen=True)
class DepthSceneStats:
    dark_percent: float
    bright_percent: float
    top_dark_percent: float
    focus_dark_percent: float
    likely_invalid_pass_scene: bool


class DepthAnythingInference:
    def __init__(self, model_path: str | Path = DEFAULT_MODEL, input_size: int = 518) -> None:
        self.model_path = Path(model_path)
        self.input_size = input_size
        if not self.model_path.exists():
            raise FileNotFoundError(f"找不到深度模型: {self.model_path}")

        self.session, self.execution_provider = self._create_session(self.model_path)
        self.input_name = self.session.get_inputs()[0].name
        self.total_inference_ms = 0.0
        self.inference_count = 0

    @property
    def average_inference_ms(self) -> float:
        if self.inference_count == 0:
            return 0.0
        return self.total_inference_ms / self.inference_count

    @staticmethod
    def _create_session(model_path: Path) -> tuple[ort.InferenceSession, str]:
        available = set(ort.get_available_providers())
        provider_order: list[tuple[str, list[str]]] = [
            ("TensorRT", ["TensorrtExecutionProvider", "CUDAExecutionProvider", "CPUExecutionProvider"]),
            ("CUDA", ["CUDAExecutionProvider", "CPUExecutionProvider"]),
            ("CPU", ["CPUExecutionProvider"]),
        ]

        options = ort.SessionOptions()
        options.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_ALL

        for name, providers in provider_order:
            usable = [provider for provider in providers if provider in available]
            if not usable:
                continue

            try:
                return ort.InferenceSession(str(model_path), sess_options=options, providers=usable), name
            except Exception:
                continue

        raise RuntimeError("无法创建 ONNX Runtime Session，请检查 onnxruntime/onnxruntime-gpu 安装。")

    def predict_depth(self, bgr_image: np.ndarray, resize_to_original: bool = False) -> tuple[np.ndarray, float]:
        start = time.perf_counter()
        original_h, original_w = bgr_image.shape[:2]

        if original_w != self.input_size or original_h != self.input_size:
            model_input = cv2.resize(bgr_image, (self.input_size, self.input_size), interpolation=cv2.INTER_AREA)
        else:
            model_input = bgr_image

        input_tensor = self._preprocess(model_input)
        output = self.session.run(None, {self.input_name: input_tensor})[0]
        depth = np.squeeze(output).astype(np.float32)

        if resize_to_original and depth.shape[:2] != (original_h, original_w):
            depth = cv2.resize(depth, (original_w, original_h), interpolation=cv2.INTER_AREA)

        inference_ms = (time.perf_counter() - start) * 1000.0
        self.total_inference_ms += inference_ms
        self.inference_count += 1
        return depth, inference_ms

    def _preprocess(self, bgr_image: np.ndarray) -> np.ndarray:
        image = bgr_image.astype(np.float32) / 255.0
        image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

        mean = np.array([0.485, 0.456, 0.406], dtype=np.float32)
        std = np.array([0.229, 0.224, 0.225], dtype=np.float32)
        image = (image - mean) / std

        return np.transpose(image, (2, 0, 1))[None, :, :, :].astype(np.float32)


def crop_center_square(image: np.ndarray) -> np.ndarray:
    h, w = image.shape[:2]
    size = min(w, h)
    x = (w - size) // 2
    y = (h - size) // 2
    return image[y:y + size, x:x + size].copy()


def create_visualization(depth_map: np.ndarray) -> np.ndarray:
    min_val = float(np.min(depth_map))
    max_val = float(np.max(depth_map))
    if max_val <= min_val:
        normalized = np.zeros(depth_map.shape, dtype=np.uint8)
    else:
        normalized = ((depth_map - min_val) * 255.0 / (max_val - min_val)).clip(0, 255).astype(np.uint8)

    return cv2.applyColorMap(normalized, cv2.COLORMAP_INFERNO)


def get_ground_percentiles(depth_map: np.ndarray, ground_start_y: int) -> tuple[float, float]:
    ground_depths = depth_map[ground_start_y:, :].reshape(-1)
    return (
        float(np.percentile(ground_depths, 5)),
        float(np.percentile(ground_depths, 30)),
    )


def calculate_obstacle_percent(depth_map: np.ndarray) -> float:
    ground_start_y = int(depth_map.shape[0] * 0.4)
    _, ground_percentile30 = get_ground_percentiles(depth_map, ground_start_y)
    obstacle_mask = depth_map >= ground_percentile30
    return float(np.mean(obstacle_mask) * 100.0)


def analyze_scene(depth_map: np.ndarray, focus_dark_threshold_percent: float = 78.0) -> DepthSceneStats:
    min_val = float(np.min(depth_map))
    max_val = float(np.max(depth_map))
    value_range = max_val - min_val
    if value_range <= np.finfo(np.float32).eps:
        return DepthSceneStats(100.0, 0.0, 100.0, 100.0, True)

    dark_threshold = min_val + value_range * 0.08
    bright_threshold = min_val + value_range * 0.45

    dark_mask = depth_map <= dark_threshold
    bright_mask = depth_map >= bright_threshold

    h, w = depth_map.shape[:2]
    top_mask = dark_mask[: h // 2, :]
    focus_mask = dark_mask[int(h * 0.12): int(h * 0.68), int(w * 0.22): int(w * 0.78)]

    dark_percent = float(np.mean(dark_mask) * 100.0)
    bright_percent = float(np.mean(bright_mask) * 100.0)
    top_dark_percent = float(np.mean(top_mask) * 100.0) if top_mask.size else 0.0
    focus_dark_percent = float(np.mean(focus_mask) * 100.0) if focus_mask.size else 0.0

    top_dark_threshold_percent = min(95.0, focus_dark_threshold_percent + 6.0)
    full_dark_threshold_percent = min(95.0, focus_dark_threshold_percent + 4.0)
    likely_invalid_pass_scene = (
        focus_dark_percent >= focus_dark_threshold_percent
        or (top_dark_percent >= top_dark_threshold_percent and dark_percent >= 60.0)
        or (dark_percent >= full_dark_threshold_percent and bright_percent <= 18.0)
    )

    return DepthSceneStats(
        dark_percent=dark_percent,
        bright_percent=bright_percent,
        top_dark_percent=top_dark_percent,
        focus_dark_percent=focus_dark_percent,
        likely_invalid_pass_scene=likely_invalid_pass_scene,
    )


def judge_pass_status(
    obstacle_percent: float,
    scene_stats: DepthSceneStats,
    pass_threshold: float = 65.0,
    dark_threshold: float = 78.0,
) -> str:
    if obstacle_percent > pass_threshold:
        return f"无法通过（障碍 {obstacle_percent:.1f}% > 阈值 {pass_threshold:.1f}%）"

    if scene_stats.likely_invalid_pass_scene:
        return f"可能无法通过（前方暗区 {scene_stats.focus_dark_percent:.1f}% >= 阈值 {dark_threshold:.1f}%）"

    if obstacle_percent >= pass_threshold - 10.0:
        return f"谨慎通过（障碍 {obstacle_percent:.1f}% 接近阈值 {pass_threshold:.1f}%）"

    return f"可以通过（障碍 {obstacle_percent:.1f}% <= 阈值 {pass_threshold:.1f}%）"


def run_image_demo(
    image_path: str | Path,
    model_path: str | Path = DEFAULT_MODEL,
    pass_threshold: float = 65.0,
    dark_threshold: float = 78.0,
    crop_square: bool = True,
) -> None:
    image = cv2.imread(str(image_path))
    if image is None:
        raise FileNotFoundError(f"无法读取图片: {image_path}")

    if crop_square:
        image = crop_center_square(image)

    model = DepthAnythingInference(model_path)
    depth_map, inference_ms = model.predict_depth(image, resize_to_original=False)
    depth_preview = create_visualization(depth_map)
    obstacle_percent = calculate_obstacle_percent(depth_map)
    scene_stats = analyze_scene(depth_map, dark_threshold)
    status = judge_pass_status(obstacle_percent, scene_stats, pass_threshold, dark_threshold)

    print(f"执行器: {model.execution_provider}")
    print(f"推理时间: {inference_ms:.1f} ms")
    print(f"障碍比例: {obstacle_percent:.1f}%")
    print(f"暗区: 全图 {scene_stats.dark_percent:.1f}%, 上半区 {scene_stats.top_dark_percent:.1f}%, 前方 {scene_stats.focus_dark_percent:.1f}%")
    print(f"判断: {status}")

    cv2.imshow("capture", image)
    cv2.imshow("depth", depth_preview)
    cv2.waitKey(0)


def main() -> None:
    parser = argparse.ArgumentParser(description="Depth Anything 深度图推理测试参考")
    parser.add_argument("--image", required=True, help="输入图片路径")
    parser.add_argument("--model", default=str(DEFAULT_MODEL), help="depth_anything_vits14.onnx 路径")
    parser.add_argument("--pass-threshold", type=float, default=65.0, help="障碍阈值，默认 65")
    parser.add_argument("--dark-threshold", type=float, default=78.0, help="前方暗区阈值，默认 78")
    parser.add_argument("--no-crop", action="store_true", help="不裁切中间正方形")
    args = parser.parse_args()

    run_image_demo(
        image_path=args.image,
        model_path=args.model,
        pass_threshold=args.pass_threshold,
        dark_threshold=args.dark_threshold,
        crop_square=not args.no_crop,
    )


if __name__ == "__main__":
    main()
