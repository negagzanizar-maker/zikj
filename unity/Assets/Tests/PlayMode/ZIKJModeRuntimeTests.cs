#if UNITY_INCLUDE_TESTS
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ZIKJModeRuntimeTests
{
    readonly List<string> errors = new();

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        errors.Clear();
        Application.logMessageReceived -= CaptureErrors;
        Application.logMessageReceived += CaptureErrors;
        yield return CleanupRuntimeObjects();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Application.logMessageReceived -= CaptureErrors;
        yield return CleanupRuntimeObjects();
    }

    [UnityTest]
    public IEnumerator Infected_RuntimeSmoke()
    {
        return RunMode(GameManager.GameMode.Infected, 6);
    }

    [UnityTest]
    public IEnumerator BattleRoyale_RuntimeSmoke()
    {
        return RunMode(GameManager.GameMode.BattleRoyale, 8);
    }

    [UnityTest]
    public IEnumerator GrandPrix_RuntimeSmoke()
    {
        return RunMode(GameManager.GameMode.GrandPrix, 8);
    }

    [UnityTest]
    public IEnumerator DriftArena_RuntimeSmoke()
    {
        return RunMode(GameManager.GameMode.DriftArena, 8);
    }

    [UnityTest]
    public IEnumerator TronTrails_RuntimeSmoke()
    {
        return RunMode(GameManager.GameMode.TronTrails, 8);
    }

    [UnityTest]
    public IEnumerator LastOneLit_RuntimeSmoke()
    {
        return RunMode(GameManager.GameMode.LastOneLit, 8);
    }

    [UnityTest]
    public IEnumerator VenueLauncher_RuntimeSmoke()
    {
        Scene scene = SceneManager.CreateScene("RuntimeSmoke_VenueLauncher");
        SceneManager.SetActiveScene(scene);

        var bootstrapGo = new GameObject("Venue Launcher Bootstrap");
        var launcher = bootstrapGo.AddComponent<VenueLauncherScene>();
        launcher.buildOnStart = true;

        yield return null;
        yield return null;

        Assert.NotNull(Object.FindFirstObjectByType<VenueLauncherScene>(), "Missing VenueLauncherScene component");
        Assert.NotNull(Object.FindFirstObjectByType<VenueLauncherReturnHotkey>(), "Missing launcher return hotkey");
        Assert.NotNull(Object.FindFirstObjectByType<WaypointCircuit>(), "Missing launcher preview circuit");
        Assert.NotNull(Object.FindFirstObjectByType<TrackMeshBuilder>(), "Missing launcher preview track");
        Assert.NotNull(Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(), "Missing launcher EventSystem");
        Assert.NotNull(Object.FindFirstObjectByType<Canvas>(), "Missing launcher Canvas");
        Assert.NotNull(Camera.main, "Missing launcher Main Camera");

        string[] targetScenes = VenueLauncherScene.TargetSceneNames();
        Assert.AreEqual(7, targetScenes.Length, "Launcher should expose every Unity game/tool scene.");
        Assert.GreaterOrEqual(Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length, targetScenes.Length,
            "Launcher did not create enough launch buttons.");

        foreach (string targetScene in targetScenes)
            Assert.IsTrue(Application.CanStreamedLevelBeLoaded(targetScene), $"Launcher target is not in Build Settings: {targetScene}");

        Assert.Zero(errors.Count, string.Join("\n", errors));
    }

    [UnityTest]
    public IEnumerator ProjectionAlignment_RuntimeSmoke()
    {
        Scene scene = SceneManager.CreateScene("RuntimeSmoke_ProjectionAlignment");
        SceneManager.SetActiveScene(scene);

        var bootstrapGo = new GameObject("Projection Alignment Bootstrap");
        var alignment = bootstrapGo.AddComponent<ProjectionAlignmentScene>();
        alignment.buildOnStart = true;

        yield return null;
        yield return null;

        Assert.NotNull(Object.FindFirstObjectByType<ProjectionAlignmentScene>(), "Missing ProjectionAlignmentScene component");
        Assert.NotNull(Object.FindFirstObjectByType<WaypointCircuit>(), "Missing WaypointCircuit");
        Assert.NotNull(Object.FindFirstObjectByType<TrackMeshBuilder>(), "Missing TrackMeshBuilder");
        Assert.NotNull(Object.FindFirstObjectByType<TrackingReceiver>(), "Missing TrackingReceiver");
        Assert.NotNull(Object.FindFirstObjectByType<TrackingInputManager>(), "Missing TrackingInputManager");
        Assert.NotNull(Object.FindFirstObjectByType<TrackingDebugHUD>(), "Missing TrackingDebugHUD");
        Assert.NotNull(Object.FindFirstObjectByType<PerformanceMonitor>(), "Missing PerformanceMonitor");
        Assert.NotNull(Camera.main, "Missing Main Camera");
        Assert.IsTrue(Camera.main.orthographic, "Projection alignment camera should be orthographic");

        for (int i = 0; i < 4; i++)
            Assert.NotNull(GameObject.Find($"Projector Coverage P{i}"), $"Missing projector coverage P{i}");

        Assert.GreaterOrEqual(Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Length, 18,
            "Projection alignment grid did not create enough line renderers.");
        Assert.GreaterOrEqual(alignment.AlignmentPointCount, 81,
            "Projection alignment did not create the full calibration point grid.");
        Assert.AreEqual(6, alignment.TrackingMarkerPreviewCount,
            "Projection alignment should show six tracking marker previews.");
        Assert.Zero(errors.Count, string.Join("\n", errors));
    }

    [UnityTest]
    public IEnumerator TrackingSimulatorPacketMovesRegisteredKart()
    {
        return RunTrackingPacketMovesRegisteredKart(AllocateUdpPort(), "RuntimeSmoke_TrackingUdp");
    }

    [UnityTest]
    public IEnumerator TrackingDefaultPort5555MovesRegisteredKart()
    {
        return RunTrackingPacketMovesRegisteredKart(5555, "RuntimeSmoke_TrackingUdp5555");
    }

    [UnityTest]
    public IEnumerator TrackingFirstFrameTakesOverAiFallbackKart()
    {
        Scene scene = SceneManager.CreateScene("RuntimeSmoke_TrackingHandoff");
        SceneManager.SetActiveScene(scene);

        int port = AllocateUdpPort();
        var receiverGo = new GameObject("Tracking Receiver");
        var receiver = receiverGo.AddComponent<TrackingReceiver>();
        receiver.autoStart = false;
        receiver.listenPort = port;
        receiver.StartListening();

        var inputGo = new GameObject("Tracking Input Manager");
        var input = inputGo.AddComponent<TrackingInputManager>();
        input.trackingEnabled = true;
        input.sourceUsesPrototypePixels = false;
        input.useInterpolation = false;

        var kartGo = new GameObject("AI Fallback Kart");
        kartGo.AddComponent<Rigidbody>();
        var kart = kartGo.AddComponent<KartController>();
        kartGo.AddComponent<KartAI>();
        kart.kartIndex = 11;
        kart.isPlayer = false;
        kart.trackingMode = false;
        kart.Teleport(Vector3.zero, 0f);
        input.RegisterKart(kart);

        var sender = new UdpClient();
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        byte[] payload = Encoding.UTF8.GetBytes(
            "{\"kart_id\":11,\"x\":3.0,\"y\":4.0,\"heading\":0.5,\"speed\":6.0,\"t\":12345.0}");

        for (int i = 0; i < 30; i++)
        {
            sender.Send(payload, payload.Length, endpoint);
            yield return new WaitForSeconds(0.02f);
            yield return new WaitForFixedUpdate();
            yield return null;

            if (receiver.packetsReceived > 0 && kart.trackingMode)
                break;
        }

        sender.Close();

        Assert.Greater(receiver.packetsReceived, 0, "Tracking receiver did not receive handoff packets.");
        Assert.IsTrue(kart.trackingMode, "First tracking frame did not switch the AI fallback kart into tracking mode.");
        Assert.That(kart.transform.position.x, Is.EqualTo(3f).Within(0.1f));
        Assert.That(kart.transform.position.z, Is.EqualTo(4f).Within(0.1f));
    }

    [UnityTest]
    public IEnumerator TrackingInterpolationAppliesFreshFrame()
    {
        Scene scene = SceneManager.CreateScene("RuntimeSmoke_TrackingInterpolation");
        SceneManager.SetActiveScene(scene);

        int port = AllocateUdpPort();
        var receiverGo = new GameObject("Tracking Receiver");
        var receiver = receiverGo.AddComponent<TrackingReceiver>();
        receiver.autoStart = false;
        receiver.listenPort = port;
        receiver.StartListening();

        var inputGo = new GameObject("Tracking Input Manager");
        var input = inputGo.AddComponent<TrackingInputManager>();
        input.trackingEnabled = true;
        input.sourceUsesPrototypePixels = false;
        input.useInterpolation = true;
        input.interpolationDampTime = 0.05f;
        input.trackingTimeout = 1f;

        var kartGo = new GameObject("Interpolated Tracked Kart");
        kartGo.AddComponent<Rigidbody>();
        var kart = kartGo.AddComponent<KartController>();
        kart.kartIndex = 8;
        kart.isPlayer = false;
        kart.trackingMode = true;
        kart.Teleport(Vector3.zero, 0f);
        input.RegisterKart(kart);

        var sender = new UdpClient();
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        var expectedPosition = new Vector3(12f, 0f, -4f);
        byte[] payload = Encoding.UTF8.GetBytes(
            "{\"kart_id\":8,\"x\":12.0,\"y\":-4.0,\"heading\":0.75,\"speed\":9.0,\"t\":12345.0}");

        for (int i = 0; i < 45; i++)
        {
            sender.Send(payload, payload.Length, endpoint);
            yield return new WaitForSeconds(0.02f);
            yield return new WaitForFixedUpdate();
            yield return null;

            if (receiver.packetsReceived > 0 &&
                Vector3.Distance(kart.transform.position, expectedPosition) < 0.5f)
            {
                break;
            }
        }

        sender.Close();

        Assert.Greater(receiver.packetsReceived, 0, "Tracking receiver did not receive interpolation test packets.");
        Assert.Zero(receiver.parseErrors, "Tracking receiver reported JSON parse errors.");
        Assert.That(Vector3.Distance(kart.transform.position, expectedPosition), Is.LessThan(0.5f));
        Assert.That(kart.heading, Is.EqualTo(0.75f).Within(0.01f));
        Assert.That(kart.speed, Is.GreaterThan(0.01f));
    }

    [UnityTest]
    public IEnumerator TrackingTimeoutStopsStaleRegisteredKart()
    {
        Scene scene = SceneManager.CreateScene("RuntimeSmoke_TrackingTimeout");
        SceneManager.SetActiveScene(scene);

        int port = AllocateUdpPort();
        var receiverGo = new GameObject("Tracking Receiver");
        var receiver = receiverGo.AddComponent<TrackingReceiver>();
        receiver.autoStart = false;
        receiver.listenPort = port;
        receiver.StartListening();

        var inputGo = new GameObject("Tracking Input Manager");
        var input = inputGo.AddComponent<TrackingInputManager>();
        input.trackingEnabled = true;
        input.sourceUsesPrototypePixels = false;
        input.useInterpolation = false;
        input.trackingTimeout = 0.05f;
        input.stopOnTrackingTimeout = true;

        var kartGo = new GameObject("Timeout Tracked Kart");
        kartGo.AddComponent<Rigidbody>();
        var kart = kartGo.AddComponent<KartController>();
        kart.kartIndex = 9;
        kart.isPlayer = false;
        kart.trackingMode = true;
        kart.Teleport(Vector3.zero, 0f);
        input.RegisterKart(kart);

        var sender = new UdpClient();
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        byte[] payload = Encoding.UTF8.GetBytes(
            "{\"kart_id\":9,\"x\":5.0,\"y\":3.0,\"heading\":0.25,\"speed\":14.0,\"t\":12345.0}");

        for (int i = 0; i < 30; i++)
        {
            sender.Send(payload, payload.Length, endpoint);
            yield return new WaitForSeconds(0.02f);
            yield return new WaitForFixedUpdate();
            yield return null;

            if (receiver.packetsReceived > 0 && kart.speed > 0.1f)
                break;
        }

        sender.Close();

        Assert.Greater(receiver.packetsReceived, 0, "Tracking receiver did not receive timeout test packets.");
        Assert.That(kart.speed, Is.GreaterThan(0.1f), "Tracking packet did not apply speed before timeout.");

        Vector3 lastTrackedPosition = kart.transform.position;
        yield return new WaitForSeconds(input.trackingTimeout + 0.12f);
        yield return null;

        Assert.That(kart.speed, Is.EqualTo(0f).Within(0.001f), "Stale tracking did not stop kart speed.");
        Assert.That(kart.throttleInput, Is.EqualTo(0f).Within(0.001f), "Stale tracking left throttle input active.");
        Assert.That(Vector3.Distance(kart.transform.position, lastTrackedPosition), Is.LessThan(0.05f),
            "Tracking-mode kart drifted after tracking timed out.");
    }

    [UnityTest]
    public IEnumerator TrackingModeSuppressesAiAndPhysicsDrift()
    {
        Scene scene = SceneManager.CreateScene("RuntimeSmoke_TrackingAiSuppression");
        SceneManager.SetActiveScene(scene);

        new GameObject("Waypoint Circuit").AddComponent<WaypointCircuit>();

        var kartGo = new GameObject("AI Suppressed Tracked Kart");
        kartGo.AddComponent<Rigidbody>();
        var kart = kartGo.AddComponent<KartController>();
        kartGo.AddComponent<KartAI>();
        kart.kartIndex = 10;
        kart.isPlayer = false;
        kart.trackingMode = true;
        kart.Teleport(new Vector3(10f, 0f, -10f), 0f);
        kart.speed = 8f;
        kart.throttleInput = 1f;
        kart.steerInput = 1f;

        Vector3 start = kart.transform.position;
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.That(Vector3.Distance(kart.transform.position, start), Is.LessThan(0.01f),
            "Tracking-mode kart moved under local physics/AI.");
        Assert.That(kart.throttleInput, Is.EqualTo(0f).Within(0.001f), "AI throttle was not cleared for tracking-mode kart.");
        Assert.That(kart.steerInput, Is.EqualTo(0f).Within(0.001f), "AI steering was not cleared for tracking-mode kart.");
    }

    IEnumerator RunTrackingPacketMovesRegisteredKart(int port, string sceneName)
    {
        Scene scene = SceneManager.CreateScene(sceneName);
        SceneManager.SetActiveScene(scene);

        var receiverGo = new GameObject("Tracking Receiver");
        var receiver = receiverGo.AddComponent<TrackingReceiver>();
        receiver.autoStart = false;
        receiver.listenPort = port;
        receiver.StartListening();

        var inputGo = new GameObject("Tracking Input Manager");
        var input = inputGo.AddComponent<TrackingInputManager>();
        input.trackingEnabled = true;
        input.sourceUsesPrototypePixels = true;
        input.useInterpolation = false;

        var kartGo = new GameObject("Tracked Kart");
        kartGo.AddComponent<Rigidbody>();
        var kart = kartGo.AddComponent<KartController>();
        kart.kartIndex = 3;
        kart.isPlayer = false;
        kart.trackingMode = true;
        kart.Teleport(Vector3.zero, 0f);
        input.RegisterKart(kart);

        var sender = new UdpClient();
        var endpoint = new IPEndPoint(IPAddress.Loopback, port);
        var expectedPosition = new Vector3(420f * WaypointCircuit.Scale, 0f, -260f * WaypointCircuit.Scale);
        const float ExpectedHeading = -1.25f;

        for (int i = 0; i < 30; i++)
        {
            byte[] payload = Encoding.UTF8.GetBytes(
                "{\"kart_id\":3,\"x\":420.0,\"y\":260.0,\"heading\":1.25,\"speed\":22.0,\"t\":12345.0}");
            sender.Send(payload, payload.Length, endpoint);

            yield return new WaitForSeconds(0.02f);
            yield return new WaitForFixedUpdate();
            yield return null;

            if (receiver.packetsReceived > 0 &&
                Vector3.Distance(kart.transform.position, expectedPosition) < 0.25f)
            {
                break;
            }
        }

        sender.Close();

        Assert.Greater(receiver.packetsReceived, 0, "Tracking receiver did not receive simulator-shaped UDP packets.");
        Assert.Zero(receiver.parseErrors, "Tracking receiver reported JSON parse errors.");
        Assert.That(kart.transform.position.x, Is.EqualTo(expectedPosition.x).Within(0.25f));
        Assert.That(kart.transform.position.z, Is.EqualTo(expectedPosition.z).Within(0.25f));
        Assert.That(kart.heading, Is.EqualTo(ExpectedHeading).Within(0.01f));
        Assert.That(kart.speed, Is.GreaterThan(0.1f));
    }

    IEnumerator RunMode(GameManager.GameMode mode, int expectedKarts)
    {
        Scene scene = SceneManager.CreateScene("RuntimeSmoke_" + mode);
        SceneManager.SetActiveScene(scene);

        var bootstrapGo = new GameObject("Runtime Smoke Bootstrap");
        var bootstrap = bootstrapGo.AddComponent<InfectedSceneBootstrap>();
        bootstrap.buildOnStart = true;
        bootstrap.gameMode = mode;
        bootstrap.kartCount = expectedKarts;
        bootstrap.playerKartIndex = 0;

        yield return null;
        yield return null;

        AssertCoreScene(mode, expectedKarts);

        for (int i = 0; i < 12; i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        AssertNonPlayerAiFallbackIsDriving(mode, expectedKarts);

        for (int i = 0; i < 78; i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        AssertCoreScene(mode, expectedKarts);
        Assert.Zero(errors.Count, string.Join("\n", errors));
    }

    void AssertCoreScene(GameManager.GameMode mode, int expectedKarts)
    {
        var manager = Object.FindFirstObjectByType<GameManager>();
        Assert.NotNull(manager, $"{mode}: missing GameManager");
        Assert.AreEqual(mode, manager.gameMode, $"{mode}: wrong GameManager mode");
        Assert.NotNull(manager.Karts, $"{mode}: karts were not spawned");
        Assert.AreEqual(expectedKarts, manager.Karts.Length, $"{mode}: wrong kart count");

        int playerCount = 0;
        int aiFallbackCount = 0;
        foreach (var kart in manager.Karts)
        {
            Assert.NotNull(kart, $"{mode}: null kart");
            Assert.NotNull(kart.GetComponent<KartAI>(), $"{mode}: kart missing AI");
            Assert.NotNull(kart.GetComponent<KartVisualEffects>(), $"{mode}: kart missing visuals");

            if (kart.isPlayer)
                playerCount++;
            else if (!kart.trackingMode)
                aiFallbackCount++;
        }

        Assert.AreEqual(1, playerCount, $"{mode}: expected one player kart");
        Assert.AreEqual(expectedKarts - 1, aiFallbackCount,
            $"{mode}: expected non-player karts to use AI fallback until live tracking packets arrive");

        Assert.NotNull(Object.FindFirstObjectByType<WaypointCircuit>(), $"{mode}: missing WaypointCircuit");
        Assert.NotNull(Object.FindFirstObjectByType<TrackMeshBuilder>(), $"{mode}: missing TrackMeshBuilder");
        AssertThemedEnvironment(mode);
        Assert.NotNull(Object.FindFirstObjectByType<TrackingReceiver>(), $"{mode}: missing TrackingReceiver");
        Assert.NotNull(Object.FindFirstObjectByType<TrackingInputManager>(), $"{mode}: missing TrackingInputManager");
        Assert.NotNull(Object.FindFirstObjectByType<TrackingDebugHUD>(), $"{mode}: missing TrackingDebugHUD");
        Assert.NotNull(Object.FindFirstObjectByType<PerformanceMonitor>(), $"{mode}: missing PerformanceMonitor");
        Assert.NotNull(Camera.main, $"{mode}: missing Main Camera");
        Assert.NotNull(Camera.main.GetComponent<CameraRig>(), $"{mode}: missing CameraRig");

        var tracking = Object.FindFirstObjectByType<TrackingInputManager>();
        Assert.AreEqual(expectedKarts - 1, tracking.GetTrackedKartCount(), $"{mode}: tracking registration mismatch");

        switch (mode)
        {
            case GameManager.GameMode.Infected:
                Assert.IsTrue(InfectedMode.I != null && InfectedMode.I.IsRunning, "Infected mode is not running");
                Assert.NotNull(InfectedHUD.I, "Missing Infected HUD");
                break;
            case GameManager.GameMode.BattleRoyale:
                Assert.IsTrue(BattleRoyaleMode.I != null && BattleRoyaleMode.I.IsRunning, "Battle Royale mode is not running");
                Assert.NotNull(BattleRoyaleHUD.I, "Missing Battle Royale HUD");
                break;
            case GameManager.GameMode.GrandPrix:
                Assert.IsTrue(GrandPrixMode.I != null && GrandPrixMode.I.IsRunning, "Grand Prix mode is not running");
                Assert.NotNull(GrandPrixHUD.I, "Missing Grand Prix HUD");
                break;
            case GameManager.GameMode.DriftArena:
                Assert.IsTrue(DriftArenaMode.I != null && DriftArenaMode.I.IsRunning, "Drift Arena mode is not running");
                Assert.NotNull(DriftArenaHUD.I, "Missing Drift Arena HUD");
                break;
            case GameManager.GameMode.TronTrails:
                Assert.IsTrue(TronTrailsMode.I != null && TronTrailsMode.I.IsRunning, "Tron Trails mode is not running");
                Assert.NotNull(TronTrailsHUD.I, "Missing Tron Trails HUD");
                break;
            case GameManager.GameMode.LastOneLit:
                Assert.IsTrue(LastOneLitMode.I != null && LastOneLitMode.I.IsRunning, "Last One Lit mode is not running");
                Assert.NotNull(LastOneLitHUD.I, "Missing Last One Lit HUD");
                break;
        }
    }

    void AssertNonPlayerAiFallbackIsDriving(GameManager.GameMode mode, int expectedKarts)
    {
        var manager = Object.FindFirstObjectByType<GameManager>();
        Assert.NotNull(manager, $"{mode}: missing GameManager before AI fallback assertion");

        int fallbackKarts = 0;
        int movingKarts = 0;

        foreach (var kart in manager.Karts)
        {
            if (kart == null || kart.isPlayer)
                continue;

            fallbackKarts++;
            Assert.IsFalse(kart.trackingMode, $"{mode}: non-player kart entered tracking mode before any tracking packet");

            var ai = kart.GetComponent<KartAI>();
            Assert.NotNull(ai, $"{mode}: non-player kart missing AI fallback");

            if (ai.enabled && kart.speed > 0.1f)
                movingKarts++;
        }

        Assert.AreEqual(expectedKarts - 1, fallbackKarts, $"{mode}: wrong AI fallback kart count");
        Assert.Greater(movingKarts, 0, $"{mode}: AI fallback did not move any non-player karts");
    }

    void AssertThemedEnvironment(GameManager.GameMode mode)
    {
        var environment = Object.FindFirstObjectByType<InfectedEnvironmentBuilder>();
        Assert.NotNull(environment, $"{mode}: missing themed environment builder");
        Assert.AreEqual(ExpectedEnvironmentTheme(mode), environment.theme, $"{mode}: wrong environment theme");

        switch (mode)
        {
            case GameManager.GameMode.Infected:
                Assert.NotNull(GameObject.Find("Zombie Quarantine Gate Sign"), "Infected: missing zombie quarantine gate");
                Assert.NotNull(GameObject.Find("Zombie Wrecked Car West Body"), "Infected: missing zombie wrecked car dressing");
                break;
            case GameManager.GameMode.BattleRoyale:
                Assert.NotNull(GameObject.Find("War Zone Command Bunker"), "Battle Royale: missing war-zone bunker");
                Assert.NotNull(GameObject.Find("War Zone Drop Zone Marker"), "Battle Royale: missing war-zone drop marker");
                break;
            case GameManager.GameMode.GrandPrix:
                Assert.NotNull(GameObject.Find("Grand Prix Rainbow Start Arch 0"), "Grand Prix: missing colorful start arch");
                Assert.NotNull(GameObject.Find("Grand Prix Boost Pad Preview 0 Base"), "Grand Prix: missing boost pad preview dressing");
                var grandPrix = Object.FindFirstObjectByType<GrandPrixMode>();
                Assert.NotNull(grandPrix, "Grand Prix: missing GrandPrixMode");
                Assert.GreaterOrEqual(grandPrix.ActiveItemBoxCount(), 3, "Grand Prix: item boxes did not spawn");
                Assert.GreaterOrEqual(grandPrix.ActiveBoostPadCount(), 2, "Grand Prix: boost pads did not spawn");
                break;
            case GameManager.GameMode.DriftArena:
                Assert.NotNull(GameObject.Find("Moroccan Tent Ridge"), "Drift Arena: missing Moroccan tent ridge");
                Assert.NotNull(GameObject.Find("Moroccan Green Star Center Ray 0"), "Drift Arena: missing Moroccan star motif");
                break;
        }
    }

    InfectedEnvironmentBuilder.ArenaTheme ExpectedEnvironmentTheme(GameManager.GameMode mode)
    {
        return mode switch
        {
            GameManager.GameMode.Infected => InfectedEnvironmentBuilder.ArenaTheme.ZombieApocalypse,
            GameManager.GameMode.BattleRoyale => InfectedEnvironmentBuilder.ArenaTheme.WarZone,
            GameManager.GameMode.GrandPrix => InfectedEnvironmentBuilder.ArenaTheme.ColorfulKartArena,
            GameManager.GameMode.DriftArena => InfectedEnvironmentBuilder.ArenaTheme.MoroccanTent,
            GameManager.GameMode.LastOneLit => InfectedEnvironmentBuilder.ArenaTheme.Spotlight,
            _ => InfectedEnvironmentBuilder.ArenaTheme.NeonArena
        };
    }

    void CaptureErrors(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
            errors.Add($"{type}: {condition}\n{stackTrace}");
    }

    IEnumerator CleanupRuntimeObjects()
    {
        foreach (var receiver in Object.FindObjectsByType<TrackingReceiver>(FindObjectsSortMode.None))
        {
            receiver.StopListening();
            Object.Destroy(receiver.gameObject);
        }

        foreach (var launcherHotkey in Object.FindObjectsByType<VenueLauncherReturnHotkey>(FindObjectsSortMode.None))
            Object.Destroy(launcherHotkey.gameObject);

        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.name.StartsWith("RuntimeSmoke_"))
                continue;

            foreach (var root in scene.GetRootGameObjects())
                Object.Destroy(root);

            if (scene.isLoaded)
                SceneManager.UnloadSceneAsync(scene);
        }

        yield return null;
        yield return null;
    }

    static int AllocateUdpPort()
    {
        using (var client = new UdpClient(0))
        {
            return ((IPEndPoint)client.Client.LocalEndPoint).Port;
        }
    }
}
#endif
