using UnityEngine;
using System.Collections;
using UnityEngine.UI;
/// <summary>
/// Character item. This script deals with all of the character's interaction
/// with items.
/// </summary>
public class CharacterItem : Character
{
	// These relate to if we are holding an item at the end of a scene so that
	// we will be able to spawn it and hold it at the beginning of the next.
	public static int itemHoldingAtEndP1, itemHoldingAtEndP2, itemHoldingAtEndP3, itemHoldingAtEndP4;
	// Both of these mounts have set positions and rotations to help make
	// look holding the specific items better.
	// The mount used for grabbing collectable and throwable items.
	public Transform grabMountItem;
	// Mount used for yes, weapons.
	public Transform grabMountWeapon;
	// I use IK when in the grab animation to make the character's hand move
	// towards the item they are trying to pick up. This is the weight for that,
	// from 0 - 1. Check OnAnimatorIK to see it.
	float _handWeight = 0;
	// How long for the weight to take to get to 1.
	float _handBlendTime = 3f;
	// Which item we are trying to pick up after we get close enough to one. This
	// gets assigned in the Pickup() method.
	Transform itemTryingToPickUp;
	CharacterStatus _charStatus;
	CharacterMotor _charMotor;
	CharacterAttacking _charAttacking;
    GameObject _itemGroup; // Group for displaying an item we have.
	// An image for an item's health/condition. Used for weapons and throwables.
    Image itemHealthImage;
	// And the image icon for that item.
	Image itemIconImage;
	// This will be assigned the gameObject of an item we are holding.
	public GameObject ItemHolding { get; private set; }

	void Awake()
	{
		_charStatus = GetComponent<CharacterStatus> ();
		_charMotor = GetComponent<CharacterMotor> ();
		_charAttacking = GetComponent<CharacterAttacking> ();
	}

	void Start ()
	{
		detectionRadius = transform.GetComponentInChildren<DetectionRadius> ();
		anim = GetComponent<Animator> ();
		// If we have brought an item into a following scene, here is where
		// we will create it and hold it.
		if(gameObject.tag == "Player" && ( (_charStatus.PlayerNumber == 1 && itemHoldingAtEndP1 != 0)
            || (_charStatus.PlayerNumber == 2 && itemHoldingAtEndP2 != 0)
            || (_charStatus.PlayerNumber == 3 && itemHoldingAtEndP3 != 0)
            || (_charStatus.PlayerNumber == 4 && itemHoldingAtEndP4 != 0))
		   || (gameObject.tag == "Enemy" && Random.value > 0.5f
		    && Manager_Game.ItemAppearRate != AmountRating.None))
			HoldItemAtStart();
	}

	void Update ()
	{
		if(!anim.IsInTransition(0))
		{
			// I only have one Pickup state but I used a tag for Pickup
			// just in case you add more pickup states. Here we simply
			// just rotate and move towards our item we are trying to pick up.
			if(_charStatus.BaseStateInfo.IsTag("Pickup"))
			{
				if(itemTryingToPickUp != null && grabMountItem.childCount == 0)
				{
					if(!grabMountItem.GetComponent<Collider>().enabled)
					{
						float dist = Vector3.Distance(transform.position, itemTryingToPickUp.position) - 0.5f;
						Vector3 dir = (itemTryingToPickUp.position - transform.position);
						dist = Mathf.Clamp(dist, 0, 1);
						float amount = 30;
						GetComponent<Rigidbody>().AddForce(new Vector3((dir.normalized.x * amount) * dist, 0, (dir.normalized.z * amount) * dist));
						_charMotor.ManualRotate(dir, false);
					}
				}
			}
		}
		else // Not in a transition.
		{
			// If we are going into a state where we aren't busy such as
			// idle in this case.
			if(_charStatus.BaseNextStateInfo.IsTag("NotBusy"))
			{
				// If we managed to grab an item and are not setup to hold it
				// yet (HoldStage == 0), we will do so here.
				if(ItemHolding != null && anim.GetInteger("HoldStage") == 0)
				{
					Base_Item baseItem = ItemHolding.GetComponent<Base_Item>();
					// Check which type of item we have.
					if(baseItem.ItemType == Base_Item.ItemTypes.Collectable)
					{
						// Collectables are just that, they get "collected"
						// and used right away.
						ItemCollectable itemCollectable = ItemHolding.GetComponent<ItemCollectable>();
						UseCollectable(itemCollectable.myType, itemCollectable.MaxValue, ItemHolding.GetComponent<AudioSource>().clip);
						Destroy(ItemHolding.gameObject);
						// The item has been destroyed so we aren't holding
						// it anymore.
						ItemHolding = null;
					}
					else
					{
						// I have throwable items setting HoldStage to 1, and
						// weapons setting it to 3. 1 sets the layer for the
						// hand to hold the item. Check the Animator layer
						// "Hold_Item" to see that.
						if(baseItem.ItemType == Base_Item.ItemTypes.Throwable)
							anim.SetInteger("HoldStage", 1);
						else if(baseItem.ItemType == Base_Item.ItemTypes.Weapon)
							anim.SetInteger("HoldStage", 3);
						// Only players have the item show its health/condition
						// info on screen.
						if(!IsEnemy)
						{
							itemIconImage.sprite = baseItem.icon;
                            itemHealthImage.fillAmount = (float)(baseItem.Condition * 0.01f);
							// Find this in the Manager_UI "Canvas_Overlay"
							// gameObject to see where this is. It is under
							// P1_UI -> Item_Group
                            _itemGroup.SetActive(true);
						}
					}
				}
				// Once we are over 80% of our pickup animation, we will
				// reset our itemTryingtoPickUp because it we grabbed something
				// it would have been done already.
				if(_charStatus.BaseStateInfo.IsTag("Pickup")
				   && _charStatus.BaseStateInfo.normalizedTime > 0.8f)
				{
					if(itemTryingToPickUp)
					{
						itemTryingToPickUp = null; // Reset this
					}
				}
			}
		}
	}

	void OnAnimatorIK(int layerIndex)
	{
		// We only use IK when trying to pick an item up and we are in a
		// Pickup animation.
		if(itemTryingToPickUp == null || !_charStatus.BaseStateInfo.IsTag("Pickup"))
			return;
		
		// we set the weight so most of the look-turn is done with the head, not the body.
		anim.SetIKPositionWeight (AvatarIKGoal.RightHand, _handWeight);
		anim.SetIKPosition(AvatarIKGoal.RightHand, itemTryingToPickUp.position + new Vector3(0, 0.3f, 0));
	}
	// Here is where we set our hand weight to either 0 (stop using IK)
	// or 1 (start using IK)
	IEnumerator BlendHandWeight(bool start)
	{
		float time = 0;
		if(start)
		{
			while (time < _handBlendTime)
			{
				_handWeight = time / _handBlendTime;
				time += Time.deltaTime;
				yield return 0;
			}
			_handWeight = 1;
		}
		else
		{
			while (_handWeight > 0)
			{
				_handWeight = time / _handBlendTime;
				time -= Time.deltaTime;
				yield return 0;
			}
			_handWeight = 0;
		}
		StopCoroutine ("BlendHandWeight");
	}
	// This gets called if we had an item in the previous scene and are now
	// needing to carry it into the current one. This is also used for
	// enemies to make them come in holding a random item (not a collectable).
	void HoldItemAtStart()
	{
		int itemHolding = 0;
		if(!IsEnemy)
		{
			if(_charStatus.PlayerNumber == 1)
				itemHolding = itemHoldingAtEndP1;
			else if(_charStatus.PlayerNumber == 2)
				itemHolding = itemHoldingAtEndP2;
            else if(_charStatus.PlayerNumber == 3)
                itemHolding = itemHoldingAtEndP3;
            else if(_charStatus.PlayerNumber == 4)
                itemHolding = itemHoldingAtEndP4;
		}
		else
		{
			itemHolding = Random.Range(1, Manager_Game.instance.AllCarryableItems.Count + 1);
		}
		// I have all of the items in the Manager_Game's AllCarryableItems list.
		// They are ordered in a way I wanted to match with my itemHoldingAtEnd.
		// If itemHoldingAtEnd = 1: throwable rock, 2: throwable knife, 3: sword,
		// 4: pole
		ItemHolding = Instantiate(Manager_Game.instance.AllCarryableItems[itemHolding - 1], grabMountItem.position, Quaternion.identity) as GameObject;
		Base_Item baseItem = ItemHolding.GetComponent<Base_Item>();
		Transform grabbedMount = grabMountItem;
		// Determine if it is a weapon, if so, we assign it to our weapon mount
		// instead.
		if(baseItem.ItemType == Base_Item.ItemTypes.Weapon)
			grabbedMount = grabMountWeapon;
		// Tell the item it was grabbed so it can be held properly. This is
		// the same way as picking it up normally.
		baseItem.WasGrabbed (grabbedMount);
		if(!IsEnemy)
		{
			// Setup info for the item if we are a player.
			itemIconImage.sprite = baseItem.icon;
            int pNum = _charStatus.PlayerNumber;
            itemHealthImage.fillAmount = pNum == 1 ? Manager_Game.itemHealthAmountP1 : pNum == 2 ? Manager_Game.itemHealthAmountP2 : pNum == 3 ? Manager_Game.itemHealthAmountP3 : Manager_Game.itemHealthAmountP4;
            _itemGroup.SetActive(true);
            baseItem.Condition = Mathf.RoundToInt(100 * (float)itemHealthImage.fillAmount);
		}
		// I explain this more above in Update().
		if(baseItem.ItemType == Base_Item.ItemTypes.Throwable)
			anim.SetInteger("HoldStage", 1);
		else if(baseItem.ItemType == Base_Item.ItemTypes.Weapon)
			anim.SetInteger("HoldStage", 3);
	}
	// Here we check to see which collectable we have obtained since that is
	// when this is called.
	void UseCollectable(ItemCollectable.ItemCollectableTypes typeUsing, int maxValue, AudioClip sfxToPlay)
	{
		switch(typeUsing)
		{
		case ItemCollectable.ItemCollectableTypes.Heal:
			_charStatus.WasHealed(maxValue);
			break;
		}
		// Each collectable should have a audio clip.
		if(sfxToPlay != null)
			AudioSource.PlayClipAtPoint(sfxToPlay, myTransform.position);
		else Debug.LogWarning("Item's audio clip was null!");
	}
	// Assign our UI for our Item_Group.
	public void CreatedSetup(int myPlayNumber)
	{
		itemHealthImage = Manager_UI.instance.plItemHealthImageAmount [myPlayNumber - 1];
		itemIconImage = Manager_UI.instance.plItemIcon [myPlayNumber - 1];
        _itemGroup = itemHealthImage.transform.parent.parent.gameObject;
        if (!_itemGroup)
            Debug.LogWarning("ITEM GROUP NOT FOUND FOR " + gameObject.name);
	}
	// Drop item after holding guard button. This starts the animation.
	public void DropItem()
	{
		if(anim.GetFloat("Move") > 0.9f)
			return;
		anim.SetInteger ("HoldStage", 4);
	}
	// Item actually being dropped. Manual = 1 would be manually dropping it after
	// holding the guard button, calling the method above. Manual = 0 would be
	// dropping the item after being hit. The only different between the two is
	// the direction the item's rigidbody velocity ends up being.
	public void DroppedItem(int manualDrop)
	{
		ItemHolding.GetComponent<Base_Item> ().WasDropped (manualDrop);
		anim.SetInteger ("HoldStage", 0);
		ItemHolding = null;
		if(gameObject.tag == "Player")
            _itemGroup.SetActive (false);
	}
	// This is after using a weapon and it broke, we get this message.
	public void DroppedBrokenItem()
	{
		anim.SetInteger ("HoldStage", 0);
		ItemHolding = null;
		if(gameObject.tag == "Player")
            _itemGroup.SetActive (false);
	}

	// Here is where we attempt to pickup an item.
	public void Pickup()
	{
		// If there are no items in grab range or we are already
		// trying to pick up an item, then we get out of here.
		if(detectionRadius.inGrabRangeItems.Count == 0 || itemTryingToPickUp != null
		   || anim.GetFloat("Move") > 0.6f)
			return;

		// Find the closest one to us.
		Transform itemToGrab = detectionRadius.inGrabRangeItems [0];
		if(detectionRadius.inGrabRangeItems.Count > 1)
		{
			float shortestSoFar = 100;
			foreach(Transform item in detectionRadius.inGrabRangeItems)
			{
				if(item != null)
				{
					float dist = Vector3.Distance(myTransform.position, item.position);
					if(dist < shortestSoFar)
					{
						shortestSoFar = dist;
						itemToGrab = item;
					}
				}
			}
		}
		// If we found an item to pick up, then we will go into our pickup
		// state using the following trigger.
		if(itemToGrab)
		{
			anim.SetTrigger("ItemActionFired");
			itemTryingToPickUp = itemToGrab;
			// Start the IK for making our hand move towards the item.
			StartCoroutine(BlendHandWeight(true));
		}
	}
	// Throwing a throwable item. Only when on the ground.
	public void Throw()
	{
		if(!_charMotor.onGround)
			return;
		anim.SetInteger ("HoldStage", 2);
		anim.SetTrigger ("ItemActionFired");
		// We no longer have the item so disable its health display.
        if(_itemGroup)
            _itemGroup.SetActive (false);
	}
	// We have successfully grabbed an item. This is called from out CollisionHand
	// which calls WasGrabbed on the item, which calls this for us.
	public void GrabbedItem(Transform itemGrabbed)
	{
		// We are now holding this item.
		ItemHolding = itemGrabbed.gameObject;
		itemTryingToPickUp = null;
		// Our mounts are no longer needed.
		if(grabMountItem.GetComponent<Collider>().enabled)
			grabMountItem.GetComponent<Collider>().enabled = false;
		if(grabMountWeapon.GetComponent<Collider>().enabled)
			grabMountWeapon.GetComponent<Collider>().enabled = false;
		// Set IK hand weight back to 0
		StartCoroutine(BlendHandWeight(false));
	}
	// For weapons to take damage after each hit. This gets called to us
	// from the weapon itself so we can update its health bar on the UI.
	public void ItemTookDamage(int healthLeft)
	{
		if(IsEnemy)
			return;
        itemHealthImage.fillAmount = (float)(healthLeft * 0.01f);
	}
	// This gets called after we get hit.
	public void ResetParameters()
	{
		grabMountItem.GetComponent<Collider>().enabled = false;
		grabMountWeapon.GetComponent<Collider>().enabled = false;
		itemTryingToPickUp = null;
		if(ItemHolding != null)
			DroppedItem(0); // Drop the item we are holding.
	}
	// Activate our grab mount's trigger so we can pick up an item with it.
	// This gets called during the pick up animation itself through an
	// animation event.
	public void AnimEventActivatingHandTrigger(int activate)
	{
		if(activate == 1 && itemTryingToPickUp != null)
		{
			if(itemTryingToPickUp.GetComponent<Base_Item>().ItemType != Base_Item.ItemTypes.Weapon)
				grabMountItem.GetComponent<Collider>().enabled = true;
			else grabMountWeapon.GetComponent<Collider>().enabled = true;
		}
		else
		{
			if(grabMountItem.GetComponent<Collider>().enabled)
				grabMountItem.GetComponent<Collider>().enabled = false;
			if(grabMountWeapon.GetComponent<Collider>().enabled)
				grabMountWeapon.GetComponent<Collider>().enabled = false;
		}
	}
	// This also uses an animation event. This time during the throw animation
	// for telling the item it was thrown.
	public void AnimEventUseThrowable()
	{
		if(ItemHolding != null)
		{
			ItemHolding.SendMessage("WasThrown", 7);
			ItemHolding = null;
			anim.SetInteger("HoldStage", 0);
		}
	}
	// We setup the hitbox on the weapon we are using and activate it for use
	// here from another animation event. You can't use bool parameters in
	// animation events so that's why I keep using integers.
	public void AnimEventUseWeapon(int enableCol)
	{
		if(ItemHolding != null)
		{
			if(enableCol == 2)
			{
				HitboxProperties hitboxUsing = ItemHolding.GetComponent<HitboxProperties>();
				_charAttacking.myCurrentHitbox = hitboxUsing;
				if(_charAttacking.myCurrentHitbox) // Make sure we have a hitbox set.
				{
					// reset it
					_charAttacking.myCurrentHitbox.myTriggerCol.GetComponent<Collider>().enabled = false;
					_charAttacking.myCurrentHitbox.RemoveCharactersHit();
				}
				hitboxUsing.Anim = anim;
				hitboxUsing.CharAttacking = _charAttacking;
				hitboxUsing.CharStatus = _charStatus;
				hitboxUsing.Attacker = myTransform;
                if (_charStatus.BaseStateInfo.IsName("Atk_WeaponSwing_2")) // In second swing attack, it's the last hit so we add more of an effect.
                {
                    hitboxUsing.hitForce = hitboxUsing.defHitForce + 1.8f;
                    hitboxUsing.hitHeight = 2;
                }
                else
                {
                    hitboxUsing.hitForce = hitboxUsing.defHitForce;
                    hitboxUsing.hitHeight = 0;
                }
				_charAttacking.SetHitbox = true;
			}
			// Full strength when 2 is passed in.
			_charAttacking.myCurrentHitbox.fullStrength = enableCol == 2;
			// The collider on the item will only be enabled if enableCol > 0.
			ItemHolding.GetComponent<CapsuleCollider>().enabled = enableCol > 0;
			if(enableCol == 0)
				_charAttacking.SetHitbox = false;
		}
	}
}