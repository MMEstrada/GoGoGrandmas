using UnityEngine;
using System.Collections;
using UnityEngine.UI;
/// <summary>
/// Menu demo. Menu used in the demo scene only since it's smaller and only meant for the demo.
/// The menu has a few options:
/// * Number of Players
/// * Number of Human Players
/// * Difficulty
/// 
/// Note if you want to use the demo scenes, place the demo menu scene in the build setting for the first index (0)
/// and the demo level directly after that (so the demo level will be index 1)
/// </summary>
public class Menu_Demo : MonoBehaviour
{
	public static Menu_Demo instance;

	public CanvasGroup canGroup_Menu;
	public Slider[] slid_Options;
	public Text[] tex_Options;
	public Button but_Start;

	void Awake()
	{
		if (!instance)
			instance = this;
	}

	// Made this a coroutine so we can wait until after the transition from Manager_UI has finished before
	// making our menu interactable.
	IEnumerator Start()
	{
		if (Manager_Game.instance.currentArea != CurrentArea.Demo_Menu)
		{
			StopCoroutine("Start");
			yield break;
		}
		canGroup_Menu.alpha = 1; // Make sure our menu is visible.
		EvenTrigUpdateSetting("Player"); EvenTrigUpdateSetting("Human");
		EvenTrigUpdateSetting("Diff"); // Update all settings with current slider values.
		while (!Manager_UI.InTransition)
			yield return new WaitForSeconds(0.0001f);
		while (Manager_UI.InTransition)
			yield return new WaitForSeconds(0.0001f);
		canGroup_Menu.interactable = canGroup_Menu.blocksRaycasts = true;
		but_Start.Select();
		StopCoroutine("Start");
	}

	IEnumerator OnLevelWasLoaded(int level)
	{
		if (Manager_Game.instance.currentArea != CurrentArea.Demo_Menu)
		{
			StopCoroutine("OnLevelWasLoaded");
			yield break;
		}
		canGroup_Menu.alpha = 1; // Make sure our menu is visible.
		EvenTrigUpdateSetting("Player"); EvenTrigUpdateSetting("Human");
		EvenTrigUpdateSetting("Diff"); // Update all settings with current slider values.
		while (!Manager_UI.InTransition)
			yield return new WaitForSeconds(0.0001f);
		while (Manager_UI.InTransition)
			yield return new WaitForSeconds(0.0001f);
		canGroup_Menu.interactable = canGroup_Menu.blocksRaycasts = true;
		but_Start.Select();
		StopCoroutine("OnLevelWasLoaded");
	}

	// Fade out the canvas after selecting the Start option. The menu can no longer be
	// interacted with.
	IEnumerator FadeOutCanvas()
	{
		canGroup_Menu.interactable = canGroup_Menu.blocksRaycasts = false;
		while (canGroup_Menu.alpha > 0.02f)
		{
			canGroup_Menu.alpha -= 0.25f * Time.deltaTime;
			yield return new WaitForSeconds(0.0001f);
		}
		canGroup_Menu.alpha = 0;
		StopCoroutine("FadeOutCanvas");
	}

	// Called when we change a setting on the UI.
	public void EvenTrigUpdateSetting (string settingName)
	{
		if (settingName.Contains("Player"))
		{
			Manager_Game.NumberOfPlayers = (int)slid_Options[0].value;
			tex_Options[0].text = "Number of Players:  <color=silver>" + slid_Options[0].value.ToString() + "</color>";
		}
		else if (settingName.Contains("Human"))
		{
			Manager_Game.NumberOfHumans = (int)slid_Options[1].value;
			tex_Options[1].text = "Number of Humans:  <color=silver>" + slid_Options[1].value.ToString() + "</color>";
		}
		else if (settingName.Contains("Diff"))
		{
			int diffNumber = (int)slid_Options[2].value;
			Manager_Game.Difficulty = (GameDifficulty)diffNumber;
			tex_Options[2].text = "Difficulty:  <color=silver>" + Manager_Game.Difficulty.ToString() + "</color>";
		}
	}

	// Called after pressing the "Start" button.
	public void EvenTrigStartGame()
	{
		StartCoroutine("FadeOutCanvas");
		// Make sure our human count does not exceed the player total, otherwise we will
		// set the human count to the player total.
		if (slid_Options[1].value > slid_Options[0].value)
		{
			slid_Options[1].value = slid_Options[0].value;
			Manager_Game.NumberOfHumans = (int)slid_Options[1].value;
		}
		Manager_Audio.PlaySound (GetComponent<AudioSource>(), Manager_Audio.instance.sfxMenuChoose, true);
		Manager_Game.instance.StartGame ();
	}
}