using UnityEngine;
using System.Collections;
/// <summary>
/// Player cutscene. This takes care of what the player does during each of the
/// game's cutscenes that they participate in. The Manager_Cutscene calls
/// StartCutscene here for the players during the start of each cutscene and
/// EndCutscene during the end of each cutscene so that the player(s) can do what
/// they need to during the start and end of each cutscene they participate in.
/// </summary>
public class PlayerCutscene : Character
{
	// Used for letting you know that the player has reached the placed waypoint
	// so that they may do what comes next.
	bool _reachedWaypoint = false;
	bool _jump;
	bool _useNavAgent;
	CharacterStatus _charStatus;
	CharacterMotor _charMotor;
	CharacterItem _charItem;
	NavMeshAgent _myAgent;
	Transform _targetForNavMesh;
	Vector3 _targetPos;

	void Awake()
	{
		_charStatus = GetComponent<CharacterStatus> ();
		_charMotor = GetComponent<CharacterMotor> ();
		_charItem = GetComponent<CharacterItem> ();
		anim = GetComponent<Animator> ();
		_myAgent = GetComponentInChildren<NavMeshAgent> ();
	}

	void Update()
	{
		// Make sure we are in a cutscene before proceeding.
		if(!Manager_Cutscene.instance.inCutscene || !_myAgent || !_useNavAgent)
			return;
		if(!_myAgent.gameObject.activeSelf)
			_myAgent.gameObject.SetActive(true);
		float distFromTarget = 1;
		Vector3 move = Vector3.zero;
		if(_targetForNavMesh)
		{
			_targetPos = _targetForNavMesh.position;
			move = _myAgent.desiredVelocity.normalized;
			distFromTarget = Vector3.Distance(transform.position, _targetForNavMesh.position);
		}
		else _targetPos = myTransform.position; // Default position in case there is no target for nav mesh.
		float multiplier = distFromTarget * 0.5f;
		multiplier = Mathf.Clamp(multiplier, 0.3f, 1);
		move *= multiplier;
		
		_myAgent.SetDestination(_targetPos);
		
		// Update the agent's position 
		_myAgent.transform.position = transform.position;
		if(move.magnitude < 0.1f)
			move = Vector3.zero;

		_charMotor.Move(move, _jump, false);
		if (_jump)
			_jump = false;
	}

	void OnTriggerStay(Collider other)
	{
		// Make sure we are in a cutscene before checking for waypoints.
		if(!Manager_Cutscene.instance.inCutscene)
			return;

		if(other.gameObject.tag == "Waypoint" && (!_targetForNavMesh || _targetForNavMesh == other.transform))
		{
			// We reached where we needed to go. This could be reset if you
			// have multiple waypoints. I only use one for running into the scene.
			_reachedWaypoint = true;
		}
	}
	// What the player does during the Run_In or Run_Out cutscenes. The
	// Manager_Cutscene sets the runToTrans. It will be the transform to run
	// to.
	IEnumerator RunInOrOut(bool runIn, Transform runToTrans)
	{
		_reachedWaypoint = false;
		if(runIn) // Wait a bit if running in.
			yield return new WaitForSeconds(2);
		_targetForNavMesh = runToTrans;
		_useNavAgent = true;
		if(!runIn) // Running out of the scene.
		{
			if(_charItem.ItemHolding != null)
			{
				// Keep track of the item we are holding so we can "keep" it
				// for the next scene.
				GetLeaveSceneItemNumber(_charStatus.PlayerNumber);
			}
			else
			{
				// Reset these when not holding an item at the end of a scene.
				if(_charStatus.PlayerNumber == 1)
					CharacterItem.itemHoldingAtEndP1 = 0;
				else if(_charStatus.PlayerNumber == 2)
					CharacterItem.itemHoldingAtEndP2 = 0;
                else if(_charStatus.PlayerNumber == 3)
                    CharacterItem.itemHoldingAtEndP3 = 0;
                else if(_charStatus.PlayerNumber == 4)
                    CharacterItem.itemHoldingAtEndP4 = 0;
			}
		}
		float finishTimer = 0;
		// Here we wait until we reach a waypoint or run in long enough.
		// Only need to hit waypoint upon entering, not leaving.
		while(!_reachedWaypoint && finishTimer < 4.5f)
		{
			finishTimer += Time.deltaTime;
			yield return 0;
		}
		_targetForNavMesh = null;
		_useNavAgent = false;
		_charMotor.Move (Vector3.zero, false, false);
		// Finish running. Most likely won't get here during the run out
		// sequence since the new scene will load.
		if(runIn)
		{
			// We are ready. When we have all of the players ready, that last
			// player will tell the Manager_Cutscene that the cutscene can end.
			Manager_Cutscene.instance.numberOfReadyPlayers++;
			if(Manager_Cutscene.instance.numberOfReadyPlayers == Manager_Targeting.instance.playerTargets.Count)
				Manager_Cutscene.instance.EndCutscene();
			// Reset this so it can be used again.
			_reachedWaypoint = false;
			// Show our player number above our character.
			_charStatus.ActivateTargetPlayerNumber(_charStatus.PlayerNumber);
		}
		while(!_charStatus.AIOn && Manager_Cutscene.instance.inCutscene)
			yield return new WaitForSeconds(0.0001f);
		if(!_charStatus.AIOn)
			_myAgent.gameObject.SetActive(false);
		StopCoroutine ("RunInOrOut");
	}
	// What we do at the end of each stage. We freeze and face the camera,
	// followed by running out of the scene after the results are done displaying.
	IEnumerator StageClear(Transform runToTrans)
	{
		_reachedWaypoint = false;
		_targetForNavMesh = runToTrans;
		_useNavAgent = true;
		// Move in the direction of the waypoint. Once they touch it,
		// they will stop and face the camera. Or they will if the
		// waypoint's trigger is not placed the best and the backup timer
		// kicks in (finishTimer).
		float finishTimer = 0;
		while(!_reachedWaypoint && finishTimer < 3.5f)
		{
			finishTimer += Time.deltaTime;
			yield return 0;
		}
		_targetForNavMesh = null;
		_useNavAgent = false;
		// Here we specify we have a lookAtTarget and we want it to be the main
		// camera.
		_charMotor.Move (Vector3.zero, false, false, true, Camera.main.transform);
		anim.SetTrigger ("WinFired"); // Go into our win state. Just another idle is what I used.
		anim.SetBool ("InWin", true);
        finishTimer = 0;
		Transform camUsing = Camera.main.transform;
		// Face the camera and start to look at it.
        while(finishTimer < 3 && (Vector3.Angle(-myTransform.forward, new Vector3(camUsing.forward.x, 0, camUsing.forward.z)) > 37))
		{
            finishTimer += Time.deltaTime;
			_charMotor.ManualRotate((camUsing.position - myTransform.position).normalized, false);
			_charMotor.Move (Vector3.zero, false, false, true, camUsing);
			yield return new WaitForSeconds (0.0001f);
		}
        finishTimer = 0;
		// Maintain looking at the camera.
        while (_charMotor.lookWeight < 0.98f && finishTimer < 3)
		{
            finishTimer += Time.deltaTime;
			_charMotor.Move (Vector3.zero, false, false, true, camUsing);
			yield return new WaitForSeconds (0.0001f);
		}
		// Wait until the results are done displaying. The Manager_UI will reset
		// this when the user presses the Continue button at the end of the
		// results.
		while(Manager_UI.instance.DisplayingResults)
			yield return new WaitForSeconds(0.0001f);
		_charMotor.Move (Vector3.zero, false, false);
		anim.SetBool("InWin", false); // No longer stay in win state.
		// Run out of the scene using the end waypoint.
		StartCoroutine (RunInOrOut (false, Manager_Cutscene.instance.waypoints[1]));
		StopCoroutine ("StageClear");
	}

	void GetLeaveSceneItemNumber(int playNumb)
	{
		// We need to check what item we are holding so it can't be null.
		if(_charItem.ItemHolding == null)
			return;
		// Getting the index of our item by finding its name in the
		// AllCarryableItems list. We need to use + 1 since 0 for our
		// itemHoldingAtEnd variable = no item while 0 in that Items list
		// obviously is an item.
		int itemIndex = Manager_Game.instance.AllCarryableItems.FindIndex (item => _charItem.ItemHolding.name.Contains(item.name)) + 1;
		if (playNumb == 1)
		{
			CharacterItem.itemHoldingAtEndP1 = itemIndex;
            Manager_Game.itemHealthAmountP1 = (float)(_charItem.ItemHolding.GetComponent<Base_Item>().Condition * 0.01f); // Save the item's current health.
		}
		else if (playNumb == 2)
		{
			CharacterItem.itemHoldingAtEndP2 = itemIndex;
            Manager_Game.itemHealthAmountP2 = (float)(_charItem.ItemHolding.GetComponent<Base_Item>().Condition * 0.01f);
		}
        else if (playNumb == 3)
        {
            CharacterItem.itemHoldingAtEndP3 = itemIndex;
            Manager_Game.itemHealthAmountP3 = (float)(_charItem.ItemHolding.GetComponent<Base_Item>().Condition * 0.01f);
        }
        else if (playNumb == 4)
        {
            CharacterItem.itemHoldingAtEndP4 = itemIndex;
            Manager_Game.itemHealthAmountP4 = (float)(_charItem.ItemHolding.GetComponent<Base_Item>().Condition * 0.01f);
        }
	}

	// Called on us at the start of each cutscene. Play the correct
	// actions/coroutines for the cutscene.
	public void StartCutscene (CutsceneTypes cutsceneStarting, Transform waypoint = null)
	{
		_charStatus.InputChange (false); // Disable controls or AI.
		switch(cutsceneStarting)
		{
		case CutsceneTypes.Run_In:
		case CutsceneTypes.Run_Out:
			StartCoroutine(RunInOrOut(cutsceneStarting == CutsceneTypes.Run_In, waypoint));
			break;
		case CutsceneTypes.Stage_Clear:
			StartCoroutine(StageClear(waypoint));
			break;
		}
	}

	public void EndCutscene (CutsceneTypes cutsceneEnding)
	{
		_charStatus.InputChange (true); // Re-enable controls or AI.
		switch(cutsceneEnding)
		{
		case CutsceneTypes.Run_In:
			_charStatus.Vulnerable = true;
			break;
			// Update our stats for running out so that the Manager_Game can give
			// them back to us at the start of a new scene.
		case CutsceneTypes.Run_Out:
			if(_charStatus.PlayerNumber == 1)
				Manager_Game.StatsP1 = _charStatus.Stats;
			else if(_charStatus.PlayerNumber == 2)
				Manager_Game.StatsP2 = _charStatus.Stats;
            else if(_charStatus.PlayerNumber == 3)
                Manager_Game.StatsP3 = _charStatus.Stats;
            else if(_charStatus.PlayerNumber == 4)
                Manager_Game.StatsP4 = _charStatus.Stats;
			break;
		}
	}
}