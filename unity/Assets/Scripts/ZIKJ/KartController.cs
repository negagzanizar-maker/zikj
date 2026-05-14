using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    const float MaxSpeed = 240f * 0.05f;
    const float Accel = 380f * 0.05f;
    const float BrakeDecel = 540f * 0.05f;
    const float FrictionPerS = 0.6f;
    const float SteerRate = 3.2f;

    [Header("Identity")]
    public int kartIndex;
    public bool isPlayer;
    public Color kartColor = Color.white;

    [Header("Runtime State")]
    public float heading;
    public float speed;
    public float angVel;

    [HideInInspector] public float maxSpeedMul = 1f;
    [HideInInspector] public float steerMul = 1f;
    [HideInInspector] public float throttleInput;
    [HideInInspector] public float steerInput;

    Rigidbody rb;
    Renderer[] renderers;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        renderers = GetComponentsInChildren<Renderer>();
    }

    void FixedUpdate()
    {
        if (isPlayer) ReadPlayerInput();

        float dt = Time.fixedDeltaTime;
        float th = Mathf.Clamp(throttleInput, -1f, 1f);
        float st = Mathf.Clamp(steerInput, -1f, 1f);
        float maxV = MaxSpeed * Mathf.Max(0.1f, maxSpeedMul);

        if (th > 0f)
            speed = Mathf.Min(speed + Accel * th * dt, maxV);
        else if (th < 0f)
            speed = Mathf.Max(speed - BrakeDecel * -th * dt, 0f);

        speed *= Mathf.Pow(FrictionPerS, dt);
        speed = Mathf.Clamp(speed, 0f, maxV);

        float speedRatio = Mathf.Clamp01(speed / MaxSpeed);
        float steerEffect = SteerRate * Mathf.Max(speedRatio, 0.15f) * steerMul;
        angVel = st * steerEffect;
        heading += angVel * dt;

        Vector3 pos = rb.position;
        pos.x += Mathf.Cos(heading) * speed * dt;
        pos.z += Mathf.Sin(heading) * speed * dt;
        pos.y = 0f;

        if (WaypointCircuit.I != null)
            pos = WaypointCircuit.I.ClampToTrack(pos);

        rb.MovePosition(pos);
        if (speed > 0.05f)
        {
            rb.MoveRotation(Quaternion.LookRotation(
                new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading)),
                Vector3.up));
        }
    }

    void ReadPlayerInput()
    {
        float throttle = 0f;
        float steer = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) throttle += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) throttle -= 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) steer -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) steer += 1f;

        throttleInput = throttle;
        steerInput = steer;
    }

    public void Teleport(Vector3 position, float h)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        heading = h;
        speed = 0f;
        angVel = 0f;

        Quaternion rot = Quaternion.LookRotation(
            new Vector3(Mathf.Cos(h), 0f, Mathf.Sin(h)),
            Vector3.up);

        rb.position = position;
        rb.rotation = rot;
        transform.SetPositionAndRotation(position, rot);
    }

    public void NudgePosition(Vector3 offset)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.MovePosition(WaypointCircuit.I != null
            ? WaypointCircuit.I.ClampToTrack(rb.position + offset)
            : rb.position + offset);
    }

    public void StopImmediately()
    {
        speed = 0f;
        throttleInput = 0f;
        steerInput = 0f;
    }

    public void SetVisualColor(Color color)
    {
        renderers ??= GetComponentsInChildren<Renderer>();

        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            if (rend.material != null)
                rend.material.color = color;
        }
    }
}
