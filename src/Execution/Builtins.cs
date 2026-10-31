using System.Globalization;

namespace Lait.Execution;

/// <summary>
/// Объект, предоставляющий доступ к встроенным функциям языка.
/// </summary>
public class Builtins
{
    public Builtins(IEnvironment environment)
    {
        List<BuiltinFunction> functions =
        [
            new(
                "print", // print(value: int): void - выводит число в десятичной записи и завершает строку
                ["value"],
                returnsValue: false,
                arguments =>
                {
                    environment.WriteLine(arguments[0].ToString(CultureInfo.InvariantCulture));
                    return 0;
                }),

            new(
                "readInt", // readInt(): int - читает одну строку ввода и возвращает целое число
                [],
                returnsValue: true,
                _ => ReadInteger(environment)),

            new(
                "abs", // abs(value: int): int - возвращает модуль числа
                ["value"],
                returnsValue: true,
                arguments => Math.Abs(arguments[0])),

            new(
                "min", // min(left: int, right: int): int - возвращает меньшее из двух чисел
                ["left", "right"],
                returnsValue: true,
                arguments => Math.Min(arguments[0], arguments[1])),

            new(
                "max", // max(left: int, right: int): int - возвращает большее из двух чисел
                ["left", "right"],
                returnsValue: true,
                arguments => Math.Max(arguments[0], arguments[1])),
        ];

        Functions = functions.ToDictionary(function => function.Name);
    }

    /// <summary>
    /// Встроенные функции языка по именам.
    /// </summary>
    public IReadOnlyDictionary<string, BuiltinFunction> Functions { get; }

    private static int ReadInteger(IEnvironment environment)
    {
        string? line = environment.ReadLine();
        if (line is null)
        {
            throw new InvalidOperationException("readInt(): достигнут конец ввода.");
        }

        string text = line.Trim(' ', '\t');
        int start = text.Length > 0 && text[0] is '+' or '-' ? 1 : 0;
        if (start == text.Length)
        {
            throw new InvalidOperationException("readInt(): ожидалось целое число.");
        }

        for (int index = start; index < text.Length; index++)
        {
            if (!char.IsAsciiDigit(text[index]))
            {
                throw new InvalidOperationException("readInt(): неверный формат целого числа.");
            }
        }

        if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidOperationException("readInt(): число вне диапазона int.");
        }

        return value;
    }
}