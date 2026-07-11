namespace Application.Quiz;

/// <summary>
/// 題庫:知識王答題流程的資料源。正解與比對邏輯封在實作內 —— 呼叫端(FSM)只能問「對不對」,
/// 拿不到正解。用 index 定址(無狀態);答題進度由 ctx 的 CurrentQuestionIndex 掌握。
/// </summary>
public interface IQuizBank
{
    /// <summary>第 index 題的內容(發問用,含題幹 + 選項)。</summary>
    string QuestionAt(int index);

    /// <summary>第 index 題的 answer 是否正確。</summary>
    bool IsCorrect(int index, string answer);

    /// <summary>題數。</summary>
    int Count { get; }
}
