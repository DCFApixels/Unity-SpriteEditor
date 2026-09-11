# Sprite Editor asset authoring

When adding or updating third-party code or libraries, acknowledge the project and link its
upstream source in both READMEs. Preserve original copyright headers, include the applicable
license text and notices, and record the source version in `ThirdPartyNotices.md`.

For requests to create or edit **Sprite Editor compositor assets**, use the editor-side
`DCFApixels.SpriteEditor.SpriteEditorApi`, not generated Unity YAML or simulated mouse clicks.
This guidance is for authoring images; ordinary plugin source-code tasks do not require running it.

Read [Documentation~/AgentAPI.md](Documentation~/AgentAPI.md) for command syntax, JSON operations,
coordinates, safety/Undo semantics and complete workflows. Examples live in
[Documentation~/Examples](Documentation~/Examples).

- Discover `sprite_editor_*` commands on the intended running Editor. Always pass the explicit
  Unity project path. The Pipeline adapter is optional; direct C# API calls work without it.
- Respect the project's compilation and asset-editing rules. Missing commands are not permission
  to install packages, start another Editor, recompile, or modify an unrelated project.
- For a generated image, first use an available image-generation tool, then import the resulting
  local PNG/JPEG with `sprite_editor_import_image`. Reuse the returned asset path in a File layer.
  The API does not generate images or download URLs.
- Prefer File layers and nondestructive transforms. Drawing strokes are useful for touch-ups,
  masks and simple procedural marks, not a substitute for an image-generation tool.
- Inspect before editing an existing document. Use stable layer IDs and the returned revision;
  use `@aliases` for newly added layers within a batch. Do not identify layers by display names.
- Validate unfamiliar batches with `dryRun:true`. Check the API's `success`, not only the CLI
  process/transport result. Unknown fields and unsupported settings are errors.
- After a timeout or save failure, inspect before retrying: additions and strokes are not
  idempotent. Never replay a whole batch merely because its response was lost.
- Render a preview and inspect it visually before declaring an image task complete. Rendering
  uses the actual compositor and requires graphics; do not launch it with `-nographics`.
- Scope new assets to the user's requested output location. Do not overwrite unrelated files,
  alter existing source import settings, or delete working documents to retry a failed command.

## Documentation maintenance

- Brush terminology: **procedural brush** has no tip texture; **textured brush** uses a tip texture.
  In Russian use **процедурная кисть** and **текстурная кисть**. Use these names consistently in UI and documentation.

- Keep README files concise: introduction, installation, quick start and links to the guide.
- Write the EN/RU user guides for artists: lead with the desired visual result, where to click,
  and how controls change the image. Omit rendering/storage/cache internals, Undo implementation
  details, regression history and descriptions of incidental UI layout behavior. Keep warnings only
  when they affect the result, compatibility or loss of editable work. Ordinary Undo shortcuts belong
  in the shortcuts page. Keep programming contracts in the separate technical reference, not the guides.
- Update the matching `Documentation~/en/` and `Documentation~/ru/` user-guide pages for feature changes.
  Keep the shared English API contract in `Documentation~/AgentAPI.md` accurate; preserve existing URLs.
- Follow `Documentation~/building.md` for website validation. Jekyll builds are documentation-only;
  they do not authorize a Unity build, compilation or asset reimport.
