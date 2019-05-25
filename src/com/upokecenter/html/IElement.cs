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

namespace com.upokecenter.html {
    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="T:com.upokecenter.html.IElement"]/*'/>
public interface IElement : INode {
    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IElement.getAttribute(System.String)"]/*'/>
  string getAttribute(string name);

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IElement.getAttributeNS(System.String,System.String)"]/*'/>
  string getAttributeNS(string _namespace, string name);

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IElement.getAttributes"]/*'/>
  IList<IAttr> getAttributes();

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IElement.getElementById(System.String)"]/*'/>
  IElement getElementById(string id);

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IElement.getElementsByTagName(System.String)"]/*'/>
  IList<IElement> getElementsByTagName(string tagName);

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IElement.getId"]/*'/>
  string getId();

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IElement.getInnerHTML"]/*'/>
  string getInnerHTML();

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IElement.getLocalName"]/*'/>
  string getLocalName();

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IElement.getNamespaceURI"]/*'/>
  string getNamespaceURI();

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IElement.getPrefix"]/*'/>
  string getPrefix();

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.IElement.getTagName"]/*'/>
  string getTagName();
}
}
