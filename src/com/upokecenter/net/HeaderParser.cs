using System;
using System.Globalization;
using System.Text;
using Com.Upokecenter.util;

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
namespace Com.Upokecenter.Net {
    /// <summary>Contains methods useful for parsing header
    /// fields.</summary>
  public sealed class HeaderParser {
    private static string[] emptyStringArray = new string[0];

    private static int GetPositiveNumber(string v, int index) {
      int length = v.Length;
      var c = (char)0;
      var haveNumber = false;
      int startIndex = index;
      string number = null;
      while (index < length) { // skip whitespace
        c = v[index];
        if (c < '0' || c > '9') {
          if (!haveNumber) {
            return -1;
          }
          try {
            number = v.Substring(startIndex, index - startIndex);
            return
    Int32.Parse(
      number,
      NumberStyles.AllowLeadingSign,
      CultureInfo.InvariantCulture);
          } catch (FormatException) {
            return Int32.MaxValue;
          }
        } else {
          haveNumber = true;
        }
        ++index;
      }
      try {
        number = v.Substring(startIndex, length - startIndex);
        return
    Int32.Parse(
      number,
      NumberStyles.AllowLeadingSign,
      CultureInfo.InvariantCulture);
      } catch (FormatException) {
        return Int32.MaxValue;
      }
    }

    internal static int GetResponseCode(string str) {
      var index = 0;
      int endIndex = str.Length;
      int indexStart = index;
      if (endIndex - index > 12 && (str[index] == 72) && (str[index + 1] ==
        84) && (str[index + 2] == 84) && (str[index + 3] == 80) &&
        (str[index + 4] == 47) && (str[index + 5] >= 48 && str[index + 5] <=
        57) && (str[index + 6] == 46) && (str[index + 7] >= 48 && str[index +
        7] <= 57) && (str[index + 8] == 32) && ((str[index + 9] >= 48 &&
        str[index + 9] <= 57) && (str[index + 10] >= 48 && str[index + 10]
        <= 57) && (str[index + 11] >= 48 && str[index + 11] <= 57)) &&
        (str[index + 12] == 32)) {
        var c1 = (int)(str[index + 9] - '0');
        var c2 = (int)(str[index + 10] - '0');
        var c3 = (int)(str[index + 11] - '0');
        int code = (c1 * 100) + (c2 * 10) + c3;
      }
      return -1;
    }

    private static int SkipOws(byte[] bytes, int index, int endIndex) {
   while (index < endIndex && (bytes[index] == 0x09 || bytes[index] ==
        0x20)) {
     ++index;
   }
   return index;
    }

    private static int SkipObsFold(byte[] bytes, int index, int endIndex) {
      if (endIndex - index > 1 && (bytes[index] == 0x0d && bytes[index + 1]
        == 0x0a)) {
        int si = index;
        var found = false;
        index += 2;
        while (index < endIndex && (bytes[index] == 0x20 || bytes[index] ==
          0x09)) {
          found = true;
          ++index;
        }
        return found ? index : si;
      }
      return index;
    }

    private static int SkipOwsCommaOws(byte[] bytes, int index, int endIndex) {
      int s = index;
      s = SkipOws(bytes, s, endIndex);
      if (s >= endIndex || bytes[s] != ',') {
{ return index;
} }
      return SkipOws(bytes, s, endIndex);
    }

    private static int[] valueIllegalHttpTokenChars = {
      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
      1, 0, 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0, 0, 1,
      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1,
      1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0,
      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 1,
    };

    private static bool IsTokenText(byte b) {
      return (b & 0x7f) != 0 || valueIllegalHttpTokenChars[b] == 0;
    }

    private static bool IsQdText(byte b) {
      return b != '"' && b != '\\' && (
         (b & 0x7f) != 0 || b == 0x09 || (b >= 0x20 && b <= 0x7e));
    }

    private static bool IsQpText(byte b) {
      return (b & 0x7f) != 0 || b == 0x09 || (b >= 0x20 && b <= 0x7e);
    }

private static int ParseCacheControl(
    byte[] bytes,
    int index,
    int endIndex) {
      var emptyItems = 0;
      int startIndex = index;
      // NOTE: Assumes lines were already unfolded (each obs-fold is
      // converted to one or more SP)
      index = SkipOws(bytes, index, endIndex);
      while (index < endIndex) {
        if (bytes[index] == ',') {
          ++emptyItems;
          if (emptyItems > 2) {
            return startIndex;
          }
          ++index;
          index = SkipOws(bytes, index, endIndex);
        } else {
          emptyItems = 0;
          int si = index;
          while (index < endIndex &&
                 (bytes[index] != '=' && bytes[index] != ',' &&
                  (((int)bytes[index]) & 0xff) > 0x20)) {
            if (!IsTokenText(bytes[index])) {
              return startIndex;
            }
            ++index;
          }
          if (si == index) {
            return startIndex;
          }
          if (index < endIndex && bytes[index] == '=') {
            ++index;
            if (index < endIndex && bytes[index] == '"') {
              ++index;
              while (index < endIndex && (bytes[index] != '"')) {
                if (bytes[index] == '\\' && index + 1 < endIndex) {
                  if (!IsQpText(bytes[index])) {
                    return startIndex;
                  }
                  index += 2;
                } else {
                  if (!IsQdText(bytes[index])) {
                    return startIndex;
                  }
                  ++index;
                }
              }
              if (index == endIndex) {
                return startIndex;
              }
              ++index;
            } else {
              si = index;
              while (index < endIndex && (bytes[index] != ',' &&
                    (((int)bytes[index]) & 0xff) > 0x20)) {
                if (!IsTokenText(bytes[index])) {
                  return startIndex;
                }
                ++index;
              }
              if (si == index) {
                return startIndex;
              }
            }
          }
          if (index == endIndex) {
            return endIndex;
          }
          si = SkipOwsCommaOws(bytes, index, endIndex);
          if (si != index) {
            return startIndex;
          }
        }
      }
      return (emptyItems >= 2) ? startIndex : endIndex;
    }
  }
}
