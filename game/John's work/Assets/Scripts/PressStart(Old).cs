using UnityEngine;
using System.Collections;

public class PressStart1 : MonoBehaviour {

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

	// Use this for initialization
	void Start () {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        anim = GetComponent<Animator>();
        }
	
	// Update is called once per frame
	void Update () {
        ready = anim.GetCurrentAnimatorStateInfo(0).IsName("Idle");
        GetComponent<UnityEngine.UI.Text>().enabled = (mode == 0);

    }

	void OnGUI () {
		if (mode == 0 && ready) {
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
        }
    }
	
	void FlipToMenu(int menuNumber){
        if (anim.GetBool("flipped")) { anim.Play("Menu_Flip_Rev"); }
        else { anim.Play("Menu_Flip"); }
        anim.SetBool("flipped", !anim.GetBool("flipped"));
        anim.SetInteger("mode", menuNumber);
      
    }
}
