using System.ComponentModel.DataAnnotations;

namespace FaceFiles.Models;

// A named group of people (e.g. "College", "Work"). The "All" view is not stored: it is built automatically.
public class Folder
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}
