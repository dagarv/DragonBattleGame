using System;
using DragonBattle.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragonBattle.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class WinnerScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private Button restartButton;

        [Header("Text")]
        [SerializeField] private string titleFormat = "{0} Wins!";
        [SerializeField] private string victoryText = "Victory";
        [SerializeField] private string defeatText = "Defeat";

        [Header("Audio")]
        [SerializeField] private SoundCue clickSound = new SoundCue();

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.5f;
        [SerializeField] private AnimationCurve panelScale = new AnimationCurve(
            new Keyframe(0f, 0.7f), new Keyframe(0.7f, 1.05f), new Keyframe(1f, 1f));

        private Action onRestart;
        private float shownTime;
        private bool isShown;

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            SetVisible(false);
            restartButton.onClick.AddListener(HandleRestartClicked);
        }

        private void OnDestroy()
        {
            restartButton.onClick.RemoveListener(HandleRestartClicked);
        }

        public void Show(string winnerName, Color winnerColor, bool playerWon, Action restartAction)
        {
            onRestart = restartAction;
            titleLabel.text = string.Format(titleFormat, winnerName);
            titleLabel.color = winnerColor;
            subtitleLabel.text = playerWon ? victoryText : defeatText;
            shownTime = Time.unscaledTime;
            isShown = true;
            SetVisible(true);
            canvasGroup.alpha = 0f;

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
            }
        }

        private void Update()
        {
            if (!isShown)
            {
                return;
            }

            float progress = Mathf.Clamp01((Time.unscaledTime - shownTime) / fadeDuration);
            canvasGroup.alpha = progress;
            if (panel != null)
            {
                float scale = panelScale.Evaluate(progress);
                panel.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void HandleRestartClicked()
        {
            clickSound.Play2D();
            onRestart?.Invoke();
        }
    }
}
