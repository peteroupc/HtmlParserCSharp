/*
If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/

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

    /// <summary>* Represents an HTML element. @author Peter.</summary>
public interface IElement : INode {
    /// <summary>* Gets an attribute declared on this element. @param name
    /// an attribute name. @return the attribute's value, or null if the
    /// attribute doesn't exist.</summary>
  string getAttribute(string name);

    /// <summary>* Gets an attribute of this element, with the given
    /// _namespace name and local name. @param _namespace the attribute's
    /// _namespace name. @param name the attribute's local name. @return
    /// the attribute's value, or null if the attribute doesn't
    /// exist.</summary>
  string getAttributeNS(string _namespace, string name);

    /// <summary>Gets a list of all attributes declared on this
    /// element.</summary>
  IList<IAttr> getAttributes();

    /// <summary>Gets all descendents, both direct and indirect, that have
    /// the specified id, using case-sensitive matching. @param
    /// id.</summary>
  IElement getElementById(string id);

    /// <summary>* Gets all descendents, both direct and indirect, that
    /// have the specified tag name, using ASCII case-insensitive matching.
    /// @param tagName A tag name.</summary>
  IList<IElement> getElementsByTagName(string tagName);

    /// <summary>Gets the value of the id attribute on this element.
    /// @return the value of the id attribute, or null if it doesn't
    /// exist.</summary>
  string getId();

    /// <summary>Gets a serialized form of this HTML element. @return a
    /// _string consisting of the serialized form of this element's
    /// children, in HTML.</summary>
  string getInnerHTML();

    /// <summary>* Gets the element's local name. For elements with no
    /// _namespace, this will equal the element's tag name. @return the
    /// element's local name. This method doesn't convert it to uppercase
    /// even for HTML elements, unlike getTagName.</summary>
  string getLocalName();

    /// <summary>* Gets the _namespace name of this element. For HTML
    /// elements, it will equal "http://www.w3.org/1999/xhtml".</summary>
  string getNamespaceURI();
  string getPrefix();

    /// <summary>* Gets the name of the element as used on its HTML tags.
    /// @return the element's tag name. For HTML elements, an uppercase
    /// version of the name will be returned.</summary>
  string getTagName();
}
}
