namespace OmniClip.Core.Models;

public class ClipboardEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ContentType ContentType { get; set; } = ContentType.Text;
    public string PlainText { get; set; } = string.Empty;
    public string RichText { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string SourceApp { get; set; } = string.Empty;
    public string SourceWindow { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public int CharCount { get; set; }
    public string Tags { get; set; } = "[]";
    public bool IsFavorite { get; set; }
    public bool IsPinned { get; set; }
    public bool IsSensitive { get; set; }
    public bool InKnowledgeBase { get; set; }
    public double ImportanceScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessed { get; set; }
}
