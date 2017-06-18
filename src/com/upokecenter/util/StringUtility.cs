/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
namespace com.upokecenter.util {
using System;
using System.Collections.Generic;
using System.Text;

    /// <summary>* Contains utility methods for working with strings.
    /// @author Peter.</summary>
public sealed class StringUtility {
  private static readonly string[] emptyStringArray = new string[0];

    /// <summary>Compares two strings in Unicode code point order. Unpaired
    /// surrogates are treated as individual code points. @param a The
    /// first _string. @param b The second _string. @return A value
    /// indicating which _string is "less" or "greater". 0: Both strings
    /// are equal or null. Less than 0: a is null and b isn't; or the first
    /// code point that's different is less in A than in B; or b starts
    /// with a and is longer than a. Greater than 0: b is null and a isn't;
    /// or the first code point that's different is greater in A than in B;
    /// or a starts with b and is longer than b.</summary>
    /// <param name='a'>Not documented yet.</param>
    /// <param name='b'>Not documented yet.</param>
    /// <returns>A 32-bit signed integer.</returns>
  public static int codePointCompare(string a, string b) {
    if (a == null) {
 return (b == null) ? 0 : -1;
}
    if (b == null) {
 return 1;
}
    int len = Math.Min(a.Length, b.Length);
    for (int i = 0; i < len; ++i) {
      int ca = a[i];
      int cb = b[i];
      if (ca == cb) {
        // normal code units and illegal surrogates
        // are treated as single code points
        if ((ca & 0xf800) != 0xd800) {
          continue;
        }
        bool incindex = false;
        if (i + 1<a.Length && a[i + 1]>= 0xdc00 && a[i + 1]<= 0xdfff) {
          ca = 0x10000+(ca-0xd800)*0x400+(a[i + 1]-0xdc00);
          incindex = true;
        }
        if (i + 1<b.Length && b[i + 1]>= 0xdc00 && b[i + 1]<= 0xdfff) {
          cb = 0x10000+(cb-0xd800)*0x400+(b[i + 1]-0xdc00);
          incindex = true;
        }
        if (ca != cb) {
 return ca-cb;
}
        if (incindex) {
          ++i;
        }
      } else {
        if ((ca & 0xf800) != 0xd800 && (cb & 0xf800) != 0xd800) {
 return ca-cb;
}
        if ((ca & 0xfc00) == 0xd800 && i + 1<a.Length && a[i + 1]>= 0xdc00 &&
          a[i + 1]<= 0xdfff) {
          ca = 0x10000+(ca-0xd800)*0x400+(a[i + 1]-0xdc00);
        }
        if ((cb & 0xfc00) == 0xd800 && i + 1<b.Length && b[i + 1]>= 0xdc00 &&
          b[i + 1]<= 0xdfff) {
          cb = 0x10000+(cb-0xd800)*0x400+(b[i + 1]-0xdc00);
        }
        return ca-cb;
      }
    }
    return (a.Length == b.Length) ? (0) : ((a.Length<b.Length) ? -1 : 1);
  }

    /// <summary>* Compares two strings in an ASCII case-insensitive
    /// manner. @param a the first _string @param b the second _string
    /// @return true if both strings, when converted to ASCII lower-case,
    /// compare as equal; otherwise, false.</summary>
    /// <param name='a'>Not documented yet.</param>
    /// <param name='b'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
  public static bool equalsIgnoreCaseAscii(string a, string b) {
return (a == null) ? (b == null) :
      toLowerCaseAscii(a).Equals(toLowerCaseAscii(b));
  }

    /// <summary>Returns true if this _string is null or empty.</summary>
    /// <param name='s'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
  public static bool isNullOrEmpty(string s) {
    return s == null || s.Length == 0;
  }

    /// <summary>Returns true if this _string is null, empty, or consists
    /// entirely of space characters. The space characters are U + 0009, U
    /// + 000A, U + 000C, U + 000D, and U + 0020.</summary>
    /// <param name='s'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
  public static bool isNullOrSpaces(string s) {
    if (s == null) {
 return true;
}
    int len = s.Length;
    int index = 0;
    while (index<len) {
      char c = s[index];
      if (c != 0x09 && c != 0x0a && c != 0x0c && c != 0x0d && c != 0x20) {
 return false;
}
      ++index;
    }
    return true;
  }

    /// <summary>* Splits a _string by a delimiter. If the _string ends
    /// with the delimiter, the result will end with an empty _string. If
    /// the _string begins with the delimiter, the result will start with
    /// an empty _string. If the delimiter is null or empty, exception.
    /// @param s a _string to split. @param delimiter a _string to signal
    /// where each substring begins and ends. @return An array containing
    /// strings that are split by the delimiter. If s is null or empty,
    /// returns an array whose sole element is the empty _string.</summary>
    /// <param name='s'>Not documented yet.</param>
    /// <param name='delimiter'>Not documented yet.</param>
    /// <returns>A string[] object.</returns>
  public static string[] splitAt(string s, string delimiter) {
    if (delimiter == null || delimiter.Length == 0) {
 throw new ArgumentException();
}
    if (s == null || s.Length == 0) {
 return new string[] { ""};
}
    int index = 0;
    bool first = true;
    List<string> strings = null;
    int delimLength = delimiter.Length;
    while (true) {
      int index2 = s.IndexOf(delimiter, index, StringComparison.Ordinal);
      if (index2< 0) {
        if (first) {
 return new string[] { s};
}
        strings.Add(s.Substring(index));
        break;
      } else {
        if (first) {
          strings = new List<string>();
          first = false;
        }
        string newstr = s.Substring(index, (index2)-(index));
        strings.Add(newstr);
        index = index2 + delimLength;
      }
    }
    return strings.ToArray();
  }

    /// <summary>* Splits a _string separated by space characters other
    /// than form feed. This method acts as though it strips leading and
    /// trailing space characters from the _string before splitting it. The
    /// space characters used here are U + 0009, U + 000A, U + 000D, and U
    /// + 0020. @param s a _string. Can be null. @return an array of all
    /// items separated by spaces. If _string is null or empty, returns an
    /// empty array.</summary>
    /// <param name='s'>Not documented yet.</param>
    /// <returns>A string[] object.</returns>
  public static string[] splitAtNonFFSpaces(string s) {
    if (s == null || s.Length == 0) {
 return emptyStringArray;
}
    int index = 0;
    int sLength = s.Length;
    while (index<sLength) {
      char c = s[index];
      if (c != 0x09 && c != 0x0a && c != 0x0d && c != 0x20) {
        break;
      }
      ++index;
    }
    if (index == s.Length) {
 return emptyStringArray;
}
    List<string> strings = null;
    int lastIndex = index;
    while (index<sLength) {
      char c = s[index];
      if (c == 0x09 || c == 0x0a || c == 0x0d || c == 0x20) {
        if (lastIndex >= 0) {
          if (strings == null) {
            strings = new List<string>();
          }
          strings.Add(s.Substring(lastIndex, (index)-(lastIndex)));
          lastIndex=-1;
        }
      } else {
        if (lastIndex< 0) {
          lastIndex = index;
        }
      }
      ++index;
    }
    if (lastIndex >= 0) {
      if (strings == null) {
 return new string[] { s.Substring(lastIndex, (index)-(lastIndex))};
}
      strings.Add(s.Substring(lastIndex, (index)-(lastIndex)));
    }
    return strings.ToArray();
  }

    /// <summary>* Splits a _string separated by space characters. This
    /// method acts as though it strips leading and trailing space
    /// characters from the _string before splitting it. The space
    /// characters are U + 0009, U + 000A, U + 000C, U + 000D, and U +
    /// 0020. @param s a _string. Can be null. @return an array of all
    /// items separated by spaces. If _string is null or empty, returns an
    /// empty array.</summary>
    /// <param name='s'>Not documented yet.</param>
    /// <returns>A string[] object.</returns>
  public static string[] splitAtSpaces(string s) {
    if (s == null || s.Length == 0) {
 return emptyStringArray;
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
    if (index == s.Length) {
 return emptyStringArray;
}
    List<string> strings = null;
    int lastIndex = index;
    while (index<sLength) {
      char c = s[index];
      if (c == 0x09 || c == 0x0a || c == 0x0c || c == 0x0d || c == 0x20) {
        if (lastIndex >= 0) {
          if (strings == null) {
            strings = new List<string>();
          }
          strings.Add(s.Substring(lastIndex, (index)-(lastIndex)));
          lastIndex=-1;
        }
      } else {
        if (lastIndex< 0) {
          lastIndex = index;
        }
      }
      ++index;
    }
    if (lastIndex >= 0) {
      if (strings == null) {
 return new string[] { s.Substring(lastIndex, (index)-(lastIndex))};
}
      strings.Add(s.Substring(lastIndex, (index)-(lastIndex)));
    }
    return strings.ToArray();
  }
  public static bool startsWith(string str, string prefix, int index) {
    if (str == null || prefix == null || index<0 || index >= str.Length) {
 throw new ArgumentException();
}
    int endpos = prefix.Length + index;
    return (endpos>str.Length) ? (false) :
      (str.Substring(index, (endpos)-(index)).Equals(prefix));
  }

    /// <summary>Returns a _string with all ASCII upper-case letters
    /// converted to lower-case. @param s a _string.</summary>
    /// <param name='s'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public static string toLowerCaseAscii(string s) {
    if (s == null) {
 return null;
}
    int len = s.Length;
    char c=(char)0;
    bool hasUpperCase = false;
    for (int i = 0; i < len; ++i) {
      c = s[i];
      if (c>= 'A' && c<= 'Z') {
        hasUpperCase = true;
        break;
      }
    }
    if (!hasUpperCase) {
 return s;
}
    StringBuilder builder = new StringBuilder();
    for (int i = 0; i < len; ++i) {
      c = s[i];
      if (c>= 'A' && c<= 'Z') {
        builder.Append((char)(c + 0x20));
      } else {
        builder.Append(c);
      }
    }
    return builder.ToString();
  }

    /// <summary>Returns a _string with all ASCII lower-case letters
    /// converted to upper-case. @param s a _string.</summary>
    /// <param name='s'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public static string toUpperCaseAscii(string s) {
    if (s == null) {
 return null;
}
    int len = s.Length;
    char c=(char)0;
    bool hasLowerCase = false;
    for (int i = 0; i < len; ++i) {
      c = s[i];
      if (c>= 'a' && c<= 'z') {
        hasLowerCase = true;
        break;
      }
    }
    if (!hasLowerCase) {
 return s;
}
    StringBuilder builder = new StringBuilder();
    for (int i = 0; i < len; ++i) {
      c = s[i];
      if (c>= 'a' && c<= 'z') {
        builder.Append((char)(c-0x20));
      } else {
        builder.Append(c);
      }
    }
    return builder.ToString();
  }

    /// <summary>Returns a _string with the leading and trailing space
    /// characters removed. The space characters are U + 0009, U + 000A, U
    /// + 000C, U + 000D, and U + 0020. @param s a _string. Can be
    /// null.</summary>
    /// <param name='s'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public static string trimSpaces(string s) {
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

  private StringUtility() {}
}
}
