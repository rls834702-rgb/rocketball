using UnityEngine;

/// <summary>Mobile controls: floating stick on the left, buttons on the right. Keyboard works in the Editor/PC.</summary>
public class TouchControls : InputProvider
{
    class Btn
    {
        public string label; public Vector2 pos; public float r; public Color color;
        public bool held, prev; public int finger = -1;
    }

    Btn jump, boost, airRoll, drift, cam;
    Btn[] all;
    int stickFinger = -1;
    Vector2 stickOrigin, stickPos, stick;
    float u;
    GUIStyle style;
    Texture2D circle;

    void Awake()
    {
        circle = MakeCircle(128);
        jump = new Btn { label = "JUMP", color = new Color(0.2f, 0.8f, 0.3f) };
        boost = new Btn { label = "BOOST", color = new Color(1f, 0.6f, 0.1f) };
        airRoll = new Btn { label = "AIR\nROLL", color = new Color(0.3f, 0.6f, 1f) };
        drift = new Btn { label = "DRIFT", color = new Color(0.8f, 0.3f, 0.9f) };
        cam = new Btn { label = "CAM", color = new Color(0.7f, 0.7f, 0.7f) };
        all = new[] { jump, boost, airRoll, drift, cam };
    }

    static Texture2D MakeCircle(int s)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        float c = (s - 1) * 0.5f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1f - d) * c * 0.5f)));
            }
        t.Apply(); return t;
    }

    void Layout()
    {
        float W = Screen.width, H = Screen.height;
        u = H * 0.1f;
        jump.pos = new Vector2(W - 2.0f * u, H - 2.1f * u); jump.r = 1.35f * u;
        boost.pos = new Vector2(W - 4.9f * u, H - 1.5f * u); boost.r = 1.15f * u;
        airRoll.pos = new Vector2(W - 4.6f * u, H - 4.0f * u); airRoll.r = 1.0f * u;
        drift.pos = new Vector2(W - 1.7f * u, H - 4.6f * u); drift.r = 0.95f * u;
        cam.pos = new Vector2(W - 1.0f * u, 3.0f * u); cam.r = 0.65f * u;
    }

    public override void Read(CarInput i)
    {
        Layout();
        i.throttle = i.steer = i.pitch = 0f;
        i.jumpHeld = i.boost = i.airRoll = i.drift = false;

        if (Input.touchCount > 0 || stickFinger != -1) ReadTouch(i);
        else ReadKeyboard(i);
    }

    void ReadTouch(CarInput i)
    {
        float W = Screen.width, H = Screen.height;
        foreach (var b in all) { b.prev = b.held; b.held = false; }
        bool stickAlive = false;

        for (int k = 0; k < Input.touchCount; k++)
        {
            Touch t = Input.GetTouch(k);
            Vector2 p = new Vector2(t.position.x, H - t.position.y);
            bool ended = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;

            if (t.fingerId == stickFinger)
            {
                if (ended) continue;
                stickAlive = true; stickPos = p;
                continue;
            }
            if (ended) continue;

            Btn hit = null;
            foreach (var b in all) if ((p - b.pos).magnitude < b.r * 1.2f) { hit = b; break; }
            if (hit != null) { hit.held = true; continue; }

            if (stickFinger == -1 && t.phase == TouchPhase.Began && p.x < W * 0.5f)
            {
                stickFinger = t.fingerId; stickOrigin = stickPos = p; stickAlive = true;
            }
        }
        if (!stickAlive) { stickFinger = -1; stick = Vector2.zero; }
        else
        {
            Vector2 d = stickPos - stickOrigin;
            float R = 1.6f * u;
            stick = Vector2.ClampMagnitude(d / R, 1f);
            if (stick.magnitude < 0.12f) stick = Vector2.zero;
        }

        float sx = stick.x, sy = -stick.y; // screen y is down
        i.steer = sx; i.throttle = sy; i.pitch = -sy;
        i.jumpHeld = jump.held; i.jumpDown = jump.held && !jump.prev;
        i.boost = boost.held; i.airRoll = airRoll.held; i.drift = drift.held;
        if (cam.held && !cam.prev) CameraRig.BallCam = !CameraRig.BallCam;
    }

    void ReadKeyboard(CarInput i)
    {
        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        i.steer = h; i.throttle = v; i.pitch = -v;
        i.jumpHeld = Input.GetKey(KeyCode.Space);
        i.jumpDown = Input.GetKeyDown(KeyCode.Space);
        i.boost = Input.GetKey(KeyCode.LeftShift) || Input.GetMouseButton(0);
        i.airRoll = Input.GetKey(KeyCode.E);
        i.drift = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl);
        if (Input.GetKeyDown(KeyCode.C)) CameraRig.BallCam = !CameraRig.BallCam;
    }

    void OnGUI()
    {
        if (circle == null || all == null) return;
        if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        Layout();
        style.fontSize = Mathf.RoundToInt(u * 0.38f);

        // stick
        Vector2 o = stickFinger != -1 ? stickOrigin : new Vector2(2.6f * u, Screen.height - 2.6f * u);
        Vector2 knob = o + stick * 1.6f * u * new Vector2(1, 1);
        knob = new Vector2(knob.x, o.y + stick.y * 1.6f * u);
        Draw(o, 1.8f * u, new Color(1, 1, 1, 0.18f));
        Draw(knob, 0.75f * u, new Color(1, 1, 1, stickFinger != -1 ? 0.55f : 0.3f));

        foreach (var b in all)
        {
            Color c = b.color; c.a = b.held ? 0.85f : 0.4f;
            Draw(b.pos, b.r, c);
            GUI.color = Color.white;
            GUI.Label(new Rect(b.pos.x - b.r, b.pos.y - b.r, b.r * 2, b.r * 2),
                      b == cam ? (CameraRig.BallCam ? "BALL" : "CAR") : b.label, style);
        }
    }

    void Draw(Vector2 c, float r, Color col)
    {
        GUI.color = col;
        GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2, r * 2), circle);
        GUI.color = Color.white;
    }
}
