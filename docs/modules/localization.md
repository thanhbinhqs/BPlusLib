# Localization

Thread-safe helper for loading and retrieving localized strings using ResourceManager. Supports multiple cultures, fallback cultures, and additional resource files. Fully cross-platform.

## Classes

### LocalizationHelper
Provides a thread-safe helper for loading and retrieving localized strings using `System.Resources.ResourceManager`. Supports multiple cultures, fallback cultures, and additional resource files.

| Method | Returns | Description |
|--------|---------|-------------|
| LocalizationHelper(string baseName, string? resourceDirectory) | LocalizationHelper | Initializes a new instance with the base name of embedded .resx resources |
| LoadCulture(string cultureCode) | void | Loads resources for a specific culture (e.g., "en-US", "vi-VN") |
| GetString(string key) | string | Gets a localized string for the specified key using CurrentUICulture |
| GetString(string key, params object[] args) | string | Gets a formatted localized string with provided arguments |
| HasKey(string key) | bool | Checks whether the specified resource key exists |
| GetAvailableCultures() | IReadOnlyList\<string\> | Returns all available culture codes that have been loaded or discovered |
| GetSystemUICulture() | static string | Gets the name of the current UI culture |
| GetSystemCulture() | static string | Gets the name of the current culture |
| SetDefaultCulture(string cultureCode) | void | Sets the fallback culture to use when a key is not found |
| AddResourceFile(string filePath) | void | Loads an additional .resources file for lookup |

## Usage

```csharp
using BPlusLib.Foundation.Localization;

// Initialize with a resource base name
var l10n = new LocalizationHelper("MyApp.Resources.Strings");

// Pre-load specific cultures
l10n.LoadCulture("en-US");
l10n.LoadCulture("vi-VN");

// Set a fallback culture
l10n.SetDefaultCulture("en");

// Retrieve localized strings
string welcome = l10n.GetString("WelcomeMessage");
string greeting = l10n.GetString("HelloUser", "Alice"); // Formatted string

// Check if a key exists
bool hasKey = l10n.HasKey("MissingKey"); // returns false

// Get available cultures
var cultures = l10n.GetAvailableCultures();

// Load additional resource files
l10n.AddResourceFile("/path/to/extra.resources");
```

## Dependencies
- `System.Resources.ResourceManager` (built-in)
- `System.Globalization` (built-in)
- No external NuGet packages required
- Fully cross-platform (no P/Invoke)
