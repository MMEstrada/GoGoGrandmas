using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// Manager_ cutscene. A simple and easy to use cutscene engine I put together.
/// In StartCutscene() you specify which cutscene is starting up and this script
/// will send a message to all players for them to do whatever it is they should
/// do for the given cutscene.
/// Current cutscenes:
/// * Running in and out of the scene.
/// * Ending a stage.
/// </summary>
public class Manager_Cutscene : MonoBehaviour
{
	public static Manager_Cutscene instance;
	// The number of current ready players who are ready to move on in the
	// cutscene. When this equals the total amount of players in the scene,
	// the cutscene can continue. I just use this right now for making the
	// cutscene end after all players have reached the waypoint at the start of
	// a scene.
	public int numberOfReadyPlayers { get; set; }

	// A gameObject I made that shows the black border at the top and bottom
	// of the screen like most games use for cutscenes.
	public GameObject cutsceneBorders {get; private set;}
	public CutsceneTypes currentCutscene {get; private set;}
	public bool inCutscene {get; private set;}

	// I put the starting and ending waypoints here. The starting one is the one
	// the players will run to at the start of a scene while the ending one...
	// yeah, the spot the players will run to at the end of a scene. I find
	// them based on their gameObject name, so don't change it. If you would rather
	// place them yourself then you can get rid of the get; and set; after
	// waypoints.  If you do, place the start one in the inspector first,
	// followed by the end one.
	public List<Transform> waypoints { get; private set; }
	// The animator for the cutscene borders which has the animations on it.
	Animator animCutsceneBorders;

	void Awake()
	{
		if(instance == null)
			instance = this;
	}

	void Start()
	{
		// I put the cutscene borders object on the Manager_UI's gameObject which
		// is the main Canvas_Overlay gameObject.
		cutsceneBorders = Manager_UI.instance.transform.Find ("CutsceneBorders").gameObject;
		animCutsceneBorders = cutsceneBorders.GetComponent<Animator> ();
		// If this isn't null and there is more than one in this list, then you must have them set
		// in the inspector.
		if(waypoints == null || waypoints.Count == 0)
		{
			waypoints = new List<Transform>();
			List<GameObject> waypointsGO = new List<GameObject>(4) {GameObject.Find("Waypoint_Start"), GameObject.Find("Waypoint_End"), null,
				null};
			waypoints.Add(waypointsGO[0].transform);
			waypoints.Add(waypointsGO[1].transform);
			// This next part only matters for end stage scenes. I put
			// the waypoint in the boundary end on those scenes for
			// the players to move to before they face the camera before the
			// results are displayed. Make sure for these scenes that the
			// Boundary_End_Win gameObject is found.
			GameObject BoundEnd = GameObject.Find("Boundary_End_Win");
			if(BoundEnd != null && BoundEnd.transform.childCount > 0)
			{
				waypoints.Add(BoundEnd.transform.GetChild(0));
			}
		}
		Invoke ("CutsceneEnter", 0.75f);
	}

	void CutsceneEnter()
	{
		StartCutscene (CutsceneTypes.Run_In);
	}

	public void StartCutscene (CutsceneTypes cutsceneStarting)
	{
		currentCutscene = cutsceneStarting;
		numberOfReadyPlayers = 0;
		inCutscene = true;
		Transform waypointUsing = null;
		// Set up waypoints using for a cutscene if necessary.
		if(cutsceneStarting == CutsceneTypes.Run_In)
			waypointUsing = waypoints[0];
		else if(cutsceneStarting == CutsceneTypes.Run_Out)
			waypointUsing = waypoints[1];
		else if(cutsceneStarting == CutsceneTypes.Stage_Clear)
			waypointUsing = waypoints[2]; // Spot for player to run to before facing the camera.
		// Tell all players a cutscene is starting so they can do their own thing
		// for it.
		foreach(Transform player in Manager_Targeting.instance.playerTargets)
		{
			player.GetComponent<PlayerCutscene>().StartCutscene(cutsceneStarting, waypointUsing);
		}
		if(cutsceneStarting == CutsceneTypes.Run_Out)
		{
			// Disable the UI display and the camera's script when exiting a
			// scene.
			Manager_UI.instance.DisplayUI(false);
			Camera_BEU.instance.enabled = false;
			// The new scene will load after this gets called.
			Invoke("EndCutscene", 3);
		}
		// Tell the Manager_UI to start displaying the results.
		else if(cutsceneStarting == CutsceneTypes.Stage_Clear && !Manager_UI.instance.DisplayingResults)
			Manager_UI.instance.StartCoroutine("DisplayTheResults");
		if(currentCutscene != CutsceneTypes.Stage_Clear)
		{
			// Have the cutscene borders play their opening animation.
			if(cutsceneBorders && animCutsceneBorders)
				animCutsceneBorders.Play ("Open");
		}
	}

	public void EndCutscene()
	{
		numberOfReadyPlayers = 0;
		if(currentCutscene != CutsceneTypes.Stage_Clear)
		{
			if(cutsceneBorders && animCutsceneBorders)
			{
				// Only have the cutsceneBorders play their closing animation
				// when entering a scene since the should stay one when leaving
				// the scene.
				if(currentCutscene == CutsceneTypes.Run_In)
					animCutsceneBorders.Play ("Close");
			}
		}
		// Inform the players the cutscene has ended.
		foreach(Transform player in Manager_Targeting.instance.playerTargets)
			player.SendMessage("EndCutscene", currentCutscene);
		// If we are in the runOut cutscene, we will load a new level which is
		// right after our current one in the build settings if our current scene index
	  	// is less than the total amount of scenes, otherwise we will restart the game by loading scene 0.
		// The next scene to load will need to be changed most likely after you add more scenes.
		if (currentCutscene == CutsceneTypes.Run_Out)
		{
			int curBuildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene ().buildIndex;
	  		bool isUnderBuildAmount = curBuildIndex < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
			Manager_UI.instance.StartCoroutine (Manager_UI.instance.StartTransition (TransitionTypes.Fading,
			false, isUnderBuildAmount, isUnderBuildAmount ? curBuildIndex + 1 : 0, 1));
		}
		currentCutscene = CutsceneTypes.None;
		ResetCutsceneBool ();
	}

	void ResetCutsceneBool()
	{
		inCutscene = false;
	}
}