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

namespace com.upokecenter.html {
    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="T:com.upokecenter.html.NodeType"]/*'/>
public sealed class NodeType {
    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="F:com.upokecenter.html.NodeType.DOCUMENT_NODE"]/*'/>
  public const int DOCUMENT_NODE = 9;

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="F:com.upokecenter.html.NodeType.COMMENT_NODE"]/*'/>
  public const int COMMENT_NODE = 8;

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="F:com.upokecenter.html.NodeType.ELEMENT_NODE"]/*'/>
  public const int ELEMENT_NODE = 1;

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="F:com.upokecenter.html.NodeType.TEXT_NODE"]/*'/>
  public const int TEXT_NODE = 3;

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="F:com.upokecenter.html.NodeType.DOCUMENT_TYPE_NODE"]/*'/>
  public const int DOCUMENT_TYPE_NODE = 10;

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="F:com.upokecenter.html.NodeType.PROCESSING_INSTRUCTION_NODE"]/*'/>
  public const int PROCESSING_INSTRUCTION_NODE = 7;

  private NodeType() {
}
}
}
