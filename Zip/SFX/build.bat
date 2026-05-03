@echo off
set PATH=C:\TDM-GCC-64\bin;%PATH%

where gcc >nul 2>nul
if errorlevel 1 (
    echo GCC not found. Please check the PATH.
    pause
    exit /b 1
)

set CFLAGS=-mwindows -municode -Os -s -ffunction-sections -fdata-sections -static
set LDFLAGS=-Wl,--gc-sections -lkernel32 -luser32 -lgdi32 -lcomctl32 -lcomdlg32 -lshell32 -lole32
set OUT=sfx.exe

echo Building sfx...

if exist resource.rc (
    echo Compiling resources...
    windres resource.rc -o resource.o
    if errorlevel 1 (
        echo Resource compilation failed.
        pause
        exit /b 1
    )
    set OBJS=main.c unzip.c ini.c resource.o
) else (
    set OBJS=main.c unzip.c ini.c
)

gcc %CFLAGS% -o %OUT% %OBJS% %LDFLAGS%
if errorlevel 1 (
    echo Compilation failed.
    if exist resource.o del resource.o
    pause
    exit /b 1
)

if exist resource.o del resource.o
echo Compilation succeeded.

where upx >nul 2>nul
if errorlevel 1 (
    echo UPX not found, skipping compression.
) else (
    echo Compressing with UPX...
    upx --best --lzma %OUT%
)

echo Done.
pause
