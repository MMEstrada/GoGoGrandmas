using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// Manager_Mobile - Used to send messages to the player when they have interacted with one of the
/// virtual mobile buttons or joystick on screen during mobile play. Only used when
/// the usingMobile bool of Manager_Game is true.
/// </summary>
public class Manager_Mobile : MonoBehaviour
{
    public static Manager_Mobile instance;
    public List<VirtualJoystick> virtualJoysticks;

    List<GameObject> _allPlayers; // This will be obtained from Manager_Game after it creates the players. Needed for sending players mobile input event messages upon pressing the virtual buttons.

    void Awake()
    {
        if (!instance)
            instance = this;
    }

    void Start()
    {
        Invoke("CheckToDisable", 0.3f);
    }

    void CheckToDisable()
    {
        // The virtual joystick and this game object will not be disabled if
        // the usingMobile bool of Manager_Game is true and the currently loaded
        // scene is not a menu, otherwise mobile is completely disabled.
        bool enableMe = Manager_Game.usingMobile && !Manager_Game.instance.IsInMenu;
        virtualJoysticks[0].transform.parent.gameObject.SetActive(enableMe);
        gameObject.SetActive(enableMe);
    }

    public void SetPlayers(List<GameObject> allPlayers)
    {
        _allPlayers = allPlayers;
    }

    /// <summary>
    /// These are all of the virtual button messages. Only 1 and 3 for the
    /// setting parameter will be passed into these.  PlayerInput receives these
    /// messages.
    /// </summary>
    /// <param name="setting">0 = No action, 1 = pressed down, 2 (done in PlayerInput) = held, 3 = released</param>
    public void EvenTrigActionPressedP1(int setting)
    {
        _allPlayers[0].SendMessage("ActionButtonPressed", setting);
    }

    public void EvenTrigTargetPressedP1(int setting)
    {
        _allPlayers[0].SendMessage("TargetButtonPressed", setting);
    }

    public void EvenTrigGuardPressedP1(int setting)
    {
        _allPlayers[0].SendMessage("GuardButtonPressed", setting);
    }

    public void EvenTrigJumpPressedP1()
    {
        _allPlayers[0].gameObject.SendMessage("JumpButtonPressed");
    }
}
