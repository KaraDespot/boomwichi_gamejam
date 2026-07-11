# Урок 2.3: SceneLoader — единая точка загрузки сцен

### Зачем
Чтобы не разбрасывать `SceneManager.LoadScene(...)` по всему проекту.

### Требования к `SceneLoader`
- Метод `Load(string sceneName)`
- (бонус, позже) корутина для загрузки асинхронно `LoadAsync`
- Логи в консоль при загрузке (полезно для обучения)

### Советы для учеников
- Сцены лучше грузить **по имени**, но:
  - имя сцены должно быть **в Build Settings**
  - имя должно совпадать с файлом `.unity`
- Хорошая практика: завести класс `SceneNames` с константами (см. подробное объяснение в разделе **Урок 2.1** выше):
  - `public const string Bootstrap = "Bootstrap";` и т.д.
  - Это защищает от опечаток и делает код понятнее

### Пример кода `SceneLoader`

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

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

    public void Load(string sceneName)
    {
        Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    // БОНУС: асинхронная загрузка (можно добавить позже)
    public void LoadAsync(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            // Здесь можно обновлять прогрессбар
            Debug.Log($"Loading progress: {operation.progress}");
            yield return null;
        }

        Debug.Log("Scene loaded");
    }
}
```

---

### Объяснение: Что такое `SceneManager.LoadScene`?

**`SceneManager.LoadScene`** — это встроенный метод Unity для загрузки сцен.

**Как работает:**
```csharp
SceneManager.LoadScene("MainMenu"); // загружает сцену по имени
```

**Что происходит:**
1. Unity находит сцену с таким именем
2. Выгружает текущую сцену (уничтожает все объекты)
3. Загружает новую сцену
4. Игра продолжается в новой сцене

**Важно:**
- Сцена **должна быть** в Build Settings
- Имя сцены должно **точно совпадать** с именем файла (без `.unity`)
- Загрузка происходит **мгновенно** (игра "зависает" на момент загрузки)

**Проблема синхронной загрузки:**
```csharp
// Плохо: игра "зависает" на время загрузки
SceneManager.LoadScene("GameScene"); 
// Игрок видит черный экран, не может понять, что происходит
```

**Решение — асинхронная загрузка:**
```csharp
// Хорошо: загрузка происходит постепенно, можно показать прогресс
SceneLoader.Instance.LoadAsync("GameScene");
// Можно показать прогрессбар, игрок видит, что игра работает
```

**Аналогия:** 
- Синхронная загрузка = резко переключить канал на ТВ (мгновенно, но может "моргнуть")
- Асинхронная загрузка = плавно переключить канал (плавно, с анимацией)

---

### Объяснение: Что такое корутины (Coroutines)?

**Корутина** — это метод, который может "приостановиться" и продолжить выполнение позже.

**Обычный метод:**
```csharp
public void DoSomething()
{
    Debug.Log("Шаг 1");
    Debug.Log("Шаг 2");
    Debug.Log("Шаг 3");
    // Все выполняется сразу, за один кадр
}
```

**Корутина:**
```csharp
public IEnumerator DoSomething()
{
    Debug.Log("Шаг 1");
    yield return null; // приостановиться на 1 кадр
    
    Debug.Log("Шаг 2");
    yield return new WaitForSeconds(1f); // приостановиться на 1 секунду
    
    Debug.Log("Шаг 3");
    // Выполняется постепенно, не блокируя игру
}
```

**Как это работает:**
- `IEnumerator` — тип возвращаемого значения (интерфейс для итератора)
- `yield return null` — "подожди один кадр, потом продолжай"
- `yield return new WaitForSeconds(1f)` — "подожди 1 секунду, потом продолжай"
- `StartCoroutine(...)` — запускает корутину

**Пример в SceneLoader:**
```csharp
private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
{
    // Начинаем загрузку
    AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
    
    // Пока загрузка не завершена
    while (!operation.isDone)
    {
        // Каждый кадр проверяем прогресс
        Debug.Log($"Loading progress: {operation.progress}");
        yield return null; // ждем следующий кадр
    }
    
    // Загрузка завершена
    Debug.Log("Scene loaded");
}
```

**Зачем это нужно:**
- ✅ Игра не "зависает" во время загрузки
- ✅ Можно показывать прогрессбар
- ✅ Можно показывать анимацию загрузки
- ✅ Игрок видит, что игра работает

**Аналогия:** Как загрузка файла — видно прогресс, можно отменить, игра не зависает.

---

### Объяснение: Что такое `AsyncOperation`?

**`AsyncOperation`** — это объект, который представляет асинхронную операцию (загрузку сцены).

**Свойства `AsyncOperation`:**
- `isDone` — `true`, когда загрузка завершена, `false` — еще идет
- `progress` — прогресс от 0.0 до 1.0 (0% до 100%)

**Пример использования:**
```csharp
AsyncOperation operation = SceneManager.LoadSceneAsync("GameScene");

// Проверяем, завершена ли загрузка
if (operation.isDone)
{
    Debug.Log("Загрузка завершена!");
}

// Смотрим прогресс
float percent = operation.progress * 100f; // 0.9 = 90%
Debug.Log($"Загружено: {percent}%");
```

**В цикле:**
```csharp
while (!operation.isDone) // пока НЕ завершено
{
    // Загрузка еще идет
    Debug.Log($"Прогресс: {operation.progress}");
    yield return null; // ждем следующий кадр
}
// Когда вышли из цикла, значит operation.isDone == true
```

**Аналогия:** Как загрузка видео на YouTube — видно процент, можно отслеживать прогресс.

---

### Объяснение: Что такое `yield return null`?

**`yield return null`** — это команда "подожди один кадр, потом продолжай".

**Как это работает:**
```csharp
private IEnumerator Example()
{
    Debug.Log("Кадр 1");
    yield return null; // приостановиться, ждем следующего кадра
    
    Debug.Log("Кадр 2");
    yield return null; // приостановиться, ждем следующего кадра
    
    Debug.Log("Кадр 3");
}
```

**Что происходит:**
1. Выполняется `Debug.Log("Кадр 1")`
2. `yield return null` → Unity говорит: "Ок, подожду до следующего кадра"
3. Unity отрисовывает кадр, обрабатывает ввод
4. Возвращается к корутине, выполняет `Debug.Log("Кадр 2")`
5. И так далее...

**Зачем это нужно в загрузке сцен:**
```csharp
while (!operation.isDone)
{
    Debug.Log($"Progress: {operation.progress}");
    yield return null; // ждем кадр, чтобы проверить прогресс снова
}
```

**Без `yield return null`:**
```csharp
// Плохо: цикл выполнится миллион раз за один кадр
while (!operation.isDone)
{
    Debug.Log($"Progress: {operation.progress}");
    // Нет yield → цикл крутится бесконечно, игра зависает!
}
```

**С `yield return null`:**
```csharp
// Хорошо: проверяем прогресс каждый кадр
while (!operation.isDone)
{
    Debug.Log($"Progress: {operation.progress}");
    yield return null; // ждем кадр, игра не зависает
}
```

**Аналогия:** Как проверка почты — не нужно проверять каждую секунду, достаточно раз в день (`yield return null` = раз в кадр).

---

### Объяснение: Что такое строковая интерполяция (`$"..."`)?

**Строковая интерполяция** — способ вставить переменные в строку.

**Без интерполяции (старый способ):**
```csharp
string sceneName = "MainMenu";
string message = "Loading scene: " + sceneName; // сложение строк
Debug.Log(message);
```

**С интерполяцией (новый способ):**
```csharp
string sceneName = "MainMenu";
Debug.Log($"Loading scene: {sceneName}"); // вставляем переменную в строку
```

**Как работает:**
- `$` перед строкой — включает интерполяцию
- `{переменная}` — место, куда вставится значение переменной

**Примеры:**
```csharp
string name = "Иван";
int age = 15;

// Можно вставлять переменные
Debug.Log($"Имя: {name}, Возраст: {age}"); 
// Выведет: "Имя: Иван, Возраст: 15"

// Можно вычислять выражения
Debug.Log($"Через год будет: {age + 1}"); 
// Выведет: "Через год будет: 16"

// Можно вызывать методы
Debug.Log($"Прогресс: {operation.progress * 100}%"); 
// Выведет: "Прогресс: 75%"
```

**В SceneLoader:**
```csharp
Debug.Log($"Loading scene: {sceneName}");
// Если sceneName = "MainMenu", выведет: "Loading scene: MainMenu"
```

**Преимущества:**
- ✅ Код короче и понятнее
- ✅ Легче читать
- ✅ Можно вставлять выражения

**Аналогия:** Как заполнение бланка — вместо "Имя: " + имя, просто пишешь `{имя}` в нужном месте.

---

### Объяснение: Зачем нужен отдельный `SceneLoader`?

**Проблема без `SceneLoader`:**
```csharp
// В MainMenu:
SceneManager.LoadScene("GameScene");

// В GameScene:
SceneManager.LoadScene("MainMenu");

// В Settings:
SceneManager.LoadScene("MainMenu");

// Проблемы:
// ❌ Если переименуешь сцену, нужно менять везде
// ❌ Можно опечататься в имени сцены
// ❌ Нет единого места для логики загрузки
```

**Решение с `SceneLoader`:**
```csharp
// Везде используем один метод:
SceneLoader.Instance.Load(SceneNames.GameScene);

// Преимущества:
// ✅ Одно место для логики загрузки
// ✅ Легко добавить логирование
// ✅ Легко добавить асинхронную загрузку
// ✅ Защита от опечаток через SceneNames
```

**Что можно добавить в `SceneLoader` позже:**
- Логирование всех загрузок
- Анимация загрузки
- Проверка существования сцены
- Сохранение данных перед загрузкой

**Аналогия:** Как единая служба доставки — вместо того, чтобы каждый магазин сам доставлял товары, есть одна служба, которая делает это правильно.

---

### Объяснение: Синхронная vs Асинхронная загрузка

**Синхронная загрузка** (`Load`):
```csharp
public void Load(string sceneName)
{
    SceneManager.LoadScene(sceneName); // загружает сразу, игра "зависает"
}
```

**Что происходит:**
1. Вызывается `Load("GameScene")`
2. Игра останавливается
3. Выгружается текущая сцена
4. Загружается новая сцена
5. Игра продолжается

**Плюсы:**
- ✅ Просто использовать
- ✅ Мгновенная загрузка

**Минусы:**
- ❌ Игра "зависает" на время загрузки
- ❌ Нет возможности показать прогресс
- ❌ Игрок видит черный экран

---

**Асинхронная загрузка** (`LoadAsync`):
```csharp
public void LoadAsync(string sceneName)
{
    StartCoroutine(LoadSceneAsyncCoroutine(sceneName)); // загружает постепенно
}
```

**Что происходит:**
1. Вызывается `LoadAsync("GameScene")`
2. Начинается загрузка в фоне
3. Игра продолжает работать
4. Можно показывать прогрессбар
5. Когда загрузка завершена, сцена переключается

**Плюсы:**
- ✅ Игра не "зависает"
- ✅ Можно показать прогрессбар
- ✅ Можно показать анимацию загрузки
- ✅ Лучший пользовательский опыт

**Минусы:**
- ❌ Сложнее реализовать
- ❌ Нужно управлять состоянием загрузки

**Когда использовать:**
- **Синхронная** — для быстрых переходов (меню → меню)
- **Асинхронная** — для больших сцен (меню → игровой уровень)

---

### Проверка
- Метод `Load()` загружает сцену мгновенно
- Метод `LoadAsync()` загружает сцену постепенно (можно использовать позже)
- В консоли видны логи при загрузке сцен
