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

namespace com.upokecenter.html {
using System;
using System.IO;
using com.upokecenter.net;
using com.upokecenter.util;

public sealed class HtmlDocument {
  private sealed class ParseURLListener : IResponseListener<IDocument> {
public IDocument processResponse(string url, PeterO.Support.InputStream
      stream,
        IHttpHeaders headers) {
      string contentType=headers.getHeaderField("content-type");
      return HtmlDocument.parseStream(stream, headers.getUrl(), contentType,
          headers.getHeaderField("content-language"));
    }
  }

    /// <summary>* Gets the absolute URL from an HTML element. @param node
    /// A HTML element containing a URL @return an absolute URL of the
    /// element's SRC, DATA, or HREF, or an empty _string if none
    /// exists.</summary>
    /// <param name='node'>The parameter <paramref name='node'/> is not
    /// documented yet.</param>
    /// <returns>A string object.</returns>
  public static string getHref(IElement node) {
    string name = node.getTagName();
    string href="";
    if ("A".Equals(name) || "LINK".Equals(name) || "AREA".Equals(name) ||
        "BASE".Equals(name)) {
      href=node.getAttribute("href");
    } else if ("OBJECT".Equals(name)) {
      href=node.getAttribute("data");
    } else if ("IMG".Equals(name) || "SCRIPT".Equals(name) ||
        "FRAME".Equals(name) || "SOURCE".Equals(name) ||
        "TRACK".Equals(name) || "IFRAME".Equals(name) ||
        "AUDIO".Equals(name) || "VIDEO".Equals(name) ||
        "EMBED".Equals(name)) {
      href=node.getAttribute("src");
    } else {
 return "";
}
    return (href==null || href.Length==0) ? ("") :
      (HtmlDocument.resolveURL(node, href, null));
  }

    /// <summary>Utility method for converting a relative URL to an
    /// absolute one, using the _base URI and the encoding of the given
    /// node. @param node An HTML node, usually an IDocument or IElement
    /// @param href A relative or absolute URL. @return An absolute
    /// URL.</summary>
    /// <param name='node'>The parameter <paramref name='node'/> is not
    /// documented yet.</param>
    /// <param name='href'>The parameter <paramref name='href'/> is not
    /// documented yet.</param>
    /// <returns>A string object.</returns>
  public static string getHref(INode node, string href) {
    return (href==null || href.Length==0) ? ("") :
      (HtmlDocument.resolveURL(node, href, null));
  }

    /// <summary>Parses an HTML document from an input stream, using
    /// "about:blank" as its address. @param stream an input stream @ if an
    /// I/O error occurs.</summary>
    /// <param name='stream'>The parameter <paramref name='stream'/> is not
    /// documented yet.</param>
    /// <returns>An IDocument object.</returns>
  public static IDocument parseStream(PeterO.Support.InputStream stream) {
    return parseStream(stream,"about:blank");
  }

  public static IDocument parseStream(
      PeterO.Support.InputStream stream,
      string address) {
    return parseStream(stream,address,"text/html");
  }

  public static IDocument parseStream(
      PeterO.Support.InputStream stream,
      string address,
      string contentType) {
    return parseStream(stream, address, contentType, null);
  }

    /// <summary>* Parses an HTML document from an input stream, using the
    /// given URL as its address. @param stream an input stream
    /// representing an HTML document. @param address an absolute URL
    /// representing an address. @param contentType Desired MIME media type
    /// of the document, including the charset parameter, if any. Examples:
    /// "text/html" or "application/xhtml+xml; charset=utf-8". @param
    /// contentLang Language tag from the Content-Language header @return
    /// an IDocument representing the HTML document. @ if an I/O error
    /// occurs @ if the given address is not an absolute URL.</summary>
    /// <param name='stream'>The parameter <paramref name='stream'/> is not
    /// documented yet.</param>
    /// <param name='address'>The parameter <paramref name='address'/> is
    /// not documented yet.</param>
    /// <param name='contentType'>The parameter <paramref
    /// name='contentType'/> is not documented yet.</param>
    /// <param name='contentLang'>The parameter <paramref
    /// name='contentLang'/> is not documented yet.</param>
    /// <returns>An IDocument object.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='stream'/> or <paramref name='address'/> or <paramref
    /// name='contentType'/> is null.</exception>
  public static IDocument parseStream(
      PeterO.Support.InputStream stream,
      string address,
      string contentType,
      string contentLang) {
    if ((stream) == null) {
 throw new ArgumentNullException(nameof(stream));
}
    if ((address) == null) {
 throw new ArgumentNullException(nameof(address));
}
    if ((contentType) == null) {
 throw new ArgumentNullException(nameof(contentType));
}
    if (!stream.markSupported()) {
      stream = new PeterO.Support.BufferedInputStream(stream);
    }
      // TODO: Use MediaType to get media type and charset
      string mediatype = contentType;
      string charset = contentType;
    if (mediatype.Equals("text/html")) {
        // TODO: add lang (from Content-Language?)
      var parser = new HtmlParser(stream, address, charset, contentLang);
      return parser.parse();
    } else if (mediatype.Equals("application/xhtml+xml") ||
        mediatype.Equals("application/xml") ||
        mediatype.Equals("image/svg+xml") ||
        mediatype.Equals("text/xml")) {
      var parser = new XhtmlParser(stream, address, charset, contentLang);
      return parser.parse();
    } else {
 throw new ArgumentException("content type not supported: "+mediatype);
}
  }

    /// <summary>* Parses an HTML document from a URL. @param url URL of
    /// the HTML document. In addition to HTTP and other URLs supported by
    /// URLConnection, this method also supports Data URLs. @return a
    /// document _object from the HTML document @ if an I/O error occurs,
    /// such as a network error, a download error, and so on.</summary>
    /// <param name='url'>The parameter <paramref name='url'/> is not
    /// documented yet.</param>
    /// <returns>An IDocument object.</returns>
  public static IDocument parseURL(string url) {
    return DownloadHelper.downloadUrl(url,
        new ParseURLListener(), false);
  }

  private HtmlDocument() {}
}
}
