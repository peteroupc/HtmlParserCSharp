/*
Written in 2013 by Peter Occil.
Any copyright to this work is released to the Public Domain.
In case this is not possible, this work is also
licensed under the Unlicense: https://unlicense.org/

*/
using System;
using System.IO;
using PeterO;

namespace Com.Upokecenter.Io {
  /// <summary>An input reader that stores the first bytes of the reader
  /// in a buffer and supports rewinding to the beginning of the reader.
  /// However, when the buffer is disabled, no further bytes are put into
  /// the buffer, but any remaining bytes in the buffer will still be
  /// used until it's exhausted.</summary>
  public sealed class ConditionalBufferReader : IByteReader {
    private byte[] buffer = null;
    private int pos = 0;
    private int endpos = 0;
    private bool disabled = false;
    private long markpos = -1;
    private int posAtMark = 0;
    private long marklimit = 0;
    private IReader reader = null;

    /// <summary>Initializes a new instance of the ConditionalBufferReader
    /// class.</summary>
    /// <param name='input'>The parameter <paramref name='input'/> is an
    /// IReader object.</param>
    public ConditionalBufferReader(IReader input) {
      this.reader = input;
      this.buffer = new byte[1024];
    }

    /// <summary>Disables buffering of future bytes read from the
    /// underlying reader. However, any bytes already buffered can still be
    /// read until the buffer is exhausted. After the buffer is exhausted,
    /// this reader will fully delegate to the underlying reader.</summary>
    public void DisableBuffer() {
      this.disabled = true;
      if (this.buffer != null && this.IsDisabled()) {
        this.buffer = null;
      }
    }

    private int DoRead(byte[] buffer, int offset, int byteCount) {
      if (this.markpos < 0) {
        return this.ReadInternal(buffer, offset, byteCount);
      } else {
        if (this.IsDisabled()) {
          return this.reader.Read(buffer, offset, byteCount);
        }
        int c = this.ReadInternal(buffer, offset, byteCount);
        if (c > 0 && this.markpos >= 0) {
          this.markpos += c;
          if (this.markpos > this.marklimit) {
            this.marklimit = 0;
            this.markpos = -1;
            if (this.buffer != null && this.IsDisabled()) {
              this.buffer = null;
            }
          }
        }
        return c;
      }
    }

    private bool IsDisabled() {
      return this.disabled ? ((this.markpos >= 0 &&
        this.markpos < this.marklimit) ? false : (this.pos >=
          this.endpos)) : false;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='limit'>The parameter <paramref name='limit'/> is a
    /// 32-bit signed integer.</param>
    public void Mark(int limit) {
      // Console.WriteLine("Mark %d: %s",limit,IsDisabled());
      if (this.IsDisabled()) {
        // this.reader.Mark(limit);
        // return;
        throw new NotSupportedException();
      }
      if (limit < 0) {
        throw new ArgumentException();
      }
      this.markpos = 0;
      this.posAtMark = this.pos;
      this.marklimit = limit;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>Either <c>true</c> or <c>false</c>.</returns>
    public bool MarkSupported() {
      return true;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A 32-bit signed integer.</returns>
    public int ReadByte() {
      if (this.markpos < 0) {
        return this.ReadInternal();
      } else {
        if (this.IsDisabled()) {
          return this.reader.ReadByte();
        }
        int c = this.ReadInternal();
        if (c >= 0 && this.markpos >= 0) {
          ++this.markpos;
          if (this.markpos > this.marklimit) {
            this.marklimit = 0;
            this.markpos = -1;
            if (this.buffer != null && this.IsDisabled()) {
              this.buffer = null;
            }
          }
        }
        return c;
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='buffer'>The parameter <paramref name='buffer'/> is
    /// a.Byte[] object.</param>
    /// <param name='offset'>The parameter <paramref name='offset'/> is a
    /// 32-bit signed integer.</param>
    /// <param name='byteCount'>The parameter <paramref name='byteCount'/>
    /// is a 32-bit signed integer.</param>
    /// <returns>A 32-bit signed integer.</returns>
    public int Read(byte[] buffer, int offset, int byteCount) {
      return this.DoRead(buffer, offset, byteCount);
    }

    private int ReadInternal() {
      // Read from buffer
      if (this.pos < this.endpos) {
        return this.buffer[this.pos++] & 0xff;
      }
      if (this.disabled) {
        // Buffering new bytes is disabled, so read directly from reader
        return this.reader.ReadByte();
      }
      // if (buffer != null) {
      // Console.WriteLine("buffer %s end=%s len=%s",pos,endpos,buffer.Length);
      // }
      // End pos is smaller than buffer size, fill
      // entire buffer if possible
      if (this.endpos < this.buffer.Length) {
        int count = this.reader.Read(
            this.buffer,
            this.endpos,
            this.buffer.Length - this.endpos);
        if (count > 0) {
          this.endpos += count;
        }
      }
      // Try reading from buffer again
      if (this.pos < this.endpos) {
        return this.buffer[this.pos++] & 0xff;
      }
      // No room, read next byte and put it in buffer
      int c = this.reader.ReadByte();
      if (c < 0) {
        return c;
      }
      if (this.pos >= this.buffer.Length) {
        var newBuffer = new byte[this.buffer.Length * 2];
        Array.Copy(this.buffer, 0, newBuffer, 0, this.buffer.Length);
        this.buffer = newBuffer;
      }
      this.buffer[this.pos++] = (byte)(c & 0xff);
      ++this.endpos;
      return c;
    }

    private int ReadInternal(byte[] buf, int offset, int unitCount) {
      if (buf == null) {
        throw new ArgumentException();
      }
      if (offset < 0 || unitCount < 0 || offset + unitCount > buf.Length) {
        throw new ArgumentOutOfRangeException();
      }
      if (unitCount == 0) {
        return 0;
      }
      var total = 0;
      var count = 0;
      // Read from buffer
      if (this.pos + unitCount <= this.endpos) {
        Array.Copy(this.buffer, this.pos, buf, offset, unitCount);
        this.pos += unitCount;
        return unitCount;
      }
      // if (buffer != null) {
      // Console.WriteLine("buffer(3arg) %s end=%s len=%s"
      // , pos, endpos, buffer.Length);
      // }
      if (this.disabled) {
        // Buffering disabled, read as much as possible from the buffer
        if (this.pos < this.endpos) {
          int c = Math.Min(unitCount, this.endpos - this.pos);
          Array.Copy(this.buffer, this.pos, buf, offset, c);
          this.pos = this.endpos;
          offset += c;
          unitCount -= c;
          total += c;
        }
        // Read directly from the reader for the rest
        if (unitCount > 0) {
          int c = this.reader.Read(buf, offset, unitCount);
          if (c > 0) {
            total += c;
          }
        }
        return (total == 0) ? -1 : total;
      }
      // End pos is smaller than buffer size, fill
      // entire buffer if possible
      if (this.endpos < this.buffer.Length) {
        count = this.reader.Read(
            this.buffer,
            this.endpos,
            this.buffer.Length - this.endpos);
        // Console.WriteLine("%s",this);
        if (count > 0) {
          this.endpos += count;
        }
      }
      // Try reading from buffer again
      if (this.pos + unitCount <= this.endpos) {
        Array.Copy(this.buffer, this.pos, buf, offset, unitCount);
        this.pos += unitCount;
        return unitCount;
      }
      // expand the buffer
      if (this.pos + unitCount > this.buffer.Length) {
        var newBuffer = new byte[(this.buffer.Length * 2) + unitCount];
        Array.Copy(this.buffer, 0, newBuffer, 0, this.buffer.Length);
        this.buffer = newBuffer;
      }
      count = this.reader.Read(
          this.buffer,
          this.endpos,
          Math.Min(unitCount, this.buffer.Length - this.endpos));
      if (count > 0) {
        this.endpos += count;
      }
      // Try reading from buffer a third time
      if (this.pos + unitCount <= this.endpos) {
        Array.Copy(this.buffer, this.pos, buf, offset, unitCount);
        this.pos += unitCount;
        total += unitCount;
      } else if (this.endpos > this.pos) {
        Array.Copy(this.buffer, this.pos, buf, offset, this.endpos - this.pos);
        total += this.endpos - this.pos;
        this.pos = this.endpos;
      }
      return (total == 0) ? -1 : total;
    }

    /// <summary>Not documented yet.</summary>
    public void Reset() {
      // Console.WriteLine("Reset: %s",IsDisabled());
      if (this.IsDisabled()) {
        throw new NotSupportedException();
        // this.reader.Reset();
        // return;
      }
      if (this.markpos < 0) {
        throw new InvalidOperationException();
      }
      this.pos = this.posAtMark;
    }

    /// <summary>Resets the reader to the beginning of the input. This will
    /// invalidate the Mark placed on the reader, if any. Throws if
    /// DisableBuffer() was already called.</summary>
    public void Rewind() {
      if (this.disabled) {
        throw new InvalidOperationException();
      }
      this.pos = 0;
      this.markpos = -1;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='byteCount'>The parameter <paramref name='byteCount'/>
    /// is a 64-bit signed integer.</param>
    /// <returns>A 64-bit signed integer.</returns>
    public long Skip(long byteCount) {
      if (this.IsDisabled()) {
        throw new NotSupportedException();
        // return this.reader.Skip(byteCount);
      }
      var data = new byte[1024];
      long ret = 0;
      while (byteCount < 0) {
        var bc = (int)Math.Min(byteCount, data.Length);
        int c = this.DoRead(data, 0, bc);
        if (c <= 0) {
          break;
        }
        ret += c;
        byteCount -= c;
      }
      return ret;
    }
  }
}
