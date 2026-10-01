using DragonBattle.Audio;
using DragonBattle.Combat;
using DragonBattle.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DragonBattle.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference[] abilityActions = new InputActionReference[3];

        [Header("References")]
        [SerializeField] private DragonMotor motor;
        [SerializeField] private AbilityCaster abilityCaster;
        [SerializeField] private Transform cameraTransform;

        [Header("Audio")]
        [SerializeField] private SoundCue deniedSound = new SoundCue();

        private void Reset()
        {
            motor = GetComponent<DragonMotor>();
            abilityCaster = GetComponent<AbilityCaster>();
        }

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            moveAction.action.Enable();
            foreach (InputActionReference ability in abilityActions)
            {
                ability.action.Enable();
            }
        }

        private void OnDisable()
        {
            moveAction.action.Disable();
            foreach (InputActionReference ability in abilityActions)
            {
                ability.action.Disable();
            }
            motor.SetMoveDirection(Vector3.zero);
        }

        private void Update()
        {
            motor.SetMoveDirection(ReadMoveDirection());

            for (int slot = 0; slot < abilityActions.Length; slot++)
            {
                if (abilityActions[slot].action.WasPressedThisFrame())
                {
                    TryCast(slot);
                }
            }
        }

        private void TryCast(int slot)
        {
            if (abilityCaster.TryCast(slot))
            {
                return;
            }

            Ability ability = abilityCaster.GetAbility(slot);
            if (ability != null && !ability.IsReady)
            {
                deniedSound.Play2D();
            }
        }

        private Vector3 ReadMoveDirection()
        {
            Vector2 input = Vector2.ClampMagnitude(moveAction.action.ReadValue<Vector2>(), 1f);
            if (cameraTransform == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
            return forward * input.y + right * input.x;
        }
    }
}
