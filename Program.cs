using FaceFiles.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "Data"));
Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "images", "user_photos"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    var hasDateOfBirth = db.Database
        .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info('People') WHERE name = 'DateOfBirth'")
        .Single() > 0;

    if (!hasDateOfBirth)
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE People ADD COLUMN DateOfBirth TEXT NULL");
    }

    var hasFunction = db.Database
        .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info('People') WHERE name = 'Function'")
        .Single() > 0;

    if (!hasFunction)
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE People ADD COLUMN Function TEXT NULL");
    }

    // NEW: add the Title column to existing databases that were created before it existed
    var hasTitle = db.Database
        .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info('People') WHERE name = 'Title'")
        .Single() > 0;

    if (!hasTitle)
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE People ADD COLUMN Title TEXT NULL");
    }

    // NEW: create the Folders table if it doesn't exist yet (EnsureCreated won't add tables to an existing database)
    db.Database.ExecuteSqlRaw(
        "CREATE TABLE IF NOT EXISTS Folders (Id INTEGER NOT NULL CONSTRAINT PK_Folders PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL)");

    // NEW: link table so a person can be in many folders
    db.Database.ExecuteSqlRaw(
        "CREATE TABLE IF NOT EXISTS PersonFolders (PersonId INTEGER NOT NULL, FolderId INTEGER NOT NULL, CONSTRAINT PK_PersonFolders PRIMARY KEY (PersonId, FolderId))");

    // One-off: the earlier one-folder-per-person version stored a FolderId on People.
    // Copy those into the link table, then clear the old column so it never runs twice.
    var hasOldFolderId = db.Database
        .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info('People') WHERE name = 'FolderId'")
        .Single() > 0;

    if (hasOldFolderId)
    {
        db.Database.ExecuteSqlRaw("INSERT OR IGNORE INTO PersonFolders (PersonId, FolderId) SELECT Id, FolderId FROM People WHERE FolderId IS NOT NULL");
        db.Database.ExecuteSqlRaw("UPDATE People SET FolderId = NULL WHERE FolderId IS NOT NULL");
    }

    var hasAdditionalDetails = db.Database
        .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info('People') WHERE name = 'AdditionalDetailsJson'")
        .Single() > 0;

    if (!hasAdditionalDetails)
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE People ADD COLUMN AdditionalDetailsJson TEXT NULL");
    }

    var hasSocialLinks = db.Database
        .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info('People') WHERE name = 'SocialLinksJson'")
        .Single() > 0;

    if (!hasSocialLinks)
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE People ADD COLUMN SocialLinksJson TEXT NULL");
    }
}

var isRender = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RENDER"));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment() && !isRender)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

app.Run();
