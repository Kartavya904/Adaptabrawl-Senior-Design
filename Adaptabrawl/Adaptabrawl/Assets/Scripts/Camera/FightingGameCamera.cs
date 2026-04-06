using UnityEngine;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Camera
{
    public class FightingGameCamera : MonoBehaviour
    {
        public static FightingGameCamera Instance { get; private set; }

        [Header("Targets")]
        [SerializeField] private FighterController player1;
        [SerializeField] private FighterController player2;
        
        [Header("Camera Settings")]
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private Vector2 cameraOffset = Vector2.zero;
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 15f;
        [SerializeField] private float distancePadding = 2f;
        
        [Header("Bounds")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private Vector2 minBounds = new Vector2(-10f, -5f);
        [SerializeField] private Vector2 maxBounds = new Vector2(10f, 5f);

        [Header("Impact Shake")]
        [SerializeField] private float defaultShakeDuration = 0.1f;
        [SerializeField] private float defaultShakeMagnitude = 0.14f;
        [SerializeField] private float shakeDamping = 14f;
        
        private UnityEngine.Camera cam;
        private Vector3 targetPosition;
        private float shakeTimeRemaining;
        private float shakeMagnitude;
        private Vector3 shakeOffset;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        
        private void Start()
        {
            cam = GetComponent<UnityEngine.Camera>();
            if (cam == null)
                cam = UnityEngine.Camera.main;
            
            // Find players if not assigned
            if (player1 == null || player2 == null)
            {
                var fighters = FindObjectsByType<FighterController>(FindObjectsSortMode.None);
                if (fighters.Length >= 2)
                {
                    player1 = fighters[0];
                    player2 = fighters[1];
                }
            }
        }
        
        private void LateUpdate()
        {
            if (player1 == null || player2 == null) return;
            
            UpdateCameraPosition();
        }
        
        private void UpdateCameraPosition()
        {
            // Calculate midpoint between players
            Vector3 midpoint = (player1.transform.position + player2.transform.position) / 2f;
            
            // Calculate distance between players
            float distance = Vector3.Distance(player1.transform.position, player2.transform.position);
            
            // Adjust camera distance based on player distance
            float targetDistance = Mathf.Clamp(distance + distancePadding, minDistance, maxDistance);
            
            // Calculate target position
            targetPosition = midpoint + (Vector3)cameraOffset;
            targetPosition.z = transform.position.z; // Maintain Z position
            
            // Apply bounds if enabled
            if (useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
                targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
            }
            
            // Smoothly move camera
            Vector3 desiredPosition = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            UpdateShakeOffset();
            transform.position = desiredPosition + shakeOffset;
            
            // Optionally adjust orthographic size based on distance
            if (cam != null && cam.orthographic)
            {
                float targetSize = Mathf.Lerp(5f, 10f, (distance - minDistance) / (maxDistance - minDistance));
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, followSpeed * Time.deltaTime);
            }
        }

        public void TriggerImpactShake(float duration, float magnitude)
        {
            shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, duration > 0f ? duration : defaultShakeDuration);
            shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude > 0f ? magnitude : defaultShakeMagnitude);
        }

        private void UpdateShakeOffset()
        {
            if (shakeTimeRemaining <= 0f)
            {
                shakeOffset = Vector3.zero;
                shakeMagnitude = 0f;
                return;
            }

            shakeTimeRemaining -= Time.unscaledDeltaTime;
            float currentMagnitude = Mathf.Lerp(0f, shakeMagnitude, shakeTimeRemaining / Mathf.Max(defaultShakeDuration, 0.001f));

            Vector2 randomOffset = Random.insideUnitCircle * currentMagnitude;
            shakeOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);
            shakeMagnitude = Mathf.MoveTowards(shakeMagnitude, 0f, shakeDamping * Time.unscaledDeltaTime);
        }
        
        public void SetPlayers(FighterController p1, FighterController p2)
        {
            player1 = p1;
            player2 = p2;
        }
        
        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
            useBounds = true;
        }
    }
}
