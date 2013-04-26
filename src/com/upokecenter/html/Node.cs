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
internal class Node : INode {
	private IList<Node> childNodes;
	private Node parentNode=null;
	private IDocument ownerDocument=null;

	int nodeType;


	private string baseURI=null;
	public Node(int nodeType){
		this.nodeType=nodeType;
		childNodes=new List<Node>();
	}


	public void appendChild(Node node){
		if(node==this)
			throw new ArgumentException();
		node.parentNode=this;
		node.ownerDocument=(this is IDocument) ? (IDocument)this : ownerDocument;
		childNodes.Add(node);
	}

	private void fragmentSerializeInner(
			INode current, StringBuilder builder){
		if(current.getNodeType()==NodeType.ELEMENT_NODE){
			IElement e=((IElement)current);
			string tagname=e.getTagName();
			string namespaceURI=e.getNamespaceURI();
			if(HtmlParser.HTML_NAMESPACE.Equals(namespaceURI) ||
					HtmlParser.SVG_NAMESPACE.Equals(namespaceURI) ||
					HtmlParser.MATHML_NAMESPACE.Equals(namespaceURI)){
				tagname=e.getLocalName();
			}
			builder.Append('<');
			builder.Append(tagname);
			foreach(IAttr attr in e.getAttributes()){
				namespaceURI=attr.getNamespaceURI();
				builder.Append(' ');
				if(namespaceURI==null || namespaceURI.Length==0){
					builder.Append(attr.getLocalName());
				} else if(namespaceURI.Equals(HtmlParser.XML_NAMESPACE)){
					builder.Append("xml:");
					builder.Append(attr.getLocalName());
				} else if(namespaceURI.Equals(
						"http://www.w3.org/2000/xmlns/")){
					if(!"xmlns".Equals(attr.getLocalName())) {
						builder.Append("xmlns:");
					}
					builder.Append(attr.getLocalName());
				} else if(namespaceURI.Equals(HtmlParser.XLINK_NAMESPACE)){
					builder.Append("xlink:");
					builder.Append(attr.getLocalName());
				} else {
					builder.Append(attr.getName());
				}
				builder.Append("=\"");
				string value=attr.getValue();
				for(int i=0;i<value.Length;i++){
					char c=value[i];
					if(c=='&') {
						builder.Append("&amp;");
					} else if(c==0xa0) {
						builder.Append("&nbsp;");
					} else if(c=='"') {
						builder.Append("&quot;");
					} else {
						builder.Append(c);
					}
				}
				builder.Append('"');
			}
			builder.Append('>');
			if(HtmlParser.HTML_NAMESPACE.Equals(namespaceURI)){
				string localName=e.getLocalName();
				if("area".Equals(localName) ||
						"base".Equals(localName) ||
						"basefont".Equals(localName) ||
						"bgsound".Equals(localName) ||
						"br".Equals(localName) ||
						"col".Equals(localName) ||
						"embed".Equals(localName) ||
						"frame".Equals(localName) ||
						"hr".Equals(localName) ||
						"img".Equals(localName) ||
						"input".Equals(localName) ||
						"keygen".Equals(localName) ||
						"link".Equals(localName) ||
						"menuitem".Equals(localName) ||
						"meta".Equals(localName) ||
						"param".Equals(localName) ||
						"source".Equals(localName) ||
						"track".Equals(localName) ||
						"wbr".Equals(localName))
					return;
				if("pre".Equals(localName) ||
						"textarea".Equals(localName) ||
						"listing".Equals(localName)){
					foreach(INode node in e.getChildNodes()){
						if(node.getNodeType()==NodeType.TEXT_NODE &&
								((IText)node).getData().Length>0 &&
								((IText)node).getData()[0]=='\n'){
							builder.Append('\n');
						}
					}
				}
			}
			// Recurse
			foreach(INode child in e.getChildNodes()){
				fragmentSerializeInner(child,builder);
			}
			builder.Append("</");
			builder.Append(tagname);
			builder.Append(">");
		} else if(current.getNodeType()==NodeType.TEXT_NODE){
			INode parent=current.getParentNode();
			if(parent is IElement &&
					HtmlParser.HTML_NAMESPACE.Equals(((IElement)parent).getNamespaceURI())){
				string localName=((IElement)parent).getLocalName();
				if("script".Equals(localName) ||
						"style".Equals(localName) ||
						"script".Equals(localName) ||
						"xmp".Equals(localName) ||
						"iframe".Equals(localName) ||
						"noembed".Equals(localName) ||
						"noframes".Equals(localName) ||
						"plaintext".Equals(localName)){
					builder.Append(((IText)current).getData());
				} else {
					string value=((IText)current).getData();
					for(int i=0;i<value.Length;i++){
						char c=value[i];
						if(c=='&') {
							builder.Append("&amp;");
						} else if(c==0xa0) {
							builder.Append("&nbsp;");
						} else if(c=='<') {
							builder.Append("&lt;");
						} else if(c=='>') {
							builder.Append("&gt;");
						} else {
							builder.Append(c);
						}
					}
				}
			}
		} else if(current.getNodeType()==NodeType.COMMENT_NODE){
			builder.Append("<!--");
			builder.Append(((IComment)current).getData());
			builder.Append("-->");
		} else if(current.getNodeType()==NodeType.DOCUMENT_TYPE_NODE){
			builder.Append("<!DOCTYPE ");
			builder.Append(((IDocumentType)current).getName());
			builder.Append(">");
		} else if(current.getNodeType()==NodeType.PROCESSING_INSTRUCTION_NODE){
			builder.Append("<?");
			builder.Append(((IProcessingInstruction)current).getTarget());
			builder.Append(' ');
			builder.Append(((IProcessingInstruction)current).getData());
			builder.Append(">");			// NOTE: may be erroneous
		}
	}

	public virtual string getBaseURI() {
		INode parent=getParentNode();
		if(baseURI==null){
			if(parent==null)
				return "about:blank";
			else
				return parent.getBaseURI();
		} else {
			if(parent==null)
				return baseURI;
			else {
				URL ret=URL.parse(baseURI,URL.parse(parent.getBaseURI()));
				return (ret==null) ? parent.getBaseURI() : ret.ToString();
			}
		}
	}

	public IList<INode> getChildNodes() {
		return new List<INode>(childNodes);
	}

	internal IList<Node> getChildNodesInternal(){
		return childNodes;
	}

	protected internal string getInnerHtmlInternal(){
		StringBuilder builder=new StringBuilder();
		foreach(INode child in getChildNodes()){
			fragmentSerializeInner(child,builder);
		}
		return builder.ToString();
	}

	public virtual string getLanguage(){
		INode parent=getParentNode();
		if(parent==null){
			parent=getOwnerDocument();
			if(parent==null)return "";
			return parent.getLanguage();
		} else
			return parent.getLanguage();
	}

	public virtual string getNodeName() {
		return "";
	}

	public int getNodeType(){
		return nodeType;
	}

	public virtual IDocument getOwnerDocument(){
		return ownerDocument;
	}
	public INode getParentNode() {
		return parentNode;
	}
	public virtual string getTextContent(){
		return null;
	}
	public void insertBefore(Node child, Node sibling){
		if(sibling==null){
			appendChild(child);
			return;
		}
		if(childNodes.Count==0)
			throw new InvalidOperationException();
		int childNodesSize=childNodes.Count;
		for(int j=0;j<childNodesSize;j++){
			if(childNodes[j].Equals(sibling)){
				child.parentNode=this;
				child.ownerDocument=(child is IDocument) ? (IDocument)this : ownerDocument;
				childNodes.Insert(j,child);
				return;
			}
		}
		throw new ArgumentException();
	}

	public void removeChild(Node node){
		node.parentNode=null;
		childNodes.Remove(node);
	}

	internal void setBaseURI(string value){
		INode parent=getParentNode();
		if(parent==null){
			baseURI=value;
		} else {
			string val=URL.parse(value,URL.parse(parent.getBaseURI())).ToString();
			baseURI=(val==null) ? parent.getBaseURI() : val.ToString();
		}
	}

	void setOwnerDocument(IDocument document){
		ownerDocument=document;
	}

	internal virtual string toDebugString() {
		return null;
	}


	public override string ToString() {
		return getNodeName();
	}

}
}
