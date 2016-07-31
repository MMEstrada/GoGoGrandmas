using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour {

    public float speed = 5f;
    public float maxSpeed = 3f;
    public float jumpHeight;
    
    public bool facingRight = true;
    public bool canJump = true;

    private Rigidbody2D rb2d;

	// Use this for initialization
	void Start () {
        rb2d = GetComponent<Rigidbody2D>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void FixedUpdate()
    {
        float h = Input.GetAxis("Horizontal");
    }
}
