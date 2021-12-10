using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public enum SoundType
    {
        roundStart,
        gameOver,
        thump,
        slap,
        positive,
        ninja,
        samurai,
        squish,
        weird
    }

    public GameObject audioSourcePrefab;
    private Vector3 camPos;

    [Header("Audio Files")]
    public AudioClip mainMusic;
    public AudioClip[] roundStart;
    public AudioClip gameOver;
    public AudioClip[] thump;
    public AudioClip[] slap;
    public AudioClip[] positive;
    public AudioClip ninja;
    public AudioClip samurai;
    public AudioClip[] squish;
    public AudioClip[] weird;

    private AudioSource mainSource;

    // Start is called before the first frame update
    void Start()
    {
        camPos = GameObject.FindGameObjectWithTag("MainCamera").transform.position;

        GameObject newObj = Instantiate(audioSourcePrefab, camPos, Quaternion.identity);
        mainSource = newObj.GetComponent<AudioSource>();
        mainSource.clip = mainMusic;
        mainSource.loop = true;

        SetMainMusicVolume(.15f);
        PlayMainMusic();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlaySound(SoundType.roundStart);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlaySound(SoundType.gameOver);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlaySound(SoundType.thump);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PlaySound(SoundType.slap);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            PlaySound(SoundType.positive);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            PlaySound(SoundType.ninja);
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            PlaySound(SoundType.samurai);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            PlaySound(SoundType.squish);
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            PlaySound(SoundType.weird);
        }
    }

    private AudioClip SelectRandom(AudioClip[] array)
    {
        int choice = Mathf.Min(array.Length - 1, (int)(Random.value * array.Length));
        return array[choice];
    }

    public void PlaySound(SoundType type)
    {
        PlaySound(type, camPos);
    }

    public void PlaySound(SoundType type, Vector3 position)
    {
        AudioClip clip = null;

        switch (type)
        {
            case SoundType.roundStart:
                clip = SelectRandom(roundStart);
                break;
            case SoundType.gameOver:
                clip = gameOver;
                break;
            case SoundType.thump:
                clip = SelectRandom(thump);
                break;
            case SoundType.slap:
                clip = SelectRandom(slap);
                break;
            case SoundType.positive:
                clip = SelectRandom(positive);
                break;
            case SoundType.ninja:
                clip = ninja;
                break;
            case SoundType.samurai:
                clip = samurai;
                break;
            case SoundType.squish:
                clip = SelectRandom(squish);
                break;
            case SoundType.weird:
                clip = SelectRandom(weird);
                break;
        }

        GameObject newObj = Instantiate(audioSourcePrefab, camPos, Quaternion.identity);
        AudioSource source = newObj.GetComponent<AudioSource>();
        source.clip = clip;
        source.Play();
        Destroy(newObj, clip.length + .5f);
    }

    public void PlayMainMusic()
    {
        mainSource.Play();
    }

    public void StopMainMusic()
    {
        mainSource.Stop();
    }

    public void SetMainMusicVolume(float vol)
    {
        mainSource.volume = vol;
    }
}
