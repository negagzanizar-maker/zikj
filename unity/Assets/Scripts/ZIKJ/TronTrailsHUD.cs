using UnityEngine;
using UnityEngine.UI;

public class TronTrailsHUD : MonoBehaviour
{
    public static TronTrailsHUD I { get; private set; }

    [Header("HUD Text Elements")]
    public Text aliveCountText;
    public Text statusText;
    public Text livesText;
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
        if (helpText != null) helpText.text = "WASD DRIVE | AVOID LIGHT TRAILS | C/V CAM | 1-7 VIEWS";
    }

    void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (TronTrailsMode.I == null) return;

        if (aliveCountText != null)
            aliveCountText.text = $"{TronTrailsMode.I.AliveKarts} / {TronTrailsMode.I.TotalKarts} ALIVE";

        if (statusText != null)
        {
            if (!TronTrailsMode.I.IsAlive(playerKartIndex))
            {
                statusText.text = "ELIMINATED";
                statusText.color = new Color(1f, 0.18f, 0.16f);
            }
            else if (TronTrailsMode.I.IsFrozen(playerKartIndex))
            {
                statusText.text = $"FROZEN {TronTrailsMode.I.FrozenRemaining(playerKartIndex):F1}s";
                statusText.color = Color.white;
            }
            else
            {
                statusText.text = "ALIVE";
                statusText.color = new Color(0.18f, 0.92f, 1f);
            }
        }

        if (livesText != null)
        {
            int lives = TronTrailsMode.I.Lives(playerKartIndex);
            livesText.text = $"LIVES {new string('|', Mathf.Max(0, lives))}";
        }
    }

    public void ShowResult(string message)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = message;
    }
}
