# Unity Sprite Editor

Editor-only layered texture compositor for Unity.

Open it from **Window > Sprite Editor**. A composition can stay temporary, be saved as a
`TextureCompositor` asset, previewed in the editor, and exported to PNG. PNG files exported
inside `Assets` are imported as single Sprite assets automatically.

## Installation

Requires Unity 6 (`6000.0`) or newer. Burst and Collections are declared as package
dependencies and are installed by Unity Package Manager.

In Unity, open **Window > Package Management > Package Manager**, choose
**Install package from git URL**, and enter:

```text
https://github.com/DCFApixels/Unity-SpriteEditor.git
```

Alternatively, add the package directly to `Packages/manifest.json`:

```json
"com.dcfa_pixels.sprite-editor": "https://github.com/DCFApixels/Unity-SpriteEditor.git"
```

To track a specific release, append its tag to the URL, for example `#v0.2.0`.

## Layer stack

The list is displayed from top to bottom, like Photoshop. Rendering walks the list from the
bottom layer to the top layer. Supported layers:

- File
- Drawing
- Color Fill
- Gradient
- Outline
- SDF
- Group

Groups are non-isolated structural containers. Their child layers are blended into the parent
stack as if the group did not introduce an intermediate color surface. Nested groups are
supported. Group opacity, blend mode, transforms, and modifiers are intentionally reserved for
a later isolated-group implementation.

Use the row context menu to reorder layers, move a layer into the group above it, move it out of
a group, ungroup it, or add children directly to a group. Layers can also be dragged by their
handle: insertion lines reorder or move them between hierarchy levels, while dropping on the
highlighted center of a group moves the layer inside it.

New layers and groups receive document-local sequential names (`Layer 1`, `Layer 2`, and so on).
The counter is serialized with the compositor and does not reuse numbers after deletion.

## Drawing on the Preview

Select a Drawing layer and paint directly on the left Preview. The brush writes only to the
selected layer while the displayed result still respects the complete layer order, groups,
opacity, blend modes, modifiers, and effects. Use `[` and `]` to decrease or increase brush size.

The Preview header exposes brush color, size, hardness, Brush/Eraser mode, and a normalized pattern
center. `Mirror X` reflects across the vertical axis through that center; `Mirror Y` reflects across
the horizontal axis. Both can be enabled together.

Live repetition modes include Horizontal, Vertical, Grid, and Radial. The copy count is adjustable,
including independent X/Y counts for Grid. `Copy` repeats the stroke directly, while
`Alternate Mirror` reflects every second cell or sector. `Continue` allows brush dabs to cross a
cell/sector boundary; `Clip` confines every repeated copy to its own cell or radial sector.

Saved Drawing layers keep their pixel texture as a sub-asset of the `TextureCompositor`, so a
composition remains self-contained.

## Outline and SDF

Outline and SDF can consume either the item directly below them in the same container or a
specific layer/group selected by stable ID. If the input is a group, SpriteEditor builds one
mask by combining the alpha channels of all visible descendant layers. The group's color is not
composited separately. Self-references and cyclic effect dependencies are rejected.

Distance algorithms include exact Euclidean EDT, approximate Euclidean (8-neighbour chamfer),
Manhattan, and Chebyshev. Outline width, outline softness, SDF maximum distance, and transform
position are expressed in output pixels. Pivot is normalized, scale is a multiplier, and
rotation is expressed in degrees.

## Blend modes

- Normal: standard source-over alpha composition.
- Multiply: standard separable multiply with alpha composition.
- Overwrite: replaces the complete RGBA pixel; layer opacity interpolates between the backdrop
  and replacement pixel.

Layer modifiers are materials applied in list order after that layer's transform.
