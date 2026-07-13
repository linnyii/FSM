namespace Bot;

public sealed class User(string id, bool isAdmin)
{
    public string Id { get; } = id;
    public bool IsAdmin { get; } = isAdmin;
    public int Score { get; set; }     
    public bool IsOnline { get; set; } 
}
