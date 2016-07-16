using UnityEngine;
using System.Collections;

public class PressStart : MonoBehaviour {

	public int mode = 0;
	float flipDamping = 1f;
	public float turn = 0;
	public float rot;
	public IEnumerator flipRoutine;
	public bool ready = true;
    public int screenWidth;
    public int screenHeight;
    public Texture linkImage;
   // IEnumerator gameModes {"Single Player", "Co-op" };
    Animator anim;
	public GameObject selected;
	public GameObject PSES;
	public GameObject MM;
	public GameObject OP;
	public GameObject LB;


	// Use this for initialization
	void Start () {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        anim = GetComponent<Animator>();
		//UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject (GameObject.FindGameObjectWithTag ("UI Element"));
		PSES = GameObject.Find("EventSystemPS");
		MM = GameObject.Find ("EventSystemMM");
		OP = GameObject.Find ("EventSystemOP");
		LB = GameObject.Find ("EventSystemLB");
        }


	// Update is called once per frame
	void Update () {
		if (!UnityEngine.EventSystems.EventSystem.current)
			UnityEngine.EventSystems.EventSystem.current = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
		//if (!UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject)
		//	UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject (UnityEngine.EventSystems.EventSystem.current.firstSelectedGameObject);
		ready = anim.GetCurrentAnimatorStateInfo(0).IsName("Idle");
		//if (!UnityEngine.EventSystems.EventSystem.current.alreadySelecting) {
			//UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject (GameObject.FindGameObjectWithTag ("UI Element"));
			//selected = GameObject.FindGameObjectWithTag ("UI Element");
		//}
        //GetComponent<UnityEngine.UI.Text>().enabled = (mode == 0);

		PSES.SetActive(!(MM.activeInHierarchy || OP.activeInHierarchy || LB.activeInHierarchy));
       
        for (int x = 0; x < 5; x++) {
            GameObject child = gameObject.transform.GetChild(x).gameObject;
            child.SetActive(mode == x);
            }
        

		//UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(transform.Find("Main Menu").GetComponent<RectTransform>());

		if (mode == 0 && ready)
        {
            if (Input.anyKeyDown)
            {
                FlipToMenu(1);
            }
        }
        //mode 1 - Main Menu
        //mode 2 - Lobby
        //mode 3 - Options


    }

	void OnGUI () {
		/*if (mode == 0 && ready) {
            if (GUI.Button(new Rect(Screen.width / 2 - Screen.width / 5 / 2, Screen.height * 3 / 4 - Screen.height / 10 / 2, Screen.width / 5, Screen.height / 10), "Start"))
            {
                FlipToMenu(1);
            }
        }
		if (mode == 1 && ready) {
			
			if (GUI.Button (new Rect(Screen.width / 2 - Screen.width / 5 / 2, Screen.height * 3 / 4 - Screen.height / 20, Screen.width / 5, Screen.height / 10), "New Game")){

                FlipToMenu(2);
			}
            if (GUI.Button (new Rect(Screen.width/2 - Screen.width/5/2, Screen.height*3/4 - Screen.height/20, Screen.width/5, Screen.height/10), "Level Select")) {
                FlipToMenu(0);
			}
		}
        if (mode == 2 && ready)
        {

            if (GUI.Button(new Rect(Screen.width / 2 - Screen.width / 5 / 2, Screen.height * 3 / 4 - Screen.height / 10 / 2, Screen.width / 5, Screen.height / 10), linkImage))
            {
                Application.LoadLevel("AI Test");
            }

            if (GUI.Button(new Rect(Screen.width / 2 - Screen.width / 5 / 2, Screen.height * 1 / 4 - Screen.height / 10 / 2, Screen.width / 5, Screen.height / 10), "Flip Back"))
            {
                FlipToMenu(1);
            }
        }*/
    }
	
	public void FlipToMenu(int menuNumber){

        if (anim.GetBool("flipped")) { anim.Play("Menu_Flip_Rev"); }
        else { anim.Play("Menu_Flip"); }
        //anim.SetBool("flipped", !anim.GetBool("flipped"));
        anim.SetInteger("mode", menuNumber);


    }
}
