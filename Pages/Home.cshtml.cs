using FaceFiles.Data;
using FaceFiles.Models;
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

    public IList<Person> AlphabeticalPeople { get; set; } = [];

    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(string? search)
    {
        SearchTerm = search;

        var query = _context.People.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim().ToLower();
            query = query.Where(person =>
                person.FirstName.ToLower().Contains(term) ||
                person.LastName.ToLower().Contains(term));
        }

        AlphabeticalPeople = await query
            .OrderBy(person => person.FirstName)
            .ThenBy(person => person.LastName)
            .ToListAsync();
    }
}
