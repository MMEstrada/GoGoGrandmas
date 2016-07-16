using UnityEngine;
using System.Collections;
/// <summary>
/// Character. The base script for all characters(players and enemies). This
/// just holds references to things so that the other character scripts don't need
/// to create their own variable to do the same. They can just use these. Each
/// script does need to setup their own reference of these though. Meaning, they
/// can't all share the ones here. These are just here in case any of those scripts
/// want them.
/// </summary>
public class Character : MonoBehaviour
{
	// A reference to a detectionRadius on our gameObject which is used to
	// find characters and items nearby.
	public DetectionRadius detectionRadius {get; set;}
	public Animator anim {get; set;}
	// Just another thing I added to access the character's transform. Nothing
	// important.
	public Transform myTransform { get {return transform;} }
	// A simple way to see if the character is an enemy by returning its tag's name.
	public bool IsEnemy { get { return gameObject.tag == "Enemy"; } }
	public Rigidbody myRigidbody { get; set; } // A reference to our rigidbody. Each character script gets the reference in Awake if it needs it.
}