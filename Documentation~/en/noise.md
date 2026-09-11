---
title: "Procedural Noise"
parent: "Layers and groups"
grand_parent: "English"
nav_order: 1
lang: "en"
description: "Generate procedural noise textures inside Unity with WhimTex. Adjust Perlin, OpenSimplex2, Cellular, fractals and domain warp for clouds, VFX and masks."
permalink: "/en/noise/"
alternate: "ru/noise.md"
---

# Procedural Noise

Use Noise for clouds, grain, stone-like patterns or a starting point for a height map.
Add **Noise** from the layer menu and adjust the settings while watching the image.

## Start with the pattern

| Setting | What to try |
| :--- | :--- |
| Noise Type | OpenSimplex2 or Perlin for smooth variation; Cellular for cells; Value for a simpler random pattern. |
| Seed | Change the pattern without changing its character. |
| Scale | Increase for finer detail, decrease for larger shapes. |
| Offset | Move the pattern. |
| Fractal | FBm adds detail; Ridged emphasizes ridges; PingPong creates repeated bands. |
| Octaves | Add more levels of detail. |
| Domain Warp | Bend and distort the pattern; Strength controls how far. |

For finer control, **Lacunarity** changes the spacing between detail scales and **Gain**
changes how strongly the smaller details show.
With Cellular, try **Distance**, **Return** and **Jitter** to change the shape and regularity of the cells.

## Color texture or height map?

Choose **Encoding → Color Values** when using the noise as a visible grayscale image.
Choose **Linear Data** when using it as a height map or packing it into texture channels.

To create surface relief:

1. Add Noise and choose **Linear Data**.
2. Choose **Fractal → FBm**, start with three octaves and adjust Scale.
3. Add **Normal Map** above it and choose **Generation → Height Map**.
4. In Normal Map's Advanced settings, choose **Input Space → Linear**.
5. Adjust Strength and Smoothing. Change Noise Seed to try another surface.

**Tiled** preview helps you inspect seams, but does not make Noise itself seamless.
For the next step, see [Normal Map](normal-map.md).
