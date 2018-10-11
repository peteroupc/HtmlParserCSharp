/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
using System;
using System.IO;
using System.Text;
using PeterO.Text;
namespace com.upokecenter.io {;

    /// <summary>Not documented yet.</summary>
public sealed class StreamUtility {
    /// <summary>Not documented yet.</summary>
    /// <param name='stream'>Not documented yet.</param>
    /// <param name='output'>Not documented yet.</param>
public static void copyStream(
  PeterO.Support.InputStream stream,
  Stream output) {
    var buffer = new byte[8192];
    while (true) {
      int count = stream.Read(buffer, 0, buffer.Length);
      if (count < 0) {
        break;
      }
      output.Write(buffer, 0, count);
    }
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='stream'>Not documented yet.</param>
  public static void skipToEnd(PeterO.Support.InputStream stream) {
    if (stream == null) {
 return;
}
    while (true) {
      var x = new byte[1024];
      try {
        int c = stream.Read(x, 0, x.Length);
        if (c < 0) {
          break;
        }
      } catch (IOException) {
        break;  // maybe this stream is already closed
      }
    }
  }

  private StreamUtility() {
}
}
}
