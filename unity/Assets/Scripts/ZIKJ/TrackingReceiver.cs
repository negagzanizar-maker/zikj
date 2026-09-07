using System;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// UDP listener for real-time kart tracking data from Python OpenCV service.
/// Expected packet format (JSON):
/// { "kart_id": int, "x": float, "y": float, "heading": float, "speed": float, "t": float }
/// </summary>
public class TrackingReceiver : MonoBehaviour
{
    public static TrackingReceiver I { get; private set; }

    [Header("Network")]
    public int listenPort = 5555;
    public bool autoStart = true;

    [Header("Metrics")]
    public float frameLatencyMs;
    public int packetsReceived;
    public int parseErrors;

    UdpClient udpClient;
    bool listening;
    Queue<TrackingFrame> frameQueue = new();

    public struct TrackingFrame
    {
        public int kartId;
        public float x, y, heading, speed;
        public float timestamp;
        public float receivedAt;
    }

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (autoStart)
            StartListening();
    }

    public void StartListening()
    {
        if (listening)
        {
            Debug.LogWarning("Tracking receiver already listening.");
            return;
        }

        try
        {
            udpClient = new UdpClient(listenPort);
            udpClient.BeginReceive(OnPacketReceived, null);
            listening = true;
            Debug.Log($"[Tracking] Listening on UDP :{listenPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Tracking] Failed to start: {e.Message}");
        }
    }

    public void StopListening()
    {
        listening = false;
        udpClient?.Close();
        udpClient = null;
        Debug.Log("[Tracking] Stopped listening.");
    }

    void OnPacketReceived(IAsyncResult ar)
    {
        try
        {
            if (!listening || udpClient == null)
                return;

            IPEndPoint ep = null;
            byte[] data = udpClient.EndReceive(ar, ref ep);

            string json = Encoding.UTF8.GetString(data);
            if (ParseTrackingFrame(json, out var frame))
            {
                frame.receivedAt = NowSeconds();
                lock (frameQueue)
                {
                    frameQueue.Enqueue(frame);
                    while (frameQueue.Count > 512)
                        frameQueue.Dequeue();
                }
                packetsReceived++;
            }
            else
            {
                parseErrors++;
            }

            if (listening)
                udpClient.BeginReceive(OnPacketReceived, null);
        }
        catch (Exception e)
        {
            if (!listening)
                return;

            Debug.LogError($"[Tracking] Receive error: {e.Message}");
            if (listening)
                udpClient.BeginReceive(OnPacketReceived, null);
        }
    }

    bool ParseTrackingFrame(string json, out TrackingFrame frame)
    {
        frame = default;
        try
        {
            // Simple JSON parsing (no external dependency)
            var data = JsonUtility.FromJson<TrackingFrameJson>(json);
            frame = new TrackingFrame
            {
                kartId = data.kart_id,
                x = data.x,
                y = data.y,
                heading = data.heading,
                speed = data.speed,
                timestamp = data.t > 0f ? data.t : data.timestamp
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryGetLatestFrame(out TrackingFrame frame)
    {
        frame = default;
        lock (frameQueue)
        {
            if (frameQueue.Count == 0)
                return false;

            frame = frameQueue.Dequeue();
            frameLatencyMs = (NowSeconds() - frame.receivedAt) * 1000f;
            return true;
        }
    }

    void OnDestroy()
    {
        StopListening();
    }

    [System.Serializable]
    private class TrackingFrameJson
    {
        public int kart_id;
        public float x, y, heading, speed;
        public float t;
        public float timestamp;
    }

    static float NowSeconds()
    {
        return (float)(System.Diagnostics.Stopwatch.GetTimestamp() /
            (double)System.Diagnostics.Stopwatch.Frequency);
    }
}
