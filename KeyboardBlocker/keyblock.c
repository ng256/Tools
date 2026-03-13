/*
 * Keyboard Blocker - Windows application to block keyboard input
 * Copyright (c) 2025 Pavel Bashkardin
 *
 * This file is part of Keyboard Blocker project.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 * Description: Main source file for Keyboard Blocker application.
 *              Implements window procedure, low-level keyboard hook,
 *              tray icon management, balloon notifications, and
 *              inter-process communication via mutex and event.
 *              Includes runtime removal of white background from tray icon.
 */

#include "keyblock.h"
#include "resource.h"   // for icon IDs (IDI_ICON16, IDI_ICON32)

#pragma comment(linker, "/SUBSYSTEM:WINDOWS")

// Global variables
HINSTANCE       g_hInst;
HANDLE          g_hMutex = NULL;
HANDLE          g_hStopEvent = NULL;
HANDLE          g_hWatchThread = NULL;
HHOOK           g_hHook = NULL;
HWND            g_hwnd = NULL;
NOTIFYICONDATA  g_nid = {0};

//-------------------------------------------------------------------
// Entry point
//-------------------------------------------------------------------
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
    g_hInst = hInstance;

    g_hMutex = CreateMutexA(NULL, FALSE, MUTEX_NAME);
    if (g_hMutex == NULL)
        return 1;

	
    // ========== SECOND INSTANCE ==========
	if (GetLastError() == ERROR_ALREADY_EXISTS)
    {
        // Signal the first one to stop
        HANDLE hEvent = OpenEventA(EVENT_MODIFY_STATE, FALSE, EVENT_NAME);
        if (hEvent)
        {
            SetEvent(hEvent);
            CloseHandle(hEvent);
        }
        CloseHandle(g_hMutex);
        return 0;
    }

    // ========== FIRST INSTANCE ==========
    g_hStopEvent = CreateEventA(NULL, TRUE, FALSE, EVENT_NAME);
    if (!g_hStopEvent)
    {
        CloseHandle(g_hMutex);
        return 1;
    }

    WNDCLASSEXA wc = {0};
    wc.cbSize        = sizeof(WNDCLASSEXA);
    wc.lpfnWndProc   = WindowProc;
    wc.hInstance     = g_hInst;
    wc.lpszClassName = "KeyboardBlockerClass";
	wc.hIcon         = LoadIcon(g_hInst, MAKEINTRESOURCE(IDI_ICON32));
	wc.hIconSm       = LoadIcon(g_hInst, MAKEINTRESOURCE(IDI_ICON16));
    if (!RegisterClassExA(&wc))
    {
        CloseHandle(g_hStopEvent);
        CloseHandle(g_hMutex);
        return 1;
    }

    g_hwnd = CreateWindowExA(0, "KeyboardBlockerClass", "KeyboardBlocker",
                             0, 0, 0, 0, 0, NULL, NULL, g_hInst, NULL);
    if (!g_hwnd)
    {
        CloseHandle(g_hStopEvent);
        CloseHandle(g_hMutex);
        return 1;
    }

    g_hWatchThread = CreateThread(NULL, 0, WatchThreadProc, NULL, 0, NULL);
    if (!g_hWatchThread)
    {
        DestroyWindow(g_hwnd);
        CloseHandle(g_hStopEvent);
        CloseHandle(g_hMutex);
        return 1;
    }

    MSG msg;
    while (GetMessage(&msg, NULL, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    return 0;
}

//-------------------------------------------------------------------
// Window procedure
//-------------------------------------------------------------------
LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch (uMsg)
    {
        case WM_CREATE:
            AddTrayIcon(hwnd);
            ShowBalloonBlocked(hwnd);
            g_hHook = SetWindowsHookExW(WH_KEYBOARD_LL, LowLevelKeyboardProc,
                                        GetModuleHandleW(NULL), 0);
            if (!g_hHook)
                PostQuitMessage(1);
            return 0;

        case WM_TRAYICON:
            if (lParam == WM_RBUTTONUP)
                ShowTrayMenu(hwnd);          // Rigth click
            else if (lParam == WM_LBUTTONUP)
                ShowBalloonBlocked(hwnd);    // Left click
            return 0;

        case WM_STOP_BLOCKING:
            // Signal from watch thread – stop the application
            ShowBalloonUnblocked(hwnd);
            DestroyWindow(hwnd);
            return 0;

        case WM_DESTROY:
            RemoveTrayIcon();
            if (g_hHook)
                UnhookWindowsHookEx(g_hHook);
            if (g_hWatchThread)
            {
                WaitForSingleObject(g_hWatchThread, 1000);
                CloseHandle(g_hWatchThread);
            }
            if (g_hStopEvent)
                CloseHandle(g_hStopEvent);
            if (g_hMutex)
                CloseHandle(g_hMutex);
            PostQuitMessage(0);
            return 0;

        case WM_QUERYENDSESSION:
            ShowBalloonUnblocked(hwnd);
            DestroyWindow(hwnd);
            return TRUE;

        default:
            return DefWindowProc(hwnd, uMsg, wParam, lParam);
    }
}

//-------------------------------------------------------------------
// Keyboard hook – blocks everything
//-------------------------------------------------------------------
LRESULT CALLBACK LowLevelKeyboardProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    if (nCode < 0)
        return CallNextHookEx(NULL, nCode, wParam, lParam);
    if (nCode == HC_ACTION)
        return 1;   // mark as handled
    return CallNextHookEx(NULL, nCode, wParam, lParam);
}

//-------------------------------------------------------------------
// Watch thread: waits for stop event
//-------------------------------------------------------------------
DWORD WINAPI WatchThreadProc(LPVOID lpParam)
{
    WaitForSingleObject(g_hStopEvent, INFINITE);
    PostMessage(g_hwnd, WM_STOP_BLOCKING, 0, 0);
    return 0;
}

//-------------------------------------------------------------------
// Add tray icon (using resource IDI_ICON16) with white background removal
//-------------------------------------------------------------------
void AddTrayIcon(HWND hwnd)
{
    g_nid.cbSize = sizeof(NOTIFYICONDATA);
    g_nid.hWnd = hwnd;
    g_nid.uID = 1;
    g_nid.uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP;
    g_nid.uCallbackMessage = WM_TRAYICON;

    // Load original icon from resources
    HICON hOriginalIcon = (HICON)LoadImage(g_hInst, MAKEINTRESOURCE(IDI_ICON16),
                                           IMAGE_ICON, 16, 16, LR_DEFAULTCOLOR);
    if (!hOriginalIcon)
        hOriginalIcon = (HICON)LoadImage(NULL, MAKEINTRESOURCE(IDI_APPLICATION),
                                         IMAGE_ICON, 16, 16, LR_DEFAULTCOLOR);

    // Try to remove white background
    HICON hFinalIcon = RemoveWhiteBackground(hOriginalIcon);
    if (hFinalIcon)
    {
        // Successfully processed – use it and destroy original
        g_nid.hIcon = hFinalIcon;
        if (hOriginalIcon)
            DestroyIcon(hOriginalIcon);
    }
    else
    {
        // Processing failed – use original (or fallback) as is
        g_nid.hIcon = hOriginalIcon;
    }

    lstrcpyA(g_nid.szTip, "Keyboard Blocker");
    Shell_NotifyIconA(NIM_ADD, &g_nid);

    // The icon has been copied by the system; we can destroy our copy (except if it's the one we keep)
    if (g_nid.hIcon && g_nid.hIcon != hOriginalIcon)
        DestroyIcon(g_nid.hIcon);
    // If we used hOriginalIcon, it's also safe to destroy because it was copied.
    if (hOriginalIcon && hOriginalIcon != g_nid.hIcon)
        DestroyIcon(hOriginalIcon);
}

//-------------------------------------------------------------------
// Remove tray icon
//-------------------------------------------------------------------
void RemoveTrayIcon()
{
    g_nid.uFlags = 0;
    Shell_NotifyIconA(NIM_DELETE, &g_nid);
}

//-------------------------------------------------------------------
// Show balloon information
//-------------------------------------------------------------------
void ShowBalloonMessage(HWND hwnd, const char* title, const char* text, DWORD infoFlags)
{
    NOTIFYICONDATAA nid = {0};
    nid.cbSize = sizeof(NOTIFYICONDATAA);
    nid.hWnd = hwnd;
    nid.uID = 1;
    nid.uFlags = NIF_INFO;

    lstrcpyA(nid.szInfoTitle, title);
    lstrcpyA(nid.szInfo, text);
    nid.dwInfoFlags = infoFlags;
    nid.uTimeout = 10000;

    Shell_NotifyIconA(NIM_MODIFY, &nid);
}

//-------------------------------------------------------------------
// Show block notification
//-------------------------------------------------------------------
void ShowBalloonBlocked(HWND hwnd)
{
    ShowBalloonMessage(hwnd, "Keyboard Blocker",
                       "Keyboard is blocked.\n"
                       "Run the app again or right-click the icon and select Exit to unblock.",
                       NIIF_WARNING);
}

//-------------------------------------------------------------------
// Show unblock notification
//-------------------------------------------------------------------
void ShowBalloonUnblocked(HWND hwnd)
{
    ShowBalloonMessage(hwnd, "Keyboard Blocker",
                       "Keyboard is now unblocked.",
                       NIIF_INFO);
}

//-------------------------------------------------------------------
// Context menu on right click
//-------------------------------------------------------------------
void ShowTrayMenu(HWND hwnd)
{
    HMENU hMenu = CreatePopupMenu();
    AppendMenuA(hMenu, MF_STRING, 1, "Exit");

    POINT pt;
    GetCursorPos(&pt);
    SetForegroundWindow(hwnd);

    int cmd = TrackPopupMenu(hMenu, TPM_RETURNCMD | TPM_NONOTIFY,
                              pt.x, pt.y, 0, hwnd, NULL);
    if (cmd == 1)
        DestroyWindow(hwnd);

    DestroyMenu(hMenu);
}

//-------------------------------------------------------------------
// Remove white background from a 32-bit icon using a heuristic that
// only removes white pixels connected to the image border.
// Returns a new icon if successful, otherwise NULL.
//-------------------------------------------------------------------
HICON RemoveWhiteBackground(HICON hIcon)
{
    if (!hIcon)
        return NULL;

    // Get icon information
    ICONINFO iconInfo;
    if (!GetIconInfo(hIcon, &iconInfo))
        return NULL;

    // Work only with the color bitmap
    BITMAP bmp;
    if (!GetObject(iconInfo.hbmColor, sizeof(BITMAP), &bmp))
    {
        DeleteObject(iconInfo.hbmMask);
        DeleteObject(iconInfo.hbmColor);
        return NULL;
    }

    // We need a 32-bit bitmap to manipulate alpha channel
    if (bmp.bmBitsPixel != 32)
    {
        // Not 32-bit – cannot process easily, return NULL
        DeleteObject(iconInfo.hbmMask);
        DeleteObject(iconInfo.hbmColor);
        return NULL;
    }

    HDC hdcScreen = GetDC(NULL);
    HDC hdcMem = CreateCompatibleDC(hdcScreen);
    HBITMAP hbmNew = CreateCompatibleBitmap(hdcScreen, bmp.bmWidth, bmp.bmHeight);
    HBITMAP hbmOld = (HBITMAP)SelectObject(hdcMem, hbmNew);

    // Draw the original icon onto the new bitmap
    DrawIconEx(hdcMem, 0, 0, hIcon, bmp.bmWidth, bmp.bmHeight, 0, NULL, DI_NORMAL);

    // Prepare BITMAPINFO for GetDIBits/SetDIBits
    BITMAPINFO bmi = {0};
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bmi.bmiHeader.biWidth = bmp.bmWidth;
    bmi.bmiHeader.biHeight = -bmp.bmHeight; // top-down
    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32;
    bmi.bmiHeader.biCompression = BI_RGB;

    // Allocate memory for pixel data (using HeapAlloc instead of malloc)
    HANDLE hHeap = GetProcessHeap();
    DWORD* pixels = (DWORD*)HeapAlloc(hHeap, 0, bmp.bmWidth * bmp.bmHeight * 4);
    if (!pixels)
    {
        SelectObject(hdcMem, hbmOld);
        DeleteDC(hdcMem);
        ReleaseDC(NULL, hdcScreen);
        DeleteObject(hbmNew);
        DeleteObject(iconInfo.hbmMask);
        DeleteObject(iconInfo.hbmColor);
        return NULL;
    }

    // Get pixel bits
    if (!GetDIBits(hdcMem, hbmNew, 0, bmp.bmHeight, pixels, &bmi, DIB_RGB_COLORS))
    {
        HeapFree(hHeap, 0, pixels);
        SelectObject(hdcMem, hbmOld);
        DeleteDC(hdcMem);
        ReleaseDC(NULL, hdcScreen);
        DeleteObject(hbmNew);
        DeleteObject(iconInfo.hbmMask);
        DeleteObject(iconInfo.hbmColor);
        return NULL;
    }

    // -----------------------------------------------------------------
    // Heuristic: mark white pixels connected to the image border.
    // We'll use a simple BFS flood fill from border white pixels.
    // -----------------------------------------------------------------
    int width = bmp.bmWidth;
    int height = bmp.bmHeight;
    int totalPixels = width * height;

    // Helper macro to test for "white" (all components > 240)
    #define IS_WHITE(color) (GetRValue(color) > 240 && GetGValue(color) > 240 && GetBValue(color) > 240)

    // Visited array (1 = background white pixel)
    BYTE* visited = (BYTE*)HeapAlloc(hHeap, HEAP_ZERO_MEMORY, totalPixels);
    if (!visited)
    {
        HeapFree(hHeap, 0, pixels);
        SelectObject(hdcMem, hbmOld);
        DeleteDC(hdcMem);
        ReleaseDC(NULL, hdcScreen);
        DeleteObject(hbmNew);
        DeleteObject(iconInfo.hbmMask);
        DeleteObject(iconInfo.hbmColor);
        return NULL;
    }

    // Queue for BFS (store pixel indices)
    int* queue = (int*)HeapAlloc(hHeap, 0, totalPixels * sizeof(int));
    if (!queue)
    {
        HeapFree(hHeap, 0, visited);
        HeapFree(hHeap, 0, pixels);
        SelectObject(hdcMem, hbmOld);
        DeleteDC(hdcMem);
        ReleaseDC(NULL, hdcScreen);
        DeleteObject(hbmNew);
        DeleteObject(iconInfo.hbmMask);
        DeleteObject(iconInfo.hbmColor);
        return NULL;
    }

    int head = 0, tail = 0;

    // Enqueue all border pixels that are white
    // Top row (y = 0)
    for (int x = 0; x < width; x++)
    {
        int idx = x;
        if (!visited[idx] && IS_WHITE(pixels[idx]))
        {
            visited[idx] = 1;
            queue[tail++] = idx;
        }
    }
    // Bottom row (y = height-1)
    for (int x = 0; x < width; x++)
    {
        int idx = (height - 1) * width + x;
        if (!visited[idx] && IS_WHITE(pixels[idx]))
        {
            visited[idx] = 1;
            queue[tail++] = idx;
        }
    }
    // Left column (x = 0), excluding corners already processed
    for (int y = 1; y < height - 1; y++)
    {
        int idx = y * width;
        if (!visited[idx] && IS_WHITE(pixels[idx]))
        {
            visited[idx] = 1;
            queue[tail++] = idx;
        }
    }
    // Right column (x = width-1), excluding corners already processed
    for (int y = 1; y < height - 1; y++)
    {
        int idx = y * width + (width - 1);
        if (!visited[idx] && IS_WHITE(pixels[idx]))
        {
            visited[idx] = 1;
            queue[tail++] = idx;
        }
    }

    // BFS with 4‑connectivity
    int dx[4] = { -1, 1, 0, 0 };
    int dy[4] = { 0, 0, -1, 1 };

    while (head < tail)
    {
        int idx = queue[head++];
        int x = idx % width;
        int y = idx / width;

        for (int dir = 0; dir < 4; dir++)
        {
            int nx = x + dx[dir];
            int ny = y + dy[dir];
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                int nidx = ny * width + nx;
                if (!visited[nidx] && IS_WHITE(pixels[nidx]))
                {
                    visited[nidx] = 1;
                    queue[tail++] = nidx;
                }
            }
        }
    }

    // Now set alpha to 0 for all visited (background) pixels
    for (int i = 0; i < totalPixels; i++)
    {
        if (visited[i])
        {
            pixels[i] &= 0x00FFFFFF; // zero alpha
        }
    }

    // Free temporary BFS structures
    HeapFree(hHeap, 0, queue);
    HeapFree(hHeap, 0, visited);

    // Undefine helper macro (cleanup)
    #undef IS_WHITE

    // Write back the modified pixels
    if (!SetDIBits(hdcMem, hbmNew, 0, height, pixels, &bmi, DIB_RGB_COLORS))
    {
        HeapFree(hHeap, 0, pixels);
        SelectObject(hdcMem, hbmOld);
        DeleteDC(hdcMem);
        ReleaseDC(NULL, hdcScreen);
        DeleteObject(hbmNew);
        DeleteObject(iconInfo.hbmMask);
        DeleteObject(iconInfo.hbmColor);
        return NULL;
    }

    HeapFree(hHeap, 0, pixels);

    // Create a new icon from the modified bitmap.
    // For 32‑bit icons the mask should be a monochrome bitmap with all bits zero
    // to allow the alpha channel to control transparency.
    // We create a new mask (all black) and discard the original one.
    HBITMAP hbmNewMask = CreateBitmap(width, height, 1, 1, NULL);
    if (!hbmNewMask)
    {
        SelectObject(hdcMem, hbmOld);
        DeleteDC(hdcMem);
        ReleaseDC(NULL, hdcScreen);
        DeleteObject(hbmNew);
        DeleteObject(iconInfo.hbmMask);
        DeleteObject(iconInfo.hbmColor);
        return NULL;
    }

    ICONINFO newInfo = {0};
    newInfo.fIcon = iconInfo.fIcon;
    newInfo.xHotspot = iconInfo.xHotspot;
    newInfo.yHotspot = iconInfo.yHotspot;
    newInfo.hbmMask = hbmNewMask;      // new all‑black mask
    newInfo.hbmColor = hbmNew;

    HICON hNewIcon = CreateIconIndirect(&newInfo);

    // Cleanup
    SelectObject(hdcMem, hbmOld);
    DeleteDC(hdcMem);
    ReleaseDC(NULL, hdcScreen);
    DeleteObject(hbmNew);
    DeleteObject(hbmNewMask);
    DeleteObject(iconInfo.hbmMask);
    DeleteObject(iconInfo.hbmColor);

    return hNewIcon;
}