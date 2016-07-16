using UnityEngine;
using System.Collections;
/// <summary>
/// Manager_ audio. Here you can put misc sounds. I put ones here that didn't really
/// fit in putting elsewhere. The menu ones though, I have my Manager_Menu access
/// these from here.
/// Note that this gameObject does not get destroyed for the sake of music not stopping when moving in
/// between scenes.
/// </summary>
public class Manager_Audio : MonoBehaviour
{
	public static Manager_Audio instance;
	public AudioClip[] sfxsGuardHit;
	public AudioClip sfxMenuChoose;
	public AudioClip sfxMenuChange;
	public AudioClip sfxMenuMove;

	void Awake ()
	{
		if (instance == null)
		{
			instance = this; // A reference to this script so any script can access it easily.
			DontDestroyOnLoad (this.gameObject);
		}
		else
		{
			if(instance != this) // A duplicate of this game object is present in the scene, remove it since it isn't using the exact same version of this script.
				Destroy (this.gameObject);
		}
	}

	// Play a sound while stopping a currently playing one from a given audio
	// source with an option to play a one shot of the sound.
	public static void PlaySound(AudioSource audioSource, AudioClip sfx, bool oneShot = false)
	{
		audioSource.Stop ();
		audioSource.clip = sfx;
		if(oneShot)
			audioSource.PlayOneShot(sfx);
		else audioSource.Play();
	}
}