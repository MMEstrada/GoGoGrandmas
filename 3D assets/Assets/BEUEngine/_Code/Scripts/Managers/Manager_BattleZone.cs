using UnityEngine;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// Manager_ battle zone. The main script for battles. This holds all of the
/// different zones for battles, the current area number you are in for the scene,
/// and the start and end boundaries of the scene as well. This script informs
/// the camera to update its min and max positions for the next battle and also
/// informs the Manager_UI to show the "Battle Start!" and "Go!" text.
/// Note: be sure to leave all zone gameObjects active in the scene
/// so that they can be found. That would include the Start, Trigger, and End
/// boundaries for each battle zone. I have it setup that way now but just thought
/// I would mention it.
/// </summary>
public class Manager_BattleZone : MonoBehaviour
{
	public static Manager_BattleZone instance;
	// The start boundary is the one that prevents the players from moving past
	// the beginning of the scene so this is a collider gameObject.
	public Transform startBoundary;
	// The end boundary is the one that trigger the cutscene for the players to
	// run out of the scene so this is a trigger gameObject.
	public Transform endBoundary;
	// Set the maxAreaNumber to the max area number the scene will have.
	// Note that 0 is the start of a scene and it doesn't go to 1 until AFTER
	// the first battle.
	public int maxAreaNumber;
	public List<GameObject> zonesStart {get; private set;}
	public List<GameObject> zonesTrigger {get; private set;}
	public List<GameObject> zonesEnd {get; private set;}
	public int currentAreaNumber { get; private set; }
	public bool InBattle {get; set;}

	int _maxAreaNumber;

	void Awake()
	{
		if(instance == null)
			instance = this;
	}

	void Start ()
	{
		zonesStart = GameObject.FindGameObjectsWithTag ("BattleStart").ToList() ;
		zonesTrigger = GameObject.FindGameObjectsWithTag ("BattleTrigger").ToList();
		zonesEnd = GameObject.FindGameObjectsWithTag ("BattleEnd").ToList();

		// Here I am sorting these lists by the parent gameObject's name since
		// that is the one that has the number at the end, such as BattleZone_0.
		// This way, the lists will be sorted by which one comes first in the 
		// current scene.  That's how I have them set up.
		zonesStart.Sort (delegate(GameObject x, GameObject y)
		{
			return x.transform.parent.name.CompareTo(y.transform.parent.name);
		});
		zonesTrigger.Sort (delegate(GameObject x, GameObject y)
		                 {
			return x.transform.parent.name.CompareTo(y.transform.parent.name);
		});
		zonesEnd.Sort (delegate(GameObject x, GameObject y)
		                 {
			return x.transform.parent.name.CompareTo(y.transform.parent.name);
		});
		// Deactivate all Start boundary gameObjects so we are able to enter each
		// battle zone.
		foreach(GameObject starter in zonesStart)
			starter.SetActive(false);
	}
	// Are we currently entering or finishing a battle?
	public void BattleChange(bool enteringBattle)
	{
		if(enteringBattle)
		{
			// Turn on the start boundary so that we can't escape where we came
			// from.
			zonesStart[currentAreaNumber].SetActive(true);
			// Make the "Battle Start!" text flash on screen.
			Manager_UI.instance.UIFlash(Manager_UI.instance.TextsBattle[0]);
			InBattle = true;
            Manager_Game.instance.DisableDeadPlayers();
		}
		else // Battle has ended.
		{
			InBattle = false;
			// Deactivate these since they are no longer needed. Disabling
			// the End one allows us to proceed onward.
			zonesTrigger[currentAreaNumber].SetActive(false);
			zonesEnd[currentAreaNumber].SetActive(false);
			// The current area is now over so we increase this.
			currentAreaNumber++;
			// The "Go!" text will flash on screen now...
			Manager_UI.instance.UIColorFlash(Manager_UI.instance.TextsBattle[1], 3, true);
			// Have the camera setup its ranges for the next area.
			Camera_BEU.instance.SetupRanges();
			// Remove all dead enemies in the scene by making them
			// flash away.
			Manager_Game.instance.Invoke ("RemoveDeadEnemies", 0.5f);
		}
	}
}
