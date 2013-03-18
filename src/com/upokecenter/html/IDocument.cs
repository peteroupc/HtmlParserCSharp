namespace com.upokecenter.html {
using System;

using System.Collections.Generic;

/**
 * 
 * Represents an HTML document.  This is the root of
 * the document hierarchy.
 * 
 * @author Peter
 *
 */
public interface IDocument : INode {
	/**
	 * 
	 * Gets all descendents, both direct and indirect, that have
	 * the specified tag name, using ASCII case-insensitive matching.
	 * 
	 * @param _string A tag name.
	 * 
	 */
	IList<IElement> getElementsByTagName(string _string);
	/**
	 * 
	 * Gets the document type of this document, if any.
	 * 
	 * 
	 */
	 IDocumentType getDoctype();
	/**
	 * 
	 * Gets the character encoding used in this document.
	 * 
	 * @return A character encoding name.
	 */
	string getCharacterSet();
	/**
	 * 
	 * Gets the root element of this document.
	 * 
	 * 
	 */
	 IElement getDocumentElement();
}

}
