using UnityEngine;

[RequireComponent(typeof(KartController))]
public class KartAI : MonoBehaviour
{
    public enum Behaviour { FollowTrack, Chase, Flee }

    [HideInInspector] public Behaviour behaviour = Behaviour.FollowTrack;
    [HideInInspector] public Transform target;

    const float WaypointReach = 1.6f;

    KartController kart;
    int wpIdx;

    void Awake()
    {
        kart = GetComponent<KartController>();
    }

    void Start()
    {
        if (WaypointCircuit.I != null)
            wpIdx = WaypointCircuit.I.Nearest(transform.position);
    }

    void FixedUpdate()
    {
        if (kart == null || kart.isPlayer) return;

        switch (behaviour)
        {
            case Behaviour.Chase:
                ChaseTarget();
                break;
            case Behaviour.Flee:
                FleeTarget();
                break;
            default:
                FollowTrack(1f);
                break;
        }
    }

    void FollowTrack(float throttle)
    {
        var circuit = WaypointCircuit.I;
        if (circuit == null || circuit.Points == null || circuit.Points.Length == 0) return;

        Vector3 wp = circuit.Points[wpIdx];
        SteerToward(wp, throttle);

        if (Vector3.Distance(transform.position, wp) < WaypointReach)
            wpIdx = circuit.Advance(wpIdx);
    }

    void ChaseTarget()
    {
        if (target == null)
        {
            FollowTrack(1f);
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);
        SteerToward(target.position, dist < 2f ? 0.55f : 1f);
    }

    void FleeTarget()
    {
        if (target == null)
        {
            FollowTrack(0.9f);
            return;
        }

        Vector3 awayDir = transform.position - target.position;
        awayDir.y = 0f;
        if (awayDir.sqrMagnitude < 0.001f)
            awayDir = transform.right;

        SteerToward(transform.position + awayDir.normalized * 5f, 1f);
    }

    void SteerToward(Vector3 dest, float throttle)
    {
        float dx = dest.x - transform.position.x;
        float dz = dest.z - transform.position.z;
        float targetHeading = Mathf.Atan2(dz, dx);
        float diff = Mathf.DeltaAngle(kart.heading * Mathf.Rad2Deg, targetHeading * Mathf.Rad2Deg) / 180f;

        kart.throttleInput = Mathf.Clamp01(throttle);
        kart.steerInput = Mathf.Clamp(diff * 3f, -1f, 1f);
    }
}
