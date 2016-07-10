using UnityEngine;
using System.Collections;

public class playerMovement : MonoBehaviour {

	public float movementSpeed = 50f;
	public float maxSpeed = 3f;
	public float jumpHeight = 10f;
	public bool canJump = false;
    public float h;
    public float v;

	private Rigidbody rb;
	private Animator anim;
	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody> ();
		anim = GetComponent<Animator> ();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
	    h = Input.GetAxis("Horizontal");
	    v = Input.GetAxis ("Vertical");

		if (Mathf.Abs(h * rb.velocity.x) < maxSpeed) {
			if (h > 0) {
				print (Vector3.right);
				rb.AddForce (Vector3.right * movementSpeed * h);
			}
			if (h < 0) {
				print (Vector3.left);
				rb.AddForce (Vector3.left * movementSpeed * Mathf.Abs(h));
			}
		}

		if (Mathf.Abs (rb.velocity.x) > maxSpeed) {
			rb.velocity = new Vector3 (Mathf.Sign (rb.velocity.x) * maxSpeed, rb.velocity.y, rb.velocity.z);
		}

		if (Mathf.Abs (v * rb.velocity.z) < maxSpeed) {
			if (v > 0) {
				print("moving forward");
				rb.AddForce (Vector3.forward * movementSpeed * v);
			}
			if (v < 0) {
				print ("moving backward");
				rb.AddForce (Vector3.back * movementSpeed * Mathf.Abs(v));
			}
		}

		if (Mathf.Abs(rb.velocity.z) > maxSpeed) {
			rb.velocity = new Vector3 (rb.velocity.x, rb.velocity.y, Mathf.Sign(rb.velocity.z) * maxSpeed);
		}

		if (Input.GetKeyDown (KeyCode.Space) && canJump) {
			print ("what is going on here");
			rb.AddForce (Vector3.up * jumpHeight);
			canJump = false;
		}
	}

	void OnCollisionEnter (Collision col) {
		if (col.gameObject.tag == "Ground") {
			canJump = true;
			print ("you can jump");
		}
	}
}
