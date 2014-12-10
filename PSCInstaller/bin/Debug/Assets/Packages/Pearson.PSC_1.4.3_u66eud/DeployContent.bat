@echo off
set scriptpath=%~d0%~p0
set flagfilename=_overwrite.flg

if exist "%UserProfile%\AppData\Local\Packages\Pearson.PSC_2dnf9zyx96z42\LocalState\%flagfilename%" (
	echo Content has been previously deployed.
	goto:promptforoverwrite
)
goto:copycontent

:promptforoverwrite
set /p answer=Would you like to overwrite existing content[Y/y/N/n]?:
if /i {%answer%}=={y} (goto :copycontent)
if /i {%answer%}=={yes} (goto :copycontent)

echo CONTENT HAS NOT BEEN DEPLOYED.
goto:exit

:copycontent
echo DEPLOYING PSC CONTENT ...

copy /Y /V "%scriptpath%PSC\_overwrite.flg" "%UserProfile%\AppData\Local\Packages\Pearson.PSC_2dnf9zyx96z42\LocalState"

"%scriptpath%7za" x -y -o"%UserProfile%\AppData\Local\Packages\Pearson.PSC_2dnf9zyx96z42\LocalState" "%scriptpath%PSC\*.7z"

echo CONTENT DEPLOY COMPLETE.

:exit
pause
exit /b 0 