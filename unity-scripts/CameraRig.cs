using UnityEngine;

// Orthographic overhead camera sized to show the full track.
// Attach this to the Main Camera GameObject.
[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    // Track center in Unity world units (matches WaypointCircuit constants)
    //   X center = (SL + SR) / 2 = (14 + 50) / 2 = 32
    //   Z center = CZ           = -(360 * 0.05) = -18
    [Header("Position")]
    public float centerX   = 32f;
    public float centerZ   = -18f;
    public float height    = 30f;

    // orthoSize = half the vertical world-space height shown.
    // Track is 36 unity units tall → size 20 gives padding on each side.
    [Header("Projection")]
    public float orthoSize = 20f;

    Camera cam;

    void Awake()
    {
        cam                  = GetComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = orthoSize;
        cam.nearClipPlane    = 0.1f;
        cam.farClipPlane     = height + 10f;

        // Look straight down: camera forward = -Y, camera up = -Z
        // (so +Z = bottom of screen, matching canvas Y-down convention)
        transform.SetPositionAndRotation(
            new Vector3(centerX, height, centerZ),
            Quaternion.LookRotation(Vector3.down, -Vector3.forward));
    }
}
