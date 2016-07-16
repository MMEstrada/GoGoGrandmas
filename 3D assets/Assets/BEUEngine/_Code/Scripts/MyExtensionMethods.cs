using UnityEngine;
using System.Collections;
/// <summary>
/// My extension methods. I hold all of my extension methods here. I made one for
/// changing the main color of your character's skinnedMeshRenderer
/// materials and one for changing them to flash a certain color, cyan in my case.
/// They can flash a color or just become invisible and visible again.
/// It signals invulnerability.
/// </summary>
public static class MyExtensionMethods
{
	// Here you pass in your skinnedMeshRenderers and can change
	// the material's color on them. Pass in the default color to go back to
	// it when vulnerable again.
	public static void MaterialColorChange(this SkinnedMeshRenderer[] mySkinnedMeshes, ColorChangeTypes colorChange, Color myDefaultColor, bool vulnerable = true)
	{
		Color newColor = myDefaultColor;
		if(colorChange == ColorChangeTypes.Is_Vulnerable)
		{
			if(!vulnerable)
				newColor = Color.cyan; // Color I use for invulnerability.
		}
		else if(colorChange == ColorChangeTypes.Healed)
			newColor = Color.green; // Heal color.

		foreach(SkinnedMeshRenderer myMesh in mySkinnedMeshes)
		{
			foreach(Material mat in myMesh.materials)
				mat.color = newColor; // Assign this color to each material.
		}
	}

	// Make the character flash on and off. If FlashType.Renderer_Disable is
	// passed in, they will disapper and reappear. When isFlashing is false, this
	// will return you back to normal (visible or default color). You only
	// need to pass in your default color when using the Color_Change FlashType.
	public static void Flashing(this SkinnedMeshRenderer[] mySkinnedMeshes, bool isFlashing, FlashType flashType, Color myDefaultColor = default(Color))
	{
		foreach(SkinnedMeshRenderer myMesh in mySkinnedMeshes)
		{
			if(isFlashing)
			{
				if(flashType == FlashType.Color_Change)
				{
					if(mySkinnedMeshes[0].GetComponent<Renderer>().materials[0].color == myDefaultColor)
					{
						foreach(Material mat in myMesh.materials)
							mat.color = Color.cyan; // My color chosen for invulnerability.
					}
					else
					{
						foreach(Material mat in myMesh.materials)
							mat.color = myDefaultColor;
					}
				}
				// Disable or reenable our mesh to be seen based. This makes it
				// the opposite of what it currently is.
				else myMesh.enabled = !myMesh.enabled;
			}
			else
			{
				if(flashType == FlashType.Color_Change)
				{
					foreach(Material mat in myMesh.materials)
						mat.color = myDefaultColor;
				}
				else myMesh.enabled = true;
			}
		}
	}
}