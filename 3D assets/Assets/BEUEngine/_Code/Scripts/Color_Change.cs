using UnityEngine;
using System.Collections;
/// <summary>
/// Color_ change. Change the color of a gameObject's material. For regular
/// renderers.
/// </summary>
public class Color_Change : MonoBehaviour
{
	public Color colorToChangeTo;

	int _totalMats; // Total number of materials on our renderer, make sure we have one.

	void Start ()
	{
		if(GetComponent<Renderer>())
		{
			_totalMats = GetComponent<Renderer>().materials.Length;
			foreach(Material mat in GetComponent<Renderer>().materials)
				mat.color = colorToChangeTo;
		}
		else Debug.LogWarning("No renderer found on game object " + gameObject.name + " which has a Color_Change script attached.");
	}

	// Remove created instance(s) of our materials whenever mySkinnedMeshes.materials was called
	// as we get destroyed otherwise they will add up from enemies overtime and can eventually
	// crash the game!
	void OnDestroy()
	{
		// Make sure the game is NOT shutting done first. This gets set to true when the application quits.
		if(Manager_Game.GameShuttingDown)
			return;
		for (int j = 0; j < _totalMats; j++)
			DestroyImmediate (GetComponent<Renderer>().materials[j]);
	}
}