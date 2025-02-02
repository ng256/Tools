#include "prettytable.h"

void SetCursorPosition(int x, int y) {
#ifdef _WIN32
    HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
    COORD pos = {x, y};
    SetConsoleCursorPosition(hConsole, pos);
#else
    std::cout << "\033[" << y << ";" << x << "H";
#endif
}

void GetCursorPosition(int& x, int& y) {
#ifdef _WIN32
    HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
    CONSOLE_SCREEN_BUFFER_INFO csbi;
    if (GetConsoleScreenBufferInfo(hConsole, &csbi)) {
        x = csbi.dwCursorPosition.X;
        y = csbi.dwCursorPosition.Y;
    } else {
        x = y = -1; // Error
    }
#else
    std::cout << "\033[6n";
    std::string response;
    std::getline(std::cin, response);
    if (response.size() >= 4 && response[0] == '\033' && response[1] == '[') {
        size_t pos = response.find(';');
        if (pos != std::string::npos) {
            y = std::stoi(response.substr(2, pos - 2));
            x = std::stoi(response.substr(pos + 1));
        }
    } else {
        x = y = -1; // Error
    }
#endif
}

int GetConsoleWidth() {
#ifdef _WIN32
    CONSOLE_SCREEN_BUFFER_INFO csbi;
    GetConsoleScreenBufferInfo(GetStdHandle(STD_OUTPUT_HANDLE), &csbi);
    return csbi.srWindow.Right - csbi.srWindow.Left + 1;
#else
    struct winsize w;
    ioctl(STDOUT_FILENO, TIOCGWINSZ, &w);
    return w.ws_col;
#endif
}

TableBorder TableBorder::TextSymbols = TableBorder(L'-', L'|', L'+', L'+', L'+', L'+', L'+', L'+', L'+', L'+', L'+');
TableBorder TableBorder::AsciiSymbols = TableBorder(L'─', L'│', L'┌', L'┐', L'└', L'┘', L'┬', L'┴', L'├', L'┤', L'┼');
TableBorder TableBorder::InvisibleSymbols = TableBorder(L' ', L' ', L' ', L' ', L' ', L' ', L' ', L' ', L' ', L' ', L' ');

TableBorder::TableBorder(wchar_t horizontal, wchar_t vertical, wchar_t topLeft, wchar_t topRight, wchar_t bottomLeft, wchar_t bottomRight,
                         wchar_t topJunction, wchar_t bottomJunction, wchar_t leftJunction, wchar_t rightJunction, wchar_t centerJunction)
    : Horizontal(horizontal), Vertical(vertical), TopLeft(topLeft), TopRight(topRight),
      BottomLeft(bottomLeft), BottomRight(bottomRight), TopJunction(topJunction),
      BottomJunction(bottomJunction), LeftJunction(leftJunction), RightJunction(rightJunction),
      CenterJunction(centerJunction) {}

TableSettings::TableSettings(TableBorder border, int absoluteWidth, bool drawRowBorders, bool drawColumnBorders)
    : Border(border), AbsoluteWidth(absoluteWidth), DrawRowBorders(drawRowBorders), DrawColumnBorders(drawColumnBorders) {}

TableColumn::TableColumn(const std::wstring& header, int width, TableTextAlignment headerAlignment, TableTextAlignment cellAlignment)
    : Header(header), Width(std::max(width, 1)), HeaderAlignment(headerAlignment), CellAlignment(cellAlignment) {}

TableRow::TableRow(const std::initializer_list<std::wstring>& values) : Cells(values) {}

Table::Table(TableSettings settings) : Settings(settings) {}

void Table::AddColumn(const std::wstring& name, int width, TableTextAlignment headerAlignment, TableTextAlignment cellAlignment) {
    Columns.emplace_back(name, width, headerAlignment, cellAlignment);
}

void Table::AddRow(const std::initializer_list<std::wstring>& values) {
    Rows.emplace_back(values);
}

std::wstring Table::ToString() {
    int* columnWidths = CalculateColumnWidths();
    std::wstring topBorder = CreateHorizontalBorder(columnWidths, Settings.Border.TopJunction);
    std::wstring middleBorder = CreateHorizontalBorder(columnWidths, Settings.Border.CenterJunction);
    std::wstring bottomBorder = CreateHorizontalBorder(columnWidths, Settings.Border.BottomJunction);

    std::wostringstream oss;
    oss << topBorder << NEWLINE;

    if (Settings.DrawColumnBorders) {
        oss << Settings.Border.Vertical << L" ";
        for (size_t i = 0; i < Columns.size(); ++i) {
            oss << std::left << std::setw(columnWidths[i]) << AlignText(Columns[i].Header, columnWidths[i], Columns[i].HeaderAlignment) << L" " << Settings.Border.Vertical << L" ";
        }
        oss << NEWLINE;
    } else {
        for (size_t i = 0; i < Columns.size(); ++i) {
            oss << std::left << std::setw(columnWidths[i]) << AlignText(Columns[i].Header, columnWidths[i], Columns[i].HeaderAlignment) << L" ";
        }
        oss << NEWLINE;
    }

    if (!Rows.empty()) {
        oss << middleBorder << NEWLINE;

        for (const auto& row : Rows) {
            auto wrappedCells = WordWrap(row.Cells, columnWidths);
            int rowHeight = GetMaxRowHeight(wrappedCells);

            for (int j = 0; j < rowHeight; ++j) {
                if (Settings.DrawColumnBorders) {
                    oss << Settings.Border.Vertical << L" ";
                }
                for (size_t k = 0; k < Columns.size(); ++k) {
                    if (j < wrappedCells[k].size()) {
                    	auto alignment = Columns[k].CellAlignment;
                    	if (j == wrappedCells[k].size() - 1 && alignment == TableTextAlignment::Justify) alignment = TableTextAlignment::Left;
                        oss << std::left << std::setw(columnWidths[k]) << AlignText(wrappedCells[k][j], columnWidths[k], alignment) << L" ";
                    } else {
                        oss << std::left << std::setw(columnWidths[k]) << std::wstring(columnWidths[k], L' ') << L" ";
                    }
                    if (Settings.DrawColumnBorders) {
                        oss << Settings.Border.Vertical << L" ";
                    }
                }
                oss << NEWLINE;
            }

            if (&row != &Rows.back() && Settings.DrawRowBorders) {
                oss << middleBorder << NEWLINE;
            }
        }
    }

    oss << bottomBorder;
    delete[] columnWidths;
    return oss.str();
}

void Table::PrintTable() {
	GetCursorPosition(this->initialCursorLeft, this->initialCursorTop);
	
    std::wcout << ToString() << std::endl;
    
	GetCursorPosition(this->returnCursorLeft, this->returnCursorTop);
}

void Table::UpdateCell(int column, int row, const std::wstring& value) {
    if (column < 0 || column >= Columns.size() || row < 0 || row >= Rows.size()) {
        throw std::out_of_range("Column or row index is out of range.");
    }
    Rows[row].Cells[column] = value;
}

void Table::PrintCell(int column, int row) {

    if(this->initialCursorLeft < 0 || this->initialCursorTop < 0) {
    	throw std::logic_error("PrintTable must be called at least once before using PrintCell.");
	}
		
    if (column < 0 || column >= Columns.size() || row < 0 || row >= Rows.size()) {
        throw std::out_of_range("Column or row index is out of range.");
    }
    
    int* columnWidths = CalculateColumnWidths();
    const TableBorder& border = Settings.Border;

    // Calculate the starting coordinates for the specified cell
    int x = this->initialCursorLeft;
    for (int i = 0; i < column; ++i) {
        x += columnWidths[i] + 3; // column width + 2 spaces + border
    }

    int y = this->initialCursorTop + 3; // Start of data (after headers)
    for (int i = 0; i < row; ++i) {
        y += WordWrap(Rows[i].Cells[0], columnWidths[0]).size() + 1; // row height + border
    }

    // Retrieve the cell value
    std::wstring cellValue = Rows[row].Cells[column];
    auto wrappedLines = WordWrap(cellValue, columnWidths[column]);

    // Set the cursor position
    SetCursorPosition(x + 2, y);

    // Print each line of the cell
    for (int i = 0; i < wrappedLines.size(); ++i) {
        std::wcout << AlignText(wrappedLines[i], columnWidths[column], Columns[column].CellAlignment) << NEWLINE;
    }
    
    x = this->returnCursorLeft;
    y = this->returnCursorTop;

    SetCursorPosition(x, y);


    delete[] columnWidths;
}

int Table::MinTableWidth() const {
    int minColumnWidth = Columns.empty() ? 1 : std::min_element(Columns.begin(), Columns.end(), [](const TableColumn& a, const TableColumn& b) {
        return a.Width < b.Width;
    })->Width;
    return (Columns.size() + 1) * 3 + Columns.size() * minColumnWidth;
}

int* Table::CalculateColumnWidths() const {
    int absoluteWidth = std::max(Settings.AbsoluteWidth, MinTableWidth());
    int totalRelativeWidth = std::accumulate(Columns.begin(), Columns.end(), 0, [](int sum, const TableColumn& col) {
        return sum + col.Width;
    });
    int totalAvailableWidth = absoluteWidth - (Columns.size() + 1) * 3;
    int* columnWidths = new int[Columns.size()];

    for (size_t i = 0; i < Columns.size(); ++i) {
        columnWidths[i] = static_cast<int>(std::floor(static_cast<double>(totalAvailableWidth) * Columns[i].Width / totalRelativeWidth));
    }

    int remainingWidth = totalAvailableWidth - std::accumulate(columnWidths, columnWidths + Columns.size(), 0);
    for (int i = 0; i < remainingWidth; ++i) {
        columnWidths[i % Columns.size()]++;
    }

    return columnWidths;
}

std::vector<std::vector<std::wstring>> Table::WordWrap(const std::vector<std::wstring>& cells, int* columnWidths) const {
    std::vector<std::vector<std::wstring>> wrappedCells(cells.size());
    for (size_t i = 0; i < cells.size(); ++i) {
        wrappedCells[i] = WordWrap(cells[i], columnWidths[i]);
    }
    return wrappedCells;
}

std::vector<std::wstring> Table::WordWrap(const std::wstring& text, int width) const {
    std::wistringstream iss(text);
    std::vector<std::wstring> words;
    std::wstring word;
    while (iss >> word) {
        words.push_back(word);
    }

    std::vector<std::wstring> lines;
    std::wstring currentLine;

    for (const auto& word : words) {
        if (currentLine.length() + word.length() + 1 > width) {
            lines.push_back(currentLine);
            currentLine = word;
        } else {
            if (!currentLine.empty()) {
                currentLine += L" ";
            }
            currentLine += word;
        }
    }

    if (!currentLine.empty()) {
        lines.push_back(currentLine);
    }

    return lines;
}

int Table::GetMaxRowHeight(const std::vector<std::vector<std::wstring>>& wrappedCells) const {
    int maxHeight = 0;
    for (const auto& cellLines : wrappedCells) {
        maxHeight = std::max(maxHeight, static_cast<int>(cellLines.size()));
    }
    return maxHeight;
}

std::wstring Table::JustifyText(const std::wstring& text, int width) const {
    std::wistringstream iss(text);
    std::vector<std::wstring> words;
    std::wstring word;
    while (iss >> word) {
        words.push_back(word);
    }

    if (words.size() == 1) {
        return words[0] + std::wstring(width - words[0].length(), L' ');
    }

    int totalSpaces = width - text.length() + words.size() - 1;
    int spaceSlots = words.size() - 1;
    int spacesPerSlot = totalSpaces / spaceSlots;
    int extraSpaces = totalSpaces % spaceSlots;

    std::wostringstream oss;
    for (size_t i = 0; i < words.size(); ++i) {
        oss << words[i];
        if (i < spaceSlots) {
            oss << std::wstring(spacesPerSlot + (i < extraSpaces ? 1 : 0), L' ');
        }
    }

    return oss.str();
}

std::wstring Table::AlignText(const std::wstring& text, int width, TableTextAlignment alignment) const {
    switch (alignment) {
        case TableTextAlignment::Left:
            return text + std::wstring(width - text.length(), L' ');
        case TableTextAlignment::Center:
            return std::wstring((width - text.length()) / 2, L' ') + text + std::wstring((width - text.length() + 1) / 2, L' ');
        case TableTextAlignment::Right:
            return std::wstring(width - text.length(), L' ') + text;
        case TableTextAlignment::Justify:
            return JustifyText(text, width);
        default:
            return text;
    }
}

std::wstring Table::CreateHorizontalBorder(int* columnWidths, wchar_t junction) const {
    std::wostringstream oss;
    oss << Settings.Border.TopLeft;
    for (size_t i = 0; i < Columns.size(); ++i) {
        oss << std::wstring(columnWidths[i] + 2, Settings.Border.Horizontal);
        if (i < Columns.size() - 1) {
            oss << junction;
        }
    }
    oss << Settings.Border.TopRight;
    return oss.str();
}
