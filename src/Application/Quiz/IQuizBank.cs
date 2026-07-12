namespace Application.Quiz;

public interface IQuizBank
{
    string GetTheQuestionAt(int index);
    bool CheckIsCorrect(int index, string answer);
    int Count { get; }
}
