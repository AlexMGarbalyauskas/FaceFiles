using FaceFiles.Data;
using FaceFiles.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FaceFiles.Pages.People;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public DeleteModel(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [BindProperty]
    public Person? Person { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Person = await _context.People.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (Person is null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var person = await _context.People.FirstOrDefaultAsync(item => item.Id == id);
        if (person is null)
        {
            return NotFound();
        }

        PhotoStorage.DeletePhoto(_environment, person.PhotoPath);
        _context.People.Remove(person);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Home");
    }
}
