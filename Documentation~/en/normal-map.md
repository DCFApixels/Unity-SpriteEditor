---
title: "Normal Map"
parent: "Effect layers"
nav_order: 1
lang: "en"
permalink: "/en/normal-map/"
grand_parent: "English"
alternate: "ru/normal-map.md"
---

# Normal Map

Start in **Simple**, which shows the common controls without changing the calculation.
**Advanced** exposes everything. Switching back does not reset advanced values;
the editor shows a notice when those settings have been modified.

## From an existing height map

1. Add Normal Map above File, Drawing or Noise.
2. Leave Input at Previous, or assign Target explicitly.
3. Choose Generation → Height Map and the intended Source Channel.
4. Start with Strength 4 and Smoothing 1 px, then adjust surface relief.
5. Hide the source itself if the output should contain only the normal map.

Brighter means higher. Strength 0 produces a flat normal; Invert Height reverses the height.
For a numeric linear source, open Advanced and choose Input Space → Linear.
Color Values reads displayed RGB; alpha is unaffected by color space.

## From a color texture

Choose Generation → Texture. It estimates height from contrast at multiple scales;
it does not recover actual geometry or reliably separate lighting from material color.

In Advanced, choose Output → Height to inspect the reconstruction first. Adjust
Fine / Medium / Large Detail and the medium/large radii.
Light Removal attenuates broad brightness changes, but can also remove real large-scale relief.
Return Output to Normal before saving the normal map.

## Controls by purpose

| Section | Controls |
| :--- | :--- |
| Source | Height channel; Input Space; Ignore Transparent for alpha-normalized smoothing. |
| Surface | Strength, Smoothing, Invert Height, Edges; Derivative in Advanced. |
| Height Levels | Black/White Level and Gamma for the reconstructed height range. |
| Texture Detail | Detail scales and weights, used only in Texture mode. |
| Output | Flip X/Y, Alpha, Normal/Height and Encoding. |

Edges controls boundary sampling independently of Transform: Clamp, Repeat or Mirror.
Use Repeat for a tileable source. Alpha → Opaque creates a solid map;
Source retains source coverage. When Alpha supplies height, transparency participates as height.

## Export without changing the data

Keep Normal blend, opacity 1, identity Swizzle and no color FX.
Blending does not renormalize vectors. Transform moves the image but does not rotate its normals.

- **Packed Color:** PNG, TGA, PSD and display; import PNG/TGA as Normal Map without grayscale conversion.
- **Linear Data:** raw 0–1 values for linear EXR or Texture2D; these look different in the color preview.

Use Flip Y if the consumer expects the opposite green-channel direction.
[Complete parameter ranges](../AgentAPI.md#normal-map-settings).
