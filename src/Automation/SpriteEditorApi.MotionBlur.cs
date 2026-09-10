using Newtonsoft.Json.Linq;
using UnityEngine;
using static DCFApixels.SpriteEditor.AgentJson;

namespace DCFApixels.SpriteEditor
{
    public static partial class SpriteEditorApi
    {
        private static void SetMotionBlur(MotionBlurLayer layer, JObject value)
        {
            Keys(value, "mode", "strength", "distance", "angle", "arc", "center", "direction", "edges");
            layer.mode = Enum(value, "mode", layer.mode);
            layer.strength = Number(value, "strength", layer.strength, 0f, MotionBlurLayer.MaximumStrength);
            layer.distance = Number(value, "distance", layer.distance, 0f, MotionBlurLayer.MaximumDistance);
            layer.angle = Number(value, "angle", layer.angle, -180f, 180f);
            layer.arc = Number(value, "arc", layer.arc, 0f, 360f);
            if (value["center"] != null)
            {
                Require(value["center"] is JArray array && array.Count == 2, "center must be [x, y].");
                layer.center = new Vector2(Number(value["center"][0], "center.x", 0f, 1f),
                    Number(value["center"][1], "center.y", 0f, 1f));
            }
            layer.direction = Enum(value, "direction", layer.direction);
            layer.edges = Enum(value, "edges", layer.edges);
        }

        private static JObject MotionBlurSnapshot(MotionBlurLayer layer) => new JObject
        {
            ["mode"] = layer.mode.ToString(),
            ["strength"] = layer.strength,
            ["distance"] = layer.distance,
            ["angle"] = layer.angle,
            ["arc"] = layer.arc,
            ["center"] = Json(layer.center),
            ["direction"] = layer.direction.ToString(),
            ["edges"] = layer.edges.ToString()
        };
    }
}
