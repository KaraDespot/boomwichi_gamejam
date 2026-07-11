# HUD + Pause + экраны игры (Win/Lose)

---

## 0. Что изменилось в коде (кратко)

Ниже — то, что уже появилось в teacher repo для этого этапа.

### Добавлено

- **HUD как prefab**: `Assets/_Prefabs/UI/HUDCanvas.prefab`
- **Контроллер HUD**: `Assets/_Scripts/UI/GameplayHUDController.cs`

### Изменено

- **Смена оружия → обновление HUD**: `Assets/_Scripts/Weapons/WeaponManager.cs` публикует событие `OnWeaponChanged`, HUD обновляет иконку текущего оружия.
- **Опыт за убийства** (чтобы XP/Level на HUD реально росли): задействована цепочка `EnemySpawner/SimpleEnemySpawner` → `EnemyDeathRewarder` → `PlayerProgression`.

---

## 1. Зачем этот этап

Во время боя игроку нужно быстро понимать:

- сколько у него **HP** и **маны**;
- сколько **XP** до следующего уровня и какой **уровень** сейчас;
- какое **оружие** активно.

А ещё игре нужны понятные состояния:

- **Pause** (пауза по кнопке);
- **Lose** (смерть игрока);
- **Win** (победа и что делать дальше).

---

## 2. Из чего состоит система и как связаны компоненты (карта связей)

### 2.1. HUD (GameplayHUDController)

`GameplayHUDController` — это **presenter**: он **не хранит** статы, а только:

- подписывается на события `PlayerStats`, `PlayerProgression`, `WeaponManager`;
- обновляет UI-элементы (`Text`, `Image.fillAmount`, `Image.sprite`).

Источники данных:

- `PlayerStats` → HP/мана
- `PlayerProgression` → XP/уровень
- `WeaponManager` → текущее оружие → `WeaponData.icon`

### 2.2. Pause

- `PauseController` слушает:
  - `EventBus` (пауза/возобновление)
  - `InputManager` (нажатия Pause/Cancel)
- В паузе показывается `pausePanel`, кнопки ведут в `Resume`/`Main Menu`.

### 2.3. Win/Lose

`GameLoopFlowController`:

- при смерти игрока включает `LosePanel` и переводит игру в состояние Lose;
- при победе включает `WinPanel` и переводит игру в состояние Win;
- прячет `Pause`, если нужно;
- кнопки Win/Lose вызывают методы `GameManager` (restart/menu/next level).

---

## 3. Пошагово (три слоя: Editor / Code / Check)

### 3.1. Подключи HUD prefab в игровую сцену

- **Editor**
  - Открой сцену уровня (`Assets/_Scenes/Levels/GameScene*.unity`).
  - Перетащи `Assets/_Prefabs/UI/HUDCanvas.prefab` в Hierarchy.
  - Проверь, что объект **включён** (Active).
  - В `HUDCanvas` найди компонент `GameplayHUDController` и проверь, что ссылки на UI-элементы назначены (Image/Text поля).

- **Code**
  - Смотреть: `Assets/_Scripts/UI/GameplayHUDController.cs`
    - поля `hpFillImage`, `hpValueText`, `manaFillImage`, `xpFillImage`, `levelValueText`, `weaponIconImage`.

- **Check**
  - Запусти Play Mode: HUD должен быть виден сразу.
  - Если HUD “пропал”, проверь у `HUDCanvas`:
    - `RectTransform` (scale/позиция);
    - `Canvas` и `Canvas Scaler` (см. шаг 3.2).

---

### 3.2. Масштабирование UI под разные разрешения (обязательно)

Цель: HUD должен оставаться на месте на **16:9**, **21:9** и **4:3**.

#### 3.2.1. Canvas Scaler (масштаб всего UI)

- **Editor**
  - Выдели `HUDCanvas`.
  - В `Canvas Scaler` выставь:
    - **UI Scale Mode**: `Scale With Screen Size`
    - **Reference Resolution**: `1920 x 1080` (или то, под что рисовался твой HUD)
    - **Screen Match Mode**: `Match Width Or Height`
    - **Match**: начни с `0.5`, потом подстрой под свой визуал

- **Code**
  - В коде HUD масштабированием **не занимается** — это зона ответственности `RectTransform` + `Canvas Scaler`.

- **Check**
  - HUD стал выглядеть одинаково “по размеру” при смене разрешения (он масштабируется, а не остаётся в пикселях как на одном мониторе).

#### 3.2.2. Anchors и Pivot (чтобы элементы не “уезжали”)

- **Editor**
  - Правило №1: **каждый блок HUD должен быть привязан якорями к своему месту**.
    - HP: `Top-Left`
    - Mana: обычно рядом → тоже `Top-Left`
    - XP/Level: туда, где по макету (часто `Top-Center` или `Bottom-Left`) — но якорь должен соответствовать месту
    - Weapon icon: чаще `Top-Right`
  - Правило №2: внутри блока (fill, текст) элементы позиционируются **относительно родителя**, а не “где-то на экране”.
  - Правило №3: **Pivot выбирай под anchors**:
    - `Top-Left` → pivot (0, 1)
    - `Top-Right` → pivot (1, 1)
    - `Bottom-Left` → pivot (0, 0)

- **Check (Game View)**
  - Проверь в Game View 3 разрешения:
    - `1920x1080` (16:9)
    - `2560x1080` (21:9)
    - `1024x768` (4:3)
  - HUD не должен:
    - вылезать за экран;
    - менять отступы от краёв “на глаз”;
    - налезать сам на себя.

---

### 3.3. Проверь обновление HP/Mana/XP/Level

- **Editor**
  - На `HUDCanvas` в `GameplayHUDController` лучше назначить ссылки явно (так надёжнее и понятнее):
    - `playerStats` (на игроке)
    - `playerProgression` (на игроке)
    - `weaponManager` (на игроке)

- **Code**
  - `GameplayHUDController` подписывается в `OnEnable`:
    - `PlayerStats.OnHealthChanged`, `PlayerStats.OnManaChanged`
    - `PlayerProgression.OnExperienceChanged`, `PlayerProgression.OnLevelUp`
  - И делает `RefreshAll()` при включении, чтобы HUD сразу показал корректные значения.

- **Check**
  - Получи урон → меняется HP fill и число.
  - Потрать ману (если в проекте это уже подключено) → меняется mana fill.
  - Убей врага → растёт XP fill; при накоплении → растёт уровень.

---

### 3.4. Проверь иконку оружия и переключение

- **Editor**
  - Убедись, что у `WeaponData` для каждого оружия назначена `icon` (Sprite).
  - В HUD назначена ссылка `weaponIconImage`.

- **Code**
  - `WeaponManager` вызывает `OnWeaponChanged(currentWeapon)`.
  - HUD ставит:
    - `weaponIconImage.sprite = weapon.WeaponData.icon`
    - `weaponIconImage.enabled = (icon != null)`

- **Check**
  - Переключи оружие (Next/Prev) → иконка должна меняться.
  - Если у оружия нет иконки → картинка скрывается (Image выключается).

---

### 3.5. Подключи Pause / Lose / Win экраны

- **Editor**
  - На сцене должны быть панели:
    - `Pause` (выключена на старте)
    - `LosePanel` (выключена на старте)
    - `WinPanel` (выключена на старте)
  - В `PauseController` назначь:
    - `pausePanel`, `buttonResume`, `buttonMainMenu`
  - В `GameLoopFlowController` назначь:
    - `losePanel`, `loseRestartButton`, `loseMenuButton`
    - `winPanel`, `winMenuButton`, `winNextLevelButton`
    - `pausePanel` (чтобы пауза не висела поверх win/lose)

- **Code**
  - Смотреть:
    - `Assets/_Scripts/UI/PauseController.cs`
    - `Assets/_Scripts/UI/GameLoopFlowController.cs`

- **Check**
  - Нажми Pause → появляется pause панель, Resume возвращает в игру.
  - Умри → показывается Lose, кнопки работают.
  - Выиграй → показывается Win, кнопки работают.

---

## 4. Частые ошибки и хрупкие места

- **HUD “не видно”**
  - Проверь, что `HUDCanvas` активен.
  - Проверь `RectTransform` (scale/позиция) и `Canvas Scaler`.

- **UI съезжает на другом разрешении**
  - Значит anchors/pivot выставлены неправильно (элемент визуально в углу, но якорь стоит в центре/в другом месте).

- **XP не растёт**
  - Проверь, что в сцене есть `EnemyDeathRewarder`, а враги имеют `EnemyStats`, и при смерти есть `ExperienceReward`.

- **Иконка оружия не меняется**
  - Проверь `WeaponData.icon`.
  - Проверь подписку HUD на `WeaponManager.OnWeaponChanged`.

- **Pause висит поверх Win/Lose**
  - В `GameLoopFlowController` должно быть назначено поле `pausePanel`.

---

## 5. Mini smoke-test (чеклист)

- [ ] HUD виден сразу после старта уровня
- [ ] HP: меняется fill и число при уроне
- [ ] Mana: меняется fill (если мана тратится в проекте)
- [ ] XP/Level: XP растёт за убийства, уровень обновляется
- [ ] Weapon icon: меняется при переключении оружия
- [ ] Pause: открывается/закрывается, кнопки работают
- [ ] Lose: появляется при смерти игрока, Restart/Menu работают
- [ ] Win: появляется при победе, Menu/Next работают
- [ ] На `1920x1080`, `2560x1080`, `1024x768` HUD не съезжает

---

## 6. Что будет дальше

Дальше ты закрепишь UI как систему: подготовишь перенос состояния (HP/Mana/XP/Level/оружие) между уровнями, чтобы HUD всегда показывал актуальные значения.

