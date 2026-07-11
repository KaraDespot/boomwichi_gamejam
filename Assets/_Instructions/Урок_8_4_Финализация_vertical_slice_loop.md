# Финализация vertical slice loop: доводим игру до логического конца

---

## 0. Что изменилось в коде (кратко)

Ниже — изменения ветки **vertical slice loop** относительно предыдущего состояния (до финализации основного цикла).

### Добавлено

- **Последовательность уровней (данные)**:
  - `Assets/_Scripts/Core/LevelSequenceData.cs` — ScriptableObject, который хранит имена игровых сцен в порядке прохождения.
  - `Assets/Resources/Levels/LevelSequence_Default.asset` — пример последовательности уровней (загружается через `Resources`).
  - `Assets/_ScriptableObjects/Levels/LevelSequenceData.asset` — учебный ассет для редактирования в Editor (используется приоритетно в Unity Editor).

### Изменено

- **Прогрессия по уровням и замыкание run-цикла**:
  - `Assets/_Scripts/Core/GameManager.cs` — добавлена загрузка sequence, старт игры с 0-го уровня, `TryLoadNextLevel()` для кнопки “Next Level”, fallback если sequence не найден.
  - `Assets/_Scripts/Core/SceneLoader.cs` — публикует `EventBus.OnLevelLoaded` при загрузке любой сцены (единый “сигнал жизни” для UI/систем).
- **Логика победы/поражения и “что дальше” после победы**:
  - `Assets/_Scripts/UI/GameLoopFlowController.cs` — кнопка победы “Next” ведёт на следующий уровень (`winNextLevelButton`), добавлено правило “победа разрешена после обязательного encounter” через `EventBus.OnEncounterCompleted` и `requiredEncounterIdForWin`.
  - `Assets/_Scripts/UI/ExitWinTrigger.cs` — уточнён контракт: trigger только **запрашивает** победу, решение принимает `GameLoopFlowController`.
  - `Assets/_Scripts/UI/MainMenuController.cs` — добавлены null-проверки и безопасные обработчики.
  - `Assets/_Scripts/UI/PauseController.cs` — добавлена “шапка” и уточнены контракты подписок.
- **Сцены и данные уровней**:
  - `Assets/_Scenes/Levels/*` — добавлены/обновлены примеры игровых сцен для прохождения (несколько вариантов `GameScene`).
  - `ProjectSettings/EditorBuildSettings.asset` — обновлён список сцен (Build Settings) для запуска sequence.

---

## 1. Зачем этот этап

Vertical slice — это не “набор систем”, а **маленькая игра, которую можно пройти**.

Ты считаешь вертикальный срез готовым только тогда, когда:

- игра **стартует** предсказуемо;
- у уровня есть **цель** (пройти encounter и добраться до выхода);
- есть **конец** (win/lose);
- после победы понятно, что делать дальше: **перейти на следующий уровень** или **вернуться в меню**.

В этом этапе ты замыкаешь **core gameplay loop** до логического конца.

---

## 2. Из чего состоит система и как связаны компоненты (карта связей)

Ниже — “скелет” вертикального среза и связи между компонентами.

### 2.1. Старт run-а и загрузка уровня

- `MainMenuController` вызывает `GameManager.StartGame()`.
- `GameManager` выбирает 0-й уровень из `LevelSequenceData` и грузит его через `SceneLoader.LoadWithLoading(...)`.

### 2.2. Gameplay: encounter → открытие выхода

- `EncounterTrigger` запускает encounter при входе игрока в trigger.
- Когда encounter завершён, `EncounterTrigger`:
  - включает объекты из `activateOnCompleted` (например, выход);
  - публикует событие `EventBus.OnEncounterCompleted(encounterId)`.

### 2.3. Победа (win)

- На объекте выхода висит `ExitWinTrigger`.
- Когда игрок входит в trigger выхода, `ExitWinTrigger` вызывает `GameLoopFlowController.RequestWinFromExit()`.
- `GameLoopFlowController` проверяет контракт победы:
  - игра в состоянии `Playing`;
  - выход реально активирован на сцене;
  - (опционально) завершён нужный encounter (`requiredEncounterIdForWin`) — это подтверждается через `EventBus.OnEncounterCompleted`.
- Если всё ок — `GameLoopFlowController` переводит игру в `Won` через `GameManager.EnterWinState()` и показывает `WinPanel`.

### 2.4. Поражение (lose)

- `PlayerStats` вызывает `OnDeath`, когда здоровье падает до 0.
- `GameLoopFlowController` подписан на `PlayerStats.OnDeath` и при смерти:
  - переводит игру в `Lost` через `GameManager.EnterLoseState()`;
  - показывает `LosePanel`.

### 2.5. “Что дальше” после победы

- На `WinPanel` есть кнопка **Next Level**.
- Она вызывает `GameManager.TryLoadNextLevel()`:
  - если следующий уровень есть — он загружается;
  - если уровни закончились — игрок возвращается в меню.

---

## 3. Пошагово (три слоя: Editor / Code / Check)

### 3.1. Собери последовательность уровней (LevelSequenceData)

- **Editor**
  - Открой Project и найди папку `Assets/Resources/Levels/`.
  - Убедись, что там есть `LevelSequence_Default.asset` (или создай свой `LevelSequenceData` и положи его в `Resources/Levels/`).
  - Внутри `LevelSequenceData` заполни список сцен:
    - впиши **имена сцен** (строки) в порядке прохождения;
    - имена должны совпадать с именами сцен в Build Settings (иначе переход не найдёт сцену).
  - Если используешь override, назначь его в `GameManager` (поле `levelSequenceOverride`).

- **Code**
  - Смотреть:
    - `Assets/_Scripts/Core/LevelSequenceData.cs` — формат данных и методы `TryGetLevelSceneName / FindLevelIndex`.
    - `Assets/_Scripts/Core/GameManager.cs` — `StartGame()` и `TryLoadNextLevel()`.

- **Check**
  - Запусти Play Mode из `MainMenu`.
  - Нажми “New Game”:
    - ожидаемо: загружается **первый** уровень из последовательности (через Loading).

---

### 3.2. Проверь “правило честной победы”: выход активируется только после encounter

- **Editor**
  - Открой сцену первого уровня (из sequence).
  - Найди объект с `EncounterTrigger`.
  - В `EncounterTrigger` проверь поле `activateOnCompleted`:
    - туда должен быть добавлен объект выхода (или маркер выхода), который должен быть **выключен на старте**.
  - Выход:
    - имеет `Collider`;
    - у коллайдера включён **Is Trigger**.

- **Code**
  - Смотреть: `Assets/_Scripts/Encounters/EncounterTrigger.cs`
    - завершение encounter включает `activateOnCompleted`;
    - публикуется `EventBus.OnEncounterCompleted(encounterId)`.

- **Check**
  - До победы над врагами выход неактивен.
  - После завершения encounter выход становится активным.

---

### 3.3. Свяжи выход с победой и “замком” по encounter (опционально, но очень полезно)

Это шаг про **замыкание логики**: победа не просто “вошёл в триггер”, а “вошёл в триггер после выполнения цели уровня”.

- **Editor**
  - На объекте выхода должен быть `ExitWinTrigger`.
  - В `ExitWinTrigger` назначь `flowController` на объект с `GameLoopFlowController`.
  - В `GameLoopFlowController` назначь:
    - `exitActivationObjectOverride` — объект выхода (или объект, который включается после encounter);
    - `requiredEncounterIdForWin` — ID обязательного encounter (если хочешь строгую проверку).

- **Code**
  - Смотреть:
    - `Assets/_Scripts/UI/ExitWinTrigger.cs` — trigger только делегирует win-запрос.
    - `Assets/_Scripts/UI/GameLoopFlowController.cs` — контракт победы и проверка `requiredEncounterIdForWin`.
    - `Assets/_Scripts/Core/EventBus.cs` — событие `OnEncounterCompleted`.

- **Check**
  - Если `requiredEncounterIdForWin` задан:
    - до завершения нужного encounter победа должна **не** срабатывать;
    - после завершения — должна срабатывать.

---

### 3.4. Проверь lose/win экраны и “что дальше”

- **Editor**
  - На сцене должен быть `GameLoopFlowController` со ссылками:
    - `losePanel`, `loseRestartButton`, `loseMenuButton`
    - `winPanel`, `winMenuButton`, `winNextLevelButton`
    - `pausePanel` (чтобы пауза не висела поверх win/lose)

- **Code**
  - Смотреть:
    - `Assets/_Scripts/UI/GameLoopFlowController.cs` — показ панелей, обработчики кнопок.
    - `Assets/_Scripts/Core/GameManager.cs` — `EnterLoseState / EnterWinState / TryLoadNextLevel`.

- **Check**
  - Умри: появляется `LosePanel`, время останавливается, кнопки работают:
    - Restart перезапускает уровень;
    - Menu возвращает в главное меню.
  - Выиграй: появляется `WinPanel`, время останавливается, кнопки работают:
    - Menu возвращает в главное меню;
    - Next Level загружает следующий уровень или возвращает в меню, если это был последний.

---

## 4. Частые ошибки и хрупкие места

### 4.1. Победа не срабатывает на выходе

- Проверь:
  - у выхода включён `Collider` и стоит **Is Trigger**;
  - у игрока корректный tag `Player`;
  - в `ExitWinTrigger` назначен `flowController`;
  - `exitActivationObjectOverride` в `GameLoopFlowController` указывает на активируемый выход.

### 4.2. Победа срабатывает “слишком рано”

- Причины:
  - выход активен на старте (должен быть выключен);
  - `requiredEncounterIdForWin` пустой — тогда win-замок по ID не включён;
  - ID не совпадает с тем, что реально публикует encounter.

### 4.3. Next Level не работает

- Проверь:
  - `LevelSequenceData` реально найден (через `Resources/Levels/...`) или назначен `levelSequenceOverride`;
  - строки имён сцен совпадают с Build Settings;
  - в `EditorBuildSettings.asset` добавлены нужные сцены.

### 4.4. После win/lose “всё зависло”

- Это нормально: `GameManager` ставит `Time.timeScale = 0`.
- UI должен продолжать работать, потому что это отдельная система и клики не зависят от timeScale.

---

## 5. Mini smoke-test (чеклист)

- [ ] Из `MainMenu` игра запускается и попадает на первый уровень из sequence.
- [ ] Encounter запускается и завершается только после добивания врагов.
- [ ] После завершения encounter выход становится активным.
- [ ] Вход в выход переводит игру в `Won` и показывает `WinPanel`.
- [ ] Кнопка “Next Level” на `WinPanel` загружает следующий уровень (или возвращает в меню, если он последний).
- [ ] Смерть игрока переводит игру в `Lost` и показывает `LosePanel`.
- [ ] Restart/Menu на `LosePanel` работают.
- [ ] В Console нет новых ошибок.

---

## 6. Что будет дальше

Дальше ты добавишь “удобство игрока” (HUD, polish UI, настройки, сохранения), но главное уже сделано: у игры есть **начало, цель и логический конец** — то есть настоящий vertical slice.

