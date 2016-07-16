using UnityEngine;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// Camera_ BEU. My more simple camera script I use for a beat em' up style game.
/// </summary>
public class Camera_BEU : MonoBehaviour
{
	// A reference to this script so we can access the camera from any script when
	// needed.
	public static Camera_BEU instance;
	public float distanceToMoveOnMenu = 280; // How far to go on the main menu or demo menu scene before heading back left.
	// Layers to check for when the camera casts a ray seeing if any obstacles
	// are hindering its view of the players. For now I only use terrain as the
	// obstacle. If the ray hits anything with a layer from here, the camera will
	// go up higher and reset back to the height it was at when below it is clear.
	public LayerMask obstacleCheckMask;
	// A speed delay for the camera on the title screen. The higher, the slower
	// the camera will move on the title screen.
	public float speedTitleDelay = 2;
	// The min distance the camera can be from all of the player's overall position.
	public float distanceMin = 4.5f;
	// And the max...
	public float distanceMax = 9;
	// How high off the ground the cam should always go to.
	public float camHeightAboveGround = 3;
	// All of the players the camera is currently keeping track of.
	public List<Transform> targets;
	// The current distance away.
	public float distance = 5;
	public float xPosClampOffset = 4; // Offset from the bounds of the Start and End boundaries to get clamped between.
	public float zPosClampOffset = 1; // Offset from the bounds of the Trigger boundary for the z position of the camera.

	float _myPosY; // What our current Y position will be. Our raycast that hits the ground will change this.
	// The camera's references to the Manager_BattleZone's lists of these
	// for easier access.
	List<GameObject> _zonesStart;
	List<GameObject> _zonesTrigger;
	List<GameObject> _zonesEnd;
	// The min value the y angle of the camera can go to followed by the max.
	// I just use 360 - minAngleY for the max.
	float minAngleY = 1;
	float maxAngleY = 359;
	// The velocity for the camera's position movement in Mathf.SmoothDamp
	float _curVelX;
	float _curVelY;
	float _curVelZ;
	// Used for additional distance to keep the camera away from a player if they
	// get too close.
	float addedDist = 0;
	// A timer to reset any added distance.
	float _resetAddedDistTimer = 0;
	// A reference to the Manager_BattleZone's Transforms of these for easier access.
	Transform _startBoundary;
	Transform _endBoundary;
    Collider _startBoundCol;
    Collider _endBoundCol;
	// The minimum and maximum x and z position the camera can go to. Used during
	// battles to clamp the camera into the current battle zone area to prevent
	// seeing enemies from spawning in and to give you an idea of where the battle
	// zone starts and ends.
	float _minX = 5;
	float _maxX = 25;
	float _minZ = 9;
	float _maxZ = 17;
	// The current position we are looking at.
	Vector3 _lookAtPos;
	// The position the camera starts at when the scene loads. I use this to
	// determine where the camera should base its overall y position so be sure
	// to set the camera at a desired height before starting the scene.
	Vector3 _startPos;
	// A variable to store our updated position.
	Vector3 _newPos;
	// This will store our current y eulerAngle.
	float _angleY;

	void Awake()
	{
		if(instance == null)
			instance = this;
	}

	// I make a Start a coroutine here simply for when the camera is on the
	// title screen to allow it to stay in Start and just move back and forth.
	System.Collections.IEnumerator Start()
	{
		_startPos = transform.position;
		_lookAtPos = transform.position + transform.forward;
		yield return new WaitForSeconds(0.0001f);
		if(!Manager_Game.instance.IsInMenu)
		{
			Invoke ("SetupRanges", 0.6f);
			Invoke ("FindPlayers", 0.9f);
		}
		else
		{
			// Just setting an end point for the camera to go
			// to on the title screen by using its right side as a reference.
			Vector3 endPos = _startPos + transform.right * distanceToMoveOnMenu;
			do
			{
				// Lerp will always be a value from 0 - 1 since we divide by the
				// same amount we give it.
				float lerp = Mathf.PingPong(Time.time, speedTitleDelay) / speedTitleDelay;
				transform.position = Vector3.Lerp (_startPos, endPos, lerp);
				yield return 0;
				// We continue to move like this the whole time we are in the
				// main menu scene.
			} while(Manager_Game.instance.IsInMenu);
		}
		_myPosY = _startPos.y;
	}
	
	void FixedUpdate ()
	{
		// Absolutely make sure there are valid targets present before
		// proceeding.
		if(targets == null || targets.Count == 0 || targets.Contains(null)
			|| Manager_Game.instance.IsPaused)
			return;
		// Here we check to see how many targets/players there are and
		// zoom in and out based on that. The values done for more than 2
		// players are estimates that worked well for me in another game but
		// I didn't get to test it for this game since I only use 2 players.
		// I kept that in though just in case you would like to try it out if
		// you get more than 2 players.
		_lookAtPos = targets[0].position;
		if(targets.Count == 2)
			_lookAtPos = ((targets[0].position + targets[1].position)) * 0.5f;
		else if(targets.Count == 3)
			_lookAtPos = ((targets[0].position + targets[1].position
			              + targets[2].position) * 0.333f);
		else if(targets.Count == 4)
			_lookAtPos = ((targets[0].position + targets[1].position
			              + targets[2].position + targets[3].position) * 0.25f);

		// I currently only use the x distance for the characters since that is
		// the main thing that matters. z doesn't matter as much since I make
		// use of the addedDist for that if a character gets too close to the
		// camera.
		if(targets.Count == 2)
		{
			distance = Vector3.Distance(new Vector3(targets[0].position.x, 0, 0), new Vector3(targets[1].position.x, 0, 0)) + distanceMin;
		}
		else if(targets.Count > 2)
		{
			distance = distanceMin;
			for(int i = 0; i < targets.Count; i++)
			{
				for(int j = targets.Count - 1; j > 0; j--)
				{
					if(targets[i] != targets[j])
						distance += Vector3.Distance(new Vector3(targets[i].position.x, 0, 0), new Vector3(targets[j].position.x, 0, 0));
				}
			}
			distance *= 0.5f;
		}

        // I use a raycast that will always hit ground, and the camera's height will always get a set height
        // above the ground (camHeightAboveGround).
        RaycastHit rayHit;
        if (Physics.Raycast(transform.position + new Vector3(0, 0.5f, 0), Vector3.down, out rayHit, 20, obstacleCheckMask))
        {
            // Cam's height will go to our set height above the ground.
            _myPosY = rayHit.point.y + camHeightAboveGround;
        }

        int greaterForAmount = 0; // Amount of players far enough away from.
        if (addedDist < 4) // Limit additional distance added to 4.
		{
			foreach(Transform player in targets)
			{
                float dist = Vector3.Distance(transform.position, player.position + Vector3.up);
				// If any player target gets too close...
                if (dist < 4.5f)
                {
                    // slowly add more distance.
                    addedDist += 1 * Time.deltaTime;
                    _resetAddedDistTimer = 0;
                }
                else if (dist > 4.8f)
                    greaterForAmount++;
			}
		}
		// If this is greater than 0 then it means that a character had recently
		// gotten too close.
		if(addedDist > 0)
		{
			_resetAddedDistTimer += Time.deltaTime;
			// Start removing addedDist after a set time to end up zooming back
			// to where we were before a character had gotten too close if far enough away from each player.
            if(_resetAddedDistTimer > 2 && greaterForAmount == Manager_Targeting.instance.playerTargets.Count())
				addedDist -= Time.deltaTime;
			addedDist = Mathf.Clamp(addedDist, 0, 4);
		}
		// Clamp values...
		distance = Mathf.Clamp (distance, distanceMin, distanceMax);
		// Only when we are in battle, we will clamp between the current
		// min and max values for x and z which are gotten when a battle
		// ends to get the values for the next area ahead of time. I just
		// wanted to do it that way.
		if(Manager_BattleZone.instance.InBattle)
		{
			_lookAtPos.x = Mathf.Clamp (_lookAtPos.x, _minX, _maxX);
			_lookAtPos.z = Mathf.Clamp (_lookAtPos.z, _minZ, _maxZ);
		}
		// Make sure the cam can't go beyond the start or end boundaries.
        _lookAtPos.x = Mathf.Clamp (_lookAtPos.x, _startBoundCol.bounds.max.x + 9, _endBoundCol.bounds.min.x - 2);
		// After all is done above, set the lookAtPos to a higher value so it doesn't
		// look at the ground where the character's feet are.
		_lookAtPos += Vector3.up;
		// Grab our current position.
		_newPos = transform.position;
		// This will smooth each of the position components individually
		var positionX = Mathf.SmoothDamp(_newPos.x,_lookAtPos.x,ref _curVelX,0.2f);
		var positionY = Mathf.SmoothDamp(_newPos.y, _myPosY,ref _curVelY,0.2f);
		var positionZ = Mathf.SmoothDamp(_newPos.z,_lookAtPos.z - (distance + addedDist),ref _curVelZ,0.2f);
		
		// Take the result
		_newPos = new Vector3(positionX, positionY, positionZ);
		// update our position to what _newPos now has.
		transform.position = _newPos;
		// Create a rotation to look at the targets/players.
		Quaternion lookRot = Quaternion.LookRotation((_lookAtPos) - transform.position, Vector3.up);
		// Prevent looking up too high if the lookAtPos.y exceeds the camera's.
		// Rotate to face the targets/players with given clamp values below...
		if (_lookAtPos.y > transform.position.y)
			lookRot.x = 0;
		transform.rotation = Quaternion.Slerp (transform.rotation, lookRot, 3 * Time.deltaTime);
		_angleY = transform.eulerAngles.y;
		// Negative Clamp
		if(_angleY > minAngleY){ if(_angleY < 90){ _angleY = minAngleY;} }
		// Positive Clamp
		if(_angleY < maxAngleY){ if(_angleY > 240){ _angleY = maxAngleY;} }
		// Actually rotate now... Clamp the X angle so the camera doesn't look
		// up too high or too low.
		transform.rotation = Quaternion.Euler(Mathf.Clamp (transform.rotation.eulerAngles.x, 0.1f, 30), _angleY, transform.rotation.eulerAngles.z);
	}

	// I use this to quickly swap out a target for another one. Currently
	// used for having the camera target a player's ragdoll when they die
	// and then going back to the player after a set time and the ragdoll
	// gets removed.
	public void ChangeTarget(Transform curTarget, Transform newTarget)
	{
		if(targets.Count == 0)
			return;

		for(int i = 0; i < targets.Count; i++)
		{
			if(targets[i] == curTarget)
			{
				targets[i] = newTarget;
				break;
			}
		}
	}

	// Setup ranges for battle. If we are in the first area, we also get
	// the zones from Manager_BattleZone.
	public void SetupRanges()
	{
		int curArea = Manager_BattleZone.instance.currentAreaNumber;
		if(curArea == 0)
		{
			_zonesStart = Manager_BattleZone.instance.zonesStart;
			_zonesTrigger = Manager_BattleZone.instance.zonesTrigger;
			_zonesEnd = Manager_BattleZone.instance.zonesEnd;
			_startBoundary = Manager_BattleZone.instance.startBoundary;
			_endBoundary = Manager_BattleZone.instance.endBoundary;
            _startBoundCol = _startBoundary.GetComponent<Collider>();
            _endBoundCol = _endBoundary.GetComponent<Collider>();
		}
		if(curArea <= Manager_BattleZone.instance.maxAreaNumber)
		{
			// The values at the end can be adjusted to how clamped you want
			// the camera to be when in a battle based on the current
			// battle zone. In this side-scrolling type game, min.x
			// would be the farthest left and max.x would be the farthest
			// right of the bounds of the collider. Min.z would be the lowest/
			// closest to the screen and max would be the furthest away from
			// the screen.
			_minX = _zonesStart[curArea].GetComponent<Collider>().bounds.max.x + xPosClampOffset;
			_maxX = _zonesEnd[curArea].GetComponent<Collider>().bounds.min.x - xPosClampOffset;
			_minZ = _zonesTrigger [curArea].GetComponent<Collider>().bounds.min.z - zPosClampOffset;
		}
		// I just simply set the maxZ to 3 more than the min so it can
		// zoom in only a slight bit more.
		_maxZ = _minZ + 3;
	}

	// Find the players currently in the targets list in Manager_Targeting.
	void FindPlayers()
	{
		// Clear the current list of targets we may have so we can start
		// fresh.
		targets = new List<Transform> ();
		foreach(Transform player in Manager_Targeting.instance.playerTargets)
			targets.Add (player);
		// The more players, the max distance will be extended a bit.
		distanceMax += targets.Count;
		if(distanceMax <= distanceMin + 0.5f)
			distanceMax += 2; // Should have these more apart.
	}

	// Called in CharacterStatus of a player after they have lost their last life so they won't be targeted anymore.
	// The player passes in their transform of their ragdoll target if they have one, otherwise just their transform.
	public void RemoveTarget(Transform targetToRemove)
	{
		if (targets.Count == 0)
			return;
		if(targets.Contains(targetToRemove))
			targets.Remove(targetToRemove);
	}
}