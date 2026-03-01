using UnityEngine;
using Adaptabrawl.Gameplay;

// Legacy movement script - replaced by MovementController
// Keeping for backwards compatibility
public class PlayerMove : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    private MovementController movementController;

    private void Start()
    {
        movementController = GetComponent<MovementController>();
        if (movementController == null)
        {
            movementController = gameObject.AddComponent<MovementController>();
        }
    }

    void Update()
    {
        // Use legacy input for now - will be replaced by Input System
        float xInput = Input.GetAxis("Horizontal");
        float yInput = Input.GetAxis("Vertical");
        
        if (movementController != null)
        {
            movementController.SetMoveInput(new Vector2(xInput, yInput));
            
            if (Input.GetButtonDown("Jump"))
            {
                movementController.Jump();
            }
        }
        else
        {
            // Fallback to old behavior
            body.linearVelocity = new Vector2(xInput * 5f, yInput * 5f);
        }
    }
}
