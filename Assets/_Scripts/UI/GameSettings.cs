using UnityEngine;

/*
 * GameSettings
 * Назначение: единая статическая точка хранения, загрузки и применения настроек sound/music/fullscreen.
 * Роль в игре: обеспечивает одинаковое поведение настроек в MainMenu и в игровых сценах.
 * Связи: PlayerPrefs (сохранение), Screen (fullscreen), AudioSource в активной сцене.
 * Как используется:
 * - UI-контроллеры вызывают SetSound/SetMusic/SetFullscreen при изменении контролов.
 * - SettingsBootstrapper вызывает Load + Apply при старте и после загрузки каждой сцены.
 * Идеи расширения:
 * - Перенести громкости в AudioMixer-группы.
 * - Добавить quality/language в эту же модель.
 * - Добавить событие "настройки применены" для UI-виджетов.
 * Практические советы:
 * - Ключи PlayerPrefs держим только здесь, чтобы не ловить опечатки в разных скриптах.
 * - Применение по loop/non-loop — это резервная эвристика; для точности лучше назначать явные массивы источников.
 */
public static class GameSettings
{
    public const string SoundPrefKey = "settings_sound";
    public const string MusicPrefKey = "settings_music";
    public const string FullscreenPrefKey = "settings_fullscreen";

    public const float DefaultSound = 1f;
    public const float DefaultMusic = 1f;
    public const int DefaultFullscreen = 1;

    public struct Data
    {
        public float Sound;
        public float Music;
        public bool Fullscreen;
    }

    /// <summary>
    /// Загружает текущие значения настроек из PlayerPrefs с дефолтами и нормализацией диапазона громкости.
    /// </summary>
    public static Data Load()
    {
        return new Data
        {
            Sound = Mathf.Clamp01(PlayerPrefs.GetFloat(SoundPrefKey, DefaultSound)),
            Music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicPrefKey, DefaultMusic)),
            Fullscreen = PlayerPrefs.GetInt(FullscreenPrefKey, DefaultFullscreen) == 1
        };
    }

    /// <summary>
    /// Контракт: немедленно применяет переданные значения в рантайме и не изменяет сами сохранённые данные.
    /// Почему так: разделяем "загрузить/сохранить" и "применить", чтобы поведение было предсказуемым в любой сцене.
    /// Как дебажить: если меняется не тот звук, проверьте, не сработал ли резервный путь по loop/non-loop вместо явных массивов.
    /// </summary>
    public static void Apply(Data data, AudioSource[] soundSources = null, AudioSource[] musicSources = null)
    {
        ApplySound(data.Sound, soundSources);
        ApplyMusic(data.Music, musicSources);
        ApplyFullscreen(data.Fullscreen);
    }

    /// <summary>
    /// Контракт: сохраняет и применяет громкость эффектов (канал sound).
    /// Почему так: UI сразу даёт обратную связь без отдельной кнопки Apply.
    /// Как дебажить: если значение в UI меняется, а звук нет — проверьте PlayerPrefs и список источников soundSources.
    /// </summary>
    public static void SetSound(float value, AudioSource[] soundSources = null)
    {
        Data data = Load();
        data.Sound = Mathf.Clamp01(value);
        Save(data);
        ApplySound(data.Sound, soundSources);
    }

    /// <summary>
    /// Контракт: сохраняет и применяет громкость музыки (канал music).
    /// Почему так: поведение идентично SetSound, чтобы ученикам было проще поддерживать оба канала.
    /// Как дебажить: если музыка не реагирует, проверьте loop у источников или назначьте musicSources явно.
    /// </summary>
    public static void SetMusic(float value, AudioSource[] musicSources = null)
    {
        Data data = Load();
        data.Music = Mathf.Clamp01(value);
        Save(data);
        ApplyMusic(data.Music, musicSources);
    }

    /// <summary>
    /// Контракт: сохраняет и сразу применяет fullscreen-режим.
    /// Почему так: это платформенная настройка, игрок ожидает моментальный результат.
    /// Как дебажить: если режим не меняется, проверьте, не блокирует ли ОС/платформа смену полноэкранного режима.
    /// </summary>
    public static void SetFullscreen(bool value)
    {
        Data data = Load();
        data.Fullscreen = value;
        Save(data);
        ApplyFullscreen(data.Fullscreen);
    }

    private static void Save(Data data)
    {
        PlayerPrefs.SetFloat(SoundPrefKey, Mathf.Clamp01(data.Sound));
        PlayerPrefs.SetFloat(MusicPrefKey, Mathf.Clamp01(data.Music));
        PlayerPrefs.SetInt(FullscreenPrefKey, data.Fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static void ApplySound(float value, AudioSource[] soundSources)
    {
        ApplyVolume(value, useLoopSources: false, explicitSources: soundSources);
    }

    private static void ApplyMusic(float value, AudioSource[] musicSources)
    {
        ApplyVolume(value, useLoopSources: true, explicitSources: musicSources);
    }

    /// <summary>
    /// Общий слой применения громкости.
    /// Если explicitSources не назначены, используется резервный путь: loop=true как music, loop=false как sound.
    /// </summary>
    private static void ApplyVolume(float value, bool useLoopSources, AudioSource[] explicitSources)
    {
        if (explicitSources != null && explicitSources.Length > 0)
        {
            for (int i = 0; i < explicitSources.Length; i++)
            {
                if (explicitSources[i] != null)
                    explicitSources[i].volume = value;
            }

            return;
        }

        AudioSource[] sceneSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneSources.Length; i++)
        {
            AudioSource source = sceneSources[i];
            if (source != null && source.loop == useLoopSources)
                source.volume = value;
        }
    }

    private static void ApplyFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}
