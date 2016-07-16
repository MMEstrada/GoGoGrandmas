using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
/// <summary>
/// Manager_ targeting. This will keep track of all of the players and enemies
/// that can be targeted in the scene. In other words, they are alive and not
/// spawning (for enemies). The target cursor prefab will be kept here and will
/// be used for each player. It will be disabled and re-enabled when needed after
/// being created once.
/// </summary>
public class Manager_Targeting : MonoBehaviour
{
	public static Manager_Targeting instance;
	public GameObject targetCursor;
	public Text tarCurPlayerText;
    public List<Transform> playerTargets;// { get; set; } // All players that are alive in the scene(can be targeted)
	public List<Transform> enemyTargets { get; set; }

	public GameObject p1TargetCursor { get; private set; }
	public GameObject p2TargetCursor { get; private set; }
    public GameObject p3TargetCursor { get; private set; }
    public GameObject p4TargetCursor { get; private set; }

    public float[,] targetCursorXOffsets {get; private set;}

	void Awake()
	{
		if(instance == null)
			instance = this;

		playerTargets = new List<Transform> ();
		enemyTargets = new List<Transform> ();
		// An offset for each target cursor so that they won't appear in the same
		// spot when over the same enemy.
		targetCursorXOffsets = new float[4, 4]
            {
                {0, 0, 0, 0}, // 1 player targeting
                {-0.25f, 0.25f, 0, 0 }, // 2 players targeting
                {-0.5f, 0, 0.5f, 0 }, // 3 players targeting
                {-0.75f, -0.25f, 0.25f, 0.75f } // 4 players targeting
            };
	}
	// Find the closest enemy near the player who called this method if the
	// player doesn't have one already targeted. Otherwise this won't be called
	// and they will just shuffle through all of the available enemy targets.
	Transform FindNearestEnemy(Transform player)
	{
		Transform nearestEnemy = null;
		if(enemyTargets.Count == 0) // If no targets then get out of here.
			return null;
		
		// Same trick used multiple times in this project for finding a nearest object.
		float shortestDistSoFar = 1000;
		foreach(Transform enemy in enemyTargets)
		{
			float distFromPlayer = Vector3.Distance(enemy.position, player.position);
			if(distFromPlayer <= shortestDistSoFar)
			{
				nearestEnemy = enemy;
				shortestDistSoFar = distFromPlayer; // Update shortestDistSoFar with the current since it was less than or equal to it.
			}
		}

		return nearestEnemy;
	}

	void CreateTargetCursor(int playerNumber)
	{
		GameObject cursor = Instantiate(targetCursor, transform.position, Quaternion.identity) as GameObject;
		Text curText = cursor.GetComponentInChildren<Text> ();
		// The text to be displayed on the target cursor is just a 'P' followed
		// by the player number of the player.
		curText.text = "P" + playerNumber.ToString ();
		if(playerNumber == 1)
		{
			curText.color = Color.blue;
			// Assign our target cursor prefab to this newly created one so now
			// P1 has one.
			p1TargetCursor = cursor;
		}
		else if(playerNumber == 2)
		{
			curText.color = Color.red;
			p2TargetCursor = cursor;
		}
        else if(playerNumber == 3)
        {
            curText.color = Color.green;
            p3TargetCursor = cursor;
        }
        else if(playerNumber == 4)
        {
            curText.color = Color.yellow;
            p4TargetCursor = cursor;
        }
	}
	// Stop targeting.
	public void DisableTargetCursors(int playerNumber, CharacterAttacking charAttacking)
	{
		if(charAttacking == null)
			return;
		if(playerNumber == 1)
			if(p1TargetCursor != null)
				p1TargetCursor.SetActive(false);
		if(playerNumber == 2)
			if(p2TargetCursor != null)
				p2TargetCursor.SetActive(false);
        if(playerNumber == 3)
            if(p3TargetCursor != null)
                p3TargetCursor.SetActive(false);
        if(playerNumber == 4)
            if(p4TargetCursor != null)
                p4TargetCursor.SetActive(false);
		charAttacking.TargetedCharacter = null;
	}
	// Another overload for this method, this time with the option of disabling
	// all targets cursors together.
	public void DisableTargetCursors(bool allCursors, CharacterAttacking charAttacking)
	{
		if(charAttacking == null)
			return;
		if(allCursors)
		{
			if(p1TargetCursor != null)
				p1TargetCursor.SetActive(false);
			if(p2TargetCursor != null)
				p2TargetCursor.SetActive(false);
            if(p3TargetCursor != null)
                p3TargetCursor.SetActive(false);
            if(p4TargetCursor != null)
                p4TargetCursor.SetActive(false);
		}
		charAttacking.TargetedCharacter = null;
	}

	public void TargetACharacter(int playerNumber, CharacterAttacking charAttacking, Transform manualTarget = null)
	{
		if(enemyTargets.Count == 0) // If there are no targets present.
		{
			// Disable the player's targetCursor if it is active.
			if(playerNumber == 1 && p1TargetCursor != null)
				if(p1TargetCursor.activeSelf)
					p1TargetCursor.SetActive(false);
			if(playerNumber == 2 && p2TargetCursor != null)
				if(p2TargetCursor.activeSelf)
					p2TargetCursor.SetActive(false);
            if(playerNumber == 3 && p3TargetCursor != null)
                if(p3TargetCursor.activeSelf)
                    p3TargetCursor.SetActive(false);
            if(playerNumber == 4 && p4TargetCursor != null)
                if(p4TargetCursor.activeSelf)
                    p4TargetCursor.SetActive(false);
			charAttacking.TargetedCharacter = null;
			return; // Get out of here as there is nothing to target.
		}
		
		if(enemyTargets.Count > 0)
		{
			// Create a clone of the targetCursorPrefab
			if((playerNumber == 1 && p1TargetCursor == null)
			   || (playerNumber == 2 && p2TargetCursor == null)
                || (playerNumber == 3 && p3TargetCursor == null)
                || (playerNumber == 4 && p4TargetCursor == null))
			{
				CreateTargetCursor(playerNumber);
			}
			if(playerNumber == 1)
				p1TargetCursor.SetActive(true); // If our targetCursor is not active, then set it to be active.
            else if(playerNumber == 2) p2TargetCursor.SetActive(true);
            else if(playerNumber == 3) p3TargetCursor.SetActive(true);
            else if(playerNumber == 4) p4TargetCursor.SetActive(true);

			if(charAttacking.TargetedCharacter == null) // If we don't have any enemy targeted.
			{
				if(enemyTargets.Count == 1)
					charAttacking.TargetedCharacter = enemyTargets[0]; // Only one target available, so target them.
				else
					charAttacking.TargetedCharacter = FindNearestEnemy(charAttacking.transform); // Otherwise we need to find the closest one.
			}
			else //If the player already has an enemy targeted then we will index through the other enemies or just stay on the one we are on
				// if there is only one.
			{
				// Give us the index of the target we have selected.
				int index = enemyTargets.IndexOf(charAttacking.TargetedCharacter);
				
				if (index < enemyTargets.Count - 1)
				{
					index ++;
				}
				else
				{
					// Have index go back to first element in list.
					index = 0;
				}
                DeselectTarget(charAttacking.TargetedCharacter, charAttacking.transform);
				if(manualTarget == null)
					charAttacking.TargetedCharacter = enemyTargets[index]; // Change our target.
				else charAttacking.TargetedCharacter = manualTarget;
			}
			// Select our new target.
			SelectTarget(playerNumber, charAttacking);
		}
	}
	
    void DeselectTarget(Transform targetDeselecting, Transform playerTarget)
    {
        if (targetDeselecting)
            targetDeselecting.GetComponent<CharacterStatus>().TargetChange(false, playerTarget);
    }

	// Actually position the targetCursor on the enemy and have it setup to follow them.
	public void SelectTarget(int playerNumber, CharacterAttacking charAttacking)
	{
		if(charAttacking.TargetedCharacter != null)
		{
			GameObject chosenCursor = p1TargetCursor;
			if(playerNumber == 2)
				chosenCursor = p2TargetCursor;
            else if(playerNumber == 3)
                chosenCursor = p3TargetCursor;
            else if(playerNumber == 4)
                chosenCursor = p4TargetCursor;
            float height = charAttacking.TargetedCharacter.gameObject.GetComponent<CharacterMotor>().originalHeight;
			FaceTransform faceTransformScript = null;
			faceTransformScript = chosenCursor.GetComponent<FaceTransform>();
			// Have the target cursor face the camera and follow the targeted
			// enemy.
			faceTransformScript.transformToFace = Camera.main.transform;
			faceTransformScript.transformToFollow = charAttacking.TargetedCharacter;
            CharacterStatus eneStatus = charAttacking.TargetedCharacter.GetComponent<CharacterStatus>();
            eneStatus.TargetChange(true, charAttacking.transform);
            // Set the target cursor to follow the enemy while being at this offset.
            faceTransformScript.CreatedSetup(charAttacking.gameObject.GetComponent<CharacterStatus>().PlayerNumber, height + 1, eneStatus);
		}	
	}
}