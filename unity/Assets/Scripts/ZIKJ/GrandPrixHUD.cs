using UnityEngine;
using UnityEngine.UI;

public class GrandPrixHUD : MonoBehaviour
{
    public static GrandPrixHUD I { get; private set; }

    [Header("HUD Text Elements")]
    public Text timerText;
    public Text lapText;
    public Text placeText;
    public Text itemText;
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
        if (helpText != null) helpText.text = "WASD DRIVE | SPACE ITEM | C/V CAM | 1-7 VIEWS";
    }

    void Update()
    {
        if (GrandPrixMode.I == null) return;

        if (timerText != null)
            timerText.text = FmtTime(GrandPrixMode.I.RoundTimer);

        Refresh();
    }

    public void Refresh()
    {
        if (GrandPrixMode.I == null) return;

        bool finished = GrandPrixMode.I.IsFinished(playerKartIndex);

        if (lapText != null)
        {
            lapText.text = finished
                ? "FINISHED"
                : $"LAP {GrandPrixMode.I.PlayerLap(playerKartIndex)} / {GrandPrixMode.I.LapsToWin}";
        }

        if (placeText != null)
        {
            int place = GrandPrixMode.I.PlaceOf(playerKartIndex);
            placeText.text = $"{Ordinal(place)} PLACE";
            placeText.color = place == 1
                ? new Color(1f, 0.84f, 0.20f)
                : Color.white;
        }

        if (itemText != null)
        {
            var item = GrandPrixMode.I.HeldItem(playerKartIndex);
            itemText.text = item == GrandPrixMode.PowerUp.None
                ? "ITEM: EMPTY"
                : $"ITEM: {ItemLabel(item)}";
            itemText.color = item == GrandPrixMode.PowerUp.None
                ? new Color(0.68f, 0.78f, 0.86f)
                : new Color(0.18f, 0.92f, 1f);
        }
    }

    public void ShowResult(string message)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = message;
    }

    static string ItemLabel(GrandPrixMode.PowerUp item)
    {
        switch (item)
        {
            case GrandPrixMode.PowerUp.Boost: return "BOOST";
            case GrandPrixMode.PowerUp.Shield: return "SHIELD";
            case GrandPrixMode.PowerUp.Mine: return "MINE";
            case GrandPrixMode.PowerUp.Shock: return "SHOCK";
            default: return "EMPTY";
        }
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
