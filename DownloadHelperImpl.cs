/*
Written by Peter O.
Any copyright to this work is released to the Public Domain.
In case this is not possible, this work is also
licensed under the Unlicense: https://unlicense.org/

 */
using System;
using System.IO;
using System.Net;

namespace Com.Upokecenter.Net {
  internal static class DownloadHelperImpl {
    public static Object newCacheResponse (PeterO.Support.InputStream stream,
      IHttpHeaders headers) {
      return new Object[] { stream, headers};
    }

    public static Object newResponseCache (PeterO.Support.File name) {
      // Not Implemented Yet
      return null;
    }

    public static T downloadUrl<T> (
      String urlString,
      IResponseListener<T> callback,
      bool handleErrorResponses) {
      bool isEventHandler = (callback != null && callback is
          IDownloadEventListener<T>);
      if (isEventHandler && callback != null) {
        ((IDownloadEventListener<T>)callback).onConnecting (urlString);
      }
      //
      // Other URLs
      //
      Stream stream = null;
      String requestMethod = "GET";
      var calledConnecting = false;
      if (isEventHandler && callback != null && !calledConnecting) {
        ((IDownloadEventListener<T>)callback).onConnecting (urlString);
        calledConnecting = true;
      }
      WebRequest request = WebRequest.Create (urlString);
      request.Timeout = 10000;
      if (request is HttpWebRequest) {
        request.Method = requestMethod;
      }
      using (WebResponse response = request.GetResponse()) {
        if (isEventHandler && callback != null) {
          ((IDownloadEventListener<T>)callback).onConnected (urlString);
        }
        IHttpHeaders headers = new HttpHeaders(response);
        stream = response.GetResponseStream();
        if (response is HttpWebResponse &&
          (int)(((HttpWebResponse)response).StatusCode) >= 400) {
          if (!handleErrorResponses) {
            throw new IOException();
          }
        }
        using (Stream stream = response.GetResponseStream()) {
          T ret = (callback == null) ? default (T) : callback.processResponse(
            urlString,
            (PeterO.Support.InputStream)stream,
            headers);
          return ret;
        }
      }
    }
  }
}
