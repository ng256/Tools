#include "prettytable.h"

int main() {
    TableBorder border = TableBorder::TextSymbols;
    int absoluteWidth = GetConsoleWidth();

    TableSettings settings(border, absoluteWidth, true, true);

    Table frame(settings);
    frame.AddColumn(L"Quantum Hyperdrive Engine", 5, TableTextAlignment::Center, TableTextAlignment::Center);
    frame.PrintTable();

    Table table(settings);
    table.AddColumn(L"Test", 3, TableTextAlignment::Center, TableTextAlignment::Center);
    table.AddColumn(L"Description", 4, TableTextAlignment::Center, TableTextAlignment::Justify);
    table.AddColumn(L"Status", 3, TableTextAlignment::Center, TableTextAlignment::Center);

    table.AddRow({L"1", L"Initial System Configuration Testing", L"Waiting"});
    table.AddRow({L"2", L"Core Component Stability Verification", L"Waiting"});
    table.AddRow({L"3", L"Emergency Shutdown and Recovery Procedure Testing", L"Waiting"});

    table.PrintTable();

    for (int i = 0; i <= 10; ++i) {
        table.UpdateCell(2, 0, i == 10 ? L"Passed" :  std::to_wstring(i * 10) + L"%");
        table.PrintCell(2, 0);
        std::this_thread::sleep_for(std::chrono::milliseconds(500));
    }

    for (int i = 0; i <= 10; ++i) {
        table.UpdateCell(2, 1, i == 10 ? L"Passed" :  std::to_wstring(i * 10) + L"%");
        table.PrintCell(2, 1);
        std::this_thread::sleep_for(std::chrono::milliseconds(500));
    }

    for (int i = 0; i <= 10; ++i) {
        table.UpdateCell(2, 2, i == 10 ? L"Passed" :  std::to_wstring(i * 10) + L"%");
        table.PrintCell(2, 2);
        std::this_thread::sleep_for(std::chrono::milliseconds(500));
    }

    return 0;
}
