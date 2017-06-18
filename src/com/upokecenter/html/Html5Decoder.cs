/*
If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/

Licensed under the Expat License.

Copyright (C) 2013 Peter Occil

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

namespace com.upokecenter.html {
using System;
using System.IO;
using com.upokecenter.encoding;
internal class Html5Decoder : ITextDecoder {
  ITextDecoder decoder = null;
  bool havebom = false;
  bool havecr = false;
  bool iserror = false;
  public Html5Decoder(ITextDecoder decoder) {
    if (decoder == null) {
 throw new ArgumentException();
}
    this.decoder = decoder;
  }

  public int decode(PeterO.Support.InputStream stream) {
    return decode(stream, TextEncoding.ENCODING_ERROR_THROW);
  }

  public int decode(PeterO.Support.InputStream stream, IEncodingError error) {
    int[] value = new int[1];
    int c = decode(stream, value, 0, 1, error);
    return (c <= 0) ? (-1) : (value[0]);
  }

  public int decode(PeterO.Support.InputStream stream, int[] buffer, int
    offset, int length) {
    return decode(stream, buffer, offset, length,
      TextEncoding.ENCODING_ERROR_THROW);
  }

  public int decode(PeterO.Support.InputStream stream, int[] buffer, int
    offset, int length, IEncodingError error) {
    if (stream == null || buffer == null || offset<0 || length<0 ||
        offset + length>buffer.Length) {
 throw new ArgumentException();
}
    if (length == 0) {
 return 0;
}
    int count = 0;
    while (length>0) {
      int c = decoder.decode(stream, error);
      if (!havebom && !havecr && c >= 0x20 && c <= 0x7e) {
        buffer[offset]=c;
        ++offset;
        ++count;
        --length;
        continue;
      }
      if (c< 0) {
        break;
      }
      if (c == 0x0d) {
        // CR character
        havecr = true;
        c = 0x0a;
      } else if (c == 0x0a && havecr) {
        havecr = false;
        continue;
      } else {
        havecr = false;
      }
      if (c == 0xfeff && !havebom) {
        // leading BOM
        havebom = true;
        continue;
      } else if (c != 0xfeff) {
        havebom = false;
      }
      if (c<0x09 || (c >= 0x0e && c <= 0x1f) || (c >= 0x7f && c <= 0x9f) ||
      (c & 0xfffe) == 0xfffe || c>0x10ffff || c == 0x0b || (c >= 0xfdd0 &&
            c <= 0xfdef)) {
        // control character or noncharacter
        iserror = true;
      }
      buffer[offset]=c;
      ++offset;
      ++count;
      --length;
    }
    return count == 0 ? -1 : count;
  }

  public bool isError() {
    return iserror;
  }
}
}
