# Tools

## package-exe.bat

Double-click `package-exe.bat` to build the user-facing Windows x64 package.

Output directory:

```text
..\package
```

The package contains:

```text
JsonFastFormatManager.exe
jsonfmt.exe
JsonFastFormat.ico
README.txt
```

The batch file calls `..\scripts\build-package.ps1`.
