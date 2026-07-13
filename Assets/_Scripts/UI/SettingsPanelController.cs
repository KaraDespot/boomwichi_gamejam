using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/*
 * SettingsPanelController
 * Назначение: отдельный экран настроек (главное меню или оверлей).
 * Что делает: два слайдера sound/music как в паузе и кнопка «Назад» в меню.
 * Связи: GameSettings, MainMenuController, опционально AudioSource.
 * Паттерны: UI Controller.
 *
 * Настройка в сцене:
 * - Повесь компонент на свой Canvas/Panel настроек.
 * - Назначь settingsScreen, soundSlider, musicSlider, backButton в Inspector.
 * - Экран по умолчанию должен быть выключен в иерархии.
 * - В MainMenuController укажи ссылку на этот компонент.
 */
[DisallowMultipleComponent]
public class SettingsPanelController : MonoBehaviour
{
    [Header("Экран настроек")]
    [Tooltip("Корневой объект экрана: Panel или Canvas с настройками.")]
    [FormerlySerializedAs("settingsPanel")]
    [SerializeField] private GameObject settingsScreen;

    [Header("Слайдеры")]
    [Tooltip("Громкость звуковых эффектов.")]
    [SerializeField] private Slider soundSlider;

    [Tooltip("Громкость музыки.")]
    [SerializeField] private Slider musicSlider;

    [Header("Кнопки")]
    [Tooltip("Кнопка «Назад» — закрывает экран настроек и возвращает в меню.")]
    [SerializeField] private Button backButton;

    [Header("Аудио-источники (опционально)")]
    [Tooltip("Явные источники sound. Если пусто — резервный поиск по loop=false.")]
    [SerializeField] private AudioSource[] soundSources;

    [Tooltip("Явные источники music. Если пусто — резервный поиск по loop=true.")]
    [SerializeField] private AudioSource[] musicSources;

    private bool suppressCallbacks;
    private float lastAudioPreviewTime = -999f;
    private const float AudioPreviewCooldown = 0.12f;

    public event Action OnSettingsClosed;

    public bool IsOpen => settingsScreen != null && settingsScreen.activeSelf;

    private void Awake()
    {
        ValidateReferences();
        HideSettingsScreen();
    }

    private void Start()
    {
        HideSettingsScreen();
    }

    private void OnEnable()
    {
        SyncUiFromSavedSettings();
        GameSettings.Apply(GameSettings.Load(), soundSources, musicSources);
        BindUiHandlers();
    }

    private void OnDisable()
    {
        UnbindUiHandlers();
    }

    public void OpenPanel()
    {
        if (settingsScreen == null)
            return;

        AudioManager.Instance?.PlaySfx(AudioCue.Button);
        SyncUiFromSavedSettings();
        settingsScreen.SetActive(true);
    }

    public void ClosePanel()
    {
        if (settingsScreen == null)
            return;

        AudioManager.Instance?.PlaySfx(AudioCue.Button);
        HideSettingsScreen();
        OnSettingsClosed?.Invoke();
    }

    private void BindUiHandlers()
    {
        if (soundSlider != null)
            soundSlider.onValueChanged.AddListener(HandleSoundSliderChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(HandleMusicSliderChanged);

        if (backButton != null)
            backButton.onClick.AddListener(HandleBackClicked);
    }

    private void UnbindUiHandlers()
    {
        if (soundSlider != null)
            soundSlider.onValueChanged.RemoveListener(HandleSoundSliderChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(HandleMusicSliderChanged);

        if (backButton != null)
            backButton.onClick.RemoveListener(HandleBackClicked);
    }

    private void HandleBackClicked()
    {
        ClosePanel();
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

    private void HandleSoundSliderChanged(float value)
    {
        if (suppressCallbacks)
            return;

        GameSettings.SetSound(value, soundSources);
        PlaySettingsPreview();
    }

    private void HandleMusicSliderChanged(float value)
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

    private void HideSettingsScreen()
    {
        if (settingsScreen != null)
            settingsScreen.SetActive(false);
    }

    private void ValidateReferences()
    {
        if (settingsScreen == null)
            Debug.LogWarning($"{name}: назначь settingsScreen (Canvas/Panel) в Inspector.", this);

        if (soundSlider == null)
            Debug.LogWarning($"{name}: назначь soundSlider в Inspector.", this);

        if (musicSlider == null)
            Debug.LogWarning($"{name}: назначь musicSlider в Inspector.", this);

        if (backButton == null)
            Debug.LogWarning($"{name}: назначь backButton в Inspector.", this);
    }
}
