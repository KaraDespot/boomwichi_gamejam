# Урок 2.4: EventBus — простая система событий (подробная инструкция)

## Зачем нужен EventBus?

**Проблема без EventBus:**
```csharp
// Плохо: UI держит прямую ссылку на GameManager
public class PauseUI : MonoBehaviour
{
    public GameManager gameManager; // нужно вручную присваивать в Inspector
    
    void OnButtonClick()
    {
        gameManager.Pause(); // если gameManager == null, ошибка!
    }
}
```

**Решение с EventBus:**
```csharp
// Хорошо: UI подписывается на событие, не знает о GameManager
public class PauseUI : MonoBehaviour
{
    void OnEnable()
    {
        EventBus.Instance.OnGamePaused += ShowPausePanel; // подписываемся
    }
    
    void OnDisable()
    {
        EventBus.Instance.OnGamePaused -= ShowPausePanel; // отписываемся
    }
    
    void ShowPausePanel()
    {
        // показать панель паузы
    }
}
```

**Примеры использования:**
- `PlayerDied` → UI показывает экран смерти
- `QuestCompleted` → выдать награду + обновить журнал квестов
- `SettingsChanged` → применить громкость/разрешение
- `OnGamePaused` → показать панель паузы, остановить музыку
- `OnGameResumed` → скрыть панель паузы, возобновить музыку

---

## Пример кода `EventBus`

```csharp
using System;
using UnityEngine;

public class EventBus : MonoBehaviour
{
    public static EventBus Instance { get; private set; }

    // События - можно подписаться из любого места
    public event Action OnGamePaused;
    public event Action OnGameResumed;

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

    // Методы для "вызова" событий (raise/trigger)
    public void RaiseGamePaused()
    {
        OnGamePaused?.Invoke(); // вызываем событие, если есть подписчики
    }

    public void RaiseGameResumed()
    {
        OnGameResumed?.Invoke();
    }
}
```

---

## Пример использования EventBus

### В GameManager (отправляем событие):

```csharp
public void Pause()
{
    CurrentState = GameState.Paused;
    Time.timeScale = 0f;
    EventBus.Instance.RaiseGamePaused(); // отправляем событие
}
```

### В PauseUI (получаем событие):

```csharp
public class PauseUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private void OnEnable()
    {
        // Подписываемся на событие
        EventBus.Instance.OnGamePaused += ShowPausePanel;
        EventBus.Instance.OnGameResumed += HidePausePanel;
    }

    private void OnDisable()
    {
        // Отписываемся от события (ВАЖНО!)
        EventBus.Instance.OnGamePaused -= ShowPausePanel;
        EventBus.Instance.OnGameResumed -= HidePausePanel;
    }

    private void ShowPausePanel()
    {
        pausePanel.SetActive(true);
    }

    private void HidePausePanel()
    {
        pausePanel.SetActive(false);
    }
}
```

---

## Объяснение: Что такое события (Events) в C#?

**Событие** — это способ уведомить другие объекты о том, что что-то произошло.

**Аналогия из жизни:**
- Звонок в дверь — событие
- Люди в квартире — подписчики на событие
- Когда звонят, все идут открывать дверь

**В программировании:**
```csharp
// Объявляем событие
public event Action OnGamePaused;

// Подписываемся на событие (кто-то хочет узнать, когда игра на паузе)
EventBus.Instance.OnGamePaused += MyMethod;

// Вызываем событие (игра поставлена на паузу)
EventBus.Instance.RaiseGamePaused();

// Все подписчики получают уведомление и выполняют свои методы
```

**Как это работает:**
1. **Объявление события:** `public event Action OnGamePaused;`
2. **Подписка:** `OnGamePaused += MyMethod;` (добавляем метод в список)
3. **Вызов:** `OnGamePaused?.Invoke();` (вызываем все методы из списка)
4. **Отписка:** `OnGamePaused -= MyMethod;` (убираем метод из списка)

**Преимущества:**
- ✅ Объекты не знают друг о друге напрямую
- ✅ Легко добавить новых подписчиков
- ✅ Легко убрать подписчиков
- ✅ Код становится более гибким

---

## Объяснение: Что такое `Action`?

**`Action`** — это тип делегата (указатель на метод) без параметров.

**Простыми словами:** `Action` — это "ящик", в который можно положить методы.

**Пример:**
```csharp
// Объявляем Action
Action myAction;

// Кладем в него метод
myAction = MyMethod;

// Вызываем все методы в Action
myAction?.Invoke(); // вызовет MyMethod()
```

**`Action` с параметрами (на будущее):**
```csharp
// Action без параметров
Action onPaused;

// Action с одним параметром
Action<int> onCoinsChanged;

// Action с двумя параметрами
Action<int, string> onPlayerLevelUp; // уровень, имя игрока
```

**В EventBus:**
```csharp
public event Action OnGamePaused; // событие без параметров
```

**Использование:**
```csharp
// Подписка на событие без параметров
EventBus.Instance.OnGamePaused += ShowPausePanel;

// Подписка на Action с параметром (пример на будущее)
onCoinsChanged += (coins) =>
{
    Debug.Log($"Монет стало: {coins}");
};
```

**Аналогия:** Как список контактов в телефоне — можно добавить несколько номеров, и когда звонишь, все получают звонок.

---

## Объяснение: Что такое `?.Invoke()` (null-conditional operator)?

**`?.Invoke()`** — это безопасный вызов события (проверяет, не `null` ли событие).

**Проблема без `?`:**
```csharp
// Плохо: если OnGamePaused == null, будет ошибка!
OnGamePaused.Invoke(); // ❌ NullReferenceException, если нет подписчиков
```

**Решение с `?`:**
```csharp
// Хорошо: если OnGamePaused == null, ничего не произойдет
OnGamePaused?.Invoke(); // ✅ безопасно, даже если нет подписчиков
```

**Как работает:**
- Если `OnGamePaused != null` → вызываем `Invoke()`
- Если `OnGamePaused == null` → ничего не делаем

**Пример:**
```csharp
// Нет подписчиков
OnGamePaused?.Invoke(); // ничего не произойдет, ошибки не будет

// Есть подписчики
EventBus.Instance.OnGamePaused += Method1;
EventBus.Instance.OnGamePaused += Method2;
OnGamePaused?.Invoke(); // вызовет Method1() и Method2()
```

**Аналогия:** Как проверка, есть ли кто-то дома, перед тем как звонить в дверь.

---

## Объяснение: Зачем нужны `OnEnable()` и `OnDisable()`?

**`OnEnable()`** — вызывается, когда объект становится активным (включается).

**`OnDisable()`** — вызывается, когда объект становится неактивным (выключается).

**Проблема без отписки:**
```csharp
// Плохо: подписались, но не отписались
public class PauseUI : MonoBehaviour
{
    void Start()
    {
        EventBus.Instance.OnGamePaused += ShowPanel; // подписались
        // НО забыли отписаться!
    }
    
    // Объект уничтожен, но подписка осталась!
    // EventBus все еще держит ссылку на уничтоженный объект
    // → УТЕЧКА ПАМЯТИ!
}
```

**Решение с `OnEnable()`/`OnDisable()`:**
```csharp
// Хорошо: подписываемся при включении, отписываемся при выключении
public class PauseUI : MonoBehaviour
{
    private void OnEnable()
    {
        // Объект включился → подписываемся
        EventBus.Instance.OnGamePaused += ShowPanel;
    }

    private void OnDisable()
    {
        // Объект выключился → отписываемся
        EventBus.Instance.OnGamePaused -= ShowPanel;
    }
}
```

**Почему это важно:**
- ✅ Предотвращает утечки памяти
- ✅ Предотвращает вызовы методов на уничтоженных объектах
- ✅ Автоматически работает при включении/выключении объекта

**Когда вызываются:**
- `OnEnable()` → когда объект включается (впервые или после выключения)
- `OnDisable()` → когда объект выключается (или уничтожается)

**Аналогия:** Как подписка на журнал — когда переезжаешь, нужно отписаться от старого адреса и подписаться на новый.

---

## Объяснение: Что такое утечки памяти (Memory Leaks)?

**Утечка памяти** — когда объект уничтожен, но на него все еще есть ссылки.

**Пример проблемы:**
```csharp
// Создали объект
PauseUI pauseUI = new PauseUI();

// Подписались на событие
EventBus.Instance.OnGamePaused += pauseUI.ShowPanel;

// Уничтожили объект
Destroy(pauseUI);

// НО EventBus все еще держит ссылку на pauseUI!
// Объект не может быть удален из памяти
// → УТЕЧКА ПАМЯТИ
```

**Решение:**
```csharp
// Отписываемся перед уничтожением
private void OnDisable()
{
    EventBus.Instance.OnGamePaused -= ShowPanel; // убираем ссылку
}
```

**Последствия утечек:**
- ❌ Память заполняется "мертвыми" объектами
- ❌ Игра начинает тормозить
- ❌ Может привести к крашу игры

**Как избежать:**
- ✅ Всегда отписывайтесь в `OnDisable()`
- ✅ Используйте `OnEnable()`/`OnDisable()` для подписки/отписки

**Аналогия:** Как забыть отписаться от рассылки — письма продолжают приходить, даже если ты уже не живешь по этому адресу.

---

## Объяснение: Зачем нужен EventBus, если можно использовать прямые ссылки?

**Проблема с прямыми ссылками:**
```csharp
// Плохо: жесткая связь между объектами
public class PauseUI : MonoBehaviour
{
    public GameManager gameManager; // нужно вручную присваивать в Inspector
    
    void OnButtonClick()
    {
        gameManager.Pause(); // если gameManager == null, ошибка!
    }
}

// Проблемы:
// ❌ Нужно вручную присваивать ссылки в Inspector
// ❌ Если GameManager уничтожен, ошибка
// ❌ Сложно тестировать
// ❌ Сложно менять код
```

**Решение с EventBus:**
```csharp
// Хорошо: слабая связь через события
public class PauseUI : MonoBehaviour
{
    void OnEnable()
    {
        EventBus.Instance.OnGamePaused += ShowPanel; // подписываемся
    }
    
    void OnDisable()
    {
        EventBus.Instance.OnGamePaused -= ShowPanel; // отписываемся
    }
}

// Преимущества:
// ✅ Не нужно присваивать ссылки в Inspector
// ✅ Объекты не знают друг о друге
// ✅ Легко тестировать
// ✅ Легко менять код
```

**Сравнение:**

| Прямые ссылки | EventBus |
|---------------|----------|
| `gameManager.Pause()` | `EventBus.Instance.RaiseGamePaused()` |
| Нужно знать конкретный объект | Не нужно знать конкретный объект |
| Жесткая связь | Слабая связь |
| Сложно тестировать | Легко тестировать |

**Аналогия:**
- Прямые ссылки = звонить конкретному человеку (нужно знать номер)
- EventBus = объявление по радио (все, кто слушает, узнают)

---

## Объяснение: Что такое `+=` и `-=` для событий?

**`+=`** — добавляет метод в список подписчиков события.

**`-=`** — убирает метод из списка подписчиков события.

**Пример:**
```csharp
// Подписываемся (добавляем метод в список)
EventBus.Instance.OnGamePaused += ShowPanel;
EventBus.Instance.OnGamePaused += PlaySound; // можно добавить несколько методов

// Теперь в OnGamePaused два метода:
// 1. ShowPanel
// 2. PlaySound

// Отписываемся (убираем метод из списка)
EventBus.Instance.OnGamePaused -= ShowPanel;

// Теперь в OnGamePaused один метод:
// 1. PlaySound
```

**Важно:**
- Метод должен быть **точно таким же** (тот же метод, тот же объект)
- Если метод не был подписан, `-=` ничего не сделает (ошибки не будет)

**Пример с ошибкой:**
```csharp
// Подписались
EventBus.Instance.OnGamePaused += ShowPanel;

// Отписались (правильно)
EventBus.Instance.OnGamePaused -= ShowPanel; // ✅ работает

// Попытка отписаться еще раз
EventBus.Instance.OnGamePaused -= ShowPanel; // ✅ ничего не произойдет, ошибки нет
```

**Аналогия:** Как список друзей в соцсети — можно добавить (`+=`) или удалить (`-=`) друга.

---

## Проверка
- EventBus создается в Bootstrap
- GameManager вызывает `RaiseGamePaused()` при паузе
- UI подписывается на события в `OnEnable()` и отписывается в `OnDisable()`
- При паузе UI автоматически показывает панель паузы
