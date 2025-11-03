using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;

    // Update is called once per frame
    void Update()
    {
        float xInput = Input.GetAxis("Horizontal") * 5f;
        float yInput = Input.GetAxis("Vertical") * 5f;

        body.linearVelocity = new Vector2(xInput, yInput);
    }
}
