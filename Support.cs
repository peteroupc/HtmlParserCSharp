using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using com.upokecenter.util;

namespace PeterO.Support {
    /// <summary>Description of Support.</summary>
  public static class Collections {
    /// <summary>Not documented yet.</summary>
    /// <param name='list'>Not documented yet.</param>
    /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
    /// <returns>An IList(T) object.</returns>
    public static IList<T> UnmodifiableList<T>(IList<T> list) {
      return (list.IsReadOnly) ? list : (new
        System.Collections.ObjectModel.ReadOnlyCollection<T>(list));
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='list'>Not documented yet.</param>
    /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
    /// <returns>An IDictionary(TKey, TValue) object.</returns>
    public static IDictionary<TKey, TValue>
      UnmodifiableMap<TKey, TValue>(IDictionary<TKey, TValue> list) {
return list.IsReadOnly ? list : (new ReadOnlyDictionary<TKey, TValue>(list));
    }

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

  internal sealed class InputStreamWrapper : InputStream {
    private Stream valueIstr;

    public InputStreamWrapper(Stream valueIstr) {
      this.valueIstr = valueIstr;
    }
    // Just ensures that read never returns a number
    // less than 0, for compatibility with StreamReader
    public override int Read(byte[] buffer, int offset, int count) {
      int ret = this.valueIstr.Read(buffer, offset, count);
      if (ret < 0) {
 ret = 0;
}
      return ret;
    }

    public override int ReadByte() {
      return this.valueIstr.ReadByte();
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
  public sealed class WrappedInputStream : InputStream {
    private Stream valueWrapped = null;

    /// <summary>Initializes a new instance of the WrappedInputStream
    /// class.</summary>
    /// <param name='valueWrapped'>A Stream object.</param>
    public WrappedInputStream(Stream valueWrapped) {
      this.valueWrapped = valueWrapped;
    }

    /// <summary>Not documented yet.</summary>
    public new void Dispose() {
      this.valueWrapped.Dispose();
      base.Dispose();
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public override sealed int ReadByte() {
      return this.valueWrapped.ReadByte();
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='byteCount'>Not documented yet.</param>
    /// <returns>Not documented yet.</returns>
    public override sealed long skip(long byteCount) {
      var data = new byte[1024];
      long ret = 0;
      while (byteCount < 0) {
        var bc = (int)Math.Min(byteCount, data.Length);
        int c = this.Read(data, 0, bc);
        if (c <= 0) {
          break;
        }
        ret += c;
        byteCount -= c;
      }
      return ret;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public override sealed int Read(byte[] buffer, int offset, int byteCount) {
      return this.valueWrapped.Read(buffer, offset, byteCount);
    }
  }

    /// <summary>Not documented yet.</summary>
  public sealed class ByteArrayInputStream : InputStream {
    private byte[] buffer = null;
    private int pos = 0;
    private int endpos = 0;
    private long markpos = -1;
    private int posAtMark = 0;
    private long marklimit = 0;

    /// <summary>Initializes a new instance of the ByteArrayInputStream
    /// class.</summary>
    /// <param name='buffer'>A byte array.</param>
  public ByteArrayInputStream(
  byte[] buffer) : this(
  buffer,
  0,
  buffer.Length) {
    }

    /// <summary>Initializes a new instance of the ByteArrayInputStream
    /// class.</summary>
    /// <param name='buffer'>A byte array.</param>
    /// <param name='index'>A 32-bit signed integer.</param>
    /// <param name='length'>Another 32-bit signed integer.</param>
    public ByteArrayInputStream(byte[] buffer, int index, int length) {
      if (buffer == null || index < 0 || length < 0 || index + length >
        buffer.Length) {
        throw new ArgumentException();
      }
      this.buffer = buffer;
      this.pos = index;
      this.endpos = index + length;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public override sealed int available() {
      return this.endpos - this.pos;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public override sealed bool markSupported() {
      return true;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='limit'>Not documented yet.</param>
    public override sealed void mark(int limit) {
      if (limit < 0) {
 throw new ArgumentException();
}
      this.markpos = 0;
      this.posAtMark = this.pos;
      this.marklimit = limit;
    }

    private int readInternal(byte[] buf, int offset, int unitCount) {
      if (buf == null) {
 throw new ArgumentException();
}
      if (offset < 0 || unitCount < 0 || offset + unitCount > buf.Length) {
 throw new ArgumentOutOfRangeException();
}
      if (unitCount == 0) {
 return 0;
}
      int total = Math.Min(unitCount, this.endpos - this.pos);
      if (total == 0) {
 return -1;
}
      Array.Copy(this.buffer, this.pos, buf, offset, total);
      this.pos += total;
      return total;
    }

    private int readInternal() {
      // Read from buffer
      return (this.pos < this.endpos) ? (this.buffer[this.pos++] & 0xff) : (-1);
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public override sealed int ReadByte() {
      if (this.markpos < 0) {
 return this.readInternal();
} else {
        int c = this.readInternal();
        if (c >= 0 && this.markpos >= 0) {
          ++this.markpos;
          if (this.markpos > this.marklimit) {
            this.marklimit = 0;
            this.markpos = -1;
          }
        }
        return c;
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='byteCount'>Not documented yet.</param>
    /// <returns>Not documented yet.</returns>
    public override sealed long skip(long byteCount) {
      var data = new byte[1024];
      long ret = 0;
      while (byteCount < 0) {
        var bc = (int)Math.Min(byteCount, data.Length);
        int c = this.Read(data, 0, bc);
        if (c <= 0) {
          break;
        }
        ret += c;
        byteCount -= c;
      }
      return ret;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public override sealed int Read(byte[] buffer, int offset, int byteCount) {
      if (this.markpos < 0) {
 return this.readInternal(buffer, offset, byteCount);
} else {
        int c = this.readInternal(buffer, offset, byteCount);
        if (c > 0 && this.markpos >= 0) {
          this.markpos += c;
          if (this.markpos > this.marklimit) {
            this.marklimit = 0;
            this.markpos = -1;
          }
        }
        return c;
      }
    }

    /// <summary>Not documented yet.</summary>
    public override sealed void reset() {
      if (this.markpos < 0) {
 throw new IOException();
}
      this.pos = this.posAtMark;
    }
  }

    /// <summary>Not documented yet.</summary>
  public sealed class BufferedInputStream : InputStream {
    private byte[] buffer = null;
    private int pos = 0;
    private int endpos = 0;
    private bool closed = false;
    private long markpos = -1;
    private int posAtMark = 0;
    private long marklimit = 0;
    private Stream stream = null;

    /// <summary>Initializes a new instance of the BufferedInputStream
    /// class.</summary>
    /// <param name='input'>A Stream object.</param>
    public BufferedInputStream(Stream input) : this(input, 8192) {
    }

    /// <summary>Initializes a new instance of the BufferedInputStream
    /// class.</summary>
    /// <param name='input'>A Stream object.</param>
    /// <param name='buffersize'>A 32-bit signed integer.</param>
    public BufferedInputStream(Stream input, int buffersize) {
      if (input == null) {
 throw new ArgumentNullException();
}
      if (buffersize < 0) {
 throw new ArgumentException();
}
      this.buffer = new byte[buffersize];
      this.stream = input;
    }

    /// <summary>Not documented yet.</summary>
    public new void Dispose() {
      this.pos = 0;
      this.endpos = 0;
      this.stream.Dispose();
      base.Dispose();
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public override sealed int available() {
      return this.endpos - this.pos;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public override sealed bool markSupported() {
      return true;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='limit'>Not documented yet.</param>
    public override sealed void mark(int limit) {
      if (limit < 0) {
 throw new ArgumentException();
}
      this.markpos = 0;
      this.posAtMark = this.pos;
      this.marklimit = limit;
    }

    private int readInternal(byte[] buf, int offset, int unitCount) {
      if (buf == null) {
 throw new ArgumentException();
}
      if (offset < 0 || unitCount < 0 || offset + unitCount > buf.Length) {
 throw new ArgumentOutOfRangeException();
}
      if (unitCount == 0) {
 return 0;
}
      var total = 0;
      // Read from buffer
      if (this.pos + unitCount <= this.endpos) {
        Array.Copy(this.buffer, this.pos, buf, offset, unitCount);
        this.pos += unitCount;
        return unitCount;
      }
      // End pos is smaller than buffer size, fill
      // entire buffer if possible
      var count = 0;
      if (this.endpos < this.buffer.Length) {
        count = this.stream.Read(
  this.buffer,
  this.endpos,
  this.buffer.Length - this.endpos);
        // DebugUtility.Log("%s",this);
        if (count > 0) {
          this.endpos += count;
        }
      }
      // Try reading from buffer again
      if (this.pos + unitCount <= this.endpos) {
        Array.Copy(this.buffer, this.pos, buf, offset, unitCount);
        this.pos += unitCount;
        return unitCount;
      }
      // expand the buffer
      if (this.pos + unitCount > this.buffer.Length) {
        var newBuffer = new byte[(this.buffer.Length * 2) + unitCount];
        Array.Copy(this.buffer, 0, newBuffer, 0, this.buffer.Length);
        this.buffer = newBuffer;
      }
   count = this.stream.Read(
  this.buffer,
  this.endpos,
  Math.Min(unitCount, this.buffer.Length - this.endpos));
      if (count > 0) {
        this.endpos += count;
      }
      // Try reading from buffer a third time
      if (this.pos + unitCount <= this.endpos) {
        Array.Copy(this.buffer, this.pos, buf, offset, unitCount);
        this.pos += unitCount;
        total += unitCount;
      } else if (this.endpos > this.pos) {
        Array.Copy(this.buffer, this.pos, buf, offset, this.endpos - this.pos);
        total += this.endpos - this.pos;
        this.pos = this.endpos;
      }
      return (total == 0) ? -1 : total;
    }

    private int readInternal() {
      // Read from buffer
      if (this.pos < this.endpos) {
 return this.buffer[this.pos++] & 0xff;
}
      // End pos is smaller than buffer size, fill
      // entire buffer if possible
      if (this.endpos < this.buffer.Length) {
        int count = this.stream.Read(
  this.buffer,
  this.endpos,
  this.buffer.Length - this.endpos);
        if (count > 0) {
          this.endpos += count;
        }
      }
      // Try reading from buffer again
      if (this.pos < this.endpos) {
 return this.buffer[this.pos++] & 0xff;
}
      // No room, read next byte and put it in buffer
      int c = this.stream.ReadByte();
      if (c < 0) {
 return c;
}
      if (this.pos >= this.buffer.Length) {
        var newBuffer = new byte[this.buffer.Length * 2];
        Array.Copy(this.buffer, 0, newBuffer, 0, this.buffer.Length);
        this.buffer = newBuffer;
      }
      this.buffer[this.pos++] = (byte)(c & 0xff);
      ++this.endpos;
      return c;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public override sealed int ReadByte() {
      if (this.closed) {
 throw new IOException();
}
      if (this.markpos < 0) {
 return this.readInternal();
} else {
        int c = this.readInternal();
        if (c >= 0 && this.markpos >= 0) {
          ++this.markpos;
          if (this.markpos > this.marklimit) {
            this.marklimit = 0;
            this.markpos = -1;
          }
        }
        return c;
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='byteCount'>Not documented yet.</param>
    /// <returns>Not documented yet.</returns>
    public override sealed long skip(long byteCount) {
      if (this.closed) {
 throw new IOException();
}
      var data = new byte[1024];
      long ret = 0;
      while (byteCount < 0) {
        var bc = (int)Math.Min(byteCount, data.Length);
        int c = this.Read(data, 0, bc);
        if (c <= 0) {
          break;
        }
        ret += c;
        byteCount -= c;
      }
      return ret;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public override sealed int Read(byte[] buffer, int offset, int byteCount) {
      if (this.closed) {
 throw new IOException();
}
      if (this.markpos < 0) {
 return this.readInternal(buffer, offset, byteCount);
} else {
        int c = this.readInternal(buffer, offset, byteCount);
        if (c > 0 && this.markpos >= 0) {
          this.markpos += c;
          if (this.markpos > this.marklimit) {
            this.marklimit = 0;
            this.markpos = -1;
          }
        }
        return c;
      }
    }

    /// <summary>Not documented yet.</summary>
    public override sealed void reset() {
      if (this.markpos < 0 || this.closed) {
 throw new IOException();
}
      this.pos = this.posAtMark;
    }
  }

    /// <summary>Not documented yet.</summary>
  public abstract class OutputStream : Stream {
    /// <summary>Not documented yet.</summary>
    /// <param name='value'>Not documented yet.</param>
    public override sealed void SetLength(long value) {
      throw new NotSupportedException();
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public override sealed long Seek(long offset, SeekOrigin origin) {
      throw new NotSupportedException();
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
    public override sealed int Read(byte[] buffer, int offset, int count) {
      throw new NotSupportedException();
    }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
    public override sealed long Position {
      get {
        throw new NotSupportedException();
      }

      set {
        throw new NotSupportedException();
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
    public override sealed long Length {
      get {
        throw new NotSupportedException();
      }
    }

    /// <summary>Not documented yet.</summary>
    public override void Flush() {
    }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
    public override sealed bool CanWrite {
      get {
        return true;
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
    public override sealed bool CanSeek {
      get {
        return false;
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
    public override sealed bool CanRead {
      get {
        return false;
      }
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
