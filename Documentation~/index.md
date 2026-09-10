---
title: "Documentation"
nav_order: 0
permalink: "/"
has_toc: false
---

# Unity Sprite Editor

A layered sprite and texture editor, directly inside Unity.
Create, paint, combine and save an editable document with a ready-to-use texture.

[English user guide](en/index.md){: .btn .btn-primary }
[Руководство на русском](ru/index.md){: .btn }

<img class="hero-image" src="{{ '/Images/sprite-editor-heart.jpg' | relative_url }}" alt="Sprite Editor showing a layered heart in Tiled preview" width="720">

## Choose your starting point

| I want to… | English | Русский |
| :--- | :--- | :--- |
| Install and create an image | [Start here](en/getting-started.md) | [Начало работы](ru/getting-started.md) |
| Paint or edit pixels | [Brush, Pencil and Fill](en/painting.md) | [Кисть, карандаш и заливка](ru/painting.md) |
| Build textures from sources | [Layers](en/layers.md) · [Effects](en/effects.md) | [Слои](ru/layers.md) · [Эффекты](ru/effects.md) |
| Use the result in Unity | [Save and export](en/saving.md) | [Сохранение и экспорт](ru/saving.md) |
| Automate authoring | [Automation](en/automation.md) | [Автоматизация](ru/automation.md) |

## Requirements and scope

Unity 6 (`6000.0`) or newer. Sprite Editor runs only in the Editor;
saved textures and sprites work at runtime without rendering the layer stack.
Game Post FX preview is optional and currently needs URP 17.x with Universal Renderer.
Other editing tools do not require a render pipeline package.

The [technical reference](reference.md) covers rendering, storage and the agent API.
See the [changelog](https://github.com/DCFApixels/Unity-SpriteEditor/blob/main/CHANGELOG.md)
for changes and [acknowledgements](credits.md) for third-party sources and licenses.
