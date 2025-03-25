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
using Com.Upokecenter.Util;
using PeterO;

namespace Com.Upokecenter.Net {
  internal class HttpHeadersFromMap : IHttpHeaders {
    private IDictionary<string, IList<string>> valueMap;
    private IList<string> valueList;
    private string valueRequestMethod;
    private string valueUrlString;

    public HttpHeadersFromMap(
      string valueUrlString,
      string valueRequestMethod,
      IDictionary<string, IList<string>> valueMap) {
      this.valueMap = valueMap;
      this.valueUrlString = valueUrlString;
      this.valueRequestMethod = valueRequestMethod;
      this.valueList = new List<string>();
      var keyset = new List<string>();
      foreach (var str in this.valueMap.Keys) {
        if (String.IsNullOrEmpty(str)) {
          // Add status line (also has the side
          // effect that it will appear first in the valueList)
          IList<string> v = this.valueMap[str];
          if (v != null && v.Count > 0) {
            this.valueList.Add(v[0]);
          } else {
            this.valueList.Add("HTTP/1.1 200 OK");
          }
        } else {
          keyset.Add(str);
        }
      }
      keyset.Sort();
      // Add the remaining headers in sorted order
      foreach (var s in keyset) {
        IList<string> v = this.valueMap[s];
        if (v != null && v.Count > 0) {
          foreach (var ss in v) {
            this.valueList.Add(s);
            this.valueList.Add(ss);
          }
        }
      }
    }

    public string GetHeaderField(int index) {
      if (index == 0) {
        return this.valueList[0];
      }
      if (index < 0) {
        return null;
      }
      index = ((index - 1) * 2) + 1 + 1;
      return (index < 0 || index >= this.valueList.Count) ? null :
        this.valueList[index + 1];
    }

    public string GetHeaderField(string name) {
      if (name == null) {
        return this.valueList[0];
      }
      name = DataUtilities.ToLowerCaseAscii(name);
      string last = null;
      for (int i = 1; i < this.valueList.Count; i += 2) {
        string key = this.valueList[i];
        if (name.Equals(key)) {
          last = this.valueList[i + 1];
        }
      }
      return last;
    }

    public long GetHeaderFieldDate(string field, long defaultValue) {
      return 0;
      // TODO
      // return HeaderParser.parseHttpDate(GetHeaderField(field), defaultValue);
    }

    public string GetHeaderFieldKey(int index) {
      if (index == 0 || index < 0) {
        return null;
      }
      index = ((index - 1) * 2) + 1;
      return (index < 0 || index >= this.valueList.Count) ? null :
        this.valueList[index];
    }

    public IDictionary<string, IList<string>> GetHeaderFields() {
      // TODO: Make unmodifiable
      return this.valueMap;
      // return PeterO.Support.Collections.UnmodifiableMap(this.valueMap);
    }

    public string GetRequestMethod() {
      return this.valueRequestMethod;
    }

    public int GetResponseCode() {
      string status = this.GetHeaderField(null);
      return -1;
      // TODO
      // return (status == null) ? (-1) :
      // (HeaderParser.GetResponseCode(status));
    }

    public string GetUrl() {
      return this.valueUrlString;
    }
  }
}
