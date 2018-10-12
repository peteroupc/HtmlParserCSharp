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
