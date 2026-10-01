using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DragonBattle.Core
{
    [RequireComponent(typeof(Camera))]
    public class MenuCamera : MonoBehaviour
    {
        private enum Mode
        {
            Inactive,
            Menu,
            Flight
        }

        [Header("References")]
        [SerializeField] private CameraController gameplayCamera;
        [SerializeField] private Transform subject;
        [SerializeField] private Volume menuVolume;
        [SerializeField] private Light[] menuLights = Array.Empty<Light>();

        [Header("Menu Shot")]
        [SerializeField] private Vector3 focusOffset = new Vector3(0f, 1.85f, 0.9f);
        [SerializeField] private Vector3 shotOffset = new Vector3(-3.9f, 0.1f, 4.15f);
        [SerializeField] private Vector2 composition = new Vector2(0.62f, 0.6f);
        [SerializeField, Range(10f, 80f)] private float menuFieldOfView = 30f;
        [SerializeField, Min(0f)] private float introDolly = 1.4f;
        [SerializeField, Min(0.01f)] private float introDuration = 4.5f;

        [Header("Idle Motion")]
        [SerializeField, Min(0f)] private float swayAngle = 3.5f;
        [SerializeField, Min(0f)] private float swaySpeed = 0.05f;
        [SerializeField, Min(0f)] private float handheldOffset = 0.03f;
        [SerializeField, Min(0f)] private float handheldAngle = 0.3f;
        [SerializeField, Min(0f)] private float handheldSpeed = 0.3f;
        [SerializeField] private Vector2 parallax = new Vector2(0.35f, 0.18f);
        [SerializeField, Min(0.01f)] private float parallaxSmoothTime = 0.5f;

        [Header("Roar")]
        [SerializeField, Min(0f)] private float roarPushIn = 0.6f;
        [SerializeField, Min(0f)] private float roarFieldOfView = 3f;
        [SerializeField, Min(0f)] private float roarShakeOffset = 0.05f;
        [SerializeField, Min(0f)] private float roarShakeAngle = 0.9f;
        [SerializeField, Min(0f)] private float roarShakeFrequency = 18f;

        [Header("Flight")]
        [SerializeField, Min(0.1f)] private float flightDuration = 3.4f;
        [SerializeField] private Vector3 liftOffset = new Vector3(-4f, 4.5f, -1.5f);
        [SerializeField] private Vector3 approachOffset = new Vector3(0f, 0f, 15f);
        [SerializeField, Min(0f)] private float fieldOfViewKick = 14f;
        [SerializeField] private float bankAngle = -9f;
        [SerializeField, Min(0.01f)] private float lightFadeDuration = 1.2f;

        private Camera view;
        private DepthOfField depthOfField;
        private float[] lightIntensities;
        private Mode mode;
        private float gameplayFieldOfView;
        private float menuStartTime;
        private float roarStartTime = float.NegativeInfinity;
        private float roarDuration = 1f;
        private float noiseSeed;
        private Vector2 parallaxCurrent;
        private Vector2 parallaxVelocity;

        private float flightElapsed;
        private Vector3 flightStartPosition;
        private Quaternion flightStartRotation;
        private float flightStartFieldOfView;
        private Vector3 flightStartFocus;
        private Action onArrived;

        public bool IsFlying => mode == Mode.Flight;

        private void Awake()
        {
            view = GetComponent<Camera>();
            gameplayFieldOfView = view.fieldOfView;
            noiseSeed = UnityEngine.Random.value * 100f;
            lightIntensities = new float[menuLights.Length];
            for (int i = 0; i < menuLights.Length; i++)
            {
                lightIntensities[i] = menuLights[i] != null ? menuLights[i].intensity : 0f;
            }
            if (menuVolume != null && menuVolume.profile.TryGet(out DepthOfField dof))
            {
                depthOfField = dof;
            }
            SetMenuDressing(0f);
        }

        public void BeginMenu()
        {
            mode = Mode.Menu;
            menuStartTime = Time.unscaledTime;
            gameplayCamera.enabled = false;
            SetMenuDressing(1f);
            UpdateMenu();
        }

        public void Roar(float duration)
        {
            roarStartTime = Time.unscaledTime;
            roarDuration = Mathf.Max(0.01f, duration);
        }

        public void Fly(Action arrived)
        {
            onArrived = arrived;
            flightElapsed = 0f;
            flightStartPosition = transform.position;
            flightStartRotation = transform.rotation;
            flightStartFieldOfView = view.fieldOfView;
            flightStartFocus = subject.TransformPoint(focusOffset);
            mode = Mode.Flight;
        }

        private void LateUpdate()
        {
            switch (mode)
            {
                case Mode.Menu:
                    UpdateMenu();
                    break;
                case Mode.Flight:
                    UpdateFlight();
                    break;
            }
        }

        private void UpdateMenu()
        {
            float time = Time.unscaledTime;
            float intro = Ease.OutCubic((time - menuStartTime) / introDuration);
            float roar = RoarEnvelope(time);
            float roarPush = Ease.OutCubic(Mathf.Clamp01((time - roarStartTime) / roarDuration));

            Vector3 focus = subject.TransformPoint(focusOffset);
            float sway = Mathf.Sin(time * swaySpeed * Mathf.PI * 2f) * swayAngle;
            Vector3 offset = Quaternion.AngleAxis(sway, Vector3.up) * subject.TransformDirection(shotOffset);
            float dolly = introDolly * (1f - intro) - roarPushIn * (float.IsInfinity(roarStartTime) ? 0f : roarPush);
            Vector3 position = focus + offset + offset.normalized * dolly;

            parallaxCurrent = Vector2.SmoothDamp(parallaxCurrent, ReadPointer(), ref parallaxVelocity,
                parallaxSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            Quaternion look = Quaternion.LookRotation(focus - position);
            position += look * new Vector3(parallaxCurrent.x * parallax.x, parallaxCurrent.y * parallax.y, 0f);

            float fieldOfView = menuFieldOfView - roarFieldOfView * roar;
            Quaternion rotation = Quaternion.LookRotation(focus - position) * CompositionOffset(fieldOfView);

            float noiseTime = time * handheldSpeed;
            Vector3 drift = new Vector3(Noise(0f, noiseTime), Noise(10f, noiseTime), 0f) * handheldOffset;
            Vector3 tilt = new Vector3(Noise(20f, noiseTime), Noise(30f, noiseTime), Noise(40f, noiseTime) * 0.5f) * handheldAngle;

            float shakeTime = time * roarShakeFrequency;
            Vector3 shake = new Vector3(Noise(50f, shakeTime), Noise(60f, shakeTime), 0f) * (roarShakeOffset * roar);
            Vector3 shakeTilt = new Vector3(Noise(70f, shakeTime), Noise(80f, shakeTime), Noise(90f, shakeTime)) * (roarShakeAngle * roar);

            position += rotation * (drift + shake);
            rotation *= Quaternion.Euler(tilt + shakeTilt);
            transform.SetPositionAndRotation(position, rotation);
            view.fieldOfView = fieldOfView;
            UpdateFocusDistance(Vector3.Distance(position, focus));
        }

        private void UpdateFlight()
        {
            flightElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(flightElapsed / flightDuration);
            float travel = Ease.InOutCubic(t);
            Pose end = gameplayCamera.TargetPose;

            Vector3 lift = flightStartPosition + subject.TransformDirection(liftOffset);
            Vector3 approach = end.position + end.rotation * approachOffset;
            Vector3 position = Bezier(flightStartPosition, lift, approach, end.position, travel);

            Vector3 lookTarget = Vector3.Lerp(flightStartFocus, gameplayCamera.TargetFocus, Ease.Smooth(0.1f, 0.8f, travel));
            Quaternion look = Quaternion.LookRotation(lookTarget - position);
            Quaternion rotation = Quaternion.Slerp(flightStartRotation, look, Ease.Smooth(0f, 0.3f, travel));
            rotation = Quaternion.Slerp(rotation, end.rotation, Ease.Smooth(0.55f, 1f, travel));
            rotation *= Quaternion.Euler(0f, 0f, Ease.Bell(travel) * bankAngle);

            transform.SetPositionAndRotation(position, rotation);
            view.fieldOfView = Mathf.Lerp(flightStartFieldOfView, gameplayFieldOfView, travel) + Ease.Bell(travel) * fieldOfViewKick;
            UpdateFocusDistance(Vector3.Distance(position, lookTarget));

            float lightFade = Mathf.Clamp01(flightElapsed / lightFadeDuration);
            SetMenuDressing(1f - Ease.Smooth(0f, 1f, lightFade), 1f - Ease.Smooth(0f, 0.45f, travel));

            if (t >= 1f)
            {
                Arrive(end);
            }
        }

        private void Arrive(Pose end)
        {
            mode = Mode.Inactive;
            transform.SetPositionAndRotation(end.position, end.rotation);
            view.fieldOfView = gameplayFieldOfView;
            SetMenuDressing(0f);
            gameplayCamera.enabled = true;

            Action arrived = onArrived;
            onArrived = null;
            arrived?.Invoke();
        }

        private float RoarEnvelope(float time)
        {
            float t = (time - roarStartTime) / roarDuration;
            if (t <= 0f || t >= 1f)
            {
                return 0f;
            }
            return Mathf.Min(1f, t * 6f) * (1f - t) * (1f - t);
        }

        private Quaternion CompositionOffset(float fieldOfView)
        {
            float tanVertical = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanHorizontal = tanVertical * view.aspect;
            float yaw = Mathf.Atan((composition.x - 0.5f) * 2f * tanHorizontal) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan((composition.y - 0.5f) * 2f * tanVertical) * Mathf.Rad2Deg;
            return Quaternion.Euler(pitch, -yaw, 0f);
        }

        private static Vector2 ReadPointer()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return Vector2.zero;
            }
            Vector2 position = mouse.position.ReadValue();
            float x = Mathf.Clamp(position.x / Screen.width * 2f - 1f, -1f, 1f);
            float y = Mathf.Clamp(position.y / Screen.height * 2f - 1f, -1f, 1f);
            return new Vector2(x, y);
        }

        private float Noise(float offset, float time)
        {
            return Mathf.PerlinNoise(noiseSeed + offset, time) * 2f - 1f;
        }

        private void UpdateFocusDistance(float distance)
        {
            if (depthOfField != null)
            {
                depthOfField.focusDistance.value = distance;
            }
        }

        private void SetMenuDressing(float weight)
        {
            SetMenuDressing(weight, weight);
        }

        private void SetMenuDressing(float lightWeight, float volumeWeight)
        {
            if (menuVolume != null)
            {
                menuVolume.weight = volumeWeight;
            }
            for (int i = 0; i < menuLights.Length; i++)
            {
                if (menuLights[i] != null)
                {
                    menuLights[i].intensity = lightIntensities[i] * lightWeight;
                    menuLights[i].enabled = lightWeight > 0f;
                }
            }
        }

        private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float u = 1f - t;
            return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
        }
    }
}
