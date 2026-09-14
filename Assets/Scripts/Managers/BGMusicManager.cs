using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BGMusicManager : MonoBehaviour
{
    public static BGMusicManager Instance { get; private set; }

    [Header("Music Settings")]
    [Tooltip("List of background loops to rotate through.")]
    public AudioClip[] musicClips;

    [Tooltip("Time (in seconds) each clip should play before switching. Length should match musicClips.")]
    public float[] musicDurations;

    [Range(0f, 1f)] public float volume = 0.6f;
    public float fadeDuration = 1.5f;
    public bool pauseWithTimeScale = true;
    public bool autoRotate = true;


    public AudioSource currentSource;
    public AudioSource nextSource;
    private bool isFading = false;
    private int currentClipIndex = 0;
    private Coroutine rotationRoutine;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Initialize();
    }

    public void RefernceLookUp()
    {
        
    }

    public void Initialize()
    {
        if (currentSource != null) return; // already initialized

        currentSource = gameObject.AddComponent<AudioSource>();
        nextSource = gameObject.AddComponent<AudioSource>();
        SetupAudioSource(currentSource);
        SetupAudioSource(nextSource);

        // Start first track
        if (musicClips != null && musicClips.Length > 0)
        {
            currentClipIndex = 0;
            currentSource.clip = musicClips[currentClipIndex];
            currentSource.Play();

            if (autoRotate)
                rotationRoutine = StartCoroutine(MusicRotationRoutine());
        }
    }


    private void SetupAudioSource(AudioSource src)
    {
        src.loop = true;
        src.playOnAwake = false;
        src.volume = volume;
    }

    private void Update()
    {
        if (!pauseWithTimeScale) return;

        if (Time.timeScale == 0 && currentSource.isPlaying)
            currentSource.Pause();
        else if (Time.timeScale > 0 && !currentSource.isPlaying && currentSource.clip != null)
            currentSource.UnPause();
    }

    private IEnumerator MusicRotationRoutine()
    {
        while (true)
        {
            float playTime = GetDurationForClip(currentClipIndex);
            yield return new WaitForSecondsRealtime(playTime);

            int nextIndex = (currentClipIndex + 1) % musicClips.Length;
            yield return StartCoroutine(FadeToClip(musicClips[nextIndex]));

            currentClipIndex = nextIndex;
        }
    }

    private IEnumerator FadeToClip(AudioClip newClip)
    {
        if (isFading || newClip == null) yield break;
        isFading = true;

        nextSource.clip = newClip;
        nextSource.volume = 0f;
        nextSource.Play();

        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / fadeDuration;
            currentSource.volume = Mathf.Lerp(volume, 0f, t);
            nextSource.volume = Mathf.Lerp(0f, volume, t);
            yield return null;
        }

        currentSource.Stop();
        var temp = currentSource;
        currentSource = nextSource;
        nextSource = temp;
        currentSource.volume = volume;
        isFading = false;
    }

    private float GetDurationForClip(int index)
    {
        if (musicDurations == null || index >= musicDurations.Length || musicDurations[index] <= 0f)
            return 30f;
        return musicDurations[index];
    }



    //  Manual controls
    public void PlaySpecificClip(int index)
    {
        if (index < 0 || index >= musicClips.Length) return;

        StopRotation();
        StartCoroutine(FadeToClip(musicClips[index]));
        currentClipIndex = index;
    }

    public void StopRotation()
    {
        if (rotationRoutine != null)
        {
            StopCoroutine(rotationRoutine);
            rotationRoutine = null;
        }
    }
}
