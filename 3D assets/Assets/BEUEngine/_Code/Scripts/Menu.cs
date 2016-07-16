using UnityEngine;
using UnityEngine.UI;
using System.Collections;
/// <summary>
/// Menu. Each of your individual menu gameObjects will have this script. Have a
/// look at my MainMenu, CharacterMenu, and OptionsMenu gameObjects to
/// see where I used this. You just need to provide the default option to be
/// selected when the menu is open. You do that in the inspector.
/// </summary>
public class Menu : MonoBehaviour
{
	public Selectable defaultSelectedOption;
	// Easy way to find the name of this gameObject.
	public string Name { get { return gameObject.name; } }
	// Easier way to set our Animator's IsOpen bool
	public bool IsOpen
	{
		get { return _anim.GetBool ("IsOpen"); }
		set { _anim.SetBool("IsOpen", value); } // value = value given when setting this. 
	}

	Animator _anim;
	CanvasGroup _canvasGroup;

	void Awake ()
	{
		_anim = GetComponent<Animator> ();
		_canvasGroup = GetComponent<CanvasGroup> ();

		RectTransform rectTrans = GetComponent<RectTransform> ();
		// Place this rectTransform at 0, 0 when starting up.
		rectTrans.offsetMax = rectTrans.offsetMin = Vector2.zero;
	}

	void Update ()
	{
		// If we are not in our opening state.
		if(!_anim.GetCurrentAnimatorStateInfo(0).IsName("Open"))
			_canvasGroup.blocksRaycasts = _canvasGroup.interactable = false;
		else
		{
			// After the animation has fully ended
			if(_anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
			{
				// We make everything in our canvas group interactable again.
				// That is disabled during animations.
				if(!_canvasGroup.interactable)
				{
					// This is now interactable and receive raycasts from
					// the mouse.
					_canvasGroup.blocksRaycasts = _canvasGroup.interactable = true;
					// Select our default option.
					defaultSelectedOption.Select();
				}
			}
		}
	}
}