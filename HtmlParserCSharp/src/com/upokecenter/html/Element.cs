using System;
using System.Collections.Generic;
using System.Text;
using Com.Upokecenter.Util;
using PeterO;
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
  internal class Element : Node, IElement {
    private sealed class AttributeNameComparator : IComparer<IAttr> {
      public int Compare(IAttr arg0, IAttr arg1) {
        string a = arg0.GetName();
        string b = arg1.GetName();
        return String.CompareOrdinal(a, b);
      }
    }

    internal static Element FromToken(INameAndAttributes token) {
      return FromToken(token, HtmlCommon.HTML_NAMESPACE);
    }

    internal static Element FromToken(
      INameAndAttributes token,
      string namespaceValue) {
      var ret = new Element();
      ret.name = token.GetName();
      ret.attributes = new List<Attr>();
      foreach (var attribute in token.GetAttributes()) {
        ret.attributes.Add(new Attr(attribute));
      }
      ret.namespaceValue = namespaceValue;
      return ret;
    }

    private string name;

    private string namespaceValue;

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
      if (c.GetNodeType() == NodeType.ELEMENT_NODE) {
        var e = (Element)c;
        if (s == null || e.GetLocalName().Equals(s,
          StringComparison.Ordinal)) {
          nodes.Add(e);
        }
      }
      foreach (var node in c.GetChildNodes()) {
        this.CollectElements(node, s, nodes);
      }
    }

    private void CollectElementsHtml(
      INode c,
      string s,
      string valueSLowercase,
      IList<IElement> nodes) {
      if (c.GetNodeType() == NodeType.ELEMENT_NODE) {
        var e = (Element)c;
        if (s == null) {
          nodes.Add(e);
        } else if (HtmlCommon.HTML_NAMESPACE.Equals(e.GetNamespaceURI(),
          StringComparison.Ordinal) && e.GetLocalName().Equals(
            valueSLowercase,
            StringComparison.Ordinal)) {
          nodes.Add(e);
        } else if (e.GetLocalName().Equals(s, StringComparison.Ordinal)) {
          nodes.Add(e);
        }
      }
      foreach (var node in c.GetChildNodes()) {
        this.CollectElements(node, s, nodes);
      }
    }

    public string GetAttribute(string name) {
      foreach (var attr in this.GetAttributes()) {
        if (attr.GetName().Equals(name, StringComparison.Ordinal)) {
          return attr.GetValue();
        }
      }
      return null;
    }

    public string GetAttributeNS(string namespaceValue, string localName) {
      foreach (var attr in this.GetAttributes()) {
        if ((localName == null ? attr.GetLocalName() == null :
          localName.Equals(attr.GetLocalName(), StringComparison.Ordinal)) &&
          (namespaceValue == null ? attr.GetNamespaceURI() == null :
          namespaceValue.Equals(attr.GetNamespaceURI(),
          StringComparison.Ordinal))) {
          return attr.GetValue();
        }
      }
      return null;
    }

    private List<IAttr> GetAttributesList() {
      var attrs = new List<IAttr>();
      IList<Attr> thisattrs = this.attributes;
      foreach (var attr in thisattrs) {
        attrs.Add(attr);
      }
      return attrs;
    }

    public IList<IAttr> GetAttributes() {
      return this.GetAttributesList();
    }

    public IElement GetElementById(string id) {
      if (id == null) {
        throw new ArgumentException();
      }
      foreach (var node in this.GetChildNodes()) {
        if (node is IElement) {
          if (id.Equals(((IElement)node).GetId(), StringComparison.Ordinal)) {
            return (IElement)node;
          }
          IElement element = ((IElement)node).GetElementById(id);
          if (element != null) {
            return element;
          }
        }
      }
      return null;
    }

    public IList<IElement> GetElementsByTagName(string tagName) {
      if (tagName == null) {
        throw new ArgumentException();
      }
      if (tagName.Equals("*", StringComparison.Ordinal)) {
        tagName = null;
      }
      IList<IElement> ret = new List<IElement>();
      if (((Document)this.GetOwnerDocument()).IsHtmlDocument()) {
        string lowerTagName = DataUtilities.ToLowerCaseAscii(tagName);
        foreach (var node in this.GetChildNodes()) {
          this.CollectElementsHtml(node, tagName, lowerTagName, ret);
        }
      } else {
        foreach (var node in this.GetChildNodes()) {
          this.CollectElements(node, tagName, ret);
        }
      }
      return ret;
    }

    public string GetId() {
      return this.GetAttribute("id");
    }

    public string GetInnerHTML() {
      return this.GetInnerHtmlInternal();
    }

    public override sealed string GetLanguage() {
      INode parent = this.GetParentNode();
      string a = this.GetAttributeNS(HtmlCommon.XML_NAMESPACE, "lang");
      a = a ?? this.GetAttribute("lang");
      if (a != null) {
        return a;
      }
      if (parent == null) {
        parent = this.GetOwnerDocument();
        return (parent == null) ? String.Empty : parent.GetLanguage();
      } else {
        return parent.GetLanguage();
      }
    }

    public string GetLocalName() {
      return this.name;
    }

    public string GetNamespaceURI() {
      return this.namespaceValue;
    }

    public override sealed string GetNodeName() {
      return this.GetTagName();
    }

    public string GetPrefix() {
      return this.prefix;
    }

    public string GetTagName() {
      string tagName = this.name;
      if (this.prefix != null) {
        tagName = this.prefix + ":" + this.name;
      }
      return ((this.GetOwnerDocument() is Document) &&
        HtmlCommon.HTML_NAMESPACE.Equals(this.namespaceValue,
          StringComparison.Ordinal)) ?
        DataUtilities.ToUpperCaseAscii(tagName) : tagName;
    }

    public override sealed string GetTextContent() {
      var builder = new StringBuilder();
      foreach (var node in this.GetChildNodes()) {
        if (node.GetNodeType() != NodeType.COMMENT_NODE) {
          builder.Append(node.GetTextContent());
        }
      }
      return builder.ToString();
    }

    internal void MergeAttributes(INameAndAttributes token) {
      foreach (var attr in token.GetAttributes()) {
        string s = this.GetAttribute(attr.GetName());
        if (s == null) {
          this.SetAttribute(attr.GetName(), attr.GetValue());
        }
      }
    }

    internal void SetAttribute(string stringValue, string value) {
      foreach (var attr in this.GetAttributes()) {
        if (attr.GetName().Equals(stringValue, StringComparison.Ordinal)) {
          ((Attr)attr).SetValue(value);
        }
      }
      this.attributes.Add(new Attr(stringValue, value));
    }

    internal void SetLocalName(string name) {
      this.name = name;
    }

    internal void SetNamespace(string namespaceValue) {
      this.namespaceValue = namespaceValue;
    }

    public void SetPrefix(string prefix) {
      this.prefix = prefix;
    }

    internal override sealed string ToDebugString() {
      var builder = new StringBuilder();
      string extra = String.Empty;
      string ns = this.namespaceValue;
      if (!String.IsNullOrEmpty(ns)) {
        if (ns.Equals(HtmlCommon.MATHML_NAMESPACE,
          StringComparison.Ordinal)) {
          extra = "math ";
        }
        if (ns.Equals(HtmlCommon.SVG_NAMESPACE,
          StringComparison.Ordinal)) {
          extra = "svg ";
        }
      }
      builder.Append("<" + extra + this.name.ToString() + ">\n");
      List<IAttr> attribs = this.GetAttributesList();
      attribs.Sort(new AttributeNameComparator());
      foreach (var attribute in attribs) {
        // Console.WriteLine("%s %s"
        // , attribute.Getspace(), attribute.GetLocalName());
        string nsuri = attribute.GetNamespaceURI();
        if (nsuri != null) {
          string attributeName = String.Empty;
          if (HtmlCommon.XLINK_NAMESPACE.Equals(nsuri,
            StringComparison.Ordinal)) {
            attributeName = "xlink ";
          }
          if (HtmlCommon.XML_NAMESPACE.Equals(nsuri,
            StringComparison.Ordinal)) {
            attributeName = "xml ";
          }
          attributeName += attribute.GetLocalName();
          builder.Append("\u0020\u0020" + attributeName + "=\"" +
            attribute.GetValue().ToString().Replace("\n", "~~~~") + "\"\n");
        } else {
          builder.Append("\u0020\u0020" + attribute.GetName().ToString() +
            "=\"" +
            attribute.GetValue().ToString().Replace("\n", "~~~~") + "\"\n");
        }
      }
      bool isTemplate = HtmlCommon.IsHtmlElement(this, "template");
      if (isTemplate) {
        builder.Append("\u0020\u0020content\n");
      }
      foreach (var node in this.GetChildNodesInternal()) {
        string str = ((Node)node).ToDebugString();
        if (str == null) {
          continue;
        }
        string[] strarray = StringUtility.SplitAt(str, "\n");
        int len = strarray.Length;
        if (len > 0 && String.IsNullOrEmpty(strarray[len - 1])) {
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
