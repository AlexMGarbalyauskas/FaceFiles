using FaceFiles.Data;
using FaceFiles.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Person.PhotoPath = await PhotoStorage.SavePhotoAsync(_environment, PhotoUpload);
        Person.CreatedAt = DateTime.UtcNow;

        _context.People.Add(Person);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Home");
    }
}
