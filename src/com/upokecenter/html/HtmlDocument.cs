using System;
using System.Collections.Generic;
using PeterO;
using PeterO.Text;
using com.upokecenter.net;
using com.upokecenter.util;

namespace com.upokecenter.html {
    /// <summary>Not documented yet.</summary>
public static class HtmlDocument {
    /*
  private sealed class ParseURLListener : IResponseListener<IDocument> {
public IDocument processResponse(string url, IReader
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
    if ("A".Equals(name) ||
"LINK".Equals(name) ||
"AREA".Equals(name) ||
"BASE".Equals(name)) {
      href=node.getAttribute("href");
    } else if ("OBJECT".Equals(name)) {
      href=node.getAttribute("data");
    } else if ("IMG".Equals(name) ||
"SCRIPT".Equals(name) ||
"FRAME".Equals(name) ||
"SOURCE".Equals(name) ||
"TRACK".Equals(name) ||
"IFRAME".Equals(name) ||
"AUDIO".Equals(name) ||
"VIDEO".Equals(name) ||
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
      return parseStream(DataIO.ToReader(bytes));
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='str'>The parameter <paramref name='str'/> is a text
    /// string.</param>
    /// <param name='state'>The parameter <paramref name='state'/> is a
    /// text string.</param>
    /// <param name='lst'>The parameter <paramref name='lst'/> is a text
    /// string.</param>
    /// <returns>An IList(string[]) object.</returns>
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
  return parser.parseTokens(state, lst);
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='str'>The parameter <paramref name='str'/> is a text
    /// string.</param>
    /// <param name='checkError'>Either <c>true</c> or <c>false</c>.</param>
    /// <returns>An IDocument object.</returns>
    public static IDocument FromString(string str, bool checkError) {
      byte[] bytes = DataUtilities.GetUtf8Bytes(str, true);
      return parseStream(DataIO.ToReader(bytes), checkError);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>The parameter <paramref name='name'/> is a text
    /// string.</param>
    /// <returns>An IElement object.</returns>
  public static IElement CreateHtmlElement(string name) {
       var valueElement = new Element();
            valueElement.setLocalName(name);
            valueElement.setNamespace(HtmlCommon.HTML_NAMESPACE);
            return valueElement;
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>The parameter <paramref name='name'/> is a text
    /// string.</param>
    /// <param name='namespaceName'>The parameter <paramref
    /// name='namespaceName'/> is a text string.</param>
    /// <returns>An IElement object.</returns>
  public static IElement CreateElement(string name, string namespaceName) {
       var valueElement = new Element();
            valueElement.setLocalName(name);
            valueElement.setNamespace(namespaceName);
            return valueElement;
  }

    public static string ToDebugString(IList<INode> nodes) {
return Document.toDebugString(nodes);
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='str'>The parameter <paramref name='str'/> is a text
    /// string.</param>
    /// <param name='context'>The parameter <paramref name='context'/> is
    /// a.upokecenter.html.IElement object.</param>
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
    /// a.upokecenter.html.IElement object.</param>
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
      IList<INode> ret = parser.checkError(checkError).parseFragment(context);
      return ret;
    }

    /// <summary>Parses an HTML document from an input stream, using
    /// "about:blank" as its address. @param stream an input stream @ if an
    /// I/O error occurs.</summary>
    /// <param name='stream'>The parameter <paramref name='stream'/> is a
    /// IReader object.</param>
    /// <returns>An IDocument object.</returns>
  public static IDocument parseStream(IReader stream) {
    return parseStream(stream, "about:blank");
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='stream'>The parameter <paramref name='stream'/> is a
    /// IReader object.</param>
    /// <param name='checkError'>The parameter <paramref
    /// name='checkError'/> is either <c>true</c> or <c>false</c>.</param>
    /// <returns>An IDocument object.</returns>
  public static IDocument parseStream(IReader stream, bool checkError) {
    return parseStream(stream, "about:blank", "text/html", null, checkError);
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='stream'>The parameter <paramref name='stream'/> is a
    /// IReader object.</param>
    /// <param name='address'>The parameter <paramref name='address'/> is a
    /// text string.</param>
    /// <returns>An IDocument object.</returns>
  public static IDocument parseStream(
    IReader stream,
    string address) {
    return parseStream(stream, address, "text/html");
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='stream'>The parameter <paramref name='stream'/> is a
    /// IReader object.</param>
    /// <param name='address'>The parameter <paramref name='address'/> is a
    /// text string.</param>
    /// <param name='contentType'>The parameter <paramref
    /// name='contentType'/> is a text string.</param>
    /// <returns>An IDocument object.</returns>
  public static IDocument parseStream(
    IReader stream,
    string address,
    string contentType) {
    return parseStream(stream, address, contentType, null);
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
  public static IDocument parseStream(
    IReader stream,
    string address,
    string contentType,
    string contentLang) {
return parseStream(stream, address, contentType, contentLang, false);
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
    /// <param name='stream'>The parameter <paramref name='stream'/> is a
    /// IReader object.</param>
    /// <param name='address'>The parameter <paramref name='address'/> is a
    /// text string.</param>
    /// <param name='contentType'>The parameter <paramref
    /// name='contentType'/> is a text string.</param>
    /// <param name='contentLang'>The parameter <paramref
    /// name='contentLang'/> is a text string.</param>
    /// <param name='checkError'>Either <c>true</c> or <c>false</c>.</param>
    /// <returns>An IDocument object.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='stream'/> or <paramref name='address'/> or <paramref
    /// name='contentType'/> is null.</exception>
  public static IDocument parseStream(
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
    if (mediatype.Equals("text/html")) {
        // TODO: add lang (from Content-Language?)
      var parser = new HtmlParser(stream, address, charset, contentLang);
      IDocument docret = parser.checkError(checkError).parse();
      return docret;
    } else if (mediatype.Equals("application/xhtml+xml") ||
mediatype.Equals("application/xml") ||
mediatype.Equals("image/svg+xml") ||
mediatype.Equals("text/xml")) {
      throw new NotSupportedException();
// var parser = new XhtmlParser(stream, address, charset, contentLang);
 // return parser.parse();
    } else {
 throw new ArgumentException("content type not supported: " + mediatype);
}
  }
}
}
