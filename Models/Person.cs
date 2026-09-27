using System.ComponentModel.DataAnnotations;

namespace FaceFiles.Models;

public class Person
{
    public int Id { get; set; }

    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Details { get; set; }

    [StringLength(255)]
    public string? PhotoPath { get; set; }

    [Display(Name = "Uploaded On")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
