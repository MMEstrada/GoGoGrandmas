using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// Hitbox properties. Here is where all of our hitboxes have their parameters
/// for being used. This script gets placed on all hitbox body parts. That also
/// counts the grabbing one. No parameter setup is needed for that one though.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class HitboxProperties : MonoBehaviour
{
	// Add more strength to the attack if hit during its strongest point.
	public bool fullStrength = true;
	// What type of hit is this? 0 = normal, 1 = heavy hit, 2 = knock down.
	public int hurtOther {get; set;}
	public int strength {get; set;}
	public int scoreAdd {get; set;}
	public AudioClip sfxHit {get; set;}
	public float hitHeight {get; set;} // The y position of the hit direction
	public float hitDelayTime {get; set;} // Time to freeze animation upon hit.
	public float stunTime {get; set;} // Stun time after finished being pushed.
	public float stunAdd { get; set; }
	public float hitForce {get; set;} // Force applied to the hit direction.
	public float partSizeRatio {get; set;} // This goes along with the particleToCreate. 1 is regular size. 1.5f would be 50% more than regular size.
	public bool finalHit = true; // The final hit of the attack? Can be used for AI so that they only combo after this is true.
	public ParticleTypes particleToCreate {get; set;} // Particle to create upon hit

	List<GameObject> charactersHit; // List of all characters hit by the attack. Upon contact they will be added so that they can't be hit by the same attack again until removed.
	bool _isGrabBox = false; // Is this actually a grab hitbox?
    float _sfxHitVol = 1; // Volume for playing sfxHit. Weapons change this to their audioSource's volume in Start().
	ItemWeapon _itemWeapon; // Only used for weapons.
	Rigidbody _myRigidbody;

	// These next four are used by CharacterItem in its AnimEventUseWeapon method, so they are public.
	public Transform Attacker {get; set;} // Who was the attacker?
	public Animator Anim {get; set;} // The Animator of the user of this hitbox.
	public CharacterStatus CharStatus {get; set;} // References to these so they don't need to be gotten each time an attack hits.
	public CharacterAttacking CharAttacking {get; set;}

    public float defHitForce {get; private set;} // This is used with weapons so CharacterItem can access them when using a weapon to change it while maintaining its default values.

	public Collider myTriggerCol {get; private set;} // What kind of collider is being used.

	void Awake()
	{
		if(gameObject.tag != "Item")
		{
			Attacker = transform.root;
			CharStatus = Attacker.GetComponent<CharacterStatus> ();
			CharAttacking = Attacker.GetComponent<CharacterAttacking>();
			_myRigidbody = Attacker.GetComponent<Rigidbody>();
			myTriggerCol = GetComponent<Collider>();
			Anim = transform.root.GetComponent<Animator> ();
		}
		else // Must be a weapon. _attacker will get set by the user.
		{
			myTriggerCol = gameObject.GetComponent<CapsuleCollider>() as CapsuleCollider;
			_itemWeapon = GetComponent<ItemWeapon>();
		}
		if(gameObject.tag == "GrabHitbox")
			_isGrabBox = true;
		charactersHit = new List<GameObject> ();
	}

	void Start()
	{
		// For items, only weapons will have this hitbox script attached to them.
		if(gameObject.tag == "Item")
		{
			// Give the weapon some setups for the parameters.
			ItemWeapon itemWeapon = GetComponent<ItemWeapon>();
			strength = itemWeapon.Strength; stunAdd = 2 + Mathf.RoundToInt(strength * 0.1f);
			stunTime = itemWeapon.Condition * 0.01f; hitDelayTime = 0.15f; partSizeRatio = 1;
			scoreAdd = 8; hurtOther = 1; particleToCreate = ParticleTypes.HitSpark_Weapon;
			if(itemWeapon.myWeaponType == ItemWeapon.WeaponTypes.Sword)
				hitForce = 1;
			else hitForce = 0.8f;
            defHitForce = hitForce;
            AudioSource itemAudio = GetComponent<AudioSource>();
            sfxHit = itemAudio.clip;
            _sfxHitVol = itemAudio.volume;
		}
	}

	void Update ()
	{
		if(!myTriggerCol.enabled)
		{
			// Any hit characters can now be hit by this hitbox again.
			RemoveCharactersHit();
		}
	}

	void OnTriggerStay (Collider other)
	{
		if(!this.enabled || !myTriggerCol.enabled || CharAttacking == null
		   || Anim == null || CharStatus == null)
			return;
		
		if(other.transform != transform.root) // Make sure we don't hit ourselves!
		{
			if((other.gameObject.tag == "Enemy" && transform.root.tag == "Player")
			   || (other.gameObject.tag == "Player" && transform.root.tag == "Enemy")) // If the hitbox collided with an enemy and a player hit them.
			{
				CharacterStatus charStatus = other.gameObject.GetComponent<CharacterStatus>();
				if(_isGrabBox)
				{
					if(!CharAttacking.GrabbedCharacter && !Anim.GetBool("IsHurt"))
					{
						// Attempt to grab. Make sure they aren't already
						// grabbed.
						if(charStatus.Vulnerable && other.gameObject.GetComponent<Animator>().GetInteger("HurtOther") != 4)
						{
							charStatus.StopAllCoroutines();
							// Grab successful! We pass in this grabMount
							// so they can be attached to it.
							charStatus.StartCoroutine(charStatus.WasGrabbed(CharAttacking.grabMount));
							CharAttacking.GrabbedCharacter = other.transform;
							myTriggerCol.enabled = false;
							_myRigidbody.velocity = Vector3.zero;
							// We go into our grab state.
							Anim.SetBool("IsGrabbing", true);
						}
					}
					return;
				}
				// SetHitbox isn't needed for grabbing (above) since you
				// don't need to set up anything for the hitbox.
				if(!charactersHit.Contains(other.gameObject) && CharAttacking.SetHitbox
					&& (!other.isTrigger || other is BoxCollider))
				{
					if(charStatus.Vulnerable) // Can't hit if they aren't vulnerable
					{
						charactersHit.Add (other.gameObject);
						// If our fullStrength variable is true, then add an
						// additional attack strength point.
						int fullAdd = (fullStrength == true) ? 1 : 0;
						// This finds the closest point of contact between
						// this hitbox and the other character's collider.
						Vector3 hitPos = other.ClosestPointOnBounds(transform.position);
						bool guarded = false;
						Vector3 dir = -other.transform.forward;
						if(Attacker != null)
							dir = (Attacker.position - other.transform.position).normalized;
						float direction = Vector3.Dot(dir, other.transform.forward);
                        // If this is a weapon, _itemWeapon will not be null. Putting this check here
                        // so that we can't block against a weapon attack.
                        if(charStatus.BaseStateInfo.IsTag("Guard") && !_itemWeapon)
						{
							// They blocked this hitbox as the user of this
							// hitbox and the victim both are facing close
							// enough at each other.
							if(direction > 0.3f)
							{
								guarded = true;
								particleToCreate = ParticleTypes.HitSpark_Guard;
								sfxHit = Manager_Audio.instance.sfxsGuardHit[0];
							}
						}
						if(CharAttacking.GrabbedCharacter == null
						   || CharAttacking.GrabbedCharacter != other.transform)
							charStatus.StopAllCoroutines();
						Vector3 hitDir = new Vector3(Attacker.forward.x, hitHeight, Attacker.forward.z);
						charStatus.StartCoroutine(charStatus.GotHit(hitDir,
						   hitForce, stunTime, stunAdd, Mathf.RoundToInt(strength + CharStatus.Stats[2] + fullAdd), (int)hurtOther,
						   Attacker, guarded, hitPos, false, CharAttacking));
						CharAttacking.HitDelayTime = hitDelayTime; // Pass in the time to delay for, for the character who hit the enemy.
						CharAttacking.FinalHit = finalHit;
						if(sfxHit)
                            AudioSource.PlayClipAtPoint(sfxHit, transform.position, _sfxHitVol);
						Manager_Particle.instance.CreateParticle(hitPos, particleToCreate, partSizeRatio);
						// If this is a weapon.
						if(gameObject.tag == "Item")
							_itemWeapon.StruckHit(Random.Range(5, 15));
						if(guarded) // They successfully guarded this attack.
							return; // No combo or attack hit then.
						Anim.SetBool("AttackHit", true);
						if(transform.root.tag == "Player")
						{
							// A hit was given. So keep track for when we
							// are graded at the end of the stage.
							if(CharStatus.PlayerNumber == 1)
								Manager_Game.P1HitsGiven++;
							else if(CharStatus.PlayerNumber == 2)
								Manager_Game.P2HitsGiven++;
                            else if(CharStatus.PlayerNumber == 3)
                                Manager_Game.P3HitsGiven++;
                            else if(CharStatus.PlayerNumber == 4)
                                Manager_Game.P4HitsGiven++;
						}
                        if(!Anim.GetBool("OnGround") && _myRigidbody)
						{
                            _myRigidbody.velocity = Vector3.zero;
							if(hitHeight > 4)
								hitHeight = 4;
							// Add a little force to us to push us up when
							// hitting in the air.
							_myRigidbody.AddForce(0, Mathf.Abs(hitHeight), 0, ForceMode.VelocityChange);
						}
						// We scored some points.
						if(transform.root.tag == "Player")
							Manager_Game.instance.ScoreUpdate(CharStatus.PlayerNumber, (int)scoreAdd);
						// Set up the other character for our combo starter
						// if we don't have one.
						if(CharAttacking.StartedComboEnemy == null)
							CharAttacking.StartedComboEnemy = other.transform;
						// If they are our combo starter character, then we
						// gained a combo hit.
						if(CharAttacking.StartedComboEnemy == other.transform)
							CharAttacking.ComboChange(true);
					}
				}
			}
			// Hitting an item box/crate
			else if(other.gameObject.tag == "ItemStorer" && !other.isTrigger)
			{
				ItemStorer itemStorer = other.gameObject.GetComponent<ItemStorer>();
				itemStorer.GotHit(strength);
				CharAttacking.HitDelayTime = hitDelayTime; // Pass in the time to delay for, for the character who hit this item box.
				CharAttacking.FinalHit = finalHit;
				if(sfxHit)
					AudioSource.PlayClipAtPoint(sfxHit, transform.position);
				Manager_Particle.instance.CreateParticle(other.ClosestPointOnBounds(transform.position), particleToCreate, partSizeRatio);
				Anim.SetBool("AttackHit", true);
				// Stop us from moving.
                if(_myRigidbody && !_myRigidbody.isKinematic)
				    _myRigidbody.velocity = Vector3.zero;
			}
		}
	}

	public void RemoveCharactersHit()
	{
		if(charactersHit.Count > 0)
			charactersHit.Clear();
	}
}