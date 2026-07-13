/*
 * SceneLoader
 * Назначение: единая точка для загрузки сцен.
 * Что делает: загружает сцены Unity напрямую и показывает Loading только перед MainMenu.
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
    public void Load(string sceneName)
    {
        Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Стартовый переход Bootstrap -> Loading -> MainMenu.
    /// Игровые сцены загружаются напрямую через Load, без промежуточного Loading.
    /// </summary>
    public void LoadMainMenuWithLoading(Func<IEnumerator> preloadRoutine = null)
    {
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

        StartCoroutine(LoadMainMenuAfterLoadingFlow());
    }

    private IEnumerator LoadMainMenuAfterLoadingFlow()
    {
        // Даем Loading сцене гарантированно отрисоваться хотя бы один кадр.
        yield return null;

        if (_pendingPreloadRoutine != null)
        {
            yield return StartCoroutine(_pendingPreloadRoutine.Invoke());
        }

        _pendingPreloadRoutine = null;

        yield return StartCoroutine(LoadSceneAsyncCoroutine(SceneNames.MainMenu));
    }
}
