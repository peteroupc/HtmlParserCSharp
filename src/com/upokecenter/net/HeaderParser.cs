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
namespace com.upokecenter.net {
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using com.upokecenter.util;

    /// <summary>* Contains methods useful for parsing header fields.
    /// @author Peter.</summary>
public sealed class HeaderParser {
  internal enum QuotedStringRule {
    Http,
    Rfc5322,
    Smtp  // RFC5321
  }
  private static string[] emptyStringArray = new string[0];

  private static int getPositiveNumber(string v, int index) {
    int length = v.Length;
    char c=(char)0;
    var haveNumber = false;
    int startIndex = index;
    string number = null;
    while (index<length) {  // skip whitespace
      c = v[index];
      if (c<'0' || c>'9') {
        if (!haveNumber) {
 return -1;
}
        try {
          number = v.Substring(startIndex, (index)-(startIndex));
          return
  Int32.Parse(number, NumberStyles.AllowLeadingSign,
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
      number = v.Substring(startIndex, (length)-(startIndex));
      return
  Int32.Parse(number, NumberStyles.AllowLeadingSign,
    CultureInfo.InvariantCulture);
    } catch (FormatException) {
      return Int32.MaxValue;
    }
  }

  internal static int getResponseCode(string s) {
    var index = 0;
    int length = s.Length;
    if (s.IndexOf("HTTP/",index,StringComparison.Ordinal)!=index) {
 return -1;
}
    index+=5;
    index = skipZeros(s, index);
    if (index>= length || s[index]!='1') {
 return -1;
}
    ++index;
    if (index>= length || s[index]!='.') {
 return -1;
}
    ++index;
    index = skipZeros(s, index);
    if (index<length && s[index]=='1') {
      ++index;
    }
    if (index>= length || s[index]!=' ') {
 return -1;
}
    ++index;
    if (index + 3 >= length) {
 return -1;
}
    if (skipDigits(s,index)!=index+3 || s[index+3]!=' ') {
 return -1;
}
    int num = getPositiveNumber(s, index);
    return num;
  }

  private static int parse2Digit(string v, int index) {
    var value = 0;
    char c=(char)0;
    if (index<v.Length && (c=v[index])>= '0' && c<= '9') {
      value+=10*(c-'0'); index++;
    } else {
 return -1;
}
    if (index<v.Length && (c=v[index])>= '0' && c<= '9') {
      value+=(c-'0'); index++;
    } else {
 return -1;
}
    return value;
  }

  private static int parse4Digit(string v, int index) {
    var value = 0;
    char c=(char)0;
    if (index<v.Length && (c=v[index])>= '0' && c<= '9') {
      value+=1000*(c-'0'); index++;
    } else {
 return -1;
}
    if (index<v.Length && (c=v[index])>= '0' && c<= '9') {
      value+=100*(c-'0'); index++;
    } else {
 return -1;
}
    if (index<v.Length && (c=v[index])>= '0' && c<= '9') {
      value+=10*(c-'0'); index++;
    } else {
 return -1;
}
    if (index<v.Length && (c=v[index])>= '0' && c<= '9') {
      value+=(c-'0'); index++;
    } else {
 return -1;
}
    return value;
  }

    /// <summary>Parses a date _string in one of the three formats allowed
    /// by RFC2616 (HTTP/1.1). @param v a _string to parse. @param
    /// defaultValue a value to return if the _string isn't a valid date.
    /// @return number of milliseconds since midnight, January 1,
    /// 1970.</summary>
    /// <param name='v'>The parameter <paramref name='v'/> is not
    /// documented yet.</param>
    /// <param name='defaultValue'>The parameter <paramref
    /// name='defaultValue'/> is not documented yet.</param>
    /// <returns>A 64-bit signed integer.</returns>
  public static long parseHttpDate(string v, long defaultValue) {
    if (v == null) {
 return defaultValue;
}
    var index = 0;
    var rfc850 = false;
    var asctime = false;
    if (v.StartsWith("Mon" ,StringComparison.Ordinal) ||v.StartsWith("Sun",
  StringComparison.Ordinal) ||v.StartsWith("Fri",
 StringComparison.Ordinal)) {
      if (com.upokecenter.util.StringUtility.startsWith(v,"day,",3)) {
        rfc850 = true;
        index = 8;
      } else {
        index = 3;
      }
    } else if (v.StartsWith("Tue",StringComparison.Ordinal)) {
      if (com.upokecenter.util.StringUtility.startsWith(v,"sday,",3)) {
        rfc850 = true;
        index = 9;
      } else {
        index = 3;
      }
    } else if (v.StartsWith("Wed",StringComparison.Ordinal)) {
      if (com.upokecenter.util.StringUtility.startsWith(v,"nesday,",3)) {
        rfc850 = true;
        index = 11;
      } else {
        index = 3;
      }
    } else if (v.StartsWith("Thu",StringComparison.Ordinal)) {
      if (com.upokecenter.util.StringUtility.startsWith(v,"rsday,",3)) {
        rfc850 = true;
        index = 10;
      } else {
        index = 3;
      }
    } else if (v.StartsWith("Sat",StringComparison.Ordinal)) {
      if (com.upokecenter.util.StringUtility.startsWith(v,"urday,",3)) {
        rfc850 = true;
        index = 11;
      } else {
        index = 3;
      }
    } else {
 return defaultValue;
}
    int length = v.Length;
    int month = 0, day = 0, year = 0;
    int hour = 0, minute = 0, second = 0;
    if (rfc850) {
      day = parse2Digit(v, index);
      if (day< 0) {
 return defaultValue;
}
      index+=2;
      if (index<length && v[index]!='-') {
 return defaultValue;
}
      ++index;
      month = parseMonth(v, index);
      if (month< 0) {
 return defaultValue;
}
      index+=3;
      if (index<length && v[index]!='-') {
 return defaultValue;
}
      ++index;
      year = parse2Digit(v, index);
      if (day< 0) {
 return defaultValue;
}
      index+=2;
      if (index<length && v[index]!=' ') {
 return defaultValue;
}
      ++index;
      year = DateTimeUtility.convertYear(year);
    } else if (com.upokecenter.util.StringUtility.startsWith(v,",",index)) {
      index+=2;
      day = parse2Digit(v, index);
      if (day< 0) {
 return defaultValue;
}
      index+=2;
      if (index<length && v[index]!=' ') {
 return defaultValue;
}
      ++index;
      month = parseMonth(v, index);
      index+=3;
      if (month< 0) {
 return defaultValue;
}
      if (index<length && v[index]!=' ') {
 return defaultValue;
}
      ++index;
      year = parse4Digit(v, index);
      if (day< 0) {
 return defaultValue;
}
      index+=4;
      if (index<length && v[index]!=' ') {
 return defaultValue;
}
      ++index;
    } else if (com.upokecenter.util.StringUtility.startsWith(v," ",index)) {
      index+=1;
      asctime = true;
      month = parseMonth(v, index);
      if (month< 0) {
 return defaultValue;
}
      index+=3;
      if (index<length && v[index]!=' ') {
 return defaultValue;
}
      ++index;
      day = parsePadded2Digit(v, index);
      if (day< 0) {
 return defaultValue;
}
      index+=2;
      if (index<length && v[index]!=' ') {
 return defaultValue;
}
      ++index;
    } else {
 return defaultValue;
}
    hour = parse2Digit(v, index);
    if (hour< 0) {
 return defaultValue;
}
    index+=2;
    if (index<length && v[index]!=':') {
 return defaultValue;
}
    ++index;
    minute = parse2Digit(v, index);
    if (minute< 0) {
 return defaultValue;
}
    index+=2;
    if (index<length && v[index]!=':') {
 return defaultValue;
}
    ++index;
    second = parse2Digit(v, index);
    if (second< 0) {
 return defaultValue;
}
    index+=2;
    if (index<length && v[index]!=' ') {
 return defaultValue;
}
    ++index;
    if (asctime) {
      year = parse4Digit(v, index);
      if (day< 0) {
 return defaultValue;
}
      index+=4;
    } else {
      if (!com.upokecenter.util.StringUtility.startsWith(v,"GMT",index)) {
 return defaultValue;
}
      index+=3;
    }
    if (index != length) {
 return defaultValue;
}
    // NOTE: Here, the month is one-based
    return DateTimeUtility.toGmtDate(year, month, day, hour, minute, second);
  }
  private static int parseMonth(string v, int index) {
    if (com.upokecenter.util.StringUtility.startsWith(v,"Jan",index)) {
 return 1;
}
    if (com.upokecenter.util.StringUtility.startsWith(v,"Feb",index)) {
 return 2;
}
    if (com.upokecenter.util.StringUtility.startsWith(v,"Mar",index)) {
 return 3;
}
    if (com.upokecenter.util.StringUtility.startsWith(v,"Apr",index)) {
 return 4;
}
    if (com.upokecenter.util.StringUtility.startsWith(v,"May",index)) {
 return 5;
}
    if (com.upokecenter.util.StringUtility.startsWith(v,"Jun",index)) {
 return 6;
}
    if (com.upokecenter.util.StringUtility.startsWith(v,"Jul",index)) {
 return 7;
}
    if (com.upokecenter.util.StringUtility.startsWith(v,"Aug",index)) {
 return 8;
}
    if (com.upokecenter.util.StringUtility.startsWith(v,"Sep",index)) {
 return 9;
}
    if (com.upokecenter.util.StringUtility.startsWith(v,"Oct",index)) {
 return 10;
}
    if (com.upokecenter.util.StringUtility.startsWith(v,"Nov",index)) {
 return 11;
}
    return (com.upokecenter.util.StringUtility.startsWith(v,"Dec",index)) ?
      (12) : (-1);
  }
  private static int parsePadded2Digit(string v, int index) {
    var value = 0;
    char c=(char)0;
    if (index<v.Length && v[index]==' ') {
      value = 0; index++;
    } else if (index<v.Length && (c=v[index])>= '0' && c<= '9') {
      value+=10*(c-'0'); index++;
    } else {
 return -1;
}
    if (index<v.Length && (c=v[index])>= '0' && c<= '9') {
      value+=(c-'0'); index++;
    } else {
 return -1;
}
    return value;
  }
  internal static int parseToken(string str, int index, string token, bool
    optionalQuoted) {
    int length = str.Length;
    var j = 0;
    int startIndex = index;
    for (int i = index;i<length && j<token.Length;i++, j++) {
      char c = str[i];
      char cj = token[j];
      if (c!=cj && c!=(cj>= 'a' && cj<= 'z' ? cj-0x20 : cj)) {
 return startIndex;
}
    }
    index+=token.Length;
    index = skipLws(str, index, length, null);
    if (optionalQuoted) {
      if (index<length && str[index]=='=') {
        ++index;
        index = skipLws(str, index, length, null);
        if (index<length && str[index]=='"') {
            return index;
        } else {
 return startIndex;
}
        index = skipLws(str, index, length, null);
      }
    }
    if (index >= length) {
 return index;
}
    if (str[index]==',') {
      ++index;
      index = skipLws(str, index, length, null);
      return index;
    }
    return startIndex;
  }
  internal static int parseTokenWithDelta(string str, int index, string
    token, int[] result) {
    int length = str.Length;
    var j = 0;
    int startIndex = index;
    result[0]=-1;
    for (int i = index;i<length && j<token.Length;i++, j++) {
      char c = str[i];
      char cj = token[j];
      if (c!=cj && c!=(cj>= 'a' && cj<= 'z' ? cj-0x20 : cj)) {
 return startIndex;
}
    }
    index+=token.Length;
    index = skipLws(str, index, length, null);
    if (index<length && str[index]=='=') {
      ++index;
      index = skipLws(str, index, length, null);
      int number = getPositiveNumber(str, index);
      while (index<length) {
        char c = str[index];
        if (c<'0' || c>'9') {
          break;
        }
        ++index;
      }
      result[0]=number;
      if (number<-1) {
 return startIndex;
}
      index = skipLws(str, index, length, null);
    } else {
 return startIndex;
}
    if (index >= length) {
 return index;
}
    if (str[index]==',') {
      ++index;
      index = skipLws(str, index, length, null);
      return index;
    }
    return startIndex;
  }

    private static int skipDigits(string v, int index) {
    char c=(char)0;
    int length = v.Length;
    while (index<length) {
      c = v[index];
      if (c<'0' || c>'9') {
 return index;
}
      ++index;
    }
    return index;
  }

  internal static int skipDirective(string str, int io) {
    int length = str.Length;
    char c=(char)0;
    while (io<length) {  // skip non-separator
      c = str[io];
      if (c=='=' || c==',' || c==127 || c< 32) {
        break;
      }
      ++io;
    }
    io = skipLws(str, io, length, null);
    if (io<length && str[io]=='=') {
      ++io;
      io = skipLws(str, io, length, null);
      if (io<length && str[io]=='"') {
        io = skipQuotedString(str, io, length, null, QuotedStringRule.Http);
      } else {
        while (io<length) {  // skip non-separator
          c = str[io];
          if (c==',' || c==127 || c< 32) {
            break;
          }
          ++io;
        }
      }
      io = skipLws(str, io, length, null);
    }
    if (io<length && str[io]==',') {
      ++io;
      io = skipLws(str, io, length, null);
    } else {
      io = length;
    }
    return io;
  }

  private static int skipLanguageTag(string str, int index, int endIndex) {
    if (index == endIndex || str == null) {
 return index;
}
    char c = str[index];
    if (!((c>= 'A' && c<= 'Z') || (c>= 'a' && c<= 'z'))) {
 return index;  // not a valid language tag
}
    ++index;
    while (index<endIndex) {
      c = str[index];
   if (!((c>= 'A' && c<= 'Z') || (c>= 'a' && c<= 'z') || (c>= '0' && c<=
        '9') || c=='-')) {
        break;
      }
      ++index;
    }
    return index;
  }

    internal static int skipLws(string s, int index, int endIndex,
    StringBuilder builder) {
    int ret;
    // While HTTP usually only allows CRLF, it also allows
    // us to be tolerant here
    int i2 = skipNewLine(s, index, endIndex);
    ret = skipWsp(s, i2, endIndex);
    if (ret != i2) {
      if (builder != null) {
        // Note that folding LWS into a space is
        // currently optional under HTTP/1.1 sec. 2.2
        builder.Append(' ');
      }
      return ret;
    }
    return index;
  }
  private static int skipNewLine(string s, int index, int endIndex) {
    if (index + 1<endIndex && s[index]==0x0d && s[index + 1]==0x0a) {
 return index + 2;
  } else if (index<endIndex && (s[index]==0x0d || s[index]==0x0a)) {
 return index + 1;
} else {
 return index;
}
  }

    // qtext (RFC5322 sec. 3.2.1)
  internal static int skipQtext(string s, int index, int endIndex) {
    if (index<endIndex) {
      char c = s[index];
      if (c>= 33 && c<= 126 && c!='\\' && c!='"') {
 return index + 1;
}
      // obs-ctext
      if ((c<0x20 && c != 0x00 && c != 0x09 && c != 0x0a && c != 0x0d) || c
        == 0x7f) {
 return index + 1;
}
    }
    return index;
  }

  // skip space and tab characters
  private static int skipWsp(string s, int index, int endIndex) {
    while (index<endIndex) {
      char c = s[index];
      if (c != 0x20 && c != 0x09) {
 return index;
}
      ++index;
    }
    return index;
  }
  private static int skipZeros(string v, int index) {
    char c=(char)0;
    int length = v.Length;
    while (index<length) {
      c = v[index];
      if (c!='0') {
 return index;
}
      ++index;
    }
    return index;
  }

  internal static int toHexNumber(int c) {
    if (c>= 'A' && c<= 'Z') {
 return 10+c-'A';
  } else if (c>= 'a' && c<= 'z') {
 return 10+c-'a';
} else {
 return (c>= '0' && c<= '9') ? (c-'0') : (-1);
}
  }

  private HeaderParser() {}
}
}
