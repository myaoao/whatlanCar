import argparse
import json
import sys
from pathlib import Path

import cv2
import numpy as np
import onnx
import pandas as pd
import torch
from sklearn.model_selection import train_test_split
from torch import nn
from torch.utils.data import DataLoader, Dataset
from tqdm import tqdm


IMG_SIZE = (160, 120)
TURN_DEADZONE = 3

for stream in (sys.stdout, sys.stderr):
    if hasattr(stream, "reconfigure"):
        stream.reconfigure(encoding="utf-8", errors="replace")

torch.backends.cudnn.benchmark = True


def read_image(path: Path, grayscale: bool = False) -> np.ndarray:
    flag = cv2.IMREAD_GRAYSCALE if grayscale else cv2.IMREAD_COLOR
    image = cv2.imread(str(path), flag)
    if image is None:
        raise FileNotFoundError(path)
    image = cv2.resize(image, IMG_SIZE, interpolation=cv2.INTER_AREA)
    if grayscale:
        image = image[:, :, None]
    else:
        image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    return image.astype(np.float32) / 255.0


def make_labels(row: pd.Series) -> tuple[int, int, int]:
    w = int(row.get("w", 0))
    s = int(row.get("s", 0))
    jump = int(row.get("jump", 0))
    mouse_dx = float(row.get("mouse_dx", 0))

    if s:
        move = 2
    elif w:
        move = 1
    else:
        move = 0

    if mouse_dx < -TURN_DEADZONE:
        turn = 1
    elif mouse_dx > TURN_DEADZONE:
        turn = 2
    else:
        turn = 0

    return move, turn, 1 if jump else 0


def add_label_columns(df: pd.DataFrame) -> pd.DataFrame:
    labels = df.apply(make_labels, axis=1, result_type="expand")
    labels.columns = ["move_label", "turn_label", "jump_label"]
    return pd.concat([df.reset_index(drop=True), labels.reset_index(drop=True)], axis=1)


def label_counts(df: pd.DataFrame, column: str, classes: int) -> list[int]:
    counts = df[column].value_counts().to_dict()
    return [int(counts.get(i, 0)) for i in range(classes)]


def class_weights(counts: list[int], device: torch.device) -> torch.Tensor:
    total = sum(counts)
    weights = [total / max(1, len(counts) * count) for count in counts]
    clipped = [min(10.0, max(0.25, weight)) for weight in weights]
    return torch.tensor(clipped, dtype=torch.float32, device=device)


def load_manifest(data_root: Path) -> pd.DataFrame:
    rows = []
    for manifest in data_root.rglob("frame_manifest.csv"):
        session = manifest.parent
        df = pd.read_csv(manifest)
        df["session_dir"] = str(session)
        rows.append(df)

    if not rows:
        raise FileNotFoundError(
            f"No frame_manifest.csv found under {data_root}. "
            "Collect new data with the updated Data Collector first."
        )

    merged = pd.concat(rows, ignore_index=True)
    for col in ["frame_path", "depth_path", "minimap_path"]:
        merged[col] = merged.apply(lambda r: str(Path(r["session_dir"]) / str(r[col])), axis=1)
    return merged


class PolicyDataset(Dataset):
    def __init__(self, df: pd.DataFrame, use_depth: bool, use_minimap: bool):
        self.df = df.reset_index(drop=True)
        self.use_depth = use_depth
        self.use_minimap = use_minimap

    def __len__(self) -> int:
        return len(self.df)

    def __getitem__(self, idx: int):
        row = self.df.iloc[idx]
        channels = [read_image(Path(row["frame_path"]), grayscale=False)]
        if self.use_depth:
            channels.append(read_image(Path(row["depth_path"]), grayscale=False))
        if self.use_minimap:
            channels.append(read_image(Path(row["minimap_path"]), grayscale=False))

        image = np.concatenate(channels, axis=2)
        image = torch.from_numpy(image).permute(2, 0, 1)
        move = int(row["move_label"])
        turn = int(row["turn_label"])
        jump = int(row["jump_label"])
        return image, torch.tensor(move), torch.tensor(turn), torch.tensor(jump, dtype=torch.float32)


class SmallPolicyNet(nn.Module):
    def __init__(self, in_channels: int):
        super().__init__()
        self.backbone = nn.Sequential(
            nn.Conv2d(in_channels, 24, 5, stride=2, padding=2),
            nn.BatchNorm2d(24),
            nn.ReLU(inplace=True),
            nn.Conv2d(24, 48, 3, stride=2, padding=1),
            nn.BatchNorm2d(48),
            nn.ReLU(inplace=True),
            nn.Conv2d(48, 96, 3, stride=2, padding=1),
            nn.BatchNorm2d(96),
            nn.ReLU(inplace=True),
            nn.Conv2d(96, 128, 3, stride=2, padding=1),
            nn.BatchNorm2d(128),
            nn.ReLU(inplace=True),
            nn.AdaptiveAvgPool2d(1),
            nn.Flatten(),
        )
        self.move = nn.Linear(128, 3)
        self.turn = nn.Linear(128, 3)
        self.jump = nn.Linear(128, 1)

    def forward(self, x):
        features = self.backbone(x)
        return self.move(features), self.turn(features), self.jump(features)


def run_epoch(model, loader, optimizer, device, move_weights, turn_weights, jump_pos_weight):
    model.train(optimizer is not None)
    total_loss = 0.0
    total = 0
    correct_move = 0
    correct_turn = 0
    correct_jump = 0
    move_ce = nn.CrossEntropyLoss(weight=move_weights)
    turn_ce = nn.CrossEntropyLoss(weight=turn_weights)
    bce = nn.BCEWithLogitsLoss(pos_weight=jump_pos_weight)

    for image, move, turn, jump in tqdm(loader, leave=False):
        image = image.to(device)
        move = move.to(device)
        turn = turn.to(device)
        jump = jump.to(device)

        if optimizer is not None:
            optimizer.zero_grad(set_to_none=True)

        move_logits, turn_logits, jump_logits = model(image)
        loss = move_ce(move_logits, move) + turn_ce(turn_logits, turn) + bce(jump_logits.squeeze(1), jump)

        if optimizer is not None:
            loss.backward()
            optimizer.step()

        batch = image.size(0)
        total += batch
        total_loss += loss.item() * batch
        correct_move += (move_logits.argmax(1) == move).sum().item()
        correct_turn += (turn_logits.argmax(1) == turn).sum().item()
        correct_jump += ((torch.sigmoid(jump_logits.squeeze(1)) > 0.5) == (jump > 0.5)).sum().item()

    return {
        "loss": total_loss / max(1, total),
        "move_acc": correct_move / max(1, total),
        "turn_acc": correct_turn / max(1, total),
        "jump_acc": correct_jump / max(1, total),
    }


def export_onnx(model, out_path: Path, in_channels: int, device):
    model.eval()
    dummy = torch.randn(1, in_channels, IMG_SIZE[1], IMG_SIZE[0], device=device)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    torch.onnx.export(
        model,
        dummy,
        str(out_path),
        input_names=["input"],
        output_names=["move_logits", "turn_logits", "jump_logit"],
        opset_version=18,
        dynamic_axes={"input": {0: "batch"}},
    )
    exported = onnx.load(str(out_path), load_external_data=True)
    exported.ir_version = min(exported.ir_version, 9)
    onnx.save_model(exported, str(out_path), save_as_external_data=False)
    external_data = out_path.with_suffix(out_path.suffix + ".data")
    if external_data.exists():
        external_data.unlink()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--data", required=True, help="Dataset root containing session folders")
    parser.add_argument("--out", required=True, help="Output ONNX path")
    parser.add_argument("--epochs", type=int, default=3)
    parser.add_argument("--batch-size", type=int, default=32)
    parser.add_argument("--lr", type=float, default=1e-3)
    parser.add_argument("--workers", type=int, default=2)
    parser.add_argument("--resume", action="store_true", help="Continue from existing .pt weights if present")
    parser.add_argument("--no-depth", action="store_true")
    parser.add_argument("--no-minimap", action="store_true")
    args = parser.parse_args()

    data_root = Path(args.data)
    out_path = Path(args.out)
    df = load_manifest(data_root)
    df = df[df["frame_path"].map(lambda p: Path(p).exists())]
    if not args.no_depth:
        df = df[df["depth_path"].map(lambda p: Path(p).exists())]
    if not args.no_minimap:
        df = df[df["minimap_path"].map(lambda p: Path(p).exists())]

    if len(df) < 20:
        raise RuntimeError(f"Too few frames: {len(df)}. Collect at least a few minutes first.")

    df = add_label_columns(df)
    move_counts = label_counts(df, "move_label", 3)
    turn_counts = label_counts(df, "turn_label", 3)
    jump_counts = label_counts(df, "jump_label", 2)
    mouse_dx = pd.to_numeric(df.get("mouse_dx", 0), errors="coerce").fillna(0)
    print(
        "label_counts "
        f"move(stop,forward,back)={move_counts} "
        f"turn(none,left,right)={turn_counts} "
        f"jump(no,yes)={jump_counts}"
    )
    print(
        "mouse_dx "
        f"nonzero={(mouse_dx.abs() > TURN_DEADZONE).sum()} "
        f"min={mouse_dx.min():.1f} max={mouse_dx.max():.1f} "
        f"mean_abs={mouse_dx.abs().mean():.2f}"
    )
    if turn_counts[1] == 0 and turn_counts[2] == 0:
        raise RuntimeError(
            "No mouse turn labels were found: mouse_dx is always inside the deadzone. "
            "Update and run tools/vm_key_sender.ps1 in the VM, verify the collector shows non-zero mouse_dx while turning, then record again."
        )

    train_df, val_df = train_test_split(df, test_size=0.2, random_state=42, shuffle=True)
    use_depth = not args.no_depth
    use_minimap = not args.no_minimap
    in_channels = 3 + (3 if use_depth else 0) + (3 if use_minimap else 0)
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"frames={len(df)} train={len(train_df)} val={len(val_df)} device={device} channels={in_channels}")
    move_weights = class_weights(move_counts, device)
    turn_weights = class_weights(turn_counts, device)
    positive_jumps = max(1, jump_counts[1])
    negative_jumps = max(1, jump_counts[0])
    jump_pos_weight = torch.tensor([min(10.0, negative_jumps / positive_jumps)], dtype=torch.float32, device=device)
    print(
        "loss_weights "
        f"move={move_weights.detach().cpu().tolist()} "
        f"turn={turn_weights.detach().cpu().tolist()} "
        f"jump_pos={jump_pos_weight.item():.3f}"
    )

    loader_kwargs = {
        "num_workers": max(0, args.workers),
        "pin_memory": device.type == "cuda",
        "persistent_workers": args.workers > 0,
    }
    train_loader = DataLoader(
        PolicyDataset(train_df, use_depth, use_minimap),
        batch_size=args.batch_size,
        shuffle=True,
        **loader_kwargs)
    val_loader = DataLoader(
        PolicyDataset(val_df, use_depth, use_minimap),
        batch_size=args.batch_size,
        shuffle=False,
        **loader_kwargs)
    model = SmallPolicyNet(in_channels).to(device)
    checkpoint_path = out_path.with_suffix(".pt")
    if args.resume and checkpoint_path.exists():
        state = torch.load(checkpoint_path, map_location=device)
        model.load_state_dict(state)
        print(f"resumed: {checkpoint_path}")
    elif args.resume:
        print(f"resume requested but no checkpoint found: {checkpoint_path}; training from scratch")

    optimizer = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=1e-4)

    best_val = 1e9
    best_state = None
    for epoch in range(1, args.epochs + 1):
        train = run_epoch(model, train_loader, optimizer, device, move_weights, turn_weights, jump_pos_weight)
        with torch.no_grad():
            val = run_epoch(model, val_loader, None, device, move_weights, turn_weights, jump_pos_weight)
        print(f"epoch={epoch} train={train} val={val}")
        if val["loss"] < best_val:
            best_val = val["loss"]
            best_state = {k: v.detach().cpu() for k, v in model.state_dict().items()}

    if best_state is not None:
        model.load_state_dict(best_state)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    torch.save(model.state_dict(), out_path.with_suffix(".pt"))
    export_onnx(model, out_path, in_channels, device)
    metadata = {
        "img_width": IMG_SIZE[0],
        "img_height": IMG_SIZE[1],
        "in_channels": in_channels,
        "use_depth": use_depth,
        "use_minimap": use_minimap,
        "move_labels": ["stop", "forward", "back"],
        "turn_labels": ["none", "left", "right"],
        "jump_labels": ["no", "yes"],
        "label_counts": {
            "move": move_counts,
            "turn": turn_counts,
            "jump": jump_counts,
        },
        "turn_deadzone": TURN_DEADZONE,
    }
    out_path.with_suffix(".json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"saved: {out_path}")


if __name__ == "__main__":
    main()
