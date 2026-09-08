using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DCFApixels.SpriteEditor
{
    public sealed partial class TextureCompositorWindow
    {
        private const int AllPreviewChannels = 15;
        [SerializeField] private int previewChannels = AllPreviewChannels;
        [NonSerialized] private RenderTexture channelPreviewTexture;
        [NonSerialized] private Button[] channelButtons;

        private Vector4 PreviewChannelMask => new Vector4(
            (previewChannels & 1) != 0 ? 1f : 0f,
            (previewChannels & 2) != 0 ? 1f : 0f,
            (previewChannels & 4) != 0 ? 1f : 0f,
            (previewChannels & 8) != 0 ? 1f : 0f);

        private VisualElement BuildPreviewFooter()
        {
            VisualElement footer = new VisualElement();
            footer.AddToClassList("sprite-editor-preview-footer");
            footer.Add(BuildPreviewQualityControl());
            footer.RegisterCallback<GeometryChangedEvent>(evt =>
                footer.EnableInClassList("sprite-editor-preview-footer--compact", evt.newRect.width < 290f));
            toolkitPreviewFooter = new Label();
            toolkitPreviewFooter.AddToClassList("sprite-editor-preview-status");
            footer.Add(toolkitPreviewFooter);
            VisualElement channels = new VisualElement();
            channels.AddToClassList("sprite-editor-preview-channels");
            footer.Add(channels);
            channelButtons = new Button[4];
            string[] labels = { "R", "G", "B", "A" };
            for (int i = 0; i < labels.Length; i++)
            {
                int bit = 1 << i;
                Button button = new Button(() => TogglePreviewChannel(bit)) { text = labels[i] };
                button.tooltip = i == 3
                    ? "Alpha: off ignores transparency in Preview and gives the brush A=0 (no paint). " +
                      "Enable only A to view alpha in grayscale. Eraser is unaffected."
                    : labels[i] + " channel: show in Preview and use the brush value; off paints this component as 0. " +
                      "A single RGB channel is shown in grayscale; A controls its transparency. " +
                      "Existing pixels are not changed by toggling. Eraser is unaffected.";
                button.AddToClassList("sprite-editor-channel-button");
                if (i < 3)
                    button.AddToClassList("sprite-editor-channel-button--" + labels[i].ToLowerInvariant());
                channelButtons[i] = button;
                channels.Add(button);
            }
            RefreshChannelButtons();
            return footer;
        }

        private VisualElement BuildPreviewQualityControl()
        {
            VisualElement control = new VisualElement { tooltip = LivePreviewQualityContent.tooltip };
            control.AddToClassList("sprite-editor-preview-quality");
            Label label = new Label("Live Quality");
            label.AddToClassList("sprite-editor-preview-quality-label");
            control.Add(label);
            Slider quality = new Slider(
                MinimumPaintingPreviewScale * 100f, MaximumPaintingPreviewScale * 100f)
            {
                value = paintingPreviewScale * 100f,
                tooltip = LivePreviewQualityContent.tooltip
            };
            quality.AddToClassList("sprite-editor-preview-quality-slider");
            Label value = new Label($"{paintingPreviewScale * 100f:0.#}%");
            value.AddToClassList("sprite-editor-preview-quality-value");
            quality.RegisterValueChangedCallback(evt =>
            {
                paintingPreviewScale = ClampPaintingPreviewScale(evt.newValue * 0.01f);
                EditorPrefs.SetFloat(PaintingPreviewScalePrefKey, paintingPreviewScale);
                value.text = $"{paintingPreviewScale * 100f:0.#}%";
            });
            control.Add(quality);
            control.Add(value);
            return control;
        }

        private void TogglePreviewChannel(int bit)
        {
            FinishPreviewTransform();
            FinishPaintingStroke();
            previewChannels = (previewChannels ^ bit) & AllPreviewChannels;
            RefreshChannelButtons();
            UpdateChannelPreview();
            UpdateToolkitPreviewPresentation();
        }

        private void RefreshChannelButtons()
        {
            if (channelButtons == null)
                return;
            for (int i = 0; i < channelButtons.Length; i++)
                channelButtons[i].EnableInClassList("sprite-editor-channel-button--enabled", (previewChannels & (1 << i)) != 0);
        }

        private Color GetPaintingColor()
        {
            Color color = paintSettings.brushColor;
            if (paintingErase)
                return color;
            Vector4 mask = PreviewChannelMask;
            return new Color(color.r * mask.x, color.g * mask.y, color.b * mask.z, color.a * mask.w);
        }

        private void UpdateChannelPreview()
        {
            if (previewTexture == null || (previewChannels & AllPreviewChannels) == AllPreviewChannels)
            {
                ReleaseChannelPreview();
                return;
            }
            Material material = SpriteEditorMaterials.PreviewChannels;
            if (material == null)
            {
                ReleaseChannelPreview();
                return;
            }
            if (channelPreviewTexture == null || channelPreviewTexture.width != previewTexture.width ||
                channelPreviewTexture.height != previewTexture.height)
            {
                ReleaseChannelPreview();
                channelPreviewTexture = new RenderTexture(previewTexture.width, previewTexture.height, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                {
                    name = "Sprite Editor Channel Preview",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
            channelPreviewTexture.filterMode = previewTexture.filterMode;
            material.SetVector("_Channels", PreviewChannelMask);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(previewTexture, channelPreviewTexture, material);
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private void ReleaseChannelPreview()
        {
            if (channelPreviewTexture == null)
                return;
            channelPreviewTexture.Release();
            DestroyImmediate(channelPreviewTexture);
            channelPreviewTexture = null;
        }
    }
}
