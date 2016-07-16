using UnityEngine;
using System.Collections;
/// <summary>
/// Player input. For human characters. All input is done here. That doesn't
/// count when you are trying to escape a grab in Character_Status.
/// </summary>
[RequireComponent(typeof (CharacterMotor))]
public class PlayerInput : MonoBehaviour
{
	public bool walkByDefault = true; // toggle for walking state

	private Vector3 lookPos; // The position that the character should be looking towards. Used here for setting up in Player Motor
	private CharacterStatus _charStatus;
	private CharacterMotor _charMotor; // A reference to the ThirdPersonCharacter on the object
	private CharacterAttacking _charAttacking;
	CharacterItem _charItem;
	private Transform cam; // A reference to the main camera in the scene.
	private Vector3 camForward; // The current forward direction of the camera
	
	private Vector3 move; // the world-relative desired move direction, calculated from the camForward and user input.
	private bool jump; // If the jump button is pressed.
	private Animator _anim;
	private DetectionRadius _detectionRadius;
	private float vertDir;
	private float horDir;
	private float actionButtonHeldTimer;
	private float guardButtonHeldTimer;
	private float targetButtonHeldTimer;
	// The different button names. We assign each player's version depending on
	// which player number uses this script.
	private string inputHor;
	private string inputVert;
	private string inputHorJS;
	private string inputVertJS;
	private string inputAction;
	private string inputTarget;
	private string inputJump;
	private string inputRun;

	// I made guard public for Character_Status to use it for escaping grabs
	// and attempting to do an air evade.
	public string InputGuard {get; private set;}
	public float HorDir { get { return horDir; } }
	public float VertDir { get { return vertDir; } }

	void Awake()
	{
		_anim = GetComponent<Animator> ();
		_charStatus = GetComponent<CharacterStatus> ();
		_charMotor = GetComponent<CharacterMotor>();
		_charAttacking = GetComponent<CharacterAttacking> ();
		_charItem = GetComponent<CharacterItem> ();
	}

	void Start()
	{
		// get the transform of the main camera
		if (Camera.main != null)
		{
			cam = Camera.main.transform;
		}
		else
		{
			Debug.LogWarning("Where's your main camera?!");
		}
		_detectionRadius = transform.GetComponentInChildren<DetectionRadius> ();
	}
	
	void Update()
	{
		// No input allowed during a cutscene. This script shouldn't be enabled
		// in that case but for any frames that it may be...
		if(Manager_Cutscene.instance.inCutscene || Manager_Game.instance.IsPaused)
			return;

		if(Input.GetButton(inputTarget))
			targetButtonHeldTimer += Time.deltaTime;

		// If we release our target button or simply press it when we don't have a targeted
		// character, we will target one if available.
		if(Input.GetButtonUp(inputTarget) || (Input.GetButtonDown(inputTarget) && !_charAttacking.TargetedCharacter))
		{
			// Attempt to target an enemy.
			if(targetButtonHeldTimer < 0.4f) // Only change target if we have not been holding the button long.
				Manager_Targeting.instance.TargetACharacter(_charStatus.PlayerNumber, _charAttacking);
			targetButtonHeldTimer = 0;
		}

		if(!_charStatus.Busy) // All things we can do when we aren't busy.
		{
			if(!jump)
				jump = Input.GetButtonDown(inputJump); // Pressing jump button on this frame?
			if(!_charStatus.Busy)
			{
				if(_charItem.ItemHolding == null)
				{
					if(_charMotor.onGround)
					{
						if(Input.GetButton(inputAction) && Input.GetButton(InputGuard))
						{
							if(actionButtonHeldTimer < 0.15f && guardButtonHeldTimer < 0.15f)
							{
								actionButtonHeldTimer = guardButtonHeldTimer = 0;
								_charAttacking.AttackSetup(3, 0); // Grab
							}
						}
						else
						{
							if(_charAttacking.CanGuard && Input.GetButtonDown(InputGuard))
							{
								if(_charMotor.IsInLocomotion() && _charMotor.animMove > 0.4f)
									_charAttacking.DodgeSetup(vertDir, horDir);
								else
								{
									// This will get us into our guard animation.
									if(!_anim.GetBool("IsGuarding"))
										_anim.SetBool("IsGuarding", true);
								}
							}
						}
					}
					// No items close by.
					if(_detectionRadius.inGrabRangeItems.Count == 0
					   || _detectionRadius.inCloseRangeChar.Count > 0)
						InputAttack();
					else // Items in grab range.
					{
						if(_charStatus.BaseStateInfo.IsTag("NotBusy") &&
						   _charMotor.onGround)
						{
							if(Input.GetButtonDown(inputAction))
								_charItem.Pickup();
						}
					}
				}
				else // We are holding an item.
				{
					if(Input.GetButtonDown(inputAction))
					{
						// Holding a throwable.
						if(_anim.GetInteger("HoldStage") == 1)
							_charItem.Throw();
					}
					// Holding a weapon.
					if(_anim.GetInteger("HoldStage") == 3)
						InputAttack();
					if(Input.GetButton(InputGuard))
					{
						// Here we prepare to drop
						// the item we are holding.
						InputForGuard();
					}
				}
			}
		}
		else // We are busy.
		{
			// When guarding, we can attempt to dodge if enough directional
			// input is used.
			if(_charStatus.BaseStateInfo.IsName("Guarding") && !_anim.IsInTransition(0)
			   && (vertDir > 0.6f || vertDir < -0.6f || horDir > 0.6f
			    || horDir < -0.6f))
				_charAttacking.DodgeSetup(vertDir, horDir);
			else if(_charStatus.BaseStateInfo.IsName("Guard_Hit"))
			{
				// Counter after being hit when guarding.
				if(!_anim.IsInTransition(0) && _charStatus.BaseStateInfo.normalizedTime < 0.8f
				   && Input.GetButtonDown(inputAction))
					_charAttacking.AttackSetup(1, 0);
			}
			// If we are in an attack state. Ignore the counter one since we can't do anything after landing that.
            if(_charStatus.BaseStateInfo.IsTag ("Attack") && !_charStatus.BaseStateInfo.IsName("Atk_Counter"))
			{
				if(_anim.GetBool("AttackHit")) // We can combo or dodge.
				{
					if (!_charStatus.BaseStateInfo.IsName("Atk_HeavyHigh")) // Air launch move.
					{
						if(Input.GetButton(inputAction) && Input.GetButton(InputGuard))
						{
							if(actionButtonHeldTimer < 0.15f && guardButtonHeldTimer < 0.15f)
							{
								actionButtonHeldTimer = guardButtonHeldTimer = 0;
								_charAttacking.AttackSetup(3, 0);
							}
						}
						else if(Input.GetButtonUp(InputGuard))
						{
							_charAttacking.DodgeSetup(vertDir, horDir); // Dodge after landing an attack!
						}
						else InputAttack();
					}
					else
					{
						// Here if we press jump we can jump up to pursue an
						// enemy we hit into the air.
						if(Input.GetButtonDown (inputJump))
						{
							// Disable our active hitbox.
							_charAttacking.AnimEventActivateHitbox(0);
							// Jump up.
							_charMotor.Move (Vector3.zero, true, false);
						}
					}
				}
			}
			// We have a character grabbed.
			if(_charStatus.BaseStateInfo.IsName("Grab_Idle") && !_anim.IsInTransition(0))
			{
				// Attack the character we have held. Either attack or
				// throw depending on how long the attack button is held.
				InputAttack();
			}
		}

		if(Input.GetButtonUp(InputGuard))
		{
			// Stop guarding if we are.
			if(_anim.GetBool("IsGuarding"))
				_anim.SetBool("IsGuarding", false);
		}
	}
	
	// Fixed update is called in sync with physics
	private void FixedUpdate()
	{
		// read inputs. I allow this even when Busy so that you can give
		// input to vertical motion when attacking. Now, if you were to
		// want to update whether you are using joystick are not each
		// fixed update frame, this will be the spot to do it. That could be
		// useful if you were to change between them or decide to use a
		// joystick on the pause menu. You would need access to each
		// standalone input module component on the event trigger gameObject.
		// I gather them in the Manager_UI's playInputMenu[]. You would
		// activate and deactive which one(s) to use depending on the player
		// number and if you are using joystick or not.
		if (Input.GetAxis(inputHor) != 0 || Input.GetAxis(inputVert) != 0)
		{
			horDir = Input.GetAxis(inputHor);
			vertDir = Input.GetAxis(inputVert);
		}
		else if (Input.GetAxis(inputHorJS) != 0 || Input.GetAxis(inputVertJS) != 0)
		{
			horDir = Input.GetAxis(inputHorJS);
			vertDir = Input.GetAxis(inputVertJS);
		}
		else
			horDir = vertDir = 0;
		// Don't proceed if we are in a NotBusy state.
		if(!_charStatus.BaseStateInfo.IsTag("NotBusy"))
			return;
		// Calculate the move direction to pass to our player.
		if (cam != null)
		{
			// calculate camera relative direction to move:
			camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
			// We multiply our vertical input by the camera's forward direction and
			// multiply our horizontal input by the camera's right direction.
			move = vertDir * camForward + horDir * cam.right;
		}
		else // The main cam is null. Shouldn't happen.
		{
			// we use world-relative directions in the case of no main camera
			move = vertDir * Vector3.forward + horDir * Vector3.right;
		}
		if (move.magnitude > 1) move.Normalize();
		// Walk/run speed is modified by a key press and making sure
		// we are on the ground first or we are not on the ground and already
		// moving fast enough will allow us to keep that run momentum.
		bool isTargeting = _charAttacking.TargetedCharacter && Input.GetButton (inputTarget);
		bool walkToggle = (_charMotor.onGround && Input.GetButton(inputRun) && !isTargeting)
			|| (!_charMotor.onGround && _charMotor.animMove > 0.8f);
		// We select appropriate speed based on whether we're walking by default,
		// and whether the walk/run toggle button is pressed. The first number indicates
		// if the previous expression was true while the second would be if it was false.
		// So for the first part: 1 = true, 0.5f = false
		float walkMultiplier = (walkByDefault ? walkToggle ? 1 : 0.5f : walkToggle ? 0.5f : 1);
		move *= walkMultiplier;
        if (isTargeting)
            move *= 1.8f; // Moving when targeting is very slow so we increase the speed a bit more. Useful for when you always move relative to the camera, even when targeting.
		// pass all parameters to the CharacterMotor script
		_charMotor.Move(move, jump, isTargeting);
		jump = false; // Reset this since we used it above.
	}

	// The player's number gets added to the end of the button name. Check
	// Project Settings -> Input to see how they are layed out.
	public void SetupInputNames(bool aiOn, int difPlayNumber = 0)
	{
		string myPlayNumber = _charStatus.PlayerNumber.ToString ();
		if(difPlayNumber != 0)
			myPlayNumber = difPlayNumber.ToString();
		inputHor = "HorizontalP" + myPlayNumber;
		inputVert = "VerticalP" + myPlayNumber;
		inputHorJS = "HorizontalJSP" + myPlayNumber;
		inputVertJS = "VerticalJSP" + myPlayNumber;
		inputAction = "ActionP" + myPlayNumber;
		inputJump = "JumpP" + myPlayNumber;
		InputGuard = "GuardP" + myPlayNumber;
		inputTarget = "TargetP" + myPlayNumber;
		inputRun = "RunP" + myPlayNumber;
		// Button names are setup, now if this is AI, disable this script.
		// I just make sure I setup the button names first for safe keeping.
		if(aiOn || Manager_Game.usingMobile)
			this.enabled = false;
	}

	void InputAttack()
	{
		if(Input.GetButton (inputAction) || Input.GetButtonUp(inputAction))
		{
			// This bool checks to see if we have a character grabbed by checking
			// if we are in our Grab_Idle state.
			bool isGrabbing = _charStatus.BaseStateInfo.IsName("Grab_Idle");
			if(Input.GetButton(inputAction)) // Holding the action button.
			{
				actionButtonHeldTimer += Time.deltaTime;
				if(actionButtonHeldTimer > 0.25f) // After holding the button for 1/4 second.
				{
					if(!isGrabbing) // If we do not currently have a character held in our grasp.
						_charAttacking.AttackSetup(2, vertDir); // Regular heavy attack.
					else _charAttacking.GrabAttackSetup(2); // 2 = throw
					actionButtonHeldTimer = 0;
				}
			}
			else if(Input.GetButtonUp (inputAction))
			{
				if(actionButtonHeldTimer < 0.25f) // Make sure the above part doesn't get executed with this.
				{
					if(!isGrabbing)
					{
						// Can only do vertical direction when attacking if we are in the middle
						// of a combo (_anim.GetBool("AttackHit"))
						float vDir = _anim.GetBool("AttackHit") ? vertDir : 0;
						if(_detectionRadius.NextToItemStorer)
							vDir = -1; // Kick low when next to item crate.
						_charAttacking.AttackSetup(1, vDir); // Regular normal hit.
					}
					else _charAttacking.GrabAttackSetup(1); // 1 = grab regular hit
				}
				actionButtonHeldTimer = 0;
			}
		}
	}

	// Only using this when we have an item to drop when we hold our
	// guard button long enough. I call it InputForGuard since we are pressing our guard button.
	void InputForGuard()
	{
		if(Input.GetButton(InputGuard))
		{
			guardButtonHeldTimer += Time.deltaTime;
			if(guardButtonHeldTimer > 0.25f)
			{
				if(_charItem.ItemHolding != null)
					_charItem.DropItem ();
				guardButtonHeldTimer = 0;
			}
		}
		else if(Input.GetButtonUp (InputGuard))
		{
			guardButtonHeldTimer = 0;
		}
	}

	// This gets called during Character_Status when we are hurt and in the air
	// to attempt an air evade after releasing guard or jump. Make sure the attacker is
	// in an air attack.
	// Note that this gets called from CharacterStatus in Update when
	// we are not on the ground.
	public bool AttemptAirEvade()
	{
		if(_detectionRadius.inCloseRangeChar.Count == 0)
			return false;
		foreach(Transform attacker in _detectionRadius.inCloseRangeChar)
		{
			if(Vector3.Distance(attacker.position, transform.position) < 2.1f)
			{
				if (attacker && attacker.GetComponent<CharacterAttacking>().IsInAirAttack())
				{
					if(Input.GetButtonUp(InputGuard) || Input.GetButtonUp(inputJump))
						return true;
				}
			}
		}
		return false;
	}
}