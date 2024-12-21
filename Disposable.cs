/*
 * Disposable.cs
 *
 * A base class that provides a robust implementation of the IDisposable pattern.
 * Automatically disposes properties and fields of the instance that implement
 * IDisposable, ensuring resources are released correctly.
 *
 * Copyright (c) 2024 Bashkardin Pavel
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections;
using System.Reflection;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace System
{
    /// <summary>
    /// A base class that provides a robust implementation of the IDisposable pattern.
    /// Automatically disposes properties and fields of the instance that implement IDisposable,
    /// ensuring resources are released correctly.
    /// </summary>
    public abstract class Disposable : IDisposable
    {
        private bool _disposed = false; // Tracks whether the object has been disposed.
        private readonly HashSet<object> _disposedObjects = new HashSet<object>(); // Tracks already disposed objects.
        private bool _ignoreExceptionsInCatch = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Disposable"/> class.
        /// </summary>
        /// <param name="ignoreExceptions">
        /// A value indicating whether exceptions during disposing members should be ignored.
        /// </param>
        protected Disposable(bool ignoreExceptions = false)
        {
            _ignoreExceptionsInCatch = ignoreExceptions;
        }

        /// <summary>
        /// Indicates whether the object has already been disposed.
        /// </summary>
        protected bool Disposed => _disposed;

        /// <summary>
        /// Gets or sets a value indicating whether exceptions during disposing members should be ignored.
        /// </summary>
        public bool IgnoreExceptionsInCatch
        {
            get => _ignoreExceptionsInCatch;
            set => _ignoreExceptionsInCatch = value;
        }

        /// <summary>
        /// Disposes the resources managed by the class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true); // Dispose managed and unmanaged resources.
            GC.SuppressFinalize(this); // Prevent finalizer from running.
        }

        /// <summary>
        /// The core disposal method.
        /// </summary>
        /// <param name="disposing">
        /// If true, both managed and unmanaged resources are released. 
        /// If false, only unmanaged resources are released.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return; // Prevent multiple disposals.

            if (disposing)
            {
                // Dispose managed resources.
                DisposeProperties();
                DisposeFields();

                // Dispose custom managed resources.
                ClearManagedResources();
            }

            // Dispose unmanaged resources.
            ClearUnmanagedResources();

            _disposed = true; // Mark as disposed.
        }

        // Disposes all IDisposable properties of the object.
        private void DisposeProperties()
        {
            Type type = GetType();
            List<PropertyInfo> properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(prop => prop.CanRead) // Only include readable properties.
                .ToList();

            foreach (PropertyInfo property in properties)
            {
                try
                {
                    object value = property.GetValue(this); // Get the property value.
                    if (value == null || _disposedObjects.Contains(value)) continue;  // Skip null or already disposed objects.
                    DisposeValue(value, $"{type.FullName}{property.Name}"); // Dispose the property if applicable.
                }
                catch (Exception ex)
                {
                    if (!_ignoreExceptionsInCatch)
                        throw new InvalidOperationException($"Failed to dispose property {property.Name}: {ex}", ex);
                }
            }
        }

        // Disposes all IDisposable fields of the object.
        private void DisposeFields()
        {
            Type type = GetType();
            FieldInfo[] fields = type
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                try
                {
                    object value = field.GetValue(this); // Get the field value.
                    if (value == null || _disposedObjects.Contains(value)) continue;  // Skip null or already disposed objects.
                    DisposeValue(value, $"{type.FullName}{field.Name}"); // Dispose the field if applicable.
                }
                catch (Exception ex)
                {
                    if (!_ignoreExceptionsInCatch)
                        throw new InvalidOperationException($"Failed to dispose field {field.Name}: {ex}", ex);
                }
            }
        }

        // Disposes a single value, checking if it is an IDisposable or a collection.
        // The source (property or field) of the value used for logging.
        private void DisposeValue(object value, string source)
        {
            if (value is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose(); // Dispose the resource.
                }
                catch (Exception ex)
                {
                    if (!_ignoreExceptionsInCatch)
                        throw new InvalidOperationException($"Failed to dispose {source}: {ex}", ex);
                }
                finally
                {
                    _disposedObjects.Add(value); // Mark as disposed.
                }
            }
            else if (value is IEnumerable enumerable)
            {
                DisposeEnumerable(enumerable, source); // Dispose each item in the collection.
            }
        }

        // Iterates through an IEnumerable and disposes each item that implements IDisposable.
        // The source (property or field) of the enumerable used for logging.
        private void DisposeEnumerable(IEnumerable enumerable, string source)
        {
            foreach (object item in enumerable)
            {
                if (item is IDisposable disposableItem && !_disposedObjects.Contains(item))
                {
                    try
                    {
                        disposableItem.Dispose(); // Dispose the item.
                    }
                    catch (Exception ex)
                    {
                        if(!_ignoreExceptionsInCatch)
                            throw new InvalidOperationException($"Failed to dispose item in collection {source}: {ex}", ex);
                    }
                    finally
                    {
                        _disposedObjects.Add(item); // Mark as disposed.
                    }
                }
            }
        }

        /// <summary>
        /// A virtual method to clear additional managed resources.
        /// Override this method to implement custom managed resource cleanup logic.
        /// </summary>
        protected virtual void ClearManagedResources()
        {
            // Custom logic for clearing managed resources should go here.
        }

        /// <summary>
        /// A virtual method to clear unmanaged resources.
        /// Override this method to implement custom unmanaged resource cleanup logic.
        /// </summary>
        protected virtual void ClearUnmanagedResources()
        {
            // Custom logic for clearing unmanaged resources should go here.
        }

        /// <summary>
        /// Finalizer that ensures unmanaged resources are released if Dispose was not called explicitly.
        /// </summary>
        ~Disposable()
        {
            Dispose(false); // Dispose only unmanaged resources.
        }
    }
}
