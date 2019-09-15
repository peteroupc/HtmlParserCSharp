using System;
using System.Collections.Generic;
using Com.Upokecenter.util;

namespace Com.Upokecenter.Html {
internal static class HtmlCommon {
    public const string HTML_NAMESPACE =
     "http://www.w3.org/1999/xhtml";

    public const string MATHML_NAMESPACE =
     "http://www.w3.org/1998/Math/MathML";

    public const string SVG_NAMESPACE = "http://www.w3.org/2000/svg";

    public const string XLINK_NAMESPACE = "http://www.w3.org/1999/xlink";

    public static readonly string
      XML_NAMESPACE = "http://www.w3.org/XML/1998/namespace";

    public static readonly string
        XMLNS_NAMESPACE = "http://www.w3.org/2000/xmlns/";

    internal static bool IsHtmlElement(IElement ie, string name) {
      return ie != null && name.Equals(ie.getLocalName(),
  StringComparison.Ordinal) &&
          HtmlCommon.HTML_NAMESPACE.Equals(ie.getNamespaceURI(),
  StringComparison.Ordinal);
    }

    internal static bool IsMathMLElement(IElement ie, string name) {
      return ie != null && name.Equals(ie.getLocalName(),
  StringComparison.Ordinal) &&
        HtmlCommon.MATHML_NAMESPACE.Equals(ie.getNamespaceURI(),
  StringComparison.Ordinal);
    }

    internal static bool IsSvgElement(IElement ie, string name) {
      return ie != null && name.Equals(ie.getLocalName(),
  StringComparison.Ordinal) &&
         HtmlCommon.SVG_NAMESPACE.Equals(ie.getNamespaceURI(),
  StringComparison.Ordinal);
    }

    public static string ResolveURL(INode node, string url, string _base) {
 string encoding = (node is IDocument) ? ((IDocument)node).getCharset() :
   node.getOwnerDocument().getCharset();
   if ("utf-16be".Equals(encoding, StringComparison.Ordinal) ||
"utf-16le".Equals(encoding, StringComparison.Ordinal)) {
        encoding = "utf-8";
      }
      _base = _base ?? node.getBaseURI();
      URL resolved = URL.parse(url, URL.parse(_base), encoding, true);
      return (resolved == null) ? _base : resolved.ToString();
    }
  }
}
