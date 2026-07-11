using Application.Quiz;
using Xunit;

namespace Waterball.Tests;

public class QuizBankTests
{
    private static readonly ChoiceQuizBank Bank = new();

    [Fact]
    public void Count_is_three()
    {
        Assert.Equal(3, Bank.Count);
    }

    [Theory]
    [InlineData(0, "A", true)]
    [InlineData(0, "a", true)]   // 忽略大小寫
    [InlineData(0, "B", false)]
    [InlineData(1, "C", true)]
    [InlineData(2, "A", true)]
    public void IsCorrect_matches_option_letter(int index, string answer, bool expected)
    {
        Assert.Equal(expected, Bank.IsCorrect(index, answer));
    }

    [Theory]
    [InlineData(" a ", true)]        // Trim
    [InlineData("SELECT *", false)]  // 只收代號,不收選項內容
    [InlineData("", false)]
    public void IsCorrect_edge_cases(string answer, bool expected)
    {
        Assert.Equal(expected, Bank.IsCorrect(0, answer));
    }

    [Fact]
    public void IsCorrect_null_answer_is_false()
    {
        Assert.False(Bank.IsCorrect(0, null!));
    }

    [Fact]
    public void QuestionAt_contains_stem_and_four_option_labels()
    {
        var q = Bank.QuestionAt(0);
        Assert.Contains("請問哪個 SQL 語句用於選擇所有的行?", q);
        Assert.Contains("A) SELECT *", q);
        Assert.Contains("B) SELECT ALL", q);
        Assert.Contains("C) SELECT ROWS", q);
        Assert.Contains("D) SELECT DATA", q);
    }
}
