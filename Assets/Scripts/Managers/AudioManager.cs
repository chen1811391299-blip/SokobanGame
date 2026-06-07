using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Clips")]
    public AudioClip sfxMove;
    public AudioClip sfxPush;
    public AudioClip sfxUndo;
    public AudioClip sfxComplete;
    public AudioClip sfxMenu;

    [Header("BGM Clips（可先留空）")]
    public AudioClip bgmMainMenu;
    public AudioClip bgmGameplay;
    public AudioClip bgmEditor;

    [Range(0f, 1f)]
    public float bgmVolume = 0.4f;

    private AudioSource _sfxSrc;
    private AudioSource _bgmSrc;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfxSrc             = gameObject.AddComponent<AudioSource>();
        _sfxSrc.playOnAwake = false;

        _bgmSrc             = gameObject.AddComponent<AudioSource>();
        _bgmSrc.loop        = true;
        _bgmSrc.playOnAwake = false;
        _bgmSrc.volume      = bgmVolume;
    }

    // SFX
    public static void PlayMove()     => Instance?._PlaySFX(Instance.sfxMove,     0.55f);
    public static void PlayPush()     => Instance?._PlaySFX(Instance.sfxPush,     0.80f);
    public static void PlayUndo()     => Instance?._PlaySFX(Instance.sfxUndo,     0.50f);
    public static void PlayComplete() => Instance?._PlaySFX(Instance.sfxComplete, 1.00f);
    public static void PlayMenu()     => Instance?._PlaySFX(Instance.sfxMenu,     0.70f);

    // BGM
    public static void PlayBGM(AudioClip clip)  => Instance?._PlayBGM(clip);
    public static void StopBGM()                => Instance?._bgmSrc?.Stop();
    public static void SetBGMVolume(float vol)
    {
        if (Instance != null) Instance._bgmSrc.volume = Mathf.Clamp01(vol);
    }

    private void _PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (_bgmSrc.clip == clip && _bgmSrc.isPlaying) return;
        _bgmSrc.clip = clip;
        _bgmSrc.Play();
    }

    private void _PlaySFX(AudioClip clip, float vol)
    {
        if (clip) _sfxSrc.PlayOneShot(clip, vol);
    }
}
