using UnityEngine;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Bootstrap component placed on the root fighter by FighterFactory.
    /// On Start() — after the full prefab hierarchy is live — it locates the
    /// "Stander" child (the root-motion animated character) and attaches
    /// CameraBoundsConstraint to it, then removes itself.
    /// </summary>
    public class StanderCameraConstraint : MonoBehaviour
    {
        private void Start()
        {
            Transform stander = transform.Find("Stander");

            if (stander != null)
            {
                if (stander.GetComponent<CameraBoundsConstraint>() == null)
                    stander.gameObject.AddComponent<CameraBoundsConstraint>();
            }
            else
            {
                Debug.LogWarning($"[StanderCameraConstraint] No 'Stander' child found on '{gameObject.name}'. CameraBoundsConstraint was not added.");
            }

            Destroy(this);
        }
    }
}
