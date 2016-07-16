using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// Detection radius. This helps us find items and opposing characters nearby. It uses a large sphere trigger.
/// Whenever objects get inside of it, conditions are checked to see if they should be added to any of the lists here.
/// </summary>
public class DetectionRadius : MonoBehaviour
{
	public SearchForTypes mySearchType;
	// Check this if you want the character using this to be able to get items.
	// That goes for whether they are AI or not, doesn't matter.
	public bool searchForItems = true;
	public LayerMask charactersNearbyMask {get; private set;} // For checking opposing characters nearby. There is a separate layer for players and enemies. Enemies check for players and players check for enemies.
	public List<Transform> inRangeChar {get; private set;} // All characters in our trigger's radius.
	public List<Transform> inCloseRangeChar {get; private set;} // Opposing characters in hit range.
	public List<Transform> inRangeItems {get; private set;} // ALl items in our trigger's radius that can be interacted with.
	public List<Transform> inGrabRangeItems {get; private set;} // Items close enough to grab.
	public List<Transform> itemStorersInRange {get; private set;} // Item boxes/crates
	List<Transform> itemStorersCloseBy; // Item boxes/crates close enough to kick.

	// I use these next three for AI to see which sides around this character
	// are open.
	public bool FoeNearBack {get; private set;}
	public bool FoeNearLeft {get; private set;}
	public bool FoeNearRight {get; private set;}
	// This checks to see if there are any item boxes/crates close by enough
	// to us to let us know we can kick them if they are.
	public bool NextToItemStorer { get { return itemStorersCloseBy != null &&
			itemStorersCloseBy.Count > 0; } }

	void Start()
	{
		inRangeChar = new List<Transform> ();
		inCloseRangeChar = new List<Transform> ();
		inRangeItems = new List<Transform> ();
		inGrabRangeItems = new List<Transform> ();
		itemStorersInRange = new List<Transform> ();
		itemStorersCloseBy = new List<Transform> ();
		// Players search for enemies and enemies search for players.
		// The search types have the same name as the character tags.
		if(transform.parent.tag == "Player")
			mySearchType = SearchForTypes.Enemy;
		else mySearchType = SearchForTypes.Player;
	}

	void Update()
	{
		// Remove any null instances. This can happen after they get
		// destroyed or removed in some other way.
		if(inRangeChar.Contains(null))
			inRangeChar.Remove(null);
		if(inCloseRangeChar.Contains(null))
			inCloseRangeChar.Remove(null);
		if(itemStorersCloseBy.Contains(null))
			itemStorersCloseBy.Remove(null);
		if(itemStorersInRange.Contains(null))
			itemStorersInRange.Remove(null);
		if(inRangeItems.Contains(null))
			inRangeItems.Remove(null);
		if(inGrabRangeItems.Contains(null))
			inGrabRangeItems.Remove(null);
	}

	void FixedUpdate()
	{
		// See which opposing characters are close by. Only needed to check
		// when in battle.
		if(Manager_BattleZone.instance.InBattle && mySearchType == SearchForTypes.Enemy
		   && inCloseRangeChar.Count > 0)
		{
			EnemiesNearCheck();
		}
	}

	void OnTriggerStay (Collider other)
	{
		// We found our search type.
		if(other.gameObject.tag == mySearchType.ToString() && !other.isTrigger)
		{
			// Make sure they aren't dead. I add that to the character's name
			// after they die so accessing a script from them isn't needed.
			if(!other.gameObject.name.Contains("Dead"))
			{
				if(!inRangeChar.Contains(other.transform))
					inRangeChar.Add(other.transform);

				float dist = Vector3.Distance(transform.position, other.transform.position);
				if(dist < 1.6f || (dist < 3.2f && other.GetComponent<CharacterStatus>().AmIFalling)) // This is how close I use in order to add these.
				{
					if(!inCloseRangeChar.Contains(other.transform))
						inCloseRangeChar.Add(other.transform);
				}
				else // Otherwise they are too far away to be considered in close
					// range.
				{
					if(inCloseRangeChar.Contains(other.transform))
						inCloseRangeChar.Remove(other.transform);
				}
			}
			else
			{
				if(inRangeChar.Contains(other.transform))
					inRangeChar.Remove(other.transform);
				if(inCloseRangeChar.Contains(other.transform))
					inCloseRangeChar.Remove(other.transform);
			}
		}
		// Here we can check for items if allowed.
		if(searchForItems)
		{
			// The SphereCollider trigger is the one I use for detecting the
			// item. Each item has this.
			if(other.gameObject.tag == "Item" && other.isTrigger &&
			   other is SphereCollider)
			{
				float dist = Vector3.Distance(transform.position, other.transform.position);
				// Make sure the item isn't already held (transform.parent != null)
				// and that there rigidbody is not disabled.
				if(other.transform.parent == null && !other.GetComponent<Rigidbody>().isKinematic)
				{
					if(!inRangeItems.Contains(other.transform))
						inRangeItems.Add(other.transform);

					if(dist < 1) // Close enough to grab them.
					{
						if(!inGrabRangeItems.Contains(other.transform))
							inGrabRangeItems.Add(other.transform);
					}
					else
					{
						if(inGrabRangeItems.Contains(other.transform))
							inGrabRangeItems.Remove(other.transform);
					}
				}
				else
				{
					if(inRangeItems.Contains(other.transform))
						inRangeItems.Remove(other.transform);
					if(inGrabRangeItems.Contains(other.transform))
						inGrabRangeItems.Remove(other.transform);
				}
			}
			// ItemStorers are the crates/item boxes I use that contain items.
			if(other.gameObject.tag == "ItemStorer")
			{
				float dist = Vector3.Distance(transform.position, other.transform.position);
				if(!itemStorersInRange.Contains(other.transform))
					itemStorersInRange.Add(other.transform);
				if(dist < 1.3f) // Close enough range to kick them.
				{
					if(!itemStorersCloseBy.Contains(other.transform))
						itemStorersCloseBy.Add(other.transform);
				}
				else
				{
					if(itemStorersCloseBy.Contains(other.transform))
						itemStorersCloseBy.Remove(other.transform);
				}
			}
		}
	}

	// Remove any of these from their list when we exit their trigger.
	void OnTriggerExit(Collider other)
	{
		if(other.gameObject.tag == mySearchType.ToString() && !other.isTrigger)
		{
			if(inRangeChar.Contains(other.transform))
				inRangeChar.Remove(other.transform);
			if(inCloseRangeChar.Contains(other.transform))
				inCloseRangeChar.Remove(other.transform);
		}

		if(other.gameObject.tag == "Item")
		{
			if(inRangeItems.Contains(other.transform))
				inRangeItems.Remove(other.transform);
		}

		if(other.gameObject.tag == "ItemStorer")
		{
			if(itemStorersInRange.Contains(other.transform))
				itemStorersInRange.Remove(other.transform);
			if(itemStorersCloseBy.Contains(other.transform))
				itemStorersCloseBy.Remove(other.transform);
		}
	}

	// Used for AI to see which opposing characters are around us so they can
	// move around us more accordingly.
	void EnemiesNearCheck()
	{
		RaycastHit rayHit;
		// Do a check for the back, left side, and right side.
		for(int i = 0; i < 3; i++)
		{
			Vector3 posChecking = -transform.forward;
			bool checkUsing = false;
			if(i == 1) posChecking = transform.right;
			else if(i == 2) posChecking = -transform.right;
			if(Physics.Raycast (new Ray(transform.position + Vector3.up, posChecking), out rayHit, 1, charactersNearbyMask))
			{
				// If this is true then we have an opposing character on this
				// side of us so make this true.
				if(rayHit.collider.gameObject.tag == mySearchType.ToString())
					checkUsing = true;
				else checkUsing = false;
			}
			else checkUsing = false;
			if(i == 0) FoeNearBack = checkUsing;
			else if(i == 1) FoeNearRight = checkUsing;
			else if(i == 2) FoeNearLeft = checkUsing;
		}
	}
}