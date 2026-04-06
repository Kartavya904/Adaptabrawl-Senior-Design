using UnityEngine;

namespace Adaptabrawl.Camera
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public class ImpactCameraShake : MonoBehaviour
    {
        [SerializeField] private float positionalAmplitude = 0.45f;
        [SerializeField] private float rotationalAmplitude = 3f;
        [SerializeField] private float traumaDecay = 2.4f;
        [SerializeField] private float shakeFrequency = 32f;

        private Vector3 lastPositionOffset = Vector3.zero;
        private Quaternion lastRotationOffset = Quaternion.identity;
        private float trauma;
        private float holdTimeRemaining;
        private float rotationTrauma;
        private float noiseSeed;

        public static ImpactCameraShake EnsureExistsOnMainCamera()
        {
            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
                mainCamera = Object.FindFirstObjectByType<UnityEngine.Camera>();

            if (mainCamera == null)
                return null;

            ImpactCameraShake shake = mainCamera.GetComponent<ImpactCameraShake>();
            if (shake == null)
                shake = mainCamera.gameObject.AddComponent<ImpactCameraShake>();

            return shake;
        }

        private void Awake()
        {
            noiseSeed = Random.value * 1000f;
        }

        public void AddShake(float magnitude, float duration, float rotationMagnitude = 1f)
        {
            trauma = Mathf.Clamp01(Mathf.Max(trauma, magnitude));
            holdTimeRemaining = Mathf.Max(holdTimeRemaining, duration);
            rotationTrauma = Mathf.Clamp(Mathf.Max(rotationTrauma, rotationMagnitude), 0f, 3f);
        }

        private void LateUpdate()
        {
            RemovePreviousOffset();

            if (trauma <= 0f && holdTimeRemaining <= 0f)
                return;

            float dt = Time.unscaledDeltaTime;
            if (holdTimeRemaining > 0f)
            {
                holdTimeRemaining -= dt;
            }
            else
            {
                trauma = Mathf.MoveTowards(trauma, 0f, traumaDecay * dt);
                rotationTrauma = Mathf.MoveTowards(rotationTrauma, 0f, traumaDecay * dt * 1.25f);
            }

            float traumaPower = trauma * trauma;
            if (traumaPower <= 0.0001f)
                return;

            float noiseTime = Time.unscaledTime * shakeFrequency;
            float x = SampleSignedNoise(noiseTime, 0f);
            float y = SampleSignedNoise(noiseTime, 17.13f);
            float zRot = SampleSignedNoise(noiseTime, 43.37f);

            lastPositionOffset = new Vector3(x, y, 0f) * (positionalAmplitude * traumaPower);
            lastRotationOffset = Quaternion.Euler(0f, 0f, zRot * rotationalAmplitude * rotationTrauma * traumaPower);

            transform.position += lastPositionOffset;
            transform.rotation *= lastRotationOffset;
        }

        private void RemovePreviousOffset()
        {
            transform.position -= lastPositionOffset;
            transform.rotation *= Quaternion.Inverse(lastRotationOffset);
            lastPositionOffset = Vector3.zero;
            lastRotationOffset = Quaternion.identity;
        }

        private float SampleSignedNoise(float time, float offset)
        {
            return Mathf.PerlinNoise(noiseSeed + offset, time) * 2f - 1f;
        }
    }
}
