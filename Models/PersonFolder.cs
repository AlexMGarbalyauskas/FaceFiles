using Microsoft.EntityFrameworkCore;

namespace FaceFiles.Models;

// Links one person to one folder. A person can have many of these, so they can be in several folders.
[PrimaryKey(nameof(PersonId), nameof(FolderId))]
public class PersonFolder
{
    public int PersonId { get; set; }

    public int FolderId { get; set; }
}
