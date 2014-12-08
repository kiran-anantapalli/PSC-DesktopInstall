@echo off
set scriptpath=%~d0%~p0

REGEDIT /S %scriptpath%AllowAllTrustedApps.reg

dism /Online /Add-ProvisionedAppxPackage /PackagePath:"%scriptpath%Pearson.PSC_1.4.3.0.appxbundle" /skiplicense
pause

powershell -Command "Add-AppxPackage '%scriptpath%Pearson.PSC_1.4.3.0.appxbundle' -DependencyPath '%scriptpath%Dependencies\x86\Microsoft.VCLibs.x86.12.00.appx'"
pause
