using UnityEngine;

public enum Team { Blue = 0, Orange = 1 }

/// <summary>Input state for one car (filled by touch controls or by a bot).</summary>
public class CarInput
{
    public float throttle;   // -1..1 (ground)
    public float steer;      // -1..1 (ground steer / air yaw / air roll)
    public float pitch;      // -1..1, +1 = nose up (stick pulled back)
    public bool jumpDown;    // pressed this frame
    public bool jumpHeld;
    public bool boost;
    public bool airRoll;     // hold: steer axis becomes roll
    public bool drift;       // handbrake / powerslide

    public void Clear()
    {
        throttle = steer = pitch = 0f;
        jumpDown = jumpHeld = boost = airRoll = drift = false;
    }
}

public abstract class InputProvider : MonoBehaviour
{
    public abstract void Read(CarInput input);
}
