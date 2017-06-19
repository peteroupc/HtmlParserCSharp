namespace com.upokecenter.html.data {
using System;
using System.Collections.Generic;
using System.Text;
using com.upokecenter.html;
using com.upokecenter.util;

    /// <summary>* Implements Exclusive XML Canonicalization as specified
    /// at: http://www.w3.org/TR/xml-exc-c14n/ @author Peter.</summary>
sealed class ExclusiveCanonicalXML {
  private sealed class AttrComparer : IComparer<IAttr> {
    public int Compare(IAttr arg0, IAttr arg1) {
      string namespace1 = String.IsNullOrEmpty(arg0.getPrefix()) ?
          "" : arg0.getNamespaceURI();
      string namespace2 = String.IsNullOrEmpty(arg1.getPrefix()) ?
          "" : arg1.getNamespaceURI();
      // compare _namespace URIs (attributes without a prefix
      // are considered to have no _namespace URI)
      int cmp = StringUtility.codePointCompare(namespace1, namespace2);
      if (cmp == 0) {
        // then compare their local names
        cmp = StringUtility.codePointCompare(arg0.getLocalName(),
            arg1.getLocalName());
      }
      return cmp;
    }
  }
  private sealed class NamespaceAttr : IAttr {
    string prefix;
    string localName;
    string value;
    string name;
    public NamespaceAttr(string prefix, string value) {
      if (prefix.Length == 0) {
        this.prefix="";
        this.localName="xmlns";
        this.value = value;
        this.name="xmlns";
      } else {
        this.prefix="xmlns";
        this.localName = prefix;
        this.name="xmlns:"+value;
        this.value = value;
      }
    }
    public string getLocalName() {
      return localName;
    }
    public string getName() {
      return name;
    }
    public string getNamespaceURI() {
      return "http://www.w3.org/2000/xmlns/";
    }
    public string getPrefix() {
      return prefix;
    }
    public string getValue() {
      return value;
    }
  }

  private sealed class NamespaceAttrComparer : IComparer<IAttr> {
    public int Compare(IAttr arg0, IAttr arg1) {
      return StringUtility.codePointCompare(arg0.getName(), arg1.getName());
    }
  }

  private static readonly IComparer<IAttr> attrComparer = new AttrComparer();
  private static readonly IComparer<IAttr> attrNamespaceComparer = new
    NamespaceAttrComparer();

  public static string canonicalize(
      INode node,
      bool includeRoot,
      IDictionary<string, string> prefixList
) {
    return canonicalize(node, includeRoot, prefixList, false);
  }
  public static string canonicalize(
      INode node,
      bool includeRoot,
      IDictionary<string, string> prefixList,
      bool withComments) {
    if ((node) == null) {
 throw new ArgumentNullException("node");
}
    var builder = new StringBuilder();
IList<IDictionary<string, string>> stack = new
      List<IDictionary<string, string>>();
    prefixList = prefixList ?? (new PeterO.Support.LenientDictionary<string,
      string>());
      foreach (var prefix in prefixList.Keys) {
        string nsvalue = prefixList[prefix];
        checkNamespacePrefix(prefix, nsvalue);
    }
    PeterO.Support.LenientDictionary<string, string> item = new
      PeterO.Support.LenientDictionary<string, string>();
    stack.Add(item);
    if (node is IDocument) {
      var beforeElement = true;
      foreach (var child in node.getChildNodes()) {
        if (child is IElement) {
          beforeElement = false;
          canonicalize(child, builder, stack, prefixList, true, withComments);
     } else if (withComments || child.getNodeType() !=
          NodeType.COMMENT_NODE) {
          canonicalizeOutsideElement(child, builder, beforeElement);
        }
      }
    } else if (includeRoot) {
      canonicalize(node, builder, stack, prefixList, true, withComments);
    } else {
      foreach (var child in node.getChildNodes()) {
        canonicalize(child, builder, stack, prefixList, true, withComments);
      }
    }
    return builder.ToString();
  }
  private static void canonicalize(
      INode node,
      StringBuilder builder,
      IList<IDictionary<string, string>> namespaceStack,
      IDictionary<string, string> prefixList,
      bool addPrefixes,
      bool withComments) {
    int nodeType = node.getNodeType();
    if (nodeType == NodeType.COMMENT_NODE) {
      if (withComments) {
        builder.Append("<!--");
        builder.Append(((IComment)node).getData());
        builder.Append("-->");
      }
    } else if (nodeType == NodeType.PROCESSING_INSTRUCTION_NODE) {
      builder.Append("<?");
      builder.Append(((IProcessingInstruction)node).getTarget());
      string data=((IProcessingInstruction)node).getData();
      if (data.Length>0) {
        builder.Append(' ');
        builder.Append(data);
      }
      builder.Append("?>");
    } else if (nodeType == NodeType.ELEMENT_NODE) {
      IElement e=((IElement)node);
  IDictionary<string, string>
        nsRendered = namespaceStack[namespaceStack.Count-1];
      var copied = false;
      builder.Append('<');
      if (e.getPrefix() != null && e.getPrefix().Length>0) {
        builder.Append(e.getPrefix());
        builder.Append(':');
      }
      builder.Append(e.getLocalName());
      var attrs = new List<IAttr>();
      ISet<string> declaredNames = null;
      if (addPrefixes && prefixList.Count>0) {
        declaredNames = new HashSet<string>();
      }
      foreach (var attr in e.getAttributes()) {
        string name = attr.getName();
        string nsvalue = null;
        if ("xmlns".Equals(name)) {
          attrs.Add(attr);  // add default namespace
            if (declaredNames != null) {
            declaredNames.Add("");
          }
          nsvalue = attr.getValue();
          checkNamespacePrefix("",nsvalue);
} else if (name.StartsWith("xmlns:",StringComparison.Ordinal) &&
          name.Length>6) {
          attrs.Add(attr);  // add prefix namespace
            if (declaredNames != null) {
            declaredNames.Add(attr.getLocalName());
          }
          nsvalue = attr.getValue();
          checkNamespacePrefix(attr.getLocalName(), nsvalue);
        }
      }
      if (declaredNames != null) {
        // add declared prefixes to list
        foreach (var prefix in prefixList.Keys) {
          if (prefix == null || declaredNames.Contains(prefix)) {
            continue;
          }
          string value = prefixList[prefix];
          if (value == null) {
            value="";
          }
          attrs.Add(new NamespaceAttr(prefix, value));
        }
      }
      attrs.Sort(attrNamespaceComparer);
      foreach (var attr in attrs) {
        string prefix = attr.getLocalName();
        if (attr.getPrefix().Length == 0) {
          prefix="";
        }
        string value = attr.getValue();
        bool isEmpty = String.IsNullOrEmpty(prefix);
        bool isEmptyDefault=(isEmpty && String.IsNullOrEmpty(value));
        var renderNamespace = false;
        if (isEmptyDefault) {
          // condition used for Canonical XML
          //renderNamespace=(
          // (e.getParentNode() is IElement) &&
          //
  // !String.IsNullOrEmpty(((IElement)e.getParentNode()).getAttribute("xmlns"
          //))
          //);

          // changed condition for Exclusive XML Canonicalization
          renderNamespace=(isVisiblyUtilized(e,"") ||
              prefixList.ContainsKey("")) && nsRendered.ContainsKey("");
        } else {
          string renderedValue = nsRendered[prefix];
       renderNamespace=(renderedValue == null ||
            !renderedValue.Equals(value));
          // added condition for Exclusive XML Canonicalization
          renderNamespace = renderNamespace && (isVisiblyUtilized(e, prefix) ||
              prefixList.ContainsKey(prefix));
        }
        if (renderNamespace) {
          renderAttribute(builder,
              (isEmpty ? null : "xmlns"),
              (isEmpty ? "xmlns" : prefix),
              value);
          if (!copied) {
            copied = true;
    nsRendered = new
              PeterO.Support.LenientDictionary<string, string>(nsRendered);
          }
          nsRendered.Add(prefix, value);
        }
      }
      namespaceStack.Add(nsRendered);
      attrs.Clear();
      // All other attributes
      foreach (var attr in e.getAttributes()) {
        string name = attr.getName();
        if (!("xmlns".Equals(name) ||
       (name.StartsWith("xmlns:",StringComparison.Ordinal) &&
              name.Length>6))) {
          // non-_namespace node
          attrs.Add(attr);
        }
      }
      attrs.Sort(attrComparer);
      foreach (var attr in attrs) {
        renderAttribute(builder,
            attr.getPrefix(), attr.getLocalName(), attr.getValue());
      }
      builder.Append('>');
      foreach (var child in node.getChildNodes()) {
  canonicalize(child, builder, namespaceStack, prefixList, false, withComments);
      }
      namespaceStack.RemoveAt(namespaceStack.Count-1);
      builder.Append("</");
      if (e.getPrefix() != null && e.getPrefix().Length>0) {
        builder.Append(e.getPrefix());
        builder.Append(':');
      }
      builder.Append(e.getLocalName());
      builder.Append('>');
    } else if (nodeType == NodeType.TEXT_NODE) {
      string comment=((IText)node).getData();
      for (int i = 0;i<comment.Length; ++i) {
        char c = comment[i];
        if (c == 0x0d) {
          builder.Append("&#xD;");
        } else if (c=='>') {
          builder.Append("&gt;");
        } else if (c=='<') {
          builder.Append("&lt;");
        } else if (c=='&') {
          builder.Append("&amp;");
        } else {
          builder.Append(c);
        }
      }
    }
  }

  private static void canonicalizeOutsideElement(
      INode node,
      StringBuilder builder,
      bool beforeDocument) {
    int nodeType = node.getNodeType();
    if (nodeType == NodeType.COMMENT_NODE) {
      if (!beforeDocument) {
        builder.Append('\n');
      }
      builder.Append("<!--");
      builder.Append(((IComment)node).getData());
      builder.Append("-->");
      if (beforeDocument) {
        builder.Append('\n');
      }
    } else if (nodeType == NodeType.PROCESSING_INSTRUCTION_NODE) {
      if (!beforeDocument) {
        builder.Append('\n');
      }
      builder.Append("<?");
      builder.Append(((IProcessingInstruction)node).getTarget());
      string data=((IProcessingInstruction)node).getData();
      if (data.Length>0) {
        builder.Append(' ');
        builder.Append(data);
      }
      builder.Append("?>");
      if (beforeDocument) {
        builder.Append('\n');
      }
    }
  }

  private static void checkNamespacePrefix(string prefix, string nsvalue) {
    if (prefix.Equals("xmlns")) {
 throw new ArgumentException("'xmlns' _namespace declared");
}
    if (prefix.Equals("xml") && !"http://www.w3.org/XML/1998/namespace"
      .Equals(nsvalue)) {
 throw new ArgumentException("'xml' bound to wrong namespace name");
}
    if (!"xml" .Equals(prefix) && "http://www.w3.org/XML/1998/namespace"
      .Equals(nsvalue)) {
 throw new ArgumentException("'xml' bound to wrong namespace name");
}
    if ("http://www.w3.org/2000/xmlns/".Equals(nsvalue)) {
 throw new ArgumentException("'prefix' bound to xmlns namespace name");
}
    if (!String.IsNullOrEmpty(nsvalue)) {
      if (!URIUtility.hasSchemeForURI(nsvalue)) {
 throw new ArgumentException(nsvalue+" is not a valid namespace URI.");
}
    } else if (!"".Equals(prefix)) {
 throw new ArgumentException("can't undeclare a prefix");
}
  }

  private static bool isVisiblyUtilized(IElement element, string s) {
    string prefix = element.getPrefix();
    if (prefix == null) {
      prefix="";
    }
    if (s.Equals(prefix)) {
 return true;
}
    if (s.Length>0) {
      foreach (var attr in element.getAttributes()) {
        prefix = attr.getPrefix();
        if (prefix == null) {
          continue;
        }
        if (s.Equals(prefix)) {
 return true;
}
      }
    }
    return false;
  }

  private static void renderAttribute(StringBuilder builder,
      string prefix, string name, string value) {
    builder.Append(' ');
    if (!String.IsNullOrEmpty(prefix)) {
      builder.Append(prefix);
      builder.Append(":");
    }
    builder.Append(name);
    builder.Append("=\"");
    for (int i = 0;i<value.Length; ++i) {
      char c = value[i];
      if (c == 0x0d) {
        builder.Append("&#xD;");
      } else if (c == 0x09) {
        builder.Append("&#x9;");
      } else if (c == 0x0a) {
        builder.Append("&#xA;");
      } else if (c=='"') {
        builder.Append("&#x22;");
      } else if (c=='<') {
        builder.Append("&lt;");
      } else if (c=='&') {
        builder.Append("&amp;");
      } else {
        builder.Append(c);
      }
    }
    builder.Append('"');
  }

  private ExclusiveCanonicalXML() {}
}
}
