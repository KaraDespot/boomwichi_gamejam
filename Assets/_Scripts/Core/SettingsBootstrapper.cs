using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * SettingsBootstrapper
 * Назначение: глобально применять сохранённые настройки при старте и на каждой загрузке сцены.
 * Роль в игре: убирает "случайную" глобальность и делает применение настроек явным и предсказуемым.
 * Связи: SceneManager.sceneLoaded и статический API GameSettings.
 * Как используется: вешается в Bootstrap-сцене на объект, который живёт между сценами.
 * Идеи расширения:
 * - Добавить флаг отладочного логирования применяемых значений.
 * - Добавить событие для UI-индикаторов "настройки применены".
 * Практические советы:
 * - В проекте должен быть один активный экземпляр этого компонента.
 * - Если "настройки не доезжают" в сцену, первым делом проверьте подписку на sceneLoaded.
 */
[DisallowMultipleComponent]
public class SettingsBootstrapper : MonoBehaviour
{
    /// <summary>
    /// Контракт: подписываемся на SceneManager.sceneLoaded только на время активности компонента.
    /// Почему так: защищаемся от дублей подписок при повторной активации объектов.
    /// Как дебажить: если Apply вызывается несколько раз на одну сцену — ищите дубликаты bootstrap-объекта.
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    /// <summary>
    /// Контракт: симметрично снимает подписку, которую добавили в OnEnable.
    /// Почему так: предотвращаем утечки подписок и повторные вызовы после отключения объекта.
    /// Как дебажить: при странных повторных реакциях проверьте, что OnDisable реально срабатывает.
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    /// <summary>
    /// Контракт: применяет настройки сразу при создании bootstrap-объекта.
    /// Почему так: первая сцена после запуска тоже должна получить актуальные параметры.
    /// Как дебажить: если стартует с дефолтами, проверьте порядок запуска и значения в PlayerPrefs.
    /// </summary>
    private void Awake()
    {
        ApplyCurrentSettings();
    }

    /// <summary>
    /// Контракт: после каждой загрузки сцены повторно применяет сохранённые настройки.
    /// Почему так: новая сцена может создать новые AudioSource и сбросить контекст применения.
    /// Как дебажить: добавьте breakpoint здесь и проверьте, что handler вызывается при каждом переходе.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCurrentSettings();
    }

    private static void ApplyCurrentSettings()
    {
        GameSettings.Apply(GameSettings.Load());
    }
}
