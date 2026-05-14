using UnityEngine;
using UnityEngine.InputSystem;

// Arcade kart physics matching browser engine.js (1 canvas px = 0.05 Unity units)
[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    const float MaxSpeed     = 240 * 0.05f;  // 12 u/s
    const float Accel        = 380 * 0.05f;  // 19 u/s²
    const float BrakeDecel   = 540 * 0.05f;  // 27 u/s²
    const float FrictionPerS = 0.6f;          // speed *= pow(0.6, dt) each frame
    const float SteerRate    = 3.2f;          // rad/s at full speed

    [Header("Identity")]
    public int   kartIndex;
    public bool  isPlayer;
    public Color kartColor = Color.white;

    [Header("Runtime State  (read-only in Inspector)")]
    public float heading;       // radians, 0 = facing +X
    public float speed;
    public float angVel;

    // Modifiers set by game mode (1 = normal)
    [HideInInspector] public float maxSpeedMul = 1f;
    [HideInInspector] public float steerMul    = 1f;

    // Input: written each FixedUpdate by player or KartAI
    [HideInInspector] public float throttleInput;
    [HideInInspector] public float steerInput;

    Rigidbody rb;

    void Awake()
    {
        rb             = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        if (isPlayer) ReadPlayerInput();

        float dt = Time.fixedDeltaTime;
        float th = Mathf.Clamp(throttleInput, -1f, 1f);
        float st = Mathf.Clamp(steerInput,   -1f, 1f);
        float maxV = MaxSpeed * maxSpeedMul;

        // Acceleration / braking
        if (th > 0f)
            speed = Mathf.Min(speed + Accel * th * dt, maxV);
        else if (th < 0f)
            speed = Mathf.Max(speed + BrakeDecel * th * dt, 0f);

        // Exponential friction
        speed *= Mathf.Pow(FrictionPerS, dt);
        speed  = Mathf.Max(speed, 0f);

        // Steering (scales with speed so stopped karts don't spin)
        float steerEffect = SteerRate * Mathf.Max(speed / MaxSpeed, 0.15f) * steerMul;
        angVel   = st * steerEffect;
        heading += angVel * dt;

        // Integrate position
        Vector3 pos = rb.position;
        pos.x += Mathf.Cos(heading) * speed * dt;
        pos.z += Mathf.Sin(heading) * speed * dt;
        pos.y  = 0f;

        // Keep on track
        if (WaypointCircuit.I != null)
            pos = WaypointCircuit.I.ClampToTrack(pos);

        rb.MovePosition(pos);
        if (speed > 0.05f)
            rb.MoveRotation(Quaternion.LookRotation(
                new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading)), Vector3.up));
    }

    void ReadPlayerInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        
        // WASD input for new Input System
        throttleInput = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        steerInput    = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
    }

    // Called by GameManager to place kart at spawn
    public void Teleport(Vector3 position, float h)
    {
        heading = h;
        speed   = 0f;
        rb.position = position;
        rb.rotation = Quaternion.LookRotation(
            new Vector3(Mathf.Cos(h), 0f, Mathf.Sin(h)), Vector3.up);
        transform.SetPositionAndRotation(position, rb.rotation);
    }

    // Called externally for soft overlap resolution
    public void NudgePosition(Vector3 offset)
    {
        rb.MovePosition(rb.position + offset);
    }
}
