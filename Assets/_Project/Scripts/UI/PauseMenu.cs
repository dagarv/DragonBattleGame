using System.Collections;
using System.Collections.Generic;
using DragonBattle.Audio;
using DragonBattle.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DragonBattle.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private TMP_FontAsset itemFont;
        [SerializeField] private TMP_FontAsset bodyFont;

        [Header("Palette")]
        [SerializeField] private Color titleTop = new Color(1f, 0.9f, 0.62f);
        [SerializeField] private Color titleBottom = new Color(0.95f, 0.45f, 0.12f);
        [SerializeField] private Color accent = new Color(1f, 0.62f, 0.22f);
        [SerializeField] private Color textNormal = new Color(0.93f, 0.87f, 0.78f, 0.55f);
        [SerializeField] private Color textHighlight = new Color(1f, 0.93f, 0.78f, 1f);
        [SerializeField] private Color scrimColor = new Color(0.02f, 0.012f, 0.01f, 0.75f);

        [Header("Text")]
        [SerializeField] private string kickerText = "The Duel Holds Its Breath";
        [SerializeField] private string titleText = "Paused";
        [SerializeField] private string hintText = "<color=#FFB45A>Enter</color>  Select      <color=#FFB45A>Esc</color>  Resume      <color=#FFB45A>Mouse</color>  Point";

        [Header("Layout")]
        [SerializeField] private int sortingOrder = 60;
        [SerializeField, Min(0f)] private float leftMargin = 150f;

        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float openDuration = 0.35f;
        [SerializeField, Min(0f)] private float itemStagger = 0.06f;
        [SerializeField, Min(0.01f)] private float itemDuration = 0.4f;
        [SerializeField, Min(0.01f)] private float closeDuration = 0.2f;

        [Header("Audio")]
        [SerializeField] private SoundCue openSound = new SoundCue();
        [SerializeField] private SoundCue hoverSound = new SoundCue();
        [SerializeField] private SoundCue confirmSound = new SoundCue();
        [SerializeField] private SoundCue backSound = new SoundCue();

        private readonly List<MenuItem> items = new List<MenuItem>();

        private MenuKit kit;
        private InputAction pauseAction;
        private CanvasGroup rootGroup;
        private RectTransform contentRoot;
        private RectTransform divider;
        private TMP_Text title;
        private GameObject lastSelected;
        private Coroutine transition;
        private bool isOpen;
        private bool suppressHoverSound;

        private void Reset()
        {
            gameManager = GetComponent<GameManager>();
        }

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GetComponent<GameManager>();
            }

            pauseAction = new InputAction("Pause", InputActionType.Button);
            pauseAction.AddBinding("<Keyboard>/escape");
            pauseAction.AddBinding("<Gamepad>/start");

            Build();
            SetShown(0f);
            SetInteractable(false);
        }

        private void OnEnable()
        {
            pauseAction.Enable();
        }

        private void OnDisable()
        {
            pauseAction.Disable();
        }

        private void OnDestroy()
        {
            pauseAction.Dispose();
            kit?.Dispose();
        }

        private void Update()
        {
            if (pauseAction.WasPressedThisFrame())
            {
                if (isOpen)
                {
                    Resume();
                }
                else if (gameManager.CanPause)
                {
                    Open();
                }
                return;
            }

            if (isOpen && rootGroup.interactable)
            {
                KeepSelection();
            }
        }

        private void Open()
        {
            gameManager.SetPaused(true);
            if (!gameManager.IsPaused)
            {
                return;
            }

            isOpen = true;
            openSound.Play2D();
            Play(OpenSequence());
        }

        private void Resume()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            backSound.Play2D();
            SetInteractable(false);
            ClearSelection();
            gameManager.SetPaused(false);
            Play(CloseSequence());
        }

        private void Play(IEnumerator routine)
        {
            if (transition != null)
            {
                StopCoroutine(transition);
            }
            transition = StartCoroutine(routine);
        }

        private IEnumerator OpenSequence()
        {
            foreach (MenuItem item in items)
            {
                item.SetReveal(0f);
            }
            SetInteractable(true);
            Select(items[0]);

            Vector2 origin = new Vector2(leftMargin, 20f);
            float start = Time.unscaledTime;
            float end = Mathf.Max(openDuration, itemStagger * items.Count + itemDuration);
            while (true)
            {
                float t = Time.unscaledTime - start;
                float open = Ease.OutCubic(t / openDuration);
                SetShown(open);
                contentRoot.anchoredPosition = origin + new Vector2(-40f * (1f - open), 0f);
                divider.localScale = new Vector3(open, 1f, 1f);
                title.characterSpacing = Mathf.Lerp(18f, 0f, open);

                for (int i = 0; i < items.Count; i++)
                {
                    items[i].SetReveal((t - 0.08f - itemStagger * i) / itemDuration);
                }

                if (t >= end)
                {
                    break;
                }
                yield return null;
            }

            SetShown(1f);
            contentRoot.anchoredPosition = origin;
            transition = null;
        }

        private IEnumerator CloseSequence()
        {
            float from = rootGroup.alpha;
            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < closeDuration)
            {
                SetShown(from * (1f - Ease.InCubic((Time.unscaledTime - start) / closeDuration)));
                yield return null;
            }
            SetShown(0f);
            transition = null;
        }

        private void HandleRestart()
        {
            if (!isOpen)
            {
                return;
            }
            confirmSound.Play2D();
            SetInteractable(false);
            gameManager.Restart();
        }

        private void HandleMainMenu()
        {
            if (!isOpen)
            {
                return;
            }
            confirmSound.Play2D();
            SetInteractable(false);
            gameManager.ReturnToMenu();
        }

        private void HandleQuit()
        {
            if (!isOpen)
            {
                return;
            }
            backSound.Play2D();
            SetInteractable(false);
            GameSettings.Save();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleHighlighted()
        {
            if (!suppressHoverSound && rootGroup.interactable)
            {
                hoverSound.Play2D();
            }
        }

        private void Select(MenuItem item)
        {
            EventSystem system = EventSystem.current;
            if (system == null)
            {
                return;
            }
            suppressHoverSound = true;
            system.SetSelectedGameObject(null);
            system.SetSelectedGameObject(item.gameObject);
            suppressHoverSound = false;
            lastSelected = item.gameObject;
        }

        private void KeepSelection()
        {
            EventSystem system = EventSystem.current;
            if (system == null)
            {
                return;
            }

            GameObject selected = system.currentSelectedGameObject;
            if (selected != null && selected.activeInHierarchy)
            {
                lastSelected = selected;
                return;
            }

            if (lastSelected != null && lastSelected.activeInHierarchy)
            {
                suppressHoverSound = true;
                system.SetSelectedGameObject(lastSelected);
                suppressHoverSound = false;
            }
        }

        private static void ClearSelection()
        {
            EventSystem system = EventSystem.current;
            if (system != null)
            {
                system.SetSelectedGameObject(null);
            }
        }

        private void SetShown(float amount)
        {
            rootGroup.alpha = amount;
        }

        private void SetInteractable(bool interactable)
        {
            rootGroup.interactable = interactable;
            rootGroup.blocksRaycasts = interactable;
        }

        private void Build()
        {
            int layer = LayerMask.NameToLayer("UI");
            kit = new MenuKit(layer >= 0 ? layer : gameObject.layer, itemFont, accent, textNormal, textHighlight);

            RectTransform root = kit.CreateRect("Pause Menu", transform);
            Canvas canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.gameObject.AddComponent<GraphicRaycaster>();
            rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            Image scrim = kit.CreateImage("Scrim", root, scrimColor);
            scrim.raycastTarget = true;
            MenuKit.Stretch(scrim.rectTransform);

            contentRoot = kit.CreateRect("Content", root);
            contentRoot.anchorMin = new Vector2(0f, 0.5f);
            contentRoot.anchorMax = new Vector2(0f, 0.5f);
            contentRoot.pivot = new Vector2(0f, 0.5f);
            contentRoot.sizeDelta = new Vector2(900f, 900f);
            contentRoot.anchoredPosition = new Vector2(leftMargin, 20f);

            BuildHeader();
            BuildItems();
            BuildHint(root);
        }

        private void BuildHeader()
        {
            TMP_Text kicker = kit.CreateText("Kicker", contentRoot, bodyFont, 24f, accent, kickerText.ToUpperInvariant());
            kicker.characterSpacing = 28f;
            MenuKit.Place(kicker.rectTransform, new Vector2(4f, 250f), new Vector2(900f, 40f));

            title = kit.CreateText("Title", contentRoot, titleFont, 112f, Color.white, titleText);
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(titleTop, titleTop, titleBottom, titleBottom);
            MenuKit.Place(title.rectTransform, new Vector2(0f, 170f), new Vector2(1000f, 140f));
            Material titleMaterial = title.fontMaterial;
            titleMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            titleMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.85f));
            titleMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.15f);
            titleMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.35f);
            titleMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.6f);
            titleMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.15f);

            divider = kit.CreateImage("Divider", contentRoot, accent, kit.FadeSprite).rectTransform;
            MenuKit.Place(divider, new Vector2(30f, 90f), new Vector2(520f, 2f));

            RectTransform marker = kit.CreateImage("DividerMarker", contentRoot, accent).rectTransform;
            MenuKit.Place(marker, new Vector2(10f, 90f), new Vector2(12f, 12f));
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.anchoredPosition = new Vector2(10f, 90f);
            marker.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private void BuildItems()
        {
            MenuItem resume = AddItem("Resume", 0);
            MenuItem restart = AddItem("Restart", 1);
            MenuItem mainMenu = AddItem("Main Menu", 2);
            MenuItem quit = AddItem("Quit", 3);
            resume.Submitted += Resume;
            restart.Submitted += HandleRestart;
            mainMenu.Submitted += HandleMainMenu;
            quit.Submitted += HandleQuit;
            MenuKit.LinkVertical(items);
        }

        private MenuItem AddItem(string label, int index)
        {
            MenuItem item = kit.CreateItem(contentRoot, label, new Vector2(0f, 10f - 84f * index), 50f, 640f, 76f, false);
            item.Highlighted += HandleHighlighted;
            items.Add(item);
            return item;
        }

        private void BuildHint(RectTransform root)
        {
            TMP_Text hint = kit.CreateText("Hints", root, bodyFont, 20f, new Color(0.93f, 0.87f, 0.78f, 0.6f), hintText);
            hint.characterSpacing = 6f;
            hint.rectTransform.anchorMin = Vector2.zero;
            hint.rectTransform.anchorMax = Vector2.zero;
            hint.rectTransform.pivot = new Vector2(0f, 0.5f);
            hint.rectTransform.anchoredPosition = new Vector2(leftMargin + 4f, 46f);
            hint.rectTransform.sizeDelta = new Vector2(1200f, 40f);
        }
    }
}
