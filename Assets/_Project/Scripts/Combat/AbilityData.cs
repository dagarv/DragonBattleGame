using UnityEngine;

namespace DragonBattle.Combat
{
    [CreateAssetMenu(fileName = "AbilityData", menuName = "Dragon Battle/Ability Data")]
    public class AbilityData : ScriptableObject
    {
        [Header("Display")]
        [SerializeField] private string displayName = "Ability";
        [SerializeField] private Sprite icon;

        [Header("Stats")]
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float cooldown = 3f;
        [SerializeField, Min(0f)] private float range = 5f;
        [SerializeField, Min(0f)] private float radius = 2f;
        [SerializeField, Min(0f)] private float knockback = 5f;

        [Header("Effects")]
        [SerializeField] private GameObject castEffect;
        [SerializeField] private GameObject impactEffect;
        [SerializeField, Min(0.1f)] private float effectLifetime = 3f;

        [Header("Audio")]
        [SerializeField] private AudioClip castSound;
        [SerializeField] private AudioClip impactSound;

        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public float Damage => damage;
        public float Cooldown => cooldown;
        public float Range => range;
        public float Radius => radius;
        public float Knockback => knockback;
        public GameObject CastEffect => castEffect;
        public GameObject ImpactEffect => impactEffect;
        public float EffectLifetime => effectLifetime;
        public AudioClip CastSound => castSound;
        public AudioClip ImpactSound => impactSound;
    }
}
