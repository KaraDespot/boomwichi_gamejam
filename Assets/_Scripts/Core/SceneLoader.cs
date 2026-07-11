/*
 * SceneLoader
 * Назначение: единая точка для загрузки сцен.
 * Что делает: синхронно и асинхронно загружает сцены Unity, логирует прогресс при асинхронной загрузке.
 * Связи: используется GameManager и BootstrapManager при переходах между сценами.
 * Паттерны: Singleton, Facade над UnityEngine.SceneManagement.
 */
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField, Min(0f)] private float minimumLoadingDuration = 3.2f;

    private string _pendingSceneName;
    private bool _waitForLoadingScene;
    private Func<IEnumerator> _pendingPreloadRoutine;

    // Инициализирует Singleton SceneLoader и делает объект переживающим смену сцен.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    /// <summary>
    /// Асинхронно загружает сцену по имени.
    /// Подходит для простых переходов, когда не нужен прогресс загрузки.
    /// </summary>
    public void Load(string sceneName)
    {
        Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Запускает асинхронную загрузку сцены.
    /// Можно расширить для показа экрана загрузки/прогресса.
    /// </summary>
    public void LoadAsync(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    /// <summary>
    /// Переход в целевую сцену через Loading:
    /// сначала открываем Loading, затем уже из неё асинхронно грузим целевую сцену.
    /// </summary>
    public void LoadWithLoading(string targetSceneName, Func<IEnumerator> preloadRoutine = null)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("SceneLoader: target scene name is empty.");
            return;
        }

        if (targetSceneName == SceneNames.Loading)
        {
            Load(targetSceneName);
            return;
        }

        _pendingSceneName = targetSceneName;
        _pendingPreloadRoutine = preloadRoutine;
        _waitForLoadingScene = true;
        Load(SceneNames.Loading);
    }

    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        float startTime = Time.unscaledTime;

        while (!operation.isDone)
        {
            bool minDurationReached = Time.unscaledTime - startTime >= minimumLoadingDuration;
            bool loadingReady = operation.progress >= 0.9f;

            if (loadingReady && minDurationReached)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        Debug.Log("Scene loaded");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Важный hook для EventBus:
        // публикуем факт загрузки ЛЮБОЙ сцены (MainMenu, Loading, GameScene, etc).
        // Это даёт внешним системам единый "сигнал жизни" без прямой зависимости от SceneLoader.
        if (EventBus.Instance != null)
            EventBus.Instance.RaiseLevelLoaded(scene.name);

        // Логика ниже относится только к двухшаговому flow через Loading:
        // сначала открыли Loading, затем из неё грузим целевую сцену.
        if (!_waitForLoadingScene)
            return;

        if (scene.name != SceneNames.Loading)
            return;

        _waitForLoadingScene = false;

        if (string.IsNullOrWhiteSpace(_pendingSceneName))
        {
            Debug.LogError("SceneLoader: pending scene is empty after Loading scene opened.");
            return;
        }

        StartCoroutine(LoadPendingSceneFlow());
    }

    private IEnumerator LoadPendingSceneFlow()
    {
        // Даем Loading сцене гарантированно отрисоваться хотя бы один кадр.
        yield return null;

        if (_pendingPreloadRoutine != null)
        {
            yield return StartCoroutine(_pendingPreloadRoutine.Invoke());
        }

        string sceneToLoad = _pendingSceneName;
        _pendingSceneName = null;
        _pendingPreloadRoutine = null;

        yield return StartCoroutine(LoadSceneAsyncCoroutine(sceneToLoad));
    }
}
