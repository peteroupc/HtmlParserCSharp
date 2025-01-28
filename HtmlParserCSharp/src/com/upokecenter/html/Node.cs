using System;
using System.Collections.Generic;
using System.Text;
using Com.Upokecenter.Util;
/*

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

namespace Com.Upokecenter.Html {
  internal class Node : INode {
    private IList<INode> childNodes;
    private INode parentNode = null;
    private IDocument ownerDocument = null;

    private int valueNodeType;

    private string baseURI = null;

    public Node(int valueNodeType) {
      this.valueNodeType = valueNodeType;
      this.childNodes = new List<INode>();
    }

    public void AppendChild(INode node) {
      if (node == this) {
        throw new ArgumentException();
      }
      ((Node)node).parentNode = this;
      ((Node)node).ownerDocument = (this is IDocument) ? (IDocument)this :
        this.ownerDocument;
      this.childNodes.Add(node);
    }

    private void FragmentSerializeInner(
      INode current,
      StringBuilder builder) {
      if (current.GetNodeType() == NodeType.ELEMENT_NODE) {
        var e = (IElement)current;
        string tagname = e.GetTagName();
        string namespaceURI = e.GetNamespaceURI();
        if (HtmlCommon.HTML_NAMESPACE.Equals(namespaceURI,
          StringComparison.Ordinal) ||
          HtmlCommon.SVG_NAMESPACE.Equals(namespaceURI,
            StringComparison.Ordinal) ||
          HtmlCommon.MATHML_NAMESPACE.Equals(namespaceURI,
            StringComparison.Ordinal)) {
          tagname = e.GetLocalName();
        }
        builder.Append('<');
        builder.Append(tagname);
        foreach (var attr in e.GetAttributes()) {
          namespaceURI = attr.GetNamespaceURI();
          builder.Append(' ');
          if (namespaceURI == null || namespaceURI.Length == 0) {
            builder.Append(attr.GetLocalName());
          } else if (namespaceURI.Equals(HtmlCommon.XML_NAMESPACE,
            StringComparison.Ordinal)) {
            builder.Append("xml:");
            builder.Append(attr.GetLocalName());
          } else if (namespaceURI.Equals(
            "http://www.w3.org/2000/xmlns/",
            StringComparison.Ordinal)) {
            if (!"xmlns".Equals(attr.GetLocalName(),
              StringComparison.Ordinal)) {
              builder.Append("xmlns:");
            }
            builder.Append(attr.GetLocalName());
          } else if (namespaceURI.Equals(HtmlCommon.XLINK_NAMESPACE,
            StringComparison.Ordinal)) {
            builder.Append("xlink:");
            builder.Append(attr.GetLocalName());
          } else {
            builder.Append(attr.GetName());
          }
          builder.Append("=\"");
          string value = attr.GetValue();
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
        if (HtmlCommon.HTML_NAMESPACE.Equals(namespaceURI,
          StringComparison.Ordinal)) {
          string localName = e.GetLocalName();
          if ("area".Equals(localName, StringComparison.Ordinal) ||
            "base".Equals(localName, StringComparison.Ordinal) ||
            "basefont".Equals(localName, StringComparison.Ordinal) ||
            "bgsound".Equals(localName, StringComparison.Ordinal) ||
            "br".Equals(localName, StringComparison.Ordinal) ||
            "col".Equals(localName, StringComparison.Ordinal) ||
            "embed".Equals(localName, StringComparison.Ordinal) ||
            "frame".Equals(localName, StringComparison.Ordinal) ||
            "hr".Equals(localName, StringComparison.Ordinal) ||
            "img".Equals(localName, StringComparison.Ordinal) ||
            "input".Equals(localName, StringComparison.Ordinal) ||
            "keygen".Equals(localName, StringComparison.Ordinal) ||
            "link".Equals(localName, StringComparison.Ordinal) ||
            "menuitem".Equals(localName, StringComparison.Ordinal) ||
            "meta".Equals(localName, StringComparison.Ordinal) ||
            "param".Equals(localName, StringComparison.Ordinal) ||
            "source".Equals(localName, StringComparison.Ordinal) ||
            "track".Equals(localName, StringComparison.Ordinal) ||
            "wbr".Equals(localName, StringComparison.Ordinal)) {
            return;
          }
          if ("pre".Equals(localName, StringComparison.Ordinal) ||
            "textarea".Equals(localName, StringComparison.Ordinal) ||
            "listing".Equals(localName, StringComparison.Ordinal)) {
            foreach (var node in e.GetChildNodes()) {
              if (node.GetNodeType() == NodeType.TEXT_NODE) {
                string nodeData = ((IText)node).GetData();
                if (nodeData.Length > 0 && nodeData[0] == '\n') {
                  builder.Append('\n');
                }
              }
            }
          }
        }
        // Recurse
        foreach (var child in e.GetChildNodes()) {
          this.FragmentSerializeInner(child, builder);
        }
        builder.Append("</");
        builder.Append(tagname);
        builder.Append(">");
      } else if (current.GetNodeType() == NodeType.TEXT_NODE) {
        INode parent = current.GetParentNode();
        if (parent is IElement &&
          HtmlCommon.HTML_NAMESPACE.Equals((
          (IElement)parent).GetNamespaceURI(),
          StringComparison.Ordinal)) {
          string localName = ((IElement)parent).GetLocalName();
          if ("script".Equals(localName, StringComparison.Ordinal) ||
            "style".Equals(localName, StringComparison.Ordinal) ||
            "script".Equals(localName, StringComparison.Ordinal) ||
            "xmp".Equals(localName, StringComparison.Ordinal) ||
            "iframe".Equals(localName, StringComparison.Ordinal) ||
            "noembed".Equals(localName, StringComparison.Ordinal) ||
            "noframes".Equals(localName, StringComparison.Ordinal) ||
            "plaintext".Equals(localName, StringComparison.Ordinal)) {
            builder.Append(((IText)current).GetData());
          } else {
            string value = ((IText)current).GetData();
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
      } else if (current.GetNodeType() == NodeType.COMMENT_NODE) {
        builder.Append("<!--");
        builder.Append(((IComment)current).GetData());
        builder.Append("-->");
      } else if (current.GetNodeType() == NodeType.DOCUMENT_TYPE_NODE) {
        builder.Append("<!DOCTYPE ");
        builder.Append(((IDocumentType)current).GetName());
        builder.Append(">");
      } else if (current.GetNodeType() ==
        NodeType.PROCESSING_INSTRUCTION_NODE) {
        builder.Append("<?");
        builder.Append(((IProcessingInstruction)current).GetTarget());
        builder.Append(' ');
        builder.Append(((IProcessingInstruction)current).GetData());
        builder.Append(">"); // NOTE: may be erroneous
      }
    }

    public virtual string GetBaseURI() {
      INode parent = this.GetParentNode();
      if (this.baseURI == null) {
        if (parent == null) {
          return "about:blank";
        } else {
          return parent.GetBaseURI();
        }
      } else {
        if (parent == null) {
          return this.baseURI;
        } else {
          URL ret = URL.Parse(this.baseURI, URL.Parse(parent.GetBaseURI()));
          return (ret == null) ? parent.GetBaseURI() : ret.ToString();
        }
      }
    }

    public IList<INode> GetChildNodes() {
      var cn = new List<INode>();
      for (var i = 0; i < this.childNodes.Count; ++i) {
        cn.Add(this.childNodes[i]);
      }
      return cn;
    }

    internal IList<INode> GetChildNodesInternal() {
      return this.childNodes;
    }

    protected internal string GetInnerHtmlInternal() {
      var builder = new StringBuilder();
      foreach (var child in this.GetChildNodes()) {
        this.FragmentSerializeInner(child, builder);
      }
      return builder.ToString();
    }

    public virtual string GetLanguage() {
      INode parent = this.GetParentNode();
      if (parent == null) {
        parent = this.GetOwnerDocument();
        return (parent == null) ? String.Empty : parent.GetLanguage();
      } else {
        return parent.GetLanguage();
      }
    }

    public virtual string GetNodeName() {
      return String.Empty;
    }

    public int GetNodeType() {
      return this.valueNodeType;
    }

    public virtual IDocument GetOwnerDocument() {
      return this.ownerDocument;
    }

    public INode GetParentNode() {
      return this.parentNode;
    }

    public virtual string GetTextContent() {
      return null;
    }

    public void InsertBefore(Node child, Node sibling) {
      if (sibling == null) {
        this.AppendChild(child);
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

    public void RemoveChild(INode node) {
      ((Node)node).parentNode = null;
      IList<INode> cn = this.childNodes;
      cn.Remove(node);
    }

    internal void SetBaseURI(string value) {
      INode parent = this.GetParentNode();
      if (parent == null) {
        this.baseURI = value;
      } else {
        string val = URL.Parse(value, URL.Parse(
          parent.GetBaseURI())).ToString();
        this.baseURI = (val == null) ? parent.GetBaseURI() : val.ToString();
      }
    }

    internal void SetOwnerDocument(IDocument document) {
      this.ownerDocument = document;
    }

    internal virtual string ToDebugString() {
      return null;
    }

    public override string ToString() {
      return this.GetNodeName();
    }
  }
}
