---
title: "Save and export"
parent: "English"
nav_order: 13
lang: "en"
permalink: "/en/saving/"
alternate: "ru/saving.md"
previous_page: "en/post-fx.md"
next_page: "en/shortcuts.md"
---

# Save and export

`Ctrl+S` saves the document, or opens Save As the first time.
**Save** updates it without a dialog and is disabled when there is nothing to save;
**Save As** makes an independent copy. Closing a changed document offers **Save / Discard / Cancel**.

The saved `.asset` already contains a full-resolution **Texture2D**, **Output Sprite**,
and the editable layer document. Assign the main asset to a texture field, or expand it to use the sprite.
A separate export is optional.

> **Important.**
> Unity uses the output from the **last explicit Sprite Editor save**.
> Save again after Undo/Redo, FX changes or edits to linked source textures.

## Export when you need a separate file

| Format | Best for |
| :--- | :--- |
| **PNG / TGA** | Color images with transparency. |
| **JPEG** | Opaque images; exports against white at quality 95. |
| **EXR** | Linear HDR RGB + alpha. |
| **PSD** | Layered interchange with nested groups and a merged image. |
| **Texture2D (.asset)** | A standalone Unity texture without the editable document. |

## Saved references, HDR export and PSD trade-offs

Saving preserves output references, including after canvas resize. Drawing pixels and embedded FX
are stored in the document; external textures and referenced FX stay linked. Project thumbnails
show saved pixels. Output Sprite has a centered pivot, full-rectangle mesh and 100 PPU.

EXR and Texture2D retain HDR; PNG/JPEG/TGA/PSD clamp an export copy.
Images exported into Assets are imported as single Sprites, except EXR, which is a linear texture.
Replacing a standalone Texture2D asks for confirmation and preserves references.

PSD preserves names, order, visibility, opacity and supported blends. Compatible fills, gradients
and Outline strokes stay editable; other effects are rasterized and unsupported blends approximated.
Export notes explain compromises per layer. Keep the native document: PSD does not retain live
effect targets or shader code.

[PSD export details](../PsdExport.md)
