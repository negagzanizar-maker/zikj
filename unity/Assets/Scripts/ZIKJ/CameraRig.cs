using UnityEngine;

// Multi-angle camera for fast playtesting of the generated arena.
[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    public enum ViewMode
    {
        Chase,
        CloseChase,
        HighChase,
        SideFollow,
        Bumper,
        Overhead,
        Arena
    }

    [Header("Overview")]
    public float centerX = 32f;
    public float centerZ = -18f;
    public float overviewHeight = 26f;
    public float overviewDistance = 30f;

    [Header("Chase")]
    public float chaseHeight = 6.4f;
    public float chaseDistance = 8.8f;
    public float lookAhead = 5.4f;
    public float followSharpness = 7f;
    public ViewMode viewMode = ViewMode.Arena;

    [Header("Projection")]
    public float fieldOfView = 56f;
    public float overheadSize = 14f;
    public float arenaOrthoSize = 24f;

    Camera cam;
    Transform target;
    Vector3 velocity;
    string lastHint;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 160f;
        cam.backgroundColor = new Color(0.02f, 0.025f, 0.035f);

        ApplyInstantPose();
    }

    void Start()
    {
        UpdateHelpText();
    }

    void LateUpdate()
    {
        HandleInput();

        if (target == null)
            target = FindPlayerTarget();

        CameraPose pose = BuildPose();
        ApplyProjection(pose);
        MoveToward(pose);
        UpdateHelpText();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
            NextMode();

        if (Input.GetKeyDown(KeyCode.V))
            PreviousMode();

        if (Input.GetKeyDown(KeyCode.Alpha1)) SetMode(ViewMode.Chase);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetMode(ViewMode.CloseChase);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetMode(ViewMode.HighChase);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetMode(ViewMode.SideFollow);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetMode(ViewMode.Bumper);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetMode(ViewMode.Overhead);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SetMode(ViewMode.Arena);
    }

    void NextMode()
    {
        int count = System.Enum.GetValues(typeof(ViewMode)).Length;
        SetMode((ViewMode)(((int)viewMode + 1) % count));
    }

    void PreviousMode()
    {
        int count = System.Enum.GetValues(typeof(ViewMode)).Length;
        SetMode((ViewMode)(((int)viewMode + count - 1) % count));
    }

    void SetMode(ViewMode mode)
    {
        if (viewMode == mode) return;

        viewMode = mode;
        velocity = Vector3.zero;
        lastHint = null;
    }

    CameraPose BuildPose()
    {
        Vector3 arenaCenter = OverviewLookAt();

        if (target == null)
            return ArenaPose(arenaCenter);

        Vector3 forward = FlatForward(target);
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        Vector3 kart = target.position;

        switch (viewMode)
        {
            case ViewMode.CloseChase:
                return PerspectivePose(kart - forward * 5.2f + Vector3.up * 3.1f, kart + forward * 6.8f + Vector3.up * 0.85f, 66f);

            case ViewMode.HighChase:
                return PerspectivePose(kart - forward * 12.5f + Vector3.up * 11f, kart + forward * 3.2f + Vector3.up * 0.4f, 52f);

            case ViewMode.SideFollow:
                return PerspectivePose(kart - forward * 5.5f + right * 7.5f + Vector3.up * 4.8f, kart + forward * 3.5f + Vector3.up * 0.8f, 60f);

            case ViewMode.Bumper:
                return PerspectivePose(kart + forward * 1.4f + Vector3.up * 0.95f, kart + forward * 13f + Vector3.up * 0.85f, 72f);

            case ViewMode.Overhead:
                return OrthoPose(kart + Vector3.up * 31f, kart, Quaternion.LookRotation(Vector3.down, forward), overheadSize);

            case ViewMode.Arena:
                return ArenaPose(arenaCenter);

            default:
                return PerspectivePose(kart - forward * chaseDistance + Vector3.up * chaseHeight, kart + forward * lookAhead + Vector3.up * 0.85f, fieldOfView);
        }
    }

    CameraPose ArenaPose(Vector3 arenaCenter)
    {
        Vector3 position = arenaCenter + new Vector3(-overviewDistance * 0.35f, overviewHeight, -overviewDistance);
        return OrthoPose(position, arenaCenter, Quaternion.LookRotation(arenaCenter - position, Vector3.up), arenaOrthoSize);
    }

    CameraPose PerspectivePose(Vector3 position, Vector3 lookAt, float fov)
    {
        return new CameraPose
        {
            position = position,
            rotation = Quaternion.LookRotation(lookAt - position, Vector3.up),
            orthographic = false,
            fieldOfView = fov
        };
    }

    CameraPose OrthoPose(Vector3 position, Vector3 lookAt, Quaternion rotation, float size)
    {
        return new CameraPose
        {
            position = position,
            rotation = rotation,
            orthographic = true,
            orthographicSize = size,
            fieldOfView = fieldOfView
        };
    }

    void ApplyProjection(CameraPose pose)
    {
        cam.orthographic = pose.orthographic;

        if (pose.orthographic)
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, pose.orthographicSize, ProjectionT());
        else
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, pose.fieldOfView, ProjectionT());
    }

    void MoveToward(CameraPose pose)
    {
        float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.SmoothDamp(transform.position, pose.position, ref velocity, 0.18f, Mathf.Infinity, Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, pose.rotation, t);
    }

    void ApplyInstantPose()
    {
        CameraPose pose = BuildPose();
        cam.orthographic = pose.orthographic;
        cam.fieldOfView = pose.fieldOfView;
        cam.orthographicSize = pose.orthographicSize <= 0f ? arenaOrthoSize : pose.orthographicSize;
        transform.SetPositionAndRotation(pose.position, pose.rotation);
    }

    Transform FindPlayerTarget()
    {
        var karts = FindObjectsByType<KartController>(FindObjectsSortMode.None);
        foreach (var kart in karts)
        {
            if (kart != null && kart.isPlayer)
                return kart.transform;
        }

        return null;
    }

    Vector3 FlatForward(Transform t)
    {
        Vector3 forward = t.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        return forward.normalized;
    }

    Vector3 OverviewLookAt()
    {
        return new Vector3(centerX, 0f, centerZ);
    }

    float ProjectionT()
    {
        return 1f - Mathf.Exp(-9f * Time.deltaTime);
    }

    void UpdateHelpText()
    {
        TextTarget text = HelpText();
        if (text.isMissing) return;

        string hint = $"{text.prefix} | C/V CAM | 1-7 VIEWS | {ModeLabel()}";
        if (hint == lastHint) return;

        text.value.text = hint;
        lastHint = hint;
    }

    TextTarget HelpText()
    {
        if (BattleRoyaleHUD.I != null && BattleRoyaleHUD.I.helpText != null)
            return new TextTarget(BattleRoyaleHUD.I.helpText, "WASD DRIVE | RAM TO DAMAGE");

        if (GrandPrixHUD.I != null && GrandPrixHUD.I.helpText != null)
            return new TextTarget(GrandPrixHUD.I.helpText, "WASD DRIVE | SPACE ITEM");

        if (DriftArenaHUD.I != null && DriftArenaHUD.I.helpText != null)
            return new TextTarget(DriftArenaHUD.I.helpText, "WASD DRIVE | HOLD HARD TURNS TO DRIFT");

        if (TronTrailsHUD.I != null && TronTrailsHUD.I.helpText != null)
            return new TextTarget(TronTrailsHUD.I.helpText, "WASD DRIVE | AVOID LIGHT TRAILS");

        if (LastOneLitHUD.I != null && LastOneLitHUD.I.helpText != null)
            return new TextTarget(LastOneLitHUD.I.helpText, "WASD DRIVE | AVOID OTHER SPOTLIGHTS");

        if (InfectedHUD.I != null && InfectedHUD.I.helpText != null)
            return new TextTarget(InfectedHUD.I.helpText, "WASD DRIVE");

        return new TextTarget(null, "");
    }

    string ModeLabel()
    {
        switch (viewMode)
        {
            case ViewMode.CloseChase: return "CLOSE";
            case ViewMode.HighChase: return "HIGH";
            case ViewMode.SideFollow: return "SIDE";
            case ViewMode.Bumper: return "BUMPER";
            case ViewMode.Overhead: return "OVERHEAD";
            case ViewMode.Arena: return "ARENA";
            default: return "CHASE";
        }
    }

    struct CameraPose
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool orthographic;
        public float fieldOfView;
        public float orthographicSize;
    }

    struct TextTarget
    {
        public readonly UnityEngine.UI.Text value;
        public readonly string prefix;
        public bool isMissing => value == null;

        public TextTarget(UnityEngine.UI.Text value, string prefix)
        {
            this.value = value;
            this.prefix = prefix;
        }
    }
}
