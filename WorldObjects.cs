using UnityEngine;

public class BallController : MonoBehaviour
{
    public Rigidbody rb { get; private set; }
    const float MaxSpeed = 60f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1f;
        rb.drag = 0.03f;
        rb.angularDrag = 0.3f;
        rb.maxAngularVelocity = 20f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void FixedUpdate()
    {
        if (rb.velocity.sqrMagnitude > MaxSpeed * MaxSpeed) rb.velocity = rb.velocity.normalized * MaxSpeed;
    }

    public void ResetBall()
    {
        rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        rb.position = new Vector3(0f, 1.3f, 0f);
        transform.position = rb.position;
    }
}

public class BoostPad : MonoBehaviour
{
    public float amount = 12f;
    public float respawn = 4f;
    public GameObject visual;
    float timer;

    void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f && visual != null) visual.SetActive(true);
        }
        if (visual != null && visual.activeSelf) visual.transform.Rotate(0f, 90f * Time.deltaTime, 0f);
    }

    void OnTriggerStay(Collider other)
    {
        if (timer > 0f) return;
        var car = other.GetComponentInParent<CarController>();
        if (car == null || car.boost >= CarController.MaxBoost) return;
        car.AddBoost(amount);
        timer = respawn;
        if (visual != null) visual.SetActive(false);
    }
}

public class GoalTrigger : MonoBehaviour
{
    public Team scoringTeam; // team that gets the point when the ball enters

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<BallController>() != null && GameManager.I != null)
            GameManager.I.OnGoal(scoringTeam);
    }
}
