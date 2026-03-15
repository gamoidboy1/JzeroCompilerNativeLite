JzeroCompilerNativeLite

Purpose:
- A lower-RAM native WinForms replacement for the Electron-based JzeroCompiler.

Features:
- Workspace browser for .c, .h, and .txt files
- Search by file name and file content
- Open, auto-save, create, rename, and delete files/folders
- Compile and run C code with C:\MinGW\bin\gcc.exe
- Stream program output and send stdin input
- Persist workspace path in AppData

Build:
- Run build.bat

Notes:
- This version is designed for RAM efficiency, so it uses native controls instead of Electron, Monaco, and xterm.
