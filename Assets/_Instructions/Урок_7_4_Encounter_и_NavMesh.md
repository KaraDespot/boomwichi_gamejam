# Encounter + NavMesh: волны врагов и навигация

---

## 0. Что изменилось в коде (кратко)

### Добавлено

- **Encounter-система (данные + триггер)**:
  - `Assets/_Scripts/Encounters/EncounterData.cs`
  - `Assets/_Scripts/Encounters/WaveData.cs`
  - `Assets/_Scripts/Encounters/EncounterTrigger.cs`
- **ScriptableObject-конфиги для примера**:
  - `Assets/_ScriptableObjects/Encounters/EncounterData_Lesson4_Intro.asset`
  - `Assets/_ScriptableObjects/Encounters/WaveData_SpiderStarter.asset`
  - `Assets/_ScriptableObjects/Encounters/WaveData_SkeletonFinisher.asset`
- **Навигация на сцене (teacher repo)**:
  - `Assets/_Scenes/GameScene/NavMesh-NavMeshSurface_Lesson7.4.asset` (baked NavMesh)

### Изменено

- **Враг**:
  - `Assets/_Scripts/Enemies/EnemyBase.cs` — движение через `NavMeshAgent` + “страховка” (fallback) и учебные дефолты при автодобавлении агента
- **Спавн под encounter**:
  - `Assets/_Scripts/Enemies/EnemySpawner.cs` — спавн “конкретного типа врага в конкретной точке” для волновой системы
- **Упрощённый спавнер (прояснён и расширен)**:
  - `Assets/_Scripts/Enemies/SimpleEnemySpawner.cs` — пояснения “это облегчённый вариант” + fallback-метод для encounter
- **Сцена teacher repo**:
  - `Assets/_Scenes/GameScene.unity` — добавлены объекты для демонстрации: `EncounterTrigger_Lesson7.4`, `NavMeshSurface_Lesson7.4`, маркер/объект, который включается после завершения encounter

---

## 1. Зачем этот этап

До этого враги могли существовать “сами по себе” (стоять на сцене или спавниться по таймеру). Теперь мы делаем шаг к уровню, который реально проходится как игра:

- игрок **входит в зону** → начинается бой (encounter);
- враги приходят **волнами**;
- враги **двигаются умно** (обходят препятствия) благодаря NavMesh;
- когда все враги добиты — **открывается выход** (или включается нужный объект).

---

## 2. Из чего состоит система и как связаны компоненты (карта связей)

- **`WaveData` (ScriptableObject)**: “одна волна”
  - какого врага спавнить (`EnemyData`)
  - сколько врагов (`EnemyCount`)
  - задержки и интервал (`StartDelay`, `SpawnInterval`)
  - ждать ли добивания перед следующей волной (`WaitUntilWaveDefeated`)
- **`EncounterData` (ScriptableObject)**: “набор волн + правила”
  - массив волн (`waves`)
  - пауза между волнами (`DelayBetweenWaves`)
  - `OneShot` — можно ли пройти один раз за сцену (в этом курсе это **для экспериментов**)
- **`EncounterTrigger` (MonoBehaviour на сцене)**: “режиссёр”
  - запускает волны при входе игрока в trigger
  - следит за живыми врагами через `EnemyBase.OnDied`
  - завершает encounter **только после добивания** оставшихся врагов
  - включает объекты из `activateOnCompleted` (например, выход)
- **Спавнеры (MonoBehaviour на сцене)**:
  - **`EnemySpawner`** — каноничный вариант для encounter/wave (умеет спавнить *конкретного врага в конкретной точке*)
  - **`SimpleEnemySpawner`** — облегчённый вариант для ранних шагов; в 7.4 может использоваться как **fallback**, чтобы encounter не ломался “молчаливо”
- **Навигация (на сцене + на враге)**:
  - `NavMeshSurface` на сцене + Bake NavMesh
  - `NavMeshAgent` у врага (в teacher repo он может добавляться автоматически, чтобы не требовать ручной переделки префабов)

---

## 2.1. NavMesh простыми словами: зачем, как пользоваться и что это даёт

Представь, что уровень — это город, а NavMesh — это “карта дорог”, по которым разрешено ходить.

- **Зачем нужен NavMesh**
  - Чтобы враги **обходили стены и углы**, а не пытались идти “сквозь” препятствия.
  - Чтобы мы не писали сложную математику поиска пути вручную.
  - Чтобы поведение было **предсказуемым**: враг идёт туда, куда можно пройти.

- **Из чего состоит система (минимум)**
  - **`NavMeshSurface`** (на сцене): компонент, который умеет **собрать** (Bake) навигационную сетку.
  - **Bake (запекание)**: момент, когда Unity строит “дороги” на основе геометрии уровня.
  - **`NavMeshAgent`** (на враге): компонент, который умеет **двигаться по NavMesh** и строить путь.

- **Что можно делать с помощью NavMesh в этой теме**
  - Задать врагу цель (игрока) и сказать: “иди к нему” — агент сам найдёт путь вокруг препятствий.
  - Ограничить, насколько близко враг подходит, через `stoppingDistance` (у нас это связано с `AttackRange`).
  - Делать простую AI-логику “вижу → бегу → атакую” без ручного pathfinding.

- **Для чего NavMesh может пригодиться в целом**
  - **NPC и союзники**: чтобы они ходили по уровню так же “разумно”, как враги (не упирались в стены).
  - **Патрули/маршруты**: можно дать персонажу несколько точек и он будет ходить между ними по NavMesh (это уже следующий уровень, но идея та же).
  - **Безопасные зоны и “нельзя ходить сюда”**: можно делать области, куда агент не заходит (например, лава/обрывы/опасные места).
  - **Переходы через специальные места**: прыжки/лестницы/двери/мостики часто делаются как “специальные переходы” навигации (позже).
  - **Click-to-move** (если когда-нибудь понадобится): игрок кликает по земле, а персонаж идёт туда по NavMesh.

- **Как этим пользоваться (шпаргалка на 30 секунд)**
  - На сцене: есть `NavMeshSurface` → нажали **Bake** → появились “разрешённые зоны ходьбы”.
  - На враге: есть `NavMeshAgent` → в коде вызывается `SetDestination(target.position)`.
  - В Play Mode: если NavMesh не готов, враг может перейти на **fallback-движение** (это страховка, чтобы игра не ломалась), и в Console появится подсказка, что проверить.

## 3. Пошагово (три слоя: Editor / Code / Check)

### 3.1. Подготовь NavMesh на сцене (чтобы враги ходили “по дороге”, а не в стену)

- **Editor**
  - Открой `GameScene`.
  - Найди объект `NavMeshSurface_Lesson7.4` (или создай новый пустой объект и добавь на него компонент `NavMeshSurface`).

  - **Как правильно сделать Bake (самое важное)**
    1. Выдели объект с `NavMeshSurface` в Hierarchy.
    2. В Inspector у компонента `NavMeshSurface` проверь базовые вещи:
       - **Layer Mask**: включены слои, из которых состоит “земля/пол”, по которому должны ходить враги.
       - **Use Geometry**: откуда брать “форму уровня” для запекания (обычно можно оставить как есть; важно, чтобы ваш пол реально учитывался при Bake).
       - **Ignore NavMeshAgent / Ignore NavMeshObstacle**: обычно можно оставить включёнными (чтобы сами агенты не мешали запеканию).
    3. Нажми кнопку **Bake** в этом же компоненте `NavMeshSurface`.

  - **Как понять, что Bake сработал**
    - После Bake на сцене появляется NavMesh-данные (в teacher repo это сохранено как ассет NavMesh).
    - В Scene View обычно видно “покрытие” навигации, когда выделен объект `NavMeshSurface` и включены Gizmos.
    - Если покрытия не видно, но Bake был: попробуй открыть окно навигации (если оно есть в твоей версии Unity): `Window` → `AI` → `Navigation`, и включить отображение NavMesh.

- **Code**
  - Враг двигается через `NavMeshAgent` внутри `EnemyBase`. Если NavMesh не запечён или враг “не на NavMesh”, сработает fallback-движение.
  - Смотреть: `Assets/_Scripts/Enemies/EnemyBase.cs` → `TryMoveWithNavMesh()` (там есть предупреждение, что проверить).

- **Check**
  - Запусти Play Mode.
  - Враг должен **обходить препятствия**, а не пытаться идти по прямой через стену.
  - Если враг “тупит” или в Console есть предупреждение про NavMesh fallback:
    - перепроверь, что ты нажал **Bake** на `NavMeshSurface`;
    - проверь, что враг стоит **на полу**, а не чуть выше/ниже (агент должен оказаться “на сетке”);
    - проверь, что “пол” попадает в **Layer Mask** у `NavMeshSurface`.

---

### 3.2. Создай данные волн (WaveData) и собери EncounterData

- **Editor**
  - В `Project` открой папку `Assets/_ScriptableObjects/Encounters/`.
  - Создай 1–2 `WaveData`:
    - укажи `EnemyData` (melee или ranged)
    - поставь `EnemyCount` (например, 3)
    - поставь `SpawnInterval` (например, 0.5)
    - реши `WaitUntilWaveDefeated` (для обучения чаще **true**)
  - Создай `EncounterData` и заполни:
    - `waves` = твои `WaveData`
    - `DelayBetweenWaves` (например, 1)
    - `OneShot` — по желанию (это **режим эксперимента**, а не обязательное правило)

- **Code**
  - Валидация “волна вообще настроена”:
    - `WaveData.IsValid` проверяет, что `EnemyData` назначен и `EnemyData.prefab` не пустой.
  - Валидация “encounter готов”:
    - `EncounterData.HasAnyValidWave()` должна быть true.
  - Смотреть:
    - `Assets/_Scripts/Encounters/WaveData.cs`
    - `Assets/_Scripts/Encounters/EncounterData.cs`

- **Check**
  - В инспекторе у `WaveData` не должно быть пустого `EnemyData`.
  - В `EnemyData` должен быть назначен `prefab`, иначе волна будет считаться невалидной и может пропускаться.

---

### 3.3. Поставь EncounterTrigger в сцену и свяжи его с данными и выходом

- **Editor**
  - На сцене создай объект `EncounterTrigger_Lesson7.4` (или используй уже готовый).
  - Добавь:
    - `Collider` (например, `CapsuleCollider`) и включи **Is Trigger**
    - компонент `EncounterTrigger`
  - В `EncounterTrigger` назначь:
    - `Encounter Data` = твой `EncounterData`
    - `Enemy Spawner` = `EnemySpawner` (рекомендуемый вариант)
    - (опционально) `Simple Enemy Spawner` = `SimpleEnemySpawner` (fallback)
    - `Activate On Completed` = объект “выход/маркер” (в teacher repo есть пример `ExitUnlockedMarker_Lesson7.4`)
  - Выставь размер коллайдера так, чтобы игрок реально входил в зону.

- **Code**
  - `EncounterTrigger` стартует при `OnTriggerEnter`, если вошёл Player (по tag или по наличию `PlayerController`).
  - Важно: если нет `EnemySpawner`, будет попытка fallback на `SimpleEnemySpawner`, и в консоли будет предупреждение (это сделано специально, чтобы ученик не ловил “тишину”).
  - Смотреть: `Assets/_Scripts/Encounters/EncounterTrigger.cs` → `Awake()`, `StartEncounter()`, `SpawnWaveEnemy()`.

- **Check**
  - Запусти сцену и зайди игроком в триггер-зону.
  - Должно быть видно:
    - пошёл спавн врагов волны
    - после добивания всех врагов — включается объект из `activateOnCompleted` (например, “выход” стал активным)

---

### 3.4. Проверь, что враги “привязаны к цели” (иначе они будут стоять или вести себя странно)

- **Editor**
  - Убедись, что в сцене есть игрок с `PlayerController`.
  - Если спавнишь врагов через `EnemySpawner`/encounter, цель обычно выставляется автоматически при спавне.

- **Code**
  - `EnemySpawner.SpawnEnemy(...)` назначает цель через `enemy.SetTarget(target)`.
  - `EnemyBase` также может искать цель сам (если `autoResolveTargetOnStart = true`).
  - Смотреть:
    - `Assets/_Scripts/Enemies/EnemySpawner.cs` → `SpawnEnemy(EnemyData data, Transform spawnPointOverride, Transform targetOverride = null)`
    - `Assets/_Scripts/Enemies/EnemyBase.cs` → `SetTarget()`, `ResolveTargetOnce()`

- **Check**
  - В Play Mode враги должны реально преследовать игрока (в пределах `DetectionRange`) и атаковать в `AttackRange`.

---

## 4. Частые ошибки и хрупкие места

### 4.1. Trigger не работает

- **Симптом**: заходишь в зону — ничего не происходит.
- **Проверь**:
  - у коллайдера стоит **Is Trigger**
  - у игрока корректный tag `Player` (или есть `PlayerController`)
  - в `EncounterTrigger` назначен `EncounterData`

### 4.2. Encounter не спавнит врагов

- **Симптом**: триггер сработал, но враги не появляются.
- **Проверь**:
  - в `WaveData` назначен `EnemyData`, а в `EnemyData` назначен `prefab`
  - есть точки спавна: либо `encounterSpawnPoints`, либо точки в спавнере
  - на сцене есть `EnemySpawner` (рекомендуется) или `SimpleEnemySpawner` (fallback)

### 4.3. Враги “не ходят по умному” (NavMesh)

- **Симптом**: враг идёт странно/упирается/движется “вручную”.
- **Проверь**:
  - сделан Bake NavMesh
  - `NavMeshSurface` активен
  - враг стоит на NavMesh (не висит в воздухе/не под землёй)
- **Важно**: в teacher repo есть fallback-движение, чтобы игра не ломалась, но для обучения NavMesh нужно уметь чинить причину.

### 4.4. Null при disable/enable и “двойные события”

- **Симптом**: ошибки в Console при выключении объектов или странные двойные завершения.
- **Идея**: `EncounterTrigger` отписывается и чистит tracked-врагов в `OnDisable`, а `EnemyBase.Die()` защищён от двойной смерти через `isDead`.

---

## 5. Mini smoke-test (чеклист)

- [ ] В `GameScene` есть `NavMeshSurface` и NavMesh запечён (враги обходят препятствия).
- [ ] Есть `EncounterData` с минимум одной валидной `WaveData`.
- [ ] `EncounterTrigger` стоит на объекте с `Collider (Is Trigger = true)`.
- [ ] При входе игрока в триггер:
  - [ ] стартует encounter (видно по поведению/логам)
  - [ ] враги появляются волнами
- [ ] После убийства всех врагов:
  - [ ] включается объект из `activateOnCompleted` (выход/маркер)
- [ ] В Console нет новых ошибок (кроме допустимых учебных предупреждений, которые ты понимаешь и можешь объяснить).

---

## 6. Что будет дальше

Дальше этот encounter-слой подключается к “победе/поражению” и логике уровня: выход активируется после обязательных encounter, а затем игра может корректно завершаться (win/lose flow).

