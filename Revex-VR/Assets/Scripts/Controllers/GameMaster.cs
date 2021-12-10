using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
    public enum GameState
    {
        init,               // Base state
        connectingRevEx,    // Waiting for revex device
        needsCalibrate,     // Need to calibrate for the first time
        startMenu,          // Now in start menu
        playStart,          // Play has been selected, now select mode
        paused,             // Player can pause with menu options
        playing,            // Currently playing the game
        ended               // Game has ended, leads back to start menu
    }

    public UIController uiControl;
    public NinjaArmController armControl;

    public GameObject[] fruitChoices;

    private GameState currentState = GameState.init;

    private List<GameObject> fruits = new List<GameObject>();
    private Ray leftEdge, rightEdge;

    // Start is called before the first frame update
    void Start()
    {
        // Init here
        Camera mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        leftEdge = mainCam.ScreenPointToRay(new Vector3(0f, mainCam.pixelHeight / 2f));
        rightEdge = mainCam.ScreenPointToRay(new Vector3(mainCam.pixelWidth, mainCam.pixelHeight / 2f));

        currentState = GameState.connectingRevEx;   // We are now waiting for revex
        uiControl.DisplayConnect(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == GameState.connectingRevEx)
        {
            if (armControl._status == PsscDeviceStatus.ArmEstimation)   // We have succesfully connected
            {
                currentState = GameState.needsCalibrate;    // Always assume we calibrate on reconnect
                uiControl.ShowMessage("RevEx device connected!");
                uiControl.DisplayConnect(false);
            }
        }

        // Temp
        if (Input.GetKeyDown(KeyCode.F))
        {
            //DeleteFirstFruit(); // Just to cycle thru
            fruits.Add(createRandomFruit());
        }

        CheckFruits();
    }

    private void CheckFruits()
    {
        List<GameObject> toRemove = new List<GameObject>();

        foreach (GameObject g in fruits)
        {
            if (g.transform.position.y <= -1f)
            {
                toRemove.Add(g);
            }
        }

        foreach (GameObject g in armControl.fruitsHit)
        {
            toRemove.Add(g);
        }

        // Delete any uneeded fruits
        foreach (GameObject g in toRemove)
        {
            if (fruits.Find(x => g))
            {
                fruits.Remove(g);
            }
            else if (armControl.fruitsHit.Find(x => g))
            {
                armControl.fruitsHit.Remove(g); // Pop this one
            }

            
            Destroy(g, .1f);
        }
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
        float radius = Random.Range(.6f, 1.25f);
        float angle = Random.Range(20f, 160f);
        //Vector3 finalLanding = new Vector3(radius * Mathf.Cos(angle * Mathf.Deg2Rad), .05f, radius * Mathf.Sin(angle * Mathf.Deg2Rad));
        Vector3 finalLanding = GameObject.Find("HITTHIS").transform.position;

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
}
