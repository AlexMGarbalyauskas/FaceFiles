using FaceFiles.Data;
using FaceFiles.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FaceFiles.Pages;

public class HomeModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public HomeModel(ApplicationDbContext context)
    {
        _context = context;
    }

    // A folder plus how many people are in it (shown on the folder buttons)
    public record FolderSummary(int Id, string Name, int PersonCount);

    public IList<Person> AlphabeticalPeople { get; set; } = [];

    public IList<FolderSummary> Folders { get; set; } = [];

    // Person id -> names of every folder they are in (shown in the "All" view)
    public Dictionary<int, List<string>> PersonFolderNames { get; set; } = [];

    public int TotalCount { get; set; }

    // Null means the automatic "All people" view
    public int? SelectedFolderId { get; set; }

    public string? SelectedFolderName { get; set; }

    // People in the selected folder, ignoring search (decides whether it can be deleted)
    public int SelectedFolderPersonCount { get; set; }

    public string? SearchTerm { get; set; }

    // Error message that survives the redirect after a failed create/delete
    [TempData]
    public string? FolderError { get; set; }

    public async Task OnGetAsync(string? search, int? folder)
    {
        SearchTerm = search;
        await LoadFoldersAsync(folder);

        var query = _context.People.AsNoTracking().AsQueryable();

        // Folder view shows only that folder's people; "All" shows everyone
        if (SelectedFolderId.HasValue)
        {
            var selectedFolderId = SelectedFolderId.Value;
            var memberIds = _context.PersonFolders
                .Where(link => link.FolderId == selectedFolderId)
                .Select(link => link.PersonId);

            query = query.Where(person => memberIds.Contains(person.Id));
        }

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim().ToLower();
            query = query.Where(person =>
                person.FirstName.ToLower().Contains(term) ||
                (person.LastName ?? string.Empty).ToLower().Contains(term));
        }

        AlphabeticalPeople = await query
            .OrderBy(person => person.FirstName)
            .ThenBy(person => person.LastName)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateFolderAsync(string? folderName)
    {
        var name = folderName?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            FolderError = "Enter a folder name.";
            return RedirectToPage();
        }

        if (name.Length > 100)
        {
            name = name[..100];
        }

        var lowerName = name.ToLower();
        var exists = await _context.Folders.AnyAsync(item => item.Name.ToLower() == lowerName);
        if (exists)
        {
            FolderError = $"A folder called \"{name}\" already exists.";
            return RedirectToPage();
        }

        var newFolder = new Folder { Name = name };
        _context.Folders.Add(newFolder);
        await _context.SaveChangesAsync();

        // Jump straight into the new folder
        return RedirectToPage(new { folder = newFolder.Id });
    }

    public async Task<IActionResult> OnPostDeleteFolderAsync(int folderId)
    {
        var folder = await _context.Folders.FirstOrDefaultAsync(item => item.Id == folderId);
        if (folder is null)
        {
            return RedirectToPage();
        }

        // Rule: only empty folders can be deleted
        var hasPeople = await _context.PersonFolders.AnyAsync(link => link.FolderId == folderId);
        if (hasPeople)
        {
            FolderError = $"\"{folder.Name}\" still has people in it. Remove them from it on their Details page first.";
            return RedirectToPage(new { folder = folderId });
        }

        _context.Folders.Remove(folder);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadFoldersAsync(int? requestedFolderId)
    {
        var folders = (await _context.Folders.AsNoTracking().ToListAsync())
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var folderNames = folders.ToDictionary(item => item.Id, item => item.Name);

        // Only count links that point at folders that still exist
        var links = (await _context.PersonFolders.AsNoTracking().ToListAsync())
            .Where(link => folderNames.ContainsKey(link.FolderId))
            .ToList();

        var counts = links
            .GroupBy(link => link.FolderId)
            .ToDictionary(group => group.Key, group => group.Count());

        Folders = folders
            .Select(item => new FolderSummary(item.Id, item.Name, counts.GetValueOrDefault(item.Id)))
            .ToList();

        PersonFolderNames = links
            .GroupBy(link => link.PersonId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(link => folderNames[link.FolderId])
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList());

        TotalCount = await _context.People.CountAsync();

        // An unknown folder id falls back to the "All" view
        var selected = folders.FirstOrDefault(item => item.Id == requestedFolderId);
        SelectedFolderId = selected?.Id;
        SelectedFolderName = selected?.Name;
        SelectedFolderPersonCount = selected is null ? 0 : counts.GetValueOrDefault(selected.Id);
    }
}
