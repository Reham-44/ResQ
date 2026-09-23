namespace ResQ.Domain.Entities;

public class Notification
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification() { }

    public Notification(string userId, string title, string message)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        UserId = userId;
        Title = title;
        Message = message;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsRead() => IsRead = true;
}
