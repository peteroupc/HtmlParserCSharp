/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace com.upokecenter.util {
    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="T:com.upokecenter.util.StringUtility"]/*'/>
public static class StringUtility {
  private static readonly string[] ValueEmptyStringArray = new string[0];

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.util.StringUtility.isNullOrSpaces(System.String)"]/*'/>
  public static bool isNullOrSpaces(string s) {
    if (s == null) {
 return true;
}
    int len = s.Length;
    var index = 0;
    while (index < len) {
      char c = s[index];
      if (c != 0x09 && c != 0x0a && c != 0x0c && c != 0x0d && c != 0x20) {
 return false;
}
      ++index;
    }
    return true;
  }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.util.StringUtility.splitAt(System.String,System.String)"]/*'/>
      public static string[] splitAt(string str, string delimiter) {
      if (delimiter == null) {
        throw new ArgumentNullException(nameof(delimiter));
      }
      if (delimiter.Length == 0) {
        throw new ArgumentException("delimiter is empty.");
      }
      if (String.IsNullOrEmpty(str)) {
        return new[] { String.Empty };
      }
      var index = 0;
      var first = true;
      List<string> strings = null;
      int delimLength = delimiter.Length;
      while (true) {
        int index2 = str.IndexOf(delimiter, index, StringComparison.Ordinal);
        if (index2 < 0) {
          if (first) {
            var strret = new string[1];
            strret[0] = str;
            return strret;
          }
          strings = strings ?? (new List<string>());
          strings.Add(str.Substring(index));
          break;
        } else {
          first = false;
          string newstr = str.Substring(index, index2 - index);
          strings = strings ?? (new List<string>());
          strings.Add(newstr);
          index = index2 + delimLength;
        }
      }
      return (string[])strings.ToArray();
    }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.util.StringUtility.SplitAtSpTabCrLf(System.String)"]/*'/>
  public static string[] SplitAtSpTabCrLf(string s) {
    if (s == null || s.Length == 0) {
 return ValueEmptyStringArray;
}
    var index = 0;
    int valueSLength = s.Length;
    while (index < valueSLength) {
      char c = s[index];
      if (c != 0x09 && c != 0x0a && c != 0x0d && c != 0x20) {
        break;
      }
      ++index;
    }
    if (index == s.Length) {
 return ValueEmptyStringArray;
}
    List<string> strings = null;
    int lastIndex = index;
    while (index < valueSLength) {
      char c = s[index];
      if (c == 0x09 || c == 0x0a || c == 0x0d || c == 0x20) {
        if (lastIndex >= 0) {
          strings = strings ?? (new List<string>());
          strings.Add(s.Substring(lastIndex, (index)-lastIndex));
          lastIndex = -1;
        }
      } else {
        if (lastIndex < 0) {
          lastIndex = index;
        }
      }
      ++index;
    }
    if (lastIndex >= 0) {
      if (strings == null) {
 return new string[] { s.Substring(lastIndex, (index)-lastIndex)};
}
      strings.Add(s.Substring(lastIndex, (index)-lastIndex));
    }
    return strings.ToArray();
  }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.util.StringUtility.SplitAtSpTabCrLfFf(System.String)"]/*'/>
  public static string[] SplitAtSpTabCrLfFf(string s) {
    if (s == null || s.Length == 0) {
 return ValueEmptyStringArray;
}
    var index = 0;
    int valueSLength = s.Length;
    while (index < valueSLength) {
      char c = s[index];
      if (c != 0x09 && c != 0x0a && c != 0x0c && c != 0x0d && c != 0x20) {
        break;
      }
      ++index;
    }
    if (index == s.Length) {
 return ValueEmptyStringArray;
}
    List<string> strings = null;
    int lastIndex = index;
    while (index < valueSLength) {
      char c = s[index];
      if (c == 0x09 || c == 0x0a || c == 0x0c || c == 0x0d || c == 0x20) {
        if (lastIndex >= 0) {
          strings = strings ?? (new List<string>());
          strings.Add(s.Substring(lastIndex, (index)-lastIndex));
          lastIndex = -1;
        }
      } else {
        if (lastIndex < 0) {
          lastIndex = index;
        }
      }
      ++index;
    }
    if (lastIndex >= 0) {
      if (strings == null) {
 return new string[] { s.Substring(lastIndex, (index)-lastIndex)};
}
      strings.Add(s.Substring(lastIndex, (index)-lastIndex));
    }
    return strings.ToArray();
  }
}
}
