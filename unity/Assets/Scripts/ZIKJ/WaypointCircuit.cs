using UnityEngine;

[DefaultExecutionOrder(-100)]
public class WaypointCircuit : MonoBehaviour
{
    // Track constants scaled from the browser prototype.
    public const float Scale = 0.05f;
    public const float StraightLeft = 280 * Scale;
    public const float StraightRight = 1000 * Scale;
    public const float TopZ = -(200 * Scale);
    public const float BottomZ = -(520 * Scale);
    public const float ArcCenterZ = -(360 * Scale);
    public const float ArcRadius = 160 * Scale;
    public const float HalfWidth = 70 * Scale;
    public const float KartRadius = 11 * Scale;
    public const int Count = 80;

    const float InnerRadius = ArcRadius - HalfWidth;
    const float OuterRadius = ArcRadius + HalfWidth;

    public static WaypointCircuit I { get; private set; }
    public Vector3[] Points { get; private set; }

    void Awake()
    {
        I = this;
        Points = Generate();
    }

    static Vector3[] Generate()
    {
        float straight = StraightRight - StraightLeft;
        float arc = Mathf.PI * ArcRadius;
        float total = 2f * straight + 2f * arc;
        var pts = new Vector3[Count];

        for (int i = 0; i < Count; i++)
            pts[i] = At((float)i / Count, straight, arc, total);

        return pts;
    }

    public static Vector3 At(float t)
    {
        float straight = StraightRight - StraightLeft;
        float arc = Mathf.PI * ArcRadius;
        return At(t, straight, arc, 2f * straight + 2f * arc);
    }

    static Vector3 At(float t, float straight, float arc, float total)
    {
        float d = Mathf.Repeat(t, 1f) * total;

        if (d < straight)
            return new Vector3(StraightLeft + d, 0f, TopZ);
        d -= straight;

        if (d < arc)
        {
            float a = Mathf.PI * 0.5f - (d / arc) * Mathf.PI;
            return new Vector3(
                StraightRight + ArcRadius * Mathf.Cos(a),
                0f,
                ArcCenterZ + ArcRadius * Mathf.Sin(a));
        }
        d -= arc;

        if (d < straight)
            return new Vector3(StraightRight - d, 0f, BottomZ);
        d -= straight;

        {
            float a = -Mathf.PI * 0.5f - (d / arc) * Mathf.PI;
            return new Vector3(
                StraightLeft + ArcRadius * Mathf.Cos(a),
                0f,
                ArcCenterZ + ArcRadius * Mathf.Sin(a));
        }
    }

    public Vector3 SpawnPosition(int index, int total)
    {
        total = Mathf.Max(1, total);
        float t = 0.02f + (float)index / total * 0.14f;
        Vector3 center = At(t);
        float side = (index % 2 == 0) ? -HalfWidth * 0.35f : HalfWidth * 0.35f;
        return center + new Vector3(0f, 0f, side);
    }

    public float SpawnHeading() => 0f;

    public int Nearest(Vector3 pos)
    {
        EnsurePoints();

        int best = 0;
        float bestSq = float.MaxValue;

        for (int i = 0; i < Count; i++)
        {
            float sq = (Points[i] - pos).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = i;
            }
        }

        return best;
    }

    public int Advance(int idx) => (idx + 1) % Count;

    public Vector3 ClampToTrack(Vector3 pos)
    {
        float safeHalfWidth = HalfWidth - KartRadius;

        if (pos.x >= StraightLeft && pos.x <= StraightRight)
        {
            float centerZ = pos.z > ArcCenterZ ? TopZ : BottomZ;
            pos.z = Mathf.Clamp(pos.z, centerZ - safeHalfWidth, centerZ + safeHalfWidth);
            pos.y = 0f;
            return pos;
        }

        float centerX = pos.x > StraightRight ? StraightRight : StraightLeft;
        float dx = pos.x - centerX;
        float dz = pos.z - ArcCenterZ;
        float dist = Mathf.Sqrt(dx * dx + dz * dz);

        if (dist < 0.001f)
        {
            dx = pos.x > StraightRight ? 1f : -1f;
            dz = 0f;
            dist = 1f;
        }

        float clamped = Mathf.Clamp(dist, InnerRadius + KartRadius, OuterRadius - KartRadius);
        pos.x = centerX + (dx / dist) * clamped;
        pos.z = ArcCenterZ + (dz / dist) * clamped;
        pos.y = 0f;
        return pos;
    }

    void EnsurePoints()
    {
        Points ??= Generate();
    }

    void OnDrawGizmos()
    {
        EnsurePoints();
        Gizmos.color = Color.yellow;

        for (int i = 0; i < Count; i++)
            Gizmos.DrawLine(Points[i], Points[(i + 1) % Count]);
    }
}
