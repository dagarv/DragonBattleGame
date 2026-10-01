using System.Collections;
using DragonBattle.Audio;
using DragonBattle.Core;
using UnityEngine;

namespace DragonBattle.Combat.Abilities
{
    public class TailAttack : Ability
    {
        [SerializeField, Range(0f, 1f)] private float impactTime = 0.45f;
        [SerializeField] private Vector3 sweepCenter = new Vector3(0f, 1f, 0f);

        [Header("Swing Audio")]
        [SerializeField, Range(0f, 1f)] private float swingTime = 0.3f;
        [SerializeField] private SoundCue swing = new SoundCue();

        protected override IEnumerator Perform()
        {
            Caster.DragonAnimator.Trigger(DragonAnimator.TailHash);
            yield return WaitForStateTime(DragonAnimator.TailStateHash, Mathf.Min(swingTime, impactTime));
            if (!IsAlive)
            {
                yield break;
            }

            swing.Play(transform.position, Caster.RandomPitch());
            yield return WaitForStateTime(DragonAnimator.TailStateHash, impactTime);
            if (!IsAlive)
            {
                yield break;
            }

            EffectSpawner.Spawn(Data.CastEffect, transform.position, Quaternion.identity, Data.EffectLifetime);
            foreach (Health target in CollectTargets(transform.TransformPoint(sweepCenter), Data.Range))
            {
                ApplyHit(target, transform.position);
            }

            yield return WaitForLocomotion();
        }
    }
}
