using System.Text;

namespace Application.Quiz;

/// <summary>一題選擇題:題幹 + 四選項(依序對應 A/B/C/D)+ 正解代號('A'..'D')。</summary>
public sealed record QuizQuestion(string Stem, string[] Options, char CorrectOption);

/// <summary>
/// 選擇題題庫。<see cref="QuestionAt"/> 把題幹 + 四選項格式化成可發問字串;
/// <see cref="IsCorrect"/> 只接受字母代號(Trim + 忽略大小寫;回選項內容不算對)。
/// 之後要加題只需往 <see cref="_questions"/> 補 <see cref="QuizQuestion"/>,Count 自動反映、流程不動。
/// </summary>
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

    public string QuestionAt(int index)
    {
        var q = _questions[index];
        var sb = new StringBuilder(q.Stem);
        for (var i = 0; i < q.Options.Length; i++)
            sb.Append('\n').Append(Labels[i]).Append(") ").Append(q.Options[i]);
        return sb.ToString();
    }

    public bool IsCorrect(int index, string answer)
    {
        var a = answer?.Trim();
        if (string.IsNullOrEmpty(a) || a.Length != 1)
            return false;
        return char.ToUpperInvariant(a[0]) == _questions[index].CorrectOption;
    }
}
