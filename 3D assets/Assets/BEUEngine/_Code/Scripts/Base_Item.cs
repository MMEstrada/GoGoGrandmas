using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Base_ item. Parent/Base class for all items. This holds things all of the
/// items have in common. I currently don't use condition for collectable items
/// but it's definitely possible to make to just have less of an effect with
/// lower condition.
/// IItem has the WasGrabbed and WasDropped methods.
/// </summary>
public class Base_Item : MonoBehaviour, IItem
{
	// Determines how good the item will be. 0 = bruised/broken/not a good value.
	// 100 = perfect condition.
	private int condition = 100;
	// I have three different types of items and each has their own script.
	public enum ItemTypes
	{
		Collectable = 0, Throwable = 1, Weapon = 2
	}
	public int Condition { get { return condition; } set { condition = value; }}
	public ItemTypes ItemType { get; set; }
	// An offset that can be used for each item for making it look more properly
	// held. I found an offset that looks good for certain items. I currently only
	// use this for the throwables.
	public Vector3 HeldOffset {get; set;}
	// An image icon for the item to be shown on the UI. Used along with showing
	// the currently held item's condition/health.
	public Sprite icon;
	// A reference to the FlashAway script on this item which makes the item
	// flash and then get destroyed after a set chosen time.
	public FlashAway flashAway {get; set;}
	public Rigidbody myRigidbody {get; set;}
	// This gets called from a hand object that touches this item when it is
	// active on the hand's CollisionHand script. The grabbedMount here will be
	// the handMount of a character.
	public void WasGrabbed(Transform grabbedMount)
	{
		// No time limit when grabbed to flash away when grabbed.
		if(flashAway)
			flashAway.ResetFlashTime(Mathf.Infinity);
		// Make sure the item will not move after being held.
		GetComponent<Rigidbody>().isKinematic = true;
		GetComponent<Collider>().enabled = false;
		transform.parent = grabbedMount;
		// I just set up the held offsets in a method.
		HeldOffset = GetHeldOffset();
		// The character grabbing this item is informed that they were
		// successful.
		transform.root.SendMessage("GrabbedItem", transform);
		transform.localPosition = HeldOffset;
		transform.localEulerAngles = Vector3.zero;
	}

	// manualDrop = 1 means that the character dropped it by choice.
	// 0 = the character dropped it after being hit. This gets called by
	// the character who dropped the item in their PlayerItem script.
	public void WasDropped(int manualDrop)
	{
		GetComponent<Rigidbody>().isKinematic = false;
		GetComponent<Collider>().enabled = true;
		GetComponent<Rigidbody>().drag = 0;
		Vector3 moveDir = -transform.root.forward;
		transform.parent = null; // Item is no longer held.
		Vector2 randomDir;
		if(manualDrop == 0) // Drop after character holding it was hit.
		{
			randomDir = Random.insideUnitCircle;
			moveDir = new Vector3 (randomDir.x * 1.5f, 4, randomDir.y * 1.5f);
		}
		else moveDir.y = 4;
		GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
		GetComponent<Rigidbody>().velocity = moveDir;
		// The flash time gets reset to 10 seconds. So after 10 seconds this item
		// will start to flash before getting destroyed, unless picked up again,
		// which stops the timer for that.
		if(flashAway)
			flashAway.ResetFlashTime(10);
	}

	public Vector3 GetHeldOffset()
	{
		Vector3 heldOffset = Vector3.zero;
		// Offsets I found to be good for these throwable items.
		if(gameObject.name.Contains("Rock"))
			heldOffset = new Vector3(0.06f, 0.14f, -0.02f);
		else if(gameObject.name.Contains("Knife"))
			heldOffset = new Vector3(0.06f, 0.07f, -0.01f);
		return heldOffset;
	}

	// Remove our material upon being destroyed, just in case it was a created instance, in order
	// to help free up memory.
	void OnDestroy()
	{
		if(GetComponent<Renderer>())
			DestroyImmediate(GetComponent<Renderer>().material);
	}
}