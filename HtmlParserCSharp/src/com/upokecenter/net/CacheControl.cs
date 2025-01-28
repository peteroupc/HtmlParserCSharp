/*

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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Com.Upokecenter.Util;
using PeterO;
using PeterO.Cbor;

namespace Com.Upokecenter.Net {
  internal class CacheControl {
    private class AgedHeaders : IHttpHeaders {
      private CacheControl cc = null;
      private long age = 0;
      private IList<string> list = new List<string>();

      public AgedHeaders(CacheControl cc, long age, long length) {
        this.list.Add(cc.headers[0]);
        for (int i = 1; i < cc.headers.Count; i += 2) {
          string key = cc.headers[i];
          if (key != null) {
            key = DataUtilities.ToLowerCaseAscii(key);
            if ("content-length".Equals(key) || "age".Equals(key)) {
              continue;
            }
          }
          this.list.Add(cc.headers[i]);
          this.list.Add(cc.headers[i + 1]);
        }
        this.age = age / 1000; // convert age to seconds
        this.list.Add("age");
        this.list.Add(Convert.ToString(this.age, CultureInfo.InvariantCulture));
        this.list.Add("content-length");
        this.list.Add(Convert.ToString(length, CultureInfo.InvariantCulture));
        // Console.WriteLine("aged=%s",list);
        this.cc = cc;
      }

      public string GetHeaderField(int index) {
        index = (index * 2) + 1 + 1;
        return (index < 0 || index >= this.list.Count) ? null :
          this.list[index + 1];
      }
      public string GetHeaderField(string name) {
        if (name == null) {
          return this.list[0];
        }
        name = DataUtilities.ToLowerCaseAscii(name);
        string last = null;
        for (int i = 1; i < this.list.Count; i += 2) {
          string key = this.list[i];
          if (name.Equals(key)) {
            last = this.list[i + 1];
          }
        }
        return last;
      }
      public long GetHeaderFieldDate(string field, long defaultValue) {
        string hf = this.GetHeaderField(field);
        throw new NotSupportedException();
        /* return HeaderParser.ParseHttpDate(hf, defaultValue);
        */
      }
      public string GetHeaderFieldKey(int index) {
        index = (index * 2) + 1;
        return (index < 0 || index >= this.list.Count) ? null :
          this.list[index];
      }
      public IDictionary<string, IList<string >> GetHeaderFields() {
        IDictionary<string, IList<string >> map = new
        Dictionary<string, IList<string>>();
        map.Add(null, new string[] { this.list[0]});
        for (int i = 1; i < this.list.Count; i += 2) {
          string key = this.list[i];
          IList<string> templist = map[key];
          if (templist == null) {
            templist = new List<string>();
            map.Add(key, templist);
          }
          templist.Add(this.list[i + 1]);
        }
        // Make lists unmodifiable
        /* foreach (var key in new List<string>(map.Keys)) {
          map.Add (key, PeterO.Support.Collections.UnmodifiableList (map[key]));
        }
        return PeterO.Support.Collections.UnmodifiableMap (map);
        */ return map;
      }
      public string GetRequestMethod() {
        return this.cc.requestMethod;
      }
      public int GetResponseCode() {
        return this.cc.code;
      }

      public string GetUrl() {
        return this.cc.uri;
      }
    }
    private class CacheControlSerializer {
      public CacheControl ReadObjectFromStream(Stream stream) {
        try {
          CBORObject jsonobj = CBORObject.ReadJSON(
              stream);
          var cc = new CacheControl();
          cc.cacheability = jsonobj["cacheability"].AsInt32();
          cc.noStore = jsonobj["noStore"].AsBoolean();
          cc.noTransform = jsonobj["noTransform"].AsBoolean();
          cc.mustRevalidate = jsonobj["mustRevalidate"].AsBoolean();
          cc.requestTime = Int64.Parse(jsonobj["requestTime"].AsString(),
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture);
          cc.responseTime = Int64.Parse(jsonobj["responseTime"].AsString(),
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture);
          cc.maxAge = Int64.Parse(jsonobj["maxAge"].AsString(),
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture);
          cc.date = Int64.Parse(jsonobj["date"].AsString(),
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture);
          cc.code = jsonobj["code"].AsInt32();
          cc.age = Int64.Parse(jsonobj["age"].AsString(),
            NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture);
          cc.uri = jsonobj["uri"].AsString();
          cc.requestMethod = jsonobj["requestMethod"].AsString();
          if (cc.requestMethod != null) {
            cc.requestMethod = DataUtilities.ToLowerCaseAscii(
                cc.requestMethod);
          }
          cc.headers = new List<string>();
          CBORObject jsonarr = jsonobj["headers"];
          for (int i = 0; i < jsonarr.Count; ++i) {
            string str = jsonarr[i].AsString();
            if (str != null && (i % 2) != 0) {
              str = DataUtilities.ToLowerCaseAscii(str);
              if ("age".Equals(str) ||
                "connection".Equals(str) ||
                "keep-alive".Equals(str) ||
                "proxy-authenticate".Equals(str) ||
                "proxy-authorization".Equals(str) ||
                "te".Equals(str) ||
                "trailers".Equals(str) ||
                "transfer-encoding".Equals(str) ||
                "upgrade".Equals(str)) {
                // Skip "age" header field and
                // hop-by-hop header fields
                ++i;
                continue;
              }
            }
            cc.headers.Add(str);
          }
          return cc;
        } catch (InvalidCastException e) {
          Console.WriteLine(e.StackTrace);
          return null;
        } catch (FormatException e) {
          Console.WriteLine(e.StackTrace);
          return null;
        }
      }
      public void WriteObjectToStream(CacheControl o, Stream stream) {
        CBORObject jsonobj = CBORObject.NewMap();
        jsonobj.Set("cacheability", o.cacheability);
        jsonobj.Set("noStore", o.noStore);
        jsonobj.Set("noTransform", o.noTransform);
        jsonobj.Set("mustRevalidate", o.mustRevalidate);
        jsonobj.Set("requestTime", o.requestTime+"");
        jsonobj.Set("responseTime", o.responseTime+"");
        jsonobj.Set("maxAge",o.maxAge+"");
        jsonobj.Set("date", o.date+"");
        jsonobj.Set("uri", o.uri);
        jsonobj.Set("requestMethod", o.requestMethod);
        jsonobj.Set("code", o.code);
        jsonobj.Set("age", Convert.ToString(o.age,
          CultureInfo.InvariantCulture));
        CBORObject jsonarr = CBORObject.NewArray();
        foreach (var header in o.headers) {
          jsonarr.Add(header);
        }
        jsonobj.Set("headers", jsonarr);
        byte[] bytes = jsonobj.ToJSONBytes();
        stream.Write(bytes, 0, bytes.Length);
      }
    }

    public static CacheControl FromFile(string f) {
      using (var fs = new FileStream(f.ToString(), FileMode.Open)) {
        return new CacheControlSerializer().ReadObjectFromStream(fs);
      }
    }
    public static CacheControl GetCacheControl(IHttpHeaders headers, long
      requestTime) {
      return null;
      /* var cc = new CacheControl();
            var proxyRevalidate = false;
            var sMaxAge = 0;
            var publicCache = false;
            var privateCache = false;
            var noCache = false;
            long expires = 0;
            var hasExpires = false;
            cc.uri = headers.GetUrl();
            string cacheControl = headers.GetHeaderField("cache-control");
            if (cacheControl != null) {
              var index = 0;
              var intval = new int[1];
              while (index < cacheControl.Length) {
                int current = index;
                if ((index = HeaderParser.ParseToken(
                  cacheControl,
                  current,
                  "private",
                  true)) != current) {
                  privateCache = true;
                } else if ((index = HeaderParser.ParseToken(
                  cacheControl,
                  current,
                  "no-cache",
                  true)) != current) {
                  noCache = true;
                  // Console.WriteLine("returning early because it saw no-cache");
                  return null; // return immediately, this is not cacheable
                } else if ((index = HeaderParser.ParseToken(
                  cacheControl,
                  current,
                  "no-store",
                  false)) != current) {
                  cc.noStore = true;
                  // Console.WriteLine("returning early because it saw no-store");
      // return immediately, this is not cacheable or
      // storable
                  return null;
                } else if ((index = HeaderParser.ParseToken(
                  cacheControl,
                  current,
                  "public",
                  false)) != current) {
                  publicCache = true;
                } else if ((index = HeaderParser.ParseToken(
                  cacheControl,
                  current,
                  "no-transform",
                  false)) != current) {
                  cc.noTransform = true;
                } else if ((index = HeaderParser.ParseToken(
                  cacheControl,
                  current,
                  "must-revalidate",
                  false)) != current) {
                  cc.mustRevalidate = true;
                } else if ((index = HeaderParser.ParseToken(
                  cacheControl,
                  current,
                  "proxy-revalidate",
                  false)) != current) {
                  proxyRevalidate = true;
                } else if ((index = HeaderParser.ParseTokenWithDelta(
                  cacheControl,
                  current,
                  "max-age",
                  intval)) != current) {
                  cc.maxAge = intval[0];
                } else if ((index = HeaderParser.ParseTokenWithDelta(
                  cacheControl,
                  current,
                  "s-maxage",
                  intval)) != current) {
                  sMaxAge = intval[0];
                } else {
                  index = HeaderParser.SkipDirective(cacheControl, current);
                }
              }
              if (!publicCache && !privateCache && !noCache) {
                noCache = true;
              }
            } else {
              int code = headers.GetResponseCode();
              if ((code == 200 || code == 203 || code == 300 || code == 301||
      code == 410) && headers.GetHeaderField("authorization") == null) {
                publicCache = true;
                privateCache = false;
              } else {
                noCache = true;
              }
            }
            if (headers.GetResponseCode() == 206) {
              noCache = true;
            }
            string pragma = headers.GetHeaderField("pragma");
            if (pragma != null && "no-cache"
              .Equals(DataUtilities.ToLowerCaseAscii(pragma))) {
              noCache = true;
              // Console.WriteLine("returning early because it saw pragma no-cache");
              return null;
            }
            long now = DateTimeUtility.GetCurrentDate();
            cc.code = headers.GetResponseCode();
            cc.date = now;
            cc.responseTime = now;
            cc.requestTime = requestTime;
            if (proxyRevalidate) {
              // Enable must-revalidate for simplicity;
              // proxyRevalidate usually only applies to shared caches
              cc.mustRevalidate = true;
            }
            if (headers.GetHeaderField("date") != null) {
              cc.date = headers.GetHeaderFieldDate("date", Int64.MinValue);
              if (cc.date == Int64.MinValue) {
                noCache = true;
              }
            } else {
              noCache = true;
            }
            string expiresHeader = headers.GetHeaderField("expires");
            if (expiresHeader != null) {
              expires = headers.GetHeaderFieldDate("expires", Int64.MinValue);
              hasExpires = cc.date != Int64.MinValue;
            }
            if (headers.GetHeaderField("age") != null) {
              try {
                cc.age = Int32.Parse(headers.GetHeaderField(
                  "age"), NumberStyles.AllowLeadingSign,
        CultureInfo.InvariantCulture);
                if (cc.age < 0) {
                  cc.age = 0;
                }
              } catch (FormatException) {
                cc.age = -1;
              }
            }
            if (cc.maxAge > 0 || sMaxAge > 0) {
              long maxAge = cc.maxAge; // max age in seconds
              if (maxAge == 0) {
                maxAge = sMaxAge;
              }
              if (cc.maxAge > 0 && sMaxAge > 0) {
                maxAge = Math.Max(cc.maxAge, sMaxAge);
              }
              cc.maxAge = maxAge * 1000L; // max-age and s-maxage are in seconds
              hasExpires = false;
            } else if (hasExpires && !noCache) {
              long maxAge = expires - cc.date;
              cc.maxAge = (maxAge > Int32.MaxValue) ? Int32.MaxValue :
      (int)maxAge;
            } else {
              cc.maxAge = (noCache || cc.noStore) ? 0 : (24L * 3600L * 1000L);
            }
            string reqmethod = headers.GetRequestMethod();
            if (reqmethod == null || (
                !DataUtilities.ToLowerCaseAscii(reqmethod).Equals("get"))) {
              // caching responses other than GET responses not supported
              return null;
            }
            cc.requestMethod = DataUtilities.ToLowerCaseAscii(reqmethod);
            cc.cacheability = 2;
            if (noCache) {
              cc.cacheability = 0;
            } else if (privateCache) {
              cc.cacheability = 1;
            }
            var i = 0;
            cc.headers.Add(headers.GetHeaderField(null));
            while (true) {
              string newValue = headers.GetHeaderField(i);
              if (newValue == null) {
                break;
              }
              string key = headers.GetHeaderFieldKey(i);
              ++i;
              if (key == null) {
                // Console.WriteLine("null key");
                continue;
              }
              key = DataUtilities.ToLowerCaseAscii(key);
              // to simplify matters, don't include Age header fields;
              // so-called hop-by-hop headers are also not included
              if (!"age".Equals(key) &&
                !"connection".Equals(key) &&
                !"keep-alive".Equals(key) &&
                !"proxy-authenticate".Equals(key) &&
                !"proxy-authorization".Equals(key) &&
                !"te".Equals(key) &&
                !"trailer".Equals(key) &&
                // NOTE: NOT Trailers
                !"transfer-encoding".Equals(key) &&
                !"upgrade".Equals(key)) {
                cc.headers.Add(key);
                cc.headers.Add(newValue);
              }
            }
            // Console.WriteLine(" cc: %s",cc);
            return cc;
      */
    }
    public static void ToFile(CacheControl o, string file) {
      Stream fs = new FileStream(file.ToString(), FileMode.Create);
      try {
        new CacheControlSerializer().WriteObjectToStream(o, fs);
      } finally {
        if (fs != null) {
          fs.Close();
        }
      }
    }
    private int cacheability = 0;
    // Client must not store the response
    // to disk and must remove it from memory
    // as soon as it's finished with it
    private bool noStore = false;
    // Client must not convert the response
    // to a different format before caching it
    private bool noTransform = false;
    // Client must re-check the server
    // after the response becomes stale
    private bool mustRevalidate = false;
    private long requestTime = 0;
    private long responseTime = 0;
    private long maxAge = 0;
    private long date = 0;
    private long age = 0;

    private int code = 0;
    private string uri = String.Empty;
    private string requestMethod = String.Empty;
    private IList<string> headers;

    private CacheControl() {
      this.headers = new List<string>();
    }

    private long GetAge() {
      throw new NotSupportedException();
      /* long now = DateTimeUtility.GetCurrentDate();
      long age = Math.Max(0, Math.Max(now - this.date, this.age));
      age += this.responseTime - this.requestTime;
      age += now - this.responseTime;
      age = (age > Int32.MaxValue) ? Int32.MaxValue : (int)age;
      return age;
      */
    }

    public int GetCacheability() {
      return this.cacheability;
    }

    public IHttpHeaders GetHeaders(long length) {
      return new AgedHeaders(this, this.GetAge(), length);
    }

    public string GetRequestMethod() {
      return this.requestMethod;
    }

    public string GetUri() {
      return this.uri;
    }

    public bool IsFresh() {
      return (this.cacheability == 0 || this.noStore) ? false : (this.maxAge >
        this.GetAge());
    }

    public bool IsMustRevalidate() {
      return this.mustRevalidate;
    }

    public bool IsNoStore() {
      return this.noStore;
    }

    public bool IsNoTransform() {
      return this.noTransform;
    }
    public override string ToString() {
      return "CacheControl [cacheability=" + this.cacheability + ", noStore=" +
        this.noStore + ", noTransform=" + this.noTransform +
        ", mustRevalidate=" + this.mustRevalidate + ", requestTime=" +
        this.requestTime + ", responseTime=" + this.responseTime + ", maxAge=" +
        this.maxAge + ", date=" + this.date + ", age=" + this.age + ", code=" +
        this.code + ", headerFields=" + this.headers + "]";
    }
  }
}
