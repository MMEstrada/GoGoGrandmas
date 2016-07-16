using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// Character AI. The AI will first move towards the battle area after spawning.
/// Once they have made it, they will choose a random player and target them.
/// If the number of enemies around the player is less than the total number of player targets + 1, they will go after
/// them and attack when within the minAttackDist getting on a side of the player
/// that is open. Otherwise, they will wander around and wait. I have both the
/// players and enemies do this behaviour.
/// If items are present, the AI will have a random chance of going after one of
/// them if there are no opposing characters nearby them.
/// TargetForNavMesh relates to the destination transform that the AI will go to,
/// whether it be a player, enemy, or item. They will choose accordingly
/// depending on which AI state they are currently in. This will allow the AI to
/// remain having a player target (characterAttacking.TargetedCharacter) and have
/// their characterStatus still as well as their detectionRadius info even if they
/// have an item for the targetForNavMesh. This allows them to still guard, dodge,
/// or attack when needed.
/// </summary>

public class CharacterAI : Character
{
	// Serialize also makes them visible in the inspector, even when private.
	[Range(0.4f, 1.5f)] [SerializeField] public float minAttackDist;
	[Range(0.8f, 2.5f)] [SerializeField] public float maxAttackDist;
	// The max combo allowed for this character to go to. Used with difficulty.
	public int maxCombo = 6;
	// This is basically the % chance the character will attack or evade,
	// respectively, when they can. Used with Random.value < _attackRatio for
	// example.
	public int attackRatio = 20;
	public int evadeRatio = 10;

	public int healthBelowForHealthItem = 2; // How much health less the other player will need to have than ours before we consider grabbing a healing item. Make this negative to have it go the other way.
	// I made bools for these so you can decide if the character can do these
	// things. I say easier enemies should be more limited while harder ones
	// could do all of them. I just base it off of the difficulty setting for
	// this project.
	public bool canUseGrabs;
	public bool canUseAirAttacks;
	public bool canGetItems;

	CharacterStatus _charStatus;
	CharacterMotor    _charMotor;
	CharacterAttacking _charAttacking;
	CharacterItem _charItem;
	// Our current destination target for our NavMeshAgent.
	public Transform targetForNavMesh;
	// A leader player we are following, for players only. This will be
	// set to a human player to follow.
	Transform leadPlayer;
	Vector3 targetPos; // Current position for our nav mesh.
	string[] _healingItemNames; // Place healing item names in here for the AI to check. Done in Start()
	// How long we have been been wandering around a chosen spot. Used for
	// making the character move to another random spot some time after.
	float _wanderingSpotTime = 0;
	// A timer for when the character chooses a random spot around a targeted
	// character while pursuing.
	float _choseRandomOffsetTimer = 0;
	// A timer for changing to a closer target every so often.
	float _changeNewTargetTimer = 0;
	// Which direction are we strafing in? Less than 0 = left, 0 = none, and 
	// greater than 0 = right.
	float _strafeDir = 0;
	float distFromTarget;
	int _hitsWhileGrabbing = 0; // Number of times the AI hit a held character.

    // Hashes
    int _atk_Light_1;
    int _atk_Light_2;

	// I use this to allow the AI to only change a strafe direction when this is
	// true so they don't keep constantly choosing a new one.
	bool _canChooseStrafeDir = true;
	bool jump = false;
	bool _choseNewPath; // If our path has gone outside of the navmesh bounds when retreating, we will choose a new path.
	// Did we actually choose a random offset from our targeted character.
	bool _chosenRandomOffset;
	bool _wanderSpotSet; // If wandering, we check to see if we have set up a random spot to move to around a targeted character.
	bool _enteredArea; // If we are now active and ready to target players (for enemies only since the players automatically set this to true)
	Vector3 _randomOffset = Vector3.zero; // Our random position to move to when wandering.
	GameDifficulty _difficulty; // Used for easier access to the game's difficulty.
	// References to a target's Status and Detection Radius to see how many enemies
	// are around them. That goes for enemies only.
	CharacterStatus targetCharStatus;
	DetectionRadius targetDetRadius;
	public AIStates aiState; // Our current AI state.
	// Access to our NavMeshAgent on our Nav Agent GameObject
	public NavMeshAgent Agent {get; private set;}
	public float StrafeDir { get { return _strafeDir; } } // Current amount we are strafing.

	void Awake()
	{
		_charStatus = GetComponent<CharacterStatus> ();
		_charMotor = GetComponent<CharacterMotor> ();
		_charAttacking = GetComponent<CharacterAttacking> ();
		_charItem = GetComponent<CharacterItem> ();
		Agent = GetComponentInChildren<NavMeshAgent> ();
		Agent.stoppingDistance = 0.1f; // default distance to stop from a target that I use.
	}

	void Start()
	{
		detectionRadius = GetComponentInChildren<DetectionRadius> ();
		anim = GetComponent<Animator> ();
		// I only have two healing items, so I made the length of this array 2, and placed them inside.
		// Change this as needed to suit your own game.  I use this when the AI looks for items since they
		// always go after the healing ones first.
		_healingItemNames = new string[2] { "Apple", "Orange" };
        _atk_Light_1 = Animator.StringToHash("Atk_Light_1");
        _atk_Light_2 = Animator.StringToHash("Atk_Light_2");
		if(!_charStatus.IsEnemy) // If we are a player.
		{
			_enteredArea = true;
			Invoke ("FindLeadPlayer", 1);
		}
		else
		{
			// We now know what difficulty the game is in.
			_difficulty = Manager_Game.Difficulty;
			SetupDifficultyThings (); // Setup things based on that info.
		}
	}

	void FindLeadPlayer()
	{
		// The leader player at the start will simply be the first player
		// created by the Manager_Game (player one) as they will always be
		// human controlled.
		leadPlayer = Manager_Targeting.instance.playerTargets [0];
	}

	void Update()
	{
		if(Manager_Cutscene.instance.inCutscene || Manager_Game.instance.IsPaused)
			return;

		if(_charStatus.Busy)
		{
			// If we are in an attack state...
			if(_charStatus.BaseStateInfo.IsTag("Attack") && !_charStatus.BaseStateInfo.IsName("Atk_Slide"))
			{
				// If we landed an attack and are no longer in attack hit delay...
				if(Manager_BattleZone.instance.InBattle && anim.GetBool("AttackHit") && _charAttacking.HitDelayTime == 0)
				{
					// Atk_HeavyHigh is the launch attack state that sends the
					// hit character into the air setting them up for air combos.
					if(!_charStatus.BaseStateInfo.IsName("Atk_HeavyHigh"))
					{
						if(!_charStatus.BaseStateInfo.IsName("Atk_Heavy_N")) // Only bother if we are not in our finisher.
						{
							// Choose a combo if we haven't achieved a long enough one
							// past our max given only if there is only one character in close range,
							// or we are an enemy and are not in hard.  Otherwise we will attempt to
							// dodge since you can dodge after an attack hits. AI players act more
							// like hard AI enemies.
							bool chooseToCombo = (!IsEnemy && detectionRadius.inCloseRangeChar.Count < 2)
								|| (IsEnemy && (_difficulty != GameDifficulty.Hard || detectionRadius.inCloseRangeChar.Count < 2));
							if(chooseToCombo && _charAttacking.Combo < maxCombo)
								ComboSetup();
							else if(!chooseToCombo && detectionRadius.inCloseRangeChar.Any(enemy => enemy.GetComponent<CharacterAttacking>().IsAttacking))
								_charAttacking.DodgeSetup(Random.Range(-1, 2), Random.Range(-1, 2));
						}
					}
					else // We launched the target up into the air, so we get ready to jump to pursue them.
					{
						if(canUseAirAttacks && (_charAttacking.TargetedCharacter && _charAttacking.TargetedCharacter.position.y > myTransform.position.y + 1.2f))
						{
							if(Random.value > 0.6f)
							{
								_charAttacking.AnimEventActivateHitbox(0);
								// True here is for jumping, no need to move (Vector3.zero)
								// Just jump.
								_charMotor.Move (Vector3.zero, true, false);
							}
						}
					}
				}
			}
			else if(_charStatus.BaseStateInfo.IsName("Guard_Hit"))
			{
				// Counter after being hit when guarding. Can only be done
				// before 80% of the animation has finished. Can be lowered if
				// desired.
				if(!anim.IsInTransition(0) && _charStatus.BaseStateInfo.normalizedTime < 0.8f
				   && Random.Range(0, 100) < attackRatio)
					_charAttacking.AttackSetup(1, 0);
			}
			// Next is where we attack or throw a grabbed character.
			else if(_charStatus.BaseStateInfo.IsTag("Grab"))
			{
				if(!anim.IsInTransition(0))
				{
					// Grab_Idle is the base grabbing state where we can
					// choose to throw or hit a grabbed character.
					if(_charStatus.BaseStateInfo.IsName("Grab_Idle"))
					{
						// I only make the AI hit a grabbed character no more
						// than 4 times (> 3). If they have, then they will
						// throw them.
						if(Random.value > 0.9f || _hitsWhileGrabbing > 3)
							_charAttacking.GrabAttackSetup(2); // Throw em'
						else
						{
							if(Random.Range (0, 100) < attackRatio)
							{
								_hitsWhileGrabbing++;
								_charAttacking.GrabAttackSetup(1); // Hit em'
							}
						}
					}
				}
			}
			else
			{
				// If we are guarding.
				if(anim.GetBool("IsGuarding"))
				{
					// If there are no more opposing characters nearby or
					// our targetedCharacter is no longer attacking.
					if(detectionRadius.inCloseRangeChar.Count == 0
					   || (targetCharStatus && !targetCharStatus.AmIAttacking))
					{
						if(anim.GetBool("IsGuarding") && Random.value > 0.5f)
						{
							// Stop guarding.
							anim.SetBool("IsGuarding", false);
						}
					}
				}
			}
			return;
		}
		// Checks to see if any items are present and nearby.
		bool itemsCloseNearby = detectionRadius.inGrabRangeItems.Count > 0;
		bool itemsNearby = detectionRadius.inRangeItems.Count > 0
			|| detectionRadius.itemStorersInRange.Count > 0;
		// See if player 1's health is less than ours - healthBelowForHealthItem (random amount), only applies to players of course.
		// Since there is only one other player, we check all players by using Any and see if the status we are checking
        // is not our own and if its health Stats[0] is less than our by our chosen amount. I use
		// this bool anytime we check for healing items below.
        bool letTeamMateGet = !IsEnemy && (Manager_Game.instance.allPlayerStatus.Any(status => status != _charStatus && status.Stats[0] < _charStatus.Stats[0] - healthBelowForHealthItem) );
		// Here is where I make the AI get rid of an item they are holding if they
		// find a healing item/collectable nearby if they have less than 90% health.
		if(_charItem.ItemHolding != null && (_charStatus.Stats[0] < _charStatus.Stats[1] * 0.9f))
		{
			// If there is a healing item nearby we seek it out by getting
			// rid of an item we are holding so we can grab that one and heal
			// ourselves, unless we are a player and player 1's health is less than ours.
			if(itemsNearby && detectionRadius.inRangeItems.Count > 0
				&& detectionRadius.inRangeItems.Any(item => item != null && _healingItemNames.Any(itemName => item.name.Contains(itemName))))
			{
				if(!letTeamMateGet && Random.value > 0.96f)
				{
					// Holding a weapon
					if(anim.GetInteger("HoldStage") == 3)
						_charItem.DropItem ();
					// Holding a throwable
					else if(anim.GetInteger("HoldStage") == 1)
						_charItem.Throw();
					// Target that item.
					targetForNavMesh = detectionRadius.inRangeItems.Find(item => item != null && _healingItemNames.Any(itemName => item.name.Contains(itemName)));
					aiState = AIStates.Pursue_Item;
				}
			}
		}

		// This next part is only for players when you aren't in battle. The AI
		// will follow the leader player, when they aren't going after an item that
		// is. Same goes for if there are no enemy targets present.
		if(!IsEnemy && (!Manager_BattleZone.instance.InBattle || Manager_Targeting.instance.enemyTargets.Count == 0))
		{
			if(_strafeDir != 0)
				_strafeDir = 0;
			if(!leadPlayer)
				return;
            if(itemsNearby)
			{
				if(Random.Range(0f, 20f) > 19.5f)
				{
					// First check to see if there are any item containers nearby. If not, next
					// check to see if there are items nearby, and if so, we look through them all and
					// see if any of their names contain one from our _healingItemNames array, otherwise,
					// we simply check to see that none of any of them are not a healing item by using All()
					// on the _healingItemNames array.
					if( detectionRadius.itemStorersInRange.Count > 0 ||
						(detectionRadius.inRangeItems.Count > 0 && (!letTeamMateGet
							&& detectionRadius.inRangeItems.Any(item => item != null && _healingItemNames.Any(itemName => item.name.Contains(itemName))) )
							|| (detectionRadius.inRangeItems.Any(item => item != null && _healingItemNames.All(itemName => !item.name.Contains(itemName))) ))
						|| (!letTeamMateGet && _charStatus.Stats[0] < _charStatus.Stats[1])) // Do not have full health.
					{
						aiState = AIStates.Pursue_Item;
						targetForNavMesh = FindNearestTarget();
					}
				}
			}
			// If we aren't pursuing an item, lets follow the leader player.
			if(aiState != AIStates.Pursue_Item && targetForNavMesh != leadPlayer)
			{
				if(Agent.stoppingDistance != _charStatus.PlayerNumber - 1)
					Agent.stoppingDistance = _charStatus.PlayerNumber - 1;
				targetForNavMesh = leadPlayer;
			}
			// Otherwise we want to get an item nearby first.
			else if(aiState == AIStates.Pursue_Item)
			{
				if(targetForNavMesh && (targetForNavMesh.tag == "Item"
				   || targetForNavMesh.tag == "ItemStorer"))
				{
					if(Agent.stoppingDistance != 0.3f)
						Agent.stoppingDistance = 0.3f;
					if(itemsCloseNearby)
					{
						if(_charStatus.BaseStateInfo.IsTag("NotBusy") &&
						   _charMotor.onGround)
						{
							if(Random.value > 0.98f)
							{
								_charItem.Pickup();
							}
						}
					}
					if(detectionRadius.NextToItemStorer)
					{
						FaceCharacterTarget(); // Face the item container.
						if(Random.value > 0.97f)
						{
							// Use low attack (-1 second parameter)
							_charAttacking.AttackSetup(1, -1);
						}
					}
				}
				if (!itemsNearby || _charItem.ItemHolding != null || targetForNavMesh == null)
					aiState = AIStates.Wander;
			}

			if(targetForNavMesh)
			{
				this.targetPos = targetForNavMesh.position;
				Agent.SetDestination(targetPos);
				// update the agent's position
				Agent.transform.position = myTransform.position;
				Vector3 moveDir = Agent.desiredVelocity.normalized;
				distFromTarget = Vector3.Distance(myTransform.position, targetForNavMesh.position);
				float distToSlowDown = (aiState != AIStates.Pursue_Item) ? 3 : 1.2f;
				if(distFromTarget < distToSlowDown) moveDir *= (aiState != AIStates.Pursue_Item ? 0.5f : 0.2f); // Walk or move very slow if pursuing an item and near it.
				// If the desired velocity is that small, just zero it out.
				// A personal preference of mine to aid in preventing the
				// character from doing unnecessary small movements.
				if(moveDir.magnitude < 0.2f)
					moveDir = Vector3.zero;
				_charMotor.Move(moveDir, false, _charAttacking.TargetedCharacter && distFromTarget < 3 && aiState != AIStates.Retreat); // AI will go into targeting and strafing mode if less than 3 units away from their targeted character and not retreating.
			}
			return;
		}
		// Have enemies just wander if there are no players around to target.
		// That would be in the case of a Game Over. If we don't have a
		// targetForNavMesh, we will simply choose random spots to move to around
		// our current location.
		if(IsEnemy && Manager_Targeting.instance.playerTargets.Count == 0)
		{
			aiState = AIStates.Wander;
			Wander ();
		}
		// Reset any hits given while grabbing a character after leaving
		// grabbing by either throwing or our grab being broken out of.
		if(_hitsWhileGrabbing > 0)
		{
			if(_charStatus.BaseStateInfo.IsName("Grab_Throw")
			   || _charStatus.BaseStateInfo.IsName("Grab_Break"))
				_hitsWhileGrabbing = 0;
		}
		// Players who have chosen a new path have a different stopping distance, the end boundary in the
		// current battle area's x and z position with their own y position.
		if(Agent.stoppingDistance != 0.1f && (IsEnemy || (!IsEnemy && !_choseNewPath)))
			Agent.stoppingDistance = 0.1f;
		distFromTarget = 10;
		float enemiesNearPlayer = 0;
		// This is the only place where a new targetForNavMesh is found for sake of
		// convenience. When you want to find a new one at some point, I make the
		// current one null, so during the next frame it will find a new one here
		// depending on what our current AI state is.
		if(aiState != AIStates.Spawn)
		{
			// I don't want players (!IsEnemy) to have another player targeted when
			// reaching this point since we are in battle.
			if(targetForNavMesh == null || (!IsEnemy && targetForNavMesh.tag == "Player"))
				targetForNavMesh = FindNearestTarget();
			else
			{
				if(targetForNavMesh.tag == "Item")
					distFromTarget = Vector3.Distance(myTransform.position, targetForNavMesh.position);
				else
				{
					if(_charAttacking.TargetedCharacter)
						distFromTarget = Vector3.Distance(new Vector3(myTransform.position.x, 0, myTransform.position.z),
						                                  new Vector3(_charAttacking.TargetedCharacter.position.x, 0, _charAttacking.TargetedCharacter.position.z));
				}
			}
			// An easy way to find out how many enemies are close to our
			// targeted character.
			if(targetDetRadius != null)
				enemiesNearPlayer = targetDetRadius.inCloseRangeChar.Count;
		}

		if(aiState != AIStates.Pursue)
			if(_strafeDir != 0)
				_strafeDir = 0; // Reset strafing if not pursuing.
		if (targetForNavMesh)
		{
			Vector3 move = Agent.desiredVelocity;
			float maxDistForRetreat = 5;
			// update the progress if the character has made it to the previous target
			if(aiState != AIStates.Retreat)
				targetPos = targetForNavMesh.position + _randomOffset;
			else // Retreating
			{
				// I found this part here works well for retreating from a target.
				if(targetForNavMesh && !_choseNewPath)
				{
                    if(distFromTarget < maxDistForRetreat)
                    {
                        targetPos = myTransform.position + ( (myTransform.position -
                            (targetForNavMesh.position) ) * maxDistForRetreat); // Go out at least as far as the distance you are checking against (maxDistForRetreat) so that the enemy stays away nicely
                    }
					else move = Vector3.zero; // Far enough away, don't need to move so we can face our target, done below with MinMovement on EnemyMotor.
				}
			}
			// If our retreat position has gone outside of the bounds of the navmesh...
			if (!Agent.hasPath && !_choseNewPath && _enteredArea && aiState == AIStates.Retreat)
			{
				// We will use the position of the end boundary for the current battle area, and use our
				// y position instead.
				Vector3 newPos = Manager_BattleZone.instance.zonesEnd[Manager_BattleZone.instance.currentAreaNumber].transform.position;
				newPos.y = myTransform.position.y + 0.05f;
				targetPos = newPos;
				if (!IsEnemy)
					Agent.stoppingDistance = 3; // The players can't get close enough to the end boundary since they can't go through its collider like the enemies so I increase this.
				_choseNewPath = true; // Now we can't change our new position until we get out of retreat since it would interfere.
			}
			Agent.SetDestination(targetPos);
			// update the agent's position 
			Agent.nextPosition = myTransform.position;
			if(move.magnitude < 0.1f)
				move = Vector3.zero;
			// This bool refers to if we should rotate and face our target, whether we are close enough and not retreating,
			// or far away enough when retreating.
			bool faceTarget = _charAttacking.TargetedCharacter && ((distFromTarget < 3 && aiState != AIStates.Retreat) || (aiState == AIStates.Retreat && distFromTarget > maxDistForRetreat));
			// use the values to move the character
			_charMotor.Move(move, jump, faceTarget);
		}
		else // If no target for nav mesh
		{
			// We still need to call the character's move method, but we send zeroed input as the move param.
			_charMotor.Move(Vector3.zero, jump, false);
		}
		if(jump) // Reset this in case we jumped above.
			jump = false;
		// This will be true if there are any opposing characters close by us.
		bool charCloseNearby = detectionRadius.inCloseRangeChar.Count > 0;

		if(targetForNavMesh == null)
			return;
		// A random chance of choosing a closer target if there is more
		// than one available.
		if(_charAttacking.TargetedCharacter)
		{
			// Choose appropriate list based on if we are an enemy or a player.
			List<Transform> charChecking = Manager_Targeting.instance.playerTargets;
			if(!IsEnemy) charChecking = Manager_Targeting.instance.enemyTargets;

			_changeNewTargetTimer += Time.deltaTime;
			if(_changeNewTargetTimer > Random.Range(4, 8))
			{
				_changeNewTargetTimer = 0;
				foreach(Transform character in charChecking)
				{
					if(character != null && character != _charAttacking.TargetedCharacter)
					{
						if(Vector3.Distance(myTransform.position, character.position) < distFromTarget)
						{
						   if(Random.value > 0.5f)
							{
								// Reset so that we will find a new target on the
								// next frame which will be closer since
								// FindNearestTarget() does just that.
								ResetPlayerTarget();
								return;
							}
						}
					}
				}
			}
		}
		// I put these here so that an enemy will always be able to attack or guard
		// when nearby a targeted character. If their target is dead, they will
		// reset it. If there are any nearby characters will be ready to guard or
		// dodge if possible.
		if(targetCharStatus)
		{
			if(charCloseNearby)
				AttemptGuardOrDodge ();
			AttemptAttack (distFromTarget);
			if(targetCharStatus.Stats[0] < 0)
			{
				ResetPlayerTarget();
				return;
			}
		}
		// Here, in each state, I specify specific unique things the character
		// will do in each state. Pursuing for example, they will choose a spot
		// around their targeted character to move to and attack when close.
		// Going into other states from the current state are done here as
		// well.
		switch(aiState)
		{
		case AIStates.Wander:
			// Choose random spots around a target to move to.
			Wander ();
			if(targetForNavMesh.tag == "Player" && targetCharStatus && targetCharStatus.Vulnerable
					&& enemiesNearPlayer < Manager_Targeting.instance.playerTargets.Count + 1)
			{
				if(Random.Range(-10f, 10f) > 9f)
					aiState = AIStates.Pursue;
			}
			// If we are able to get items and there are some nearby, have a
			// chance at going after them if no opposing characters are close by.
			if(canGetItems && itemsNearby && !charCloseNearby)
			{
				if(Random.Range(0f, 20f) > 18f)
					aiState = AIStates.Pursue_Item;
			}
			break;

		case AIStates.Pursue:
			if(targetForNavMesh.tag == "Player" || targetForNavMesh.tag == "Enemy")
			{
                if(!IsEnemy || (enemiesNearPlayer < Manager_Targeting.instance.playerTargets.Count + 1))
				{
						// Stop any strafing if too far away from our target.
					if (distFromTarget > 3 && _strafeDir != 0)
						_strafeDir = 0;
					if(targetCharStatus.Vulnerable)
					{
					  // Choose a random spot to go to near a target and make sure
					  // there isn't already an opposing character there.
						if(!_chosenRandomOffset && targetDetRadius)
						{
							if(!targetDetRadius.FoeNearBack && Random.Range(-20f, 20f) > 19f)
							{
								_chosenRandomOffset = true;
								_randomOffset = -targetForNavMesh.forward * 0.95f;
							}
							else if(!targetDetRadius.FoeNearRight && Random.Range(-20f, 20f) > 19f)
							{
								_chosenRandomOffset = true;
								_randomOffset = targetForNavMesh.right * 0.95f;
							}
							else if(!targetDetRadius.FoeNearLeft && Random.Range(-20f, 20f) > 18.8f)
							{
								_chosenRandomOffset = true;
								_randomOffset = -targetForNavMesh.right * 0.95f;
							}
						}
						else
						{
							// A random chance to choose a strafe direction only if
							// our targeted character is close by.
							if(charCloseNearby && detectionRadius.inCloseRangeChar.Contains(targetCharStatus.transform)
							   && _canChooseStrafeDir)
							{
								if(Random.value > 0.5f)
									_strafeDir = -1;
								else _strafeDir = 1;
								_canChooseStrafeDir = false;
							}
							// If they aren't nearby... stop strafing.
							else if(!charCloseNearby || !detectionRadius.inCloseRangeChar.Contains(targetCharStatus.transform))
							{
								if(_strafeDir != 0 && Random.value > 0.98f)
								{
									_strafeDir = 0;
									_canChooseStrafeDir = true;
								}
							}
							// Reset our current strafe direction after a set time
							// so we can choose a new one.
							_choseRandomOffsetTimer += Time.deltaTime;
							if(_choseRandomOffsetTimer > Random.Range(4, 6))
							{
								if(!_canChooseStrafeDir && Random.value > 0.99f)
									_canChooseStrafeDir = true;
								if(_choseRandomOffsetTimer > Random.Range(8f, 14f))
								{
									_chosenRandomOffset = false;
									_choseRandomOffsetTimer = 0;
									_canChooseStrafeDir = true;
								}
							}
						}
					}
					else
					{
						// Make sure our target is alive (health > 0)
						if(targetCharStatus.Stats[0] > 0)
						{
							if(!targetCharStatus.BaseStateInfo.IsTag("Dodging")
							   && distFromTarget < 3 && Random.Range(0f, 10f) > 9f)
								aiState = AIStates.Retreat;
						}
						else // They are dead.
						{
							// Reset our target and we will choose another on the
							// next frame from the same location way above.
                            FindNearestTarget();
							return;
						}
					}
				}
				else // A go back to wander chance if they are too many opposing characters already around our target.
				{
					if(Random.Range(0f, 10f) > 9.8f)
						aiState = AIStates.Wander;
				}
			}
			else if(targetForNavMesh.tag == "Item")
			{
				// Go after that item we have targeted if no opposing characters
				// are close by.
				if(canGetItems && !charCloseNearby)
					aiState = AIStates.Pursue_Item;
				// Reset this regardless so we can choose a new targetForNavMesh.
				targetForNavMesh = null;
				return;
			}
			// If all of these are true, we will have a chance of going at a
			// nearby item.
			if(canGetItems && !charCloseNearby && itemsCloseNearby)
			{
				if(Random.Range(0f, 20f) > 19.2f)
				{
					aiState = AIStates.Pursue_Item;
					targetForNavMesh = null;
					return;
				}
			}
			break;
			// When retreating, we wait until our targeted player is vulnerable
			// again. There is a chance that targetCharacterStatus could become
			// null if the player were to die, so if that were to happen, we
			// would reset our player target and return to wandering. A new target
			// will be chosen again if one is available.
		case AIStates.Retreat:
			if(targetCharStatus != null)
			{
                if (targetCharStatus.Stats[0] > 0)
                {
                    if (!targetCharStatus.Vulnerable)
                    {
                        // If far enough away, I make the character face their target
                        // while they are in retreat and in idle.
                        if (distFromTarget > 5)
                            FaceCharacterTarget();
                    }
                    else
                    {
                        // Try to pursue again after they are vulnerable.
                        if (Random.Range(0f, 10f) > 9.7f)
                        {
                            if (_choseNewPath)
                                _choseNewPath = false;
                            aiState = AIStates.Pursue;
                        }
                    }
                }
                else
                {
                    FindNearestTarget();
                    return;
                }
			}
			else
			{
				if(Random.value > 0.97f)
				{
					if (_choseNewPath)
						_choseNewPath = false;
					ResetPlayerTarget();
					aiState = AIStates.Wander;
					return;
				}
			}
			break;

		case AIStates.Pursue_Item:
			if(!canGetItems)
				aiState = AIStates.Wander;

			// Get rid of any random offset when going after items since we
			// don't want it. We just want the item's position.
			if(_randomOffset.magnitude > 0)
				_randomOffset = Vector3.zero;
			// If we are currently holding an item.
			if(_charItem.ItemHolding != null)
			{
				targetForNavMesh = null;
				aiState = AIStates.Pursue;
				return;
			}
			else // Not holding an item.
			{
				if(!itemsNearby)
				{
					targetForNavMesh = null;
					aiState = AIStates.Pursue;
					return;
				}
			   // Make sure there are no players nearby before going after an item.
				if(!charCloseNearby)
				{
					if(targetForNavMesh.tag == "Item")
					{
						if(itemsCloseNearby)
						{
							// Attempt to pick up an item in grab range.
							if(Random.value > 0.98f)
								_charItem.Pickup();
						}
					}
					// We reset this so at the top we will begin searching for
					// a new target, an item this time which is done in the
					// FindNearestTarget() method.
					else
					{
						targetForNavMesh = null;
						return;
					}
				}
				else // There are opposing characters nearby.
				{
					// We will attempt to go after them.
					if(Random.Range(0f, 20f) > 19.4f)
						aiState = AIStates.Pursue;
				}
			}
			break;
		}
	}

	void OnEnable()
	{
		// Reenable our Agent if it isn't active.
		if(Agent)
		{
			if(!Agent.gameObject.activeSelf)
				Agent.gameObject.SetActive(true);
			Agent.Resume();
		}
		if(!IsEnemy)
			aiState = AIStates.Wander; // Default state upon enable for players.
	}

	// The different ratios based on the current difficulty.
	// The ratios are based on a percent since I use them in a
	// Random.Range from 0 to 100.
	void SetupDifficultyThings()
	{
		switch(_difficulty)
		{
		case GameDifficulty.Easy:
			attackRatio = 5;
			evadeRatio = 8;
			maxCombo = 4;
			canUseGrabs = canUseAirAttacks = false;
			break;
		case GameDifficulty.Normal:
			attackRatio = 11;
			evadeRatio = 16;
			maxCombo = 7;
			canUseGrabs = false;
			canUseAirAttacks = true;
			break;
		case GameDifficulty.Hard:
			attackRatio = 17;
			evadeRatio = 24;
			maxCombo = 10;
			// We can do all things.
			canUseGrabs = canUseAirAttacks = true;
			break;
		}
        if(Manager_Game.usingMobile && IsEnemy) // Slightly easier AI when using mobile.
        {
            attackRatio = Mathf.RoundToInt(attackRatio * 0.8f);
            evadeRatio = Mathf.RoundToInt(evadeRatio * 0.8f);
            maxCombo--;
        }
	}

	void ResetPlayerTarget()
	{
		targetForNavMesh = null;
		targetCharStatus = null;
		_charAttacking.TargetedCharacter = null;
	}

	void ComboSetup()
	{
		// If we have reached our maxCombo, no more comboing then. Or if
		// perhaps we no longer have a targeted character. Also make sure this we
		// are in the final hit of our attack animation. All of my attacks only
		// have one hit so this doesn't really apply right now, but make sure
		// any multiple hit attack animations you have will have FinalHit set
		// on the actual final hit. Check HitboxProperties for more info.
		// Also for enemies, if not in hard, a random chance of exiting before comboing.
		if(_charAttacking.Combo >= maxCombo || !Manager_BattleZone.instance.InBattle
		   || !_charAttacking.FinalHit || (IsEnemy && ( (_difficulty == GameDifficulty.Easy && Random.value > 0.4f)) || (_difficulty == GameDifficulty.Normal && Random.value > 0.8f)) )
		{
			return;
		}
		int attackUsed = 0;
		float vertDir = 0;
		// Random ratios for how often to choose a certain attack. Check below
		// for how they are used. Note that Random.value always give you a value
		// in between 0 and 1.
		float randomBasic = 0.8f;
		// Checks to see if we have already used our low or high attack in our
		// current combo.
		bool usedLow = anim.GetBool ("UsedLow");
		bool usedHigh = anim.GetBool ("UsedHigh");
		if(_charMotor.onGround)
		{
            if(_charStatus.BaseStateInfo.shortNameHash == _atk_Light_1
                || _charStatus.BaseStateInfo.shortNameHash == _atk_Light_2)
			{
				if(Random.value > randomBasic)
					attackUsed = 1;
			}
			else
			{
                if(Random.Range(0, 100) < attackRatio && (!usedLow || !usedHigh))
                {
                    if(!usedHigh && Random.value > 0.5f)
                        vertDir = 1;
                    if(!usedLow && Random.value > 0.4f)
                        vertDir = -1;
                }
                else
                {
                    // Attempt a high strong attack for knocking an opposing
                    // character into the air.
                    if(Random.value > 0.4f)
                        vertDir = 1;
                    attackUsed = 2; // use a strong attack.
                }
			}
		}
		else // Not on ground
		{
            if (_charStatus.BaseStateInfo.IsName("Atk_AirMed_1"))
            {
                if(Random.Range(0, 100) < attackRatio)
                    attackUsed = 1;
            }
            else if (_charStatus.BaseStateInfo.IsName("Atk_AirMed_2"))
            {
                if(Random.Range(0, 100) < attackRatio)
                    attackUsed = 1;
                if (!IsEnemy || _difficulty == GameDifficulty.Hard)
                    vertDir = 1;
            }
			else if(_charStatus.BaseStateInfo.IsName("Atk_AirMedHigh"))
			{
				attackUsed = 2; // Air slam!
			}
		}
		if(attackUsed != 0) // We chose an attack, so use it!
			_charAttacking.AttackSetup (attackUsed, vertDir);
	}

	// Here we check to see if we can attack and what distance away we are.
	// If we are holding a throwable, the distance will be increased so that
	// we can throw it at a further range.
	void AttemptAttack(float distFromTarget)
	{
		if(targetCharStatus == null || (detectionRadius.inCloseRangeChar.Count == 0
		   && anim.GetInteger("HoldStage") != 1)) // HoldStage 1 means we are holding a throwable.
			return;
		float minDistUsing = minAttackDist;
		float maxDistUsing = maxAttackDist;
		bool holdingThrowable = false;
		if(anim.GetInteger("HoldStage") == 1)
		{
			maxDistUsing = maxAttackDist + 3;
			holdingThrowable = true;
		}

		// This part determines if we are in range to attack.
		if(distFromTarget > minDistUsing && distFromTarget < maxDistUsing)
		{
			// Face our target when within range and in idle. Otherwise just get the angle from us
			// facing our target currently.
			float angle = FaceCharacterTarget();
			// If facing our target by a good amount and making sure
			// they are vulnerable and are not guarding. If they are, they will
			// have a small chance of attacking.
			if(targetCharStatus.Vulnerable) // Make sure they are Vulnerable.
			{
				if(angle < 55
				   && (!targetCharStatus.BaseStateInfo.IsTag("Guard")
				   || (targetCharStatus.BaseStateInfo.IsTag("Guard") && Random.value > 0.95f)))
				{
					if(Random.Range(0, 100) < attackRatio)
					{
						if(!holdingThrowable)
						{
							// Attempt a grab. Grabs are done by moving slightly.
							if(canUseGrabs && _charItem.ItemHolding == null && anim.GetFloat("Move") > 0.2f && Random.value > 0.7f)
								_charAttacking.AttackSetup(3, 0); // Attempt grab.
							else
							{
								int attackUsed = 1;
								if(anim.GetFloat("Move") > 0.94f)
									attackUsed = 2; // If moving too fast, go for the slide attack.
								float vDir = 0;
								// If we are next to an itemStorer, we will use our
								// low attack and kick it.
								if(detectionRadius.NextToItemStorer && detectionRadius.inCloseRangeChar.Count == 0)
									vDir = -1;
								_charAttacking.AttackSetup(attackUsed, vDir);
							}
						}
						// If we are holding a throwable, then throw it!
						else _charItem.Throw();
					}
				}
			}
			else // They aren't vulnerable so a random chance to retreat below.
				// if they aren't simply dodging.
			{
				if(!targetCharStatus.BaseStateInfo.IsTag("Dodging"))
					if(Random.Range(0f, 20f) > 19f)
						aiState = AIStates.Retreat;
			}
		}
	}

	void OnCollisionStay(Collision other)
	{
		// Check to see if we are on the ground before proceeding.
		if(!_charMotor.onGround || _charStatus.Busy)
			return;
		// This next check is for seeing if we are colliding with something
		// on the side. If so, we will randomly jump.
		if(other.gameObject.tag == "Terrain")
		{
			if(other.contacts.Length > 0)
			{
				foreach(ContactPoint cont in other.contacts)
				{
					if(cont.normal.x > 0.2f || cont.normal.x < -0.2f
					   || cont.normal.z > 0.2f || cont.normal.z < -0.2f)
					{
						if(Random.Range (0, 20f) > 19.5f)
						{
							jump = true;
						}
					}
					else jump = false;
				}
			}
		}
	}

	void OnTriggerExit(Collider other)
	{
		if(!IsEnemy) // Only enemies can pass here.
			return;

		if(other.gameObject.tag == "EndSpawn")
		{
			// If we have exited our end spawn trigger bounds.
			if(!_enteredArea)
				_enteredArea = true; // We can now target players.
		}
	}

	float FaceCharacterTarget()
	{
		if(targetForNavMesh == null)
			return 0;
		float angle = 5;
		// Only face a target when in idle.
		if(_charStatus.BaseStateInfo.IsName("Idle"))
		{
			angle = Vector3.Angle(myTransform.forward, targetForNavMesh.position - myTransform.position);
			Vector3 dir = (targetForNavMesh.position - transform.position);
			// I manually have a value here for rotating. Gotten from above.
			_charMotor.ManualRotate(dir, false);
		}
		return angle;
	}

	void Wander()
	{
		// Set up a spot to wander to.
		if(!_wanderSpotSet)
		{
			Vector2 randomAdd = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
			if(targetForNavMesh == null)
				randomAdd = Random.insideUnitCircle; // move to a random spot.
			_randomOffset = new Vector3(randomAdd.x, 0, randomAdd.y);
			_wanderSpotSet = true;
		}
		else // If we have set up a random spot.
		{
			_wanderingSpotTime += Time.deltaTime;
			// Reset so we can choose a new random offset after a set time.
			if(_wanderingSpotTime > Random.Range(6f, 9f))
			{
				_wanderingSpotTime = 0;
				_wanderSpotSet = false;
			}
		}
	}
	// Find our nearest target, whether it be a player, enemy, or item, based
	// on what state we are currently in and what is available.
	Transform FindNearestTarget()
	{
		Transform nearestTarget = null;
		if( (aiState != AIStates.Pursue_Item && ( (!IsEnemy && Manager_Targeting.instance.enemyTargets.Count == 0)
		     || (IsEnemy && Manager_Targeting.instance.playerTargets.Count == 0) ) )
		   || (aiState == AIStates.Pursue_Item && (detectionRadius == null
		   || detectionRadius.inRangeItems.Count == 0 && detectionRadius.itemStorersInRange.Count == 0)))
		{
			if(aiState == AIStates.Pursue_Item)
				aiState = AIStates.Pursue; // If we were pursuing an item, there were none nearby so pursue again.
			return null;
		}
		// Setup our correct target list to check from.
		float shortestSoFar = 100;
		List<Transform> targets = Manager_Targeting.instance.playerTargets;
		if(aiState == AIStates.Pursue_Item)
		{
			// Put the names of all of your healing items in that Any() and All() parts I have
			// since that checks for healing items. You can just put a part of
			// their name. They will only attempt to go after those if their
			// health isn't maxed. If any are not healing, they will go after any of the
			// ones that aren't healing then using All to make sure the item's name is not in _healingItemNames.
			// You can check below in the foreach loop where we check for dist < shortestSoFar
			if(detectionRadius.inRangeItems.Count > 0 && ( (_charStatus.Stats[0] < _charStatus.Stats[1]
				&& detectionRadius.inRangeItems.Any(item => item != null && _healingItemNames.Any(itemName => item.name.Contains(itemName)) ) )
				|| (detectionRadius.inRangeItems.Any(item => item != null && _healingItemNames.All(itemName => !item.name.Contains(itemName)) ) )) )
				targets = detectionRadius.inRangeItems;
			else if(detectionRadius.itemStorersInRange.Count > 0)
				targets = detectionRadius.itemStorersInRange;
			else
			{
				// No need to pursue item then. This stuff is here just in case we
				// reach here.
				aiState = AIStates.Wander;
				targetForNavMesh = null;
				return null;
			}
		}
		else // Not pursuing an item.
		{
			// For players to target enemies.
			if(!IsEnemy)
				targets = Manager_Targeting.instance.enemyTargets;
		}
		foreach(Transform curTarget in targets)
		{
			if(curTarget != null)
			{
				// Just another check seeing if an item checking doesn't have a parent.
				// Detection radius already does this but double checks can be good.
				// If a character is what we are looking at, we make sure they are
				// alive.
				if(((curTarget.tag == "Player" || curTarget.tag == "Enemy") && curTarget.GetComponent<CharacterStatus>().Stats[0] > 0)
				   || (curTarget.tag == "ItemStorer" || (curTarget.tag == "Item" && curTarget.parent == null)))
				{
					if(curTarget.tag == "Item"
					   && curTarget.GetComponent<Base_Item>().ItemType == Base_Item.ItemTypes.Collectable)
					{
						// If we are checking an item and we have less than
						// 90% health left and the item is a collectable, we will
						// go for that right away.
						if(_charStatus.Stats[0] < _charStatus.Stats[1] * 0.9f)
						{
							nearestTarget = curTarget;
							break;
						}
					}
					float dist = Vector3.Distance(transform.position, curTarget.position);
					// Here we check to see that the distance checking is less than the shortest so far.
					// Then we see if the curTarget is not an item, or if it is, we see that we have less than
					// max health and the curTarget is a healing item, otherwise, we simply check that it is not
					// a healing item.
					if(dist < shortestSoFar && (curTarget.tag != "Item" || ( (_charStatus.Stats[0] < _charStatus.Stats[1] && _healingItemNames.Any(itemName => curTarget.name.Contains(itemName) ) )
						|| (_healingItemNames.All(itemName => !curTarget.name.Contains(itemName) ) ) ) ) )
					{
						shortestSoFar = dist;
						nearestTarget = curTarget;
					}
				}
			}
		}
		if(nearestTarget != null)
		{
			if(nearestTarget.tag == "Player" || nearestTarget.tag == "Enemy")
			{
				// We now have a targeted character.
				_charAttacking.TargetedCharacter = nearestTarget;
				// AI players also retarget the same way human players do.
				if(gameObject.tag == "Player")
					Manager_Targeting.instance.TargetACharacter(_charStatus.PlayerNumber, _charAttacking);
				// Get references from them.
				targetCharStatus = nearestTarget.GetComponent<CharacterStatus>();
				targetDetRadius = targetCharStatus.detectionRadius;
			}
			this.targetForNavMesh = nearestTarget;
		}
		return nearestTarget;
	}
	// A manual way to change a target. I use this for having an AI
	// character randomly change their target to someone who attacked them if they
	// don't have them targeted already.
	public void ChangeTarget(Transform newTarget)
	{
		_charAttacking.TargetedCharacter = newTarget;
		// AI players also retarget the same way human players do.
		if(!_charStatus.IsEnemy)
			Manager_Targeting.instance.TargetACharacter(_charStatus.PlayerNumber, _charAttacking);
		if (newTarget) // Make sure the target is not null just in case.
		{
			targetCharStatus = newTarget.GetComponent<CharacterStatus>();
			targetDetRadius = targetCharStatus.detectionRadius;
			this.targetForNavMesh = newTarget;
		}
	}

	public void AttemptGuardOrDodge()
	{
		// Should at least have one character nearby and some character targeted.
		// Also, can't do anything here if not on the ground and a safety check for being hurt.
		// Guarding is the only busy state you can dodge in.
		if(detectionRadius.inCloseRangeChar.Count == 0 || detectionRadius.inCloseRangeChar.Contains(null)
			|| !_charMotor.onGround || anim.GetBool("IsHurt") || (_charStatus.Busy && !_charStatus.BaseStateInfo.IsName("Guarding")))
			return;
		if(_charAttacking.TargetedCharacter == null
		   || targetCharStatus == null)
			return;

		if(targetCharStatus.transform == detectionRadius.inCloseRangeChar[0])
		{

			// If our targeted character is attacking (in an attack state and
			// in less than 90% of the animation) or falling. No guarding when holding an
			// item. Enemies can be jumped on so we check to see if this is an enemy and the other character is falling.
			if((targetCharStatus.AmIAttacking || (IsEnemy && targetCharStatus.AmIFalling)) && _charItem.ItemHolding == null)
			{
				// Attempt to guard the attack.
				if(_charAttacking.CanGuard && Random.Range(0, 100) < evadeRatio)
					if(!anim.GetBool("IsGuarding"))
						anim.SetBool("IsGuarding", true);
			}
		}
		else
		{
			// If our current target is not equal to the character in close
			// range of us, we attempt to dodge instead.
			CharacterStatus tarStatus = detectionRadius.inCloseRangeChar[0].GetComponent<CharacterStatus>();
			if(Random.Range (0, 100) < evadeRatio &&
				(tarStatus.AmIAttacking || (IsEnemy && tarStatus.AmIFalling)))
					_charAttacking.DodgeSetup(Random.Range(-1, 2), Random.Range(-1, 2));
		}
		if(detectionRadius.inCloseRangeChar.Count == 1)
			return;
		// Attempt to dodge from any characters close by who are attacking.
		foreach(Transform character in detectionRadius.inCloseRangeChar)
		{
			if(character)
			{
				if(Random.Range (0, 100) < evadeRatio)
				{
					if(character != targetCharStatus.transform)
					{
					    if(character.GetComponent<CharacterStatus>().AmIAttacking)
							_charAttacking.DodgeSetup(Random.Range(-1, 2), Random.Range(-1, 2));
					}
					else
					{
						if(targetCharStatus.AmIAttacking || (IsEnemy && targetCharStatus.AmIFalling))
							_charAttacking.DodgeSetup(Random.Range(-1, 2), Random.Range(-1, 2));
					}
				}
			}
		}
	}
	// Things to reset for this script. This gets called when hit.
	public void ResetParameters()
	{
		if(_charStatus == null || !_charStatus.AIOn)
			return;
		if(Agent)
			Agent.Stop ();
		_hitsWhileGrabbing = 0;
	}

	// We can attempt to air evade when in the air if
	// a given attacker is attacking which would be any opposing character
	// in close range. Make sure the attacker is in an air attack.
	// Note that this gets called from CharacterStatus in Update when
	// we are not on the ground.
	public bool AttemptAirEvade()
	{
		if(detectionRadius.inCloseRangeChar.Count == 0)
			return false;
		foreach(Transform attacker in detectionRadius.inCloseRangeChar)
		{
			if(Vector3.Distance(attacker.position, transform.position) < 2)
			{
				if (attacker && attacker.GetComponent<CharacterAttacking>().IsInAirAttack())
				{
					if(Random.Range(0, 100) < (evadeRatio * 0.33f))
						return true;
				}
			}
		}
		return false;
	}

	// This is only called for enemies.
	public IEnumerator SpawnIn(EnemySpawnType spawnType)
	{
		// Make sure the enemies are spawned with the rotation of the spawn point
		// so make it face the direction facing the trigger area for the current
		// battle zone.
		aiState = AIStates.Spawn;
		Vector3 moveDir = myTransform.forward;
		// Entered area becomes true after exiting our spawn spot trigger bounds
		// for the current area.
		while(!_enteredArea)
		{
			if(spawnType == EnemySpawnType.Normal)
				_charMotor.Move(moveDir, false, false);
			else if (spawnType == EnemySpawnType.Find_Way && !targetForNavMesh)
				targetForNavMesh = FindNearestTarget();
			yield return new WaitForSeconds(0.0001f);
		}
		// We can now target a character.
		// I just change the character's name to the name given in the
		// corresponding enum. Just a personal preference.
		gameObject.name = _charStatus.myCharacterEnemy.ToString ();
		aiState = AIStates.Wander;
		_charStatus.Vulnerable = true; // We can now be hit.
		// We are now added to the enemyTargets list so players can target us.
		Manager_Targeting.instance.enemyTargets.Add (myTransform);
		StopCoroutine ("SpawnIn");
		yield break;
	}
}