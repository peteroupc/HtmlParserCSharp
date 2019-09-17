/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PeterO;
using PeterO.Text;

namespace Com.Upokecenter.Util {
    /// <summary>A URL object under the WHATWG's URL specification. See
    /// http://url.spec.whatwg.org/.</summary>
  public sealed class URL {
    private enum ParseState {
      SchemeStart,
      Scheme,
      SchemeData,
      NoScheme,
      RelativeOrAuthority,
      Relative,
      RelativeSlash,
      AuthorityFirstSlash,
      AuthoritySecondSlash,
      AuthorityIgnoreSlashes,
      Authority, Query, Fragment, Host, FileHost,
      RelativePathStart, RelativePath, HostName, Port,
    }

    private static string hex = "0123456789ABCDEF";
    // encodingerror uses ? as repl. ch. when encoding
    // querySerializerError uses decimal HTML entity of bad c.p. when encoding
    private static void AppendOutputBytes(
      StringBuilder builder,
      byte[] bytes) {
      for (int i = 0; i < bytes.Length; ++i) {
        int c = bytes[i] & 0xff;
        if (c == 0x20) {
          builder.Append((char)0x2b);
        } else if (c == 0x2a || c == 0x2d || c == 0x2e ||
            (c >= 0x30 && c <= 0x39) || (c >= 0x41 && c <= 0x5a) ||
            (c >= 0x5f) || (c >= 0x61 && c <= 0x7a)) {
          builder.Append((char)c);
        } else {
          builder.Append('%');
          builder.Append(hex[(c >> 4) & 0x0f]);
          builder.Append(hex[c & 0x0f]);
        }
      }
    }

    private static string HostParse(string stringValue) {
      if (stringValue.Length > 0 && stringValue[0] == '[') {
        if (stringValue[stringValue.Length - 1] != ']') {
          var ipv6 = new int[8];
          var piecePointer = 0;
          var index = 1;
          var compress = -1;
          int ending = stringValue.Length - 1;
          int c = (index >= ending) ? -1 : stringValue[index];
          if (c == ':') {
            if (index + 1 >= ending || stringValue[index + 1] != ':') {
              return null;
            }
            index += 2;
            ++piecePointer;
            compress = piecePointer;
          }
          while (index < ending) {
            if (piecePointer >= 8) {
              return null;
            }
            c = stringValue[index];
            if ((c & 0xfc00) == 0xd800 && index + 1 < ending &&
                (stringValue[index + 1] & 0xfc00) == 0xdc00) {
              // Get the Unicode code point for the surrogate pair
            c = 0x10000 + ((c & 0x3ff) << 10) + (stringValue[index + 1] &
0x3ff);
++index;
            } else if ((c & 0xf800) == 0xd800) {
              // illegal surrogate
              throw new ArgumentException();
            }
            ++index;
            if (c == ':') {
              if (compress >= 0) {
                return null;
              }
              ++piecePointer;
              compress = piecePointer;
              continue;
            }
            var value = 0;
            var length = 0;
            while (length < 4) {
              if (c >= 'A' && c <= 'F') {
                value = value * 16 + (c - 'A') + 10;
                ++index;
                ++length;
                c = (index >= ending) ? -1 : stringValue[index];
              } else if (c >= 'a' && c <= 'f') {
                value = value * 16 + (c - 'a') + 10;
                ++index;
                ++length;
                c = (index >= ending) ? -1 : stringValue[index];
              } else if (c >= '0' && c <= '9') {
                value = value * 16 + (c - '0');
                ++index;
                ++length;
                c = (index >= ending) ? -1 : stringValue[index];
              } else {
                break;
              }
            }
            if (c == '.') {
              if (length == 0) {
                return null;
              }
              index -= length;
              break;
            } else if (c == ':') {
              ++index;
              c = (index >= ending) ? -1 : stringValue[index];
              if (c < 0) {
                return null;
              }
            } else if (c >= 0) {
              return null;
            }
            ipv6[piecePointer] = value;
            ++piecePointer;
          }
          // IPv4
          if (c >= 0) {
            if (piecePointer > 6) {
              return null;
            }
            var dotsSeen = 0;
            while (index < ending) {
              var value = 0;
              while (c >= '0' && c <= '9') {
                value = value * 10 + (c - '0');
                if (value > 255) {
                  return null;
                }
                ++index;
                c = (index >= ending) ? -1 : stringValue[index];
              }
              if (dotsSeen < 3 && c != '.') {
                return null;
              } else if (dotsSeen == 3 && c == '.') {
                return null;
              }
              ipv6[piecePointer] = (ipv6[piecePointer] * 256) + value;
              if (dotsSeen == 0 || dotsSeen == 2) {
                ++piecePointer;
              }
              ++dotsSeen;
            }
          }
          if (compress >= 0) {
            int swaps = piecePointer - compress;
            piecePointer = 7;
            while (piecePointer != 0 && swaps != 0) {
              int ptr = compress - swaps + 1;
              int tmp = ipv6[piecePointer];
              ipv6[piecePointer] = ipv6[ptr];
              ipv6[ptr] = tmp;
              --piecePointer;
              --swaps;
            }
          } else if (compress < 0 && piecePointer != 8) {
            return null;
          }
        }
      }
      try {
        // DebugUtility.Log("was: %s",stringValue);
        stringValue = PercentDecode(stringValue, "utf-8");
        // DebugUtility.Log("now: %s",stringValue);
      } catch (IOException) {
        return null;
      }
      return stringValue;
    }

    private static string HostSerialize(string stringValue) {
      return (stringValue == null) ? String.Empty : stringValue;
    }

    private static bool IsHexDigit(int c) {
      return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' &&
        c <= '9');
    }

    private static bool IsUrlCodePoint(int c) {
      if (c <= 0x20) {
        return false;
      }
      if (c < 0x80) {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >=
          '0' && c <= '9') || ((c & 0x7F) == c && "!$&'()*+,-./:;=?@_~"
          .IndexOf((char)c) >= 0);
        } else if ((c & 0xfffe) == 0xfffe) {
          return false;
        } else return ((c >= 0xa0 && c <= 0xd7ff) || (c >= 0xe000 && c <=
0xfdcf)
        ||
    (c >= 0xfdf0 && c <= 0xffef) || (c >= 0x10000 && c <= 0x10fffd)) ?
         true : false;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='s'>The parameter <paramref name='s'/> is a text
    /// string.</param>
    /// <returns>An URL object.</returns>
    public static URL Parse(string s) {
      return Parse(s, null, null, false);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='s'>The parameter <paramref name='s'/> is a text
    /// string.</param>
    /// <param name='baseurl'>The parameter <paramref name='baseurl'/> is
    /// a.Upokecenter.Util.URL object.</param>
    /// <returns>An URL object.</returns>
    public static URL Parse(string s, URL baseurl) {
      return Parse(s, baseurl, null, false);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='s'>The parameter <paramref name='s'/> is a text
    /// string.</param>
    /// <param name='baseurl'>The parameter <paramref name='baseurl'/> is
    /// a.Upokecenter.Util.URL object.</param>
    /// <param name='encoding'>The parameter <paramref name='encoding'/> is
    /// a text string.</param>
    /// <returns>An URL object.</returns>
    public static URL Parse(string s, URL baseurl, string encoding) {
      return Parse(s, baseurl, encoding, false);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='s'>The parameter <paramref name='s'/> is a text
    /// string.</param>
    /// <param name='baseurl'>The parameter <paramref name='baseurl'/> is
    /// a.Upokecenter.Util.URL object.</param>
    /// <param name='encoding'>The parameter <paramref name='encoding'/> is
    /// a text string.</param>
    /// <param name='strict'>The parameter <paramref name='strict'/> is
    /// either <c>true</c> or <c>false</c>.</param>
    /// <returns>An URL object.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='baseurl'/> is null.</exception>
    public static URL Parse(
      string s,
      URL baseurl,
      string encoding,
      bool strict) {
      if (s == null) {
        throw new ArgumentException();
      }
      var beginning = 0;
      int ending = s.Length - 1;
      var relative = false;
      var url = new URL();
      ICharacterEncoder encoder = null;
      ParseState state = ParseState.SchemeStart;
      if (encoding != null) {
        encoder = Encodings.GetEncoding(encoding).GetEncoder();
      }
      if (s.IndexOf("http://", StringComparison.Ordinal) == 0) {
        state = ParseState.AuthorityIgnoreSlashes;
        url.scheme = "http";
        beginning = 7;
        relative = true;
      } else {
        while (beginning < s.Length) {
          char c = s[beginning];
          if (c != 0x09 && c != 0x0a && c != 0x0c && c != 0x0d && c != 0x20) {
            break;
          }
          ++beginning;
        }
      }
      while (ending >= beginning) {
        char c = s[ending];
        if (c != 0x09 && c != 0x0a && c != 0x0c && c != 0x0d && c != 0x20) {
          ++ending;
          break;
        }
        --ending;
      }
      if (ending < beginning) {
        ending = beginning;
      }
      var atflag = false;
      var bracketflag = false;
      var buffer = new StringBuilder();
      StringBuilder query = null;
      StringBuilder fragment = null;
      StringBuilder password = null;
      StringBuilder username = null;
      StringBuilder schemeData = null;
      var error = false;
      IList<string> path = new List<string>();
      int index = beginning;
      var hostStart = -1;
      var portstate = 0;
      while (index <= ending) {
        int oldindex = index;
        var c = -1;
        if (index >= ending) {
          c = -1;
          ++index;
        } else {
          c = s[index];
          if ((c & 0xfc00) == 0xd800 && index + 1 < ending &&
              (s[index + 1] & 0xfc00) == 0xdc00) {
            // Get the Unicode code point for the surrogate pair
            c = 0x10000 + ((c & 0x3ff) << 10) + (s[index + 1] & 0x3ff);
            ++index;
          } else if ((c & 0xf800) == 0xd800) {
            // illegal surrogate
            throw new ArgumentException();
          }
          ++index;
        }
        switch (state) {
          case ParseState.SchemeStart:
            if (c >= 'A' && c <= 'Z') {
              if (c + 0x20 <= 0xffff) {
  { buffer.Append((char)(c + 0x20));
}
  } else if (c + 0x20 <= 0x10ffff) {
    buffer.Append((char)((((c + 0x20 - 0x10000) >> 10) & 0x3ff) | 0xd800));
    buffer.Append((char)(((c + 0x20 - 0x10000) & 0x3ff) | 0xdc00));
}
              state = ParseState.Scheme;
            } else if (c >= 'a' && c <= 'z') {
              if (c <= 0xffff) {
  { buffer.Append((char)c);
}
  } else if (c <= 0x10ffff) {
    buffer.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
    buffer.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
}
              state = ParseState.Scheme;
            } else {
              index = oldindex;
              state = ParseState.NoScheme;
            }
            break;
          case ParseState.Scheme:
            if (c >= 'A' && c <= 'Z') {
              if (c + 0x20 <= 0xffff) {
  { buffer.Append((char)(c + 0x20));
}
  } else if (c + 0x20 <= 0x10ffff) {
    buffer.Append((char)((((c + 0x20 - 0x10000) >> 10) & 0x3ff) | 0xd800));
    buffer.Append((char)(((c + 0x20 - 0x10000) & 0x3ff) | 0xdc00));
}
       } else if ((c >= 'a' && c <= 'z') || c == '.' || c == '-' || c ==
              '+') {
              if (c <= 0xffff) {
  { buffer.Append((char)c);
}
  } else if (c <= 0x10ffff) {
    buffer.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
    buffer.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
}
            } else if (c == ':') {
              url.scheme = buffer.ToString();
              buffer.Remove(0, buffer.Length);
              if (url.scheme.Equals("http", StringComparison.Ordinal) ||
url.scheme.Equals("https", StringComparison.Ordinal) ||
url.scheme.Equals("ftp", StringComparison.Ordinal) ||
url.scheme.Equals("gopher", StringComparison.Ordinal) ||
url.scheme.Equals("ws", StringComparison.Ordinal) ||
url.scheme.Equals("wss", StringComparison.Ordinal) ||
url.scheme.Equals("file", StringComparison.Ordinal)) {
                relative = true;
              }
              if (url.scheme.Equals("file", StringComparison.Ordinal)) {
                state = ParseState.Relative;
                relative = true;
              } else if (relative && baseurl != null &&
                    url.scheme.Equals(baseurl.scheme,
  StringComparison.Ordinal)) {
                state = ParseState.RelativeOrAuthority;
              } else if (relative) {
                state = ParseState.AuthorityFirstSlash;
              } else {
                schemeData = new StringBuilder();
                state = ParseState.SchemeData;
              }
            } else {
              buffer.Remove(0, buffer.Length);
              index = beginning;
              state = ParseState.NoScheme;
            }
            break;
          case ParseState.SchemeData:
            if (c == '?') {
              query = new StringBuilder();
              state = ParseState.Query;
              break;
            } else if (c == '#') {
              fragment = new StringBuilder();
              state = ParseState.Fragment;
              break;
            }
            if (c >= 0 && (!IsUrlCodePoint(c) && c != '%') || (c == '%' &&
              (index + 2 > ending || !IsHexDigit(s[index]) ||
              !IsHexDigit(s[index + 1])))) {
              error = true;
            }
            if (c >= 0 && c != 0x09 && c != 0x0a && c != 0x0d) {
              if (c < 0x20 || c == 0x7f) {
                PercentEncode(schemeData, c);
              } else if (c < 0x7f) {
                if (c <= 0xffff) {
  { schemeData.Append((char)c);
}
  } else if (c <= 0x10ffff) {
    schemeData.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
    schemeData.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
}
              } else {
                PercentEncodeUtf8(schemeData, c);
              }
            }
            break;
          case ParseState.NoScheme:
            if (baseurl == null) {
              return null;
            }
            // DebugUtility.Log("no scheme: [%s] [%s]",s,baseurl);
            if (!(baseurl.scheme.Equals("http", StringComparison.Ordinal) ||
baseurl.scheme.Equals(
  "https",
  StringComparison.Ordinal) || baseurl.scheme.Equals("ftp",
  StringComparison.Ordinal) || baseurl.scheme.Equals(
    "gopher",
    StringComparison.Ordinal) || baseurl.scheme.Equals("ws",
  StringComparison.Ordinal) || baseurl.scheme.Equals("wss",
  StringComparison.Ordinal) ||
                baseurl.scheme.Equals("file", StringComparison.Ordinal))) {
              return null;
            }
            state = ParseState.Relative;
            index = oldindex;
            break;
          case ParseState.RelativeOrAuthority:
            if (c == '/' && index < ending && s[index] == '/') {
              ++index;
              state = ParseState.AuthorityIgnoreSlashes;
            } else {
              error = true;
              state = ParseState.Relative;
              index = oldindex;
            }
            break;
          case ParseState.Relative: {
              relative = true;
              if (!"file".Equals(url.scheme, StringComparison.Ordinal)) {
                if (baseurl == null) {
                  throw new ArgumentNullException(nameof(baseurl));
                }
                url.scheme = baseurl.scheme;
              }
              if (c < 0) {
                url.host = baseurl.host;
                url.port = baseurl.port;
                path = PathList(baseurl.path);
                url.query = baseurl.query;
              } else if (c == '/' || c == '\\') {
                if (c == '\\') {
                  error = true;
                }
                state = ParseState.RelativeSlash;
              } else if (c == '?') {
                url.host = baseurl.host;
                url.port = baseurl.port;
                path = PathList(baseurl.path);
                query = new StringBuilder();
                state = ParseState.Query;
              } else if (c == '#') {
                url.host = baseurl.host;
                url.port = baseurl.port;
                path = PathList(baseurl.path);
                url.query = baseurl.query;
                fragment = new StringBuilder();
                state = ParseState.Fragment;
              } else {
                url.host = baseurl.host;
                url.port = baseurl.port;
                path = PathList(baseurl.path);
                if (path.Count > 0) { // Pop path
                  path.RemoveAt(path.Count - 1);
                }
                state = ParseState.RelativePath;
                index = oldindex;
              }
              break;
            }
          case ParseState.RelativeSlash:
            if (c == '/' || c == '\\') {
              if (c == '\\') {
                error = true;
              }
              state = "file".Equals(url.scheme, StringComparison.Ordinal) ?
ParseState.FileHost :
                ParseState.AuthorityIgnoreSlashes;
              } else {
              if (baseurl != null) {
                url.host = baseurl.host;
                url.port = baseurl.port;
              }
              state = ParseState.RelativePath;
              index = oldindex;
            }
            break;
          case ParseState.AuthorityFirstSlash:
            if (c == '/') {
              state = ParseState.AuthoritySecondSlash;
            } else {
              error = true;
              state = ParseState.AuthorityIgnoreSlashes;
              index = oldindex;
            }
            break;
          case ParseState.AuthoritySecondSlash:
            if (c == '/') {
              state = ParseState.AuthorityIgnoreSlashes;
            } else {
              error = true;
              state = ParseState.AuthorityIgnoreSlashes;
              index = oldindex;
            }
            break;
          case ParseState.AuthorityIgnoreSlashes:
            if (c != '/' && c != '\\') {
              username = new StringBuilder();
              index = oldindex;
              hostStart = index;
              state = ParseState.Authority;
            } else {
              error = true;
            }
            break;
          case ParseState.Authority:
            if (c == '@') {
              if (atflag) {
                StringBuilder result = (password == null) ? username : password;
                error = true;
                result.Append("%40");
              }
              atflag = true;
              string bstr = buffer.ToString();
              for (var i = 0; i < bstr.Length; ++i) {
                int cp = DataUtilities.CodePointAt(bstr, i);
                if (cp >= 0x10000) {
                  ++i;
                }
                if (cp == 0x9 || cp == 0xa || cp == 0xd) {
                  error = true;
                  continue;
                }
                if ((!IsUrlCodePoint(c) && c != '%') || (cp == '%' &&
                (i + 3 > bstr.Length || !IsHexDigit(bstr[index + 1]) ||
                    !IsHexDigit(bstr[index + 2])))) {
                  error = true;
                }
                if (cp == ':' && password == null) {
                  password = new StringBuilder();
                  continue;
                }
                StringBuilder result = (password == null) ? username : password;
                if (cp <= 0x20 || cp >= 0x7F || ((cp & 0x7F) == cp && "#<>?`\""
                    .IndexOf((char)cp) >= 0)) {
                  PercentEncodeUtf8(result, cp);
                } else {
                  if (cp <= 0xffff) {
  { result.Append((char)cp);
}
  } else if (cp <= 0x10ffff) {
    result.Append((char)((((cp - 0x10000) >> 10) & 0x3ff) | 0xd800));
    result.Append((char)(((cp - 0x10000) & 0x3ff) | 0xdc00));
}
                }
              }

              // DebugUtility.Log("username=%s",username);
              // DebugUtility.Log("password=%s",password);
              buffer.Remove(0, buffer.Length);
              hostStart = index;
            } else if (c < 0 || ((c & 0x7F) == c && "/\\?#".IndexOf((char)c) >=
              0)) {
              buffer.Remove(0, buffer.Length);
              state = ParseState.Host;
              index = hostStart;
            } else {
              if (c <= 0xffff) {
  { buffer.Append((char)c);
}
  } else if (c <= 0x10ffff) {
    buffer.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
    buffer.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
}
            }
            break;
          case ParseState.FileHost:
            if (c < 0 || ((c & 0x7F) == c && "/\\?#".IndexOf((char)c) >= 0)) {
              index = oldindex;
              if (buffer.Length == 2) {
                int c1 = buffer[0];
                int c2 = buffer[1];
                if (
                  (c2 == '|' || c2 == ':') && ((c1 >= 'A' && c1 <= 'Z') ||
(c1 >=
                'a' && c1 <= 'z'))) {
                  state = ParseState.RelativePath;
                  break;
                }
              }
              string host = HostParse(buffer.ToString());
              if (host == null) {
                throw new ArgumentException();
              }
              url.host = host;
              buffer.Remove(0, buffer.Length);
              state = ParseState.RelativePathStart;
            } else if (c == 0x09 || c == 0x0a || c == 0x0d) {
              error = true;
            } else {
              if (c <= 0xffff) {
  { buffer.Append((char)c);
}
  } else if (c <= 0x10ffff) {
    buffer.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
    buffer.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
}
            }
            break;
          case ParseState.Host:
          case ParseState.HostName:
            if (c == ':' && !bracketflag) {
              string host = HostParse(buffer.ToString());
              if (host == null) {
                return null;
              }
              url.host = host;
              buffer.Remove(0, buffer.Length);
              state = ParseState.Port;
            } else if (c < 0 || ((c & 0x7F) == c && "/\\?#".IndexOf((char)c) >=
              0)) {
              string host = HostParse(buffer.ToString());
              if (host == null) {
                return null;
              }
              url.host = host;
              buffer.Remove(0, buffer.Length);
              index = oldindex;
              state = ParseState.RelativePathStart;
            } else if (c == 0x09 || c == 0x0a || c == 0x0d) {
              error = true;
            } else {
              if (c == '[') {
                bracketflag = true;
              } else if (c == ']') {
                bracketflag = false;
              }
              if (c <= 0xffff) {
  { buffer.Append((char)c);
}
  } else if (c <= 0x10ffff) {
    buffer.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
    buffer.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
}
            }
            break;
          case ParseState.Port:
            if (c >= '0' && c <= '9') {
              if (c != '0') {
                portstate = 2; // first non-zero found
              } else if (portstate == 0) {
                portstate = 1; // have a port number
              }
              if (portstate == 2) {
                if (c <= 0xffff) {
  { buffer.Append((char)c);
}
  } else if (c <= 0x10ffff) {
    buffer.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
    buffer.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
}
              }
      } else if (c < 0 || ((c & 0x7F) == c && "/\\?#".IndexOf((char)c) >=
              0)) {
              string bufport = String.Empty;
              if (portstate == 1) {
                bufport = "0";
              } else if (portstate == 2) {
                bufport = buffer.ToString();
              }
              // DebugUtility.Log("port: [%s]",buffer.ToString());
              if ((url.scheme.Equals("http", StringComparison.Ordinal) ||
url.scheme.Equals("ws", StringComparison.Ordinal)) &&
                  bufport.Equals("80", StringComparison.Ordinal)) {
                bufport = String.Empty;
              }
              if ((url.scheme.Equals("https", StringComparison.Ordinal) ||
url.scheme.Equals("wss", StringComparison.Ordinal)) &&
                  bufport.Equals("443", StringComparison.Ordinal)) {
                bufport = String.Empty;
              }
              if ((url.scheme.Equals("gopher", StringComparison.Ordinal)) &&
bufport.Equals("70", StringComparison.Ordinal)) {
                bufport = String.Empty;
              }
              if ((url.scheme.Equals("ftp", StringComparison.Ordinal)) &&
bufport.Equals("21", StringComparison.Ordinal)) {
                bufport = String.Empty;
              }
              url.port = bufport;
              buffer.Remove(0, buffer.Length);
              state = ParseState.RelativePathStart;
              index = oldindex;
            } else if (c == 0x09 || c == 0x0a || c == 0x0d) {
              error = true;
            } else {
              return null;
            }
            break;
          case ParseState.Query:
            if (c < 0 || c == '#') {
              var utf8 = true;
              if (relative) {
                utf8 = true;
              }
              if (utf8 || encoder == null) {
                // NOTE: Encoder errors can never happen in
                // this case
string bstr = buffer.ToString();
for (var i = 0; i < bstr.Length; ++i) {
                  int ch = DataUtilities.CodePointAt(bstr, i);
                  if (ch >= 0x10000) {
                    ++i;
                  }
                  if (ch < 0x21 || ch > 0x7e || ch == 0x22 || ch == 0x23 ||
                    ch == 0x3c || ch == 0x3e || ch == 0x60) {
                    PercentEncodeUtf8(query, ch);
                  } else {
  { query.Append((char)ch);
}
                  }
                }
              } else {
             byte[] bytes =
                    Encodings.EncodeToBytes(
                    Encodings.StringToInput(buffer.ToString()),
                    encoder);
                    foreach (var ch in bytes) {
                      if (ch < 0x21 || ch > 0x7e || ch == 0x22 || ch == 0x23 ||
                    ch == 0x3c || ch == 0x3e || ch == 0x60) {
                        PercentEncode(query, ch);
                      } else {
  { query.Append((char)ch);
}
                    }
                  }
                }
              buffer.Remove(0, buffer.Length);
              if (c == '#') {
                fragment = new StringBuilder();
                state = ParseState.Fragment;
              }
            } else if (c == 0x09 || c == 0x0a || c == 0x0d) {
              error = true;
            } else {
              if ((!IsUrlCodePoint(c) && c != '%') || (c == '%' &&
                  (index + 2 > ending || !IsHexDigit(s[index]) ||
                    !IsHexDigit(s[index + 1])))) {
                error = true;
              }
              if (c <= 0xffff) {
  { buffer.Append((char)c);
}
  } else if (c <= 0x10ffff) {
    buffer.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
    buffer.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
}
            }
            break;
          case ParseState.RelativePathStart:
            if (c == '\\') {
              error = true;
            }
            state = ParseState.RelativePath;
            if (c != '/' && c != '\\') {
              index = oldindex;
            }
            break;
          case ParseState.RelativePath:
            if ((c < 0 || c == '/' || c == '\\') || (c == '?' || c == '#')) {
              if (c == '\\') {
                error = true;
              }
              if (buffer.Length == 2 && buffer[0] == '.' &&
                  buffer[1] == '.') {
                if (path.Count > 0) {
                  path.RemoveAt(path.Count - 1);
                }
                if (c != '/' && c != '\\') {
                  path.Add(String.Empty);
                }
              } else if (buffer.Length == 1 && buffer[0] == '.') {
                if (c != '/' && c != '\\') {
                  path.Add(String.Empty);
                }
              } else {
                if ("file".Equals(url.scheme, StringComparison.Ordinal) &&
path.Count == 0 &&
                    buffer.Length == 2) {
                  int c1 = buffer[0];
                  int c2 = buffer[1];
                  if (
                    (c2 == '|' || c2 == ':') && ((c1 >= 'A' && c1 <= 'Z') ||
(c1 >=
                'a' && c1 <= 'z'))) {
                    buffer[1] = ':';
                  }
                }
                path.Add(buffer.ToString());
              }
              buffer.Remove(0, buffer.Length);
              if (c == '?') {
                query = new StringBuilder();
                state = ParseState.Query;
              }
              if (c == '#') {
                fragment = new StringBuilder();
                state = ParseState.Fragment;
              }
            } else if (c == '%' && index + 2 <= ending && s[index] == '2' &&
                (s[index + 1] == 'e' || s[index + 1] == 'E')) {
              index += 2;
              buffer.Append((char)'.');
            } else if (c == 0x09 || c == 0x0a || c == 0x0d) {
              error = true;
            } else {
              if ((!IsUrlCodePoint(c) && c != '%') || (c == '%' &&
                  (index + 2 > ending || !IsHexDigit(s[index]) ||
                    !IsHexDigit(s[index + 1])))) {
                error = true;
              }
              if (c <= 0x20 || c >= 0x7F || ((c & 0x7F) == c && "#<>?`\""
                    .IndexOf((char)c) >= 0)) {
                PercentEncodeUtf8(buffer, c);
              } else {
                if (c <= 0xffff) {
  { buffer.Append((char)c);
}
  } else if (c <= 0x10ffff) {
    buffer.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
    buffer.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
}
              }
            }
            break;
          case ParseState.Fragment:
            if (c < 0) {
              break;
            }
            if (c == 0x09 || c == 0x0a || c == 0x0d) {
              error = true;
            } else {
              if ((!IsUrlCodePoint(c) && c != '%') || (c == '%' &&
                  (index + 2 > ending || !IsHexDigit(s[index]) ||
                    !IsHexDigit(s[index + 1])))) {
                error = true;
              }
              if (c < 0x20 || c == 0x7f) {
                PercentEncode(fragment, c);
              } else if (c < 0x7f) {
                if (c <= 0xffff) {
  { fragment.Append((char)c);
}
  } else if (c <= 0x10ffff) {
    fragment.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
    fragment.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
}
              } else {
                PercentEncodeUtf8(fragment, c);
              }
            }
            break;
          default: throw new InvalidOperationException();
        }
      }
      if (error && strict) {
        return null;
      }
      if (schemeData != null) {
        url.schemeData = schemeData.ToString();
      }
      var builder = new StringBuilder();
      if (path.Count == 0) {
        builder.Append('/');
      } else {
        foreach (var segment in path) {
          builder.Append('/');
          builder.Append(segment);
        }
      }
      url.path = builder.ToString();
      if (query != null) {
        url.query = query.ToString();
      }
      if (fragment != null) {
        url.fragment = fragment.ToString();
      }
      if (password != null) {
        url.password = password.ToString();
      }
      if (username != null) {
        url.username = username.ToString();
      }
      return url;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='input'>The parameter <paramref name='input'/> is a
    /// text string.</param>
    /// <param name='delimiter'>The parameter <paramref name='delimiter'/>
    /// is a text string.</param>
    /// <param name='encoding'>The parameter <paramref name='encoding'/> is
    /// a text string.</param>
    /// <param name='useCharset'>The parameter <paramref
    /// name='useCharset'/> is either <c>true</c> or <c>false</c>.</param>
    /// <param name='isindex'>The parameter <paramref name='isindex'/> is
    /// either <c>true</c> or <c>false</c>.</param>
    /// <returns>An IList(string[]) object.</returns>
    public static IList<string[]> ParseQueryString(
      string input,
      string delimiter,
      string encoding,
      bool useCharset,
      bool isindex) {
      if (input == null) {
        throw new ArgumentException();
      }
      delimiter = delimiter ?? "&";
      encoding = encoding ?? "utf-8";
      for (int i = 0; i < input.Length; ++i) {
        if (input[i] > 0x7f) {
          throw new ArgumentException();
        }
      }
      string[] strings = StringUtility.splitAt(input, delimiter);
      IList<string[]> pairs = new List<string[]>();
      foreach (var str in strings) {
        if (str.Length == 0) {
          continue;
        }
        int index = str.IndexOf('=');
        string name = str;
        string value = String.Empty;
        if (index >= 0) {
          name = str.Substring(0, index - 0);
          value = str.Substring(index + 1);
        }
        name = name.Replace('+', ' ');
        value = value.Replace('+', ' ');
        if (useCharset && "_charset_".Equals(name, StringComparison.Ordinal)) {
          string ch = Encodings.ResolveAlias(value);
          if (ch != null) {
            useCharset = false;
            encoding = ch;
          }
        }
        var pair = new string[] { name, value };
        pairs.Add(pair);
      }
      try {
        foreach (var pair in pairs) {
          pair[0] = PercentDecode(pair[0], encoding);
          pair[1] = PercentDecode(pair[1], encoding);
        }
      } catch (IOException e) {
        throw e;
      }
      return pairs;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='s'>The parameter <paramref name='s'/> is a text
    /// string.</param>
    /// <returns>An IList(string) object.</returns>
    public static IList<string> PathList(string s) {
      IList<string> str = new List<string>();
      if (s == null || s.Length == 0) {
        return str;
      }
      if (s[0] != '/') {
        throw new ArgumentException();
      }
      var i = 1;
      while (i <= s.Length) {
        int io = s.IndexOf('/', i);
        if (io >= 0) {
          str.Add(s.Substring(i, io - i));
          i = io + 1;
        } else {
          str.Add(s.Substring(i));
          break;
        }
      }
      return str;
    }

    private static string PercentDecode(string str, string encoding) {
      int len = str.Length;
      var percent = false;
      for (int i = 0; i < len; ++i) {
        char c = str[i];
        if (c == '%') {
          percent = true;
        } else if (c >= 0x80) {
          // Non-ASCII characters not allowed
          return null;
        }
      }
      if (!percent) {
        return str;
      }
      var enc = Encodings.GetEncoding(encoding);
      using (var mos = new MemoryStream()) {
        for (int i = 0; i < len; ++i) {
          int c = str[i];
          if (c == '%') {
            if (i + 2 < len) {
              int a = ToHexNumber(str[i + 1]);
              int b = ToHexNumber(str[i + 2]);
              if (a >= 0 && b >= 0) {
                mos.WriteByte((byte)((a * 16) + b));
                i += 2;
                continue;
              }
            }
          }
          mos.WriteByte((byte)(c & 0xff));
        }
        return Encodings.DecodeToString(enc, mos.ToArray());
      }
    }

    private static void PercentEncode(StringBuilder buffer, int b) {
     buffer.Append((char)'%');
     buffer.Append(hex[(b >> 4) & 0x0f]);
     buffer.Append(hex[b & 0x0f]);
    }

    private static void PercentEncodeUtf8(StringBuilder buffer, int cp) {
      if (cp <= 0x7f) {
      buffer.Append((char)'%');
      buffer.Append(hex[(cp >> 4) & 0x0f]);
      buffer.Append(hex[cp & 0x0f]);
    } else if (cp <= 0x7ff) {
      PercentEncode(buffer, 0xc0 | ((cp >> 6) & 0x1f));
        PercentEncode(buffer, 0x80 | (cp & 0x3f));
      } else if (cp <= 0xffff) {
        PercentEncode(buffer, 0xe0 | ((cp >> 12) & 0x0f));
        PercentEncode(buffer, 0x80 | ((cp >> 6) & 0x3f));
        PercentEncode(buffer, 0x80 | (cp & 0x3f));
      } else {
        PercentEncode(buffer, 0xf0 | ((cp >> 18) & 0x07));
        PercentEncode(buffer, 0x80 | ((cp >> 12) & 0x3f));
        PercentEncode(buffer, 0x80 | ((cp >> 6) & 0x3f));
        PercentEncode(buffer, 0x80 | (cp & 0x3f));
      }
    }

    private static int ToHexNumber(int c) {
      if (c >= 'A' && c <= 'Z') {
        return 10 + c - 'A';
      } else if (c >= 'a' && c <= 'z') {
        return 10 + c - 'a';
      } else {
        return (c >= '0' && c <= '9') ? (c - '0') : (-1);
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='input'>The parameter <paramref name='input'/> is
    /// a.Text.ICharacterInput object.</param>
    /// <param name='encoder'>The parameter <paramref name='encoder'/> is
    /// a.Text.ICharacterEncoder object.</param>
    /// <returns>A byte array.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='encoder'/> or <paramref name='input'/> is null.</exception>
    /// <exception cref='ArgumentException'>Code point out of
    /// range.</exception>
    public static byte[] EncodeToBytesHtml(
      PeterO.Text.ICharacterInput input,
      ICharacterEncoder encoder) {
      if (encoder == null) {
        throw new ArgumentNullException(nameof(encoder));
      }
      if (input == null) {
        throw new ArgumentNullException(nameof(input));
      }
      var writer = new PeterO.ArrayWriter();
      while (true) {
        int cp = input.ReadChar();
        int enc = encoder.Encode(cp, writer);
        if (enc == -2) {
          if (cp < 0 || cp >= 0x110000 || ((cp & 0xf800) == 0xd800)) {
            throw new ArgumentException("code point out of range");
          }
          writer.WriteByte(0x26);
          writer.WriteByte(0x23);
          if (cp == 0) {
            writer.WriteByte(0x30);
          } else {
            while (cp > 0) {
              writer.WriteByte(0x30 + (cp % 10));
              cp /= 10;
            }
          }
          writer.WriteByte(0x3b);
        }
        if (enc == -1) {
          break;
        }
      }
      return writer.ToArray();
    }

  /// <summary>Not documented yet.</summary>
  /// <summary>Not documented yet.</summary>
  /// <param name='pairs'>Not documented yet.</param>
  /// <param name='delimiter'>Not documented yet.</param>
  /// <param name='encoding'>Not documented yet.</param>
  /// <returns/>
  /// <exception cref='ArgumentNullException'>The parameter <paramref
  /// name='pairs'/> is null.</exception>
    public static string ToQueryString(
  IList<string[]> pairs,
  string delimiter,
  string encoding) {
      encoding = encoding ?? "utf-8";
      ICharacterEncoding ienc = Encodings.GetEncoding(encoding);
      if (ienc == null) {
        throw new ArgumentException(nameof(encoding));
      }
      ICharacterEncoder encoder = ienc.GetEncoder();
      var builder = new StringBuilder();
      var first = true;
      if (pairs == null) {
        throw new ArgumentNullException(nameof(pairs));
      }
      foreach (var pair in pairs) {
        if (!first) {
          builder.Append(delimiter == null ? "&" : delimiter);
        }
        first = false;
        if (pair == null || pair.Length < 2) {
          throw new ArgumentException();
        }
          // TODO: Use htmlFallback parameter in EncodeToBytes
        // added to next version of Encoding library
        AppendOutputBytes(
  builder,
  EncodeToBytesHtml(Encodings.StringToInput(pair[0]), encoder));
        builder.Append('=');
        {
          StringBuilder objectTemp = builder;
          byte[] objectTemp2 = EncodeToBytesHtml(
          Encodings.StringToInput(pair[1]),
          encoder);
          AppendOutputBytes(objectTemp, objectTemp2);
}
      }
      return builder.ToString();
    }

    private string scheme = String.Empty;

    private string schemeData = String.Empty;

    private string username = String.Empty;

    private string password = null;

    private string host = null;

    private string path = String.Empty;

    private string query = null;

    private string fragment = null;

    private string port = String.Empty;

    /// <summary>Not documented yet.</summary>
    /// <param name='obj'>The parameter <paramref name='obj'/> is a Object
    /// object.</param>
    /// <returns>Either <c>true</c> or <c>false</c>.</returns>
    public override bool Equals(object obj) {
      if (this == obj) {
        return true;
      }
      if (obj == null) {
        return false;
      }
      if (this.GetType() != obj.GetType()) {
        return false;
      }
      var other = (URL)obj;
      if (this.fragment == null) {
        if (other.fragment != null) {
          return false;
        }
      } else if (!this.fragment.Equals(other.fragment,
  StringComparison.Ordinal)) {
        return false;
      }
      if (this.host == null) {
        if (other.host != null) {
          return false;
        }
      } else if (!this.host.Equals(other.host, StringComparison.Ordinal)) {
        return false;
      }
      if (this.password == null) {
        if (other.password != null) {
          return false;
        }
      } else if (!this.password.Equals(other.password,
  StringComparison.Ordinal)) {
        return false;
      }
      if (this.path == null) {
        if (other.path != null) {
          return false;
        }
      } else if (!this.path.Equals(other.path, StringComparison.Ordinal)) {
        return false;
      }
      if (this.port == null) {
        if (other.port != null) {
          return false;
        }
      } else if (!this.port.Equals(other.port, StringComparison.Ordinal)) {
        return false;
      }
      if (this.query == null) {
        if (other.query != null) {
          return false;
        }
      } else if (!this.query.Equals(other.query, StringComparison.Ordinal)) {
        return false;
      }
      if (this.scheme == null) {
        if (other.scheme != null) {
          return false;
        }
      } else if (!this.scheme.Equals(other.scheme, StringComparison.Ordinal)) {
        return false;
      }
      if (this.schemeData == null) {
        if (other.schemeData != null) {
          return false;
        }
      } else if (!this.schemeData.Equals(other.schemeData,
  StringComparison.Ordinal)) {
        return false;
      }
      if (this.username == null) {
        if (other.username != null) {
          return false;
        }
      } else {
        return !this.username.Equals(other.username, StringComparison.Ordinal);
      }
      return true;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetFragment() {
      return this.fragment ?? String.Empty;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetHash() {
      return String.IsNullOrEmpty(this.fragment) ? String.Empty : "#" +
        this.fragment;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetHost() {
      return (this.port.Length == 0) ? HostSerialize(this.host) :
        (HostSerialize(this.host) + ":" + this.port);
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetHostname() {
      return HostSerialize(this.host);
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetPassword() {
      return this.password == null ? String.Empty : this.password;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetPath() {
      return this.path;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetPathname() {
      if (this.schemeData.Length > 0) {
        return this.schemeData;
      } else {
        return this.path;
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetPort() {
      return this.port;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetProtocol() {
      return this.scheme + ":";
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetQueryString() {
      return this.query == null ? String.Empty : this.query;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetScheme() {
      return this.scheme;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetSchemeData() {
      return this.schemeData;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetSearch() {
      return (this.query == null || this.query.Length == 0) ? String.Empty :
        "?" + this.query;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>A text string.</returns>
    public string GetUsername() {
      return this.username == null ? String.Empty : this.username;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>The return value is not documented yet.</returns>
    public override sealed int GetHashCode() {
      var prime = 31;
      var result = 17;
      if (this.fragment != null) {
        for (var i = 0; i < this.fragment.Length; ++i) {
          result = (prime * result) + this.fragment[i];
        }
      }
      if (this.host != null) {
        for (var i = 0; i < this.host.Length; ++i) {
          result = (prime * result) + this.host[i];
        }
      }
      if (this.password != null) {
        for (var i = 0; i < this.password.Length; ++i) {
          result = (prime * result) + this.password[i];
        }
      }
      if (this.path != null) {
        for (var i = 0; i < this.path.Length; ++i) {
          result = (prime * result) + this.path[i];
        }
      }
      if (this.port != null) {
        for (var i = 0; i < this.port.Length; ++i) {
          result = (prime * result) + this.port[i];
        }
      }
      if (this.query != null) {
        for (var i = 0; i < this.query.Length; ++i) {
          result = (prime * result) + this.query[i];
        }
      }
      if (this.scheme != null) {
        for (var i = 0; i < this.scheme.Length; ++i) {
          result = (prime * result) + this.scheme[i];
        }
      }
      if (this.schemeData != null) {
        for (var i = 0; i < this.schemeData.Length; ++i) {
          result = (prime * result) + this.schemeData[i];
        }
      }
      if (this.username != null) {
        for (var i = 0; i < this.username.Length; ++i) {
          result = (prime * result) + this.username[i];
        }
      }
      return result;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>The return value is not documented yet.</returns>
    public override sealed string ToString() {
      var builder = new StringBuilder();
      builder.Append(this.scheme);
      builder.Append(':');
      if (this.scheme.Equals("file", StringComparison.Ordinal) ||
this.scheme.Equals("http", StringComparison.Ordinal) ||
this.scheme.Equals("https", StringComparison.Ordinal) ||
this.scheme.Equals("ftp", StringComparison.Ordinal) ||
this.scheme.Equals("gopher", StringComparison.Ordinal) ||
this.scheme.Equals("ws", StringComparison.Ordinal) ||
this.scheme.Equals("wss", StringComparison.Ordinal)) {
        // NOTE: We check relative schemes here
        // rather than have a relative flag,
        // as specified in the URL Standard
        // (since the protocol can't be changed
        // as this class is immutable, we can
        // do this variation).
        builder.Append("//");
        if (this.username.Length != 0 || this.password != null) {
          builder.Append(this.username);
          if (this.password != null) {
            builder.Append(':');
            builder.Append(this.password);
          }
          builder.Append('@');
        }
        builder.Append(HostSerialize(this.host));
        if (this.port.Length > 0) {
          builder.Append(':');
          builder.Append(this.port);
        }
        builder.Append(this.path);
      } else {
        builder.Append(this.schemeData);
      }
      if (this.query != null) {
        builder.Append('?');
        builder.Append(this.query);
      }
      if (this.fragment != null) {
        builder.Append('#');
        builder.Append(this.fragment);
      }
      return builder.ToString();
    }
  }
}
