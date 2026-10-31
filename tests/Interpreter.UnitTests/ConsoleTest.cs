using System.Diagnostics;
using System.Text;

using Xunit;

namespace Lait.Interpreter.UnitTests;

public class ConsoleTest
{
    public static TheoryData<string, string, string, int> ProcessUsesStandardStreamsAndExitCodesData()
    {
        return new()
        {
            { "print(10 + 7 * (2 * 3 + 4) + 5 + -5);", "", "80", 0 },
            { "print(abs(readInt()));", "-7\n", "7", 0 },
            { "print(readInt()); print(1)", "7\n", "7", 1 },
            { "print(42); 1 / 0; print(99);", "", "42", 1 },
            { "print(2147483647 + 1);", "", "", 1 },
            { "print(readInt());", "wrong\n", "", 1 },
            { "print(readInt());", "", "", 1 },
            { "print(1); @", "", "1", 1 },
            { "", "", "", 0 },
            { "print(1)", "", "", 1 },
            { "print(1); func other(): void {}", "", "1", 1 },
            { "print(1); print(-(-2147483647 - 1));", "", "1", 1 },
            { "print(abs(-2147483647 - 1));", "", "", 1 },
            { "print((-2147483647 - 1) / -1);", "", "", 1 },
            { "print(readInt());", "2147483648\n", "", 1 },
            { "print(readInt());", "-2147483648\n", "-2147483648", 0 },
            { "print(readInt());", "42", "42", 0 },
            { new string('(', 100000) + "1" + new string(')', 100000) + ";", "", "", 1 },
        };
    }

    [Theory]
    [MemberData(nameof(ProcessUsesStandardStreamsAndExitCodesData))]
    public async Task ProcessUsesStandardStreamsAndExitCodes(
        string body, string input, string expectedOutput, int expectedExitCode)
    {
        string path = Path.Combine(Path.GetTempPath(), "Программа Lait " + Guid.NewGuid().ToString("N") + ".lait");
        try
        {
            await File.WriteAllTextAsync(path, $"func main(): void {{ {body} }}");
            (int exitCode, string output, string error) = await RunProcess(new[] { path }, input);

            Assert.Equal(expectedExitCode, exitCode);
            Assert.Equal(expectedOutput.Length == 0 ? string.Empty : expectedOutput + Environment.NewLine, output);
            if (expectedExitCode == 0)
            {
                Assert.Equal(string.Empty, error);
            }
            else
            {
                Assert.NotEmpty(error);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task MissingArgumentReportsError()
    {
        (int exitCode, string output, string error) = await RunProcess(Array.Empty<string>(), string.Empty);

        Assert.NotEqual(0, exitCode);
        Assert.Equal(string.Empty, output);
        Assert.NotEmpty(error);
    }

    [Fact]
    public async Task MissingFileReportsError()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".lait");
        (int exitCode, string output, string error) = await RunProcess(new[] { missingPath }, string.Empty);

        Assert.NotEqual(0, exitCode);
        Assert.Equal(string.Empty, output);
        Assert.NotEmpty(error);
        Assert.Contains("Не удалось прочитать исходный файл или выполнить ввод/вывод.", error);
    }

    [Fact]
    public async Task ExtraArgumentReportsError()
    {
        string[] arguments = ["first.lait", "second.lait"];

        (int exitCode, string output, string error) = await RunProcess(arguments, string.Empty);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, output);
        Assert.Contains("Использование:", error);
    }

    [Fact]
    public async Task DirectoryInsteadOfSourceReportsError()
    {
        string[] arguments = [Path.GetTempPath()];

        (int exitCode, string output, string error) = await RunProcess(arguments, string.Empty);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, output);
        Assert.Contains("Не удалось прочитать исходный файл или выполнить ввод/вывод.", error);
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunProcess(
        IEnumerable<string> arguments, string input)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
        };
        startInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new() { StartInfo = startInfo };
        process.Start();
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(15));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return (process.ExitCode, await outputTask, await errorTask);
    }
}