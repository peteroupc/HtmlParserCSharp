namespace com.upokecenter.html {
using System;

/**
 * 
 * Contains constants for node types.
 * 
 * @author Peter
 *
 */
public sealed class NodeType {
	private NodeType(){}
	/**
	 * A document node.
	 */
	public static readonly int DOCUMENT_NODE=9;
	/**
	 * A comment node.
	 */
	public static readonly int COMMENT_NODE=8;
	/**
	 * An HTML element node.
	 */
	public static readonly int ELEMENT_NODE=1;
	/**
	 * A node containing text.
	 */
	public static readonly int TEXT_NODE=3;
	/**
	 * A DOCTYPE node.
	 */
	public static readonly int DOCUMENT_TYPE_NODE = 10;
}
}
