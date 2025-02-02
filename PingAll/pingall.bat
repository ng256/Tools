@echo off
setlocal
call :setEsc
set fn=%1
if "%fn%" == "" (set "fn=%~dp0ip.txt")
for /F "eol=; tokens=1,2* delims= " %%a in (%fn%) do (
	call :iter %%a %%b %%c
)
pause
exit /B 0

:setEsc
	for /F "tokens=1,2 delims=#" %%a in ('"prompt #$H#$E# & echo on & for %%b in (1) do rem"') do (
		set ESC=%%b
		exit /B 0
)

:iter
	if "%1" == "" (exit /B 0)
	if "%2" == "" (call :title %1) else (call :doPing %1 %2 %3)
		exit /B 0
)
:doPing
	echo | set /P =%1 %ESC%[33m%2 %3 %ESC%[0m
	ping -n 1 -w 200 %1 >nul 2>&1
	if %errorlevel% equ 0 (
		echo %ESC%[92m online %ESC%[0m
	) else (
		echo %ESC%[91m offline %ESC%[0m
	)
	exit /B 0

:title
	echo %ESC%[4;41;93m %1 %ESC%[0m
	exit /B 0
