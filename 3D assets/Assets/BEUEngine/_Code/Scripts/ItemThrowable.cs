using UnityEngine;
using System.Collections;
/// <summary>
/// Item throwable. For all items that can be thrown. They can break too just like
/// the weapons after their condition value reaches 0.
/// </summary>
public class ItemThrowable : Base_Item, IDamageable
{
	// Place all of you different throwable types here. They are basically
	// just different names representing the object.
	enum ItemThrowableTypes
	{
		Rock = 0, Knife = 1
	}
	public GameObject brokenPrefab; // For when it shatters.
	public PhysicMaterial bounceFrictionMaterial; // It bounces off of hit characters.
	public PhysicMaterial maxFrictionMaterial; // To prevent it from sliding when on ground.
	public int Strength = 10; 
	ItemThrowableTypes myType; // What kind of throwable am I?
	// To have the object have a certain angle when thrown. Used for the knives
	// to make them straight when thrown.
	Vector3 eulerThrown;
	// Direction we were thrown in.
	Vector3 _thrownDir;
	AudioSource _myAudio;
	bool onGround = false;
	bool _thrown = false;
	float _startDrag = 2;
	// The collider for this throwable that makes it collide when thrown.
	// I use a capsule collider for this.
	CapsuleCollider _myTrigger;
	// Character who is holding/has thrown us.
	public Transform user {get; set;}

	void Awake()
	{
		// This script is needed for items so that they can vanish after
		// a set time. A common behaviour.
		flashAway = GetComponent<FlashAway> ();
		myRigidbody = GetComponent<Rigidbody> ();
		ItemType = ItemTypes.Throwable;
		_myAudio = GetComponent<AudioSource>();
	}

	void Start ()
	{
		// The drag changes after thrown so it doesn't affect it while thrown.
		_startDrag = myRigidbody.drag;
		_myTrigger = GetComponent<CapsuleCollider> ();
		if(gameObject.name.Contains("Rock"))
			myType = ItemThrowableTypes.Rock;
		else if(gameObject.name.Contains("Knife"))
		{
		    myType = ItemThrowableTypes.Knife;
			eulerThrown = new Vector3(90, 90, 0);
		}
		// I increase this so that the rigidbody will fall asleep at a higher
		// velocity. When asleep, it will stop moving.
		myRigidbody.sleepThreshold = 1;
		GetComponent<Collider>().material = maxFrictionMaterial;
	}

	void FixedUpdate ()
	{
		if(onGround)
		{
			if(_thrown)
				_thrown = false;
			// Give high friction when on the ground to prevent any sliding.
			if(GetComponent<Collider>().material != maxFrictionMaterial)
				GetComponent<Collider>().material = maxFrictionMaterial;
			if(_myTrigger.enabled)
				_myTrigger.enabled = false;
			if(myRigidbody.drag != _startDrag)
				myRigidbody.drag = _startDrag;
		}
		else
		{
			if(_thrown)
			{
				if(myType == ItemThrowableTypes.Knife)
				{
					// Set the angle for the knife so that it is thrown
					// straight. I provide this after it is thrown in the
					// WasThrown() method.
					transform.eulerAngles = eulerThrown;
				}
			}
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if(other.gameObject.tag == "Player" || other.gameObject.tag == "Enemy")
		{
			// Will only hit opposing characters/characters who don't have the
			// same tag as the user who threw it.
			if(_thrown && user != null && user.tag != other.tag)
			{
				CharacterStatus charStatus = other.gameObject.GetComponent<CharacterStatus>();
				if(charStatus.Vulnerable)
				{
					_thrown = false;
					_myTrigger.enabled = false; // No longer can hit.
					// Can now rotate in all axis.
					if(myType == ItemThrowableTypes.Knife)
						myRigidbody.constraints = RigidbodyConstraints.None;
					// Opposite velocity we currently have but a fraction of it.
					Vector3 myVel = myRigidbody.velocity * -0.4f;
					Manager_Particle.instance.CreateParticle(other.ClosestPointOnBounds(transform.position), ParticleTypes.HitSpark_Throwable, 1 + myVel.magnitude * 0.1f);
					if(_myAudio && _myAudio.clip != null)
						Manager_Audio.PlaySound(_myAudio, _myAudio.clip, true);
					// A boost of rotation speed.
					myRigidbody.AddTorque(-360, 0, 0, ForceMode.Impulse);
					// A good bounce height.
					myVel.y = 5;
					myRigidbody.velocity = myVel;
					// Damage the character who was hit. Using our velocity
					// speed we determine how much stun to add and other things...
					charStatus.StopAllCoroutines();
					charStatus.StartCoroutine(charStatus.GotHit(myVel.normalized, myVel.magnitude * 0.2f,
					myVel.magnitude * 0.1f, myVel.magnitude * 0.4f + (Strength * 0.1f), Strength + Mathf.RoundToInt(myVel.magnitude * 0.5f), 1, user, false, other.ClosestPointOnBounds(transform.position)));
					// This item takes damage based on how fast it was moving. Throwing items will always break
                    // after hitting twice since the max Condition is 100 and they take 50 damage each time they hit.
					Condition = TakeDamage(50, 50);
					if(Condition <= 0) // Broke
					{
						GameObject meBroken = Instantiate(brokenPrefab, transform.position, transform.rotation) as GameObject;
						for(int i = 0; i < meBroken.transform.childCount; i++)
						{
							Transform curChild = meBroken.transform.GetChild(i);
							Rigidbody childRigid = curChild.GetComponent<Rigidbody> ();
							if(!childRigid)
								curChild.gameObject.AddComponent<Rigidbody>();
							childRigid.velocity = myVel + Random.insideUnitSphere * 1.5f;
							childRigid.mass = myRigidbody.mass / meBroken.transform.childCount;
							childRigid.drag = _startDrag;
							childRigid.angularDrag = myRigidbody.angularDrag;
						}
						// Detach all broken pieces. Those are all that are needed
						// so we can destroy this gameObject now after destroying
						// the main broken prefab gameObject.
						meBroken.transform.DetachChildren();
						Destroy(meBroken);
						Destroy (this.gameObject);
					}
				}
			}
		}
	}

	void OnCollisionStay(Collision other)
	{
		if(transform.parent != null || myRigidbody.isKinematic)
		{
			return;
		}
		// Check to see when on the ground.

		// Check to see if we are touching an untagged object and are on top of it while not being held.
		if((other.gameObject.tag == "Untagged" || other.gameObject.tag == "Terrain") && transform.parent == null && myRigidbody.velocity.y < 0.5f)
		{
			if(other.contacts.Length > 0) // In OnCollision methods, contacts holds all the points of contact with the other object collided with.
			{
				foreach(ContactPoint touchingPoint in other.contacts)
				{
					if(touchingPoint.normal.y > 0.1f) // 1 means we are directly on a flat/nearly flat surface.
					{
						if(!onGround)
							onGround = true;
					}
					else
					{
						if(onGround)
							onGround = false;
					}
				}
			}
		}
	}
	
	void OnCollisionExit(Collision other)
	{
		// These Monobehaviour methods activate even when the script is
		// disabled so I don't want that. Also checking to make sure that
		// we are not held.
		if(!this.enabled || transform.parent != null
		   || myRigidbody.isKinematic)
			return;
		// Check to see if we are touching an untagged object and are on top of it while not being held.
		if((other.gameObject.tag == "Untagged" || other.gameObject.tag == "Terrain"))
		{
			if(onGround)
			{
				if(other.contacts.Length > 0) // In OnCollision methods, contacts holds all the points of contact with the other object collided with.
				{
					foreach(ContactPoint touchingPoint in other.contacts)
					{
						if(touchingPoint.normal.y > 0.1f) // 1 means we are directly on a flat/nearly flat surface.
							onGround = false;
					}
				}
			}
		}
	}

	// The method for all damageable objects.
	public int TakeDamage(int damage, int max)
	{
		if(damage > max) damage = max; // Shouldn't go past max.
		int condition = Condition;
		condition = Mathf.Clamp (condition - damage, 0, max);
		return condition;
	}
	// The user calls this method on this item during their throw animation
	// event.
	public void WasThrown(float thrownSpeed)
	{
		_thrownDir = transform.root.forward + Vector3.up;
		// Have the mass decrease the speed thrown a bit.
		thrownSpeed -= myRigidbody.mass;
		user = transform.root;
		if(myType == ItemThrowableTypes.Knife)
			// Make the knife aim straight in the direction the user is facing.
			eulerThrown.y = user.eulerAngles.y;
		transform.parent = null; // No longer held.
		GetComponent<Collider>().enabled = true; // Our main collider can now collide with things.
		myRigidbody.drag = 0; // Don't want drag slowing us down when thrown.
		_thrownDir.x *= thrownSpeed; _thrownDir.z *= thrownSpeed;
		myRigidbody.isKinematic = false; // Make sure this is false before updating velocity.
		myRigidbody.velocity = _thrownDir;
		_thrown = true;
		onGround = false;
		// I freeze the rotation for the knife when thrown so it always faces
		// straight.
		if(myType == ItemThrowableTypes.Knife)
			myRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
		// Allow some bounciness upon collision.
		GetComponent<Collider>().material = bounceFrictionMaterial;
		// Our trigger for hitting characters.
		_myTrigger.enabled = true;
		// 15 seconds after being thrown is when this item will start to flash
		// away and then be destroyed shortly after.
		flashAway.ResetFlashTime (15);
	}
}