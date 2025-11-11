using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip flipClip;
    public AudioClip matchClip;
    public AudioClip missClip;
    public AudioClip gameOverClip;   // time-up / lose
    public AudioClip winClip;        // NEW: win jingle

    AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        if (!src) src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
    }

    public void Flip()
    {
        if (flipClip) src.PlayOneShot(flipClip);
    }

    public void Match(int combo)
    {
        // you could vary pitch/volume with combo if you want
        if (matchClip) src.PlayOneShot(matchClip);
    }

    public void Miss()
    {
        if (missClip) src.PlayOneShot(missClip);
    }

    public void GameOver()
    {
        if (gameOverClip) src.PlayOneShot(gameOverClip);
    }

    public void Win()   // NEW
    {
        if (winClip) src.PlayOneShot(winClip);
    }
}
