/*******************************************************************

•   File: ThreadSafeSortedSet.cs

•   Description

    The  ThreadSafeSortedSet<T> class  represents  a thread-safe
    set thatstores elements in a sorted order. It implements the
    interfaces    ISet<T>,     ICollection<T>,   IEnumerable<T>,
    ISerializable,  and IDeserializationCallback.   It  provides
    safe access to  the   collection from multiple   threads and
    supports collection operations such as adding, removing, and
    searching   for  elements. The  class   requires  type  T to
    implement   the   IComparable<T>    interface  for   correct
    comparison  of elements.

•   License

	This software is distributed under the MIT License (MIT)

    © 2024 Pavel Bashkardin.

    Permission is  hereby granted, free of charge, to any person
	obtaining   a copy    of    this  software    and associated
	documentation  files  (the “Software”),    to  deal   in the
	Software without  restriction, including without  limitation
	the rights to use, copy, modify, merge, publish, distribute,
	sublicense,  and/or  sell  copies   of  the Software, and to
	permit persons to whom the Software  is furnished to  do so,
	subject to the following conditions:

	The above copyright  notice and this permission notice shall
	be  included  in all copies   or substantial portions of the
	Software.

	THE  SOFTWARE IS  PROVIDED  “AS IS”, WITHOUT WARRANTY OF ANY
	KIND, EXPRESS  OR IMPLIED, INCLUDING  BUT NOT LIMITED TO THE
	WARRANTIES  OF MERCHANTABILITY, FITNESS    FOR A  PARTICULAR
	PURPOSE AND NONINFRINGEMENT. IN  NO EVENT SHALL  THE AUTHORS
	OR  COPYRIGHT HOLDERS  BE  LIABLE FOR ANY CLAIM,  DAMAGES OR
	OTHER LIABILITY,  WHETHER IN AN  ACTION OF CONTRACT, TORT OR
	OTHERWISE, ARISING FROM, OUT OF   OR IN CONNECTION  WITH THE
	SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

 ******************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.Serialization;

/// <summary>
/// Represents a thread-safe collection of sorted elements that implement the <see cref="ISet{T}"/> interface.
/// </summary>
/// <typeparam name="T">The type of elements in the sorted set. Must implement <see cref="IComparable{T}"/>.</typeparam>
/// <remarks>
/// This class is designed to ensure safe access in multi-threaded environments and maintains the order of elements.
/// </remarks>
public class ThreadSafeSortedSet<T> : ISet<T>,
    ICollection<T>,
    IEnumerable<T>,
    IEnumerable,
    ICollection,
    IReadOnlyCollection<T>,
    IReadOnlySet<T>,
    ISerializable,
    IDeserializationCallback
    where T : IComparable<T>
{
    private readonly SortedSet<T> _sortedSet;
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadSafeSortedSet{T}"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor creates an empty sorted set that is thread-safe.
    /// </remarks>
    public ThreadSafeSortedSet()
    {
        _sortedSet = new SortedSet<T>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadSafeSortedSet{T}"/> class 
    /// with the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer used to order the elements in the set.</param>
    /// <remarks>
    /// This constructor creates an empty sorted set that is thread-safe and uses 
    /// the provided comparer to sort its elements.
    /// </remarks>
    public ThreadSafeSortedSet(IComparer<T> comparer)
    {
        _sortedSet = new SortedSet<T>(comparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadSafeSortedSet{T}"/> class 
    /// that contains elements copied from the specified collection.
    /// </summary>
    /// <param name="collection">The collection of elements to add to the sorted set.</param>
    /// <remarks>
    /// This constructor creates a sorted set that is thread-safe and initializes it
    /// with the elements of the specified collection.
    /// </remarks>
    public ThreadSafeSortedSet(IEnumerable<T> collection)
    {
        _sortedSet = new SortedSet<T>(collection);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadSafeSortedSet{T}"/> class 
    /// that contains elements copied from the specified collection and uses the specified comparer.
    /// </summary>
    /// <param name="collection">The collection of elements to add to the sorted set.</param>
    /// <param name="comparer">The comparer to use for sorting the elements in the sorted set.</param>
    /// <remarks>
    /// This constructor creates a sorted set that is thread-safe and initializes it
    /// with the elements of the specified collection, using the provided comparer for ordering.
    /// </remarks>
    public ThreadSafeSortedSet(IEnumerable<T> collection, IComparer<T> comparer)
    {
        _sortedSet = new SortedSet<T>(collection, comparer);
    }

    /// <summary>
    /// Adds an item to the <see cref="ThreadSafeSortedSet{T}"/>.
    /// </summary>
    /// <param name="item">The item to add to the sorted set.</param>
    /// <returns>
    /// Returns true if the item was added successfully; otherwise, false if the item 
    /// already exists in the sorted set.
    /// </returns>
    /// <remarks>
    /// This method is thread-safe, allowing concurrent additions to the set.
    /// </remarks>
    public void Add(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            _sortedSet.Add(item);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes all items from the <see cref="ThreadSafeSortedSet{T}"/>.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe and will clear the set for all concurrent users.
    /// </remarks>
    public void Clear()
    {
        _sortedSet.Clear();
    }


    /// <summary>
    /// Removes all elements in the specified collection from the current set.
    /// </summary>
    /// <param name="other">The collection of elements to remove.</param>
    /// <remarks>
    /// This method is thread-safe and will modify the set concurrently with other operations.
    /// </remarks>
    public void ExceptWith(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            _sortedSet.ExceptWith(other);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Modifies the current set to contain only elements that are also in the specified collection.
    /// </summary>
    /// <param name="other">The collection to intersect with.</param>
    /// <remarks>
    /// This method is thread-safe and will modify the set concurrently with other operations.
    /// </remarks>
    public void IntersectWith(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            _sortedSet.IntersectWith(other);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Determines whether the current set is a proper subset of the specified collection.
    /// A proper subset is one that is not equal to the collection.
    /// </summary>
    /// <param name="other">The collection to compare with.</param>
    /// <returns>
    /// true if the current set is a proper subset of the specified collection; otherwise, false.
    /// </returns>
    bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.IsProperSubsetOf(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Determines whether the current set is a proper superset of the specified collection.
    /// A proper superset is one that contains all elements of the collection but is not equal to it.
    /// </summary>
    /// <param name="other">The collection to compare with.</param>
    /// <returns>
    /// true if the current set is a proper superset of the specified collection; otherwise, false.
    /// </returns>
    bool IReadOnlySet<T>.IsProperSupersetOf(IEnumerable<T> other)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.IsProperSupersetOf(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Determines whether the current set is a subset of the specified collection.
    /// A subset is one that contains all elements of the collection or is equal to it.
    /// </summary>
    /// <param name="other">The collection to compare with.</param>
    /// <returns>
    /// true if the current set is a subset of the specified collection; otherwise, false.
    /// </returns>
    bool IReadOnlySet<T>.IsSubsetOf(IEnumerable<T> other)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.IsSubsetOf(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Determines whether the current set is a superset of the specified collection.
    /// A superset is one that contains all elements of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare with.</param>
    /// <returns>
    /// true if the current set is a superset of the specified collection; otherwise, false.
    /// </returns>
    bool IReadOnlySet<T>.IsSupersetOf(IEnumerable<T> other)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.IsSupersetOf(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Determines whether the current set shares any elements with the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare with.</param>
    /// <returns>
    /// true if the current set contains any elements in common with the specified collection; otherwise, false.
    /// </returns>
    bool IReadOnlySet<T>.Overlaps(IEnumerable<T> other)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.Overlaps(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Determines whether the current set is equal to the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare with.</param>
    /// <returns>
    /// true if the current set and the specified collection contain the same elements; otherwise, false.
    /// </returns>
    bool IReadOnlySet<T>.SetEquals(IEnumerable<T> other)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.SetEquals(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Determines whether the current set is a proper subset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare with.</param>
    /// <returns>
    /// true if the current set is a proper subset of the specified collection; otherwise, false.
    /// </returns>
    bool IReadOnlySet<T>.IsProperSubsetOf(IEnumerable<T> other)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.IsProperSubsetOf(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Determines whether the current set is a proper superset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare with.</param>
    /// <returns>
    /// true if the current set is a proper superset of the specified collection; otherwise, false.
    /// </returns>
    bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.IsProperSupersetOf(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Determines whether the current set is a subset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare with.</param>
    /// <returns>
    /// true if the current set is a subset of the specified collection; otherwise, false.
    /// </returns>
    bool ISet<T>.IsSubsetOf(IEnumerable<T> other)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.IsSubsetOf(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Determines whether the current set is a superset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare with.</param>
    /// <returns>
    /// true if the current set is a superset of the specified collection; otherwise, false.
    /// </returns>
    bool ISet<T>.IsSupersetOf(IEnumerable<T> other)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.IsSupersetOf(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Determines if the current set overlaps with the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare with the current set.</param>
    /// <returns>True if the current set contains any elements that are also in the specified collection; otherwise, false.</returns>
    bool ISet<T>.Overlaps(IEnumerable<T> other)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.Overlaps(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Determines whether the current set and the specified collection contain the same elements.
    /// </summary>
    /// <param name="other">The collection to compare against the current set.</param>
    /// <returns>True if the current set and the specified collection contain the same elements; otherwise, false.</returns>
    bool ISet<T>.SetEquals(IEnumerable<T> other)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.SetEquals(other);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Modifies the current set to contain only elements that are present in either the current set or the specified collection, but not both.
    /// </summary>
    /// <param name="other">The collection to compare with the current set.</param>
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            _sortedSet.SymmetricExceptWith(other);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Modifies the current set to contain all elements that are present in the current set, the specified collection, or both.
    /// </summary>
    /// <param name="other">The collection to combine with the current set.</param>
    public void UnionWith(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            _sortedSet.UnionWith(other);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Copies the elements of the current set to an array, starting at a specified array index.
    /// </summary>
    /// <param name="array">The array to copy the elements to.</param>
    /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        _lock.EnterReadLock();
        try
        {
            _sortedSet.CopyTo(array, arrayIndex);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Removes the specified element from the current set.
    /// </summary>
    /// <param name="item">The element to remove from the set.</param>
    /// <returns>True if the element is successfully removed; otherwise, false.</returns>
    public bool Remove(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            return _sortedSet.Remove(item);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Copies the elements of the current set to the specified array, starting at the specified index.
    /// </summary>
    /// <param name="array">The destination array to copy to.</param>
    /// <param name="index">The zero-based index in the array at which storing begins.</param>
    public void CopyTo(Array array, int index)
    {
        _lock.EnterReadLock();
        try
        {
            ((ICollection)_sortedSet).CopyTo(array, index);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the number of elements contained in the set.
    /// </summary>
    /// <returns>The number of elements in the set.</returns>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _sortedSet.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether access to the collection is synchronized (thread safe).
    /// </summary>
    /// <returns>Always returns false, as the inner collection does not guarantee synchronization.</returns>
    public bool IsSynchronized
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return ((ICollection)_sortedSet).IsSynchronized;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Gets an object that can be used to synchronize access to the collection.
    /// </summary>
    /// <returns>The object used to synchronize access.</returns>
    public object SyncRoot
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return ((ICollection)_sortedSet).SyncRoot;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    /// <returns>True if the collection is read-only; otherwise, false.</returns>
    public bool IsReadOnly
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return ((ICollection<T>)_sortedSet).IsReadOnly;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Determines whether the collection contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the collection.</param>
    /// <returns>True if the item is found in the collection; otherwise, false.</returns>
    public bool Contains(T item)
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.Contains(item);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Adds an item to the set.
    /// </summary>
    /// <param name="item">The object to add to the set.</param>
    /// <returns>True if the item was added to the set; otherwise, false.</returns>
    bool ISet<T>.Add(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            return _sortedSet.Add(item);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        _lock.EnterReadLock();
        try
        {
            return _sortedSet.GetEnumerator();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through a non-generic collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the non-generic collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        _lock.EnterReadLock();
        try
        {
            return ((IEnumerable)_sortedSet).GetEnumerator();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Populates a SerializationInfo with the data needed to serialize the target object.
    /// </summary>
    /// <param name="info">The SerializationInfo to populate with data.</param>
    /// <param name="context">The StreamingContext that contains the source of the serialized stream.</param>
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        _lock.EnterReadLock();
        try
        {
            ((ISerializable)_sortedSet).GetObjectData(info, context);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Implements the IDeserializationCallback interface and performs additional processing
    /// when the object is deserialized.
    /// </summary>
    /// <param name="sender">The source of the deserialization event.</param>
    public void OnDeserialization(object? sender)
    {
        _lock.EnterReadLock();
        try
        {
            ((IDeserializationCallback)_sortedSet).OnDeserialization(sender);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
