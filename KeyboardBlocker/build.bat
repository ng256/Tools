@echo off
setlocal enabledelayedexpansion

REM ============================================================================
REM Keyboard Blocker - Build script for Windows
REM Copyright (c) 2025 Pavel Bashkardin
REM
REM This file is part of Keyboard Blocker project.
REM
REM Permission is hereby granted, free of charge, to any person obtaining a copy
REM of this software and associated documentation files (the "Software"), to deal
REM in the Software without restriction, including without limitation the rights
REM to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
REM copies of the Software, and to permit persons to whom the Software is
REM furnished to do so, subject to the following conditions:
REM
REM The above copyright notice and this permission notice shall be included in all
REM copies or substantial portions of the Software.
REM
REM THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
REM IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
REM FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
REM AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
REM LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
REM OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
REM SOFTWARE.
REM
REM Description: Build script for Keyboard Blocker application.
REM              Compiles resources, C source, links with optimizations
REM              for minimal size, displays imported DLLs, and optionally
REM              applies UPX compression.
REM ============================================================================

REM ------------------------------------------------------------
REM Configuration
REM ------------------------------------------------------------
set PROJECT_NAME=keyblock
set RC_FILE=resource.rc
set C_FILE=keyblock.c

REM Derive object file names from source files
set RC_OBJ=%RC_FILE:.rc=.o%
set C_OBJ=%C_FILE:.c=.o%
set EXE_FILE=%PROJECT_NAME%.exe

REM Path to GCC bin folder (adjust if necessary)
set PATH=C:\TDM-GCC-64\bin;%PATH%

REM ------------------------------------------------------------
REM Clean previous build files
REM ------------------------------------------------------------
echo Cleaning previous build files...
if exist %C_OBJ% del %C_OBJ%
if exist %RC_OBJ% del %RC_OBJ%
if exist %EXE_FILE% del %EXE_FILE%

REM ------------------------------------------------------------
REM Compile resources
REM ------------------------------------------------------------
echo Compiling resources...
windres -i %RC_FILE% -o %RC_OBJ% --input-format=rc -O coff -F pe-i386
if errorlevel 1 goto error

REM ------------------------------------------------------------
REM Compile C source
REM ------------------------------------------------------------
echo Compiling %C_FILE%...
gcc -m32 -Os -s -static -static-libgcc -ffunction-sections -fdata-sections -mwindows -I. -c %C_FILE% -o %C_OBJ%
if errorlevel 1 goto error

REM ------------------------------------------------------------
REM Link
REM ------------------------------------------------------------
echo Linking...
gcc -m32 -Os -s -static -static-libgcc -ffunction-sections -fdata-sections -mwindows %C_OBJ% %RC_OBJ% -o %EXE_FILE% -Wl,--gc-sections
if errorlevel 1 goto error

REM ------------------------------------------------------------
REM Stripping
REM ------------------------------------------------------------
echo Stripping executable...
strip %EXE_FILE%
if errorlevel 1 goto error

REM ------------------------------------------------------------
REM Show imported DLLs
REM ------------------------------------------------------------
echo Checking dependencies (imported DLLs):
objdump -p %EXE_FILE% | find "DLL Name"
if errorlevel 1 goto error

REM ------------------------------------------------------------
REM Optional UPX compression
REM ------------------------------------------------------------
where upx >nul 2>nul
if errorlevel 1 (
    echo UPX not found, skipping compression.
) else (
    echo Applying UPX compression...
    upx --best %EXE_FILE%
    if errorlevel 1 (
        echo UPX compression failed.
    ) else (
        echo UPX compression completed.
    )
)

REM ------------------------------------------------------------
REM Clean build files
REM ------------------------------------------------------------
echo Cleaning build files...
if exist %C_OBJ% del %C_OBJ%
if exist %RC_OBJ% del %RC_OBJ%

echo Done. Output file: %EXE_FILE%
goto end

:error
echo Compilation failed!

:end
pause