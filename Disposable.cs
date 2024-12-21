/*
 *  Disposable.cs
 *
 *  A base class that provides a robust implementation of the IDisposable pattern.
 *  Automatically disposes properties and fields of the instance that implement
 *  IDisposable, ensuring resources are released correctly.
 *
 *  Copyright (c) 2024 Bashkardin Pavel
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */

using System.Collections;
using System.Diagnostics;
using System.Reflection;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace System
{
    /// <summary>
    /// A base class that provides a robust implementation of the IDisposable pattern.
    /// Automatically disposes properties and fields of the instance that implement IDisposable,
    /// ensuring resources are released correctly.
    /// </summary>
    public abstract class Disposable : IDisposable, IAsyncDisposable
    {
        private bool _disposed = false; // Tracks whether the object has been disposed.
        private readonly HashSet<object> _disposedObjects = new HashSet<object>(); // Tracks already disposed objects.
        private bool _ignoreExceptions = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Disposable"/> class.
        /// </summary>
        /// <param name="ignoreExceptions">
        /// A value indicating whether exceptions during disposing members should be ignored.
        /// </param>
        protected Disposable(bool ignoreExceptions = false)
        {
            _ignoreExceptions = ignoreExceptions;
        }

        /// <summary>
        /// Indicates whether the object has already been disposed.
        /// </summary>
        protected bool Disposed => _disposed;

        /// <summary>
        /// Gets or sets a value indicating whether exceptions during disposing members should be ignored.
        /// </summary>
        public bool IgnoreExceptions
        {
            get => _ignoreExceptions;
            set => _ignoreExceptions = value;
        }

        #region Synchronous Methods

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
                catch (Exception e)
                {
                    if (!_ignoreExceptions)
                        throw new InvalidOperationException($"Failed to dispose property {property.Name}: {e}", e);
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
                catch (Exception e)
                {
                    if (!_ignoreExceptions)
                        throw new InvalidOperationException($"Failed to dispose field {field.Name}: {e}", e);
                }
            }
        }

        // Disposes a single value, checking if it is an IDisposable or a collection.
        private void DisposeValue(object value, string source)
        {
            if (value is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose(); // Dispose the resource.
                }
                catch (Exception e)
                {
                    if (!_ignoreExceptions)
                        throw new InvalidOperationException($"Failed to dispose {source}: {e}", e);
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
                    catch (Exception e)
                    {
                        if (!_ignoreExceptions)
                            throw new InvalidOperationException($"Failed to dispose item in collection {source}: {e}", e);
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
            ClearManagedResourcesAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// A virtual method to clear unmanaged resources.
        /// Override this method to implement custom unmanaged resource cleanup logic.
        /// </summary>
        protected virtual void ClearUnmanagedResources()
        {
            ClearUnmanagedResourcesAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Finalizer that ensures unmanaged resources are released if Dispose was not called explicitly.
        /// </summary>
        ~Disposable()
        {
            Dispose(false); // Dispose only unmanaged resources.
        }

        #endregion

        #region Asynchronous Methods

        private ValueTask DisposeTask()
        {
            try
            {
                Dispose(); // Synchronously dispose resources
                return ValueTask.CompletedTask; // If Dispose completes successfully, return completed task.
            }
            catch (Exception e)
            {
                // If Dispose throws an exception, return it as part of ValueTask
                return ValueTask.FromException(e);
            }
        }

        /// <summary>
        /// Asynchronously disposes the resources managed by the class.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            try
            {
                await DisposeTask();
                await DisposeAsync(true); // Asynchronously dispose managed and unmanaged resources.
                GC.SuppressFinalize(this); // Prevent finalizer from running.
            }
            catch (Exception e)
            {
                // Return the exception as part of ValueTask if DisposeAsync fails.
                await ValueTask.FromException(e);
            }
        }

        /// <summary>
        /// The core asynchronous disposal method.
        /// </summary>
        /// <param name="disposing">
        /// If true, both managed and unmanaged resources are released asynchronously.
        /// If false, only unmanaged resources are released asynchronously.
        /// </param>
        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (_disposed)
                return; // Prevent multiple disposals.

            if (disposing)
            {
                // Asynchronously dispose managed resources.
                await DisposePropertiesAsync();
                await DisposeFieldsAsync();

                // Asynchronously dispose custom managed resources.
                await ClearManagedResourcesAsync();
            }

            // Asynchronously dispose unmanaged resources.
            await ClearUnmanagedResourcesAsync();

            _disposed = true; // Mark as disposed.
        }

        // Asynchronously disposes all IDisposable properties of the object.
        private async Task DisposePropertiesAsync()
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
                    await DisposeValueAsync(value, $"{type.FullName}{property.Name}"); // Asynchronously dispose the property if applicable.
                }
                catch (Exception e)
                {
                    if (!_ignoreExceptions)
                        throw new InvalidOperationException($"Failed to dispose property {property.Name}: {e}", e);
                }
            }
        }

        // Asynchronously disposes all IDisposable fields of the object.
        private async Task DisposeFieldsAsync()
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
                    await DisposeValueAsync(value, $"{type.FullName}{field.Name}"); // Asynchronously dispose the field if applicable.
                }
                catch (Exception e)
                {
                    if (!_ignoreExceptions)
                        throw new InvalidOperationException($"Failed to dispose field {field.Name}: {e}", e);
                }
            }
        }

        // Asynchronously disposes a single value.
        private async Task DisposeValueAsync(object value, string source)
        {
            if (value is IAsyncDisposable asyncDisposable)
            {
                try
                {
                    await asyncDisposable.DisposeAsync(); // Asynchronously dispose the resource.
                }
                catch (Exception e)
                {
                    if (!_ignoreExceptions)
                        throw new InvalidOperationException($"Failed to dispose {source}: {e}", e);
                }
                finally
                {
                    _disposedObjects.Add(value); // Mark as disposed.
                }
            }
            else if (value is IEnumerable enumerable)
            {
                await DisposeEnumerableAsync(enumerable, source); // Asynchronously dispose each item in the collection.
            }
        }

        // Asynchronously iterates through an IEnumerable and disposes each item.
        private async Task DisposeEnumerableAsync(IEnumerable enumerable, string source)
        {
            foreach (object item in enumerable)
            {
                if (item is IAsyncDisposable asyncDisposableItem && !_disposedObjects.Contains(item))
                {
                    try
                    {
                        await asyncDisposableItem.DisposeAsync(); // Asynchronously dispose the item.
                    }
                    catch (Exception e)
                    {
                        if (!_ignoreExceptions)
                            throw new InvalidOperationException($"Failed to dispose item in collection {source}: {e}", e);
                    }
                    finally
                    {
                        _disposedObjects.Add(item); // Mark as disposed.
                    }
                }
            }
        }

        /// <summary>
        /// A virtual method to asynchronously clear additional managed resources.
        /// Override this method to implement custom asynchronous managed resource cleanup logic.
        /// </summary>
        protected virtual Task ClearManagedResourcesAsync()
        {
            // Custom logic for clearing managed resources should go here.

            return Task.CompletedTask;
        }

        /// <summary>
        /// A virtual method to asynchronously clear unmanaged resources.
        /// Override this method to implement custom asynchronous unmanaged resource cleanup logic.
        /// </summary>
        protected virtual Task ClearUnmanagedResourcesAsync()
        {
            // Custom logic for clearing unmanaged resources should go here.

            return Task.CompletedTask;
        }

        #endregion
    }
}
