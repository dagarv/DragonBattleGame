using System;
using System.Collections;
using System.Collections.Generic;
using DragonBattle.Audio;
using DragonBattle.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragonBattle.UI
{
    [RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
    public class MainMenu : MonoBehaviour
    {
        private const float VolumeStep = 0.1f;

        private static bool skipNextOpen;

        [Header("Scene")]
        [SerializeField] private MenuCamera menuCamera;
        [SerializeField] private CanvasGroup hud;
        [SerializeField] private Behaviour[] hideDuringMenu = Array.Empty<Behaviour>();
        [SerializeField] private DragonAnimator hero;

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
        [SerializeField] private Color scrimColor = new Color(0.02f, 0.012f, 0.01f, 0.9f);

        [Header("Text")]
        [SerializeField] private string kickerText = "A Duel of Fire and Fury";
        [SerializeField] private string titleText = "Dragon\nBattle";
        [SerializeField] private string matchupText = "SoulEater   vs   TerrorBringer";
        [SerializeField] private string hintText = "<color=#FFB45A>Enter</color>  Select      <color=#FFB45A>Esc</color>  Back      <color=#FFB45A>Mouse</color>  Point";

        [Header("Layout")]
        [SerializeField, Min(0f)] private float letterboxHeight = 92f;
        [SerializeField, Min(0f)] private float leftMargin = 150f;

        [Header("Intro Timing")]
        [SerializeField, Min(0.01f)] private float fadeInDuration = 2f;
        [SerializeField, Min(0f)] private float titleDelay = 0.6f;
        [SerializeField, Min(0.01f)] private float titleDuration = 1.5f;
        [SerializeField, Min(0f)] private float itemsDelay = 1.6f;
        [SerializeField, Min(0f)] private float itemStagger = 0.1f;
        [SerializeField, Min(0.01f)] private float itemDuration = 0.6f;

        [Header("Play Timing")]
        [SerializeField, Min(0.01f)] private float exitDuration = 0.7f;
        [SerializeField, Min(0f)] private float roarSoundDelay = 0.35f;
        [SerializeField, Min(0.01f)] private float roarDuration = 1.6f;
        [SerializeField, Min(0f)] private float flightDelay = 1.3f;
        [SerializeField, Min(0.01f)] private float hudFadeDuration = 0.6f;
        [SerializeField, Min(0.01f)] private float quitFadeDuration = 0.8f;

        [Header("Audio")]
        [SerializeField] private SoundCue hoverSound = new SoundCue();
        [SerializeField] private SoundCue confirmSound = new SoundCue();
        [SerializeField] private SoundCue backSound = new SoundCue();
        [SerializeField] private SoundCue adjustSound = new SoundCue();
        [SerializeField] private SoundCue playSound = new SoundCue();
        [SerializeField] private SoundCue roarSound = new SoundCue();
        [SerializeField] private SoundCue whooshSound = new SoundCue();

        private enum Page
        {
            Main,
            Options
        }

        private class OptionRow
        {
            public MenuItem Item;
            public TMP_Text Value;
            public RectTransform Fill;
            public Func<float> ReadFill;
            public Func<string> ReadText;
            public Action<int> Change;
        }

        private readonly List<MenuItem> mainItems = new List<MenuItem>();
        private readonly List<MenuItem> optionItems = new List<MenuItem>();
        private readonly List<OptionRow> optionRows = new List<OptionRow>();

        private CanvasGroup rootGroup;
        private CanvasGroup contentGroup;
        private CanvasGroup mainPage;
        private CanvasGroup optionsPage;
        private CanvasGroup kicker;
        private CanvasGroup titleGroup;
        private CanvasGroup matchupGroup;
        private CanvasGroup hintGroup;
        private TMP_Text title;
        private TMP_Text kickerLabel;
        private RectTransform contentRoot;
        private RectTransform divider;
        private RectTransform dividerMarker;
        private Image scrim;
        private RectTransform letterboxTop;
        private RectTransform letterboxBottom;
        private Image fader;
        private MenuKit kit;
        private Sprite scrimSprite;

        private Page page = Page.Main;
        private Action onPlay;
        private bool isOpen;
        private bool isBusy;
        private bool skipThisLoad;
        private bool suppressHoverSound;
        private GameObject lastSelected;

        public static void SkipNextOpen()
        {
            skipNextOpen = true;
        }

        private void Awake()
        {
            rootGroup = GetComponent<CanvasGroup>();
            skipThisLoad = skipNextOpen;
            skipNextOpen = false;

            if (skipThisLoad)
            {
                gameObject.SetActive(false);
                return;
            }

            Build();
            SetHudVisible(0f);
            SetBehavioursEnabled(false);
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = true;
        }

        private void OnDestroy()
        {
            kit?.Dispose();
            MenuKit.DestroySprite(scrimSprite);
        }

        public bool Open(Action playAction)
        {
            if (skipThisLoad)
            {
                return false;
            }

            onPlay = playAction;
            isOpen = true;
            menuCamera.BeginMenu();
            StartCoroutine(Intro());
            return true;
        }

        private void Update()
        {
            if (!isOpen || isBusy || !rootGroup.interactable)
            {
                return;
            }

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

        private IEnumerator Intro()
        {
            isBusy = true;
            fader.color = Color.black;
            foreach (MenuItem item in mainItems)
            {
                item.SetReveal(0f);
            }
            kicker.alpha = 0f;
            titleGroup.alpha = 0f;
            matchupGroup.alpha = 0f;
            hintGroup.alpha = 0f;
            divider.localScale = new Vector3(0f, 1f, 1f);
            dividerMarker.localScale = Vector3.zero;

            float start = Time.unscaledTime;
            float end = itemsDelay + itemStagger * mainItems.Count + itemDuration;
            bool selected = false;
            while (true)
            {
                float t = Time.unscaledTime - start;

                fader.color = new Color(0f, 0f, 0f, 1f - Ease.InOutCubic(t / fadeInDuration));

                float kickerT = Mathf.Clamp01((t - titleDelay * 0.6f) / 1f);
                kicker.alpha = Ease.OutCubic(kickerT);
                kickerLabel.characterSpacing = Mathf.Lerp(60f, 28f, Ease.OutCubic(kickerT));

                float titleT = Mathf.Clamp01((t - titleDelay) / titleDuration);
                titleGroup.alpha = Ease.OutCubic(titleT);
                title.characterSpacing = Mathf.Lerp(24f, 0f, Ease.OutCubic(titleT));
                float titleScale = Mathf.Lerp(1.08f, 1f, Ease.OutCubic(titleT));
                title.rectTransform.localScale = new Vector3(titleScale, titleScale, 1f);

                float dividerT = Mathf.Clamp01((t - titleDelay - titleDuration * 0.5f) / 0.8f);
                divider.localScale = new Vector3(Ease.OutCubic(dividerT), 1f, 1f);
                dividerMarker.localScale = Vector3.one * Ease.OutBack(Mathf.Clamp01(dividerT * 1.6f));
                matchupGroup.alpha = Ease.OutCubic(Mathf.Clamp01((t - titleDelay - titleDuration * 0.7f) / 0.8f));

                for (int i = 0; i < mainItems.Count; i++)
                {
                    float itemT = (t - itemsDelay - itemStagger * i) / itemDuration;
                    mainItems[i].SetReveal(itemT);
                }
                hintGroup.alpha = Ease.OutCubic(Mathf.Clamp01((t - end + 0.3f) / 0.6f));

                if (!selected && t >= itemsDelay)
                {
                    selected = true;
                    rootGroup.interactable = true;
                    Select(mainItems[0], false);
                }

                if (t >= Mathf.Max(end, fadeInDuration))
                {
                    break;
                }
                yield return null;
            }

            fader.color = Color.clear;
            hintGroup.alpha = 1f;
            isBusy = false;
        }

        private void HandlePlay()
        {
            if (isBusy)
            {
                return;
            }
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            isBusy = true;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
            confirmSound.Play2D();
            playSound.Play2D();

            if (hero != null)
            {
                hero.CrossFade(DragonAnimator.ScreamStateHash, 0.2f);
            }
            menuCamera.Roar(roarDuration);

            float start = Time.unscaledTime;
            bool roared = false;
            bool flying = false;
            bool arrived = false;
            Vector2 contentOrigin = contentRoot.anchoredPosition;
            float scrimAlpha = scrim.color.a;

            while (!arrived)
            {
                float t = Time.unscaledTime - start;

                if (!roared && t >= roarSoundDelay)
                {
                    roared = true;
                    roarSound.Play(hero != null ? hero.transform.position : menuCamera.transform.position);
                }

                float exit = Ease.InOutCubic(t / exitDuration);
                contentGroup.alpha = 1f - exit;
                hintGroup.alpha = 1f - exit;
                contentRoot.anchoredPosition = contentOrigin + new Vector2(-80f * exit, 0f);
                MenuKit.SetAlpha(scrim, scrimAlpha * (1f - Ease.InOutCubic(t / (exitDuration * 1.6f))));

                if (!flying && t >= flightDelay)
                {
                    flying = true;
                    whooshSound.Play2D();
                    menuCamera.Fly(() => arrived = true);
                }

                float bars = flying ? Ease.InOutCubic((t - flightDelay) / 1.2f) : 0f;
                SetLetterbox(1f - bars);
                yield return null;
            }

            SetLetterbox(0f);
            SetBehavioursEnabled(true);
            EventSystem system = EventSystem.current;
            if (system != null)
            {
                system.SetSelectedGameObject(null);
            }

            isOpen = false;
            Action play = onPlay;
            onPlay = null;
            play?.Invoke();

            float fadeStart = Time.unscaledTime;
            while (Time.unscaledTime - fadeStart < hudFadeDuration)
            {
                SetHudVisible(Ease.OutCubic((Time.unscaledTime - fadeStart) / hudFadeDuration));
                yield return null;
            }
            SetHudVisible(1f);
            gameObject.SetActive(false);
        }

        private void HandleQuit()
        {
            if (isBusy)
            {
                return;
            }
            StartCoroutine(QuitSequence());
        }

        private IEnumerator QuitSequence()
        {
            isBusy = true;
            rootGroup.interactable = false;
            backSound.Play2D();
            GameSettings.Save();

            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < quitFadeDuration)
            {
                float t = (Time.unscaledTime - start) / quitFadeDuration;
                fader.color = new Color(0f, 0f, 0f, Ease.InOutCubic(t));
                AudioListener.volume = GameSettings.MasterVolume * (1f - t);
                yield return null;
            }
            fader.color = Color.black;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowPage(Page target)
        {
            if (isBusy || page == target)
            {
                return;
            }
            StartCoroutine(SwitchPage(target));
        }

        private IEnumerator SwitchPage(Page target)
        {
            isBusy = true;
            rootGroup.interactable = false;
            CanvasGroup from = target == Page.Options ? mainPage : optionsPage;
            CanvasGroup to = target == Page.Options ? optionsPage : mainPage;
            List<MenuItem> fromItems = target == Page.Options ? mainItems : optionItems;
            List<MenuItem> toItems = target == Page.Options ? optionItems : mainItems;
            const float outDuration = 0.25f;

            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < outDuration)
            {
                float t = (Time.unscaledTime - start) / outDuration;
                for (int i = 0; i < fromItems.Count; i++)
                {
                    fromItems[i].SetReveal(1f - Ease.InCubic(t));
                }
                from.alpha = 1f - Ease.InCubic(t);
                yield return null;
            }
            SetPageVisible(from, false);
            SetPageVisible(to, true);
            page = target;
            RefreshOptions();

            float duration = itemStagger * 0.6f * toItems.Count + itemDuration * 0.7f;
            start = Time.unscaledTime;
            bool selected = false;
            while (true)
            {
                float t = Time.unscaledTime - start;
                to.alpha = Ease.OutCubic(t / 0.3f);
                for (int i = 0; i < toItems.Count; i++)
                {
                    toItems[i].SetReveal((t - itemStagger * 0.6f * i) / (itemDuration * 0.7f));
                }
                if (!selected)
                {
                    selected = true;
                    rootGroup.interactable = true;
                    Select(target == Page.Options ? optionItems[0] : mainItems[1], false);
                }
                if (t >= duration)
                {
                    break;
                }
                yield return null;
            }
            isBusy = false;
        }

        private void Select(MenuItem item, bool withSound)
        {
            EventSystem system = EventSystem.current;
            if (system == null)
            {
                return;
            }
            suppressHoverSound = !withSound;
            system.SetSelectedGameObject(null);
            system.SetSelectedGameObject(item.gameObject);
            suppressHoverSound = false;
            lastSelected = item.gameObject;
        }

        private void HandleHighlighted()
        {
            if (!suppressHoverSound && rootGroup.interactable)
            {
                hoverSound.Play2D();
            }
        }

        private void HandleCancel()
        {
            if (page == Page.Options && !isBusy)
            {
                backSound.Play2D();
                GameSettings.Save();
                ShowPage(Page.Main);
            }
        }

        private void RefreshOptions()
        {
            foreach (OptionRow row in optionRows)
            {
                if (row.Value != null)
                {
                    row.Value.text = row.ReadText();
                }
                if (row.Fill != null)
                {
                    row.Fill.anchorMax = new Vector2(row.ReadFill(), 1f);
                }
            }
        }

        private void SetHudVisible(float alpha)
        {
            if (hud != null)
            {
                hud.alpha = alpha;
            }
        }

        private void SetBehavioursEnabled(bool isEnabled)
        {
            foreach (Behaviour behaviour in hideDuringMenu)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = isEnabled;
                }
            }
        }

        private void SetLetterbox(float amount)
        {
            float height = letterboxHeight * Ease.InOutCubic(amount);
            letterboxTop.sizeDelta = new Vector2(0f, height);
            letterboxBottom.sizeDelta = new Vector2(0f, height);
        }

        private static void SetPageVisible(CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            group.gameObject.SetActive(visible);
        }

        private void Build()
        {
            kit = new MenuKit(gameObject.layer, itemFont, accent, textNormal, textHighlight);
            scrimSprite = CreateScrimSprite();

            RectTransform root = (RectTransform)transform;

            scrim = kit.CreateImage("Scrim", root, scrimColor, scrimSprite);
            MenuKit.Stretch(scrim.rectTransform);

            contentRoot = kit.CreateRect("Content", root);
            contentRoot.anchorMin = new Vector2(0f, 0.5f);
            contentRoot.anchorMax = new Vector2(0f, 0.5f);
            contentRoot.pivot = new Vector2(0f, 0.5f);
            contentRoot.sizeDelta = new Vector2(900f, 900f);
            contentRoot.anchoredPosition = new Vector2(leftMargin, 20f);
            contentGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();

            BuildHeader();
            BuildMainPage();
            BuildOptionsPage();
            BuildFrame(root);

            SetPageVisible(mainPage, true);
            SetPageVisible(optionsPage, false);
            SetLetterbox(1f);
            RefreshOptions();
        }

        private void BuildHeader()
        {
            kickerLabel = kit.CreateText("Kicker", contentRoot, bodyFont, 24f, accent, kickerText.ToUpperInvariant());
            kickerLabel.characterSpacing = 28f;
            MenuKit.Place(kickerLabel.rectTransform, new Vector2(4f, 330f), new Vector2(900f, 40f));
            kicker = kickerLabel.gameObject.AddComponent<CanvasGroup>();

            title = kit.CreateText("Title", contentRoot, titleFont, 128f, Color.white, titleText);
            title.lineSpacing = -22f;
            title.enableVertexGradient = true;
            title.colorGradient = new VertexGradient(titleTop, titleTop, titleBottom, titleBottom);
            MenuKit.Place(title.rectTransform, new Vector2(0f, 190f), new Vector2(1000f, 280f));
            Material titleMaterial = title.fontMaterial;
            titleMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            titleMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.85f));
            titleMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.15f);
            titleMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.35f);
            titleMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.6f);
            titleMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.15f);
            titleMaterial.EnableKeyword(ShaderUtilities.Keyword_Glow);
            titleMaterial.SetColor(ShaderUtilities.ID_GlowColor, new Color(1f, 0.38f, 0.08f, 0.45f));
            titleMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, 0.45f);
            titleMaterial.SetFloat(ShaderUtilities.ID_GlowPower, 0.6f);
            titleGroup = title.gameObject.AddComponent<CanvasGroup>();
            title.gameObject.AddComponent<TextShimmer>();

            divider = kit.CreateImage("Divider", contentRoot, accent, kit.FadeSprite).rectTransform;
            MenuKit.Place(divider, new Vector2(30f, 40f), new Vector2(520f, 2f));

            dividerMarker = kit.CreateImage("DividerMarker", contentRoot, accent).rectTransform;
            MenuKit.Place(dividerMarker, new Vector2(10f, 40f), new Vector2(12f, 12f));
            dividerMarker.pivot = new Vector2(0.5f, 0.5f);
            dividerMarker.anchoredPosition = new Vector2(10f, 40f);
            dividerMarker.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private void BuildMainPage()
        {
            mainPage = CreatePage("MainPage");

            TMP_Text matchup = kit.CreateText("Matchup", mainPage.transform, bodyFont, 26f, textNormal, matchupText.ToUpperInvariant());
            matchup.characterSpacing = 12f;
            MenuKit.Place(matchup.rectTransform, new Vector2(4f, -6f), new Vector2(900f, 40f));
            matchupGroup = matchup.gameObject.AddComponent<CanvasGroup>();

            MenuItem play = CreateItem(mainPage.transform, "Play", new Vector2(0f, -120f), 56f, 640f, 84f, false);
            MenuItem options = CreateItem(mainPage.transform, "Options", new Vector2(0f, -210f), 56f, 640f, 84f, false);
            MenuItem quit = CreateItem(mainPage.transform, "Quit", new Vector2(0f, -300f), 56f, 640f, 84f, false);
            play.Submitted += HandlePlay;
            options.Submitted += () =>
            {
                confirmSound.Play2D();
                ShowPage(Page.Options);
            };
            quit.Submitted += HandleQuit;
            mainItems.Add(play);
            mainItems.Add(options);
            mainItems.Add(quit);
            MenuKit.LinkVertical(mainItems);
        }

        private void BuildOptionsPage()
        {
            optionsPage = CreatePage("OptionsPage");

            TMP_Text heading = kit.CreateText("Heading", optionsPage.transform, bodyFont, 26f, accent, "OPTIONS");
            heading.characterSpacing = 28f;
            MenuKit.Place(heading.rectTransform, new Vector2(4f, -6f), new Vector2(900f, 40f));

            float y = -90f;
            const float spacing = 64f;
            AddSliderRow("Master Volume", y, () => GameSettings.MasterVolume, GameSettings.SetMasterVolume);
            AddSliderRow("Music", y -= spacing, () => GameSettings.MusicVolume, GameSettings.SetMusicVolume);
            AddSliderRow("Effects", y -= spacing, () => GameSettings.EffectsVolume, GameSettings.SetEffectsVolume);
            AddToggleRow("Screen Shake", y -= spacing, () => GameSettings.ScreenShake, GameSettings.SetScreenShake);
            AddToggleRow("Fullscreen", y -= spacing, () => GameSettings.Fullscreen, GameSettings.SetFullscreen);

            MenuItem back = CreateItem(optionsPage.transform, "Back", new Vector2(0f, y - spacing - 20f), 40f, 760f, 64f, false);
            back.Submitted += HandleCancel;
            optionItems.Add(back);

            foreach (MenuItem item in optionItems)
            {
                item.Cancelled += HandleCancel;
            }
            MenuKit.LinkVertical(optionItems);
        }

        private void AddSliderRow(string label, float y, Func<float> read, Action<float> write)
        {
            MenuItem item = CreateItem(optionsPage.transform, label, new Vector2(0f, y), 34f, 760f, 58f, true);
            RectTransform valueArea = item.transform.Find("Content/Value") as RectTransform;

            Image track = kit.CreateImage("Track", valueArea, new Color(1f, 1f, 1f, 0.14f));
            track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            track.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            track.rectTransform.pivot = new Vector2(0f, 0.5f);
            track.rectTransform.anchoredPosition = new Vector2(40f, 0f);
            track.rectTransform.sizeDelta = new Vector2(170f, 4f);

            Image fill = kit.CreateImage("Fill", track.rectTransform, accent);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(1f, 1f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            TMP_Text value = kit.CreateText("Amount", valueArea, bodyFont, 24f, textHighlight, string.Empty);
            value.alignment = TextAlignmentOptions.MidlineRight;
            MenuKit.Place(value.rectTransform, new Vector2(220f, 0f), new Vector2(80f, 40f));
            value.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            value.rectTransform.anchorMax = new Vector2(0f, 0.5f);

            AddArrows(valueArea);

            OptionRow row = new OptionRow
            {
                Item = item,
                Value = value,
                Fill = fill.rectTransform,
                ReadFill = read,
                ReadText = () => Mathf.RoundToInt(read() * 100f) + "%"
            };
            row.Change = direction =>
            {
                float next = Mathf.Clamp01(Mathf.Round((read() + direction * VolumeStep) * 10f) / 10f);
                if (Mathf.Approximately(next, read()))
                {
                    return;
                }
                write(next);
                adjustSound.Play2D(0.9f + next * 0.3f);
                RefreshOptions();
            };
            item.Adjusted += row.Change;
            optionRows.Add(row);
            optionItems.Add(item);
        }

        private void AddToggleRow(string label, float y, Func<bool> read, Action<bool> write)
        {
            MenuItem item = CreateItem(optionsPage.transform, label, new Vector2(0f, y), 34f, 760f, 58f, true);
            RectTransform valueArea = item.transform.Find("Content/Value") as RectTransform;

            TMP_Text value = kit.CreateText("State", valueArea, bodyFont, 26f, textHighlight, string.Empty);
            value.alignment = TextAlignmentOptions.Center;
            value.characterSpacing = 10f;
            MenuKit.Stretch(value.rectTransform);

            AddArrows(valueArea);

            OptionRow row = new OptionRow
            {
                Item = item,
                Value = value,
                ReadFill = () => 0f,
                ReadText = () => read() ? "ON" : "OFF"
            };
            row.Change = direction =>
            {
                write(!read());
                adjustSound.Play2D(read() ? 1.15f : 0.9f);
                RefreshOptions();
            };
            item.Adjusted += row.Change;
            optionRows.Add(row);
            optionItems.Add(item);
        }

        private void AddArrows(RectTransform valueArea)
        {
            TMP_Text left = kit.CreateText("Left", valueArea, bodyFont, 30f, accent, "<");
            left.alignment = TextAlignmentOptions.Center;
            MenuKit.Place(left.rectTransform, new Vector2(0f, 0f), new Vector2(30f, 40f));
            TMP_Text right = kit.CreateText("Right", valueArea, bodyFont, 30f, accent, ">");
            right.alignment = TextAlignmentOptions.Center;
            right.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            right.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            right.rectTransform.pivot = new Vector2(1f, 0.5f);
            right.rectTransform.anchoredPosition = Vector2.zero;
            right.rectTransform.sizeDelta = new Vector2(30f, 40f);
        }

        private void BuildFrame(RectTransform root)
        {
            letterboxTop = kit.CreateImage("LetterboxTop", root, Color.black).rectTransform;
            letterboxTop.anchorMin = new Vector2(0f, 1f);
            letterboxTop.anchorMax = new Vector2(1f, 1f);
            letterboxTop.pivot = new Vector2(0.5f, 1f);

            letterboxBottom = kit.CreateImage("LetterboxBottom", root, Color.black).rectTransform;
            letterboxBottom.anchorMin = new Vector2(0f, 0f);
            letterboxBottom.anchorMax = new Vector2(1f, 0f);
            letterboxBottom.pivot = new Vector2(0.5f, 0f);

            TMP_Text hint = kit.CreateText("Hints", root, bodyFont, 20f, new Color(0.93f, 0.87f, 0.78f, 0.6f), hintText);
            hint.characterSpacing = 6f;
            hint.rectTransform.anchorMin = Vector2.zero;
            hint.rectTransform.anchorMax = Vector2.zero;
            hint.rectTransform.pivot = new Vector2(0f, 0.5f);
            hint.rectTransform.anchoredPosition = new Vector2(leftMargin + 4f, letterboxHeight * 0.5f);
            hint.rectTransform.sizeDelta = new Vector2(1200f, 40f);
            hintGroup = hint.gameObject.AddComponent<CanvasGroup>();

            fader = kit.CreateImage("Fader", root, Color.black);
            MenuKit.Stretch(fader.rectTransform);
        }

        private CanvasGroup CreatePage(string name)
        {
            RectTransform pageRect = kit.CreateRect(name, contentRoot);
            MenuKit.Stretch(pageRect);
            return pageRect.gameObject.AddComponent<CanvasGroup>();
        }

        private MenuItem CreateItem(Transform parent, string text, Vector2 position, float fontSize, float width, float height, bool hasValue)
        {
            MenuItem item = kit.CreateItem(parent, text, position, fontSize, width, height, hasValue);
            item.Highlighted += HandleHighlighted;
            return item;
        }

        private static Sprite CreateScrimSprite()
        {
            const int width = 256;
            const int height = 64;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = (float)y / (height - 1);
                float edge = Mathf.Max(Mathf.SmoothStep(0.25f, 0f, v), Mathf.SmoothStep(0.8f, 1f, v)) * 0.5f;
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width - 1);
                    float side = 1f - Mathf.SmoothStep(0f, 0.62f, u);
                    float alpha = Mathf.Clamp01(side + edge * (1f - side));
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
        }
    }
}
