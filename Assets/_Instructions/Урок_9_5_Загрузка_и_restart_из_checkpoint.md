# Урок 9.5 — Загрузка и restart из checkpoint (активный слот, Continue, lose)

---

## 0. Что изменилось в коде (кратко)

Сравнение с состоянием **после этапа «Урок 9.4 — Checkpoint system»** (запись checkpoint на диск есть, **загрузка и сценарии после смерти в меню — ещё нет**). В teacher repo добавлен полный **load / restart flow**: выбор одного из трёх слотов перед **New Game** и **Continue**, **активный слот** в `GameManager`, загрузка данных в gameplay-сцену с отложенным применением к игроку, **Restart** на lose-экране с попыткой загрузить checkpoint из активного слота и **fallback** на перезагрузку текущей сцены, русские подписи в UI слотов, уточнённые комментарии в коде и **Editor-инструмент** для сброса всех слотов при проверках.

### Добавлено

- **`Assets/_Scripts/UI/SaveSlotSelectionController.cs`** — панель выбора слота 0..2 для режимов New Game и Continue; кнопки и визуал собираются **вручную** в Unity, код только связывает ссылки из Inspector с `CheckpointSaveSystem` и `GameManager`.
- **`Assets/_Scripts/Editor/CheckpointSaveDevTools.cs`** — пункт меню **Tools → Save Tools → Clear Checkpoint Saves**: с подтверждением очищает все три слота через `CheckpointSaveSystem.Delete` (включая debug JSON рядом с save). Только **Unity Editor**, в билд игроку не попадает.

### Изменено

- **`Assets/_Scripts/Core/GameManager.cs`**
  - **Активный save-слот** `activeSaveSlotIndex`: задаётся при **New Game** / **Continue** из меню; все новые checkpoint-записи идут в **`ResolveSaveSlot`**: при валидном активном слоте используется он, иначе — инспекторный **`checkpointSlotIndex`** на `GameLoopFlowController` как fallback для старых сцен.
  - **`StartNewGameInSlot`**: выставляет активный слот, вызывает **`CheckpointSaveSystem.Delete`** для выбранного слота (чтобы старый checkpoint не мешал новому прохождению), затем **`StartGame()`**.
  - **`TryContinueFromSlot`**: загрузка данных из слота, установка активного слота, **`LoadFromCheckpointData`**.
  - **`RestartFromCheckpointOrScene`**, **`TryLoadActiveCheckpoint`**: сценарий lose-**Restart** — сначала попытка загрузить checkpoint активного слота, иначе обычный **`RestartGameScene`**.
  - **`LoadFromCheckpointData`**, **`ResolveCheckpointScene`**, **`CreateRuntimeStateFromCheckpoint`**: выбор сцены (текущий уровень vs следующий после выхода), сбор **`pendingPlayerRuntimeState`**; позиция checkpoint применяется **только** для safe-point внутри уровня, не для checkpoint на выходе уровня.
  - Шапочный комментарий класса обновлён под текущую реальность этапа; у **`StartGame()`** зафиксирован контракт: метод **не** выбирает слот и **не** меняет `activeSaveSlotIndex` (важно для отладки и для понимания, почему restart через checkpoint может не сработать без прохода через меню со слотом).

- **`Assets/_Scripts/UI/MainMenuController.cs`**
  - New Game и Continue открывают **одну и ту же** панель выбора слота через **`SaveSlotSelectionController`**.
  - **Continue** в главном меню: **`buttonContinue.interactable = CheckpointSaveSystem.HasAnySave()`** при **`Start`** и **`OnEnable`** (`RefreshContinueButtonState`). Если сохранений нет — кнопка неактивна (стандартное затемнение Unity).
  - Устранён дублирующий блок **`/// <summary>`** перед **`RefreshContinueButtonState`**: оставлен один комментарий с контрактом (когда вызывается, связь с **`HasAnySave`**, назначение **`interactable`**).

- **`Assets/_Scripts/UI/SaveSlotSelectionController.cs`**
  - Тексты занятых слотов на русском: **`Уровень N`**, **`ОЗ N`**; подпись неизвестной сцены — **`Неизвестно`**.
  - Tooltip для блока занятого слота уточнён: вместо «HP» используется **«ОЗ»** в описании полей.

- **`Assets/_Scripts/UI/GameLoopFlowController.cs`**
  - Кнопка lose-**Restart** вызывает **`GameManager.RestartFromCheckpointOrScene`** (раньше могла вести только к перезагрузке сцены).
  - Шапка класса обновлена: явно указаны связи с **`RestartFromCheckpointOrScene`**; **`checkpointSlotIndex`** описан как **fallback**, если активный слот в `GameManager` не задан.

- **`Assets/_Scripts/UI/PauseController.cs`**
  - В **`Awake`** и **`Start`** вызывается **`HidePausePanel()`**, чтобы при загрузке новой gameplay-сцены панель паузы не оставалась включённой из-за состояния prefab/сцены.

- **`Assets/_Scripts/Save/CheckpointSaveSystem.cs`** (логика этапа 9.4, используемая здесь)
  - **`HasAnySave()`** — для отключения Continue без прохода по слотам.
  - **`Delete`** — очистка слота при New Game и в Editor-devtool; внутри вызывается удаление debug JSON.

- **Сцена `Assets/_Scenes/MainMenu.unity`** (в teacher repo)
  - Вручную подключены кнопка **Continue**, панель выбора слотов, три ячейки слота (occupied / empty, иконки, **TMP_Text**).

### Зависимости

- Без изменений относительно 9.4: пакет **Save Game Free**, **TextMeshPro** для текстов слотов.

---

## 1. Зачем этот этап

После этапа 9.4 игра **умеет записывать** checkpoint в файлы, но без загрузки запись остаётся «половиной системы». Этот этап закрывает цикл:

- **Continue** из главного меню — явный выбор слота и загрузка прогресса в нужную gameplay-сцену с восстановлением **уровня персонажа, ОЗ, маны, опыта, слота оружия** и (при необходимости) **позиции** safe-point.
- **Restart** после поражения — та же кнопка по смыслу для игрока («попробовать снова»), но сначала игра **пытается вернуть сохранённый прогресс** из активного слота; если сохранения нет или слот не был выбран в этой сессии — выполняется **перезагрузка текущей сцены** (предсказуемый fallback).
- **Три слота** — разделение профилей и понятная демонстрация на занятии без автоматической «магии последнего save».

Ограничения **упрощённой save-модели** остаются теми же, что в 9.4: в save **не попадают** активная волна encounter, враги, снаряды, полный мир; для checkpoint **на выходе уровня** следующая сцена открывается **как при обычном входе** в уровень (позиция spawn следующей сцены не берётся из вектора выхода предыдущей — это сознательное упрощение, зафиксированное в комментариях к **`CreateRuntimeStateFromCheckpoint`**).

### 1.1. Где этот этап относительно 9.2 и 9.4 (одна таблица)

| Слой | Этап 9.2 (runtime) | Этап 9.4 (диск, запись) | Этап 9.5 (диск, чтение + меню + lose) |
|------|--------------------|-------------------------|----------------------------------------|
| Переживает закрытие игры | Нет | Да (слот Save Game Free + JSON) | Да, плюс **явный выбор слота** |
| Откуда берётся «куда грузить» | Только текущий flow сцены | Данные в **`CheckpointSaveData`** | То же + **`LoadFromCheckpointData`** / **`ResolveCheckpointScene`** |
| После смерти на lose | Раньше — только перезапуск сцены | Save уже мог быть на диске | **Restart** сначала пытается **загрузить** этот save из **активного** слота |

---

## 2. Из чего состоит система и как связаны компоненты (карта связей)

### 2.1. Данные и диск (наследие 9.4)

- **`CheckpointSaveData`** — тот же DTO; при загрузке читается через **`CheckpointSaveSystem.TryLoad(slot, out data)`**.
- **`CheckpointSaveSystem`** — ключи слотов `checkpoint_progress_0` … `2`; **`Delete`**, **`HasAnySave`**, **`Save`**, **`TryLoad`**.

### 2.2. Меню и выбор профиля (слота)

- **`MainMenuController`** — кнопки **New Game**, **Continue**, настройки, выход; открывает **`SaveSlotSelectionController.OpenForNewGame`** / **`OpenForContinue`**; обновляет доступность **Continue** через **`CheckpointSaveSystem.HasAnySave()`**.
- **`SaveSlotSelectionController`** — три **`SaveSlotView`** (кнопка ячейки, объекты **occupied** / **empty**, **TMP_Text** для уровня, ОЗ, сцены); по клику: **`StartNewGameInSlot`** или **`TryContinueFromSlot`**.

### 2.3. Игровой менеджер и активный слот

- **`GameManager`**
  - **`TrySetActiveSaveSlot`** / поле **`activeSaveSlotIndex`** — какой слот считается «текущим профилем» после выбора в меню.
  - **`TrySaveCheckpointData`** / **`ResolveSaveSlot`** — куда писать новый checkpoint после выхода или из **`CheckpointTrigger`**: сначала активный слот, иначе fallback из **`GameLoopFlowController.checkpointSlotIndex`**.
  - **`LoadFromCheckpointData`** — выбор имени сцены, выставление **`currentLevelIndex`**, заполнение **`pendingPlayerRuntimeState`**, вызов **`LoadGameplayScene`** через **`SceneLoader.Instance.LoadWithLoading`** (как и при обычном старте уровня — через сцену **Loading**, см. этап старта игры в курсе).
  - **`HandleSceneLoaded`** — после загрузки **gameplay**-сцены применяет **`ApplyPendingPlayerRuntimeState`** (числа + оружие + при необходимости **позиция** через **`ApplyCheckpointPosition`** с отключением **`CharacterController`** на один кадр переноса).

### 2.4. Поражение и перезапуск

- **`GameLoopFlowController`** — по смерти игрока показывает lose-панель, вызывает **`GameManager.EnterLoseState`**; кнопка **Restart** → **`RestartFromCheckpointOrScene`**.

### 2.5. Пауза при входе в уровень

- **`PauseController`** — при появлении на сцене принудительно скрывает **`pausePanel`**, чтобы не тянуть активное состояние UI между загрузками.

### 2.6. Инструмент преподавателя

- **`CheckpointSaveDevTools`** — только Editor: массовая очистка слотов для повторяемых демо и проверок **Continue** / пустых слотов.

---

## 3. Пошагово (три слоя: Editor / Code / Check)

### 3.1. Идея «активного слота» и связь с lose-Restart

**Editor**

- Ничего обязательного: понимание для демонстрации на доске или в схеме.

**Code**

- Прочитать в **`GameManager`**: **`activeSaveSlotIndex`**, методы **`StartNewGameInSlot`**, **`TryContinueFromSlot`**, **`ResolveSaveSlot`**, **`RestartFromCheckpointOrScene`**, **`TryLoadActiveCheckpoint`**.
- Зафиксировать правило: **Restart через checkpoint читает только активный слот**, выбранный при старте сессии из меню. Прямой вызов **`StartGame()`** без выбора слота **не меняет** `activeSaveSlotIndex` — в комментарии к **`StartGame()`** это описано явно.

**Check**

- В Play Mode: **New Game** → выбрать слот 1 → дойти до сохранения / смерти → **Restart** должен читать слот **1**, а не «последний случайный».

---

### 3.2. Главное меню: кнопки и ссылки на панель слотов

**Editor**

- Открыть сцену **`MainMenu`**.
- Найти объект с **`MainMenuController`**.
- Убедиться, что назначены:
  - **`buttonNewGame`**, **`buttonContinue`**, **`buttonSettings`**, **`buttonExit`** (по требованиям сцены);
  - **`saveSlotSelectionController`** — ссылка на объект с компонентом **`SaveSlotSelectionController`** (часто корень панели или дочерний объект);
  - **`settingsPanelController`** — как на этапе настроек.
- Войти в Play Mode с пустыми слотами (или после **Clear Checkpoint Saves**, см. 3.11): кнопка **Continue** должна быть **неактивной** (серой).

**Code**

- **`MainMenuController`**: **`RefreshContinueButtonState`** в **`Start`** и **`OnEnable`**; **`HandleNewGameClicked`** / **`HandleContinueClicked`** открывают панель.

**Check**

- Нет сохранений → **Continue** не нажимается.
- После появления хотя бы одного save в любом из слотов 0..2 → после перезапуска Play или повторного **`OnEnable`** меню **Continue** снова активна.

---

### 3.3. Панель выбора слота: ручная сборка в Unity

**Editor**

Ниже — **развёрнутый** порядок работ в Unity Editor для сборки панели с нуля (в teacher repo панель уже может быть собрана — тогда использовать как образец и сверить ссылки).

1. **Canvas / родитель**  
   - Убедиться, что в сцене **`MainMenu`** есть **Canvas** с **Canvas Scaler** (как принято в проекте).  
   - Создать пустой **`Panel_SaveSlots`** под Canvas (или под корневым UI-объектом меню). Задать **Anchors** / **RectTransform** так, чтобы панель перекрывала экран или центрировалась.  
   - По умолчанию **`Panel_SaveSlots`** можно оставить **выключенным** (`SetActive(false)`), чтобы не мешать главному меню до нажатия New Game / Continue.

2. **Кнопка закрытия**  
   - Добавить дочерний **Button** (например **`Button_CloseSlots`**), подпись «Закрыть» / иконка крестика.  
   - Позже в Inspector у **`SaveSlotSelectionController`** поле **`closeButton`** указывает на этот **Button**.

3. **Три ячейки слота (повторить для Slot 0, 1, 2)**  
   - Создать пустой родитель **`Slot_0`**.  
   - Добавить компонент **Button** на корень **`Slot_0`** (или на дочерний полноразмерный объект): это будет **`slotButton`** — клик по всей ячейке.  
   - Внутри **`Slot_0`** создать два дочерних объекта:
     - **`Occupied`** — для отображения данных save: **Image** (иконка персонажа), три дочерних **`TextMeshPro - Text (UI)`** для уровня, ОЗ и имени сцены.  
     - **`Empty`** — для пустого слота: один **`TMP_Text`** с надписью **New** / **Пустой слот** (на усмотрение дизайна; код только включает/выключает корневые объекты **`occupiedState`** / **`emptyState`**).
   - Продублировать структуру для **`Slot_1`** и **`Slot_2`**.

4. **Компонент контроллера**  
   - На **`Panel_SaveSlots`** (или на отдельном дочернем **`SaveSlotPanel`**) добавить компонент **`SaveSlotSelectionController`**.  
   - **`panelRoot`**: перетащить **`Panel_SaveSlots`** (тот объект, чей **`SetActive`** управляет видимостью всей панели).  
   - **`closeButton`**: перетащить кнопку из шага 2.  
   - **`Slot Views`**: выставить **Size = 3**.  
     - **Element 0**: **`Slot Button`** → Button с **`Slot_0`**; **`Occupied State`** → объект **`Occupied`**; **`Empty State`** → **`Empty`**; **`Character Icon`** → **Image** иконки; **`Level Text`**, **`Health Text`**, **`Scene Text`** → соответствующие **TMP**.  
     - **Element 1** / **2** — то же для **`Slot_1`**, **`Slot_2`**.

5. **Связь с главным меню**  
   - На объекте с **`MainMenuController`** в поле Inspector **`Save Slot Selection Controller`** (в коде — **`saveSlotSelectionController`**) перетащить объект, на котором висит **`SaveSlotSelectionController`** (часто тот же **`Panel_SaveSlots`**).

6. **Кнопки New Game / Continue**  
   - Оставить подписку **только из кода** (`OnEnable` у **`MainMenuController`**): в Inspector у **Button** **не** добавлять дублирующий **`OnClick`** на **`StartGame`**, если это ломает новый flow.  
   - Проверить, что **`buttonNewGame`** и **`buttonContinue`** назначены на реальные кнопки сцены.

7. **`defaultCharacterSprite` (опционально)**  
   - Если на **Image** иконки нет спрайта в prefab, назначить спрайт в поле Inspector **`Default Character Sprite`** у **`SaveSlotSelectionController`** — тогда при **`RefreshSlot`** код подставит спрайт для занятого слота.

**Code**

- **`SaveSlotSelectionController`**: **`OpenForNewGame`** / **`OpenForContinue`**, **`RefreshSlot`** — читает **`TryLoad`**, включает **`interactable`** у кнопки: в режиме Continue пустой слот **не** кликабелен для загрузки; в режиме New Game пустой слот **кликабелен** (новый прогон).

**Check**

- Открыть панель в обоих режимах: пустой слот в **Continue** не запускает игру при клике; в **New Game** — запускает **`StartNewGameInSlot`** для этого индекса.

---

### 3.4. New Game: очистка слота и старт с первого уровня

**Editor**

- В Play Mode нажать **New Game** → выбрать, например, слот **2**, в котором уже был старый прогресс.
- Убедиться, что начался запуск с **первого** уровня sequence (или fallback-сцены), без подтягивания старых HP из предыдущего save.

**Code**

- **`GameManager.StartNewGameInSlot`**: **`TrySetActiveSaveSlot`** → **`CheckpointSaveSystem.Delete(slotIndex)`** → **`StartGame()`**.
- **`CheckpointSaveSystem.Delete`**: удаляет ключ Save Game Free и вызывает **`CheckpointJsonDebugExporter.Delete`** — debug JSON для слота тоже убирается.

**Check**

- После New Game в выбранном слоте: в **`persistentDataPath`** для этого индекса нет актуального **`checkpoint_slot_N.json`** (или файл отсутствует после удаления).
- В Console — лог очистки слота из **`CheckpointSaveSystem`**.

---

### 3.5. Continue: выбор слота и загрузка

**Editor**

- Сначала получить save (пройти уровень до win с успешным checkpoint или зайти в **`CheckpointTrigger`**).
- В главном меню нажать **Continue** → открыть панель → выбрать слот с данными.
- Убедиться, что загрузилась **ожидаемая** gameplay-сцена (текущий уровень из save или **следующая** после checkpoint выхода — см. следующий подпункт).

**Code**

- **`TryContinueFromSlot`**: **`TryLoad`** → **`LoadFromCheckpointData`**.
- **`ResolveCheckpointScene`**: если **`savedFromLevelExit`** и задано **`nextSceneName`** — грузится **следующая** сцена; иначе — **`completedSceneName`** (возврат в тот же уровень для safe-point).

**Check**

- Сравнить поведение с содержимым **`checkpoint_slot_N.json`**: поля **`savedFromLevelExit`**, **`nextSceneName`**, **`completedSceneName`**, **`checkpointPosition`**.

---

### 3.6. Две разновидности checkpoint для загрузки (выход vs safe-point)

**Editor**

- Настроить на уровне и **выход с win**, и при желании **зону `CheckpointTrigger`**; выполнить оба сценария и сравнить результат загрузки.

**Code**

- **`CreateRuntimeStateFromCheckpoint`**: **`ShouldApplyPosition = !data.savedFromLevelExit`** — для выхода уровня позиция из save **не** применяется к игроку при входе в следующую сцену (игрок появляется как задумано сценой / spawn).

**Check**

- Safe-point: после Continue игрок оказывается **около** сохранённой позиции триггера (с учётом коллайдеров и **`CharacterController`**).
- Выход уровня: после Continue открывается **следующий** уровень, позиция — **не** из вектора выхода предыдущей сцены.

---

### 3.7. Отложенное применение состояния игрока после `sceneLoaded`

**Editor**

- Не требуется; полезно открыть **Console** с фильтром по **`GameManager`**.

**Code**

- **`GameManager.HandleSceneLoaded`**: если есть **`pendingPlayerRuntimeState`** и имя сцены — **gameplay** (не Bootstrap / MainMenu / Loading), вызвать **`ApplyPendingPlayerRuntimeState`**.
- Применение: **`PlayerProgression.ApplyRuntimeState`**, **`PlayerStats.ApplyRuntimeState`**, **`WeaponManager.ApplyRuntimeState`**, затем при флаге позиции — **`ApplyCheckpointPosition`**.

**Check**

- После Continue: **HUD** и игровые значения совпадают с ожидаемыми из JSON (уровень, ОЗ, мана, опыт, оружие).

---

### 3.8. Lose-экран: одна кнопка Restart и цепочка в коде

**Editor**

- На геймплейной сцене найти **`GameLoopFlowController`**.
- Убедиться, что **`loseRestartButton`** назначена и вешает на **`GameLoopFlowController`** (через UnityEvent не вручную дублирует другой вызов).
- Проверить, что при lose вызывается **`EnterLoseState`** (timeScale 0, UI input) — визуально пауза/ввод как на этапе lose/win.

**Code**

- **`HandleLoseRestartClicked`** → **`GameManager.RestartFromCheckpointOrScene`**.
- **`RestartFromCheckpointOrScene`**: если **`TryLoadActiveCheckpoint()`** вернул **true** — загрузка пошла; иначе **`RestartGameScene()`** (перезагрузка текущей gameplay-сцены по **`ResolveCurrentGameplayScene`**).

**Check**

- Есть save в активном слоте → **Restart** возвращает к прогрессу (сцена + статы).
- Нет save / слот не выбирался → **Restart** ведёт к **началу текущей** сцены (fallback).

---

### 3.9. Пауза не должна «залипать» при загрузке уровня

**Editor**

- На префабе или в сцене gameplay открыть **`PauseController`**, проверить ссылку **`pausePanel`**.
- Убедиться, что в Inspector у **`pausePanel`** не задано «включено по умолчанию» как единственный источник правды — **`Awake`** всё равно скроет панель при старте объекта.

**Code**

- **`PauseController.Awake`** и **`Start`**: **`HidePausePanel()`**.

**Check**

- Запустить уровень, открыть паузу, перейти через загрузку на другой уровень (Continue или Next): при старте новой сцены панель паузы **закрыта**, пока игрок снова не нажал паузу.

---

### 3.10. Fallback `checkpointSlotIndex` на `GameLoopFlowController`

**Editor**

- На **`GameLoopFlowController`** посмотреть поле **`checkpointSlotIndex`** (0..2) — это слот для **записи** на выходе уровня, если **`activeSaveSlotIndex`** ещё не задан (например, старая сцена без меню слотов в тесте).

**Code**

- **`GameManager.ResolveSaveSlot`**: приоритет активному слоту из меню, затем инспекторное значение, затем безопасный **0** с предупреждением.

**Check**

- Осознанно вызвать только **`StartGame()`** из отладки без меню: сохранение win должно уйти в fallback-слот согласно Inspector; **Restart через checkpoint** при **`activeSaveSlotIndex == -1`** должен уйти в **перезагрузку сцены**, а не в загрузку save.

---

### 3.11. Editor-инструмент: Clear Checkpoint Saves

**Editor**

- В верхнем меню Unity выбрать **Tools → Save Tools → Clear Checkpoint Saves**.
- Прочитать диалог: подтвердить **«Очистить»** или отменить.
- После очистки: при открытом **MainMenu** в Play Mode при необходимости **выйти из Play и зайти снова** или перезагрузить сцену меню — чтобы **`OnEnable`** снова вызвал **`RefreshContinueButtonState`** и **Continue** стала неактивной, если слотов больше нет.

**Code**

- **`CheckpointSaveDevTools.ClearCheckpointSaves`**: цикл **`CheckpointSaveSystem.Delete(i)`** для **`i`** от 0 до **`SlotCount - 1`**; лог **`clearedCount`** в Console.

**Check**

- Все три слота пусты: **`HasAnySave()`** ложно, файлы **`checkpoint_slot_*.json`** для очищенных слотов удалены или отсутствуют после операции.
- Понимание для методики: инструмент **не** трогает сцены и префабы, только данные save в **`persistentDataPath`**.

**Зачем это в уроке**

- Быстро вернуть демо к состоянию «нет сохранений» без ручного поиска файлов в папке пользователя.
- Повторять проверки **Continue**, **пустых слотов**, **Restart** с чистого листа.
- Показать ту же **`CheckpointSaveSystem.Delete`**, что вызывается при **New Game** в выбранном слоте — единый канон очистки (включая JSON).

---

### 3.12. Проверка компиляции (опционально для методики CI / копий проекта)

**Editor**

- Не обязательно для младших групп; для старших — показать, что teacher repo собирается из командной строки.

**Code**

- В корне проекта (где лежат **`Assembly-CSharp.csproj`** и **`Assembly-CSharp-Editor.csproj`**, генерируемые Unity):

```text
dotnet build Assembly-CSharp.csproj --no-restore
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

**Check**

- **0 ошибок, 0 предупреждений** в teacher repo на момент фиксации этапа.

---

## 4. Частые ошибки и хрупкие места

- **Continue активна, хотя слоты очищены** — возможен кэш UI до **`RefreshContinueButtonState`**; выйти из Play, снова войти, либо перезагрузить **`MainMenu`**; после **Clear Checkpoint Saves** в том же Play не забыть перезапуск сессии меню.
- **Restart всегда только перезагружает сцену** — проверить, что перед этим был выбран слот через **New Game** или **Continue**; проверить **`activeSaveSlotIndex`** и наличие файла save; не вызывать только **`StartGame()`** без слота, если ожидается загрузка checkpoint.
- **Загрузка «не та сцена»** — сверить **`savedFromLevelExit`**, **`nextSceneName`**, **`completedSceneName`** в JSON; проверить **`LevelSequenceData`** и **Build Settings** для имён сцен.
- **Игрок не на позиции safe-point** — проверить **`checkpointPosition`** в JSON, коллайдеры, высоту пола; **`ApplyCheckpointPosition`** временно отключает **`CharacterController`** — если на игроке нет контроллера, перенос всё равно идёт по **`Transform`**.
- **Дублирующиеся игроки** — **`GameManager`** предупреждает и берёт первый найденный набор компонентов; для демо на сцене должен быть **один** корректный игрок.
- **Панель слотов не открывается** — **`saveSlotSelectionController`** не назначен в **`MainMenuController`** или **`panelRoot`** / кнопки слотов пустые в массиве.
- **Путаница TMP vs Legacy UI** — слоты в teacher repo рассчитаны на **`TMP_Text`**; при смешении с **`Text`** поля не заполнятся.

---

## 5. Mini smoke-test (чеклист)

- [ ] **MainMenu**: **Continue** неактивна при отсутствии любых save; после появления save — активна.
- [ ] **New Game** → выбор слота → старт с «чистого» прогресса в этом слоте; старый JSON для слота не мешает (удалён или перезаписан согласно сценарию).
- [ ] **Continue** → выбор занятого слота → корректная сцена и корректные **Уровень / ОЗ / оружие / мана / опыт** на HUD и в геймплее.
- [ ] **Checkpoint на выходе уровня** → Continue → **следующий** уровень; позиция игрока — как у нового входа в сцену, не «в воздухе у выхода».
- [ ] **CheckpointTrigger (safe-point)** → Continue → тот же уровень, позиция около точки сохранения.
- [ ] **Смерть** → **Restart** при наличии save в активном слоте → загрузка прогресса; без save → перезагрузка текущей сцены.
- [ ] **Пауза** не остаётся открытой после загрузки другой gameplay-сцены.
- [ ] **Tools → Save Tools → Clear Checkpoint Saves** → подтверждение → все слоты пусты, **Continue** после перезапуска Play неактивна.
- [ ] В Console нет критичных ошибок на сценариях выше; при успешном save по-прежнему желательны логи из этапа 9.4 (**Save Game Free** + **Checkpoint JSON written**).

---

## 6. Что будет дальше

Следующие темы учебного маршрута обычно уходят в **дополнительные модули** (инвентарь, квесты, NPC и т.д.) или в полировку вертикального среза — без обязательного расширения checkpoint до «полного снимка мира». Для этого этапа достаточно уверенно объяснять **границу ответственности** save: что в записке есть, чего намеренно нет, и как **Editor-инструмент** помогает сбрасывать состояние для чистых прогонов демонстрации.
