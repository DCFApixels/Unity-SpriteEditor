using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

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
        public static Gradient Clone(Gradient gradient)
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