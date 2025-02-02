using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrettyTable;

/// <summary>
/// Enumeration representing the text alignment options.
/// </summary>
public enum TableTextAlignment
{
    /// <summary>
    /// Align text to the left.
    /// </summary>
    Left,

    /// <summary>
    /// Center align text.
    /// </summary>
    Center,

    /// <summary>
    /// Align text to the right.
    /// </summary>
    Right,

    /// <summary>
    /// Justify text to fill the available width.
    /// </summary>
    Justify
}

/// <summary>
/// Enumeration representing the types of vertical borders.
/// </summary>
public enum VerticalBorder
{
    /// <summary>
    /// Represents the middle vertical border.
    /// </summary>
    Middle,

    /// <summary>
    /// Represents the left vertical border.
    /// </summary>
    Left,

    /// <summary>
    /// Represents the right vertical border.
    /// </summary>
    Right,
}

/// <summary>
/// Enumeration representing the types of horizontal borders.
/// </summary>
public enum HorizontalBorder
{
    /// <summary>
    /// Represents the middle horizontal border.
    /// </summary>
    Middle,

    /// <summary>
    /// Represents the top horizontal border.
    /// </summary>
    Top,

    /// <summary>
    /// Represents the bottom horizontal border.
    /// </summary>
    Bottom,
}

/// <summary>
/// Class representing the border settings for a table.
/// </summary>
public class TableBorder
{
    /// <summary>
    /// Gets or sets the horizontal border character.
    /// </summary>
    public char Horizontal { get; private set; }

    /// <summary>
    /// Gets or sets the vertical border character.
    /// </summary>
    public char Vertical { get; private set; }

    /// <summary>
    /// Gets or sets the top-left corner character.
    /// </summary>
    public char TopLeft { get; private set; }

    /// <summary>
    /// Gets or sets the top-right corner character.
    /// </summary>
    public char TopRight { get; private set; }

    /// <summary>
    /// Gets or sets the bottom-left corner character.
    /// </summary>
    public char BottomLeft { get; private set; }

    /// <summary>
    /// Gets or sets the bottom-right corner character.
    /// </summary>
    public char BottomRight { get; private set; }

    /// <summary>
    /// Gets or sets the top junction character.
    /// </summary>
    public char TopJunction { get; private set; }

    /// <summary>
    /// Gets or sets the bottom junction character.
    /// </summary>
    public char BottomJunction { get; private set; }

    /// <summary>
    /// Gets or sets the left junction character.
    /// </summary>
    public char LeftJunction { get; private set; }

    /// <summary>
    /// Gets or sets the right junction character.
    /// </summary>
    public char RightJunction { get; private set; }

    /// <summary>
    /// Gets or sets the center junction character.
    /// </summary>
    public char CenterJunction { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableBorder"/> class.
    /// </summary>
    /// <param name="horizontal">The horizontal border character.</param>
    /// <param name="vertical">The vertical border character.</param>
    /// <param name="topLeft">The top-left corner character.</param>
    /// <param name="topRight">The top-right corner character.</param>
    /// <param name="bottomLeft">The bottom-left corner character.</param>
    /// <param name="bottomRight">The bottom-right corner character.</param>
    /// <param name="topJunction">The top junction character.</param>
    /// <param name="bottomJunction">The bottom junction character.</param>
    /// <param name="leftJunction">The left junction character.</param>
    /// <param name="rightJunction">The right junction character.</param>
    /// <param name="centerJunction">The center junction character.</param>
    public TableBorder(char horizontal, char vertical, char topLeft, char topRight, char bottomLeft, char bottomRight,
                      char topJunction, char bottomJunction, char leftJunction, char rightJunction, char centerJunction)
    {
        Horizontal = horizontal;
        Vertical = vertical;
        TopLeft = topLeft;
        TopRight = topRight;
        BottomLeft = bottomLeft;
        BottomRight = bottomRight;
        TopJunction = topJunction;
        BottomJunction = bottomJunction;
        LeftJunction = leftJunction;
        RightJunction = rightJunction;
        CenterJunction = centerJunction;
    }

    /// <summary>
    /// Predefined border style using text symbols.
    /// </summary>
    public static readonly TableBorder TextSymbols = new TableBorder(
        horizontal: '-',
        vertical: '|',
        topLeft: '+',
        topRight: '+',
        bottomLeft: '+',
        bottomRight: '+',
        topJunction: '+',
        bottomJunction: '+',
        leftJunction: '+',
        rightJunction: '+',
        centerJunction: '+'
    );

    /// <summary>
    /// Predefined border style using ASCII symbols.
    /// </summary>
    public static readonly TableBorder AsciiSymbols = new TableBorder(
        horizontal: '─',
        vertical: '│',
        topLeft: '┌',
        topRight: '┐',
        bottomLeft: '└',
        bottomRight: '┘',
        topJunction: '┬',
        bottomJunction: '┴',
        leftJunction: '├',
        rightJunction: '┤',
        centerJunction: '┼'
    );

    /// <summary>
    /// Predefined border style using invisible symbols.
    /// </summary>
    public static readonly TableBorder InvisibleSymbols = new TableBorder(
        horizontal: ' ',
        vertical: ' ',
        topLeft: ' ',
        topRight: ' ',
        bottomLeft: ' ',
        bottomRight: ' ',
        topJunction: ' ',
        bottomJunction: ' ',
        leftJunction: ' ',
        rightJunction: ' ',
        centerJunction: ' '
    );

    /// <summary>
    /// Creates a horizontal border for the table.
    /// </summary>
    /// <param name="borderType">The type of horizontal border.</param>
    /// <param name="columnWidths">The widths of the columns.</param>
    /// <returns>A string representing the horizontal border.</returns>
    public string CreateHorizontalBorder(HorizontalBorder borderType, params int[] columnWidths)
    {
        char junction = borderType switch
        {
            HorizontalBorder.Top => TopJunction,
            HorizontalBorder.Middle => CenterJunction,
            HorizontalBorder.Bottom => BottomJunction,
            _ => throw new ArgumentOutOfRangeException(nameof(borderType), borderType, null)
        };

        var borderParts = new List<string>
        {
            TopLeft.ToString()
        };

        for (int i = 0; i < columnWidths.Length; i++)
        {
            borderParts.Add(new string(Horizontal, columnWidths[i] + 2));
            if (i < columnWidths.Length - 1)
            {
                borderParts.Add(junction.ToString());
            }
        }

        borderParts.Add(TopRight.ToString());
        return string.Join("", borderParts);
    }

    /// <summary>
    /// Creates a vertical border for the table.
    /// </summary>
    /// <param name="borderType">The type of vertical border.</param>
    /// <param name="rowHeights">The heights of the rows.</param>
    /// <returns>A string representing the vertical border.</returns>
    public string CreateVerticalBorder(VerticalBorder borderType, params int[] rowHeights)
    {
        char junction = borderType switch
        {
            VerticalBorder.Left => LeftJunction,
            VerticalBorder.Middle => CenterJunction,
            VerticalBorder.Right => RightJunction,
            _ => throw new ArgumentOutOfRangeException(nameof(borderType), borderType, null)
        };

        var borderParts = new List<string>
        {
            TopLeft.ToString()
        };

        for (int i = 0; i < rowHeights.Length; i++)
        {
            borderParts.Add(new string(Vertical, rowHeights[i] + 2));
            if (i < rowHeights.Length - 1)
            {
                borderParts.Add(junction.ToString());
            }
        }

        borderParts.Add(BottomLeft.ToString());
        return string.Join("", borderParts);
    }
}

/// <summary>
/// Class representing the settings for a table.
/// </summary>
public class TableSettings
{
    /// <summary>
    /// Gets or sets the border settings for the table.
    /// </summary>
    public TableBorder Border { get; set; }

    /// <summary>
    /// Gets or sets the absolute width of the table.
    /// </summary>
    public int AbsoluteWidth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to draw row borders.
    /// </summary>
    public bool DrawRowBorders { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to draw column borders.
    /// </summary>
    public bool DrawColumnBorders { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableSettings"/> class.
    /// </summary>
    /// <param name="border">The border settings for the table.</param>
    /// <param name="absoluteWidth">The absolute width of the table.</param>
    /// <param name="drawRowBorders">A value indicating whether to draw row borders.</param>
    /// <param name="drawColumnBorders">A value indicating whether to draw column borders.</param>
    public TableSettings(TableBorder border, int absoluteWidth, bool drawRowBorders = true, bool drawColumnBorders = true)
    {
        Border = border;
        AbsoluteWidth = absoluteWidth;
        DrawRowBorders = drawRowBorders;
        DrawColumnBorders = drawColumnBorders;
    }
}

/// <summary>
/// Class representing a column in a table.
/// </summary>
public class TableColumn
{
    /// <summary>
    /// Gets or sets the header text for the column.
    /// </summary>
    public string Header { get; private set; }

    /// <summary>
    /// Gets or sets the width of the column.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets or sets the alignment of the header text.
    /// </summary>
    public TableTextAlignment HeaderAlignment { get; set; }

    /// <summary>
    /// Gets or sets the alignment of the cell text.
    /// </summary>
    public TableTextAlignment CellAlignment { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableColumn"/> class.
    /// </summary>
    /// <param name="header">The header text for the column.</param>
    /// <param name="width">The width of the column.</param>
    /// <param name="headerAlignment">The alignment of the header text.</param>
    /// <param name="cellAlignment">The alignment of the cell text.</param>
    public TableColumn(string header, int width, TableTextAlignment headerAlignment = TableTextAlignment.Left, TableTextAlignment cellAlignment = TableTextAlignment.Left)
    {
        Header = header;
        Width = Math.Max(width, 1); // Minimum column width is 1 character
        HeaderAlignment = headerAlignment;
        CellAlignment = cellAlignment;
    }
}

/// <summary>
/// Class representing a row in a table.
/// </summary>
public class TableRow
{
    /// <summary>
    /// Gets the list of cell values in the row.
    /// </summary>
    public List<string> Cells { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableRow"/> class.
    /// </summary>
    /// <param name="values">The cell values for the row.</param>
    public TableRow(params string[] values)
    {
        Cells = new List<string>(values);
    }
}

/// <summary>
/// Representing the cursor position.
/// </summary>
public struct CursorPosition
{
    public int Left { get; private set; }
    public int Top { get; set; }

    public CursorPosition(int left, int top)
    {
        Left = left;
        Top = top;
    }

    /// <summary>
    /// Gets the current cursor position of the console.
    /// </summary>
    public static CursorPosition ConsoleCursorPosition => new CursorPosition(Console.CursorLeft, Console.CursorTop);
}

internal class DrawTableCache
{
    public Table Table;
    public CursorPosition TablePosition;
    public string[][][] FormattedCells; // Cells values wrapped and aligmented by column width and alignment.
    public int[] ColumnWidths;
    public int[] RowHeights;
    public string TopBorder;
    public string MiddleBorder;
    public string BottomBorder;

    public DrawTableCache(Table table)
    {
        Table = table ?? throw new ArgumentNullException(nameof(table));
        TableBorder border = Table.Settings.Border;

        // Save the initial cursor position
        TablePosition = new CursorPosition(Console.CursorLeft, Console.CursorTop);

        ColumnWidths = CalculateColumnWidths();
        TopBorder = border.CreateHorizontalBorder(HorizontalBorder.Top, ColumnWidths);
        MiddleBorder = border.CreateHorizontalBorder(HorizontalBorder.Middle, ColumnWidths);
        BottomBorder = border.CreateHorizontalBorder(HorizontalBorder.Bottom, ColumnWidths);

        var drawRowBorders = Table.Settings.DrawRowBorders;
        var drawColumnBorders = Table.Settings.DrawColumnBorders;

        // Initialize the formatted cells array
        FormattedCells = new string[Table.Rows.Count][][];
        RowHeights = new int[Table.Rows.Count];

        for (int rowIndex = 0; rowIndex < Table.Rows.Count; rowIndex++)
        {
            TableRow row = Table.Rows[rowIndex];
            var wrappedCells = row.Cells.Take(Table.Columns.Count).Select((cell, i) => cell.WordWrap(ColumnWidths[i])).ToList();
            int rowHeight = wrappedCells.Max(cellLines => cellLines.Count);
            RowHeights[rowIndex] = rowHeight;

            FormattedCells[rowIndex] = new string[Table.Columns.Count][];
            for (int columnIndex = 0; columnIndex < Table.Columns.Count; columnIndex++)
            {
                FormattedCells[rowIndex][columnIndex] = new string[rowHeight];
                var currentAlignment = Table.Columns[columnIndex].CellAlignment;

                for (int wrappedLineIndex = 0; wrappedLineIndex < rowHeight; wrappedLineIndex++)
                {
                    if (wrappedLineIndex < wrappedCells[columnIndex].Count)
                    {
                        if (wrappedLineIndex == wrappedCells[columnIndex].Count - 1 && currentAlignment == TableTextAlignment.Justify)
                        {
                            currentAlignment = TableTextAlignment.Left;
                        }
                        FormattedCells[rowIndex][columnIndex][wrappedLineIndex] = wrappedCells[columnIndex][wrappedLineIndex].AlignText(ColumnWidths[columnIndex], currentAlignment);
                    }
                    else
                    {
                        FormattedCells[rowIndex][columnIndex][wrappedLineIndex] = new string(' ', ColumnWidths[columnIndex]);
                    }
                }
            }
        }
    }

    // Calculates the minimum width of the table.
    private int CalculateMinTableWidth()
    {
        List<TableColumn> Columns = Table.Columns;

        // Minimum table width is the sum of minimum column widths plus border widths
        int minColumnWidth = Columns.Count > 0 ? Columns.Min(c => c.Width) : 1;
        return (Columns.Count + 1) * 3 + Columns.Count * minColumnWidth;
    }

    // Calculates the widths of the columns.
    private int[] CalculateColumnWidths()
    {
        var columns = Table.Columns;
        var absoluteWidth = Table.Settings.AbsoluteWidth;

        absoluteWidth = Math.Max(absoluteWidth, CalculateMinTableWidth());
        int totalRelativeWidth = columns.Sum(c => c.Width); // Total relative width of all columns
        int totalAvailableWidth = absoluteWidth - (columns.Count + 1) * 3; // Account for borders and padding
        int[] columnWidths = new int[columns.Count];

        // Calculate the width for each column proportionally to its relative width
        for (int i = 0; i < columns.Count; i++)
        {
            columnWidths[i] = (int)Math.Floor((double)totalAvailableWidth * columns[i].Width / totalRelativeWidth);
        }

        // Adjust widths to ensure the total width matches the available width
        int remainingWidth = totalAvailableWidth - columnWidths.Sum();
        for (int i = 0; i < remainingWidth; i++)
        {
            columnWidths[i % columns.Count]++;
        }

        return columnWidths;
    }
}


/// <summary>
/// Class representing a table.
/// </summary>
public class Table
{
    /// <summary>
    /// Gets the list of columns in the table.
    /// </summary>
    public List<TableColumn> Columns { get; private set; }

    /// <summary>
    /// Gets the list of rows in the table.
    /// </summary>
    public List<TableRow> Rows { get; private set; }

    /// <summary>
    /// Gets the settings for the table.
    /// </summary>
    public TableSettings Settings { get; private set; }

    private CursorPosition startCursorPosition = new CursorPosition(-1, -1);
    private CursorPosition endCursorPosition = new CursorPosition(-1, -1);

    /// <summary>
    /// Initializes a new instance of the <see cref="Table"/> class.
    /// </summary>
    /// <param name="settings">The settings for the table.</param>
    public Table(TableSettings settings)
    {
        Columns = new List<TableColumn>();
        Rows = new List<TableRow>();
        Settings = settings;
    }

    /// <summary>
    /// Adds a column to the table.
    /// </summary>
    /// <param name="name">The name of the column.</param>
    /// <param name="width">The width of the column.</param>
    /// <param name="headerAlignment">The alignment of the header text.</param>
    /// <param name="cellAlignment">The alignment of the cell text.</param>
    public void AddColumn(string name, int width, TableTextAlignment headerAlignment = TableTextAlignment.Left, TableTextAlignment cellAlignment = TableTextAlignment.Left)
    {
        Columns.Add(new TableColumn(name, width, headerAlignment, cellAlignment));
    }

    /// <summary>
    /// Adds a row to the table.
    /// </summary>
    /// <param name="values">The cell values for the row.</param>
    public void AddRow(params string[] values)
    {
        Rows.Add(new TableRow(values));
    }

    // Calculates the minimum width of the table.
    private int MinTableWidth()
    {
        // Minimum table width is the sum of minimum column widths plus border widths
        int minColumnWidth = Columns.Count > 0 ? Columns.Min(c => c.Width) : 1;
        return (Columns.Count + 1) * 3 + Columns.Count * minColumnWidth;
    }

    // Calculates the widths of the columns.
    private int[] CalculateColumnWidths()
    {
        var border = Settings.Border;
        int absoluteWidth = Math.Max(Settings.AbsoluteWidth, MinTableWidth());
        int totalRelativeWidth = Columns.Sum(c => c.Width); // Total relative width of all columns
        int totalAvailableWidth = absoluteWidth - (Columns.Count + 1) * 3; // Account for borders and padding
        int[] columnWidths = new int[Columns.Count];

        // Calculate the width for each column proportionally to its relative width
        for (int i = 0; i < Columns.Count; i++)
        {
            columnWidths[i] = (int)Math.Floor((double)totalAvailableWidth * Columns[i].Width / totalRelativeWidth);
        }

        // Adjust widths to ensure the total width matches the available width
        int remainingWidth = totalAvailableWidth - columnWidths.Sum();
        for (int i = 0; i < remainingWidth; i++)
        {
            columnWidths[i % Columns.Count]++;
        }

        return columnWidths;
    }

    /// <summary>
    /// Converts the table to its string representation.
    /// </summary>
    /// <returns>The string representation of the table.</returns>
    public override string ToString()
    {
        var border = Settings.Border; // Get the border settings from the table settings
        var drawRowBorders = Settings.DrawRowBorders; // Check if row borders should be drawn
        var drawColumnBorders = Settings.DrawColumnBorders; // Check if column borders should be drawn

        int[] columnWidths = CalculateColumnWidths(); // Calculate the widths of the columns
        string topBorder = border.CreateHorizontalBorder(HorizontalBorder.Top, columnWidths); // Create the top border
        string middleBorder = border.CreateHorizontalBorder(HorizontalBorder.Middle, columnWidths); // Create the middle border
        string bottomBorder = border.CreateHorizontalBorder(HorizontalBorder.Bottom, columnWidths); // Create the bottom border

        var stringBuilder = new StringBuilder(); // Initialize a StringBuilder to build the table string
        stringBuilder.AppendLine(topBorder); // Append the top border to the string

        // Print headers
        if (drawColumnBorders)
        {
            // If column borders are to be drawn, append the headers with borders
            stringBuilder.AppendLine($"{border.Vertical} {string.Join($" {border.Vertical} ", Columns.Select((c, i) => c.Header.AlignText(columnWidths[i], c.HeaderAlignment)))} {border.Vertical}");
        }
        else
        {
            // If column borders are not to be drawn, append the headers without borders
            stringBuilder.AppendLine($"{string.Join(" ", Columns.Select((c, i) => c.Header.AlignText(columnWidths[i], c.HeaderAlignment)))}");
        }

        // Print rows
        if (Rows.Count > 0)
        {
            // Append the middle border with adjusted junctions
            stringBuilder.AppendLine(middleBorder.Replace(border.TopLeft, border.LeftJunction).Replace(border.TopRight, border.RightJunction));

            for (int rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
            {
                TableRow row = Rows[rowIndex]; // Get the current row
                var wrappedCells = row.Cells.Take(Columns.Count).Select((cell, i) => cell.WordWrap(columnWidths[i])).ToList(); // Wrap the cell text
                int rowHeight = wrappedCells.Max(cellLines => cellLines.Count); // Calculate the maximum row height

                for (int wrappedLineIndex = 0; wrappedLineIndex < rowHeight; wrappedLineIndex++)
                {
                    var formattedCells = new List<string>(); // List to hold formatted cell strings
                    for (int columnIndex = 0; columnIndex < Columns.Count; columnIndex++)
                    {
                        var currentAlignment = Columns[columnIndex].CellAlignment; // Get the current alignment
                        if (wrappedLineIndex == wrappedCells[columnIndex].Count - 1 && currentAlignment == TableTextAlignment.Justify)
                        {
                            currentAlignment = TableTextAlignment.Left; // Last line in the cell, do not justify
                        }

                        if (wrappedLineIndex < wrappedCells[columnIndex].Count)
                        {
                            // Format the cell text with the current alignment
                            formattedCells.Add(wrappedCells[columnIndex][wrappedLineIndex].AlignText(columnWidths[columnIndex], currentAlignment));
                        }
                        else
                        {
                            // If there are no more lines to wrap, add empty spaces
                            formattedCells.Add(new string(' ', columnWidths[columnIndex]));
                        }
                    }

                    if (drawColumnBorders)
                    {
                        // If column borders are to be drawn, append the formatted cells with borders
                        stringBuilder.AppendLine($"{border.Vertical} {string.Join($" {border.Vertical} ", formattedCells)} {border.Vertical}");
                    }
                    else
                    {
                        // If column borders are not to be drawn, append the formatted cells without borders
                        stringBuilder.AppendLine(string.Join(" ", formattedCells));
                    }
                }

                if (row != Rows.Last() && drawRowBorders)
                {
                    // If it's not the last row and row borders are to be drawn, append the middle border with adjusted junctions
                    stringBuilder.AppendLine(middleBorder.Replace(border.TopLeft, border.LeftJunction).Replace(border.TopRight, border.RightJunction));
                }
            }
        }

        // Append the bottom border with adjusted junctions
        stringBuilder.AppendLine(bottomBorder.Replace(border.TopLeft, border.BottomLeft).Replace(border.TopRight, border.BottomRight));

        return stringBuilder.ToString(); // Return the string representation of the table
    }

    /// <summary>
    /// Converts a specific cell to its string representation with table formatting.
    /// </summary>
    /// <param name="column">The column index of the cell.</param>
    /// <param name="row">The row index of the cell.</param>
    /// <returns>The string representation of the cell with table formatting.</returns>
    public string ToString(int column, int row)
    {
        if (startCursorPosition.Left == -1 || startCursorPosition.Top == -1)
        {
            throw new InvalidOperationException("PrintTable must be called at least once before using PrintCell.");
        }

        if (column < 0 || column >= Columns.Count || row < 0 || row >= Rows.Count)
        {
            throw new ArgumentOutOfRangeException("Column or row index is out of range.");
        }

        var border = Settings.Border;
        var drawColumnBorders = Settings.DrawColumnBorders;

        int[] columnWidths = CalculateColumnWidths();

        var stringBuilder = new StringBuilder();

        // Get the cell value and wrap it
        var cellValue = Rows[row].Cells[column];
        var wrappedCells = cellValue.WordWrap(columnWidths[column]);

        // Format the cell with alignment
        var formattedCells = new List<string>();
        for (int j = 0; j < wrappedCells.Count; j++)
        {
            var currentAlignment = Columns[column].CellAlignment;
            if (j == wrappedCells.Count - 1 && currentAlignment == TableTextAlignment.Justify)
            {
                currentAlignment = TableTextAlignment.Left; // Last line in the cell, do not justify
            }

            formattedCells.Add(wrappedCells[j].AlignText(columnWidths[column], currentAlignment));
        }

        if (drawColumnBorders)
        {
            stringBuilder.Append($"{border.Vertical} {string.Join($" {border.Vertical} ", formattedCells)} {border.Vertical}");
        }
        else
        {
            stringBuilder.Append(string.Join(" ", formattedCells));
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Updates the value of a specific cell.
    /// </summary>
    /// <param name="column">The column index of the cell.</param>
    /// <param name="row">The row index of the cell.</param>
    /// <param name="value">The new value of the cell.</param>
    public void UpdateCell(int column, int row, string value)
    {
        if (column < 0 || column >= Columns.Count || row < 0 || row >= Rows.Count)
        {
            throw new ArgumentOutOfRangeException("Column or row index is out of range.");
        }

        Rows[row].Cells[column] = value;
    }

    /// <summary>
    /// Prints a specific cell to the console with table formatting.
    /// </summary>
    /// <param name="column">The column index of the cell.</param>
    /// <param name="row">The row index of the cell.</param>
    public void PrintCell(int column, int row)
    {
        if (startCursorPosition.Left < 0 || startCursorPosition.Top < 0)
        {
            throw new InvalidOperationException("PrintTable must be called at least once before using PrintCell.");
        }

        if (column < 0 || column >= Columns.Count || row < 0 || row >= Rows.Count)
        {
            throw new ArgumentOutOfRangeException("Column or row index is out of range.");
        }

        CursorPosition returnCursorPosition = CursorPosition.ConsoleCursorPosition;
        int[] columnWidths = CalculateColumnWidths();
        var border = Settings.Border;

        // Calculate the starting coordinates for the specified cell
        int x = startCursorPosition.Left;
        for (int i = 0; i < column; i++)
        {
            x += columnWidths[i] + 3; // column width + 2 spaces + border
        }

        int y = startCursorPosition.Top + 3; // Start of data (after headers)
        for (int i = 0; i < row; i++)
        {
            y += Rows[i].Cells[0].WordWrap(columnWidths[0]).Count + 1; // row height + border
        }

        // Retrieve the cell value
        string cellValue = Rows[row].Cells[column];
        var wrappedLines = cellValue.WordWrap(columnWidths[column]);

        // Print each line of the cell
        for (int i = 0; i < wrappedLines.Count; i++)
        {
            Console.SetCursorPosition(x + 2, y + i); // x + space + border
            Console.Write(wrappedLines[i].AlignText(columnWidths[column], Columns[column].CellAlignment));
        }

        x = returnCursorPosition.Left;
        y = returnCursorPosition.Top;

        Console.SetCursorPosition(x, y);
    }

    /// <summary>
    /// Prints the table to the console.
    /// </summary>
    public void PrintTable()
    {
        // Save the initial cursor position
        startCursorPosition = new CursorPosition(Console.CursorLeft, Console.CursorTop);

        Console.WriteLine(this.ToString());

        // Save the cursor position after table
        endCursorPosition = new CursorPosition(Console.CursorLeft, Console.CursorTop);
    }
}

internal static class StringExtensions
{
    // Wraps text to fit within a specified width.
    public static List<string> WordWrap(this string text, int width, char nonBreakingSpace = '_')
    {
        var words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = "";

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            if (nonBreakingSpace != '\0')
                word = word.Replace(nonBreakingSpace, ' '); // Processing non-breaking spaces
            if (currentLine.Length + word.Length + 1 > width)
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                if (currentLine.Length > 0)
                {
                    currentLine += " ";
                }
                currentLine += word;
            }
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine);
        }

        return lines;
    }

    // Justifies text within a specified width.
    public static string JustifyText(this string text, int width)
    {
        // Split the input text into words
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 1)
        {
            // If there's only one word, pad it to the right
            return words[0].PadRight(width);
        }

        int totalSpaces = width - text.Replace(" ", "").Length; // Calculate spaces to add
        int spaceSlots = words.Length - 1;                     // Number of slots between words
        int spacesPerSlot = totalSpaces / spaceSlots;          // Minimum spaces per slot
        int extraSpaces = totalSpaces % spaceSlots;            // Extra spaces to distribute

        var justifiedLine = new StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            justifiedLine.Append(words[i]);
            if (i < spaceSlots)
            {
                // Add spaces after each word
                justifiedLine.Append(new string(' ', spacesPerSlot + (i < extraSpaces ? 1 : 0)));
            }
        }

        return justifiedLine.ToString();
    }

    // Aligns text within a specified width.
    public static string AlignText(this string text, int width, TableTextAlignment alignment)
    {
        switch (alignment)
        {
            case TableTextAlignment.Left:
                return text.PadRight(width);
            case TableTextAlignment.Center:
                return text.PadLeft(width / 2 + text.Length / 2).PadRight(width);
            case TableTextAlignment.Right:
                return text.PadLeft(width);
            case TableTextAlignment.Justify:
                return JustifyText(text, width);
            default:
                return text;
        }
    }
}
