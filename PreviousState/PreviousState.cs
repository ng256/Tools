/***********************************************************************
The class PreviousState represents information about the previous 
state of the object and provides methods to undo any changes. The class 
PreviousStateStack contains these objects. In user code:

Instantiate PreviousState each time your program makes any changes.
Fill the PreviousState properties:
 * Obj      Changed object.
 * Value    Object value before changing.
 * Action   Delegate that contains instructions for restore value. 
            It usually sets PreviousState.Value to the object and 
			moves the focus to the changed GUI component. 
			By default it just sets Value to Obj.

Next step push created PreviousState in the PreviousStateStack.
Pop it when need undo changes (Ctrl+Z or "Undo" button is pressed) 
and run the PreviousState.Action.
************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Security;
using System.Reflection;

namespace System
{
    /// <summary>
    /// Encapsulates a method that restores the previous state of an object.
    /// </summary>
    /// <param name="obj">Managed object to restore to previous value.</param>
    /// <param name="value">The previous state of the managed object.</param>
    public delegate void RestoreAction(ref object obj, object value, params object[] parameters);

    /// <summary>
    /// Represents the implementation of <see cref="Stack{T}"/> for storage and retrieval
    /// previous state of managed objects.
    /// </summary>
    public class PreviousStateStack : Stack<IEnumerable<PreviousState>>, IEnumerable<IEnumerable<PreviousState>>
    {
        private const int DEF_CAPACITY = 256;

        /// <summary>
        /// Initializes a new instance <see cref="PreviousStateStack"/>
        /// with initial capacity <see cref="DEF_CAPACITY"/>.
        /// </summary>
        public PreviousStateStack() : base(DEF_CAPACITY)
        {
        }

        /// <summary>
        /// Initializes a new instance <see cref="PreviousStateStack"/>
        /// with initial capacity <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">Initial number of elements.</param>
        public PreviousStateStack(int capacity) : base(capacity)
        {
        }

        /// <summary>
        /// Initializes a new instance <see cref="PreviousStateStack"/>
        /// with the specified stack item by the <paramref name="items"/> parameter.
        /// </summary>
        /// <param name="items">Objects <see cref="PreviousState"/> to be inserted into <see cref="Stack{T}" />.</param>
        public PreviousStateStack(params PreviousState[] items) : base(DEF_CAPACITY)
        {
            Push(items);
        }

        /// <summary>
        /// Restores the previous state of the managed object,
        /// represented by this instance <see cref="PreviousState"/>.
        /// </summary>
        public void Restore()
        {
            var states = Pop();
            foreach (var state in states) state.Restore();
        }

        /// <summary>
        /// Inserts the enumerated objects <see cref="PreviousState"/> as the top element of the stack <see cref="Stack{T}" />.
        /// </summary>
        /// <param name="items">
        /// Objects <see cref="PreviousState"/> to be inserted into <see cref="Stack{T}" />.
        /// </param>
        public void Push(params PreviousState[] items)
        {
            Push((IEnumerable<PreviousState>)items);
        }
    }

    /// <summary>
    /// Provides information about the previous state of managed objects.
    /// </summary>
    [SecurityCritical]
    public sealed class PreviousState
    {
        private IntPtr _objPtr;

        // Cached setter for restoring object value
        private readonly Action<object, object> _setter;

        private static void DefaultAction(ref object obj, object value, params object[] parameters)
        {
            try
            {
                // Safe default restore: if setter known, assign directly
                if (obj is PropertyTargetWrapper wrapper)
                {
                    wrapper.Setter(wrapper.Target, value);
                }
                else
                {
                    // fallback (no specific target) – ignored safely
                }
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Defines an action that allows you to restore the previous state of the managed object.
        /// </summary>
        public RestoreAction Action { get; private set; }

        /// <summary>
        /// Managed object for which to store the previous value.
        /// </summary>
        public object Obj
        {
            get
            {
                var handle = GCHandle.FromIntPtr(_objPtr);
                return handle.Target;
            }
            private set
            {
                var handle = GCHandle.Alloc(value);
                _objPtr = (IntPtr)handle;
            }
        }

        /// <summary>
        /// Stores the previous value of the managed object.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Initializes a new instance <see cref="PreviousState"/>
        /// for the specified managed object and its previous state.
        /// </summary>
        /// <param name="obj">Object for which the previous state will be saved.</param>
        /// <param name="value">The previous state of the object.</param>
        public PreviousState(object obj, object value)
        {
            obj = obj ?? throw new ArgumentNullException(nameof(obj));
            Value = value;
            Obj = obj;
            Action = DefaultAction;
        }

        /// <summary>
        /// Initializes a new instance <see cref="PreviousState"/>
        /// for the specified managed object, its previous state, and the action by which
        /// it will be restored.
        /// </summary>
        /// <param name="obj">Object for which the previous state will be saved.</param>
        /// <param name="value">The previous state of the object.</param>
        /// <param name="action">An action that allows you to restore the previous state of the object.</param>
        public PreviousState(object obj, object value, RestoreAction action) : this(obj, value)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>
        /// Restores the previous state of the managed object,
        /// represented by this instance <see cref="PreviousState"/>.
        /// </summary>
        public void Restore()
        {
            object obj = Obj;
            Action?.Invoke(ref obj, Value);
        }

        /// <summary>
        /// Creates PreviousState from lambda expression (safe alternative to __makeref).
        /// Example: PreviousState.Create(() => button.Text);
        /// </summary>
        public static PreviousState Create<T>(Expression<Func<T>> expression)
        {
            if (expression.Body is MemberExpression member && member.Member is PropertyInfo property)
            {
                var targetExpr = (ConstantExpression)((MemberExpression)member.Expression).Expression;
                var targetObj = targetExpr.Value;
                var getter = property.GetGetMethod();
                var setter = property.GetSetMethod();
                if (setter == null)
                    throw new InvalidOperationException("Property is read-only.");

                var oldValue = getter.Invoke(targetObj, null);

                var setterDelegate = (Action<object, object>)Delegate.CreateDelegate(
                    typeof(Action<object, object>),
                    setter.IsStatic ? null : targetObj,
                    setter
                );

                var wrapper = new PropertyTargetWrapper(targetObj, setterDelegate);

                return new PreviousState(wrapper, oldValue);
            }

            throw new ArgumentException("Expression must be a property access.", nameof(expression));
        }

        // internal wrapper to hold property target and setter delegate
        private sealed class PropertyTargetWrapper
        {
            public readonly object Target;
            public readonly Action<object, object> Setter;

            public PropertyTargetWrapper(object target, Action<object, object> setter)
            {
                Target = target;
                Setter = setter;
            }
        }
    }
}
