using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace FaceFiles.Models;

public class Person
{
    public int Id { get; set; }

    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? LastName { get; set; }

    // NEW: a person's title (e.g. "Dr", "Manager"). Nullable so existing rows stay valid.
    [StringLength(100)]
    public string? Title { get; set; }

    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(150)]
    public string? Function { get; set; }

    [StringLength(500)]
    public string? Details { get; set; }

    public string? AdditionalDetailsJson { get; set; }

    public string? SocialLinksJson { get; set; }

    [NotMapped]
    public List<string> AdditionalDetails
    {
        get => string.IsNullOrWhiteSpace(AdditionalDetailsJson)
            ? []
            : JsonSerializer.Deserialize<List<string>>(AdditionalDetailsJson) ?? [];
        set => AdditionalDetailsJson = JsonSerializer.Serialize(value ?? []);
    }

    [NotMapped]
    public List<SocialLink> SocialLinks
    {
        get => string.IsNullOrWhiteSpace(SocialLinksJson)
            ? []
            : JsonSerializer.Deserialize<List<SocialLink>>(SocialLinksJson) ?? [];
        set => SocialLinksJson = JsonSerializer.Serialize(value ?? []);
    }

    [StringLength(255)]
    public string? PhotoPath { get; set; }

    [Display(Name = "Uploaded On")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
