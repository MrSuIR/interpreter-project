using Lait.Lexemes;
using Lait.Parsing;

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
            Parser parser = new(Lexer.FromFile(args[0]), Console.In, Console.Out);
            parser.ParseProgram();
            return 0;
        }
        catch (Exception exception) when (exception is FormatException
            or InvalidOperationException or IOException or UnauthorizedAccessException
            or ArgumentException or NotSupportedException or System.Security.SecurityException
            or OverflowException or DivideByZeroException)
        {
            Console.Error.WriteLine($"Ошибка: {exception.Message}");
            return 1;
        }
    }
}