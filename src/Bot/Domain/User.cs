namespace Bot;

/// <summary>
/// 純領域模型:社群使用者的身分與計分。放 Bot 專案的 <c>Domain/</c> 資料夾(命名空間 <c>Bot</c>),
/// 不依賴 FSM 以外任何型別。Bot 層(<see cref="IBotContext"/>、guard)與 Application 層皆使用它。
/// </summary>
public sealed class User
{
    public string Id { get; }
    public bool IsAdmin { get; }
    public int Score { get; set; }      // 知識王計分(批三/四用)
    public bool IsOnline { get; set; }  // 預留;OnlineCount 仍用 int 計數器

    public User(string id, bool isAdmin)
    {
        Id = id;
        IsAdmin = isAdmin;
    }
}
