using UnityEngine;
using System.Collections;
/// <summary>
/// Cloud movement. Simply used for side-scrolling clouds. I use this for my clouds in my demo scenes.
/// </summary>
public class CloudMovement : MonoBehaviour
{
    public Vector2 minAndMaxX; // When the clouds's x position gets below the min X ('x' value here), it will start at the max, which will be the 'y' value here.
    public float speed;

    Vector2 _myPos;

    void Update ()
    {
        _myPos = transform.position;
        if (_myPos.x < minAndMaxX.x)
        {
            _myPos.x = minAndMaxX.y;
            transform.position = _myPos;
        }
        else
            transform.Translate(-Vector3.right * speed * Time.deltaTime);
	}
}