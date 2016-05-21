using UnityEngine;
using System.Collections;

public class playerMovement : MonoBehaviour {

	public float movementSpeed;
	public float maxSpeed;
	public float jumpHeight;
	public bool canJump;

	private Rigidbody rb;
	private Animator anim;
	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody> ();
		anim = GetComponent<Animator> ();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis ("Vertical");

		if (Mathf.Abs(h * rb.velocity.x) < maxSpeed) {
			if (h > 0) {
				rb.AddForce (Vector3.right * movementSpeed);
			}
			if (h < 0) {
				rb.AddForce (Vector3.left * movementSpeed);
			}
		}

		if (Mathf.Abs (rb.velocity.x) > maxSpeed) {
			rb.velocity = new Vector3 (Mathf.Sign (rb.velocity.x) * maxSpeed, rb.velocity.y, rb.velocity.z);
		}

		if (Mathf.Abs (h * rb.velocity.z) < maxSpeed) {
			if (h > 0) {
				rb.AddForce (Vector3.forward * movementSpeed);
			}
			if (h < 0) {
				rb.AddForce (Vector3.back * movementSpeed);
			}
		}

		if (Mathf.Abs(rb.velocity.z) > maxSpeed) {
			rb.velocity = new Vector3 (rb.velocity.x, rb.velocity.y, Mathf.Sign(rb.velocity.z) * maxSpeed);
		}

		if (Input.GetKeyDown (KeyCode.Space) && canJump) {
			rb.AddForce (Vector3.up * jumpHeight);
			canJump = false;
		}
	}

	void OnCollisionEnter (Collision col) {
		if (col.gameObject.tag == "ground") {
			canJump = true;
		}
	}
}
