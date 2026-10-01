using System;
using System.Collections;
using DragonBattle.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace DragonBattle.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FightBanner : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform root;
        [SerializeField] private Image swoosh;
        [SerializeField] private RectTransform label;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float startDelay = 0.3f;
        [SerializeField, Min(0.01f)] private float swooshDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float slamDuration = 0.2f;
        [SerializeField, Min(0f)] private float holdDuration = 0.9f;
        [SerializeField, Min(0.01f)] private float exitDuration = 0.35f;

        [Header("Motion")]
        [SerializeField] private AnimationCurve slamScale = new AnimationCurve(
            new Keyframe(0f, 2.4f), new Keyframe(0.75f, 0.92f), new Keyframe(1f, 1f));
        [SerializeField, Min(0f)] private float exitScale = 1.15f;
        [SerializeField, Min(0f)] private float shakeStrength = 18f;
        [SerializeField, Min(0.01f)] private float shakeDuration = 0.3f;

        [Header("Audio")]
        [SerializeField] private SoundCue slamSound = new SoundCue();

        private Vector2 rootOrigin;

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            root = transform as RectTransform;
        }

        private void Awake()
        {
            rootOrigin = root.anchoredPosition;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            Hide();
        }

        public void Play(Action onFight)
        {
            StopAllCoroutines();
            StartCoroutine(Animate(onFight));
        }

        private IEnumerator Animate(Action onFight)
        {
            Hide();
            yield return new WaitForSecondsRealtime(startDelay);

            canvasGroup.alpha = 1f;
            label.localScale = Vector3.zero;
            yield return Tween(swooshDuration, t => swoosh.fillAmount = 1f - (1f - t) * (1f - t));

            yield return Tween(slamDuration, t =>
            {
                float scale = slamScale.Evaluate(t);
                label.localScale = new Vector3(scale, scale, 1f);
            });

            slamSound.Play2D();
            onFight?.Invoke();

            yield return Tween(shakeDuration, t =>
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * (shakeStrength * (1f - t));
                root.anchoredPosition = rootOrigin + offset;
            });
            root.anchoredPosition = rootOrigin;

            yield return new WaitForSecondsRealtime(holdDuration);

            yield return Tween(exitDuration, t =>
            {
                canvasGroup.alpha = 1f - t;
                float scale = Mathf.Lerp(1f, exitScale, t);
                root.localScale = new Vector3(scale, scale, 1f);
            });

            Hide();
        }

        private static IEnumerator Tween(float duration, Action<float> step)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                step(elapsed / duration);
                yield return null;
            }
            step(1f);
        }

        private void Hide()
        {
            canvasGroup.alpha = 0f;
            swoosh.fillAmount = 0f;
            label.localScale = Vector3.zero;
            root.anchoredPosition = rootOrigin;
            root.localScale = Vector3.one;
        }
    }
}
