using Lait.Lexemes;
using Lait.Parsing;
using Lait.Runtime;

namespace Lait.Interpreter;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Использование: Interpreter <исходный файл.lait>");
            return 1;
        }

        try
        {
            Parser parser = new(Lexer.FromFile(args[0]));
            IReadOnlyList<Instruction> instructions = parser.ParseProgram();
            VirtualMachine machine = new();
            machine.Execute(instructions, Console.In, Console.Out);
            return 0;
        }
        catch (Exception exception) when (exception is FormatException
            or InvalidOperationException or IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            Console.Error.WriteLine($"Ошибка: {exception.Message}");
            return 1;
        }
    }
}