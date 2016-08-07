using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIFunctions : MonoBehaviour {

    public GameObject menus;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void New_Game()
    {
        SceneManager.LoadScene(1);
    }

    public void Load_Game()
    {
        SceneManager.LoadScene(2);
    }

    public void Options()
    {
        SceneManager.LoadScene(3);
    }

    public void Exit_Game()
    {
        Application.Quit();
    }

    public void Start_Game()
    {
        SceneManager.LoadScene(4);
    }

    public void Back()
    {
        SceneManager.LoadScene(1);
    }
}

