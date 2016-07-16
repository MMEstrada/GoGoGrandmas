using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// Manager_ game. Keeps track of many static things such as the player's scores,
/// Number of players, the difficulty, and lots more. I decided against making
/// an item manager so I just have the item prefabs stored here as well. I didn't
/// feel I had enough purpose to make an item manager just for the sake of item
/// prefabs.
/// Note: When adding items to this, be sure to do so in the Manager_Game
/// prefab so that it updates the prefab.
/// 
/// A side note, if you want to use the demo scenes, place the demo menu scene in the build setting for
/// the first index (0) and the demo level directly after that (so the demo level will be index 1).
/// I have it setup right now so that you use the regular canyon scenes using the MainMenu, Canyon_0, and Canyon_1
/// scenes in that order in the build settings.  Make sure one of the canyon scenes is right after the main menu
/// scene or the demo scene is right after the demo menu scene in the build settings.
/// 
/// </summary>
public class Manager_Game : MonoBehaviour
{
	public static Manager_Game instance;
	public static bool GameShuttingDown;
    public static bool usingMobile = false;
	public static int NumberOfPlayers = 1;
	public static int NumberOfHumans = 1;
	public static int P1Score, P2Score, P3Score, P4Score = 0;
    public static int P1Lives = 3, P2Lives = 3, P3Lives = 3, P4Lives = 3;
	// These next ones here are for the grading system.
	public static int P1HitsGiven, P2HitsGiven, P3HitsGiven, P4HitsGiven = 0;
	public static int P1HitsTaken, P2HitsTaken, P3HitsTaken, P4HitsTaken = 0;
	public static int P1MaxCombo, P2MaxCombo, P3MaxCombo, P4MaxCombo = 0;
	public static int P1LivesLost, P2LivesLost, P3LivesLost, P4LivesLost = 0;

	public static float itemHealthAmountP1, itemHealthAmountP2, itemHealthAmountP3, itemHealthAmountP4;
	// Keep track of the player's current stats so they can get them back during
	// the next scene from here.
	public static int[] StatsP1;
	public static int[] StatsP2;
	public static int[] StatsP3;
	public static int[] StatsP4;
	public static GameDifficulty Difficulty = GameDifficulty.Hard;
	// You can choose how often you get items during your game!
	// Take note though that enemies can also grab them, if you allow them too,
	// anyway, by setting their canGetItems bool to true.
	public static AmountRating ItemAppearRate = AmountRating.Low;
	// Players who have lost all of their lives will go here. After the count
	// of this equals the total number of players, a Game Over occurs. I
	// made this static so that it won't reset when transferring between scenes.
	public static List<GameObject> playersDefeated;
	// The playable characters who were chosen for the current game to be played
	// as.
	public static List<PlayerCharacters> PlayersChosen;
	// Where the players will spawn from. Make the rotation of this face the
	// direction you want the players to run in after spawning.
	public Transform spawnPointPlayer;
	// The grade given to the players at the end of a stage.
	public string P1Grade {get; private set;}
	public string P2Grade { get; private set; }
	public string P3Grade { get; private set; }
	public string P4Grade { get; private set; }
	// All of the different player prefabs go here. They should be placed in the
	// order they are listed in the PlayerCharacters enum.
	public List<GameObject> playerPrefabs;
	public List<GameObject> ICPrefabs; // Item collectables
	public List<GameObject> ITPrefabs; // Item throwables
	public List<GameObject> IWPrefabs; // Item weapons
	// This holds all of the weapons and throwable items and is used if
	// you carry an item out of a scene so you "take" it into the next one.
	public List<GameObject> AllCarryableItems;
	// All of the dead enemies in the scene. Used for making them all disappear
	// together at the end of a battle and allowing their dead bodies to remain
	// until then.
	public List<GameObject> enemiesDead { get; set; }
	//  Storing all enemies in the scene here, regardless if they are spawning,
	// dead, or whatever. Used for the enemy spawners so they know if they can
	// spawn or not.
	public List<GameObject> enemiesAll { get; set; }
	public List<CharacterStatus> allPlayerStatus {get; private set;} // Access to all the player's status scripts in the scene. Right now the CharacterAI for players uses this to see if their health is greater than player 1
	public bool IsPaused { get; private set; }
	// Are we currently in a menu scene? Place all of your menu area names here.
	public bool IsInMenu { get { return currentArea == CurrentArea.Main_Menu || currentArea == CurrentArea.Demo_Menu; } }
	// I made an enum with all of the different area/scene names for easier
	// access instead of using finding the name of the level using the SceneManager
	public CurrentArea currentArea {get; private set;}
	int _playerWhoPaused = 1; // To not let another player pause or unpause when another player does.

	void Awake()
	{
		if(instance == null)
			instance = this;
		// Default characters chosen.
		if (PlayersChosen == null)
			PlayersChosen = new List<PlayerCharacters> (4) { PlayerCharacters.Ethan, PlayerCharacters.Dude,
			PlayerCharacters.Ethan_Twin, PlayerCharacters.Dude_Twin};
		if(playersDefeated == null)
			playersDefeated = new List<GameObject> ();
		enemiesDead = new List<GameObject> ();
		// Get the current area by checking the loaded level's name.
		string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
		if(levelName.Contains("Main"))
			currentArea = CurrentArea.Main_Menu;
		else if(levelName.Contains("Demo"))
		{
			if (!levelName.Contains("Menu"))
				currentArea = CurrentArea.Demo;
			else
				currentArea = CurrentArea.Demo_Menu;
		}
		else if(levelName.Contains("Canyon_0"))
			currentArea = CurrentArea.Canyon_0;
		else if(levelName.Contains("Canyon_1"))
			currentArea = CurrentArea.Canyon_1;
        enemiesAll = new List<GameObject> ();
        enemiesDead = new List<GameObject> ();
        allPlayerStatus = new List<CharacterStatus>();
	}

	void Start()
	{
		// Only create players and remove unused assets when not in the main menu scene
		if(!IsInMenu)
		{
			InvokeRepeating("RemoveUnusedAssets", 15, 15); // Is first called after 15 seconds and will keep getting called every 15 seconds
			CreatePlayers ();
		}
	}

	// Free up memory by removing assets that are no longer being used in the scene, such
	// as created material instances for example, if any.  Very important, as this can help
	// prevent the game from crashing!
	void RemoveUnusedAssets()
	{
		Resources.UnloadUnusedAssets ();
	}

	void CreatePlayers()
	{
		for (int i = 0; i < NumberOfPlayers; i++)
		{
			// If the current player has no more lives, they will not be created.
            if ((i == 0 && P1Lives == 0) || (i == 1 && P2Lives == 0) || (i == 2 && P3Lives == 0)
                || (i == 3 && P4Lives == 0))
            {
                continue;
            }
            Vector3 offset = spawnPointPlayer.right;
            GameObject player = Instantiate (playerPrefabs [(int)PlayersChosen [i]].gameObject, spawnPointPlayer.position + (offset * i), spawnPointPlayer.rotation) as GameObject;
            player.SendMessage ("CreatedSetup", i + 1); // Player number assigning based on how many are in the player status list.
			Manager_Targeting.instance.playerTargets.Add (player.transform);
			// Set the UI's image of the character.
            Manager_UI.instance.plCharImage [i].sprite = Manager_UI.instance.charIcons [(int)PlayersChosen [i]];
			allPlayerStatus.Add(player.GetComponent<CharacterStatus>());
		}
        if (NumberOfPlayers < 4)
            Manager_UI.ShowP4Results = false;
        else
            Manager_UI.ShowP4Results = true;
        if (NumberOfPlayers < 3)
            Manager_UI.ShowP3Results = false;
        else
            Manager_UI.ShowP3Results = true;
        if (NumberOfPlayers < 2)
            Manager_UI.ShowP2Results = false;
        else
            Manager_UI.ShowP2Results = true;

        if (usingMobile)
        {
            List<GameObject> allPlayers = new List<GameObject>();
            for (int i = 0; i < allPlayerStatus.Count; i++)
                allPlayers.Add(allPlayerStatus[i].gameObject);
            Manager_Mobile.instance.SetPlayers(allPlayers);
        }
        Manager_UI.instance.SetupPlayerUIScaling (NumberOfPlayers);
	}

	void Update()
	{
		if(IsInMenu || Manager_Cutscene.instance.inCutscene)
			return;
		// I use Manager_UI's results checks to see if that player is around.
		// Unpausing is done through the event trigger event of the text option
		// selected through Manager_UI. Check the PauseGroup on the Canvas_Overlay
		// to see those.
		if(!IsPaused)
		{
            // If using mobile and you have four fingers on the screen, this will check if any of them have double-tapped.
			if((!usingMobile && Input.GetButtonUp("PauseP1") || (usingMobile && Input.touchCount == 4 && Input.touches.Any(tou => tou.tapCount == 2))) && Manager_UI.ShowP1Results && (!IsPaused || (IsPaused && _playerWhoPaused == 1)))
				Pause (1);
			else if(Input.GetButtonUp("PauseP2") && Manager_UI.ShowP2Results && (!IsPaused || (IsPaused && _playerWhoPaused == 2)))
				Pause (2);
            else if(Input.GetButtonUp("PauseP3") && Manager_UI.ShowP3Results && (!IsPaused || (IsPaused && _playerWhoPaused == 3)))
                Pause (3);
            else if(Input.GetButtonUp("PauseP4") && Manager_UI.ShowP4Results && (!IsPaused || (IsPaused && _playerWhoPaused == 4)))
                Pause (4);
		}
	}

	// This is where any change made to the player's score needs to be
	// called from so that their static variable can be updated as well.
	public void ScoreUpdate(int playerNumber, int scoreChange)
	{
		int scoreUsing = P1Score;
		Text textUpdating = Manager_UI.instance.plScoreText[playerNumber - 1];
		if(playerNumber == 4)
		{
			P4Score += scoreChange;
			scoreUsing = P4Score;
		}
		else if(playerNumber == 3)
		{
			P3Score += scoreChange;
			scoreUsing = P3Score;
		}
		else if(playerNumber == 2)
		{
			P2Score += scoreChange;
			scoreUsing = P2Score;
		}
		else if(playerNumber == 1)
		{
			P1Score += scoreChange;
			scoreUsing = P1Score;
		}
		textUpdating.text = scoreUsing.ToString();
	}
	// Same goes with lives...
	public int LiveChange(int playerNumber, int amount)
	{
		int livesUsing = P1Lives;
		if (playerNumber == 2)
			livesUsing = P2Lives;
		else if (playerNumber == 3)
			livesUsing = P3Lives;
		else if (playerNumber == 4)
			livesUsing = P4Lives;
		livesUsing += amount;
		if(playerNumber == 1)
		{
			P1Lives = livesUsing;
			Manager_UI.instance.plLiveText[0].text = P1Lives.ToString();
		}
		else if(playerNumber == 2)
		{
			P2Lives = livesUsing;
			Manager_UI.instance.plLiveText[1].text = P2Lives.ToString();
		}
		else if(playerNumber == 3)
		{
			P3Lives = livesUsing;
			Manager_UI.instance.plLiveText[2].text = P3Lives.ToString();
		}
		else if(playerNumber == 4)
		{
			P4Lives = livesUsing;
			Manager_UI.instance.plLiveText[3].text = P4Lives.ToString();
		}
		return livesUsing;
	}
	// Remove dead enemies. This is called after a battle has ended. The
	// characters have the FlashAway script on them or on their child
	// SkinnedMeshRenderer gameObject if it is a ragdoll.
	public void RemoveDeadEnemies()
	{
		if(enemiesDead.Count == 0)
			return;
		foreach(GameObject enemy in enemiesDead)
		{
			if(enemy != null)
			{
				FlashAway flashAway = enemy.GetComponent<FlashAway>();
				if(flashAway == null) // Must be a ragdoll enemy.
					flashAway = enemy.transform.GetComponentInChildren<FlashAway>();
				flashAway.ResetFlashTime(2);
			}
		}
		// Enemy cleanup is taken care of.
		enemiesDead.Clear ();
	}

	// A button event when you press the start button on the main menu or demo menu.
	public void StartGame()
	{
		// Load the level after our current one in the build settings.
		Manager_UI.instance.StartCoroutine (Manager_UI.instance.StartTransition (TransitionTypes.Fading, false, true, UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1));
	}

	// The grades for a given percent value.
	string FindGrade(float percentage)
	{
		if(percentage >= 0.99f)
			return "S";
		if(percentage >= 0.96f)
			return "A +";
		if(percentage >= 0.93f)
			return "A";
		if(percentage >= 0.9f)
			return "A -";
		if(percentage >= 0.86f)
			return "B +";
		if(percentage >= 0.83f)
			return "B";
		if(percentage >= 0.8f)
			return "B -";
		if(percentage >= 0.76f)
			return "C +";
		if(percentage >= 0.73f)
			return "C";
		if(percentage >= 0.7f)
			return "C -";
		if(percentage >= 0.66f)
			return "D +";
		if(percentage >= 0.63f)
			return "D";
		if(percentage >= 0.6f)
			return "D -";
		return "F";
	}
	// I give the grades based off of the total hits given / hits taken * 1.4f
	// so you need to get over twice as many hits as what you take in order to
	// get a great grade.
	public int DetermineGrades(int playerNumber)
	{
		float perc = 1; // Start at 100% by default.
		// To change how strict the grading is, change the amount HitsTaken is * by. Right now it's 1.4.
		// The higher, the more strict the grading.
		if(playerNumber == 1)
		{
			// To avoid a divide by zero exception if you don't get hit...
			if(P1HitsTaken != 0)
				perc = Mathf.Clamp (P1HitsGiven / (P1HitsTaken * 1.4f), 0, 1);
			perc -= P1LivesLost * 0.1f; // Lose 5% for each life lost.
			// Gain 1% for each combo hit count. Max now is 10 so 10 max combo = 10% added.
			if(P1MaxCombo > 3)
				perc += P1MaxCombo * 0.01f;
            if (P1LivesLost > 0) // For every life lost, your max grade obtainable will go down by 10%
                perc = Mathf.Clamp(perc, 0, 1 - (P1LivesLost * 0.1f)); // A smaller amount max for losing any lives... 0.95 = A
			P1Grade = FindGrade (perc);
		}
		else if(playerNumber == 2)
		{
			// To avoid a divide by zero exception if you don't get hit...
			if(P2HitsTaken != 0)
				perc = Mathf.Clamp (P2HitsGiven / (P2HitsTaken * 1.4f), 0, 1);
			perc -= P2LivesLost * 0.1f;
			if(P2MaxCombo > 3)
				perc += P2MaxCombo * 0.01f;
            if (P2LivesLost > 0) // For every life lost, your max grade obtainable will go down by 10%
                perc = Mathf.Clamp(perc, 0, 1 - (P2LivesLost * 0.1f));
			P2Grade = FindGrade (perc);
		}
		else if(playerNumber == 3)
		{
			// To avoid a divide by zero exception if you don't get hit...
			if(P3HitsTaken != 0)
				perc = Mathf.Clamp (P3HitsGiven / (P3HitsTaken * 1.4f), 0, 1);
			perc -= P3LivesLost * 0.1f;
			if(P3MaxCombo > 3)
				perc += P3MaxCombo * 0.01f;
			if (P3LivesLost > 0) // For every life lost, your max grade obtainable will go down by 10%
				perc = Mathf.Clamp(perc, 0, 1 - (P3LivesLost * 0.1f));
			P3Grade = FindGrade (perc);
		}
		else if(playerNumber == 4)
		{
			// To avoid a divide by zero exception if you don't get hit...
			if(P4HitsTaken != 0)
				perc = Mathf.Clamp (P4HitsGiven / (P4HitsTaken * 1.4f), 0, 1);
			perc -= P4LivesLost * 0.1f;
			if(P4MaxCombo > 3)
				perc += P4MaxCombo * 0.01f;
			if (P4LivesLost > 0) // For every life lost, your max grade obtainable will go down by 10%
				perc = Mathf.Clamp(perc, 0, 1 - (P4LivesLost * 0.1f));
			P4Grade = FindGrade (perc);
		}
		// Bonus points give away.
		if(perc >= 0.99f) // 'S' rank
			return 90;
		if(perc >= 0.9f)
			return 70;
		if(perc >= 0.8f)
			return 50;
		if(perc >= 0.7f)
			return 30;
		if(perc >= 0.6f)
			return 0;
		return -10; // 'F' rank
	}

	public void Pause(int playerWhoPaused = 1)
	{
		_playerWhoPaused = playerWhoPaused;
		if(!IsPaused)
		{
			IsPaused = true;
			Manager_UI.instance.Pause(playerWhoPaused);

			Time.timeScale = 0;
		}
		else // We are paused
		{
			Time.timeScale = 1;
			// A delay so that the player's input would be re-enabled after a delay
			// so that you wouldn't do the action pressed on the pause menu when
			// resuming the game.
			Invoke ("EndPause", 0.3f);
		}
		StopCoroutine ("Pause");
	}

	void EndPause()
	{
		IsPaused = false;
	}
	                 
	// Simply reset all static variables.
	public void RestartGame()
	{
		Time.timeScale = 1;
		P1Score = P2Score = P1MaxCombo = P2MaxCombo = P1HitsGiven =
			P2HitsGiven = P1HitsTaken = P2HitsTaken = P1LivesLost =
				P2LivesLost = 0;
		P3Score = P4Score = P3MaxCombo = P4MaxCombo = P3HitsGiven =
			P4HitsGiven = P3HitsTaken = P4HitsTaken = P3LivesLost =
				P4LivesLost = 0;
		StatsP1 = null;
		StatsP2 = null;
		StatsP3 = null;
		StatsP4 = null;
        P1Lives = P2Lives = P3Lives = P4Lives = 3;
		CharacterItem.itemHoldingAtEndP1 = CharacterItem.itemHoldingAtEndP2 = CharacterItem.itemHoldingAtEndP3 = 
			CharacterItem.itemHoldingAtEndP4 = 0; // No more holding items for players if they were.
		UnityEngine.SceneManagement.SceneManager.LoadScene (0);
	}

	// Quit based on if using the editor or a build of the game.
	public void Quit () 
	{
		Time.timeScale = 1;
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#else
		Application.Quit();
		#endif
	}

	// All game over starting things will go here.
	public void GameOver()
	{
		Manager_UI.instance.StartCoroutine ("GameOverUI");
	}

    public void DisableDeadPlayers()
    {
        if (playersDefeated.Count > 0)
            foreach (GameObject player in playersDefeated)
                if (player && player.activeSelf)
                    player.SetActive(false);
    }

    void OnApplicationQuit()
	{
		GameShuttingDown = true;
	}
}