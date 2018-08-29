/*
Written by Peter O. in 2013.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/
If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using com.upokecenter.net;
using com.upokecenter.util;

namespace com.upokecenter.net {
  internal sealed class HttpHeaders : IHttpHeaders {
    internal interface IWebResponse {
      string ResponseUri { get; }
    }

    internal interface IHttpWebResponse : IWebResponse {
      Version ProtocolVersion { get; }
      int StatusCode { get; }
      string StatusDescription { get; }
      string Method { get; }
    }

    public IWebResponse response;
    public HttpHeaders(IWebResponse response) {
      this.response = response;
    }

    public string getUrl() {
      return this.response.ResponseUri.ToString();
    }

    public string getRequestMethod() {
      return (response is IHttpWebResponse) ?
        (((IHttpWebResponse)this.response).Method) : ("");
    }

    public string getHeaderField(string name) {
      if (this.response is IHttpWebResponse) {
        if (name == null) {
 return GetStatusLine((IHttpWebResponse)this.response);
}
      }
      return this.response.Headers.Get(name);
    }

    private static string GetStatusLine(IHttpWebResponse resp) {
      Version vers = resp.ProtocolVersion;
      if (vers == HttpVersion.Version10) {
 return "HTTP/1.0 "+Convert.ToString(
  (int)resp.StatusCode,
  System.Globalization.CultureInfo.InvariantCulture)+" "
            +resp.StatusDescription;
 }
      else return "HTTP/1.1 "+Convert.ToString(
  (int)resp.StatusCode,
  System.Globalization.CultureInfo.InvariantCulture)+" "
            +resp.StatusDescription;
    }

    public string getHeaderField(int name) {
      if (name< 0) {
 return null;
}
      if (this.response is IHttpWebResponse) {
        if (name == 0) {
 return GetStatusLine((IHttpWebResponse)this.response);
}
        --name;
      }
      return (name>this.response.Headers.Count) ? (null) :
        (this.response.Headers.Get(name-1));
    }

    public string getHeaderFieldKey(int name) {
      if (name< 0) {
 return null;
}
      if (this.response is IHttpWebResponse) {
        if (name == 0) {
 return null;
}
        --name;
      }
      return (name>this.response.Headers.Count) ? (null) :
        (this.response.Headers.GetKey(name-1));
    }

    public int getResponseCode() {
      if (response is FtpWebResponse) {
 return (int)((FtpWebResponse)response).StatusCode;
}
      return (response is IHttpWebResponse) ?
        ((int)((IHttpWebResponse)response).StatusCode) : (0);
    }

    public long getHeaderFieldDate(string field, long defaultValue) {
      String value = getHeaderField(field);
      return (value == null) ? (defaultValue) :
        (HeaderParser.parseHttpDate(value, defaultValue));
    }

    public IDictionary<string, IList<string>> getHeaderFields() {
      IDictionary<string, IList<string>> map = new
        PeterO.Support.LenientDictionary<string, IList<string>>();
      if (this.response is IHttpWebResponse) {
 map.Add(null, PeterO.Support.Collections.UnmodifiableList(new
   String[] { GetStatusLine((IHttpWebResponse)this.response)}));
}
      foreach (String k in this.response.Headers.AllKeys) {
  map.Add(DataUtilities.ToLowerCaseAscii(k),

  PeterO.Support.Collections.UnmodifiableList(this.response.Headers.GetValues(k)));
      }
      return PeterO.Support.Collections.UnmodifiableMap(map);
    }
  }
}
