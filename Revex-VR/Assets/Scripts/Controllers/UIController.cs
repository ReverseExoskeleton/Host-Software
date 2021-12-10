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

    public void ShowMessage(string message)
    {
        messages.Add(message);
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

    public void DisplayStartMenu(bool enabled) {
        // ... some other stuff

        DisplayHighScores(enabled);

        // ... some other stuff
    }

    public void DisplayPauseMenu(bool enabled) {
    }

    public void DisplayGameModeSelect(bool enabled) {

    }

    public void DisplayHighScores(bool enabled, int userScore = NoNewUserScore) {
      if (userScore != NoNewUserScore) {
        // Get user name ...
        //HighScores.AddNew(userName, userScore);
      }

      // Populate the high score board on the screen 
      //HighScores.GiveMeTheScores();
      //...

      if (userScore != NoNewUserScore) {
        // More scores??
        // Also text box below board showing how shit they are (ranking)
      }
    }
}
