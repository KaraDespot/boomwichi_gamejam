## Settings Panel: sound / music / fullscreen

Эта инструкция описывает, как собрать минимальные настройки:

- sound (громкость эффектов)
- music (громкость музыки)
- fullscreen (полноэкранный режим)

Канон UX в проекте:

- **MainMenu**: настройки — это **отдельная панель** (открывается кнопкой `Settings`, закрывается кнопкой `Back`).
- **GameScene**: настройки **встроены внутрь Pause** (видны прямо на панели паузы, без отдельного окна настроек).
- Настройки должны применяться **глобально**: при старте и при переходах между сценами.

---

### 0) Что изменилось в коде (кратко)

Фактические изменения в текущем состоянии teacher repo:

- **Добавлено**:
  - `Assets/_Scripts/UI/GameSettings.cs` — единая точка хранения/загрузки/применения настроек.
  - `Assets/_Scripts/Core/SettingsBootstrapper.cs` — глобальное применение настроек при старте и после загрузки каждой сцены.
  - `Assets/_Scripts/UI/PauseSettingsBinder.cs` — биндинг контролов настроек, встроенных в Pause.
- **Изменено**:
  - `Assets/_Scripts/UI/SettingsPanelController.cs` — оконный контроллер панели настроек (для MainMenu).
  - `Assets/_Scripts/UI/MainMenuController.cs` — открывает окно настроек и валидирует ссылки.
  - `Assets/_Scripts/UI/PauseController.cs` — отвечает только за паузу (без логики настроек).
  - `Assets/_Scenes/Bootstrap.unity` — добавлен `SettingsBootstrapper`.
  - `Assets/_Scenes/MainMenu.unity` — назначены ссылки на Settings UI.
  - `Assets/_Prefabs/UI/UIRootCanvas.prefab` — на объект `Pause` добавлен `PauseSettingsBinder`.
  - `Assets/_Scenes/Levels/GameScene.unity` — в сцене больше нет “оконного” контроллера настроек; настройки работают через встроенный Pause UI.

---

### 1) Зачем этот этап

Настройки нужны, чтобы:

- игрок мог регулировать громкость и режим экрана под себя;
- изменения применялись сразу (видимый результат);
- значения не “терялись” при переходах между сценами.

В teacher repo это также удобная тема, чтобы показать:

- как UI управляет игровыми параметрами;
- чем отличается **“сохранить”** от **“применить”**;
- как сделать поведение глобальным (между сценами).

---

### 2) Что такое PlayerPrefs (простыми словами)

`PlayerPrefs` — это встроенное в Unity простое хранилище **“ключ → значение”** для маленьких данных.

Пример “как думать”:

- `"settings_sound"` → `0.8`
- `"settings_fullscreen"` → `1`

Ключевые свойства:

- значения переживают **смену сцен** и **перезапуск игры**;
- удобен для **настроек** (громкость, fullscreen, чувствительность мыши);
- не подходит для больших сохранений (инвентарь, квесты, мир) — там лучше отдельная система сохранений.

В этом проекте `PlayerPrefs` спрятан внутрь `GameSettings`, поэтому UI-код работает с методами:

- `GameSettings.SetSound(...)`
- `GameSettings.SetMusic(...)`
- `GameSettings.SetFullscreen(...)`

а детали `PlayerPrefs` остаются внутри `GameSettings`.

---

### 3) Из чего состоит система (карта связей)

Система состоит из 3 слоёв:

1) **Хранилище значений (persist layer)**  
   `GameSettings` читает/пишет значения в `PlayerPrefs` по ключам:
   - `settings_sound`
   - `settings_music`
   - `settings_fullscreen`

2) **Глобальное применение (apply layer)**  
   `SettingsBootstrapper` висит в сцене `Bootstrap` и:
   - применяет настройки при старте;
   - повторно применяет настройки после каждой загрузки сцены (`SceneManager.sceneLoaded`).

3) **UI (два сценария интерфейса)**  
   - **MainMenu**: `SettingsPanelController` управляет отдельным окном (open/close/back) и вызывает `GameSettings.Set...`.
   - **GameScene / Pause**: `PauseSettingsBinder` связывает встроенные в Pause контролы с `GameSettings.Set...`.

---

### 4) Пошагово: MainMenu — отдельная панель настроек

#### Шаг 4.1 — Проверить структуру UI в MainMenu

**Editor**

- Открыть сцену `Assets/_Scenes/MainMenu.unity`.
- В иерархии должны существовать:
  - объект с `MainMenuController`;
  - кнопка `Settings` (обычно объект `SettingsBTN`);
  - объект `SettingsPanel` (панель настроек), который:
    - изначально **выключен** (`SetActive = false`);
    - содержит:
      - слайдер sound
      - слайдер music
      - toggle fullscreen
      - кнопку `Back`.

**Code**

- `MainMenuController` должен иметь поля:
  - `buttonNewGame`, `buttonSettings`, `buttonExit`
  - `settingsPanelController`
- `SettingsPanelController` должен иметь ссылки:
  - `settingsPanel` (корень панели)
  - `soundSlider`, `musicSlider`, `fullscreenToggle`, `backButton`

**Check**

- Enter Play Mode → нажать `Settings` → панель появляется.
- Нажать `Back` → панель скрывается.

---

#### Шаг 4.2 — Назначить ссылки в `MainMenuController`

**Editor**

- Выбрать объект `MainMenuController` в иерархии.
- В инспекторе назначить:
  - `Button New Game` → кнопка старта игры
  - `Button Settings` → кнопка `SettingsBTN`
  - `Button Exit` → кнопка выхода
  - `Settings Panel Controller` → компонент `SettingsPanelController` (обычно на объекте панели или на объекте контроллера рядом).

**Code**

- В `Assets/_Scripts/UI/MainMenuController.cs` есть валидация ссылок и обработчик:
  - `HandleSettingsClicked()` вызывает `settingsPanelController.OpenPanel()`.

**Check**

- Enter Play Mode → открыть Console.
- Нажать `Settings`. Ошибок вида “не назначен в Inspector” быть не должно.

---

#### Шаг 4.3 — Назначить ссылки в `SettingsPanelController`

**Editor**

- Выбрать объект с компонентом `SettingsPanelController` (в MainMenu).
- В инспекторе назначить:
  - `Settings Panel` → корневой объект панели `SettingsPanel`.
  - `Sound Slider` → слайдер sound.
  - `Music Slider` → слайдер music.
  - `Fullscreen Toggle` → toggle fullscreen.
  - `Back Button` → кнопка `Back`.
- Поля `Sound Sources` и `Music Sources`:
  - можно оставить пустыми как минимальный вариант;
  - лучше назначить явно, если в сцене есть конкретные AudioSource для музыки/эффектов.

**Code**

- `Assets/_Scripts/UI/SettingsPanelController.cs`:
  - при включении панели делает `SyncUiFromSavedSettings()`;
  - применяет текущие значения через `GameSettings.Apply(...)`;
  - подписывает обработчики `onValueChanged` и `onClick`.

**Check**

- Enter Play Mode → открыть Settings → двигать слайдеры:
  - значения должны меняться (и сохраняться).
- Переключить fullscreen toggle:
  - `Screen.fullScreen` должен измениться (в Editor эффект может отличаться от билда).

---

### 5) Пошагово: GameScene — настройки внутри Pause

#### Шаг 5.1 — Убедиться, что настройки действительно встроены в Pause

**Editor**

- Открыть префаб `Assets/_Prefabs/UI/UIRootCanvas.prefab`.
- Найти объект `Pause` (обычно выключен по умолчанию).
- На объекте `Pause` должен быть компонент `PauseSettingsBinder`.
- Внутри `Pause` должны быть:
  - слайдер sound
  - слайдер music
  - toggle fullscreen
  - кнопки `Resume` и `Main Menu` (их обслуживает `PauseController`).

**Code**

- `Assets/_Scripts/UI/PauseController.cs` отвечает только за:
  - показать/скрыть паузу;
  - кнопки `Resume`/`Main Menu`;
  - подписки на `EventBus` и `InputManager`.
- `Assets/_Scripts/UI/PauseSettingsBinder.cs` отвечает только за:
  - синхронизировать UI из `GameSettings.Load()`;
  - вызывать `GameSettings.Set...` при изменениях контролов.

**Check**

- Enter Play Mode → открыть паузу (клавиша паузы или кнопка/событие).
- На Pause панели должны быть видны настройки.

---

#### Шаг 5.2 — Назначить ссылки на слайдеры/тоггл в `PauseSettingsBinder` (важно)

**Editor**

- Открыть `Assets/_Prefabs/UI/UIRootCanvas.prefab`.
- Выбрать объект `Pause`.
- В компоненте `PauseSettingsBinder` проверить, что назначены:
  - `Sound Slider`
  - `Music Slider`
  - `Fullscreen Toggle`

Важно:

- Если поля `Sound Slider` и `Music Slider` пустые, биндер перейдёт в **резервный автопоиск**.
- Резервный путь не ломает игру, но **в teacher repo это не канон**, потому что:
  - порядок слайдеров в иерархии может поменяться;
  - биндер может привязаться к “не тому” слайдеру.

**Code**

- В `PauseSettingsBinder.ResolveReferencesIfMissing()` есть автопоиск `GetComponentsInChildren<Slider>(true)`.
- Основной путь — ручные ссылки в инспекторе.

**Check**

- После назначения полей:
  - Enter Play Mode → открыть паузу → проверить, что оба слайдера меняют соответствующую громкость.

---

### 6) Пошагово: глобальное применение настроек на все сцены (Bootstrap)

#### Шаг 6.1 — Убедиться, что SettingsBootstrapper существует в Bootstrap

**Editor**

- Открыть сцену `Assets/_Scenes/Bootstrap.unity`.
- Найти объект `BootstrapManager`.
- На нём должен быть компонент `SettingsBootstrapper` (или объект рядом, но в проекте канон — на `BootstrapManager`).

**Code**

- `Assets/_Scripts/Core/SettingsBootstrapper.cs`:
  - в `Awake` применяет `GameSettings.Apply(GameSettings.Load())`;
  - в `OnEnable` подписывается на `SceneManager.sceneLoaded`;
  - в `OnDisable` отписывается;
  - на каждую загрузку сцены снова делает `Apply`.

**Check**

- Из MainMenu поменять громкости/fullscreen.
- Запустить игру (перейти в GameScene).
- Открыть Pause: значения должны соответствовать сохранённым.

---

### 7) Частые ошибки и хрупкие места

- **PauseSettingsBinder не назначены слайдеры**  
  Симптом: слайдеры ведут себя странно (или меняют не то).  
  Проверка: `UIRootCanvas.prefab` → объект `Pause` → `PauseSettingsBinder` → поля `Sound Slider`/`Music Slider`/`Fullscreen Toggle`.

- **В сцене два биндерa**  
  Симптом: двойные реакции на изменение значения.  
  Проверка: предупреждение в Console про несколько `PauseSettingsBinder`.

- **Sound/Music “не туда” применяются**  
  Причина: если `soundSources/musicSources` не заданы, используется резервная эвристика `AudioSource.loop`.  
  Решение: назначить явные источники в `SettingsPanelController` и/или `PauseSettingsBinder`.

- **Fullscreen в Editor ведёт себя не так, как в билде**  
  Это нормальная особенность. Проверять в билде при необходимости.

---

### 8) Mini smoke-test (чеклист)

- [ ] В MainMenu кнопка `Settings` открывает панель `SettingsPanel`.
- [ ] Кнопка `Back` закрывает панель.
- [ ] В MainMenu слайдеры sound/music и toggle fullscreen меняют значения сразу.
- [ ] Запуск игры → в GameScene открывается Pause.
- [ ] В Pause видны sound/music/fullscreen и они меняют значения.
- [ ] Поменять настройки в MainMenu → перейти в GameScene → в Pause значения уже применены (глобальность работает).
- [ ] Console без новых ошибок (предупреждения только по делу).

---

### 9) Опционально: вариант хранения настроек через JSON / Save Game Free

Этот вариант полезен, если уже внедряется система сохранений и нужно хранить настройки в файле рядом с другими данными.

Важно:

- для настроек из 2–3 значений `PlayerPrefs` проще и быстрее;
- JSON/Save Game Free добавляет больше шагов интеграции и больше точек отказа.

Мини-идея архитектуры:

- `GameSettings` остаётся единым API для UI (`SetSound/SetMusic/SetFullscreen`).
- Вместо `PlayerPrefs` внутри `GameSettings` используется чтение/запись JSON (через Save Game Free).
- `SettingsBootstrapper` остаётся прежним: он не знает, где лежат данные, он только делает `Load + Apply`.

Рекомендация по методике:

- базовая реализация урока — `PlayerPrefs`;
- JSON/Save Game Free — отдельный “Опционально/Позже” блок.

---

### 10) Что будет дальше

Следующий логичный шаг после настроек — тема сохранений прогресса (checkpoint / load / restart), где уже появляется полноценная save-система для больших данных.

