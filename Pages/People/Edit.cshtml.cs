using FaceFiles.Data;
using FaceFiles.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FaceFiles.Pages.People;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public EditModel(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [BindProperty]
    public Person Person { get; set; } = new();

    [BindProperty]
    public IFormFile? PhotoUpload { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var person = await _context.People.FirstOrDefaultAsync(item => item.Id == id);
        if (person is null)
        {
            return NotFound();
        }

        Person = person;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var personToUpdate = await _context.People.FirstOrDefaultAsync(item => item.Id == id);
        if (personToUpdate is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            Person.PhotoPath = personToUpdate.PhotoPath;
            return Page();
        }

        personToUpdate.FirstName = Person.FirstName;
        personToUpdate.LastName = Person.LastName;
        personToUpdate.Details = Person.Details;

        if (PhotoUpload is not null && PhotoUpload.Length > 0)
        {
            PhotoStorage.DeletePhoto(_environment, personToUpdate.PhotoPath);
            personToUpdate.PhotoPath = await PhotoStorage.SavePhotoAsync(_environment, PhotoUpload);
        }

        await _context.SaveChangesAsync();
        return RedirectToPage("/Home");
    }
}
