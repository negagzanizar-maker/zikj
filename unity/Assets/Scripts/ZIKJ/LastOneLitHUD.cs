using UnityEngine;
using UnityEngine.UI;

public class LastOneLitHUD : MonoBehaviour
{
    public static LastOneLitHUD I { get; private set; }

    [Header("HUD Text Elements")]
    public Text timerText;
    public Text lightText;
    public Text aliveCountText;
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
        if (helpText != null) helpText.text = "WASD DRIVE | AVOID OTHER SPOTLIGHTS | C/V CAM | 1-7 VIEWS";
    }

    void Update()
    {
        if (LastOneLitMode.I == null) return;

        if (timerText != null)
            timerText.text = FmtTime(LastOneLitMode.I.RoundTimer);

        Refresh();
    }

    public void Refresh()
    {
        if (LastOneLitMode.I == null) return;

        if (lightText != null)
        {
            float pct = LastOneLitMode.I.LightPercent(playerKartIndex) * 100f;
            bool alive = LastOneLitMode.I.IsAlive(playerKartIndex);
            lightText.text = alive ? $"YOUR LIGHT {pct:F0}%" : "YOUR LIGHT OUT";
            lightText.color = alive && pct > 30f
                ? new Color(1f, 0.84f, 0.24f)
                : new Color(1f, 0.25f, 0.18f);
        }

        if (aliveCountText != null)
            aliveCountText.text = $"{LastOneLitMode.I.AliveKarts} / {LastOneLitMode.I.TotalKarts} STILL LIT";

        if (rankText != null)
        {
            int place = LastOneLitMode.I.PlaceOf(playerKartIndex);
            rankText.text = LastOneLitMode.I.IsAlive(playerKartIndex)
                ? $"{Ordinal(place)} BY LIGHT"
                : "ELIMINATED";
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
