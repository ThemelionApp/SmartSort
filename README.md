# SmartSort v0.5

SmartSort is a preview-first Windows file organizer.

## New in v0.5

- Editable sorting rules from the new **Rules** button.
- Users can create, delete, and modify destination folders.
- Each rule supports:
  - Destination folder
  - Filename keywords
  - File extensions
  - Confidence level
- Adjustable Ready/Review threshold.
- Duplicate detection can be enabled or disabled.
- Temporary-download quarantine can be enabled or disabled.
- Rules persist per Windows user in `%APPDATA%\SmartSort\settings.json`.
- Preview now shows:
  - File/folder name
  - Extension/type
  - Size
  - Modified date/time
  - Destination
  - Ready/Review status
  - Matching reason
- Folder sizes are calculated recursively when possible.
- Sorting remains reversible via **Undo last sort**.

## Build

Publish as a self-contained single Windows executable:

```powershell
dotnet publish SmartSort.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o publish
```

The result is `publish\SmartSort.exe`.
