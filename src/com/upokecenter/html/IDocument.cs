using System;
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
/// <summary>* Represents an HTML document.</summary>
public interface IDocument : INode {
    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IDocument.getCharset"]/*'/>
  string getCharset();

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IDocument.getDoctype"]/*'/>
   IDocumentType getDoctype();

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IDocument.getDocumentElement"]/*'/>
   IElement getDocumentElement();

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IDocument.getElementById(System.String)"]/*'/>
  IElement getElementById(string id);

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IDocument.getElementsByTagName(System.String)"]/*'/>
  IList<IElement> getElementsByTagName(string _string);

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IDocument.getURL"]/*'/>
  string getURL();
}
}
