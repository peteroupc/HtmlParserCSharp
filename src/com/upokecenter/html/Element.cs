using System;
using System.Collections.Generic;
using System.Text;
using Com.Upokecenter.util;
using PeterO;
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

namespace Com.Upokecenter.Html {
  internal class Element : Node, IElement {
    private sealed class AttributeNameComparator : IComparer<IAttr> {
      public int Compare(IAttr arg0, IAttr arg1) {
        string a = arg0.getName();
        string b = arg1.getName();
        return String.Compare(a, b, StringComparison.Ordinal);
      }
    }

    internal static Element FromToken(INameAndAttributes token) {
      return FromToken(token, HtmlCommon.HTML_NAMESPACE);
    }

    internal static Element FromToken(
      INameAndAttributes token,
      string _namespace) {
      var ret = new Element();
      ret.name = token.getName();
      ret.attributes = new List<Attr>();
      foreach (var attribute in token.getAttributes()) {
        ret.attributes.Add(new Attr(attribute));
      }
      ret._namespace = _namespace;
      return ret;
    }

    private string name;

    private string _namespace;

    private string prefix = null;

    private IList<Attr> attributes;

    internal Element() : base(NodeType.ELEMENT_NODE) {
      this.attributes = new List<Attr>();
    }

    public Element(string name) : base(NodeType.ELEMENT_NODE) {
      this.attributes = new List<Attr>();
      this.name = name;
    }

    internal void AddAttribute(Attr value) {
      this.attributes.Add(value);
    }

    private void CollectElements(INode c, string s, IList<IElement> nodes) {
      if (c.getNodeType() == NodeType.ELEMENT_NODE) {
        var e = (Element)c;
        if (s == null || e.getLocalName().Equals(s, StringComparison.Ordinal)) {
          nodes.Add(e);
        }
      }
      foreach (var node in c.getChildNodes()) {
        this.CollectElements(node, s, nodes);
      }
    }

    private void CollectElementsHtml(
    INode c,
    string s,
    string valueSLowercase,
    IList<IElement> nodes) {
      if (c.getNodeType() == NodeType.ELEMENT_NODE) {
        var e = (Element)c;
        if (s == null) {
          nodes.Add(e);
        } else if (HtmlCommon.HTML_NAMESPACE.Equals(e.getNamespaceURI(),
  StringComparison.Ordinal) &&
            e.getLocalName().Equals(valueSLowercase,
  StringComparison.Ordinal)) {
          nodes.Add(e);
        } else if (e.getLocalName().Equals(s, StringComparison.Ordinal)) {
          nodes.Add(e);
        }
      }
      foreach (var node in c.getChildNodes()) {
        this.CollectElements(node, s, nodes);
      }
    }

    public string getAttribute(string name) {
      foreach (var attr in this.getAttributes()) {
        if (attr.getName().Equals(name, StringComparison.Ordinal)) {
          return attr.getValue();
        }
      }
      return null;
    }

    public string getAttributeNS(string _namespace, string localName) {
      foreach (var attr in this.getAttributes()) {
        if ((localName == null ? attr.getLocalName() == null :
          localName.Equals(attr.getLocalName(), StringComparison.Ordinal)) &&
            (_namespace == null ? attr.getNamespaceURI() == null :
              _namespace.Equals(attr.getNamespaceURI(),
  StringComparison.Ordinal)))
          return attr.getValue();
      }
      return null;
    }

    public IList<IAttr> getAttributes() {
      return new List<IAttr>(this.attributes);
    }

    public IElement getElementById(string id) {
      if (id == null) {
        throw new ArgumentException();
      }
      foreach (var node in this.getChildNodes()) {
        if (node is IElement) {
          if (id.Equals(((IElement)node).getId(), StringComparison.Ordinal)) {
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
      if (tagName.Equals("*", StringComparison.Ordinal)) {
        tagName = null;
      }
      IList<IElement> ret = new List<IElement>();
      if (((Document)this.getOwnerDocument()).isHtmlDocument()) {
        string lowerTagName = DataUtilities.ToLowerCaseAscii(tagName);
        foreach (var node in this.getChildNodes()) {
          this.CollectElementsHtml(node, tagName, lowerTagName, ret);
        }
      } else {
        foreach (var node in this.getChildNodes()) {
          this.CollectElements(node, tagName, ret);
        }
      }
      return ret;
    }

    public string getId() {
      return this.getAttribute("id");
    }

    public string getInnerHTML() {
      return this.getInnerHtmlInternal();
    }

    public override sealed string getLanguage() {
      INode parent = this.getParentNode();
      string a = this.getAttributeNS(HtmlCommon.XML_NAMESPACE, "lang");
      a = a ?? this.getAttribute("lang");
      if (a != null) {
        return a;
      }
      if (parent == null) {
        parent = this.getOwnerDocument();
        return (parent == null) ? String.Empty : parent.getLanguage();
      } else {
        return parent.getLanguage();
      }
    }

    public string getLocalName() {
      return this.name;
    }

    public string getNamespaceURI() {
      return this._namespace;
    }

    public override sealed string getNodeName() {
      return this.getTagName();
    }

    public string getPrefix() {
      return this.prefix;
    }

    public string getTagName() {
      string tagName = this.name;
      if (this.prefix != null) {
        tagName = this.prefix + ":" + this.name;
      }
      return ((this.getOwnerDocument() is Document) &&
          HtmlCommon.HTML_NAMESPACE.Equals(this._namespace,
  StringComparison.Ordinal)) ?
            DataUtilities.ToUpperCaseAscii(tagName) : tagName;
    }

    public override sealed string getTextContent() {
      var builder = new StringBuilder();
      foreach (var node in this.getChildNodes()) {
        if (node.getNodeType() != NodeType.COMMENT_NODE) {
          builder.Append(node.getTextContent());
        }
      }
      return builder.ToString();
    }

    internal void MergeAttributes(INameAndAttributes token) {
      foreach (var attr in token.getAttributes()) {
        string s = this.getAttribute(attr.getName());
        if (s == null) {
          this.SetAttribute(attr.getName(), attr.getValue());
        }
      }
    }

    internal void SetAttribute(string _string, string value) {
      foreach (var attr in this.getAttributes()) {
        if (attr.getName().Equals(_string, StringComparison.Ordinal)) {
          ((Attr)attr).setValue(value);
        }
      }
      this.attributes.Add(new Attr(_string, value));
    }

    internal void SetLocalName(string name) {
      this.name = name;
    }

    internal void SetNamespace(string _namespace) {
      this._namespace = _namespace;
    }

    public void SetPrefix(string prefix) {
      this.prefix = prefix;
    }

    internal override sealed string toDebugString() {
      var builder = new StringBuilder();
      string extra = String.Empty;
      if (HtmlCommon.MATHML_NAMESPACE.Equals(this._namespace,
  StringComparison.Ordinal)) {
        extra = "math ";
      }
      if (HtmlCommon.SVG_NAMESPACE.Equals(this._namespace,
  StringComparison.Ordinal)) {
        extra = "svg ";
      }
      builder.Append("<" + extra + this.name.ToString() + ">\n");
      var attribs = new List<IAttr>(this.getAttributes());
      attribs.Sort(new AttributeNameComparator());
      foreach (var attribute in attribs) {
        // DebugUtility.Log("%s %s"
        // , attribute.getNamespace(), attribute.getLocalName());
        if (attribute.getNamespaceURI() != null) {
          string attributeName = String.Empty;
          if (HtmlCommon.XLINK_NAMESPACE.Equals(attribute.getNamespaceURI(),
  StringComparison.Ordinal)) {
            attributeName = "xlink ";
          }
          if (HtmlCommon.XML_NAMESPACE.Equals(attribute.getNamespaceURI(),
  StringComparison.Ordinal)) {
            attributeName = "xml ";
          }
          attributeName += attribute.getLocalName();
          builder.Append("\u0020\u0020" + attributeName + "=\"" +
            attribute.getValue().ToString().Replace("\n", "~~~~") + "\"\n");
        } else {
          builder.Append("\u0020\u0020" + attribute.getName().ToString() +
"=\"" +
          attribute.getValue().ToString().Replace("\n", "~~~~") + "\"\n");
        }
      }
      bool isTemplate = HtmlCommon.isHtmlElement(this, "template");
      if (isTemplate) {
        builder.Append("\u0020\u0020content\n");
      }
      foreach (var node in this.getChildNodesInternal()) {
        string str = ((Node)node).toDebugString();
        if (str == null) {
          continue;
        }
        string[] strarray = StringUtility.splitAt(str, "\n");
        int len = strarray.Length;
        if (len > 0 && strarray[len - 1].Length == 0) {
          --len; // ignore trailing empty string
        }
        for (int i = 0; i < len; ++i) {
          string el = strarray[i];
          // TODO: Separate template content from child nodes;
          // content is child nodes for convenience currently
          if (isTemplate) {
            {
              builder.Append("\u0020\u0020");
            }
          }
          builder.Append("\u0020\u0020");
          builder.Append(el);
          builder.Append("\n");
        }
      }
      return builder.ToString();
    }
  }
}
