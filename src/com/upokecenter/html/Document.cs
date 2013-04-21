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
internal class Document : Node, IDocument {
	internal DocumentType doctype;
	internal string encoding;
	private DocumentMode docmode=DocumentMode.NoQuirksMode;

	internal Document() : base(NodeType.DOCUMENT_NODE) {
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

	public string getCharacterSet() {
		return (encoding==null) ? "utf-8" : encoding;
	}


	public IDocumentType getDoctype(){
		return doctype;
	}

	public IElement getDocumentElement() {
		foreach(INode node in getChildNodes()){
			if(node is IElement)
				return (IElement)node;
		}
		return null;
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
		if(isHtmlDocument()){
			collectElementsHtml(this,tagName,
					StringUtility.toLowerCaseAscii(tagName),ret);
		} else {
			collectElements(this,tagName,ret);
		}
		return ret;
	}

	internal DocumentMode getMode() {
		return docmode;
	}
	public override IDocument getOwnerDocument(){
		return null;
	}

	internal bool isHtmlDocument(){
		return true;
	}

	internal void setMode(DocumentMode mode) {
		docmode=mode;
	}

	internal override string toDebugString(){
		StringBuilder builder=new StringBuilder();
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
				builder.Append("| ");
				builder.Append(el.Replace("~~~~","\n"));
				builder.Append("\n");
			}
		}
		return builder.ToString();
	}

	public override string getNodeName(){
		return "#document";
	}

	internal string address;
	internal string defaultLanguage;

	public string getURL() {
		return address;
	}

	public override string getLanguage(){
		return (defaultLanguage==null) ? "" : defaultLanguage;
	}

}
}
