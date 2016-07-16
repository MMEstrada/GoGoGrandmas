using UnityEngine;
using System.Collections;
/// <summary>
/// Player input. For human characters. All input is done here. That doesn't
/// count when you are trying to escape a grab in Character_Status.
/// </summary>
[RequireComponent(typeof (CharacterMotor))]
public class PlayerInputMobile : MonoBehaviour
{
	public bool walkByDefault = true; // toggle for walking state

    public VirtualJoystick _myVirtJoystick;
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
    private float changeActionPressedTimer; // For changing our action button setting to 0 (none) or (2) held. Done very quickly.
    private float changeTargetPressedTimer; // Same as above, only for our target button.
    private float changeGuardPressedTimer; // And for our guard button.
    private int actionButtonPressed; // 0 = none, 1 = just pressed down, 2 = held, 3 = just released.
    private int targetButtonPressed; // 0 = none, 1 = just pressed down, 2 = held, 3 = just released.
    private bool jumpButtonPressed; // Only true or false since it only has one use, jumping the same height.
	// I made guard public for Character_Status to use it for escaping grabs
	// and attempting to do an air evade.
	public string InputGuard {get; private set;}
	public float HorDir { get { return horDir; } }
	public float VertDir { get { return vertDir; } }
    public int guardButtonPressed { get; private set; } // CharacterStatus accesses this when recovering from stun so we need it public.

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

		if(targetButtonPressed != 0 && targetButtonPressed != 3)
			targetButtonHeldTimer += Time.deltaTime;
		// If we release our target button or simply press it when we don't have a targeted
		// character, we will target one if available.
		if(targetButtonPressed == 3 || (targetButtonPressed == 1 && !_charAttacking.TargetedCharacter))
		{
			// Attempt to target an enemy.
			if(targetButtonHeldTimer < 0.6f) // Only change target if we have not been holding the button long.
				Manager_Targeting.instance.TargetACharacter(_charStatus.PlayerNumber, _charAttacking);
			targetButtonHeldTimer = 0;
		}

		if(!_charStatus.Busy) // All things we can do when we aren't busy.
		{
			if(!jump)
				jump = jumpButtonPressed; // Pressing jump button on this frame?
			if(!_charStatus.Busy)
			{
				if(_charItem.ItemHolding == null)
				{
					if(_charMotor.onGround)
					{
						if((actionButtonPressed != 0 && actionButtonPressed != 3) && (guardButtonPressed != 0 && guardButtonPressed != 3))
						{
							if(actionButtonHeldTimer < 0.15f && guardButtonHeldTimer < 0.15f)
							{
								actionButtonHeldTimer = guardButtonHeldTimer = 0;
								_charAttacking.AttackSetup(3, 0); // Grab
							}
						}
						else
						{
							if(_charAttacking.CanGuard && guardButtonPressed == 1)
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
							if(actionButtonPressed == 1)
								_charItem.Pickup();
						}
					}
				}
				else // We are holding an item.
				{
					if(actionButtonPressed == 1)
					{
						// Holding a throwable.
						if(_anim.GetInteger("HoldStage") == 1)
							_charItem.Throw();
					}
					// Holding a weapon.
					if(_anim.GetInteger("HoldStage") == 3)
						InputAttack();
					if(guardButtonPressed != 0 && guardButtonPressed != 3)
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
				   && actionButtonPressed == 1)
					_charAttacking.AttackSetup(1, 0);
			}
			// If we are in an attack state. Ignore the counter one since we can't do anything after landing that.
            if(_charStatus.BaseStateInfo.IsTag ("Attack") && !_charStatus.BaseStateInfo.IsName("Atk_Counter"))
			{
				if(_anim.GetBool("AttackHit")) // We can combo or dodge.
				{
					if (!_charStatus.BaseStateInfo.IsName("Atk_HeavyHigh")) // Air launch move.
					{
						if((actionButtonPressed != 0 && actionButtonPressed != 3) && (guardButtonPressed != 0 && guardButtonPressed != 3))
						{
							if(actionButtonHeldTimer < 0.15f && guardButtonHeldTimer < 0.15f)
							{
								actionButtonHeldTimer = guardButtonHeldTimer = 0;
								_charAttacking.AttackSetup(3, 0);
							}
						}
						else if(guardButtonPressed == 3)
						{
							_charAttacking.DodgeSetup(vertDir, horDir); // Dodge after landing an attack!
						}
						else InputAttack();
					}
					else
					{
						// Here if we press jump we can jump up to pursue an
						// enemy we hit into the air.
						if(jumpButtonPressed)
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

        if (actionButtonPressed != 0 && actionButtonPressed != 3)
            actionButtonHeldTimer = Mathf.Clamp(actionButtonHeldTimer + Time.deltaTime, 0, 3); // I make 3 the max for the actionButtonHeldTimer since it doesn't need to go high.
        else actionButtonHeldTimer = 0;

        if (actionButtonPressed != 0 && actionButtonPressed != 2)
        {
            changeActionPressedTimer += Time.deltaTime;
            if (changeActionPressedTimer > 0.1f)
            {
                changeActionPressedTimer = 0;
                actionButtonPressed = actionButtonPressed == 1 ? 2 : 0;
            }
        }

        if (targetButtonPressed != 0 && targetButtonPressed != 2)
        {
            changeTargetPressedTimer += Time.deltaTime;
            if (changeTargetPressedTimer > 0.1f)
            {
                changeTargetPressedTimer = 0;
                targetButtonPressed = targetButtonPressed == 1 ? 2 : 0;
            }
        }

        if (guardButtonPressed != 0 && guardButtonPressed != 2)
        {
            changeGuardPressedTimer += Time.deltaTime;
            if (changeGuardPressedTimer > 0.1f)
            {
                changeGuardPressedTimer = 0;
                guardButtonPressed = guardButtonPressed == 1 ? 2 : 0;
            }
        }

        if (guardButtonPressed == 3) // Released our guard button.
		{
			// Stop guarding if we are.
			if(_anim.GetBool("IsGuarding"))
				_anim.SetBool("IsGuarding", false);
		}
	}
	
	// Fixed update is called in sync with physics
	private void FixedUpdate()
	{
        if (!_myVirtJoystick)
            return;
        if (Input.touchCount > 0)
        {
            // During mobile play, the _myVirtJoystick gets obtained from
            // Manager_Mobile and we use it to figure out where our touch is
            // on the screen in relation to the joystick graphic.
            horDir = _myVirtJoystick.posit.x; vertDir = _myVirtJoystick.posit.y;
        }
        else
        { horDir = vertDir = 0; }
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
		bool isTargeting = _charAttacking.TargetedCharacter && targetButtonPressed == 2;
		bool walkToggle = (_charMotor.onGround && (Input.touchCount == 2 || Input.touchCount == 3) && !isTargeting)
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
		jump = jumpButtonPressed = false; // Reset this since we used it above.
	}

	// The player's number gets added to the end of the button name. Check
	// Project Settings -> Input to see how they are layed out.
	public void SetupInputNames(bool aiOn, int difPlayNumber = 0)
	{
		// Button names are setup, now if this is AI, disable this script.
		// I just make sure I setup the button names first for safe keeping.
		if(aiOn || !Manager_Game.usingMobile)
			this.enabled = false;
        if(_charStatus.PlayerNumber <= Manager_Mobile.instance.virtualJoysticks.Count)
            _myVirtJoystick = Manager_Mobile.instance.virtualJoysticks[_charStatus.PlayerNumber - 1];
    }

	void InputAttack()
	{
        // We are either holding our action button (2) or just released it (3)
		if(actionButtonPressed == 2 || actionButtonPressed == 3)
		{
			// This bool checks to see if we have a character grabbed by checking
			// if we are in our Grab_Idle state.
			bool isGrabbing = _charStatus.BaseStateInfo.IsName("Grab_Idle");
			if(actionButtonPressed == 2) // Holding the action button.
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
			else
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
		if(guardButtonPressed == 2)
		{
			guardButtonHeldTimer += Time.deltaTime;
			if(guardButtonHeldTimer > 0.25f)
			{
				if(_charItem.ItemHolding != null)
					_charItem.DropItem ();
				guardButtonHeldTimer = 0;
			}
		}
		else if(guardButtonPressed == 3) // Just released the guard button.
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
					if(guardButtonPressed == 3 || jumpButtonPressed)
						return true;
				}
			}
		}
		return false;
	}

    /// <summary>
    /// These get called from Manager_Mobile upon pressing a virtual button on the
    /// screen during mobile (Manager_Game's usingMobile bool == true) play. Setting
    /// the button's setting to 0 or 2 is done above in this script.
    /// </summary>
    /// <param name="setting">0 = no action (not passed in), 1 = just pressed down, 2 = held (not passed in), 3 = just released</param>
    public void ActionButtonPressed(int setting)
    {
        actionButtonPressed = setting;
    }

    public void TargetButtonPressed(int setting)
    {
        targetButtonPressed = setting;
    }

    public void GuardButtonPressed(int setting)
    {
        guardButtonPressed = setting;
    }

    // No setting required here since this button always does the same thing
    // regardless of being pressed, held, or released.
    public void JumpButtonPressed()
    {
        jumpButtonPressed = true;
    }
}