using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using com.upokecenter.util;

namespace PeterO.Support {
    /// <summary>Description of Support.</summary>
  public static class Collections {
    /// <summary>Not documented yet.</summary>
    /// <param name='enu'>Not documented yet.</param>
    /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
    /// <returns>A T[] object.</returns>
    public static T[] ToArray<T>(IEnumerable<T> enu) {
      return ((enu as List<T>) ?? (new List<T>(enu))).ToArray();
    }
  }

    /// <summary>Dictionary that allows null keys and doesn't throw
    /// exceptions if keys are not found, both of which are HashMap
    /// behaviors.</summary>
    /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
public sealed class LenientDictionary<TKey, TValue> :
    IDictionary<TKey, TValue> {
    private TValue nullValue;
    private bool hasNull = false;
    private IDictionary<TKey, TValue> valueWrapped;

    /// <summary>Initializes a new instance of the LenientDictionary
    /// class.</summary>
    public LenientDictionary() {
      this.valueWrapped = new Dictionary<TKey, TValue>();
    }

    /// <summary>Initializes a new instance of the LenientDictionary
    /// class.</summary>
    /// <param name='other'>An IDictionary object.</param>
    public LenientDictionary(IDictionary<TKey, TValue> other) {
      if (default(TKey) == null && other.ContainsKey(default(TKey))) {
        // If dictionary contains null, add the values manually,
        // because the constructor will throw an exception
        // otherwise
        this.valueWrapped = new Dictionary<TKey, TValue>();
        foreach (var kvp in other) {
          this.AddInternal(kvp.Key, kvp.Value);
        }
      } else {
        this.valueWrapped = new Dictionary<TKey, TValue>(other);
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='key'>Not documented yet.</param>
    /// <returns>A TValue object.</returns>
    public TValue this[TKey key] {
      get {
        if (Object.Equals(key, null) && this.hasNull && default(TKey) == null) {
 return this.nullValue;
}
        TValue val;
     return this.valueWrapped.TryGetValue(key, out val) ? val :
          default(TValue);
      }

      set {
        if (Object.Equals(key, null) && default(TKey) == null) {
          this.hasNull = true;
          this.nullValue = value;
        }
        this.valueWrapped[key] = value;
      }
    }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
    public ICollection<TKey> Keys {
      get {
        if (this.hasNull) {
          var keys = new List<TKey>(this.valueWrapped.Keys);
          keys.Add(default(TKey));
          return keys;
        } else {
 return this.valueWrapped.Keys;
}
      }
    }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
    public ICollection<TValue> Values {
      get {
        if (this.hasNull) {
          var keys = new List<TValue>(this.valueWrapped.Values);
          keys.Add(this.nullValue);
          return keys;
        } else {
 return this.valueWrapped.Values;
}
      }
    }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
    public int Count {
      get {
        return this.valueWrapped.Count + (this.hasNull ? 1 : 0);
      }
    }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
    public bool IsReadOnly {
      get {
        return false;
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='key'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
    public bool ContainsKey(TKey key) {
    return (Object.Equals(key, null) && default(TKey) == null) ?
        this.hasNull : this.valueWrapped.ContainsKey(key);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='key'>Not documented yet.</param>
    /// <param name='value'>Not documented yet.</param>
    public void Add(TKey key, TValue value) {
      this.AddInternal(key, value);
    }

    private void AddInternal(TKey key, TValue value) {
      if (Object.Equals(key, null)) {
        this.hasNull = true;
        this.nullValue = value;
      } else {
 this.valueWrapped[key] = value;
}
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='key'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
    public bool Remove(TKey key) {
      if (Object.Equals(key, null)) {
        bool ret = this.hasNull;
        this.hasNull = false;
        this.nullValue = default(TValue);
        return ret;
      } else {
 return this.valueWrapped.Remove(key);
}
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='key'>Not documented yet.</param>
    /// <param name='value'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
    public bool TryGetValue(TKey key, out TValue value) {
      if (Object.Equals(key, null)) {
        value = this.hasNull ? this.nullValue : default(TValue);
        return this.hasNull;
      } else {
 return this.valueWrapped.TryGetValue(key, out value);
}
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='item'>Not documented yet.</param>
    public void Add(KeyValuePair<TKey, TValue> item) {
      this.Add(item.Key, item.Value);
    }

    /// <summary>Not documented yet.</summary>
    public void Clear() {
      this.hasNull = true;
      this.nullValue = default(TValue);
      this.valueWrapped.Clear();
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='item'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
    public bool Contains(KeyValuePair<TKey, TValue> item) {
      return this.ContainsKey(item.Key);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='array'>Not documented yet.</param>
    /// <param name='arrayIndex'>Not documented yet.</param>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
      if (array != null && arrayIndex < array.Length && this.hasNull) {
     array[arrayIndex] = new KeyValuePair<TKey,
          TValue>(default(TKey), this.nullValue);
        ++arrayIndex;
      }
      this.valueWrapped.CopyTo(array, arrayIndex);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='item'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
    public bool Remove(KeyValuePair<TKey, TValue> item) {
      return this.Remove(item.Key);
    }

    private IEnumerable<KeyValuePair<TKey, TValue>> Iterator() {
      if (this.hasNull) {
    yield return new KeyValuePair<TKey, TValue>(
  default(TKey),
  this.nullValue);
      }
      foreach (var kvp in this.valueWrapped) {
        yield return kvp;
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
      return this.Iterator().GetEnumerator();
    }

  System.Collections.IEnumerator
      System.Collections.IEnumerable.GetEnumerator() {
      return this.Iterator().GetEnumerator();
    }
  }

  internal sealed class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey,
    TValue> {
    private IDictionary<TKey, TValue> valueWrapped;

    public ReadOnlyDictionary(IDictionary<TKey, TValue> valueWrapped) {
      this.valueWrapped = valueWrapped;
    }

    public TValue this[TKey key] {
      get {
        return this.valueWrapped[key];
      }

      set {
        throw new NotSupportedException();
      }
    }

    public ICollection<TKey> Keys {
      get {
        return this.valueWrapped.Keys;
      }
    }

    public ICollection<TValue> Values {
      get {
        return this.valueWrapped.Values;
      }
    }

    public int Count {
      get {
        return this.valueWrapped.Count;
      }
    }

    public bool IsReadOnly {
      get {
        return true;
      }
    }

    public bool ContainsKey(TKey key) {
      return this.valueWrapped.ContainsKey(key);
    }

    public void Add(TKey key, TValue value) {
      throw new NotSupportedException();
    }

    public bool Remove(TKey key) {
      throw new NotSupportedException();
    }

    public bool TryGetValue(TKey key, out TValue value) {
      return this.valueWrapped.TryGetValue(key, out value);
    }

    public void Add(KeyValuePair<TKey, TValue> item) {
      throw new NotSupportedException();
    }

    public void Clear() {
      throw new NotSupportedException();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
      return this.valueWrapped.Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
      this.valueWrapped.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) {
      throw new NotSupportedException();
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
      return this.valueWrapped.GetEnumerator();
    }

  System.Collections.IEnumerator
      System.Collections.IEnumerable.GetEnumerator() {
      return this.valueWrapped.GetEnumerator();
    }
  }

    /// <summary>Not documented yet.</summary>
  public abstract class InputStream : Stream {
    /// <summary>Not documented yet.</summary>
    /// <returns>A 32-bit signed integer.</returns>
    public virtual int available() {
      return 0;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='limit'>Not documented yet.</param>
    public virtual void mark(int limit) {
      throw new NotSupportedException();
    }

    /// <summary>Not documented yet.</summary>
    public virtual void reset() {
      throw new NotSupportedException();
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A Boolean object.</returns>
    public virtual bool markSupported() {
      return false;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='count'>Not documented yet.</param>
    /// <returns>A 64-bit signed integer.</returns>
    public virtual long skip(long count) {
      return 0;
    }

    //------------------------------------------

    /// <summary>Not documented yet.</summary>
    public sealed override void Write(byte[] buffer, int offset, int count) {
      throw new NotSupportedException();
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='value'>Not documented yet.</param>
    public sealed override void SetLength(long value) {
      throw new NotSupportedException();
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public sealed override long Seek(long offset, SeekOrigin origin) {
      throw new NotSupportedException();
    }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
    public sealed override long Position {
      get {
        throw new NotSupportedException();
      }

      set {
        throw new NotSupportedException();
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
    public sealed override long Length {
      get {
        throw new NotSupportedException();
      }
    }

    /// <summary>Not documented yet.</summary>
    public sealed override void Flush() {
      throw new NotSupportedException();
    }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
    public sealed override bool CanWrite {
      get {
        return false;
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
    public sealed override bool CanSeek {
      get {
        return false;
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
    public sealed override bool CanRead {
      get {
        return true;
      }
    }
  }
}
