using System.Globalization;

namespace Lait.Runtime;

/// <summary>
/// Последовательно выполняет инструкции, используя переданные потоки ввода и вывода.
/// </summary>
public class VirtualMachine
{
    public void Execute(IEnumerable<Instruction> instructions, TextReader input, TextWriter output)
    {
        Stack<int> stack = new();
        int instructionNumber = 0;
        foreach (Instruction instruction in instructions)
        {
            instructionNumber++;
            try
            {
                ExecuteInstruction(instruction, stack, input, output);
            }
            catch (OverflowException exception)
            {
                throw new InvalidOperationException(
                    $"Инструкция {instructionNumber}: переполнение int.", exception);
            }
            catch (DivideByZeroException exception)
            {
                throw new InvalidOperationException(
                    $"Инструкция {instructionNumber}: деление на ноль.", exception);
            }
        }
    }

    private static void ExecuteInstruction(
        Instruction instruction, Stack<int> stack, TextReader input, TextWriter output)
    {
        switch (instruction.Operation)
        {
            case Operation.Push:
                stack.Push(instruction.Operand);
                break;
            case Operation.Read:
                stack.Push(ReadInteger(input));
                break;
            case Operation.Print:
                output.WriteLine(stack.Pop().ToString(CultureInfo.InvariantCulture));
                output.Flush();
                break;
            case Operation.Discard:
                stack.Pop();
                break;
            case Operation.Negate:
                stack.Push(checked(-stack.Pop()));
                break;
            case Operation.Absolute:
                stack.Push(Math.Abs(stack.Pop()));
                break;
            default:
                int right = stack.Pop();
                int left = stack.Pop();
                int result = instruction.Operation switch
                {
                    Operation.Add => checked(left + right),
                    Operation.Subtract => checked(left - right),
                    Operation.Multiply => checked(left * right),
                    Operation.Divide => checked(left / right),
                    Operation.Minimum => Math.Min(left, right),
                    Operation.Maximum => Math.Max(left, right),
                    _ => throw new InvalidOperationException("Неизвестная инструкция VM."),
                };
                stack.Push(result);
                break;
        }
    }

    private static int ReadInteger(TextReader input)
    {
        string? line = input.ReadLine();
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