using Lait.Lexemes;

using Xunit;

namespace Lait.Lexemes.UnitTests;

public class TextScannerTest
{
    [Theory]
    [MemberData(nameof(GetPeekData))]
    public void Peek_returns_character_at_offset(string input, int offset, char expected)
    {
        TextScanner scanner = new(input);

        char actual = scanner.Peek(offset);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(GetRepeatedPeekData))]
    public void Peek_does_not_change_position(string input, char expected)
    {
        TextScanner scanner = new(input);

        char first = scanner.Peek();
        char second = scanner.Peek();

        Assert.Equal(expected, first);
        Assert.Equal(expected, second);
    }

    [Theory]
    [MemberData(nameof(GetAdvanceData))]
    public void Advance_moves_position_forward(string input, int advanceCount, char expected)
    {
        TextScanner scanner = new(input);

        for (int i = 0; i < advanceCount; i++)
        {
            scanner.Advance();
        }

        char actual = scanner.Peek();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(GetIsEndData))]
    public void Is_end_reports_whether_input_is_finished(string input, int advanceCount, bool expected)
    {
        TextScanner scanner = new(input);

        for (int i = 0; i < advanceCount; i++)
        {
            scanner.Advance();
        }

        bool actual = scanner.IsEnd();

        Assert.Equal(expected, actual);
    }

    public static TheoryData<string, int, char> GetPeekData()
    {
        return new TheoryData<string, int, char>
        {
            {
                "abc",
                0,
                'a'
            },
            {
                "abc",
                1,
                'b'
            },
            {
                "abc",
                2,
                'c'
            },
            {
                "abc",
                3,
                '\0'
            },
            {
                string.Empty,
                0,
                '\0'
            },
        };
    }

    public static TheoryData<string, char> GetRepeatedPeekData()
    {
        return new TheoryData<string, char>
        {
            {
                "abc",
                'a'
            },
        };
    }

    public static TheoryData<string, int, char> GetAdvanceData()
    {
        return new TheoryData<string, int, char>
        {
            {
                "abc",
                1,
                'b'
            },
            {
                "abc",
                2,
                'c'
            },
            {
                "abc",
                3,
                '\0'
            },
        };
    }

    public static TheoryData<string, int, bool> GetIsEndData()
    {
        return new TheoryData<string, int, bool>
        {
            {
                string.Empty,
                0,
                true
            },
            {
                "a",
                0,
                false
            },
            {
                "a",
                1,
                true
            },
        };
    }
}