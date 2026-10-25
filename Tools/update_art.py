"""Update Unity textures from the SVG files in ArtSources."""
from pathlib import Path
import io

import resvg_py
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]


def main():
    count = 0
    for source in sorted((ROOT / "ArtSources").rglob("*.svg")):
        relative = source.relative_to(ROOT / "ArtSources")
        targets = [
            (ROOT / "Assets" / folder / relative).with_suffix(extension)
            for folder in ("Art", "Resources")
            for extension in (".png", ".jpg", ".jpeg")
            if (ROOT / "Assets" / folder / relative).with_suffix(extension).is_file()
        ]
        if len(targets) != 1:
            raise ValueError(f"Expected one matching Unity texture for {relative}, found {len(targets)}")
        target = targets[0]
        image = Image.open(io.BytesIO(resvg_py.svg_to_bytes(
            svg_string=source.read_text(encoding="utf-8")
        )))
        with Image.open(target) as original:
            if image.size != original.size:
                raise ValueError(f"Keep {relative} at {original.size} pixels to preserve sprite slices")
        if target.suffix.lower() in {".jpg", ".jpeg"}:
            image.convert("RGB").save(target, quality=95)
        else:
            image.save(target, format="PNG")
        count += 1
    print(f"Updated {count} textures.")


if __name__ == "__main__":
    main()
