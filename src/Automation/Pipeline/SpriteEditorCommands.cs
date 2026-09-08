#if SPRITE_EDITOR_PIPELINE
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;

namespace DCFApixels.SpriteEditor
{
    public static class SpriteEditorCommands
    {
        [CliCommand("sprite_editor_describe", "Describe Sprite Editor's agent API, operations, enums and limits.", MainThreadRequired = true)]
        public static JObject Describe() => JObject.Parse(SpriteEditorApi.Describe());

        [CliCommand("sprite_editor_inspect", "Read a compositor's layer IDs, settings and revision before editing.", MainThreadRequired = true)]
        public static JObject Inspect([CliArg("assetPath", "Project-relative compositor .asset path", Required = true)] string assetPath)
            => JObject.Parse(SpriteEditorApi.Inspect(assetPath));

        [CliCommand("sprite_editor_execute", "Validate/apply a Sprite Editor JSON batch file. Check result.success as well as transport success.", MainThreadRequired = true)]
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
