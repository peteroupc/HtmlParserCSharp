using System;
using System.Collections.Generic;
using Com.Upokecenter.Net;
using Com.Upokecenter.Util;
using PeterO;
using PeterO.Text;

namespace Com.Upokecenter.Html {
  /// <summary>Not documented yet.</summary>
  public static class HtmlDocument {
    /*
    private sealed class ParseURLListener : IResponseListener<IDocument> {
    public IDocument processResponse(string url, IReader
      stream,
        IHttpHeaders headers) {
      string contentType=headers.GetHeaderField("content-type");
      return HtmlDocument.ParseStream(stream, headers.GetUrl(), contentType,
          headers.GetHeaderField("content-language"));
    }
    }

    // <summary>Gets the absolute URL from an HTML element.</summary>
    // <param name='node'>A HTML element containing a URL</param>
    // <returns>An absolute URL of the
    // element's SRC, DATA, or HREF, or an empty stringValue if none
    // exists.</returns>
    public static string getHref(IElement node) {
    string name = node.GetTagName();
    string href="";
    if ("A".Equals(name) ||
    "LINK".Equals(name) ||
    "AREA".Equals(name) ||
    "BASE".Equals(name)) {
      href=node.GetAttribute("href");
    } else if ("OBJECT".Equals(name)) {
      href=node.GetAttribute("data");
    } else if ("IMG".Equals(name) ||
    "SCRIPT".Equals(name) ||
    "FRAME".Equals(name) ||
    "SOURCE".Equals(name) ||
    "TRACK".Equals(name) ||
    "IFRAME".Equals(name) ||
    "AUDIO".Equals(name) ||
    "VIDEO".Equals(name) ||
    "EMBED".Equals(name)) {
      href=node.GetAttribute("src");
    } else {
    return "";
    }
    return (href==null || href.Length==0) ? ("") :
      (HtmlDocument.resolveURL(node, href, null));
    }

    // <summary>An auxiliary method for converting a relative URL to an
    // absolute one, using the _base URI and the encoding of the specified
    // node.</summary>
    // <param name='node'>An HTML node, usually an IDocument or IElement.</param>
    // <param name='href'>A relative or absolute URL.</param>
    // <returns>An absolute URL.</returns>
    public static string getHref(INode node, string href) {
    return (href==null || href.Length==0) ? ("") :

        (HtmlDocument.resolveURL(
          node,
          href,
          null));
    }
    */

    /// <summary>Not documented yet.</summary>
    /// <param name='str'>The parameter <paramref name='str'/> is a text
    /// string.</param>
    /// <returns>An IDocument object.</returns>
    public static IDocument FromString(string str) {
      byte[] bytes = DataUtilities.GetUtf8Bytes(str, true);
      return ParseStream(DataIO.ToReader(bytes));
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='str'>The parameter <paramref name='str'/> is a text
    /// string.</param>
    /// <param name='state'>The parameter <paramref name='state'/> is a
    /// text string.</param>
    /// <param name='lst'>The parameter <paramref name='lst'/> is a text
    /// string.</param>
    /// <returns>An IList(string[]) object.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='state'/> is null.</exception>
    public static IList<string[]> ParseTokens(
      string str,
      string state,
      string lst) {
      byte[] bytes = DataUtilities.GetUtf8Bytes(str, true);

      // TODO: add lang (from Content-Language?)
      var parser = new HtmlParser(
        DataIO.ToReader(bytes),
        "about:blank",
        "utf-8",
        null);
      if (state == null) {
        throw new ArgumentNullException(nameof(state));
      }
      return parser.ParseTokens(state, lst);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='str'>The parameter <paramref name='str'/> is a text
    /// string.</param>
    /// <param name='checkError'>Either <c>true</c> or <c>false</c>.</param>
    /// <returns>An IDocument object.</returns>
    public static IDocument FromString(string str, bool checkError) {
      byte[] bytes = DataUtilities.GetUtf8Bytes(str, true);
      return ParseStream(DataIO.ToReader(bytes), checkError);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>The parameter <paramref name='name'/> is a text
    /// string.</param>
    /// <returns>An IElement object.</returns>
    public static IElement CreateHtmlElement(string name) {
      var valueElement = new Element();
      valueElement.SetLocalName(name);
      valueElement.SetNamespace(HtmlCommon.HTML_NAMESPACE);
      return valueElement;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>The parameter <paramref name='name'/> is a text
    /// string.</param>
    /// <param name='namespaceName'>The parameter <paramref
    /// name='namespaceName'/> is a text string.</param>
    /// <returns>An IElement object.</returns>
    public static IElement CreateElement(
      string name,
      string namespaceName) {
      var valueElement = new Element();
      valueElement.SetLocalName(name);
      valueElement.SetNamespace(namespaceName);
      return valueElement;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='nodes'>Not documented yet.</param>
    /// <returns>The return value is not documented yet.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='nodes'/> is null.</exception>
    public static string ToDebugString(IList<INode> nodes) {
      if (nodes == null) {
        throw new ArgumentNullException(nameof(nodes));
      }
      return Document.ToDebugString(nodes);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='str'>The parameter <paramref name='str'/> is a text
    /// string.</param>
    /// <param name='context'>The parameter <paramref name='context'/> is
    /// a.Upokecenter.Html.IElement object.</param>
    /// <returns>An IList(INode) object.</returns>
    public static IList<INode> FragmentFromString(
      string str,
      IElement context) {
      return FragmentFromString(str, context, false);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='str'>The parameter <paramref name='str'/> is a text
    /// string.</param>
    /// <param name='context'>The parameter <paramref name='context'/> is
    /// a.Upokecenter.Html.IElement object.</param>
    /// <param name='checkError'>The parameter <paramref
    /// name='checkError'/> is either <c>true</c> or <c>false</c>.</param>
    /// <returns>An IList(INode) object.</returns>
    public static IList<INode> FragmentFromString(
      string str,
      IElement context,
      bool checkError) {
      byte[] bytes = DataUtilities.GetUtf8Bytes(str, true);

      // TODO: add lang (from Content-Language?)
      var parser = new HtmlParser(
        DataIO.ToReader(bytes),
        "about:blank",
        "utf-8",
        null);
      IList<INode> ret = parser.CheckError(checkError).ParseFragment(context);
      return ret;
    }

    /// <summary>Parses an HTML document from an input stream, using
    /// "about:blank" as its address.</summary>
    /// <param name='stream'>An input stream.</param>
    /// <returns>An IDocument object.</returns>
    public static IDocument ParseStream(IReader stream) {
      return ParseStream(stream, "about:blank");
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='stream'>The parameter <paramref name='stream'/> is a
    /// IReader object.</param>
    /// <param name='checkError'>The parameter <paramref
    /// name='checkError'/> is either <c>true</c> or <c>false</c>.</param>
    /// <returns>An IDocument object.</returns>
    public static IDocument ParseStream(IReader stream, bool checkError) {
      return ParseStream(stream, "about:blank", "text/Html", null, checkError);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='stream'>The parameter <paramref name='stream'/> is a
    /// IReader object.</param>
    /// <param name='address'>The parameter <paramref name='address'/> is a
    /// text string.</param>
    /// <returns>An IDocument object.</returns>
    public static IDocument ParseStream(
      IReader stream,
      string address) {
      return ParseStream(stream, address, "text/Html");
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='stream'>The parameter <paramref name='stream'/> is a
    /// IReader object.</param>
    /// <param name='address'>The parameter <paramref name='address'/> is a
    /// text string.</param>
    /// <param name='contentType'>The parameter <paramref
    /// name='contentType'/> is a text string.</param>
    /// <returns>An IDocument object.</returns>
    public static IDocument ParseStream(
      IReader stream,
      string address,
      string contentType) {
      return ParseStream(stream, address, contentType, null);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='stream'>The parameter <paramref name='stream'/> is a
    /// IReader object.</param>
    /// <param name='address'>The parameter <paramref name='address'/> is a
    /// text string.</param>
    /// <param name='contentType'>The parameter <paramref
    /// name='contentType'/> is a text string.</param>
    /// <param name='contentLang'>The parameter <paramref
    /// name='contentLang'/> is a text string.</param>
    /// <returns>An IDocument object.</returns>
    public static IDocument ParseStream(
      IReader stream,
      string address,
      string contentType,
      string contentLang) {
      return ParseStream(stream, address, contentType, contentLang, false);
    }

    /// <summary>* Parses an HTML document from an input stream, using the
    /// specified URL as its address. @ if an I/O error occurs @ if the
    /// specified address is not an absolute URL.</summary>
    /// <param name='stream'>An input stream representing an HTML
    /// document.</param>
    /// <param name='address'>An absolute URL representing an
    /// address.</param>
    /// <param name='contentType'>Desired MIME media type of the document,
    /// including the charset parameter, if any. Examples: "text/Html" or
    /// "application/xhtml+xml; charset=utf-8".</param>
    /// <param name='contentLang'>Language tag from the Content-Language
    /// header.</param>
    /// <param name='checkError'>Either <c>true</c> or <c>false</c>.</param>
    /// <returns>An IDocument representing the HTML document.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='stream'/> or <paramref name='address'/> or <paramref
    /// name='contentType'/> is null.</exception>
    public static IDocument ParseStream(
      IReader stream,
      string address,
      string contentType,
      string contentLang,
      bool checkError) {
      if (stream == null) {
        throw new ArgumentNullException(nameof(stream));
      }
      if (address == null) {
        throw new ArgumentNullException(nameof(address));
      }
      if (contentType == null) {
        throw new ArgumentNullException(nameof(contentType));
      }
      // TODO: Use MediaType to get media type and charset
      string mediatype = contentType;
      string charset = "utf-8";
      if (mediatype.Equals("text/Html", StringComparison.Ordinal)) {
        // TODO: add lang (from Content-Language?)
        var parser = new HtmlParser(stream, address, charset, contentLang);
        IDocument docret = parser.CheckError(checkError).Parse();
        return docret;
      } else if (mediatype.Equals("application/xhtml+xml",
        StringComparison.Ordinal) ||
        mediatype.Equals("application/xml", StringComparison.Ordinal) ||
        mediatype.Equals("image/svg+xml", StringComparison.Ordinal) ||
        mediatype.Equals("text/xml", StringComparison.Ordinal)) {
        throw new NotSupportedException();
        // var parser = new XhtmlParser(stream, address, charset, contentLang);
        // return parser.parse();
      } else {
        throw new ArgumentException("content type not supported: " +
          mediatype);
      }
    }
  }
}
