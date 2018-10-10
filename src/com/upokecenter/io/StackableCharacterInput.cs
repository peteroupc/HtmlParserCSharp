/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PeterO.Text;
namespace com.upokecenter.io {
    /// <summary>* A character input stream where additional inputs can be
    /// stacked on top of it. It supports advanced marking capabilities.
    /// @author Peter.</summary>
public sealed class StackableCharacterInput : IMarkableCharacterInput {
  private class InputAndBuffer : ICharacterInput {
    private int[] valueBuffer;
    private ICharacterInput valueCharInput;
    private int pos = 0;

    public InputAndBuffer(
  ICharacterInput valueCharInput,
  int[] valueBuffer,
  int offset,
  int length) {
      this.valueCharInput = valueCharInput;
      if (length > 0) {
        this.valueBuffer = new int[length];
        Array.Copy(valueBuffer, offset, this.valueBuffer, 0, length);
      } else {
        this.valueBuffer = null;
      }
    }

    public int ReadChar() {
      if (this.valueCharInput != null) {
        int c = this.valueCharInput.ReadChar();
        if (c >= 0) {
 return c;
}
        this.valueCharInput = null;
      }
      if (this.valueBuffer != null) {
        if (this.pos < this.valueBuffer.Length) {
 return this.valueBuffer[this.pos++];
}
        this.valueBuffer = null;
      }
      return -1;
    }

    public int Read(int[] buf, int offset, int unitCount) {
      if (buf == null) {
 throw new ArgumentNullException(nameof(buf));
}
      if (offset < 0) {
 throw new ArgumentException("offset less than 0 ("
   +(offset)+")");
}
      if (unitCount < 0) {
 throw new ArgumentException("unitCount less than 0 ("
   +(unitCount)+")");
}
      if (offset + unitCount > buf.Length) {
 throw new
   ArgumentOutOfRangeException("offset+unitCount more than "
   +(buf.Length)+" ("
   +(offset + unitCount) + ")");
}
      if (unitCount == 0) {
 return 0;
}
      var count = 0;
      if (this.valueCharInput != null) {
        int c = this.valueCharInput.Read(buf, offset, unitCount);
        if (c <= 0) {
          this.valueCharInput = null;
        } else {
          offset += c;
          unitCount -= c;
          count += c;
        }
      }
      if (this.valueBuffer != null) {
        int c = Math.Min(unitCount, this.valueBuffer.Length - this.pos);
        if (c > 0) {
          Array.Copy(this.valueBuffer, this.pos, buf, offset, c);
        }
        this.pos += c;
        count += c;
        if (c == 0) {
          this.valueBuffer = null;
        }
      }
      return (count == 0) ? -1 : count;
    }
  }

  private int pos = 0;
  private int endpos = 0;
  private bool haveMark = false;
  private int[] valueBuffer = null;
  private IList<ICharacterInput> valueStack = new List<ICharacterInput>();

    /// <summary>Initializes a new instance of the StackableCharacterInput
    /// class.</summary>
    /// <param name='source'>An ICharacterInput object.</param>
  public StackableCharacterInput(ICharacterInput source) {
    this.valueStack.Add(source);
  }

    /// <summary>Not documented yet.</summary>
    /// <returns>A 32-bit signed integer.</returns>
  public int getMarkPosition() {
    return this.pos;
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='count'>Not documented yet.</param>
  public void moveBack(int count) {
    if (count < 0) {
 throw new ArgumentException("count less than 0 ("
   +(count)+")");
}
    if (this.haveMark && this.pos >= count) {
      this.pos -= count;
      return;
    }
    throw new IOException();
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='input'>Not documented yet.</param>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='input'/> is null.</exception>
  public void pushInput(ICharacterInput input) {
    if (input == null) {
 throw new ArgumentNullException(nameof(input));
}
    // Move unread characters in valueBuffer, since this new
    // input sits on top of the existing input
    this.valueStack.Add(
  new InputAndBuffer(
  input,
  this.valueBuffer,
  this.pos,
  this.endpos - this.pos));
    this.endpos = this.pos;
  }

    /// <summary>Not documented yet.</summary>
    /// <returns>A 32-bit signed integer.</returns>
  public int ReadChar() {
    if (this.haveMark) {
      // Read from valueBuffer
      if (this.pos < this.endpos) {
 return this.valueBuffer[this.pos++];
}
      // DebugUtility.Log(this);
      // End pos is smaller than valueBuffer size, fill
      // entire valueBuffer if possible
      if (this.endpos < this.valueBuffer.Length) {
        int count = this.readInternal(
  this.valueBuffer,
  this.endpos,
  this.valueBuffer.Length - this.endpos);
        if (count > 0) {
          this.endpos += count;
        }
      }
      // Try reading from valueBuffer again
      if (this.pos < this.endpos) {
 return this.valueBuffer[this.pos++];
}
      // DebugUtility.Log(this);
      // No room, read next character and put it in valueBuffer
      int c = this.readInternal();
      if (c < 0) {
 return c;
}
      if (this.pos >= this.valueBuffer.Length) {
        var newBuffer = new int[this.valueBuffer.Length * 2];
        Array.Copy(this.valueBuffer, 0, newBuffer, 0, this.valueBuffer.Length);
        this.valueBuffer = newBuffer;
      }
      // DebugUtility.Log(this);
      this.valueBuffer[this.pos++] = (byte)(c & 0xff);
      ++this.endpos;
      return c;
    } else {
 return this.readInternal();
}
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='buf'>Not documented yet.</param>
    /// <param name='offset'>Not documented yet.</param>
    /// <param name='unitCount'>Not documented yet. (3).</param>
    /// <returns>A 32-bit signed integer.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='buf'/> is null.</exception>
  public int Read(int[] buf, int offset, int unitCount) {
    if (this.haveMark) {
      if (buf == null) {
 throw new ArgumentNullException(nameof(buf));
}
      if (offset < 0) {
 throw new ArgumentException("offset less than 0 ("
   +(offset)+")");
}
      if (unitCount < 0) {
 throw new ArgumentException("unitCount less than 0 ("
   +(unitCount)+")");
}
      if (offset + unitCount > buf.Length) {
 throw new
   ArgumentOutOfRangeException("offset+unitCount more than "
   +(buf.Length)+" ("
   +(offset + unitCount) + ")");
}
      if (unitCount == 0) {
 return 0;
}
      // Read from valueBuffer
      if (this.pos + unitCount <= this.endpos) {
        Array.Copy(this.valueBuffer, this.pos, buf, offset, unitCount);
        this.pos += unitCount;
        return unitCount;
      }
      // End pos is smaller than valueBuffer size, fill
      // entire valueBuffer if possible
      var count = 0;
      if (this.endpos < this.valueBuffer.Length) {
        count = this.readInternal(
  this.valueBuffer,
  this.endpos,
  this.valueBuffer.Length - this.endpos);
        // DebugUtility.Log("%s",this);
        if (count > 0) {
          this.endpos += count;
        }
      }
      var total = 0;
      // Try reading from valueBuffer again
      if (this.pos + unitCount <= this.endpos) {
        Array.Copy(this.valueBuffer, this.pos, buf, offset, unitCount);
        this.pos += unitCount;
        return unitCount;
      }
      // expand the valueBuffer
      if (this.pos + unitCount > this.valueBuffer.Length) {
        var newBuffer = new int[(this.valueBuffer.Length * 2) + unitCount];
        Array.Copy(this.valueBuffer, 0, newBuffer, 0, this.valueBuffer.Length);
        this.valueBuffer = newBuffer;
      }
  count = this.readInternal(
  this.valueBuffer,
  this.endpos,
  Math.Min(unitCount, this.valueBuffer.Length - this.endpos));
      if (count > 0) {
        this.endpos += count;
      }
      // Try reading from valueBuffer a third time
      if (this.pos + unitCount <= this.endpos) {
        Array.Copy(this.valueBuffer, this.pos, buf, offset, unitCount);
        this.pos += unitCount;
        total += unitCount;
      } else if (this.endpos > this.pos) {
   Array.Copy(
  this.valueBuffer,
  this.pos,
  buf,
  offset,
  this.endpos - this.pos);
        total += this.endpos - this.pos;
        this.pos = this.endpos;
      }
      return (total == 0) ? -1 : total;
    } else {
 return this.readInternal(buf, offset, unitCount);
}
  }

  private int readInternal() {
    if (this.valueStack.Count == 0) {
 return -1;
}
    while (this.valueStack.Count > 0) {
      int index = this.valueStack.Count - 1;
      int c = this.valueStack[index].ReadChar();
      if (c == -1) {
        this.valueStack.RemoveAt(index);
        continue;
      }
      return c;
    }
    return -1;
  }

  private int readInternal(int[] buf, int offset, int unitCount) {
    if (this.valueStack.Count == 0) {
 return -1;
}
    #if DEBUG
if (!(buf != null)) {
 throw new InvalidOperationException("buf");
}
if (!(offset >= 0)) {
 throw new
  InvalidOperationException("offset less than 0 ("
  +(offset)+")");
 }
if (!(unitCount >= 0)) {
 throw new
  InvalidOperationException("unitCount less than 0 ("
  +(unitCount)+")");
 }
if (!((offset + unitCount) <= buf.Length)) {
 throw new
  InvalidOperationException("offset+unitCount more than "
  +(buf.Length)+" ("
  +(offset + unitCount) + ")");
 }
#endif
    if (unitCount == 0) {
 return 0;
}
    var count = 0;
    while (this.valueStack.Count > 0 && unitCount > 0) {
      int index = this.valueStack.Count - 1;
      int c = this.valueStack[index].Read(buf, offset, unitCount);
      if (c <= 0) {
        this.valueStack.RemoveAt(index);
        continue;
      }
      count += c;
      unitCount -= c;
      if (unitCount == 0) {
        break;
      }
      this.valueStack.RemoveAt(index);
    }
    return count;
  }

    /// <summary>Not documented yet.</summary>
    /// <returns>A 32-bit signed integer.</returns>
  public int setHardMark() {
    if (this.valueBuffer == null) {
      this.valueBuffer = new int[16];
      this.pos = 0;
      this.endpos = 0;
      this.haveMark = true;
    } else if (this.haveMark) {
      // Already have a mark; shift valueBuffer to the new mark
      if (this.pos > 0 && this.pos < this.endpos) {
     Array.Copy(
  this.valueBuffer,
  this.pos,
  this.valueBuffer,
  0,
  this.endpos - this.pos);
      }
      this.endpos -= this.pos;
      this.pos = 0;
    } else {
      this.pos = 0;
      this.endpos = 0;
      this.haveMark = true;
    }
    return 0;
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='pos'>Not documented yet.</param>
  public void setMarkPosition(int pos) {
    if (!this.haveMark || pos < 0 || pos > this.endpos) {
 throw new IOException();
}
    this.pos = pos;
  }

    /// <summary>Not documented yet.</summary>
    /// <returns>A 32-bit signed integer.</returns>
  public int setSoftMark() {
    if (!this.haveMark) {
      this.setHardMark();
    }
    return this.getMarkPosition();
  }
}
}
