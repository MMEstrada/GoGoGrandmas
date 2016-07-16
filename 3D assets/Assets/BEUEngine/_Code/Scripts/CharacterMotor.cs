using UnityEngine;
using System.Collections;
/// <summary>
/// Character motor. The main movement base for the characters. This also checks
/// whether they are on the ground, gravity, does rotation, and a slew of other
/// related things.
/// </summary>
public class CharacterMotor : Character
{
	// Serialize makes them visible in the inspector, even when private.
	[SerializeField] private float jumpPower = 12; // determines the jump force applied when jumping (and therefore the jump height)
	[SerializeField] private float airSpeed = 6; // determines the max speed of the character while airborne
	[SerializeField] private float airControl = 2; // determines the response speed of controlling the character while airborne
	[Range(2, 5)] [SerializeField] public float gravityMultiplier = 4; // gravity modifier - often higher than natural gravity feels right for game characters
	[SerializeField] [Range(0.1f, 3f)] private float moveSpeedMultiplier = 1; // how much the move speed of the character will be multiplied by
	[SerializeField] [Range(0.1f, 3f)] public float animSpeedMultiplier = 1; // how much the animation of the character will be multiplied by
	[SerializeField] private AdvancedSettings advancedSettings; // Container for the advanced settings class , this allows the advanced settings to be in a foldout in the inspector

	[System.Serializable]
	public class AdvancedSettings
	{
		// The higher these turn speeds are, the more delayed our turning will be.
		public float stationaryTurnSpeed = 0.2f;
		public float movingTurnSpeed = 0.25f;
		public float airTurnSpeed = 0.4f;

		public float headLookResponseSpeed = 2; // speed at which head look follows its target
		public PhysicMaterial zeroFrictionMaterial; // used when in motion to enable smooth movement
		public PhysicMaterial highFrictionMaterial; // used when stationary to avoid sliding down slopes
		public float jumpRepeatDelayTime = 0.25f; // amount of time that must elapse between landing and being able to jump again
		public float groundStickyEffect = 5f; // power of 'stick to ground' effect - prevents bumping down slopes.
	}
	// Every layer to look for ground.
	public LayerMask groundCheckMask;

	// IK for the head to look at a target using these.
	float lookBlendTime = 1; // How long to get head to look at target.
	public float lookWeight {get; private set;} // PlayerCutscene checks this, so its public.

	int m_Loco_0Id; // Locomotion state on first layer (Base Layer)
	int m_Loco_1Id; // Locomotion state on second layer (LowHealth)

	// The transform where the character will be looking at
	private Transform lookAtTransform;
	private Vector3 currentLookPos; // The current position where the character is looking
	private float lastAirTime; // USed for checking when the character was last in the air for controlling jumps
	private CapsuleCollider capsule; // The collider for the character
	private BoxCollider grabTriggerCol; // A trigger collider so we can be hit while being grabbed since we disable our regular collider during that time so we don't collide with anything normally.
	private const float HALF = 0.5f; // whats it says, it's a constant for a half. Faster processing than always multiplying by this otherwise.
	private bool jumpFired; // Did we attempt to jump?
	private bool lookAtTransformBool; // Should we try to look at our set lookAtTransform?
    private bool _hittingWall; // Are we currently bumping into a terrain wall?  Used to minimize movement if we are.
	private float defaultAirSpeed;
	private float strafe; // Our current strafe direction.
	private float forwardAmount; // This sets our "Move" Animator parameter.
	private float rotYVel = 0; // Velocity for rotating our y angle.
	private CharacterStatus _charStatus;
	private CharacterAttacking _charAttacking;
	private CharacterAI _charAI;
	private PlayerInput _playerInput; // Our input.
    //private PlayerInputMobile _playerInputMobile; // Input for mobile.
	private Vector3 velocity; // our velocity/moveDirection. Gotten from our rigidbody.velocity and updated as we want.

	public bool onGround { get; private set; } // Is the character on the ground
	public bool MinMovement { get { return anim.GetBool ("MinMovement"); } // Min movement is when our Move parameter is between -0.2f && 0.2f as well as our Strafe parameter.
		set { anim.SetBool("MinMovement", value); } }
	public float animMove {get; private set;} // I only use this to check our "Move" parameter's value.
    public float originalHeight {get; private set;} // Used for tracking the original height of the characters capsule collider
	// Our currently set direction to move in.
	public Vector3 moveDirection { get; set; }

	void Awake()
	{
		capsule = GetComponent<CapsuleCollider>();
		grabTriggerCol = GetComponent<BoxCollider>();
		anim = GetComponent<Animator> ();
		_charStatus = GetComponent<CharacterStatus> ();
		_charAttacking = GetComponent<CharacterAttacking> ();
		_charAI = GetComponent<CharacterAI> ();
        if (!IsEnemy)
        {
            _playerInput = GetComponent<PlayerInput>();
            //_playerInputMobile = GetComponent<PlayerInputMobile>();
        }
		// I use these since when we have low health, we will move a bit slower.
		// When we get more than 25% health again, we will go back to our
		// default speeds for these.
		defaultAirSpeed = airSpeed;
		m_Loco_0Id = Animator.StringToHash ("Base Layer.Locomotion");
		m_Loco_1Id = Animator.StringToHash ("LowHealth.Locomotion");
		myRigidbody = GetComponent<Rigidbody> ();
		if (!grabTriggerCol)
			Debug.LogWarning("PLEASE ASSIGN YOUR Player's GRAB TRIGGER COLLIDER");
		else
			grabTriggerCol.enabled = false;
	}

	private void Start()
	{
		// Make sure our capsule was successfully assigned.
		if (capsule == null)
		{
			Debug.LogError(" collider cannot be cast to CapsuleCollider");
		}
		else
		{
			// Get the default height of our capsule collider and set
			// our center properly based on that.
			originalHeight = capsule.height;
			capsule.center = Vector3.up * originalHeight * HALF;
		}
		
		// give the look position a default in case the character is not under control
		currentLookPos = myTransform.forward;
		myRigidbody.interpolation = RigidbodyInterpolation.None;
	}

	void Update()
	{
		// Helps us know if we need to go back into Idle or a moving state in our Animator more easily.
		MinMovement = (animMove < 0.2f && animMove > -0.2f) && (strafe < 0.2f && strafe > -0.2f);
	}

	void FixedUpdate()
	{
		if(!_charStatus.BaseStateInfo.IsTag("NotBusy") || Manager_Cutscene.instance.inCutscene)
		{
			// If any either of these states, we will begin setting our velocity back to zero.
			if(_charStatus.BaseStateInfo.IsName("Throw") || _charStatus.BaseStateInfo.IsTag("Pickup") || _charStatus.BaseStateInfo.IsTag("Grab"))
				myRigidbody.velocity = Vector3.MoveTowards(myRigidbody.velocity, new Vector3(0, myRigidbody.velocity.y, 0), 3 * Time.deltaTime);
			// Move() won't be called during this but we still need to check
			// if we are on the ground, assign correct physic materials, 
			// update our animator, and scale our capsule when needed. Those
			// all get done through Move() but not with the above condition so
			// that's why I do this here.
			velocity = myRigidbody.velocity;
			// If we are NOT dodging, we will reset our forwardAmount, which is what our Animator's
			// Move parameter will be set to.  When dodging, we will keep where it is at so that
			// when the dodge finishes we can go right back into locomotion or if not pressing
			// any direction, we will return to idle as normal.
			if(!Manager_Cutscene.instance.inCutscene && (!_charStatus.BaseStateInfo.IsTag("Dodging")
			   && (!anim.IsInTransition(0) || (anim.IsInTransition(0) && !_charStatus.BaseNextStateInfo.IsTag("Dodging")))))
				forwardAmount = 0;
			GroundCheck();
			SetFriction();
			UpdateAnimator();
			ScaleCapsule();
		}
	}

	void LateUpdate()
	{
		// This is used to prevent our character from rotating at all on the x or z which is not desired.
		Vector3 rot = myTransform.rotation.eulerAngles;
		rot.x = 0;
		rot.z = 0;
		myTransform.rotation = Quaternion.Euler(rot);
	}

    void OnCollisionStay(Collision other)
    {
        // Check to see if we need to minimize movement due to contact from a terrain wall or my trees which have the
        // tag SceneryCollide. You can use that tag for specific scenery objects you want the character to also
        // stop when running into it.
        if (other.gameObject.tag == "Terrain" || other.gameObject.tag == "SceneryCollide")
        {
            bool hittingAWall = false;
            foreach(ContactPoint hitPoint in other.contacts)
            {
                float hitDir = Vector3.Dot(myTransform.forward, (hitPoint.point - myTransform.position));
                // Make sure we are hitting the wall by facing it (hitDir > 0.1f) and that it is not
                // a slope we are hitting, more a wall (hitPoint.normal.y < 0.2f)
                if (hitDir > 0.1f && hitPoint.normal.y < 0.2f && (hitPoint.normal.x < -0.5f || hitPoint.normal.x > 0.5f || hitPoint.normal.z < -0.5f
                    || hitPoint.normal.z > 0.5f))
                {
                    hittingAWall = true;
                    break; // If any points are touching a wall, we will break out of here so that hittingAWall will not be set back to false
                }
                else _hittingWall = false;
            }
            _hittingWall = hittingAWall; // Did any of the collision points hit a wall and we were facing it?
        }
    }

    void OnCollisionExit(Collision other)
    {
        if (other.gameObject.tag == "Terrain")
            _hittingWall = false;
    }
	// Blend our head look to look at a target or stop looking at it.
	// The only target I make the player look at is a camera right now.
	private void BlendLookWeight(bool startLookingAt)
	{
		lookWeight = Mathf.Lerp(lookWeight, startLookingAt ? 1.0f : 0, Time.deltaTime * lookBlendTime);
	}

	private bool IsInIdle()
	{
		return _charStatus.BaseStateInfo.IsName ("Idle") || _charStatus.BaseStateInfo.IsName("Idle_2");
			//|| _charStatus.BaseStateInfo.IsName("Idle_3"); // A third idle in case you use one.
	}


	public bool IsInLocomotion()
	{
		return _charStatus.BaseStateInfo.fullPathHash == m_Loco_0Id || _charStatus.BaseStateInfo.fullPathHash == m_Loco_1Id;
	}
	// When you have low health you slow down a little bit so I make
	// sure to speed up the animation speed slightly since it is overly slow.
	public void LowHealthSetup(bool gettingLowHealth)
	{
		if(gettingLowHealth)
		{
			// Don't move as fast in the air.
			airSpeed = defaultAirSpeed * 0.6f;
		}
		else // Recovering from low health.
		{
			airSpeed = defaultAirSpeed;
		}
	}
	// The Move function is designed to be called from a separate component
	// based on user input, or an AI control script
	public void Move(Vector3 move, bool jump, bool isTargeting, bool lookAtATransform = false, Transform lookAtTrans = null)
	{
		if(anim == null || myRigidbody.isKinematic)
			return;
		if (move.magnitude > 1) move.Normalize();
		if(_charStatus.LowHealth)
		{
			if(_charAttacking.HitDelayTime == 0)
			{
				// Animation plays faster when in wounded locomotion and running
				float speedToUse = 1 + ((_charStatus.BaseStateInfo.IsName("Locomotion") && animMove > 0.6f) ? 0.3f : 0);
				if(animSpeedMultiplier != speedToUse)
					animSpeedMultiplier = speedToUse;
			}
		}
		else
		{
			if(animSpeedMultiplier != 1)
				animSpeedMultiplier = 1;
		}
		// transfer input parameters to member variables.
		this.moveDirection = move;
		this.jumpFired = jump;
		this.lookAtTransform = lookAtTrans;
		this.lookAtTransformBool = lookAtATransform;
		if(!isTargeting && strafe != 0)
			strafe = 0;
		// If we decided to look at something
		if(lookAtTrans != null)
			BlendLookWeight(true);
		else // Not looking at anything so reset any look blend weight.
		{
			if(lookWeight != 0)
				BlendLookWeight(false);
		}
		// grab current velocity, we will be changing it.
		velocity = myRigidbody.velocity;
		// set velocity to our multiplier so you can have characters with a wide
		// range of speeds easily.
		if(onGround)
		{
			velocity.x *= moveSpeedMultiplier;
			velocity.z *= moveSpeedMultiplier;
		}

		// Only enable Interpolation when our character is being controlled by a human. I found that
		// it is sluggish to use with an AI controlled character. During cut-scenes we don't want to
		// use .Interpolate either.
		
		ConvertMoveInput(isTargeting); // converts the relative move vector into local turn & fwd values
		ScaleCapsule(); // Scale our collider during certain animations.
		ApplyExtraTurnRotation(isTargeting); // this is in addition to root rotation in the animations
		GroundCheck(); // detect and stick to ground
		SetFriction(); // use low or high friction values depending on the current state
		// control and velocity handling is different when grounded and airborne:
		if (onGround)
		{
			HandleGroundedVelocities(isTargeting);
		}
		else
		{
			HandleAirborneVelocities();
		}
		UpdateAnimator(); // send input and other state parameters to the animator
		// reassign velocity, since it will have been modified by the above functions.
		myRigidbody.velocity = velocity;
	}
	
	private void ConvertMoveInput(bool isTargeting)
	{
		// convert the world relative moveDirection vector into a local-relative
		// turn amount and forward amount required to head in the desired
		// direction.
        Vector3 localMove = myTransform.InverseTransformDirection(moveDirection);
        if (!isTargeting || _charStatus.AIOn || _charAI.enabled)
        {
            forwardAmount = localMove.z; // This gets set to our "Move" animator parameter.
            if (_charAI.enabled)
                strafe = _charAI.StrafeDir;
        }
        else if (isTargeting && _playerInput && !_charStatus.AIOn)// Setup for strafing for human controlled players.
        {
            strafe = localMove.x; forwardAmount = localMove.z;
            // If you would rather move where when you press forward you move towards your target,
            // left and right strafe around the target in a circle, and pressing back makes you
            // move away from your target, you can comment the above line of code and uncomment this
            // lines out below. If you use these instead, make sure to go to the end of FixedUpdate in
            // PlayerInput and get rid of the added move boost since that was used to work with the
            // above line of code since it results in moving slower.

            /*
            if (_playerInput.enabled)
            {
                strafe = _playerInput.HorDir; forwardAmount = _playerInput.VertDir;
            }
            else if(_playerInputMobile.enabled)
            {
                strafe = _playerInputMobile.HorDir; forwardAmount = _playerInputMobile.VertDir;
            }*/
		}
	}
	
	private void ScaleCapsule()
	{
		float capHeightRat = anim.GetFloat("CapHeight");
		// scale the capsule collider according to
		// if we are going into our landing animation...
		if (capsule.height != originalHeight * capHeightRat)
		{
			float amountUsing = originalHeight * capHeightRat;
			capsule.height = Mathf.MoveTowards(capsule.height, amountUsing,
			                                   Time.deltaTime*3);
			// The first part (Vector3.up * originalHeight) gives us our original capsule height.
			// The second part (* amountUsing) gives us the percent of our original height.
			// The third part relates to it always being proper to have the center.y half of the collider's height.
			// So if the height were to be 2, then the center.y would be 1.
			capsule.center = Vector3.MoveTowards(capsule.center,
			                                     Vector3.up * amountUsing * HALF,
			                                     Time.deltaTime*1.5f);
		}
		// ... everything else. Make sure the height is set back to default.
		else
		{
			float amountUsing = originalHeight;
			capsule.height = Mathf.MoveTowards(capsule.height, amountUsing, Time.deltaTime * 3);
			capsule.center = Vector3.MoveTowards(capsule.center, Vector3.up * (amountUsing * HALF), Time.deltaTime * 1.5f);
		}
	}

	private void ApplyExtraTurnRotation(bool isTargeting)
	{
		Vector3 dirRotate = moveDirection;
		// If strafe != 0, that means we have a targeted character and are holding our target button
		// which is checked in PlayerInput.
		if(isTargeting)
			dirRotate = (_charAttacking.TargetedCharacter.position - myTransform.position).normalized;
		float turnSpeed = advancedSettings.movingTurnSpeed;
		// help the character turn faster. Allows you to choose how fast you want your character to
		// turn with the settings in the inspector for stationary and moving turn speeds.
		// If we are moving faster then we will use our movingTurnSpeed more over
		// our stationary turn speed.
		if(!onGround)
			turnSpeed = advancedSettings.airTurnSpeed;
		else turnSpeed = Mathf.Lerp(advancedSettings.stationaryTurnSpeed, advancedSettings.movingTurnSpeed,
		                forwardAmount);
		Quaternion rotationQu = Quaternion.LookRotation (myTransform.forward);
		// This check prevents getting that "look rotation viewing vector is zero"
		// message.
		if(new Vector3(dirRotate.x, 0, dirRotate.z).magnitude > 0.05f)
		{
			rotationQu = Quaternion.LookRotation(new Vector3(dirRotate.x, 0, dirRotate.z));
			// Here is where we finally rotate. The higher the turn speed, the
			// more delayed our turning is.
			myTransform.rotation = Quaternion.Euler(0, Mathf.SmoothDampAngle(myTransform.rotation.eulerAngles.y, rotationQu.eulerAngles.y, ref rotYVel, turnSpeed), 0);
		}
	}
	
	private void GroundCheck()
	{
		//Ray ray = new Ray(transform.position + Vector3.up*.1f, -Vector3.up);
		// Sort hits by distance away.
		// This bool is used to see if we just landed so that we can create
		// a dust particle. You could play a sound too.
		bool onGroundBefore = onGround;
		if(myRigidbody.velocity.y < -0.4f || myRigidbody.velocity.y > 0.8f)
			onGround = false;
		Ray ray = new Ray(myTransform.position + Vector3.up * 0.4f, -Vector3.up);
		//RaycastHit[] hits = Physics.RaycastAll(ray, 0.5f, groundCheckMask);
		RaycastHit hit;
		// Sort hits by distance away.
		if (myRigidbody.velocity.y < jumpPower * 0.5f)
		{
			if(Physics.Raycast(ray, out hit, 0.8f, groundCheckMask)
				|| Physics.SphereCast(ray, capsule.radius * 0.5f, out hit, 0.6f, groundCheckMask))
			{
				// check that we hit a non-trigger collider (and not the character itself)
				if (!hit.collider.isTrigger)
				{
					string tagName = hit.collider.tag;
					string goName = hit.collider.gameObject.name;
					// this counts as being on ground.
					bool hittingTerrain = tagName == "Terrain" || goName.Contains("Terrain" ) || tagName == "Item"
						|| goName.Contains("Crate") || ((!IsEnemy && tagName == "Player") || IsEnemy);
					// stick to surface - helps character stick to ground - specially when running down slopes.
					// Do not stick when our CapHeight float has changed (not equal to 1) since it is most likely
					// changing frequently and would cause a jerky effect.
					if (hittingTerrain && ((myRigidbody.velocity.y <= 0 && anim.GetFloat("CapHeight") == 1) || (onGround && _charStatus.BaseStateInfo.IsTag("Attack"))))
					{
						// Make sure our line is hitting, not our spherecast, since that could make us stick to the
						// ground when trying to move off of a ledge. Ensure that we are hitting terrain.
						RaycastHit rayHit;
						if (Physics.Raycast(ray, out rayHit, 0.8f, groundCheckMask))
						{
							if (rayHit.collider.gameObject.tag == "Terrain" || hit.collider.gameObject.name == "Terrain")
							{
								myRigidbody.position = Vector3.MoveTowards(myRigidbody.position, rayHit.point,
									Time.deltaTime * advancedSettings.groundStickyEffect);
							}
						}
					}
					// Just landed! Here is where you can put a particle and sound
					// for landing.
					if (hittingTerrain)
					{
						// I increase the size of this dust particle based on the 
						// vertical velocity when landing. The more, the larger
						// the particle will be. Less than -2 should be a good indication that
						// we fell fast enough.
						if(!onGroundBefore && myRigidbody.velocity.y < -2 && (tagName == "Terrain" || goName.Contains("Terrain")))
							Manager_Particle.instance.CreateParticle(myTransform.position - new Vector3(0, 0.25f, 0), ParticleTypes.Dust_Land, 0.8f + Mathf.Abs(myRigidbody.velocity.y * 0.1f));
						onGround = true;
					}
					else if (!hittingTerrain)
					{
						// Jump on an enemy!
						if (!IsEnemy && tagName == "Enemy" && _charStatus.Stats[0] > 0)
						{
							CharacterStatus eStatus = hit.collider.gameObject.GetComponent<CharacterStatus>();
							if (eStatus.Vulnerable)
							{
								eStatus.StopAllCoroutines();
								eStatus.StartCoroutine(eStatus.GotHit(Vector3.zero, 0, 0.3f, 1, 5, 0, myTransform, false, hit.point, false, _charAttacking));
								Manager_Particle.instance.CreateParticle(hit.point, ParticleTypes.HitSpark_Normal, 0.8f);
                                AudioSource.PlayClipAtPoint(_charAttacking.sfxsAttackHits[0], myTransform.position);
								JumpBounce();
							}
						}
					}
				}
			}
		}
		
		// remember when we were last in air, for jump delay
		// lastAirTime gets updated to the current time.
		if (!onGround) lastAirTime = Time.time;
	}
	
	private void SetFriction()
	{
		if (onGround)
		{
			// set friction to low or high, depending on if we're moving
			if (_charStatus.BaseStateInfo.IsTag("Attack") || MinMovement
				|| (_charStatus.BaseStateInfo.IsTag("HurtDowned") && myRigidbody.velocity.y < 1))
			{
				// when not moving this helps prevent sliding on slopes:
				if(capsule.material != advancedSettings.highFrictionMaterial)
					capsule.material = advancedSettings.highFrictionMaterial;
			}
			else
			{
				if(capsule.material != advancedSettings.zeroFrictionMaterial)
					capsule.material = advancedSettings.zeroFrictionMaterial;
			}
		}
		else // Not on ground
		{
			if(!_charStatus.BaseStateInfo.IsTag("Hurt") || _charStatus.BaseStateInfo.IsTag("HurtDowned"))
			{
				if(capsule.material != advancedSettings.zeroFrictionMaterial)
					capsule.material = advancedSettings.zeroFrictionMaterial;
			}
			else
			{
				if(capsule.material != advancedSettings.highFrictionMaterial)
					capsule.material = advancedSettings.highFrictionMaterial;
			}
		}
	}
	
	private void HandleGroundedVelocities(bool isTargeting)
	{
		if(IsInIdle() && !anim.IsInTransition(0))
			velocity = Vector3.zero;
		else
		{
			// This gives us extra movement. 
			// Use this for sure if you have locomotion animations that do not have movement applied in them
			// or simply do not use Apply Root Motion for those animations.
			float addedSpeed = (_charStatus.Stats != null && _charStatus.Stats.Length > 4) ? _charStatus.Stats[4] : 3;
			addedSpeed *= 0.1f;
			float baseSpeed = isTargeting ? 3 : 4;
			if(!Manager_Cutscene.instance.inCutscene)
				velocity = (myTransform.forward * ((baseSpeed + addedSpeed)) * anim.GetFloat ("Move"));
			else velocity = (myTransform.forward * baseSpeed * anim.GetFloat("Move"));
			if(isTargeting) // Here we check for strafing and will get added velocity to move left or right based on our strafe float
				velocity += (myTransform.right * (baseSpeed * HALF + addedSpeed)) * strafe;
		}
		// Jumping setup basically.
		AttemptJump ();
	}

	void JumpBounce()
	{
		velocity = moveDirection * (defaultAirSpeed * 0.5f);
		velocity.y = jumpPower;
		onGround = false;
	}

	// I use this to make the character rotate without needing to move. Don't use if
	// ApplyExtraTurnRotation is being called. Don't use if the Move()
	// method is being called either since that uses ApplyExtraTurnRotation. 
	// This method also has an option to rotate in reverse from the direction given.
	public void ManualRotate(Vector3 dir, bool reverse, float rotSpeed = 3)
	{
		float dirAmount = reverse ? -1 : 1;
		Quaternion rotationQu = Quaternion.LookRotation (Vector3.forward);
		if(new Vector3(dir.x, 0, dir.z).magnitude > 0.03f)
		{
			rotationQu = Quaternion.LookRotation(new Vector3(dir.x * dirAmount, 0, dir.z * dirAmount));
			myTransform.rotation = Quaternion.Slerp(myTransform.rotation, rotationQu, rotSpeed * Time.deltaTime);
		}
	}

	public void AttemptJump()
	{
		// check whether conditions are right to allow a jump:
		bool animationJumping = _charStatus.BaseStateInfo.IsName("Jumping");
		// There is a delay before we can re-jump.
		bool okToRepeatJump = Time.time > (lastAirTime + advancedSettings.jumpRepeatDelayTime);
		if (jumpFired && okToRepeatJump && !animationJumping)
		{
			// jump!
			onGround = false;
			velocity = moveDirection * airSpeed;
			velocity.y = jumpPower;
		}
		else
			velocity.y = myRigidbody.velocity.y; // If we aren't jumping, update the velocity variable with our current rigidbody's velocity then.
	}

	private void HandleAirborneVelocities()
	{
		// we allow some movement in air, but it's very different to when on ground
		// (typically allowing a small change in trajectory)
		// airMove is like our air momentum which is based off of our current moveDirection
		// when we enter the air.
		Vector3 airMove = new Vector3(moveDirection.x*airSpeed, velocity.y, moveDirection.z*airSpeed);
		velocity = Vector3.Lerp(velocity, airMove, Time.deltaTime*airControl);

		// apply extra gravity from gravity multiplier:
		Vector3 extraGravityForce = (Physics.gravity*gravityMultiplier) - Physics.gravity;
		myRigidbody.AddForce(extraGravityForce);
	}
	
	private void UpdateAnimator()
	{
		// Here we tell the anim what to do based on the current states and inputs.
		
		// only use root motion when on ground or hurt. I want root motion when
		// hurt to prevent being pushed too much unexpectedly.
		anim.applyRootMotion = (onGround && (_charStatus.BaseStateInfo.IsTag("Attack") || _charStatus.BaseStateInfo.IsTag("Dodging") || IsInIdle()
			|| _charAttacking.IsInGrab() || _charStatus.BaseStateInfo.IsName("Win") || _charStatus.LowHealth));
		// update the animator parameters
		float moveAmount = forwardAmount;
        float amountMoveRat = _hittingWall ? 0.1f : 1; // If touching a wall, movement will be minimized.
		// If strafing quite a lot then I wanted to reset forward movement.
		// You don't have to though, just my personal preference.
		if(strafe < -0.7f || strafe > 0.7f)
			moveAmount = 0;
        anim.SetFloat("Move", moveAmount * amountMoveRat, 0.2f, Time.deltaTime);
		anim.SetFloat("Strafe", strafe, 0.2f, Time.deltaTime);
		anim.SetBool("OnGround", onGround);
		animMove = anim.GetFloat ("Move");
		anim.SetFloat("VertVel", velocity.y);
		UpdateAnimSpeed ();
	}

	private void OnAnimatorIK(int layerIndex)
	{
		// This bool gets set in Move() if we want to start looking at our
		// lookAtTransform we set there.
		if(!lookAtTransformBool)
			return;
		// we set the weight so most of the look-turn is done with the head, not the body.
		anim.SetLookAtWeight(lookWeight, 0.2f, 2.5f);
		
		// if a transform is assigned as a look target, it overrides the vector lookPos value
		if (lookAtTransform != null)
		{
			currentLookPos = lookAtTransform.position;
		}
		
		// Used for the head look feature.
		anim.SetLookAtPosition(currentLookPos);
	}
	
	void OnDisable()
	{
		lookWeight = 0f;
	}
	
	// Gets called when we get hit.
	public void ResetParameters()
	{
		anim.SetFloat ("Move", 0);
		anim.SetFloat ("Strafe", 0);
		if(!myRigidbody.isKinematic)
			myRigidbody.velocity = Vector3.zero;
		moveDirection = Vector3.zero;
		velocity = Vector3.zero;
	}

	// The anim speed multiplier allows the overall speed of the animator to be edited.
	public void UpdateAnimSpeed()
	{
        if(_charAttacking.HitDelayTime == 0 || anim.GetBool("IsHurt"))
		{
			if (onGround)
			{
				anim.speed = animSpeedMultiplier;
			}
			else
			{
				// but we don't want to use that while airborne
				anim.speed = 1;
			}
		}
		else // In the middle of hit delay after landing an attack.
		{
			if(anim.speed != 0)
				anim.speed = 0;
		}
	}

	// Another version of this method, used to manually set a new speed for 
	// our animator.
	public void UpdateAnimSpeed(float newAnimSpeed)
	{
		animSpeedMultiplier = newAnimSpeed;
		anim.speed = newAnimSpeed;
	}

	public void GrabbedSetup(bool isGrabbed)
	{
		grabTriggerCol.enabled = isGrabbed;
		capsule.enabled = !isGrabbed;
	}
}