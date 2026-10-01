using DragonBattle.Audio;
using DragonBattle.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonBattle.UI
{
    public class AbilitySlotUI : MonoBehaviour
    {
        [SerializeField] private AbilityCaster caster;
        [SerializeField, Min(0)] private int slot;
        [SerializeField] private string keyText = "1";

        [Header("Elements")]
        [SerializeField] private Image icon;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private Image frame;
        [SerializeField] private TMP_Text cooldownLabel;
        [SerializeField] private TMP_Text keyLabel;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private RectTransform pulseTarget;

        [Header("Colors")]
        [SerializeField] private Color readyFrameColor = new Color(1f, 0.78f, 0.3f);
        [SerializeField] private Color coolingFrameColor = new Color(0.35f, 0.35f, 0.4f);
        [SerializeField] private Color readyIconColor = Color.white;
        [SerializeField] private Color unavailableIconColor = new Color(0.45f, 0.45f, 0.5f);

        [Header("Pulse")]
        [SerializeField, Min(1f)] private float pulseScale = 1.15f;
        [SerializeField, Min(0.01f)] private float pulseDuration = 0.25f;

        [Header("Audio")]
        [SerializeField] private SoundCue readySound = new SoundCue();

        private Ability ability;
        private bool wasReady = true;
        private int shownTenths = -1;
        private float pulseStartTime = float.NegativeInfinity;

        private void OnEnable()
        {
            caster.OnAbilityCast += HandleAbilityCast;
        }

        private void OnDisable()
        {
            caster.OnAbilityCast -= HandleAbilityCast;
        }

        private void Start()
        {
            ability = caster.GetAbility(slot);
            if (ability == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (ability.Data.Icon != null)
            {
                icon.sprite = ability.Data.Icon;
            }
            if (keyLabel != null)
            {
                keyLabel.text = keyText;
            }
            if (nameLabel != null)
            {
                nameLabel.text = ability.Data.DisplayName;
            }
        }

        private void Update()
        {
            if (ability == null)
            {
                return;
            }

            float remaining = ability.CooldownRemaining;
            bool isReady = ability.IsReady;

            cooldownOverlay.fillAmount = ability.CooldownNormalized;
            int tenths = isReady ? 0 : ToDisplayTenths(remaining);
            if (tenths != shownTenths)
            {
                shownTenths = tenths;
                cooldownLabel.text = FormatTenths(tenths);
            }
            icon.color = caster.CanCast(slot) ? readyIconColor : unavailableIconColor;
            if (frame != null)
            {
                frame.color = isReady ? readyFrameColor : coolingFrameColor;
            }

            if (isReady && !wasReady)
            {
                pulseStartTime = Time.time;
                readySound.Play2D();
            }
            wasReady = isReady;

            UpdatePulse();
        }

        private void HandleAbilityCast(int castSlot, Ability castAbility)
        {
            if (castSlot == slot)
            {
                pulseStartTime = Time.time;
            }
        }

        private void UpdatePulse()
        {
            if (pulseTarget == null)
            {
                return;
            }

            float progress = (Time.time - pulseStartTime) / pulseDuration;
            float scale = progress >= 0f && progress < 1f ? Mathf.Lerp(pulseScale, 1f, progress) : 1f;
            pulseTarget.localScale = new Vector3(scale, scale, 1f);
        }

        private static int ToDisplayTenths(float seconds)
        {
            if (seconds <= 0f)
            {
                return 0;
            }
            return seconds >= 1f ? Mathf.CeilToInt(seconds) * 10 : Mathf.CeilToInt(seconds * 10f);
        }

        private static string FormatTenths(int tenths)
        {
            if (tenths <= 0)
            {
                return string.Empty;
            }
            return tenths >= 10 ? (tenths / 10).ToString() : (tenths / 10f).ToString("0.0");
        }
    }
}
