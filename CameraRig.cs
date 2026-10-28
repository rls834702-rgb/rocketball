using UnityEngine;

public class CameraRig : MonoBehaviour
{
    public static bool BallCam = true;
    public Transform car, ball;
    public float distance = 9f, height = 4.2f;

    Camera cam;
    Vector3 camDir = Vector3.forward;

    void Awake() { cam = GetComponent<Camera>(); }

    void LateUpdate()
    {
        if (car == null) return;
        float dt = Time.deltaTime;
        Vector3 cp = car.position;
        Vector3 dir = Vector3.ProjectOnPlane(car.forward, Vector3.up);
        if (BallCam && ball != null)
        {
            Vector3 d = ball.position - cp; d.y = 0f;
            if (d.sqrMagnitude > 4f) dir = d;
        }
        if (dir.sqrMagnitude < 0.01f) dir = camDir;
        camDir = Vector3.Slerp(camDir, dir.normalized, 1f - Mathf.Exp(-5f * dt));

        Vector3 anchor = cp + Vector3.up * 1.2f;
        Vector3 desired = cp - camDir * distance + Vector3.up * height;
        Vector3 off = desired - anchor;
        if (Physics.SphereCast(anchor, 0.4f, off.normalized, out RaycastHit hit, off.magnitude, GameBootstrap.GroundMask))
            desired = anchor + off.normalized * Mathf.Max(1.5f, hit.distance - 0.2f);

        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-9f * dt));

        Vector3 look = cp + Vector3.up * 1.0f;
        if (BallCam && ball != null) look = Vector3.Lerp(look, ball.position, 0.55f);
        else look += camDir * 8f;
        Quaternion rot = Quaternion.LookRotation(look - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-10f * dt));

        float speed = car.GetComponent<Rigidbody>().velocity.magnitude;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 70f + speed * 0.6f, 1f - Mathf.Exp(-4f * dt));
    }
}
