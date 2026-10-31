using Lait.Interpreter;
using Lait.TestDoubles;

using Xunit;

namespace Interpreter.UnitTests;

public class AcceptanceTest
{
    public static TheoryData<string, string, string> CompleteProgramsData()
    {
        return new()
        {
            // Пустой блок и комментарии перед программой, в теле и после неё.
            { "/* начало */ func main(): void { /* тело */ } // конец", "", "" },

            // Приоритет операций, скобки и унарный минус.
            { "func main(): void { print(10 + 7 * (2 * 3 + 4) + 5 + -5); }", "", "80\n" },

            // Все бинарные операции и их левая ассоциативность.
            { "func main(): void { print(10 - 3 - 2); print(24 / 3 / 2); print(20 / 2 * 3 + 4 - 1); }", "", "5\n4\n33\n" },

            // Рекурсивный унарный минус и деление с усечением к нулю.
            { "func main(): void { print(---5); print(-7 / 2); print(7 / -2); }", "", "-5\n-3\n-3\n" },

            // Литералы и границы диапазона вычисляемых значений.
            { "func main(): void { print(0); print(2147483647); print(-2147483647 - 1); }", "", "0\n2147483647\n-2147483648\n" },

            // Вызовы с одним и двумя аргументами, вложенные функции.
            { "func main(): void { print(abs(-8)); print(min(9, 5)); print(max(9, 5)); print(max(abs(-8), min(9, 5))); }", "", "8\n5\n9\n8\n" },

            // Вызов без аргументов, формат ввода и последняя строка без переноса.
            { "func main(): void { print(readInt()); print(readInt()); print(readInt()); }", " \t+007\t \n-2147483648\n2147483647", "7\n-2147483648\n2147483647\n" },

            // Операнды и вложенные аргументы читаются слева направо по одному разу.
            { "func main(): void { print(max(readInt() - readInt(), readInt())); print(readInt()); }", "10\n3\n2\n99\n", "7\n99\n" },

            // Инструкции-выражения вычисляются, но сами ничего не выводят.
            { "func main(): void { 1 + 2; abs(-3); min(4, 5); max(6, 7); readInt(); print(readInt() * readInt()); }", "99\n6\n7\n", "42\n" },

            // Объявление переменной с инициализацией и чтение переменной в выражении.
            { "func main(): void { int x = 7; int y = x * 2; print(x + 5); print(y - x); }", "", "12\n7\n" },

            // Присваивание переменной, в том числе с использованием её прежнего значения.
            { "func main(): void { int x = 1; x = x + 2; print(x); x = x * 10; print(x); }", "", "3\n30\n" },

            // Вложенное присваивание правоассоциативно; значение присваивания - присвоенное число.
            { "func main(): void { int a = 0; int b = 0; int c = 3; a = b = c + 1; print(a); print(b); print(a = 7); print(a); }", "", "4\n4\n7\n7\n" },

            // Переменная получает введённое число.
            { "func main(): void { int n = readInt(); print(n * n); }", "-6\n", "36\n" },

            // Функция с параметрами и результатом.
            { "func sum(a: int, b: int): int { return a + b; } func main(): void { print(sum(2, 3)); }", "", "5\n" },

            // Функция без параметров с результатом.
            { "func answer(): int { return 42; } func main(): void { print(answer() + 1); }", "", "43\n" },

            // Функция с параметром без результата вызывается как инструкция.
            { "func show(x: int): void { print(x * 2); } func main(): void { show(21); show(-1); }", "", "42\n-2\n" },

            // return завершает функцию досрочно, в том числе функцию без результата.
            { "func stop(): void { print(1); return; print(2); } func first(): int { return 1; print(5); return 2; } func main(): void { stop(); print(first()); }", "", "1\n1\n" },

            // Функции можно объявлять в любом порядке и вызывать друг из друга.
            { "func main(): void { print(sumOfSquares(3, 4)); } func sumOfSquares(a: int, b: int): int { return square(a) + square(b); } func square(x: int): int { return x * x; }", "", "25\n" },

            // Аргументы пользовательской функции вычисляются слева направо по одному разу.
            { "func diff(a: int, b: int): int { return a - b; } func main(): void { print(diff(readInt(), readInt())); print(readInt()); }", "10\n3\n99\n", "7\n99\n" },

            // Параметры передаются по значению.
            { "func inc(x: int): int { x = x + 1; return x; } func main(): void { int x = 5; print(inc(x)); print(x); }", "", "6\n5\n" },

            // Переменные разных функций независимы.
            { "func f(): int { int x = 10; return x; } func main(): void { int x = 1; print(f()); print(x); }", "", "10\n1\n" },

            // Вложенный блок видит внешние переменные и может их изменять.
            { "func main(): void { int x = 1; { x = 5; int y = x + 1; print(y); } print(x); }", "", "6\n5\n" },

            // Во вложенном блоке переменная может перекрыть внешнюю; инициализатор видит внешнюю.
            { "func main(): void { int x = 1; { int x = x + 1; print(x); } print(x); }", "", "2\n1\n" },

            // После выхода из блока его имя можно объявить снова.
            { "func main(): void { { int x = 1; print(x); } int x = 2; print(x); }", "", "1\n2\n" },

            // Имена функций и переменных находятся в разных пространствах имён.
            { "func main(): void { int max = max(1, 2); int print = 3; print(max + print); }", "", "5\n" },

            // RectSquare: площадь прямоугольника по координатам двух противоположных вершин.
            {
                """
                func width(x1: int, x2: int): int {
                    return abs(x2 - x1);
                }

                func height(y1: int, y2: int): int {
                    return abs(y2 - y1);
                }

                func area(x1: int, y1: int, x2: int, y2: int): int {
                    return width(x1, x2) * height(y1, y2);
                }

                func main(): void {
                    int x1 = readInt();
                    int y1 = readInt();
                    int x2 = readInt();
                    int y2 = readInt();
                    print(area(x1, y1, x2, y2));
                }
                """,
                "1\n2\n4\n-2\n",
                "12\n"
            },

            // CircleSquare: целая часть площади круга, число пи приближено дробью 355/113.
            {
                """
                func circleArea(radius: int): int {
                    return radius * radius * 355 / 113;
                }

                func main(): void {
                    int radius = readInt();
                    print(circleArea(radius));
                    print(circleArea(1));
                }
                """,
                "10\n",
                "314\n3\n"
            },
        };
    }

    public static TheoryData<string, string, string> InvalidProgramsData()
    {
        return new()
        {
            { "func other(): void {}", "", "" },
            { "func main(): void {} func main(): void {}", "", "" },
            { "func main(): void { print(1) }", "", "" },

            // Ошибка разбора или проверки обнаруживается до выполнения первой инструкции.
            { "func main(): void { print(42); print(readInt()) readInt(); }", "7\n9\n", "" },
            { "func main(): void { print(42); min(1); print(99); }", "", "" },
            { "func main(): void { print(42); 1 + print(1); }", "", "" },
            { "func main(): void { print(42); @ print(99); }", "", "" },

            // Переменные: объявление обязательно до использования, инициализация обязательна.
            { "func main(): void { print(42); print(x); }", "", "" },
            { "func main(): void { print(x); int x = 1; }", "", "" },
            { "func main(): void { int x = x + 1; }", "", "" },
            { "func main(): void { int x; }", "", "" },
            { "func main(): void { { int x = 1; } print(x); }", "", "" },

            // Повтор имени в одной области видимости.
            { "func main(): void { int x = 1; int x = 2; }", "", "" },
            { "func f(a: int): void { int a = 1; } func main(): void { f(1); }", "", "" },
            { "func f(a: int, a: int): void {} func main(): void { f(1, 2); }", "", "" },

            // Присваивать можно только переменной.
            { "func main(): void { 1 = 2; }", "", "" },
            { "func main(): void { int x = 1; abs(x) = 2; }", "", "" },

            // Функции: имена, число аргументов, результат и return.
            { "func main(): void { print(42); unknown(); }", "", "" },
            { "func f(a: int): int { return a; } func main(): void { print(f()); }", "", "" },
            { "func f(): void {} func main(): void { print(f()); }", "", "" },
            { "func f(): int { print(1); } func main(): void { print(f()); }", "", "" },
            { "func f(): int { return; } func main(): void { print(f()); }", "", "" },
            { "func main(): void { return 1; }", "", "" },
            { "func f(): void {} func f(): void {} func main(): void { f(); }", "", "" },
            { "func abs(x: int): int { return x; } func main(): void { print(abs(1)); }", "", "" },
            { "func main(): void { print(42); main(); }", "", "" },
            { "func main(): int { return 0; }", "", "" },
            { "func main(x: int): void {}", "", "" },
            { "func main(): void { int x = 1; x(); }", "", "" },
        };
    }

    public static TheoryData<string, string, string, string> RuntimeFailingProgramsData()
    {
        return new()
        {
            // Ошибка выполнения внутри пользовательской функции сохраняет предыдущий вывод.
            { "func divide(a: int, b: int): int { return a / b; } func main(): void { print(1); print(divide(1, 0)); print(2); }", "", "Деление на ноль.", "1\n" },
            { "func twice(x: int): int { return x * 2; } func main(): void { int x = readInt(); print(x); print(twice(x)); }", "2000000000\n", "Результат операции выходит за диапазон int.", "2000000000\n" },
        };
    }

    public static TheoryData<string, string, string> RuntimeFailuresData()
    {
        return new()
        {
            { "1 / 0", "", "Деление на ноль." },
            { "2147483647 + 1", "", "Результат операции выходит за диапазон int." },
            { "abs(-2147483647 - 1)", "", "Результат операции выходит за диапазон int." },
            { "readInt()", "", "readInt(): достигнут конец ввода." },
            { "readInt()", "\n", "readInt(): ожидалось целое число." },
            { "readInt()", "1 2\n", "readInt(): неверный формат целого числа." },
            { "readInt()", "2147483648\n", "readInt(): число вне диапазона int." },
        };
    }

    [Theory]
    [MemberData(nameof(CompleteProgramsData))]
    public void CompleteProgramProducesExpectedOutput(string source, string inputText, string expectedOutput)
    {
        FakeEnvironment environment = new(inputText);
        LaitInterpreter interpreter = new(environment);

        interpreter.Execute(source);

        Assert.Equal(expectedOutput, environment.Output);
    }

    [Theory]
    [MemberData(nameof(InvalidProgramsData))]
    public void InvalidProgramStopsExecution(string source, string inputText, string expectedOutput)
    {
        FakeEnvironment environment = new(inputText);
        LaitInterpreter interpreter = new(environment);

        Exception? error = Record.Exception(() => interpreter.Execute(source));

        Assert.IsType<FormatException>(error);
        Assert.Equal(expectedOutput, environment.Output);
    }

    [Theory]
    [MemberData(nameof(RuntimeFailuresData))]
    public void RuntimeFailurePreservesOutputAndStopsExecution(string expression, string inputText, string expectedMessage)
    {
        FakeEnvironment environment = new(inputText);
        LaitInterpreter interpreter = new(environment);
        string source = $"func main(): void {{ print(42); {expression}; print(99); }}";

        Exception? error = Record.Exception(() => interpreter.Execute(source));

        InvalidOperationException runtimeError = Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(expectedMessage, runtimeError.Message);
        Assert.Equal("42\n", environment.Output);
    }

    [Theory]
    [MemberData(nameof(RuntimeFailingProgramsData))]
    public void RuntimeFailureInProgramPreservesOutput(
        string source, string inputText, string expectedMessage, string expectedOutput)
    {
        FakeEnvironment environment = new(inputText);
        LaitInterpreter interpreter = new(environment);

        Exception? error = Record.Exception(() => interpreter.Execute(source));

        InvalidOperationException runtimeError = Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(expectedMessage, runtimeError.Message);
        Assert.Equal(expectedOutput, environment.Output);
    }
}