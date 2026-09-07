using UnityEngine;
using UnityEngine.UI;

public class BattleRoyaleHUD : MonoBehaviour
{
    public static BattleRoyaleHUD I { get; private set; }

    [Header("HUD Text Elements")]
    public Text timerText;
    public Text statusText;
    public Text aliveCountText;
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
        if (helpText != null) helpText.text = "WASD DRIVE | RAM TO DAMAGE | C/V CAM | 1-7 VIEWS";
    }

    void Update()
    {
        if (BattleRoyaleMode.I == null) return;

        if (timerText != null)
            timerText.text = FmtTime(BattleRoyaleMode.I.RoundTimer);

        Refresh();
    }

    public void Refresh()
    {
        if (BattleRoyaleMode.I == null) return;

        bool alive = BattleRoyaleMode.I.IsAlive(playerKartIndex);
        float hearts = BattleRoyaleMode.I.GetHearts(playerKartIndex);
        float maxHearts = BattleRoyaleMode.I.StartingHearts;

        if (statusText != null)
        {
            if (alive)
            {
                statusText.text = $"HEARTS {hearts:F1} / {maxHearts:F0}";
                statusText.color = hearts > maxHearts * 0.35f
                    ? new Color(0.45f, 0.85f, 1f)
                    : new Color(1f, 0.28f, 0.20f);
            }
            else
            {
                statusText.text = "KART OFF";
                statusText.color = new Color(1f, 0.16f, 0.12f);
            }
        }

        if (aliveCountText != null)
        {
            int total = BattleRoyaleMode.I.TotalKarts;
            aliveCountText.text = $"{BattleRoyaleMode.I.AliveKarts} / {total} ALIVE";
        }
    }

    public void ShowResult(string message)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = message;
    }

    static string FmtTime(float sec)
    {
        sec = Mathf.Max(sec, 0f);
        int m = (int)(sec / 60f);
        int s = (int)(sec % 60f);
        return $"{m}:{s:D2}";
    }
}
