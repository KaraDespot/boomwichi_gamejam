using System;
using System.IO;
using UnityEngine;

/// <summary>
/// CheckpointJsonDebugExporter
/// Что делает: пишет читаемую JSON-копию checkpoint-save рядом с рабочим сохранением Save Game Free.
/// Зачем нужен в игре: файл можно открыть руками и увидеть, какие данные реально попали в сохранение.
/// Связи: вызывается CheckpointSaveSystem после успешной записи Save Game Free.
/// Как используется: каждый слот получает отдельный файл checkpoint_slot_0.json / 1 / 2 в persistentDataPath.
/// Расширения: кнопка "Открыть папку save", экспорт нескольких файлов, сравнение слотов.
/// Совет: JSON-правка подходит для отладки и обучения, но не защищает от читов.
/// Совет: на Windows путь обычно находится внутри профиля пользователя в папке AppData/LocalLow.
/// </summary>
public static class CheckpointJsonDebugExporter
{
    private const string FileNameFormat = "checkpoint_slot_{0}.json";

    /// <summary>
    /// Контракт: экспортирует pretty-print JSON для слота 0..2 в Application.persistentDataPath.
    /// Гарантирует лог с полным путём при успешной записи.
    /// Не заменяет Save Game Free: это только читаемая debug-копия для проверки урока.
    /// Почему так: persistentDataPath одинаково работает в Editor и в билде, а JSON легко открыть текстовым редактором.
    /// Потенциальное применение: ручная проверка playerLevel, health, nextSceneName перед уроком загрузки.
    /// </summary>
    public static bool Export(int slotIndex, CheckpointSaveData data)
    {
        if (slotIndex < 0 || slotIndex >= CheckpointSaveSystem.SlotCount)
        {
            Debug.LogError(
                $"CheckpointJsonDebugExporter: неверный индекс слота {slotIndex}. " +
                $"Допустимый диапазон: 0..{CheckpointSaveSystem.SlotCount - 1}.");
            return false;
        }

        if (data == null)
        {
            Debug.LogError("CheckpointJsonDebugExporter.Export: data == null.");
            return false;
        }

        try
        {
            string fileName = string.Format(FileNameFormat, slotIndex);
            string path = Path.Combine(Application.persistentDataPath, fileName);
            string json = JsonUtility.ToJson(data, true);

            File.WriteAllText(path, json);
            Debug.Log($"Checkpoint JSON written: {path}");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"CheckpointJsonDebugExporter: ошибка записи JSON для слота {slotIndex}: {exception.Message}");
            return false;
        }
    }

    /// <summary>
    /// Контракт: удаляет debug-JSON выбранного слота, если файл существует.
    /// Метод используется вместе с очисткой основного Save Game Free слота и не создаёт новый файл.
    /// Почему так: читаемая JSON-копия должна показывать актуальное состояние слота, а не старый прогресс после New Game.
    /// Потенциальное применение: отдельная кнопка очистки слота в меню профилей.
    /// </summary>
    public static bool Delete(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= CheckpointSaveSystem.SlotCount)
            return false;

        try
        {
            string fileName = string.Format(FileNameFormat, slotIndex);
            string path = Path.Combine(Application.persistentDataPath, fileName);

            if (File.Exists(path))
                File.Delete(path);

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"CheckpointJsonDebugExporter: ошибка удаления JSON для слота {slotIndex}: {exception.Message}");
            return false;
        }
    }
}
