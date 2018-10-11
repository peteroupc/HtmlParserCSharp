/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
using System;
using System.Collections.Generic;
namespace com.upokecenter.util {;

    /// <summary>Not documented yet.</summary>
    /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
public sealed class IndexedObjectList<T> {
  private IList<T> strongrefs = new List<T>();
  private IList<WeakReference> weakrefs = new List<WeakReference>();
  private Object syncRoot = new Object();

  // Remove the strong reference, but keep the weak
  // reference; the index becomes no good when the
  // _object is garbage collected

    /// <summary>Not documented yet.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A T object.</returns>
  public T receiveObject(int index) {
    if (index < 0) {
 return default(T);
}
    T ret = default(T);
    lock (this.syncRoot) {
      if (index >= this.strongrefs.Count) {
 return default(T);
}
      ret = this.strongrefs[index];
      if (ret == null) {
 throw new InvalidOperationException();
}
      this.strongrefs[index] = default(T);
    }
    return ret;
  }

  // Keep a strong reference and a weak reference

    /// <summary>Not documented yet.</summary>
    /// <param name='value'>Not documented yet.</param>
    /// <returns>A 32-bit signed integer.</returns>
  public int sendObject(T value) {
    if (value == null) {
 return -1;  // Special case for null
}
    lock (this.syncRoot) {
      for (int i = 0; i < this.strongrefs.Count; ++i) {
        if (this.strongrefs[i] == null) {
          if (this.weakrefs[i] == null ||
              this.weakrefs[i].Target == null) {
            // If the _object is garbage collected
            // the index is available for use again
            // DebugUtility.Log("Adding _object %d",i);
            this.strongrefs[i] = value;
            this.weakrefs[i] = new WeakReference(value);
            return i;
          }
        }
      }
      // Keep a strong and weak reference of
      // the same _object
      int ret = this.strongrefs.Count;
      // DebugUtility.Log("Adding _object %d",ret);
      this.strongrefs.Add(value);
      this.weakrefs.Add(new WeakReference(value));
      return ret;
    }
  }
}
}
