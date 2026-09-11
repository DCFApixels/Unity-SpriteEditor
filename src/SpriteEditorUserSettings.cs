using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    internal static class SpriteEditorUserSettings
    {
        private const string LightKey = "DCFApixels.SpriteEditor.Preview.CheckerLight";
        private const string DarkKey = "DCFApixels.SpriteEditor.Preview.CheckerDark";
        private const string ErrorKey = "DCFApixels.SpriteEditor.Preview.InvalidPixels";
        private const string SizeKey = "DCFApixels.SpriteEditor.Preview.CheckerSize";
        private const string PostFxBackgroundKey = "DCFApixels.SpriteEditor.Preview.PostFxBackground";
        private const string PresetsFolderKey = "DCFApixels.SpriteEditor.PresetsFolder";
        internal static string DefaultPresetsFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DCFApixels", "SpriteEditor", "Presets");
        internal static string PresetsFolder => EditorPrefs.GetString(PresetsFolderKey, DefaultPresetsFolder);

        internal static bool TrySetPresetsFolder(string value, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                ResetPresetsFolder();
                return true;
            }
            try
            {
                value = value.Trim();
                if (!Path.IsPathFullyQualified(value))
                {
                    error = "Choose an absolute folder path so it is the same in every project.";
                    return false;
                }
                value = Path.GetFullPath(value);
                if (File.Exists(value))
                {
                    error = "This path points to a file. Choose a folder instead.";
                    return false;
                }
                EditorPrefs.SetString(PresetsFolderKey, value);
                Changed?.Invoke();
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException ||
                exception is NotSupportedException || exception is System.Security.SecurityException || exception is UnauthorizedAccessException)
            {
                error = "Cannot use this folder path: " + exception.Message;
                return false;
            }
        }

        internal static void ResetPresetsFolder()
        {
            EditorPrefs.DeleteKey(PresetsFolderKey);
            Changed?.Invoke();
        }
        private static Color? postFxBackground = Load(PostFxBackgroundKey);
        internal static Color PostFxBackground
        {
            get => postFxBackground ?? Color.black;
            set => Save(PostFxBackgroundKey, ref postFxBackground, value);
        }
        internal const int DefaultCheckerSize = 16;
        internal const int MinimumCheckerSize = 1;
        internal const int MaximumCheckerSize = 128;
        private static int checkerSize = Mathf.Clamp(EditorPrefs.GetInt(SizeKey, DefaultCheckerSize), MinimumCheckerSize, MaximumCheckerSize);
        private static Color? checkerLight = Load(LightKey);
        private static Color? checkerDark = Load(DarkKey);
        private static Color? invalidPixels = Load(ErrorKey);

        internal static event Action Changed;

        internal static int CheckerSize
        {
            get => checkerSize;
            set
            {
                value = Mathf.Clamp(value, MinimumCheckerSize, MaximumCheckerSize);
                if (checkerSize == value) return;
                checkerSize = value;
                EditorPrefs.SetInt(SizeKey, value);
                Changed?.Invoke();
            }
        }

        internal static Color CheckerLight
        {
            get => checkerLight ?? (EditorGUIUtility.isProSkin ? new Color(.30f, .30f, .30f) : new Color(.84f, .84f, .84f));
            set => Save(LightKey, ref checkerLight, value);
        }

        internal static Color CheckerDark
        {
            get => checkerDark ?? (EditorGUIUtility.isProSkin ? new Color(.23f, .23f, .23f) : new Color(.70f, .70f, .70f));
            set => Save(DarkKey, ref checkerDark, value);
        }

        internal static Color InvalidPixels
        {
            get => invalidPixels ?? Color.magenta;
            set => Save(ErrorKey, ref invalidPixels, value);
        }

        private static Color? Load(string key)
        {
            if (!ColorUtility.TryParseHtmlString(EditorPrefs.GetString(key, string.Empty), out Color color)) return null;
            color.a = 1f;
            return color;
        }

        private static void Save(string key, ref Color? stored, Color color)
        {
            color = new Color(Channel(color.r), Channel(color.g), Channel(color.b), 1f);
            string html = "#" + ColorUtility.ToHtmlStringRGB(color);
            ColorUtility.TryParseHtmlString(html, out color);
            if (stored.HasValue && stored.Value == color) return;
            stored = color;
            EditorPrefs.SetString(key, html);
            Changed?.Invoke();
        }

        private static float Channel(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp01(value);

        internal static void Reset()
        {
            EditorPrefs.DeleteKey(PresetsFolderKey);
            ResetPreviewAppearance();
        }

        internal static void ResetPreviewAppearance()
        {
            EditorPrefs.DeleteKey(LightKey);
            EditorPrefs.DeleteKey(DarkKey);
            EditorPrefs.DeleteKey(ErrorKey);
            EditorPrefs.DeleteKey(SizeKey);
            EditorPrefs.DeleteKey(PostFxBackgroundKey);
            postFxBackground = null;
            checkerSize = DefaultCheckerSize;
            checkerLight = checkerDark = invalidPixels = null;
            Changed?.Invoke();
        }
    }
}
