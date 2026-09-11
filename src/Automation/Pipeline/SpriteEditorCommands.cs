#if SPRITE_EDITOR_PIPELINE
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;

namespace DCFApixels.SpriteEditor
{
    public static class SpriteEditorCommands
    {
        [CliCommand("sprite_editor_begin", "Create a named reservation and capture context in one call. Mutates the open document; use only for requested content creation or edits. No automatic save.", MainThreadRequired = true)]
        public static JObject Begin(
            [CliArg("requestId", "Caller-generated UUID; reuse identical arguments for retries", Required = true)] string requestId,
            [CliArg("name", "Reserved layer name")] string name = "Generating…",
            [CliArg("source", "none, merged, or layer (active layer unless sourceLayerId is supplied)")] string source = "none",
            [CliArg("area", "canvas or selection")] string area = "canvas",
            [CliArg("sessionId", "Optional explicit session; otherwise the only open, currently focused, or last-focused document")] string sessionId = null,
            [CliArg("sourceLayerId", "Optional stable source layer ID")] string sourceLayerId = null,
            [CliArg("selectionMode", "strict clips to selection; guide allows output outside it within the captured crop")] string selectionMode = "strict",
            [CliArg("padding", "Selection context pixels, 0..4096; -1 uses default (strict: 32, guide: 128)")] int padding = -1)
            => JObject.Parse(SpriteEditorApi.LiveBegin(requestId, name, source, area, sessionId, sourceLayerId, selectionMode, padding));

        [CliCommand("sprite_editor_lock", "Reserve an existing layer for inline FX/settings editing; keeps rendering unchanged. No automatic save.", MainThreadRequired = true)]
        public static JObject Lock(
            [CliArg("requestId", "Caller-generated unique ID; reuse identical arguments for retries", Required = true)] string requestId,
            [CliArg("layerId", "Existing layer GUID", Required = true)] string layerId,
            [CliArg("sessionId", "Optional explicit open document")] string sessionId = null,
            [CliArg("expectedRevision", "Optional layer contentRevision from inspection")] string expectedRevision = null)
            => JObject.Parse(SpriteEditorApi.LiveLock(requestId, layerId, sessionId, expectedRevision));

        [CliCommand("sprite_editor_sessions", "List open WhimTex documents, including unsaved documents, with live session IDs.", MainThreadRequired = true)]
        public static JObject Sessions() => JObject.Parse(SpriteEditorApi.LiveSessions());

        [CliCommand("sprite_editor_live", "Reserve, preview or complete an agent layer in an open document using a JSON request file.", MainThreadRequired = true)]
        public static JObject Live([CliArg("requestPath", "Absolute path to a live-edit JSON request file", Required = true)] string requestPath)
            => JObject.Parse(SpriteEditorApi.LiveFile(requestPath));

        [CliCommand("sprite_editor_describe", "Describe WhimTex's agent API, operations, enums and limits.", MainThreadRequired = true)]
        public static JObject Describe() => JObject.Parse(SpriteEditorApi.Describe());

        [CliCommand("sprite_editor_inspect", "Read a compositor's layer IDs, settings and revision before editing.", MainThreadRequired = true)]
        public static JObject Inspect([CliArg("assetPath", "Project-relative compositor .asset path", Required = true)] string assetPath)
            => JObject.Parse(SpriteEditorApi.Inspect(assetPath));

        [CliCommand("sprite_editor_execute", "Validate/apply a WhimTex JSON batch file. Check result.success as well as transport success.", MainThreadRequired = true)]
        public static JObject Execute([CliArg("requestPath", "Absolute path to a JSON request file", Required = true)] string requestPath)
            => JObject.Parse(SpriteEditorApi.ExecuteFile(requestPath));

        [CliCommand("sprite_editor_import_image", "Copy a generated PNG/JPEG into Assets and import it as a texture. Never overwrites.", MainThreadRequired = true)]
        public static JObject ImportImage(
            [CliArg("sourcePath", "Absolute local PNG/JPEG path", Required = true)] string sourcePath,
            [CliArg("assetPath", "New Assets/ texture path with the same extension", Required = true)] string assetPath)
            => JObject.Parse(SpriteEditorApi.ImportImage(sourcePath, assetPath));

        [CliCommand("sprite_editor_render", "Render an unfiltered compositor PNG to Temp/SpriteEditor for visual inspection.", MainThreadRequired = true)]
        public static JObject Render(
            [CliArg("assetPath", "Compositor .asset path", Required = true)] string assetPath,
            [CliArg("outputPath", "Project-relative Temp/SpriteEditor/*.png path", Required = true)] string outputPath,
            [CliArg("maxSize", "Longest output side, 1..4096")] int maxSize = 1024,
            [CliArg("overwrite", "Explicitly replace an existing preview PNG")] bool overwrite = false)
            => JObject.Parse(SpriteEditorApi.Render(assetPath, outputPath, maxSize, overwrite));
    }
}
#endif
