/*
Written in 2013 by Peter Occil.
Any copyright to this work is released to the Public Domain.
In case this is not possible, this work is also
licensed under the Unlicense: https://unlicense.org/

*/
using System;
using System.Collections.Generic;

namespace Com.Upokecenter.Util {
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
    /// <param name='index'>The parameter <paramref name='index'/> is a
    /// 32-bit signed integer.</param>
    /// <returns>A T object.</returns>
    public T ReceiveObject (int index) {
      if (index < 0) {
        return default (T);
      }
      T ret = default (T);
      lock (this.syncRoot) {
        if (index >= this.strongrefs.Count) {
          return default (T);
        }
        ret = this.strongrefs[index];
        if (ret == null) {
          throw new InvalidOperationException();
        }
        this.strongrefs[index] = default (T);
      }
      return ret;
    }

    // Keep a strong reference and a weak reference

    /// <summary>Not documented yet.</summary>
    /// <param name='value'>The parameter <paramref name='value'/> is a `0
    /// object.</param>
    /// <returns>A 32-bit signed integer.</returns>
    public int SendObject (T value) {
      if (value == null) {
        return -1; // Special case for null
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
        this.strongrefs.Add (value);
        this.weakrefs.Add (new WeakReference(value));
        return ret;
      }
    }
  }
}
