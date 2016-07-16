using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// Menu input. Script for disabling and enabling the correct player's UI navigation with the
/// event system depending on the player number and input received. It will enable the keyboard
/// or joystick StandaloneInputModule depending on which input is received. If one input is enabled and you 
/// give input for the other, the selected option will deselect for a moment, and the default menu
/// option will be selected shortly after. An example would be keyboard input enabled and you put in joystick input.
/// 
/// This script gets placed on a child gameObject of the EventSystem if not on a menu. There is a child gameObject for
/// each player number. If on the menus, this will get placed on the main EventSystem gameObject since only player 1
/// should navigate the menus.
/// </summary>
public class MenuInput : MonoBehaviour
{
	public static MenuInput instance;

	public int playerNumber = 1;

	StandaloneInputModule[] myInputs; // This will get the keyboard and joystick input modules on this gameObject.
	string inputHor, inputHorJS, inputVert, inputVertJS, inputSubmit, inputSubmitJS, inputStart; // Saved input names for this player number.
	bool useTimer; // Should we use a timer for selecting the default option on screen in case we lose our selection from the mouse cursor or switching between keyboard and joystick in game, which could happen.
	float timer; // The timer to go with the above bool.
	KeyCode[] keyboardAxis; // Directional input with the keyboard. Used when time is frozen.

	void Awake()
	{
		if (instance == null)
			instance = this;
		myInputs = gameObject.GetComponents<StandaloneInputModule>();
		inputHor = myInputs[0].horizontalAxis; inputHorJS = myInputs[1].horizontalAxis;
		inputVert = myInputs[0].verticalAxis; inputVertJS = myInputs[1].verticalAxis;
		inputSubmit = myInputs[0].submitButton; inputSubmitJS = myInputs[1].submitButton;
		inputStart = "PauseP" + playerNumber.ToString();
		// These are the default movement keys I use for movement with player 1 and player 2. If yours are different,
		// change them here.
        if (playerNumber == 1 || playerNumber == 3)
			keyboardAxis = new KeyCode[4] { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.W };
        else if(playerNumber == 2 || playerNumber == 4)
			keyboardAxis = new KeyCode[4] { KeyCode.LeftArrow, KeyCode.DownArrow, KeyCode.RightArrow, KeyCode.UpArrow };
	}

	void Update ()
	{
		if(myInputs == null || myInputs.Length == 0)
		{
			Debug.LogWarning("ASSIGN INPUTS PLEASE FOR " + gameObject.name);
			return;
		}

		// Anytime we press our pause/start button, the default option will get selected on whatever menu we are
		// currently on, whether it is a menu scene, or just the pause menu.  I have this here just in case
		// you lose your selection from using the mouse cursor, or changing between keyboard and joystick in game,
		// which might happen.  I have a check for that below also but it only works for a given scenario.
		if (Input.GetButtonDown(inputStart))
		{
			if(Time.timeScale > 0)
				Invoke("ResetDefaultSelected", 0.3f);
			else
				useTimer = true;
		}
		// Can't use Invoke when time is frozen so we use a timer instead. Can't use any Time methods.
		if (useTimer)
		{
			if(timer < 1)
				timer += 0.08f;
			else
			{
				useTimer = false;
				ResetDefaultSelected(); // Select the current default option on screen.
				timer = 0;
			}
		}
		// If our keyboard input is enabled.
		if (myInputs[0].enabled)
		{
			// Note that getting the joystick axis does work when time is frozen (Time.timeScale == 0),
			// unlike keyboard axis.
			if (Input.GetAxis(inputHorJS) != 0 || Input.GetAxis(inputVertJS) != 0
				|| ( (playerNumber == 1 && Input.GetKeyDown(KeyCode.Joystick1Button3) ) 
                    || (playerNumber == 2 && Input.GetKeyDown(KeyCode.Joystick2Button3) )
                    || (playerNumber == 3 && Input.GetKeyDown(KeyCode.Joystick3Button3) )
                    || (playerNumber == 4 && Input.GetKeyDown(KeyCode.Joystick4Button3) ) ) )
			{
				myInputs[0].enabled = false;
				Invoke("ResetDefaultSelected", 0.4f);
				myInputs[1].enabled = true;
			}
		}
		else if (myInputs[1].enabled) // Our joystick input is enabled.
		{
			// If we press any keyboard input. Note that Input.GetAxis for keyboard input does not work when
			// time is frozen (Time.timeScale ==0), which is why I made a separate condition for it.
			if ( (Time.timeScale > 0 && (Input.GetAxis(inputHor) != 0 || Input.GetAxis(inputVert) != 0) )
				|| (Time.timeScale == 0 && (keyboardAxis.Any(key => Input.GetKeyDown(key) ) ) )
                || ( ((playerNumber == 1 || playerNumber == 3) && Input.GetKeyDown(KeyCode.Z) ) 
                    || ((playerNumber == 2 || playerNumber == 4) && Input.GetKeyDown(KeyCode.N) ) ))
			{
				myInputs[1].enabled = false;
				Invoke("ResetDefaultSelected", 0.4f);
				myInputs[0].enabled = true;
			}
		}
		else // Neither inputs are enabled.
		{
			// We check for keyboard input here...
			if ( (Time.timeScale > 0 && (Input.GetAxis(inputHor) != 0 || Input.GetAxis(inputVert) != 0) )
				|| (Time.timeScale == 0 && (keyboardAxis.Any(key => Input.GetKeyDown(key) ) ) )
                || ( ((playerNumber == 1 || playerNumber == 3) && Input.GetKeyDown(KeyCode.Z) ) 
                    || ((playerNumber == 2 || playerNumber == 4) && Input.GetKeyDown(KeyCode.N) ) ))
			{
				myInputs[0].enabled = true; // Enable our keyboard input.
			}
			// And joystick input here...
			else if (Input.GetAxis(inputHorJS) != 0 || Input.GetAxis(inputVertJS) != 0
                || ( (playerNumber == 1 && Input.GetKeyDown(KeyCode.Joystick1Button3) ) 
                    || (playerNumber == 2 && Input.GetKeyDown(KeyCode.Joystick2Button3) )
                    || (playerNumber == 3 && Input.GetKeyDown(KeyCode.Joystick3Button3) )
                    || (playerNumber == 4 && Input.GetKeyDown(KeyCode.Joystick4Button3) ) ) )
			{
				myInputs[1].enabled = true; // Enable our joystick input.
			}
			// When neither inputs are enabled, this will ensure we if we press our submit button on the menu,
			// we will start the game since the default option on both the main menu and demo menu is Start.
            if (Input.GetButtonDown(inputSubmit) || Input.GetButtonDown(inputSubmitJS))
            {
                if (Manager_Game.instance.IsInMenu)
                {
                    if (Manager_Game.instance.currentArea == CurrentArea.Main_Menu)
                    {
                        Manager_Menu.instance.EndMenu();
                        Manager_UI.instance.allMenus[0].IsOpen = false;
                    }
                    else if (Manager_Game.instance.currentArea == CurrentArea.Demo_Menu)
                        Menu_Demo.instance.EvenTrigStartGame();
                }
                else
                {
                    if (Time.timeScale == 0) // We are paused.
                    {
                        Manager_UI.instance.EvenTrigUnpause(); // Default option is return to game, so unpause.
                        Manager_Audio.PlaySound (Manager_UI.instance.GetComponent<AudioSource>(), Manager_Audio.instance.sfxMenuMove, true);
                    }
                }
            }
		}
	}

	// If on a menu, prepare to find the current open menu so we can select its default option.
	// If not on a menu, we simply check to see that one of the inputMenus gameObject is activated
	// so that we can use its event system to select the default pause option. Note that if 
	// no event system component is present in the scene, an error will occur when trying to select
	// a selectable so that's why I have that safety check there.
	void ResetDefaultSelected()
	{
		if (Manager_Game.instance.IsInMenu)
		{
			IEnumerable<Menu> openMenu = from Menu curMenu in Manager_UI.instance.allMenus
			                             where curMenu.IsOpen
			                             select curMenu; // Only one menu will be open so only one will be added.
			foreach (Menu curMenu in openMenu) // Always use foreach to go through IEnumerable
				curMenu.defaultSelectedOption.Select();
		}
		else
		{
			if (Manager_UI.instance.playInputMenu.Any(inputGO => inputGO.activeSelf))
			{
				Manager_UI.instance.secondaryPauseOption.Select();
				Manager_UI.instance.defaultPauseOption.Select();
			}
		}
	}

	// Unpausing or exiting the menu.
	public void DisableInput()
	{
		if (myInputs != null && myInputs.Length > 0)
		{
			myInputs[0].enabled = myInputs[1].enabled = false;
			gameObject.SetActive(false);
		}
	}
}