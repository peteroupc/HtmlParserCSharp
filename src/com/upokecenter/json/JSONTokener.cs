// Modified by Peter O. to use generics and
// to use int and -1 as the terminating
// value rather than char and 0, among
// other things; also moved from org.json.
// Still in the public domain;
// public domain dedication: http://creativecommons.org/publicdomain/zero/1.0/
namespace com.upokecenter.json {
using System;
using System.Globalization;
using System.Text;

public class JSONTokener {
    /// <summary>Get the hex value of a character (base16). @param c A
    /// character between '0' and '9' or between 'A' and 'F' or between 'a'
    /// and 'f'. @return An int between 0 and 15, or -1 if c was not a hex
    /// digit.</summary>
    /// <param name='c'>Not documented yet.</param>
    /// <returns>A 32-bit signed integer.</returns>
  public static int dehexchar(char c) {
    if (c >= '0' && c <= '9') {
 return c - '0';
}
    if (c >= 'A' && c <= 'F') {
 return c + 10 - 'A';
}
    return (c >= 'a' && c <= 'f') ? (c + 10 - 'a') : (-1);
  }

  private static string trimSpaces(string s) {
    if (s == null || s.Length == 0) {
 return s;
}
    int index = 0;
    int sLength = s.Length;
    while (index<sLength) {
      char c = s[index];
      if (c != 0x09 && c != 0x0a && c != 0x0c && c != 0x0d && c != 0x20) {
        break;
      }
      ++index;
    }
    if (index == sLength) {
 return "";
}
    int startIndex = index;
    index = sLength-1;
    while (index >= 0) {
      char c = s[index];
      if (c != 0x09 && c != 0x0a && c != 0x0c && c != 0x0d && c != 0x20) {
 return s.Substring(startIndex, (index + 1)-(startIndex));
}
      --index;
    }
    return "";
  }

    /// <summary>
    /// Convert
    /// <code>%</code>
    /// <i>hh</i>
    ///  sequences to single characters, and convert plus to
    /// space. @param s A _string that may contain
    /// <code>+</code>
    ///  &nbsp;
    /// <small>(plus)</small>
    ///  and
    /// <code>%</code>
    /// <i>hh</i>
    ///  sequences. @return The unescaped _string.
    /// </summary>
    /// <param name='s'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public static string unescape(string s) {
    int len = s.Length;
    var b = new StringBuilder();
    for (int i = 0; i < len; ++i) {
      char c = s[i];
      if (c == '+') {
        c = ' ';
      } else if (c == '%' && i + 2 < len) {
        int d = dehexchar(s[i + 1]);
        int e = dehexchar(s[i + 2]);
        if (d >= 0 && e >= 0) {
          c = (char)(d * 16 + e);
          i += 2;
        }
      }
      b.Append(c);
    }
    return b.ToString();
  }

    /// <summary>The index of the next character.</summary>
  private int myIndex;

    /// <summary>The source _string being tokenized.</summary>
  private string mySource;

  private int options;

    /// <summary>Initializes a new instance of the JSONTokener class.
    /// Construct a JSONTokener from a _string. @param s A source
    /// _string.</summary>
    /// <param name='s'>A string object.</param>
    /// <param name='options'>A 32-bit signed integer.</param>
  public JSONTokener(string s, int options) {
    myIndex = 0;
    mySource = s;
    this.options = options;
  }

    /// <summary>Back up one character. This provides a sort of lookahead
    /// capability, so that you can test for a digit or letter before
    /// attempting to parse the next number or identifier.</summary>
  public void back() {
    if (myIndex > 0) {
      --myIndex;
    }
  }

    /// <summary>Determine if the source _string still contains characters
    /// that next() can consume. @return true if not yet at the end of the
    /// source.</summary>
    /// <returns>A Boolean object.</returns>
  public bool more() {
    return myIndex < mySource.Length;
  }

    /// <summary>Get the next character in the source _string. @return The
    /// next character, or 0 if past the end of the source
    /// _string.</summary>
    /// <returns>A 32-bit signed integer.</returns>
  public int next() {
    int c = more() ? mySource[myIndex] : -1;
    ++myIndex;
    return c;
  }

    /// <summary>Consume the next character, and check that it matches a
    /// specified character. @param c The character to match. @return The
    /// character. @ if the character does not match.</summary>
    /// <param name='c'>Not documented yet.</param>
    /// <returns>A 32-bit signed integer.</returns>
  public int next(char c) {
    int n = next();
    if (n != c) {
 throw syntaxError("Expected '" + c + "' and instead saw '" +
          n + "'.");
 }
    return n;
  }

    /// <summary>Get the next n characters. @param n The number of
    /// characters to take. @return A _string of n characters. @exception
    /// Json.InvalidJsonException Substring bounds error if there are not n
    /// characters remaining in the source _string.</summary>
    /// <param name='n'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public string next(int n) {
    int i = myIndex;
    int j = i + n;
    if (j >= mySource.Length) {
 throw syntaxError("Substring bounds error");
}
    myIndex += n;
    return mySource.Substring(i, (j)-(i));
  }

  public int getOptions() {
    return options;
  }

    /// <summary>Get the next comment or comments in the _string, if any.
    /// (Added by Peter O., 5/6/2013).</summary>
    /// <returns>A string object.</returns>
  public string nextComment() {
    StringBuilder builder = new StringBuilder();
    while (true) {
      int c = next();
      if (c=='#' && (options & JSONObject.OPTION_SHELL_COMMENTS)!=0) {
        // Shell-style single-line comment
        bool haveChar = false;
        while (true) {
          c = next();
          if (c != '\n' && c != '\r' && c != -1) {
            if (haveChar || c>' ') {
              if (!haveChar && builder.Length>0) {
 builder.Append(' ');  // append space if comment is continuing
}
              builder.Append((char)c);
              haveChar = true;
            }
          } else {
 break;  // end of line
}
        }
  } else if (c == '/') {
        switch (next()) {
        case '/':{  // single-line comment
          bool haveChar = false;
          while (true) {
            c = next();
            if (c != '\n' && c != '\r' && c != -1) {
              if (haveChar || c>' ') {
                if (!haveChar && builder.Length>0) {
 builder.Append(' ');  // append space if comment is continuing
}
                builder.Append((char)c);
                haveChar = true;
              }
            } else {
 break;  // end of line
}
          }
          break;
        }
        case '*':{  // multi-line comment
          bool haveChar = false;
          while (true) {
            c = next();
            if (c == -1) {
 throw syntaxError("Unclosed comment.");
}
            if (c == '*') {
              if (next() == '/') {
                break;
              }
              back();
            }
            if (haveChar || c>' ') {
              if (!haveChar && builder.Length>0) {
 builder.Append(' ');  // append space if comment is continuing
}
              builder.Append((char)c);
              haveChar = true;
            }
          }
          break;
        }
        default: back();
          return builder.ToString();
        }
      } else if (c == -1) {
        return builder.ToString();  // reached end of _string
      } else if (c>' ') {
        // reached an ordinary character
        back();
        return builder.ToString();
      }
    }
  }

    /// <summary>Get the next char in the _string, skipping whitespace and
    /// comments (slashslash and slashstar). @ @return A character, or 0 if
    /// there are no more characters.</summary>
    /// <returns>A 32-bit signed integer.</returns>
  public int nextClean() {
    nextComment();
    while (true) {
      int c = next();
      if (c == -1 || c > ' ') {
 return c;
}
    }
  }

    /// <summary>
    /// Return the characters up to the next close quote
    /// character. Backslash processing is done. The formal
    /// JSON format does not allow strings in single quotes,
    /// but an implementation is allowed to accept them. @param
    /// quote The quoting character, either
    /// <code>"</code>
    ///  &nbsp;
    /// <small>(double quote)</small>
    ///  or
    /// <code>'</code>
    ///  &nbsp;
    /// <small>(single quote)</small>
    ///  . @return A string. @exception
    /// Json.InvalidJsonException Unterminated _string.
    /// </summary>
    /// <param name='quote'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public string nextString(int quote) {
    int c;
    var sb = new StringBuilder();
    while (true) {
      c = next();
      switch (c) {
      case -1:
      case 0x0A:
      case 0x0D:
        throw syntaxError("Unterminated string");
      case '\\':
        c = next();
        switch (c) {
        case 'b':
          sb.Append('\b');
          break;
        case 't':
          sb.Append('\t');
          break;
        case 'n':
          sb.Append('\n');
          break;
        case 'f':
          sb.Append('\f');
          break;
        case 'r':
          sb.Append('\r');
          break;
        case 'u':

  sb.Append((char)Int32.Parse(next(4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
          break;
        case 'x' :

  sb.Append((char)Int32.Parse(next(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
          break;
        default:
          sb.Append((char)c);
          break;
        }
        break;
      default: if (c == quote) {
 return sb.ToString();
}
        sb.Append((char)c);
        break;
      }
    }
  }

    /// <summary>Get the text up but not including the specified character
    /// or the end of line, whichever comes first. @param d A delimiter
    /// character. @return A _string.</summary>
    /// <param name='d'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public string nextTo(char d) {
    var sb = new StringBuilder();
    while (true) {
      int c = next();
      if (c == d || c == -1 || c == '\n' || c == '\r') {
        if (c != -1) {
          back();
        }
        return trimSpaces(sb.ToString());
      }
      sb.Append((char)c);
    }
  }

    /// <summary>Get the text up but not including one of the specified
    /// delimeter characters or the end of line, which ever comes first.
    /// @param delimiters A set of delimiter characters. @return A _string,
    /// trimmed.</summary>
    /// <param name='delimiters'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public string nextTo(string delimiters) {
    int c;
    var sb = new StringBuilder();
    while (true) {
      c = next();
      if (c==-1 || delimiters.IndexOf((char)c) >= 0 ||
          c == '\n' || c == '\r') {
        if (c != -1) {
          back();
        }
        return trimSpaces(sb.ToString());
      }
      sb.Append((char)c);
    }
  }

    /// <summary>Get the next value. The value can be a Boolean, Double,
    /// Integer, JSONArray, JSONObject, or string, or the JSONObject.NULL
    /// _object. @exception Json.InvalidJsonException The source conform to
    /// JSON syntax. @return An _object.</summary>
    /// <returns>An arbitrary object.</returns>
  public Object nextValue() {
    int c = nextClean();
    string s;

    if (c == '"' || c == '\'') {
 return nextString(c);
}
    if (c == '{') {
      back();
      return new JSONObject(this);
    }
    if (c == '[') {
      back();
      return new JSONArray(this);
    }
    var sb = new StringBuilder();
    int b = c;
    while (c >= ' ' && c != ':' && c != ',' && c != ']' && c != '}' &&
        c != '/') {
      sb.Append((char)c);
      c = next();
    }
    back();
    s = trimSpaces(sb.ToString());
    if (s.Equals("true")) {
 return (object)true;
}
    if (s.Equals("false")) {
 return (object)false;
}
    if (s.Equals("null")) {
 return JSONObject.NULL;
}
    if ((b >= '0' && b <= '9') || b == '.' || b == '-' || b == '+') {
      try {
        return
  Int32.Parse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
      } catch (FormatException) {}
      try {
        return Double.Parse(s, CultureInfo.InvariantCulture);
      } catch (FormatException) {}
    }
    if (s.Length == 0) {
 throw syntaxError("Missing value.");
}
    return s;
  }

    /// <summary>Skip characters until past the requested _string. If it is
    /// not found, we are left at the end of the source. @param to A
    /// _string to skip past.</summary>
    /// <param name='to'>Not documented yet.</param>
  public void skipPast(string to) {
    myIndex = mySource.IndexOf(to, myIndex, StringComparison.Ordinal);
    if (myIndex < 0) {
      myIndex = mySource.Length;
    } else {
      myIndex += to.Length;
    }
  }

    /// <summary>Skip characters until the next character is the requested
    /// character. If the requested character is not found, no characters
    /// are skipped. @param to A character to skip to. @return The
    /// requested character, or zero if the requested character is not
    /// found.</summary>
    /// <param name='to'>Not documented yet.</param>
    /// <returns>A 32-bit signed integer.</returns>
  public int skipTo(int to) {
    int c;
    int index = myIndex;
    do {
      c = next();
      if (c == -1) {
        myIndex = index;
        return c;
      }
    } while (c != to);
    back();
    return c;
  }

    /// <summary>Make a Json.InvalidJsonException to signal a syntax error.
    /// @param message The error message. @return A
    /// Json.InvalidJsonException _object, suitable for throwing.</summary>
  public Json.InvalidJsonException syntaxError(string message) {
    return new Json.InvalidJsonException(message + ToString(), myIndex);
  }

    /// <summary>Make a printable _string of this JSONTokener. @return " at
    /// character [myIndex] of [mySource]".</summary>
    /// <returns>A string object.</returns>
  public override string ToString() {
    return " at character " + myIndex + " of " + mySource;
  }

    /// <summary>
    /// Unescape the source text. Convert
    /// <code>%</code>
    /// <i>hh</i>
    ///  sequences to single characters, and convert plus to
    /// space. There are Web transport systems that insist on
    /// doing unnecessary URL encoding. This provides a way to
    /// undo it.
    /// </summary>
  internal void unescape() {
    mySource = unescape(mySource);
  }
}
}
