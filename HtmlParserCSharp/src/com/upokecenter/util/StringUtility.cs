/*
Written in 2013 by Peter Occil.
Any copyright to this work is released to the Public Domain.
In case this is not possible, this work is also
licensed under the Unlicense: https://unlicense.org/

*/
using System;
using System.Collections.Generic;
using System.Text;

namespace Com.Upokecenter.Util {
  /// <summary>Contains auxiliary methods for working with
  /// strings.</summary>
  public static class StringUtility {
    private static readonly string[] ValueEmptyStringArray = new string[0];

    /// <summary>Not documented yet.</summary>
    /// <param name='s'>The parameter <paramref name='s'/> is a text
    /// string.</param>
    /// <returns>Either <c>true</c> or <c>false</c>.</returns>
    public static bool IsNullOrSpaces(string s) {
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

    /// <summary>Splits a string by a delimiter. If the string ends with
    /// the delimiter, the result will end with an empty string. If the
    /// string begins with the delimiter, the result will start with an
    /// empty string. If the delimiter is null or empty, exception. @param
    /// s a string to split.</summary>
    /// <param name='str'>The parameter <paramref name='str'/> is a text
    /// string.</param>
    /// <param name='delimiter'>A string to signal where each substring
    /// begins and ends.</param>
    /// <returns>An array containing strings that are split by the
    /// delimiter. If s is null or empty, returns an array whose sole
    /// element is the empty string.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='delimiter'/> is null.</exception>
    /// <exception cref='ArgumentException'>Delimiter is empty.</exception>
    public static string[] SplitAt(string str, string delimiter) {
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
          strings = strings ?? new List<string>();
          strings.Add(str.Substring(index));
          break;
        } else {
          first = false;
          string newstr = str.Substring(index, index2 - index);
          strings = strings ?? new List<string>();
          strings.Add(newstr);
          index = index2 + delimLength;
        }
      }
      return (string[])strings.ToArray();
    }

    /// <summary>Splits a string separated by space characters other than
    /// form feed. This method acts as though it strips leading and
    /// trailing space characters from the string before splitting it. The
    /// space characters used here are U+0009, U+000A, U+000D, and
    /// U+0020.</summary>
    /// <param name='s'>A string. Can be null.</param>
    /// <returns>An array of all items separated by spaces. If string is
    /// null or empty, returns an empty array.</returns>
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
            strings = strings ?? new List<string>();
            strings.Add(s.Substring(lastIndex, index - lastIndex));
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
          return new string[] { s.Substring(lastIndex, index - lastIndex) };
        }
        strings.Add(s.Substring(lastIndex, index - lastIndex));
      }
      return strings.ToArray();
    }

    /// <summary>Splits a string separated by space characters. This method
    /// acts as though it strips leading and trailing space characters from
    /// the string before splitting it. The space characters are U+0009,
    /// U+000A, U+000C, U+000D, and U+0020.</summary>
    /// <param name='s'>A string. Can be null.</param>
    /// <returns>An array of all items separated by spaces. If string is
    /// null or empty, returns an empty array.</returns>
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
            strings = strings ?? new List<string>();
            strings.Add(s.Substring(lastIndex, index - lastIndex));
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
          return new string[] { s.Substring(lastIndex, index - lastIndex) };
        }
        strings.Add(s.Substring(lastIndex, index - lastIndex));
      }
      return strings.ToArray();
    }
  }
}
