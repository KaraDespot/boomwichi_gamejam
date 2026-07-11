using UnityEditor;
using UnityEngine;

/// <summary>
/// CheckpointSaveDevTools
/// Что делает: добавляет editor-инструмент для очистки всех checkpoint save-слотов.
/// Зачем нужен в проекте: при проверке урока 14 часто требуется быстро вернуться к состоянию "нет сохранений".
/// Связи: использует CheckpointSaveSystem.Delete, поэтому очищает те же три слота, что видит игровое меню.
/// Как используется: пункт меню Tools/Save Tools/Clear Checkpoint Saves запускается вручную в Unity Editor.
/// Возможные расширения: очистка одного выбранного слота, открытие persistentDataPath, вывод содержимого слотов.
/// Совет: если Continue остаётся активной после очистки, перезапустить MainMenu или проверить RefreshContinueButtonState.
/// Совет: если файл JSON остался на диске, проверить Console и права записи в Application.persistentDataPath.
/// </summary>
public static class CheckpointSaveDevTools
{
    /// <summary>
    /// Контракт: вручную очищает все checkpoint save-слоты из Unity Editor.
    /// Входные условия: проект открыт в Editor, Save Game Free доступен, пользователь подтвердил операцию в dialog.
    /// Шаги: запросить подтверждение, пройти по слотам 0..2, вызвать CheckpointSaveSystem.Delete для каждого, вывести итог в Console.
    /// Типичные поломки: SaveGame Free не может удалить файл, persistentDataPath недоступен, старый MainMenu уже закэшировал состояние Continue.
    /// Что проверить: Console, persistentDataPath и состояние кнопки Continue после повторного входа в MainMenu.
    /// </summary>
    [MenuItem("Tools/Save Tools/Clear Checkpoint Saves", priority = 200)]
    private static void ClearCheckpointSaves()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Clear Checkpoint Saves",
            "Будут очищены все checkpoint save-слоты урока 14. Операция нужна только для проверки и не затрагивает сцены или префабы.",
            "Очистить",
            "Отмена");

        if (!confirmed)
            return;

        int clearedCount = 0;
        for (int i = 0; i < CheckpointSaveSystem.SlotCount; i++)
        {
            if (CheckpointSaveSystem.Delete(i))
                clearedCount++;
        }

        Debug.Log($"CheckpointSaveDevTools: очищено save-слотов {clearedCount}/{CheckpointSaveSystem.SlotCount}.");
    }
}
