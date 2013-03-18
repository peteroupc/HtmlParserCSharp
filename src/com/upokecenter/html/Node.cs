namespace com.upokecenter.html {
using System;

using System.Collections.Generic;

internal class Node : INode {
	private IList<Node> childNodes;
	private Node parentNode=null;
	private IDocument ownerDocument=null;

	public virtual IDocument getOwnerDocument(){
		return ownerDocument;
	}

	void setOwnerDocument(IDocument document){
		ownerDocument=document;
	}

	int nodeType;
	public Node(int nodeType){
		this.nodeType=nodeType;
		childNodes=new List<Node>();
	}
	internal virtual string toDebugString() {
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

	public void appendChild(Node node){
		if(node==this)
			throw new ArgumentException();
		node.parentNode=this;
		node.ownerDocument=(this is IDocument) ? (IDocument)this : ownerDocument;
		childNodes.Add(node);
	}

	internal IList<Node> getChildNodesInternal(){
		return childNodes;
	}

	public int getNodeType(){
		return nodeType;
	}
	public INode getParentNode() {
		return parentNode;
	}
	public void removeChild(Node node){
		node.parentNode=null;
		childNodes.Remove(node);
	}
	public IList<INode> getChildNodes() {
		return new List<INode>(childNodes);
	}

	public virtual string getBaseURI() {
		IDocument doc=getOwnerDocument();
		if(doc==null)return "";
		return doc.getBaseURI();
	}

	public virtual string getTextContent(){
		return null;
	}
}
}
