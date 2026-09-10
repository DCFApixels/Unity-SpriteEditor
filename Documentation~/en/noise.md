---
title: "Procedural Noise"
parent: "Layers and groups"
grand_parent: "English"
nav_order: 1
lang: "en"
permalink: "/en/noise/"
alternate: "ru/noise.md"
---

# Procedural Noise

Noise generates OpenSimplex2, OpenSimplex2S, Perlin, Value, Value Cubic or Cellular on the GPU.
Choose Seed, adjust Scale/Offset, and add fractals or Domain Warp. Larger Scale means finer detail;
it is measured across the shorter canvas side. Drag numeric labels or sliders to update the preview.

**Color Values** outputs display-oriented grayscale; **Linear Data** keeps raw 0–1 values for masks,
height maps and channel packing. RGB contains the same scalar and alpha is 1. Only parameters are
stored, not pixel snapshots. Export uses the same coordinates/seed at full resolution.
Tiled preview repeats the result; Noise itself is not inherently seamless.

## Choose the noise character

| Setting | When to change it |
| :--- | :--- |
| Noise Type | OpenSimplex2/Perlin for smooth variation; Cellular for cell structure; Value for interpolated random values. |
| Seed | Get another pattern with the same settings. |
| Scale / Offset | Change detail size and position in noise space, independently of the layer Transform. |
| Fractal | FBm combines octaves; Ridged emphasizes ridges; PingPong folds repeating transitions. |
| Octaves | Number of detail levels, 1–8. More levels cost more GPU work. |
| Lacunarity / Gain | Frequency growth and amplitude decay for successive octaves. |
| Domain Warp | Distort coordinates before evaluating noise. Strength controls displacement. |
| Encoding | Color Values for visible gray; Linear Data for numeric masks and height. |

Weighted Strength changes octave contribution based on the noise result.
Cellular Distance, Return and Jitter control cells; Ping Pong Strength belongs to Fractal → PingPong.
Changing modes does not turn their retained settings into shared tool preferences.

## Example: a procedural height map

1. Add Noise and choose Encoding → Linear Data.
2. Choose Fractal → FBm, start with three octaves and adjust Scale.
3. Add Normal Map above it: Generation → Height Map, Input Space → Linear in Advanced.
4. Tune Normal Map Strength and Smoothing. Changing Noise Seed produces another surface.

Generation runs directly on the GPU at the requested resolution, without an additional two-stage
Noise refinement algorithm. Cost grows with pixel count, octave count and Warp.
Saving uses full resolution.

[All parameters and limits](../AgentAPI.md#noise-settings) · [Normal Map](normal-map.md)
