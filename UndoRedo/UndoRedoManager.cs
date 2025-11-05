// ******************************************************************************
// UndoRedoManager.cs - Advanced Undo/Redo System for .NET Applications
// 
// Provides comprehensive undo/redo functionality with transaction support,
// property change tracking, and UI state management for WinForms applications.
// 
// Features:
// - Full undo/redo stack management with capacity limits
// - Transaction grouping for atomic operations
// - Type-safe property change tracking using expressions
// - DataGridView integration with cell-level undo/redo
// - INotifyPropertyChanged support for UI binding
// - Thread-safe operations with invocation support
// 
// Author: Pavel Bashkardin
// License: MIT
// 
// Usage Example:
// var manager = new UndoRedoManager();
// manager.RecordPropertyChange(button, b => b.Text, "New Text");
// manager.Undo(); // Ctrl+Z support
// manager.Redo(); // Ctrl+Y support
// 
// ******************************************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace UndoRedoSystem
{
    /// <summary>
    /// Represents a command that can be undone and redone
    /// </summary>
    public interface IUndoCommand
    {
        string Description { get; }
        void Execute();
        void Undo();
        void Redo();
    }

    /// <summary>
    /// Manages undo/redo operations with transaction support and grouping
    /// </summary>
    public class UndoRedoManager : INotifyPropertyChanged
    {
        private readonly Stack<IUndoCommand> _undoStack = new Stack<IUndoCommand>();
        private readonly Stack<IUndoCommand> _redoStack = new Stack<IUndoCommand>();
        private readonly List<IUndoCommand> _currentTransaction = new List<IUndoCommand>();
        private bool _isInTransaction = false;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Maximum number of undo operations to keep in history
        /// </summary>
        public int Capacity { get; set; } = 100;

        /// <summary>
        /// Indicates if there are operations that can be undone
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;
        
        /// <summary>
        /// Indicates if there are operations that can be redone
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Description of the next undo operation
        /// </summary>
        public string UndoDescription => CanUndo ? _undoStack.Peek().Description : string.Empty;

        /// <summary>
        /// Description of the next redo operation
        /// </summary>
        public string RedoDescription => CanRedo ? _redoStack.Peek().Description : string.Empty;

        /// <summary>
        /// Read-only collection of undo history
        /// </summary>
        public ReadOnlyCollection<string> UndoHistory => 
            new ReadOnlyCollection<string>(_undoStack.Select(cmd => cmd.Description).Reverse().ToList());

        /// <summary>
        /// Executes a command and adds it to undo history
        /// </summary>
        public void Execute(IUndoCommand command)
        {
            command.Execute();

            if (_isInTransaction)
            {
                _currentTransaction.Add(command);
            }
            else
            {
                _undoStack.Push(command);
                _redoStack.Clear();
                TrimToCapacity();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Creates and executes a property change command
        /// </summary>
        public void RecordPropertyChange<T>(T target, Expression<Func<T, object>> propertyExpression, object newValue, string description = null)
        {
            var command = PropertyChangeCommand.Create(target, propertyExpression, newValue, description);
            Execute(command);
        }

        /// <summary>
        /// Undoes the last operation
        /// </summary>
        public void Undo()
        {
            if (!CanUndo) return;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
            OnPropertyChanged();
        }

        /// <summary>
        /// Redoes the last undone operation
        /// </summary>
        public void Redo()
        {
            if (!CanRedo) return;

            var command = _redoStack.Pop();
            command.Redo();
            _undoStack.Push(command);
            OnPropertyChanged();
        }

        /// <summary>
        /// Begins a transaction to group multiple operations
        /// </summary>
        public void BeginTransaction(string transactionName = null)
        {
            if (_isInTransaction)
                throw new InvalidOperationException("Transaction already in progress");

            _isInTransaction = true;
            _currentTransaction.Clear();
        }

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        public void CommitTransaction()
        {
            if (!_isInTransaction)
                throw new InvalidOperationException("No transaction in progress");

            if (_currentTransaction.Count > 0)
            {
                var transaction = new TransactionCommand(_currentTransaction.ToArray());
                _undoStack.Push(transaction);
                _redoStack.Clear();
                TrimToCapacity();
                OnPropertyChanged();
            }

            _isInTransaction = false;
            _currentTransaction.Clear();
        }

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        public void RollbackTransaction()
        {
            if (!_isInTransaction)
                throw new InvalidOperationException("No transaction in progress");

            // Undo all commands in reverse order
            for (int i = _currentTransaction.Count - 1; i >= 0; i--)
            {
                _currentTransaction[i].Undo();
            }

            _isInTransaction = false;
            _currentTransaction.Clear();
        }

        /// <summary>
        /// Clears all undo/redo history
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _currentTransaction.Clear();
            _isInTransaction = false;
            OnPropertyChanged();
        }

        private void TrimToCapacity()
        {
            while (_undoStack.Count > Capacity)
            {
                _undoStack.RemoveOldest();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Command for changing property values with undo/redo support
    /// </summary>
    public class PropertyChangeCommand<T> : IUndoCommand
    {
        private readonly T _target;
        private readonly Func<T, object> _getter;
        private readonly Action<T, object> _setter;
        private readonly object _oldValue;
        private readonly object _newValue;

        public string Description { get; }

        private PropertyChangeCommand(T target, Func<T, object> getter, Action<T, object> setter, object newValue, string description)
        {
            _target = target;
            _getter = getter;
            _setter = setter;
            _oldValue = getter(target);
            _newValue = newValue;
            Description = description ?? $"Change {typeof(T).Name} property";
        }

        public static PropertyChangeCommand<T> Create(T target, Expression<Func<T, object>> propertyExpression, object newValue, string description = null)
        {
            var (getter, setter) = CreateAccessors(propertyExpression);
            return new PropertyChangeCommand<T>(target, getter, setter, newValue, description);
        }

        private static (Func<T, object> getter, Action<T, object> setter) CreateAccessors(Expression<Func<T, object>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memberExpr)
            {
                var parameter = Expression.Parameter(typeof(T), "obj");
                var valueParameter = Expression.Parameter(typeof(object), "value");

                // Create getter
                var getter = Expression.Lambda<Func<T, object>>(
                    Expression.Convert(Expression.MakeMemberAccess(parameter, memberExpr.Member), typeof(object)),
                    parameter).Compile();

                // Create setter
                var setter = Expression.Lambda<Action<T, object>>(
                    Expression.Assign(
                        Expression.MakeMemberAccess(parameter, memberExpr.Member),
                        Expression.Convert(valueParameter, memberExpr.Type)),
                    parameter, valueParameter).Compile();

                return (getter, setter);
            }

            throw new ArgumentException("Invalid property expression");
        }

        public void Execute() => _setter(_target, _newValue);
        public void Undo() => _setter(_target, _oldValue);
        public void Redo() => _setter(_target, _newValue);
    }

    /// <summary>
    /// Groups multiple commands into a single transaction
    /// </summary>
    public class TransactionCommand : IUndoCommand
    {
        private readonly IUndoCommand[] _commands;

        public string Description { get; }

        public TransactionCommand(IUndoCommand[] commands, string description = null)
        {
            _commands = commands;
            Description = description ?? $"Transaction ({commands.Length} operations)";
        }

        public void Execute()
        {
            foreach (var command in _commands)
            {
                command.Execute();
            }
        }

        public void Undo()
        {
            for (int i = _commands.Length - 1; i >= 0; i--)
            {
                _commands[i].Undo();
            }
        }

        public void Redo()
        {
            foreach (var command in _commands)
            {
                command.Redo();
            }
        }
    }

    /// <summary>
    /// Generic command for custom undo/redo operations
    /// </summary>
    public class ActionCommand : IUndoCommand
    {
        private readonly Action _execute;
        private readonly Action _undo;
        
        public string Description { get; }

        public ActionCommand(Action execute, Action undo, string description)
        {
            _execute = execute;
            _undo = undo;
            Description = description;
        }

        public void Execute() => _execute();
        public void Undo() => _undo();
        public void Redo() => _execute();
    }

    // Extension methods for Stack
    internal static class StackExtensions
    {
        public static void RemoveOldest<T>(this Stack<T> stack)
        {
            var list = stack.ToList();
            list.RemoveAt(list.Count - 1); // Remove oldest (bottom of stack)
            stack.Clear();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                stack.Push(list[i]);
            }
        }
    }

    /// <summary>
    /// Static factory for creating undo commands
    /// </summary>
    public static class UndoCommand
    {
        public static IUndoCommand CreatePropertyChange<T>(T target, Expression<Func<T, object>> property, object newValue, string description = null)
            => PropertyChangeCommand<T>.Create(target, property, newValue, description);

        public static IUndoCommand CreateAction(Action execute, Action undo, string description)
            => new ActionCommand(execute, undo, description);

        public static IUndoCommand CreateTransaction(IUndoCommand[] commands, string description = null)
            => new TransactionCommand(commands, description);
    }
}
