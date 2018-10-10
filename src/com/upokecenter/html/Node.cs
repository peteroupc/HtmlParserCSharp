using System;
using System.Collections.Generic;
using System.Text;
using com.upokecenter.util;
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
internal class Node : INode {
  private IList<INode> childNodes;
  private INode parentNode = null;
  private IDocument ownerDocument = null;

  private int ValueNodeType;

  private string baseURI = null;

  public Node(int ValueNodeType) {
    this.ValueNodeType = ValueNodeType;
    this.childNodes = new List<INode>();
  }

  public void appendChild(Node node) {
    if (node == this) {
 throw new ArgumentException();
}
    node.parentNode = this;
    node.ownerDocument = (this is IDocument) ? (IDocument)this :
      this.ownerDocument;
    this.childNodes.Add(node);
  }

  private void fragmentSerializeInner(
      INode current,
      StringBuilder builder) {
    if (current.getNodeType() == NodeType.ELEMENT_NODE) {
      var e = (IElement)current;
      string tagname = e.getTagName();
      string namespaceURI = e.getNamespaceURI();
      if (HtmlCommon.HTML_NAMESPACE.Equals(namespaceURI) ||
          HtmlCommon.SVG_NAMESPACE.Equals(namespaceURI) ||
          HtmlCommon.MATHML_NAMESPACE.Equals(namespaceURI)) {
        tagname = e.getLocalName();
      }
      builder.Append('<');
      builder.Append(tagname);
      foreach (var attr in e.getAttributes()) {
        namespaceURI = attr.getNamespaceURI();
        builder.Append(' ');
        if (namespaceURI == null || namespaceURI.Length == 0) {
          builder.Append(attr.getLocalName());
        } else if (namespaceURI.Equals(HtmlCommon.XML_NAMESPACE)) {
          builder.Append("xml:");
          builder.Append(attr.getLocalName());
        } else if (namespaceURI.Equals(
            "http://www.w3.org/2000/xmlns/")) {
          if (!"xmlns".Equals(attr.getLocalName())) {
            builder.Append("xmlns:");
          }
          builder.Append(attr.getLocalName());
        } else if (namespaceURI.Equals(HtmlCommon.XLINK_NAMESPACE)) {
          builder.Append("xlink:");
          builder.Append(attr.getLocalName());
        } else {
          builder.Append(attr.getName());
        }
        builder.Append("=\"");
        string value = attr.getValue();
        for (int i = 0; i < value.Length; ++i) {
          char c = value[i];
          if (c == '&') {
            builder.Append("&amp;");
          } else if (c == 0xa0) {
            builder.Append("&nbsp;");
          } else if (c == '"') {
            builder.Append("&#x22;");
          } else {
            builder.Append(c);
          }
        }
        builder.Append('"');
      }
      builder.Append('>');
      if (HtmlCommon.HTML_NAMESPACE.Equals(namespaceURI)) {
        string localName = e.getLocalName();
        if ("area".Equals(localName) ||
            "base".Equals(localName) || "basefont".Equals(localName) ||
            "bgsound".Equals(localName) || "br".Equals(localName) ||
            "col".Equals(localName) || "embed".Equals(localName) ||
            "frame".Equals(localName) || "hr".Equals(localName) ||
            "img".Equals(localName) || "input".Equals(localName) ||
            "keygen".Equals(localName) || "link".Equals(localName) ||
            "menuitem".Equals(localName) || "meta".Equals(localName) ||
            "param".Equals(localName) || "source".Equals(localName) ||
            "track".Equals(localName) || "wbr".Equals(localName)) {
 return;
}
        if ("pre".Equals(localName) ||
            "textarea".Equals(localName) || "listing".Equals(localName)) {
          foreach (var node in e.getChildNodes()) {
            if (node.getNodeType() == NodeType.TEXT_NODE &&
                ((IText)node).getData().Length > 0 &&
                ((IText)node).getData()[0] == '\n') {
              builder.Append('\n');
            }
          }
        }
      }
      // Recurse
      foreach (var child in e.getChildNodes()) {
        this.fragmentSerializeInner(child, builder);
      }
      builder.Append("</");
      builder.Append(tagname);
      builder.Append(">");
    } else if (current.getNodeType() == NodeType.TEXT_NODE) {
      INode parent = current.getParentNode();
      if (parent is IElement &&
HtmlCommon.HTML_NAMESPACE.Equals(((IElement)parent).getNamespaceURI())) {
        string localName = ((IElement)parent).getLocalName();
        if ("script".Equals(localName) || "style".Equals(localName) ||
            "script".Equals(localName) || "xmp".Equals(localName) ||
            "iframe".Equals(localName) || "noembed".Equals(localName) ||
            "noframes".Equals(localName) || "plaintext".Equals(localName)) {
          builder.Append(((IText)current).getData());
        } else {
          string value = ((IText)current).getData();
          for (int i = 0; i < value.Length; ++i) {
            char c = value[i];
            if (c == '&') {
              builder.Append("&amp;");
            } else if (c == 0xa0) {
              builder.Append("&nbsp;");
            } else if (c == '<') {
              builder.Append("&lt;");
            } else if (c == '>') {
              builder.Append("&gt;");
            } else {
              builder.Append(c);
            }
          }
        }
      }
    } else if (current.getNodeType() == NodeType.COMMENT_NODE) {
      builder.Append("<!--");
      builder.Append(((IComment)current).getData());
      builder.Append("-->");
    } else if (current.getNodeType() == NodeType.DOCUMENT_TYPE_NODE) {
      builder.Append("<!DOCTYPE ");
      builder.Append(((IDocumentType)current).getName());
      builder.Append(">");
    } else if (current.getNodeType() == NodeType.PROCESSING_INSTRUCTION_NODE) {
      builder.Append("<?");
      builder.Append(((IProcessingInstruction)current).getTarget());
      builder.Append(' ');
      builder.Append(((IProcessingInstruction)current).getData());
      builder.Append(">");  // NOTE: may be erroneous
    }
  }

  public virtual string getBaseURI() {
    INode parent = this.getParentNode();
    if (this.baseURI == null) {
      if (parent == null) {
 return "about:blank";
} else {
 return parent.getBaseURI();
}
    } else {
      if (parent == null) {
 return this.baseURI;
} else {
        URL ret = URL.parse(this.baseURI, URL.parse(parent.getBaseURI()));
        return (ret == null) ? parent.getBaseURI() : ret.ToString();
      }
    }
  }

  public IList<INode> getChildNodes() {
    return new List<INode>(this.childNodes);
  }

  internal IList<INode> getChildNodesInternal() {
    return this.childNodes;
  }

  protected internal string getInnerHtmlInternal() {
    var builder = new StringBuilder();
    foreach (var child in this.getChildNodes()) {
      this.fragmentSerializeInner(child, builder);
    }
    return builder.ToString();
  }

  public virtual string getLanguage() {
    INode parent = this.getParentNode();
    if (parent == null) {
      parent = this.getOwnerDocument();
      return (parent == null) ? String.Empty : parent.getLanguage();
    } else {
 return parent.getLanguage();
}
  }

  public virtual string getNodeName() {
    return String.Empty;
  }

  public int getNodeType() {
    return this.ValueNodeType;
  }

  public virtual IDocument getOwnerDocument() {
    return this.ownerDocument;
  }

  public INode getParentNode() {
    return this.parentNode;
  }

  public virtual string getTextContent() {
    return null;
  }

  public void insertBefore(Node child, Node sibling) {
    if (sibling == null) {
      this.appendChild(child);
      return;
    }
    if (this.childNodes.Count == 0) {
 throw new InvalidOperationException();
}
    int childNodesSize = this.childNodes.Count;
    for (int j = 0; j < childNodesSize; ++j) {
      if (this.childNodes[j].Equals(sibling)) {
        child.parentNode = this;
    child.ownerDocument = (child is IDocument) ? (IDocument)this :
          this.ownerDocument;
        this.childNodes.Insert(j, child);
        return;
      }
    }
    throw new ArgumentException();
  }

  public void removeChild(Node node) {
    node.parentNode = null;
    this.childNodes.Remove(node);
  }

  internal void setBaseURI(string value) {
    INode parent = this.getParentNode();
    if (parent == null) {
      this.baseURI = value;
    } else {
      string val = URL.parse(value, URL.parse(parent.getBaseURI())).ToString();
      this.baseURI = (val == null) ? parent.getBaseURI() : val.ToString();
    }
  }

  internal void setOwnerDocument(IDocument document) {
    this.ownerDocument = document;
  }

  internal virtual string toDebugString() {
    return null;
  }

  public override string ToString() {
    return this.getNodeName();
  }
}
}
