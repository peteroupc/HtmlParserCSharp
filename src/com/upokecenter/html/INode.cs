/*
If you like this, you should donate to Peter O.
at: http://upokecenter.com/d/



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

/**
 * 
 * Represents a node in the document _object model (DOM).  All DOM
 * objects implement this interface.
 * 
 * @author Peter
 *
 */
public interface INode {


	/**
	 * Returns the _base URL of this node.  URLs on this
	 * node are resolved relative to this URL.
	 */
	string getBaseURI();
	/**
	 * 
	 * Gets the direct children of this node.
	 * 
	 * @return A list of the direct children of this node.
	 */
	IList<INode> getChildNodes();
	/**
	 * Gets the language of this node.  Not defined in the DOM specification.
	 */
	string getLanguage();
	/**
	 * Gets the name of this node.  For HTML elements, this will
	 * be the same as the tag name.
	 */
	string getNodeName();
	/**
	 * 
	 * Returns the type of node represented by this _object.
	 * 
	 * @return A node type integer; see NodeType.
	 */
	int getNodeType();
	/**
	 * 
	 * Gets the document that owns this node.
	 * 
	 * @return the owner document, or null if this is a document _object.
	 */
	IDocument getOwnerDocument();
	/**
	 * 
	 * Gets the parent node of this node.
	 * 
	 * @return the parent node, or null if this is the root node.
	 */
	INode getParentNode();
	/**
	 * 
	 * Gets all the text found within this element.
	 * 
	 * @return All the concatenated text, except comments, for Element
	 * nodes; or the text of Comment nodes; or null otherwise.
	 */
	string getTextContent();
}

}
