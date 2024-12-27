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

using System;
using System.Collections.Generic;

namespace Com.Upokecenter.Html {
  /// <summary>Represents an HTML element.</summary>
  public interface IElement : INode {
    /// <summary>Gets an attribute declared on this element.</summary>
    /// <param name='name'>An attribute name.</param>
    /// <returns>The return value is not documented yet.</returns>
    string GetAttribute (string name);

    /// <summary>Gets an attribute of this element, with the given
    /// _namespace name and local name. @param _namespace the attribute's
    /// _namespace name. @param name the attribute's local name.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetAttributeNS (string _namespace, string name);

    /// <summary>Gets a list of all attributes declared on this
    /// element.</summary>
    /// <returns>The return value is not documented yet.</returns>
    IList<IAttr> GetAttributes();

    /// <summary>Gets all descendents, both direct and indirect, that have
    /// the specified id, using case-sensitive matching. @param
    /// id.</summary>
    /// <param name='id'>The parameter <paramref name='id'/> is a text
    /// string.</param>
    /// <returns>The return value is not documented yet.</returns>
    IElement GetElementById (string id);

    /// <summary>Gets all descendents, both direct and indirect, that have
    /// the specified tag name, using a basic case-insensitive comparison.
    /// (Two strings are equal in such a comparison, if they match after
    /// converting the basic uppercase letters A to Z (U+0041 to U+005A) in
    /// both strings to basic lowercase letters.).</summary>
    /// <param name='tagName'>A tag name.</param>
    /// <returns>The return value is not documented yet.</returns>
    IList<IElement> GetElementsByTagName (string tagName);

    /// <summary>Gets the value of the id attribute on this
    /// element.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetId();

    /// <summary>Gets a serialized form of this HTML element.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetInnerHTML();

    /// <summary>Gets the element's local name. For elements with no
    /// _namespace, this will equal the element's tag name.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetLocalName();

    /// <summary>Gets the _namespace name of this element. For HTML
    /// elements, it will equal "http://www.w3.org/1999/xhtml".</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetNamespaceURI();

    /// <summary>Not documented yet.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetPrefix();

    /// <summary>Gets the name of the element as used on its HTML
    /// tags.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetTagName();
  }
}
