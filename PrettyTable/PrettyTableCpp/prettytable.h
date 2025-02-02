#ifndef PRETTYTABLE_H
#define PRETTYTABLE_H

#include <iostream>
#include <vector>
#include <string>
#include <sstream>
#include <iomanip>
#include <algorithm>
#include <cmath>
#include <thread>
#include <chrono>
#include <numeric> // Добавлен заголовочный файл для std::accumulate

#ifdef _WIN32
#include <windows.h>
#define NEWLINE "\r\n"
#else
#include <sys/ioctl.h>
#include <unistd.h>
#define NEWLINE "\n"
#endif

void SetCursorPosition(int x, int y);
int GetConsoleWidth();

enum class TableTextAlignment {
    Center = 0,
    Left = 1,
    Right = 2,
    Justify = 3
};

class TableBorder {
public:
    wchar_t Horizontal;
    wchar_t Vertical;
    wchar_t TopLeft;
    wchar_t TopRight;
    wchar_t BottomLeft;
    wchar_t BottomRight;
    wchar_t TopJunction;
    wchar_t BottomJunction;
    wchar_t LeftJunction;
    wchar_t RightJunction;
    wchar_t CenterJunction;

    TableBorder(wchar_t horizontal, wchar_t vertical, wchar_t topLeft, wchar_t topRight, wchar_t bottomLeft, wchar_t bottomRight,
                wchar_t topJunction, wchar_t bottomJunction, wchar_t leftJunction, wchar_t rightJunction, wchar_t centerJunction);

    static TableBorder TextSymbols;
    static TableBorder AsciiSymbols;
    static TableBorder InvisibleSymbols;
};

class TableSettings {
public:
    TableBorder Border;
    int AbsoluteWidth;
    bool DrawRowBorders;
    bool DrawColumnBorders;

    TableSettings(TableBorder border, int absoluteWidth, bool drawRowBorders = true, bool drawColumnBorders = true);
};

class TableColumn {
public:
    std::wstring Header;
    int Width;
    TableTextAlignment HeaderAlignment;
    TableTextAlignment CellAlignment;

    TableColumn(const std::wstring& header, int width, TableTextAlignment headerAlignment = TableTextAlignment::Left, TableTextAlignment cellAlignment = TableTextAlignment::Left);
};

class TableRow {
public:
    std::vector<std::wstring> Cells;

    TableRow(const std::initializer_list<std::wstring>& values);
};

class Table {
public:
    std::vector<TableColumn> Columns;
    std::vector<TableRow> Rows;
    TableSettings Settings;
    Table(TableSettings settings);

    void AddColumn(const std::wstring& name, int width, TableTextAlignment headerAlignment = TableTextAlignment::Left, TableTextAlignment cellAlignment = TableTextAlignment::Left);
    void AddRow(const std::initializer_list<std::wstring>& values);
    std::wstring ToString();
    void PrintTable();
    void UpdateCell(int column, int row, const std::wstring& value);
    void PrintCell(int column, int row);

private:
    int initialCursorLeft = -1;
    int initialCursorTop = -1;
    int returnCursorLeft = -1;
    int returnCursorTop = -1;

    int MinTableWidth() const;
    int* CalculateColumnWidths() const;
    std::vector<std::vector<std::wstring>> WordWrap(const std::vector<std::wstring>& cells, int* columnWidths) const;
    std::vector<std::wstring> WordWrap(const std::wstring& text, int width) const;
    int GetMaxRowHeight(const std::vector<std::vector<std::wstring>>& wrappedCells) const;
    std::wstring JustifyText(const std::wstring& text, int width) const;
    std::wstring AlignText(const std::wstring& text, int width, TableTextAlignment alignment) const;
    std::wstring CreateHorizontalBorder(int* columnWidths, wchar_t junction) const;
};

#endif // PRETTYTABLE_H
