using UnityEngine;

[DefaultExecutionOrder(-100)]
public class WaypointCircuit : MonoBehaviour
{
    // Track constants scaled from browser prototype (1 canvas px = 0.05 Unity units)
    const float S  = 0.05f;
    const float SL = 280  * S;   // 14f   straight left X
    const float SR = 1000 * S;   // 50f   straight right X
    const float TZ = -(200 * S); // -10f  top Z  (canvas Y flipped so +Z = screen-up)
    const float BZ = -(520 * S); // -26f  bottom Z
    const float CZ = -(360 * S); // -18f  arc center Z
    const float AR = 160  * S;   //  8f   arc radius

    public const float HalfWidth  = 70  * S;   // 3.5  track half-width (centeline ± this)
    public const float KartRadius  = 11  * S;   // 0.55 kart collision radius
    public const int   Count       = 80;

    public static WaypointCircuit I { get; private set; }
    public Vector3[] Points { get; private set; }

    void Awake()
    {
        I      = this;
        Points = Generate();
    }

    // ── Generation ──────────────────────────────────────────────────────────

    static Vector3[] Generate()
    {
        float top   = SR - SL;
        float arc   = Mathf.PI * AR;
        float total = 2 * top + 2 * arc;
        var pts = new Vector3[Count];
        for (int i = 0; i < Count; i++)
            pts[i] = At((float)i / Count, top, arc, total);
        return pts;
    }

    // Public static overload: usable before singleton is ready (e.g. TrackMeshBuilder)
    public static Vector3 At(float t)
    {
        float top = SR - SL; float arc = Mathf.PI * AR;
        return At(t, top, arc, 2 * top + 2 * arc);
    }

    static Vector3 At(float t, float top, float arc, float total)
    {
        float d = ((t % 1f) + 1f) % 1f * total;

        // 1. Top straight  SL → SR  (heading 0 = +X)
        if (d < top)
            return new Vector3(SL + d, 0f, TZ);
        d -= top;

        // 2. Right arc  top-right → bottom-right  (clockwise, angle π/2 → -π/2)
        if (d < arc)
        {
            float a = Mathf.PI * 0.5f - (d / arc) * Mathf.PI;
            return new Vector3(SR + AR * Mathf.Cos(a), 0f, CZ + AR * Mathf.Sin(a));
        }
        d -= arc;

        // 3. Bottom straight  SR → SL
        if (d < top)
            return new Vector3(SR - d, 0f, BZ);
        d -= top;

        // 4. Left arc  bottom-left → top-left  (clockwise, angle -π/2 → -3π/2)
        {
            float a = -Mathf.PI * 0.5f - (d / arc) * Mathf.PI;
            return new Vector3(SL + AR * Mathf.Cos(a), 0f, CZ + AR * Mathf.Sin(a));
        }
    }

    // ── Utilities ────────────────────────────────────────────────────────────

    public Vector3 SpawnPosition(int index, int total)
    {
        float t      = 0.02f + (float)index / total * 0.14f;
        Vector3 c    = At(t);
        float  side  = (index % 2 == 0) ? -HalfWidth * 0.35f : HalfWidth * 0.35f;
        // On the top straight the perpendicular direction is ±Z
        return c + new Vector3(0f, 0f, side);
    }

    public float SpawnHeading() => 0f;   // top straight faces +X → heading 0

    public int Nearest(Vector3 pos)
    {
        int   best   = 0;
        float bestSq = float.MaxValue;
        for (int i = 0; i < Count; i++)
        {
            float sq = (Points[i] - pos).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = i; }
        }
        return best;
    }

    public int Advance(int idx) => (idx + 1) % Count;

    public Vector3 ClampToTrack(Vector3 pos)
    {
        int     ni   = Nearest(pos);
        Vector3 c    = Points[ni];
        float   dx   = pos.x - c.x;
        float   dz   = pos.z - c.z;
        float   dist = Mathf.Sqrt(dx * dx + dz * dz);
        if (dist > HalfWidth && dist > 0.001f)
        {
            pos.x = c.x + (dx / dist) * HalfWidth;
            pos.z = c.z + (dz / dist) * HalfWidth;
        }
        pos.y = 0f;
        return pos;
    }

    // ── Editor gizmos ────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (Points == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < Count; i++)
            Gizmos.DrawLine(Points[i], Points[(i + 1) % Count]);
    }
}
