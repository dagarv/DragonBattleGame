using System.Collections;
using DragonBattle.Combat;
using DragonBattle.Core;
using UnityEngine;

namespace DragonBattle.Audio
{
    public class DragonAudio : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private AbilityCaster abilityCaster;
        [SerializeField] private DragonMotor motor;
        [SerializeField] private Rigidbody body;
        [SerializeField, Range(0.5f, 1.5f)] private float voicePitch = 1f;

        [Header("Voice")]
        [SerializeField] private SoundCue roar = new SoundCue();
        [SerializeField, Range(0f, 1f)] private float roarChance = 0.5f;
        [SerializeField] private SoundCue hurt = new SoundCue();
        [SerializeField, Min(0f)] private float hurtCooldown = 0.6f;
        [SerializeField] private SoundCue death = new SoundCue();

        [Header("Body")]
        [SerializeField] private SoundCue bodyImpact = new SoundCue();
        [SerializeField, Min(1f)] private float heavyHitDamage = 22f;
        [SerializeField, Range(0f, 1f)] private float lightHitVolume = 0.55f;
        [SerializeField] private SoundCue bodyFall = new SoundCue();
        [SerializeField, Min(0f)] private float bodyFallDelay = 1f;

        [Header("Music Ducking")]
        [SerializeField, Range(0f, 1f)] private float hitDuckDepth = 0.45f;
        [SerializeField, Min(0f)] private float hitDuckHold = 0.25f;
        [SerializeField, Min(0.01f)] private float hitDuckRelease = 0.8f;
        [SerializeField, Range(0f, 1f)] private float deathDuckDepth = 0.8f;
        [SerializeField, Min(0f)] private float deathDuckHold = 1.5f;
        [SerializeField, Min(0.01f)] private float deathDuckRelease = 1.5f;

        [Header("Footsteps")]
        [SerializeField] private SoundCue footstep = new SoundCue();
        [SerializeField, Min(0.05f)] private float stepInterval = 0.42f;
        [SerializeField, Range(0f, 1f)] private float minStepSpeed = 0.2f;

        private float nextHurtTime;
        private float stepProgress;

        private void Reset()
        {
            health = GetComponent<Health>();
            abilityCaster = GetComponent<AbilityCaster>();
            motor = GetComponent<DragonMotor>();
            body = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            health.OnDamaged += HandleDamaged;
            health.OnDied += HandleDied;
            abilityCaster.OnAbilityCast += HandleAbilityCast;
        }

        private void OnDisable()
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDied -= HandleDied;
            abilityCaster.OnAbilityCast -= HandleAbilityCast;
        }

        private void Update()
        {
            if (health.IsDead || !abilityCaster.DragonAnimator.IsInLocomotion)
            {
                stepProgress = 0f;
                return;
            }

            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;
            float speed = Mathf.Clamp01(velocity.magnitude / motor.MoveSpeed);
            if (speed < minStepSpeed)
            {
                stepProgress = 0.5f;
                return;
            }

            stepProgress += speed * Time.deltaTime / stepInterval;
            if (stepProgress >= 1f)
            {
                stepProgress -= 1f;
                footstep.Play(transform.position, voicePitch);
            }
        }

        private void HandleAbilityCast(int slot, Ability ability)
        {
            if (Random.value < roarChance)
            {
                roar.Play(transform.position, voicePitch);
            }
        }

        private void HandleDamaged(Health source, float amount)
        {
            float weight = Mathf.Clamp01(amount / heavyHitDamage);
            bodyImpact.Play(transform.position, Mathf.Lerp(1.1f, 0.9f, weight), Mathf.Lerp(lightHitVolume, 1f, weight));
            SoundPlayer.Duck(hitDuckDepth * weight, hitDuckHold, hitDuckRelease);

            if (source.IsDead || Time.time < nextHurtTime)
            {
                return;
            }

            nextHurtTime = Time.time + hurtCooldown;
            hurt.Play(transform.position, voicePitch);
        }

        private void HandleDied(Health source)
        {
            SoundPlayer.Duck(deathDuckDepth, deathDuckHold, deathDuckRelease);
            death.Play(transform.position, voicePitch);
            StartCoroutine(PlayBodyFall());
        }

        private IEnumerator PlayBodyFall()
        {
            yield return new WaitForSeconds(bodyFallDelay);
            bodyFall.Play(transform.position, voicePitch);
        }
    }
}
