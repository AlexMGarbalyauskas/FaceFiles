using FaceFiles.Data;
using FaceFiles.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FaceFiles.Pages.People;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public DetailsModel(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public Person? Person { get; set; }

    // All folders, for the folder checkboxes
    public List<Folder> Folders { get; set; } = [];

    // Ids of the folders this person is currently in
    public List<int> MemberFolderIds { get; set; } = [];

    // Ticked folders from the checkboxes (empty = remove from all folders)
    [BindProperty]
    public List<int> SelectedFolderIds { get; set; } = [];

    [BindProperty]
    public string? NewDetail { get; set; }

    [BindProperty]
    public string? NewSurname { get; set; }

    // Bound from the Title edit form (input name="NewTitle")
    [BindProperty]
    public string? NewTitle { get; set; }

    [BindProperty]
    public DateTime? NewDateOfBirth { get; set; }

    [BindProperty]
    public string? NewBio { get; set; }

    [BindProperty]
    public IFormFile? PhotoUpload { get; set; }

    [BindProperty]
    public int DetailIndex { get; set; }

    [BindProperty]
    public string? EditedDetail { get; set; }

    [BindProperty]
    public string? NewSocialPlatform { get; set; }

    [BindProperty]
    public string? NewSocialUrl { get; set; }

    public async Task OnGetAsync(int id)
    {
        Person = await _context.People.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

        Folders = (await _context.Folders.AsNoTracking().ToListAsync())
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        MemberFolderIds = await _context.PersonFolders
            .AsNoTracking()
            .Where(link => link.PersonId == id)
            .Select(link => link.FolderId)
            .ToListAsync();
    }

    // Saves the ticked folders: adds missing links, removes unticked ones.
    // The person always stays in the "All people" list.
    public async Task<IActionResult> OnPostUpdateFoldersAsync(int id)
    {
        var person = await FindPersonAsync(id);
        if (person is null)
        {
            return NotFound();
        }

        // Only accept folders that exist
        var validFolderIds = await _context.Folders
            .Where(item => SelectedFolderIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToListAsync();

        var existingLinks = await _context.PersonFolders
            .Where(link => link.PersonId == id)
            .ToListAsync();

        _context.PersonFolders.RemoveRange(existingLinks.Where(link => !validFolderIds.Contains(link.FolderId)));

        foreach (var folderId in validFolderIds.Where(folderId => existingLinks.All(link => link.FolderId != folderId)))
        {
            _context.PersonFolders.Add(new PersonFolder { PersonId = id, FolderId = folderId });
        }

        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddDetailAsync(int id)
    {
        var person = await _context.People.FirstOrDefaultAsync(item => item.Id == id);
        if (person is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(NewDetail))
        {
            var details = person.AdditionalDetails;
            details.Add(NewDetail.Trim());
            person.AdditionalDetails = details;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateSurnameAsync(int id)
    {
        var person = await FindPersonAsync(id);
        if (person is null)
        {
            return NotFound();
        }

        person.LastName = NewSurname?.Trim() ?? string.Empty;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    // Saves the title. An empty value clears it (stored as null).
    public async Task<IActionResult> OnPostUpdateTitleAsync(int id)
    {
        var person = await FindPersonAsync(id);
        if (person is null)
        {
            return NotFound();
        }

        var title = NewTitle?.Trim();
        person.Title = string.IsNullOrWhiteSpace(title) ? null : title;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateDateOfBirthAsync(int id)
    {
        var person = await FindPersonAsync(id);
        if (person is null)
        {
            return NotFound();
        }

        // Ignore dates in the future; an empty value clears the date of birth
        if (NewDateOfBirth is null || NewDateOfBirth.Value.Date <= DateTime.Today)
        {
            person.DateOfBirth = NewDateOfBirth;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateBioAsync(int id)
    {
        var person = await FindPersonAsync(id);
        if (person is null)
        {
            return NotFound();
        }

        person.Details = NewBio?.Trim();
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddPhotoAsync(int id)
    {
        var person = await FindPersonAsync(id);
        if (person is null)
        {
            return NotFound();
        }

        if (PhotoUpload is not null && PhotoUpload.Length > 0)
        {
            PhotoStorage.DeletePhoto(_environment, person.PhotoPath);
            person.PhotoPath = await PhotoStorage.SavePhotoAsync(_environment, PhotoUpload);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateDetailAsync(int id)
    {
        var person = await FindPersonAsync(id);
        if (person is null)
        {
            return NotFound();
        }

        var details = person.AdditionalDetails;
        if (DetailIndex >= 0 && DetailIndex < details.Count)
        {
            if (string.IsNullOrWhiteSpace(EditedDetail))
            {
                details.RemoveAt(DetailIndex);
            }
            else
            {
                details[DetailIndex] = EditedDetail.Trim();
            }

            person.AdditionalDetails = details;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeletePersonAsync(int id)
    {
        var person = await FindPersonAsync(id);
        if (person is null)
        {
            return NotFound();
        }

        // Remove the person's folder links too, so no empty-folder checks are blocked by a deleted person
        var links = await _context.PersonFolders.Where(link => link.PersonId == id).ToListAsync();
        _context.PersonFolders.RemoveRange(links);

        PhotoStorage.DeletePhoto(_environment, person.PhotoPath);
        _context.People.Remove(person);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Home");
    }

    public async Task<IActionResult> OnPostAddSocialLinkAsync(int id)
    {
        var person = await FindPersonAsync(id);
        if (person is null)
        {
            return NotFound();
        }

        var isValidUrl = Uri.TryCreate(NewSocialUrl?.Trim(), UriKind.Absolute, out var socialUri)
            && (socialUri.Scheme == Uri.UriSchemeHttp || socialUri.Scheme == Uri.UriSchemeHttps);

        if (!string.IsNullOrWhiteSpace(NewSocialPlatform) && isValidUrl)
        {
            var links = person.SocialLinks;
            links.Add(new SocialLink
            {
                Platform = NewSocialPlatform.Trim(),
                Url = NewSocialUrl!.Trim()
            });
            person.SocialLinks = links;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }

    private Task<Person?> FindPersonAsync(int id)
    {
        return _context.People.FirstOrDefaultAsync(item => item.Id == id);
    }
}
