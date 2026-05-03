#include <windows.h>
#include <commctrl.h>
#include <shlobj.h>
#include "unzip.h"
#include "resource.h"
#include "ini.h"

static HWND hPathEdit, hStatusEdit;
static HANDLE g_heap; 

typedef struct {
    wchar_t* licenseText;
    wchar_t* buttonAccept;
    wchar_t* buttonDecline;
    wchar_t* buttonExtract;
    wchar_t* buttonBrowse;
    wchar_t* windowTitle;
    wchar_t* postInstallCmd;
    wchar_t* postInstallArgs;
    wchar_t* msgExtractStart;
    wchar_t* msgExtractDone;
    wchar_t* msgExtractFail;
    wchar_t* msgReady;
    wchar_t* browseTitle;
    wchar_t* labelFolder;
    wchar_t* defaultPath;
} SFXStrings;

static SFXStrings g_strings;

static void FreeStrings(void) {
    #define FREE_STR(field) if (g_strings.field) { HeapFree(g_heap, 0, g_strings.field); g_strings.field = NULL; }
	
    FREE_STR(licenseText);
    FREE_STR(buttonAccept);
    FREE_STR(buttonDecline);
    FREE_STR(buttonExtract);
    FREE_STR(buttonBrowse);
    FREE_STR(windowTitle);
    FREE_STR(postInstallCmd);
    FREE_STR(postInstallArgs);
    FREE_STR(msgExtractStart);
    FREE_STR(msgExtractDone);
    FREE_STR(msgExtractFail);
    FREE_STR(msgReady);
    FREE_STR(browseTitle);
    FREE_STR(labelFolder);
    FREE_STR(defaultPath);
	
    #undef FREE_STR
}

static wchar_t* CopyString(const wchar_t* src) {
    if (!src) return NULL;
    size_t len = lstrlenW(src);
    wchar_t* dst = (wchar_t*)HeapAlloc(g_heap, 0, (len + 1) * sizeof(wchar_t));
    if (dst) lstrcpyW(dst, src);
    return dst;
}

static void LoadStringsFromIni(IniFile* ini) {
    #define GET_VALUE(field, section, key, def) do { \
        const wchar_t* val = IniGetValue(ini, section, key); \
        g_strings.field = CopyString(val ? val : def); \
    } while(0)

    // ----- License -----
    GET_VALUE(licenseText,      L"License",     L"Text",           NULL);

    // ----- UI -----
    GET_VALUE(buttonAccept,     L"UI",          L"ButtonAccept",   L"Accept");
    GET_VALUE(buttonDecline,    L"UI",          L"ButtonDecline",  L"Decline");
    GET_VALUE(buttonExtract,    L"UI",          L"ButtonExtract",  L"Extract");
    GET_VALUE(buttonBrowse,     L"UI",          L"ButtonBrowse",   L"Browse...");
    GET_VALUE(windowTitle,      L"UI",          L"WindowTitle",    L"SFX Unzip");
    GET_VALUE(labelFolder,      L"UI",          L"LabelFolder",    L"Extract to folder:");
    GET_VALUE(browseTitle,      L"UI",          L"BrowseTitle",    L"Select destination folder");

    // ----- Paths -----
    GET_VALUE(defaultPath,      L"Paths",       L"DefaultPath",    L"");

    // ----- Messages -----
    GET_VALUE(msgReady,         L"Messages",    L"MsgReady",       L"Ready to extract.\r\nSelect a folder and click 'Extract'.\r\n");
    GET_VALUE(msgExtractStart,  L"Messages",    L"MsgExtractStart",L"Extraction started...\r\n");
    GET_VALUE(msgExtractDone,   L"Messages",    L"MsgExtractDone", L"\r\nDone! All files extracted successfully.\r\n");
    GET_VALUE(msgExtractFail,   L"Messages",    L"MsgExtractFail", L"\r\nFailed to extract archive.\r\n");

    // ----- PostInstall -----
    GET_VALUE(postInstallCmd,   L"PostInstall", L"Command",        NULL);
    GET_VALUE(postInstallArgs,  L"PostInstall", L"Arguments",      L"");

    #undef GET_VALUE
}

static inline BOOL IsSpace(wchar_t c) {
    return c == L' ' || c == L'\t';
}

void AppendLog(const wchar_t* text) {
    SendMessageW(hStatusEdit, EM_SETSEL, (WPARAM)-1, (LPARAM)-1);
    SendMessageW(hStatusEdit, EM_REPLACESEL, FALSE, (LPARAM)text);
}

static int CALLBACK BrowseCallbackProc(HWND hwnd, UINT uMsg, LPARAM lParam, LPARAM lpData) {
    if (uMsg == BFFM_INITIALIZED) {
        SendMessage(hwnd, BFFM_SETSELECTION, TRUE, lpData);
    }
    return 0;
}

void BrowseFolder(HWND hwndParent) {
    wchar_t path[MAX_PATH];
    GetWindowTextW(hPathEdit, path, MAX_PATH);

    BROWSEINFOW bi = {0};
    bi.hwndOwner = hwndParent;
    bi.lpszTitle = g_strings.browseTitle;
    bi.ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE;
    bi.lpfn = BrowseCallbackProc;
    bi.lParam = (LPARAM)path;

    LPITEMIDLIST pidl = SHBrowseForFolderW(&bi);
    if (pidl != NULL) {
        if (SHGetPathFromIDListW(pidl, path)) {
            SetWindowTextW(hPathEdit, path);
        }
        CoTaskMemFree(pidl);
    }
}

static BOOL CreateDirectoryRecursive(LPCWSTR path) {
    if (CreateDirectoryW(path, NULL)) return TRUE;
    if (GetLastError() == ERROR_ALREADY_EXISTS) return TRUE;
    wchar_t parent[MAX_PATH];
    lstrcpyW(parent, path);
    wchar_t* last = wcsrchr(parent, L'\\');
    if (last) {
        *last = 0;
        if (!CreateDirectoryRecursive(parent)) return FALSE;
        return CreateDirectoryW(path, NULL) || GetLastError() == ERROR_ALREADY_EXISTS;
    }
    return FALSE;
}

DWORD WINAPI ExtractThread(LPVOID lpParam) {
    HWND hwndParent = (HWND)lpParam;
    wchar_t exePath[MAX_PATH];
    GetModuleFileNameW(NULL, exePath, MAX_PATH);

    wchar_t destDir[MAX_PATH];
    GetWindowTextW(hPathEdit, destDir, MAX_PATH);

    SetWindowTextW(hStatusEdit, L"");
    AppendLog(g_strings.msgExtractStart);

    BOOL result = ExtractFromSFX(exePath, destDir, TRUE, hStatusEdit);

    if (result) {
        AppendLog(g_strings.msgExtractDone);

        if (g_strings.postInstallCmd && *g_strings.postInstallCmd) {
            wchar_t cmdLine[MAX_PATH * 2];
            const wchar_t* args = g_strings.postInstallArgs ? g_strings.postInstallArgs : L"";
            
            if (*args) {
                wsprintfW(cmdLine, L"\"%s\" %s", g_strings.postInstallCmd, args);
            } else {
                wsprintfW(cmdLine, L"\"%s\"", g_strings.postInstallCmd);
            }
            
            STARTUPINFOW si = { sizeof(si) };
            PROCESS_INFORMATION pi;
            if (CreateProcessW(NULL, cmdLine, NULL, NULL, FALSE, 0, NULL, destDir, &si, &pi)) {
                CloseHandle(pi.hProcess);
                CloseHandle(pi.hThread);
            } else {
                wchar_t errMsg[512];
                wsprintfW(errMsg, L"Failed to launch post-install script:\n%s\nError code: %lu", 
                          g_strings.postInstallCmd, GetLastError());
                MessageBoxW(hwndParent, errMsg, g_strings.windowTitle, MB_OK | MB_ICONWARNING);
            }
        }
        
        MessageBoxW(hwndParent, g_strings.msgExtractDone, g_strings.windowTitle, MB_OK | MB_ICONINFORMATION);
		ExitProcess(0);
        
    } else {
        AppendLog(g_strings.msgExtractFail);
        MessageBoxW(hwndParent, g_strings.msgExtractFail, g_strings.windowTitle, MB_OK | MB_ICONERROR);
    }

    EnableWindow(GetDlgItem(hwndParent, IDC_EXTRACT), TRUE);
    return 0;
}

static void CenterWindow(HWND hwnd) {
    RECT rc, rcParent;
    HWND hwndParent = GetParent(hwnd);
    if (hwndParent) {
        GetWindowRect(hwndParent, &rcParent);
    } else {
        rcParent.left = 0; rcParent.top = 0;
        rcParent.right = GetSystemMetrics(SM_CXSCREEN);
        rcParent.bottom = GetSystemMetrics(SM_CYSCREEN);
    }
    GetWindowRect(hwnd, &rc);
    int width = rc.right - rc.left;
    int height = rc.bottom - rc.top;
    int x = rcParent.left + (rcParent.right - rcParent.left - width) / 2;
    int y = rcParent.top + (rcParent.bottom - rcParent.top - height) / 2;
    SetWindowPos(hwnd, NULL, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
}


static INT_PTR CALLBACK LicenseDlgProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {
        case WM_INITDIALOG: {
            SetWindowTextW(hwnd, g_strings.windowTitle);
            SetDlgItemTextW(hwnd, IDC_ACCEPT, g_strings.buttonAccept);
            SetDlgItemTextW(hwnd, IDC_DECLINE, g_strings.buttonDecline);
            SetDlgItemTextW(hwnd, IDC_LICENSE_TEXT, g_strings.licenseText ? g_strings.licenseText : L"(No license text provided)");
            CenterWindow(hwnd);
            return TRUE;
        }
        case WM_COMMAND:
            switch (LOWORD(wParam)) {
                case IDC_ACCEPT:
                    EndDialog(hwnd, IDOK);
                    return TRUE;
                case IDC_DECLINE:
                    EndDialog(hwnd, IDCANCEL);
                    return TRUE;
                case IDCANCEL:
                    EndDialog(hwnd, IDCANCEL);
                    return TRUE;
            }
            break;
        case WM_CLOSE:
            EndDialog(hwnd, IDCANCEL);
            return TRUE;
    }
    return FALSE;
}

INT_PTR CALLBACK DlgProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {
        case WM_INITDIALOG: {
            hPathEdit = GetDlgItem(hwnd, IDC_PATH);
            hStatusEdit = GetDlgItem(hwnd, IDC_STATUS);

            SetWindowTextW(hwnd, g_strings.windowTitle);
            SetDlgItemTextW(hwnd, IDC_STATIC, g_strings.labelFolder);
            SetDlgItemTextW(hwnd, IDC_EXTRACT, g_strings.buttonExtract);
            SetDlgItemTextW(hwnd, IDC_BROWSE, g_strings.buttonBrowse);

            wchar_t curDir[MAX_PATH];
            if (g_strings.defaultPath && *g_strings.defaultPath) {
                lstrcpyW(curDir, g_strings.defaultPath);
                CreateDirectoryRecursive(curDir);
            } else {
                GetCurrentDirectoryW(MAX_PATH, curDir);
            }
            SetWindowTextW(hPathEdit, curDir);

            SendMessageW(hStatusEdit, EM_SETREADONLY, TRUE, 0);
            AppendLog(g_strings.msgReady);
            CenterWindow(hwnd);
            return TRUE;
        }
        case WM_COMMAND:
            switch (LOWORD(wParam)) {
                case IDC_BROWSE:
                    BrowseFolder(hwnd);
                    return TRUE;
                case IDC_EXTRACT: {
                    EnableWindow(GetDlgItem(hwnd, IDC_EXTRACT), FALSE);
                    HANDLE hThread = CreateThread(NULL, 0, ExtractThread, hwnd, 0, NULL);
                    if (hThread) CloseHandle(hThread);
                    return TRUE;
                }
                case IDCANCEL:
                    DestroyWindow(hwnd);
                    return TRUE;
            }
            break;
        case WM_CLOSE:
            DestroyWindow(hwnd);
            return TRUE;
        case WM_DESTROY:
            PostQuitMessage(0);
            return TRUE;
    }
    return FALSE;
}

int WINAPI wWinMain(HINSTANCE hInst, HINSTANCE hPrevInstance, LPWSTR lpCmdLine, int nShow) {
    g_heap = GetProcessHeap();
    (void)hPrevInstance;
    (void)lpCmdLine;
    (void)nShow;

    // Get comment from EXE
    wchar_t exePath[MAX_PATH];
    GetModuleFileNameW(NULL, exePath, MAX_PATH);
    wchar_t* comment = NULL;
    GetZipComment(exePath, &comment, g_heap);

    // Prepare INI text: if no sections, add [SFX]
    wchar_t* iniText = NULL;
    if (comment) {
        BOOL hasSection = FALSE;
        const wchar_t* p = comment;
        while (*p) {
            const wchar_t* eol = wcschr(p, L'\n');
            if (!eol) eol = p + lstrlenW(p);
            const wchar_t* trimmed = p;
            while (trimmed < eol && IsSpace(*trimmed)) trimmed++;
            if (trimmed < eol && *trimmed == L'[') {
                hasSection = TRUE;
                break;
            }
            p = (*eol == L'\n') ? eol + 1 : eol;
        }
        if (hasSection) {
            iniText = (wchar_t*)HeapAlloc(g_heap, 0, (lstrlenW(comment) + 1) * sizeof(wchar_t));
            if (iniText) lstrcpyW(iniText, comment);
        } else {
            const wchar_t* prefix = L"[SFX]\r\n";
            size_t newLen = lstrlenW(prefix) + lstrlenW(comment) + 1;
            iniText = (wchar_t*)HeapAlloc(g_heap, 0, newLen * sizeof(wchar_t));
            if (iniText) {
                lstrcpyW(iniText, prefix);
                lstrcatW(iniText, comment);
            }
        }
    } else {
        iniText = (wchar_t*)HeapAlloc(g_heap, 0, sizeof(L"[SFX]\r\n"));
        if (iniText) lstrcpyW(iniText, L"[SFX]\r\n");
    }

    // Parse INI
    IniFile* ini = NULL;
    if (iniText) {
        DWORD flags = INI_FLAG_UNESCAPE | INI_FLAG_EXPAND_ENV;
        ini = IniParse(iniText, -1, g_heap, flags);
        HeapFree(g_heap, 0, iniText);
    }

    // Load strings (with defaults)
    LoadStringsFromIni(ini);
    if (ini) IniFree(ini);
    if (comment) HeapFree(g_heap, 0, comment);

    // License agreement
    BOOL licenseAccepted = TRUE;
    if (g_strings.licenseText && *g_strings.licenseText) {
        INT_PTR res = DialogBox(hInst, MAKEINTRESOURCE(IDD_LICENSE), NULL, LicenseDlgProc);
        if (res != IDOK) licenseAccepted = FALSE;
    }

    if (!licenseAccepted) {
        FreeStrings();
        return 0;
    }

    INITCOMMONCONTROLSEX icc = {sizeof(icc), ICC_STANDARD_CLASSES};
    InitCommonControlsEx(&icc);

    DialogBox(hInst, MAKEINTRESOURCE(IDD_MAIN), NULL, DlgProc);

    MSG msg;
    while (GetMessage(&msg, NULL, 0, 0)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    FreeStrings();
    return (int)msg.wParam;
}
