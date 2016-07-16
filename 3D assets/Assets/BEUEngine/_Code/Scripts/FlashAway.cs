using UnityEngine;
using System.Collections;
/// <summary>
/// Flash away. A handy little script you can add to gameObjects to make them
/// flash (disappear and reappear) after a set amount of time that you
/// can set.  After they reach a certain amount of time after flashing they 
/// will be removed from the scene.  I use it for the small rocks and
/// other small objects such as crate pieces to be removed after a set time in
/// this project.
/// </summary>
public class FlashAway : MonoBehaviour
{
	public float timeToFlashIn = 4;
	// Destroy the parent if there is one so that the whole gameObject will be
	// removed. You wouldn't always want to do this though, depends on what
	// gameObject has this script.
	public bool destroyParent = true;
	public bool autoFlashAway = false;
	float _flashTime = 0;
	bool _flashTimerStarted = false;

	IEnumerator Start ()
	{
		// Wait a bit so we have plenty of time to set our timeToFlashIn after adding this script to a gameObject and then accessing it.
		yield return new WaitForSeconds(0.9f);
		if(autoFlashAway)
		{
			InvokeRepeating("Flash", timeToFlashIn, 0.3f);
			_flashTimerStarted = true;
		}
		StopCoroutine("Start");
	}

	void FixedUpdate()
	{
		if(_flashTimerStarted) // Begin the count down, or count up I guess.
		{
			_flashTime += Time.deltaTime;
			// Will start flashing 3 seconds before being destroyed.
			if(_flashTime > timeToFlashIn + 3)
			{
				if(!destroyParent)
					Destroy(this.gameObject);
				else
				{
					if(transform.parent != null)
						Destroy (transform.parent.gameObject);
					else Destroy(this.gameObject);
				}
			}
		}
	}

	void Flash ()
	{
		if(!enabled)
		{
			CancelInvoke("Flash");
			if(GetComponent<Renderer>())
				GetComponent<Renderer>().enabled = true;
			return;
		}
		// Set our renderer to whatever it currently isn't.  So if it is
		// currently true it will set it to false, and vice versa.
		if(GetComponent<Renderer>())
			GetComponent<Renderer>().enabled = !GetComponent<Renderer>().enabled;
		else
		{
			// Find whatever kind of mesh renderer this gameObject has!
			SkinnedMeshRenderer[] skinnedMesh = transform.GetComponentsInChildren<SkinnedMeshRenderer>();
			if(skinnedMesh != null && skinnedMesh.Length > 0)
			{
				foreach(SkinnedMeshRenderer mesh in skinnedMesh)
					mesh.GetComponent<Renderer>().enabled = !mesh.GetComponent<Renderer>().enabled;
			}
			else
			{
				MeshRenderer[] myMeshRenderers = transform.GetComponentsInChildren<MeshRenderer>();
				if(myMeshRenderers != null)
				{
					foreach(MeshRenderer mesh in myMeshRenderers)
						mesh.enabled = !mesh.enabled;
				}
			}
		}
	}

	public void ResetFlashTime(float newTimeToFlashIn)
	{
		CancelInvoke("Flash");
		if(GetComponent<Renderer>())
			GetComponent<Renderer>().enabled = true;
		_flashTime = 0;
		timeToFlashIn = newTimeToFlashIn;
		InvokeRepeating("Flash", newTimeToFlashIn, 0.3f);
		MeshRenderer[] myMeshRenderers = transform.GetComponentsInChildren<MeshRenderer>();
		if(myMeshRenderers != null)
		{
			foreach(MeshRenderer mesh in myMeshRenderers)
				mesh.enabled = true;
		}
		_flashTimerStarted = true;
	}
}
