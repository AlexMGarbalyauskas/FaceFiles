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
}
