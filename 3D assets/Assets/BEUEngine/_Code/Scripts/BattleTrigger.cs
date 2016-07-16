using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// Battle trigger. When all players have entered this, either a battle will start
/// or the end of the scene has been reached. With EndTrigger set to true, you
/// are telling the gameObject this script is attached to that this trigger
/// is the end of the stage, not just the scene.
/// </summary>
public class BattleTrigger : MonoBehaviour
{
	public bool EndTrigger = false;
	// Make sure to keep track of the players inside to see if the amount is
	// equal to the total number of players who are in the scene.
	List<Transform> playersInside;

	void Start ()
	{
		playersInside = new List<Transform> ();
	}

	void OnTriggerEnter (Collider other)
	{
		// If a cutscene is already in progress, no need to go further.
		if(Manager_Cutscene.instance.inCutscene)
			return;

		if(other.gameObject.tag == "Player")
		{
			if(!playersInside.Contains(other.transform))
				playersInside.Add(other.transform);

			CharacterStatus cStatus = other.gameObject.GetComponent<CharacterStatus>();
			// Once the amount of players inside equals the total number of 
			// remaining characters in the scene/ones who can be targeted by
			// enemies and if we are not currently in a battle already...
			if(playersInside.Count == Manager_Targeting.instance.playerTargets.Count
			   && !Manager_BattleZone.instance.InBattle)
			{
				if(!EndTrigger)
					Manager_BattleZone.instance.BattleChange(true); // Start the battle!
				else
				{
					// Disable input for the character who just entered.
					cStatus.InputChange(false);
					CutsceneTypes cutsceneToDo = CutsceneTypes.Run_Out;
					if(gameObject.name.Contains("Win"))
					{
						cutsceneToDo = CutsceneTypes.Stage_Clear;
					}
					Manager_Cutscene.instance.StartCutscene(cutsceneToDo);
				}
			}
			else
			{
				if(EndTrigger)
				{
					cStatus.InputChange(false);
					CutsceneTypes cutsceneToDo = CutsceneTypes.Run_Out;
					if(gameObject.name.Contains("Win"))
					{
						cutsceneToDo = CutsceneTypes.Stage_Clear;
					}
					Manager_Cutscene.instance.StartCutscene(cutsceneToDo);
				}
			}
		}
	}

	// Here, players can actually leave the trigger and exit the playersInside
	// list. Needed to make sure no players are left behind since the Start
	// boundary of the current area could then block them out.
	void OnTriggerExit (Collider other)
	{
		if(other.gameObject.tag == "Player")
		{
			if(playersInside.Contains(other.transform))
				playersInside.Remove(other.transform);
		}
	}
}