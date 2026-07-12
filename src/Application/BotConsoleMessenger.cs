using Application.Output;
using Bot;

namespace Application;

public sealed class BotConsoleMessenger : IMessenger
{
    private readonly ChatRoomView _chatRoom;
    private readonly ForumView _forum;
    private readonly BroadcastView _broadcast;

    public BotConsoleMessenger(TextWriter? output = null)
    {
        var @out = output ?? Console.Out;
        _chatRoom = new ChatRoomView(@out);
        _forum = new ForumView(@out);
        _broadcast = new BroadcastView(@out);
    }

    public void SendChat(string content, IReadOnlyList<string>? tags = null) =>
        _chatRoom.BotSays(content, tags);

    public void CommentPost(string postId, string content, IReadOnlyList<string>? tags = null) =>
        _forum.BotComments(postId, content, tags);

    public void GoBroadcasting() => _broadcast.BotStarts();

    public void Speak(string content) => _broadcast.BotSpeaks(content);

    public void StopBroadcasting() => _broadcast.BotStops();
}
