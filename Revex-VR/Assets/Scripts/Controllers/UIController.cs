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
}
