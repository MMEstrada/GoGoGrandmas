using UnityEngine;
using System.Collections;

public class MainMenu : MonoBehaviour {

    public GameObject MainMenuUI;
    
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        if (Input.GetButtonDown("NewGame"))
        {
            Application.LoadLevel(1);
        }

        if (Input.GetButtonDown("LoadGame"))
        {
            Application.LoadLevel(2);
        }

        if (Input.GetButtonDown("Options"))
        {
            Application.LoadLevel(3);
        }

        if (Input.GetButtonDown("ExitGame"))
        {
            Application.Quit();
        }
    
	}
}
