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

using System;
using System.Collections.Generic;

namespace Com.Upokecenter.Html {
    /// <summary>Represents a node in the document object model (DOM). All
    /// DOM objects implement this interface.</summary>
public interface INode {
    /// <summary>Returns the _base URL of this node. URLs on this node are
    /// resolved relative to this URL.</summary>
    /// <returns>The return value is not documented yet.</returns>
  string GetBaseURI();

    /// <summary>Gets the direct children of this node.</summary>
    /// <returns>The return value is not documented yet.</returns>
  IList<INode> GetChildNodes();

    /// <summary>Gets the language of this node. Not defined in the DOM
    /// specification.</summary>
    /// <returns>The return value is not documented yet.</returns>
  string GetLanguage();

    /// <summary>Gets the name of this node. For HTML elements, this will
    /// be the same as the tag name.</summary>
    /// <returns>The return value is not documented yet.</returns>
  string GetNodeName();

    /// <summary>Returns the type of node represented by this
    /// object.</summary>
    /// <returns>The return value is not documented yet.</returns>
  int GetNodeType();

    /// <summary>Gets the document that owns this node.</summary>
    /// <returns>The return value is not documented yet.</returns>
  IDocument GetOwnerDocument();

    /// <summary>Gets the parent node of this node.</summary>
    /// <returns>The return value is not documented yet.</returns>
  INode GetParentNode();

    /// <summary>Gets all the text found within this element.</summary>
    /// <returns>The return value is not documented yet.</returns>
  string GetTextContent();

    /// <summary>Not documented yet.</summary>
    /// <param name='node'>The parameter <paramref name='node'/> is
    /// a.Upokecenter.Html.INode object.</param>
    void AppendChild(INode node);

    /// <summary>Not documented yet.</summary>
    /// <param name='node'>The parameter <paramref name='node'/> is
    /// a.Upokecenter.Html.INode object.</param>
    void RemoveChild(INode node);
}
}
