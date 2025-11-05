using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using UndoRedoSystem;

namespace UndoRedoExample
{
    public partial class Form1 : Form
    {
        private UndoRedoManager _undoManager = new UndoRedoManager();
        private DataGridView _dataGridView;

        public Form1()
        {
            InitializeComponent();
            SetupDataGridView();
            SetupUndoRedoHandlers();
        }

        private void SetupDataGridView()
        {
            _dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false
            };
            // Add columns
            _dataGridView.Columns.Add("Name", "Name");
            _dataGridView.Columns.Add("Age", "Age");
            _dataGridView.Columns.Add("City", "City");
            // Add test data
            _dataGridView.Rows.Add("John", "25", "New York");
            _dataGridView.Rows.Add("Alice", "30", "London");
            _dataGridView.Rows.Add("Bob", "35", "Paris");
            // Subscribe to edit events
            _dataGridView.CellBeginEdit += DataGridView_CellBeginEdit;
            _dataGridView.CellEndEdit += DataGridView_CellEndEdit;

            this.Controls.Add(_dataGridView);
        }

        private void SetupUndoRedoHandlers()
        {
            // Handlers for Ctrl+Z and Ctrl+Y
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Z)
                {
                    _undoManager.Undo();
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.Y)
                {
                    _undoManager.Redo();
                    e.Handled = true;
                }
            };
        }

        private DataGridViewCell _currentEditingCell;
        private object _originalValue;

        private void DataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                _currentEditingCell = _dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
                _originalValue = _currentEditingCell.Value;
            }
        }

        private void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_currentEditingCell != null)
            {
                var newValue = _currentEditingCell.Value;

                // Create a command to change the cell
                var command = new DataGridViewCellChangeCommand(
                    _dataGridView,
                    _currentEditingCell.RowIndex,
                    _currentEditingCell.ColumnIndex,
                    _originalValue,
                    newValue,
                    $"Edit cell ({_currentEditingCell.RowIndex}, {_currentEditingCell.ColumnIndex})"
                );
                _undoManager.Execute(command);

                _currentEditingCell = null;
                _originalValue = null;
            }
        }
    }

    /// <summary>
    /// Command to change a DataGridView cell with full UI support
    /// </summary>
    public class DataGridViewCellChangeCommand : IUndoCommand
    {
        private readonly DataGridView _dataGridView;
        private readonly int _rowIndex;
        private readonly int _columnIndex;
        private readonly object _oldValue;
        private readonly object _newValue;

        public string Description { get; }

        public DataGridViewCellChangeCommand(DataGridView dataGridView, int rowIndex, int columnIndex,
                                           object oldValue, object newValue, string description)
        {
            _dataGridView = dataGridView ?? throw new ArgumentNullException(nameof(dataGridView));
            _rowIndex = rowIndex;
            _columnIndex = columnIndex;
            _oldValue = oldValue;
            _newValue = newValue;
            Description = description;
        }

        public void Execute()
        {
            // Set the new value
            SetCellValue(_newValue);
            // Perform UI actions
            FocusCell();
        }

        public void Undo()
        {
            // Restore the old value
            SetCellValue(_oldValue);
            // Perform the same UI actions as during execution
            FocusCell();
        }

        public void Redo()
        {
            // Set the new value (repeat the action)
            SetCellValue(_newValue);
            // Perform the same UI actions
            FocusCell();
        }

        private void SetCellValue(object value)
        {
            if (_dataGridView.InvokeRequired)
            {
                _dataGridView.Invoke(new Action<object>(SetCellValue), value);
                return;
            }
            // Ensure indices are valid
            if (_rowIndex >= 0 && _rowIndex < _dataGridView.Rows.Count &&
                _columnIndex >= 0 && _columnIndex < _dataGridView.Columns.Count)
            {
                var cell = _dataGridView.Rows[_rowIndex].Cells[_columnIndex];
                cell.Value = value ?? DBNull.Value;
            }
        }

        private void FocusCell()
        {
            if (_dataGridView.InvokeRequired)
            {
                _dataGridView.Invoke(new Action(FocusCell));
                return;
            }
            // Activate DataGridView
            _dataGridView.Focus();
            // Ensure indices are valid
            if (_rowIndex >= 0 && _rowIndex < _dataGridView.Rows.Count &&
                _columnIndex >= 0 && _columnIndex < _dataGridView.Columns.Count)
            {
                // Scroll to the cell if it is not visible
                var cell = _dataGridView.Rows[_rowIndex].Cells[_columnIndex];
                if (!cell.Displayed)
                {
                    _dataGridView.FirstDisplayedScrollingRowIndex = _rowIndex;
                }
                // Select the cell
                _dataGridView.ClearSelection();
                _dataGridView.CurrentCell = cell;
                cell.Selected = true;
                // Additional visual highlighting
                cell.Style.BackColor = Color.LightYellow;

                // Remove highlighting after a short time
                var timer = new Timer { Interval = 1000 };
                timer.Tick += (s, e) =>
                {
                    cell.Style.BackColor = _dataGridView.DefaultCellStyle.BackColor;
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }
    }

    /// <summary>
    /// Extended command for grouping multiple changes in DataGridView
    /// </summary>
    public class DataGridViewMultiChangeCommand : IUndoCommand
    {
        private readonly DataGridView _dataGridView;
        private readonly CellChange[] _changes;

        public string Description { get; }

        public DataGridViewMultiChangeCommand(DataGridView dataGridView, CellChange[] changes, string description)
        {
            _dataGridView = dataGridView;
            _changes = changes;
            Description = description;
        }

        public void Execute()
        {
            ApplyChanges(_changes.Select(c => (c.RowIndex, c.ColumnIndex, c.NewValue)).ToArray());
            FocusFirstCell();
        }

        public void Undo()
        {
            ApplyChanges(_changes.Select(c => (c.RowIndex, c.ColumnIndex, c.OldValue)).ToArray());
            FocusFirstCell();
        }

        public void Redo()
        {
            ApplyChanges(_changes.Select(c => (c.RowIndex, c.ColumnIndex, c.NewValue)).ToArray());
            FocusFirstCell();
        }

        private void ApplyChanges((int Row, int Column, object Value)[] changes)
        {
            if (_dataGridView.InvokeRequired)
            {
                _dataGridView.Invoke(new Action<(int, int, object)[]>(ApplyChanges), changes);
                return;
            }
            foreach (var (row, column, value) in changes)
            {
                if (row >= 0 && row < _dataGridView.Rows.Count &&
                    column >= 0 && column < _dataGridView.Columns.Count)
                {
                    _dataGridView.Rows[row].Cells[column].Value = value ?? DBNull.Value;
                }
            }
        }

        private void FocusFirstCell()
        {
            if (_dataGridView.InvokeRequired)
            {
                _dataGridView.Invoke(new Action(FocusFirstCell));
                return;
            }
            if (_changes.Length > 0)
            {
                var firstChange = _changes[0];
                _dataGridView.Focus();

                if (firstChange.RowIndex >= 0 && firstChange.RowIndex < _dataGridView.Rows.Count &&
                    firstChange.ColumnIndex >= 0 && firstChange.ColumnIndex < _dataGridView.Columns.Count)
                {
                    var cell = _dataGridView.Rows[firstChange.RowIndex].Cells[firstChange.ColumnIndex];
                    _dataGridView.CurrentCell = cell;
                    cell.Selected = true;

                    if (!cell.Displayed)
                    {
                        _dataGridView.FirstDisplayedScrollingRowIndex = firstChange.RowIndex;
                    }
                }
            }
        }
    }

    public struct CellChange
    {
        public int RowIndex { get; }
        public int ColumnIndex { get; }
        public object OldValue { get; }
        public object NewValue { get; }

        public CellChange(int rowIndex, int columnIndex, object oldValue, object newValue)
        {
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    // Example of usage with grouped changes (transaction)
    public class DataGridViewTransactionExample
    {
        private UndoRedoManager _undoManager = new UndoRedoManager();

        public void PerformBulkUpdate(DataGridView dataGridView)
        {
            var changes = new[]
            {
                new CellChange(0, 0, dataGridView.Rows[0].Cells[0].Value, "Updated Name"),
                new CellChange(0, 1, dataGridView.Rows[0].Cells[1].Value, "30"),
                new CellChange(1, 2, dataGridView.Rows[1].Cells[2].Value, "Berlin")
            };
            var transactionCommand = new DataGridViewMultiChangeCommand(
                dataGridView,
                changes,
                "Bulk update cells"
            );
            _undoManager.Execute(transactionCommand);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
