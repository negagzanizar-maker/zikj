using UnityEngine;
using UnityEngine.UI;

public class DriftArenaHUD : MonoBehaviour
{
    public static DriftArenaHUD I { get; private set; }

    [Header("HUD Text Elements")]
    public Text timerText;
    public Text scoreText;
    public Text comboText;
    public Text hotZoneText;
    public Text rankText;
    public Text helpText;

    [Header("Result Panel")]
    public GameObject resultPanel;
    public Text resultText;

    [Header("Config")]
    public int playerKartIndex = 0;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
    }

    void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (helpText != null) helpText.text = "WASD DRIVE | HOLD HARD TURNS TO DRIFT | C/V CAM | 1-7 VIEWS";
    }

    void Update()
    {
        if (DriftArenaMode.I == null) return;

        if (timerText != null)
            timerText.text = FmtTime(DriftArenaMode.I.RoundTimer);

        Refresh();
    }

    public void Refresh()
    {
        if (DriftArenaMode.I == null) return;

        if (scoreText != null)
            scoreText.text = $"SCORE {DriftArenaMode.I.Score(playerKartIndex):F0}";

        if (comboText != null)
        {
            float combo = DriftArenaMode.I.Combo(playerKartIndex);
            float angle = DriftArenaMode.I.DriftAngle(playerKartIndex);
            comboText.text = DriftArenaMode.I.IsDrifting(playerKartIndex)
                ? $"COMBO x{combo:F1} | DRIFT {angle:F0} DEG"
                : $"COMBO x{combo:F1}";
            comboText.color = DriftArenaMode.I.IsDrifting(playerKartIndex)
                ? new Color(1f, 0.56f, 0.10f)
                : Color.white;
        }

        if (hotZoneText != null)
        {
            bool hot = DriftArenaMode.I.PlayerInHotZone(playerKartIndex);
            hotZoneText.text = hot ? "HOT ZONE x3" : "HOT ZONE SEEK IT";
            hotZoneText.color = hot ? new Color(1f, 0.56f, 0.10f) : new Color(0.68f, 0.78f, 0.86f);
        }

        if (rankText != null)
        {
            int place = DriftArenaMode.I.PlaceOf(playerKartIndex);
            rankText.text = $"{Ordinal(place)} / {DriftArenaMode.I.TotalKarts}";
        }
    }

    public void ShowResult(string message)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = message;
    }

    static string Ordinal(int place)
    {
        switch (place)
        {
            case 1: return "1ST";
            case 2: return "2ND";
            case 3: return "3RD";
            default: return $"{place}TH";
        }
    }

    static string FmtTime(float sec)
    {
        sec = Mathf.Max(sec, 0f);
        int m = (int)(sec / 60f);
        int s = (int)(sec % 60f);
        return $"{m}:{s:D2}";
    }
}
