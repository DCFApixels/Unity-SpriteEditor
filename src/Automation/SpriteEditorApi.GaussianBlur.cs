using Newtonsoft.Json.Linq;
using static DCFApixels.SpriteEditor.AgentJson;

namespace DCFApixels.SpriteEditor
{
    public static partial class SpriteEditorApi
    {
        private static void SetGaussianBlur(GaussianBlurLayer layer, JObject value)
        {
            Keys(value, "strength", "radius", "edges");
            layer.strength = Number(value, "strength", layer.strength, 0f, GaussianBlurLayer.MaximumStrength);
            layer.radius = Number(value, "radius", layer.radius, 0f, GaussianBlurLayer.MaximumRadius);
            layer.edges = Enum(value, "edges", layer.edges);
        }

        private static JObject GaussianBlurSnapshot(GaussianBlurLayer layer) => new JObject
        {
            ["strength"] = layer.strength,
            ["radius"] = layer.radius,
            ["edges"] = layer.edges.ToString()
        };
    }
}
