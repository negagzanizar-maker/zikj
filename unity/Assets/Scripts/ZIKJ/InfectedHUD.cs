using UnityEngine;
using UnityEngine.UI;

public class InfectedHUD : MonoBehaviour
{
    public static InfectedHUD I { get; private set; }

    [Header("HUD Text Elements")]
    public Text timerText;
    public Text statusText;
    public Text infectedCountText;
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
        if (helpText != null) helpText.text = "WASD / ARROWS - DRIVE";
    }

    void Update()
    {
        if (InfectedMode.I == null) return;

        if (timerText != null)
            timerText.text = FmtTime(InfectedMode.I.RoundTimer);

        Refresh();
    }

    public void Refresh()
    {
        if (InfectedMode.I == null) return;

        bool infected = InfectedMode.I.IsInfected(playerKartIndex);
        bool immune = InfectedMode.I.IsImmune(playerKartIndex);

        if (statusText != null)
        {
            if (immune)
            {
                statusText.text = "IMMUNE";
                statusText.color = new Color(0.35f, 1f, 0.60f);
            }
            else if (infected)
            {
                statusText.text = "INFECTED";
                statusText.color = new Color(1f, 0.18f, 0.22f);
            }
            else
            {
                statusText.text = "CLEAN";
                statusText.color = new Color(0.45f, 0.85f, 1f);
            }
        }

        if (infectedCountText != null)
        {
            int total = InfectedMode.I.TotalKarts;
            infectedCountText.text = $"{InfectedMode.I.InfectedKarts} / {total} INFECTED";
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
