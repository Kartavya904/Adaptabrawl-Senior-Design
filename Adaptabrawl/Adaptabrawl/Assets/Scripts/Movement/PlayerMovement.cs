using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        float xInput = Input.GetAxis("Horizontal") * 5;
        float yInput = Input.GetAxis("Vertical") * 5;

        body.velocity = new Vector2(xInput, yInput);
    }
}
