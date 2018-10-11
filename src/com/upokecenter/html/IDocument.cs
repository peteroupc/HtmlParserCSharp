using System.Collections.Generic;

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
/// <summary>* Represents an HTML document. This is the root of the
public interface IDocument : INode {
    /// <summary>* Gets the character encoding used in this document.
    /// @return A character encoding name.</summary>
    /// <returns>Not documented yet.</returns>
  string getCharset();

    /// <summary>* Gets the document type of this document, if
    /// any.</summary>
    /// <returns>Not documented yet.</returns>
   IDocumentType getDoctype();

    /// <summary>* Gets the root element of this document.</summary>
    /// <returns>Not documented yet.</returns>
   IElement getDocumentElement();

    /// <summary>Not documented yet.</summary>
    /// <param name='id'>Not documented yet.</param>
    /// <returns>Not documented yet.</returns>
  IElement getElementById(string id);

    /// <summary>* Gets all descendents, both direct and indirect, that
    /// have the specified tag name, using ASCII case-insensitive matching.
    /// @param _string A tag name.</summary>
    /// <param name='_string'>Not documented yet.</param>
    /// <returns>Not documented yet.</returns>
  IList<IElement> getElementsByTagName(string _string);

    /// <summary>Gets the document's address @return An absolute
    /// URL.</summary>
    /// <returns>Not documented yet.</returns>
  string getURL();
}
}
