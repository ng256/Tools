using System.Linq;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;
using static System.Extensions;

namespace System.Collections.Generic
{

    /// <summary>
    /// Represents a generic collection of associated <see cref="string" /> keys
    /// and <typeparamref name="T"/> values that can be accessed either with the key or with the index.
    /// </summary>
    /// <typeparam name="T">
    /// Value type.
    /// </typeparam>
    [Serializable]
    [DebuggerDisplay("Count = {Count.ToString()}")]
    [DebuggerTypeProxy(typeof(NameValueCollection<>.Enumerator))]
    public class NameValueCollection<T> : NameObjectCollectionBase, IDictionary, IEnumerable<KeyValuePair<string, T>>
    {
        private T[] _values;
        private string[] _keys;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValueCollection{T}" /> class that is empty,
        /// has the default initial capacity and uses the default case-insensitive hash code provider
        /// and the default case-insensitive comparer.
        /// </summary>
        public NameValueCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValueCollection{T}" /> class that is empty,
        /// has the default initial capacity and uses the specified hash code provider and the specified comparer.
        /// </summary>
        /// <param name="hashProvider">
        /// The <see cref="T:System.Collections.IHashCodeProvider" />
        /// that will supply the hash codes for all keys in the <see cref="NameValueCollection{T}" />.
        /// </param>
        /// <param name="comparer">
        /// The <see cref="IComparer" /> to use to determine whether two keys are equal.
        /// </param>
        [Obsolete("Please use NameValueCollection<T>(IEqualityComparer) instead.")]
        public NameValueCollection(IHashCodeProvider hashProvider, IComparer comparer)
            : base(hashProvider, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValueCollection{T}" /> class that is empty,
        /// has the specified initial capacity and uses the default case-insensitive hash code provider
        /// and the default case-insensitive comparer.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of entries that the <see cref="NameValueCollection{T}" /> can contain.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="capacity" /> is less than zero.
        /// </exception>
        public NameValueCollection(int capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValueCollection{T}" /> class that is empty,
        /// has the default initial capacity, and uses the specified <see cref="IEqualityComparer" /> object.
        /// </summary>
        /// <param name="equalityComparer">The <see cref="IEqualityComparer" /> object to use
        /// to determine whether two keys are equal and to generate hash codes for the keys in the collection.
        /// </param>
        public NameValueCollection(IEqualityComparer equalityComparer)
            : base(equalityComparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValueCollection{T}" /> class that is empty,
        /// has the specified initial capacity, and uses the specified <see cref="IEqualityComparer" /> object.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of entries that the <see cref="NameValueCollection{T}" /> object can contain.
        /// </param>
        /// <param name="equalityComparer">
        /// The <see cref="IEqualityComparer" /> object to use to determine whether two keys are equal
        /// and to generate hash codes for the keys in the collection.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="capacity" /> is less than zero.
        /// </exception>
        public NameValueCollection(int capacity, IEqualityComparer equalityComparer)
            : base(capacity, equalityComparer)
        {
        }

        /// <summary>
        /// Copies the entries from the specified <see cref="NameValueCollection{T}" />
        /// to a new <see cref="NameValueCollection{T}" />
        /// with the specified initial capacity or the same initial capacity
        /// as the number of entries copied, whichever is greater,
        /// and using the default case-insensitive hash code provider
        /// and the default case-insensitive comparer.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of entries that the <see cref="NameValueCollection{T}" /> can contain.
        /// </param>
        /// <param name="col">
        /// The <see cref="NameValueCollection{T}" /> to copy to the new <see cref="NameValueCollection{T}"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="capacity" /> is less than zero.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="col" /> is <see langword="null" />.
        /// </exception>
        public NameValueCollection(int capacity, NameValueCollection<T> col)
            : base(capacity)
        {
            if (col == null)
                throw new ArgumentNullException(nameof(col), 
                    GetResourceString("ArgumentNull_Collection"));
            Add(col);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValueCollection{T}" /> class that is empty,
        /// has the specified initial capacity and uses the specified hash code provider and the specified comparer.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of entries that the <see cref="NameValueCollection{T}" /> can contain.
        /// </param>
        /// <param name="hashProvider">
        /// The <see cref="IHashCodeProvider" /> that will supply the hash codes
        /// for all keys in the <see cref="NameValueCollection{T}" />.
        /// </param>
        /// <param name="comparer">
        /// The <see cref="IComparer" /> to use to determine whether two keys are equal.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="capacity" /> is less than zero.</exception>
        [Obsolete("Please use NameValueCollection<T>(Int32, IEqualityComparer) instead.")]
        public NameValueCollection(int capacity, IHashCodeProvider hashProvider, IComparer comparer)
            : base(capacity, hashProvider, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameValueCollection{T}" /> class that is serializable
        /// and uses the specified <see cref="SerializationInfo" /> and <see cref="StreamingContext" />.
        /// </summary>
        /// <param name="info">
        /// A <see cref="SerializationInfo" /> object that contains the information
        /// required to serialize the new <see cref="NameValueCollection{T}"/>.
        /// </param>
        /// <param name="context">A <see cref="StreamingContext" /> object that contains the source
        /// and destination of the serialized stream associated with the new <see cref="NameValueCollection{T}"/>.
        /// </param>
        protected NameValueCollection(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Resets the cached arrays of the collection to <see langword="null" />.
        /// </summary>
        protected void InvalidateCachedArrays()
        {
            _values = null;
            _keys = null;
        }

        /// <summary>
        /// Returns an array that contains all the values in the <see cref="NameValueCollection{T}"/>.
        /// </summary>
        /// <returns>
        /// An array that contains all the values in the <see cref="NameValueCollection{T}"/>.
        /// </returns>
        protected T[] GetAllValues()
        {
            int count = Count;
            List<T> list = new List<T>(count);
            for (int i = 0; i < count; ++i)
            {
                list.AddRange(Get(i));
            }

            return list.ToArray();
        }

        /// <summary>
        /// Gets collection of <see cref="DictionaryEntry"/> entries that contains
        /// all keys and values pairs in the <see cref="NameValueCollection{T}" />.
        /// </summary>
        /// <returns>
        /// A collection of <see cref="DictionaryEntry"/> that contains
        /// all the entries in the <see cref="NameValueCollection{T}"/>.
        /// </returns>
        protected IEnumerable<KeyValuePair<string, T>> GetAllEntries()
        {
            return 
                from key in Keys
                from value in Get(key)
                select new KeyValuePair<string, T>(key, value);
        }

        /// <summary>Adds an entry with the specified name and value to the <see cref="NameValueCollection{T}" />.</summary>
        /// <param name="name">The <see cref="string" /> key of the entry to add. The key can be <see langword="null" />.</param>
        /// <param name="value">The value of the entry to add. The value can be <see langword="null" />.</param>
        /// <exception cref="NotSupportedException">The collection is read-only. </exception>
        public virtual void Add(string name, T value)
        {
            if (IsReadOnly)
                throw new NotSupportedException(GetResourceString("CollectionReadOnly"));
            InvalidateCachedArrays();
            List<T> list = BaseGet(name) as List<T>;
            if (list == null)
            {
                list = new List<T>(1) {value};
                BaseAdd(name, list);
            }
            else
            {
                if (value == null) return;
                list.Add(value);
            }
        }

        /// <summary>
        /// Copies the entries in the specified <see cref="NameValueCollection{T}" />
        /// to the current <see cref="NameValueCollection{T}" />.
        /// </summary>
        /// <param name="collection">
        /// The <see cref="NameValueCollection{T}" /> to copy
        /// to the current <see cref="NameValueCollection{T}" />.
        /// </param>
        /// <exception cref="T:System.NotSupportedException">
        /// The collection is read-only.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="collection" /> is <see langword="null" />.
        /// </exception>
        public void Add(NameValueCollection<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection), 
                    GetResourceString("ArgumentNull_Collection"));
            InvalidateCachedArrays();
            int count = collection.Count;
            for (int i = 0; i < count; i++)
            {
                string key = collection.GetKey(i);
                T[] values = collection.Get(i);
                if (values != null)
                {
                    foreach (var value in values)
                        Add(key, value);
                }
                else
                    Add(key, default(T));
            }
        }

        /// <summary>
        /// Gets the values associated with the specified key
        /// from the <see cref="NameValueCollection{T}" />.
        /// </summary>
        /// <param name="name">
        /// The <see cref="string"/> key of the entry that contains the values to get.
        /// The key can be <see langword="null" />.
        /// </param>
        /// <returns>
        /// An array that contains the values associated
        /// with the specified key from the <see cref="NameValueCollection{T}" />,
        /// if found; otherwise, <see langword="null" />.
        /// </returns>
        public virtual T[] Get(string name)
        {
            List<T> list = BaseGet(name) as List<T>;
            return list?.ToArray();
        }

        /// <summary>
        /// Gets the values at the specified index of the <see cref="NameValueCollection{T}" />.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the entry that contains the values to get from the collection.
        /// </param>
        /// <returns>
        /// An array that contains the values at the specified index of the <see cref="NameValueCollection{T}" />,
        /// if found; otherwise, <see langword="null" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index" /> is outside the valid range of indexes for the collection.
        /// </exception>
        public virtual T[] Get(int index)
        {
            List<T> list = BaseGet(index) as List<T>;
            return list?.ToArray();
        }

        /// <summary>Sets the value of an entry in the <see cref="NameValueCollection{T}" />.
        /// </summary>
        /// <param name="name">
        /// The <see cref="string" /> key of the entry to add the new value to.
        /// The key can be <see langword="null" />.
        /// </param>
        /// <param name="value">
        /// The new value to add to the specified entry.
        /// The value can be <see langword="null" />.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// The collection is read-only.
        /// </exception>
        public virtual void Set(string name, T value)
        {
            if (IsReadOnly)
                throw new NotSupportedException(GetResourceString("CollectionReadOnly"));
            InvalidateCachedArrays();
            BaseSet(name, new List<T>(1) {value});
        }

        /// <summary>Sets the value of an entry in the <see cref="NameValueCollection{T}" />.
        /// </summary>
        /// <param name="name">
        /// The <see cref="string" /> key of the entry to add the new value to.
        /// The key can be <see langword="null" />.
        /// </param>
        /// <param name="values">
        /// New values to add to the specified entry.
        /// The value can be <see langword="null" />.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// The collection is read-only.
        /// </exception>
        public virtual void Set(string name, params T[] values)
        {
            if (IsReadOnly)
                throw new NotSupportedException(GetResourceString("CollectionReadOnly"));
            InvalidateCachedArrays();
            BaseSet(name, new List<T>(values));
        }

        /// <summary>
        /// Invalidates the cached arrays and removes all entries from the <see cref="NameValueCollection{T}" />.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">
        /// The collection is read-only.
        /// </exception>
        public virtual void Clear()
        {
            if (IsReadOnly)
                throw new NotSupportedException(GetResourceString("CollectionReadOnly"));
            InvalidateCachedArrays();
            BaseClear();
        }

        /// <summary>
        /// Copies the entire <see cref="NameValueCollection{T}" />
        /// to a compatible one-dimensional <see cref="Array" />,
        /// starting at the specified index of the target array.</summary>
        /// <param name="dest">The one-dimensional <see cref="Array" />
        /// that is the destination of the elements copied from <see cref="NameValueCollection{T}" />.
        /// The <see cref="Array" /> must have zero-based indexing.</param>
        /// <param name="index">
        /// The zero-based index in <paramref name="dest" /> at which copying begins.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dest" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index" /> is less than zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="dest" /> is multidimensional.
        /// -or-
        /// The number of elements in the source <see cref="NameValueCollection{T}" />
        /// is greater than the available space from <paramref name="index" />
        /// to the end of the destination <paramref name="dest" />.
        /// </exception>
        /// <exception cref="T:System.InvalidCastException">
        /// The type of the source <see cref="NameValueCollection{T}" />
        /// cannot be cast automatically to the type of the destination <paramref name="dest" />.
        /// </exception>
        public virtual void CopyTo(T[] dest, int index)
        {
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));
            if (dest.Rank != 1)
                throw new ArgumentException(
                    GetResourceString("Arg_MultiRank"));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index),
                    GetResourceString("IndexOutOfRange",
                        (object) index.ToString(CultureInfo.CurrentCulture)));
            if (_values == null) _values = GetAllValues();
            int count = _values.Length;
            if (dest.Length - index < count)
                throw new ArgumentException(
                    GetResourceString("Arg_InsufficientSpace"));
            for (int i = 0; i < count; i++)
                dest.SetValue(_values[i], i + index);
        }

        /// <summary>
        /// Removes the entries with the specified key from the <see cref="NameValueCollection{T}"/>.
        /// </summary>
        /// <param name="name">
        /// The <see cref="string" /> key of the entry to remove. The key can be <see langword="null" />.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// The collection is read-only.
        /// </exception>
        public virtual void Remove(string name)
        {
            InvalidateCachedArrays();
            BaseRemove(name);
        }

        /// <summary>Gets a value indicating whether the <see cref="NameValueCollection{T}" />
        /// contains keys that are not <see langword="null" />.</summary>
        /// <returns>
        /// <see langword="true" /> if the <see cref="NameValueCollection{T}" />
        /// contains keys that are not <see langword="null" />; otherwise, <see langword="false" />.
        /// </returns>
        public bool HasKeys()
        {
            return BaseHasKeys();
        }

        /// <summary>
        /// Gets the key at the specified index of the <see cref="NameValueCollection{T}" />.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the key to get from the collection.
        /// </param>
        /// <returns>
        /// A <see cref="string" /> that contains the key at the specified index of the <see cref="NameValueCollection{T}" />,
        /// if found; otherwise, <see langword="null" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index" /> is outside the valid range of indexes for the collection.
        /// </exception>
        public virtual string GetKey(int index)
        {
            return BaseGetKey(index);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets all the keys in the <see cref="NameValueCollection{T}" />.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> array that contains all the keys of the <see cref="NameValueCollection{T}" />.
        /// </returns>
        public new string[] Keys
        {
            get
            {
                if (_keys == null)
                    _keys = BaseGetAllKeys();
                return _keys;
            }
        }

        /// <summary>
        /// Gets all the values in the <see cref="NameValueCollection{T}" />.
        /// </summary>
        /// <returns>
        /// An array that contains all the values of the <see cref="NameValueCollection{T}" />.
        /// </returns>
        public virtual T[] Values
        {
            get
            {
                if (_values == null)
                    _values = GetAllValues();
                return _values;
            }
        }

        /// <summary>
        /// Gets of sets the values at the specified index of the <see cref="NameValueCollection{T}" />.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the entry to locate in the collection.
        /// </param>
        /// <returns>
        /// An array that contains all the values at the specified index of the <see cref="NameValueCollection{T}" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index" /> is outside the valid range of indexes for the collection.
        /// </exception>
        public T[] this[int index]
        {
            get { return Get(index); }
            set { BaseSet(index, new List<T>(value)); }
        }

        /// <summary>
        /// Gets of sets the values at the specified name of the <see cref="NameValueCollection{T}" />.
        /// </summary>
        /// <param name="name">
        /// A <see cref="string" /> that contains the name of the entry to locate in the collection.
        /// </param>
        /// <returns>
        /// An array that contains all the values at the specified name of the <see cref="NameValueCollection{T}" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="name" /> is outside the valid range of names for the collection.
        /// </exception>
        public T[] this[string name]
        {
            get { return Get(name); }
            set { BaseSet(name, new List<T>(value)); }
        }

        #endregion

        #region IDictionary, IEnumerable

        /// <inheritdoc cref="IDictionary.Values"/>
        ICollection IDictionary.Values => Values;

        /// <inheritdoc cref="IDictionary.IsReadOnly"/>
        public new bool IsReadOnly => base.IsReadOnly;

        /// <inheritdoc cref="IDictionary.IsFixedSize"/>
        public bool IsFixedSize => false;

        /// <inheritdoc cref="IDictionary.Keys"/>
        ICollection IDictionary.Keys => Keys;

        /// <inheritdoc cref="IDictionary.Contains"/>
        bool IDictionary.Contains(object key)
        {
            return key is string s && Keys.Contains(s);
        }

        /// <inheritdoc cref="IDictionary.Add"/>
        void IDictionary.Add(object key, object value)
        {
            ((NameValueCollection<T>) this).Add(key.CastTo<string>(nameof(key)), 
                value.CastTo<T>(nameof(value)));
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public override IEnumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc cref="IDictionary.GetEnumerator"/>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc cref="IDictionary.Remove"/>
        void IDictionary.Remove(object key)
        {
            Remove(key.CastTo<string>(nameof(key)));
        }

        /// <inheritdoc cref="IDictionary.this"/>
        object IDictionary.this[object key]
        {
            get
            {
                return Get(key.CastTo<string>(nameof(key)));
            }
            set
            {
                string k = key.CastTo<string>(nameof(key));
                if (value is IEnumerable<T> collection)
                {
                    ((NameValueCollection<T>) this).Set(k, collection.ToArray());
                }
                else
                {
                    ((NameValueCollection<T>) this).Set(k, value.CastTo<T>(nameof(value)));
                }
            }
        }

        #endregion

        #region Enumerator

        private class Enumerator : IEnumerator<KeyValuePair<string, T>>, IDictionaryEnumerator
        {
            // A special property for viewing entries in the debugger.
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            private IEnumerable<KeyValuePair<string, T>> Entries { get; }

            // Enumerate all entries in collection.
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly IEnumerator<KeyValuePair<string, T>> _enumerator;

            // Initialize the enumerator.
            public Enumerator(NameValueCollection<T> collection)
            {
                IEnumerable<KeyValuePair<string, T>> entries = 
                    collection.GetAllEntries().ToArray();
                Entries = entries;
                _enumerator = entries.GetEnumerator();
            }


            // Returns the element of the collection corresponding
            // to the current position of the enumerator. 
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            KeyValuePair<string, T> IEnumerator<KeyValuePair<string, T>>.Current
            {
                get { return _enumerator.Current; }
            }

            // Returns the object of the collection corresponding
            // to the current position of the enumerator. 
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            object IEnumerator.Current
            {
                get
                {
                    IEnumerator enumerator = ((IEnumerator) _enumerator);
                    return enumerator.Current;
                }
            }

            // Gets the key of the current dictionary entry.
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            object IDictionaryEnumerator.Key
            {
                get
                {
                    IEnumerator<KeyValuePair<string, T>> enumerator = 
                        ((IEnumerator<KeyValuePair<string, T>>) this);
                    return enumerator.Current.Key;
                }
            }

            // Gets the value of the current dictionary entry.
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            object IDictionaryEnumerator.Value
            {
                get
                {
                    IEnumerator<KeyValuePair<string, T>> enumerator = 
                        ((IEnumerator<KeyValuePair<string, T>>) this);
                    return enumerator.Current.Value;
                }
            }

            // Gets both the key and the value of the current dictionary entry.
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    IEnumerator<KeyValuePair<string, T>> enumerator = 
                        ((IEnumerator<KeyValuePair<string, T>>) this);
                    return new DictionaryEntry(enumerator.Current.Key,
                        enumerator.Current.Value);
                }
            }

            // Move enumerator to the next entry.
            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            // Reset enumerator to start position.
            public void Reset()
            {
                _enumerator.Reset();
            }

            // Unused dispose pattern.
            public void Dispose()
            {
            }
        }

        #endregion
    }
}

#region Extensions

namespace System
{
    // Internal tools.
    internal static class Extensions
    {
        // Mscorlib resources.
        private static ResourceSet _mscorlib = null;

        // Gets mscorlib internal error message.
        internal static string GetResourceString(string name)
        {
            if (_mscorlib == null)
            {
                var a = Assembly.GetAssembly(typeof(object));
                var n = a.GetName().Name;
                var m = new ResourceManager(n, a);
                _mscorlib = m.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            }

            return _mscorlib.GetString(name);
        }

        // Gets parametrized mscorlib internal error message.
        internal static string GetResourceString(string name, params object[] args)
        {
            return string.Format(GetResourceString(name) ?? throw new ArgumentNullException(nameof(name)), args);
        }

        // Casts an object to the specified type.
        internal static T CastTo<T>(this object source, string name)
        {
            switch (source)
            {
                case null:
                    return default(T) == null // Check if the type T is nullable.
                        ? default(T)
                        : throw new ArgumentNullException(name, GetResourceString("Arg_NullReferenceException"));
                case T dest:
                    return dest;
                default:
                    throw new ArgumentException(GetResourceString("Arg_WrongType", source, typeof(T)), name);
            }
        }
    }
}

#endregion