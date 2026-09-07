using UnityEngine;

/// <summary>
/// Simple on-screen debug HUD for tracking system status.
/// Shows: latency, packets received, parse errors, FPS, tracked karts.
/// </summary>
public class TrackingDebugHUD : MonoBehaviour
{
    public static TrackingDebugHUD I { get; private set; }

    [Header("UI")]
    public bool showHUD = false;
    public float fontSize = 14f;
    public Vector2 hudPosition = new Vector2(10f, 10f);

    GUIStyle labelStyle;
    float frameRateUpdate;
    float currentFPS;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
    }

    void Update()
    {
        // Update FPS every 0.2 seconds
        frameRateUpdate -= Time.deltaTime;
        if (frameRateUpdate <= 0f)
        {
            currentFPS = 1f / Time.deltaTime;
            frameRateUpdate = 0.2f;
        }

        // Toggle HUD with T key
        if (Input.GetKeyDown(KeyCode.T))
            showHUD = !showHUD;
    }

    void OnGUI()
    {
        if (!showHUD || TrackingReceiver.I == null)
            return;

        EnsureStyles();

        GUILayout.BeginArea(new Rect(hudPosition.x, hudPosition.y, 300f, 300f));
        GUILayout.Label("=== TRACKING STATUS ===", labelStyle);

        var receiver = TrackingReceiver.I;
        GUILayout.Label($"FPS: {currentFPS:F1}", labelStyle);
        GUILayout.Label($"Latency: {receiver.frameLatencyMs:F1}ms", labelStyle);
        GUILayout.Label($"Packets: {receiver.packetsReceived}", labelStyle);
        GUILayout.Label($"Errors: {receiver.parseErrors}", labelStyle);

        if (TrackingInputManager.I != null)
        {
            GUILayout.Label($"Tracked Karts: {TrackingInputManager.I.GetTrackedKartCount()}", labelStyle);
            GUILayout.Label($"Interpolation: {(TrackingInputManager.I.useInterpolation ? "ON" : "OFF")}", labelStyle);
        }

        GUILayout.Label("(Press T to toggle)", new GUIStyle(GUI.skin.label) { fontSize = 10 });
        GUILayout.EndArea();
    }

    void EnsureStyles()
    {
        labelStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = (int)fontSize,
            normal = { textColor = new Color(0, 1, 0, 0.8f) }
        };
    }
}
