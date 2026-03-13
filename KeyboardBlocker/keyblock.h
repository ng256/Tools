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
 * Description: Header file for Keyboard Blocker application. Declares constants,
 *              global variables, and function prototypes used for keyboard blocking,
 *              tray icon management, inter-process communication, and white background
 *              removal from icons.
 */

#ifndef KEYBOARDBLOCKER_H
#define KEYBOARDBLOCKER_H

#include <windows.h>
#include <shellapi.h>

//-------------------------------------------------------------------
// Constants
//-------------------------------------------------------------------
#define MUTEX_NAME          "KeyboardBlockerMutex"
#define EVENT_NAME          "KeyboardBlockerStopEvent"
#define WM_TRAYICON         (WM_APP + 1)
#define WM_STOP_BLOCKING    (WM_APP + 2)

//-------------------------------------------------------------------
// Global variables (defined in keyboardblocker.c)
//-------------------------------------------------------------------
extern HINSTANCE       g_hInst;
extern HANDLE          g_hMutex;
extern HANDLE          g_hStopEvent;
extern HANDLE          g_hWatchThread;
extern HHOOK           g_hHook;
extern HWND            g_hwnd;
extern NOTIFYICONDATA  g_nid;

//-------------------------------------------------------------------
// Function prototypes
//-------------------------------------------------------------------
LRESULT CALLBACK WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
LRESULT CALLBACK LowLevelKeyboardProc(int nCode, WPARAM wParam, LPARAM lParam);
DWORD WINAPI     WatchThreadProc(LPVOID lpParam);
void             AddTrayIcon(HWND hwnd);
void             RemoveTrayIcon(void);
void             ShowTrayMenu(HWND hwnd);
void             ShowBalloonMessage(HWND hwnd, const char* title, const char* text, DWORD infoFlags);
void             ShowBalloonBlocked(HWND hwnd);
void             ShowBalloonUnblocked(HWND hwnd);
HICON            RemoveWhiteBackground(HICON hIcon);

#endif // KEYBOARDBLOCKER_H