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
namespace com.upokecenter.encoding {
using System;
using System.IO;

sealed class Utf32Encoding : ITextEncoder, ITextDecoder {
  private bool utf32be;
  private int b1=-1, b2=-1, b3=-1;

  public Utf32Encoding(bool utf16be) {
    this.utf32be = utf16be;
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
    int cp = 0;
    int count = 0;
    while (length>0) {
      int b = stream.ReadByte();
      if (b<0 && (b1 >= 0 || b2 >= 0 || b3 >= 0)) {
        b1 = b2 = b3=-1;
        int o = error.emitDecoderError(buffer, offset, length);
        offset+=o;
        count+=o;
        length-=o;
        break;
      } else if (b< 0) {
        break;
      }
      if (b3 >= 0) {
        if ((utf32be && b1 != 0) || (!utf32be && b != 0)) {
          b1 = b2 = b3=-1;
          int o = error.emitDecoderError(buffer, offset, length);
          offset+=o;
          count+=o;
          length-=o;
        } else {
      cp=(utf32be) ? (b1 << 24)|(b2 << 16)|(b3 << 8)|b :
            (b << 24)|(b3 << 16)|(b2 << 8)|b1;
          b1 = b2 = b3=-1;
          if ((cp >= 0xd800 && cp <= 0xdfff) || cp<0 || cp >= 0x110000) {
            // Surrogate and out-of-range code points are illegal
            int o = error.emitDecoderError(buffer, offset, length);
            offset+=o;
            count+=o;
            length-=o;
          } else {
            buffer[offset++]=(cp);
            ++count;
            --length;
          }
        }
        continue;
      } else if (b2 >= 0) {
        b3 = b;
        continue;
      } else if (b1 >= 0) {
        b2 = b;
        continue;
      } else {
        b1 = b;
        continue;
      }
    }
    return (count <= 0) ? -1 : count;
  }

  public void encode(Stream stream, int[] array, int offset, int length) {
    encode(stream, array, offset, length, TextEncoding.ENCODING_ERROR_THROW);
  }

  public void encode(Stream stream, int[] array, int offset, int length,
    IEncodingError error) {
    if (stream == null || array == null) {
 throw new ArgumentException();
}
    if (offset<0 || length<0 || offset + length>array.Length) {
 throw new ArgumentOutOfRangeException();
}
    for (int i = 0;i<array.Length; ++i) {
      int cp = array[offset + i];
      if (cp<0 || cp >= 0x110000 || ((cp & 0xf800) == 0xd800)) {
        error.emitEncoderError(stream, cp);
        continue;
      }
      int byte1=(cp >> 24) & 0xff;
      int byte2=(cp >> 16) & 0xff;
      int byte3=(cp >> 8) & 0xff;
      int byte4=(cp) & 0xff;
      stream.WriteByte(unchecked((byte)(utf32be ? byte1 : byte4)));
      stream.WriteByte(unchecked((byte)(utf32be ? byte2 : byte3)));
      stream.WriteByte(unchecked((byte)(utf32be ? byte3 : byte2)));
      stream.WriteByte(unchecked((byte)(utf32be ? byte4 : byte1)));
    }
  }
}
}
