using DragonBattle.Combat;
using UnityEngine;

namespace DragonBattle.Core
{
    public class DragonAnimator : MonoBehaviour
    {
        public static readonly int SpeedHash = Animator.StringToHash("Speed");
        public static readonly int FireHash = Animator.StringToHash("Fire");
        public static readonly int TailHash = Animator.StringToHash("Tail");
        public static readonly int TakeOffHash = Animator.StringToHash("TakeOff");
        public static readonly int FlyShootHash = Animator.StringToHash("FlyShoot");
        public static readonly int LandHash = Animator.StringToHash("Land");
        public static readonly int GetHitHash = Animator.StringToHash("GetHit");
        public static readonly int DieHash = Animator.StringToHash("Die");
        public static readonly int FlyStateHash = Animator.StringToHash("Fly");
        public static readonly int FlyShootStateHash = Animator.StringToHash("FlyShoot");
        public static readonly int FireStateHash = Animator.StringToHash("Fire");
        public static readonly int TailStateHash = Animator.StringToHash("Tail");
        public static readonly int TakeOffStateHash = Animator.StringToHash("TakeOff");
        public static readonly int LandStateHash = Animator.StringToHash("Land");
        public static readonly int ScreamStateHash = Animator.StringToHash("Scream");

        private const string LocomotionTag = "Locomotion";

        [SerializeField] private Animator animator;
        [SerializeField] private Health health;
        [SerializeField] private float speedDampTime = 0.1f;

        public Animator Animator => animator;

        public bool IsInLocomotion =>
            animator.GetCurrentAnimatorStateInfo(0).IsTag(LocomotionTag) && !animator.IsInTransition(0);

        public bool IsBusy
        {
            get
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsTag(LocomotionTag))
                {
                    return true;
                }
                return animator.IsInTransition(0) && !animator.GetNextAnimatorStateInfo(0).IsTag(LocomotionTag);
            }
        }

        public bool IsInState(int stateHash)
        {
            return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == stateHash && !animator.IsInTransition(0);
        }

        public bool TryGetStateTime(int stateHash, out float normalizedTime)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            if (current.shortNameHash == stateHash)
            {
                normalizedTime = current.normalizedTime;
                return true;
            }

            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
                if (next.shortNameHash == stateHash)
                {
                    normalizedTime = next.normalizedTime;
                    return true;
                }
            }

            normalizedTime = 0f;
            return false;
        }

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamaged += HandleDamaged;
                health.OnDied += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamaged -= HandleDamaged;
                health.OnDied -= HandleDied;
            }
        }

        public void SetSpeed(float normalizedSpeed)
        {
            animator.SetFloat(SpeedHash, Mathf.Clamp01(normalizedSpeed), speedDampTime, Time.deltaTime);
        }

        public void Trigger(int hash)
        {
            animator.SetTrigger(hash);
        }

        public void CrossFade(int stateHash, float duration)
        {
            animator.CrossFadeInFixedTime(stateHash, duration);
        }

        private void HandleDamaged(Health source, float amount)
        {
            if (!source.IsDead && IsInLocomotion)
            {
                animator.SetTrigger(GetHitHash);
            }
        }

        private void HandleDied(Health source)
        {
            animator.ResetTrigger(GetHitHash);
            animator.SetTrigger(DieHash);
        }
    }
}
