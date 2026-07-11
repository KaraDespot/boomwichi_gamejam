/*
 * BootstrapManager
 * Назначение: стартовая точка приложения, которая гарантирует создание всех основных менеджеров.
 * Что делает: один раз инициализирует GameManager, SceneLoader, EventBus, InputManager и переходит в главное меню.
 * Связи: сцена Bootstrap, классы GameManager, SceneLoader, EventBus, InputManager, SceneNames.
 * Паттерны: Bootstrapper, псевдо‑Singleton (через флаг _initialized), Service Locator (FindFirstObjectByType).
 */

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class BootstrapManager : MonoBehaviour
{
    // Защита от повторной инициализации, если Bootstrap загрузится повторно
    private static bool _initialized;

    /// <summary>
    /// Гарантирует, что Bootstrap выполнится только один раз и поднимет основные менеджеры.
    /// </summary>
    private void Awake()
    {
        if (_initialized)
        {
            Destroy(gameObject);
            return;
        }

        _initialized = true;
        DontDestroyOnLoad(gameObject);

        // Создаём/поднимаем основные менеджеры
        CreateGameManager();
        CreateSceneLoader();
        CreateEventBus();
        CreateInputManager();

    }

    private void Start()
    {
        // Стартовый flow урока: Bootstrap -> Loading -> MainMenu.
        SceneLoader.Instance.LoadWithLoading(SceneNames.MainMenu, PreloadBeforeMainMenu);
    }

    private IEnumerator PreloadBeforeMainMenu()
    {
        // Простая точка расширения: сюда добавляем обязательную подгрузку
        // (настройки, сохранения, локализация и т.п.) по мере роста проекта.
        yield return null;
    }

    /// <summary>
    /// Создаёт GameManager, если его ещё нет в сцене, и помечает как DontDestroyOnLoad.
    /// </summary>
    private static void CreateGameManager()
    {
        GameManager existing = FindFirstObjectByType<GameManager>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        DontDestroyOnLoad(go);
    }

    /// <summary>
    /// Создаёт SceneLoader, если его ещё нет в сцене, и помечает как DontDestroyOnLoad.
    /// Обёртка над Unity SceneManager.
    /// </summary>
    private static void CreateSceneLoader()
    {
        SceneLoader existing = FindFirstObjectByType<SceneLoader>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("SceneLoader");
        go.AddComponent<SceneLoader>();
        DontDestroyOnLoad(go);
    }

    /// <summary>
    /// Создаёт EventBus, если его ещё нет в сцене, и помечает как DontDestroyOnLoad.
    /// Реализует паттерн Event Bus / Observer.
    /// </summary>
    private static void CreateEventBus()
    {
        EventBus existing = FindFirstObjectByType<EventBus>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("EventBus");
        go.AddComponent<EventBus>();
        DontDestroyOnLoad(go);
    }

    /// <summary>
    /// Создаёт InputManager, если его ещё нет в сцене, назначает ему InputActionAsset из Resources.
    /// Связывает систему ввода Unity Input System с остальной архитектурой.
    /// </summary>
    private static void CreateInputManager()
    {
        InputManager existing = FindFirstObjectByType<InputManager>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("InputManager");
        InputManager inputManager = go.AddComponent<InputManager>();

        // Загружаем из Resources — работает и в редакторе, и в билде
        inputManager.inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");

        if (inputManager.inputActions == null)
        {
            Debug.LogError("InputManager: Не удалось загрузить InputSystem_Actions! " +
                "Убедитесь, что файл InputSystem_Actions.inputactions лежит в папке Assets/Resources/");
        }

        DontDestroyOnLoad(go);
    }
}

