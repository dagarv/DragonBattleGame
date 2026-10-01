using TMPro;
using UnityEngine;

namespace DragonBattle.UI
{
    public class DamagePopup : MonoBehaviour
    {
        private const string Digits = "0123456789";

        [SerializeField] private TMP_Text label;
        [SerializeField, Min(0.1f)] private float lifetime = 0.9f;
        [SerializeField, Min(0f)] private float riseDistance = 2f;
        [SerializeField, Min(0f)] private float horizontalScatter = 0.8f;
        [SerializeField] private AnimationCurve scaleOverLife = new AnimationCurve(
            new Keyframe(0f, 0.4f), new Keyframe(0.12f, 1.35f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.85f));
        [SerializeField] private AnimationCurve alphaOverLife = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.6f, 1f), new Keyframe(1f, 0f));

        private Vector3 startPosition;
        private Vector3 drift;
        private Vector3 baseScale;
        private Color baseColor;
        private float startTime;
        private Transform cameraTransform;

        private void Awake()
        {
            startPosition = transform.position;
            baseScale = transform.localScale;
            baseColor = label.color;
            startTime = Time.time;
        }

        public void PrewarmGlyphs()
        {
            if (label != null && label.font != null)
            {
                label.font.TryAddCharacters(Digits);
            }
        }

        public void Show(float amount, Color color, float sizeMultiplier)
        {
            label.text = Mathf.RoundToInt(amount).ToString();
            baseColor = color;
            label.color = color;
            baseScale = transform.localScale * sizeMultiplier;
            startPosition = transform.position;
            Vector2 scatter = Random.insideUnitCircle * horizontalScatter;
            drift = new Vector3(scatter.x, 0f, scatter.y);
            startTime = Time.time;
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
            Apply(0f);
        }

        private void LateUpdate()
        {
            float progress = (Time.time - startTime) / lifetime;
            if (progress >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            Apply(progress);
        }

        private void Apply(float progress)
        {
            float rise = 1f - (1f - progress) * (1f - progress);
            transform.position = startPosition + drift * rise + Vector3.up * (riseDistance * rise);
            transform.localScale = baseScale * scaleOverLife.Evaluate(progress);

            if (cameraTransform != null)
            {
                transform.rotation = cameraTransform.rotation;
            }

            Color color = baseColor;
            color.a *= alphaOverLife.Evaluate(progress);
            label.color = color;
        }
    }
}
