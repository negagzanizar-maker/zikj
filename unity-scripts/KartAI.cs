using UnityEngine;

// AI brain: follows waypoints, switches to chase/flee when InfectedMode instructs it.
[RequireComponent(typeof(KartController))]
public class KartAI : MonoBehaviour
{
    public enum Behaviour { FollowTrack, Chase, Flee }

    [HideInInspector] public Behaviour behaviour = Behaviour.FollowTrack;
    [HideInInspector] public Transform  target;    // for Chase / Flee

    KartController kart;
    int wpIdx;

    const float WP_REACH = 1.6f;    // metres to "collect" a waypoint

    void Awake() => kart = GetComponent<KartController>();

    void Start()
    {
        if (WaypointCircuit.I != null)
            wpIdx = WaypointCircuit.I.Nearest(transform.position);
    }

    void FixedUpdate()
    {
        if (kart.isPlayer) return;

        switch (behaviour)
        {
            case Behaviour.FollowTrack: FollowTrack(1f);  break;
            case Behaviour.Chase:       ChaseTarget();     break;
            case Behaviour.Flee:        FleeTarget();      break;
        }
    }

    // ── Behaviours ───────────────────────────────────────────────────────────

    void FollowTrack(float throttle)
    {
        var circuit = WaypointCircuit.I;
        if (circuit == null) return;

        Vector3 wp = circuit.Points[wpIdx];
        SteerToward(wp, throttle);

        if (Vector3.Distance(transform.position, wp) < WP_REACH)
            wpIdx = circuit.Advance(wpIdx);
    }

    void ChaseTarget()
    {
        if (target == null) { FollowTrack(1f); return; }
        float dist     = Vector3.Distance(transform.position, target.position);
        float throttle = dist < 2f ? 0.5f : 1f;    // ease off when very close
        SteerToward(target.position, throttle);
    }

    void FleeTarget()
    {
        if (target == null) { FollowTrack(0.9f); return; }
        // Run away: steer opposite to target, but still roughly follow track
        Vector3 awayDir = (transform.position - target.position).normalized;
        SteerToward(transform.position + awayDir * 5f, 1f);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    void SteerToward(Vector3 dest, float throttle)
    {
        float dx            = dest.x - transform.position.x;
        float dz            = dest.z - transform.position.z;
        float targetHeading = Mathf.Atan2(dz, dx);  // same convention as KartController.heading

        // DeltaAngle gives signed shortest rotation [-180, 180], normalised to [-1, 1]
        float diff = Mathf.DeltaAngle(kart.heading * Mathf.Rad2Deg,
                                      targetHeading * Mathf.Rad2Deg) / 180f;

        kart.throttleInput = throttle;
        kart.steerInput    = Mathf.Clamp(diff * 3f, -1f, 1f);
    }
}
