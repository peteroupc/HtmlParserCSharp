/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
using System;
using System.IO;
using PeterO;

namespace com.upokecenter.io {
    /// <summary>* An input stream that stores the first bytes of the
    /// stream in a buffer and supports rewinding to the beginning of the
    /// stream. However, when the buffer is disabled, no further bytes are
    /// put into the buffer, but any remaining bytes in the buffer will
    /// still be used until it's exhausted. @author Peter O.</summary>
public sealed class ConditionalBufferInputStream :
  IReader, com.upokecenter.html.HtmlParser.IInputStream {
  private byte[] buffer = null;
  private int pos = 0;
  private int endpos = 0;
  private bool disabled = false;
  private long markpos = -1;
  private int posAtMark = 0;
  private long marklimit = 0;
  private IReader stream = null;

    /// <summary>Initializes a new instance of the
    /// ConditionalBufferInputStream class.</summary>
    /// <param name='input'>A PeterO.Support.InputStream object.</param>
  public ConditionalBufferInputStream(IReader input) {
    this.stream = input;
    this.buffer = new byte[1024];
  }

    /// <summary>Disables buffering of future bytes read from the
    /// underlying stream. However, any bytes already buffered can still be
    /// read until the buffer is exhausted. After the buffer is exhausted,
    /// this stream will fully delegate to the underlying stream.</summary>
  public void disableBuffer() {
    this.disabled = true;
    if (this.buffer != null && this.isDisabled()) {
      this.buffer = null;
    }
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='buffer'>Not documented yet.</param>
    /// <param name='offset'>Not documented yet.</param>
    /// <param name='byteCount'>Not documented yet. (3).</param>
    /// <returns>A 32-bit signed integer.</returns>
  public int doRead(byte[] buffer, int offset, int byteCount) {
    if (this.markpos < 0) {
 return this.readInternal(buffer, offset, byteCount);
} else {
      if (this.isDisabled()) {
 return this.stream.Read(buffer, offset, byteCount);
}
      int c = this.readInternal(buffer, offset, byteCount);
      if (c > 0 && this.markpos >= 0) {
        this.markpos += c;
        if (this.markpos > this.marklimit) {
          this.marklimit = 0;
          this.markpos = -1;
          if (this.buffer != null && this.isDisabled()) {
            this.buffer = null;
          }
        }
      }
      return c;
    }
  }

  private bool isDisabled() {
    return this.disabled ? ((this.markpos >= 0 &&
      this.markpos<this.marklimit) ? false : (this.pos<this.endpos)) :
      (false);
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='limit'>Not documented yet.</param>
  public void mark(int limit) {
    // DebugUtility.Log("mark %d: %s",limit,isDisabled());
    if (this.isDisabled()) {
        // this.stream.mark(limit);
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
    /// <returns>A Boolean object.</returns>
  public bool markSupported() {
    return true;
  }

    /// <summary>Not documented yet.</summary>
    /// <returns>A 32-bit signed integer.</returns>
  public int ReadByte() {
    if (this.markpos < 0) {
 return this.readInternal();
} else {
      if (this.isDisabled()) {
 return this.stream.ReadByte();
}
      int c = this.readInternal();
      if (c >= 0 && this.markpos >= 0) {
        ++this.markpos;
        if (this.markpos > this.marklimit) {
          this.marklimit = 0;
          this.markpos = -1;
          if (this.buffer != null && this.isDisabled()) {
            this.buffer = null;
          }
        }
      }
      return c;
    }
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='buffer'>Not documented yet.</param>
    /// <param name='offset'>Not documented yet.</param>
    /// <param name='byteCount'>Not documented yet. (3).</param>
    /// <returns>A 32-bit signed integer.</returns>
  public int Read(byte[] buffer, int offset, int byteCount) {
    return this.doRead(buffer, offset, byteCount);
  }

  private int readInternal() {
    // Read from buffer
    if (this.pos < this.endpos) {
 return this.buffer[this.pos++] & 0xff;
}
    // if (buffer != null) {
  // DebugUtility.Log("buffer %s end=%s len=%s",pos,endpos,buffer.Length);
// }
    if (this.disabled) {
 // Buffering disabled, so read directly from stream
      return this.stream.ReadByte();
 }
    // End pos is smaller than buffer size, fill
    // entire buffer if possible
    if (this.endpos < this.buffer.Length) {
      int count = this.stream.Read(
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
    int c = this.stream.ReadByte();
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

  private int readInternal(byte[] buf, int offset, int unitCount) {
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
  // DebugUtility.Log("buffer(3arg) %s end=%s len=%s",pos,endpos,buffer.Length);
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
      // Read directly from the stream for the rest
      if (unitCount > 0) {
        int c = this.stream.Read(buf, offset, unitCount);
        if (c > 0) {
          total += c;
        }
      }
      return (total == 0) ? -1 : total;
    }
    // End pos is smaller than buffer size, fill
    // entire buffer if possible
    if (this.endpos < this.buffer.Length) {
      count = this.stream.Read(
  this.buffer,
  this.endpos,
  this.buffer.Length - this.endpos);
      // DebugUtility.Log("%s",this);
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
count = this.stream.Read(
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
  public void reset() {
    // DebugUtility.Log("reset: %s",isDisabled());
    if (this.isDisabled()) {
        throw new NotSupportedException();
      // this.stream.reset();
      // return;
    }
    if (this.markpos < 0) {
 throw new IOException();
}
    this.pos = this.posAtMark;
  }

    /// <summary>* Resets the stream to the beginning of the input. This
    /// will invalidate the mark placed on the stream, if any. @ if
    /// disableBuffer() was already called.</summary>
  public void rewind() {
    if (this.disabled) {
 throw new IOException();
}
    this.pos = 0;
    this.markpos = -1;
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='byteCount'>Not documented yet.</param>
    /// <returns>A 64-bit signed integer.</returns>
  public long skip(long byteCount) {
    if (this.isDisabled()) {
        throw new NotSupportedException();
 // return this.stream.skip(byteCount);
}
    var data = new byte[1024];
    long ret = 0;
    while (byteCount < 0) {
      var bc = (int)Math.Min(byteCount, data.Length);
      int c = this.doRead(data, 0, bc);
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
