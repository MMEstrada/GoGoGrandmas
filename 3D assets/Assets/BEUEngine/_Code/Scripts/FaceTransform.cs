using UnityEngine;
using System.Collections;
/// <summary>
/// Face transform. Used to make things like target cursors, face a transform
/// and follow a target. Rotation can't be done when parented to the character, so
/// that's why you need to do it this way. The target cursor would always have the
/// character's rotation if it is a child of it.
/// You provide the character to follow using and provide an offset from their
/// position.
/// </summary>
public class FaceTransform : MonoBehaviour
{
	public Transform transformToFace;
	public Transform transformToFollow;
	// If you have this start out as a child of the character, then detach it.
	public bool detach = true;

    float [,] targetOffsets;
    int _myPlayNum;
	Vector3 startLocalPos;
    CharacterStatus enemyStatus;

    Vector3 offset;

	void Start()
	{
        targetOffsets = Manager_Targeting.instance.targetCursorXOffsets;
		if(transform.parent != null)
		{
			startLocalPos = transform.localPosition;
			offset = startLocalPos;
		}
		if(detach)
			transform.SetParent(null, false);
	}

	void Update ()
	{
		if(transformToFace) // This is always the camera for this game.
		{
			transform.eulerAngles = new Vector3(transform.eulerAngles.x, transformToFace.eulerAngles.y, transform.eulerAngles.z);
		}
		if(transformToFollow)
		{
            if (enemyStatus)
                offset.x = targetOffsets[enemyStatus.TargetedByCharacters.Count - 1, _myPlayNum - 1];
			transform.position = transformToFollow.position + offset;
		}
	}

    public void CreatedSetup(int pNum, float heightOffset, CharacterStatus enStatus)
    {
        _myPlayNum = pNum;
        offset.y = heightOffset;
        enemyStatus = enStatus;
    }
}