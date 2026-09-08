using System;
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
            toolkitPreviewFooter = new Label();
            toolkitPreviewFooter.AddToClassList("sprite-editor-preview-status");
            footer.Add(toolkitPreviewFooter);
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
                      "Existing pixels are not changed by toggling. Eraser is unaffected.";
                button.AddToClassList("sprite-editor-channel-button");
                if (i < 3)
                    button.AddToClassList("sprite-editor-channel-button--" + labels[i].ToLowerInvariant());
                channelButtons[i] = button;
                footer.Add(button);
            }
            RefreshChannelButtons();
            return footer;
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

        private Color GetPaintingColor(DrawingLayer layer)
        {
            Color color = layer.brushColor;
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
