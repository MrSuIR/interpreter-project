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

            // Factorial: рекурсия и ветвление без else.
            {
                """
                func factorial(n: int): int {
                    if (n <= 1) {
                        return 1;
                    }

                    return n * factorial(n - 1);
                }

                func main(): void {
                    print(factorial(readInt()));
                    print(factorial(0));
                }
                """,
                "5\n",
                "120\n1\n"
            },

            // GCD: алгоритм Евклида с циклом while и остатком от деления.
            {
                """
                func gcd(a: int, b: int): int {
                    while (b != 0) {
                        int rest = a % b;
                        a = b;
                        b = rest;
                    }

                    return a;
                }

                func main(): void {
                    print(gcd(readInt(), readInt()));
                    print(gcd(17, 5));
                }
                """,
                "48\n36\n",
                "12\n1\n"
            },

            // IsPrime: цикл for, break, continue и функция с результатом bool.
            {
                """
                func isPrime(n: int): bool {
                    if (n < 2) {
                        return false;
                    }

                    bool prime = true;
                    for (int divisor = 2; divisor * divisor <= n; divisor = divisor + 1) {
                        if (n % divisor == 0) {
                            prime = false;
                            break;
                        }
                    }

                    return prime;
                }

                func main(): void {
                    int limit = readInt();
                    for (int n = 1; n <= limit; n = n + 1) {
                        if (!isPrime(n)) {
                            continue;
                        }

                        print(n);
                    }
                }
                """,
                "20\n",
                "2\n3\n5\n7\n11\n13\n17\n19\n"
            },

            // IsLeapYear: логические операции с учётом их приоритета.
            {
                """
                func isLeapYear(year: int): bool {
                    return year % 4 == 0 && year % 100 != 0 || year % 400 == 0;
                }

                func main(): void {
                    int count = readInt();
                    while (count > 0) {
                        if (isLeapYear(readInt())) {
                            print(1);
                        } else {
                            print(0);
                        }

                        count = count - 1;
                    }
                }
                """,
                "4\n2024\n1900\n2000\n2023\n",
                "1\n0\n1\n0\n"
            },

            // Sqrt: квадратный корень методом Ньютона в типе float.
            {
                """
                func squareRoot(value: float): float {
                    float x = value;
                    if (x < 1.0) {
                        x = 1.0;
                    }

                    for (int step = 0; step < 30; step = step + 1) {
                        x = (x + value / x) / 2.0;
                    }

                    return x;
                }

                func main(): void {
                    printFloat(squareRoot(readFloat()));
                    printFloat(squareRoot(2.0));
                }
                """,
                "6.25\n",
                "2.500000\n1.414214\n"
            },

            // Average: явные преобразования между int и float.
            {
                """
                func main(): void {
                    int count = readInt();
                    int sum = 0;
                    for (int i = 0; i < count; i = i + 1) {
                        sum = sum + readInt();
                    }

                    printFloat(toFloat(sum) / toFloat(count));
                    print(toInt(-7.9));
                    print(toInt(7.9));
                }
                """,
                "3\n1\n2\n4\n",
                "2.333333\n-7\n7\n"
            },

            // Цепочка else if; каждый путь выполнения функции заканчивается return.
            {
                """
                func sign(x: int): int {
                    if (x > 0) {
                        return 1;
                    } else if (x < 0) {
                        return -1;
                    } else {
                        return 0;
                    }
                }

                func main(): void {
                    print(sign(5));
                    print(sign(-3));
                    print(sign(0));
                }
                """,
                "",
                "1\n-1\n0\n"
            },

            // Логические операции вычисляются по короткой схеме.
            {
                """
                func touch(result: bool): bool {
                    print(1);
                    return result;
                }

                func main(): void {
                    if (false && touch(true)) {
                        print(2);
                    }

                    if (true || touch(false)) {
                        print(3);
                    }

                    if (true && touch(false)) {
                        print(4);
                    } else {
                        print(5);
                    }
                }
                """,
                "",
                "3\n1\n5\n"
            },

            // break и continue действуют на ближайший цикл.
            {
                """
                func main(): void {
                    for (int i = 1; i <= 3; i = i + 1) {
                        int j = 0;
                        while (true) {
                            j = j + 1;
                            if (j == 2) {
                                continue;
                            }

                            if (j > 3) {
                                break;
                            }

                            print(i * 10 + j);
                        }
                    }
                }
                """,
                "",
                "11\n13\n21\n23\n31\n33\n"
            },

            // Сравнения чисел и bool, отрицание, остаток от деления отрицательного числа.
            {
                """
                func check(condition: bool): void {
                    if (condition) {
                        print(1);
                    } else {
                        print(0);
                    }
                }

                func main(): void {
                    check(1.5 < 2.25);
                    check(2.0 >= 2.0);
                    check(true == !false);
                    check(1 != 1);
                    check(-0.5 > -1.0);
                    check(7 % 3 == 1 && -7 % 3 == -1);
                    bool flag = false;
                    flag = !flag;
                    check(flag);
                }
                """,
                "",
                "1\n1\n1\n0\n1\n1\n1\n"
            },

            // Арифметика float и вывод с шестью знаками после точки.
            {
                """
                func main(): void {
                    printFloat(1.5 + 2.25 * 2.0 - -0.5);
                    printFloat(7.0 / 2.0);
                    printFloat(0.1 + 0.2);
                    printFloat(readFloat());
                }
                """,
                " -12.5 \n",
                "6.500000\n3.500000\n0.300000\n-12.500000\n"
            },

            // return внутри цикла завершает функцию.
            {
                """
                func firstDivisor(n: int): int {
                    for (int d = 2; d < n; d = d + 1) {
                        if (n % d == 0) {
                            return d;
                        }
                    }

                    return n;
                }

                func main(): void {
                    print(firstDivisor(91));
                    print(firstDivisor(13));
                }
                """,
                "",
                "7\n13\n"
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

            // Типы: неявных преобразований нет, операции требуют подходящих операндов.
            { "func main(): void { print(42); int x = 1.5; }", "", "" },
            { "func main(): void { print(42); float x = 1; }", "", "" },
            { "func main(): void { print(42); bool x = 1; }", "", "" },
            { "func main(): void { print(42); print(1.5); }", "", "" },
            { "func main(): void { print(42); printFloat(1); }", "", "" },
            { "func main(): void { print(42); 1 + 1.5; }", "", "" },
            { "func main(): void { print(42); 1.5 % 2.0; }", "", "" },
            { "func main(): void { print(42); true + 1; }", "", "" },
            { "func main(): void { print(42); -true; }", "", "" },
            { "func main(): void { print(42); !1; }", "", "" },
            { "func main(): void { print(42); 1 && true; }", "", "" },
            { "func main(): void { print(42); true < false; }", "", "" },
            { "func main(): void { print(42); 1 == 1.0; }", "", "" },
            { "func main(): void { print(42); int x = 0; x = true; }", "", "" },
            { "func f(x: float): void {} func main(): void { print(42); f(1); }", "", "" },
            { "func f(): bool { return 1; } func main(): void { print(42); f(); }", "", "" },
            { "func main(): void { print(42); toInt(1); }", "", "" },
            { "func main(): void { print(42); toFloat(1.5); }", "", "" },

            // Условия ветвлений и циклов имеют тип bool.
            { "func main(): void { print(42); if (1) {} }", "", "" },
            { "func main(): void { print(42); while (0) {} }", "", "" },
            { "func main(): void { print(42); for (int i = 0; i; i = i + 1) {} }", "", "" },

            // Синтаксис: блоки обязательны, условие в скобках, литерал float с цифрами по обе стороны точки.
            { "func main(): void { print(42); if (true) print(1); }", "", "" },
            { "func main(): void { print(42); while true {} }", "", "" },
            { "func main(): void { print(42); float x = 1.; }", "", "" },
            { "func main(): void { print(42); float x = .5; }", "", "" },
            { "func main(): void { print(42); for (int i = 0; i < 3) {} }", "", "" },

            // break и continue допускаются только внутри цикла текущей функции.
            { "func main(): void { print(42); break; }", "", "" },
            { "func main(): void { print(42); if (true) { continue; } }", "", "" },
            { "func f(): void { break; } func main(): void { while (true) { f(); } }", "", "" },

            // Переменная цикла for видна только в цикле.
            { "func main(): void { for (int i = 0; i < 3; i = i + 1) {} print(i); }", "", "" },

            // Каждый путь выполнения функции с результатом заканчивается return.
            { "func f(x: int): int { if (x > 0) { return 1; } } func main(): void { print(f(1)); }", "", "" },
            { "func f(x: int): int { while (true) { return 1; } } func main(): void { print(f(1)); }", "", "" },
            { "func f(): float { return; } func main(): void { printFloat(f()); }", "", "" },
        };
    }

    public static TheoryData<string, string, string, string> RuntimeFailingProgramsData()
    {
        return new()
        {
            // Ошибка выполнения внутри пользовательской функции сохраняет предыдущий вывод.
            { "func divide(a: int, b: int): int { return a / b; } func main(): void { print(1); print(divide(1, 0)); print(2); }", "", "Деление на ноль.", "1\n" },
            { "func twice(x: int): int { return x * 2; } func main(): void { int x = readInt(); print(x); print(twice(x)); }", "2000000000\n", "Результат операции выходит за диапазон int.", "2000000000\n" },

            // Ошибка выполнения внутри цикла прекращает программу.
            { "func main(): void { float x = 2.0; print(1); while (true) { x = x * x; } }", "", "Результат операции выходит за диапазон float.", "1\n" },
            { "func main(): void { for (int i = 3; i >= 0; i = i - 1) { print(12 / i); } }", "", "Деление на ноль.", "4\n6\n12\n" },
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
            { "1 % 0", "", "Деление на ноль." },
            { "(-2147483647 - 1) % -1", "", "Результат операции выходит за диапазон int." },
            { "1.0 / 0.0", "", "Деление на ноль." },
            { "toInt(2147483648.0)", "", "toInt(): число вне диапазона int." },
            { "toInt(-2147483649.0)", "", "toInt(): число вне диапазона int." },
            { "readFloat()", "", "readFloat(): достигнут конец ввода." },
            { "readFloat()", " \n", "readFloat(): ожидалось число." },
            { "readFloat()", "1.5.2\n", "readFloat(): неверный формат числа." },
            { "readFloat()", "1e5\n", "readFloat(): неверный формат числа." },
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