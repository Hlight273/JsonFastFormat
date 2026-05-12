JsonFastFormat

For normal users:
1. Run JsonFastFormatManager.exe.
2. Click the enable button to add the JSON right-click menu.
3. Right-click a .json file and choose the preview menu item.

Files:
- JsonFastFormatManager.exe: GUI manager and right-click preview handler.
- jsonfmt.exe: streaming JSON formatter used by the manager.
- JsonFastFormat.ico: placeholder icon resource.

The executables are published as self-contained single-file Windows x64 apps.
The preview command writes formatted JSON to %TEMP%\JsonFastFormatPreview and opens it with Notepad. It does not modify the original JSON file.
