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

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

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
            push(items);
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

        private static void DefaultAction(ref object obj, object value, params object[] parameters)
        {
            try
            {
                var reference = __makeref(obj);
                __refvalue(reference, object) = value;
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Defines an action that allows you to restore the previous state of the managed object.
        /// </summary>
        public RestoreAction Action { get; } = DefaultAction;

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
                _objPtr = (IntPtr) handle;
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
            obj=obj?? throw new ArgumentNullException(nameof(obj));
            Value = value;
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
            Action = action?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>
        /// Restores the previous state of the managed object,
        /// represented by this instance <see cref="PreviousState"/>.
        /// </summary>
        public void Restore()
        {
            objectobj = obj;
            Action?.Invoke(ref obj, this.Value);
        }
    }
} 