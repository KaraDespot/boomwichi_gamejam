using UnityEngine;

/// <summary>
/// Временный скрипт для проверки работы PlayerProgression.
/// </summary>
public class PlayerExperienceTest : MonoBehaviour
{
    public PlayerProgression progression;

    private void Awake()
    {
        if (progression == null)
            progression = FindFirstObjectByType<PlayerProgression>();
    }

    private void Update()
    {
        if (progression == null)
            return;

        // Добавить 50 опыта по нажатию клавиши X
        if (Input.GetKeyDown(KeyCode.X))
        {
            progression.AddExperience(50f);
        }
    }
}