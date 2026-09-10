---
title: "Blending and clipping"
parent: "English"
nav_order: 8
lang: "en"
permalink: "/en/blending/"
alternate: "ru/blending.md"
previous_page: "en/effects.md"
next_page: "en/shader-fx.md"
---

# Blending and clipping

Enable **Clipping Mask** in the row menu, or `Alt`-click the boundary above the base layer.
Consecutive clipped layers use the first non-clipped sibling below, within the same group.
They keep their colors/effects but cannot expand the base's alpha.

## Clipping, group blending and blend modes

A hidden, transparent or missing base hides the chain. Base opacity applies once to the whole result.
Groups participating in clipping are isolated while in that chain. Clipping supports Undo/Redo,
duplication, the agent API and PSD clipping flags; merging bakes it.

Ordinary blend modes combine RGB in overlaps with source-over alpha.
**Overwrite** replaces complete RGBA; layer opacity interpolates old/new pixels.
**None** leaves the composition unchanged.

Normal · Add · Subtract · Multiply · Divide · Screen · Overlay · Darken · Lighten ·
Color Dodge · Color Burn · Linear Dodge (Add) · Linear Burn · Linear Light ·
Linear Light Add/Sub · Vivid Light · Pin Light · Hard Mix · Hard Light · Soft Light ·
Difference · Exclusion · Negation · None · Overwrite.
