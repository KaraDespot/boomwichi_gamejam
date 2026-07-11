# Старт игры и `Loading`: нормальный flow сцен + warm-up

---

## 0. Что изменилось в коде (кратко)

Ниже — изменения ветки **Start and Loading** (смысл урока) относительно предыдущего состояния.

### Добавлено

- **Новая сцена загрузки**:
  - `Assets/_Scenes/Loading.unity`
- **Окно-инструмент для быстрой навигации по сценам (открыть сцену / стартануть из Bootstrap)**:
  - `Assets/_Scripts/Editor/SceneToolsWindow.cs`
- **Визуальные ассеты для Loading (опционально, можно заменить на простой текст)**:
  - `Assets/_Art/Animations/LoadingScreen/*`
  - `Assets/Loading screen package/*`

### Изменено

- **Имена сцен вынесены в константы + добавлено имя `Loading`**:
  - `Assets/_Scripts/Core/SceneNames.cs`
- **Переход “через Loading” как отдельный сценарий загрузки**:
  - `Assets/_Scripts/Core/SceneLoader.cs`
- **Стартовый flow теперь проходит через `Loading` (warm-up ещё до меню)**:
  - `Assets/_Scripts/Core/BootstrapManager.cs`
- **Запуск игры из меню грузит `GameScene` через `Loading`**:
  - `Assets/_Scripts/Core/GameManager.cs`
- **Build Settings: сцены добавлены и порядок зафиксирован**:
  - `ProjectSettings/EditorBuildSettings.asset`

---

## 1. Зачем нужен `Loading` (не только “красивая картинка”)

Если грузить большую сцену “в лоб”, игрок часто видит:

- чёрный экран;
- “зависание” на пару секунд;
- ощущение, что игра сломалась.

**`Loading`** решает сразу две задачи:

- **Визуально**: показать “игра работает, идёт переход”.
- **Технически**: это **удобное место для подготовки игры (warm-up)**, чтобы потом в начале `GameScene` не было резких тормозов и странных задержек.

### Почему `Loading` может быть два раза

В этом проекте flow выглядит так:

`Bootstrap → Loading → MainMenu → Loading → GameScene`

И это нормально.

- Первый `Loading` (после `Bootstrap`) — “коридор подготовки” **ещё до меню**: можно сделать стартовую подготовку один раз.
- Второй `Loading` (между меню и игрой) — “переход в уровень”: можно подготовить то, что нужно именно для `GameScene`.

---

## 2. Из чего состоит система и как связаны компоненты (карта связей)

- **`SceneNames`** (`Assets/_Scripts/Core/SceneNames.cs`)
  - хранит имена сцен как константы: `Bootstrap`, `MainMenu`, `Loading`, `GameScene`
  - защищает от опечаток в строках

- **`BootstrapManager`** (`Assets/_Scripts/Core/BootstrapManager.cs`)
  - создаёт менеджеры (живут между сценами)
  - запускает стартовый flow: **`Bootstrap → Loading → MainMenu`**
  - содержит “крючок” под подготовку: `PreloadBeforeMainMenu()`

- **`MainMenuController`** (`Assets/_Scripts/UI/MainMenuController.cs`)
  - вешает действия на кнопки меню
  - на “New Game” вызывает `GameManager.Instance.StartGame()`

- **`GameManager`** (`Assets/_Scripts/Core/GameManager.cs`)
  - управляет состоянием игры (меню/игра/пауза)
  - при старте игры делает переход: **`MainMenu → Loading → GameScene`**

- **`SceneLoader`** (`Assets/_Scripts/Core/SceneLoader.cs`)
  - единая точка загрузки сцен
  - умеет:
    - `Load(sceneName)` — быстрая “мгновенная” загрузка (может подвиснуть на тяжёлой сцене)
    - `LoadAsync(sceneName)` — асинхронная загрузка (без зависания)
    - `LoadWithLoading(targetSceneName, preloadRoutine)` — **наш основной сценарий** “через Loading”

---

## 2.1. Контракт `LoadWithLoading` (что должно произойти по шагам)

Когда мы вызываем `SceneLoader.Instance.LoadWithLoading(targetSceneName, preloadRoutine)`, ожидается такой порядок:

1) **Открываем сцену `Loading`** (быстро, синхронно)  
2) **Даём `Loading` отрисоваться хотя бы 1 кадр** (чтобы игрок точно увидел экран загрузки)  
3) **Выполняем подготовку** (если она передана как `preloadRoutine`)  
4) **Асинхронно грузим целевую сцену** (`LoadAsync` внутри корутины)  
5) (Опционально) **держим Loading минимум N секунд**, чтобы не было “моргания”, если сцена грузится слишком быстро

### Почему важен “1 кадр”

Если сразу начать тяжёлую работу (загрузку/подготовку) **в этот же момент**, Unity может не успеть нормально показать `Loading`, и игрок увидит:

- чёрный экран;
- или Loading появится “слишком поздно”.

Поэтому “подождать 1 кадр” — это как сказать:  
**“Сначала покажи экран, потом работай”**.

---

## 3. Пошагово (Editor / Code / Check)

Ниже шаги сделаны так, чтобы ты мог(ла) собрать минимальный рабочий старт игры у себя.

### 3.1. Добавь сцены в Build Settings и проверь порядок (обязательно)

- **Editor**
  - Открой `File → Build Profiles...` (или `Build Settings`, в зависимости от версии Unity).
  - В списке Scenes In Build должны быть **4 сцены** и в таком порядке:
    1. `Bootstrap`
    2. `MainMenu`
    3. `Loading`
    4. `GameScene`
  - Если сцены нет — открой её и нажми **Add Open Scenes**.
  - Убедись, что **все 4 сцены включены (галочка Enabled)**.

- **Code**
  - В коде сцены загружаются **по имени** через `SceneNames.*`.
  - Если сцены нет в Build Settings — Unity не сможет её загрузить по имени.

- **Check**
  - Нажми Play в Unity.
  - Игра должна стартовать с `Bootstrap` и перейти дальше по flow (см. шаг 3.4).

---

### 3.2. Проверь `SceneNames`: имена сцен должны совпадать с файлами

- **Editor**
  - Проверь, что файлы сцен называются ровно так:
    - `Bootstrap.unity`
    - `MainMenu.unity`
    - `Loading.unity`
    - `GameScene.unity`
  - Важно: **регистр букв тоже важен**.

- **Code**
  - Открой `Assets/_Scripts/Core/SceneNames.cs` и проверь, что там:
    - `public const string Loading = "Loading";`
    - и остальные сцены тоже совпадают по имени

- **Check**
  - Если в консоли появляются ошибки “scene not found” — почти всегда это:
    - сцена не в Build Settings
    - или имя не совпадает

---

### 3.3. Настрой старт игры: `Bootstrap → Loading → MainMenu`

- **Editor**
  - Открой сцену `Bootstrap`.
  - В сцене должен быть объект `BootstrapManager` с компонентом `BootstrapManager`.

- **Code**
  - В `Assets/_Scripts/Core/BootstrapManager.cs` стартовый flow вызывается в `Start()`:
    - `SceneLoader.Instance.LoadWithLoading(SceneNames.MainMenu, PreloadBeforeMainMenu);`
  - Метод `PreloadBeforeMainMenu()` сейчас пустой (это нормально): это точка для warm-up.

- **Check**
  - Нажми Play.
  - Ты должен увидеть:
    - сначала `Loading`,
    - затем — `MainMenu`.

---

### 3.4. Настрой кнопку “New Game”: `MainMenu → Loading → GameScene`

- **Editor**
  - Открой сцену `MainMenu`.
  - Найди объект с `MainMenuController`.
  - Убедись, что в инспекторе назначены ссылки:
    - `buttonNewGame`
    - `buttonExit`

- **Code**
  - В `Assets/_Scripts/UI/MainMenuController.cs` кнопка “New Game” вызывает:
    - `GameManager.Instance.StartGame()`
  - В `Assets/_Scripts/Core/GameManager.cs` метод `StartGame()` делает:
    - `SceneLoader.Instance.LoadWithLoading(SceneNames.GameScene);`

- **Check**
  - В Play Mode нажми “New Game”.
  - Должно быть:
    - переход на `Loading`,
    - затем загрузка `GameScene`.

---

### 3.5. Пойми разницу `Load` и `LoadAsync` (коротко и по делу)

- **Editor**
  - Ничего не настраивай — это про понимание, чтобы не делать “зависания”.

- **Code**
  - `Load(sceneName)`:
    - просто грузит сцену сразу;
    - на тяжёлой сцене может создать “фриз” (подвисание).
  - `LoadAsync(sceneName)`:
    - грузит сцену постепенно, кадры продолжают рисоваться;
    - идеально сочетается с `Loading`.

- **Check**
  - Если переходы кажутся “морганием” — это ок на маленьких сценах.
  - Если переходы подвисают — почти всегда нужен `LoadAsync` + `Loading`.

---

## 4. Warm-up: что делаем сейчас и что переносим позже

Это обязательный методический блок: **Loading — место, куда можно переносить подготовку игры**.

### Базово — делаем сейчас

- **Переход через Loading**:
  - `Bootstrap → Loading → MainMenu`
  - `MainMenu → Loading → GameScene`
- **Минимальный Loading-экран**:
  - достаточно текста “Loading...” или простого анимированного UI.
- **Одна точка загрузки**:
  - используем `SceneLoader` и `SceneNames`, чтобы не плодить `SceneManager.LoadScene(...)` по проекту.

### Позже / по мере роста проекта — что можно переносить в Loading

Пока это **не обязательно**, но полезно держать в голове такие идеи:

- **Прогрев префабов**:
  - первый “пробный” запуск врагов/снарядов/VFX (или подготовка к pooling),
  - чтобы в `GameScene` первая атака/первый спавн не давали резкий лаг.
- **Настройки и save/load**:
  - прочитать и применить настройки (звук, fullscreen),
  - подготовить данные чекпоинта (если они уже появятся позже).
- **Кэширование ссылок**:
  - заранее найти и сохранить нужные ссылки (чтобы меньше делать `Find*` в горячих местах),
  - подготовить “быстрые” ссылки для UI/игровых систем.

Важно: это не про “супер-оптимизацию”, а про простой принцип:  
**лучше подготовиться в Loading, чем лагать в момент, когда игрок уже дерётся**.

---

## 5. Частые ошибки и хрупкие места

### 5.1. Сцена не грузится (Scene not found)

- **Причина №1**: сцена не добавлена в Build Settings  
- **Причина №2**: имя сцены в `SceneNames` не совпадает с файлом `.unity`  
- **Решение**: сделай шаг 3.1 и 3.2

### 5.2. Loading “не видно”, сначала чёрный экран

- **Причина**: тяжёлая работа началась раньше, чем Loading успел отрисоваться  
- **Идея решения**: в `LoadWithLoading` есть “1 кадр”, который даёт Loading появиться — это обязательная часть контракта

### 5.3. Loading показывается слишком долго / слишком быстро

- В `SceneLoader` есть `minimumLoadingDuration` — это “защита от моргания”.
- Можешь оставить как есть: главное понять идею, а не “подбирать идеальные секунды”.

### 5.4. Кнопка New Game не работает

- Проверь, что в `MainMenu` у `MainMenuController` назначены ссылки на кнопки.
- Проверь, что в сцене `Bootstrap` действительно создаётся `GameManager` (он `DontDestroyOnLoad`).

---

## 6. Mini smoke-test (чеклист)

- [ ] В Build Settings добавлены и включены 4 сцены в порядке: `Bootstrap`, `MainMenu`, `Loading`, `GameScene`.
- [ ] При Play стартует `Bootstrap`, затем видно `Loading`, затем открывается `MainMenu`.
- [ ] В `MainMenu` кнопка “New Game” переводит на `Loading` и затем открывает `GameScene`.
- [ ] В консоли нет ошибок “scene not found”.
- [ ] Понимаешь (можешь объяснить), чем `Load` отличается от `LoadAsync`.

---

## 7. Что будет дальше

Дальше мы подключаем **win/lose flow** и замыкаем полноценный игровой цикл: начало (старт) → игра → победа/поражение → выход в меню.

