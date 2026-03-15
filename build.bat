@echo off
setlocal
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set FCTB=packages\fastcoloredtextbox.1.0.0\lib\net40\FastColoredTextBox.dll
%CSC% /target:winexe /out:JzeroCompilerNativeLite.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:%FCTB% Program.cs PromptDialog.cs SettingsDialog.cs MainForm.cs
if %errorlevel% neq 0 exit /b %errorlevel%
copy /Y %FCTB% FastColoredTextBox.dll >nul
