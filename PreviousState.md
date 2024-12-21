# Undo/Redo with PreviousState and PreviousStateStack in .NET
This article describes the implementation of two classes — PreviousState and PreviousStateStack — that allow you to manage the previous state of an object. These classes facilitate the implementation of an undo/redo functionality for any application that requires it. The PreviousState class holds information about the object's previous state and provides a method to restore it. The PreviousStateStack class works as a container for these states, enabling pushing and popping of states, much like a stack data structure.

## Key Concepts
### PreviousState Class
The PreviousState class represents the state of a managed object before it was modified. Each instance of this class stores:
- The Obj property, which holds the reference to the object.
- The Value property, which holds the value of the object before it was modified.
- The Action delegate, which provides a way to restore the object to its previous state.
- The RestoreAction delegate is responsible for restoring the object to its previous value by executing custom restore logic. By default, this delegate simply assigns the previous value back to the object.
  
| Property | Description |  
| -------- | ----------- |  
| __Obj__      | Changed object. |  
| __Value__    | Object value before before it was modified. |  
| __Action__   | Delegate that provides a way to restore the object to its previous state by executing custom restore logic. It usually sets __PreviousState.Value__ to the object and moves the focus. By default, this delegate simply assigns the previous value back to the object. |  

### PreviousStateStack Class
The PreviousStateStack class is a stack implementation that stores and retrieves PreviousState instances. The stack allows multiple states to be stored, and when a user wants to undo a change, the last state can be popped from the stack and restored. This class also provides methods for pushing new states onto the stack and restoring the object to its previous state.
## How to Use
- Instantiate __PreviousState__ each time your program makes any changes. This instance contains the object, its value before the change, and the RestoreAction delegate that can restore the value.
- Fill the __PreviousState__ properties.  
- Next step push created __PreviousState__ in the __PreviousStateStack__. This ensures that the state is saved for potential undo.  
- Pop it when need undo changes (<kbd>Ctrl</kbd>+<kbd>Z</kbd> or "Undo" button is pressed) and call the **Restore** method, which executes the **RestoreAction** delegate, restoring the object to its previous state.  

## Example
Here is a full example demonstrating how to implement the undo functionality for a TextBox in Windows Forms:
```csharp
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace UndoExample
{
    public partial class MainForm : Form
    {
        private PreviousStateStack previousStateStack;

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button undoButton;

        public MainForm()
        {
            InitializeComponent();
            previousStateStack = new PreviousStateStack();
            previousStateStack.Push(new PreviousState(textBox1, textBox1.Text)); // Save initial text.
        }

        // Event handler for text change in the TextBox
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            // Save the previous state of the TextBox before the change
            previousStateStack.Push(new PreviousState(textBox1, textBox1.Text));
        }

        // Event handler for the "Undo" button click
        private void undoButton_Click(object sender, EventArgs e)
        {
            // Restore the previous state
            previousStateStack.Restore();
        }

        // Form initialization method
        private void InitializeComponent()
        {
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.undoButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(12, 12);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(400, 100);
            this.textBox1.TabIndex = 0;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // undoButton
            // 
            this.undoButton.Location = new System.Drawing.Point(12, 120);
            this.undoButton.Name = "undoButton";
            this.undoButton.Size = new System.Drawing.Size(75, 23);
            this.undoButton.TabIndex = 1;
            this.undoButton.Text = "Undo";
            this.undoButton.UseVisualStyleBackColor = true;
            this.undoButton.Click += new System.EventHandler(this.undoButton_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(424, 161);
            this.Controls.Add(this.undoButton);
            this.Controls.Add(this.textBox1);
            this.Name = "MainForm";
            this.Text = "Undo Example";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
```
Next, here is an example that implements undoing a change in a DataGridView while keeping the focus on the cell whose value was undo.
```csharp
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace UndoDataGridViewExample
{
    public partial class MainForm : Form
    {
        private PreviousStateStack previousStateStack;

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button undoButton;

        public MainForm()
        {
            InitializeComponent();
            previousStateStack = new PreviousStateStack();
            InitializeDataGridView();
        }

        // Initialize the DataGridView with some data
        private void InitializeDataGridView()
        {
            dataGridView1.ColumnCount = 2;
            dataGridView1.Columns[0].Name = "ID";
            dataGridView1.Columns[1].Name = "Name";

            dataGridView1.Rows.Add(1, "John");
            dataGridView1.Rows.Add(2, "Jane");
            dataGridView1.Rows.Add(3, "Doe");

            // Initial state (save the current values)
            SaveCurrentState();
        }

        // Event handler for cell value changes in the DataGridView
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // Save the previous state before the change
            var cell = dataGridView1[e.ColumnIndex, e.RowIndex];
            var oldValue = cell.Value;

            // Push the previous state into the stack with a custom undo action
            previousStateStack.Push(new PreviousState(cell, oldValue, (ref object obj, object value) =>
            {
                // Restore the previous value
                var currentCell = (DataGridViewCell)obj;
                currentCell.Value = value;

                // Restore the focus to the same cell after undo
                dataGridView1.CurrentCell = currentCell; // Move focus to the changed cell
            }));
        }

        // Event handler for the "Undo" button click
        private void undoButton_Click(object sender, EventArgs e)
        {
            // Restore the previous state and focus will be handled inside the action
            previousStateStack.Restore();
        }

        // Save current state of all cells
        private void SaveCurrentState()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    previousStateStack.Push(new PreviousState(cell, cell.Value, (ref object obj, object value) =>
                    {
                        var currentCell = (DataGridViewCell)obj;
                        currentCell.Value = value;
                    }));
                }
            }
        }

        // Form initialization method
        private void InitializeComponent()
        {
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.undoButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 12);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(400, 200);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellValueChanged);
            // 
            // undoButton
            // 
            this.undoButton.Location = new System.Drawing.Point(12, 220);
            this.undoButton.Name = "undoButton";
            this.undoButton.Size = new System.Drawing.Size(75, 23);
            this.undoButton.TabIndex = 1;
            this.undoButton.Text = "Undo";
            this.undoButton.UseVisualStyleBackColor = true;
            this.undoButton.Click += new System.EventHandler(this.undoButton_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(424, 261);
            this.Controls.Add(this.undoButton);
            this.Controls.Add(this.dataGridView1);
            this.Name = "MainForm";
            this.Text = "Undo DataGridView Example";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}

```
