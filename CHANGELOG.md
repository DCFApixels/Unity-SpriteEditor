# Changelog

All notable changes to Sprite Editor are documented in this file.

## [Unreleased]

### Added

- Hold Shift during a brush/eraser stroke to lock its direction horizontally or vertically on
  the canvas. Shift-click connects the previous painted endpoint to the clicked point, including
  consecutive Shift-clicks, with the existing brush spacing, symmetry, repeat clipping, and Undo.

### Changed

- Keep output fields, layer rows, and drawing controls alive during value changes and selection.
  Rebuild only the affected subtree when the document, visible layer structure, or bound layer
  instance changes. Repeat modes, gradient modes, and effect inputs toggle existing controls.
- Synchronize fields through shared value bindings: update only changed values without emitting
  change events, preserve active text/caret and pointer interactions, and reconcile on focus or
  capture release. External notifications are coalesced instead of scheduling full UI rebuilds.
- Refresh modifier items only when their source list or contents change, and handle layer dragging
  with a pointer manipulator that cleans up capture on cancellation and detachment.

### Fixed

- Output width and height commit typed values on Enter or focus loss, so temporarily clearing
  a dimension while typing does not shrink the document and preview to a one-pixel strip.
- Selecting an unselected layer while editing its name or opacity no longer detaches the field.
- Brush Step no longer rewrites and clamps its text during typing; the model remains clamped and
  the field is reconciled when editing ends.
- External changes no longer discard UI refreshes while a field is focused. Effect and modifier
  windows also synchronize directly after Undo/Redo.
- Preview errors reuse one HelpBox, and cancelled layer drops restore the row's original margin.

## [0.4.0] - 2026-09-08

### Changed

- Rebuilt every Sprite Editor window and its `TextureCompositor` custom inspector with UI Toolkit.
- Replaced the main window with a resizable two-pane layout, retained-mode preview, recursive layer
  tree, drawing toolbar, and UI Toolkit drag-and-drop while preserving the existing editing workflow.
- Preserved direct Preview painting, brush and eraser hotkeys, groups, effect targets, modifiers,
  layer selection, Undo/Redo, and saved-compositor reopening across the migration.
- The Drawing `Step` value can now be scrubbed by dragging its label horizontally.

### Performance

- Settings panels and layer rows now rebuild only when editor state changes instead of every GUI
  event, and the Preview caches its image layout so the checkerboard is not repainted unnecessarily.

### Fixed

- Numeric label scrubbing no longer loses pointer capture after its first value change.
- Text and numeric field editing is no longer interrupted by a delayed external refresh while the
  active UI Toolkit field has keyboard focus.
- Drawing symmetry and repetition guides are hidden immediately after selecting a non-Drawing layer,
  preventing stale guide geometry from appearing offset after the Preview layout changes.

## [0.3.2] - 2026-09-07

### Changed

- Replaced the layer-selection radio button with a color-highlighted selected row.
- A layer or group can now be selected by clicking its free row area without interfering with
  visibility toggles, drag handles, or other row controls.

## [0.3.1] - 2026-09-07

### Changed

- Parallelized exact Euclidean distance-transform rows and columns with Burst jobs.
- Run the two approximate distance fields concurrently and parallelized their initialization and
  signed-output passes.
- SDF two-color gradients and Outline output now write directly into the destination Texture2D
  native buffer, removing intermediate pixel arrays and redundant `SetPixelData` copies.
- Reuse the signed-distance output buffer as the distance-to-object workspace and skip clearing
  temporary arrays that are fully overwritten.

## [0.3.0] - 2026-09-07

### Added

- Added every blend mode exposed by DataMath `DMBlend`: None, Add, Subtract, Multiply, Divide,
  Screen, Overlay, Darken, Lighten, Dodge, Burn, Linear Dodge, Linear Burn, Linear Light,
  Linear Light Add/Sub, Vivid Light, Pin Light, Hard Mix, Hard Light, Soft Light, Difference,
  Exclusion, Negation, and Overwrite.

### Changed

- Standard blend modes now use Photoshop/PDF-style source-over alpha composition, so their blend
  function affects only overlapping coverage and non-overlapping pixels remain visible.
- RGB blend functions run in an sRGB Photoshop-style blend space even when the Unity project uses
  Linear color space.
- Existing serialized Normal, Multiply, and Overwrite mode values remain compatible.

## [0.2.9] - 2026-09-07

### Fixed

- Clip repetition mode now confines an entire continuous stroke to the cell or radial sector where
  it started instead of continuing after the cursor crosses into another repeated shape.

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
