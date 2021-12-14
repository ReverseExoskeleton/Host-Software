using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField]
    private Transform messageTf;
    private RectTransform messagePanel;
    private Text messageText;
    public float messageDuration = 3f;

    [SerializeField]
    private Transform connectingTf;
    private Transform circleTf;
    private bool displayConnecting;
    private float conStart;

    [SerializeField]
    private Transform startTf;

    [SerializeField]
    private Transform timerTf;
    private Text timerText;

    [SerializeField]
    private Transform scoreTf;
    private Text scoreText;

    [SerializeField]
    private Transform highscoresTf;
    private Transform highscoreContainer;
    [SerializeField]
    private Transform highscoreInputTf;
    [SerializeField]
    private GameObject playerHighscorePrefab;

    private List<string> messages = new List<string>();
    private float msgStart;
    private bool msgDisplayed = false;

    private const int NoNewUserScore = -1;

    private void Start()
    {
        // Setup messages
        messagePanel = messageTf.Find("Panel").GetComponent<RectTransform>();
        messageText = messageTf.Find("Text").GetComponent<Text>();

        // Setup connecting
        circleTf = connectingTf.Find("Img");

        // Setup timer
        timerText = timerTf.GetComponentInChildren<Text>();

        // Setup timer
        scoreText = scoreTf.GetComponentInChildren<Text>();

        // Setup highscores
        highscoreContainer = highscoresTf.Find("Panel").Find("List");
    }

    private void Update()
    {
        ProcessMessages();

        if (displayConnecting)
        {
            float angle = (1 + Mathf.Sin((Time.time - conStart) * Mathf.PI / 2f)) / 2f * 360;
            circleTf.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void ProcessMessages()
    {
        if (msgDisplayed)
        {
            if (Time.time - msgStart >= messageDuration)
            {
                if (messages.Count == 0)
                {
                    messageText.gameObject.SetActive(false);
                    messagePanel.gameObject.SetActive(false);
                }
                msgDisplayed = false;
            }
        }
        else if (messages.Count > 0)
        {
            messageText.text = messages[0];
            messagePanel.sizeDelta = new Vector2(messageText.preferredWidth + 20, 50f);
            messagePanel.gameObject.SetActive(true);
            messageText.gameObject.SetActive(true);

            msgDisplayed = true;
            msgStart = Time.time;
            messages.RemoveAt(0);
        }
    }

    public void AddToScoreboard(string entry)
    {
        GameObject obj = Instantiate(playerHighscorePrefab, highscoreContainer);
        obj.GetComponentInChildren<Text>().text = entry;
    }

    public void ShowMessage(string message)
    {
        messages.Add(message);
    }

    public void ClearMessageQueue()
    {
        messages.Clear();
    }
    public void DisplayConnect(bool enabled)
    {
        if (enabled && !displayConnecting)
        {
            connectingTf.gameObject.SetActive(true);
            conStart = Time.time;
        }
        else
        {
            connectingTf.gameObject.SetActive(false);
        }

        displayConnecting = enabled;

    }

    public void DisplayTimer(bool enabled)
    {
        timerTf.gameObject.SetActive(enabled);
    }

    public void UpdateTimer(float time)
    {
        timerText.text = time.ToString("0.0");
    }

    public void DisplayScore(bool enabled)
    {
        scoreTf.gameObject.SetActive(enabled);
    }

    public void UpdateScore(int score)
    {
        scoreText.text = "Score: " + score;
    }

    public void DisplayStartMenu(bool enabled) {
        startTf.gameObject.SetActive(enabled);
    }

    public void DisplayPauseMenu(bool enabled) {
    }

    public void DisplayHighScores(bool enabled, Dictionary<string, int> scores, bool includeInput) {
        if (highscoreContainer == null)
        {
            highscoreContainer = highscoresTf.Find("Panel").Find("List");
        }

        highscoresTf.gameObject.SetActive(enabled);
        if (includeInput)
        {
            highscoreInputTf.gameObject.SetActive(enabled);
        }
        
        foreach (Transform t in highscoreContainer.GetComponentInChildren<Transform>())
        {
            if (t != highscoresTf)
            {
                Destroy(t.gameObject);
            }
        }

        if (enabled)
        {
            foreach (string pName in scores.Keys)
            {
                scores.TryGetValue(pName, out int score);
                AddToScoreboard(pName + "  |  " + score);
            }
        }
    }

    public bool GetHighscoresOpen()
    {
        return highscoresTf.gameObject.activeSelf;
    }

    public void CloseHighscores()
    {
        highscoresTf.gameObject.SetActive(false);
    }
}
