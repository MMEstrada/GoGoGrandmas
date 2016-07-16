using UnityEngine;
using System.Collections;
/// <summary>
/// Random scaling. Randomly change the scale on an object using a given range. Only takes place upon creation. Preferred
/// for scenery that doesn't stay in place, like clouds.
/// </summary>
public class RandomScaling : MonoBehaviour
{
    public Vector3 randomChangeRange;

	void Start ()
    {
        transform.localScale +=
         new Vector3(Random.Range(0, randomChangeRange.x), Random.Range(0, randomChangeRange.y), Random.Range(0, randomChangeRange.z));
	}
}