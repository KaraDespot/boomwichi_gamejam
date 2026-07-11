# Урок 2.5: Связь UI ↔ Core (подробная инструкция)

## Зачем нужна связь UI и Core?

В игре UI (пользовательский интерфейс) должен общаться с Core (ядром игры):
- Кнопка "Новая игра" должна запускать игру
- Кнопка "Пауза" должна ставить игру на паузу
- UI должен показывать актуальное состояние игры

Есть два основных способа связи:
1. **Прямые ссылки** (проще, но менее гибко)
2. **Через EventBus** (сложнее, но более гибко)

Для начала используем **прямые ссылки** через UnityEvent в Inspector — это проще и наглядно.

---

## ⚠️ ВАЖНО: Как правильно запускать игру

**Проблема:** Если запустить сцену `GameScene` напрямую (не через Bootstrap), менеджеры не создадутся, и будет ошибка `NullReferenceException`.

**Правильный способ:**
1. **Всегда запускайте игру через сцену `Bootstrap`** (она должна быть первой в Build Settings)
2. Bootstrap создаст все менеджеры
3. Bootstrap автоматически загрузит `MainMenu`
4. Из `MainMenu` можно перейти в `GameScene` через кнопку "Новая игра"

**Неправильный способ:**
- ❌ Запускать `GameScene` напрямую
- ❌ Запускать `MainMenu` напрямую (менеджеры не создадутся)

**Проверка:**
- В Build Settings сцена `Bootstrap` должна быть **первой** (Index 0)
- При запуске Play автоматически открывается `Bootstrap`, затем `MainMenu`

---

## MainMenu — подключение кнопок

### Шаг 1: Создание кнопок в Unity

1. Откройте сцену `MainMenu`
2. В Canvas создайте кнопки:
   - "Новая игра" (будет загружать `GameScene`)
   - "Выход"
   - (опционально) "Настройки", "Загрузить"

### Шаг 2: Важно! Где находится GameManager?

**Проблема:** В сцене `MainMenu` нет объекта `GameManager` в Hierarchy!

**Почему?** По инструкциям из уроков 2.1-2.2 менеджеры создаются **программно** в `BootstrapManager`, а не вручную в сцене. Они появляются только во время выполнения игры (runtime).

**Решение:** Есть два способа подключить кнопки:

---

### Вариант А: Через код (рекомендуется)

Создайте скрипт `MainMenuController.cs` и прикрепите его к Canvas или пустому объекту в сцене:

```csharp
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button buttonNewGame;
    [SerializeField] private Button buttonExit;

    private void Start()
    {
        // Подключаем кнопки через код
        buttonNewGame.onClick.AddListener(() => GameManager.Instance.StartGame());
        buttonExit.onClick.AddListener(() => Application.Quit());
    }
}
```

**Шаги:**
1. Создайте скрипт `MainMenuController.cs` в папке `Assets/Scripts/UI/`
2. Прикрепите его к Canvas в сцене `MainMenu`
3. В Inspector перетащите кнопки в поля `Button New Game` и `Button Exit`
4. **Важно:** Убедитесь, что сцена `GameScene` (или `Gameplay`) добавлена в Build Settings!
5. Готово! Кнопки будут работать

**Проверка:**
1. Убедитесь, что в Build Settings сцены стоят в порядке:
   - `Bootstrap` (Index 0) — первая
   - `MainMenu` (Index 1)
   - `GameScene` или `Gameplay` (Index 2)
2. Запустите игру через `Bootstrap` (Play в Unity)
3. Должна автоматически загрузиться сцена `MainMenu`
4. Нажмите кнопку "Новая игра"
5. Должна загрузиться сцена `GameScene` (или `Gameplay`, в зависимости от названия)
6. В `GameScene` можно протестировать паузу (нажмите `Esc`)

**Преимущества:**
- ✅ Не нужно создавать объекты менеджеров в сцене
- ✅ Работает с программно созданными менеджерами
- ✅ Код понятный и наглядный

---

### Вариант Б: Через Inspector (если нужен для обучения)

Если хотите использовать Inspector для наглядности, нужно временно создать объект GameManager в сцене:

**Внимание:** Это только для обучения! В реальном проекте менеджеры создаются программно.

1. В сцене `MainMenu` создайте пустой GameObject `GameManager`
2. Добавьте на него компонент `GameManager` (скрипт)
3. Теперь в Inspector кнопки можно перетащить этот объект

**Важно:** 
- При запуске игры `BootstrapManager` создаст свой `GameManager`
- В сцене будет два `GameManager` (один в сцене, один созданный программно)
- Singleton защитит от дублей, но лучше использовать Вариант А

---

### Шаг 3: Подключение кнопки "Новая игра" (если используете Вариант Б)

1. Выберите кнопку "Новая игра"
2. В Inspector найдите компонент `Button`
3. В секции `On Click()` нажмите `+` (добавить событие)
4. Перетащите объект `GameManager` из Hierarchy в поле (или выберите из списка)
5. В выпадающем списке выберите: `GameManager` → `StartGame()`


**Визуально в Inspector:**
```
On Click ()
  └─ GameManager (GameManager)
      └─ StartGame()
```

**Что происходит:**
- При нажатии на кнопку вызывается метод `GameManager.StartGame()`
- Игра переходит в состояние `Playing`
- Загружается сцена `GameScene` (или `Gameplay`, в зависимости от того, как вы назвали сцену)

**Важно:** 
- Убедитесь, что сцена `GameScene` (или `Gameplay`) добавлена в Build Settings
- Имя сцены в Build Settings должно совпадать с именем файла (без `.unity`)

**Как проверить и настроить имя сцены:**

1. **Проверьте Build Settings:**
   - Откройте `File` → `Build Settings`
   - Посмотрите список сцен — там должно быть имя вашей игровой сцены
   - Запомните точное имя (например, `GameScene` или `Gameplay`)

2. **Если сцена называется `GameScene`:**
   - Уже настроено в `SceneNames.GameScene`
   - Ничего менять не нужно

3. **Если сцена называется `Gameplay` (или другое имя):**
   - Откройте файл `SceneNames.cs` (или создайте его, если нет)
   - Добавьте константу: `public const string Gameplay = "Gameplay";`
   - Или измените `GameManager.StartGame()`:
     ```csharp
     SceneLoader.Instance.Load("Gameplay"); // вместо SceneNames.GameScene
     ```

**Пример SceneNames.cs:**
```csharp
public static class SceneNames
{
    public const string Bootstrap = "Bootstrap";
    public const string MainMenu = "MainMenu";
    public const string GameScene = "GameScene";  // или Gameplay = "Gameplay"
}
```

---

### Шаг 4: Подключение кнопки "Выход"

1. Выберите кнопку "Выход"
2. В Inspector в `On Click()` добавьте событие
3. Выберите: `Application` → `Quit()`

**Важно:** `Application.Quit()` работает только в собранной игре (Build), в редакторе Unity не сработает. Объясните это ученикам.

---

## GameScene — панель паузы

### ⚠️ ВАЖНО: Как попасть в GameScene для тестирования?

**Правильный способ:**
1. Запустите игру через сцену `Bootstrap` (Play в Unity)
2. Bootstrap создаст все менеджеры
3. Автоматически загрузится `MainMenu`
4. Нажмите кнопку "Новая игра" в `MainMenu`
5. Загрузится `GameScene` со всеми менеджерами

**Неправильный способ (не работает):**
- ❌ Запускать `GameScene` напрямую → менеджеры не создадутся → ошибка!

**Если нужно протестировать GameScene напрямую (для отладки):**

Если по какой-то причине нужно запустить `GameScene` напрямую (не рекомендуется), можно временно создать менеджеры в сцене:

1. В сцене `GameScene` создайте пустые GameObject'ы:
   - `GameManager` (добавьте компонент `GameManager`)
   - `SceneLoader` (добавьте компонент `SceneLoader`)
   - `EventBus` (добавьте компонент `EventBus`)

2. **Важно:** Это только для отладки! В реальном проекте всегда запускайте через Bootstrap.

3. После отладки удалите эти объекты из сцены.

---

### Важно! Где находится GameManager в GameScene?

Та же ситуация: `GameManager` создается программно в Bootstrap, его нет в сцене `GameScene`.

**Решение:** Используйте скрипт `PauseController` (см. ниже), который обращается к `GameManager.Instance` через код, а не через Inspector.

---

### Шаг 1: Создание панели паузы

1. Откройте сцену `GameScene`
2. В Canvas создайте панель `PausePanel` (Panel или GameObject)
3. Внутри панели создайте кнопки:
   - "Продолжить"
   - "Главное меню"
4. По умолчанию панель должна быть **скрыта** (`SetActive(false)`)

### Шаг 2: Создание скрипта для управления паузой

Создайте скрипт `PauseController.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button buttonResume;
    [SerializeField] private Button buttonMainMenu;

    private void OnEnable()
    {
        // Подписываемся на события EventBus при включении объекта
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnGamePaused += ShowPausePanel;
            EventBus.Instance.OnGameResumed += HidePausePanel;
        }
    }

    private void OnDisable()
    {
        // Отписываемся от событий при выключении объекта (ВАЖНО!)
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnGamePaused -= ShowPausePanel;
            EventBus.Instance.OnGameResumed -= HidePausePanel;
        }
    }

    private void Start()
    {
        // Подключаем кнопки
        if (buttonResume != null)
            buttonResume.onClick.AddListener(OnResumeClicked);
        if (buttonMainMenu != null)
            buttonMainMenu.onClick.AddListener(OnMainMenuClicked);
    }

    private void Update()
    {
        // Проверяем нажатие Esc
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        // Проверяем, что менеджеры созданы
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager не создан! Запустите игру через Bootstrap.");
            return;
        }

        // Просто вызываем методы GameManager
        // EventBus автоматически покажет/скроет панель
        if (GameManager.Instance.CurrentState == GameState.Playing)
        {
            GameManager.Instance.Pause(); // вызовет EventBus.Instance.RaiseGamePaused()
        }
        else if (GameManager.Instance.CurrentState == GameState.Paused)
        {
            GameManager.Instance.Resume(); // вызовет EventBus.Instance.RaiseGameResumed()
        }
    }

    // Эти методы вызываются автоматически через EventBus
    private void ShowPausePanel()
    {
        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    private void HidePausePanel()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    // Обработчики кнопок
    private void OnResumeClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Resume(); // вызовет EventBus, который скроет панель
    }

    private void OnMainMenuClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMenu();
    }
}
```

**Как это работает:**
1. При нажатии `Esc` → вызывается `GameManager.Pause()`
2. `GameManager.Pause()` вызывает `EventBus.Instance.RaiseGamePaused()`
3. EventBus вызывает все подписанные методы, включая `ShowPausePanel()`
4. Панель автоматически показывается!

**Важно: Почему `Start()` вместо `OnEnable()`?**
В этой версии мы используем **`OnEnable()` / `OnDisable()`**, потому что это правильный шаблон для событий:
- подписка в `OnEnable()`
- отписка в `OnDisable()`

`Start()` здесь нужен только для подключения кнопок.

**Проверки на null:**
- Все обращения к менеджерам и UI элементам защищены проверками на `null`
- Если менеджеры не созданы, выводится предупреждение в консоль

### Шаг 3: Подключение кнопок паузы

**Кнопка "Продолжить":**
1. Выберите кнопку "Продолжить"
2. В `On Click()` добавьте:
   - `GameManager` → `Resume()`
   - `PausePanel` → `GameObject` → `SetActive(false)` (скрыть панель)

**Кнопка "Главное меню":**
1. Выберите кнопку "Главное меню"
2. В `On Click()` добавьте:
   - `GameManager` → `GoToMenu()`

---

## Альтернативный способ: через EventBus (для продвинутых)

Если хотите использовать EventBus вместо прямых ссылок:

### Пример: PauseUI через EventBus

```csharp
public class PauseUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private void OnEnable()
    {
        // Подписываемся на события
        EventBus.Instance.OnGamePaused += ShowPausePanel;
        EventBus.Instance.OnGameResumed += HidePausePanel;
    }

    private void OnDisable()
    {
        // Отписываемся от событий
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

**Преимущества:**
- ✅ Автоматически показывает/скрывает панель при паузе
- ✅ Не нужно вручную управлять панелью в других скриптах
- ✅ Легко добавить другие UI элементы, которые реагируют на паузу

---

## Объяснение: Что такое UnityEvent?

**UnityEvent** — это система событий Unity, которая позволяет подключать методы через Inspector.

**Как это работает:**
1. В Inspector видно список методов
2. Можно выбрать объект и метод
3. При нажатии на кнопку вызывается метод

**Пример:**
```csharp
// В Inspector можно подключить:
Button.OnClick()
  └─ GameManager.StartGame()
```

**Преимущества:**
- ✅ Не нужно писать код для подключения кнопок
- ✅ Наглядно видно в Inspector
- ✅ Легко менять без изменения кода

**Недостатки:**
- ❌ Нужно вручную присваивать ссылки
- ❌ Если объект уничтожен, событие не сработает

---

## Объяснение: Что такое `[SerializeField]`?

**`[SerializeField]`** — это атрибут, который делает приватное поле видимым в Inspector.

**Без `[SerializeField]`:**
```csharp
private GameObject pausePanel; // не видно в Inspector
```

**С `[SerializeField]`:**
```csharp
[SerializeField] private GameObject pausePanel; // видно в Inspector
```

**Зачем это нужно:**
- ✅ Можно присвоить ссылку в Inspector
- ✅ Поле остается приватным (нельзя изменить из другого скрипта)
- ✅ Хорошая практика: приватные поля + SerializeField

**Использование:**
1. В скрипте объявляете: `[SerializeField] private GameObject pausePanel;`
2. В Inspector видите поле `Pause Panel`
3. Перетаскиваете объект из Hierarchy в это поле

---

## Объяснение: Что такое `Input.GetKeyDown`?

**`Input.GetKeyDown`** — проверяет, была ли нажата клавиша в этом кадре.

**Пример:**
```csharp
if (Input.GetKeyDown(KeyCode.Escape))
{
    // Код выполнится ТОЛЬКО один раз при нажатии Esc
}
```

**Отличия:**

| Метод | Когда срабатывает |
|-------|-------------------|
| `GetKeyDown` | Один раз при нажатии клавиши |
| `GetKey` | Пока клавиша зажата (каждый кадр) |
| `GetKeyUp` | Один раз при отпускании клавиши |

**Примеры:**
```csharp
// Esc - пауза (один раз)
if (Input.GetKeyDown(KeyCode.Escape))
    TogglePause();

// Пробел - прыжок (пока зажат)
if (Input.GetKey(KeyCode.Space))
    Jump();

// Shift - бег (пока зажат)
if (Input.GetKey(KeyCode.LeftShift))
    Run();
```

**Важно:** `Input.GetKeyDown` работает в `Update()`, который вызывается каждый кадр.

---

## Объяснение: Что такое `SetActive`?

**`SetActive(bool active)`** — включает или выключает GameObject.

**Пример:**
```csharp
pausePanel.SetActive(true);  // показать панель
pausePanel.SetActive(false); // скрыть панель
```

**Что происходит:**
- `SetActive(true)` → объект становится активным (видимым)
- `SetActive(false)` → объект становится неактивным (невидимым)

**Важно:**
- ❌ НЕ используйте `Destroy()` для скрытия UI
- ✅ Используйте `SetActive(false)` для скрытия
- ✅ Используйте `SetActive(true)` для показа

**Почему не `Destroy()`?**
- `Destroy()` уничтожает объект навсегда
- `SetActive(false)` просто скрывает, можно показать снова

---

## Проверочный сценарий

1. **Запустить Play** → автоматически попали в `MainMenu`
2. **Нажать "Новая игра"** → загрузилась `GameScene`
3. **Нажать `Esc`** → пауза открылась, timeScale = 0
4. **Нажать "Продолжить"** → пауза закрылась, timeScale = 1
5. **Нажать "Главное меню"** → вернулись в `MainMenu`
6. **Остановить Play** → убедиться, что нет ошибок/исключений

---

## Частые ошибки

### UI кнопки не кликаются

**Причина:** нет `EventSystem` или Canvas не настроен

**Решение:**
1. Проверьте, есть ли `EventSystem` в сцене (Unity создает автоматически с Canvas)
2. Проверьте, что на Canvas есть компонент `Graphic Raycaster`
3. Проверьте, что Canvas настроен правильно (Screen Space - Overlay)

### Пауза ломает UI

**Причина:** выключили EventSystem или Canvas вместо скрытия панели

**Решение:**
- Используйте `pausePanel.SetActive(false)` для скрытия
- НЕ используйте `Destroy(pausePanel)`
- НЕ выключайте Canvas или EventSystem

### Кнопка не вызывает метод

**Причина:** неправильно подключено в Inspector или объект уничтожен

**Решение:**
1. Проверьте в Inspector, что метод подключен правильно
2. Проверьте, что объект (например, GameManager) существует в сцене
3. Убедитесь, что метод публичный (`public`)

---

## Дополнительные советы

### Организация UI

Создайте структуру в Canvas:
```
Canvas
├── MainMenuPanel
│   ├── Button_NewGame
│   ├── Button_Exit
│   └── ...
├── PausePanel
│   ├── Button_Resume
│   ├── Button_MainMenu
│   └── ...
└── HUD (будет позже)
    ├── HealthBar
    ├── ManaBar
    └── ...
```

### Использование Canvas Groups

Для плавного появления/исчезновения панелей можно использовать `CanvasGroup`:

```csharp
[SerializeField] private CanvasGroup pausePanel;

private void ShowPausePanel()
{
    pausePanel.alpha = 1f; // показать
    pausePanel.interactable = true;
}

private void HidePausePanel()
{
    pausePanel.alpha = 0f; // скрыть
    pausePanel.interactable = false;
}
```

Это позволяет делать плавные переходы (через анимации или корутины).
