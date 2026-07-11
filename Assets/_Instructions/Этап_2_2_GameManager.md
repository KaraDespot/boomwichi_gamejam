# Урок 2.2: GameManager — состояния игры (Menu/Playing/Paused)

### Зачем
Чтобы в проекте была "правда в одном месте": сейчас мы в меню? играем? на паузе?

### Требования к `GameManager`
- Singleton-подобный доступ (например, `GameManager.Instance`)
- Хранит текущее состояние:
  - `Menu`
  - `Playing`
  - `Paused`
- Методы:
  - `StartGame()` → ставит `Playing`, грузит `GameScene`
  - `GoToMenu()` → ставит `Menu`, грузит `MainMenu`
  - `Pause()` / `Resume()` → переключает паузу

### Пауза (важное для учеников)
Есть два основных подхода:

- **Простой (подходит большинству учебных проектов)**:
  - `Time.timeScale = 0` на паузе
  - `Time.timeScale = 1` при продолжении
  - UI продолжает работать (если EventSystem есть)

- **Более гибкий**:
  - не менять `timeScale`, а отключать управление/AI/анимации вручную
  - полезно для онлайн-игр и сложных систем

Для обучения выбирайте **первый**.

### Пример кода `GameManager`

```csharp
using UnityEngine;

public enum GameState
{
    Menu,
    Playing,
    Paused,
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Menu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        SceneLoader.Instance.Load(SceneNames.GameScene);
        Debug.Log("Game started");
    }

    public void GoToMenu()
    {
        CurrentState = GameState.Menu;
        Time.timeScale = 1f;
        SceneLoader.Instance.Load(SceneNames.MainMenu);
        Debug.Log("Go to Main Menu");
    }

    public void Pause()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Paused;
        Time.timeScale = 0f; // простой вариант паузы
        EventBus.Instance.RaiseGamePaused();
        Debug.Log("Game paused");
    }

    public void Resume()
    {
        if (CurrentState != GameState.Paused)
            return;

        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        EventBus.Instance.RaiseGameResumed();
        Debug.Log("Game resumed");
    }
}
```

---

### Объяснение: Что такое `enum` (перечисление)?

**Enum** — это способ создать список именованных констант (вариантов выбора).

**Проблема без `enum`:**
```csharp
// Плохо: можно написать любое число, даже неправильное
int state = 0; // что это значит?
int state = 5; // такого состояния нет!
int state = -1; // ошибка!
```

**Решение с `enum`:**
```csharp
// Хорошо: можно выбрать только из списка
GameState state = GameState.Menu; // ✅ понятно
GameState state = GameState.Playing; // ✅ правильно
// GameState state = GameState.WrongState; // ❌ ошибка компиляции!
```

**Как работает `enum`:**
```csharp
public enum GameState
{
    Menu,      // автоматически = 0
    Playing,   // автоматически = 1
    Paused    // автоматически = 2
}

// Использование:
GameState current = GameState.Menu;
if (current == GameState.Playing)
{
    // игра идет
}
```

**Преимущества:**
- ✅ Понятные имена вместо чисел
- ✅ Защита от ошибок (нельзя использовать несуществующее значение)
- ✅ Автодополнение в редакторе

**Аналогия:** Как меню в кафе — можно выбрать только то, что есть в списке, а не придумывать блюда.

---

### Объяснение: Что такое Singleton (одиночка)?

**Проблема:** Нужен один `GameManager` на всю игру, но что если создать два?

**Без Singleton:**
```csharp
// В сцене MainMenu:
GameManager manager1 = new GameManager();

// В сцене GameScene:
GameManager manager2 = new GameManager(); // ❌ теперь два менеджера!

// Который правильный? Какой использовать?
```

**С Singleton:**
```csharp
// Всегда один и тот же объект
GameManager.Instance.StartGame(); // ✅ всегда работает
GameManager.Instance.Pause();     // ✅ всегда тот же менеджер
```

**Как работает Singleton в коде:**

```csharp
public class GameManager : MonoBehaviour
{
    // Статическая переменная - одна на всю программу
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // Если Instance уже существует (не null) и это не мы
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // уничтожаем дубликат
            return; // выходим из метода
        }

        // Если Instance пустой, значит мы первый
        Instance = this; // запоминаем себя как единственный экземпляр
        DontDestroyOnLoad(gameObject); // не уничтожать при смене сцены
    }
}
```

**Как это работает пошагово:**

1. **Первый раз** (в Bootstrap):
   - `Instance` = `null` (пусто)
   - Проверка `if (Instance != null)` → `false`, пропускаем
   - `Instance = this` → сохраняем себя
   - Менеджер живет дальше

2. **Второй раз** (если попытались создать еще один):
   - `Instance` уже не `null` (там первый менеджер)
   - Проверка `if (Instance != null && Instance != this)` → `true`
   - `Destroy(gameObject)` → уничтожаем дубликат
   - `return` → выходим, не продолжаем

**Использование:**
```csharp
// В любом месте кода:
GameManager.Instance.StartGame(); // обращаемся к единственному экземпляру
GameManager.Instance.Pause();
```

**Аналогия:** Как директор школы — может быть только один директор, а не десять.

---

### Объяснение: Что такое свойства (Properties) — `get` и `set`?

**Обычная переменная:**
```csharp
public int health = 100; // можно читать И менять откуда угодно

// В другом скрипте:
player.health = -50; // ❌ можно установить отрицательное!
player.health = 9999; // ❌ можно установить слишком большое!
```

**Свойство с `get` и `set`:**
```csharp
private int _health = 100; // приватная переменная (скрыта)

// Свойство - контролируемый доступ
public int Health 
{ 
    get { return _health; }        // читать можно
    set { _health = value; }       // менять можно
}
```

**Свойство с `private set` (только чтение снаружи):**
```csharp
public GameState CurrentState { get; private set; } = GameState.Menu;

// Что это значит:
// ✅ get - можно читать: GameManager.Instance.CurrentState
// ❌ set - нельзя менять снаружи (только внутри класса)
```

**Пример использования:**

```csharp
// В GameManager:
public GameState CurrentState { get; private set; } = GameState.Menu;

// Внутри GameManager можно менять:
CurrentState = GameState.Playing; // ✅ работает

// В другом скрипте:
GameManager.Instance.CurrentState = GameState.Paused; // ❌ ОШИБКА!
// Можно только читать:
GameState state = GameManager.Instance.CurrentState; // ✅ работает
```

**Зачем `private set`?**
- Защита от случайных изменений извне
- Менять состояние можно только через методы (`StartGame()`, `Pause()`)
- Гарантия, что состояние всегда корректное

**Аналогия:** Как счетчик в банке — смотреть баланс можно, но менять только через операции (пополнение/снятие), а не напрямую.

---

### Объяснение: Что такое `static`?

**Обычная переменная** — у каждого объекта своя:
```csharp
public class Player
{
    public string name; // у каждого игрока свое имя
}

Player player1 = new Player();
player1.name = "Анна";

Player player2 = new Player();
player2.name = "Борис"; // разные имена
```

**Статическая переменная** — одна на всю программу:
```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // ОДИН на всю программу
}

// Использование:
GameManager.Instance.StartGame(); // обращаемся БЕЗ создания объекта
// НЕ нужно: GameManager gm = new GameManager();
```

**Отличия:**

| Обычная переменная | Статическая переменная |
|-------------------|------------------------|
| `public int health;` | `public static int score;` |
| У каждого объекта своя | Одна на всю программу |
| `player.health` | `GameManager.Instance` |
| Нужен объект | Не нужен объект |

**Когда использовать `static`:**
- Когда нужен один экземпляр на всю программу (Singleton)
- Когда данные общие для всех (например, общий счет)
- Когда не нужны объекты (утилиты, константы)

---

### Объяснение: Что такое `DontDestroyOnLoad`?

**Проблема:** При загрузке новой сцены Unity уничтожает все объекты старой сцены.

**Без `DontDestroyOnLoad`:**
```csharp
// В сцене MainMenu:
GameManager manager = ...; // создали менеджер

// Загрузили GameScene:
// ❌ GameManager уничтожен! Теперь его нет!
```

**С `DontDestroyOnLoad`:**
```csharp
DontDestroyOnLoad(gameObject); // "не уничтожай этот объект при смене сцены"

// Загрузили GameScene:
// ✅ GameManager все еще существует!
```

**Как работает:**
- Обычные объекты → уничтожаются при смене сцены
- Объекты с `DontDestroyOnLoad` → остаются жить между сценами

**Зачем это нужно:**
- `GameManager` должен жить во всех сценах
- `SceneLoader` нужен для переключения сцен
- `EventBus` должен работать везде

**Аналогия:** Как паспорт — он остается с тобой, даже когда переезжаешь в другой город.

---

### Объяснение: `Awake()` vs `Start()` — в чем разница?

**Порядок вызова в Unity:**

1. **`Awake()`** — вызывается **первым**, когда объект создается
   - Вызывается даже если объект неактивен
   - Используется для инициализации, настройки ссылок
   - Все `Awake()` вызываются до всех `Start()`

2. **`Start()`** — вызывается **после** всех `Awake()`
   - Вызывается только если объект активен
   - Используется для логики, которая зависит от других объектов

**Пример:**

```csharp
public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        // ✅ Здесь инициализация Singleton
        // ✅ Здесь DontDestroyOnLoad
        // ✅ Здесь создание других менеджеров
        // Все это должно произойти ДО того, как другие объекты начнут работать
    }

    private void Start()
    {
        // ✅ Здесь можно использовать другие менеджеры
        // ✅ Здесь можно обращаться к GameManager.Instance из других скриптов
        // Потому что в Awake() все уже создано
    }
}
```

**Почему `Awake()` для Singleton?**
- Нужно установить `Instance` **до** того, как другие объекты попытаются его использовать
- `Awake()` гарантированно вызовется первым

**Правило:** Инициализация → `Awake()`, логика → `Start()`.

---

### Объяснение: Что такое `Time.timeScale`?

**`Time.timeScale`** — это "скорость времени" в игре.

**Значения:**
- `Time.timeScale = 1f` → нормальная скорость (1x)
- `Time.timeScale = 0.5f` → медленнее в 2 раза (0.5x)
- `Time.timeScale = 0f` → время остановлено (пауза)
- `Time.timeScale = 2f` → быстрее в 2 раза (2x)

**Что влияет:**
- ✅ Физика (Rigidbody)
- ✅ Анимации
- ✅ `Time.deltaTime`
- ❌ UI (продолжает работать)
- ❌ `Time.realtimeSinceStartup` (реальное время)

**Пример использования:**

```csharp
public void Pause()
{
    Time.timeScale = 0f; // останавливаем время
    // Теперь:
    // - Персонаж замер
    // - Враги замерли
    // - Анимации остановились
    // - UI продолжает работать (можно нажать кнопку)
}

public void Resume()
{
    Time.timeScale = 1f; // возобновляем нормальную скорость
    // Все снова работает
}
```

**Аналогия:** Как кнопка паузы на видео — останавливает воспроизведение, но интерфейс (кнопки) продолжает работать.

---

### Объяснение: Что такое `return` в методе?

**`return`** — выход из метода (прекращение выполнения).

**Без `return`:**
```csharp
public void Pause()
{
    if (CurrentState != GameState.Playing)
    {
        // что-то сделать, но метод продолжит выполняться
    }
    
    CurrentState = GameState.Paused; // ❌ выполнится даже если состояние неправильное!
}
```

**С `return`:**
```csharp
public void Pause()
{
    if (CurrentState != GameState.Playing)
        return; // ✅ выходим из метода, дальше не выполняем
    
    // Этот код выполнится ТОЛЬКО если состояние = Playing
    CurrentState = GameState.Paused;
    Time.timeScale = 0f;
}
```

**Зачем это нужно:**
- **Защита от ошибок** — не выполняем код, если условия не выполнены
- **Ранний выход** — если что-то не так, сразу выходим

**Пример пошагово:**

```csharp
public void Pause()
{
    // Шаг 1: Проверяем состояние
    if (CurrentState != GameState.Playing)
        return; // Если НЕ Playing → выходим, дальше не идем
    
    // Шаг 2: Этот код выполнится ТОЛЬКО если состояние = Playing
    CurrentState = GameState.Paused;
    Time.timeScale = 0f;
    Debug.Log("Game paused");
}
```

**Сценарий 1:** `CurrentState = GameState.Menu`
- Проверка: `Menu != Playing` → `true`
- `return` → выходим
- Код ниже не выполняется ✅

**Сценарий 2:** `CurrentState = GameState.Playing`
- Проверка: `Playing != Playing` → `false`
- Пропускаем `return`
- Выполняем код ниже ✅

**Аналогия:** Как проверка билета — если билета нет, не пускаем дальше (`return`), если есть — пропускаем.

---

### Проверка
Все еще никак не проверим, потому что не дописали, но: 
- В консоли (Debug.Log) видно смену состояния при нажатии `Esc`
- В паузе персонаж/физика "замерли" (если вы используете timeScale)
