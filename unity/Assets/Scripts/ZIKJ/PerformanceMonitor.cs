using UnityEngine;

/// <summary>
/// Frame-skip and performance monitoring for tracking latency detection.
/// Detects when frame times exceed expected (frame skip) and warns about it.
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    public static PerformanceMonitor I { get; private set; }

    [Header("Thresholds")]
    public float frameTimeThresholdMs = 20f; // Warn if frame takes > 20ms (50 FPS)
    public float trackingLatencyThresholdMs = 100f; // Warn if tracking latency > 100ms

    [Header("Metrics")]
    public float averageFrameTimeMs;
    public float maxFrameTimeMs;
    public int frameSkips; // Count of frames exceeding threshold
    public int warningCount; // Total performance warnings

    float frameTimeSum;
    int frameCount;
    float maxFrameTimeSinceWarning;
    float lastWarningTime;

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
        float frameTimeMs = Time.deltaTime * 1000f;
        frameTimeSum += frameTimeMs;
        frameCount++;

        if (frameTimeMs > maxFrameTimeMs)
            maxFrameTimeMs = frameTimeMs;

        maxFrameTimeSinceWarning = Mathf.Max(maxFrameTimeSinceWarning, frameTimeMs);

        // Check for frame skip
        if (frameTimeMs > frameTimeThresholdMs)
        {
            frameSkips++;
            if (Time.time - lastWarningTime > 1f) // Only warn once per second
            {
                Debug.LogWarning($"[Performance] Frame skip detected: {frameTimeMs:F1}ms (threshold: {frameTimeThresholdMs}ms)");
                lastWarningTime = Time.time;
                warningCount++;
                maxFrameTimeSinceWarning = 0f;
            }
        }

        // Every 60 frames, log average
        if (frameCount >= 60)
        {
            averageFrameTimeMs = frameTimeSum / frameCount;
            frameTimeSum = 0f;
            frameCount = 0;
        }

        // Check tracking latency if available
        if (TrackingReceiver.I != null && TrackingReceiver.I.frameLatencyMs > trackingLatencyThresholdMs)
        {
            if (Time.time - lastWarningTime > 1f)
            {
                Debug.LogWarning($"[Performance] High tracking latency: {TrackingReceiver.I.frameLatencyMs:F1}ms");
                lastWarningTime = Time.time;
                warningCount++;
            }
        }
    }

    public float GetAverageFrameTimeMs()
    {
        if (frameCount > 0)
            return frameTimeSum / frameCount;
        return averageFrameTimeMs;
    }

    public void ResetMetrics()
    {
        frameSkips = 0;
        warningCount = 0;
        maxFrameTimeMs = 0f;
        averageFrameTimeMs = 0f;
        frameTimeSum = 0f;
        frameCount = 0;
    }
}
