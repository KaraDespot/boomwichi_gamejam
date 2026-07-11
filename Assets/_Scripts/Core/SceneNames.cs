/*
 * SceneNames
 * Назначение: централизованное хранилище строковых имён сцен.
 * Что делает: задаёт константы для использования в коде вместо "магических" строк.
 * Связи: используется GameManager, BootstrapManager, SceneLoader и другими менеджерами сцен.
 * Паттерны: Constants/Configuration, Single Responsibility.
 */

using UnityEngine;

public static class SceneNames 
{
    public const string Bootstrap = "Bootstrap";
    public const string MainMenu = "MainMenu";
    public const string Loading = "Loading";
    public const string GameScene = "GameScene";
}
