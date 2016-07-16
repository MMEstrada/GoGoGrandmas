using UnityEngine;
using System.Collections;
/// <summary>
/// Extra gravity force. I used this to add extra gravity to rigidbody objects to
/// make them fall faster, such as my crate pieces.
/// </summary>
public class ExtraGravityForce : MonoBehaviour
{
	[Range(1, 4)] public float gravityMultiplier = 1;

	Rigidbody _myRigidbody;

	void Awake()
	{
		_myRigidbody = GetComponent<Rigidbody>();
	}

	void FixedUpdate ()
	{
		Vector3 extraGravityForce = (Physics.gravity*gravityMultiplier) - Physics.gravity;
		_myRigidbody.AddForce(extraGravityForce);
	}
}