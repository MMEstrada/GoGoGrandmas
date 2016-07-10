using UnityEngine;
using System.Collections;

public class playerMovement : MonoBehaviour {

	public float speed = 50f;
	public float maxSpeed = 3f;
	public float jumpHeight;
	public bool canJump = false;

    public Transform enemy;
    float enemyTargetDistance;

    float h;
    float v;

	private Rigidbody rb;
	//private Animator anim;

	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody> ();
		//anim = GetComponent<Animator> ();
	}

    void Update()
    {
        //anim.SetFloat("Horizontal", h);
        //anim.SetFloat("Vertical", v);
    }
	
	// Update is called once per frame
	void FixedUpdate () {
	    h = Input.GetAxis("Horizontal");
	    v = Input.GetAxis ("Vertical");

        
		if (Mathf.Abs(h * rb.velocity.x) < maxSpeed) {
			if (h > 0)
            {
				rb.AddForce(Vector3.right * speed * h);
			}
			if (h < 0)
            {
				rb.AddForce(Vector3.left * speed * Mathf.Abs(h));
			}
		}

		if (Mathf.Abs (rb.velocity.x) > maxSpeed) {
			rb.velocity = new Vector3 (Mathf.Sign (rb.velocity.x) * maxSpeed, rb.velocity.y, rb.velocity.z);
		}

		if (Mathf.Abs (v * rb.velocity.z) < maxSpeed) {
			if (v > 0)
            {
				rb.AddForce(Vector3.forward * speed * v);
			}
			if (v < 0)
            {
				rb.AddForce(Vector3.back * speed * Mathf.Abs(v));
			}
		}

		if (Mathf.Abs(rb.velocity.z) > maxSpeed) {
			rb.velocity = new Vector3 (rb.velocity.x, rb.velocity.y, Mathf.Sign(rb.velocity.z) * maxSpeed);
		}

		if (Input.GetKeyDown (KeyCode.Space) && canJump) {
            rb.AddForce(Vector3.up * jumpHeight);
			canJump = false;
		}

        enemyTargetDistance = Vector3.Distance(enemy.position, transform.position);

	}

	void OnCollisionEnter (Collision col) {
		if (col.gameObject.tag == "Ground") {
			canJump = true;
			print ("you can jump");
		}
	}
}
