using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    internal static class ShaderFXCatalog
    {
        internal sealed class Entry
        {
            internal string guid, path, menuPath, source, error, hash;
            internal ShaderFX asset;
        }

        private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private static bool initialized;
        private static bool queued;
        private static readonly HashSet<string> changed = new HashSet<string>(StringComparer.Ordinal);

        internal static string ReadSource(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new IOException("The HLSL effect source is missing. The last applied shader is retained.");
            string physical = path;
            if (path.StartsWith("Packages/", StringComparison.Ordinal))
            {
                PackageInfo package = PackageInfo.FindForAssetPath(path);
                if (package != null) physical = Path.Combine(package.resolvedPath, path.Substring(package.assetPath.Length + 1));
            }
            if (new FileInfo(physical).Length > 2 * 1024 * 1024) throw new IOException("HLSL effect exceeds 2 MiB.");
            return File.ReadAllText(physical);
        }

        private static void Inspect(string path)
        {
            entries.Remove(path);
            if (path.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase))
            {
                string physical = path;
                if (path.StartsWith("Packages/", StringComparison.Ordinal))
                {
                    PackageInfo package = PackageInfo.FindForAssetPath(path);
                    if (package != null) physical = Path.Combine(package.resolvedPath, path.Substring(package.assetPath.Length + 1));
                }
                try
                {
                    using var reader = new StreamReader(physical);
                    // Discovery reads only the first physical line of unrelated HLSL files.
                    if (!ShaderFXMetadata.TryHeader(reader.ReadLine(), out string menuPath)) return;
                    var entry = new Entry { path = path, guid = AssetDatabase.AssetPathToGUID(path), menuPath = menuPath };
                    try
                    {
                        entry.source = ReadSource(path);
                        ShaderFXMetadata.Parse(entry.source, true, out _);
                        entry.hash = AssetDatabase.GetAssetDependencyHash(path).ToString();
                    }
                    catch (Exception error) { entry.error = error.Message; }
                    entries[path] = entry;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            else if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path) as ShaderFX;
                if (asset != null && asset.EmbeddedOwner == null)
                    entries[path] = new Entry { path = path, guid = AssetDatabase.AssetPathToGUID(path), menuPath = "Shader FX/" + asset.name, asset = asset };
            }
        }

        internal static IReadOnlyList<Entry> GetEntries()
        {
            if (!initialized)
            {
                initialized = true;
                foreach (string path in AssetDatabase.GetAllAssetPaths())
                    if (path.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase)) Inspect(path);
                foreach (string guid in AssetDatabase.FindAssets("t:ShaderFX")) Inspect(AssetDatabase.GUIDToAssetPath(guid));
            }
            var list = new List<Entry>(entries.Values);
            list.Sort((a, b) => string.Compare(a.menuPath, b.menuPath, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        internal static void ShowMenu(Action<Entry> select)
        {
            var menu = new GenericMenu();
            var list = GetEntries();
            if (list.Count == 0) menu.AddDisabledItem(new GUIContent("No effects found — add a marked .hlsl file"));
            foreach (var entry in list)
            {
                bool duplicate = false;
                foreach (var other in list) if (other != entry && other.menuPath == entry.menuPath) { duplicate = true; break; }
                string label = entry.menuPath + (duplicate ? " (" + entry.path.Replace('/', '›') + ")" : "");
                var content = new GUIContent(label, entry.error ?? entry.path);
                if (entry.error != null) menu.AddDisabledItem(content);
                else menu.AddItem(content, false, () => select(entry));
            }
            menu.ShowAsContext();
        }

        internal static void AssetsChanged(params string[][] batches)
        {
            foreach (var batch in batches) foreach (string path in batch) changed.Add(path);
            if (queued) return;
            queued = true;
            EditorApplication.delayCall += FlushChanges;
        }

        private static void FlushChanges()
        {
            queued = false;
            bool shaderChanged = false;
            foreach (string path in changed)
            {
                if (initialized) Inspect(path);
                shaderChanged |= path.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cginc", StringComparison.OrdinalIgnoreCase);
            }
            if (shaderChanged)
                foreach (var fx in Resources.FindObjectsOfTypeAll<ShaderFX>())
                    if (fx != null && fx.IsCatalogLinked) fx.OnCatalogFilesChanged(changed);
            changed.Clear();
        }
    }

    internal sealed class ShaderFXCatalogPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom) =>
            ShaderFXCatalog.AssetsChanged(imported, deleted, moved, movedFrom);
    }
}
