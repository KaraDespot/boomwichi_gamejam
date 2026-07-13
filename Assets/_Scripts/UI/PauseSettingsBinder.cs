using UnityEngine;
using UnityEngine.UI;

/*
 * PauseSettingsBinder
 * Purpose: binds the pause menu sound/music sliders to the shared settings system.
 * Notes: browser build settings only expose audio controls.
 */
[DisallowMultipleComponent]
public class PauseSettingsBinder : MonoBehaviour
{
    [Header("Pause Settings Controls")]
    [Tooltip("Sound effects volume slider.")]
    [SerializeField] private Slider soundSlider;

    [Tooltip("Music volume slider.")]
    [SerializeField] private Slider musicSlider;

    [Header("Audio Sources (Optional)")]
    [Tooltip("Explicit sources for the sound channel. If empty, GameSettings uses the loop=false fallback.")]
    [SerializeField] private AudioSource[] soundSources;

    [Tooltip("Explicit sources for the music channel. If empty, GameSettings uses the loop=true fallback.")]
    [SerializeField] private AudioSource[] musicSources;

    private bool suppressCallbacks;
    private bool duplicateWarningLogged;
    private float lastAudioPreviewTime = -999f;
    private const float AudioPreviewCooldown = 0.12f;

    private void Awake()
    {
        ResolveReferencesIfMissing();
    }

    private void OnEnable()
    {
        WarnIfDuplicateBinders();
        SyncUiFromSavedSettings();
        GameSettings.Apply(GameSettings.Load(), soundSources, musicSources);
        BindUiHandlers();
    }

    private void OnDisable()
    {
        UnbindUiHandlers();
    }

    private void BindUiHandlers()
    {
        if (soundSlider != null)
            soundSlider.onValueChanged.AddListener(HandleSoundChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(HandleMusicChanged);
    }

    private void UnbindUiHandlers()
    {
        if (soundSlider != null)
            soundSlider.onValueChanged.RemoveListener(HandleSoundChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
    }

    private void SyncUiFromSavedSettings()
    {
        GameSettings.Data data = GameSettings.Load();
        suppressCallbacks = true;

        if (soundSlider != null)
            soundSlider.SetValueWithoutNotify(data.Sound);

        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(data.Music);

        suppressCallbacks = false;
    }

    private void HandleSoundChanged(float value)
    {
        if (suppressCallbacks)
            return;

        GameSettings.SetSound(value, soundSources);
        PlaySettingsPreview();
    }

    private void HandleMusicChanged(float value)
    {
        if (suppressCallbacks)
            return;

        GameSettings.SetMusic(value, musicSources);
        PlaySettingsPreview();
    }

    private void PlaySettingsPreview()
    {
        if (Time.unscaledTime - lastAudioPreviewTime < AudioPreviewCooldown)
            return;

        lastAudioPreviewTime = Time.unscaledTime;
        AudioManager.Instance?.PlaySfx(AudioCue.Settings);
    }

    private void ResolveReferencesIfMissing()
    {
        if (soundSlider != null && musicSlider != null)
            return;

        Debug.LogWarning($"{name}: pause settings references are incomplete. Trying fallback lookup.", this);

        Slider[] sliders = GetComponentsInChildren<Slider>(true);
        if (soundSlider == null && sliders.Length > 0)
            soundSlider = sliders[0];
        if (musicSlider == null && sliders.Length > 1)
            musicSlider = sliders[1];

        if (soundSlider == null || musicSlider == null)
            Debug.LogError($"{name}: PauseSettingsBinder could not restore all references. Assign sound/music in Inspector.", this);
    }

    private void WarnIfDuplicateBinders()
    {
        if (duplicateWarningLogged)
            return;

        PauseSettingsBinder[] binders = FindObjectsByType<PauseSettingsBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (binders.Length > 1)
        {
            duplicateWarningLogged = true;
            Debug.LogWarning($"{name}: found multiple PauseSettingsBinder components ({binders.Length}). Keep one active binder in the scene/prefab.", this);
        }
    }
}
