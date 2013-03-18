namespace com.upokecenter.html {
using System;

using System.Collections.Generic;

/**
 * 
 * Represents an HTML element.
 * 
 * @author Peter
 *
 */
public interface IElement : INode {
	/**
	 * 
	 * Gets the name of the element as used on its HTML tags.
	 * 
	 * @return the element's tag name.  For HTML elements,
	 * an uppercase version of the name will be returned.
	 */
	string getTagName();
	/**
	 * 
	 * Gets the element's local name.  For elements with no
	 * _namespace, this will equal the element's tag name.
	 * 
	 * @return the element's local name. This method doesn't
	 * convert it to uppercase even for HTML elements, unlike
	 * getTagName.
	 */
	string getLocalName();
	/**
	 * 
	 * Gets the _namespace name of this element.  For HTML elements,
	 * it will equal "http://www.w3.org/1999/xhtml".
	 * 
	 * 
	 */
	string getNamespaceURI();
	/**
	 * 
	 * Gets an attribute declared on this element.
	 * 
	 * @param name an attribute name.
	 * @return the attribute's value, or null if the attribute doesn't
	 * exist.
	 */
	string getAttribute(string name);
	/**
	 * 
	 * Gets an attribute of this element, with the given _namespace
	 * name and local name.
	 * 
	 * @param _namespace the attribute's _namespace name.
	 * @param name the attribute's local name.
	 * @return the attribute's value, or null if the attribute doesn't
	 * exist.
	 */
	string getAttributeNS(string _namespace, string name);

	/**
	 * 
	 * Gets all descendents, both direct and indirect, that have
	 * the specified tag name, using ASCII case-insensitive matching.
	 * 
	 * @param tagName A tag name.
	 */
	IList<IElement> getElementsByTagName(string tagName);
}

}
