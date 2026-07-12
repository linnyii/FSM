using System.Text;

namespace Application.Quiz;

public sealed record QuizQuestion(string Question, string[] Options, char CorrectOption);

public sealed class ChoiceQuizBank : IQuizBank
{
    private static readonly char[] Labels = ['A', 'B', 'C', 'D'];

    private readonly QuizQuestion[] _questions =
    [
        new("請問哪個 SQL 語句用於選擇所有的行?",
            ["SELECT *", "SELECT ALL", "SELECT ROWS", "SELECT DATA"], 'A'),
        new("請問哪個 CSS 屬性可用於設置文字的顏色?",
            ["text-align", "font-size", "color", "padding"], 'C'),
        new("請問在計算機科學中,「XML」代表什麼?",
            ["Extensible Markup Language", "Extensible Modeling Language",
             "Extended Markup Language", "Extended Modeling Language"], 'A'),
    ];

    public int Count => _questions.Length;

    public string GetTheQuestionAt(int index)
    {
        var theQuestion = _questions[index];
        var stringBuilder = new StringBuilder(theQuestion.Question);
        for (var i = 0; i < theQuestion.Options.Length; i++)
            stringBuilder.Append('\n').Append(Labels[i]).Append(") ").Append(theQuestion.Options[i]);
        return stringBuilder.ToString();
    }

    public bool CheckIsCorrect(int index, string answer)
    {
        var a = answer.Trim();
        if (string.IsNullOrEmpty(a) || a.Length != 1)
            return false;
        return char.ToUpperInvariant(a[0]) == _questions[index].CorrectOption;
    }
}
