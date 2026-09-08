# Unity Sprite Editor

Editor-only layered texture compositor for Unity. The editor interface is built with UI Toolkit,
including its resizable Preview, recursive layer tree, inspectors, and auxiliary editor windows.

Open it from **Window > Sprite Editor**. A composition can stay temporary, be saved as a
`TextureCompositor` asset, previewed in the editor, and exported to PNG. PNG files exported
inside `Assets` are imported as single Sprite assets automatically.

Saved compositions can be reopened by double-clicking their `TextureCompositor` asset in the
Project window, or by selecting the asset and pressing **Open in Sprite Editor** in the Inspector.

## Installation

All implementation files live in `src/`, including the editor-only assembly definition,
layer implementations, editor windows, and shaders. Package metadata, documentation, and
the license remain at the repository root.

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

To track a specific published release, append its tag to the URL, for example `#v0.3.2`.

## Preview transform tool

Select a non-group layer and enable **Transform** in the Preview header (shortcut **T**).
Drag inside the frame to move, drag its eight handles to resize, or drag the round handle
to rotate around the layer pivot. **Shift** constrains movement to an axis, preserves resize
proportions, or snaps rotation to 15°. **Escape** cancels the current drag; **T** or **Enter**
exits the tool. Each drag is one Undo action. Drawing is suspended while this tool is active.
The frame represents the layer's full source canvas, including transparent pixels, and edits
the same Position, Scale, and Rotation values as the layer settings. Groups remain pass-through
containers without a transform.

**Tiling** in the Transform settings (also shown below the Preview header while Transform is
active) controls both axes: **Clip** leaves pixels outside the frame transparent, **Repeat**
tiles the source canvas, and **Mirror** alternates reflected tiles. Clip is the default, including
for existing documents. The mode is saved per layer, supports Undo, and applies to Preview and
export without changing the source texture's import settings. For Drawing layers these are
rendered copies; painting still edits the source inside the primary tile, independently of brush repeat.

## Layer stack

Use a layer's **… > Convert to Drawing** menu to rasterize it, including an existing Drawing:

- **Keep Transform** captures the source pixels and keeps Pivot, Position, Scale, Rotation, and
  Tiling editable. The transform is not included in the captured pixels.
- **Apply Transform** captures the transformed pixels at the full document resolution, including
  Repeat/Mirror tiling, then resets the entire Transform to its default (including Clip).
  Content outside the document canvas is cropped when applying the transform.

The replacement keeps its name, ID, stack position, visibility, opacity, blend mode, and FX.
FX stay live after the transform instead of being applied twice. Drawing brush/symmetry/repeat
settings are retained when converting an existing Drawing. Outline/SDF sources are resolved
using their current previous/specific target, but the resulting Drawing is a static snapshot.
Undo/Redo covers the replacement and its pixel textures, including temporary compositions.

Groups are merged in color against transparency, including their visible descendants and
their transforms/FX. The replacement uses Normal blending, opacity 1, and an identity Transform;
groups have no active transform, so their two conversion modes produce the same result.
A confirmation warns that pass-through interactions with external layers can change and
effects targeting removed descendants lose their targets. References to the converted layer's
own ID are retained.

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

New layers receive document-local sequential names (`Layer 1`, `Layer 2`, and so on).
Groups use a separate sequence (`Group 1`, `Group 2`, and so on). Both counters are serialized
with the compositor and do not reuse numbers after deletion. Existing names are preserved;
number detection includes nested groups and layers.

## Drawing on the Preview

Select a Drawing layer and paint directly on the left Preview. The brush writes only to the
selected layer while the displayed result still respects the complete layer order, groups,
opacity, blend modes, modifiers, and effects. LMB uses the selected Brush/Eraser tool; RMB temporarily
erases without changing that selection. The foreground and background color swatches can both be
edited, and `X` swaps them. Use `[` and `]` to decrease or increase brush size.
Hold Shift during a stroke to lock it horizontally or vertically, chosen by the first movement
and held until Shift is released. The axes follow the canvas, including on transformed layers.
Shift-click connects the last painted endpoint to the clicked point; repeated Shift-clicks draw
connected straight segments. To start an independent axis-aligned line, click its start without
Shift, then hold Shift while dragging. These modes also work with the eraser, brush spacing,
mirroring, and repetition. Clip still limits a connection to the repeat shape containing its
starting endpoint. Undo/Redo, clearing that layer, and changing documents reset the connection anchor;
connections never carry over from another drawing layer or a different canvas size.
`Step` controls the distance between consecutive brush stamps as a percentage of brush diameter:
lower values produce a smoother stroke, while higher values are faster and can produce dotted lines.

While the Sprite Editor window has focus, Unity's global and contextual Shortcut Manager commands
are suspended so they cannot consume drawing hotkeys. Text and numeric fields continue to receive
normal keyboard input. `Ctrl/Cmd+Z` remains available for Undo; use `Ctrl+Y` or
`Ctrl/Cmd+Shift+Z` for Redo. Unity shortcuts are restored as soon as the window loses focus or closes.

`Live Quality` controls the temporary Preview resolution used while a stroke is active, from 12.5%
to 100%. The default is 37.5%; selecting 100% disables downscaling. This is an editor preference and
does not alter the saved composition or exported texture.

The Preview header exposes brush color, size, hardness, Brush/Eraser mode, and a normalized pattern
center. `Mirror X` reflects across the vertical axis through that center; `Mirror Y` reflects across
the horizontal axis. Both can be enabled together.

Live repetition modes include Horizontal, Vertical, Grid, and Radial. The copy count is adjustable,
including independent X/Y counts for Grid. `Copy` repeats the stroke directly, while
`Alternate Mirror` reflects every second cell or sector. `Continue` allows brush dabs to cross a
cell/sector boundary; `Clip` confines the complete drag stroke to the cell or radial sector where it
started. Drawing from any repeated or alternately mirrored cell/sector keeps the active copy directly
under the cursor.

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

Distance transforms use Burst and Native Collections. Exact Euclidean rows and columns run in
parallel, while SDF and Outline output is written directly into the destination texture buffer to
avoid managed pixel arrays and redundant copies.

## Blend modes

- Photoshop-style modes use source-over alpha composition. The blend function affects RGB only
  where source and backdrop alpha overlap; non-overlapping pixels preserve the color and alpha of
  the layer that is present.
- Supported modes: Normal, Add, Subtract, Multiply, Divide, Screen, Overlay, Darken, Lighten,
  Color Dodge, Color Burn, Linear Dodge (Add), Linear Burn, Linear Light, Linear Light Add/Sub,
  Vivid Light, Pin Light, Hard Mix, Hard Light, Soft Light, Difference, Exclusion, and Negation.
- None leaves the existing composite unchanged.
- Overwrite: replaces the complete RGBA pixel; layer opacity interpolates between the backdrop
  and replacement pixel.

For direct Photoshop comparisons, use an sRGB document with **Blend RGB Colors Using Gamma**
disabled. Other Photoshop document profiles or gamma-blending settings intentionally produce
different RGB values, although alpha composition remains the same.

Normal, Multiply, and Overwrite retain their existing serialized values, so compositions created
with earlier package versions keep the same modes after upgrading.

Layer modifiers are materials applied in list order after that layer's transform.

## UI maintenance

Create controls when their document or layer is bound. Register model readers in
`SpriteEditorUI.ValueBindings` and update through `Refresh`, which uses
`SetValueWithoutNotify` only when values differ. An active field owns its unfinished input;
normal model refreshes reconcile it after focus/capture release. Explicit Undo/Redo can force
model values into the existing controls. Bind every new editable field, including fields whose
values can change through hotkeys or another window.

Value callbacks must not clear or recreate parent containers. Toggle conditional controls with
visibility/enabled state. `RefreshToolkitLayerHierarchy` compares the visible tree's layer and
container references, order, and depth before rebuilding rows; selection only updates their
presentation. Layer-setting windows rebuild when their bound layer instance changes, including
managed-reference replacement during Undo. Modifier list refreshes compare the list and contents.
Keep external model notifications coalesced and pending until processed rather than dropping
notifications during input. None of these UI refresh paths should emit model-change events.

After changing these paths, check in Unity: type multi-digit W/H and fractional Step values;
scrub Step and both components of Center/Transform; edit an unselected layer's name/opacity;
switch Repeat/Gradient/Input modes; change a layer in another window while an input is focused;
then exercise Undo/Redo, group collapse/reordering, and cancelled drag-and-drop. Verify that
focus, caret, pointer capture, and scroll survive ordinary value updates.
