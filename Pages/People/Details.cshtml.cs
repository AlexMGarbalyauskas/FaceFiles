using FaceFiles.Data;
using FaceFiles.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FaceFiles.Pages.People;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Person? Person { get; set; }

    public async Task OnGetAsync(int id)
    {
        Person = await _context.People.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
    }
}
