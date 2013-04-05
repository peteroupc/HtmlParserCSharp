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

using System.Collections.Generic;




using com.upokecenter.util;
internal class Element : Node, IElement {
	private sealed class AttributeNameComparator : IComparer<HtmlParser.Attrib> {
		public int Compare(HtmlParser.Attrib arg0, HtmlParser.Attrib arg1) {
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
		ret.attributes=new List<HtmlParser.Attrib>();
		foreach(HtmlParser.Attrib attribute in token.getAttributes()){
			ret.attributes.Add(new HtmlParser.Attrib(attribute));
		}
		ret._namespace=_namespace;
		return ret;
	}

	private string name;

	private string _namespace;

	private string prefix=null;

	private IList<HtmlParser.Attrib> attributes;

	internal Element() : base(NodeType.ELEMENT_NODE) {
		attributes=new List<HtmlParser.Attrib>();
	}

	public Element(string name) : base(NodeType.ELEMENT_NODE) {
		attributes=new List<HtmlParser.Attrib>();
		this.name=name;
	}

	public string getAttribute(string name) {
		foreach(HtmlParser.Attrib attr in getAttributes()){
			if(attr.getName().Equals(name))
				return attr.getValue();
		}
		return null;
	}

	public void setPrefix(string prefix){
		this.prefix=prefix;
	}

	public string getAttributeNS(string _namespace, string localName) {
		foreach(HtmlParser.Attrib attr in getAttributes()){
			if(attr.isAttribute(localName,_namespace))
				return attr.getValue();
		}
		return null;
	}
	public IList<HtmlParser.Attrib> getAttributes() {
		return attributes;
	}

	public string getLocalName() {
		return name;
	}

	public string getNamespaceURI() {
		return _namespace;
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
		foreach(HtmlParser.Attrib attr in token.getAttributes()){
			string s=getAttribute(attr.getName());
			if(s==null){
				setAttribute(attr.getName(),attr.getValue());
			}
		}
	}

	public void setAttribute(string _string, string value) {
		foreach(HtmlParser.Attrib attr in getAttributes()){
			if(attr.getName().Equals(_string)){
				attr.setValue(value);
			}
		}
		attributes.Add(new HtmlParser.Attrib(_string,value));
	}
	internal void setName(string name) {
		this.name = name;
	}

	internal void setNamespace(string _namespace) {
		this._namespace = _namespace;
	}

	internal override sealed string toDebugString(){
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		string extra="";
		if(HtmlParser.MATHML_NAMESPACE.Equals(_namespace)) {
			extra="math ";
		}
		if(HtmlParser.SVG_NAMESPACE.Equals(_namespace)) {
			extra="svg ";
		}
		builder.Append("<"+extra+name.ToString()+">\n");
		List<HtmlParser.Attrib> attribs=new List<HtmlParser.Attrib>(getAttributes());
		attribs.Sort(new AttributeNameComparator());
		foreach(HtmlParser.Attrib attribute in attribs){
			//Console.WriteLine("%s %s",attribute.getNamespace(),attribute.getLocalName());
			if(attribute.getNamespace()!=null){
				string extra1="";
				if(HtmlParser.XLINK_NAMESPACE.Equals(attribute.getNamespace())) {
					extra1="xlink ";
				}
				if(HtmlParser.XML_NAMESPACE.Equals(attribute.getNamespace())) {
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
			foreach(string el in strarray){
				builder.Append("  ");
				builder.Append(el);
				builder.Append("\n");
			}
		}
		return builder.ToString();
	}

	public override sealed string ToString(){
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		builder.Append("Element: "+name.ToString()+", "+_namespace.ToString()+"\n");
		foreach(HtmlParser.Attrib attribute in getAttributes()){
			builder.Append("Attribute: "+attribute.getName().ToString()+"="+
					attribute.getValue().ToString()+"\n");
		}
		foreach(Node node in getChildNodesInternal()){
			string str=node.ToString();
			string[] strarray=StringUtility.splitAt(str,"\n");
			foreach(string el in strarray){
				builder.Append("  ");
				builder.Append(el);
				builder.Append("\n");
			}
		}
		return builder.ToString();
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
	public IList<IElement> getElementsByTagName(string tagName) {
		if(tagName==null)
			throw new ArgumentException();
		if(tagName.Equals("*")) {
			tagName="";
		}
		IList<IElement> ret=new List<IElement>();
		if(((Document) getOwnerDocument()).isHtmlDocument()){
			collectElementsHtml(this,tagName,
					StringUtility.toLowerCaseAscii(tagName),ret);
		} else {
			collectElements(this,tagName,ret);
		}
		return ret;
	}


	public override sealed string getTextContent(){
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		foreach(INode node in getChildNodes()){
			if(node.getNodeType()!=NodeType.COMMENT_NODE){
				builder.Append(node.getTextContent());
			}
		}
		return builder.ToString();
	}
}
}
