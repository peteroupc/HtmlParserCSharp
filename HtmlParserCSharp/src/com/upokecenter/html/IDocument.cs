using System;
using System.Collections.Generic;

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

namespace Com.Upokecenter.Html {
  /// <summary>* Represents an HTML document.</summary>
  public interface IDocument : INode {
    /// <summary>Gets the character encoding used in this
    /// document.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetCharset();

    /// <summary>Gets the document type of this document, if any.</summary>
    /// <returns>The return value is not documented yet.</returns>
    IDocumentType GetDoctype();

    /// <summary>Gets the root element of this document.</summary>
    /// <returns>The return value is not documented yet.</returns>
    IElement GetDocumentElement();

    /// <summary>Not documented yet.</summary>
    /// <param name='id'>The parameter <paramref name='id'/> is a text
    /// string.</param>
    /// <returns>The return value is not documented yet.</returns>
    IElement GetElementById(string id);

    /// <summary>Gets all descendents, both direct and indirect, that have
    /// the specified tag name, using a basic case-insensitive comparison.
    /// (Two strings are equal in such a comparison, if they match after
    /// converting the basic uppercase letters A to Z (U+0041 to U+005A) in
    /// both strings to basic lowercase letters.).</summary>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <missing-param name='stringValue'/>
    /// <param name='stringValue'>A tag name.</param>
    /// <returns>The return value is not documented yet.</returns>
    /// <param name='tagName'>The parameter <paramref name='tagName'/> is
    /// not documented yet.</param>
    /// <param name='tagName'>The parameter <paramref name='tagName'/> is
    /// not documented yet.</param>
    IList<IElement> GetElementsByTagName(string tagName);

    /// <summary>Gets the document's address.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetURL();
  }
}
