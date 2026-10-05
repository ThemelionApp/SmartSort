# SmartSort

Portable Windows file/folder sorter.

## End-user build
The produced `SmartSort.exe` is self-contained. End users do **not** need Python, .NET, an installer, or admin rights.

## Build with GitHub Actions
1. Put these files in a GitHub repository.
2. Open **Actions** -> **Build SmartSort Windows EXE** -> **Run workflow**.
3. Download the `SmartSort-Windows-x64` artifact.
4. Extract `SmartSort.exe` and run it on any Windows 10/11 x64 PC.

## Safety
- Preview-first.
- No permanent deletion.
- Exact duplicates are moved to `Delete Candidates\\Duplicates`.
- Low-confidence items go to `00 - Review`.
- Every sort creates an undo history JSON file.
- SmartSort ignores its own output folders on repeat scans.

## Drag-and-drop
You can drag a folder onto `SmartSort.exe`; the app opens using that folder as the target.


## v0.4 interface
Compact utility layout inspired by the original prototype: folder bar, dense stats strip, full-size preview table, and permanently visible Undo / Sort actions.
