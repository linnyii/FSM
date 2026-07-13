namespace Bot;

public interface IMessenger
{
    void SendChat(string content, IReadOnlyList<string>? tags = null);

    void CommentPost(string postId, string content, IReadOnlyList<string>? tags = null);

    void GoBroadcasting();

    void Speak(string content);

    void StopBroadcasting();
}
