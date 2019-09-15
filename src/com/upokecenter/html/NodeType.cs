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

namespace Com.Upokecenter.Html {
    /// <summary>Contains constants for node types.</summary>
public sealed class NodeType {
    /// <summary>A document node.</summary>
  public const int DOCUMENT_NODE = 9;

    /// <summary>A comment node.</summary>
  public const int COMMENT_NODE = 8;

    /// <summary>An HTML element node.</summary>
  public const int ELEMENT_NODE = 1;

    /// <summary>A node containing text.</summary>
  public const int TEXT_NODE = 3;

    /// <summary>A DOCTYPE node.</summary>
  public const int DOCUMENT_TYPE_NODE = 10;

    /// <summary>Not documented yet.</summary>
  public const int PROCESSING_INSTRUCTION_NODE = 7;

  private NodeType() {
}
}
}
