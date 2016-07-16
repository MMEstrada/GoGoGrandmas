using UnityEngine;
using System.Collections;
/// <summary>
/// Collision hand. This is used for grabbing items. This script gets placed on the
/// character's grab mounts for items. Those would be grabMountItem and
/// grabMountWeapon.
/// </summary>
public class CollisionHand : MonoBehaviour {

	bool isWeaponMount;

	void Start()
	{
		isWeaponMount = gameObject.name.Contains("Weapon");
	}

	void OnTriggerEnter(Collider other)
	{
		if(GetComponent<Collider>() == null)
		{
			Debug.LogError("You need to assign a collider for " + gameObject.name);
			return;
		}
		if(!GetComponent<Collider>().enabled) // This collider needs to be enabled.
			return;

		// Here we check to see if the other gameObject implements the IItem
		// interface. That means it is an item and can be picked up. I have all
		// of my items implement that interface since they can all be picked up.
		IItem isItemCheck = (IItem) other.GetComponent( typeof(IItem) );
		bool otherIsWeapon = other.gameObject.GetComponent<ItemWeapon>() != null;
		bool canPickup = (isWeaponMount && otherIsWeapon) // Can only pick up a corresponding item type based on what mount we are.
			|| (!isWeaponMount && !otherIsWeapon);
		if( isItemCheck != null && canPickup && transform.childCount == 0
		   && other.transform.parent == null && !other.GetComponent<Rigidbody>().isKinematic)
		{
			// Pick up the other gameObject since it is an item!
			isItemCheck.WasGrabbed(transform);
		}
	}
}