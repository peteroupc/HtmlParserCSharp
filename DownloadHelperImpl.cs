/*
Written by Peter O. in 2013.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/
If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
 */
using System;
using System.IO;
using System.Net;

namespace com.upokecenter.net {
  internal static class DownloadHelperImpl {
    public static Object newCacheResponse(PeterO.Support.InputStream stream,
      IHttpHeaders headers) {
      return new Object[] { stream, headers};
    }

    public static Object newResponseCache(PeterO.Support.File name) {
      // Not Implemented Yet
      return null;
    }

    public static T downloadUrl<T>(
      String urlString,
      IResponseListener<T> callback,
      bool handleErrorResponses) {
 bool isEventHandler=(callback != null && callback is
        IDownloadEventListener<T>);
      if (isEventHandler && callback != null) {
        ((IDownloadEventListener<T>)callback).onConnecting(urlString);
      }
      //
      // Other URLs
      //
      Stream stream = null;
      String requestMethod="GET";
      bool calledConnecting = false;
      WebResponse response = null;
      try {
        if (isEventHandler && callback != null && !calledConnecting) {
          ((IDownloadEventListener<T>)callback).onConnecting(urlString);
          calledConnecting = true;
        }
        WebRequest request = WebRequest.Create(urlString);
        request.Timeout = 10000;
        if (request is HttpWebRequest) {
 request.Method = requestMethod;
}
        response = request.GetResponse();
        if (isEventHandler && callback != null) {
          ((IDownloadEventListener<T>)callback).onConnected(urlString);
        }
        IHttpHeaders headers = new HttpHeaders(response);
        stream = response.GetResponseStream();
        if (response is HttpWebResponse &&
          (int)(((HttpWebResponse)response).StatusCode) >= 400) {
          if (!handleErrorResponses) {
 throw new IOException();
}
        }
        if (stream != null) {
          stream = new PeterO.Support.BufferedInputStream(stream);
        }
        T ret=(callback == null) ? default(T) : callback.processResponse(
          urlString,
          (PeterO.Support.InputStream)stream,
          headers);
        return ret;
      } finally {
        if (stream != null) {
          try {
            stream.Close();
          } catch (IOException) {}
        }
        response.Close();
      }
    }
  }
}
