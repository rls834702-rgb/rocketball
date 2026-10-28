using UnityEngine;

public class BotBrain : InputProvider
{
    public CarController car;
    public Transform ball;
    public Vector3 attackGoal, ownGoal;
    public bool isChaser = true;
    public float skill = 0.85f; // 0..1

    float stuckTimer, reverseTimer, jumpCooldown;

    public override void Read(CarInput i)
    {
        i.Clear();
        if (car == null || ball == null || GameManager.I == null || !GameManager.I.CarsActive) return;
        jumpCooldown -= Time.deltaTime;

        Vector3 bp = ball.position, cp = car.transform.position;
        Vector3 toGoal = attackGoal - bp; toGoal.y = 0f; toGoal.Normalize();
        Vector3 target;
        bool behindBall = Vector3.Dot(cp - bp, toGoal) < 0f;
        if (isChaser)
            target = behindBall ? bp - toGoal * 2.2f : Vector3.Lerp(ownGoal, bp, 0.3f) + Vector3.right * Mathf.Sign(cp.x) * 6f;
        else
            target = Vector3.Lerp(ownGoal, bp, 0.35f);
        target.y = 0f;

        if (car.Grounded) GroundLogic(i, target, bp, cp);
        else AirLogic(i, bp);
    }

    void GroundLogic(CarInput i, Vector3 target, Vector3 bp, Vector3 cp)
    {
        Vector3 local = car.transform.InverseTransformPoint(target);
        float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        float dist = new Vector2(local.x, local.z).magnitude;

        i.steer = Mathf.Clamp(angle / 25f, -1f, 1f);
        i.throttle = (dist < 4f && car.Speed > 12f) ? 0f : 1f;
        i.drift = Mathf.Abs(angle) > 70f && car.Speed > 10f;
        i.boost = Mathf.Abs(angle) < 14f && dist > 14f && car.boost > 8f && Random.value < skill + 0.1f;

        // stuck handling
        if (car.Speed < 1.2f && i.throttle > 0f) stuckTimer += Time.deltaTime; else stuckTimer = 0f;
        if (stuckTimer > 1.5f) { reverseTimer = 1f; stuckTimer = 0f; }
        if (reverseTimer > 0f) { reverseTimer -= Time.deltaTime; i.throttle = -1f; i.steer = -i.steer; i.boost = false; }

        // jump for aerial / high balls
        Vector3 flat = bp - cp; float hd = new Vector2(flat.x, flat.z).magnitude;
        if (jumpCooldown <= 0f && bp.y > 2.6f && hd < 9f + 6f * skill)
        {
            i.jumpDown = i.jumpHeld = true; jumpCooldown = 1.2f;
        }
        else if (jumpCooldown <= 0f && hd < 5f && bp.y < 2.2f && car.Speed > 12f && Random.value < 0.02f)
        {
            i.jumpDown = true; jumpCooldown = 1.0f; // flip shot
        }
    }

    void AirLogic(CarInput i, Vector3 bp)
    {
        Vector3 toBall = bp - car.transform.position;
        bool wantFlip = toBall.magnitude < 9f && car.CanFlip && car.AirTime > 0.12f && jumpCooldown <= 0f;
        if (wantFlip)
        {
            i.jumpDown = true; i.pitch = -1f; jumpCooldown = 1.0f; // forward dodge
            return;
        }

        // aim nose at ball when it is above, otherwise level out
        Vector3 lu = car.transform.InverseTransformDirection(Vector3.up);
        i.airRoll = true;
        i.steer = Mathf.Clamp(lu.x * 3f, -1f, 1f);
        float wantPitch = 0f;
        if (toBall.y > 1.5f && toBall.magnitude < 14f)
        {
            Vector3 lb = car.transform.InverseTransformDirection(toBall.normalized);
            wantPitch = Mathf.Clamp(lb.y * 3f, -1f, 1f);
            i.airRoll = false; i.steer = Mathf.Clamp(lb.x * 3f, -1f, 1f);
        }
        else wantPitch = Mathf.Clamp(-lu.z * 3f, -1f, 1f);
        i.pitch = wantPitch;
        i.boost = toBall.y > 2f && toBall.magnitude < 16f && car.boost > 10f;
    }
}
