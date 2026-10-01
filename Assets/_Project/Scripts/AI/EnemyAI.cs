using DragonBattle.Combat;
using DragonBattle.Core;
using UnityEngine;

namespace DragonBattle.AI
{
    public class EnemyAI : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Chase,
            Attack,
            Dead
        }

        private const int FireSlot = 0;
        private const int TailSlot = 1;
        private const int FlySlot = 2;

        [Header("References")]
        [SerializeField] private DragonMotor motor;
        [SerializeField] private AbilityCaster abilityCaster;
        [SerializeField] private Health health;
        [SerializeField] private Health target;

        [Header("Awareness")]
        [SerializeField, Min(0f)] private float startDelay = 1.5f;
        [SerializeField, Min(0f)] private float detectionRange = 40f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float holdDistance = 6f;
        [SerializeField, Min(0f)] private float circleDistance = 10f;
        [SerializeField, Range(0f, 1f)] private float strafeWeight = 0.6f;
        [SerializeField] private Vector2 strafeSwitchInterval = new Vector2(2f, 4f);

        [Header("Attacking")]
        [SerializeField, Range(0.1f, 1f)] private float rangeMargin = 0.85f;
        [SerializeField, Min(0f)] private float flyMinDistance = 6f;
        [SerializeField, Min(0f)] private float flyRestTime = 8f;
        [SerializeField, Range(0f, 1f)] private float meleeChance = 0.5f;
        [SerializeField, Min(0f)] private float meleeCommitTime = 3f;
        [SerializeField, Range(0f, 90f)] private float aimTolerance = 15f;
        [SerializeField, Min(0f)] private float aimTimeout = 1f;
        [SerializeField] private Vector2 recoveryTime = new Vector2(0.6f, 1.4f);

        private float stateEnterTime;
        private float nextDecisionTime;
        private float nextStrafeSwitchTime;
        private float flyReadyTime;
        private float meleeIntentEndTime;
        private float strafeSign = 1f;
        private int pendingSlot = -1;
        private bool hasCast;
        private Collider targetCollider;

        public State CurrentState { get; private set; } = State.Idle;
        public Health Target => target;

        private void Reset()
        {
            motor = GetComponent<DragonMotor>();
            abilityCaster = GetComponent<AbilityCaster>();
            health = GetComponent<Health>();
        }

        private void Awake()
        {
            SetTarget(target);
        }

        private void OnEnable()
        {
            health.OnDied += HandleDied;
            nextDecisionTime = Time.time + startDelay;
        }

        private void OnDisable()
        {
            health.OnDied -= HandleDied;
            motor.SetMoveDirection(Vector3.zero);
            motor.SetLookDirection(Vector3.zero);
        }

        public void SetTarget(Health newTarget)
        {
            target = newTarget;
            targetCollider = target != null ? target.GetComponent<Collider>() : null;
        }

        private void Update()
        {
            switch (CurrentState)
            {
                case State.Idle:
                    UpdateIdle();
                    break;
                case State.Chase:
                    UpdateChase();
                    break;
                case State.Attack:
                    UpdateAttack();
                    break;
            }
        }

        private void UpdateIdle()
        {
            motor.SetMoveDirection(Vector3.zero);
            if (HasTarget && Time.time >= nextDecisionTime && ToTarget().magnitude <= detectionRange)
            {
                EnterState(State.Chase);
            }
        }

        private void UpdateChase()
        {
            if (!HasTarget)
            {
                EnterState(State.Idle);
                return;
            }

            float reach = ReachDistance();
            if (Time.time >= nextDecisionTime && !abilityCaster.IsLocked)
            {
                int slot = ChooseAbility(reach);
                if (slot >= 0)
                {
                    pendingSlot = slot;
                    EnterState(State.Attack);
                    return;
                }
            }

            motor.SetMoveDirection(ChaseDirection(reach));
        }

        private void UpdateAttack()
        {
            Vector3 toTarget = HasTarget ? ToTarget() : transform.forward;
            motor.SetMoveDirection(Vector3.zero);
            motor.SetLookDirection(toTarget);

            if (hasCast)
            {
                if (!abilityCaster.IsLocked)
                {
                    if (pendingSlot == FlySlot)
                    {
                        flyReadyTime = Time.time + flyRestTime;
                    }
                    nextDecisionTime = Time.time + Random.Range(recoveryTime.x, recoveryTime.y);
                    EnterState(HasTarget ? State.Chase : State.Idle);
                }
                return;
            }

            if (!HasTarget)
            {
                EnterState(State.Idle);
                return;
            }

            bool isAimed = pendingSlot != FireSlot || Vector3.Angle(transform.forward, toTarget) <= aimTolerance;
            if (isAimed && abilityCaster.TryCast(pendingSlot))
            {
                hasCast = true;
                return;
            }

            if (Time.time >= stateEnterTime + aimTimeout)
            {
                EnterState(State.Chase);
            }
        }

        private void EnterState(State next)
        {
            CurrentState = next;
            stateEnterTime = Time.time;
            hasCast = false;
            if (next != State.Attack)
            {
                pendingSlot = -1;
                motor.SetLookDirection(Vector3.zero);
            }
            if (next == State.Chase)
            {
                meleeIntentEndTime = Random.value < meleeChance ? Mathf.Max(Time.time, nextDecisionTime) + meleeCommitTime : 0f;
            }
        }

        private int ChooseAbility(float reach)
        {
            if (reach <= AbilityReach(TailSlot) && abilityCaster.CanCast(TailSlot))
            {
                return TailSlot;
            }
            bool isFar = reach > AbilityReach(FireSlot);
            bool isRested = Time.time >= flyReadyTime;
            if (reach >= flyMinDistance && (isFar || isRested) && reach <= AbilityReach(FlySlot) && abilityCaster.CanCast(FlySlot))
            {
                return FlySlot;
            }
            if (!WantsMelee && reach <= AbilityReach(FireSlot) && abilityCaster.CanCast(FireSlot))
            {
                return FireSlot;
            }
            return -1;
        }

        private Vector3 ChaseDirection(float reach)
        {
            Vector3 toTarget = ToTarget();
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            Vector3 forward = toTarget.normalized;
            float approach = Mathf.Clamp((reach - DesiredReach()) * 0.5f, -1f, 1f);
            Vector3 direction = forward * approach;

            if (reach <= circleDistance)
            {
                if (Time.time >= nextStrafeSwitchTime)
                {
                    strafeSign = Random.value < 0.5f ? -1f : 1f;
                    nextStrafeSwitchTime = Time.time + Random.Range(strafeSwitchInterval.x, strafeSwitchInterval.y);
                }
                direction += Vector3.Cross(Vector3.up, forward) * (strafeSign * strafeWeight);
            }

            return Vector3.ClampMagnitude(direction, 1f);
        }

        private float DesiredReach()
        {
            if (Time.time < nextDecisionTime)
            {
                return holdDistance;
            }
            if (WantsMelee)
            {
                return AbilityReach(TailSlot);
            }
            if (abilityCaster.GetAbility(FireSlot).IsReady)
            {
                return AbilityReach(FireSlot);
            }
            if (abilityCaster.GetAbility(TailSlot).IsReady)
            {
                return AbilityReach(TailSlot);
            }
            return holdDistance;
        }

        private float AbilityReach(int slot)
        {
            Ability ability = abilityCaster.GetAbility(slot);
            return ability != null ? ability.Data.Range * rangeMargin : 0f;
        }

        private float ReachDistance()
        {
            if (targetCollider == null)
            {
                return ToTarget().magnitude;
            }
            Vector3 closest = targetCollider.ClosestPoint(transform.position) - transform.position;
            closest.y = 0f;
            return closest.magnitude;
        }

        private Vector3 ToTarget()
        {
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            return toTarget;
        }

        private bool HasTarget => target != null && !target.IsDead;

        private bool WantsMelee => Time.time < meleeIntentEndTime && abilityCaster.GetAbility(TailSlot).IsReady;

        private void HandleDied(Health source)
        {
            EnterState(State.Dead);
            motor.SetMoveDirection(Vector3.zero);
        }
    }
}
