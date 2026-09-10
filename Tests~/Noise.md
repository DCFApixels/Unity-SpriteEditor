# Noise validation

Offline checks, without compiling or opening Unity:

```text
node Tests~/NoiseContract.test.mjs
```

`NoiseApiSmoke.cs` and `NoiseSmoke.cs` are opt-in C# snippets for the connected Editor,
only after the user has manually compiled the package. Do not trigger compilation or imports to run them.
The API snippet checks partial settings, full signed seed range, validation, discovery and round trips.
The GPU snippet creates only a small transient document and textures; it checks all six algorithms and
four noise fractals, seed determinism, high-bit seed precision, domain warp, inversion, color encoding,
opaque grayscale and matching sample coordinates at different resolutions. It does not save assets.

Manual interaction checks:

1. Add Noise Layer. Drag Scale, Seed and Offset labels continuously; the pattern must update before release.
2. Change fractal/algorithm; conditional controls should show/hide without replacing existing input elements.
3. Edit through Properties as well as Layer Settings. Undo/Redo should restore generator parameters and output.
4. Duplicate, nest in a group, transform, Swizzle and rasterize using the normal layer commands.
5. Save/reopen a document and verify that the seed/pattern is retained.

For a full-size performance measurement, explicitly render/export a 5000 x 5000 canvas with both simple
noise and Cellular + 8 octaves + Domain Warp. Record algorithm, device, dimensions and warm-up separately.
Do not treat the normal 512 px preview as a full-resolution benchmark. This implementation has no
automatic low/full-resolution refinement stage and makes no fixed frame-time guarantee.
