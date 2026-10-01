using DragonBattle.Combat;
using DragonBattle.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonBattle.UI
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private DragonIdentity identity;

        [Header("Elements")]
        [SerializeField] private Image fill;
        [SerializeField] private Image trail;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private RectTransform shakeTarget;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float trailDelay = 0.45f;
        [SerializeField, Min(0.01f)] private float trailSpeed = 0.6f;
        [SerializeField, Min(0f)] private float shakeStrength = 6f;
        [SerializeField, Min(0.01f)] private float shakeDuration = 0.2f;

        private float trailHoldUntil;
        private float shakeEndTime;
        private Vector2 shakeOrigin;

        private void OnEnable()
        {
            health.OnDamaged += HandleDamaged;
            if (shakeTarget != null)
            {
                shakeOrigin = shakeTarget.anchoredPosition;
            }
        }

        private void OnDisable()
        {
            health.OnDamaged -= HandleDamaged;
        }

        private void Start()
        {
            if (identity != null && nameLabel != null)
            {
                nameLabel.text = identity.DisplayName;
            }
            fill.fillAmount = health.Normalized;
            trail.fillAmount = health.Normalized;
            RefreshValue();
        }

        private void Update()
        {
            fill.fillAmount = health.Normalized;
            if (Time.time >= trailHoldUntil && trail.fillAmount > fill.fillAmount)
            {
                trail.fillAmount = Mathf.MoveTowards(trail.fillAmount, fill.fillAmount, trailSpeed * Time.deltaTime);
            }

            if (shakeTarget != null)
            {
                float remaining = shakeEndTime - Time.time;
                shakeTarget.anchoredPosition = remaining > 0f
                    ? shakeOrigin + Random.insideUnitCircle * (shakeStrength * remaining / shakeDuration)
                    : shakeOrigin;
            }
        }

        private void HandleDamaged(Health source, float amount)
        {
            trailHoldUntil = Time.time + trailDelay;
            shakeEndTime = Time.time + shakeDuration;
            RefreshValue();
        }

        private void RefreshValue()
        {
            if (valueLabel != null)
            {
                valueLabel.text = $"{Mathf.CeilToInt(health.CurrentHealth)} / {Mathf.CeilToInt(health.MaxHealth)}";
            }
        }
    }
}
