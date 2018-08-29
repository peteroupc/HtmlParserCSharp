using System;

namespace com.upokecenter.html {
  using com.upokecenter.util;

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

    private static readonly string
        XMLNS_NAMESPACE = "http://www.w3.org/2000/xmlns/";

    public static string resolveURL(INode node, string url, string _base) {
 string encoding = ((node is IDocument) ? ((IDocument)node).getCharacterSet() :
   node.getOwnerDocument().getCharacterSet());
      if ("utf-16be".Equals(encoding) || "utf-16le".Equals(encoding)) {
        encoding = "utf-8";
      }
      _base = _base ?? node.getBaseURI();
      URL resolved = URL.parse(url, URL.parse(_base), encoding, true);
      return (resolved == null) ? _base : resolved.ToString();
    }
  }
}
