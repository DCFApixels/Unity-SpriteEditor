using UnityEditor;
using UnityEngine;

namespace DCFApixels.SpriteEditor
{
    public enum BlendMode
    {
        Overwrite,
        Multiply
        // Добавьте другие режимы по необходимости
    }

    [System.Serializable]
    public struct TextureTransform
    {
        public static readonly TextureTransform Default = new TextureTransform { pivot = new Vector2(0.5f, 0.5f), position = Vector2.zero, scale = Vector2.one, rotation = 0f };
        public Vector2 pivot;
        public Vector2 position;
        public Vector2 scale;
        public float rotation; // degrees

        public Matrix4x4 ToMatrix()
        {
            Vector2 p = pivot;
            Matrix4x4 mat = Matrix4x4.identity;
            // Shift to pivot
            mat.m00 = 1; mat.m03 = -p.x;
            mat.m11 = 1; mat.m13 = -p.y;
            // Scale
            Matrix4x4 scaleMat = Matrix4x4.Scale(new Vector3(scale.x, scale.y, 1));
            mat = scaleMat * mat;
            // Rotate
            Matrix4x4 rotMat = Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotation));
            mat = rotMat * mat;
            // Shift back + position
            mat.m03 += p.x + position.x;
            mat.m13 += p.y + position.y;
            return mat;
        }

        public bool IsIdentity()
        {
            return position == Vector2.zero && scale == Vector2.one && rotation == 0f;
        }

        public void Reset()
        {
            pivot = new Vector2(0.5f, 0.5f);
            position = Vector2.zero;
            scale = Vector2.one;
            rotation = 0f;
        }
    }

    public static class GradientUtility
    {
        private static readonly GradientAlphaKey[] _alpha = new GradientAlphaKey[] { new(1, 0), new(1, 1) };
        public static readonly Gradient WhiteToBlack = Create(new GradientColorKey[] { new(Color.white, 0f), new(Color.black, 1f) });
        public static Gradient Create(Gradient gradient)
        {
            var result = new Gradient();
            result.SetKeys(gradient.colorKeys, gradient.alphaKeys);
            return result;
        }
        public static Gradient Create(GradientColorKey[] colorKeys)
        {
            return Create(colorKeys, _alpha);
        }
        public static Gradient Create(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys)
        {
            var result = new Gradient();
            result.SetKeys(colorKeys, alphaKeys);
            return result;
        }

        public static bool IsTwoColorGradient(Gradient gradient, out Color l, out Color r)
        {
            var colors = gradient.colorKeys;
            var alphas = gradient.alphaKeys;
            l = default; r = default;
            if (colors.Length <= 0 || alphas.Length <= 0 || 
                colors.Length > 2 || alphas.Length > 2) { return false; }

            if (colors.Length == 1)
            {
                l = r = colors[0].color;
            }
            else
            {
                if (colors[0].time == 0f && colors[1].time == 1f)
                {
                    l = colors[0].color;
                    r = colors[1].color;
                }
                else
                {
                    return false;
                }
            }

            if (alphas.Length == 1)
            {
                l.a = r.a = alphas[0].alpha;
            }
            else
            {
                if (alphas[0].time == 0f && alphas[1].time == 1f)
                {
                    l.a = alphas[0].alpha;
                    r.a = alphas[1].alpha;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }


    public static class SEGUI
    {
        public static void DrawTextureTransform(ref TextureTransform transform)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
            if (GUILayout.Button("Reset"))
            {
                transform.Reset();
                GUI.changed = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;

            float dw = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 80f;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pivot", GUILayout.Width(EditorGUIUtility.labelWidth));
            transform.pivot = EditorGUILayout.Vector2Field("", transform.pivot);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Position", GUILayout.Width(EditorGUIUtility.labelWidth));
            transform.position = EditorGUILayout.Vector2Field("", transform.position);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scale", GUILayout.Width(EditorGUIUtility.labelWidth));
            transform.scale = EditorGUILayout.Vector2Field("", transform.scale);
            EditorGUILayout.EndHorizontal();

            transform.rotation = EditorGUILayout.FloatField("Rotation", transform.rotation);

            EditorGUIUtility.labelWidth = dw;
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
    }
}