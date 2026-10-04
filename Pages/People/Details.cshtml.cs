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

    [BindProperty]
    public string? NewDetail { get; set; }

    [BindProperty]
    public string? NewSurname { get; set; }

    [BindProperty]
    public DateTime? NewDateOfBirth { get; set; }

    [BindProperty]
    public string? NewBio { get; set; }

    [BindProperty]
    public IFormFile? PhotoUpload { get; set; }

    public async Task OnGetAsync(int id)
    {
        Person = await _context.People.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
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

    public async Task<IActionResult> OnPostUpdateDateOfBirthAsync(int id)
    {
        var person = await FindPersonAsync(id);
        if (person is null)
        {
            return NotFound();
        }

        person.DateOfBirth = NewDateOfBirth;
        await _context.SaveChangesAsync();
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

    private Task<Person?> FindPersonAsync(int id)
    {
        return _context.People.FirstOrDefaultAsync(item => item.Id == id);
    }
}
