using UnityEngine;
using UnityEngine.UI;
using System.Collections;
/// <summary>
/// Player Attacking. This script is for all of the attacking-related things.
/// A currently held character, combo count, hit attack sounds, and more.
/// </summary>
public class CharacterAttacking : Character
{
	public AudioClip[] voicesAtk;
	public AudioClip[] voicesAtkHeavy;
	public AudioClip[] voicesThrow; // For throwing a character.
	public AudioClip[] voicesDodge;
	// The hitbox info for all of our hitboxes. You can assign this in the
	// inspector if desired. The order they should be found in should be:
	// left leg, right leg, body part for grab mount, left forearm, right forearm.
	// I find them manually in the Awake() method if this is null. If they
	// aren't in that order then it will throw my SetupHitboxStats() off since
	// that is the order I specify which hitbox to activate based on the current
	// attack state.
	public HitboxProperties[] _myHitboxes;
	public AudioClip[] sfxsAttackHits; // The attack hit sounds.

	CharacterStatus _charStatus;
	CharacterMotor _charMotor;
	AudioSource _myAudio;

	// Animator Hashes.
	int Atk_Hea_Hi;
	int Atk_Counter;
	int Atk_Air_1;
	int Atk_Air_2;
	int Atk_Air_Hi;
	int Atk_Air_Heavy;

	public Transform grabMount; // A mount for where a held character will be kept.
	// How long we have been guarding. You can only guard for a set time before
	// you will stop guarding and not be able to guard for a few seconds.
	float _guardingTimer = 0;
	float _grabbingNoCharacterTimer; // A backup timer just in case we should ever still be grabbing but a character we grabbed died and didn't give us the signal indication. Very rare for this to happen.
    // Access to our "Hits" text group on our UI from Manager_UI.
    GameObject hitsGroup; // Holds all of the hit count group things, including the combo rating image text.
    GameObject hitsCounterGroup; // Child gameObject called Group_HitCount for holding the hit amount and the "Hits!" text.
    // This is for displaying our current combo count next to our Hits text.
    Text textHitCount;
    Image ima_ComboRating; // I use an image of text for displaying how well our combo was.
	// Our current hitbox we are using.
	public HitboxProperties myCurrentHitbox { get; set; }
	// A character we started our combo on. Our combo will only go up if we
	// are hitting this character. It will reset in ComboChange(false)
	public Transform StartedComboEnemy {get; set;}
	// Our currently selected target.
	public Transform TargetedCharacter {get; set;}
	// A character we are holding. Used for sending grabbed messages to.
	public Transform GrabbedCharacter {get; set;}
	// A delay time for when an attack hits.
	public float HitDelayTime { get; set; }
	// Our current combo count.
	public int Combo {get; set;}
	// If our attack is the final hit of the attack animation. Used for AI for
	// only making the AI attack if this is true since you would want them to
	// combo after the final hit of the attack.
	public bool FinalHit {get; set;}
	// Are we able to guard? If not, we have guarded too long and have to wait
	// a bit.
	public bool CanGuard { get; set;}
	// Has the current hitbox been setup? When this is true, it will be able
	// to hit in HitboxProperties.
	public bool SetHitbox {get; set;}
	// Prevents us from attacking in the air, going back to falling, and then
	// being able to attack in the air again before touching the ground. When
	// we touch the ground, this will reset, allowing us to attack in the air
	// again.
	public bool CanUseAirAttacks {get; private set;}
	// A simple way to determine if we can say we are attacking by checking to
	// see if we are in an attack state and are in less than 90% of the animation.
	public bool IsAttacking
	{ 
		get {
			return (anim.GetCurrentAnimatorStateInfo (0).IsTag ("Attack")
					&& _charStatus.BaseStateInfo.normalizedTime < 0.9f)
			;}
	}

	void Awake ()
	{
		_charStatus = GetComponent<CharacterStatus> ();
		_charMotor = GetComponent<CharacterMotor> ();
		anim = GetComponent<Animator> ();
		// Detection radius helps us keep track of what opposing characters
		// and items are around us.
		detectionRadius = transform.GetComponentInChildren<DetectionRadius> ();
		// Setting this for Character_Status so that it doesn't have to.
		_charStatus.detectionRadius = detectionRadius;
		Atk_Air_1 = Animator.StringToHash ("Atk_AirMed_1");
		Atk_Air_2 = Animator.StringToHash ("Atk_AirMed_2");
		Atk_Air_Hi = Animator.StringToHash ("Atk_AirMedHigh");
		Atk_Air_Heavy = Animator.StringToHash ("Atk_AirHeavy");
		Atk_Hea_Hi = Animator.StringToHash("Atk_HeavyHigh");
		Atk_Counter = Animator.StringToHash("Atk_Counter");
		myRigidbody = GetComponent<Rigidbody>();
		_myAudio = GetComponent<AudioSource>();
	}

	void Start()
	{
		// If this is null or has nothing in it, then we didn't set it up
		// in the inspector so we find all instances of it in our children
		// gameObjects.
		if(_myHitboxes == null || _myHitboxes.Length == 0)
			_myHitboxes = transform.GetComponentsInChildren<HitboxProperties> ();
		CanGuard = true;
		myCurrentHitbox = _myHitboxes [0]; // Default so that one is set.
		CanUseAirAttacks = true;
	}

	void Update ()
	{
		if(!anim.IsInTransition(0))
		{
			if(_charStatus.BaseStateInfo.IsName("Guarding"))
			{
				// We can guard up to 3 seconds before losing our guard.
				_guardingTimer = Mathf.Clamp(_guardingTimer + Time.deltaTime, 0, 3);
				if(_guardingTimer >= 3)
				{
					// Now we have to wait a bit before we can guard again.
					// This is done so you can't spam/hold guards for a long time.
					_guardingTimer = 3;
					anim.SetBool("IsGuarding", false);
					CanGuard = false;
				}
			}
			// If we are in our grab idle state, we will check to make sure that we have a character
			// grabbed, and if not, set off our timer to reset us from grabbing. This is very rare.
			if (_charStatus.BaseStateInfo.IsName("Grab_Idle") && _charStatus.BaseStateInfo.normalizedTime > 0.3f)
			{
				if (!GrabbedCharacter)
				{
					if (_grabbingNoCharacterTimer < 3)
						_grabbingNoCharacterTimer += Time.deltaTime;
					else
					{
						_grabbingNoCharacterTimer = 0;
						Debug.Log("EscapeGrabNoCharacter for " + gameObject.name);
						anim.SetBool("IsGrabbing", false); // Stop grabbing animation.
					}
				}
			}
			// Not busy states include: idles, locomotion, and the jumping blend
			// tree.
			if(_charStatus.BaseStateInfo.IsTag("NotBusy"))
			{
				// Here we reset our guardingTimer. It will reset faster if
				// we can still guard, meaning we didn't hold it too long.
				float bonus = 2;
				if(!CanGuard)
					bonus = 1;
				_guardingTimer = Mathf.Clamp(_guardingTimer - bonus * Time.deltaTime, 0, 3);
				if(!CanGuard)
				{
					if(_guardingTimer <= 0)
					{
						CanGuard = true;
						_guardingTimer = 0;
					}
				}
				// We landed after using an air attack so we can use them again.
				if(!CanUseAirAttacks && _charMotor.onGround)
					CanUseAirAttacks = true;
				// If combo manages to not reset by the time we get back into
				// idle or locomotion, then it will do so here. That could happen
				// from killing multiple opposing characters at once.
				if(Combo > 0 && !_charStatus.BaseStateInfo.IsName("Jumping"))
					ComboChange(false);
			}
		}
		else
		{
			// If we are not going into an attacking state.
			if(!_charStatus.BaseNextStateInfo.IsTag("Attack"))
			{
				// Reset parameters
				if(anim.GetInteger ("AttackUsed") != 0)
					anim.SetInteger("AttackUsed", 0);
				if(anim.GetBool("UsedLow"))
					anim.SetBool("UsedLow", false);
				if(anim.GetBool("UsedHigh"))
					anim.SetBool("UsedHigh", false);
				DisableAttackCollider();
				if (_charStatus.BaseNextStateInfo.IsTag("Dodging"))
				{
					// We aren't vulnerable when dodging. I have our character
					// change color during this time to show that.
					if (_charStatus.Vulnerable)
					{
						_charStatus.Vulnerable = false;
						_charStatus.StartMaterialColorChange(2, ColorChangeTypes.Is_Vulnerable);
					}
				}
				else if (_charStatus.BaseNextStateInfo.IsTag("NotBusy"))
				{
					// Dodging is about to end so make us Vulnerable again and
					// reset our color.
					if (_charStatus.BaseStateInfo.IsTag("Dodging") && !_charStatus.Vulnerable)
					{
						_charStatus.Vulnerable = true;
						_charStatus.StartMaterialColorChange(2, ColorChangeTypes.Is_Vulnerable);
					}
					if (!_charMotor.onGround) // Currently in the air.
					{
						// We have exited our attack animation in the air and
						// have not landed yet so make sure we can't keep using
						// air attacks until landing.
						if (_charStatus.BaseStateInfo.IsTag("Attack") && CanUseAirAttacks)
						{
							if (anim.GetFloat("VertVel") < 4)
								CanUseAirAttacks = false;
							else
								CanUseAirAttacks = true;
						}
					}
				}
			}
			else
			{
				// Using our low and high attacks so that we can't go back into
				// those animations until our combo is over.
				if(_charStatus.BaseNextStateInfo.IsName("Atk_MedLow"))
					if(!anim.GetBool("UsedLow"))
						anim.SetBool("UsedLow", true);
				if(_charStatus.BaseNextStateInfo.IsName("Atk_MedHigh"))
					if(!anim.GetBool("UsedHigh"))
						anim.SetBool("UsedHigh", true);
			}
			// We reset this simply anytime we go into a transition.
			if(anim.GetBool("AttackHit"))
				anim.SetBool("AttackHit", false);
		}

		// Next is where we move and rotate towards a targeted character when
		// we are attacking. If we are in our slide animation, we only do this
		// code when we are less than 40% in the animation.
		if(!_charStatus.BaseStateInfo.IsName ("Atk_Slide") || _charStatus.BaseStateInfo.normalizedTime < 0.4f)
		{
			if(TargetedCharacter != null)
			{
				// If we are in our Grab, Guard, or Throwing state I do this too,
				// but only for the rotation, not movement.
				// If we aren't in any of those states then we must be in an
				// attack state if we get past this because I check to see that
				// our current hitbox is active or if we are in the air and haven't
				// hit an opposing character yet.
				if((_charStatus.BaseStateInfo.IsName("Grab") || _charStatus.BaseStateInfo.IsTag("Guard")
				   || _charStatus.BaseStateInfo.IsName("Throw")) || IsAttacking
				 || (!_charMotor.onGround && !anim.GetBool("AttackHit")))
				{
					Vector3 dir = (TargetedCharacter.position - transform.position);
					if(_charStatus.BaseStateInfo.IsTag("Attack"))
					{
						// Add a small amount of force to move towards our target.
						float dist = Vector3.Distance(transform.position, TargetedCharacter.position);
						// I use dist to determine how much force to add
						// keeping it at a minimum of 0.1f
						dist = Mathf.Clamp(dist, 0.3f, 1.2f);
						float amount = 150;
						if(!_charMotor.onGround)
							amount = 60; // Less force is applied in the air.
						else dir.y = 0;
						// Make sure we aren't in our slide animation since
						// that already has its own movement.
						if(!_charStatus.BaseStateInfo.IsName("Atk_Slide"))
							myRigidbody.AddForce(new Vector3((dir.normalized.x * amount) * dist, 0, (dir.normalized.z * amount) * dist));
					}
					// Actually rotate towards our target.
					_charMotor.ManualRotate(dir, false);
				}
			}
		}
		// We must have hit someone if this is greater than 0 so
		// our animation freezes for however long HitDelayTime is.
		// It's a very small amount.
		if(HitDelayTime > 0)
		{
			HitDelayTime -= Time.deltaTime;
		}
		else
		{
			if(HitDelayTime != 0)
				HitDelayTime = 0;
			// Character motor is in charge of the animator's speed.
			if(anim.speed == 0)
				_charMotor.UpdateAnimSpeed ();
		}
	}
	// Here is where our hitboxes get stats sent to them on strength,
	// scoreToAdd, etc, right before they are activated.
	void SetupHitboxStats()
	{
		// Disable any active attack colliders.
		DisableAttackCollider ();
		// 0 = regular hit, 1 = heavy hit, 2 = knock down
		int hurtOther = 0;
		// This gets added to the user's strength for the overall
		// power of the attack.
		int atkPower = 1;
		int scoreAdd = 5;
		float hitHeight = 0; // The y position of the hit direction
		// How long to be in hit delay after the attack hits.
		float hitDelayTime = 0.1f;
		// How much time to be stunned for after being pushed from the attack
		// has ended.
		float stunTime = 0.7f;
		float stunAdd = 1; // How much stun to add from this attack. Characters will be stunned after reaching over 8 stun
		// How far in the hit direction to be pushed. Right now I just have the
		// hit direction defaulted to being pushed relative to the character's
		// positions. More info on that in the CharacterStatus GotHit() coroutine.
		float hitForce = 0.2f;
		// Any size change for the particle that gets created? 1 is default normal
		// 100% of its actual size.
		float partSizeRatio = 1;
		AudioClip sfxHit = sfxsAttackHits [0];
		HitboxProperties hitboxUsing = _myHitboxes [4]; // Right forearm default
		ParticleTypes partToCreate = ParticleTypes.HitSpark_Normal;

		// Next, set up each attack state's stats however you want. Make sure to
		// set the correct hitbox to use for each attack. Check your character's
		// attack animation to see which one you want to use.
		if(_charStatus.BaseStateInfo.IsName("Atk_Light_2"))
		{
			hitboxUsing = _myHitboxes[3]; // Left forearm
		}
		else if(_charStatus.BaseStateInfo.IsName("Atk_Light_3"))
		{
			atkPower = 2;
			hitDelayTime += 0.02f; hitForce += 0.1f; hitHeight += 0.1f;
			stunTime += 0.05f; stunAdd += 0.3f;
		}
		else if(_charStatus.BaseStateInfo.IsName("Atk_MedHigh"))
		{
			atkPower = 4;
			hitDelayTime += 0.05f; hitHeight += 3;
			scoreAdd += 3; stunAdd += 0.5f;
			partSizeRatio = 1.25f;
		}
		else if(_charStatus.BaseStateInfo.shortNameHash == Atk_Hea_Hi)
		{
			atkPower = 5;
			hitDelayTime += 0.05f; hitForce += 0.2f; hitHeight = 7;
			scoreAdd += 3; hurtOther = 1; stunAdd += 0.6f;
			partSizeRatio = 1.25f;
		}
		else if(_charStatus.BaseStateInfo.IsName("Atk_MedLow"))
		{
			atkPower = 4;
			hitboxUsing = _myHitboxes[1]; // right leg
			hitDelayTime += 0.05f; hitForce += 0.25f; hitHeight += 0.2f;
			stunTime += 0.1f; scoreAdd += 3; hurtOther = 1; stunAdd += 0.5f;
			partSizeRatio = 1.25f;
		}
		else if(_charStatus.BaseStateInfo.IsName("Atk_Heavy_N"))
		{
			atkPower = 7; hitboxUsing = _myHitboxes[3];
			hitDelayTime += 0.1f; hitForce += 2; hitHeight += 3;
			stunTime += 0.7f; scoreAdd += 5; hurtOther = 2; stunAdd ++;
			partSizeRatio = 1.5f;
		}
		else if(_charStatus.BaseStateInfo.shortNameHash == Atk_Air_1)
		{
			atkPower = 3; hitboxUsing = _myHitboxes[0]; // left leg
			hitDelayTime += 0.12f; hitForce += 0.3f; hitHeight += 4;
			stunTime += 0.1f; scoreAdd += 5; stunAdd += 0.5f;
			partSizeRatio = 1.25f;
		}
		else if(_charStatus.BaseStateInfo.shortNameHash == Atk_Air_2)
		{
			atkPower = 3; hitboxUsing = _myHitboxes[1];
			hitDelayTime += 0.12f; hitForce += 0.3f; hitHeight += 4;
			stunTime += 0.15f; scoreAdd += 3; hurtOther = 1; stunAdd += 0.5f;
			partSizeRatio = 1.25f;
		}
		else if(_charStatus.BaseStateInfo.shortNameHash == Atk_Air_Hi)
		{
			atkPower = 3; hitboxUsing = _myHitboxes[1];
			hitDelayTime += 0.13f; hitForce += 0.3f; hitHeight += 4.5f;
			stunTime += 0.15f; scoreAdd += 3; hurtOther = 1; stunAdd += 0.5f;
			partSizeRatio = 1.3f;
		}
		else if(_charStatus.BaseStateInfo.shortNameHash == Atk_Air_Heavy)
		{
			atkPower = 5; hitboxUsing = _myHitboxes[3];
			hitDelayTime += 0.22f; hitForce += 0.75f; hitHeight = -8;
			stunTime += 0.8f; scoreAdd += 6; hurtOther = 2; stunAdd ++;
			partSizeRatio = 1.6f;
		}
		else if(_charStatus.BaseStateInfo.IsName("Atk_Counter"))
		{
			atkPower = 6; hitboxUsing = _myHitboxes[3];
			hitDelayTime += 0.2f; hitForce += 1.2f; hitHeight += 4;
			stunTime += 0.7f; scoreAdd += 7; hurtOther = 2; stunAdd += 1.25f;
            partSizeRatio = 1.5f;
		}
		else if(_charStatus.BaseStateInfo.IsName("Atk_Slide"))
		{
			atkPower = 7; hitboxUsing = _myHitboxes[1];
			hitDelayTime += 0.2f; hitForce += 1.2f; hitHeight += 4.5f;
			stunTime += 0.6f; scoreAdd += 6; hurtOther = 2; stunAdd ++;
			partSizeRatio = 1.3f;
		}
		else if(_charStatus.BaseStateInfo.IsName("Grab_Hit"))
		{
			hitboxUsing = _myHitboxes[1]; hitForce = 0;
		}
		// I made the enemy's attacks have a little less effect so the
		// game isn't overly difficult.
		if (IsEnemy)
		{
			stunTime *= 0.7f;
			atkPower--;
		}
		myCurrentHitbox = hitboxUsing;
		// Make sure the hitbox is disabled. We activate it during the
		// animation event of the attack animation.
		myCurrentHitbox.myTriggerCol.enabled = false;
		// update our hitboxes' properties for this attack we are using.
		hitboxUsing.hurtOther = hurtOther; hitboxUsing.strength = _charStatus.Stats[2] + atkPower;
		hitboxUsing.scoreAdd = scoreAdd; hitboxUsing.hitHeight = hitHeight;
		hitboxUsing.hitDelayTime = hitDelayTime; hitboxUsing.stunTime = stunTime;
		hitboxUsing.hitForce = hitForce; hitboxUsing.partSizeRatio = partSizeRatio;
		hitboxUsing.sfxHit = sfxHit; hitboxUsing.particleToCreate = partToCreate;
		hitboxUsing.stunAdd = stunAdd;
		SetHitbox = true;
	}

	void DisableAttackCollider()
	{
		// Reset all hitbox colliders so they can't hit.
		foreach(HitboxProperties hitbox in _myHitboxes)
			hitbox.myTriggerCol.enabled = false;
	}

	// Are we currently in an air attack?
	public bool IsInAirAttack()
	{
		return _charStatus.BaseStateInfo.shortNameHash == Atk_Air_1 || _charStatus.BaseStateInfo.shortNameHash == Atk_Air_2
				|| _charStatus.BaseStateInfo.shortNameHash == Atk_Air_Hi || _charStatus.BaseStateInfo.shortNameHash == Atk_Air_Heavy;
	}

	// This gets called whenever we are hit.
	public void ResetParameters()
	{
		anim.SetBool ("IsGuarding", false);
		anim.SetInteger ("AttackUsed", 0);
		DisableAttackCollider ();
		myCurrentHitbox = _myHitboxes [0];
        if (!IsEnemy && hitsCounterGroup.activeSelf)
            hitsCounterGroup.SetActive(false); // Our combo was interrupted so stop displaying it.
		SetHitbox = false;
        HitDelayTime = 0;
	}
	// This gets called at the creation of this character.
	public void CreatedSetup(int myPlayNumber)
	{
		// Find our groups in the Manager_UI for our hit counter.
		hitsGroup = Manager_UI.instance.plHitsGroup[myPlayNumber - 1];
		textHitCount = Manager_UI.instance.plHitCount[myPlayNumber - 1];
        string nameOfObject = "Group_HitCount_P" + myPlayNumber.ToString();
        Transform hitCountGroup = hitsGroup.transform.FindChild(nameOfObject);
        if (hitCountGroup)
            hitsCounterGroup = hitCountGroup.gameObject;
        else
            Debug.LogWarning("DID NOT FIND HIT COUNT GROUP FOR Player " + myPlayNumber);
        ima_ComboRating = Manager_UI.instance.plComboRatingImage[myPlayNumber - 1];
	}

	public void AttackSetup(int attackUsed, float vertDir)
	{
		// We can't attack if we are hurt, or attempting to use an air attack when we
		// aren't able to while in the air.
		if(anim.GetBool("IsHurt") || (!CanUseAirAttacks && !_charMotor.onGround))
			return;

		anim.SetInteger ("AttackUsed", attackUsed);
		anim.SetFloat ("VertDir", vertDir);
		anim.SetTrigger ("AttackFired"); // Trigger for going into attack animations, and our grab.
	}

	public void GrabAttackSetup(int attackUsed)
	{
		if(GrabbedCharacter != null)
		{
			anim.SetInteger("AttackUsed", attackUsed);
			anim.SetTrigger("AttackFired");
		}
	}
	// Dodging can be related to attacking so I put it here.
	public void DodgeSetup(float vertDir, float horDir)
	{
		// You can dodge after landing an attack, dodge while in a guard state unless you are 
		// AI controlled, or also dodge if in locomotion and moving
		// faster than 0.4 which would be walking.
		if((anim.IsInTransition(0) && _charStatus.BaseNextStateInfo.IsTag("Dodging"))
			|| ((!_charStatus.BaseStateInfo.IsName("Guarding") && !_charStatus.AIOn)
		   && (!_charMotor.IsInLocomotion() || _charMotor.animMove < 0.4f)
				&& !anim.GetBool("AttackHit")))
			return;

		// Set our dodge directions to determine which dodge to do.
		anim.SetInteger("DodgeDirVert", Mathf.RoundToInt (vertDir));
		anim.SetInteger("DodgeDirHor", Mathf.RoundToInt (horDir));
		anim.SetTrigger ("DodgeFired"); // Do the dodge.
	}
	// Reset or add to our combo. Enable and disable our hit count group
	// for human players so we can see how many hits we have gotten in a row.
	public void ComboChange(bool add)
	{
		if(add)
		{
			Combo++;
			if(!_charStatus.IsEnemy)
			{
				textHitCount.text = Combo.ToString ();
                if (Combo > 1 && !hitsGroup.activeSelf)
                    hitsGroup.SetActive(true);
                else if (Combo < 2 && hitsCounterGroup.activeSelf)
                    hitsCounterGroup.SetActive(false); // Do not display when we only have one hit in the combo.
                if (!hitsCounterGroup.activeSelf)
                    hitsCounterGroup.SetActive(true);
                if (ima_ComboRating.gameObject.activeSelf)
                {
                    CancelInvoke("EndDisplayRating");
                    StopCoroutine("ComboRatingFlash"); // Just in case this is going off.
                    ima_ComboRating.gameObject.SetActive(false);
                }
			}
		}
		else // End combo
		{
			if(!_charStatus.IsEnemy)
			{
				if(_charStatus.PlayerNumber == 1)
				{
					if(Combo > Manager_Game.P1MaxCombo)
						Manager_Game.P1MaxCombo = Combo;
				}
				else if(_charStatus.PlayerNumber == 2)
				{
					if(Combo > Manager_Game.P2MaxCombo)
						Manager_Game.P2MaxCombo = Combo;
				}
                else if(_charStatus.PlayerNumber == 3)
                {
                    if(Combo > Manager_Game.P3MaxCombo)
                        Manager_Game.P3MaxCombo = Combo;
                }
                else if(_charStatus.PlayerNumber == 4)
                {
                    if(Combo > Manager_Game.P4MaxCombo)
                        Manager_Game.P4MaxCombo = Combo;
                }
				textHitCount.text = "";
                hitsCounterGroup.SetActive(false); // Disable this group since is isn't needed now.
                if (Combo > 2)
                {
                    if (!ima_ComboRating.gameObject.activeSelf)
                        ima_ComboRating.gameObject.SetActive(true); // Show our combo image rating text.
                    // A combo from 3-4 hits will be the first image (Good), 5-6 hits will be 2nd (Nice!),
                    // 7-9 hits will be 3rd (Rad!), and the max is 10 hits (Epic!!)
                    ima_ComboRating.sprite = Manager_UI.instance.sprComboRatings[Combo < 5 ? 0 : Combo < 7 ? 1 : Combo < 10 ? 2 : 3];
                    if(Combo > 4) // "Nice!" or better
                        StartCoroutine(ComboRatingFlash(Combo)); // Make the image text's main color change from white to gray.
                    Invoke("EndDisplayRating", 3);
                }
			}
			Combo = 0;
			StartedComboEnemy = null;
		}
	}

    // Gets invoked from right above to stop displaying our text rating and the hit count group itself.
    void EndDisplayRating()
    {
        ima_ComboRating.gameObject.SetActive(false);
        hitsCounterGroup.SetActive(true); // Default to show when activating the hitsGroup main gameObject again as it shows how many hits we are giving in combo.
        if(hitsGroup.activeSelf)
            hitsGroup.SetActive(false);
    }

    // Flash image text for combo ratings between two main colors based on a given time taken.
    // The higher you put for the float variable, rate, the slower the color will change.
    IEnumerator ComboRatingFlash(int curCombo)
    {
        float rate = Combo < 7 ? 0.5f : Combo < 10 ? 0.35f : 0.15f;
        while (ima_ComboRating.gameObject.activeSelf)
        {
            ima_ComboRating.color = Color.Lerp(Color.white, Color.gray, Mathf.PingPong(Time.time, rate) / rate);
            yield return new WaitForSeconds(0.0001f);
        }
        StopCoroutine("ComboRatingFlash");
    }
	// We lost our grab.
	public void GrabBroken()
	{
		anim.SetBool("IsGrabbing", false);
		GrabbedCharacter = null;
		// I freeze the characters after they have grabbed someone so
		// we unfreeze them here.
		myRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
		ComboChange (false);
	}

	public bool IsInGrab()
	{
		return _charStatus.BaseStateInfo.IsTag("Grab") || _charStatus.BaseStateInfo.IsName("Grab_Hit");
	}
	// For players to reset their target cursor in case they had an enemy
	// targeted who just died.
	public void TargetChange(Transform manualTarget = null)
	{
		Manager_Targeting.instance.TargetACharacter (_charStatus.PlayerNumber, this);
	}

	// Here is where our hitbox gets activated.
	// activating: 0 = disable, 1 = not full strength hit, 2 = full strength hit
	public void AnimEventActivateHitbox(int activating)
	{
		if(_charStatus.BaseStateInfo.IsName("Grab"))
			myCurrentHitbox = _myHitboxes[2]; // Our grab hitbox. I have this on the spine
		if(activating == 2) // Only setup hitbox stats at the start of the activation.
		{
			if(myCurrentHitbox) // Make sure we have a hitbox set.
			{
				// reset it
				myCurrentHitbox.myTriggerCol.GetComponent<Collider>().enabled = false;
				myCurrentHitbox.RemoveCharactersHit();
			}
			// Our grab hitbox doesn't need attack parameters like regular
			// attacks do.
			if(!_charStatus.BaseStateInfo.IsName("Grab"))
				SetupHitboxStats();
			else DisableAttackCollider ();
		}
		// Now, if activating is greater than 0, we enable our hitbox after
		// setting up its full strength parameter as well as stats from above
		// which get set once when activating == 2 during the start of the attack.
		if(myCurrentHitbox)
		{
			myCurrentHitbox.fullStrength = activating == 2;
			myCurrentHitbox.myTriggerCol.enabled = activating > 0;
		}
		if(activating == 0)
			SetHitbox = false;
	}

	// This gets called on our throw animation for throwing characters.
	// We unfreeze our constraints and tell our character we have grabbed
	// that they were thrown.
	public void AnimEventThrowCharacter()
	{
		anim.SetBool("IsGrabbing", false);
		GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
		if(GrabbedCharacter)
		{
			CharacterStatus pStatus = GrabbedCharacter.GetComponent<CharacterStatus>();
			// I just use 10 for a default throw damage.
			pStatus.StartCoroutine(pStatus.WasThrown(_charStatus.Stats[2] + 9, myTransform));
		}
		GrabbedCharacter = null; // No longer have a grabbed character.
		if (voicesThrow.Length > 0)
			Manager_Audio.PlaySound(_myAudio, voicesThrow[Random.Range(0, voicesThrow.Length)]);
	}

	// Gets called from an attack animation. An int is passed in to choose which attack voices to choose from.
	// Since some animations are used for multiple states, such as my attack high one, I check to see which
	// state we are in first.
	public void AnimEventVoiceAtk(int collection)
	{
		if (_charStatus.BaseStateInfo.shortNameHash == Atk_Hea_Hi || _charStatus.BaseStateInfo.shortNameHash == Atk_Counter)
			collection = 1;
		// Note that I did not use voice clips for the weakest attacks.
		// 0 = regular attack voice clips. 1 = heavy attack voice clips. Return if
		// none are found on this character for our chosen collection.
		if((collection == 0 && voicesAtk.Length == 0) || (collection == 1 && voicesAtkHeavy.Length == 0))
			return;
		AudioClip voiceClip = null;
		if (collection == 0)
			voiceClip = voicesAtk[Random.Range(0, voicesAtk.Length)];
		else if (collection == 1)
			voiceClip = voicesAtkHeavy[Random.Range(0, voicesAtkHeavy.Length)];
		if (voiceClip != null)
			Manager_Audio.PlaySound(_myAudio, voiceClip);
	}

	// Dodge animations calls this to play a voice clip when dodging.
	public void AnimEventVoiceDodge()
	{
		if (voicesDodge.Length == 0)
			return;
		int indexUsing = Random.Range(0, voicesDodge.Length - 2);
		// This next check is to see if we air evaded since the left and right dodges I use are the same
		// one used for the air evade so we just check to see if we are off the ground. If this is true,
		// I choose a set index, which I used a particular voice clip for where Ethan says "Whoa!"
		// Of course this only applies if the character is Ethan. The last index is the one used for this.
		if (!_charMotor.onGround && (_charStatus.BaseStateInfo.IsName("DodgeLeft") || _charStatus.BaseStateInfo.IsName("DodgeRight")))
			indexUsing = voicesDodge.Length - 1;
		Manager_Audio.PlaySound(_myAudio, voicesDodge[indexUsing]);
	}
}
