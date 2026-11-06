using FaceFiles.Data;
using FaceFiles.Models;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FaceFiles.Pages;

public class HomeModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public HomeModel(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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

    [TempData]
    public string? ImportMessage { get; set; }

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

    public async Task<IActionResult> OnGetExportJsonAsync()
    {
        var export = await BuildExportAsync();
        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        return File(Encoding.UTF8.GetBytes(json), "application/json", $"facefiles-profiles-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        var export = await BuildExportAsync();
        var csv = new StringBuilder();
        csv.AppendLine("Id,First name,Last name,Title,Function,Email,Phone,Date of birth,Bio,Personality evaluation,Folders,Social links,Additional details,Notes history");

        foreach (var person in export.People)
        {
            csv.AppendLine(string.Join(",",
                Csv(person.Id),
                Csv(person.FirstName),
                Csv(person.LastName),
                Csv(person.Title),
                Csv(person.Function),
                Csv(person.Email),
                Csv(person.PhoneNumber),
                Csv(person.DateOfBirth),
                Csv(person.Bio),
                Csv(person.PersonalityEvaluation),
                Csv(string.Join("; ", person.Folders)),
                Csv(string.Join("; ", person.SocialLinks.Select(link => $"{link.Platform}: {link.Url}"))),
                Csv(string.Join("; ", person.AdditionalDetails)),
                Csv(string.Join("; ", person.NoteHistory.Select(note => $"{note.NoteType} ({note.CreatedAt:yyyy-MM-dd HH:mm}): {note.Content}")))));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"facefiles-profiles-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    public async Task<IActionResult> OnGetBackupDatabaseAsync()
    {
        var temporaryPath = Path.Combine(Path.GetTempPath(), $"FaceFiles-{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("The database connection string is not configured.");

            await using var source = new SqliteConnection(connectionString);
            await using var destination = new SqliteConnection($"Data Source={temporaryPath}");
            await source.OpenAsync();
            await destination.OpenAsync();
            source.BackupDatabase(destination);

            var bytes = await System.IO.File.ReadAllBytesAsync(temporaryPath);
            return File(bytes, "application/vnd.sqlite3", $"facefiles-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db");
        }
        finally
        {
            if (System.IO.File.Exists(temporaryPath))
            {
                System.IO.File.Delete(temporaryPath);
            }
        }
    }

    public async Task<IActionResult> OnPostImportCsvAsync(IFormFile? csvFile)
    {
        if (csvFile is null || csvFile.Length == 0)
        {
            ImportMessage = "Choose a CSV export file to import.";
            return RedirectToPage();
        }

        if (!Path.GetExtension(csvFile.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ImportMessage = "Only CSV files can be imported.";
            return RedirectToPage();
        }

        using var reader = new StreamReader(csvFile.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var rows = ParseCsv(await reader.ReadToEndAsync());
        if (rows.Count < 2)
        {
            ImportMessage = "The CSV file does not contain any profile rows.";
            return RedirectToPage();
        }

        var headers = rows[0];
        var requiredHeaders = new[]
        {
            "First name", "Last name", "Title", "Email", "Phone", "Date of birth",
            "Bio", "Personality evaluation", "Folders", "Social links", "Additional details"
        };
        var columnIndexes = requiredHeaders.ToDictionary(
            header => header,
            header => Array.FindIndex(headers.ToArray(), value => value.Equals(header, StringComparison.OrdinalIgnoreCase)));

        if (columnIndexes.Values.Any(index => index < 0))
        {
            ImportMessage = "This CSV does not appear to be a FaceFiles profile export.";
            return RedirectToPage();
        }

        var existingPeople = await _context.People.ToListAsync();
        var folders = await _context.Folders.ToListAsync();
        var imported = 0;
        var skipped = 0;

        foreach (var row in rows.Skip(1))
        {
            var firstName = GetCsvValue(row, columnIndexes["First name"]).Trim();
            if (string.IsNullOrWhiteSpace(firstName))
            {
                skipped++;
                continue;
            }

            var lastName = GetCsvValue(row, columnIndexes["Last name"]).Trim();
            var email = GetCsvValue(row, columnIndexes["Email"]).Trim();
            var duplicate = existingPeople.FirstOrDefault(person =>
                person.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(person.LastName?.Trim(), lastName, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(email) ||
                 string.Equals(person.Email?.Trim(), email, StringComparison.OrdinalIgnoreCase)));

            if (duplicate is not null)
            {
                skipped++;
                continue;
            }

            var person = new Person
            {
                FirstName = Limit(firstName, 100),
                LastName = LimitOrNull(lastName, 100),
                Title = LimitOrNull(GetCsvValue(row, columnIndexes["Title"]).Trim(), 100),
                Function = LimitOrNull(GetCsvValue(row, FindOptionalColumn(headers, "Function")).Trim(), 150),
                Email = LimitOrNull(email, 254),
                PhoneNumber = LimitOrNull(GetCsvValue(row, columnIndexes["Phone"]).Trim(), 50),
                DateOfBirth = ParseDate(GetCsvValue(row, columnIndexes["Date of birth"])),
                Details = LimitOrNull(GetCsvValue(row, columnIndexes["Bio"]).Trim(), 500),
                PersonalityEvaluation = LimitOrNull(GetCsvValue(row, columnIndexes["Personality evaluation"]).Trim(), 2000),
                AdditionalDetails = SplitList(GetCsvValue(row, columnIndexes["Additional details"]))
            };

            person.SocialLinks = ParseSocialLinks(GetCsvValue(row, columnIndexes["Social links"]));
            _context.People.Add(person);
            await _context.SaveChangesAsync();
            existingPeople.Add(person);

            var importedNotes = GetCsvValue(row, FindOptionalColumn(headers, "Notes history")).Trim();
            if (!string.IsNullOrWhiteSpace(importedNotes))
            {
                _context.ProfileNoteHistory.Add(new ProfileNoteHistory
                {
                    PersonId = person.Id,
                    NoteType = "Imported notes",
                    Content = importedNotes,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var folderName in SplitList(GetCsvValue(row, columnIndexes["Folders"])))
            {
                var folder = folders.FirstOrDefault(item =>
                    item.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase));

                if (folder is null)
                {
                    folder = new Folder { Name = Limit(folderName, 100) };
                    _context.Folders.Add(folder);
                    await _context.SaveChangesAsync();
                    folders.Add(folder);
                }

                _context.PersonFolders.Add(new PersonFolder
                {
                    PersonId = person.Id,
                    FolderId = folder.Id
                });
            }

            await _context.SaveChangesAsync();
            imported++;
        }

        ImportMessage = $"Imported {imported} profile{(imported == 1 ? string.Empty : "s")}. " +
                        $"{skipped} row{(skipped == 1 ? string.Empty : "s")} skipped.";
        return RedirectToPage();
    }

    private async Task<ProfileExport> BuildExportAsync()
    {
        var people = await _context.People.AsNoTracking().ToListAsync();
        var folders = await _context.Folders.AsNoTracking().ToListAsync();
        var links = await _context.PersonFolders.AsNoTracking().ToListAsync();
        var noteHistory = await _context.ProfileNoteHistory.AsNoTracking().ToListAsync();
        var folderNames = folders.ToDictionary(folder => folder.Id, folder => folder.Name);

        return new ProfileExport(people.Select(person => new PersonExport(
            person.Id,
            person.FirstName,
            person.LastName,
            person.Title,
            person.Function,
            person.Email,
            person.PhoneNumber,
            person.DateOfBirth?.ToString("yyyy-MM-dd"),
            person.Details,
            person.PersonalityEvaluation,
            links.Where(link => link.PersonId == person.Id)
                .Where(link => folderNames.ContainsKey(link.FolderId))
                .Select(link => folderNames[link.FolderId])
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            person.SocialLinks,
            person.AdditionalDetails,
            noteHistory.Where(note => note.PersonId == person.Id).OrderBy(note => note.CreatedAt).ToList())).ToList());
    }

    private static string Csv(object? value)
    {
        var text = Convert.ToString(value) ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private static List<List<string>> ParseCsv(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var value = new StringBuilder();
        var quoted = false;

        for (var index = 0; index < csv.Length; index++)
        {
            var character = csv[index];
            if (character == '"')
            {
                if (quoted && index + 1 < csv.Length && csv[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                row.Add(value.ToString());
                value.Clear();
            }
            else if ((character == '\r' || character == '\n') && !quoted)
            {
                if (character == '\r' && index + 1 < csv.Length && csv[index + 1] == '\n')
                {
                    index++;
                }

                row.Add(value.ToString());
                value.Clear();
                if (row.Any(field => !string.IsNullOrWhiteSpace(field)))
                {
                    rows.Add(row);
                }
                row = new List<string>();
            }
            else
            {
                value.Append(character);
            }
        }

        if (value.Length > 0 || row.Count > 0)
        {
            row.Add(value.ToString());
            if (row.Any(field => !string.IsNullOrWhiteSpace(field)))
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    private static string GetCsvValue(IReadOnlyList<string> row, int index) =>
        index >= 0 && index < row.Count ? row[index] : string.Empty;

    private static int FindOptionalColumn(IReadOnlyList<string> headers, string name) =>
        Array.FindIndex(headers.ToArray(), value => value.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static List<string> SplitList(string value) =>
        value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<SocialLink> ParseSocialLinks(string value)
    {
        return SplitList(value)
            .Select(item =>
            {
                var separator = item.IndexOf(':');
                return separator <= 0 || separator == item.Length - 1
                    ? null
                    : new SocialLink
                    {
                        Platform = item[..separator].Trim(),
                        Url = item[(separator + 1)..].Trim()
                    };
            })
            .Where(link => link is not null)
            .Cast<SocialLink>()
            .ToList();
    }

    private static DateTime? ParseDate(string value) =>
        DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date) ? date : null;

    private static string Limit(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string? LimitOrNull(string value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Limit(value, maxLength);

    private sealed record ProfileExport(List<PersonExport> People);

    private sealed record PersonExport(
        int Id,
        string FirstName,
        string? LastName,
        string? Title,
        string? Function,
        string? Email,
        string? PhoneNumber,
        string? DateOfBirth,
        string? Bio,
        string? PersonalityEvaluation,
        List<string> Folders,
        List<SocialLink> SocialLinks,
        List<string> AdditionalDetails,
        List<ProfileNoteHistory> NoteHistory);

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
