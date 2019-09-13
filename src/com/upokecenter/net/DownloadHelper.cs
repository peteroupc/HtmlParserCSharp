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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using com.upokecenter.util
namespace com.upokecenter.net {
public sealed class DownloadHelper {
  private sealed class CacheFilter {
    private string cacheFileName;

    public CacheFilter(string cacheFileName) {
      this.cacheFileName = cacheFileName;
    }

    public bool accept(PeterO.Support.File dir, string filename) {
      return filename.StartsWith(cacheFileName+"-",StringComparison.Ordinal) &&
          !filename.EndsWith(".cache",StringComparison.Ordinal);
    }
  }

  internal class CacheResponseInfo {
    public Object cr = null;
    public PeterO.Support.File trueCachedFile = null;
    public PeterO.Support.File trueCacheInfoFile = null;
  }

    /// <summary>* Connects to a URL to download data from that URL. @param
    /// urlString a URL _string. All schemes (protocols) supported by
    /// Java's URLConnection are supported. Data URLs are also supported.
    /// @param callback an _object to call back on, particularly when the
    /// data is ready to be downloaded. Can be null. If the _object also
    /// implements IDownloadEventListener, it will also have its
    /// onConnecting and onConnected methods called. @return the _object
    /// returned by the callback's processResponse method. @ if an I/O
    /// error occurs, particularly network errors. @ if urlString is
    /// null.</summary>
    /// <param name='urlString'>The parameter <paramref name='urlString'/>
    /// is not documented yet.</param>
    /// <param name='callback'>The parameter <paramref name='callback'/> is
    /// not documented yet.</param>
    /// <returns>A T object.</returns>
  ///
  public static T downloadUrl<T>(
      string urlString,
      IResponseListener<T> callback) {
    return downloadUrl(urlString, callback, false);
  }

    /// <summary>* Connects to a URL to download data from that URL. @param
    /// urlString a URL _string. All schemes (protocols) supported by
    /// Java's URLConnection are supported. Data URLs are also supported.
    /// @param callback an _object to call back on, particularly when the
    /// data is ready to be downloaded. Can be null. If the _object also
    /// implements IDownloadEventListener, it will also have its
    /// onConnecting and onConnected methods called. @param
    /// handleErrorResponses if true, the processResponse method of the
    /// supplied callback _object will also be called if an error response
    /// is returned. In this case, the _stream_ argument of that method
    /// will contain the error response body, if any, or null otherwise. If
    /// false and an error response is received, an IOException may be
    /// thrown instead of calling the processResponse method. This
    /// parameter does not affect whether an exception is thrown if the
    /// connection fails. @return the _object returned by the callback's
    /// processResponse method. @ if an I/O error occurs, particularly
    /// network errors. @ if urlString is null.</summary>
    /// <param name='urlString'>The parameter <paramref name='urlString'/>
    /// is not documented yet.</param>
    /// <param name='callback'>The parameter <paramref name='callback'/> is
    /// not documented yet.</param>
    /// <param name='handleErrorResponses'>The parameter <paramref
    /// name='handleErrorResponses'/> is not documented yet.</param>
    /// <returns>A T object.</returns>
  ///
  public static T downloadUrl<T>(
      string urlString,
      IResponseListener<T> callback,
      bool handleErrorResponses) {
    if (urlString == null) {
      throw new ArgumentNullException();
    }
 bool isEventHandler=(callback != null && callback is
       IDownloadEventListener<T>);
    URL uri = null;
    if (isEventHandler && callback != null) {
      ((IDownloadEventListener<T>)callback).onConnecting(urlString);
    }
    uri = URL.parse(urlString);
    if (uri == null) {
      throw new ArgumentException();
    }
    return DownloadHelperImpl.downloadUrl(urlString, callback,
      handleErrorResponses);
  }

  internal static CacheResponseInfo getCachedResponse(
    string urlString,
    PeterO.Support.File pathForCache,
    bool getStream) {
    var incompleteName = new bool[1];
     string cacheFileName=getCacheFileName(urlString,incompleteName)+".htm";
    PeterO.Support.File trueCachedFile = null;
    PeterO.Support.File trueCacheInfoFile = null;
    var crinfo = new CacheResponseInfo();
    if (pathForCache != null && pathForCache.isDirectory()) {
      var cacheFiles = new PeterO.Support.File[] {
        new PeterO.Support.File(pathForCache, cacheFileName)
      };
      if (incompleteName[0]) {
        List<PeterO.Support.File> list = new List<PeterO.Support.File>();
        var filter = new CacheFilter(cacheFileName);
        foreach (var f in pathForCache.listFiles()) {
          if (filter.accept(pathForCache, f.getName())) {
            list.Add(f);
          }
        }
        cacheFiles = list.ToArray();
      } else if (!getStream) {
        crinfo.trueCachedFile = cacheFiles[0];
        crinfo.trueCacheInfoFile = new
          PeterO.Support.File(crinfo.trueCachedFile.ToString()+".cache");
        return crinfo;
      }
      //DebugUtility.Log("%s, getStream=%s",(cacheFiles),getStream);
      foreach (var cacheFile in cacheFiles) {
        if (cacheFile.isFile() && getStream) {
          var fresh = false;
          IHttpHeaders headers = null;
    var cacheInfoFile = new PeterO.Support.File(cacheFile.ToString()+".cache");
          if (cacheInfoFile.isFile()) {
            try {
              CacheControl cc = CacheControl.fromFile(cacheInfoFile);
              //DebugUtility.Log("havecache: %s",cc!=null);
              if (cc == null) {
                fresh = false;
              } else {
                fresh=(cc == null) ? false : cc.isFresh();
                if (!urlString.Equals(cc.getUri())) {
                  // Wrong URI
                  continue;
                }
                //DebugUtility.Log("reqmethod: %s",cc.getRequestMethod());
                if (!"get".Equals(cc.getRequestMethod())) {
                  fresh = false;
                }
              }
              headers=(cc == null) ? new
                  FileBasedHeaders(urlString, cacheFile.Length) :
                cc.getHeaders(cacheFile.Length);
            } catch (IOException) {
              //DebugUtility.Log(e.StackTrace);
              fresh = false;
              headers = new FileBasedHeaders(urlString, cacheFile.Length);
            }
          } else {
            long maxAgeMillis = 24L*3600L*1000L;
            long
  timeDiff =
    Math.Abs(cacheFile.lastModified()-(DateTimeUtility.getCurrentDate()));
            fresh=(timeDiff <= maxAgeMillis);
            headers = new FileBasedHeaders(urlString, cacheFile.Length);
          }
          //DebugUtility.Log("fresh=%s",fresh);
          if (!fresh) {
            // Too old, download again
            trueCachedFile = cacheFile;
            trueCacheInfoFile = cacheInfoFile;
            trueCachedFile.delete();
            trueCacheInfoFile.delete();
            break;
          } else {
            PeterO.Support.InputStream stream = null;
            try {
              stream = new PeterO.Support.BufferedInputStream(new
                PeterO.Support.WrappedInputStream(new
                FileStream(cacheFile.ToString(), FileMode.Open)), 8192);
              crinfo.cr = DownloadHelperImpl.newCacheResponse(stream,
                  headers);
              //DebugUtility.Log("headerfields: %s",headers.getHeaderFields());
            } catch (IOException) {
              // if we get an exception here, we download again
              crinfo.cr = null;
            } finally {
              if (stream != null) {
                try {
  stream.Dispose();
} catch (IOException) {}
              }
            }
          }
        }
      }
    }
    if (pathForCache != null) {
      if (trueCachedFile == null) {
        if (incompleteName[0]) {
          var i = 0;
          do {
            trueCachedFile = new PeterO.Support.File(pathForCache,
           cacheFileName+"-"
                  +Convert.ToString(i, CultureInfo.InvariantCulture));
            ++i;
          } while (trueCachedFile.exists());
        } else {
          trueCachedFile = new PeterO.Support.File(pathForCache, cacheFileName);
        }
      }
 trueCacheInfoFile = new
        PeterO.Support.File(trueCachedFile.ToString()+".cache");
    }
    crinfo.trueCachedFile = trueCachedFile;
    crinfo.trueCacheInfoFile = trueCacheInfoFile;
    return crinfo;
  }
  private static string getCacheFileName(string uri, bool[] incomplete) {
    var builder = new StringBuilder();
    for (int i = 0;i<uri.Length; ++i) {
      char c = uri[i];
      if (c<= 0x20 || c==127 || c=='$' || c=='/' || c=='\\' || c==':' ||
          c=='"' || c=='\'' || c=='|' || c=='<' || c=='>' || c=='*' || c=='?') {
        builder.Append('$');
        builder.Append("0123456789ABCDEF"[(c>>4)&15]);
        builder.Append("0123456789ABCDEF"[(c)&15]);
      } else {
        builder.Append(c);
      }
      if (builder.Length >= 190) {
        if (incomplete != null) {
          incomplete[0]=true;
        }
        return builder.ToString();
      }
    }
    if (incomplete != null) {
      incomplete[0]=false;
    }
    return builder.ToString();
  }

  public static Object getLegacyResponseCache(PeterO.Support.File cachePath) {
    return DownloadHelperImpl.newResponseCache(cachePath);
  }

  public static void pruneCache(PeterO.Support.File cache, long maximumSize) {
    if (cache == null || !cache.isDirectory()) {
      return;
    }
    while (true) {
      long length = 0;
      var exceeded = false;
      long oldest = Int64.MaxValue;
      var count = 0;
      IList<PeterO.Support.File> files = new List<PeterO.Support.File>();
      recursiveListFiles(cache, files);
      foreach (var file in files) {
        if (file.isFile()) {
          length+=file.Length;
          if (length>maximumSize) {
            exceeded = true;
          }
          oldest = file.lastModified();
          ++count;
        }
      }
      if (count <= 1||!exceeded) {
        return;
      }
 long threshold = oldest + Math.Abs(oldest-DateTimeUtility.getCurrentDate())/2;
      count = 0;
      foreach (var file in files) {
        if (file.lastModified()<threshold) {
          if (file.isDirectory()) {
            if (file.delete()) {
              ++count;
            }
          } else {
            length-=file.Length;
            if (file.delete()) {
              ++count;
            }
            if (length<maximumSize) {
              return;
            }
          }
        }
      }
      if (count == 0) {
        return;
      }
    }
  }

  private static void recursiveListFiles(PeterO.Support.File file,
    IList<PeterO.Support.File> files) {
    foreach (var f in file.listFiles()) {
      if (f.isDirectory()) {
        recursiveListFiles(f, files);
      }
      files.Add(f);
    }
  }

  private DownloadHelper() {}
}
}
