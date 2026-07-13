using System;
using UnityEngine;

/*
 * GameSettings
 * Назначение: единая статическая точка хранения, загрузки и применения настроек sound/music.
 * Роль в игре: обеспечивает одинаковое поведение настроек в MainMenu и в игровых сценах.
 * Связи: PlayerPrefs (сохранение), AudioSource в активной сцене.
 * Как используется:
 * - UI-контроллеры вызывают SetSound/SetMusic при изменении контролов.
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

    public const float DefaultSound = 1f;
    public const float DefaultMusic = 1f;

    public static event Action<Data> OnSettingsApplied;

    public struct Data
    {
        public float Sound;
        public float Music;
    }

    /// <summary>
    /// Загружает текущие значения настроек из PlayerPrefs с дефолтами и нормализацией диапазона громкости.
    /// </summary>
    public static Data Load()
    {
        return new Data
        {
            Sound = Mathf.Clamp01(PlayerPrefs.GetFloat(SoundPrefKey, DefaultSound)),
            Music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicPrefKey, DefaultMusic))
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
        OnSettingsApplied?.Invoke(data);
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
        OnSettingsApplied?.Invoke(data);
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
        OnSettingsApplied?.Invoke(data);
    }

    private static void Save(Data data)
    {
        PlayerPrefs.SetFloat(SoundPrefKey, Mathf.Clamp01(data.Sound));
        PlayerPrefs.SetFloat(MusicPrefKey, Mathf.Clamp01(data.Music));
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

        AudioSource[] sceneSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneSources.Length; i++)
        {
            AudioSource source = sceneSources[i];
            if (source != null && source.loop == useLoopSources)
                source.volume = value;
        }
    }

}
