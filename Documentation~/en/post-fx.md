---
title: "Game post-processing"
parent: "English"
nav_order: 12
lang: "en"
permalink: "/en/post-fx/"
alternate: "ru/post-fx.md"
previous_page: "en/color.md"
next_page: "en/saving.md"
---

# Game post-processing

Enable **Post FX** in the footer and open its arrow drawer to choose Scene View, Game Camera or
a Volume Profile. This optional feature currently supports **URP 17.x with Universal Renderer**.

Post FX affects presentation only: painting, sampling, saved textures and exports use the original
composition. The editor itself and Shader Processor do not require URP.

## Post FX: sources, background and alpha-based depth

- **Scene View** inherits the active view's post-processing switch and volumes.
- **Game Camera** inherits a chosen camera's settings and volumes; an empty field uses MainCamera.
- **Profile** uses a Volume Profile with manually controlled camera parameters.
- **Background:** Solid Color or Checkerboard, both opaque underlays before processing.
  The solid color and checker appearance are shared with User Settings.
- **Depth:** Solid is constant; Alpha Height maps alpha to a height surface; Alpha Mask puts
  pixels below Threshold at the far plane. Depth uses original alpha, not the background/channel display.
- **Link to Zoom** links simulated distance to zoom. **Animate** refreshes time-varying effects
  up to 8 fps. Closing the drawer keeps Post FX enabled.

## Post FX: Renderer Features and compatibility limits

The synthetic opaque surface runs through the selected Universal Renderer's active features,
including Full Screen Pass and SSAO. Depth/Depth Normals describe its alpha relief; extra color,
depth and normal passes are requested as needed. Disabling Post FX releases editor-owned resources.

This is not a copy of the scene: no scene geometry, lighting or camera stacks are rendered.
Features must declare their inputs and support offscreen cameras; scene-object filters may exclude
the surface. Motion-history and material-buffer features are not guaranteed.
TAA, the pipeline's Motion Blur and temporal AO are skipped. STP and unsupported pipelines/renderers
show the original preview with a notice. Cameras, profiles and project settings are never edited.
