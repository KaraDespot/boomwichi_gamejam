using UnityEngine;
using UnityEngine.UI;

/*
 * PauseSettingsBinder
 * Назначение: связать существующие контролы в Pause с общей системой настроек.
 * Роль в игре: реализует встроенные настройки в паузе без отдельного окна и без кнопки Back.
 * Связи: слайдеры/тоггл в Pause, GameSettings, опционально явные массивы AudioSource.
 * Как используется: вешается на объект Pause (или дочерний Settings) в UIRootCanvas, ссылки задаются в Inspector.
 * Идеи расширения:
 * - Добавить отображение числовых значений громкости рядом со слайдерами.
 * - Добавить кнопку "Сбросить по умолчанию" внутри Pause.
 * Практические советы:
 * - Канонический путь: все ссылки назначены вручную; автопоиск нужен только как страховка.
 * - Если звук реагирует не так, назначьте soundSources/musicSources явно, не полагайтесь на резервный путь.
 */
[DisallowMultipleComponent]
public class PauseSettingsBinder : MonoBehaviour
{
    [Header("Контролы настроек в паузе")]
    [Tooltip("Слайдер громкости звуковых эффектов (sound).")]
    [SerializeField] private Slider soundSlider;

    [Tooltip("Слайдер громкости музыки (music).")]
    [SerializeField] private Slider musicSlider;

    [Tooltip("Тоггл полноэкранного режима.")]
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Аудио-источники (опционально)")]
    [Tooltip("Явные источники для канала sound. Рекомендуется назначать вручную; иначе используется резервный путь по loop=false.")]
    [SerializeField] private AudioSource[] soundSources;

    [Tooltip("Явные источники для канала music. Рекомендуется назначать вручную; иначе используется резервный путь по loop=true.")]
    [SerializeField] private AudioSource[] musicSources;

    private bool suppressCallbacks;
    private bool duplicateWarningLogged;

    private void Awake()
    {
        ResolveReferencesIfMissing();
    }

    /// <summary>
    /// Контракт: порядок строго sync -> apply -> bind listeners.
    /// Почему так: сначала безопасно выставляем UI без рекурсии, затем применяем значения в сцену, и только потом слушаем ввод игрока.
    /// Как дебажить: если при открытии паузы значения "прыгают", проверьте suppressCallbacks и дубли биндеров.
    /// </summary>
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

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(HandleFullscreenChanged);
    }

    private void UnbindUiHandlers()
    {
        if (soundSlider != null)
            soundSlider.onValueChanged.RemoveListener(HandleSoundChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(HandleFullscreenChanged);
    }

    private void SyncUiFromSavedSettings()
    {
        GameSettings.Data data = GameSettings.Load();
        suppressCallbacks = true;

        if (soundSlider != null)
            soundSlider.SetValueWithoutNotify(data.Sound);

        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(data.Music);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(data.Fullscreen);

        suppressCallbacks = false;
    }

    private void HandleSoundChanged(float value)
    {
        if (suppressCallbacks)
            return;

        GameSettings.SetSound(value, soundSources);
    }

    private void HandleMusicChanged(float value)
    {
        if (suppressCallbacks)
            return;

        GameSettings.SetMusic(value, musicSources);
    }

    private void HandleFullscreenChanged(bool value)
    {
        if (suppressCallbacks)
            return;

        GameSettings.SetFullscreen(value);
    }

    /// <summary>
    /// Резервный путь: пробует найти ссылки в дочерних объектах, если их забыли назначить в Inspector.
    /// Основной путь в teacher-repo: ссылки выставляются вручную.
    /// </summary>
    private void ResolveReferencesIfMissing()
    {
        if (soundSlider != null && musicSlider != null && fullscreenToggle != null)
            return;

        Debug.LogWarning($"{name}: ссылки настроек Pause не полностью назначены. Выполняю резервный автопоиск.", this);

        if (soundSlider == null || musicSlider == null)
        {
            Slider[] sliders = GetComponentsInChildren<Slider>(true);
            if (soundSlider == null && sliders.Length > 0)
                soundSlider = sliders[0];
            if (musicSlider == null && sliders.Length > 1)
                musicSlider = sliders[1];
        }

        if (fullscreenToggle == null)
            fullscreenToggle = GetComponentInChildren<Toggle>(true);

        if (soundSlider == null || musicSlider == null || fullscreenToggle == null)
            Debug.LogError($"{name}: PauseSettingsBinder не смог восстановить все ссылки. Назначьте sound/music/fullscreen в Inspector.", this);
    }

    private void WarnIfDuplicateBinders()
    {
        if (duplicateWarningLogged)
            return;

        PauseSettingsBinder[] binders = FindObjectsByType<PauseSettingsBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (binders.Length > 1)
        {
            duplicateWarningLogged = true;
            Debug.LogWarning($"{name}: найдено несколько PauseSettingsBinder ({binders.Length}). Проверьте, что в сцене/префабе остался один активный биндер.", this);
        }
    }
}
