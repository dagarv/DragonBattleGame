using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonBattle.UI
{
    public class MenuKit
    {
        private readonly int layer;
        private readonly TMP_FontAsset itemFont;
        private readonly Color accent;
        private readonly Color textNormal;
        private readonly Color textHighlight;

        public Sprite SoftSprite { get; }
        public Sprite FadeSprite { get; }

        public MenuKit(int layer, TMP_FontAsset itemFont, Color accent, Color textNormal, Color textHighlight)
        {
            this.layer = layer;
            this.itemFont = itemFont;
            this.accent = accent;
            this.textNormal = textNormal;
            this.textHighlight = textHighlight;
            SoftSprite = CreateSoftSprite();
            FadeSprite = CreateFadeSprite();
        }

        public void Dispose()
        {
            DestroySprite(SoftSprite);
            DestroySprite(FadeSprite);
        }

        public RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, float size, Color color, string text)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                label.font = font;
            }
            label.fontSize = size;
            label.color = color;
            label.text = text;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public MenuItem CreateItem(Transform parent, string text, Vector2 position, float fontSize, float width, float height, bool hasValue)
        {
            RectTransform rect = CreateRect(text, parent);
            Place(rect, position, new Vector2(width, height));
            CanvasGroup group = rect.gameObject.AddComponent<CanvasGroup>();

            RectTransform content = CreateRect("Content", rect);
            Stretch(content);
            content.pivot = new Vector2(0f, 0.5f);

            Image glow = CreateImage("Glow", content, new Color(accent.r, accent.g * 0.7f, accent.b * 0.5f, 0.32f), SoftSprite);
            glow.raycastTarget = true;
            glow.rectTransform.anchorMin = new Vector2(0f, 0f);
            glow.rectTransform.anchorMax = new Vector2(1f, 1f);
            glow.rectTransform.pivot = new Vector2(0f, 0.5f);
            glow.rectTransform.offsetMin = new Vector2(-30f, -6f);
            glow.rectTransform.offsetMax = new Vector2(0f, 6f);

            RectTransform marker = CreateImage("Marker", content, accent).rectTransform;
            marker.anchorMin = new Vector2(0f, 0.5f);
            marker.anchorMax = new Vector2(0f, 0.5f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.anchoredPosition = new Vector2(10f, 0f);
            float markerSize = Mathf.Round(fontSize * 0.22f);
            marker.sizeDelta = new Vector2(markerSize, markerSize);
            marker.localRotation = Quaternion.Euler(0f, 0f, 45f);

            TMP_Text label = CreateText("Label", content, itemFont, fontSize, textNormal, text.ToUpperInvariant());
            label.characterSpacing = 8f;
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(0f, 1f);
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(width - 40f, 0f);
            label.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            label.margin = new Vector4(36f, 0f, 0f, 0f);

            RectTransform underline = CreateImage("Underline", content, accent, FadeSprite).rectTransform;
            underline.anchorMin = new Vector2(0f, 0f);
            underline.anchorMax = new Vector2(0f, 0f);
            underline.pivot = new Vector2(0f, 0.5f);
            underline.anchoredPosition = new Vector2(36f, 6f);
            underline.sizeDelta = new Vector2(Mathf.Min(width * 0.55f, 360f), 2f);

            RectTransform valueArea = null;
            if (hasValue)
            {
                valueArea = CreateRect("Value", content);
                valueArea.anchorMin = new Vector2(1f, 0.5f);
                valueArea.anchorMax = new Vector2(1f, 0.5f);
                valueArea.pivot = new Vector2(1f, 0.5f);
                valueArea.anchoredPosition = Vector2.zero;
                valueArea.sizeDelta = new Vector2(330f, height);
            }

            MenuItem item = rect.gameObject.AddComponent<MenuItem>();
            item.targetGraphic = glow;
            item.Setup(content, label, glow, marker, underline, valueArea, group, textNormal, textHighlight);
            return item;
        }

        public static void LinkVertical(List<MenuItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                Navigation navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = items[(i - 1 + items.Count) % items.Count],
                    selectOnDown = items[(i + 1) % items.Count]
                };
                items[i].navigation = navigation;
            }
        }

        public static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        public static void DestroySprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }
            Object.Destroy(sprite.texture);
            Object.Destroy(sprite);
        }

        private static Sprite CreateSoftSprite()
        {
            const int width = 128;
            const int height = 32;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = Mathf.Sin((y + 0.5f) / height * Mathf.PI);
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width - 1);
                    float alpha = Mathf.Pow(1f - u, 1.6f) * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, u * 8f)) * v * v;
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateFadeSprite()
        {
            const int width = 128;
            Texture2D texture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / (width - 1);
                texture.SetPixel(x, 0, new Color(1f, 1f, 1f, Mathf.Pow(1f - u, 1.3f)));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, 1f), new Vector2(0.5f, 0.5f));
        }
    }
}
