/*
 * SceneLoader
 * Назначение: единая точка для загрузки сцен.
 * Что делает: загружает сцены Unity напрямую и сообщает системам о смене активной сцены.
 * Связи: используется GameManager и BootstrapManager при переходах между сценами.
 * Паттерны: Singleton, Facade над UnityEngine.SceneManagement.
 */
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

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
        Debug.Log($"SceneLoader: loading scene {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Публикуем факт загрузки любой сцены для систем, которым нужен единый сигнал перехода.
        if (EventBus.Instance != null)
            EventBus.Instance.RaiseLevelLoaded(scene.name);
    }
}
