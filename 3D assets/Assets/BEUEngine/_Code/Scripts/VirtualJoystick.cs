using UnityEngine;
using System.Collections;
using UnityEngine.UI;
//////////////////////////////////////////////////////////////
// Joystick.cs
// Penelope iPhone Tutorial
//
// Joystick creates a movable joystick (via GUITexture) that 
// handles touch input, taps, and phases. Dead zones can control
// where the joystick input gets picked up and can be normalized.
//
// Optionally, you can enable the touchPad property from the editor
// to treat this Joystick as a TouchPad. A TouchPad allows the finger
// to touch down at any point and it tracks the movement relatively 
// without moving the graphic
//////////////////////////////////////////////////////////////

/// <summary>
/// This script gets attached to a child gameObject of Manager_Mobile called
/// VirtualJoystick_P1 and the player's PlayerInputMobile script will access it if
/// Manager_Game's usingMobile bool is true.
/// Start the game with maximize on play selected or already maximized to have
/// the joystick in the proper position.  You can change the gameObject's (VirtualJoystick_P1) position
/// x and/or y before starting the game to change where it appears on screen.
/// </summary>
public class VirtualJoystick : MonoBehaviour
{
    // A simple class for bounding how far the GUITexture will move
    class Boundary
    {
        public Vector2 min = Vector2.zero;
        public Vector2 max = Vector2.zero;
    }
    static private float tapTimeDelta = 0.3f;               // Time allowed between taps

    public bool touchPad;                                   // Is this a TouchPad?
    public Rect touchZone;
    public Vector2 deadZone = Vector2.zero;                     // Control when position is output
    public bool normalize = false;                          // Normalize output after the dead-zone?
    public Vector2 posit;                                    // [-1, 1] in x,y
    public int tapCount;                                            // Current tap count

    int lastFingerId = -1;                              // Finger last used for this joystick
    float tapTimeWindow;                            // How much time there is left for a tap to occur
    Vector2 fingerDownPos;

    GUITexture ima_GUI;                              // Joystick graphic
    Rect defaultRect;                               // Default position / extents of the joystick graphic
    Boundary guiBoundary;            // Boundary for joystick graphic
    public Vector2 guiTouchOffset;                     // Offset to apply to touch input
    public Vector2 guiCenter;                          // Center of joystick

    void Start()
    {
        // Cache this component at startup instead of looking up every frame	
        ima_GUI = GetComponent<GUITexture>();
        guiBoundary = new Boundary();
        // Store the default rect for the gui, so we can snap back to it
        defaultRect = ima_GUI.pixelInset;

        defaultRect.x += transform.position.x * Screen.width;// + gui.pixelInset.x; // -  Screen.width * 0.5;
        defaultRect.y += transform.position.y * Screen.height;

        transform.position = new Vector3(0, 0, transform.position.z);

        if (touchPad)
        {
            // If a texture has been assigned, then use the rect ferom the gui as our touchZone
            if (ima_GUI.texture)
                touchZone = defaultRect;
        }
        else
        {
            // This is an offset for touch input to match with the top left
            // corner of the GUI
            guiTouchOffset.x = defaultRect.width * 0.25f;
            guiTouchOffset.y = defaultRect.height * 0.25f;

            // Cache the center of the GUI, since it doesn't change
            guiCenter.x = defaultRect.x + guiTouchOffset.x;
            guiCenter.y = defaultRect.y + guiTouchOffset.y;

            // Let's build the GUI boundary, so we can clamp joystick movement
            guiBoundary.min.x = defaultRect.x - guiTouchOffset.x;
            guiBoundary.max.x = defaultRect.x + guiTouchOffset.x;
            guiBoundary.min.y = defaultRect.y - guiTouchOffset.y;
            guiBoundary.max.y = defaultRect.y + guiTouchOffset.y;
        }
    }

    void Disable()
    {
        gameObject.SetActive(false);
    }

    void ResetJoystick()
    {
        // Release the finger control and set the joystick back to the default position
        ima_GUI.pixelInset = defaultRect;
        lastFingerId = -1;
        posit = Vector2.zero;
        fingerDownPos = Vector2.zero;

        if (touchPad)
        {
            Color guiColor = ima_GUI.color; guiColor.a = 0.1f;
            ima_GUI.color = guiColor;
        }
    }

    bool IsFingerDown()
    {
        return (lastFingerId != -1);
    }

    void Update()
    {
        int count = Input.touchCount;

        // Adjust the tap time window while it still available
        if (tapTimeWindow > 0)
            tapTimeWindow -= Time.deltaTime;
        else
            tapCount = 0;

        if (count == 0)
            ResetJoystick();
        else
        {
            for (int i = 0; i < count; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 guiTouchPos = touch.position - guiTouchOffset;
                
                var shouldLatchFinger = false;
                if (touchPad)
                {
                    if (touchZone.Contains(touch.position))
                        shouldLatchFinger = true;
                }
                else if (ima_GUI.HitTest(touch.position))
                {
                    shouldLatchFinger = true;
                }

                // Latch the finger if this is a new touch
                if (shouldLatchFinger && (lastFingerId == -1 || lastFingerId != touch.fingerId))
                {

                    if (touchPad)
                    {
                        Color guiColor = ima_GUI.color; guiColor.a = 0.3f;
                        ima_GUI.color = guiColor;

                        lastFingerId = touch.fingerId;
                        fingerDownPos = touch.position;
                    }

                    lastFingerId = touch.fingerId;

                    // Accumulate taps if it is within the time window
                    if (tapTimeWindow > 0)
                        tapCount++;
                    else
                    {
                        tapCount = 1;
                        tapTimeWindow = tapTimeDelta;
                    }
                }

                if (lastFingerId == touch.fingerId)
                {
                    // Override the tap count with what the iPhone SDK reports if it is greater
                    // This is a workaround, since the iPhone SDK does not currently track taps
                    // for multiple touches
                    if (touch.tapCount > tapCount)
                        tapCount = touch.tapCount;

                    if (touchPad)
                    {
                        // For a touchpad, let's just set the position directly based on distance from initial touchdown
                        posit.x = Mathf.Clamp((touch.position.x - fingerDownPos.x) / (touchZone.width / 2), -1, 1);
                        posit.y = Mathf.Clamp((touch.position.y - fingerDownPos.y) / (touchZone.height / 2), -1, 1);
                    }
                    else
                    {
                        Rect pixInset = ima_GUI.pixelInset;
                        // Change the location of the joystick graphic to match where the touch is
                        pixInset.x = Mathf.Clamp(guiTouchPos.x, guiBoundary.min.x, guiBoundary.max.x);
                        pixInset.y = Mathf.Clamp(guiTouchPos.y, guiBoundary.min.y, guiBoundary.max.y);
                        ima_GUI.pixelInset = pixInset;
                    }

                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        ResetJoystick();
                }
            }
        }

        if (!touchPad)
        {
            // Get a value between -1 and 1 based on the joystick graphic location
            posit.x = (ima_GUI.pixelInset.x + guiTouchOffset.x - guiCenter.x) / guiTouchOffset.x;
            posit.y = (ima_GUI.pixelInset.y + guiTouchOffset.y - guiCenter.y) / guiTouchOffset.y;
        }

        // Adjust for dead zone	
        var absoluteX = Mathf.Abs(posit.x);
        var absoluteY = Mathf.Abs(posit.y);

        if (absoluteX < deadZone.x)
        {
            // Report the joystick as being at the center if it is within the dead zone
            posit.x = 0;
        }
        else if (normalize)
        {
            // Rescale the output after taking the dead zone into account
            posit.x = Mathf.Sign(posit.x) * (absoluteX - deadZone.x) / (1 - deadZone.x);
        }

        if (absoluteY < deadZone.y)
        {
            // Report the joystick as being at the center if it is within the dead zone
            posit.y = 0;
        }
        else if (normalize)
        {
            // Rescale the output after taking the dead zone into account
            posit.y = Mathf.Sign(posit.y) * (absoluteY - deadZone.y) / (1 - deadZone.y);
        }
    }
}
