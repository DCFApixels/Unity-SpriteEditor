using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace DCFApixels.SpriteEditor
{
    internal sealed class ShaderFXSourceBuilder
    {
        private const int MaximumCharacters = 2 * 1024 * 1024;
        private static readonly Regex Include = new Regex("^\\s*#\\s*(include|include_with_pragmas)\\s+\"([^\"]+)\"\\s*$");
        private static readonly Regex Identifier = new Regex("^[A-Za-z_][A-Za-z0-9_]*$");
        private readonly string projectRoot = Path.GetDirectoryName(Application.dataPath);

        internal static string Build(ShaderFX effect, string assetPath)
        {
            ShaderFXSourceBuilder builder = new ShaderFXSourceBuilder();
            StringBuilder properties = new StringBuilder();
            StringBuilder uniforms = new StringBuilder();
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal)
            {
                "_MainTex", "_MainTex_TexelSize", "_InputSize", "_CanvasSize", "_PreviewScale",
                "ApplyFX", "SampleInput", "SpriteFXFragment", "vert_img", "v2f_img"
            };
            foreach (ShaderFXParameter parameter in effect.Parameters)
            {
                if (parameter == null || string.IsNullOrEmpty(parameter.name) || !Identifier.IsMatch(parameter.name) ||
                    !names.Add(parameter.name))
                    throw new InvalidOperationException($"Invalid, duplicate or reserved parameter name: '{parameter?.name}'. Use an HLSL identifier such as _Amount.");
                string name = parameter.name;
                switch (parameter.type)
                {
                    case ShaderFXParameterType.Float:
                        properties.AppendLine($"{name} (\"{name}\", Float) = 0");
                        uniforms.AppendLine($"float {name};");
                        break;
                    case ShaderFXParameterType.Color:
                        properties.AppendLine($"{name} (\"{name}\", Vector) = (1,1,1,1)");
                        uniforms.AppendLine($"float4 {name};");
                        break;
                    case ShaderFXParameterType.Vector:
                        properties.AppendLine($"{name} (\"{name}\", Vector) = (0,0,0,0)");
                        uniforms.AppendLine($"float4 {name};");
                        break;
                    case ShaderFXParameterType.Texture2D:
                        if (!names.Add(name + "_TexelSize"))
                            throw new InvalidOperationException($"Reserved texture parameter: {name}_TexelSize.");
                        properties.AppendLine($"{name} (\"{name}\", 2D) = \"white\" {{}}");
                        uniforms.AppendLine($"sampler2D {name};\nfloat4 {name}_TexelSize;");
                        break;
                    default: throw new InvalidOperationException($"Unsupported parameter type: {parameter.type}.");
                }
            }
            string expanded = builder.ResolveIncludes(effect.Code ?? string.Empty, assetPath);
            return "Shader \"Hidden/TextureCompositor/ShaderFX/" + effect.ShaderKey + "\"\n{\n" +
                "Properties {\n_MainTex (\"Input\", 2D) = \"white\" {}\n" + properties + "}\n" +
                "SubShader { Cull Off ZWrite Off ZTest Always Blend Off\nPass {\nCGPROGRAM\n" +
                "#pragma vertex vert_img\n#pragma fragment SpriteFXFragment\n#pragma target 3.5\n" +
                "#include \"UnityCG.cginc\"\nsampler2D _MainTex;\nfloat4 _MainTex_TexelSize;\n" +
                "float4 _InputSize;\nfloat4 _CanvasSize;\nfloat _PreviewScale;\n" + uniforms +
                "float4 SampleInput(float2 uv) { return tex2D(_MainTex, uv); }\n" +
                LineDirective(1, assetPath) + expanded + "\n#line 1 \"SpriteFXWrapper\"\n" +
                "float4 SpriteFXFragment(v2f_img input) : SV_Target { return ApplyFX(input.uv, SampleInput(input.uv)); }\n" +
                "ENDCG\n}\n}\nFallback Off\n}\n";
        }

        private string ResolveIncludes(string source, string sourcePath)
        {
            if (source.Length > MaximumCharacters)
                throw new InvalidOperationException("Shader FX source exceeds the 2 MiB character limit.");
            StringBuilder result = new StringBuilder();
            bool blockComment = false;
            using (StringReader reader = new StringReader(source))
            {
                string original;
                int lineNumber = 0;
                while ((original = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    string line = original;
                    Match include = Include.Match(MaskComments(original, ref blockComment));
                    if (include.Success)
                    {
                        try
                        {
                            string requested = include.Groups[2].Value.Replace('\\', '/');
                            string resolved = Resolve(requested, sourcePath);
                            if (requested.StartsWith("Assets/", StringComparison.Ordinal) ||
                                requested.StartsWith("Packages/", StringComparison.Ordinal) ||
                                requested.StartsWith(".", StringComparison.Ordinal) ||
                                File.Exists(PhysicalPath(resolved)))
                            {
                                Group pathGroup = include.Groups[2];
                                line = original.Substring(0, pathGroup.Index) + resolved +
                                    original.Substring(pathGroup.Index + pathGroup.Length);
                            }
                        }
                        catch (Exception exception)
                        {
                            throw new InvalidOperationException($"{sourcePath}:{lineNumber}: {exception.Message}", exception);
                        }
                    }
                    result.AppendLine(line);
                }
            }
            if (blockComment)
                throw new InvalidOperationException($"{sourcePath}: unterminated block comment.");
            return result.ToString();
        }

        private string Resolve(string requested, string sourcePath)
        {
            requested = requested.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(requested) || Path.IsPathRooted(requested) ||
                requested.IndexOfAny(new[] { ':', '"', '\r', '\n' }) >= 0)
                throw new InvalidOperationException($"Use a project, package or relative library path: {requested}.");
            string relative = requested.StartsWith("Assets/", StringComparison.Ordinal) || requested.StartsWith("Packages/", StringComparison.Ordinal)
                ? requested : Path.GetDirectoryName(sourcePath).Replace('\\', '/') + "/" + requested;
            string absolute = Path.GetFullPath(Path.Combine(projectRoot, relative));
            string prefix = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Library path escapes the project: {requested}.");
            relative = absolute.Substring(prefix.Length).Replace('\\', '/');
            if (!relative.StartsWith("Assets/", StringComparison.Ordinal) && !relative.StartsWith("Packages/", StringComparison.Ordinal))
                throw new InvalidOperationException($"Libraries must be inside Assets or Packages: {requested}.");
            return relative;
        }

        private string PhysicalPath(string assetPath)
        {
            if (assetPath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                PackageInfo package = PackageInfo.FindForAssetPath(assetPath);
                if (package != null && assetPath.StartsWith(package.assetPath + "/", StringComparison.Ordinal))
                    return Path.Combine(package.resolvedPath, assetPath.Substring(package.assetPath.Length + 1));
            }
            return Path.Combine(projectRoot, assetPath);
        }

        private static string LineDirective(int number, string path) => $"#line {number} \"{path.Replace('\\', '/')}\"\n";

        private static string MaskComments(string line, ref bool blockComment)
        {
            StringBuilder clean = new StringBuilder(line.Length);
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char current = line[i];
                char next = i + 1 < line.Length ? line[i + 1] : '\0';
                if (blockComment)
                {
                    clean.Append(' ');
                    if (current == '*' && next == '/')
                    {
                        blockComment = false;
                        clean.Append(' ');
                        i++;
                    }
                    continue;
                }
                if (!quoted && current == '/' && next == '/')
                {
                    clean.Append(' ', line.Length - i);
                    break;
                }
                if (!quoted && current == '/' && next == '*')
                {
                    blockComment = true;
                    clean.Append("  ");
                    i++;
                    continue;
                }
                clean.Append(current);
                if (quoted && current == '\\' && next != '\0')
                {
                    clean.Append(next);
                    i++;
                }
                else if (current == '"')
                    quoted = !quoted;
            }
            return clean.ToString();
        }
    }
}
