using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;

namespace DCFApixels.SpriteEditor
{
    // Reads Unity's missing-reference diagnostic payload, never rewrites asset YAML.
    internal static class MissingLayerData
    {
        internal sealed class ScalarText
        {
            internal readonly string Text;
            internal ScalarText(string text) { Text = text; }
        }

        private static JToken Scalar(JToken token, string text)
        {
            token.AddAnnotation(new ScalarText(text));
            return token;
        }
        internal static JObject Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new JObject();
            if (text.Length > 4 * 1024 * 1024) throw new FormatException("Saved layer data exceeds the recovery limit.");
            if (text.TrimStart().StartsWith("{")) return JObject.Parse(text);
            var lines = new List<string>();
            using (var reader = new StringReader(text))
                for (string line; (line = reader.ReadLine()) != null;)
                    if (!string.IsNullOrWhiteSpace(line)) lines.Add(line.TrimEnd());
            int index = 0;
            JToken result = Block(lines, ref index, 0);
            if (index != lines.Count || !(result is JObject obj)) throw new FormatException("Unsupported saved layer data.");
            return obj;
        }

        private static int Indent(string line) => line.Length - line.TrimStart(' ').Length;
        private static JToken Block(List<string> lines, ref int index, int depth)
        {
            if (index >= lines.Count) throw new FormatException("Incomplete saved layer data.");
            if (depth > 64) throw new FormatException("Saved layer data is nested too deeply.");
            int indent = Indent(lines[index]);
            bool array = lines[index].Substring(indent).StartsWith("- ");
            if (array)
            {
                var result = new JArray();
                while (index < lines.Count && Indent(lines[index]) == indent && lines[index].Substring(indent).StartsWith("- "))
                {
                    string value = lines[index++].Substring(indent + 2).Trim();
                    if (value.Length == 0) result.Add(Block(lines, ref index, depth + 1));
                    else if (value[0] != '{' && value[0] != '[' && value[0] != '"' && value[0] != '\'' &&
                        (value.Contains(": ") || value.EndsWith(":")))
                    {
                        var item = new JObject();
                        Pair(item, value, lines, ref index, indent + 2, depth + 1);
                        if (index < lines.Count && Indent(lines[index]) > indent)
                        {
                            var rest = Block(lines, ref index, depth + 1) as JObject;
                            if (rest == null) throw new FormatException("Unsupported sequence data.");
                            foreach (var property in rest.Properties()) item.Add(property.Name, property.Value);
                        }
                        result.Add(item);
                    }
                    else result.Add(Inline(value, depth + 1));
                }
                return result;
            }
            var map = new JObject();
            while (index < lines.Count && Indent(lines[index]) == indent && !lines[index].Substring(indent).StartsWith("- "))
            {
                string line = lines[index++].Substring(indent);
                Pair(map, line, lines, ref index, indent, depth);
            }
            return map;
        }

        private static void Pair(JObject map, string line, List<string> lines, ref int index, int indent, int depth)
        {
            int colon = line.IndexOf(':');
            if (colon < 1) throw new FormatException("Unsupported saved field: " + line);
            string key = line.Substring(0, colon).Trim(), value = line.Substring(colon + 1).Trim();
            if (map.ContainsKey(key)) throw new FormatException("Duplicate saved field: " + key);
            if (value.Length == 0 && index < lines.Count && (Indent(lines[index]) > indent ||
                Indent(lines[index]) == indent && lines[index].TrimStart().StartsWith("- ")))
                map.Add(key, Block(lines, ref index, depth + 1));
            else map.Add(key, Inline(value, depth + 1));
        }

        private static JToken Inline(string value, int depth)
        {
            if (depth > 64) throw new FormatException("Saved layer data is nested too deeply.");
            if (value.StartsWith("{") && !value.EndsWith("}") || value.StartsWith("[") && !value.EndsWith("]"))
                throw new FormatException("Unbalanced saved data.");
            if (value.StartsWith("{") && value.EndsWith("}"))
            {
                var map = new JObject();
                foreach (string part in Split(value.Substring(1, value.Length - 2)))
                {
                    int colon = part.IndexOf(':');
                    if (colon < 1) throw new FormatException("Unsupported inline field.");
                    map.Add(part.Substring(0, colon).Trim(), Inline(part.Substring(colon + 1).Trim(), depth + 1));
                }
                return map;
            }
            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                var array = new JArray();
                foreach (string part in Split(value.Substring(1, value.Length - 2))) array.Add(Inline(part, depth + 1));
                return array;
            }
            if (value.StartsWith("\"")) return JToken.Parse(value);
            if (value.StartsWith("'") && value.EndsWith("'")) return new JValue(value.Substring(1, value.Length - 2).Replace("''", "'"));
            if (value == "true" || value == "false") return Scalar(new JValue(value == "true"), value);
            if (value == "null" || value == "~") return Scalar(JValue.CreateNull(), value);
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer)) return Scalar(new JValue(integer), value);
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && !double.IsInfinity(number) && !double.IsNaN(number)) return Scalar(new JValue(number), value);
            if (value.StartsWith("|") || value.StartsWith(">") || value.StartsWith("&") || value.StartsWith("*"))
                throw new FormatException("This saved data uses an unsupported YAML construct.");
            return new JValue(value);
        }

        private static IEnumerable<string> Split(string value)
        {
            int start = 0, nesting = 0; char quote = '\0'; bool escape = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (quote != '\0')
                {
                    if (escape) escape = false;
                    else if (c == '\\' && quote == '"') escape = true;
                    else if (c == quote) quote = '\0';
                }
                else if (c == '"' || c == '\'') quote = c;
                else if (c == '{' || c == '[') nesting++;
                else if (c == '}' || c == ']') nesting--;
                else if (c == ',' && nesting == 0) { yield return value.Substring(start, i - start).Trim(); start = i + 1; }
            }
            if (quote != '\0' || nesting != 0) throw new FormatException("Unbalanced saved data.");
            if (start < value.Length) yield return value.Substring(start).Trim();
        }
    }
}
