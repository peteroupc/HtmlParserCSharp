/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
using System;
using System.Globalization;
using System.Text;
namespace com.upokecenter.util {
    /// <summary>Represents a list of integers or Unicode
    /// characters.</summary>
  ///
public sealed class IntList {
  int[] buffer;
  int ptr;
  public IntList() {
    buffer = new int[64];
    ptr = 0;
  }

  public void appendInt(int v) {
    if (ptr<buffer.Length) {
      buffer[ptr++]=v;
    } else {
      var newbuffer = new int[buffer.Length*2];
      Array.Copy(buffer, 0, newbuffer, 0, buffer.Length);
      buffer = newbuffer;
      buffer[ptr++]=v;
    }
  }

  public void appendInts(int[] array, int offset, int length) {
    if ((array) == null) {
 throw new ArgumentNullException(nameof(array));
}
    if (offset < 0) {
 throw new ArgumentException("offset less than " +"0 ("
   +Convert.ToString(offset,CultureInfo.InvariantCulture)+")");
}
    if (length < 0) {
 throw new ArgumentException("length less than " +"0 ("
   +Convert.ToString(length,CultureInfo.InvariantCulture)+")");
}
    if (offset + length > array.Length) {
 throw new ArgumentOutOfRangeException("offset+length more than "
   +Convert.ToString(array.Length,CultureInfo.InvariantCulture)+" ("
   +Convert.ToString(offset+length,CultureInfo.InvariantCulture)+")");
}
    if (ptr + length>buffer.Length) {
    var newbuffer = new int[Math.Max(buffer.Length*2, buffer.Length +
        length)];
      Array.Copy(buffer, 0, newbuffer, 0, buffer.Length);
      buffer = newbuffer;
    }
    Array.Copy(array, offset, buffer, ptr, length);
    ptr+=length;
  }

  public void appendString(string str) {
    for (int i = 0;i<str.Length; ++i) {
      int c = str[i];
      if ((c & 0xfc00) == 0xd800 && i + 1<str.Length &&
          (str[i + 1] & 0xfc00) == 0xdc00) {
        // Append a UTF-16 surrogate pair
        int cp2 = 0x10000+(c-0xd800)*0x400+(str[i + 1]-0xdc00);
        appendInt(cp2);
        ++i;
      } else if ((c & 0xf800) == 0xd800) {
 // illegal surrogate
        throw new ArgumentException();
 } else {
        appendInt(c);
      }
    }
  }

  public int[] array() {
    return buffer;
  }
  public void clearAll() {
    ptr = 0;
  }
  public int this[int i] { get {
 return get(i);
}
 set { set(i, value); }}
public int get(int index) {
    return buffer[index];
  }

    /// <summary>Sets the integer at a specified position to a new value.
    /// @param index an index into the list. @param value the integer's new
    /// value.</summary>
    /// <param name='index'>The parameter <paramref name='index'/> is not
    /// documented yet.</param>
    /// <param name='value'>The parameter <paramref name='value'/> is not
    /// documented yet.</param>
  ///
  public void set(int index, int value) {
    buffer[index]=value;
  }
  public int Count { get {
 return size();
}}
public int size() {
    return ptr;
  }
  public override sealed string ToString() {
    var builder = new StringBuilder();
    for (int i = 0; i < ptr; ++i) {
      if (buffer[i]<= 0xffff) {
        builder.Append((char)buffer[i]);
      } else {
        int ch = buffer[i]-0x10000;
        int lead = ch/0x400 + 0xd800;
        int trail=(ch & 0x3ff)+0xdc00;
        builder.Append((char)lead);
        builder.Append((char)trail);
      }
    }
    return builder.ToString();
  }
}
}
