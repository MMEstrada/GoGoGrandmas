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
    
	}

    public void New_Game()
    {
        SceneManager.LoadScene(3);
    }

    public void Load_Game()
    {
        SceneManager.LoadScene(2);
    }

    public void Options()
    {
        SceneManager.LoadScene(1);
    }

    public void Exit_Game()
    {
        Application.Quit();
    }
}
