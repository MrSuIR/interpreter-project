# Интерпретатор языка Lait

Учебный интерпретатор на C# по дисциплине "Теория автоматов и формальных языков".
Описание языка: [спецификация](docs/specification.md).

## Запуск

Требуется .NET SDK 10. Команды выполняются из корня проекта:

```powershell
dotnet build Lait.slnx
dotnet run --project src/Interpreter --no-build -- examples/arithmetic.lait
```

Пример выводит `80`. Путь к исходному файлу передаётся единственным аргументом.

## Проверка

```powershell
dotnet test Lait.slnx
```

Перечень проверок: [перечень тестов](tests/Interpreter.UnitTests/TESTLIST.md).

## Авторы

- Тимур Асланов
- Алексей Васильев
