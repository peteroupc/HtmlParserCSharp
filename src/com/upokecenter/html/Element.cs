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

namespace com.upokecenter.html {
using System;
using System.Text;
using System.Collections.Generic;
using com.upokecenter.util;
internal class Element : Node, IElement {
	private sealed class AttributeNameComparator : IComparer<IAttr> {
		public int Compare(IAttr arg0, IAttr arg1) {
			string a=arg0.getName();
			string b=arg1.getName();
			return String.Compare(a,b,StringComparison.Ordinal);
		}
	}


	internal static Element fromToken(HtmlParser.StartTagToken token){
		return fromToken(token,HtmlParser.HTML_NAMESPACE);
	}

	internal static Element fromToken(
			HtmlParser.StartTagToken token, string _namespace){
		Element ret=new Element();
		ret.name=token.getName();
		ret.attributes=new List<Attr>();
		foreach(Attr attribute in token.getAttributes()){
			ret.attributes.Add(new Attr(attribute));
		}
		ret._namespace=_namespace;
		return ret;
	}

	private string name;

	private string _namespace;

	private string prefix=null;

	private IList<Attr> attributes;

	internal Element() : base(NodeType.ELEMENT_NODE) {
		attributes=new List<Attr>();
	}

	public Element(string name) : base(NodeType.ELEMENT_NODE) {
		attributes=new List<Attr>();
		this.name=name;
	}


	internal void addAttribute(Attr value) {
		attributes.Add(value);
	}


	private void collectElements(INode c, string s, IList<IElement> nodes){
		if(c.getNodeType()==NodeType.ELEMENT_NODE){
			Element e=(Element)c;
			if(s==null || e.getLocalName().Equals(s)){
				nodes.Add(e);
			}
		}
		foreach(INode node in c.getChildNodes()){
			collectElements(node,s,nodes);
		}
	}

	private void collectElementsHtml(INode c, string s,
			string sLowercase, IList<IElement> nodes){
		if(c.getNodeType()==NodeType.ELEMENT_NODE){
			Element e=(Element)c;
			if(s==null){
				nodes.Add(e);
			} else if(HtmlParser.HTML_NAMESPACE.Equals(e.getNamespaceURI()) &&
					e.getLocalName().Equals(sLowercase)){
				nodes.Add(e);
			} else if(e.getLocalName().Equals(s)){
				nodes.Add(e);
			}
		}
		foreach(INode node in c.getChildNodes()){
			collectElements(node,s,nodes);
		}
	}

	public string getAttribute(string name) {
		foreach(IAttr attr in getAttributes()){
			if(attr.getName().Equals(name))
				return attr.getValue();
		}
		return null;
	}

	public string getAttributeNS(string _namespace, string localName) {
		foreach(IAttr attr in getAttributes()){
			if((localName==null ? attr.getLocalName()==null : localName.Equals(attr.getLocalName())) &&
					(_namespace==null ? attr.getNamespaceURI()==null : _namespace.Equals(attr.getNamespaceURI())))
				return attr.getValue();
		}
		return null;
	}
	public IList<IAttr> getAttributes() {
		return new List<IAttr>(attributes);
	}

	public IElement getElementById(string id) {
		if(id==null)
			throw new ArgumentException();
		foreach(INode node in getChildNodes()){
			if(node is IElement){
				if(id.Equals(((IElement)node).getId()))
					return (IElement)node;
				IElement element=((IElement)node).getElementById(id);
				if(element!=null)return element;
			}
		}
		return null;
	}

	public IList<IElement> getElementsByTagName(string tagName) {
		if(tagName==null)
			throw new ArgumentException();
		if(tagName.Equals("*")) {
			tagName=null;
		}
		IList<IElement> ret=new List<IElement>();
		if(((Document) getOwnerDocument()).isHtmlDocument()){
			string lowerTagName=StringUtility.toLowerCaseAscii(tagName);
			foreach(INode node in getChildNodes()){
				collectElementsHtml(node,tagName,lowerTagName,ret);
			}
		} else {
			foreach(INode node in getChildNodes()){
				collectElements(node,tagName,ret);
			}
		}
		return ret;
	}

	public string getId(){
		return getAttribute("id");
	}

	public string getInnerHTML() {
		return getInnerHtmlInternal();
	}

	public override sealed string getLanguage(){
		INode parent=getParentNode();
		string a=getAttributeNS(HtmlParser.XML_NAMESPACE,"lang");
		if(a==null) {
			a=getAttribute("lang");
		}
		if(a!=null)return a;
		if(parent==null){
			parent=getOwnerDocument();
			if(parent==null)return "";
			return parent.getLanguage();
		} else
			return parent.getLanguage();
	}
	public string getLocalName() {
		return name;
	}

	public string getNamespaceURI() {
		return _namespace;
	}
	public override sealed string getNodeName(){
		return getTagName();
	}

	public string getPrefix() {
		return prefix;
	}

	public string getTagName() {
		string tagName=name;
		if(prefix!=null){
			tagName=prefix+":"+name;
		}
		if((getOwnerDocument() is Document) &&
				HtmlParser.HTML_NAMESPACE.Equals(_namespace))
			return StringUtility.toUpperCaseAscii(tagName);
		return tagName;
	}

	public override sealed string getTextContent(){
		StringBuilder builder=new StringBuilder();
		foreach(INode node in getChildNodes()){
			if(node.getNodeType()!=NodeType.COMMENT_NODE){
				builder.Append(node.getTextContent());
			}
		}
		return builder.ToString();
	}


	internal bool isHtmlElement(string name){
		return name.Equals(this.name) && HtmlParser.HTML_NAMESPACE.Equals(_namespace);
	}

	internal bool isMathMLElement(string name){
		return name.Equals(this.name) && HtmlParser.MATHML_NAMESPACE.Equals(_namespace);
	}

	internal bool isSvgElement(string name){
		return name.Equals(this.name) && HtmlParser.SVG_NAMESPACE.Equals(_namespace);
	}

	internal void mergeAttributes(HtmlParser.StartTagToken token){
		foreach(IAttr attr in token.getAttributes()){
			string s=getAttribute(attr.getName());
			if(s==null){
				setAttribute(attr.getName(),attr.getValue());
			}
		}
	}
	internal void setAttribute(string _string, string value) {
		foreach(IAttr attr in getAttributes()){
			if(attr.getName().Equals(_string)){
				((Attr)attr).setValue(value);
			}
		}
		attributes.Add(new Attr(_string,value));
	}


	internal void setLocalName(string name) {
		this.name = name;
	}

	internal void setNamespace(string _namespace) {
		this._namespace = _namespace;
	}

	public void setPrefix(string prefix){
		this.prefix=prefix;
	}

	internal override sealed string toDebugString(){
		StringBuilder builder=new StringBuilder();
		string extra="";
		if(HtmlParser.MATHML_NAMESPACE.Equals(_namespace)) {
			extra="math ";
		}
		if(HtmlParser.SVG_NAMESPACE.Equals(_namespace)) {
			extra="svg ";
		}
		builder.Append("<"+extra+name.ToString()+">\n");
		List<IAttr> attribs=new List<IAttr>(getAttributes());
		attribs.Sort(new AttributeNameComparator());
		foreach(IAttr attribute in attribs){
			//Console.WriteLine("%s %s",attribute.getNamespace(),attribute.getLocalName());
			if(attribute.getNamespaceURI()!=null){
				string extra1="";
				if(HtmlParser.XLINK_NAMESPACE.Equals(attribute.getNamespaceURI())) {
					extra1="xlink ";
				}
				if(HtmlParser.XML_NAMESPACE.Equals(attribute.getNamespaceURI())) {
					extra1="xml ";
				}
				extra1+=attribute.getLocalName();
				builder.Append("  "+extra1+"=\""+attribute.getValue().ToString().Replace("\n","~~~~")+"\"\n");
			} else {
				builder.Append("  "+attribute.getName().ToString()+"=\""+attribute.getValue().ToString().Replace("\n","~~~~")+"\"\n");
			}
		}
		foreach(Node node in getChildNodesInternal()){
			string str=node.toDebugString();
			if(str==null) {
				continue;
			}
			string[] strarray=StringUtility.splitAt(str,"\n");
			int len=strarray.Length;
			if(len>0 && strarray[len-1].Length==0)
			{
				len--; // ignore trailing empty _string
			}
			for(int i=0;i<len;i++){
				string el=strarray[i];
				builder.Append("  ");
				builder.Append(el);
				builder.Append("\n");
			}
		}
		return builder.ToString();
	}

}
}
