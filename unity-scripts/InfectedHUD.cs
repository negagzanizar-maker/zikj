using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach to the Canvas root GameObject.
// Requires TextMeshPro package (included in Unity 6 by default).
public class InfectedHUD : MonoBehaviour
{
    public static InfectedHUD I { get; private set; }

    [Header("HUD Text Elements")]
    public TMP_Text timerText;
    public TMP_Text statusText;
    public TMP_Text infectedCountText;

    [Header("Result Panel")]
    public GameObject resultPanel;
    public TMP_Text   resultText;

    [Header("Config")]
    public int playerKartIndex = 0;

    void Awake() => I = this;

    void Start() => resultPanel.SetActive(false);

    void Update()
    {
        if (InfectedMode.I == null) return;
        timerText.text = FmtTime(InfectedMode.I.RoundTimer);
        Refresh();
    }

    public void Refresh()
    {
        if (InfectedMode.I == null) return;

        bool inf = InfectedMode.I.IsInfected(playerKartIndex);
        bool imm = InfectedMode.I.IsImmune(playerKartIndex);

        if (imm)
        {
            statusText.text  = "IMMUNE";
            statusText.color = Color.cyan;
        }
        else if (inf)
        {
            statusText.text  = "INFECTED";
            statusText.color = Color.red;
        }
        else
        {
            statusText.text  = "CLEAN";
            statusText.color = Color.green;
        }

        int clean = InfectedMode.I.CleanKarts;
        int total = InfectedMode.I.TotalKarts;
        infectedCountText.text = $"{total - clean} / {total} INFECTED";
    }

    public void ShowResult(string message)
    {
        resultPanel.SetActive(true);
        resultText.text = message;
    }

    static string FmtTime(float sec)
    {
        sec = Mathf.Max(sec, 0f);
        int m = (int)(sec / 60f);
        int s = (int)(sec % 60f);
        return $"{m}:{s:D2}";
    }
}
