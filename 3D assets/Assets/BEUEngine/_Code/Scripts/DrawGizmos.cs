using UnityEngine;
using System.Collections;
/// <summary>
/// Draw gizmos. A handy script for being able to see where invisible gameObjects
/// are located in the scene view better by giving them a wired cube or sphere with
/// a chosen size and color. This script doesn't need to be enabled on the gameObject
/// for it to work.
/// </summary>
public class DrawGizmos : MonoBehaviour
{
	public bool drawSphere = true;
	public bool drawCube = false;
	public float size = 2;
	public Color chosenColor = Color.white;
	
	void Start()
	{
		if(drawCube && drawSphere)
			drawCube = false;
	}
	// A Unity method for drawing gizmos in the scene view.
	void OnDrawGizmos ()
	{
		Gizmos.color = chosenColor;
		if(drawCube)
			Gizmos.DrawWireCube(transform.position, new Vector3(size, size, size));
		if(drawSphere)
			Gizmos.DrawWireSphere(transform.position, size);
	}
}
