/*
 * InputTest
 * Назначение: отладочный скрипт для проверки, что `InputManager` корректно читает ввод.
 * Что делает: каждый кадр выводит в консоль ненулевые оси Move/Look и одноразовые нажатия Jump/Attack.
 * Связи: использует `InputManager` (Singleton). Не нужен в релизе — оставлять только для разработки.
 */

using UnityEngine;

public class InputTest : MonoBehaviour
{
    /// <summary>
    /// Проверяет ввод каждый кадр и выводит в консоль ненулевые оси Move/Look и одноразовые нажатия Jump/Attack.
    /// </summary>
    private void Update()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogWarning("InputManager не найден!");
            return;
        }

        Vector2 move = InputManager.Instance.MoveInput;
        Vector2 look = InputManager.Instance.LookInput;

        if (move != Vector2.zero)
            Debug.Log($"Move: {move}");

        if (look != Vector2.zero)
            Debug.Log($"Look: {look}");

        if (InputManager.Instance.JumpPressed)
            Debug.Log("Jump pressed!");

        if (InputManager.Instance.AttackPressed)
            Debug.Log("Attack pressed!");

        // Сбрасывает флаги нажатий для следующего кадра.
        InputManager.Instance.ResetButtonFlags();
    }
}
