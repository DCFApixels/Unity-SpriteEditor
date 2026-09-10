---
title: "Effect layers"
parent: "English"
has_children: true
nav_order: 7
lang: "en"
permalink: "/en/effects/"
alternate: "ru/effects.md"
previous_page: "en/symmetry.md"
next_page: "en/blending.md"
---

# Effect layers

Use **+** to add an effect layer above its source:

| Goal | Layer | Start with |
| :--- | :--- | :--- |
| Outline a shape | Outline | Width, softness and position. |
| Build a distance field | SDF | Source channel, threshold and distance. |
| Generate surface normals | Normal Map | Height Map or Texture; Simple controls first. |
| Soften an image | Gaussian Blur | Radius. |
| Create a motion trail | Motion Blur | Linear distance/angle or Circular arc/center. |

**Previous** means the next sibling below the effect; **Specific** selects a layer or group.
Drag a layer onto Target to assign it. With a dragged selection, the active layer is used.
A hidden target still works; inside a target group, hidden children remain excluded.
Effects see the group's own isolated content, while the group's main composition may stay Pass Through.

## Blur controls and interactive quality

- **Gaussian Blur:** Radius is 0–256 original canvas pixels, default 8; zero bypasses it.
- **Motion Blur:** Linear uses Distance (0–512 px) and Angle. Circular uses Arc (0–360°)
  and normalized Center, with `(0.5, 0.5)` at the middle. Circular is rotation, not zoom.
  Direction is Centered/Forward/Backward; Forward follows Angle or turns counterclockwise.
- **Strength** on Motion Blur is 0–400%, default 100%. Below 100% it mixes with the original.
  Above 100% it makes translucent trails denser without changing length or color brightness;
  fully opaque pixels are unchanged.
- **Edges:** Transparent, Clamp, Repeat or Mirror, independent of Tiled preview.
  Both effects filter color with alpha correctly; use HDR Color Range to retain intensities above 1.

Large blurs use cheaper interactive previews and refine after editing settles. Saving and export
use full quality. Cache storage is bounded and separate from Undo; large cold renders can still be
expensive, and long circular arcs can show discrete traces on fine details.

[Gaussian rendering & cache](../GaussianBlur.md) · [Motion Blur details](../MotionBlur.md)

## Outline/SDF sources and Normal Map output

Outline/SDF use grayscale coverage for groups; Normal Map and blurs use RGBA.
Available distance algorithms include exact Euclidean EDT, approximate Euclidean, Manhattan and Chebyshev.
Self-referencing and cyclic effect targets are rejected.

Normal Map **Height Map** uses a chosen height channel; **Texture** estimates relief from image detail.
Use **Output → Height** to tune the reconstruction, then return to Normal.
Advanced exposes filtering, detail bands, lighting removal, levels, orientation and encoding;
switching back to Simple does not reset them.

Keep Normal blending, opacity 1 and identity Swizzle for a usable normal texture. Mixing does not
renormalize vectors; Transform moves the image without reorienting normals.
Packed Color is suited to PNG/TGA/PSD; Linear Data is for linear EXR/Texture2D.
Import an exported PNG/TGA as **Normal Map**, without grayscale conversion.

[Normal Map parameters](../AgentAPI.md#normal-map-settings)



## Example: blur a group while keeping its sources

1. Put the source layers in a group.
2. Add Gaussian Blur above it and leave Input at Previous.
3. Hide the group itself to avoid drawing a second copy; leave its children enabled.
4. Adjust Radius. For a tileable source, choose Edges → Repeat.
5. Save: the editable group remains in the document and the output texture contains the blur.

For surface relief, follow the [Normal Map walkthrough](normal-map.md).
