using System;
using System.Collections.Generic;
using PeterO;
using PeterO.Text;
using com.upokecenter.net;
using com.upokecenter.util;

namespace com.upokecenter.html {
    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="T:com.upokecenter.html.HtmlDocument"]/*'/>
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

        (HtmlDocument.resolveURL(
         node,
         href,
         null));
  }
    */

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.FromString(System.String)"]/*'/>
    public static IDocument FromString(string str) {
      byte[] bytes = DataUtilities.GetUtf8Bytes(str, true);
      return parseStream(DataIO.ToReader(bytes));
    }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.ParseTokens(System.String,System.String,System.String)"]/*'/>
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

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.FromString(System.String,System.Boolean)"]/*'/>
    public static IDocument FromString(string str, bool checkError) {
      byte[] bytes = DataUtilities.GetUtf8Bytes(str, true);
      return parseStream(DataIO.ToReader(bytes), checkError);
    }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.CreateHtmlElement(System.String)"]/*'/>
  public static IElement CreateHtmlElement(string name) {
       var valueElement = new Element();
            valueElement.setLocalName(name);
            valueElement.setNamespace(HtmlCommon.HTML_NAMESPACE);
            return valueElement;
  }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.CreateElement(System.String,System.String)"]/*'/>
  public static IElement CreateElement(string name, string namespaceName) {
       var valueElement = new Element();
            valueElement.setLocalName(name);
            valueElement.setNamespace(namespaceName);
            return valueElement;
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='nodes'>The parameter <paramref name='nodes'/> is not
    /// documented yet.</param>
    /// <returns>A string object.</returns>
    public static string ToDebugString(IList<INode> nodes) {
return Document.toDebugString(nodes);
  }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.FragmentFromString(System.String,com.upokecenter.html.IElement)"]/*'/>
  public static IList<INode> FragmentFromString(
  string str,
  IElement context) {
return FragmentFromString(str, context, false);
    }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.FragmentFromString(System.String,com.upokecenter.html.IElement,System.Boolean)"]/*'/>
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

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.parseStream(PeterO.IReader)"]/*'/>
  public static IDocument parseStream(IReader stream) {
    return parseStream(stream, "about:blank");
  }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.parseStream(PeterO.IReader,System.Boolean)"]/*'/>
  public static IDocument parseStream(IReader stream, bool checkError) {
    return parseStream(stream, "about:blank", "text/html", null, checkError);
  }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.parseStream(PeterO.IReader,System.String)"]/*'/>
  public static IDocument parseStream(
      IReader stream,
      string address) {
    return parseStream(stream, address, "text/html");
  }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.parseStream(PeterO.IReader,System.String,System.String)"]/*'/>
  public static IDocument parseStream(
      IReader stream,
      string address,
      string contentType) {
    return parseStream(stream, address, contentType, null);
  }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.parseStream(PeterO.IReader,System.String,System.String,System.String)"]/*'/>
  public static IDocument parseStream(
      IReader stream,
      string address,
      string contentType,
      string contentLang) {
return parseStream(stream, address, contentType, contentLang, false);
  }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.HtmlDocument.parseStream(PeterO.IReader,System.String,System.String,System.String,System.Boolean)"]/*'/>
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
