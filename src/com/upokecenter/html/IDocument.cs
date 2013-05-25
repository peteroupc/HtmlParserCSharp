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
 * Represents an HTML document.  This is the root of
 * the document hierarchy.
 * 
 * @author Peter
 *
 */
public interface IDocument : INode {
	/**
	 * 
	 * Gets the character encoding used in this document.
	 * 
	 * @return A character encoding name.
	 */
	string getCharacterSet();
	/**
	 * 
	 * Gets the document type of this document, if any.
	 * 
	 * 
	 */
	 IDocumentType getDoctype();
	/**
	 * 
	 * Gets the root element of this document.
	 * 
	 * 
	 */
	 IElement getDocumentElement();
	IElement getElementById(string id);
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
	 * Gets the document's address
	 * 
	 * @return An absolute URL.
	 */
	string getURL();
}

}
