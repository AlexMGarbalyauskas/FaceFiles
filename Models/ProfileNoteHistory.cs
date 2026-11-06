using System.ComponentModel.DataAnnotations;

namespace FaceFiles.Models;

public class ProfileNoteHistory
{
    public int Id { get; set; }

    public int PersonId { get; set; }

    [StringLength(100)]
    public string NoteType { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
