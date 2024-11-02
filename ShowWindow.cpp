// ShowWindow — A simple application that demonstrates how to use 
// the ShowWindow function in the Windows API using shellcode.
//
// NOTICE: you should compile it for X86 target platform without optimization!
//
// This program is based on a project by Ege BalcA+, which can be found here: 
// https://exploit.kitploit.com/2017/03/windows-x86-hide-console-window.html
// 
// Copyright © NG256, 2024.
//
// MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#include <algorithm>
#include <iostream>
#include <windows.h>
#include <string>
#include <vector>

/*const DWORD MEM_COMMIT = 0x1000;
const DWORD PAGE_EXECUTE_READWRITE = 0x40;
const BYTE SW_HIDE = 0;
const BYTE SW_MINIMIZE = 6;
const BYTE SW_RESTORE = 9;
const BYTE SW_MAXIMIZE = 3;
const BYTE SW_SHOW = 5;*/

const std::vector<BYTE> shellcode = {
0x60,                           // pushad                   ; Save all registers to stack
        0x9C,                   // pushfd                   ; Save all flags to stack
        0xFC,                   // cld                      ; Clear direction flag
        0xE8,                   // call Start               ; Call the Start function
        0x82, 0x00, 0x00, 0x00, //                          ; Offset to the Start function
        0x60,                   // pop ebp                  ; Pop the address of SFHA
        0x89, 0xE5,             // mov ebp, esp             ; Set up stack frame
        0x31, 0xC0,             // xor eax, eax             ; Clear eax register
        0x64, 0x8B, 0x50, 0x30, // mov edx, fs:[0x30]       ; Get the pointer to the PEB
        0x8B, 0x52, 0x0C,       // mov edx, [edx + 0x0C]    ; Get the pointer to the LDR
        0x8B, 0x52, 0x14,       // mov edx, [edx + 0x14]    ; Get the pointer to the first module
        0x8B, 0x72, 0x28,       // mov esi, [edx + 0x28]    ; Get the pointer to the module list
        0x0F, 0xB7, 0x4A, 0x26, // movzx ecx, [edx + 0x26]  ; Get the length of the module name
        0x31, 0xFF,             // xor edi, edi             ; Clear edi register
        0xAC,                   // lodsb                    ; Load the next byte into al
        0x3C, 0x61,             // cmp al, 'a'              ; Compare with character 'a'
        0x7C, 0x02,             // jl short                 ; If less, jump
        0x2C, 0x20,             // sub al, 0x20             ; Convert to uppercase
        0xC1, 0xCF, 0x0D,       // ror ecx, 0xD             ; Rotate right
        0x01, 0xC7,             // add edi, eax             ; Add to edi
        0xE2, 0xF2,             // loop                     ; Loop until ecx is zero
        0x52,                   // push edx                 ; Push the pointer to the module name
        0x57,                   // push edi                 ; Push the length of the module name
        0x8B, 0x52, 0x10,       // mov edx, [edx + 0x10]    ; Get the address of the base
        0x8B, 0x4A, 0x3C,       // mov ecx, [edx + 0x3C]    ; Get the module's base address
        0x8B, 0x4C, 0x11, 0x78, // mov ecx, [ecx + 0x78]    ; Get the address of the entry point
        0xE3, 0x48,             // jbe short                ; Jump if below or equal
        0x01, 0xD1,             // add ecx, edx             ; Calculate the final address
        0x51,                   // push ecx                 ; Push the final address
        0x8B, 0x59, 0x20,       // mov ebx, [ecx + 0x20]    ; Get the pointer to the address of the function
        0x01, 0xD3,             // add ebx, edx             ; Add to the base address
        0x8B, 0x49, 0x18,       // mov ecx, [ecx + 0x18]    ; Get the address of the function
        0xE3, 0x3A,             // jbe short                ; Jump if below or equal
        0x49, 0x8B, 0x34, 0x8B, // mov rsi, [ebx + ecx]     ; Get the address of ShowWindow
        0x01, 0xD6,             // add esi, edx             ; Calculate final address
        0x31, 0xFF,             // xor edi, edi             ; Clear edi register
        0xAC,                   // lodsb                    ; Load the next byte into al
        0xC1, 0xCF, 0x0D,       // ror ecx, 0xD             ; Rotate right
        0x01, 0xC7,             // add edi, eax             ; Add to edi
        0x38, 0xE0,             // cmp al, 'a'              ; Compare with character 'a'
        0x75, 0xF6,             // jne short                ; If not equal, jump back
        0x03, 0x7D, 0xF8,       // add edi, [ebp + 0xF8]    ; Add to the value
        0x3B, 0x7D, 0x24,       // cmp ebp, [ebp + 0x24]    ; Compare with base pointer
        0x75, 0xE4,             // jne short                ; If not equal, jump back
        0x58,                   // pop eax                  ; Restore EAX
        0x8B, 0x58, 0x24,       // mov ebx, [esp]           ; Get the pointer to the console window
        0x01, 0xD3,             // add ebx, edx             ; Add to the base address
        0x66, 0x8B, 0x0C, 0x4B, // mov ecx, [ebx + ecx]     ; Get the address of the console window
        0x8B, 0x58, 0x1C,       // mov ebx, [ebx + 0x1C]    ; Get the address for the handle
        0x01, 0xD3,             // add ebx, edx             ; Add to the base address
        0x8B, 0x04, 0x8B,       // mov eax, [ebx]           ; Move to EAX
        0x01, 0xD0,             // add eax, edx             ; Add to the base address
        0x89, 0x44, 0x24, 0x24, // mov [esp + 0x24], eax    ; Save handle to the stack
        0x5B,                   // pop ebx                  ; Restore EBX
        0x5B,                   // pop ebx                  ; Restore EBX
        0x61,                   // popad                    ; Pop all registers
        0x59,                   // pop ecx                  ; Restore ECX
        0x5A,                   // pop edx                  ; Restore EDX
        0x51,                   // push ecx                 ; Push the final address
        0xFF, 0xE0,             // jmp eax                  ; Jump to the shellcode
        0x5F,                   // pop edi                  ; Restore EDI
        0x5F,                   // pop edi                  ; Restore EDI
        0x5A,                   // pop edx                  ; Restore EDX
        0x8B, 0x12,             // mov edx, [edx]           ; Move to EDX
        0xEB, 0x8D,             // jmp short                ; Jump to the next instruction
        0x5D,                   // pop ebp                  ; Restore EBP
        0x6A, 0x00,             // push 0x00                ; Push NULL
        0x68, 0x33, 0x32,       // push '23'                ; Push string 'user32'
        0x00, 0x00,             //                          ; null terminator
        0x68, 0x75, 0x73, 0x65, // push 'user'              ; Push string 'user'
        0x72, 0x54,             //                          ; null terminator
        0x68, 0x4C, 0x77, 0x26, // push 'kernel32'          ; Push string 'kernel32.dll'
        0x07, 0xFF,             // call                     ; Call the LoadLibrary function
        0xD5,                   //                          ; Return to caller
        0x83, 0xC4, 0x0C,       // add esp, 0x0C            ; Clean up the stack
        0x68, 0x89, 0x6E, 0x72, // push address             ; Push address of console window
        0xCE, 0xFF,             // call                     ; Call the GetConsoleWindow function
        0xD5,                   //                          ; Return to caller
        0x6A, 0x00,             // push 0x00                ; Push NULL
        0x50,                   // push EAX                 ; Push console window handle
        0x68, 0xC2, 0xEB, 0x2E, // push 'ShowWindow'        ; Push string 'ShowWindow'
        0x6E, 0xFF,             // call                     ; Call the ShowWindow function
        0xD5,                   //                          ; Return to caller
        0x9D,                   // ret                      ; Return from the shellcode
        0x61,                   // popad                    ; Pop all registers
        0xC3                    // ret                      ; Return from main function
};

void ExecuteShellcode() {
	const LPVOID buffer = VirtualAlloc(nullptr, shellcode.size(), MEM_COMMIT, PAGE_EXECUTE_READWRITE);

    if (buffer) {
        memcpy(buffer, shellcode.data(), shellcode.size());
        auto shellcode_func = reinterpret_cast<void(*)()>(buffer);
        shellcode_func();
        VirtualFree(buffer, 0, MEM_RELEASE);
    }
}

std::string GetModuleFileNameUppercase() {
    char fileName[MAX_PATH];
    const DWORD count = GetModuleFileNameA(nullptr, fileName, MAX_PATH);

    if (count > 0) {
        std::string result(fileName);
        std::transform(result.begin(), result.end(), result.begin(), ::toupper);
        return result;
    }

    return "SHWND";
}

void ShowUsage() {
	const std::string module_name = GetModuleFileNameUppercase();
    std::cout << "Usage: " << module_name << " [option]\n\n";
    std::cout << "Options:\n";
    std::cout << "  -h                Hide the console window.\n";
    std::cout << "  -m                Minimize the console window.\n";
    std::cout << "  -r                Restore the console window.\n";
    std::cout << "  -x                Maximize the console window.\n";
    std::cout << "  -s                Show the console window (default).\n\n";
    std::cout << "Example that hides the console window:\n";
    std::cout << "  " << module_name << " -h\n";
}

int main(int argc, char* argv[]) {
    if (argc > 2) {
        std::cerr << "Error: Too many parameters." << std::endl;
        ShowUsage();
        return 1;
    }

    BYTE show_command = SW_SHOW;

    if (argc == 2) {
        std::string arg = argv[1];
        if (arg.length() == 2 && (arg[0] == '-' || arg[0] == '/')) {
            char param = tolower(arg[1]);
            switch (param) {
            case '?':
                ShowUsage();
                return 0;
            case 'h':
                show_command = SW_HIDE;
                break;
            case 'm':
                show_command = SW_MINIMIZE;
                break;
            case 'r':
                show_command = SW_RESTORE;
                break;
            case 'x':
                show_command = SW_MAXIMIZE;
                break;
            case 's':
                show_command = SW_SHOW;
                break;
            default:
                std::cerr << "Error: Invalid parameter." << std::endl;
                ShowUsage();
                return 1;
            }
        }
        else {
            std::cerr << "Error: Invalid parameter." << std::endl;
            ShowUsage();
            return 1;
        }
    }

    const HWND console_window = GetConsoleWindow();
    if (console_window) {
        ShowWindow(console_window, show_command);
    }
    else {
        std::cerr << "Error: Unable to retrieve console window handle." << std::endl;
        return 1;
    }

    ExecuteShellcode();

    return 0;
}
