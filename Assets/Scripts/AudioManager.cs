using UnityEngine;
using System.Collections.Generic;

// Audio Manager implementing Singleton pattern and Observer pattern
public class AudioManager : MonoBehaviour, IGameObserver
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip victoryMusic;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip diceRollSFX;
    [SerializeField] private AudioClip pieceMoveySFX;
    [SerializeField] private AudioClip knockoutSFX;
    [SerializeField] private AudioClip victorySFX;
    [SerializeField] private AudioClip buttonClickSFX;

    private Dictionary<string, AudioClip> sfxClips;
    private float musicVolume = 0.7f;
    private float sfxVolume = 0.7f;
    private bool isMuted = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Register as observer
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterObserver(this);
        }

        PlayMainMenuMusic();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterObserver(this);
        }
    }

    private void InitializeAudio()
    {
        // Initialize SFX dictionary for easy access
        sfxClips = new Dictionary<string, AudioClip>
        {
            { "DiceRoll", diceRollSFX },
            { "PieceMove", pieceMoveySFX },
            { "Knockout", knockoutSFX },
            { "Victory", victorySFX },
            { "ButtonClick", buttonClickSFX }
        };

        // Load saved audio settings
        LoadAudioSettings();
    }

    private void LoadAudioSettings()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.7f);
        isMuted = PlayerPrefs.GetInt("Mute", 0) == 1;

        ApplyAudioSettings();
    }

    private void ApplyAudioSettings()
    {
        if (musicSource != null)
        {
            musicSource.volume = isMuted ? 0 : musicVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = isMuted ? 0 : sfxVolume;
        }
    }

    #region Public Audio Control Methods

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null && !isMuted)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null && !isMuted)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    public void SetMute(bool mute)
    {
        isMuted = mute;
        ApplyAudioSettings();
    }

    public void PlaySFX(string clipName)
    {
        if (sfxSource != null && sfxClips.ContainsKey(clipName) && !isMuted)
        {
            sfxSource.PlayOneShot(sfxClips[clipName]);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null && !isMuted)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region Music Control

    public void PlayMainMenuMusic()
    {
        PlayMusic(mainMenuMusic);
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }

    public void PlayVictoryMusic()
    {
        PlayMusic(victoryMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource != null && clip != null)
        {
            if (musicSource.clip != clip)
            {
                musicSource.clip = clip;
                musicSource.Play();
            }
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (musicSource != null)
        {
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }

    #endregion

    #region IGameObserver Implementation

    public void OnPlayerTurnChanged(PlayerColor currentPlayer)
    {
        // Optional: Play turn change sound
        // PlaySFX("TurnChange");
    }

    public void OnDiceRolled(int value)
    {
        PlaySFX("DiceRoll");
    }

    public void OnPieceMoved(IPlayerPiece piece, int steps)
    {
        PlaySFX("PieceMove");
    }

    public void OnPieceKnockedOut(IPlayerPiece knockedPiece, IPlayerPiece attackingPiece)
    {
        PlaySFX("Knockout");
    }

    public void OnGameWon(PlayerColor winner)
    {
        PlaySFX("Victory");
        PlayVictoryMusic();
    }

    #endregion

    #region UI Button Sounds

    public void PlayButtonClickSound()
    {
        PlaySFX("ButtonClick");
    }

    #endregion
}

// Extension for easy button sound integration
public static class ButtonAudioExtensions
{
    public static void AddClickSound(this UnityEngine.UI.Button button)
    {
        button.onClick.AddListener(() => AudioManager.Instance?.PlayButtonClickSound());
    }
}