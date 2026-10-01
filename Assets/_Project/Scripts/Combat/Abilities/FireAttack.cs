using System.Collections;
using DragonBattle.Audio;
using DragonBattle.Core;
using UnityEngine;

namespace DragonBattle.Combat.Abilities
{
    public class FireAttack : Ability
    {
        [Header("Timing (normalized animation time)")]
        [SerializeField, Range(0f, 1f)] private float breathStart = 0.2f;
        [SerializeField, Range(0f, 1f)] private float impactTime = 0.45f;
        [SerializeField, Range(0f, 1f)] private float breathEnd = 0.85f;

        [Header("Breath")]
        [SerializeField, Range(0f, 180f)] private float coneAngle = 70f;
        [SerializeField, Range(0f, 45f)] private float flamePitch = 12f;
        [SerializeField, Min(0f)] private float flameLinger = 1f;

        [Header("Breath Audio")]
        [SerializeField] private AudioClip breathLoop;
        [SerializeField, Range(0f, 1f)] private float breathVolume = 0.9f;
        [SerializeField, Min(0f)] private float breathFadeIn = 0.1f;
        [SerializeField, Min(0f)] private float breathFadeOut = 0.4f;

        protected override IEnumerator Perform()
        {
            Caster.DragonAnimator.Trigger(DragonAnimator.FireHash);
            yield return WaitForStateTime(DragonAnimator.FireStateHash, breathStart);
            if (!IsAlive)
            {
                yield break;
            }

            GameObject flame = EffectSpawner.Spawn(Data.CastEffect, Caster.MouthPosition, FlameRotation, Data.EffectLifetime);
            AudioSource breath = SoundPlayer.PlayLoop(breathLoop, breathVolume, breathFadeIn, Caster.RandomPitch());
            bool hasHit = false;

            try
            {
                while (IsAlive
                       && Caster.DragonAnimator.TryGetStateTime(DragonAnimator.FireStateHash, out float time)
                       && time < breathEnd)
                {
                    if (flame != null)
                    {
                        flame.transform.SetPositionAndRotation(Caster.MouthPosition, FlameRotation);
                    }
                    SoundPlayer.SetPan(breath, Caster.MouthPosition);
                    if (!hasHit && time >= impactTime)
                    {
                        hasHit = true;
                        HitCone();
                    }
                    yield return null;
                }
            }
            finally
            {
                SoundPlayer.Stop(breath, breathFadeOut);
            }

            EffectSpawner.Release(flame, flameLinger);
            yield return WaitForLocomotion();
        }

        private Quaternion FlameRotation => Quaternion.LookRotation(transform.forward) * Quaternion.Euler(flamePitch, 0f, 0f);

        private void HitCone()
        {
            foreach (Health target in CollectTargets(transform.position, Data.Range))
            {
                Vector3 toTarget = target.transform.position - transform.position;
                toTarget.y = 0f;
                if (Vector3.Angle(transform.forward, toTarget) <= coneAngle * 0.5f)
                {
                    ApplyHit(target, transform.position);
                }
            }
        }
    }
}
