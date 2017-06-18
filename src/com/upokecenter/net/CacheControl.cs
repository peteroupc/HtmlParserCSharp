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
using com.upokecenter.io;
using com.upokecenter.json;
using com.upokecenter.util;
internal class CacheControl {
  private class AgedHeaders : IHttpHeaders {
    CacheControl cc = null;
    long age = 0;
    IList<string> list = new List<string>();

    public AgedHeaders(CacheControl cc, long age, long length) {
      list.Add(cc.headers[0]);
      for (int i = 1;i<cc.headers.Count;i+=2) {
        string key = cc.headers[i];
        if (key != null) {
          key = StringUtility.toLowerCaseAscii(key);
          if ("content-length".Equals(key)||"age".Equals(key)) {
            continue;
          }
        }
        list.Add(cc.headers[i]);
        list.Add(cc.headers[i + 1]);
      }
      this.age = age/1000;  // convert age to seconds
      list.Add("age");
      list.Add(Convert.ToString(this.age, CultureInfo.InvariantCulture));
      list.Add("content-length");
      list.Add(Convert.ToString(length, CultureInfo.InvariantCulture));
      //Console.WriteLine("aged=%s",list);
      this.cc = cc;
    }

    public string getHeaderField(int index) {
      index=(index)*2 + 1 + 1;
      return (index<0 || index >= list.Count) ? (null) : (list[index + 1]);
    }
    public string getHeaderField(string name) {
      if (name == null) {
 return list[0];
}
      name = StringUtility.toLowerCaseAscii(name);
      string last = null;
      for (int i = 1;i<list.Count;i+=2) {
        string key = list[i];
        if (name.Equals(key)) {
          last = list[i + 1];
        }
      }
      return last;
    }
    public long getHeaderFieldDate(string field, long defaultValue) {
      return HeaderParser.parseHttpDate(getHeaderField(field), defaultValue);
    }
    public string getHeaderFieldKey(int index) {
      index=(index)*2 + 1;
      return (index<0 || index >= list.Count) ? (null) : (list[index]);
    }
    public IDictionary<string, IList<string>> getHeaderFields() {
      IDictionary<string, IList<string>> map = new
        PeterO.Support.LenientDictionary<string, IList<string>>();
      map.Add(null, (new string[] { list[0]}));
      for (int i = 1;i<list.Count;i+=2) {
        string key = list[i];
        IList<string> templist = map[key];
        if (templist == null) {
          templist = new List<string>();
          map.Add(key, templist);
        }
        templist.Add(list[i + 1]);
      }
      // Make lists unmodifiable
      foreach (var key in new List<string>(map.Keys)) {
        map.Add(key, PeterO.Support.Collections.UnmodifiableList(map[key]));
      }
      return PeterO.Support.Collections.UnmodifiableMap(map);
    }
    public string getRequestMethod() {
      return cc.requestMethod;
    }
    public int getResponseCode() {
      return cc.code;
    }

    public string getUrl() {
      return cc.uri;
    }
  }
  private class CacheControlSerializer {
 public CacheControl readObjectFromStream(PeterO.Support.InputStream stream) {
      try {
        JSONObject jsonobj = new JSONObject(StreamUtility.streamToString(stream));
        CacheControl cc = new CacheControl();
        cc.cacheability=jsonobj.getInt("cacheability");
        cc.noStore=jsonobj.getBoolean("noStore");
        cc.noTransform=jsonobj.getBoolean("noTransform");
        cc.mustRevalidate=jsonobj.getBoolean("mustRevalidate");
        cc.requestTime=Int64.Parse(jsonobj.getString("requestTime"
), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        cc.responseTime=Int64.Parse(jsonobj.getString("responseTime"
), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        cc.maxAge=Int64.Parse(jsonobj.getString("maxAge"
), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        cc.date=Int64.Parse(jsonobj.getString("date"
), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        cc.code=jsonobj.getInt("code");
        cc.age=Int64.Parse(jsonobj.getString("age"
), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        cc.uri=jsonobj.getString("uri");
        cc.requestMethod=jsonobj.getString("requestMethod");
        if (cc.requestMethod != null) {
          cc.requestMethod = StringUtility.toLowerCaseAscii(cc.requestMethod);
        }
        cc.headers = new List<string>();
        JSONArray jsonarr=jsonobj.getJSONArray("headers");
        for (int i = 0;i<jsonarr.Length; ++i) {
          string str = jsonarr.getString(i);
          if (str != null && (i%2) != 0) {
            str = StringUtility.toLowerCaseAscii(str);
            if ("age".Equals(str) || "connection".Equals(str) ||
                "keep-alive".Equals(str) || "proxy-authenticate".Equals(str) ||
                "proxy-authorization".Equals(str) || "te".Equals(str) ||
                "trailers".Equals(str) || "transfer-encoding".Equals(str) ||
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
      } catch (Json.InvalidJsonException e) {
        Console.WriteLine(e.StackTrace);
        return null;
      }
    }
    public void writeObjectToStream(CacheControl o, Stream stream) {
      JSONObject jsonobj = new JSONObject();
      jsonobj.put("cacheability",o.cacheability);
      jsonobj.put("noStore",o.noStore);
      jsonobj.put("noTransform",o.noTransform);
      jsonobj.put("mustRevalidate",o.mustRevalidate);
      jsonobj.put("requestTime",
 Convert.ToString(o.requestTime, CultureInfo.InvariantCulture));
      jsonobj.put("responseTime",
 Convert.ToString(o.responseTime, CultureInfo.InvariantCulture));
 jsonobj.put("maxAge",
 Convert.ToString(o.maxAge, CultureInfo.InvariantCulture));
      jsonobj.put("date",Convert.ToString(o.date,CultureInfo.InvariantCulture));
      jsonobj.put("uri",o.uri);
      jsonobj.put("requestMethod",o.requestMethod);
      jsonobj.put("code",o.code);
      jsonobj.put("age",Convert.ToString(o.age,CultureInfo.InvariantCulture));
      JSONArray jsonarr = new JSONArray();
      foreach (var header in o.headers) {
        jsonarr.put(header);
      }
      jsonobj.put("headers",jsonarr);
      StreamUtility.stringToStream(jsonobj.ToString(), stream);
    }
  }
  public static CacheControl fromFile(PeterO.Support.File f) {
    PeterO.Support.WrappedInputStream fs = new
      PeterO.Support.WrappedInputStream(new
      FileStream(f.ToString(), FileMode.Open));
    try {
      return new CacheControlSerializer().readObjectFromStream(fs);
    } finally {
      if (fs != null) {
        fs.Close();
      }
    }
  }
  public static CacheControl getCacheControl(IHttpHeaders headers, long
    requestTime) {
    CacheControl cc = new CacheControl();
    bool proxyRevalidate = false;
    int sMaxAge = 0;
    bool publicCache = false;
    bool privateCache = false;
    bool noCache = false;
    long expires = 0;
    bool hasExpires = false;
    cc.uri = headers.getUrl();
    string cacheControl=headers.getHeaderField("cache-control");
    if (cacheControl != null) {
      int index = 0;
      int[] intval = new int[1];
      while (index<cacheControl.Length) {
        int current = index;
        if ((index=HeaderParser.parseToken(cacheControl,current,"private",
 true)) != current) {
          privateCache = true;
        } else if ((index=HeaderParser.parseToken(cacheControl,current,"no-cache",
 true)) != current) {
          noCache = true;
          //Console.WriteLine("returning early because it saw no-cache");
          return null;  // return immediately, this is not cacheable
        } else if ((index = HeaderParser.parseToken(
            cacheControl,
            current,
            "no-store",
            false))!=current) {
          cc.noStore = true;
          //Console.WriteLine("returning early because it saw no-store");
          return null;  // return immediately, this is not cacheable or storable
        } else if ((index = HeaderParser.parseToken(
            cacheControl,
            current,
            "public",
            false))!=current) {
          publicCache = true;
        } else if ((index = HeaderParser.parseToken(
            cacheControl,
            current,
            "no-transform",
            false))!=current) {
          cc.noTransform = true;
        } else if ((index = HeaderParser.parseToken(
            cacheControl,
            current,
            "must-revalidate",
            false))!=current) {
          cc.mustRevalidate = true;
        } else if ((index = HeaderParser.parseToken(
            cacheControl,
            current,
            "proxy-revalidate",
            false))!=current) {
          proxyRevalidate = true;
        } else if ((index = HeaderParser.parseTokenWithDelta(
            cacheControl,
            current,
            "max-age",
            intval))!=current) {
          cc.maxAge = intval[0];
        } else if ((index = HeaderParser.parseTokenWithDelta(
            cacheControl,
            current,
            "s-maxage",
            intval))!=current) {
          sMaxAge = intval[0];
        } else {
          index = HeaderParser.skipDirective(cacheControl, current);
        }
      }
      if (!publicCache && !privateCache && !noCache) {
        noCache = true;
      }
    } else {
      int code = headers.getResponseCode();
      if ((code == 200 || code == 203 || code == 300 || code == 301 || code == 410) &&
          headers.getHeaderField("authorization")==null) {
        publicCache = true;
        privateCache = false;
      } else {
        noCache = true;
      }
    }
    if (headers.getResponseCode() == 206) {
      noCache = true;
    }
    string pragma=headers.getHeaderField("pragma");
 if (pragma!=null && "no-cache"
      .Equals(StringUtility.toLowerCaseAscii(pragma))) {
      noCache = true;
      //Console.WriteLine("returning early because it saw pragma no-cache");
      return null;
    }
    long now = DateTimeUtility.getCurrentDate();
    cc.code = headers.getResponseCode();
    cc.date = now;
    cc.responseTime = now;
    cc.requestTime = requestTime;
    if (proxyRevalidate) {
      // Enable must-revalidate for simplicity;
      // proxyRevalidate usually only applies to shared caches
      cc.mustRevalidate = true;
    }
    if (headers.getHeaderField("date")!=null) {
      cc.date=headers.getHeaderFieldDate("date",Int64.MinValue);
      if (cc.date == Int64.MinValue) {
        noCache = true;
      }
    } else {
      noCache = true;
    }
    string expiresHeader=headers.getHeaderField("expires");
    if (expiresHeader != null) {
      expires=headers.getHeaderFieldDate("expires",Int64.MinValue);
      hasExpires=(cc.date != Int64.MinValue);
    }
    if (headers.getHeaderField("age")!=null) {
      try {
        cc.age=Int32.Parse(headers.getHeaderField("age"
), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        if (cc.age< 0) {
          cc.age = 0;
        }
      } catch (FormatException) {
        cc.age=-1;
      }
    }
    if (cc.maxAge>0 || sMaxAge>0) {
      long maxAge = cc.maxAge;  // max age in seconds
      if (maxAge == 0) {
        maxAge = sMaxAge;
      }
      if (cc.maxAge>0 && sMaxAge>0) {
        maxAge = Math.Max(cc.maxAge, sMaxAge);
      }
      cc.maxAge = maxAge*1000L;  // max-age and s-maxage are in seconds
      hasExpires = false;
    } else if (hasExpires && !noCache) {
      long maxAge = expires-cc.date;
      cc.maxAge=(maxAge>Int32.MaxValue) ? Int32.MaxValue : (int)maxAge;
    } else if (noCache || cc.noStore) {
      cc.maxAge = 0;
    } else {
      cc.maxAge = 24L*3600L*1000L;
    }
    string reqmethod = headers.getRequestMethod();
    if (reqmethod == null || (
        !StringUtility.toLowerCaseAscii(reqmethod).Equals("get"))) {
 // caching responses other than GET responses not supported
      return null;
 }
    cc.requestMethod = StringUtility.toLowerCaseAscii(reqmethod);
    cc.cacheability = 2;
    if (noCache) {
      cc.cacheability = 0;
    } else if (privateCache) {
      cc.cacheability = 1;
    }
    int i = 0;
    cc.headers.Add(headers.getHeaderField(null));
    while (true) {
      string newValue = headers.getHeaderField(i);
      if (newValue == null) {
        break;
      }
      string key = headers.getHeaderFieldKey(i);
      ++i;
      if (key == null) {
        //Console.WriteLine("null key");
        continue;
      }
      key = StringUtility.toLowerCaseAscii(key);
      // to simplify matters, don't include Age header fields;
      // so-called hop-by-hop headers are also not included
      if (!"age".Equals(key) && !"connection".Equals(key) &&
          !"keep-alive".Equals(key) && !"proxy-authenticate".Equals(key) &&
          !"proxy-authorization".Equals(key) && !"te".Equals(key) &&
          !"trailer".Equals(key) &&  // NOTE: NOT Trailers
          !"transfer-encoding".Equals(key) && !"upgrade".Equals(key)) {
        cc.headers.Add(key);
        cc.headers.Add(newValue);
      }
    }
    //Console.WriteLine(" cc: %s",cc);
    return cc;
  }
  public static void toFile(CacheControl o, PeterO.Support.File file) {
    Stream fs = new FileStream((file).ToString(), FileMode.Create);
    try {
      new CacheControlSerializer().writeObjectToStream(o, fs);
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
  private string uri="";
  private string requestMethod="";
  private IList<string> headers;

  private CacheControl() {
    headers = new List<string>();
  }

  private long getAge() {
    long now = DateTimeUtility.getCurrentDate();
    long age = Math.Max(0, Math.Max(now-date, this.age));
    age+=(responseTime-requestTime);
    age+=(now-responseTime);
    age=(age>Int32.MaxValue) ? Int32.MaxValue : (int)age;
    return age;
  }

  public int getCacheability() {
    return cacheability;
  }

  public IHttpHeaders getHeaders(long length) {
    return new AgedHeaders(this, getAge(), length);
  }

  public string getRequestMethod() {
    return requestMethod;
  }

  public string getUri() {
    return uri;
  }

  public bool isFresh() {
    return (cacheability == 0 || noStore) ? (false) : ((maxAge>getAge()));
  }

  public bool isMustRevalidate() {
    return mustRevalidate;
  }

  public bool isNoStore() {
    return noStore;
  }

  public bool isNoTransform() {
    return noTransform;
  }
  public override string ToString() {
    return "CacheControl [cacheability=" + cacheability + ", noStore="+
        noStore + ", noTransform=" + noTransform+
        ", mustRevalidate=" + mustRevalidate + ", requestTime="+
        requestTime + ", responseTime=" + responseTime + ", maxAge="+
        maxAge + ", date=" + date + ", age=" + age + ", code=" + code+
        ", headerFields=" + headers + "]";
  }
}
}
