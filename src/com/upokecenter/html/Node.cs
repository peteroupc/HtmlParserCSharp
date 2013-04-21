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
internal class Node : INode {
	private IList<Node> childNodes;
	private Node parentNode=null;
	private IDocument ownerDocument=null;

	int nodeType;

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

	private string baseURI=null;

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

	internal void setBaseURI(string value){
		INode parent=getParentNode();
		if(parent==null){
			baseURI=value;
		} else {
			string val=URL.parse(value,URL.parse(parent.getBaseURI())).ToString();
			baseURI=(val==null) ? parent.getBaseURI() : val.ToString();
		}
	}

	public IList<INode> getChildNodes() {
		return new List<INode>(childNodes);
	}

	internal IList<Node> getChildNodesInternal(){
		return childNodes;
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

	void setOwnerDocument(IDocument document){
		ownerDocument=document;
	}

	internal virtual string toDebugString() {
		return null;
	}

	public virtual string getNodeName() {
		return "";
	}

	public override string ToString() {
		return getNodeName();
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

}
}
