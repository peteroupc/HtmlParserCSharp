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

public sealed class StreamUtility {
public static void copyStream(PeterO.Support.InputStream stream, Stream
    output) {
    byte[] buffer = new byte[8192];
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
      return streamToString(input);
    } finally {
      if (input != null) {
        input.Close();
      }
    }
  }

  public static void inputStreamToFile(PeterO.Support.InputStream stream,
    PeterO.Support.File file) {
    FileStream output = null;
    try {
      output = new FileStream((file).ToString(), FileMode.Create);
      copyStream(stream, output);
    } finally {
      if (output != null) {
        output.Close();
      }
    }
  }

  public static void skipToEnd(PeterO.Support.InputStream stream) {
    if (stream == null) {
 return;
}
    while (true) {
      byte[] x = new byte[1024];
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

  public static string streamToString(PeterO.Support.InputStream stream) {
    return streamToString("UTF-8",stream);
  }

  public static string streamToString(string charset,
    PeterO.Support.InputStream stream) {
    TextReader reader = new
      StreamReader(stream, System.Text.Encoding.GetEncoding(charset));
    StringBuilder builder = new StringBuilder();
    var buffer = new char[4096];
    while (true) {
      int count = reader.Read(buffer, 0, (buffer).Length);
      if (count< 0) {
        break;
      }
      builder.Append(buffer, 0, count);
    }
    return builder.ToString();
  }

    /// <summary>* Writes a _string in UTF-8 to the specified file. If the
    /// file exists, it will be overwritten @param s a _string to write.
    /// Illegal code unit sequences are replaced with with U + FFFD
    /// REPLACEMENT CHARACTER when writing to the stream. @param file a
    /// filename @ if the file can't be created or another I/O error
    /// occurs.</summary>
    /// <param name='s'>Not documented yet.</param>
    /// <param name='file'>Not documented yet.</param>
  public static void stringToFile(string s, PeterO.Support.File file) {
    Stream os = null;
    try {
      os = new FileStream((file).ToString(), FileMode.Create);
      stringToStream(s, os);
    } finally {
      if (os != null) {
        os.Close();
      }
    }
  }

    /// <summary>* Writes a _string in UTF-8 to the specified output
    /// stream. @param s a _string to write. Illegal code unit sequences
    /// are replaced with U + FFFD REPLACEMENT CHARACTER when writing to
    /// the stream. @param stream an output stream to write to. @ if an I/O
    /// error occurs.</summary>
    /// <param name='s'>Not documented yet.</param>
    /// <param name='stream'>Not documented yet.</param>
  public static void stringToStream(string s, Stream stream) {
    byte[] bytes = new byte[4];
    for (int index = 0;index<s.Length; ++index) {
      int c = s[index];
      if (c >= 0xd800 && c <= 0xdbff && index + 1<s.Length &&
          s[index + 1]>= 0xdc00 && s[index + 1]<= 0xdfff) {
        // Get the Unicode code point for the surrogate pair
        c = 0x10000+(c-0xd800)*0x400+(s[index + 1]-0xdc00);
        ++index;
      } else if (c >= 0xd800 && c <= 0xdfff) {
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
