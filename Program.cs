using FaceFiles.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "Data"));
Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "images", "user_photos"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

var isRender = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RENDER"));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment() && !isRender)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

app.Run();
