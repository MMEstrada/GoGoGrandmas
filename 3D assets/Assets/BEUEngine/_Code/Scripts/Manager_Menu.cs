using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
/// <summary>
/// Manager_ menu. Allows you to change menus and update settings. That method will
/// be called from a OnClick event on an UI setting such as a slider change or
/// button press. This script gets placed on your main canvas for your UI menus.
/// Place all of the public things in the inspector to their corresponding buttons,
/// sliders, or whatever kind of type they are.
/// </summary>
public class Manager_Menu : MonoBehaviour
{
	public static Manager_Menu instance;

	public Menu currentMenu;
	// Just in case you use different icons for the characters on the character
	// select screen versus the main game screen, they will go here.
	// These are the icons for the characters on the character select screen.
	public Sprite[] icons_PlayChar;
	// The group object for P2's section on the character select screen.
	public GameObject vertCharGroupP2, vertCharGroupP3, vertCharGroupP4;
	public Text[] text_PlayChar; // Names of the characters
	public Text[] text_StatsPlayers; // Their stats to be displayed.
	public Text text_Dif; // Difficulty of the game to be displayed.
	public Text text_ItemRate; // Rate of items that is displayed.
	public Text text_NumbOfPlayers; // Total number of players
	public Text text_NumbOfHumans; // ...and number of humans players from that.
	public Image[] icon_PlayChar; // Image to use for each player character.
	public Slider[] slid_PlayChar; // Slider to change between characters.
	public Slider slid_Dif; // Sliders to change various settings...
	public Slider slid_ItemRate;
	public Slider slid_NumbOfPlayers;
	public Slider slid_NumbOfHumans;

	private AudioClip sfxMenuChange; // Various menu sounds, obtained from Manager_Audio for easy access.
	private AudioClip sfxMenuChoose;

	void Awake()
	{
		if (instance == null)
			instance = this;
	}

	void Start ()
	{
		// I get these from Manager_Audio for easy access.
		sfxMenuChoose = Manager_Audio.instance.sfxMenuChoose;
		sfxMenuChange = Manager_Audio.instance.sfxMenuChange;
		// Update the settings on the menus which what is set currently.
        UpdateSetting ("Char P1"); UpdateSetting ("Char P2"); UpdateSetting("Char P3"); UpdateSetting("Char P4");
		UpdateSetting ("Difficulty"); UpdateSetting ("Item");
		UpdateSetting ("Players"); UpdateSetting ("Human");
		// In Start() this will show your default menu, which is what I have
		// set to currentMenu when starting. That's the Main Menu of course.
		ShowMenu (currentMenu);
	}

	public void ShowMenu(Menu menu)
	{
		// If there is a current menu active...
		if(menu != null)
			currentMenu.IsOpen = false; // We close our current one by playing its close animation.
		if(Time.timeSinceLevelLoad > 1.5f)
			PlayASound(sfxMenuChoose);
		// Make the current menu equal this new one we chose and then play its
		// open animation by setting its IsOpen property to true.
		if(menu.Name.Contains("Character")) // If character select menu.
		{
            // Determine which groups need to be enabled depending on the number of players.
            if (Manager_Game.NumberOfPlayers < 4)
                vertCharGroupP4.SetActive(false);
            else
                vertCharGroupP4.SetActive(true);
            if (Manager_Game.NumberOfPlayers < 3)
                vertCharGroupP3.SetActive(false);
            else
                vertCharGroupP3.SetActive(true);
            if (Manager_Game.NumberOfPlayers < 2)
                vertCharGroupP2.SetActive(false);
            else
                vertCharGroupP2.SetActive(true);
		}

		currentMenu = menu; // This is now our currentMenu.
		currentMenu.IsOpen = true; // Play its "Open" animation.
	}

	public void UpdateSetting(string settingChoice)
	{
		// These are various names I gave these settings from the UI click
		// events. Check the menus for character and options in the Canvas_Menu
		// gameObject on the MainMenu scene for where its done. You will find them
		// on the sliders and buttons in those menus.
		if (settingChoice.Contains ("Char")) 
		{
            for (int i = 0; i < 4; i++)
            {
                string playNum = (i + 1).ToString();
                if ( (settingChoice.Contains("P" + playNum)) || (settingChoice.Contains("Player " + playNum) )) 
                {
                    if(slid_PlayChar[i].value == 0)
                    {
                        text_PlayChar[i].text = "Ethan";
                        Manager_Game.PlayersChosen[i] = PlayerCharacters.Ethan;
                        icon_PlayChar[i].sprite = icons_PlayChar[0];
                        // Display stats in a vertical line.
                        text_StatsPlayers[i].text = "5\n4"; // \n = new line
                    }
                    else if(slid_PlayChar[i].value == 1)
                    {
                        text_PlayChar[i].text = "Dude";
                        Manager_Game.PlayersChosen[i] = PlayerCharacters.Dude;
                        icon_PlayChar[i].sprite = icons_PlayChar[1];
                        text_StatsPlayers[i].text = "4\n5";
                    }
                    else if(slid_PlayChar[i].value == 2)
                    {
                        text_PlayChar[i].text = "Ethan Twin";
                        Manager_Game.PlayersChosen[i] = PlayerCharacters.Ethan_Twin;
                        icon_PlayChar[i].sprite = icons_PlayChar[2];
                        text_StatsPlayers[i].text = "6\n3";
                    }
                    else if(slid_PlayChar[i].value == 3)
                    {
                        text_PlayChar[i].text = "Dude Twin";
                        Manager_Game.PlayersChosen[i] = PlayerCharacters.Dude_Twin;
                        icon_PlayChar[i].sprite = icons_PlayChar[3];
                        text_StatsPlayers[i].text = "3\n6";
                    }
                }
            }
		}
		// Changing the difficulty setting.
		else if(settingChoice.Contains("Diff"))
		{
			if(slid_Dif.value == 0)
				Manager_Game.Difficulty = GameDifficulty.Easy;
			else if(slid_Dif.value == 1)
				Manager_Game.Difficulty = GameDifficulty.Normal;
			else Manager_Game.Difficulty = GameDifficulty.Hard;
			text_Dif.text = Manager_Game.Difficulty.ToString();
		}
		// Changing the item appear rate.
		else if(settingChoice.Contains("Item"))
		{
			if(slid_ItemRate.value == 0)
				Manager_Game.ItemAppearRate = AmountRating.None;
			else if(slid_ItemRate.value == 1)
				Manager_Game.ItemAppearRate = AmountRating.Very_Low;
			else if(slid_ItemRate.value == 2)
				Manager_Game.ItemAppearRate = AmountRating.Low;
			else if(slid_ItemRate.value == 3)
				Manager_Game.ItemAppearRate = AmountRating.Medium;
			else if(slid_ItemRate.value == 4)
				Manager_Game.ItemAppearRate = AmountRating.High;
			else Manager_Game.ItemAppearRate = AmountRating.Very_High;
			string appearRate = Manager_Game.ItemAppearRate.ToString();
			// Some of my appear rate names have a '_' in them such as
			// Very_Low and Very_High. I don't want that displayed so this
			// next part gets rid of those with an empty space.
			if(appearRate.Contains("_"))
				appearRate = appearRate.Replace('_', ' ');
			text_ItemRate.text = appearRate;
		}
		else if(settingChoice.Contains("Players")) // Number of players setting.
		{
			Manager_Game.NumberOfPlayers = (int)slid_NumbOfPlayers.value + 1;
			text_NumbOfPlayers.text = Manager_Game.NumberOfPlayers.ToString();
		}
		else if(settingChoice.Contains("Human")) // Number of human players setting.
		{
			Manager_Game.NumberOfHumans = (int)slid_NumbOfHumans.value + 1;
			text_NumbOfHumans.text = Manager_Game.NumberOfHumans.ToString();
		}
		// Making sure a sound doesn't play upon starting the scene.
		if(Time.timeSinceLevelLoad > 2)
			PlayASound (sfxMenuChange);
	}

	public void PlayASound(AudioClip sfx)
	{
		Manager_Audio.PlaySound (GetComponent<AudioSource>(), sfx, true);
	}

	// Called when "Start" has been chosen on the main menu to start the game.
	public void EndMenu()
	{
		Manager_Audio.PlaySound (GetComponent<AudioSource>(), sfxMenuChoose, true);
		Manager_Game.instance.StartGame ();
	}
}