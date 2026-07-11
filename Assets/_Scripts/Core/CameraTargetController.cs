/*
 * CameraTargetController
 * Назначение: совместимый остаток RPG-шаблона для объектов, где компонент уже мог быть назначен.
 * Что делает: больше не читает ввод мыши, потому что Boomwichi использует фиксированную камеру сверху.
 * Связи: старые prefabs/scene objects; новая сцена стола не должна зависеть от этого компонента.
 * Паттерны: Compatibility Adapter.
 */

using UnityEngine;

public class CameraTargetController : MonoBehaviour
{
    [Header("Legacy Settings")]
    [Tooltip("Оставлено для старых сцен; в Boomwichi вращение камеры отключено.")]
    [SerializeField] private float mouseSensitivity = 0.3f;

    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
    }

    public float GetMouseSensitivity()
    {
        return mouseSensitivity;
    }
}
