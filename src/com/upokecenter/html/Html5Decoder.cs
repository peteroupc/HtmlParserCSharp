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
using System;
using System.IO;
using PeterO;
using PeterO.Text;

namespace com.upokecenter.html {
  internal class Html5Decoder : ICharacterDecoder {
    private ICharacterDecoder ValueDecoder = null;
    private bool ValueHavebom = false;
    private bool ValueHavecr = false;
    private bool ValueIserror = false;

    public Html5Decoder(ICharacterDecoder ValueDecoder) {
      if (ValueDecoder == null) {
        throw new ArgumentNullException(nameof(ValueDecoder));
      }
      this.ValueDecoder = ValueDecoder;
    }

    public int ReadChar(IByteReader byteReader) {
      if (byteReader == null) {
        throw new ArgumentNullException(nameof(byteReader));
      }
      throw new NotImplementedException();
    }

    public int Read(IByteReader stream, int[] buffer, int offset, int length) {
      if (buffer == null) {
        throw new ArgumentNullException(nameof(buffer));
      }
      if (offset < 0) {
        throw new ArgumentException("offset (" + offset +
          ") is less than 0");
      }
      if (offset > buffer.Length) {
        throw new ArgumentException("offset (" + offset +
          ") is more than " + buffer.Length);
      }
      if (length < 0) {
        throw new ArgumentException("length (" + length +
          ") is less than 0");
      }
      if (length > buffer.Length) {
        throw new ArgumentException("length (" + length +
          ") is more than " + buffer.Length);
      }
      if (buffer.Length - offset < length) {
        throw new ArgumentException("buffer's length minus " + offset + " (" +
          (buffer.Length - offset) + ") is less than " + length);
      }
      if (length == 0) {
        return 0;
      }
      var count = 0;
      while (length > 0) {
        int c = this.ValueDecoder.ReadChar(stream);
        if (!this.ValueHavebom && !this.ValueHavecr && c >= 0x20 && c <= 0x7e) {
          buffer[offset] = c;
          ++offset;
          ++count;
          --length;
          continue;
        }
        if (c < 0) {
          break;
        }
        if (c == 0x0d) {
          // CR character
          this.ValueHavecr = true;
          c = 0x0a;
        } else if (c == 0x0a && this.ValueHavecr) {
          this.ValueHavecr = false;
          continue;
        } else {
          this.ValueHavecr = false;
        }
        if (c == 0xfeff && !this.ValueHavebom) {
          // leading BOM
          this.ValueHavebom = true;
          continue;
        } else if (c != 0xfeff) {
          this.ValueHavebom = false;
        }
        if (c < 0x09 || (c >= 0x0e && c <= 0x1f) || (c >= 0x7f && c <= 0x9f) ||
        (c & 0xfffe) == 0xfffe || c > 0x10ffff || c == 0x0b || (c >= 0xfdd0 &&
              c <= 0xfdef)) {
          // control character or noncharacter
          this.ValueIserror = true;
        }
        buffer[offset] = c;
        ++offset;
        ++count;
        --length;
      }
      return count == 0 ? -1 : count;
    }

    public bool isError() {
      return this.ValueIserror;
    }
  }
}
