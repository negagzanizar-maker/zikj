using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages tracking-based input for non-player karts.
/// Receives real-time position/heading/speed from TrackingReceiver and applies it to karts.
/// Supports mixed mode: player kart uses keyboard, others use tracking.
/// </summary>
public class TrackingInputManager : MonoBehaviour
{
    public static TrackingInputManager I { get; private set; }

    [Header("Tracking")]
    public bool trackingEnabled = true;
    public bool trackingDebug = false;
    public bool sourceUsesPrototypePixels = true;
    public bool enableTrackingOnFirstFrame = true;
    public float trackingTimeout = 0.35f;
    public bool stopOnTrackingTimeout = true;

    [Header("Interpolation")]
    public bool useInterpolation = true;
    public float interpolationDampTime = 0.05f; // Smooth damping for latency

    Dictionary<int, KartTrackingState> kartStates = new();

    class KartTrackingState
    {
        public KartController kart;
        public Vector3 targetPos;
        public float targetHeading;
        public float targetSpeed;
        public float lastUpdateTime;
        public bool hasFrame;
    }

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
    }

    void FixedUpdate()
    {
        if (!trackingEnabled || TrackingReceiver.I == null)
            return;

        // Process all queued tracking frames
        while (TrackingReceiver.I.TryGetLatestFrame(out var frame))
        {
            ApplyTrackingFrame(frame);
        }
    }

    void ApplyTrackingFrame(TrackingReceiver.TrackingFrame frame)
    {
        if (!kartStates.TryGetValue(frame.kartId, out var state))
            return;

        if (state.kart == null)
        {
            kartStates.Remove(frame.kartId);
            return;
        }

        // Don't override player kart input
        if (state.kart.isPlayer)
            return;

        if (enableTrackingOnFirstFrame)
            state.kart.trackingMode = true;

        // Validate and clamp tracking data (protect against corruption)
        state.targetPos = ToUnityPosition(frame.x, frame.y);
        state.targetHeading = ToUnityHeading(frame.heading);
        state.targetSpeed = Mathf.Max(0f, Mathf.Min(ToUnitySpeed(frame.speed), 50f));

        state.lastUpdateTime = Time.time;
        state.hasFrame = true;

        if (!useInterpolation)
        {
            // Direct assignment
            state.kart.Teleport(state.targetPos, state.targetHeading);
            state.kart.speed = state.targetSpeed;
        }

        if (trackingDebug)
        {
            Debug.Log($"[Tracking] Kart {frame.kartId}: pos=({frame.x:F1},{frame.y:F1}) h={frame.heading:F2} v={frame.speed:F1}");
        }
    }

    void LateUpdate()
    {
        if (!trackingEnabled)
            return;

        // Smooth interpolation for tracked karts
        float dt = Time.deltaTime;
        
        // Detect frame skip (dt > 2 × expected frame time)
        float frameSkipThreshold = 1f / 30f; // ~33ms for 30 FPS minimum
        bool frameSkip = dt > frameSkipThreshold;

        foreach (var kvp in kartStates)
        {
            var state = kvp.Value;
            if (state.kart == null || state.kart.isPlayer)
                continue;
            if (!state.hasFrame)
                continue;

            if (Time.time - state.lastUpdateTime > trackingTimeout)
            {
                if (stopOnTrackingTimeout)
                    state.kart.StopImmediately();
                continue;
            }

            if (!useInterpolation)
                continue;

            // Position interpolation with frame-skip detection
            Vector3 currentPos = state.kart.transform.position;
            float interpFactor = frameSkip ? 1f : (dt / interpolationDampTime); // Full snap on frame skip
            interpFactor = Mathf.Clamp01(interpFactor);
            
            Vector3 smoothPos = Vector3.Lerp(currentPos, state.targetPos, interpFactor);
            
            // Clamp to track if available
            if (WaypointCircuit.I != null)
                smoothPos = WaypointCircuit.I.ClampToTrack(smoothPos);
            
            state.kart.Teleport(smoothPos, state.targetHeading);

            // Speed damping
            state.kart.speed = Mathf.Lerp(state.kart.speed, state.targetSpeed, interpFactor);
        }
    }

    public void RegisterKart(KartController kart)
    {
        if (kart == null) return;

        kartStates[kart.kartIndex] = new KartTrackingState
        {
            kart = kart,
            targetPos = kart.transform.position,
            targetHeading = kart.heading,
            targetSpeed = kart.speed,
            lastUpdateTime = Time.time
        };
    }

    public void UnregisterKart(int kartIndex)
    {
        kartStates.Remove(kartIndex);
    }

    public float GetKartLatency(int kartIndex)
    {
        if (TrackingReceiver.I != null)
            return TrackingReceiver.I.frameLatencyMs;
        return -1f;
    }

    public int GetTrackedKartCount()
    {
        return kartStates.Count;
    }

    Vector3 ToUnityPosition(float x, float y)
    {
        if (!sourceUsesPrototypePixels)
            return new Vector3(x, 0f, y);

        return new Vector3(
            x * WaypointCircuit.Scale,
            0f,
            -y * WaypointCircuit.Scale);
    }

    float ToUnityHeading(float heading)
    {
        return sourceUsesPrototypePixels ? -heading : heading;
    }

    float ToUnitySpeed(float speed)
    {
        return sourceUsesPrototypePixels ? speed * WaypointCircuit.Scale : speed;
    }
}
