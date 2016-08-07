using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour {

    public float moveSpeed = 50f;
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
        float v = Input.GetAxis("Vertical");

        if (h * rb2d.velocity.x < maxSpeed)
        {
            rb2d.AddForce(Vector2.right * h * moveSpeed);
        }

        if (Mathf.Abs(rb2d.velocity.x) > maxSpeed)
        {
            rb2d.velocity = new Vector2(Mathf.Sign(rb2d.velocity.x) * maxSpeed, rb2d.velocity.y);
        }

        if (h > 0 && !facingRight)
        {
            Flip();
        }

        if (h < 0 && facingRight)
        {
            Flip();
        }

        if (Input.GetKeyDown(KeyCode.Space) && canJump)
        {
            rb2d.AddForce(Vector2.up * jumpHeight);
            canJump = false;
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 theScale = transform.localScale;
        theScale *= -1;
        transform.localScale = theScale;
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.tag == "ground")
        {
            canJump = true;
        }
    }
}
