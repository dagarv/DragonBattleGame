using System;
using DragonBattle.Combat;
using DragonBattle.Core;
using UnityEngine;

namespace DragonBattle.UI
{
    public class DamagePopupSpawner : MonoBehaviour
    {
        [Serializable]
        private class PopupTarget
        {
            public Health health;
            public Color color = Color.white;
            public float heightOffset = 3f;
        }

        [SerializeField] private DamagePopup popupPrefab;
        [SerializeField] private PopupTarget[] targets;
        [SerializeField, Min(1f)] private float bigHitDamage = 20f;
        [SerializeField, Min(1f)] private float bigHitScale = 1.4f;

        private void Awake()
        {
            if (popupPrefab != null)
            {
                popupPrefab.PrewarmGlyphs();
                EffectWarmup.Request(popupPrefab.gameObject);
            }
        }

        private void OnEnable()
        {
            foreach (PopupTarget target in targets)
            {
                target.health.OnDamaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            foreach (PopupTarget target in targets)
            {
                target.health.OnDamaged -= HandleDamaged;
            }
        }

        private void HandleDamaged(Health source, float amount)
        {
            PopupTarget target = Array.Find(targets, entry => entry.health == source);
            if (target == null)
            {
                return;
            }

            Vector3 position = source.transform.position + Vector3.up * target.heightOffset;
            DamagePopup popup = Instantiate(popupPrefab, position, Quaternion.identity, transform);
            popup.Show(amount, target.color, amount >= bigHitDamage ? bigHitScale : 1f);
        }
    }
}
