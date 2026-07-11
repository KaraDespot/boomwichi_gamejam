using BayatGames.SaveGameFree;
using UnityEngine;

/// <summary>
/// CheckpointSaveSystem
/// Что делает: сохраняет и читает три слота прогресса через пакет Save Game Free.
/// Зачем нужен в игре: окончание уровня становится безопасной точкой прогресса между игровыми сессиями.
/// Связи: GameManager собирает CheckpointSaveData, SaveGame Free пишет данные на диск.
/// Как используется: GameLoopFlowController или CheckpointTrigger вызывают GameManager, а GameManager вызывает эту систему.
/// Расширения: кнопка Continue, отдельные профили, краткое описание save для главного меню.
/// Совет: чаще всего ломаются невалидный индекс слота и случайное изменение строковых ключей.
/// Совет: при ошибке записи проверить Console, импорт `BayatGames.SaveGameFree` и права записи persistent data.
/// </summary>
public static class CheckpointSaveSystem
{
    public const int SlotCount = 3;
    private const string SlotKeyPrefix = "checkpoint_progress_";

    /// <summary>
    /// Контракт: возвращает стабильный ключ Save Game Free для слота 0..2.
    /// Ключи нельзя менять без миграции: старые сохранения лежат на диске именно под этими строками.
    /// Почему так: явная формула ключа проще для проверки, чем массив строк в разных местах проекта.
    /// Потенциальное применение: UI Continue сможет показывать три независимых профиля.
    /// </summary>
    public static string GetSlotKey(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return string.Empty;

        return SlotKeyPrefix + slotIndex;
    }

    /// <summary>
    /// Контракт: сохраняет данные в один из трёх слотов и пишет читаемую JSON-копию для проверки.
    /// Гарантирует true только если успешны оба шага: Save Game Free и debug-JSON.
    /// Если Save Game Free сработал, но JSON не создан, метод возвращает false, чтобы урок не показывал
    /// "checkpoint сохранён" без доказуемого файла для smoke-проверки.
    /// Не гарантирует сохранение врагов, projectile, текущей волны или временных эффектов.
    /// Почему так: каноничный пакет хранит рабочий save, а JSON рядом помогает увидеть структуру данных.
    /// Потенциальное применение: save slots для профилей или разных прохождений.
    /// </summary>
    public static bool Save(int slotIndex, CheckpointSaveData data)
    {
        if (!IsValidSlotIndex(slotIndex))
            return false;

        if (data == null)
        {
            Debug.LogError("CheckpointSaveSystem.Save: data == null.");
            return false;
        }

        string slotKey = GetSlotKey(slotIndex);

        try
        {
            SaveGame.Save(slotKey, data);
            bool jsonExported = CheckpointJsonDebugExporter.Export(slotIndex, data);
            if (!jsonExported)
            {
                Debug.LogWarning(
                    $"CheckpointSaveSystem: основной слот Save Game Free '{slotKey}' записан, " +
                    $"но debug-JSON для слота {slotIndex} не создан. " +
                    "Проверьте права записи в Application.persistentDataPath и Console.");
                return false;
            }

            Debug.Log($"CheckpointSaveSystem: слот {slotIndex} сохранён через Save Game Free с ключом '{slotKey}'.");
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"CheckpointSaveSystem: ошибка сохранения слота {slotIndex}: {exception.Message}");
            return false;
        }
    }

    /// <summary>
    /// Контракт: проверяет наличие checkpoint-слота без применения данных к сцене.
    /// Почему так: урок checkpoint отделяет запись от загрузки, но проверка слота полезна для QA.
    /// Потенциальное применение: включение кнопки Continue в главном меню.
    /// </summary>
    public static bool Exists(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return false;

        string slotKey = GetSlotKey(slotIndex);

        try
        {
            return SaveGame.Exists(slotKey);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"CheckpointSaveSystem: ошибка проверки слота {slotIndex}: {exception.Message}");
            return false;
        }
    }

    /// <summary>
    /// Контракт: проверяет, есть ли хотя бы один сохранённый checkpoint среди трёх слотов.
    /// Метод только читает наличие слотов и не загружает игровые сцены.
    /// Почему так: главное меню может быстро решить, активна ли кнопка Continue, без знания деталей save-данных.
    /// Потенциальное применение: индикатор наличия прогресса в стартовом экране.
    /// </summary>
    public static bool HasAnySave()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (Exists(i))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Контракт: читает сохранённые данные, но не переносит игрока и не грузит сцену.
    /// Почему так: чтение данных безопасно оставить заранее, а сам load-flow относится к следующему уроку.
    /// Потенциальное применение: экран Continue может показать имя сцены и checkpointId.
    /// </summary>
    public static bool TryLoad(int slotIndex, out CheckpointSaveData data)
    {
        data = null;

        if (!IsValidSlotIndex(slotIndex))
            return false;

        if (!Exists(slotIndex))
        {
            return false;
        }

        string slotKey = GetSlotKey(slotIndex);

        try
        {
            data = SaveGame.Load(slotKey, new CheckpointSaveData());
            return data != null;
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"CheckpointSaveSystem: ошибка чтения слота {slotIndex}: {exception.Message}");
            data = null;
            return false;
        }
    }

    /// <summary>
    /// Контракт: удаляет данные выбранного слота перед началом новой игры.
    /// Вызывать из меню после выбора слота New Game. Метод не спрашивает подтверждение и не влияет на другие слоты.
    /// Почему так: подтверждение перезаписи относится к UI-сценарию, а save-система отвечает только за техническую операцию.
    /// Потенциальное применение: кнопка Delete Slot или Reset Progress в отдельном меню профилей.
    /// </summary>
    public static bool Delete(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return false;

        string slotKey = GetSlotKey(slotIndex);

        try
        {
            if (!SaveGame.Exists(slotKey))
                return true;

            SaveGame.Delete(slotKey);
            CheckpointJsonDebugExporter.Delete(slotIndex);
            Debug.Log($"CheckpointSaveSystem: слот {slotIndex} очищен.");
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"CheckpointSaveSystem: ошибка очистки слота {slotIndex}: {exception.Message}");
            return false;
        }
    }

    /// <summary>
    /// Контракт: проверяет диапазон 0..2 перед любым обращением к Save Game Free.
    /// Почему так: невалидный индекс создаёт неправильный ключ, а значит save может оказаться не там, где ожидается.
    /// Потенциальное применение: та же проверка пригодится для UI выбора слота.
    /// </summary>
    private static bool IsValidSlotIndex(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < SlotCount)
            return true;

        Debug.LogError(
            $"CheckpointSaveSystem: неверный индекс слота {slotIndex}. " +
            $"Допустимый диапазон: 0..{SlotCount - 1}.");
        return false;
    }
}
