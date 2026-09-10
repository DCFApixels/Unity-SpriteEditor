---
title: "Color, HDR and channels"
parent: "English"
nav_order: 11
lang: "en"
permalink: "/en/color/"
alternate: "ru/color.md"
previous_page: "en/preview.md"
next_page: "en/post-fx.md"
---

# Color, HDR and channels

Picker colors, stored pixels, layer ranges and preview display are separate controls.
Change the one that belongs to the stage you want to affect.

## Channel display, HDR colors and layer ranges

One RGB channel displays grayscale, with transparency when A is enabled. A alone displays opaque
grayscale alpha. Two or three RGB channels retain their colors; A off ignores transparency.

The footer **HDR** button changes color/gradient pickers, not stored colors or layer ranges.
With HDR off, colors display and paint without their stored HDR intensity; switching back restores it.
Editing a color replaces the stored value.

In **Layer Settings → Color & Blending**, Color Range and Blend Range independently control bounded
or extended behavior. The header dropdown sets both; a blank value means they differ.
The compositor works in linear HDR. Drawing starts in 8-bit storage and promotes to half-float;
returning to Standard does not discard stored HDR. Use **Convert to 8-bit** explicitly when needed.

[HDR, storage and groups](../HDR.md)

## Pack texture data with Swizzle and Assign Channels

**Swizzle** remaps any layer/group's RGBA after FX, before range handling and blending:
`R`, `G`, `B`, `A`, `1-R`, `1-G`, `1-B`, `1-A`, `0`, `1`, `R * A`, `G * A`, `B * A`.
Products use the original input alpha. A changed group Swizzle isolates its content.

**Row menu → Assign Channels** configures selected layers in top-to-bottom tree order:

- **1–3 layers:** each layer's `R * A` goes to R, G or B; other RGB channels are zero and A is 1.
  Upper selected layers use Add; the bottom layer's blend and all opacities stay unchanged.
- **4 layers:** each layer's `R * A` goes to R, G, B or A; other channels are zero. Only Swizzle changes.
  RGB layers then have zero alpha and are skipped by ordinary blending: this preset alone
  does **not** assemble a finished four-channel texture.

Disabled above four layers. The command does not merge, reorder or move layers; existing groups,
surrounding content and clipping still affect the composition. It is one Undo step.
