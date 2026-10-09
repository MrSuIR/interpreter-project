using System.Text;

using Lait.Lexemes;
using Lait.Parsing;

namespace Lait.Interpreter;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.InputEncoding = new UTF8Encoding(false);
        Console.OutputEncoding = new UTF8Encoding(false);
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
            or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            string message = exception is FormatException or InvalidOperationException
                ? exception.Message
                : "Не удалось прочитать исходный файл или выполнить ввод/вывод.";
            Console.Error.WriteLine($"Ошибка: {message}");
            return 1;
        }
    }
}