using System.Collections.Generic;
using UnityEngine;

namespace DragonBattle.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private List<Transform> targets = new List<Transform>();

        [Header("Framing")]
        [SerializeField] private Vector3 arenaCenter = Vector3.zero;
        [SerializeField] private Vector2 focusLimits = new Vector2(6f, 6f);
        [SerializeField, Range(30f, 85f)] private float pitch = 55f;
        [SerializeField] private float yaw;
        [SerializeField] private float focusHeightOffset = 1f;

        [Header("Zoom")]
        [SerializeField] private float minDistance = 26f;
        [SerializeField] private float maxDistance = 42f;
        [SerializeField] private float distancePerMeterOfSpread = 0.9f;

        [Header("Smoothing")]
        [SerializeField] private float followSmoothTime = 0.25f;
        [SerializeField] private float zoomSmoothTime = 0.45f;

        [Header("Shake")]
        [SerializeField, Min(0f)] private float maxShakeOffset = 0.8f;
        [SerializeField, Min(0f)] private float maxShakeAngle = 1.5f;
        [SerializeField, Min(0f)] private float shakeFrequency = 22f;
        [SerializeField, Min(0.01f)] private float shakeRecovery = 1.8f;

        private Vector3 focus;
        private Vector3 focusVelocity;
        private float distance;
        private float distanceVelocity;
        private float trauma;
        private float shakeSeed;

        public Pose TargetPose
        {
            get
            {
                Quaternion rotation = ViewRotation;
                return new Pose(ComputeFocus() - rotation * Vector3.forward * ComputeDistance(), rotation);
            }
        }

        public Vector3 TargetFocus => ComputeFocus();

        private Quaternion ViewRotation => Quaternion.Euler(pitch, yaw, 0f);

        public void AddShake(float amount)
        {
            if (!GameSettings.ScreenShake)
            {
                return;
            }
            trauma = Mathf.Clamp01(trauma + amount);
        }

        public void SetTargets(params Transform[] newTargets)
        {
            targets.Clear();
            targets.AddRange(newTargets);
        }

        private void Start()
        {
            shakeSeed = Random.value * 100f;
            SnapToTargets();
        }

        private void LateUpdate()
        {
            focus = Vector3.SmoothDamp(focus, ComputeFocus(), ref focusVelocity, followSmoothTime);
            distance = Mathf.SmoothDamp(distance, ComputeDistance(), ref distanceVelocity, zoomSmoothTime);
            ApplyTransform();
            ApplyShake();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                SnapToTargets();
            }
        }

        private void SnapToTargets()
        {
            focus = ComputeFocus();
            distance = ComputeDistance();
            focusVelocity = Vector3.zero;
            distanceVelocity = 0f;
            ApplyTransform();
        }

        private Vector3 ComputeFocus()
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (Transform target in targets)
            {
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    continue;
                }
                sum += target.position;
                count++;
            }

            Vector3 average = count > 0 ? sum / count : arenaCenter;
            float x = Mathf.Clamp(average.x, arenaCenter.x - focusLimits.x, arenaCenter.x + focusLimits.x);
            float z = Mathf.Clamp(average.z, arenaCenter.z - focusLimits.y, arenaCenter.z + focusLimits.y);
            return new Vector3(x, arenaCenter.y + focusHeightOffset, z);
        }

        private float ComputeDistance()
        {
            float spread = 0f;
            for (int i = 0; i < targets.Count; i++)
            {
                for (int j = i + 1; j < targets.Count; j++)
                {
                    if (targets[i] == null || targets[j] == null)
                    {
                        continue;
                    }
                    Vector3 offset = targets[i].position - targets[j].position;
                    offset.y = 0f;
                    spread = Mathf.Max(spread, offset.magnitude);
                }
            }
            return Mathf.Clamp(minDistance + spread * distancePerMeterOfSpread, minDistance, maxDistance);
        }

        private void ApplyShake()
        {
            if (trauma <= 0f)
            {
                return;
            }

            float strength = trauma * trauma;
            float time = Time.time * shakeFrequency;
            float x = Mathf.PerlinNoise(shakeSeed, time) * 2f - 1f;
            float y = Mathf.PerlinNoise(shakeSeed + 10f, time) * 2f - 1f;
            float roll = Mathf.PerlinNoise(shakeSeed + 20f, time) * 2f - 1f;
            transform.position += transform.rotation * new Vector3(x, y, 0f) * (maxShakeOffset * strength);
            transform.rotation *= Quaternion.Euler(0f, 0f, roll * maxShakeAngle * strength);
            trauma = Mathf.Max(0f, trauma - shakeRecovery * Time.deltaTime);
        }

        private void ApplyTransform()
        {
            Quaternion rotation = ViewRotation;
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }
    }
}
