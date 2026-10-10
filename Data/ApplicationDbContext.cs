using FaceFiles.Models;
using Microsoft.EntityFrameworkCore;

namespace FaceFiles.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Person> People => Set<Person>();

    // User-created folders (College, Work, ...)
    public DbSet<Folder> Folders => Set<Folder>();

    // Which people are in which folders (many-to-many)
    public DbSet<PersonFolder> PersonFolders => Set<PersonFolder>();
}
