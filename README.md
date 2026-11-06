# FaceFiles

FaceFiles is a private people-profile management web application built with ASP.NET Core Razor Pages and SQLite. It provides one place to organize people, store profile information, manage social links, review note history, export data, and optionally use an AI assistant for profile-related questions.

## Features

- **People directory** with folders and instant search while typing
- **Profile pages** for identity, contact, biography, personality evaluation, photos, and additional details
- **Inline editing** for common profile fields without leaving the profile page
- **Notes history** that records changes to bios, evaluations, and additional details
- **Social links** with supported previews and direct-link fallbacks when a platform blocks embedding
- **Optional OpenAI assistant** available from a profile page
- **CSV and JSON exports** for portability and spreadsheet workflows
- **CSV profile restore** to re-add profiles from a saved FaceFiles CSV export
- **SQLite database backup** without stopping the application
- **Responsive interface** for desktop and mobile layouts
- **Local photo storage** under `wwwroot/images/user_photos`

## Technology

| Area | Technology |
|---|---|
| Framework | ASP.NET Core Razor Pages |
| Runtime | .NET 9 |
| Language | C# |
| Database | SQLite |
| Data access | Entity Framework Core SQLite 8.0.8 |
| Front end | Razor HTML, CSS, Bootstrap, and vanilla JavaScript |
| AI integration | Server-side OpenAI Chat Completions API |

## Requirements

- .NET 9 SDK
- Windows, macOS, or Linux
- A modern web browser
- OpenAI API credits only if the optional AI assistant is enabled

## Getting started

Clone or download the repository, then run:

```powershell
cd "C:\Users\smart\OneDrive\Desktop\Face_Files_Project"
dotnet restore
dotnet run --urls "http://localhost:5080"
```

Open [http://localhost:5080](http://localhost:5080) in your browser.

For the default development URL, run:

```powershell
dotnet run
```

The application creates the `Data` directory, the SQLite database, and the profile-photo directory when it starts. Existing databases receive compatible columns and tables through the startup checks in `Program.cs`.

## Project structure

```text
FaceFiles/
├── Data/
│   ├── ApplicationDbContext.cs
│   └── FaceFiles.db
├── Models/
│   ├── Folder.cs
│   ├── Person.cs
│   ├── PersonFolder.cs
│   ├── ProfileNoteHistory.cs
│   └── SocialLink.cs
├── Pages/
│   ├── Home.cshtml
│   ├── Home.cshtml.cs
│   └── People/
│       ├── Create.cshtml(.cs)
│       ├── Delete.cshtml(.cs)
│       ├── Details.cshtml(.cs)
│       └── Edit.cshtml(.cs)
├── wwwroot/
│   ├── css/site.css
│   ├── images/
│   └── videos/
├── Program.cs
├── FaceFiles.csproj
└── README.md
```

## How it works

FaceFiles uses the Razor Pages request model:

1. A browser requests a page such as `/Home` or `/People/Details/42`.
2. ASP.NET Core creates the matching PageModel and injects `ApplicationDbContext`.
3. The PageModel queries SQLite through Entity Framework Core.
4. Razor renders the page using the PageModel data.
5. User actions post to named handlers such as profile-save or export handlers.
6. The handler validates the request, updates the database, records history when needed, and returns the updated page.

Search is intentionally client-side: the directory is already rendered, so matching rows are hidden or shown immediately without a full-page reload.

## Data model

- `Person` stores the main identity, contact, biography, evaluation, and photo path.
- `Folder` and `PersonFolder` support many-to-many folder organization.
- Additional details and social links are stored as JSON fields on `Person`.
- `ProfileNoteHistory` stores timestamped changes to profile notes and evaluations.
- Photos are saved as files in `wwwroot/images/user_photos`; the relative path is stored on the person record.

The current application uses `EnsureCreated()` and startup compatibility checks rather than a formal EF Core migration history. Back up `Data/FaceFiles.db` before changing the model.

## OpenAI assistant configuration

The AI assistant is optional. API calls are made from the server so the API key is never sent to browser JavaScript.

Set the key as a user-level Windows environment variable:

```powershell
[Environment]::SetEnvironmentVariable(
  "OpenAI__ApiKey",
  "YOUR_NEW_KEY",
  "User"
)
```

Restart the terminal and the application after setting the variable. The configuration key maps to `OpenAI:ApiKey`.

An optional model can be configured with:

```powershell
[Environment]::SetEnvironmentVariable(
  "OpenAI__Model",
  "gpt-4o-mini",
  "User"
)
```

### Security and privacy

- Never commit an API key.
- Never place an API key in `appsettings.json`, Razor markup, JavaScript, or screenshots.
- If a key is exposed, revoke it and create a replacement.
- Profile information sent to OpenAI may include contact details, biography, personality evaluation, folders, social links, and additional details.
- OpenAI API usage requires an account with available credits.

## Export and backup

Use the **Export & backup** menu on the Home page:

- **CSV** is convenient for spreadsheets.
- **JSON** preserves nested profile data, social links, additional details, and note history.
- **Import CSV** restores profiles, folders, social links, and additional details from a saved FaceFiles CSV export. Existing matching profiles are skipped to avoid duplicates.
- **Database backup** creates a consistent SQLite copy while the application is running.

Backups contain personal information. Store them securely outside the repository and verify that a backup can be opened before relying on it.

## Development commands

Restore dependencies:

```powershell
dotnet restore
```

Build:

```powershell
dotnet build
```

Run:

```powershell
dotnet run --urls "http://localhost:5080"
```

If a build reports that `FaceFiles.exe` is locked, an existing application process is still running. Stop the specific running FaceFiles process or stop the active debugging session, then build again.

## Troubleshooting

| Problem | Resolution |
|---|---|
| `FaceFiles.exe` is locked | Stop the running application before rebuilding. |
| AI assistant returns `503` | Restart the application after setting `OpenAI__ApiKey`. |
| AI assistant returns `402` | The OpenAI project has no available credits. |
| Social preview is unavailable | The platform may block iframe embedding; use the direct profile link. |
| New fields do not appear | Restart the application so the startup database compatibility checks run. |
| Search does not navigate to another page | Search is designed to filter the current directory without reloading. |

## Documentation

See the companion [FaceFiles Project Guide](./FaceFiles-Project-Guide.docx) for a longer explanation of the application’s purpose, architecture, data flow, operations, and future improvements.

## Recommended future improvements

- Add authentication and authorization for profile data.
- Add automated tests for profile handlers, exports, backups, and note history.
- Replace startup schema checks with formal EF Core migrations as the schema grows.
- Add CSV/JSON import with validation and duplicate detection.
- Add an optional local AI provider to avoid external data transfer and per-request fees.

## License

No license has been specified for this repository. Add a license before distributing the project or accepting external contributions.
