using System;
using UnityEngine;

/*
 * LevelSequenceData
 * Назначение: хранит порядок игровых сцен для vertical slice прохождения.
 * Зачем нужен: мы не хардкодим порядок уровней в GameManager, а выносим его в данные.
 * Как реализовано: ScriptableObject со строковыми именами сцен в том порядке,
 * в котором игрок должен проходить уровни.
 */
[CreateAssetMenu(
    fileName = "LevelSequenceData",
    menuName = "Game Data/Levels/Level Sequence",
    order = 0)]
public class LevelSequenceData : ScriptableObject
{
    [Tooltip("Ordered list of gameplay scenes for the vertical slice run.")]
    [SerializeField] private string[] levelSceneNames = { SceneNames.GameScene };

    /// <summary>
    /// Количество уровней в последовательности.
    /// </summary>
    public int LevelCount => levelSceneNames != null ? levelSceneNames.Length : 0;

    /// <summary>
    /// Безопасно возвращает имя сцены по индексу.
    /// Возвращает false, если индекс выходит за границы или имя пустое.
    /// </summary>
    public bool TryGetLevelSceneName(int index, out string sceneName)
    {
        sceneName = null;

        if (levelSceneNames == null || index < 0 || index >= levelSceneNames.Length)
            return false;

        string value = levelSceneNames[index];
        if (string.IsNullOrWhiteSpace(value))
            return false;

        sceneName = value.Trim();
        return true;
    }

    /// <summary>
    /// Находит индекс уровня по имени сцены.
    /// Нужен GameManager, чтобы понять, какой уровень активен сейчас.
    /// </summary>
    public int FindLevelIndex(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || levelSceneNames == null)
            return -1;

        for (int i = 0; i < levelSceneNames.Length; i++)
        {
            if (string.Equals(levelSceneNames[i], sceneName, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }
}
