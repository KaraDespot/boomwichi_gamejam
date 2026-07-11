# Lose / Win flow: конец игры, победа и поражение

---

## 0. Что изменилось в коде (кратко)

Ниже — изменения ветки **Lose/Win flow** относительно предыдущего состояния (до урока про конец игры).

### Добавлено

- **Контроллер конца игры (win/lose flow)**:
  - `Assets/_Scripts/UI/GameLoopFlowController.cs` — показывает `LosePanel` при смерти игрока и `WinPanel` при достижении выхода, скрывает паузу, вызывает `GameManager.EnterLoseState/EnterWinState`.
- **Триггер победы на выходе**:
  - `Assets/_Scripts/UI/ExitWinTrigger.cs` — компонент на объекте выхода: при входе игрока в `Is Trigger` запрашивает победу у `GameLoopFlowController`.

### Изменено

- **Состояния игры и перезапуск сцены**:
  - `Assets/_Scripts/Core/GameManager.cs` — добавлены `GameState.Lost/Won`, методы `EnterLoseState()`, `EnterWinState()`, `RestartGameScene()` (restart теперь используется и для старта игры).
- **Сцена teacher repo**:
  - `Assets/_Scenes/GameScene.unity` — добавлен `ExitWinTrigger` на `ExitUnlockedMarker_Lesson7.4`, настроены ссылки `GameLoopFlowController` на UI-панели/кнопки, `Pause` и объект выхода, который активируется после encounter.

---

## 1. Зачем этот этап

Без победы и поражения игра ощущается “бесконечной демкой”. В этом этапе ты добавляешь финальные состояния:

- **проигрыш**, когда игрок погиб;
- **победу**, когда игрок дошёл до выхода;
- **правило честной победы**: выход появляется только после завершения боя (encounter).

В результате уровень превращается в мини-игру, которую можно пройти от начала до конца.

---

## 2. Из чего состоит система и как связаны компоненты (карта связей)

Ниже — минимальная карта связей для урока.

- **Поражение (Lose)**:
  - `PlayerStats` вызывает событие `OnDeath`
  - `GameLoopFlowController` подписан на `OnDeath` и при смерти:
    - переводит игру в `GameState.Lost` через `GameManager.EnterLoseState()`
    - показывает `LosePanel`
    - скрывает `Pause`, если оно открыто

- **Открытие выхода (после encounter)**:
  - `EncounterTrigger` на сцене, когда encounter завершён, включает объекты из списка `activateOnCompleted`
  - один из таких объектов — **выход/маркер выхода** `ExitUnlockedMarker_Lesson7.4` (в начале он выключен)

- **Победа (Win)**:
  - на объекте выхода висит `ExitWinTrigger` (у объекта есть `Collider` с `Is Trigger`)
  - когда игрок входит в триггер, `ExitWinTrigger` вызывает `GameLoopFlowController.RequestWinFromExit()`
  - `GameLoopFlowController` проверяет, что игра в состоянии `Playing` и выход уже активен, затем:
    - переводит игру в `GameState.Won` через `GameManager.EnterWinState()`
    - показывает `WinPanel`
    - скрывает `Pause`, если оно открыто

---

## 3. Пошагово (три слоя: Editor / Code / Check)

### 3.1. Подготовь UI-экраны победы и поражения

- **Editor**
  - Открой сцену `GameScene`.
  - В Hierarchy найди объект `Managers`, внутри него — `UIController`.
  - На `UIController` должен быть компонент `GameLoopFlowController`.
  - На Canvas сцены должны быть:
    - `LosePanel` (выключен на старте)
    - `WinPanel` (выключен на старте)
  - На панелях должны быть кнопки:
    - для Lose: `Restart`, `Menu`
    - для Win: `Menu` (и может быть `NextWave` как задел под следующий урок)
  - В Inspector у `GameLoopFlowController` назначь ссылки:
    - `losePanel`, `loseRestartButton`, `loseMenuButton`
    - `winPanel`, `winMenuButton`, `winNextWaveButton`
    - `pausePanel` (это объект `Pause` на сцене)

- **Code**
  - `GameLoopFlowController` показывает/прячет панели и переводит состояние игры в `GameManager`.
  - Важно: сам `GameManager` ставит `Time.timeScale = 0` в `Lost/Won`, поэтому после победы/поражения мир “замирает”, но UI продолжает работать.
  - Смотреть:
    - `Assets/_Scripts/UI/GameLoopFlowController.cs`
    - `Assets/_Scripts/Core/GameManager.cs` (`EnterLoseState`, `EnterWinState`)

- **Check**
  - Запусти Play Mode.
  - Убедись, что в начале:
    - `LosePanel` и `WinPanel` выключены
    - `Pause` выключен

---

### 3.2. Настрой условие проигрыша (смерть игрока)

- **Editor**
  - Ничего специально настраивать не нужно, кроме того, что на сцене есть игрок с компонентом `PlayerStats`.

- **Code**
  - `PlayerStats` вызывает `OnDeath`, когда здоровье упало до 0.
  - `GameLoopFlowController` подписывается на `PlayerStats.OnDeath` и вызывает `TriggerLose()`.
  - Смотреть:
    - `Assets/_Scripts/Player/PlayerStats.cs` (`OnDeath`)
    - `Assets/_Scripts/UI/GameLoopFlowController.cs` (`TrySubscribeToPlayerDeath`, `TriggerLose`)

- **Check**
  - В Play Mode получи урон до смерти.
  - Ожидаемое поведение:
    - появится `LosePanel`
    - время остановится (это нормально)
    - кнопка `Restart` перезапускает `GameScene` через `Loading`
    - кнопка `Menu` возвращает в `MainMenu`

---

### 3.3. Настрой выход, который включается после encounter

- **Editor**
  - На сцене найди `EncounterTrigger_Lesson7.4`.
  - В Inspector у `EncounterTrigger` проверь список `activateOnCompleted`:
    - там должен быть объект выхода, например `ExitUnlockedMarker_Lesson7.4`.
  - Выдели `ExitUnlockedMarker_Lesson7.4` и проверь:
    - объект **выключен** на старте (Inactive)
    - на объекте есть `Collider` и у него включён **Is Trigger**

- **Code**
  - `EncounterTrigger` включает выход только после завершения encounter.
  - Это принципиально: победа должна быть возможна **только после боя**, иначе игрок сможет “сразу уйти”.
  - Смотреть:
    - `Assets/_Scripts/Encounters/EncounterTrigger.cs` (логика завершения и активации объектов)

- **Check**
  - В Play Mode зайди в зону encounter.
  - Победи всех врагов.
  - Убедись, что после этого объект выхода стал активным (видно в Hierarchy или визуально на сцене).

---

### 3.4. Настрой условие победы (вход в триггер выхода)

- **Editor**
  - На объекте выхода `ExitUnlockedMarker_Lesson7.4` должен быть компонент `ExitWinTrigger`.
  - В `ExitWinTrigger` назначь ссылку `flowController` на `UIController` с `GameLoopFlowController`.
  - Проверь, что у игрока корректный tag `Player` (это важно для срабатывания триггера).

- **Code**
  - `ExitWinTrigger` срабатывает в `OnTriggerEnter(Collider other)` и проверяет, что вошёл игрок.
  - Затем он вызывает `GameLoopFlowController.RequestWinFromExit()`.
  - `GameLoopFlowController` дополнительно проверяет:
    - игра в состоянии `Playing`
    - выход уже активен (то есть encounter действительно завершён)
  - Смотреть:
    - `Assets/_Scripts/UI/ExitWinTrigger.cs`
    - `Assets/_Scripts/UI/GameLoopFlowController.cs` (`RequestWinFromExit`)

- **Check**
  - Пройди encounter, дождись активации выхода.
  - Зайди игроком в триггер выхода.
  - Ожидаемое поведение:
    - появится `WinPanel`
    - время остановится
    - кнопка `Menu` возвращает в `MainMenu`

---

## 4. Частые ошибки и хрупкие места

### 4.1. Победа не срабатывает на выходе

- **Проверь**:
  - у выхода включён `Collider` и стоит **Is Trigger**
  - на выходе есть `ExitWinTrigger`
  - в `ExitWinTrigger` назначен `flowController`
  - у игрока tag **`Player`**
  - выход **активен** (он должен включаться только после encounter)

### 4.2. Победа срабатывает “слишком рано”

- **Причина**: выход случайно активен на старте или включается не тем объектом.
- **Проверь**:
  - `ExitUnlockedMarker_Lesson7.4` выключен на старте
  - `EncounterTrigger.activateOnCompleted` включает именно правильный объект

### 4.3. После Win/Lose поверх экрана висит пауза

- **Причина**: ссылка `pausePanel` не назначена.
- **Проверь**:
  - в `GameLoopFlowController` назначено поле `pausePanel` на объект `Pause` в сцене

### 4.4. После Win/Lose “всё зависло и ничего не нажимается”

- **Причина**: на самом деле игра перешла в `Won/Lost` и `Time.timeScale = 0` (это нормально), но UI не принимает ввод.
- **Проверь**:
  - что кнопки реально есть на панели и включены
  - что `GameManager` переключает ввод на UI (в проекте это делает `InputManager`)

---

## 5. Mini smoke-test (чеклист)

- [ ] Проект компилируется, Console без новых ошибок по UI/flow.
- [ ] В начале `GameScene` выключены: `LosePanel`, `WinPanel`, `ExitUnlockedMarker_Lesson7.4`.
- [ ] Смерть игрока показывает `LosePanel`, `Restart` перезапускает `GameScene`, `Menu` уводит в `MainMenu`.
- [ ] Encounter запускается, враги появляются и добиваются до конца.
- [ ] После завершения encounter выход активируется.
- [ ] Вход игрока в триггер выхода показывает `WinPanel`, `Menu` уводит в `MainMenu`.
- [ ] При Win/Lose пауза не остаётся висеть поверх экрана.

---

## 6. Что будет дальше

Дальше мы замкнём вертикальный срез: свяжем encounter → выход → win/lose в один цельный gameplay loop и решим, что делать после победы (например, следующий уровень/следующая волна/возврат в меню).

