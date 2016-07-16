using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Character status. The character's main status data such as health, lives,
/// and other things like their skinned meshes and hurt voices for example.
/// </summary>
public class CharacterStatus : Character, IDamageable
{
	// Set this in the inspector to the specified character the model is.
	public PlayerCharacters myCharacter;
	public EnemyCharacters myCharacterEnemy;
	public GameObject deadPrefab; // For a ragdoll if desired.
	public Transform myHead; // I use this for creating the dizzy particle where there head is.
	public Text myPlayerNumberTarget; // The text UI for our cursor which shows us our player number.
	public AudioClip[] voicesHit; // Place all regular hurt voices here.
	public AudioClip[] voicesHitDown;
	public AudioClip voiceDie;
    public Texture[] bodyTextures; // Changed for second version of Ethan and Unity Guy. That's the only use of accessing our body textures at the moment.
	// These are for our UI health display. Only need to assign these in the
	// inspector for enemy characters since they will have their health bar
	// in world space above their head.
	public Slider healthSlider;
	public Image healthFill;

	GameObject myDizzyParticle; // You can use this for any dizzy particle.
	CharacterMotor _charMotor;
	CharacterAttacking _charAttacking;
	CharacterAI _characterAI;
    PlayerInput _playerInput;
    PlayerInputMobile _playerInputMobile;
    AudioSource _myAudio;
	
	// If we are currently flashing or changing the color of any of our materials.
	bool _matOrFlashChange = false;
	// I have two flash types: disable and renable our skinnedMeshRenderer meshes,
	// or flash them a different color.
	FlashType _myFlashType = FlashType.None;
	// A timer for resetting any flashing.
	float _matOrFlashChangeTimer = 3;
	CapsuleCollider _myCapsuleCol;
	// How high is our stun meter? When it reaches 8 after being hit, we will
	// be stunned.
	float _stunAmount = 0;
	// Our default color. Used for resetting back to this after flashing a set
	// color.
	Color _myDefaultColor;
	// Used for resetting our health bar's color after getting above lower health
	// again.
	Color _defaultHealthFill;
	int playerLives; // Our own reference to our live count.
	int[] _totalMats; // Total materials our skinned mesh has. Save the amount early so we don't have to recall it later in OnDestroy()
	// The script for our target that shows us our player number so we can have
	// it follow us.
	FaceTransform _faceTransformPlayerTarget;
	// All of our visible meshes.
	SkinnedMeshRenderer[] mySkinnedMeshes;
	ParticleEmitter _dizPartEmit; // We enable and disable the emission on our dizzy particle since it is a particle, the one I use anyway. If it were to just be a gameObject, we would instead simply activate and deactivate the gameObject.

	// These will just be easier access to getting any state info.
	public AnimatorStateInfo BaseStateInfo {get; private set;}
	public AnimatorStateInfo BaseNextStateInfo { get; private set; }

	public int PlayerNumber {get; set;}

	// Stats: { Health, Max Health, Strength, Defense, Base Speed (1 = default) }
	public int[] Stats { get; private set; }
    public List<Transform> TargetedByCharacters { get; private set;} // Who all is targeting us?  Used for targeting enemies and providing a proper offset for the player's target cursor based on how many players have an enemy targeted.
	public bool AIOn {get; set;}
	// All of the conditions for being busy. Change or add any needed or not needed.
	public bool Busy { get { return !BaseStateInfo.IsTag("NotBusy") || Manager_UI.InTransition || (anim.IsInTransition(0) && !BaseNextStateInfo.IsTag("NotBusy"))
			|| Time.timeSinceLevelLoad < 2; } }
	public bool Vulnerable {get; set;} // Can we be hit?
    public bool Stunned { get; private set; } // If we are currently dizzy.
	public bool LowHealth { get { return Stats != null && Stats.Length > 0 && (Stats [0] < (Stats [1] * 0.25f) ); } }
	// This gets taken from CharacterAttacking and is just used for
	// AI to see if we are attacking since AI only gets access to this script
	// and not the CharacterAttacking one.
	public bool AmIAttacking { get {return _charAttacking.IsAttacking; } }
	// The enemy AI checks for this to have an idea on if they should look out so they don't get jumped on.
	public bool AmIFalling { get { return myRigidbody.velocity.y < -0.2f && BaseStateInfo.IsName("Jumping"); } }

	void Awake()
	{
		_charMotor = GetComponent<CharacterMotor> ();
        if (!IsEnemy)
        {
            _playerInput = GetComponent<PlayerInput>();
            _playerInputMobile = GetComponent<PlayerInputMobile>();
        }
		_charAttacking = GetComponent<CharacterAttacking> ();
		_characterAI = GetComponent<CharacterAI> ();
		anim = GetComponent<Animator> ();
		_myCapsuleCol = GetComponent<CapsuleCollider> ();
		// Gather all of our skinned meshes.
		mySkinnedMeshes = transform.GetComponentsInChildren<SkinnedMeshRenderer> ();
		// Find all of our total materials now so we don't have to do it later. If curious why I do this
		// check the docs on Unity's scripting reference for renderer.material or renderer.materials.
		// They say to destroy any created instances of materials in OnDestroy()
		_totalMats = new int[mySkinnedMeshes.Length];
		for(int i = 0; i < mySkinnedMeshes.Length; i++)
			_totalMats[i] = mySkinnedMeshes [i].materials.Length;
		myRigidbody = GetComponent<Rigidbody>();
		_myAudio = GetComponent<AudioSource>();
        TargetedByCharacters = new List<Transform>();
	}

	void Start()
	{
		// Make our player number target follow us and face the main camera
		// by accessing its FaceTransform script. We assign these things and
		// then disable it as it will be enabled after we are done running into
		// the scene from PlayerCutscene.
		if(!IsEnemy)
		{
			_faceTransformPlayerTarget = myPlayerNumberTarget.transform.parent.GetComponent<FaceTransform>();
			_faceTransformPlayerTarget.transformToFace = Camera.main.transform;
			_faceTransformPlayerTarget.transformToFollow = myTransform;
			DeactivateTargetPlayerNumber();
		}
		else GetStats ();
		// Our default color is our starting color.
		_myDefaultColor = mySkinnedMeshes [0].material.color;
	}

	void Update ()
	{
		// Easy access to our state info. Many scripts use this.
		BaseStateInfo = anim.GetCurrentAnimatorStateInfo (0);
		BaseNextStateInfo = anim.GetNextAnimatorStateInfo (0);
		// Keep our stun amount clamped to certain values while slowly decreasing
		// it if it is over 0.
		_stunAmount = Mathf.Clamp (_stunAmount - 0.25f * Time.deltaTime, 0, 8.5f);
		// If we are currently changing color or flashing in some way.
		if(_matOrFlashChange)
		{
			if(_matOrFlashChangeTimer > 0)
				_matOrFlashChangeTimer -= Time.deltaTime;
			else
			{
				_matOrFlashChangeTimer = 0;
				_matOrFlashChange = false;
				if(IsEnemy || anim.GetInteger("Health") > 0
					|| playerLives > 0)
				{
					// I just use this one to end color changing and flashing.
					mySkinnedMeshes.Flashing(false, _myFlashType, _myDefaultColor);
					_myFlashType = FlashType.None;
					if(anim.GetInteger("Health") > 0)
						Vulnerable = true;
				}
				else
				{
					// We flash after dieing so if health isn't greater than 0
					// here, then we have lost our last life and get disabled.
					// This is only done when there is more than one player
					// remaining.
					gameObject.SetActive(false);
				}
			}
		}
		else
		{
			// In case the skinned mesh renderers for some reason don't get
			// reenabled. I only noticed this for Unity guy since he doesn't
			// have a ragdoll.  It happened randomly but this will fix it.
			if(!mySkinnedMeshes[mySkinnedMeshes.Length - 1].enabled && anim.GetInteger("Health") > 0 && !Busy)
			{
				mySkinnedMeshes.Flashing(false, _myFlashType, _myDefaultColor);
				_myFlashType = FlashType.None;
			}
		}
		// Return if in a cutscene.
		if(Manager_Cutscene.instance.inCutscene)
			return;

		if(!anim.IsInTransition(0))
		{
			if(BaseStateInfo.IsTag("NotBusy"))
			{
				// Here is where we randomly go into another Idle animation when
				// in idle long enough. Can't be done with low health
				// which is shown when layer(1)'s weight is set to 1.
				if(BaseStateInfo.IsName("Idle"))
				{
					if(anim.GetLayerWeight(1) == 0)
					{
						if(BaseStateInfo.IsName("Idle"))
						{
							// After the animation has ended for a while.
							if(BaseStateInfo.normalizedTime > Random.Range(1.5f, 2.5f))
								anim.CrossFade("Idle_2", 0.2f);
						}
					}
				}
			}
			// Air evade attemption! If we are hurt we wait until most of the animation has finished (> 0.6f)
			if(!_charMotor.onGround && myRigidbody.velocity.y < 2.5f && Vulnerable
				&& ((BaseStateInfo.IsTag("Hurt") && anim.GetInteger("HurtOther") < 2 && BaseStateInfo.normalizedTime > 0.6f)
					|| BaseStateInfo.IsTag("NotBusy")))
			{
				bool escaped = false;
				if(AIOn || IsEnemy)
				{
					escaped = _characterAI.AttemptAirEvade();
				}
				else
				{
					escaped = _playerInput.enabled ? _playerInput.AttemptAirEvade() : _playerInputMobile.AttemptAirEvade();
				}
				if(escaped)
				{
					// We will go into a dodge animation, either left or right.
					if(Random.value > 0.5f)
						anim.CrossFade("Dodging.DodgeRight", 0.1f);
					else anim.CrossFade("Dodging.DodgeLeft", 0.1f);
				}
			}
		}
	}

	// Remove created instance(s) of our materials whenever mySkinnedMeshes.materials was called
	// as we get destroyed otherwise they will add up from enemies overtime and can eventually
	// crash the game!
	void OnDestroy()
	{
		if(Manager_Game.GameShuttingDown)
			return;
		for (int i = 0; i < mySkinnedMeshes.Length; i++)
			for (int j = 0; j < _totalMats[i]; j++)
				DestroyImmediate (mySkinnedMeshes [i].materials [j]);
	}

	// Respawn after dying. Lots of things to reset here.
	void Respawn()
	{
		GetComponent<Collider>().enabled = true;
		myRigidbody.isKinematic = false;
		// Update our lives.
		playerLives = Manager_Game.instance.LiveChange (PlayerNumber, -1);
		// We lost a life so now we will lose some points for it when graded.
		if(PlayerNumber == 1)
			Manager_Game.P1LivesLost++;
		else if(PlayerNumber == 2)
			Manager_Game.P2LivesLost++;
        else if(PlayerNumber == 3)
            Manager_Game.P3LivesLost++;
        else if(PlayerNumber == 4)
            Manager_Game.P4LivesLost++;
		Vulnerable = false;
		// Stats[1] is our max health so we set all health variable things
		// back to that.
		Stats [0] = Stats [1];
		_stunAmount = 0;
		Stunned = false;
		if (myDizzyParticle && _dizPartEmit && _dizPartEmit.emit)
			_dizPartEmit.emit = false;
		anim.SetInteger ("Health", Stats [1]);
		anim.SetInteger ("HurtOther", 0);
		anim.SetBool ("IsHurt", false);
		healthSlider.value = Stats [1];
		healthFill.color = _defaultHealthFill;
		// This gets disabled after losing all health so reenable it now.
		healthFill.enabled = true;
		// Reset any movement, should be none.
		_charMotor.ResetParameters ();
		anim.Play ("Jumping");
		// Disable our wounded layer.
		anim.SetLayerWeight(1, 0);
		_charMotor.LowHealthSetup(false);
		// Get ready to start flashing, signaling that we are not vulnerable
		// and just died.
		StartFlash (0.2f, 0.3f, 2, FlashType.Renderer_Disable);
		// Get our name back to default. We added "Dead" to our name after dying
		// to help DetectionRadius to not add us to a list in there.
		gameObject.name = myCharacter.ToString();
		InputChange (true);
		_charMotor.Move (Vector3.zero, true, false);
	}

	// Flashing setup. You can choose to flash a certain color, or disable
	// and reenable your skinned mesh renderers.
	void StartFlash(float delay, float repeatRate, float flashLength, FlashType myFlashType)
	{
		// When this timer reaches 0 in update, the flashing will end and
		// you will be vulnerable again.
		_matOrFlashChangeTimer = flashLength;
		_myFlashType = myFlashType;
		_matOrFlashChange = true;
		// This will keep going for as long as you specified for flash length.
		InvokeRepeating ("FlashEffect", delay, repeatRate);
	}

	void FlashEffect()
	{
		// Create the flashing effect. Check MyExtensionMethods to see this
		// method.
		mySkinnedMeshes.Flashing (_matOrFlashChange, _myFlashType, _myDefaultColor);
		if(!_matOrFlashChange) // The flashing has ended from the _matOrFlashChangeTimer
		{
			CancelInvoke("FlashEffect");
			Vulnerable = true;
		}
	}
	// Disable or renable controls for the character.
	public void InputChange(bool enable, bool changeAI = false, int playNumControls = 0)
	{
		// I call this when you want an AI player to become human
		// controlled. They will get the chosen player number's controls while
		// still having their current player number. I use this when a
		// human player loses their last life and there is an AI still present.
		// They will take control of that AI player using their controls while
		// that AI player will still keep their current player number.
		if(changeAI)
		{
			AIOn = !AIOn;
			if (!AIOn)
			{
                if (!Manager_Game.usingMobile)
                {
                    _playerInput.enabled = true; _playerInputMobile.enabled = false;
                }
                else if (Manager_Game.usingMobile)
                {
                    _playerInputMobile.enabled = true;
                    _playerInput.enabled = false;
                }
				ActivateTargetPlayerNumber(playNumControls);
			}
			else
				Invoke("DeactivateTargetPlayerNumber", 4);
            _playerInput.SetupInputNames(AIOn, playNumControls);
            _playerInputMobile.SetupInputNames(AIOn, playNumControls);
            _faceTransformPlayerTarget.transformToFollow = myTransform;
		}

		// Disable any means of input or AI for the character.
		if(!enable)
		{
			_characterAI.enabled = false;
            if (!IsEnemy)
            {
                _playerInput.enabled = _playerInputMobile.enabled = false;
            }
			_charMotor.Invoke ("ResetParameters", 0.1f);
		}
		else // Turn back on.
		{
			if(AIOn)
			{
				_characterAI.enabled = true;
				_playerInput.enabled = _playerInputMobile.enabled = false;
			}
			else // Human controlled
			{
                if (!Manager_Game.usingMobile)
                    _playerInput.enabled = true;
                else _playerInputMobile.enabled = true;
				_characterAI.enabled = false;
			}
		}
	}

	// Assign our UI elements and I get our stats setup from here too since that
	// needs to be done after getting our player number. This gets called from
	// Manager_Game after it creates us.
	public void CreatedSetup(int playerNumber)
	{
        PlayerNumber = playerNumber;
		healthSlider = Manager_UI.instance.plHealthSlider[playerNumber - 1];
		healthFill = Manager_UI.instance.plHealthSliderFill[playerNumber - 1];
        myCharacter = Manager_Game.PlayersChosen[playerNumber - 1];
        string myCharName = myCharacter.ToString();
        if (myCharName.Contains("Twin")) // A twin variant character
        {
            if (myCharName.Contains("Ethan"))
                // Red version of Ethan's suit.
                mySkinnedMeshes[1].materials[0].mainTexture = mySkinnedMeshes[4].materials[0].mainTexture = bodyTextures[1];
            else if (myCharName.Contains("Dude"))
                mySkinnedMeshes[2].materials[1].mainTexture = bodyTextures[1]; // Lighter-colored version of Unity Guy's suit.
        }
        string myName = myCharacter.ToString();
        myName = myName.Replace('_', ' '); // Remove any '_' in the character's name.
        Manager_UI.instance.plNameText[playerNumber - 1].text = myName;
        int subtract = playerNumber > 1 ? playerNumber - Manager_Game.playersDefeated.Count : 1;
        ControlSetup (subtract);
        GetStats ();
	}

	// Setup all of our stats, update score variable for UI, and lives too.
    void GetStats()
	{
		if(!IsEnemy)
		{
            if(PlayerNumber == 1)
			{
                playerLives = Manager_Game.P1Lives;
				// This is this player's color.
				myPlayerNumberTarget.color = Color.blue;
				// Our player cursor that tells us our player number.
				myPlayerNumberTarget.text = "P1";
			}
			else if(PlayerNumber == 2)
			{
                playerLives = Manager_Game.P2Lives;
				myPlayerNumberTarget.color = Color.red;
				myPlayerNumberTarget.text = "P2";
			}
            else if(PlayerNumber == 3)
            {
                playerLives = Manager_Game.P3Lives;
                myPlayerNumberTarget.color = Color.green;
                myPlayerNumberTarget.text = "P3";
            }
            else if(PlayerNumber == 4)
            {
                playerLives = Manager_Game.P4Lives;
                myPlayerNumberTarget.color = Color.yellow;
                myPlayerNumberTarget.text = "P4";
            }
			// The Manager_Game will hold our stats at the end of each scene
			// when we assign our current ones to it.
			int[] curStats = Manager_Game.StatsP1;
			if(PlayerNumber == 2)
				curStats = Manager_Game.StatsP2;
            else if(PlayerNumber == 3)
                curStats = Manager_Game.StatsP3;
            else if(PlayerNumber == 4)
                curStats = Manager_Game.StatsP4;
			if(curStats == null) // Haven't assigned this yet so must be a new
				// game.
			{
				// Stats {Health, Max Health, Strength, Defense}
				if(myCharacter == PlayerCharacters.Ethan)
					curStats = new int[4] {100, 100, 5, 4};
				else if(myCharacter == PlayerCharacters.Dude)
					curStats = new int[4] {100, 100, 4, 5};
                else if(myCharacter == PlayerCharacters.Ethan_Twin)
                    curStats = new int[4] {100, 100, 6, 3};
                else if(myCharacter == PlayerCharacters.Dude_Twin)
                    curStats = new int[4] {100, 100, 3, 6};
			}
			Stats = curStats;
			// These methods update the UI to display the correct lives
			// and score as well. Just pass in 0 here since there is no
			// change to them.
            Manager_Game.instance.LiveChange (PlayerNumber, 0);
            Manager_Game.instance.ScoreUpdate(PlayerNumber, 0);
		}
		else // This is an enemy.
		{
			// The lowest amount of HP for an enemy. If you have multiple
			// enemies, set their baseHP to something different if desired.
			int baseHP = 80;
			if(myCharacterEnemy == EnemyCharacters.Robot_Kyle)
				Stats = new int[4]{baseHP, baseHP, 3, 4 };
			int curAreaNum = Manager_BattleZone.instance.currentAreaNumber;
			// I give enemies further in the scene a chance at more HP.
			Stats[0] = Stats[1] = Random.Range(baseHP + curAreaNum, baseHP + curAreaNum * 3);
			AIOn = true;
            _myAudio.pitch += Random.Range(-0.08f, 0.08f); // Various voice pitches for the enemy.
		}
		_defaultHealthFill = Manager_UI.DefaultHealthFillColor;
		healthSlider.minValue = 0;
		healthSlider.maxValue = Stats [1]; // Max Health
		healthSlider.value = Stats [0]; // Current health
		healthFill.enabled = true;
		anim.SetInteger ("Health", Stats [0]);
		// If we have less than 50% health left.
		if(Stats[0] < Stats[1] * 0.5f)
		{
			// More than 25% 
			if(Stats[0] > Stats[1] * 0.25f)
				healthFill.color = Color.yellow;
			else // Less than 25%
			{
				healthFill.color = Color.red;
				// We are injured.
				anim.SetLayerWeight(1, 1);
				_charMotor.LowHealthSetup(true);
			}
		}
		else
			anim.SetLayerWeight(1, 0); // Make sure we aren't injured.
	}
	// Setup for changing color. I make the character change to a light blue
	// when they aren't vulnerable to show that.
	public void StartMaterialColorChange(float timeToChangeFor, ColorChangeTypes colorType)
	{
		// When this timer reaches 0, we will go back to our normal color
		// and be vulnerable again.
		_matOrFlashChangeTimer = timeToChangeFor;
		_matOrFlashChange = true;
		_myFlashType = FlashType.Color_Change;
		// Check MyExtensionMethods to see this method.
		mySkinnedMeshes.MaterialColorChange (colorType, _myDefaultColor, colorType == ColorChangeTypes.Is_Vulnerable && Vulnerable);
	}

	// We were healed from a collectable.
	public void WasHealed(int healAmount)
	{
		// Attach a healed particle to us.
		GameObject particle = Manager_Particle.instance.CreateParticle (myTransform.position + Vector3.up, ParticleTypes.Sparkles_Heal, 1);
		particle.transform.parent = myTransform;
		int health = Stats [0];
		// Next we check to see if we will go beyond our max health after
		// recovering by the passed in healAmount. If so, we simply subtract
		// our max health by our current to get what we need. Otherwise add it all.
		int healTotal = ((health + healAmount > Stats [1]) ? Stats [1] - Stats [0] : healAmount);
		health = Mathf.Clamp (health + healAmount, 1, Stats [1]);
		// This is used to see if we will be going back up above this value
		// so we can stop being injured.
		bool hadLowerThanQuarter = Stats[0] < Stats [1] * 0.25f;
		Stats [0] = health;
		anim.SetInteger ("Health", health);
		healthSlider.value = health;
		if(hadLowerThanQuarter)
		{
			anim.SetLayerWeight(1, 0);
			_charMotor.LowHealthSetup(false); // Disable all low health setup things.
		}
		if(health > Stats[1] * 0.5f)
			healthFill.color = _defaultHealthFill;
		else if(health > Stats[1] * 0.25f)
			healthFill.color = Color.yellow;
		else healthFill.color = Color.red;
		// Create a 3D number that shows us how much we were healed by.
		Manager_Particle.instance.Create3DNumber (healTotal, myTransform.position + new Vector3 (0, _myCapsuleCol.height, 0), Vector3.up, true);
		// I use green for the heal color.
		StartMaterialColorChange (2, ColorChangeTypes.Healed);
	}

	// Here is where we find out if we are AI controlled or not and setting
	// up input button names.
    public void ControlSetup (int playerNumber)
	{
		if(IsEnemy)
			return;
		// We are an AI controlled player.
		if(playerNumber > Manager_Game.NumberOfHumans)
		{
			_characterAI.enabled = true;
			AIOn = true;
		}
		else // We are a human controlled player.
		{
			_characterAI.enabled = false;
			AIOn = false;
			_characterAI.Agent.gameObject.SetActive(false);
		}
        // If we are AI controlled, playerInput will get disabled after setting
        // up the button names.
        _playerInput.SetupInputNames(AIOn, playerNumber);
        _playerInputMobile.SetupInputNames(AIOn, playerNumber);
	}

	// The cursor that shows our player number. Here we activate it.
	public void ActivateTargetPlayerNumber(int playerNumberToDisplay)
	{
		myPlayerNumberTarget.transform.parent.gameObject.SetActive (true);
		myPlayerNumberTarget.text = "P" + playerNumberToDisplay.ToString ();
		// Set it to deactivate after a set time.
		Invoke ("DeactivateTargetPlayerNumber", 4);
	}

	public void DeactivateTargetPlayerNumber()
	{
		myPlayerNumberTarget.transform.parent.gameObject.SetActive (false);
	}

	// The main damage-taking method for all damageable objects.
	// Max will just be our max health we have.
	public int TakeDamage(int damage, int max)
	{
		int health = Stats [0];
		health = Mathf.Clamp (health - damage, 0, max);
		// Update these next 3 with the result.
		healthSlider.value = health;
		Stats [0] = health;
		anim.SetInteger ("Health", health);
		return health;
	}

    public void TargetChange(bool add, Transform charTargetingMe)
    {
        if (add && !TargetedByCharacters.Contains(charTargetingMe))
            TargetedByCharacters.Add(charTargetingMe);
        else if (!add && TargetedByCharacters.Contains(charTargetingMe))
            TargetedByCharacters.Remove(charTargetingMe);
    }

	// We were hit!
	public IEnumerator GotHit(Vector3 hitDir, float hitForce, float stunTimeAfterStop, float stunAdd, int damage, int hurtOther,
		Transform attacker, bool guarded, Vector3 hitPos, bool hitRelative = true, CharacterAttacking charAttacking = null)
	{
		// If we are dead or successfully guarded the attack, we will get
		// out of here.
		if(Stats[0] <= 0 || guarded)
		{
			if(guarded)
				anim.SetTrigger("WasHit");
			StopCoroutine("GotHit");
			yield break;
		}
		// Call the ResetParameters() method on all of our character scripts
		// that have it.
		gameObject.SendMessage("ResetParameters");
		// We subtract our defense stat from the damage received.
		int damageTaken = damage - Stats [3];
		bool slammed = hitDir.y < -3 && !_charMotor.onGround; // Air slammed.
		if(!Stunned)
		{
			_stunAmount += stunAdd; // Add stun.
			// I found 8 was a good amount for being stunned with my setup now.
			// Decrease this if you want characters to be stunned easier. You will not
			// be stunned if about to die.
			if(_stunAmount > 8 && _charMotor.onGround && !slammed && hitDir.y < 6
				&& anim.GetInteger("HurtOther") < 3 && (Stats[0] - damageTaken > 0))
			{
				// Stunned!
				hurtOther = 3;
				_stunAmount = 8;
				Stunned = true;
				if(myDizzyParticle == null)
					myDizzyParticle = Manager_Particle.instance.CreateParticle(myHead.position, ParticleTypes.Dizzy, 1);
				if(myDizzyParticle.transform.parent != myHead)
					myDizzyParticle.transform.parent = myHead;
				myDizzyParticle.transform.localPosition = Vector3.zero;
				_dizPartEmit = myDizzyParticle.GetComponent<ParticleEmitter>();
				if (!_dizPartEmit)
					Debug.Log("NO PARTICLE EMITTER FOUND FOR DIZZY PARTICLE!");
				if (!myDizzyParticle.activeSelf)
					myDizzyParticle.SetActive(true);
				else if(_dizPartEmit) _dizPartEmit.emit = true;
			}
		}
		else
		{
			// If we are already stunned and we get hit, it always will result
			// in a knock down.
			hurtOther = 2; // Knocked down
			// The hit power increases so we get pushed further than we normally
			// were going to.
			hitDir.y = Mathf.Clamp(hitDir.y + 3, 3, 6);
			hitDir.x *= 1.5f; hitDir.z *= 1.5f;
			Vulnerable = false;
			Stunned = false;
			_stunAmount = 0;
			// More damage taken too.
			damageTaken = Mathf.RoundToInt(damageTaken * 1.5f);
			// We no longer need our dizzy particle.
			if (myDizzyParticle && _dizPartEmit && _dizPartEmit.emit)
				_dizPartEmit.emit = false;
		}
		if(damageTaken < 1)
			damageTaken = 1; // Minimum of one damage.
		Manager_Particle.instance.Create3DNumber(damageTaken, hitPos, hitDir + Vector3.up, false);
		// Get our health after subtracting our damageTaken.
		int health = TakeDamage (damageTaken, Stats[1]);
		// Change our health bar slider stuff if below 50% of our max health.
		if(health < Stats[1] * 0.5f)
		{
			if(health > Stats[1] * 0.25f)
				healthFill.color = Color.yellow;
			else // Below 25% max health.
			{
				healthFill.color = Color.red;
				anim.SetLayerWeight(1, 1); // Our injured layer now activates.
				_charMotor.LowHealthSetup(true); // Make speed adjustments.
			}
		}
		if(!IsEnemy)
		{
			// Increase our Hits Taken count for grading.
			if(PlayerNumber == 1)
				Manager_Game.P1HitsTaken++;
			else if(PlayerNumber == 2)
				Manager_Game.P2HitsTaken++;
            else if(PlayerNumber == 3)
                Manager_Game.P3HitsTaken++;
            else if(PlayerNumber == 4)
                Manager_Game.P4HitsTaken++;
		}

		if(anim.GetInteger("HurtOther") == 4) // Is being held.
		{
			if(health < 1)
			{
				// If we died, we no longer need to be held.
				StopCoroutine("WasGrabbed");
				anim.SetInteger("HurtOther", 0);
				if (myTransform.parent) // Simply ensure that we still have a parent.
				{
					myTransform.SendMessageUpwards("GrabBroken"); // Tell our grabber that we are no longer grabbed.
					myTransform.parent = null;
				}
				_charMotor.GrabbedSetup(false);
			}
			else
			{
				anim.SetBool ("IsHurt", true);
				anim.SetTrigger("WasHit");
				// We were hit while grabbed so we get out of here since
				// nothing else is needed in this case. We just go into our grabbed hurt state.
				StopCoroutine("GotHit");
				yield break;
			}
		}

		// In case this needs to be reset from being grabbed, we do
		// so here.
		myRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

		Vector3 dir = -myTransform.forward;
		if(attacker != null)
			dir = (attacker.position - myTransform.position).normalized;
		if(hitRelative) // Default hit direction setup
		{
			if(attacker != null)
			{
				// We find this simply by taking our position - attacker's position
				Vector3 relDir = myTransform.position - attacker.position;
				hitDir.x = relDir.x;
				hitDir.z = relDir.z;
			}
		}

		// With Vector3.Dot here, -1 being we are dead center behind the
		// attacker who hit us. 1 being the attacker who hit us is dead
		// center in front.
		float direction = Vector3.Dot(dir, myTransform.forward);
		// We update all of our Animator parameters, saving the trigger "WasHit"
		// for last. It's best to save triggers for last so all of the
		// parameters can be set properly to determine which state to go into
		// next.
		anim.SetInteger ("HurtOther", hurtOther);
		anim.SetFloat ("HitDir", direction);
		anim.SetBool ("IsHurt", true);
		anim.SetInteger ("Health", health);
		anim.SetTrigger ("WasHit");

		if(_myAudio) // If we have an audio source, which we should.
		{
			AudioClip voiceToUse = null;
			// Assign a audio clip to play based on what kind of damage
			// we took (hurtOther)
			if(health > 0)
			{
				if(hurtOther < 2)
				{
					// If we are lightly hit (hurtOther == 0) we won't always play a hurt voice. If we
					// are hit hard (hurtOther == 1), then we always will.
					if(voicesHit.Length > 0 && ((hurtOther == 0  && Random.value > 0.5f) || hurtOther == 1))
						voiceToUse = voicesHit[Random.Range(0, voicesHit.Length)];
				}
				else if(hurtOther == 2)
				{
					if(voicesHitDown.Length > 0)
						voiceToUse = voicesHitDown[Random.Range(0, voicesHitDown.Length)];
				}
			}
			if(voiceToUse != null) // If we assigned one, play it.
				Manager_Audio.PlaySound(_myAudio, voiceToUse, false);
		}
		// If knocked down or dead, make sure we aren't vulnerable.
		if(hurtOther == 2 || health <= 0)
		{
			Vulnerable = false;
		}
		if(hitDir.y > 8) // Limit how high we can be hit up into the air.
			hitDir.y = 8;
		// Increase these by the hitForce given.
		hitDir.x *= hitForce; hitDir.z *= hitForce;
		myRigidbody.velocity = Vector3.zero;
		yield return new WaitForSeconds(0.08f);
		myRigidbody.velocity = hitDir;
		if(health > 0)
		{
			if (slammed)
			{
				while (!_charMotor.onGround)
					yield return new WaitForSeconds(0.0001f);
				// After reaching here, we have hit the ground after being slammed.

				// Amount of damage from slamming into ground will be roughly 60%
				// of total damage taken from the slam attack.
				damageTaken = Mathf.RoundToInt(damageTaken * 0.6f);
				health = TakeDamage(Mathf.RoundToInt(damageTaken), Stats[1]);
				Manager_Particle.instance.Create3DNumber(Mathf.RoundToInt(damageTaken), myTransform.position + Vector3.up, hitDir, false);
				if (health <= 0) // Died from slam.
				{
					// No more health to display
					healthFill.enabled = false;
					// Do the die coroutine instead now.
					StartCoroutine(Die(attacker, charAttacking, Vector3.up * Mathf.Abs(hitDir.y * 0.9f)));
					// We died so get out of here.
					StopCoroutine("GotHit");
					yield break;
				}
				else
				{
					// A small extra bounce from the land.
					myRigidbody.velocity = new Vector3(myRigidbody.velocity.x, Mathf.Abs(hitDir.y * 0.6f), myRigidbody.velocity.z);
				}
			}
			else
			{
				if(hitDir.y > 3)
					yield return new WaitForSeconds(0.4f);
			}
			// If knocked down, wait until we have landed properly.
			while((slammed || hurtOther == 2) && (!_charMotor.onGround || myRigidbody.velocity.y > 0.2f))
			{
				if(attacker)
					_charMotor.ManualRotate(hitDir, anim.GetFloat("HitDir") != -1, 5);
				yield return new WaitForSeconds(0.0001f);
			}

			// If we were knocked down, here is where we will reset our
			// attacker's combo if we were the character they started comboing on.
			if(hurtOther == 2 && attacker != null)
			{
				if(_charAttacking == null)
					charAttacking = attacker.GetComponent<CharacterAttacking>();
				if(charAttacking.StartedComboEnemy == myTransform)
					charAttacking.ComboChange(false);
				if(IsEnemy)
				{
					if(Manager_Targeting.instance.playerTargets.Count > 1)
					{
						// If any other character just so happened to have us
						// as their started combo character.
						foreach(Transform player in Manager_Targeting.instance.playerTargets)
						{
							if(player != attacker)
							{
								CharacterAttacking pAttacking = player.GetComponent<CharacterAttacking>();
								if(pAttacking.StartedComboEnemy == myTransform)
									pAttacking.ComboChange(false);
							}
						}
					}
				}
			}
			if (Stunned) // was stunned
			{
				while (_stunAmount > 0)
				{
					// Additional recovery with input or AI's attempt to recover.
					if (!AIOn)
					{
						if ((_playerInput.enabled && Input.GetButtonDown(_playerInput.InputGuard))
                            || (_playerInputMobile.enabled && _playerInputMobile.guardButtonPressed == 1))
							_stunAmount -= 0.3f;
					}
					else
					{
						if (Random.value < _characterAI.evadeRatio)
							_stunAmount -= 0.02f;
					}
					_stunAmount -= 5 * Time.deltaTime; // Stun recover speed
					yield return 0;
				}
				// No longer dizzy/stunned.
				Stunned = false;
				_stunAmount = 0;
				if (myDizzyParticle && _dizPartEmit && _dizPartEmit.emit)
					_dizPartEmit.emit = false;
			}
			else
			{
				yield return new WaitForSeconds(stunTimeAfterStop);
			}

			// No longer hurt. Resetting this will bring us back into our idle
			// state.
			anim.SetBool("IsHurt", false);
			anim.SetInteger("HurtOther", 0);
			// Wait until we are in a NotBusy state again.
            while(!BaseStateInfo.IsTag("NotBusy") && anim.GetInteger("Health") > 0)
				yield return new WaitForSeconds(0.0001f);
			// After recovering and back in idle or falling, reset attacker's combo.
			if(hurtOther != 2 && attacker != null)
			{
				if(charAttacking == null)
					charAttacking = attacker.GetComponent<CharacterAttacking>();
				if(charAttacking.StartedComboEnemy == myTransform)
					charAttacking.ComboChange(false);
				if(IsEnemy)
				{
					if(Manager_Targeting.instance.playerTargets.Count > 1)
					{
						foreach(Transform player in Manager_Targeting.instance.playerTargets)
						{
							if(player != attacker)
							{
								CharacterAttacking pAttacking = player.GetComponent<CharacterAttacking>();
								if(pAttacking.StartedComboEnemy == myTransform)
									pAttacking.ComboChange(false);
							}
						}
					}
				}
			}
			if(AIOn)
				_characterAI.Agent.Resume(); // Have our Agent guide us again.
			// An attempt for the AI to change its targeted character to the
			// attacker who attacked them if they don't have them targeted already.
			if(AIOn)
			{
				if(_charAttacking.TargetedCharacter != attacker && Random.value > 0.4f)
					_characterAI.ChangeTarget(attacker);
			}
			// If we were knocked down, start the flashing setup while we aren't
			// vulnerable to show that.
			if(hurtOther == 2)
			{
				StartFlash(0.2f, 0.3f, 2, FlashType.Color_Change);
			}
		}
        if(anim.GetInteger("Health") < 1)
		{
			// No more health to display
			healthFill.enabled = false;
			// This is where all the Die-relating things happen.
			StartCoroutine(Die (attacker, charAttacking, hitDir));
		}
		StopCoroutine ("GotHit");
	}

	public IEnumerator WasGrabbed(Transform grabMount)
	{
		// If HurtOther == 4, that means we are currently grabbed. If that is
		// the case, we exit out since we already are grabbed. That could happen
		// from another character attempting to grab us when another character
		// already is. We will make them lose their grab.
		if(anim.GetInteger("HurtOther") == 4 || Stats[0] <= 0)
		{
			// Make other grabber lose their grab since we already are grabbed by
			// someone else.
			if(grabMount)
				grabMount.SendMessageUpwards("GrabBroken");
			StopCoroutine("WasGrabbed");
			yield break;
		}
		// Reset these just in case. No being stunned when grabbed.
		// We won't reset _stunAmount though.
		Stunned = false;
		if (myDizzyParticle && _dizPartEmit && _dizPartEmit.emit)
			_dizPartEmit.emit = false;
		gameObject.SendMessage("ResetParameters");
		anim.SetBool ("IsHurt", false);
		anim.SetInteger("HurtOther", 4); // Grabbed
		anim.SetTrigger ("WasHit");
		_charMotor.GrabbedSetup(true);
		// We become attached to our grabber's grabMount;
		transform.parent = grabMount;
		myTransform.localPosition = Vector3.zero;
		// We are able to attempt to escape. When they attack or throw us
		// we can't until they are back in their Idle grab state.
		// Make sure we don't move.
		myRigidbody.constraints = RigidbodyConstraints.FreezeAll;
		Animator grabberAnim = grabMount.root.GetComponent<Animator> ();
		if (!grabberAnim)
			Debug.LogWarning("Animator for grabber not found!");
		while (!BaseStateInfo.IsTag("Hurt"))
			yield return 0;
		transform.localPosition = Vector3.zero; // Ensure our local position is zeroed out now.
		transform.rotation = grabMount.rotation;
		float breakFreeTimer = 0;
		float add = 0; // Additional escape boost.

		while(anim.GetInteger("HurtOther") > 3) // While we are grabbed.
		{
			if((!AIOn && !IsEnemy && ( (_playerInput.enabled && Input.GetButtonDown(_playerInput.InputGuard) ) 
                || (_playerInputMobile.enabled && _playerInputMobile.guardButtonPressed != 0 && _playerInputMobile.guardButtonPressed != 2)))
				|| ((AIOn || IsEnemy) && Random.value < _characterAI.evadeRatio))
			{
				// Human players get a larger amount since the AI is able to
				// do this much faster than us pressing a button.
				if(!AIOn && !IsEnemy)
					add = 0.6f;
				else add = 0.05f;
			}
			else add = 0;
			// Make sure we are in our main grabbed state and that our
			// grabber is now in their throwing animation which would
			// mean they are throwing us.
			if (!anim.IsInTransition(0) && BaseStateInfo.IsName("Grabbed")
				&& !grabberAnim.GetCurrentAnimatorStateInfo(0).IsName("Grab_Throw"))
			{
				breakFreeTimer += (Time.deltaTime + add);
				if (breakFreeTimer > 6 || grabberAnim.GetBool("IsHurt"))
				{
					// We escaped the grab!
					anim.SetInteger("HurtOther", 0);
					break;
				}
			}
			else if ((grabberAnim.GetCurrentAnimatorStateInfo(0).IsName("Grab_Throw"))
				|| (grabberAnim.IsInTransition(0) && grabberAnim.GetNextAnimatorStateInfo(0).IsName("Grab_Throw")))
			{
				StopCoroutine("WasGrabbed");
				StartCoroutine(BeingThrown(grabMount, grabberAnim));
				yield break;
			}
			yield return new WaitForSeconds(0.0001f);
		}
		// After getting here, we have successfully broken out of the grab.
		BrokeFromGrab(grabMount);
		StopCoroutine ("WasGrabbed");
	}

	void BrokeFromGrab(Transform grabMount)
	{
		transform.parent = null;
		grabMount.root.SendMessage("GrabBroken"); // Tell our grabber that we are no longer grabbed by them.
		myRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
		_charMotor.GrabbedSetup(false); // Disable our grabbed collider and enable our original capsule again.
		anim.SetBool ("IsHurt", false);
		anim.SetInteger ("HurtOther", 0);
		anim.ResetTrigger("WasHit"); // Just in case its active, shouldn't be.
	}

	// While we are being thrown. Using this to see if our grabber gets hurt so that we
	// can break out still.
	IEnumerator BeingThrown(Transform grabMount, Animator grabberAnim)
	{
		while (anim.GetInteger("HurtOther") != 5) // While not thrown
		{
			if (grabberAnim.GetBool("IsHurt"))
				BrokeFromGrab(grabMount);
			yield return new WaitForSeconds(0.0001f);
		}
		StopCoroutine("BeingThrown");
	}

	public IEnumerator WasThrown(int damage, Transform grabber)
	{
		StopCoroutine ("WasGrabbed"); // No need for these three now.
		StopCoroutine ("GotHit");
		StopCoroutine("BeingThrown");
		Stunned = false;
		if (myDizzyParticle && _dizPartEmit && _dizPartEmit.emit)
			_dizPartEmit.emit = false;
		// Set constraints back to normal.
		myRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
		int damageTaken = damage - Stats [3];
		if(damageTaken < 1)
			damageTaken = 1; // Still minimum of one damage.
		// This is a default direction to be thrown in.
		Vector3 thrownDir = (grabber.forward) + new Vector3(0, 5, 0);
		// Show how much damage we are taken from the throw.
		Manager_Particle.instance.Create3DNumber(damageTaken, myTransform.position + Vector3.up, thrownDir, false);
		int health = TakeDamage (damageTaken, Stats[1]);
		if(health <= 0)
			healthFill.enabled = false;
		if(health < Stats[1] * 0.5f)
		{
			if(health > Stats[1] * 0.25f)
				healthFill.color = Color.yellow;
			else
			{
				healthFill.color = Color.red;
				anim.SetLayerWeight(1, 1);
				_charMotor.LowHealthSetup(true);
			}
		}
		transform.parent = null;
		anim.SetBool ("IsHurt", true);
		anim.SetInteger ("HurtOther", 5); // 5 = thrown
		anim.SetTrigger("WasHit");
		Vulnerable = false;
		myRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
		thrownDir.x *= 3; thrownDir.z *= 3;
		if(!myRigidbody.isKinematic) // Just a safety check.
			myRigidbody.velocity = thrownDir;
		if(health > 0)
		{
			yield return new WaitForSeconds(0.5f); // Wait a moment so we don't collide with anything right away after being thrown, including our thrower.
			_charMotor.GrabbedSetup(false); // Get our trigger collider disabled and enable our regular one again, our capsule.
			// Wait until we are not on the ground anymore.
			while(_charMotor.onGround) // Wait until we leave the ground, if on it.
				yield return 0;

			while(!_charMotor.onGround) // Wait until we land again.
			{
				// Rotate to face our thrower while we are off the ground.
				_charMotor.ManualRotate(thrownDir, anim.GetFloat("HitDir") != -1);
				yield return 0;
			}
			// We landed so now reset these
			anim.SetBool("IsHurt", false);
			anim.SetInteger ("HurtOther", 0);
			// Wait until we are in our idle state again.
			while(!BaseStateInfo.IsTag("NotBusy"))
				yield return 0;
			// Begin flashing for not being vulnerable.
			StartFlash (0.2f, 0.3f, 3, FlashType.Color_Change);
			if ((IsEnemy || AIOn) && _characterAI.Agent)
				_characterAI.Agent.Resume();
		}
		else // The throw killed us.
		{
			StartCoroutine(Die (grabber, grabber.GetComponent<CharacterAttacking>(), thrownDir * 1.5f));
		}

		StopCoroutine ("WasThrown");
	}

	IEnumerator Die(Transform attacker, CharacterAttacking charAttacking, Vector3 hitDir)
	{
		if(!IsEnemy)
			InputChange (false);
		_charMotor.GrabbedSetup(false);
		if(voiceDie)
		{
			if (deadPrefab == null)
				Manager_Audio.PlaySound(_myAudio, voiceDie, false);
			else AudioSource.PlayClipAtPoint(voiceDie, myTransform.position);
		}
		// Stop displaying our target cursor.
		Manager_Targeting.instance.DisableTargetCursors (PlayerNumber, _charAttacking);
		if (myDizzyParticle && _dizPartEmit && _dizPartEmit.emit)
		{
			_dizPartEmit.emit = false;
			Stunned = false;
			_stunAmount = 0;
		}
		gameObject.name += "Dead"; // Now Detection radius' won't find us.
		if(IsEnemy)
		{
			// This enemy is no longer a target.
			if(Manager_Targeting.instance.enemyTargets.Contains(this.transform))
				Manager_Targeting.instance.enemyTargets.Remove(this.transform);
		}
		if(attacker != null)
		{
			// Setup for resetting attacker's combo and changing target for
			// anyone who has this character targeted since they are dead,
			// they shouldn't be targeted anymore.
			if(charAttacking == null)
				charAttacking = attacker.GetComponent<CharacterAttacking>();
			if(charAttacking.StartedComboEnemy == myTransform)
				charAttacking.ComboChange(false);
			if(IsEnemy)
			{
				if(charAttacking.TargetedCharacter == myTransform)
					charAttacking.TargetChange();
				if(Manager_Targeting.instance.playerTargets.Count > 1)
				{
					foreach(Transform player in Manager_Targeting.instance.playerTargets)
					{
						if(player != attacker)
						{
							CharacterAttacking pAttacking = player.GetComponent<CharacterAttacking>();
							if(pAttacking.StartedComboEnemy == myTransform)
								pAttacking.ComboChange(false);
							if(pAttacking.TargetedCharacter == myTransform)
								pAttacking.TargetChange();
						}
					}
				}
			}
		}
		// Additional height added from dying.
		hitDir.y = 3 + (hitDir.y * 2);
		if(IsEnemy)
		{
			// Get rid of our health bar and NavMeshAgent.
			Destroy (healthSlider.transform.parent.gameObject);
			Destroy (_characterAI.Agent.gameObject, 0.1f);
			_characterAI.enabled = false;
			// We are no longer needed in this list.
			Manager_Game.instance.enemiesAll.Remove(this.gameObject);
		}
		Transform myCamTarget = myTransform;
		if(deadPrefab != null) // A ragdoll is assigned.
		{
			// Create our ragdoll and give it velocity based on our hitDir.
			GameObject ragdoll = Instantiate(deadPrefab, myTransform.position, myTransform.rotation) as GameObject;
			Rigidbody ragRigid = ragdoll.GetComponentInChildren<Rigidbody>();
			ragRigid.velocity = new Vector3(hitDir.x * 3, hitDir.y, hitDir.z * 3);
			if(IsEnemy)
			{
				// Our ragdoll is the dead enemy for us now.
				Manager_Game.instance.enemiesDead.Add(ragdoll);
				Destroy(this.gameObject);
				StopCoroutine("GotHit");
				yield break;
			}
			else
			{
				// Make the cam switch its current target of this character with
				// the ragdoll. Use the one with the rigidbody on it.
				Camera_BEU.instance.ChangeTarget(myTransform, ragRigid.transform);
				myCamTarget = ragRigid.transform;
				// Make it so we can't be seen.
				foreach(SkinnedMeshRenderer skinMesh in mySkinnedMeshes)
					skinMesh.enabled = false;
				// We disable ourselves while the ragdoll is there.
				myRigidbody.isKinematic = true;
				GetComponent<Collider>().enabled = false;
				yield return new WaitForSeconds(4);
				if(playerLives > 1) // We can respawn.
				{
					// Change the cam back to us in place of our ragdoll.
					Camera_BEU.instance.ChangeTarget(ragRigid.transform, myTransform);
					Destroy (ragdoll); // No longer need the ragdoll.
				}
			}
		}
		else // Die using an animation.
		{
			if(IsEnemy && !Manager_Game.instance.enemiesDead.Contains(this.gameObject))
				Manager_Game.instance.enemiesDead.Add(this.gameObject);
		}
		if(IsEnemy)
			_charAttacking.enabled = false;
		else
		{
			if(playerLives > 1)
			{
				if(deadPrefab == null) // Died using an animation.
				{
					yield return new WaitForSeconds(2.9f); // Dead time
					foreach(SkinnedMeshRenderer skinnedMesh in mySkinnedMeshes)
						skinnedMesh.enabled = false;
					yield return new WaitForSeconds(0.1f);
				}
				Respawn();
				yield return new WaitForSeconds(1.5f);
			}
			else // No more lives left.
			{
                Manager_Game.instance.LiveChange(PlayerNumber, -1);
				// We are no longer an active player in the game...
				if(Manager_Targeting.instance.playerTargets.Contains(this.transform))
					Manager_Targeting.instance.playerTargets.Remove(this.transform);
                Manager_Game.instance.allPlayerStatus.Remove(this);
				if(!Manager_Game.playersDefeated.Contains(this.gameObject))
					Manager_Game.playersDefeated.Add(this.gameObject);
				// The Manager_UI will now not display our results at the end of
				// the stage, provided that another player makes it.
				if(PlayerNumber == 1)
					Manager_UI.ShowP1Results = false;
				else if(PlayerNumber == 2)
					Manager_UI.ShowP2Results = false;
                else if(PlayerNumber == 3)
                    Manager_UI.ShowP3Results = false;
                else if(PlayerNumber == 4)
                    Manager_UI.ShowP4Results = false;
				// The camera doesn't need to target us anymore.
				Camera_BEU.instance.targets.Remove(this.transform);
				if(Manager_Game.playersDefeated.Count == Manager_Game.NumberOfPlayers)
				{
					// Game Over!
					Manager_Game.instance.Invoke("GameOver", 1);
				}
				else // Other players remain.
				{
					// Wait until the battle ends.
					while(Manager_BattleZone.instance.InBattle)
						yield return 0;
					Camera_BEU.instance.RemoveTarget(myCamTarget);
					// Next is where we control an AI player who may still
					// be active in the game using our controls while keeping
					// their player number.
					foreach(Transform player in Manager_Targeting.instance.playerTargets)
					{
						if(player)
						{
							CharacterStatus cStatus = player.GetComponent<CharacterStatus>();
							if(cStatus.AIOn)
							{
                                if(player == Manager_Targeting.instance.playerTargets[0])
								    cStatus.InputChange(true, true, PlayerNumber);
                                cStatus.SendMessage("FindLeadPlayer");
							}
						}
					}
					// We will flash away and then be deactivated if we do not have a ragdoll
					if(!deadPrefab)
						StartFlash(0.2f, 0.6f, 3, FlashType.Renderer_Disable);
				}
			}
		}
		StopCoroutine ("Die");
	}
}