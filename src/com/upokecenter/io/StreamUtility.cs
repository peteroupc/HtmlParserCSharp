/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
namespace com.upokecenter.io {
using System;
using System.IO;
using System.Text;
using PeterO.Text;

public sealed class StreamUtility {
public static void copyStream(PeterO.Support.InputStream stream, Stream
    output) {
    var buffer = new byte[8192];
    while (true) {
      int count = stream.Read(buffer, 0, buffer.Length);
      if (count< 0) {
        break;
      }
      output.Write(buffer, 0, count);
    }
  }

  public static string fileToString(PeterO.Support.File file) {
    PeterO.Support.WrappedInputStream input = null;
    try {
      input = new PeterO.Support.WrappedInputStream(new
        FileStream((file).ToString(), FileMode.Open));
        return Encodings.DecodeToString (Encodings.UTF8, input);
    } finally {
      if (input != null) {
        input.Dispose();
      }
    }
  }

  public static void skipToEnd(PeterO.Support.InputStream stream) {
    if (stream == null) {
 return;
}
    while (true) {
      var x = new byte[1024];
      try {
        int c = stream.Read(x, 0, x.Length);
        if (c< 0) {
          break;
        }
      } catch (IOException) {
        break;  // maybe this stream is already closed
      }
    }
  }

    /// <summary>* Writes a _string in UTF-8 to the specified file. If the
    /// file exists, it will be overwritten @param s a _string to write.
    /// Illegal code unit sequences are replaced with with U + FFFD
    /// REPLACEMENT CHARACTER when writing to the stream. @param file a
    /// filename @ if the file can't be created or another I/O error
    /// occurs.</summary>
    /// <param name='s'>The parameter <paramref name='s'/> is not
    /// documented yet.</param>
    /// <param name='file'>The parameter <paramref name='file'/> is not
    /// documented yet.</param>
  public static void stringToFile(string s, PeterO.Support.File file) {
    Stream os = null;
    try {
      os = new FileStream((file).ToString(), FileMode.Create);
      stringToStream(s, os);
    } finally {
      if (os != null) {
        os.Dispose();
      }
    }
  }

    /// <summary>* Writes a _string in UTF-8 to the specified output
    /// stream. @param s a _string to write. Illegal code unit sequences
    /// are replaced with U + FFFD REPLACEMENT CHARACTER when writing to
    /// the stream. @param stream an output stream to write to. @ if an I/O
    /// error occurs.</summary>
    /// <param name='s'>The parameter <paramref name='s'/> is not
    /// documented yet.</param>
    /// <param name='stream'>The parameter <paramref name='stream'/> is not
    /// documented yet.</param>
  public static void stringToStream(string s, Stream stream) {
    var bytes = new byte[4];
    for (int index = 0;index<s.Length; ++index) {
      int c = s[index];
      if ((c & 0xfc00) == 0xd800 && index + 1<s.Length &&
          s[index + 1]>= 0xdc00 && s[index + 1]<= 0xdfff) {
        // Get the Unicode code point for the surrogate pair
        c = 0x10000+(c-0xd800)*0x400+(s[index + 1]-0xdc00);
        ++index;
      } else if ((c & 0xf800) == 0xd800) {
        // unpaired surrogate, write U + FFFD instead
        c = 0xfffd;
      }
      if (c <= 0x7f) {
        stream.WriteByte(unchecked((byte)(c)));
      } else if (c <= 0x7ff) {
        bytes[0]=((byte)(0xC0|((c >> 6) & 0x1f)));
        bytes[1]=((byte)(0x80|(c & 0x3f)));
        stream.Write(bytes, 0, 2);
      } else if (c <= 0xffff) {
        bytes[0]=((byte)(0xE0|((c >> 12) & 0x0f)));
        bytes[1]=((byte)(0x80|((c >> 6) & 0x3f)));
        bytes[2]=((byte)(0x80|(c & 0x3f)));
        stream.Write(bytes, 0, 3);
      } else {
        bytes[0]=((byte)(0xF0|((c >> 18) & 0x07)));
        bytes[1]=((byte)(0x80|((c >> 12) & 0x3f)));
        bytes[2]=((byte)(0x80|((c >> 6) & 0x3f)));
        bytes[3]=((byte)(0x80|(c & 0x3f)));
        stream.Write(bytes, 0, 4);
      }
    }
  }

  private StreamUtility() {}
}
}
