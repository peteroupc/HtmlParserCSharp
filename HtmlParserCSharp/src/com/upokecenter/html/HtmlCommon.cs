using System;
using System.Collections.Generic;
using Com.Upokecenter.Util;
using PeterO;

namespace Com.Upokecenter.Html {
  internal static class HtmlCommon {
    public const string HTML_NAMESPACE = "http://www.w3.org/1999/xhtml";

    public const string MATHML_NAMESPACE = "http://www.w3.org/1998/Math/MathML";

    public const string SVG_NAMESPACE = "http://www.w3.org/2000/svg";

    public const string XLINK_NAMESPACE = "http://www.w3.org/1999/xlink";

    public static readonly string
    XML_NAMESPACE = "http://www.w3.org/XML/1998/namespace";

    public static readonly string
    XMLNS_NAMESPACE = "http://www.w3.org/2000/xmlns/";

    internal static bool IsHtmlElement(IElement ie, string name) {
      return ie != null && name.Equals(ie.GetLocalName(),
        StringComparison.Ordinal) &&
        HtmlCommon.HTML_NAMESPACE.Equals(ie.GetNamespaceURI(),
        StringComparison.Ordinal);
    }

    internal static bool IsMathMLElement(IElement ie, string name) {
      return ie != null && name.Equals(ie.GetLocalName(),
        StringComparison.Ordinal) &&
        HtmlCommon.MATHML_NAMESPACE.Equals(ie.GetNamespaceURI(),
        StringComparison.Ordinal);
    }

    internal static bool IsSvgElement(IElement ie, string name) {
      return ie != null && name.Equals(ie.GetLocalName(),
        StringComparison.Ordinal) &&
        HtmlCommon.SVG_NAMESPACE.Equals(ie.GetNamespaceURI(),
        StringComparison.Ordinal);
    }

    public static string ResolveURLUtf8(INode node, string url, string _base) {
      _base = _base ?? node.GetBaseURI();
      // TODO: Use URL specification's version instead of RFC 3986
      return URIUtility.RelativeResolve(url, _base);
    }

    public static string ResolveURL(INode node, string url, string _base) {
      string encoding = (node is IDocument) ? ((IDocument)node).GetCharset() :
        node.GetOwnerDocument().GetCharset();
      if ("utf-16be".Equals(encoding, StringComparison.Ordinal) ||
        "utf-16le".Equals(encoding, StringComparison.Ordinal)) {
        encoding = "utf-8";
      }
      _base = _base ?? node.GetBaseURI();
      // TODO: Use URL specification's version instead of RFC 3986
      return URIUtility.RelativeResolve(url, _base);
    }
  }
}
