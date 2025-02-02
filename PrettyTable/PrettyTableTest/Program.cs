using PrettyTable;

TableBorder border = TableBorder.AsciiSymbols;
int absoluteWidth = Console.BufferWidth;

TableSettings settings = new TableSettings(border, absoluteWidth, drawRowBorders: true, drawColumnBorders: true);

Table frame = new Table(settings);
frame.AddColumn("Quantum Hyperdrive Engine", 5, TableTextAlignment.Center, TableTextAlignment.Center);

frame.PrintTable();

Table table = new Table(settings);

table.AddColumn("Test", 3, TableTextAlignment.Center, TableTextAlignment.Center);
table.AddColumn("Description", 4, TableTextAlignment.Center, TableTextAlignment.Justify);
table.AddColumn("Status", 3, TableTextAlignment.Center, TableTextAlignment.Center);

table.AddRow("1", "Initial System Configuration Testing", "Waiting");
table.AddRow("2", "Core Component Stability Verification", "Waiting");
table.AddRow("3", "Emergency Shutdown and Recovery Procedure_Testing", "Waiting");

table.PrintTable();

// Example of updating and printing a specific cell dynamically
for (int i = 0; i <= 10; i++)
{
    int column = 2, row = 0;
    table.UpdateCell(column, row, i == 10 ? "Passed" : $"{i * 10}%");
    table.PrintCell(column, row);
    Thread.Sleep(500); // Simulate process execution
}
for (int i = 0; i <= 10; i++)
{
    int column = 2, row = 1;
    table.UpdateCell(column, row, i == 10 ? "Passed" : $"{i * 10}%");
    table.PrintCell(column, row);
    Thread.Sleep(500);
}
for (int i = 0; i <= 10; i++)
{
    int column = 2, row = 2;
    table.UpdateCell(column, row, i == 10 ? "Passed" : $"{i * 10}%");
    table.PrintCell(column, row);
    Thread.Sleep(500);
}
