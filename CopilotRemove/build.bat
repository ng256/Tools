@echo off

rem Add Dev-C++ TDM-GCC to PATH
set PATH=C:\Program Files (x86)\Embarcadero\Dev-Cpp\TDM-GCC-64\bin;%PATH%

echo Compiling...
gcc remove_copilot.c -Os -s -ffunction-sections -fdata-sections -static -Wl,--gc-sections -lshlwapi -ladvapi32 -lshell32 -o remove_copilot.exe

echo Checking dependencies (imported DLLs):
objdump -p remove_copilot.exe | find "DLL Name"

echo Stripping and compressing executable...
strip remove_copilot.exe
upx --best remove_copilot.exe

echo.
echo Build finished.
pause
