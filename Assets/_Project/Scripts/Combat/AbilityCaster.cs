using System;
using System.Collections;
using DragonBattle.Audio;
using DragonBattle.Core;
using UnityEngine;

namespace DragonBattle.Combat
{
    public class AbilityCaster : MonoBehaviour
    {
        [SerializeField] private DragonAnimator dragonAnimator;
        [SerializeField] private DragonMotor motor;
        [SerializeField] private Health health;

        [Header("Audio")]
        [SerializeField, Range(0.5f, 1.5f)] private float pitchScale = 1f;
        [SerializeField, Range(0f, 0.3f)] private float pitchVariance = 0.06f;

        [Header("Combat")]
        [SerializeField] private Ability[] abilities = new Ability[3];
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private Transform mouth;

        public event Action<int, Ability> OnAbilityCast;

        public bool IsCasting { get; private set; }
        public bool IsLocked => IsCasting || dragonAnimator.IsBusy || health.IsDead;
        public int AbilityCount => abilities.Length;
        public DragonAnimator DragonAnimator => dragonAnimator;
        public DragonMotor Motor => motor;
        public Health Health => health;
        public LayerMask TargetMask => targetMask;
        public Vector3 MouthPosition => mouth != null ? mouth.position : transform.position + transform.forward + Vector3.up;

        private void Reset()
        {
            dragonAnimator = GetComponent<DragonAnimator>();
            motor = GetComponent<DragonMotor>();
            health = GetComponent<Health>();
            abilities = GetComponents<Ability>();
        }

        private void Awake()
        {
            foreach (Ability ability in abilities)
            {
                if (ability != null)
                {
                    ability.Initialize(this);
                    ability.WarmUpEffects();
                }
            }
        }

        public Ability GetAbility(int slot)
        {
            return slot >= 0 && slot < abilities.Length ? abilities[slot] : null;
        }

        public bool CanCast(int slot)
        {
            Ability ability = GetAbility(slot);
            return ability != null && ability.IsReady && !IsLocked;
        }

        public bool TryCast(int slot)
        {
            if (!CanCast(slot))
            {
                return false;
            }

            StartCoroutine(Cast(slot, abilities[slot]));
            return true;
        }

        public void PlaySound(AudioClip clip, float volume = 1f)
        {
            PlaySoundAt(clip, transform.position, volume);
        }

        public void PlaySoundAt(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip != null)
            {
                SoundPlayer.PlayOneShot(clip, position, volume, RandomPitch());
            }
        }

        public float RandomPitch()
        {
            return pitchScale * (1f + UnityEngine.Random.Range(-pitchVariance, pitchVariance));
        }

        private IEnumerator Cast(int slot, Ability ability)
        {
            IsCasting = true;
            OnAbilityCast?.Invoke(slot, ability);
            yield return ability.Cast();
            IsCasting = false;
        }
    }
}
