using UnityEngine;

namespace Adaptabrawl.Gameplay 
{
    /// <summary>
    /// Locks the object's transform to the Main Camera's viewport bounds.
    /// Attach this script to characters to prevent them from going offscreen.
    /// </summary>
    public class CameraBoundsConstraint : MonoBehaviour
    {
        private UnityEngine.Camera mainCamera;
        
        [Header("Bounds Settings")]
        [Tooltip("Fraction of screen width to act as padding barrier (e.g., 0.05 = 5% margin from edges)")]
        [SerializeField] private float paddingX = 0.05f;
        
        [Tooltip("Fraction of screen height to act as padding barrier")]
        [SerializeField] private float paddingY = 0.05f;

        private void Start()
        {
            // Grab a reference to the main camera in the scene.
            mainCamera = UnityEngine.Camera.main;
            
            if (mainCamera == null)
            {
                Debug.LogWarning("CameraBoundsConstraint requires a Main Camera in the scene, but none was found.");
            }
        }

        private void LateUpdate()
        {
            if (mainCamera == null) return;

            // Convert world position into viewport space (X and Y will be exactly 0 to 1 if on screen)
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);

            // Clamp the transform viewport position to be within limits
            viewportPos.x = Mathf.Clamp(viewportPos.x, paddingX, 1f - paddingX);
            viewportPos.y = Mathf.Clamp(viewportPos.y, paddingY, 1f - paddingY);

            // Convert back to world point and apply to transform
            transform.position = mainCamera.ViewportToWorldPoint(viewportPos);
        }
    }
}
