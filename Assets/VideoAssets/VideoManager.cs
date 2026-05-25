using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class VideoManager : MonoBehaviour
{
    const float PrepareTimeoutSeconds = 8f;

    [Header("UI & Video")]
    public GameObject videoPanel;
    public VideoPlayer videoPlayer;
    public RawImage videoScreen;
    public Slider videoSlider;
    public Button playPauseButton;
    public Sprite playIcon;
    public Sprite pauseIcon;
    private Image playPauseIconImage;
    public CanvasGroup playPauseCanvasGroup;
    public GameObject bufferingIndicator;
    private Coroutine bgmFadeCoroutine;
    private Coroutine fadeCoroutine;

    private AudioSource audiobgm;
    private AudioSource audioSource;
    private bool isDragging = false;
    private bool isReadyToPlay = false;

    void Awake()
    {
        CacheReferences();
        ConfigureVideoPlayerAudioOutput();
        videoPlayer.errorReceived += (vp, msg) => Debug.LogError("Video Error: " + msg);
    }

    void Update()
    {
        if (!isReadyToPlay || isDragging || !videoPlayer.isPlaying || videoPlayer.length <= 0)
            return;

        videoSlider.value = (float)(videoPlayer.time / videoPlayer.length);
    }

    public void PlayVideoClip(VideoClip clip)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.LogWarning("PlayVideoClip dipanggil di WebGL. Gunakan PlayVideoUrl untuk kompatibilitas WebGL.");
        }

        PlayVideoInternal(clip);
    }

    public void PlayVideoUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogError("PlayVideoUrl gagal: URL kosong.");
            return;
        }

        if (!CacheReferences())
        {
            return;
        }

        StopAllCoroutines();
        isReadyToPlay = false;

        ResetPlaybackToStart();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = ResolveVideoUrl(url);
        ConfigureVideoPlayerAudioOutput();

        videoPanel.SetActive(true);
        StartCoroutine(PrepareAndPlay());
    }

    void PlayVideoInternal(VideoClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("PlayVideo gagal: VideoClip null.");
            return;
        }

        if (!CacheReferences())
        {
            return;
        }

        StopAllCoroutines();
        isReadyToPlay = false;

        ResetPlaybackToStart();
        videoPlayer.clip = clip;
        ConfigureVideoPlayerAudioOutput();

        videoPanel.SetActive(true);
        StartCoroutine(PrepareAndPlay());
    }

    private IEnumerator PrepareAndPlay()
    {
        videoSlider.interactable = false;
        ConfigureVideoPlayerAudioOutput();
        videoPlayer.Prepare();

        float elapsed = 0f;
        while (!videoPlayer.isPrepared)
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= PrepareTimeoutSeconds)
            {
                Debug.LogError("Video gagal di-prepare dalam batas waktu. Periksa clip, codec, dan konfigurasi audio output VideoPlayer.");
                HandlePlaybackPreparationFailure();
                yield break;
            }

            yield return null;
        }

        videoPlayer.time = 0d;
        videoPlayer.frame = 0;
        videoPlayer.Play();
        yield return null; // frame delay
        videoSlider.interactable = true;

        // ✅ Fade BGM saat play pertama kali
        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(FadeAudioVolume(audiobgm, 0.18f));

        videoSlider.value = 0f;
        isReadyToPlay = true;
        UpdatePlayPauseIcon();

        // Auto-hide play/pause button
        if (playPauseCanvasGroup != null)
        {
            playPauseCanvasGroup.alpha = 1f;
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutPlayPauseIcon(2f));
        }

        // Register end-of-video callback (single subscription)
        videoPlayer.loopPointReached -= OnVideoEnd;
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    public void TogglePlayPause()
    {
        if (!isReadyToPlay) return;

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();

            // Fade in BGM saat pause
            if (bgmFadeCoroutine != null)
                StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(FadeAudioVolume(audiobgm, 1f));
        }
        else
        {
            videoPlayer.Play();

            // Fade out BGM saat play
            if (bgmFadeCoroutine != null)
                StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(FadeAudioVolume(audiobgm, 0.18f));
        }

        UpdatePlayPauseIcon();

        if (playPauseCanvasGroup != null)
        {
            playPauseCanvasGroup.alpha = 1f;
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutPlayPauseIcon(2f));
        }
    }
    private void UpdatePlayPauseIcon()
    {
        if (playPauseButton == null) return;

        if (playPauseIconImage != null)
        {
            playPauseIconImage.sprite = videoPlayer.isPlaying ? pauseIcon : playIcon;
        }
    }



    public void OnSliderBeginDrag()
    {
        isDragging = true;
    }

    public void OnSliderEndDrag()
    {
        isDragging = false;

        // Pastikan time di-set ke waktu yang dipilih setelah selesai drag
        StartCoroutine(SeekWithBuffering());
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        // Kembalikan volume BGM
        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(FadeAudioVolume(audiobgm, 1f));
    }

    private IEnumerator SeekWithBuffering()
    {
        if (bufferingIndicator != null)
            bufferingIndicator.SetActive(true);

        double newTime = videoSlider.value * videoPlayer.length;
        videoPlayer.time = newTime;

        // Tunggu sampai posisi video benar-benar update
        yield return null;

        // Tunggu hingga videoPlayer mulai main lagi
        while (!videoPlayer.isPlaying)
            yield return null;

        if (bufferingIndicator != null)
            bufferingIndicator.SetActive(false);
    }

    public void OnSliderValueChanged(float value)
    {
        // Jangan ubah time di sini saat dragging — tunggu hingga drag selesai
        if (isDragging)
        {
            // Optional: update preview UI atau waktu
        }
    }

    public void BackToThumbnails()
    {
        ResetPlaybackToStart();
        videoPanel.SetActive(false);
        isReadyToPlay = false;
        bgmFadeCoroutine = StartCoroutine(FadeAudioVolume(audiobgm, 1f));
        if (playPauseCanvasGroup != null)
        {
            playPauseCanvasGroup.alpha = 1f;
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
        }
    }

    private bool CacheReferences()
    {
        if (audiobgm == null)
        {
            GameObject holder = GameObject.Find("holder");
            if (holder != null)
            {
                audiobgm = holder.GetComponent<AudioSource>();
            }
        }

        if (videoPlayer == null)
        {
            Debug.LogError("VideoManager: VideoPlayer belum di-assign.");
            return false;
        }

        if (audioSource == null)
        {
            audioSource = videoPlayer.GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            Debug.LogError("VideoManager: AudioSource pada object VideoPlayer tidak ditemukan.");
            return false;
        }

        if (playPauseButton != null && playPauseIconImage == null)
        {
            playPauseIconImage = playPauseButton.gameObject.GetComponentInChildren<Image>();
        }

        return true;
    }

    private void ConfigureVideoPlayerAudioOutput()
    {
        if (videoPlayer == null || audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
    }

    string ResolveVideoUrl(string rawUrl)
    {
        string trimmed = rawUrl.Trim();

        if (trimmed.Contains("://"))
        {
            return trimmed;
        }

        return Application.streamingAssetsPath.TrimEnd('/') + "/" + trimmed.TrimStart('/');
    }

    private void HandlePlaybackPreparationFailure()
    {
        if (videoPlayer != null && videoPlayer.clip != null)
        {
            Debug.LogError("Prepare timeout untuk clip: " + videoPlayer.clip.name + ". Cek import setting clip (transcode Standalone/Windows) dan codec source video.");
        }

        if (bufferingIndicator != null)
        {
            bufferingIndicator.SetActive(false);
        }

        if (videoSlider != null)
        {
            videoSlider.interactable = true;
        }

        BackToThumbnails();
    }

    private void ResetPlaybackToStart()
    {
        videoPlayer.loopPointReached -= OnVideoEnd;
        videoPlayer.Stop();
        videoPlayer.time = 0d;
        videoPlayer.frame = 0;

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.time = 0f;
        }

        if (videoSlider != null)
        {
            videoSlider.value = 0f;
        }
    }
    
    private IEnumerator FadeOutPlayPauseIcon(float delay, float fadeDuration = 0.5f)
    {
        yield return new WaitForSeconds(delay);

        float startAlpha = playPauseCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            playPauseCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, time / fadeDuration);
            yield return null;
        }

        playPauseCanvasGroup.alpha = 0f;
    }
    private IEnumerator FadeAudioVolume(AudioSource audio, float targetVolume, float duration = 0.5f)
    {
        float startVolume = audio.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            audio.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            yield return null;
        }

        audio.volume = targetVolume;
    }
}
