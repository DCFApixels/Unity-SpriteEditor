# Changelog

All notable changes to Sprite Editor are documented in this file.

## [0.2.8] - 2026-09-07

### Fixed

- Undo and Redo keyboard shortcuts remain available while Sprite Editor suppresses other Unity
  shortcuts.

## [0.2.7] - 2026-09-07

### Fixed

- Painting inside an alternately mirrored repeat cell or radial sector now keeps the active brush
  copy under the cursor instead of reflecting the input a second time.

## [0.2.6] - 2026-09-07

### Fixed

- Drawing hotkeys no longer remain disabled after Unity leaves its global text-editing flag set
  while no IMGUI text field actually owns keyboard focus.

## [0.2.5] - 2026-09-07

### Fixed

- Unity global and contextual shortcuts are suspended while the Sprite Editor window has focus,
  preventing Unity commands such as Local/Global toggle from consuming Drawing hotkeys.

## [0.2.4] - 2026-09-07

### Added

- RMB temporarily erases a Drawing layer without changing the selected Brush/Eraser tool.
- Photoshop-style foreground/background brush colors with an `X` swap shortcut.

## [0.2.3] - 2026-09-07

### Added

- A persistent **Live Quality** slider controls painting-preview resolution from 12.5% to 100%.
- Setting Live Quality to 100% disables painting-preview downscaling.

## [0.2.2] - 2026-09-07

### Added

- Configurable Drawing brush stamp spacing from 1% to 400% of the brush diameter.

### Changed

- Live painting previews are throttled to 30 updates per second and rendered at a lower working
  resolution; the full-resolution preview is restored when the stroke ends.
- Brush stamps for a mouse segment and pattern repetitions are submitted in one GPU batch.
- Repeated-stamp deduplication now uses constant-time lookups instead of a quadratic scan.

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
