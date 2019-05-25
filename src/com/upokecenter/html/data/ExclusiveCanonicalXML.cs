using System;
using System.Collections.Generic;
using System.Text;
using PeterO;
using com.upokecenter.html;
using com.upokecenter.util;

namespace com.upokecenter.html.data {
    /// <include file='../../../../../docs.xml'
    /// path='docs/doc[@name="T:com.upokecenter.html.data.ExclusiveCanonicalXML"]/*'/>
sealed class ExclusiveCanonicalXML {
  private sealed class AttrComparer : IComparer<IAttr> {
    public int Compare(IAttr arg0, IAttr arg1) {
      string namespace1 = String.IsNullOrEmpty(arg0.getPrefix()) ?
          String.Empty : arg0.getNamespaceURI();
      string namespace2 = String.IsNullOrEmpty(arg1.getPrefix()) ?
          String.Empty : arg1.getNamespaceURI();
      // compare _namespace URIs (attributes without a valuePrefix
      // are considered to have no _namespace URI)
      int cmp = DataUtilities.CodePointCompare(namespace1, namespace2);
      if (cmp == 0) {
        // then compare their local names
        cmp = DataUtilities.CodePointCompare(
  arg0.getLocalName(),
  arg1.getLocalName());
      }
      return cmp;
    }
  }

  private sealed class NamespaceAttr : IAttr {
    private string valuePrefix;
    private string valueLocalName;
    private string value;
    private string valueName;

    public NamespaceAttr(string valuePrefix, string value) {
      if (valuePrefix.Length == 0) {
        this.valuePrefix = String.Empty;
        this.valueLocalName = "xmlns";
        this.value = value;
        this.valueName = "xmlns";
      } else {
        this.valuePrefix = "xmlns";
        this.valueLocalName = valuePrefix;
        this.valueName = "xmlns:" + value;
        this.value = value;
      }
    }

    public string getLocalName() {
      return this.valueLocalName;
    }

    public string getName() {
      return this.valueName;
    }

    public string getNamespaceURI() {
      return "http://www.w3.org/2000/xmlns/";
    }

    public string getPrefix() {
      return this.valuePrefix;
    }

    public string getValue() {
      return this.value;
    }
  }

  private sealed class NamespaceAttrComparer : IComparer<IAttr> {
    public int Compare(IAttr arg0, IAttr arg1) {
      return DataUtilities.CodePointCompare(arg0.getName(), arg1.getName());
    }
  }

  private static readonly IComparer<IAttr> ValueAttrComparer = new
    AttrComparer();

  private static readonly IComparer<IAttr> ValueAttrNamespaceComparer = new
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
    if (node == null) {
 throw new ArgumentNullException(nameof(node));
}
    var builder = new StringBuilder();
IList<IDictionary<string, string>> stack = new
      List<IDictionary<string, string>>();
    prefixList = prefixList ?? (new Dictionary<string, string>());
      foreach (var valuePrefix in prefixList.Keys) {
        string nsvalue = prefixList[valuePrefix];
        checkNamespacePrefix(valuePrefix, nsvalue);
    }
    Dictionary<string, string> item = new Dictionary<string, string>();
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
      string data = ((IProcessingInstruction)node).getData();
      if (data.Length > 0) {
        builder.Append(' ');
        builder.Append(data);
      }
      builder.Append("?>");
    } else if (nodeType == NodeType.ELEMENT_NODE) {
      var e = (IElement)node;
  IDictionary<string, string>
        valueNsRendered = namespaceStack[namespaceStack.Count - 1];
      var copied = false;
      builder.Append('<');
      if (e.getPrefix() != null && e.getPrefix().Length > 0) {
        builder.Append(e.getPrefix());
        builder.Append(':');
      }
      builder.Append(e.getLocalName());
      var attrs = new List<IAttr>();
      ISet<string> declaredNames = null;
      if (addPrefixes && prefixList.Count > 0) {
        declaredNames = new HashSet<string>();
      }
      foreach (var attr in e.getAttributes()) {
        string valueName = attr.getName();
        string nsvalue = null;
        if ("xmlns".Equals(valueName)) {
          attrs.Add(attr);  // add default namespace
            if (declaredNames != null) {
            declaredNames.Add(String.Empty);
          }
          nsvalue = attr.getValue();
          checkNamespacePrefix(String.Empty, nsvalue);
} else if (valueName.StartsWith("xmlns:", StringComparison.Ordinal) &&
          valueName.Length > 6) {
          attrs.Add(attr);  // add valuePrefix namespace
            if (declaredNames != null) {
            declaredNames.Add(attr.getLocalName());
          }
          nsvalue = attr.getValue();
          checkNamespacePrefix(attr.getLocalName(), nsvalue);
        }
      }
      if (declaredNames != null) {
        // add declared prefixes to list
        foreach (var valuePrefix in prefixList.Keys) {
          if (valuePrefix == null || declaredNames.Contains(valuePrefix)) {
            continue;
          }
          string value = prefixList[valuePrefix];
          value = value ?? String.Empty;
          attrs.Add(new NamespaceAttr(valuePrefix, value));
        }
      }
      attrs.Sort(ValueAttrNamespaceComparer);
      foreach (var attr in attrs) {
        string valuePrefix = attr.getLocalName();
        if (attr.getPrefix().Length == 0) {
          valuePrefix = String.Empty;
        }
        string value = attr.getValue();
        bool isEmpty = String.IsNullOrEmpty(valuePrefix);
        bool isEmptyDefault = isEmpty && String.IsNullOrEmpty(value);
        var renderNamespace = false;
        if (isEmptyDefault) {
          // condition used for Canonical XML
          // renderNamespace=(
          // (e.getParentNode() is IElement) &&
          //
  // !String.IsNullOrEmpty(((IElement)e.getParentNode()).getAttribute("xmlns"
          //))
          //);

          // changed condition for Exclusive XML Canonicalization
          renderNamespace = (isVisiblyUtilized(e, String.Empty) ||
              prefixList.ContainsKey(String.Empty)) &&
                valueNsRendered.ContainsKey(String.Empty);
        } else {
          string renderedValue = valueNsRendered[valuePrefix];
       renderNamespace = renderedValue == null || !renderedValue.Equals(value);
          // added condition for Exclusive XML Canonicalization
     renderNamespace = renderNamespace && (isVisiblyUtilized(e, valuePrefix) ||
            prefixList.ContainsKey(valuePrefix));
        }
        if (renderNamespace) {
          renderAttribute(
  builder,
  isEmpty ? null : "xmlns",
  isEmpty ? "xmlns" : valuePrefix,
  value);
          if (!copied) {
            copied = true;
    valueNsRendered = new Dictionary<string, string>(valueNsRendered);
          }
          valueNsRendered.Add(valuePrefix, value);
        }
      }
      namespaceStack.Add(valueNsRendered);
      attrs.Clear();
      // All other attributes
      foreach (var attr in e.getAttributes()) {
        string valueName = attr.getName();
        if (!("xmlns".Equals(valueName) ||
       (valueName.StartsWith("xmlns:", StringComparison.Ordinal) &&
              valueName.Length > 6))) {
          // non-_namespace node
          attrs.Add(attr);
        }
      }
      attrs.Sort(ValueAttrComparer);
      foreach (var attr in attrs) {
        renderAttribute(
  builder,
  attr.getPrefix(),
  attr.getLocalName(),
  attr.getValue());
      }
      builder.Append('>');
      foreach (var child in node.getChildNodes()) {
  canonicalize(child, builder, namespaceStack, prefixList, false, withComments);
      }
      namespaceStack.RemoveAt(namespaceStack.Count - 1);
      builder.Append("</");
      if (e.getPrefix() != null && e.getPrefix().Length > 0) {
        builder.Append(e.getPrefix());
        builder.Append(':');
      }
      builder.Append(e.getLocalName());
      builder.Append('>');
    } else if (nodeType == NodeType.TEXT_NODE) {
      string comment = ((IText)node).getData();
      for (int i = 0; i < comment.Length; ++i) {
        char c = comment[i];
        if (c == 0x0d) {
          builder.Append("&#xD;");
        } else if (c == '>') {
          builder.Append("&gt;");
        } else if (c == '<') {
          builder.Append("&lt;");
        } else if (c == '&') {
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
      string data = ((IProcessingInstruction)node).getData();
      if (data.Length > 0) {
        builder.Append(' ');
        builder.Append(data);
      }
      builder.Append("?>");
      if (beforeDocument) {
        builder.Append('\n');
      }
    }
  }

  private static void checkNamespacePrefix(string valuePrefix, string nsvalue) {
    if (valuePrefix.Equals("xmlns")) {
 throw new ArgumentException("'xmlns' _namespace declared");
}
    if (valuePrefix.Equals("xml") && !"http://www.w3.org/XML/1998/namespace"
      .Equals(nsvalue)) {
 throw new ArgumentException("'xml' bound to wrong namespace valueName");
}
    if (!"xml" .Equals(valuePrefix) && "http://www.w3.org/XML/1998/namespace"
      .Equals(nsvalue)) {
 throw new ArgumentException("'xml' bound to wrong namespace valueName");
}
    if ("http://www.w3.org/2000/xmlns/".Equals(nsvalue)) {
 throw new
   ArgumentException("'valuePrefix' bound to xmlns namespace valueName");
}
    if (!String.IsNullOrEmpty(nsvalue)) {
      if (!URIUtility.hasSchemeForURI(nsvalue)) {
 throw new ArgumentException(nsvalue + " is not a valid namespace URI.");
}
    } else if (!String.Empty.Equals(valuePrefix)) {
 throw new ArgumentException("can't undeclare a valuePrefix");
}
  }

  private static bool isVisiblyUtilized(IElement element, string s) {
    string valuePrefix = element.getPrefix();
    valuePrefix = valuePrefix ?? String.Empty;
    if (s.Equals(valuePrefix)) {
 return true;
}
    if (s.Length > 0) {
      foreach (var attr in element.getAttributes()) {
        valuePrefix = attr.getPrefix();
        if (valuePrefix == null) {
          continue;
        }
        if (s.Equals(valuePrefix)) {
 return true;
}
      }
    }
    return false;
  }

  private static void renderAttribute(
  StringBuilder builder,
  string valuePrefix,
  string valueName,
  string value) {
    builder.Append(' ');
    if (!String.IsNullOrEmpty(valuePrefix)) {
      builder.Append(valuePrefix);
      builder.Append(":");
    }
    builder.Append(valueName);
    builder.Append("=\"");
    for (int i = 0; i < value.Length; ++i) {
      char c = value[i];
      if (c == 0x0d) {
        builder.Append("&#xD;");
      } else if (c == 0x09) {
        builder.Append("&#x9;");
      } else if (c == 0x0a) {
        builder.Append("&#xA;");
      } else if (c == '"') {
        builder.Append("&#x22;");
      } else if (c == '<') {
        builder.Append("&lt;");
      } else if (c == '&') {
        builder.Append("&amp;");
      } else {
        builder.Append(c);
      }
    }
    builder.Append('"');
  }

  private ExclusiveCanonicalXML() {
}
}
}
