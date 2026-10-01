using DragonBattle.Combat;
using DragonBattle.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonBattle.UI
{
    public class OverheadHealthBar : MonoBehaviour
    {
        private static Canvas sharedCanvas;

        [SerializeField] private Health health;
        [SerializeField] private DragonIdentity identity;

        [Header("Look")]
        [SerializeField] private Sprite portrait;
        [SerializeField] private Sprite barSprite;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Color fillColor = new Color(0.36f, 0.85f, 0.2f);
        [SerializeField] private Color trailColor = new Color(0.95f, 0.85f, 0.75f);
        [SerializeField] private Color backColor = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        [SerializeField] private Color tickColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(1f)] private float healthPerTick = 10f;

        [Header("Layout")]
        [SerializeField] private Vector2 barSize = new Vector2(110f, 12f);
        [SerializeField, Min(0f)] private float portraitSize = 22f;
        [SerializeField, Min(0f)] private float extraHeight = 0.6f;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float trailDelay = 0.4f;
        [SerializeField, Min(0.01f)] private float trailSpeed = 0.6f;
        [SerializeField, Min(0.01f)] private float fadeSpeed = 3f;

        private RectTransform root;
        private RectTransform fill;
        private RectTransform trail;
        private CanvasGroup group;
        private Camera cam;
        private float headHeight;
        private float trailValue;
        private float trailHoldUntil;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
            if (identity == null)
            {
                identity = GetComponent<DragonIdentity>();
            }
        }

        private void OnEnable()
        {
            health.OnDamaged += HandleDamaged;
            if (root != null)
            {
                root.gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            health.OnDamaged -= HandleDamaged;
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (root != null)
            {
                Destroy(root.gameObject);
            }
        }

        private void Start()
        {
            cam = Camera.main;
            headHeight = MeasureHeight() + extraHeight;
            Build();
            trailValue = health.Normalized;
            SetWidth(fill, trailValue);
            SetWidth(trail, trailValue);
        }

        private void LateUpdate()
        {
            if (root == null)
            {
                return;
            }
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null)
                {
                    return;
                }
            }

            float value = health.Normalized;
            SetWidth(fill, value);
            if (Time.time >= trailHoldUntil && trailValue > value)
            {
                trailValue = Mathf.MoveTowards(trailValue, value, trailSpeed * Time.deltaTime);
            }
            trailValue = Mathf.Max(trailValue, value);
            SetWidth(trail, trailValue);

            float targetAlpha = health.IsDead ? 0f : 1f;
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * headHeight);
            bool visible = screen.z > 0f;
            if (visible)
            {
                RectTransform canvasRect = (RectTransform)sharedCanvas.transform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
                root.anchoredPosition = local;
            }
            if (root.gameObject.activeSelf != visible)
            {
                root.gameObject.SetActive(visible);
            }
        }

        private void HandleDamaged(Health source, float amount)
        {
            trailHoldUntil = Time.time + trailDelay;
        }

        private float MeasureHeight()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            float top = transform.position.y + 2f;
            bool found = false;
            foreach (Renderer r in renderers)
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                {
                    continue;
                }
                top = found ? Mathf.Max(top, r.bounds.max.y) : r.bounds.max.y;
                found = true;
            }
            return top - transform.position.y;
        }

        private static Canvas GetCanvas()
        {
            if (sharedCanvas != null)
            {
                return sharedCanvas;
            }
            GameObject go = new GameObject("OverheadBars", typeof(RectTransform));
            sharedCanvas = go.AddComponent<Canvas>();
            sharedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            sharedCanvas.sortingOrder = -10;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return sharedCanvas;
        }

        private void Build()
        {
            Canvas canvas = GetCanvas();
            Color accent = identity != null ? identity.AccentColor : Color.white;
            float rowWidth = portraitSize + 4f + barSize.x + 4f + 26f;
            float rowHeight = Mathf.Max(portraitSize, barSize.y);

            root = CreateRect("Overhead_" + name, canvas.transform);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(rowWidth, rowHeight + 20f);
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            TMP_Text nameText = CreateText("Name", root, identity != null ? identity.DisplayName : name, 15f, accent);
            RectTransform nameRect = nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.sizeDelta = new Vector2(40f, 18f);
            nameRect.anchoredPosition = Vector2.zero;
            nameText.alignment = TextAlignmentOptions.Center;

            RectTransform row = CreateRect("Row", root);
            row.anchorMin = new Vector2(0f, 0f);
            row.anchorMax = new Vector2(1f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.sizeDelta = new Vector2(0f, rowHeight);
            row.anchoredPosition = Vector2.zero;

            RectTransform portraitFrame = CreateImage("PortraitFrame", row, null, backColor).rectTransform;
            portraitFrame.anchorMin = portraitFrame.anchorMax = new Vector2(0f, 0.5f);
            portraitFrame.pivot = new Vector2(0f, 0.5f);
            portraitFrame.sizeDelta = new Vector2(portraitSize, portraitSize);
            portraitFrame.anchoredPosition = Vector2.zero;

            Image portraitImage = CreateImage("Portrait", portraitFrame, portrait, portrait != null ? Color.white : accent);
            Stretch(portraitImage.rectTransform, 2f);
            portraitImage.preserveAspect = portrait != null;

            RectTransform bar = CreateImage("Bar", row, null, backColor).rectTransform;
            bar.anchorMin = bar.anchorMax = new Vector2(0f, 0.5f);
            bar.pivot = new Vector2(0f, 0.5f);
            bar.sizeDelta = barSize + new Vector2(2f, 2f);
            bar.anchoredPosition = new Vector2(portraitSize + 4f, 0f);

            RectTransform area = CreateRect("Area", bar);
            Stretch(area, 1f);

            trail = CreateImage("Trail", area, barSprite, trailColor).rectTransform;
            fill = CreateImage("Fill", area, barSprite, fillColor).rectTransform;

            int ticks = Mathf.FloorToInt((health.MaxHealth - 0.01f) / healthPerTick);
            for (int i = 1; i <= ticks; i++)
            {
                float x = i * healthPerTick / health.MaxHealth;
                RectTransform tick = CreateImage("Tick", area, null, tickColor).rectTransform;
                tick.anchorMin = new Vector2(x, 0f);
                tick.anchorMax = new Vector2(x, 1f);
                tick.pivot = new Vector2(0.5f, 0.5f);
                tick.sizeDelta = new Vector2(1f, 0f);
                tick.anchoredPosition = Vector2.zero;
            }

            TMP_Text levelText = CreateText("Level", row, level.ToString(), 14f, Color.white);
            RectTransform levelRect = levelText.rectTransform;
            levelRect.anchorMin = levelRect.anchorMax = new Vector2(1f, 0.5f);
            levelRect.pivot = new Vector2(1f, 0.5f);
            levelRect.sizeDelta = new Vector2(26f, rowHeight);
            levelRect.anchoredPosition = Vector2.zero;
            levelText.alignment = TextAlignmentOptions.Center;
        }

        private static void SetWidth(RectTransform rect, float value)
        {
            if (rect == null)
            {
                return;
            }
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image CreateImage(string objectName, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = CreateRect(objectName, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text CreateText(string objectName, Transform parent, string value, float size, Color color)
        {
            RectTransform rect = CreateRect(objectName, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.outlineWidth = 0.2f;
            text.outlineColor = Color.black;
            return text;
        }
    }
}
