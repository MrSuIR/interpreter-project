using System.Diagnostics;

using Xunit;

namespace Lait.Interpreter.UnitTests;

public class ConsoleTest
{
    [Theory]
    [InlineData("print(10 + 7 * (2 * 3 + 4) + 5 + -5);", "", "80", 0)]
    [InlineData("print(abs(readInt()));", "-7\n", "7", 0)]
    [InlineData("print(readInt()); print(1)", "7\n", "7", 1)]
    [InlineData("print(42); 1 / 0; print(99);", "", "42", 1)]
    [InlineData("print(2147483647 + 1);", "", "", 1)]
    [InlineData("print(readInt());", "wrong\n", "", 1)]
    [InlineData("print(readInt());", "", "", 1)]
    [InlineData("print(1); @", "", "1", 1)]
    public async Task Process_uses_standard_streams_and_exit_codes(
        string body, string input, string expectedOutput, int expectedExitCode)
    {
        string path = Path.GetTempFileName();
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
    public async Task Missing_argument_reports_error()
    {
        (int exitCode, string output, string error) = await RunProcess(Array.Empty<string>(), string.Empty);

        Assert.NotEqual(0, exitCode);
        Assert.Equal(string.Empty, output);
        Assert.NotEmpty(error);
    }

    [Fact]
    public async Task Missing_file_reports_error()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".lait");
        (int exitCode, string output, string error) = await RunProcess(new[] { missingPath }, string.Empty);

        Assert.NotEqual(0, exitCode);
        Assert.Equal(string.Empty, output);
        Assert.NotEmpty(error);
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