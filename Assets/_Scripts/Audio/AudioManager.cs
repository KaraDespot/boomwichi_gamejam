using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum AudioCue
{
    Button,
    Settings,
    DayStarted,
    NewOrder,
    IngredientTake,
    IngredientPlaced,
    IngredientFall,
    Sauce,
    Package,
    GrillOpen,
    GrillClose,
    MoldShake,
    MoldFall,
    CockroachRun,
    CockroachDeath,
    CockroachTouchFood,
    CustomerHappy,
    CustomerSad,
    Tips,
    TimerAlmostGone,
    DayFinished
}

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string SfxResourcePath = "Audio/SFX/";

    [Header("Sources")]
    [SerializeField] private int sfxSourcePoolSize = 8;

    private AudioSource musicSource;
    private AudioSource[] sfxSources;
    private AudioSource moldShakeLoopSource;
    private AudioSource cockroachRunLoopSource;

    private AudioClip buttonClip;
    private AudioClip settingsClip;
    private AudioClip dayStartedClip;
    private AudioClip newOrderClip;
    private AudioClip ingredientTakeClip;
    private AudioClip ingredientPlacedClip;
    private AudioClip ingredientFallClip;
    private AudioClip sauceClip;
    private AudioClip packageClip;
    private AudioClip grillOpenClip;
    private AudioClip grillCloseClip;
    private AudioClip moldShakeClip;
    private AudioClip moldFallClip;
    private AudioClip cockroachRunClip;
    private AudioClip cockroachDeathClip;
    private AudioClip cockroachTouchFoodClip;
    private AudioClip customerHappyClip;
    private AudioClip customerSadClip;
    private AudioClip tipsClip;
    private AudioClip timerAlmostGoneClip;
    private AudioClip dayFinishedClip;

    private int nextSfxSourceIndex;
    private int aliveCockroachSoundCount;
    private bool eventBusBound;

    private float SoundVolume => GameSettings.Load().Sound;
    private float MusicVolume => GameSettings.Load().Music;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateSources();
        LoadClips();
        ApplySettings(GameSettings.Load());
    }

    private void OnEnable()
    {
        GameSettings.OnSettingsApplied += ApplySettings;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryBindEventBus();
    }

    private void OnDisable()
    {
        GameSettings.OnSettingsApplied -= ApplySettings;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindEventBus();
    }

    private void Update()
    {
        if (!eventBusBound)
            TryBindEventBus();

        RefreshCockroachRunLoopState();
    }

    public void PlaySfx(AudioCue cue, float volumeScale = 1f)
    {
        AudioClip clip = GetClip(cue);
        if (clip == null)
            return;

        AudioSource source = GetNextSfxSource();
        if (source == null)
            return;

        source.pitch = 1f;
        source.volume = SoundVolume;
        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null)
            return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = MusicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void StartMoldShakeLoop()
    {
        StartLoop(moldShakeLoopSource, moldShakeClip);
    }

    public void StopMoldShakeLoop()
    {
        StopLoop(moldShakeLoopSource);
    }

    public void StartCockroachRunLoop()
    {
        aliveCockroachSoundCount++;
        StartLoop(cockroachRunLoopSource, cockroachRunClip);
    }

    public void StopCockroachRunLoop()
    {
        aliveCockroachSoundCount = Mathf.Max(0, aliveCockroachSoundCount - 1);
        if (aliveCockroachSoundCount == 0)
            StopLoop(cockroachRunLoopSource);
    }

    public void StopAllCockroachRunLoops()
    {
        aliveCockroachSoundCount = 0;
        StopLoop(cockroachRunLoopSource);
    }

    private void CreateSources()
    {
        musicSource = CreateSource("MusicSource", loop: true);

        int sourceCount = Mathf.Max(1, sfxSourcePoolSize);
        sfxSources = new AudioSource[sourceCount];
        for (int i = 0; i < sourceCount; i++)
            sfxSources[i] = CreateSource($"SfxSource_{i + 1}", loop: false);

        moldShakeLoopSource = CreateSource("MoldShakeLoopSource", loop: true);
        cockroachRunLoopSource = CreateSource("CockroachRunLoopSource", loop: true);
    }

    private AudioSource CreateSource(string sourceName, bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = loop ? MusicVolume : SoundVolume;
        return source;
    }

    private void LoadClips()
    {
        buttonClip = LoadSfx("button");
        settingsClip = LoadSfx("settings");
        dayStartedClip = LoadSfx("daystarted_tutorial");
        newOrderClip = LoadSfx("new_order");
        ingredientTakeClip = LoadSfx("ingredient_take");
        ingredientPlacedClip = LoadSfx("ingredient_placed");
        ingredientFallClip = LoadSfx("ingredient_fall");
        sauceClip = LoadSfx("sauce");
        packageClip = LoadSfx("package");
        grillOpenClip = LoadSfx("grill_open");
        grillCloseClip = LoadSfx("grill_closed");
        moldShakeClip = LoadSfx("mold_shake");
        moldFallClip = LoadSfx("mold_fall");
        cockroachRunClip = LoadSfx("cockroach_run");
        cockroachDeathClip = LoadSfx("cockroach_death");
        cockroachTouchFoodClip = LoadSfx("cockroach_touch_food");
        customerHappyClip = LoadSfx("customer_happy");
        customerSadClip = LoadSfx("customer_sad");
        tipsClip = LoadSfx("tips");
        timerAlmostGoneClip = LoadSfx("timer_almost_gone");
        dayFinishedClip = LoadSfx("day_finished2");
    }

    private static AudioClip LoadSfx(string clipName)
    {
        return Resources.Load<AudioClip>(SfxResourcePath + clipName);
    }

    private AudioSource GetNextSfxSource()
    {
        if (sfxSources == null || sfxSources.Length == 0)
            return null;

        AudioSource source = sfxSources[nextSfxSourceIndex];
        nextSfxSourceIndex = (nextSfxSourceIndex + 1) % sfxSources.Length;
        return source;
    }

    private AudioClip GetClip(AudioCue cue)
    {
        return cue switch
        {
            AudioCue.Button => buttonClip,
            AudioCue.Settings => settingsClip,
            AudioCue.DayStarted => dayStartedClip,
            AudioCue.NewOrder => newOrderClip,
            AudioCue.IngredientTake => ingredientTakeClip,
            AudioCue.IngredientPlaced => ingredientPlacedClip,
            AudioCue.IngredientFall => ingredientFallClip,
            AudioCue.Sauce => sauceClip,
            AudioCue.Package => packageClip,
            AudioCue.GrillOpen => grillOpenClip,
            AudioCue.GrillClose => grillCloseClip,
            AudioCue.MoldShake => moldShakeClip,
            AudioCue.MoldFall => moldFallClip,
            AudioCue.CockroachRun => cockroachRunClip,
            AudioCue.CockroachDeath => cockroachDeathClip,
            AudioCue.CockroachTouchFood => cockroachTouchFoodClip,
            AudioCue.CustomerHappy => customerHappyClip,
            AudioCue.CustomerSad => customerSadClip,
            AudioCue.Tips => tipsClip,
            AudioCue.TimerAlmostGone => timerAlmostGoneClip,
            AudioCue.DayFinished => dayFinishedClip,
            _ => null
        };
    }

    private void StartLoop(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null)
            return;

        if (source.isPlaying && source.clip == clip)
            return;

        source.clip = clip;
        source.loop = true;
        source.volume = SoundVolume;
        source.Play();
    }

    private static void StopLoop(AudioSource source)
    {
        if (source != null)
            source.Stop();
    }

    private void ApplySettings(GameSettings.Data data)
    {
        if (musicSource != null)
            musicSource.volume = data.Music;

        if (sfxSources != null)
        {
            for (int i = 0; i < sfxSources.Length; i++)
            {
                if (sfxSources[i] != null)
                    sfxSources[i].volume = data.Sound;
            }
        }

        if (moldShakeLoopSource != null)
            moldShakeLoopSource.volume = data.Sound;

        if (cockroachRunLoopSource != null)
            cockroachRunLoopSource.volume = data.Sound;
    }

    private void TryBindEventBus()
    {
        if (eventBusBound || EventBus.Instance == null)
            return;

        EventBus.Instance.OnOrderStarted += HandleOrderStarted;
        EventBus.Instance.OnOrderCompleted += HandleOrderCompleted;
        EventBus.Instance.OnOrderFailed += HandleOrderFailed;
        EventBus.Instance.OnDayFinished += HandleDayFinished;
        EventBus.Instance.OnCustomerTimeWarning += HandleCustomerTimeWarning;
        EventBus.Instance.OnCockroachSpawned += HandleCockroachSpawned;
        EventBus.Instance.OnCockroachKilled += HandleCockroachKilled;
        EventBus.Instance.OnMoldFallenOnSandwich += HandleMoldFallenOnSandwich;
        EventBus.Instance.OnSandwichCockroachContaminated += HandleSandwichCockroachContaminated;
        EventBus.Instance.OnGamePaused += HandleGamePaused;
        EventBus.Instance.OnGameResumed += HandleGameResumed;
        eventBusBound = true;
    }

    private void UnbindEventBus()
    {
        if (!eventBusBound || EventBus.Instance == null)
        {
            eventBusBound = false;
            return;
        }

        EventBus.Instance.OnOrderStarted -= HandleOrderStarted;
        EventBus.Instance.OnOrderCompleted -= HandleOrderCompleted;
        EventBus.Instance.OnOrderFailed -= HandleOrderFailed;
        EventBus.Instance.OnDayFinished -= HandleDayFinished;
        EventBus.Instance.OnCustomerTimeWarning -= HandleCustomerTimeWarning;
        EventBus.Instance.OnCockroachSpawned -= HandleCockroachSpawned;
        EventBus.Instance.OnCockroachKilled -= HandleCockroachKilled;
        EventBus.Instance.OnMoldFallenOnSandwich -= HandleMoldFallenOnSandwich;
        EventBus.Instance.OnSandwichCockroachContaminated -= HandleSandwichCockroachContaminated;
        EventBus.Instance.OnGamePaused -= HandleGamePaused;
        EventBus.Instance.OnGameResumed -= HandleGameResumed;
        eventBusBound = false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCockroachRunLoops();
        ApplySettings(GameSettings.Load());
    }

    private void RefreshCockroachRunLoopState()
    {
        if (aliveCockroachSoundCount <= 0)
            return;

        if (Cockroach.ActiveInstances == null || Cockroach.ActiveInstances.Count == 0)
            StopAllCockroachRunLoops();
    }

    private void HandleOrderStarted(int orderIndex, OrderDataAsset order)
    {
        PlaySfx(AudioCue.NewOrder);
    }

    private void HandleOrderCompleted(int orderIndex, int tipsEarned)
    {
        if (tipsEarned > 0)
        {
            PlaySfx(AudioCue.CustomerHappy);
            PlaySfx(AudioCue.Tips);
            return;
        }

        PlaySfx(AudioCue.CustomerSad);
    }

    private void HandleOrderFailed(int orderIndex)
    {
        PlaySfx(AudioCue.CustomerSad);
    }

    private void HandleCustomerTimeWarning()
    {
        PlaySfx(AudioCue.TimerAlmostGone);
    }

    private void HandleDayFinished()
    {
        StopAllCockroachRunLoops();
        StopMoldShakeLoop();
        PlaySfx(AudioCue.DayFinished);
    }

    private void HandleCockroachSpawned(Cockroach cockroach)
    {
        StartCockroachRunLoop();
    }

    private void HandleCockroachKilled(Cockroach cockroach)
    {
        PlaySfx(AudioCue.CockroachDeath);
        StopCockroachRunLoop();
    }

    private void HandleMoldFallenOnSandwich(SandwichState sandwichState)
    {
        PlaySfx(AudioCue.MoldFall);
    }

    private void HandleSandwichCockroachContaminated(SandwichState sandwichState)
    {
        PlaySfx(AudioCue.CockroachTouchFood);
    }

    private void HandleGamePaused()
    {
        StopMoldShakeLoop();
    }

    private void HandleGameResumed()
    {
        ApplySettings(GameSettings.Load());
    }
}
