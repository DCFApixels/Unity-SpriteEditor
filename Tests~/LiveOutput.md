# Live output checks

Run `LiveOutputSmoke.cs` as an opt-in eval body only after the user has compiled the package.
It requires a graphics device and creates temporary textures only; no scenes, materials or assets
are modified. Run in both Gamma and Linear projects when available.

The smoke checks HDR and LDR output, transparency, different preview dimensions, mipmapped targets,
unchanged texture identity and CPU pixels, restoration, repeated disposal and a subsequent saved image.

GPU publishing must never call ReadPixels, SetPixels, Apply, SetDirty or AssetDatabase.SaveAssets.
Its source is a RenderTexture, so CopyTexture updates the destination GPU data only. The retained CPU
pixels remain the serialization source. Stop intentionally uploads that unchanged CPU image with Apply;
it must not generate mipmaps or discard CPU data. Save stops publishing before replacing serialized pixels.

## Editor integration

Use a disposable test document and material; do not modify a production prefab for these checks.

1. Save a document, assign its output texture to a material on a visible model and record its GUID/fileID.
2. Enable Live Update and paint, erase, Undo/Redo, change opacity, and change an effect. Scene View should
   follow edits and settle to full quality. EV, Debug, channel display and preview Post FX must not alter
   the material's sampled output. The channel buttons can still change new paint as documented.
3. Disable Live Update: the saved image returns and the material reference remains identical. Enable again:
   current edits return. No material or scene becomes dirty because of Live Update.
4. Save, paint more, then disable: the newly saved image returns, not the image from step 1.
5. Close with Discard, switch documents, reset window settings, or manually reload scripts. Verify restoration
   and unchanged references. Reopen/restart Unity and verify the saved texture remains assigned.
6. Resize the canvas without saving: the result fits the old output dimensions. Save: the existing texture
   gets the new dimensions without changing GUID/fileID. Check the Output Sprite too.
7. Save As creates a new asset; the old material reference and old saved image remain unchanged.
8. Reimport the compositor while Live Update is enabled. Its output should resume updating without
   replacing the material reference. Delete a disposable compositor while open: Live Update becomes unavailable.
9. Force a save failure or cancel Save As. No material reference may change, and disabling/closing must still
   restore the last successfully saved output.
