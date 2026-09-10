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

Use **+ Shader FX** in a layer's settings, write `ApplyFX`, and click **Apply**.
Code and parameters can live inside the document. For an effect on the already-composited
stack below a position, add a **Shader Processor** layer instead.

## Shader FX: a first snippet, parameters and reusable code

Add a **Float** parameter named `_Amount`, then apply this example:

```hlsl
float4 ApplyFX(float2 uv, float4 color)
{
    return float4(lerp(color.rgb, 1.0 - color.rgb, saturate(_Amount)), color.a);
}
```

Parameters support Float, Color, Vector and Texture2D. Their uniforms are generated automatically.
Code and declarations stay drafts until Apply; a compile error keeps the last working effect.
Undo/Redo restores code, caret and selection, but restored code still needs Apply.

`SampleInput(uv)` reads the layer after earlier modifiers. Return straight RGBA; opacity/blending
come later. Built-in inputs include `_MainTex`, `_MainTex_TexelSize`, `_InputSize`,
`_CanvasSize` (width, height, 1/width, 1/height) and `_PreviewScale`. Do not redeclare generated uniforms.

Standard `#include` supports project/package paths and relative paths. Relative paths start in
the document/FX asset folder, or Assets before the first save. After library edits, click Apply again;
after Save As to another folder, check relative paths. Libraries must suit the fragment-shader environment.

**+ Reference** links an external FX shared by its users; **Embed** makes an independent document-owned
copy. Save As and layer duplication copy embedded FX independently. FX run in order after Transform;
changing parameter values does not regenerate shaders.

## Shader Processor: process the lower stack instead of one layer

The Processor uses the same ApplyFX/SampleInput interface, with the lower composite as input.
Normal blending uses Opacity to mix original and processed RGBA; 100% replaces the input.
Other blends combine it with the result. Both ranges default to HDR; hiding the Processor bypasses it.

In Pass Through groups it also sees the external backdrop; isolated groups restrict it to their
children. Standalone previews and rasterization evaluate lower siblings against transparency.
Processors are clipping-chain boundaries, not clipping layers or bases. PSD bakes the composite
and retains the original layers in a hidden Source Layers folder.
