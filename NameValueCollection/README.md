# Description

__NameValueCollection\<T\>__ is a generic collection of associated string keys and given type values that can be accessed either with the key or with the index.

## Contents

1. [Introduction](#introduction)  
2. [Background](#background)  
    - [The Class Definition](#the-class-definition)  
    - [Base Methods](#base-methods)  
    - [Properties](#properties)  
    - [Collection Enumeration](#collection-enumeration)  
3. [Usage](#usage)  

## Introduction

Built into mscorlib realization of [NameValueCollection](http://docs.microsoft.com/en-us/dotnet/api/system.collections.specialized.namevaluecollection?view=net-5.0) is a collection that is similar to a [Dictionary<string,string>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-5.0) but [NameValueCollection](http://docs.microsoft.com/en-us/dotnet/api/system.collections.specialized.namevaluecollection?view=net-5.0) can have duplicate keys while [Dictionary<string,string>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-5.0) cannot. Elements can be obtained both by index and by key.
What makes this collection special, is that one key can contain several elements and that null is allowed as a key or as a value. But there is one small problem. [NameValueCollection](http://docs.microsoft.com/en-us/dotnet/api/system.collections.specialized.namevaluecollection?view=net-5.0) assumes that strings are used as both keys and values. So what if you want to store values of any type, not only string? Of course, you can convert the text to the desired type every time you get a value, but there are three significant limitations here:

- processing overhead for conversion;
- not all types support conversion to and from a string;
- references to the original objects are not preserved.


The need to store objects in a collection in the original type attracted me to write a generic form of [NameValueCollection\<T\>](https://github.com/ng256/NameValueCollection/blob/main/NameValueCollection.cs) as an alternative to [NameValueCollection](http://github.com/microsoft/referencesource/blob/master/System/compmod/system/collections/specialized/namevaluecollection.cs).

## Background

The [NameValueCollection\<T\>](https://github.com/ng256/NameValueCollection/blob/main/NameValueCollection.cs) collection is based on [NameObjectCollectionBase](http://docs.microsoft.com/en-us/dotnet/api/system.collections.specialized.nameobjectcollectionbase?view=net-5.0) - the base class for a collection of associated string keys and object values that contains base methods to access the values. The interfaces [IDictionary](https://docs.microsoft.com/en-us/dotnet/api/system.collections.idictionary?view=net-5.0), [IEnumerable\<KeyValuePair\<string,T\>\>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1?view=net-5.0) were implemented in the class as additional usability.
  
### The Class Definition

For the first time, define the class and its members that contain keys and values. Private fields will contain the cached data in specified arrays. The __InvalidateCachedArrays__ method will reset the caches and will be called every time the data changes.

```csharp
public partial class NameValueCollection<T> : NameObjectCollectionBase
{
    private string[] _keys; // Cached keys.
    private T[] _values;    // Cached values.

    // Resets the caches.
    protected void InvalidateCachedArrays()
    {
        _values = null;
        _keys = null;
    }
}
```

### Base Methods

The next step, it's time to add in the class methods that can get, set and remove data in collection using base class methods.

The __Add__ and __Set__ methods put received values into the list that paired with the specified key.

```csharp
public partial class NameValueCollection<T>
{
    // Adds single value to collection.
    public void Add(string name, T value)
    {
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

    // Adds range of values to collection.
    public void Add(NameValueCollection<T> collection)
    {
        InvalidateCachedArrays();
        int count = collection.Count;
        for (int i = 0; i < count; i++)
        {
            string key = collection.GetKey(i);
            T[] values = collection.Get(i);
            foreach (var value in values)
            {
                Add(key, value);
            }
        }
    }

    // Set single value (previous values will be removed).
    public void Set(string name, T value)
    {
        InvalidateCachedArrays();
        BaseSet(name, new List<T>(1){value});
    }

    // Set range of values (previous values will be removed).
    public void Set(string name, params T[] values)
    {
        InvalidateCachedArrays();
        BaseSet(name, new List<T>(values));
    }
}
```

The __GetKey__ and __Get__ methods return the requested key and array of associated with key values. The GetAllValues returns all values regardless of keys they are paired with, these method will be useful in the future.

```csharp
public partial class NameValueCollection<T>
{
    // Gets all values cache.
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

    // Gets all values that paired with specified key.
    public T[] Get(string name)
    {
        return ((List<T>)BaseGet(name)).ToArray();
    }

    // Gets all values at the specified index of collection.
    public T[] Get(int index)
    {
        return ((List<T>)BaseGet(index)).ToArray();
    }

    // Gets string containing the key at the specified index.
    public string GetKey(int index)
    {
        return BaseGetKey(index);
    }
}
```

The __Clear__ and __Remove__ methods delete values from the collection.

```csharp
public partial class NameValueCollection<T>
{
    // Removes values from the specified key.
    public void Remove(string name)
    {
        InvalidateCachedArrays();
        BaseRemove(name);
    }

    // Removes all data from the collection.
    public void Clear()
    {
        InvalidateCachedArrays();
        BaseClear();
    }
}
```

### Properties

Almost done! To make this collection easier to use, it's a good idea to add properties. The Keys and Values properties attempt to return cached data and update it if the cache is invalidated.

```csharp
public partial class NameValueCollection<T>
{
    // All keys that the current collection contains.
    public string[] Keys
    {
        get
        {
            if (_keys == null)
                _keys = BaseGetAllKeys();
            return _keys;
        }
    }

    // All values that the current collection contains.
    public T[] Values
    {
        get
        {
            if (_values == null)
                _values = GetAllValues();
            return _values;
        }
    }

    // Values at the specified index.
    public T[] this[int index]
    {
        get
        {
            return Get(index);
        }
        set
        {
            BaseSet(index, new List<T>(value));
        }
    }

    // Values at the specified key.
    public T[] this[string name]
    {
        get
        {
            return Get(name);
        }
        set
        {
            BaseSet(name, new List<T>(value));
        }
    }
}
```

### Collection Enumeration

The embedded class __Enumerator__ will be responsible for enumerating all key-values pairs in the collection. The __GetAllEntries__ method return all key-values pairs that are used by the enumerator.

```csharp
public partial class NameValueCollection<T>
{
    // Enumerates all entries.
    protected IEnumerable<KeyValuePair<string, T>> GetAllEntries()
    {
        return
            from key in Keys
            from value in Get(key)
            select new KeyValuePair<string, T>(key, value);
    }

    // The enumerator that can enumerate all entries in the collection.

    private class Enumerator : IEnumerator<KeyValuePair<string, T>>, IDictionaryEnumerator
    {
        // Enumerate all entries in collection.
        private readonly IEnumerator<KeyValuePair<string, T>> _enumerator;

        // Initialize the enumerator.
        public Enumerator(NameValueCollection<T> collection)
        {
            IEnumerable<KeyValuePair<string, T>> entries =
                collection.GetAllEntries().ToArray();
            _enumerator = entries.GetEnumerator();
        }

        // Returns the element of the collection corresponding
        // to the current position of the enumerator.
        KeyValuePair<string, T> IEnumerator<KeyValuePair<string, T>>.Current
        {
            get { return _enumerator.Current; }
        }

        // Returns the object of the collection corresponding
        // to the current position of the enumerator.
        object IEnumerator.Current
        {
            get
            {
                IEnumerator enumerator = ((IEnumerator) _enumerator);
                return enumerator.Current;
            }
        }

        // Gets the key of the current dictionary entry.
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

}
```
  
The last step is to implement the specified interfaces [IDictionary](https://docs.microsoft.com/en-us/dotnet/api/system.collections.idictionary?view=net-5.0) and  [IEnumerable\<KeyValuePair\<string,T\>\>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1?view=net-5.0). Some methods and properties are implemented explicitly, that is, their use requires a prior casting to the appropriate interface's type.

```csharp
public partial class NameValueCollection<T> :
  IDictionary,
  IEnumerable<KeyValuePair<string, T>>
{
    // Gets an collection containing the values.
    ICollection IDictionary.Keys => Keys;

    // Gets an collection containing the values.
    ICollection IDictionary.Values => Values;

    // Indicates whether the collection is read-only.
    public new bool IsReadOnly => base.IsReadOnly;

    // Indicates whether the collection contains a fixed number of items.
    public bool IsFixedSize => false;

    // Determines whether the collection contains an element with the specified key.
    bool IDictionary.Contains(object key)
    {
        return key is string s && Keys.Contains(s);
    }

    // Adds an object with the specified key to the colletion.
    void IDictionary.Add(object key, object value)
    {
        Add((string)key, (T)value);
    }

    // Removes the element with the specified key from the collection.
    void IDictionary.Remove(object key)
    {
       Remove((string)key);
    }

    // Gets or sets the item with the specified key.
    object IDictionary.this[object key]
    {
        get
        {
            return Get((string)key);
        }
        set
        {
            if (value is IEnumerable<T> collection)
            {
                Set((string)key, (T[])collection.ToArray());
            }
            else
            {
                Set((string)key, (T)value);
            }
        }
    }

    // The three methods below return an enumerator for current collection.
    IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator()
    {
        return new Enumerator(this);
    }

    public override IEnumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return new Enumerator(this);
    }
}
```
## Usage

```csharp
NameValueCollection<int> collection = new NameValueCollection<int>();
collection.Add("a", 123);
collection.Add("a", 456);      // 123 and 456 will be inserted into the same key.
collection.Add("b", 789);      // 789 will be inserted into another key.

int[] a = collection.Get("a"); // contains 123 and 456.
int[] b = collection.Get("b"); // contains 789.
```

At the end of this article, I would like to tell that the above code implements the basic features. In the [attached file](https://github.com/ng256/NameValueCollection/blob/main/NameValueCollection.cs), you will find the full source code containing some additional extensions that are not included in the article.
