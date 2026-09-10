---
title: "Shader FX and Processor"
parent: "English"
nav_order: 9
lang: "en"
permalink: "/en/shader-fx/"
alternate: "ru/shader-fx.md"
previous_page: "en/blending.md"
next_page: "en/preview.md"
---

# Shader FX and Processor

Use a custom shader effect when you need a look that the built-in layers do not provide.
You can use an existing effect and adjust its parameters without writing code.

## Apply an existing effect

1. Select the layer you want to change.
2. Use **+ Reference** in its FX section and choose an effect asset.
3. Adjust the effect's exposed sliders, colors or textures.
4. If you want a separate copy stored with this document, choose **Embed**.

A referenced effect is shared with other places that use it.
An embedded copy can be edited independently.
When several effects are present, their order matters.

## Affect one layer or the image below?

**Shader FX** belongs to one layer and changes that layer's image.

A **Shader Processor** is a separate layer that changes the combined image below it.
Place it above the layers you want to process.
Use **Normal** blending and lower Opacity to mix the effect with the original;
hide the Processor to compare before and after.

In a Pass Through group, a Processor can also affect the background beneath the group.
Use an isolated group if the effect should stay within that group's contents.

Unlike [Post FX preview](post-fx.md), both are included in the saved image.

## Create your own effect

If you have shader code, use **+ Shader FX**, paste it into the editor and click **Apply**.
The code and settings stay with the document; no separate file is required.
If the code contains an error, the previous working version stays visible.

Writing an effect is optional. The [shader authoring reference](../ShaderFX.md)
is for creating code and reusable libraries.
