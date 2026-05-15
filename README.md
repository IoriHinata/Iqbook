# IQBook Unity Framework

Рабочий каркас приложения по ТЗ:

- Формат `.iqbook` как ZIP-контейнер с `metadata.json`, `manifest.json`, `signature.bin`, `content.json`, `assets/...`.
- Верификация подписи RSA + проверка SHA-256 каждого файла из manifest.
- Рантайм-движок узлов/выборов (`StoryRunner`).
- Базовый bootstrap для Reader (`ReaderBootstrap`).
- Базовое окно экспорта для Editor (`Tools/IQBook/Exporter`).

## Структура

- `Assets/Iqbook/Runtime/Data` — модели данных формата.
- `Assets/Iqbook/Runtime/Crypto` — хэширование, подпись, PBKDF2/AES.
- `Assets/Iqbook/Runtime/IO` — упаковка/проверка `.iqbook`.
- `Assets/Iqbook/Runtime/Core` — исполнение графа истории.
- `Assets/Iqbook/Runtime/UI` — точка входа читалки.
- `Assets/Iqbook/Editor` — редакторный экспорт.

## Как использовать

1. Подготовьте папку исходников книги, где лежат `content.json` и `assets/...`.
2. В Unity откройте `Tools -> IQBook -> Exporter`.
3. Укажите source folder и output `.iqbook`, нажмите экспорт.
4. Для читалки добавьте `ReaderBootstrap` на сцену и укажите путь к `.iqbook`.

## Что дальше для полного продакшн-решения

- Отдельные Unity-проекты для Reader и Editor.
- Редактор графа (GraphView/UI Toolkit) + редактор карт.
- Авторский пароль и шифрование приватного ключа.
- UI-рендеринг нод, видео, интерактивных карт и сохранения прогресса.
