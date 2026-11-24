using System.Globalization;

using Lait.Ast.Declarations;
using Lait.Runtime;

using ValueType = Lait.Runtime.ValueType;

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
                "print", // print(value: int): void - выводит целое число в десятичной записи и завершает строку
                [new("value", ValueType.Int)],
                ValueType.Void,
                arguments =>
                {
                    environment.WriteLine(arguments[0].AsInt().ToString(CultureInfo.InvariantCulture));
                    return Value.Void;
                }),

            new(
                "printFloat", // printFloat(value: float): void - выводит число с шестью цифрами после точки
                [new("value", ValueType.Float)],
                ValueType.Void,
                arguments =>
                {
                    environment.WriteLine(arguments[0].AsFloat().ToString("F6", CultureInfo.InvariantCulture));
                    return Value.Void;
                }),

            new(
                "readInt", // readInt(): int - читает одну строку ввода и возвращает целое число
                [],
                ValueType.Int,
                _ => new Value(ReadInteger(environment))),

            new(
                "readFloat", // readFloat(): float - читает одну строку ввода и возвращает число с плавающей точкой
                [],
                ValueType.Float,
                _ => new Value(ReadFloat(environment))),

            new(
                "abs", // abs(value: int): int - возвращает модуль числа
                [new("value", ValueType.Int)],
                ValueType.Int,
                arguments => new Value(Math.Abs(arguments[0].AsInt()))),

            new(
                "min", // min(left: int, right: int): int - возвращает меньшее из двух чисел
                [new("left", ValueType.Int), new("right", ValueType.Int)],
                ValueType.Int,
                arguments => new Value(Math.Min(arguments[0].AsInt(), arguments[1].AsInt()))),

            new(
                "max", // max(left: int, right: int): int - возвращает большее из двух чисел
                [new("left", ValueType.Int), new("right", ValueType.Int)],
                ValueType.Int,
                arguments => new Value(Math.Max(arguments[0].AsInt(), arguments[1].AsInt()))),

            new(
                "toFloat", // toFloat(value: int): float - возвращает то же число в типе float
                [new("value", ValueType.Int)],
                ValueType.Float,
                arguments => new Value((double)arguments[0].AsInt())),

            new(
                "toInt", // toInt(value: float): int - отбрасывает дробную часть числа в сторону нуля
                [new("value", ValueType.Float)],
                ValueType.Int,
                arguments => new Value(TruncateToInteger(arguments[0].AsFloat()))),
        ];

        Functions = functions.ToDictionary(function => function.Name);
    }

    /// <summary>
    /// Встроенные функции языка по именам.
    /// </summary>
    public IReadOnlyDictionary<string, BuiltinFunction> Functions { get; }

    private static int ReadInteger(IEnvironment environment)
    {
        string text = ReadNumberLine(environment, "readInt");
        int start = text.Length > 0 && text[0] is '+' or '-' ? 1 : 0;
        if (start == text.Length)
        {
            throw new InvalidOperationException("readInt(): ожидалось целое число.");
        }

        if (SkipDigits(text, start) != text.Length)
        {
            throw new InvalidOperationException("readInt(): неверный формат целого числа.");
        }

        if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidOperationException("readInt(): число вне диапазона int.");
        }

        return value;
    }

    private static double ReadFloat(IEnvironment environment)
    {
        string text = ReadNumberLine(environment, "readFloat");
        int start = text.Length > 0 && text[0] is '+' or '-' ? 1 : 0;
        if (start == text.Length)
        {
            throw new InvalidOperationException("readFloat(): ожидалось число.");
        }

        // Допустимы цифры целой части и необязательная дробная часть: точка и хотя бы одна цифра.
        int end = SkipDigits(text, start);
        bool hasIntegerPart = end > start;
        if (hasIntegerPart && end < text.Length && text[end] == '.')
        {
            int fractionStart = end + 1;
            end = SkipDigits(text, fractionStart);
            hasIntegerPart = end > fractionStart;
        }

        if (!hasIntegerPart || end != text.Length)
        {
            throw new InvalidOperationException("readFloat(): неверный формат числа.");
        }

        double value = double.Parse(
            text,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture);
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException("readFloat(): число вне диапазона float.");
        }

        return value;
    }

    /// <summary>
    /// Читает строку ввода и убирает начальные и конечные пробелы и табуляции.
    /// </summary>
    private static string ReadNumberLine(IEnvironment environment, string functionName)
    {
        string line = environment.ReadLine()
            ?? throw new InvalidOperationException($"{functionName}(): достигнут конец ввода.");
        return line.Trim(' ', '\t');
    }

    /// <summary>
    /// Возвращает позицию первого символа, не являющегося десятичной цифрой, начиная с указанной.
    /// </summary>
    private static int SkipDigits(string text, int start)
    {
        int index = start;
        while (index < text.Length && char.IsAsciiDigit(text[index]))
        {
            index++;
        }

        return index;
    }

    private static int TruncateToInteger(double value)
    {
        // Отбрасывание дробной части даёт int, только если число строго между этими границами.
        if (value <= -2147483649.0 || value >= 2147483648.0)
        {
            throw new InvalidOperationException("toInt(): число вне диапазона int.");
        }

        return (int)value;
    }
}