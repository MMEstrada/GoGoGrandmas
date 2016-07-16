using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
/// <summary>
/// Manager_ UI. All of the things that get displayed on the screen will go here.
/// Check the Canvas_Overlay gameObject to see all of these. This gameObject will
/// have this script on it and since it has DontDestroyOnLoad, this gameObject
/// should only be in the first scene of the game, otherwise you could get
/// duplicates. I fixed that from happening though with a check in Awake().
/// I suggest looking at the UI elements on this gameObject along with what the
/// variables/references are holding here to see what exactly they are.
/// Note: the imageTransition will be the last child gameObject in the
/// gameObject that holds this script (the Canvas_Overlay prefab).  It is
/// there so that it will be over everything else when enabled and visible,
/// thus providing a nice transition.
/// 
/// For 3 players, each player's UI group will be scale to 80% on their x and be 240 pixels apart. For 4 players,
/// they will be scaled to 70% and be 198 pixels apart.
/// </summary>
public class Manager_UI : MonoBehaviour
{
	public static Manager_UI instance;
	// Is the game in a transition in between scenes?
	public static bool InTransition = false;
	// After the stage is complete, are we able to show this player's results?
	// If not, then they have lost all of their lives and are no longer playing.
	public static bool ShowP1Results = true;
    public static bool ShowP2Results, ShowP3Results, ShowP4Results;
	public static Color DefaultHealthFillColor;
    public List<GameObject> allEventSystems;
	public List<Menu> allMenus;
	// The transition UI gameObject image for transitions.
	public Image imageTransition;
	public GameObject PauseGroup; // Gameobject that holds the pause menu.
	public Selectable defaultPauseOption; // Selected right after pausing.
	public Selectable secondaryPauseOption; // Used for making sure the default pause option gets highlighted.
	// -- The Results Display gameObject things --
	// Check the Canvas_Overlay gameObject to see all of these.
	public GameObject resultsDisplay;
	public Text resultsText; // the "Results" text for the top of the results screen.
	public Text gameOverText;
	public CanvasGroup groupResultsTable; // I use this to fade in and out the main table.
	public Text resultsParameters; // Hold the text for the names of the parameters such as hits given, hits taken, and the others.
	public Text p1ResultsParameters; // holds the text/number amounts for the player's Hits Taken, Hits Given, and the others.
	public Text p2ResultsParameters;
    public Text p3ResultsParameters;
    public Text p4ResultsParameters;
	public GameObject buttonContinue;

	// The player's UI gameObjects which hold all of their personal UI things.
	// Each player will use one of these after they are created based on their
	// player number.
	public GameObject[] plUIGroup;

	// This holds the "Battle Start!" and "Go!" text.
	public List<Text> TextsBattle;
	// All of the different images for the characters.
	public Sprite[] charIcons;
    public Sprite[] sprComboRatings; // I use text for displaying how well our combo was: Good, Nice!, Rad!, and Epic!!
	// -- All of the different player related UI things --
	public Image[] plCharImage;
	public Slider[] plHealthSlider;
	// I use the fill for the player's health slider in order to change the color
	// of it when they reach lower amounts of health, and to disable this for when
	// the player dies in order to show no health left.
	public Image[] plHealthSliderFill;
	public Text[] plLiveText;
	public Text[] plScoreText;
    public Text[] plNameText;
	// The group which holds the amount of hits given in a combo and the "Hits"
	// text that gets displayed after it.
	public GameObject[] plHitsGroup;
	public Text[] plHitCount;
	public Image[] plItemIcon;
	public Image[] plItemHealthImageAmount;
    public Image[] plComboRatingImage;
	// The animator for the results panel that holds the animations I gave it.
	Animator animResultsPanel;
	// These are used for adding a nice delay after choosing an option on the
	// pause menu.  1 = Restart game, 2 = Quit
	int _pauseMenuOptionChosen = 0;
	// The timer needed for the delay.
	float _pauseMenuOptionChosenTimer = 0;
    Selectable _quitPauseOption; // Grabbing this so we can disable it during the web player version of the project and disable it (Quit option) since you can't quit in a web player.

	CurrentArea _curArea; // Our own reference to the current area.
	List<GameObject> _webPlayerHelpInfo; // Only in the demo level, this script will find these which hold the info for help displayed on screen.
	// To hold the event system gameObject so we can disable it after choosing
	// a pause menu option to disable further input.
	GameObject _webPlayerParent;
	GameObject eveSystem;
    string _defParameterText; // Default text store for the results so we can reset it when starting to show the results since this gameObject does not get destroyed.
	// If we are currently displaying the results panel.
	public bool DisplayingResults { get; private set; }
	// Waiting for input before proceeding.
	public bool WaitingForInput {get; set;}
	// I left this next one public in case you would want to disable or
	// enable certain input for UI navigation at certain times, say if you
	// were to switch between joystick and keyboard use mid-game. That would
	// be done in Player_Input.
	public List<GameObject> playInputMenu {get; private set;} // Use for pause menu controlling.

	void Awake()
	{
		if (instance == null)
			instance = this;
		else Destroy(this.gameObject); // Destroy as this script is already in use.
		// Don't destroy this gameObject in here since it will be used for the
		// main GUI text for each level. You only need to assign it once for the
		// Manager_Game prefab.
		DontDestroyOnLoad (this.gameObject);
	}

	void Start()
	{
        if (Manager_Game.usingMobile)
            eveSystem = GameObject.Find("EventSystem_Mobile");
        else
        {
            if (Manager_Game.instance.IsInMenu)
                eveSystem = GameObject.Find("EventSystem");
            else
                eveSystem = GameObject.Find("EventSystem_NotMenu");
        }
		playInputMenu = new List<GameObject>();
        if(!eveSystem)
        {
            int index = Manager_Game.usingMobile ? 2 : Manager_Game.instance.IsInMenu ? 0 : 1;
            eveSystem = Instantiate(allEventSystems[index], Vector3.zero, Quaternion.identity) as GameObject;
        }
		if (eveSystem)
		{
			if(!Manager_Game.instance.IsInMenu && !Manager_Game.usingMobile)
				foreach (Transform child in eveSystem.transform)
					playInputMenu.Add(child.gameObject);
		}
		else
			Debug.LogWarning("NO EVENT SYSTEM FOUND!");
        _defParameterText = resultsParameters.text;
		animResultsPanel = resultsDisplay.transform.GetChild(0).GetComponent<Animator> ();
		// Stop displaying any UI elements upon starting.
		DisplayUI (false);
		// Start our fading-in transition.
		imageTransition.gameObject.SetActive(true);
		Color imColor = imageTransition.color; imColor.a = 1;
		imageTransition.color = imColor;
		StartCoroutine (StartTransition (TransitionTypes.Fading, true));
		_curArea = Manager_Game.instance.currentArea;
		// Set this when this is created only since DefaultHealthFillColor is static.
		if(Time.realtimeSinceStartup < 3)
		{
			DefaultHealthFillColor = plHealthSliderFill [0].color;
		}

        if (Manager_Game.instance.IsInMenu)
        {
            Transform webPlayerHelpMenu = transform.Find("Panel_HelpInfo_Menu");
            if (webPlayerHelpMenu)
                webPlayerHelpMenu.gameObject.SetActive(true);
            else
                Debug.LogWarning("NO HELP INFO FOUND ON MENU!");
        }
        else
        {
            Transform webPlayerHelpMenu = transform.Find("Panel_HelpInfo_Menu");
            if (webPlayerHelpMenu)
                webPlayerHelpMenu.gameObject.SetActive(false); // Disable the menu's controls.
            if (_curArea == CurrentArea.Demo)
            {
                _webPlayerHelpInfo = new List<GameObject>();
                Transform webPlayerParent = transform.Find("WebPlayer_HelpInfo");
                _webPlayerParent = webPlayerParent.gameObject;
                if (webPlayerParent)
                {
                    foreach (Transform child in webPlayerParent)
                        _webPlayerHelpInfo.Add(child.gameObject);
                    _webPlayerHelpInfo[0].SetActive(true);
                    _webPlayerHelpInfo[1].SetActive(false); // Disable the large body of text controls information.
                }
                else
                    Debug.LogWarning("WEB PLAYER PARENT NOT FOUND IN DEMO LEVEL!");
            }
            else
            {
                if (_webPlayerParent)
                    _webPlayerParent.SetActive(false);
            }
        }
	}
	// This needs to be used in place of start at the beginning of a new scene
	// since this gameObject doesn't get destroyed in between scenes.
	void OnLevelWasLoaded(int level)
	{
        if (instance && instance != this)
            return;
		_curArea = Manager_Game.instance.currentArea;
		imageTransition.gameObject.SetActive(true);
		Color imColor = imageTransition.color; imColor.a = 1;
		imageTransition.color = imColor;
		if (Manager_Game.usingMobile)
            eveSystem = GameObject.Find("EventSystem_Mobile");
        else
        {
            if (Manager_Game.instance.IsInMenu)
                eveSystem = GameObject.Find("EventSystem");
            else
                eveSystem = GameObject.Find("EventSystem_NotMenu");
        }
		playInputMenu = new List<GameObject>();
        if(!eveSystem)
        {
            int index = Manager_Game.usingMobile ? 2 : Manager_Game.instance.IsInMenu ? 0 : 1;
            eveSystem = Instantiate(allEventSystems[index], Vector3.zero, Quaternion.identity) as GameObject;
        }
		if (eveSystem)
		{
			if(!Manager_Game.instance.IsInMenu && !Manager_Game.usingMobile)
				foreach (Transform child in eveSystem.transform)
					playInputMenu.Add(child.gameObject);
		}
		else
			Debug.LogWarning("NO EVENT SYSTEM FOUND!");
		if (!Manager_Game.instance.IsInMenu)
		{
			_quitPauseOption = defaultPauseOption.transform.parent.GetChild(2).GetComponent<Selectable>();
            if (_quitPauseOption)
            {
                if (Application.isWebPlayer)
                    _quitPauseOption.interactable = false; // Can't quit during the web player scene.
            }
            else
                Debug.LogWarning("NO QUIT PAUSE OPTION FOUND!");
		}
		else
		{
			if (_curArea == CurrentArea.Main_Menu)
			{
				if (allMenus.Contains(null))
				{
					allMenus = new List<Menu>();
					GameObject canvasMenu = GameObject.Find("Canvas_Menu");
					if (canvasMenu)
					{
						foreach (Transform child in canvasMenu.transform)
							allMenus.Add(child.GetComponent<Menu>());
					}
					else
						Debug.LogWarning("CANVAS MENU NOT FOUND ON MAIN MENU!");
				}
			}
		}
		DisplayUI (false);

        if (Manager_Game.instance.IsInMenu)
        {
            Transform webPlayerHelpMenu = transform.Find("Panel_HelpInfo_Menu");
            if (webPlayerHelpMenu)
                webPlayerHelpMenu.gameObject.SetActive(true);
            else
                Debug.LogWarning("NO HELP INFO FOUND ON MENU!");
            if (_webPlayerParent)
                _webPlayerParent.SetActive(false); // Do not need web player help info when not on a menu screen.
        }
        else
        {
            Transform webPlayerHelpMenu = transform.Find("Panel_HelpInfo_Menu");
            if (webPlayerHelpMenu)
                webPlayerHelpMenu.gameObject.SetActive(false); // Disable the menu's controls.
            if (_curArea == CurrentArea.Demo)
            {
                _webPlayerHelpInfo = new List<GameObject>();
                Transform webPlayerParent = transform.Find("WebPlayer_HelpInfo");
                _webPlayerParent = webPlayerParent.gameObject;
                if (webPlayerParent)
                {
                    foreach (Transform child in webPlayerParent)
                        _webPlayerHelpInfo.Add(child.gameObject);
                    _webPlayerHelpInfo[0].SetActive(true);
                    _webPlayerHelpInfo[1].SetActive(false); // Disable the large body of text controls information.
                }
                else
                    Debug.LogWarning("WEB PLAYER PARENT NOT FOUND IN DEMO LEVEL!");
            }
            else
            {
                if (_webPlayerParent)
                    _webPlayerParent.SetActive(false);
            }
        }
		StartCoroutine (StartTransition (TransitionTypes.Fading, true));
	}

	void Update()
	{
		if (Manager_Game.instance.IsPaused)
		{
			// We chose a pause option.
			if (_pauseMenuOptionChosen > 0)
			{
				// Time is frozen when paused so we can't use += Time.deltaTime
				// but we can still just use a default value to increase by.
				if (_pauseMenuOptionChosenTimer < 1)
					_pauseMenuOptionChosenTimer += 0.01f;
				else
				{
					if (_pauseMenuOptionChosen == 1)
						EvenTrigRestartGame();
					else if (_pauseMenuOptionChosen == 2)
						EvenTrigQuit();
					_pauseMenuOptionChosenTimer = 0;
					_pauseMenuOptionChosen = 0;
				}
			}
		}
		else
		{
			if(_curArea == CurrentArea.Demo)
			{
				if (Manager_Cutscene.instance.inCutscene)
				{
					if(_webPlayerParent.activeSelf)
						_webPlayerParent.SetActive(false);
				}
				else
				{
					if (!_webPlayerParent.activeSelf)
						_webPlayerParent.SetActive(true);
					if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
					{
						if (_webPlayerHelpInfo[0].activeSelf)
						{
							_webPlayerHelpInfo[0].SetActive(false);
							_webPlayerHelpInfo[1].SetActive(true);
						}
						else
						{
							_webPlayerHelpInfo[0].SetActive(true);
							_webPlayerHelpInfo[1].SetActive(false);
						}
					}
				}
			}
		}
	}
	// This is one way you can make text flash on and off. You specify the
	// text UI object and how long to make it flash for.
	IEnumerator FlashText(Text textUsing, float duration)
	{
		float timer = 0;
		float timerDelay = 0;
		textUsing.enabled = true;
		textUsing.gameObject.SetActive (true);
		while(timer < duration)
		{
			timer += Time.deltaTime;
			while(timerDelay < 0.25f)
			{
				timerDelay += Time.deltaTime;
				timer += Time.deltaTime;
				yield return 0;
			}
			// Make textUsing be enabled if it isn't, and disabled if it is.
			textUsing.enabled = !textUsing.enabled;
			timerDelay = 0;
			yield return 0;
		}
		// After the duration ends I just decide to make sure the textUsing is
		// enabled but we need to disable the gameObject.
		textUsing.enabled = true;
		textUsing.gameObject.SetActive(false);
		StopCoroutine ("FlashText");
	}

	IEnumerator TextColorChange(Text textUsing, float duration, bool randomColor = false, Color colorToChangeTo = default(Color))
	{
		float timer = 0;
		float timerDelay = 0;
		Color startColor = textUsing.color;
		textUsing.enabled = true;
		textUsing.gameObject.SetActive (true);
		while(timer < duration)
		{
			timer += Time.deltaTime;
			while(timerDelay < 0.25f)
			{
				timerDelay += Time.deltaTime;
				timer += Time.deltaTime;
				yield return 0;
			}
			if(!randomColor)
			{
				if(textUsing.color == startColor)
					textUsing.color = colorToChangeTo;
				else textUsing.color = startColor;
			}
			else textUsing.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
			timerDelay = 0;
			yield return 0;
		}
		// After the duration ends I just decide to make sure the textUsing is
		// enabled but we need to disable the gameObject.
		textUsing.enabled = true;
		textUsing.gameObject.SetActive(false);
		StopCoroutine ("TextColorChange");
	}

	public void SetupPlayerUIScaling(int numberOfPlayers)
	{
        for (int i = 0; i < numberOfPlayers; i++)
        {
            if ((i == 0 && Manager_Game.P1Lives == 0) || (i == 1 && Manager_Game.P2Lives == 0)
                || (i == 2 && Manager_Game.P3Lives == 0) || (i == 3 && Manager_Game.P4Lives == 0))
                plUIGroup[i].SetActive(false);
        }
        if (numberOfPlayers < 3)
            plUIGroup[0].transform.localScale = new Vector3(1, 0.9f, 1);
        if (numberOfPlayers == 2)
        {
            plUIGroup[1].transform.localScale = new Vector3(1, 0.9f, 1);
            for(int i = 0; i < 2; i++)
                plUIGroup[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(5 + (300 * i), -5);
        }
        else if (numberOfPlayers == 3)
        {
            for(int i = 0; i < 3; i++)
            {
                plUIGroup[i].transform.localScale = new Vector3(0.8f, 0.9f, 1);
                plUIGroup[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(5 + (240 * i), -5);
            }
		}
        else if (numberOfPlayers == 4)
        {
            for(int i = 0; i < 4; i++)
            {
                plUIGroup[i].transform.localScale = new Vector3(0.7f, 0.9f, 1);
                plUIGroup[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(2 + (198 * i), -5);
            }
        }
	}

	// Here is where the transitions happen. You can choose to have a level load
	// after it finishes as well as a startup delay.
	public IEnumerator StartTransition(TransitionTypes typeOfTrans, bool start,
	     bool loadNewLevel = false, int levelToLoad = 1, float delayTime = 1)
	{
		yield return new WaitForSeconds(delayTime);
		InTransition = true;
		imageTransition.gameObject.SetActive(true);
		switch(typeOfTrans)
		{
		case TransitionTypes.Fading:
			Color transColor = imageTransition.color;
			if(start)
			{	
				transColor.a = 1; imageTransition.color = transColor;
				// Make the fade in by decreasing the alpha value of the
				// transition object's color since it starts fully black.
				do
				{
					transColor.a -= 0.5f * Time.deltaTime;
					imageTransition.color = transColor; // Keep updating the actually alpha color of the transition.
					yield return 0;
				} while(transColor.a > 0);
				transColor.a = 0;
				imageTransition.color = transColor;
				imageTransition.gameObject.SetActive(false);
			}
			else
			{
				transColor.a = 0; imageTransition.color = transColor;
				do
				{
					transColor.a += 0.5f * Time.deltaTime;
					imageTransition.color = transColor;
					yield return 0;
				} while(transColor.a < 1);
				transColor.a = 1;
				imageTransition.color = transColor;
			}
			break;
		}

		if(loadNewLevel)
		{
			if(levelToLoad != 0)
				UnityEngine.SceneManagement.SceneManager.LoadScene(levelToLoad);
			else EvenTrigRestartGame(); // If chosen level is 0, that signals a game restart.
		}
		else
		{
			InTransition = false;
			if(!Manager_Game.instance.IsInMenu)
			{
				if(Manager_Cutscene.instance != null)
					// Wait for any cutscenes to end before proceeding, such as
					// the run in one.
					while(Manager_Cutscene.instance.inCutscene)
						yield return 0;
				if(start)
					DisplayUI(true); // Show our UI for the players now.
			}
		}
		StopCoroutine ("StartTransition");
	}
	// I made two different UI animations so far: flashing UI elements, and
	// changing their color. Currently both only for text right now since that was
	// all that was needed.
	public void UIFlash (Text textUsing, float timeToFlash = 4)
	{
		StartCoroutine(FlashText(textUsing, timeToFlash));
	}
	// You can choose to have the text change color. If you choose to have a
	// random one created, then you don't have to specify a chosen color, makes
	// sense.
	public void UIColorFlash(Text textUsing, float timeToFlash, bool randomColor, Color colorToChangeTo = default(Color))
	{
		StartCoroutine (TextColorChange (textUsing, timeToFlash, randomColor, colorToChangeTo));
	}
	// Displaying the player's UI.
	public void DisplayUI(bool enable)
	{
        for (int i = 0; i < Manager_Game.NumberOfPlayers; i++)
        {
            if (!Manager_Game.instance.IsInMenu &&
                ((i == 0 && Manager_Game.P1Lives > 0) || (i == 1 && Manager_Game.P2Lives > 0)
                || (i == 2 && Manager_Game.P3Lives > 0) || (i == 3 && Manager_Game.P4Lives > 0)))
                plUIGroup[i].SetActive(enable);
            else
                plUIGroup[i].SetActive(false);
        }
	}

	public void Pause(int playerWhoPaused = 1)
	{
		foreach (GameObject input in playInputMenu)
			input.SetActive(false);
		// Player menu controlling based on who paused.
		for (int i = 0; i < playInputMenu.Count; i++)
			playInputMenu[i].SetActive(i == playerWhoPaused - 1);
		PauseGroup.SetActive (true);
		secondaryPauseOption.Select ();
		// Make our default option selected. 
		defaultPauseOption.Select();
	}

	// These are needed for the pause menu to access the current scene's
	// Manager_Game script.
	public void EvenTrigUnpause()
	{
		foreach (GameObject input in playInputMenu)
			input.GetComponent<MenuInput>().DisableInput();
		PauseGroup.SetActive (false);
		Manager_Game.instance.Pause ();
		foreach (GameObject input in playInputMenu)
			input.SetActive(false);  // Disable all UI navigation.
	}

	public void EvenTrigRestartGame()
	{
		// Disable input for the menu after choosing an option.
		if (Manager_Game.instance.IsInMenu)
			eveSystem.GetComponent<EventSystem>().enabled = false;
		else
		{
			foreach (GameObject input in playInputMenu)
				input.SetActive(false);
		}
		_pauseMenuOptionChosen = 1;
		// If we are not in a cutscene, we must have pressed for restarting game on the
		// pause menu, so play the sound.
		if(!Manager_Cutscene.instance.inCutscene)
			Manager_Audio.PlaySound (GetComponent<AudioSource>(), Manager_Audio.instance.sfxMenuChoose, true);
		RestartGame ();
	}

	void RestartGame()
	{
		PauseGroup.SetActive (false);
		// Back to default color for the health bars.
		foreach(Image slidFill in plHealthSliderFill)
			slidFill.color = DefaultHealthFillColor;
        // Disable the item groups for the players in case they had an item.
        plItemHealthImageAmount[0].transform.parent.parent.gameObject.SetActive(false);
        plItemHealthImageAmount[1].transform.parent.parent.gameObject.SetActive(false);
        Manager_Game.instance.RestartGame ();
	}

	public void EvenTrigQuit()
	{
		// Disable input for the menu after choosing an option.
		eveSystem.GetComponent<EventSystem> ().enabled = false;
		_pauseMenuOptionChosen = 2;
		Manager_Audio.PlaySound (GetComponent<AudioSource>(), Manager_Audio.instance.sfxMenuChoose, true);
		Quit ();
	}

	void Quit()
	{
		PauseGroup.SetActive (false);
		Manager_Game.instance.Quit ();
	}

    // At the end of the stage, the results will fly in and display in an orderly fashion.
	public IEnumerator DisplayTheResults()
	{
		DisplayUI (false);
		if(DisplayingResults) // Make sure we haven't already started doing this.
			yield break; // Get out of here if we are.
		DisplayingResults = true;
		// The camera movement is no longer needed.
		Camera_BEU.instance.enabled = false;
        // Make these texts invisible if they aren't so they can fade in nicely.
        Color resulColor = resultsText.color;
        groupResultsTable.alpha = resulColor.a = 0;
        resultsText.color = resulColor;
		yield return new WaitForSeconds(2);
		// I use this as a faster way of seeing if there is more than 1 player.
        int numPlayers = Manager_Game.NumberOfPlayers;
		resultsDisplay.SetActive (true);
        resultsParameters.text = _defParameterText; // Reset the displayed text.
        p1ResultsParameters.text = p2ResultsParameters.text = p3ResultsParameters.text = 
            p4ResultsParameters.text = "";
        // Make the results panel fly in. That's the animation I gave it.
        animResultsPanel.SetBool ("IsOpen", true);
		Color resColor = resultsText.color;
		// Make sure the results panel animation has finished. I have a little
		// extra delay after that.
		while(animResultsPanel.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.25f)
			yield return 0;
		// Fade in results text.
		while(resultsText.color.a < 1)
		{
			resColor = resultsText.color;
			resColor.a += 0.5f * Time.deltaTime;
			resultsText.color = resColor;
			yield return 0;
		}
		resColor.a = 1; resultsText.color = resColor;
		// Fade in the table itself.
		while(groupResultsTable.alpha < 1)
		{
			groupResultsTable.alpha += 0.5f * Time.deltaTime;
			yield return 0;
		}
		groupResultsTable.alpha = 1;
		// I have the game manager start finding the grades here. Just to get
		// it figured out ahead of time.
		int bonusP1 = Manager_Game.instance.DetermineGrades (1);
		int bonusP2 = 0, bonusP3 = 0, bonusP4 = 0;
        if(numPlayers > 1 && ShowP2Results)
			bonusP2 = Manager_Game.instance.DetermineGrades(2);
        if(numPlayers > 2 && ShowP3Results)
            bonusP3 = Manager_Game.instance.DetermineGrades(3);
        if(numPlayers > 3 && ShowP4Results)
            bonusP4 = Manager_Game.instance.DetermineGrades(4);
		float timer = 0;
		// These will display all of the stats for the players in the correct
		// places.
		string play1Parameters = "";
		string play2Parameters = "";
        string play3Parameters = "";
        string play4Parameters = "";
		// I make each thing shown one at a time after a set amount of seconds.
		while (timer < 7)
		{
			timer += Time.deltaTime;
			if(timer > 0.5f)
			{
				if(timer < 2)
				{
					play1Parameters = Manager_Game.P1HitsGiven.ToString();
                    if (numPlayers > 1)
                    {
                        play2Parameters = Manager_Game.P2HitsGiven.ToString();
                        if (numPlayers > 2)
                        {
                            play3Parameters = Manager_Game.P3HitsGiven.ToString();
                            if (numPlayers > 3)
                                play4Parameters = Manager_Game.P4HitsGiven.ToString();
                        }
                    }
				}
				else
				{
					if(timer < 3.5f)
					{
						// The \n just simply inserts a new line. It's the same as
						// hitting return or enter.
						play1Parameters = Manager_Game.P1HitsGiven.ToString()
						+ "\n\n" + Manager_Game.P1HitsTaken.ToString();
                        if (numPlayers > 1)
                        {
                            play2Parameters = Manager_Game.P2HitsGiven.ToString()
                            + "\n\n" + Manager_Game.P2HitsTaken.ToString();
                            if (numPlayers > 2)
                            {
                                play3Parameters = Manager_Game.P3HitsGiven.ToString()
                                + "\n\n" + Manager_Game.P3HitsTaken.ToString();
                                if (numPlayers > 3)
                                {
                                    play4Parameters = Manager_Game.P4HitsGiven.ToString()
                                    + "\n\n" + Manager_Game.P4HitsTaken.ToString();
                                }
                            }
                        }
					}
					else
					{
						// Continue to add to the current parameters shown,
						// inserting a new line each time since my text box is
						// very small horizontally.
						if(timer < 5)
						{
							play1Parameters = Manager_Game.P1HitsGiven.ToString()
								+ "\n\n" + Manager_Game.P1HitsTaken.ToString()
								+ "\n\n" + Manager_Game.P1MaxCombo.ToString();
                            if (numPlayers > 1)
                            {
                                play2Parameters = Manager_Game.P2HitsGiven.ToString()
                                + "\n\n" + Manager_Game.P2HitsTaken.ToString()
                                + "\n\n" + Manager_Game.P2MaxCombo.ToString();
                                if (numPlayers > 2)
                                {
                                    play3Parameters = Manager_Game.P3HitsGiven.ToString()
                                    + "\n\n" + Manager_Game.P3HitsTaken.ToString()
                                    + "\n\n" + Manager_Game.P3MaxCombo.ToString();
                                    if (numPlayers > 3)
                                    {
                                        play4Parameters = Manager_Game.P4HitsGiven.ToString()
                                        + "\n\n" + Manager_Game.P4HitsTaken.ToString()
                                        + "\n\n" + Manager_Game.P4MaxCombo.ToString();
                                    }
                                }
                            }
						}
						else
						{
							if(timer < 6.5f)
							{
								play1Parameters = Manager_Game.P1HitsGiven.ToString()
									+ "\n\n" + Manager_Game.P1HitsTaken.ToString()
									+ "\n\n" + Manager_Game.P1MaxCombo.ToString()
									+ "\n\n" + Manager_Game.P1LivesLost.ToString();
                                if (numPlayers > 1)
                                {
                                    play2Parameters = Manager_Game.P2HitsGiven.ToString()
                                    + "\n\n" + Manager_Game.P2HitsTaken.ToString()
                                    + "\n\n" + Manager_Game.P2MaxCombo.ToString()
                                    + "\n\n" + Manager_Game.P2LivesLost.ToString();
                                    if (numPlayers > 2)
                                    {
                                        play3Parameters = Manager_Game.P3HitsGiven.ToString()
                                        + "\n\n" + Manager_Game.P3HitsTaken.ToString()
                                        + "\n\n" + Manager_Game.P3MaxCombo.ToString()
                                        + "\n\n" + Manager_Game.P3LivesLost.ToString();
                                        if (numPlayers > 3)
                                        {
                                            play4Parameters = Manager_Game.P4HitsGiven.ToString()
                                            + "\n\n" + Manager_Game.P4HitsTaken.ToString()
                                            + "\n\n" + Manager_Game.P4MaxCombo.ToString()
                                            + "\n\n" + Manager_Game.P4LivesLost.ToString();
                                        }
                                    }
                                }
							}
						}
					}
				}
			}
			// If player one is present.
			if(ShowP1Results)
				p1ResultsParameters.text = play1Parameters;
            if(numPlayers > 1 && ShowP2Results) // If the total number of players is greater than 2 and P2 is present.
				p2ResultsParameters.text = play2Parameters;
            if (numPlayers > 2 && ShowP3Results)
                p3ResultsParameters.text = play3Parameters;
            if (numPlayers > 3 && ShowP4Results)
                p4ResultsParameters.text = play4Parameters;
			yield return 0;
		}
		// Add two more lines down and then display the player's grade.
		play1Parameters += "\n\n" + Manager_Game.instance.P1Grade;
		if(ShowP1Results)
			p1ResultsParameters.text = play1Parameters;
        if(numPlayers > 1 && ShowP2Results)
		{
			play2Parameters += "\n\n" + Manager_Game.instance.P2Grade;
			p2ResultsParameters.text = play2Parameters;
		}
        if(numPlayers > 2 && ShowP3Results)
        {
            play3Parameters += "\n\n" + Manager_Game.instance.P3Grade;
            p3ResultsParameters.text = play3Parameters;
        }
        if(numPlayers > 3 && ShowP4Results)
        {
            play4Parameters += "\n\n" + Manager_Game.instance.P4Grade;
            p4ResultsParameters.text = play4Parameters;
        }
		timer = 0;
		yield return new WaitForSeconds (0.5f);
		// Bonus: + amounts
		resultsParameters.text += "\n\nBonus:";
		if(ShowP1Results)
			p1ResultsParameters.text += "\n\n+ " + bonusP1.ToString();
        if(numPlayers > 1 && ShowP2Results)
			p2ResultsParameters.text += "\n\n+ " + bonusP2.ToString();
        if(numPlayers > 2 && ShowP3Results)
            p3ResultsParameters.text += "\n\n+ " + bonusP3.ToString();
        if(numPlayers > 3 && ShowP4Results)
            p4ResultsParameters.text += "\n\n+ " + bonusP4.ToString();
		// Add bonuses and show updated scores.
		if(ShowP1Results)
			Manager_Game.instance.ScoreUpdate (1, bonusP1);
        if(numPlayers > 1 && ShowP2Results)
			Manager_Game.instance.ScoreUpdate(2, bonusP2);
        if(numPlayers > 2 && ShowP3Results)
            Manager_Game.instance.ScoreUpdate(3, bonusP3);
        if(numPlayers > 3 && ShowP4Results)
            Manager_Game.instance.ScoreUpdate(4, bonusP4);
		yield return new WaitForSeconds (1);
		// Display score text and scores.
		resultsParameters.text += "\n" + "Score:";
		if(ShowP1Results)
			p1ResultsParameters.text += "\n" + Manager_Game.P1Score.ToString ();
        if(numPlayers > 1 && ShowP2Results)
			p2ResultsParameters.text += "\n" + Manager_Game.P2Score.ToString();
        if(numPlayers > 2 && ShowP3Results)
            p3ResultsParameters.text += "\n" + Manager_Game.P3Score.ToString();
        if(numPlayers > 3 && ShowP4Results)
            p4ResultsParameters.text += "\n" + Manager_Game.P4Score.ToString();
		yield return new WaitForSeconds (1);
		eveSystem.GetComponent<EventSystem>().enabled = true; // Activate our default event system so we can use it only to click the continue button.
		buttonContinue.SetActive (true);
		eveSystem.GetComponent<StandaloneInputModule>().enabled = true; // Allow us to click the continue button.
		buttonContinue.GetComponent<Button> ().Select ();
		WaitingForInput = true;
		// Now wait for the player to press the Continue button.
		while(WaitingForInput)
			yield return 0;
		Manager_Audio.PlaySound (GetComponent<AudioSource>(), Manager_Audio.instance.sfxMenuChoose, true);
		// Close the results panel.
		animResultsPanel.SetBool ("IsOpen", false);
		DisplayingResults = false;
        // I just have the game restart after the fade out by setting the level
        // chosen to load to 0. You would want to set that to your next stage or
        // a world map if you were to have that.
        StartCoroutine(StartTransition(TransitionTypes.Fading, false, true, 0, 2));
        yield return new WaitForSeconds(1.8f);
        buttonContinue.SetActive(false);
        StopCoroutine("DisplayTheResults");
    }

	// Display the Game Over text by fading it in and out. Restarting the game
	// comes after the screen fades out.
	public IEnumerator GameOverUI()
	{
		gameOverText.gameObject.SetActive (true);
		Color gameOverCol = gameOverText.color;
		gameOverCol.a = 0; gameOverText.color = gameOverCol;
		while(gameOverCol.a < 1)
		{
			gameOverCol.a += 0.5f * Time.deltaTime;
			gameOverText.color = gameOverCol;
			yield return 0;
		}
		gameOverCol.a = 1; gameOverText.color = gameOverCol;
		// Restart the game after fading out. This has a 3 second delay to allow
		// some time for the Game Over text to fade out.
		StartCoroutine(StartTransition(TransitionTypes.Fading, false, true, 0, 3));

		while(gameOverCol.a > 0)
		{
			gameOverCol.a -= 0.5f * Time.deltaTime;
			gameOverText.color = gameOverCol;
			yield return 0;
		}
		gameOverCol.a = 0; gameOverText.color = gameOverCol;
		// Stop displaying "Game Over"
		gameOverText.gameObject.SetActive (false);
		StopCoroutine ("GameOverUI");
	}
}