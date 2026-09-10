---
title: "Troubleshooting"
parent: "English"
nav_order: 16
lang: "en"
permalink: "/en/troubleshooting/"
alternate: "ru/troubleshooting.md"
previous_page: "en/automation.md"
---

# Troubleshooting

## The brush leaves no mark

1. Check that Drawing is active; another layer type needs conversion.
2. Enable A in the Preview footer and check the selected color's alpha.
3. Clear the area selection with `Ctrl+D`: an active empty selection blocks strokes.
4. Check visibility, opacity, Clipping Mask and the clipping base.
5. On transformed Drawing layers, consider the source frame, not only canvas boundaries.

## Painted color does not match the swatch

Reset EV to 0 and enable RGBA first. Disable Post FX when comparing with the source composition.
Then check Blend, opacity, Color/Blend Range and the HDR picker.
For numeric maps, check Encoding and the source texture's imported sRGB flag.
[Color controls explained](color.md).

## An effect is empty or processes the wrong source

Previous uses the next sibling below, not an arbitrary earlier layer in the tree.
After reordering, check Input/Target. Specific is useful when the stack changes frequently.
Hidden sources are valid; hidden children of a target group remain excluded.
Self-references and cyclic targets are invalid.

## Repeated strokes or seams look unexpected

Symmetry & Repeat copies strokes; Transform Tiling repeats a source;
Tiled preview repeats the whole document. These are three different mechanisms.
See [symmetry and seamless painting](symmetry.md). Effects have their own Edges setting;
Tiled alone does not make procedural Noise seamless.

## Unity shows an older texture

Click Save in Sprite Editor: consumers use the last saved output, not the unsaved preview.
This also applies after changes to external textures, FX or Undo.
Post FX is never included in that saved texture.

## A large canvas renders slowly

Lower Live Quality while painting. Pencil intentionally keeps full-resolution preview.
Large blurs show a cheaper interactive result before refining it.
The effect cache is capped at 256 MiB, but that is not a total GPU-memory limit:
full-resolution temporary buffers and Undo storage are separate.
See [Gaussian Blur and caching](../GaussianBlur.md), [Motion Blur memory](../MotionBlur.md#cache-and-memory).

## Post-processing differs from the game

Check camera source, profile, URP support and the post-processing switch.
The preview uses a synthetic surface, not the scene; temporal effects and some Renderer Features
cannot be reproduced. See [Post FX compatibility](post-fx.md).

If the issue remains, include the package and Unity versions, reproduction steps and a minimal
shareable document in a [bug report](https://github.com/DCFApixels/Unity-SpriteEditor/issues).
