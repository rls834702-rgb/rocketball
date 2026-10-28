using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    public Team team;
    public InputProvider provider;
    public GameObject flame;

    // --- tuning ---
    public float gravity = 18f;
    public float driveAccel = 26f;
    public float brakeAccel = 40f;
    public float maxSpeed = 24f;
    public float boostSpeed = 36f;
    public float boostAccel = 32f;
    public float maxTurnRate = 2.6f;
    public float jumpImpulse = 8.5f;
    public float dodgeImpulse = 9f;
    public float dodgeSpin = 14f;
    public float pitchRate = 4.5f, yawRate = 3.2f, rollRate = 6f;

    public float boost = 33f;
    public const float MaxBoost = 100f;

    public Rigidbody rb { get; private set; }
    public CarInput input { get; private set; } = new CarInput();
    public bool Grounded { get; private set; }
    public bool CanFlip => canSecondJump && !Grounded && airTime < 1.5f;
    public float AirTime => airTime;
    public float Speed => rb.velocity.magnitude;

    Vector3 groundNormal = Vector3.up;
    bool canSecondJump = true;
    bool jumpQueued;
    float airTime, jumpHold, dodgeTimer, ignoreGround, touchTimer;
    static readonly Vector3[] wheelPos =
    {
        new Vector3( 0.7f, 0f,  1.1f), new Vector3(-0.7f, 0f,  1.1f),
        new Vector3( 0.7f, 0f, -1.1f), new Vector3(-0.7f, 0f, -1.1f)
    };

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 4f;
        rb.drag = 0.05f;
        rb.angularDrag = 0.2f;
        rb.maxAngularVelocity = 16f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void AddBoost(float amount) { boost = Mathf.Min(MaxBoost, boost + amount); }

    public void ResetTo(Vector3 pos, Quaternion rot)
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = pos; rb.rotation = rot;
        transform.SetPositionAndRotation(pos, rot);
        boost = 33f; canSecondJump = true; airTime = 0; dodgeTimer = 0; jumpHold = 0;
    }

    void Update()
    {
        input.jumpDown = false;
        if (provider != null) provider.Read(input);
        if (GameManager.I != null && GameManager.I.state == GameManager.State.Countdown) input.Clear();
        if (input.jumpDown) jumpQueued = true;
        if (flame != null)
        {
            bool on = input.boost && boost > 0.1f;
            flame.SetActive(on);
            if (on) flame.transform.localScale = new Vector3(0.5f, 0.4f, 1.2f + Random.value * 0.8f);
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        touchTimer -= dt; ignoreGround -= dt; dodgeTimer -= dt;
        CheckGround();

        if (Grounded) { airTime = 0f; canSecondJump = true; }
        else airTime += dt;

        HandleJump(dt);
        if (Grounded) DriveGround(dt); else Air(dt);
        HandleBoost(dt);
        jumpQueued = false;
    }

    void CheckGround()
    {
        int hits = 0; Vector3 n = Vector3.zero;
        if (ignoreGround <= 0f)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 o = transform.TransformPoint(wheelPos[i]);
                if (Physics.Raycast(o, -transform.up, out RaycastHit h, 0.7f, GameBootstrap.GroundMask))
                { hits++; n += h.normal; }
            }
        }
        Grounded = hits >= 2;
        groundNormal = Grounded ? (n / hits).normalized : Vector3.up;
    }

    void HandleJump(float dt)
    {
        if (jumpHold > 0f)
        {
            jumpHold -= dt;
            if (input.jumpHeld) rb.AddForce(transform.up * 22f, ForceMode.Acceleration);
        }
        if (!jumpQueued) return;

        if (Grounded)
        {
            rb.AddForce(transform.up * jumpImpulse, ForceMode.VelocityChange);
            jumpHold = 0.2f; ignoreGround = 0.15f; Grounded = false;
        }
        else if (touchTimer > 0f && Speed < 6f && Vector3.Dot(transform.up, Vector3.up) < 0.3f)
        {
            // upside down on the floor: hop and roll back onto the wheels
            rb.AddForce(Vector3.up * 8f, ForceMode.VelocityChange);
            rb.AddTorque(transform.forward * 8f, ForceMode.VelocityChange);
            ignoreGround = 0.2f;
        }
        else if (CanFlip)
        {
            canSecondJump = false;
            float sx = input.steer, sy = -input.pitch;
            if (new Vector2(sx, sy).magnitude > 0.35f)
            {
                Vector3 f = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 r = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
                Vector3 dir = (f * sy + r * sx).normalized;
                rb.AddForce(dir * dodgeImpulse, ForceMode.VelocityChange);
                rb.angularVelocity = Vector3.Cross(Vector3.up, dir) * dodgeSpin;
                dodgeTimer = 0.55f;
            }
            else
            {
                rb.AddForce(transform.up * jumpImpulse, ForceMode.VelocityChange);
            }
        }
    }

    void DriveGround(float dt)
    {
        Vector3 n = groundNormal;
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, n).normalized;
        Vector3 right = Vector3.Cross(n, fwd);
        Vector3 vel = rb.velocity;
        float fs = Vector3.Dot(vel, fwd), ss = Vector3.Dot(vel, right);

        float t = input.throttle;
        if (Mathf.Abs(t) > 0.05f)
        {
            bool braking = t * fs < -0.5f;
            float a = braking ? brakeAccel : driveAccel * (1f - Mathf.Clamp01(Mathf.Abs(fs) / maxSpeed));
            rb.AddForce(fwd * t * a, ForceMode.Acceleration);
        }
        else rb.AddForce(-fwd * fs * 0.5f, ForceMode.Acceleration);

        float grip = input.drift ? 1.8f : 14f;
        rb.AddForce(-right * ss * grip, ForceMode.Acceleration);

        float sf = Mathf.Clamp01(Mathf.Abs(fs) / 5f) * (1f - 0.45f * Mathf.Clamp01(Mathf.Abs(fs) / boostSpeed));
        float dir = fs >= -0.5f ? 1f : -1f;
        float target = input.steer * maxTurnRate * sf * dir * (input.drift ? 1.5f : 1f);
        Vector3 av = rb.angularVelocity;
        float avN = Vector3.Dot(av, n);
        rb.AddTorque(n * (target - avN) * 10f, ForceMode.Acceleration);
        rb.AddTorque(Vector3.Cross(transform.up, n) * 25f - (av - n * avN) * 5f, ForceMode.Acceleration);

        rb.AddForce(Vector3.down * gravity * 0.35f - n * gravity * 0.9f, ForceMode.Acceleration);
    }

    void Air(float dt)
    {
        rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        if (dodgeTimer > 0f) return;

        Vector3 target = transform.right * (-input.pitch * pitchRate);
        if (input.airRoll) target += -transform.forward * (input.steer * rollRate);
        else target += transform.up * (input.steer * yawRate);

        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, target, 1f - Mathf.Exp(-8f * dt));
    }

    void HandleBoost(float dt)
    {
        if (!input.boost || boost <= 0f) return;
        boost = Mathf.Max(0f, boost - 33f * dt);
        Vector3 f = transform.forward;
        float k = Mathf.Clamp01(1f - Vector3.Dot(rb.velocity, f) / boostSpeed);
        rb.AddForce(f * boostAccel * k, ForceMode.Acceleration);
    }

    void OnCollisionStay(Collision c) { if (c.gameObject.layer == GameBootstrap.ArenaLayer) touchTimer = 0.2f; }

    void OnCollisionEnter(Collision c)
    {
        var ball = c.rigidbody;
        if (ball == null || ball.GetComponent<BallController>() == null) return;
        Vector3 n = (ball.position - transform.TransformPoint(0f, 0.2f, 0.2f)).normalized;
        float closing = Mathf.Max(0f, Vector3.Dot(c.relativeVelocity, n));
        float power = 0.35f + (dodgeTimer > 0f ? 0.25f : 0f);
        ball.AddForce(n * closing * power + transform.forward * Speed * 0.1f, ForceMode.VelocityChange);
    }
}
