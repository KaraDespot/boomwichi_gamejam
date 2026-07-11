# Анимации игрока: Animator basics + Animation Event для синхронизации атаки

---

## 0. Что изменилось в коде (кратко)

Ниже — изменения, которые составляют “смысл” ветки **8.1** относительно ветки **7.4** (по факту текущего состояния teacher repo).

### Добавлено

- **Слой анимации игрока (отдельная ответственность):**
  - `Assets/_Scripts/Player/PlayerAnimationController.cs` — обновляет параметры Animator и запускает состояния Attack/Death.
- **Мост для Animation Event (правильная синхронизация):**
  - `Assets/_Scripts/Player/PlayerAttackAnimationEvents.cs` — принимает Animation Events из клипа и передаёт их в боевую логику.
- **Боевой “оркестратор” атаки игрока (фазы атаки + точки расширения под SFX/VFX):**
  - `Assets/_Scripts/Player/PlayerCombatController.cs` — запускает атаку, ждёт Animation Event, в событие вызывает реальное действие оружия.

### Добавлено / настроено в анимациях

- **Клипы игрока:**
  - `Assets/_Art/Animations/Player/Idle.anim`
  - `Assets/_Art/Animations/Player/Move.anim`
  - `Assets/_Art/Animations/Player/Death.anim`
  - `Assets/_Art/Animations/Player/Attack_Melee.anim` — содержит Animation Events
  - `Assets/_Art/Animations/Player/Attack_Staff.anim` — содержит Animation Events (используется как “дальняя” атака в демонстрации)
- **Animator Controller игрока:**
  - `Assets/_Art/Animations/Player/Player.controller` — параметры `MoveSpeed`, `Attack` (trigger), `AttackType` (int), `IsDead` (bool) и переходы.

### Изменено / подключено в префабе

- `Assets/_Prefabs/Player/Player.prefab`:
  - на объекте модели (где `Animator`) добавлены `PlayerAnimationController` и `PlayerAttackAnimationEvents`
  - на корне игрока добавлен/подключён `PlayerCombatController`
  - `WeaponManager` теперь **не выполняет урон “сразу”**, а запускает анимацию и ждёт Animation Event: `WeaponManager.HandleAttackPressed()` → `PlayerCombatController.TryStartAttack()`

> Важно: блок “звук/эффекты” в `PlayerCombatController` — **заготовка** (болванки полей под SFX/VFX), без обязательной реализации в этом этапе.

---

## 1. Зачем этот этап

Без анимаций поведение персонажа читается плохо: игрок двигается и атакует “как робот”.  
Но есть ещё более важная проблема: **когда именно должна происходить атака** — в момент нажатия кнопки или в момент, когда меч реально “долетел” до цели?

На этом этапе мы делаем две вещи:

- **Animator basics**: подключаем минимум анимаций игрока (Idle/Move/Attack/Death), чтобы поведение читалось визуально.
- **Правильную синхронизацию атаки**: действие атаки (удар/выстрел) происходит **в конкретный кадр клипа** через `Animation Event`.

Идея простая:

- нажатие кнопки = “начать красивую атаку”  
- `Animation Event` = “вот сейчас — реальный удар/выстрел”

---

## 2. Из чего состоит система и как связаны компоненты (карта связей)

### 2.1. Компоненты и их роли

- `InputManager`
  - поднимает событие `OnAttackPressed` при нажатии кнопки атаки.
- `WeaponManager`
  - подписывается на `OnAttackPressed`
  - **не наносит урон сам** в момент нажатия
  - запускает боевой цикл атаки через `PlayerCombatController.TryStartAttack()`.
- `PlayerCombatController`
  - принимает запрос “начать атаку”
  - проверяет условия (не мёртв, есть оружие, оружие может атаковать, атака не в процессе)
  - запускает анимацию через `PlayerAnimationController`
  - **ждёт Animation Event**
  - когда приходит `Animation Event` действия — вызывает `WeaponManager.PerformCurrentWeaponAttack()`
  - когда приходит `Animation Event` окончания — снимает блокировку и разрешает следующую атаку
  - содержит **точки расширения** под звук/эффекты (болванки).
- `PlayerAnimationController`
  - отдельный слой “gameplay → Animator”
  - обновляет параметр `MoveSpeed`
  - выставляет `AttackType` и дёргает trigger `Attack`
  - включает `IsDead` при смерти
  - **не решает боевые правила** (это делает `PlayerCombatController`).
- `PlayerAttackAnimationEvents`
  - висит рядом с `Animator` на визуальном объекте
  - содержит методы, которые вызываются из Animation Event в клипе
  - просто перенаправляет событие в `PlayerCombatController`.

### 2.2. Схема вызовов (самое важное)

1) Игрок нажал кнопку атаки  
2) `InputManager` вызывает `OnAttackPressed`  
3) `WeaponManager` ловит событие → вызывает `PlayerCombatController.TryStartAttack()`  
4) `PlayerCombatController`:
   - выбирает тип анимации (melee/ranged)
   - просит `PlayerAnimationController` запустить атаку (`PlayAttack(...)`)
5) Animator проигрывает клип атаки  
6) В нужный кадр клипа срабатывает **Animation Event**:
   - `OnAttackActionAnimationEvent()` → реальное действие оружия (`PerformCurrentWeaponAttack()`)
7) В конце клипа срабатывает второй **Animation Event**:
   - `OnAttackFinishedAnimationEvent()` → разблокировка следующей атаки

---

## 3. Пошагово (Editor / Code / Check)

### 3.1. Подготовить Animator Controller игрока (Idle/Move/Attack/Death)

- **Editor**
  - Открой `Player.controller` (Animator Controller игрока): `Assets/_Art/Animations/Player/Player.controller`.
  - Убедись, что в нём есть состояния (минимум):
    - `Idle`
    - `Move`
    - `Attack` (минимум 2 варианта: melee и “ranged”)
    - `Death`
  - Убедись, что параметры Animator существуют и названы **точно так же**:
    - `MoveSpeed` (float)
    - `Attack` (trigger)
    - `AttackType` (int)
    - `IsDead` (bool)
  - Настрой базовые переходы:
    - Idle ↔ Move по `MoveSpeed` (например, \(> 0.1\) в Move и \(< 0.1\) в Idle)
    - Any State → Death по `IsDead == true`
    - Any State (или Idle/Move) → Attack по trigger `Attack`
    - внутри Attack — выбор клипа по `AttackType` (0 = melee, 1 = ranged).

- **Code**
  - Параметры дёргаются из `PlayerAnimationController`:
    - `MoveSpeed` обновляется каждый кадр по силе движения.
    - `AttackType` выставляется перед атакой.
    - trigger `Attack` запускает сам факт атаки.
    - `IsDead` включается при смерти.
  - Смотреть:
    - `Assets/_Scripts/Player/PlayerAnimationController.cs` → `Update()`, `PlayAttack(...)`, `PlayDeath()`.

- **Check**
  - Запусти Play Mode.
  - Подвигайся: персонаж должен переключаться Idle ↔ Move.
  - Умри (или принудительно доведи HP до 0): должна включиться анимация Death и движение визуально остановится.

---

### 3.2. Подключить “слой анимации” и “мост событий” на префаб игрока

- **Editor**
  - Открой `Player.prefab`: `Assets/_Prefabs/Player/Player.prefab`.
  - Найди объект визуальной модели (обычно `visual` или объект, где висит `Animator`).
  - На объекте с `Animator` должны быть компоненты:
    - `PlayerAnimationController`
    - `PlayerAttackAnimationEvents`
  - На корне игрока должен быть компонент:
    - `PlayerCombatController`
  - Проверь ключевые связи в инспекторе:
    - в `PlayerAnimationController`:
      - `Animator` назначен (это **тот самый** Animator на модели)
      - `PlayerStats` назначен (или найдётся автоматически)
    - в `PlayerAttackAnimationEvents`:
      - `PlayerCombatController` назначен (или найдётся автоматически)
    - в `PlayerCombatController`:
      - `WeaponManager` назначен (или найдётся автоматически)
      - `PlayerAnimationController` назначен (или найдётся автоматически из дочерних)

- **Code**
  - По умолчанию эти скрипты пытаются найти связи сами в `Awake()`, но teacher repo должен показывать “как правильно” — когда ссылки очевидны и не теряются.
  - Смотреть:
    - `PlayerAttackAnimationEvents.Awake()`
    - `PlayerCombatController.Awake()`
    - `PlayerAnimationController.Awake()`

- **Check**
  - В Play Mode нажми Attack: должна запускаться анимация атаки.
  - Если анимация не запускается, проверь:
    - `WeaponManager` подписался на input
    - `PlayerCombatController` не блокирует атаку (не мёртв, есть оружие, оружие может атаковать).

---

### 3.3. Сделать 2 Animation Event в клипе атаки (момент действия и конец атаки)

Это главная часть этапа.

#### Какие события делаем и зачем

1) **Событие действия атаки** — `OnAttackActionAnimationEvent`
   - Зачем: чтобы удар/выстрел произошёл **ровно в нужный кадр** клипа.
   - Что делает: вызывает `PlayerCombatController.HandleAttackActionAnimationEvent()` → `WeaponManager.PerformCurrentWeaponAttack()`.

2) **Событие окончания атаки** — `OnAttackFinishedAnimationEvent`
   - Зачем: чтобы игра точно знала, когда “атака закончилась” и можно снова атаковать.
   - Что делает: вызывает `PlayerCombatController.HandleAttackFinishedAnimationEvent()` и снимает блокировку `isAttackInProgress`.

- **Editor**
  - Открой клип атаки игрока, например:
    - `Assets/_Art/Animations/Player/Attack_Melee.anim`
    - (для “дальней” демонстрации) `Assets/_Art/Animations/Player/Attack_Staff.anim`
  - Открой окно Animation (таймлайн клипа).
  - Выбери кадр, где “действие” должно произойти:
    - melee: момент, когда оружие визуально “попадает”
    - ranged: момент, когда должно “вылететь” projectile
  - Добавь Animation Event и укажи функцию:
    - `OnAttackActionAnimationEvent`
  - В самом конце клипа добавь второй Animation Event:
    - `OnAttackFinishedAnimationEvent`
  - Убедись, что **на том же объекте**, где стоит `Animator`, есть компонент `PlayerAttackAnimationEvents` (иначе Unity не найдёт методы события).

- **Code**
  - Приём событий:
    - `Assets/_Scripts/Player/PlayerAttackAnimationEvents.cs`
      - `OnAttackActionAnimationEvent()` → `PlayerCombatController.HandleAttackActionAnimationEvent()`
      - `OnAttackFinishedAnimationEvent()` → `PlayerCombatController.HandleAttackFinishedAnimationEvent()`
  - Реальное действие оружия в событие:
    - `Assets/_Scripts/Player/PlayerCombatController.cs` → `HandleAttackActionAnimationEvent()` → `WeaponManager.PerformCurrentWeaponAttack()`
  - Разблокировка атаки:
    - `PlayerCombatController.HandleAttackFinishedAnimationEvent()`

- **Check**
  - Самый простой чек: включи `enableDebugLogs` в `PlayerCombatController` (в teacher repo он может быть уже включён).
  - Запусти Play Mode и нажми Attack:
    - в Console должно быть видно, что атака стартовала и ждёт Animation Event
    - затем — сообщение, что сработало событие действия
    - затем — сообщение, что атака завершена
  - В визуале/геймплее:
    - melee: урон (или действие оружия) происходит не сразу, а в момент “удара”
    - ranged: projectile спавнится/выстреливает именно в момент “выстрела”

---

### 3.4. Научить Animator выбирать melee/ranged атаку через `AttackType`

- **Editor**
  - В `Player.controller` убедись, что параметр `AttackType` реально влияет на выбор атаки.
  - Простой учебный вариант:
    - `AttackType == 0` → клип `Attack_Melee`
    - `AttackType == 1` → клип `Attack_Staff` (как “дальняя” демонстрация)

- **Code**
  - Выбор типа анимации происходит в `PlayerCombatController.ResolveAttackAnimationType()`:
    - если текущее оружие — `RangedWeapon`, то тип = ranged
    - иначе = melee
  - Запуск:
    - `PlayerAnimationController.PlayAttack(...)` сначала ставит `AttackType`, затем триггерит `Attack`.

- **Check**
  - Поменяй оружие (если в проекте доступно переключение).
  - При melee оружии должна играться melee-атака.
  - При ranged оружии должна играться “ranged” атака (в teacher repo это может быть клип с посохом, это ок как демонстрация).

---

## 4. Частые ошибки и хрупкие места

### 4.1. Animation Event не срабатывает

- **Симптом**: анимация атаки играет, но удар/выстрел не происходит.
- **Причины**:
  - событие добавлено не в тот клип (или клип не используется в state)
  - в событии выбрана не та функция (опечатка)
  - на объекте с `Animator` нет `PlayerAttackAnimationEvents`

### 4.2. Срабатывает только первый event, а конец атаки “завис”

- **Симптом**: после одной атаки больше нельзя атаковать.
- **Причина**: забыли поставить `OnAttackFinishedAnimationEvent` в конце клипа.
- **Идея**: второй event — это “разрешение следующей атаки”.

### 4.3. Анимация не переключается на ranged

- **Симптом**: всегда играет melee, даже с дальним оружием.
- **Проверь**:
  - оружие реально является `RangedWeapon`
  - в Animator есть логика перехода/выбора по `AttackType == 1`

### 4.4. После смерти анимации/движение продолжаются странно

- **Симптом**: персонаж после смерти ещё “бежит” по анимации.
- **Проверь**:
  - событие смерти от `PlayerStats` реально приходит
  - `IsDead` параметр есть и используется в переходе в `Death`

---

## 5. Mini smoke-test (чеклист)

- [ ] В Animator есть параметры `MoveSpeed`, `Attack`, `AttackType`, `IsDead`
- [ ] Idle ↔ Move переключаются по `MoveSpeed`
- [ ] Атака запускается по trigger `Attack`
- [ ] В клипе атаки есть **2 Animation Event**:
  - [ ] `OnAttackActionAnimationEvent` (момент действия)
  - [ ] `OnAttackFinishedAnimationEvent` (конец атаки)
- [ ] В Play Mode видно, что реальное действие атаки происходит **в нужный кадр**, а не “сразу по кнопке”
- [ ] После завершения клипа можно снова атаковать (нет “залипания” атаки)

---

## 6. Что будет дальше

Дальше эта же схема (Animator + событие в нужный кадр) будет применяться к врагам и к более “богатым” атакам (эффекты, звук, разные типы ударов), но базовый принцип синхронизации останется тем же.

