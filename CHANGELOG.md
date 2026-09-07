# Changelog

All notable changes to Sprite Editor are documented in this file.

## [0.2.1] - 2026-09-07

### Added

- Saved `TextureCompositor` assets can now be opened by double-clicking them in the Project window.
- The `TextureCompositor` Inspector now includes an **Open in Sprite Editor** button.

## [0.2.0] - 2026-09-07

### Added

- GPU-backed Drawing layers painted directly on the composite Preview.
- Brush color, size, hardness, eraser, and `[` / `]` size shortcuts.
- Live reflection across configurable horizontal and vertical axes.
- Horizontal, vertical, grid, and radial repetition up to 64 copies.
- Regular and alternating-mirror repeat modes.
- Continue and per-cell/per-sector Clip boundary modes.
- Drawing textures stored as sub-assets of saved `TextureCompositor` assets.

### Fixed

- Exact Euclidean EDT now keeps the correct parabola envelope on both sides of a shape, fixing
  missing Outline/SDF pixels next to Drawing layers and other inputs.

## [0.1.0] - 2026-09-07

### Added

- Layered texture composition with file, color fill, gradient, outline, and SDF layers.
- Photoshop-style nested, non-isolated groups with drag-and-drop reordering.
- Outline and SDF inputs targeting either the previous item or a selected layer/group.
- Exact Euclidean, approximate Euclidean, Manhattan, and Chebyshev distance algorithms.
- Normal, Multiply, and true RGBA Overwrite blend modes.
- Per-layer material modifiers and editable layer transforms.
- Resizable side-by-side preview and settings layout.
- Sequential document-local `Layer n` names.
- PNG export and reusable `TextureCompositor` assets.
