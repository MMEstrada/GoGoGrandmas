using UnityEngine;
using UnityEngine.SceneManagement;
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
            SceneManager.LoadScene(3);
        }

        if (Input.GetButtonDown("LoadGame"))
        {
            SceneManager.LoadScene(2);
        }

        if (Input.GetButtonDown("Options"))
        {
            SceneManager.LoadScene(1);
        }

        if (Input.GetButtonDown("ExitGame"))
        {
            Application.Quit();
        }
    
	}
}
