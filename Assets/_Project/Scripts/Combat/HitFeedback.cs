using DragonBattle.Core;
using UnityEngine;

namespace DragonBattle.Combat
{
    public class HitFeedback : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Health health;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private CameraController cameraController;

        [Header("Flash")]
        [SerializeField, ColorUsage(false, true)] private Color flashColor = new Color(2.5f, 0.25f, 0.15f);
        [SerializeField, Min(0.01f)] private float flashDuration = 0.25f;

        [Header("Camera Shake")]
        [SerializeField, Min(0f)] private float shakePerDamage = 0.02f;
        [SerializeField, Min(0f)] private float maxShakePerHit = 0.45f;

        private MaterialPropertyBlock propertyBlock;
        private float flashEndTime;
        private bool isFlashing;

        private void Reset()
        {
            health = GetComponent<Health>();
            renderers = GetComponentsInChildren<Renderer>();
        }

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            if (cameraController == null && Camera.main != null)
            {
                cameraController = Camera.main.GetComponent<CameraController>();
            }
        }

        private void OnEnable()
        {
            health.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            health.OnDamaged -= HandleDamaged;
            ClearFlash();
        }

        private void Update()
        {
            if (!isFlashing)
            {
                return;
            }

            float remaining = flashEndTime - Time.time;
            if (remaining <= 0f)
            {
                ClearFlash();
                return;
            }

            ApplyEmission(flashColor * (remaining / flashDuration));
        }

        private void HandleDamaged(Health source, float amount)
        {
            flashEndTime = Time.time + flashDuration;
            isFlashing = true;
            ApplyEmission(flashColor);

            if (cameraController != null)
            {
                cameraController.AddShake(Mathf.Min(amount * shakePerDamage, maxShakePerHit));
            }
        }

        private void ApplyEmission(Color color)
        {
            foreach (Renderer target in renderers)
            {
                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(EmissionColorId, color);
                target.SetPropertyBlock(propertyBlock);
            }
        }

        private void ClearFlash()
        {
            isFlashing = false;
            foreach (Renderer target in renderers)
            {
                if (target != null)
                {
                    target.SetPropertyBlock(null);
                }
            }
        }
    }
}
