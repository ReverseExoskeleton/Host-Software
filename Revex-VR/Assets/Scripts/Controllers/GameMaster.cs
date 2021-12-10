using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour
{
    public enum GameState
    {
        init,               // Base state
        connectingRevEx,    // Waiting for revex device
        needsCalibrate,     // Need to calibrate for the first time
        startMenu,          // Now in start menu
        paused,             // Player can pause with menu options
        playing,            // Currently playing the game
        ended               // Game has ended, leads back to start menu
    }

    public UIController uiControl;
    public NinjaArmController armControl;
    public AudioController audioController;

    public GameObject fruitAccelerator;
    public GameObject[] fruitChoices;
    private List<GameObject> fruits = new List<GameObject>();
    private Ray leftEdge, rightEdge;

    public int userScore;
    private Scores allScores;

    public float roundLength = 60f;

    private float nextFruit;
    private float roundStart;
    private bool started = false;
    private GameState currentState = GameState.init;
    private GameState prevState = (GameState)(-1);

    // Start is called before the first frame update
    void Start()
    {
        // Init here
        Camera mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        leftEdge = mainCam.ScreenPointToRay(new Vector3(0f, mainCam.pixelHeight / 2f));
        rightEdge = mainCam.ScreenPointToRay(new Vector3(mainCam.pixelWidth, mainCam.pixelHeight / 2f));

        allScores = new Scores(Application.dataPath);

        currentState = GameState.connectingRevEx;   // We are now waiting for revex
        uiControl.DisplayConnect(true);
        uiControl.DisplayHighScores(true, allScores.GetTopScores(10), false);
    }

    // Update is called once per frame
    void Update()
    {
        //if (armControl.status != DeviceStatus.ArmEstimation) {
        //    currentState = GameState.connectingRevEx;
        //}

        switch (currentState)
        {
            case GameState.connectingRevEx:
                if (armControl.status != DeviceStatus.ArmEstimation) break;
                // We have succesfully connected
                uiControl.ShowMessage("RevEx device connected!");
                uiControl.DisplayConnect(false);

                // Always calibrate on reconnect
                currentState = GameState.needsCalibrate; 
                break;
            case GameState.needsCalibrate:
                uiControl.ShowMessage("Please face away from the monitor.");
                if (!Input.GetKeyDown(KeyCode.Space)) break;
                armControl.Recalibrate();

                if (prevState == GameState.startMenu || prevState == GameState.paused) {
                  currentState = prevState;
                  break;
                }
                throw new System.Exception("Called recalibrate from unknown state");
            case GameState.startMenu:
                uiControl.DisplayStartMenu(true);
                // Note: Goes to  `needsCalibration` or `playStart` via
                //       `Recalibrate` or `GameModeSelectMenu`, respectively
                break;
            case GameState.paused:
                uiControl.DisplayPauseMenu(true);
                // Note: Goes to `startMenu` or `playing` state via
                //       `BackToStartMenu` or `ResumeGame`, respectively
                break;
            case GameState.playing:
                if (started)
                {
                    CheckFruits();

                    uiControl.UpdateTimer(roundStart + roundLength - Time.time);
                    
                    if (Time.time >= nextFruit)
                    {
                        fruits.Add(createRandomFruit());
                        nextFruit = Time.time + Random.Range(1f, 2f);
                    }

                    if (Time.time - roundStart >= roundLength)
                    {
                        started = false;
                        CleanupFruits();
                        uiControl.UpdateTimer(0f);
                        currentState = GameState.ended;
                        audioController.PlaySound(AudioController.SoundType.gameOver);

                        uiControl.DisplayScore(false);
                        uiControl.DisplayTimer(false);
                    }
                    
                    uiControl.UpdateScore(userScore);
                }
                else
                {
                    if (Time.time > roundStart)
                    {
                        started = true;
                        nextFruit = Time.time + .5f;
                        audioController.PlaySound(AudioController.SoundType.roundStart);
                        uiControl.UpdateTimer(roundLength);
                        uiControl.UpdateScore(0);
                        uiControl.DisplayScore(true);
                    }
                    else
                    {
                        uiControl.UpdateTimer(Time.time - roundStart);
                    }
                }
                
                if (Input.GetKeyDown(KeyCode.F))
                {
                    fruits.Add(createRandomFruit());
                }
                break;
            case GameState.ended:
                if (!uiControl.GetHighscoresOpen())
                {
                    uiControl.DisplayHighScores(true, allScores.GetTopScores(10), true);
                }
                break;
            default:
                throw new System.Exception($"Unknown case {currentState}.");
        }

        // Exit game
        if (Input.GetKeyDown(KeyCode.Escape)) {
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        }
    }

    public void Recalibrate() {
        audioController.PlaySound(AudioController.SoundType.thump);
        armControl.Recalibrate();
    }

    public void BackToStartMenu() {
        audioController.PlayMainMusic();
        if (currentState == GameState.ended) {
            uiControl.DisplayHighScores(false, null, true);
        } else if (currentState == GameState.paused) {
            uiControl.DisplayPauseMenu(false);
        }
        currentState = GameState.startMenu;
        uiControl.DisplayHighScores(true, allScores.GetTopScores(10), false);
    }

    public void ResumeGame() {
        uiControl.DisplayPauseMenu(false);
        currentState = GameState.playing;
    }

    public void AddNewHighscore(GameObject textfield)
    {
        allScores.AddScore(textfield.GetComponent<InputField>().text, userScore);
        uiControl.AddToScoreboard(textfield.GetComponent<InputField>().text + "  |  " + userScore);
    }

    public void StartGame() {
        userScore = 0;
        audioController.StopMainMusic();
        audioController.PlaySound(AudioController.SoundType.thump);
        uiControl.DisplayStartMenu(false);
        currentState = GameState.playing;

        roundStart = Time.time + 3f;
        started = false;
        uiControl.DisplayTimer(true);
        uiControl.UpdateTimer(Time.time - roundStart);
    }

    private void CleanupFruits()
    {
        foreach (GameObject g in fruits)
        {
            Destroy(g);
        }
        fruits.Clear();
    }

    private void CheckFruits()
    {
        List<GameObject> fallen = new List<GameObject>();

        foreach (GameObject g in fruits)
        {
            if (g.transform.position.y <= -1f)
            {
                fallen.Add(g);
            }
        }

        // Delete any uneeded fruits
        foreach (GameObject g in fallen)
        {
            fruits.Remove(g);
            Destroy(g);
        }
        
        foreach (GameObject g in armControl.fruitsHit)
        {
            fruits.Remove(g);
            PopFruit(g);
            Destroy(g, .05f);
        }
        armControl.ClearFruitsHit();
    }

    private void PopFruit(GameObject fruit)
    {
        GameObject emitter = Instantiate(fruitAccelerator);
        emitter.transform.position = fruit.transform.position + fruit.GetComponent<Rigidbody>().velocity * .05f;
        audioController.PlaySound(AudioController.SoundType.squish);
        userScore++;
        armControl.HapticBurst(burstPeriodS: .5f, dutyCyclePrcnt: 0.75f, frequencyPrcnt: 0.75f);

        Destroy(emitter, 1f);
    }

    private GameObject createRandomFruit()
    {
        int side = Random.value >= .5f ? -1 : 1;
        
        Quaternion init_rot = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
        Vector3 ang_vel = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(-2.5f, 2.5f), Random.Range(-2.5f, 2.5f));

        // Calculate start point
        Vector3 init_pos;
        float z_dist = Random.Range(1.5f, 2.5f);
        if (side == -1) // Left Side
        {
            init_pos = leftEdge.GetPoint(z_dist) - .15f * Vector3.right;
        }
        else // Right side
        {
            init_pos = rightEdge.GetPoint(z_dist) + .15f * Vector3.right;
        }
        init_pos.y = .05f;  // Standard starting height

        // Calculate final intersection point
        float radius = Random.Range(.7f, 1.05f);
        float angle = Random.Range(30f, 150f);
        Vector3 finalLanding = new Vector3(radius * Mathf.Cos(angle * Mathf.Deg2Rad), .05f, radius * Mathf.Sin(angle * Mathf.Deg2Rad));
        //Vector3 finalLanding = GameObject.Find("HITTHIS").transform.position;

        // If it's too short switch sides
        float minDistanceThreshold = 0.4f;
        if ((finalLanding - init_pos).magnitude < minDistanceThreshold) // Let's switch sides so its not a short throw
        {
            if (side == -1) { init_pos = rightEdge.GetPoint(z_dist) + .15f * Vector3.right; }
            else { init_pos = leftEdge.GetPoint(z_dist) - .15f * Vector3.right; }
        }

        // Initaial velocity calculations
        Vector3 distance = (finalLanding - init_pos);
        float distanceMag = distance.magnitude;
        float init_y_vel = Random.Range(1f, 1.75f);
        float landing_time = -init_y_vel / Physics.gravity.y * 2f;

        Vector3 init_vel = (finalLanding - init_pos).normalized * distanceMag / landing_time;
        init_vel.y = init_y_vel;

        int choice = (int)Mathf.Min(fruitChoices.Length - 1, Random.value * fruitChoices.Length);
        GameObject g = Instantiate(fruitChoices[choice], init_pos, init_rot);

        g.GetComponent<Rigidbody>().angularVelocity = ang_vel;
        g.GetComponent<Rigidbody>().velocity = init_vel;

        return g;
    }

    private void OnApplicationQuit()
    {
        allScores.WriteToFile();
    }
}
