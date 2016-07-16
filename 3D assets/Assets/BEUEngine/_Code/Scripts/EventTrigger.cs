using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
/// <summary>
/// Event trigger. Here I edited the OnDeselect functionality so that I could
/// play a menu move sound when a selectable UI element is deselected. This script is placed on a selectable
/// which is a child of the main canvas UI. Canvas_Overlay or Canvas_Menu are the root.
/// </summary>
/// You need to implement the IDeselectHandler in order to access OnDeselect
/// for selectables.
public class EventTrigger : MonoBehaviour, IDeselectHandler
{
	public virtual void OnDeselect (BaseEventData data)
	{
		// Only play the sound if the element is interactable. I disable that
		// when animations are playing in between menus.
		if(gameObject.GetComponent<Selectable>().IsInteractable())
			Manager_Audio.PlaySound (transform.root.GetComponent<AudioSource>(), Manager_Audio.instance.sfxMenuMove, true);
	}
}