# Исходное состояние первого этапа

Дата: 2026-10-25. Unity 6000.3.19f1 (7689f4515d75), CLI 1.0.0-beta.14, Pipeline 0.9.1-exp.1.

**Уточнение:** Pipeline `build run --target WebGL` использует `BuildPlayerOptions` с subtarget=0 и переключает активный профиль с DXT на Generic. Поэтому сохранённые здесь 88 838 094 байта и браузерный замер — Generic, а не исходный DXT-профиль. Не использовать их для вывода о размере исходного DXT release. Сборки через `PublishingBuild.Queue` используют `BuildPlayerWithProfileOptions` и сохраняют subtarget профиля.

Baseline запущен **до установки Bridge и изменения runtime-кода**:

```powershell
unity command build run --target WebGL --outputPath Builds/PublishingBaseline --confirm true --caller plugin --skill unity-cli --project-path D:/GIT/boomwichi_GJ --format json
```

Build ID: `build_489d32714245`. Release: development=false, debugging=false, profiler=false, IL2CPP OptimizeSize. Включены сцены Bootstrap, MainMenu, Levels/GameScene.

На старте HEAD был `8bc98f9e2218e4c44555475cfdfdc5c11be32acd`. Уже существовавшие изменения шрифта, пакетов и документов сохранены в `pre-existing.patch`; в процессе работы владелец зафиксировал их коммитом `5eb18f0` (Create publishing plans and backlog). Эти изменения не откатывать. Build pipeline также меняет PerformanceTestRunInfo.json; это служебный файл test-framework, не функциональная часть интеграции.

Восстановимые копии до интеграции:

- ProjectSettings.asset.snapshot
- EditorBuildSettings.asset.snapshot
- Web-Desktop-Release.asset.snapshot — build profile с собственными PlayerSettings.
- manifest.json.snapshot и packages-lock.json.snapshot

Копии предназначены для сравнения и точечного восстановления. Не копировать их поверх более поздних изменений без проверки diff.

Размеры и результат браузерной проверки фиксируются после завершения сборки. Наличие запущенной сборки не означает прохождение приёмки.

Generic-сборка успешно завершилась: 0 ошибок, 4 предупреждения, 2 003 920 мс. Файлы: 88 838 094 байта; ZIP: 88 644 416 байт. `.data` — 79 306 668 байт (после распаковки 97 812 254); `.wasm` — 9 385 532 (28 479 352); framework — 78 142 (331 283). Меню визуально открылось; локальный маркер готовности — 6 583,7 мс. Resource Timing — 88 824 000 байт без HTML navigation. Подробности: file-sizes.json и browser-measurement.json.

## Методика браузерного замера

```powershell
node Tools/Publishing/serve.cjs Builds/PublishingBaseline 8080
```

Открыть http://127.0.0.1:8080/ в новой сессии браузера. Сервер привязан только к loopback, отдаёт Cache-Control: no-store и не изменяет файлы билда. Добавляет DOM-панель с Resource Timing и временем второго animation frame после сообщения о переходе в MainMenu. Готовность меню дополнительно подтвердить визуально. Время на loopback характеризует локальную загрузку и запуск, а не скорость внешнего хостинга или мобильной сети. Unity IndexedDB cache может влиять на повторный запуск; холодный замер требует нового origin/чистого browser storage.

Различать: сумму файлов билда, размер ZIP, сетевой transferSize и decodedBodySize. Для cross-origin SDK без Timing-Allow-Origin браузер может вернуть нули; такой ресурс нельзя считать бесплатным. Цель 30 000 000 байт не подтверждается одним размером ZIP.
