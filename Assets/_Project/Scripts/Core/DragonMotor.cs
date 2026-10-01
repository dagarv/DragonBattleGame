using DragonBattle.Combat;
using UnityEngine;

namespace DragonBattle.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class DragonMotor : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private DragonAnimator dragonAnimator;
        [SerializeField] private AbilityCaster abilityCaster;
        [SerializeField] private Health health;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float acceleration = 40f;
        [SerializeField] private float deceleration = 50f;
        [SerializeField] private float turnSpeed = 720f;

        [Header("Knockback")]
        [SerializeField] private float knockbackDuration = 0.35f;
        [SerializeField] private float knockbackDeceleration = 20f;

        private Vector3 desiredDirection;
        private Vector3 lookDirection;
        private Vector3 planarVelocity;
        private Vector3 steerVelocity;
        private Vector3 steerFacing;
        private bool isSteering;
        private float knockbackEndTime;

        public float MoveSpeed => moveSpeed;
        public bool IsKnockedBack => Time.time < knockbackEndTime;
        public bool CanMove => !health.IsDead && !abilityCaster.IsLocked && !IsKnockedBack;

        private void Reset()
        {
            body = GetComponent<Rigidbody>();
            dragonAnimator = GetComponent<DragonAnimator>();
            abilityCaster = GetComponent<AbilityCaster>();
            health = GetComponent<Health>();
        }

        public void SetMoveDirection(Vector3 direction)
        {
            direction.y = 0f;
            desiredDirection = Vector3.ClampMagnitude(direction, 1f);
        }

        public void SetLookDirection(Vector3 direction)
        {
            direction.y = 0f;
            lookDirection = direction;
        }

        public void Steer(Vector3 velocity, Vector3 facing)
        {
            velocity.y = 0f;
            facing.y = 0f;
            steerVelocity = velocity;
            steerFacing = facing;
            isSteering = true;
        }

        public void StopSteering()
        {
            isSteering = false;
            steerVelocity = Vector3.zero;
        }

        public void ApplyKnockback(Vector3 velocity)
        {
            if (isSteering)
            {
                return;
            }

            velocity.y = 0f;
            planarVelocity = velocity;
            knockbackEndTime = Time.time + knockbackDuration;
        }

        private void Update()
        {
            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;
            dragonAnimator.SetSpeed(velocity.magnitude / moveSpeed);
        }

        private void FixedUpdate()
        {
            if (isSteering)
            {
                MoveTowardsVelocity(steerVelocity, acceleration);
                RotateTowards(steerFacing);
                return;
            }

            if (IsKnockedBack)
            {
                MoveTowardsVelocity(Vector3.zero, knockbackDeceleration);
                return;
            }

            bool canMove = CanMove;
            Vector3 targetVelocity = canMove ? desiredDirection * moveSpeed : Vector3.zero;
            float rate = targetVelocity.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : deceleration;
            MoveTowardsVelocity(targetVelocity, rate);

            if (canMove)
            {
                RotateTowards(desiredDirection.sqrMagnitude > 0.0001f ? desiredDirection : lookDirection);
            }
        }

        private void MoveTowardsVelocity(Vector3 targetVelocity, float rate)
        {
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, rate * Time.fixedDeltaTime);
            body.linearVelocity = new Vector3(planarVelocity.x, body.linearVelocity.y, planarVelocity.z);
        }

        private void RotateTowards(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }
    }
}
