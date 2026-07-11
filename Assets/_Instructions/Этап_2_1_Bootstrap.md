# Урок 2.1: Bootstrap — точка входа и порядок инициализации

### Идея
**Bootstrap-сцена** запускается первой и:
1) создает менеджеры (которые живут между сценами),  
2) подготавливает "проектные сервисы",  
3) переводит игру в меню.

### В Unity (шаги)
1. Откройте сцену `Bootstrap`.
2. В Hierarchy создайте пустой объект `BootstrapManager`.
3. Создайте скрипт `BootstrapManager.cs` и добавьте на объект.
4. В `Build Settings` убедитесь, что `Bootstrap` стоит **первой**.

### Что должен делать `BootstrapManager` (логика)
- В `Awake()` (или `Start()`):
  - проверить, что второй раз не создается (защита от дублей)
  - создать/поднять менеджеры:
    - `GameManager`
    - `SceneLoader`
    - `EventBus`
    - (позже добавятся: SettingsManager, AudioManager, InputManager…)
  - сделать их `DontDestroyOnLoad`
  - загрузить `MainMenu`

### Пример кода `BootstrapManager` 
Ниже — **полный пример**, который можно почти без изменений вставлять в проект.

```csharp
using UnityEngine;

public class BootstrapManager : MonoBehaviour
{
    // Защита от повторной инициализации, если Bootstrap загрузится повторно
    private static bool _initialized;

    private void Awake()
    {
        // Защита от дублей, если вдруг Bootstrap окажется в сцене дважды
        if (_initialized)
        {
            Destroy(gameObject);
            return;
        }

        _initialized = true;
        DontDestroyOnLoad(gameObject);

        // Создаём/поднимаем основные менеджеры
        CreateGameManager();
        CreateSceneLoader();
        CreateEventBus();

        // Загружаем главное меню
        SceneLoader.Instance.Load(SceneNames.MainMenu);
    }

    private static void CreateGameManager()
    {
        GameManager existing = FindFirstObjectByType<GameManager>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        DontDestroyOnLoad(go);
    }

    private static void CreateSceneLoader()
    {
        SceneLoader existing = FindFirstObjectByType<SceneLoader>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("SceneLoader");
        go.AddComponent<SceneLoader>();
        DontDestroyOnLoad(go);
    }

    private static void CreateEventBus()
    {
        EventBus existing = FindFirstObjectByType<EventBus>();
        if (existing != null)
        {
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject go = new GameObject("EventBus");
        go.AddComponent<EventBus>();
        DontDestroyOnLoad(go);
    }
}
```

Дополнительно удобно завести простой класс с именами сцен:

```csharp
public static class SceneNames
{
    public const string Bootstrap = "Bootstrap";
    public const string MainMenu = "MainMenu";
    public const string GameScene = "GameScene";
}
```

### Объяснение: Зачем нужен `SceneNames`?

**Проблема без `SceneNames`:**
```csharp
// Плохо: если опечатка, игра сломается, и ошибку заметишь только при запуске
SceneManager.LoadScene("MainMen"); // опечатка!
```

**Решение с `SceneNames`:**
```csharp
// Хорошо: если опечатка, код не скомпилируется - ошибку увидишь сразу
SceneManager.LoadScene(SceneNames.MainMen); // ошибка компиляции!
SceneManager.LoadScene(SceneNames.MainMenu); // правильно
```

**Преимущества:**
- ✅ Защита от опечаток (ошибка видна сразу при написании кода)
- ✅ Автодополнение в редакторе (IDE подскажет доступные сцены)
- ✅ Легко переименовать сцену (меняешь в одном месте)
- ✅ Понятный код (`SceneNames.MainMenu` читается лучше, чем `"MainMenu"`)

---

### Объяснение: Что такое `static class`?

**Обычный класс** — нужно создавать объект (экземпляр):
```csharp
public class Player
{
    public string name;
}

// Использование:
Player player1 = new Player(); // создали объект
player1.name = "Игрок 1";
Player player2 = new Player(); // создали еще один объект
player2.name = "Игрок 2";
```

**Статический класс** — объект создавать нельзя, используется напрямую:
```csharp
public static class SceneNames
{
    public const string MainMenu = "MainMenu";
}

// Использование:
// НЕЛЬЗЯ: SceneNames names = new SceneNames(); // ❌ ошибка!
// НУЖНО: SceneNames.MainMenu // ✅ правильно
```

**Отличия:**

| Обычный класс | Статический класс |
|---------------|-------------------|
| Можно создать много объектов | Объекты создавать нельзя |
| `Player player = new Player();` | `SceneNames.MainMenu` (без `new`) |
| Каждый объект хранит свои данные | Один набор данных на всю программу |
| Подходит для игроков, врагов, предметов | Подходит для утилит, констант, помощников |

**Когда использовать `static class`:**
- Когда нужны только константы (как `SceneNames`)
- Когда нужны вспомогательные методы, которые не хранят состояние
- Когда не нужны объекты (например, `Math.PI`, `Math.Sqrt()`)

---

### Объяснение: Что такое `const` переменные?

**Обычная переменная** — можно менять:
```csharp
public class GameManager : MonoBehaviour
{
    public string currentScene = "MainMenu"; // обычная переменная
    
    void Start()
    {
        currentScene = "GameScene"; // ✅ можно изменить
        currentScene = "Bootstrap"; // ✅ можно изменить снова
    }
}
```

**Константа (`const`)** — нельзя менять после создания:
```csharp
public static class SceneNames
{
    public const string MainMenu = "MainMenu"; // константа
    
    // В методе:
    // MainMenu = "ДругоеМеню"; // ❌ ОШИБКА! Константу менять нельзя
}
```

**Отличия:**

| Обычная переменная | Константа (`const`) |
|-------------------|---------------------|
| Можно менять значение | Значение задается один раз и навсегда |
| `string name = "А"; name = "Б";` ✅ | `const string name = "А"; name = "Б";` ❌ |
| Значение может быть разным в разное время | Значение всегда одно и то же |
| Подходит для: текущее здоровье, счет | Подходит для: имена сцен, настройки по умолчанию |

**Почему `const` для имен сцен?**
- Имена сцен не должны меняться во время работы программы
- Это защита от случайных ошибок
- Компилятор может оптимизировать код с константами

**Пример использования:**
```csharp
// Вместо:
SceneManager.LoadScene("MainMenu"); // строка - можно опечататься

// Используем:
SceneManager.LoadScene(SceneNames.MainMenu); // константа - безопасно
```

---

### Проверка
А никак вы не проверите, потому что надо еще написать недостающие скрипты, но как это должно быдет работать, когда мы их напишем:
- Запускаете Play → через секунду оказываетесь в `MainMenu`
- В сцене `MainMenu` в Hierarchy (в Play Mode) видите объекты менеджеров с отметкой `DontDestroyOnLoad`

### Частые ошибки
- **Bootstrap не первая сцена** → менеджеры не создаются.
- **Менеджеры создаются и в Bootstrap, и в сценах** → появляются дубли.
