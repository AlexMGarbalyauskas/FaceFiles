using FaceFiles.Data;
using FaceFiles.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FaceFiles.Pages.People;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public CreateModel(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [BindProperty]
    public Person Person { get; set; } = new();

    [BindProperty]
    public IFormFile? PhotoUpload { get; set; }

    // Ticked folders from the checkboxes (a person can be in several)
    [BindProperty]
    public List<int> SelectedFolderIds { get; set; } = [];

    // All folders, for the checkboxes
    public List<Folder> Folders { get; set; } = [];

    // "folder" comes from the Home page so the folder you were viewing is pre-ticked
    public async Task OnGetAsync(int? folder)
    {
        if (folder.HasValue)
        {
            SelectedFolderIds = [folder.Value];
        }

        await LoadFoldersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadFoldersAsync();
            return Page();
        }

        Person.LastName ??= string.Empty;
        Person.PhotoPath = await PhotoStorage.SavePhotoAsync(_environment, PhotoUpload);
        Person.CreatedAt = DateTime.UtcNow;

        _context.People.Add(Person);
        await _context.SaveChangesAsync();

        // Person.Id exists now, so link them to every ticked folder that exists
        var validFolderIds = await _context.Folders
            .Where(item => SelectedFolderIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToListAsync();

        foreach (var folderId in validFolderIds)
        {
            _context.PersonFolders.Add(new PersonFolder { PersonId = Person.Id, FolderId = folderId });
        }

        if (validFolderIds.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        // Go back to the first folder the person was added to (or the full list)
        int? returnFolder = validFolderIds.Count > 0 ? validFolderIds[0] : null;
        return RedirectToPage("/Home", new { folder = returnFolder });
    }

    private async Task LoadFoldersAsync()
    {
        Folders = (await _context.Folders.AsNoTracking().ToListAsync())
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
