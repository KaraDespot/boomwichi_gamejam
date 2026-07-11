using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * GameManager
 * Назначение: центральный менеджер состояния игры, загрузки уровней и простого checkpoint-flow.
 * Что управляет:
 *  - текущим состоянием игры (Menu/Playing/Paused/Lost/Won) и timeScale при pause/lose/win;
 *  - загрузкой gameplay-сцен через SceneLoader и последовательностью уровней из LevelSequenceData;
 *  - активным save-слотом, выбранным в MainMenu через New Game или Continue;
 *  - записью checkpoint в активный слот, а если активный слот не задан - в инспекторный fallback slot;
 *  - Continue из выбранного слота и Restart после смерти через checkpoint с fallback на перезагрузку текущей сцены.
 *
 * Почему так:
 *  - Save-модель остаётся учебной: сохраняются безопасная точка, сцена и состояние игрока, но не активная волна encounter.
 *  - Выбор слота остаётся явным в меню, поэтому скрытая логика "последнего использованного" профиля не нужна.
 *  - Checkpoint уровня создаётся на выходе после завершения encounter, а CheckpointTrigger сохраняет safe-point внутри уровня.
 *
 * Потенциальные расширения:
 *  - подтверждение перезаписи занятого слота перед StartNewGameInSlot;
 *  - отдельный экран загрузки с подробным описанием выбранного профиля;
 *  - дата последнего сохранения или короткое имя профиля.
 *
 * Совет:
 *  - При ошибках перехода проверить, что в сцене есть ровно один Player с PlayerStats/PlayerProgression/WeaponManager.
 *  - При ошибках checkpoint проверить activeSaveSlotIndex, fallback checkpointSlotIndex, Console и наличие Save Game Free.
 */
public enum GameState
{
    Menu,
    Playing,
    Paused,
    Lost,
    Won,
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Sequence")]
    [Tooltip("Путь в Resources до LevelSequenceData без расширения .asset.")]
    [SerializeField] private string levelSequenceResourcePath = "Levels/LevelSequence_Default";
    [SerializeField] private LevelSequenceData levelSequenceOverride;

    /// <summary>
    /// Текущее состояние игры.
    /// </summary>
    public GameState CurrentState { get; private set; } = GameState.Menu;

    /// <summary>
    /// Индекс текущего уровня из LevelSequenceData.
    /// -1 означает "не определён или fallback режим".
    /// </summary>
    public int CurrentLevelIndex => currentLevelIndex;

    private LevelSequenceData levelSequenceData;
    private int currentLevelIndex = -1;
    private int activeSaveSlotIndex = -1;
    private PlayerRuntimeState pendingPlayerRuntimeState;

    /// <summary>
    /// Инициализирует singleton и подгружает конфиг последовательности уровней.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;

        LoadLevelSequenceData();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    /// <summary>
    /// Контракт: запускает прохождение с первого gameplay-уровня, но не выбирает save-слот и не меняет activeSaveSlotIndex.
    /// Безопасный основной сценарий: внутренний вызов из StartNewGameInSlot после выбора и очистки слота в MainMenu.
    /// Важно: прямой вызов из старого UI или debug-кнопки оставит activeSaveSlotIndex равным -1, поэтому restart через checkpoint может уйти в fallback-перезапуск сцены.
    /// Логика: берётся 0-й уровень из LevelSequenceData, а если sequence не найден - используется SceneNames.GameScene.
    /// </summary>
    public void StartGame()
    {
        pendingPlayerRuntimeState = null;
        currentLevelIndex = 0;

        if (!TryGetLevelSceneName(currentLevelIndex, out string firstLevelScene))
        {
            firstLevelScene = SceneNames.GameScene;
            currentLevelIndex = -1;
        }

        LoadGameplayScene(firstLevelScene);
    }

    /// <summary>
    /// Контракт: запускает новую игру в выбранном save-слоте 0..2.
    /// Вызывать после ручного выбора слота в главном меню. Метод очищает старый save этого слота без UI-подтверждения.
    /// Почему так: подтверждение перезаписи относится к экрану меню, а здесь важно не оставить старый checkpoint для restart после смерти.
    /// Потенциальное применение: разные профили прохождения на одном компьютере.
    /// </summary>
    public void StartNewGameInSlot(int slotIndex)
    {
        if (!TrySetActiveSaveSlot(slotIndex))
            return;

        CheckpointSaveSystem.Delete(slotIndex);
        StartGame();
    }

    /// <summary>
    /// Контракт: загружает существующий checkpoint из выбранного слота и делает этот слот активным для следующих сохранений.
    /// Метод не создаёт UI и не выбирает слот автоматически: слот приходит из панели главного меню.
    /// Почему так: явный выбор проще проверить на уроке, чем скрытую логику "последнего использованного" профиля.
    /// Потенциальное применение: Continue из главного меню или экран выбора профиля.
    /// </summary>
    public bool TryContinueFromSlot(int slotIndex)
    {
        if (!TrySetActiveSaveSlot(slotIndex))
            return false;

        if (!CheckpointSaveSystem.TryLoad(slotIndex, out CheckpointSaveData data))
        {
            Debug.LogWarning($"GameManager: слот {slotIndex} пуст или не читается. Continue отменён.", this);
            return false;
        }

        LoadFromCheckpointData(data);
        return true;
    }

    /// <summary>
    /// Возвращает игрока в главное меню.
    /// </summary>
    public void GoToMenu()
    {
        pendingPlayerRuntimeState = null;
        CurrentState = GameState.Menu;
        Time.timeScale = 1f;
        SceneLoader.Instance.Load(SceneNames.MainMenu);

        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();
    }

    /// <summary>
    /// Ставит игру на паузу и публикует событие в EventBus.
    /// </summary>
    public void Pause()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
        EventBus.Instance.RaiseGamePaused();
    }

    /// <summary>
    /// Снимает паузу и публикует событие в EventBus.
    /// </summary>
    public void Resume()
    {
        if (CurrentState != GameState.Paused)
            return;

        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        EventBus.Instance.RaiseGameResumed();
    }

    /// <summary>
    /// Перезапускает текущий игровой уровень.
    /// Метод сохранён для обратной совместимости со старыми кнопками/скриптами.
    /// </summary>
    public void RestartGameScene()
    {
        pendingPlayerRuntimeState = null;
        string sceneToReload = ResolveCurrentGameplayScene();
        LoadGameplayScene(sceneToReload);
    }

    /// <summary>
    /// Контракт: используется кнопкой Restart на lose screen.
    /// Активный слот задаётся заранее через New Game или Continue в главном меню.
    /// Если активный слот содержит checkpoint, загружает progress из него; если save нет, выполняет обычный restart текущей сцены.
    /// Почему так: одна кнопка остаётся понятной, но после урока 14 ведёт к прогрессу, а не всегда к началу сцены.
    /// Потенциальное применение: death retry flow в простых играх без отдельного экрана загрузки.
    /// </summary>
    public void RestartFromCheckpointOrScene()
    {
        if (TryLoadActiveCheckpoint())
            return;

        RestartGameScene();
    }

    /// <summary>
    /// Контракт: пытается загрузить checkpoint из активного save-слота без fallback restart.
    /// Активный слот появляется только после выбора профиля через New Game или Continue в главном меню.
    /// Возвращает false, если слот не выбран, пуст или повреждён.
    /// Почему так: отдельный bool-метод удобно использовать для UI-ветвления и отладочных кнопок.
    /// Потенциальное применение: отдельная кнопка Load Checkpoint в pause/debug UI.
    /// </summary>
    public bool TryLoadActiveCheckpoint()
    {
        if (!IsValidSaveSlot(activeSaveSlotIndex))
            return false;

        if (!CheckpointSaveSystem.TryLoad(activeSaveSlotIndex, out CheckpointSaveData data))
            return false;

        LoadFromCheckpointData(data);
        return true;
    }

    /// <summary>
    /// Контракт: переводит сохранённые checkpoint-данные в загрузку gameplay-сцены и отложенное применение состояния игрока.
    /// Входные условия: data получен из CheckpointSaveSystem, имя сцены не пустое или может быть восстановлено через fallback.
    /// Шаги: выбрать сцену, выставить индекс уровня, собрать runtime-состояние игрока, загрузить сцену через Loading.
    /// Типичные поломки: пустой nextSceneName у checkpoint выхода, сцена не добавлена в Build Settings, Player отсутствует в загруженной сцене.
    /// Что проверить: поля completedSceneName/nextSceneName в JSON, LevelSequenceData.asset, Console после загрузки.
    /// </summary>
    private void LoadFromCheckpointData(CheckpointSaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("GameManager: checkpoint data == null. Загрузка отменена.", this);
            return;
        }

        string sceneToLoad = ResolveCheckpointScene(data);
        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            Debug.LogWarning("GameManager: у checkpoint нет валидной сцены. Выполняется обычный restart.", this);
            RestartGameScene();
            return;
        }

        currentLevelIndex = data.savedFromLevelExit ? data.nextLevelIndex : data.completedLevelIndex;
        pendingPlayerRuntimeState = CreateRuntimeStateFromCheckpoint(data);
        LoadGameplayScene(sceneToLoad);
    }

    /// <summary>
    /// Контракт: выбирает сцену для checkpoint.
    /// Для safe-point внутри уровня открывается completedSceneName, для выхода уровня - nextSceneName.
    /// Почему так: позиция выхода из прошлого уровня не переносится в следующую сцену, а safe-point возвращает именно в тот же уровень.
    /// Потенциальное применение: разделить checkpoint внутри сцены и progress checkpoint между уровнями.
    /// </summary>
    private string ResolveCheckpointScene(CheckpointSaveData data)
    {
        if (data.savedFromLevelExit && !string.IsNullOrWhiteSpace(data.nextSceneName))
            return data.nextSceneName;

        if (!string.IsNullOrWhiteSpace(data.completedSceneName))
            return data.completedSceneName;

        if (TryGetLevelSceneName(data.nextLevelIndex, out string nextScene))
            return nextScene;

        if (TryGetLevelSceneName(data.completedLevelIndex, out string completedScene))
            return completedScene;

        return SceneNames.GameScene;
    }

    /// <summary>
    /// Контракт: переносит данные save в runtime-контейнер, который будет применён после загрузки gameplay-сцены.
    /// Позиция применяется только для checkpoint внутри уровня; checkpoint выхода грузит следующую сцену на её обычный spawn.
    /// Почему так: простая save-модель не знает spawn points следующей сцены и не сохраняет состояние мира.
    /// Потенциальное применение: позже можно добавить checkpointSpawnId вместо прямой Vector3.
    /// </summary>
    private PlayerRuntimeState CreateRuntimeStateFromCheckpoint(CheckpointSaveData data)
    {
        return new PlayerRuntimeState
        {
            Health = data.health,
            Mana = data.mana,
            Level = data.playerLevel,
            Experience = data.experience,
            WeaponSlotIndex = data.weaponSlotIndex,
            ShouldApplyPosition = !data.savedFromLevelExit,
            Position = data.checkpointPosition
        };
    }

    /// <summary>
    /// Пытается загрузить следующий уровень из sequence.
    /// Возвращает true, если уровень найден и загрузка запущена.
    /// Возвращает false, если текущий уровень последний.
    /// </summary>
    public bool TryLoadNextLevel()
    {
        UpdateCurrentLevelIndexFromActiveScene();

        int nextLevelIndex = currentLevelIndex + 1;
        if (!TryGetLevelSceneName(nextLevelIndex, out string nextScene))
            return false;

        pendingPlayerRuntimeState = CaptureCurrentPlayerRuntimeState();
        currentLevelIndex = nextLevelIndex;
        LoadGameplayScene(nextScene);
        return true;
    }

    /// <summary>
    /// Контракт: вызывать после проверки win-условий, когда игрок вошёл в trigger завершения уровня.
    /// Гарантирует запись завершённой сцены, следующей сцены и состояния игрока в выбранный слот.
    /// Для win на выходе успешное сохранение обязательно: GameLoopFlowController не покажет победу при false.
    /// Не сохраняет активных врагов, projectile, индекс текущей волны или состояние encounter.
    /// Почему так: конец уровня является понятной безопасной точкой, а не хрупким снимком середины боя.
    /// Потенциальное применение: кнопка Continue сможет открыть следующий незавершённый уровень.
    /// </summary>
    public bool TrySaveLevelCheckpointProgress(int slotIndex, Vector3 levelExitPosition)
    {
        if (!TryBuildCheckpointData(
                $"LevelComplete_{SceneManager.GetActiveScene().name}",
                levelExitPosition,
                true,
                out CheckpointSaveData data))
            return false;

        return TrySaveCheckpointData(slotIndex, data);
    }

    /// <summary>
    /// Контракт: вызывать из отдельной safe-point зоны, где сохранение разрешено дизайном уровня.
    /// Гарантирует тот же формат данных, что и сохранение на выходе, но помечает источник как отдельный checkpoint.
    /// Не проверяет encounter самостоятельно: это делает CheckpointTrigger перед вызовом.
    /// Почему так: GameManager отвечает за данные, а trigger отвечает за правила своей зоны.
    /// Потенциальное применение: checkpoint в хабе, перед сложной комнатой или после длинного перехода.
    /// </summary>
    public bool TrySaveCheckpointProgress(int slotIndex, Vector3 checkpointPosition)
    {
        if (!TryBuildCheckpointData(
                $"Checkpoint_{SceneManager.GetActiveScene().name}",
                checkpointPosition,
                false,
                out CheckpointSaveData data))
            return false;

        return TrySaveCheckpointData(slotIndex, data);
    }

    public bool TrySaveCheckpointProgress(Vector3 checkpointPosition)
    {
        if (!IsValidSaveSlot(activeSaveSlotIndex))
        {
            Debug.LogError("GameManager: save slot is not selected. Checkpoint was not saved.", this);
            return false;
        }

        if (!TryBuildCheckpointData(
                $"Checkpoint_{SceneManager.GetActiveScene().name}",
                checkpointPosition,
                false,
                out CheckpointSaveData data))
            return false;

        return TrySaveCheckpointData(activeSaveSlotIndex, data);
    }

    /// <summary>
    /// Контракт: записывает уже собранные checkpoint-данные в выбранный слот.
    /// Почему так: публичные методы отвечают за сценарий сохранения, а этот метод держит единый вызов save-системы.
    /// Потенциальное применение: одинаковое логирование для выхода уровня и отдельной checkpoint-зоны.
    /// </summary>
    private bool TrySaveCheckpointData(int slotIndex, CheckpointSaveData data)
    {
        int resolvedSlotIndex = ResolveSaveSlot(slotIndex);
        bool saved = CheckpointSaveSystem.Save(resolvedSlotIndex, data);
        if (saved)
        {
            Debug.Log(
                $"GameManager: checkpoint '{data.checkpointId}' сохранён в слот {resolvedSlotIndex} после сцены '{data.completedSceneName}'. " +
                "Активная волна encounter намеренно не сохраняется.",
                this);
        }

        return saved;
    }

    /// <summary>
    /// Контракт: выбирает слот для записи checkpoint.
    /// Активный слот из меню имеет приоритет, а значение из Inspector остаётся fallback для старых сцен.
    /// Почему так: урок 14 добавляет профили без массовой перенастройки объектов, созданных в уроке 13.
    /// Потенциальное применение: плавная миграция от одного save-слота к нескольким профилям.
    /// </summary>
    private int ResolveSaveSlot(int fallbackSlotIndex)
    {
        if (IsValidSaveSlot(activeSaveSlotIndex))
            return activeSaveSlotIndex;

        if (IsValidSaveSlot(fallbackSlotIndex))
            return fallbackSlotIndex;

        Debug.LogWarning(
            $"GameManager: fallback slot {fallbackSlotIndex} вне диапазона. Будет использован слот 0.",
            this);
        return 0;
    }

    /// <summary>
    /// Контракт: сохраняет выбранный слот как активный профиль текущего запуска.
    /// Метод только проверяет диапазон и не читает/не удаляет данные слота.
    /// Почему так: выбор профиля должен быть одним явным числом, которое используют Continue, New Game и checkpoint-save.
    /// Потенциальное применение: отображение активного профиля в debug UI.
    /// </summary>
    private bool TrySetActiveSaveSlot(int slotIndex)
    {
        if (IsValidSaveSlot(slotIndex))
        {
            activeSaveSlotIndex = slotIndex;
            return true;
        }

        Debug.LogError(
            $"GameManager: неверный save slot {slotIndex}. Допустимый диапазон: 0..{CheckpointSaveSystem.SlotCount - 1}.",
            this);
        return false;
    }

    private static bool IsValidSaveSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < CheckpointSaveSystem.SlotCount;
    }

    /// <summary>
    /// Контракт: собирает CheckpointSaveData из текущей gameplay-сцены и компонентов игрока.
    /// Входные условия: на сцене есть PlayerStats, PlayerProgression и WeaponManager.
    /// Шаги: синхронизировать индекс уровня, вычислить следующую сцену, снять HP/Mana/XP/оружие.
    /// Типичные поломки: дубликаты Player, пустой LevelSequenceData, отсутствующий WeaponManager.
    /// Что проверить: prefab игрока, LevelSequenceData.asset, Console warnings от TryResolvePlayerSystems.
    /// </summary>
    private bool TryBuildCheckpointData(
        string checkpointId,
        Vector3 checkpointPosition,
        bool savedFromLevelExit,
        out CheckpointSaveData data)
    {
        data = null;

        if (!TryResolvePlayerSystems(out PlayerStats playerStats, out PlayerProgression playerProgression, out WeaponManager weaponManager))
        {
            Debug.LogWarning(
                "GameManager: checkpoint не сохранён, потому что не найдены PlayerStats, PlayerProgression или WeaponManager.",
                this);
            return false;
        }

        UpdateCurrentLevelIndexFromActiveScene();
        string completedSceneName = SceneManager.GetActiveScene().name;
        int completedLevelIndex = currentLevelIndex;
        int nextLevelIndex = completedLevelIndex >= 0 ? completedLevelIndex + 1 : -1;
        TryGetLevelSceneName(nextLevelIndex, out string nextSceneName);

        data = new CheckpointSaveData
        {
            checkpointId = string.IsNullOrWhiteSpace(checkpointId) ? $"Checkpoint_{completedSceneName}" : checkpointId,
            completedSceneName = completedSceneName,
            nextSceneName = nextSceneName,
            completedLevelIndex = completedLevelIndex,
            nextLevelIndex = string.IsNullOrWhiteSpace(nextSceneName) ? -1 : nextLevelIndex,
            checkpointPosition = checkpointPosition,
            savedFromLevelExit = savedFromLevelExit,
            health = playerStats.CurrentHealth,
            mana = playerStats.CurrentMana,
            playerLevel = playerProgression.CurrentLevel,
            experience = playerProgression.CurrentExperience,
            weaponSlotIndex = weaponManager.CurrentWeaponSlotIndex
        };

        return true;
    }

    /// <summary>
    /// Переводит игру в состояние поражения.
    /// </summary>
    public void EnterLoseState()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Lost;
        Time.timeScale = 0f;

        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();
    }

    /// <summary>
    /// Переводит игру в состояние победы.
    /// </summary>
    public void EnterWinState()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Won;
        Time.timeScale = 0f;

        if (InputManager.Instance != null)
            InputManager.Instance.EnableUIInput();
    }

    /// <summary>
    /// Загружает sequence asset из Resources.
    /// Если не найден, система всё равно работает в fallback режиме через GameScene.
    /// </summary>
    private void LoadLevelSequenceData()
    {
        if (levelSequenceOverride != null)
        {
            levelSequenceData = levelSequenceOverride;
            return;
        }

#if UNITY_EDITOR
        // В Editor приоритет у учебного ассета в _ScriptableObjects,
        // чтобы в Editor брались именно данные, открытые для ручной настройки в проекте.
        if (TryLoadLevelSequenceFromEditorAssetPath())
            return;
#endif

        if (string.IsNullOrWhiteSpace(levelSequenceResourcePath))
        {
#if UNITY_EDITOR
            TryLoadLevelSequenceFromEditorAssetPath();
#endif
            return;
        }

        levelSequenceData = Resources.Load<LevelSequenceData>(levelSequenceResourcePath);

#if UNITY_EDITOR
        if (levelSequenceData == null)
            TryLoadLevelSequenceFromEditorAssetPath();
#endif

        if (levelSequenceData == null)
        {
            Debug.LogWarning(
                $"GameManager: LevelSequenceData not found at Resources/{levelSequenceResourcePath}. " +
                "Fallback to SceneNames.GameScene will be used.");
        }
    }

#if UNITY_EDITOR
    private bool TryLoadLevelSequenceFromEditorAssetPath()
    {
        const string editorAssetPath = "Assets/_ScriptableObjects/Levels/LevelSequenceData.asset";
        levelSequenceData = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelSequenceData>(editorAssetPath);
        return levelSequenceData != null;
    }
#endif

    /// <summary>
    /// Единая точка перехода в игровую сцену:
    /// переводит state в Playing, возвращает timeScale и включает player input.
    /// </summary>
    private void LoadGameplayScene(string sceneName)
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        SceneLoader.Instance.LoadWithLoading(sceneName);

        if (InputManager.Instance != null)
            InputManager.Instance.EnablePlayerInput();
    }

    /// <summary>
    /// Безопасный доступ к имени сцены уровня из sequence.
    /// </summary>
    private bool TryGetLevelSceneName(int levelIndex, out string sceneName)
    {
        sceneName = null;

        if (levelSequenceData == null)
            return false;

        return levelSequenceData.TryGetLevelSceneName(levelIndex, out sceneName);
    }

    /// <summary>
    /// Синхронизирует currentLevelIndex с реально активной сценой.
    /// Нужен перед вычислением "следующего" уровня.
    /// </summary>
    private void UpdateCurrentLevelIndexFromActiveScene()
    {
        if (levelSequenceData == null)
            return;

        string activeSceneName = SceneManager.GetActiveScene().name;
        int sceneIndex = levelSequenceData.FindLevelIndex(activeSceneName);
        if (sceneIndex >= 0)
            currentLevelIndex = sceneIndex;
    }

    /// <summary>
    /// Определяет, какую сцену перезапускать на Restart:
    /// 1) текущую сцену из sequence
    /// 2) если sequence не знает сцену - активную сцену
    /// 3) если активна MainMenu - fallback на GameScene
    /// </summary>
    private string ResolveCurrentGameplayScene()
    {
        UpdateCurrentLevelIndexFromActiveScene();

        if (TryGetLevelSceneName(currentLevelIndex, out string sceneName))
            return sceneName;

        return SceneManager.GetActiveScene().name == SceneNames.MainMenu
            ? SceneNames.GameScene
            : SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// Hook Unity на загрузку сцены.
    /// Если есть отложенное runtime-состояние, применяет его только в gameplay-сцене.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingPlayerRuntimeState == null)
            return;

        if (!IsGameplayScene(scene.name))
            return;

        ApplyPendingPlayerRuntimeState();
    }

    /// <summary>
    /// Проверяет, что сцена относится к gameplay.
    /// Нужна для защиты: не переносить состояние в Bootstrap/MainMenu/Loading.
    /// </summary>
    private bool IsGameplayScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        return sceneName != SceneNames.Bootstrap
               && sceneName != SceneNames.MainMenu
               && sceneName != SceneNames.Loading;
    }

    /// <summary>
    /// Снимает runtime-состояние текущего игрока перед переходом на следующий уровень.
    /// Важно: это перенос в памяти, а не сохранение на диск.
    /// </summary>
    private PlayerRuntimeState CaptureCurrentPlayerRuntimeState()
    {
        if (!TryResolvePlayerSystems(out PlayerStats playerStats, out PlayerProgression playerProgression, out WeaponManager weaponManager))
        {
            Debug.LogWarning(
                "GameManager: не удалось собрать runtime-состояние игрока. " +
                "Проверить, что на сцене есть Player с компонентами PlayerStats, PlayerProgression и WeaponManager.",
                this);
            return null;
        }

        float healthToTransfer = playerStats.CurrentHealth;
        if (healthToTransfer <= 0f)
        {
            healthToTransfer = playerStats.playerData != null
                ? Mathf.Clamp(playerStats.playerData.maxHealth, 1f, float.MaxValue)
                : 1f;

            Debug.LogWarning(
                "GameManager: при переносе на следующий уровень обнаружен HP <= 0. " +
                $"Применяем безопасное значение HP={healthToTransfer}.",
                this);
        }

        return new PlayerRuntimeState
        {
            Health = healthToTransfer,
            Mana = playerStats.CurrentMana,
            Level = playerProgression.CurrentLevel,
            Experience = playerProgression.CurrentExperience,
            WeaponSlotIndex = weaponManager.CurrentWeaponSlotIndex
        };
    }

    /// <summary>
    /// Применяет ранее сохранённое runtime-состояние к новому экземпляру игрока в следующей сцене.
    /// </summary>
    private void ApplyPendingPlayerRuntimeState()
    {
        PlayerRuntimeState stateToApply = pendingPlayerRuntimeState;
        pendingPlayerRuntimeState = null;

        if (stateToApply == null)
            return;

        if (!TryResolvePlayerSystems(out PlayerStats playerStats, out PlayerProgression playerProgression, out WeaponManager weaponManager))
        {
            Debug.LogWarning(
                "GameManager: не удалось применить runtime-состояние в новой сцене. " +
                "Проверить, что на объекте игрока присутствуют PlayerStats, PlayerProgression и WeaponManager.",
                this);
            return;
        }

        playerProgression.ApplyRuntimeState(stateToApply.Level, stateToApply.Experience);
        playerStats.ApplyRuntimeState(stateToApply.Health, stateToApply.Mana);
        weaponManager.ApplyRuntimeState(stateToApply.WeaponSlotIndex);

        if (stateToApply.ShouldApplyPosition)
            ApplyCheckpointPosition(playerStats.transform, stateToApply.Position);
    }

    /// <summary>
    /// Контракт: ставит игрока в checkpoint-позицию после загрузки сцены и применения числового состояния.
    /// Входные условия: объект игрока уже найден, сцена загружена, позиция пришла из safe-point checkpoint.
    /// Шаги: временно отключить CharacterController, перенести Transform, затем вернуть контроллер в прежнее состояние.
    /// Типичные поломки: позиция внутри стены, checkpoint стоит ниже пола, на сцене несколько игроков.
    /// Что проверить: Transform checkpoint-объекта, NavMesh/коллайдеры рядом, предупреждения GameManager в Console.
    /// </summary>
    private void ApplyCheckpointPosition(Transform playerTransform, Vector3 checkpointPosition)
    {
        if (playerTransform == null)
            return;

        CharacterController characterController = playerTransform.GetComponent<CharacterController>();
        bool wasControllerEnabled = characterController != null && characterController.enabled;

        if (wasControllerEnabled)
            characterController.enabled = false;

        playerTransform.position = checkpointPosition;

        if (wasControllerEnabled)
            characterController.enabled = true;
    }

    /// <summary>
    /// Ищет необходимые компоненты игрока для runtime-переноса.
    /// При дубликатах выбирает первый найденный и пишет предупреждение.
    /// </summary>
    private bool TryResolvePlayerSystems(
        out PlayerStats playerStats,
        out PlayerProgression playerProgression,
        out WeaponManager weaponManager)
    {
        playerStats = FindSingleOrFirst<PlayerStats>("PlayerStats");

        if (playerStats != null)
        {
            playerProgression = playerStats.GetComponent<PlayerProgression>();
            weaponManager = playerStats.GetComponent<WeaponManager>();
        }
        else
        {
            playerProgression = null;
            weaponManager = null;
        }

        if (playerProgression == null)
            playerProgression = FindSingleOrFirst<PlayerProgression>("PlayerProgression");

        if (weaponManager == null)
            weaponManager = FindSingleOrFirst<WeaponManager>("WeaponManager");

        if (playerStats != null && playerProgression != null && weaponManager != null)
            return true;

        string missing = string.Empty;
        if (playerStats == null)
            missing += "PlayerStats ";
        if (playerProgression == null)
            missing += "PlayerProgression ";
        if (weaponManager == null)
            missing += "WeaponManager ";

        Debug.LogWarning(
            $"GameManager: не найдены компоненты для runtime-переноса: {missing}. " +
            "Проверить prefab игрока и ссылки на новой gameplay-сцене.",
            this);

        return false;
    }

    /// <summary>
    /// Ищет компонент на сцене:
    /// если найден один — возвращает его,
    /// если найдено несколько — предупреждает и берёт первый.
    /// Такой подход помогает быстро найти ошибку в сцене.
    /// </summary>
    private T FindSingleOrFirst<T>(string componentName) where T : Object
    {
        T[] found = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (found == null || found.Length == 0)
            return null;

        if (found.Length > 1)
        {
            Debug.LogWarning(
                $"GameManager: найдено несколько компонентов {componentName}. " +
                $"Будет использован первый: {found[0].name}. " +
                "Для предсказуемого поведения оставьте на сцене один объект игрока.",
                this);
        }

        return found[0];
    }

    /// <summary>
    /// Временное состояние игрока для перехода ТОЛЬКО на следующий уровень.
    /// Это runtime-перенос в рамках одного запуска, не save/load на диск.
    /// sealed означает, что от этого класса нельзя наследоваться.
    /// Здесь это просто "контейнер данных" для одного сценария, без дочерних классов.
    /// </summary>
    private sealed class PlayerRuntimeState
    {
        public float Health;
        public float Mana;
        public int Level;
        public float Experience;
        public int WeaponSlotIndex;
        public bool ShouldApplyPosition;
        public Vector3 Position;
    }
}
