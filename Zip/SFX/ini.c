#include "ini.h"
#include <windows.h>

/* --------------------------------------------------------------------- */
/* Helper functions without CRT                                         */
/* --------------------------------------------------------------------- */

static HANDLE GetHeap(HANDLE heap) {
    return heap ? heap : GetProcessHeap();
}

static size_t wlen(const wchar_t* s) {
    return lstrlenW(s);
}

static wchar_t* wcpy(wchar_t* dst, const wchar_t* src) {
    return lstrcpyW(dst, src);
}

static int wcmp(const wchar_t* a, const wchar_t* b, BOOL caseSensitive) {
    if (caseSensitive)
        return lstrcmpW(a, b);
    else
        return lstrcmpiW(a, b);
}

static wchar_t* wchr(const wchar_t* s, wchar_t c) {
    while (*s) {
        if (*s == c) return (wchar_t*)s;
        s++;
    }
    return NULL;
}

static BOOL is_space(wchar_t c) {
    return c == L' ' || c == L'\t' || c == L'\r' || c == L'\n';
}

static const wchar_t* skip_spaces(const wchar_t* s) {
    while (is_space(*s)) s++;
    return s;
}

static void trim_right(wchar_t* s) {
    size_t len = wlen(s);
    while (len > 0 && is_space(s[len-1])) {
        s[--len] = L'\0';
    }
}

static wchar_t* copy_string(const wchar_t* src, HANDLE heap) {
    if (!src) return NULL;
    size_t len = wlen(src);
    wchar_t* dst = (wchar_t*)HeapAlloc(heap, 0, (len + 1) * sizeof(wchar_t));
    if (dst) wcpy(dst, src);
    return dst;
}

/* --------------------------------------------------------------------- */
/* Escape and environment variable processing                           */
/* --------------------------------------------------------------------- */

static wchar_t* unescape_string(const wchar_t* input, HANDLE heap) {
    if (!input) return NULL;
    size_t len = wlen(input);
    wchar_t* output = (wchar_t*)HeapAlloc(heap, 0, (len + 256) * sizeof(wchar_t));
    if (!output) return NULL;
    const wchar_t* src = input;
    wchar_t* dst = output;
    while (*src) {
        if (*src == L'\\') {
            src++;
            switch (*src) {
                case L'n': *dst++ = L'\n'; src++; break;
                case L'r': *dst++ = L'\r'; src++; break;
                case L't': *dst++ = L'\t'; src++; break;
                case L'\\': *dst++ = L'\\'; src++; break;
                case L'"': *dst++ = L'"'; src++; break;
                case L'\'': *dst++ = L'\''; src++; break;
                case L'0': *dst++ = L'\0'; src++; break;
                case L'D': {
                    SYSTEMTIME st;
                    GetLocalTime(&st);
                    wsprintfW(dst, L"%04d-%02d-%02d", st.wYear, st.wMonth, st.wDay);
                    dst += wlen(dst);
                    src++;
                    break;
                }
                case L'T': {
                    SYSTEMTIME st;
                    GetLocalTime(&st);
                    wsprintfW(dst, L"%02d:%02d:%02d", st.wHour, st.wMinute, st.wSecond);
                    dst += wlen(dst);
                    src++;
                    break;
                }
                default:
                    *dst++ = L'\\';
                    *dst++ = *src++;
                    break;
            }
        } else {
            *dst++ = *src++;
        }
    }
    *dst = L'\0';
    wchar_t* result = copy_string(output, heap);
    HeapFree(heap, 0, output);
    return result;
}

/* Expand environment variables %VAR% */
static wchar_t* expand_env(const wchar_t* input, HANDLE heap) {
    if (!input) return NULL;
    size_t len = wlen(input);
    wchar_t* output = (wchar_t*)HeapAlloc(heap, 0, (len + 32767) * sizeof(wchar_t));
    if (!output) return NULL;
    const wchar_t* src = input;
    wchar_t* dst = output;
    while (*src) {
        if (*src == L'%') {
            const wchar_t* start = src + 1;
            const wchar_t* end = wchr(start, L'%');
            if (end && end > start) {
                size_t varLen = end - start;
                wchar_t* varName = (wchar_t*)HeapAlloc(heap, 0, (varLen + 1) * sizeof(wchar_t));
                if (varName) {
                    // Manual copying:
                    for (size_t i = 0; i < varLen; i++) varName[i] = start[i];
                    varName[varLen] = L'\0';
                    DWORD needed = GetEnvironmentVariableW(varName, NULL, 0);
                    if (needed > 0) {
                        wchar_t* varValue = (wchar_t*)HeapAlloc(heap, 0, needed * sizeof(wchar_t));
                        if (varValue) {
                            GetEnvironmentVariableW(varName, varValue, needed);
                            wcpy(dst, varValue);
                            dst += wlen(varValue);
                            HeapFree(heap, 0, varValue);
                        } else {
                            *dst++ = L'%';
                            wcpy(dst, varName);
                            dst += wlen(varName);
                            *dst++ = L'%';
                        }
                    } else {
                        *dst++ = L'%';
                        wcpy(dst, varName);
                        dst += wlen(varName);
                        *dst++ = L'%';
                    }
                    HeapFree(heap, 0, varName);
                    src = end + 1;
                } else {
                    *dst++ = *src++;
                }
            } else {
                *dst++ = *src++;
            }
        } else {
            *dst++ = *src++;
        }
    }
    *dst = L'\0';
    wchar_t* result = copy_string(output, heap);
    HeapFree(heap, 0, output);
    return result;
}

/* Post-process value (unescape + env) */
static wchar_t* process_value(const wchar_t* raw, DWORD flags, HANDLE heap) {
    if (!raw) return NULL;
    wchar_t* step1 = NULL;
    if (flags & INI_FLAG_UNESCAPE) {
        step1 = unescape_string(raw, heap);
        if (!step1) return NULL;
    }
    wchar_t* step2 = NULL;
    if (flags & INI_FLAG_EXPAND_ENV) {
        step2 = expand_env(step1 ? step1 : raw, heap);
        if (step1) HeapFree(heap, 0, step1);
        return step2;
    }
    if (step1) return step1;
    return copy_string(raw, heap);
}

/* --------------------------------------------------------------------- */
/* Data structures                                                        */
/* --------------------------------------------------------------------- */

typedef struct KeyValue {
    wchar_t* key;
    wchar_t* value;
    struct KeyValue* next;
} KeyValue;

typedef struct Section {
    wchar_t* name;
    KeyValue* first_kv;
    struct Section* next;
	HANDLE heap;
} Section;

struct IniFile {
    HANDLE heap;
    DWORD flags;
    Section* first_section;
};

static void free_keyvalues(KeyValue* kv, HANDLE heap) {
    while (kv) {
        KeyValue* next = kv->next;
        if (kv->key) HeapFree(heap, 0, kv->key);
        if (kv->value) HeapFree(heap, 0, kv->value);
        HeapFree(heap, 0, kv);
        kv = next;
    }
}

static void free_sections(Section* sec, HANDLE heap) {
    while (sec) {
        Section* next = sec->next;
        if (sec->name) HeapFree(heap, 0, sec->name);
        free_keyvalues(sec->first_kv, heap);
        HeapFree(heap, 0, sec);
        sec = next;
    }
}

static Section* find_section(IniFile* ini, const wchar_t* name) {
    Section* sec = ini->first_section;
    while (sec) {
        if (wcmp(sec->name, name, (ini->flags & INI_FLAG_CASE_SENSITIVE) ? TRUE : FALSE) == 0)
            return sec;
        sec = sec->next;
    }
    return NULL;
}

static void add_keyvalue(Section* sec, wchar_t* key, wchar_t* value) {
    KeyValue* kv = (KeyValue*)HeapAlloc(sec->heap, 0, sizeof(KeyValue));
    if (!kv) return;
    kv->key = key;
    kv->value = value;
    kv->next = NULL;
    if (!sec->first_kv) {
        sec->first_kv = kv;
    } else {
        KeyValue* last = sec->first_kv;
        while (last->next) last = last->next;
        last->next = kv;
    }
}

/* --------------------------------------------------------------------- */
/* INI Parsing                                                           */
/* --------------------------------------------------------------------- */

IniFile* IniParse(const wchar_t* data, int length, HANDLE heap, DWORD flags) {
    if (!data) return NULL;
    heap = GetHeap(heap);
    if (length == -1) length = (int)wlen(data);

    IniFile* ini = (IniFile*)HeapAlloc(heap, HEAP_ZERO_MEMORY, sizeof(IniFile));
    if (!ini) return NULL;
    ini->heap = heap;
    ini->flags = flags;

    wchar_t* buffer = (wchar_t*)HeapAlloc(heap, 0, (length + 1) * sizeof(wchar_t));
    if (!buffer) {
        HeapFree(heap, 0, ini);
        return NULL;
    }
    RtlMoveMemory(buffer, data, length * sizeof(wchar_t));
    buffer[length] = L'\0';

    wchar_t* line = buffer;
    Section* current_section = NULL;

    while (*line) {
        wchar_t* eol = wchr(line, L'\n');
        if (eol) *eol = L'\0';
        wchar_t* eol_next = eol ? eol + 1 : line + wlen(line);

        wchar_t* trimmed = (wchar_t*)skip_spaces(line);
        if (*trimmed != L'\0' && *trimmed != L';') {
            if (*trimmed == L'[') {
                wchar_t* end_br = wchr(trimmed + 1, L']');
                if (end_br) {
                    *end_br = L'\0';
                    wchar_t* sec_name = (wchar_t*)skip_spaces(trimmed + 1);
                    trim_right(sec_name);
                    if (*sec_name) {
                        Section* sec = find_section(ini, sec_name);
                        if (!sec) {
                            sec = (Section*)HeapAlloc(heap, HEAP_ZERO_MEMORY, sizeof(Section));
                            if (sec) {
                                sec->name = copy_string(sec_name, heap);
                                sec->first_kv = NULL;
                                sec->next = ini->first_section;
								sec->heap = heap;
                                ini->first_section = sec;
                            }
                        }
                        current_section = sec;
                    }
                }
            } else if (current_section) {
                wchar_t* eq = wchr(trimmed, L'=');
                if (eq) {
                    *eq = L'\0';
                    wchar_t* key = (wchar_t*)skip_spaces(trimmed);
                    trim_right(key);
                    wchar_t* val = (wchar_t*)skip_spaces(eq + 1);
                    trim_right(val);
                    if (*key) {
                        wchar_t* processed_val = process_value(val, flags, heap);
                        wchar_t* key_copy = copy_string(key, heap);
                        add_keyvalue(current_section, key_copy, processed_val);
                    }
                }
            }
        }
        line = eol_next;
    }

    HeapFree(heap, 0, buffer);
    return ini;
}

void IniFree(IniFile* ini) {
    if (!ini) return;
    free_sections(ini->first_section, ini->heap);
    HeapFree(ini->heap, 0, ini);
}

/* --------------------------------------------------------------------- */
/* Data retrieval functions                                              */
/* --------------------------------------------------------------------- */

const wchar_t* IniGetSections(IniFile* ini, size_t* outSize) {
    if (!ini) return NULL;
    size_t count = 0, total_len = 0;
    Section* sec = ini->first_section;
    while (sec) {
        count++;
        total_len += wlen(sec->name) + 1;
        sec = sec->next;
    }
    if (count == 0) return NULL;
    wchar_t* buffer = (wchar_t*)HeapAlloc(ini->heap, 0, (total_len + 1) * sizeof(wchar_t));
    if (!buffer) return NULL;
    wchar_t* p = buffer;
    sec = ini->first_section;
    while (sec) {
        size_t len = wlen(sec->name);
        wcpy(p, sec->name);
        p += len;
        *p++ = L'\0';
        sec = sec->next;
    }
    *p = L'\0';
    if (outSize) *outSize = total_len;
    return buffer;
}

/* Get list of unique keys in a section */
const wchar_t* IniGetKeys(IniFile* ini, const wchar_t* section, size_t* outSize) {
    if (!ini || !section) return NULL;
    Section* sec = find_section(ini, section);
    if (!sec) return NULL;

    struct KeyNode { wchar_t* key; struct KeyNode* next; } *key_list = NULL;
    size_t total_len = 0;
    KeyValue* kv = sec->first_kv;
    while (kv) {
        BOOL found = FALSE;
        struct KeyNode* node = key_list;
        while (node) {
            if (wcmp(node->key, kv->key, (ini->flags & INI_FLAG_CASE_SENSITIVE) ? TRUE : FALSE) == 0) {
                found = TRUE;
                break;
            }
            node = node->next;
        }
        if (!found) {
            struct KeyNode* new_node = (struct KeyNode*)HeapAlloc(ini->heap, 0, sizeof(struct KeyNode));
            if (new_node) {
                new_node->key = copy_string(kv->key, ini->heap);
                new_node->next = key_list;
                key_list = new_node;
                total_len += wlen(kv->key) + 1;
            }
        }
        kv = kv->next;
    }
    if (!key_list) return NULL;
    wchar_t* buffer = (wchar_t*)HeapAlloc(ini->heap, 0, (total_len + 1) * sizeof(wchar_t));
    if (!buffer) {
        while (key_list) {
            struct KeyNode* next = key_list->next;
            if (key_list->key) HeapFree(ini->heap, 0, key_list->key);
            HeapFree(ini->heap, 0, key_list);
            key_list = next;
        }
        return NULL;
    }
    wchar_t* p = buffer;
    struct KeyNode* node = key_list;
    while (node) {
        size_t len = wlen(node->key);
        wcpy(p, node->key);
        p += len;
        *p++ = L'\0';
        node = node->next;
    }
    *p = L'\0';
    if (outSize) *outSize = total_len;
    while (key_list) {
        struct KeyNode* next = key_list->next;
        if (key_list->key) HeapFree(ini->heap, 0, key_list->key);
        HeapFree(ini->heap, 0, key_list);
        key_list = next;
    }
    return buffer;
}

/* Get first value */
const wchar_t* IniGetValue(IniFile* ini, const wchar_t* section, const wchar_t* key) {
    if (!ini || !section || !key) return NULL;
    Section* sec = find_section(ini, section);
    if (!sec) return NULL;
    KeyValue* kv = sec->first_kv;
    while (kv) {
        if (wcmp(kv->key, key, (ini->flags & INI_FLAG_CASE_SENSITIVE) ? TRUE : FALSE) == 0)
            return kv->value;
        kv = kv->next;
    }
    return NULL;
}

/* Get all values for repeated keys */
const wchar_t** IniGetValues(IniFile* ini, const wchar_t* section, const wchar_t* key) {
    if (!ini || !section || !key) return NULL;
    Section* sec = find_section(ini, section);
    if (!sec) return NULL;
    int count = 0;
    KeyValue* kv = sec->first_kv;
    while (kv) {
        if (wcmp(kv->key, key, (ini->flags & INI_FLAG_CASE_SENSITIVE) ? TRUE : FALSE) == 0)
            count++;
        kv = kv->next;
    }
    if (count == 0) return NULL;
    const wchar_t** arr = (const wchar_t**)HeapAlloc(ini->heap, 0, (count + 1) * sizeof(wchar_t*));
    if (!arr) return NULL;
    int idx = 0;
    kv = sec->first_kv;
    while (kv) {
        if (wcmp(kv->key, key, (ini->flags & INI_FLAG_CASE_SENSITIVE) ? TRUE : FALSE) == 0)
            arr[idx++] = kv->value;
        kv = kv->next;
    }
    arr[idx] = NULL;
    return arr;
}