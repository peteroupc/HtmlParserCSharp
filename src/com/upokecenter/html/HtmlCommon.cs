using com.upokecenter.util;

  using System;

namespace com.upokecenter.html {
internal static class HtmlCommon {
    public static readonly string HTML_NAMESPACE =
     "http://www.w3.org/1999/xhtml";

    public static readonly string MATHML_NAMESPACE =
     "http://www.w3.org/1998/Math/MathML";

    public static readonly string SVG_NAMESPACE = "http://www.w3.org/2000/svg";

    public static readonly string XLINK_NAMESPACE =
          "http://www.w3.org/1999/xlink";

    public static readonly string
      XML_NAMESPACE = "http://www.w3.org/XML/1998/namespace";

    public static readonly string
        XMLNS_NAMESPACE = "http://www.w3.org/2000/xmlns/";

    internal static bool isHtmlElement(IElement ie, string name) {
      return name.Equals(ie.getLocalName()) &&
          HtmlCommon.HTML_NAMESPACE.Equals(ie.getNamespaceURI());
    }

    internal static bool isMathMLElement(IElement ie, string name) {
      return name.Equals(ie.getLocalName()) &&
        HtmlCommon.MATHML_NAMESPACE.Equals(ie.getNamespaceURI());
    }

    internal static bool isSvgElement(IElement ie, string name) {
      return name.Equals(ie.getLocalName()) &&
         HtmlCommon.SVG_NAMESPACE.Equals(ie.getNamespaceURI());
    }

    public static string resolveURL(INode node, string url, string _base) {
 string encoding = ((node is IDocument) ?
   ((IDocument)node).getCharset() :
   node.getOwnerDocument().getCharset());
      if ("utf-16be".Equals(encoding) || "utf-16le".Equals(encoding)) {
        encoding = "utf-8";
      }
      _base = _base ?? node.getBaseURI();
      URL resolved = URL.parse(url, URL.parse(_base), encoding, true);
      return (resolved == null) ? _base : resolved.ToString();
    }
  }
}
