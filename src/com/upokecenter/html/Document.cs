using System;
using System.Collections.Generic;
using System.Text;
using PeterO;
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
internal class Document : Node, IDocument {
    internal DocumentType Doctype { get; set; }

    internal string Encoding { get; set; }

  private DocumentMode docmode = DocumentMode.NoQuirksMode;

    internal string DefaultLanguage { get; set; }

    internal string Address { get; set; }

    internal Document() : base(NodeType.DOCUMENT_NODE) {
  }

  private void collectElements(INode c, string s, IList<IElement> nodes) {
    if (c.getNodeType() == NodeType.ELEMENT_NODE) {
      var e = (IElement)c;
      if (s == null || e.getLocalName().Equals(s)) {
        nodes.Add(e);
      }
    }
    foreach (var node in c.getChildNodes()) {
      this.collectElements(node, s, nodes);
    }
  }

  private void collectElementsHtml(
  INode c,
  string s,
  string valueSLowercase,
  IList<IElement> nodes) {
    if (c.getNodeType() == NodeType.ELEMENT_NODE) {
      var e = (IElement)c;
      if (s == null) {
        nodes.Add(e);
      } else if (HtmlCommon.HTML_NAMESPACE.Equals(e.getNamespaceURI()) &&
          e.getLocalName().Equals(valueSLowercase)) {
        nodes.Add(e);
      } else if (e.getLocalName().Equals(s)) {
        nodes.Add(e);
      }
    }
    foreach (var node in c.getChildNodes()) {
      this.collectElements(node, s, nodes);
    }
  }

  public string getCharset() {
    return (this.Encoding == null) ? "utf-8" : this.Encoding;
  }

  public IDocumentType getDoctype() {
    return this.Doctype;
  }

  public IElement getDocumentElement() {
    foreach (var node in this.getChildNodes()) {
      if (node is IElement) {
 return (IElement)node;
}
    }
    return null;
  }

  public IElement getElementById(string id) {
    if (id == null) {
 throw new ArgumentException();
}
    foreach (var node in this.getChildNodes()) {
      if (node is IElement) {
        if (id.Equals(((IElement)node).getId())) {
 return (IElement)node;
}
        IElement element = ((IElement)node).getElementById(id);
        if (element != null) {
 return element;
}
      }
    }
    return null;
  }

  public IList<IElement> getElementsByTagName(string tagName) {
    if (tagName == null) {
 throw new ArgumentException();
}
    if (tagName.Equals("*")) {
      tagName = null;
    }
    IList<IElement> ret = new List<IElement>();
    if (this.isHtmlDocument()) {
      this.collectElementsHtml(
  this,
  tagName,
  DataUtilities.ToLowerCaseAscii(tagName),
  ret);
    } else {
      this.collectElements(this, tagName, ret);
    }
    return ret;
  }

  public override string getLanguage() {
    return (this.DefaultLanguage == null) ? String.Empty :
      this.DefaultLanguage;
  }

  internal DocumentMode getMode() {
    return this.docmode;
  }

  public override string getNodeName() {
    return "#document";
  }

  public override IDocument getOwnerDocument() {
    return null;
  }

  public string getURL() {
    return this.Address;
  }

  internal bool isHtmlDocument() {
    return true;
  }

  internal void setMode(DocumentMode mode) {
    this.docmode = mode;
  }

    public override string ToString() {
      return this.toDebugString();
    }

    internal override string toDebugString() {
return toDebugString(this.getChildNodes());
}

    internal static string toDebugString(IList<INode> nodes) {
    var builder = new StringBuilder();
    foreach (var node in nodes) {
        string str = ((Node)node).toDebugString();
      if (str == null) {
        continue;
      }
      string[] strarray = StringUtility.splitAt(str, "\n");
      int len = strarray.Length;
      if (len > 0 && strarray[len - 1].Length == 0) {
        --len; // ignore trailing empty _string
      }
      for (int i = 0; i < len; ++i) {
        string el = strarray[i];
        builder.Append("| ");
        builder.Append(el.Replace("~~~~", "\n"));
        builder.Append("\n");
      }
    }
    return builder.ToString();
  }
}
}
