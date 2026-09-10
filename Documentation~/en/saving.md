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

Save your document to keep its layers editable and use the result directly in Unity.
Export only when you need a separate image file.

## Save a document

Press `Ctrl+S`. The first save asks for a location; later saves update the same file.
**Save As** makes a separate copy. If you close a document with changes,
you can save, discard them or cancel closing.

The saved asset is ready to use as a **texture**.
Expand it in Project to use **Output Sprite**. Double-click it to continue editing.

Unity shows the **last saved image**. Save again after editing the document or changing a linked texture.
File layers keep their links to source textures; keep those sources in the project.

## Choose an export format

Use **Export** in the window header:

| Format | Best for |
| :--- | :--- |
| **PNG / TGA** | Color images with transparency. |
| **JPEG** | Images that do not need transparency; transparent areas become white. |
| **EXR** | HDR images. |
| **PSD** | Exchanging a layered image. |
| **Texture2D (.asset)** | A standalone Unity texture without the editable layers. |

EXR and Texture2D keep HDR brightness. PNG, JPEG, TGA and PSD use the ordinary color range.
Exporting does not reduce the colors stored in your original document.

Preview settings such as zoom, EV and Post FX are not included in the saved or exported image.

## What remains editable in PSD?

PSD keeps the layer names, order, groups, visibility, opacity and supported blends.
Compatible color fills, gradients and outlines remain editable.
Other effects become pixels, and some blend modes can look different.

Read the export notes if the result differs. Keep the original Sprite Editor document
so you can still change all effects and their sources later.
