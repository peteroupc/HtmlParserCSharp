namespace com.upokecenter.html {
using System;

using System.Collections.Generic;


using com.upokecenter.util;
internal class Document : Node, IDocument {
	internal DocumentType doctype;
	internal string encoding;
	internal string baseurl;
	private DocumentMode docmode=DocumentMode.NoQuirksMode;

	internal Document() : base(NodeType.DOCUMENT_NODE) {
	}

	internal bool isHtmlDocument(){
		return true;
	}

	public IDocumentType getDoctype(){
		return doctype;
	}

	public override IDocument getOwnerDocument(){
		return null;
	}

	internal override string toDebugString(){
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		foreach(Node node in getChildNodesInternal()){
			string str=node.toDebugString();
			if(str==null) {
				continue;
			}
			string[] strarray=StringUtility.splitAt(str,"\n");
			foreach(string el in strarray){
				builder.Append("| ");
				builder.Append(el.Replace("~~~~","\n"));
				builder.Append("\n");
			}
		}
		return builder.ToString();
	}


	public override string getBaseURI() {
		return (baseurl==null) ? "" : baseurl;
	}

	internal DocumentMode getMode() {
		return docmode;
	}

	internal void setMode(DocumentMode mode) {
		docmode=mode;
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
		if(isHtmlDocument()){
			collectElementsHtml(this,tagName,
					StringUtility.toLowerCaseAscii(tagName),ret);
		} else {
			collectElements(this,tagName,ret);
		}
		return ret;
	}

	public string getCharacterSet() {
		return (encoding==null) ? "utf-8" : encoding;
	}

	public IElement getDocumentElement() {
		foreach(INode node in getChildNodes()){
			if(node is IElement)
				return (IElement)node;
		}
		return null;
	}

}
}
