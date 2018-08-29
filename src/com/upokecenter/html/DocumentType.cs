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
using System.Text;

sealed class DocumentType : Node, IDocumentType {
  private string valuePublicId;
  private string valueSystemId;
  private string valueName;

  public DocumentType() : base(NodeType.DOCUMENT_TYPE_NODE) {
  }

  public string getName() {
    return this.valueName;
  }

  public override sealed string getNodeName() {
    return this.getName();
  }

  public string getPublicId() {
    return this.valuePublicId;
  }

  public string getSystemId() {
    return this.valueSystemId;
  }

  public override sealed string getTextContent() {
    return null;
  }

  internal override sealed string toDebugString() {
    var builder = new StringBuilder();
    builder.Append("<!DOCTYPE " + this.valueName);
    if ((this.valuePublicId != null && this.valuePublicId.Length > 0) ||
        (this.valueSystemId != null && this.valueSystemId.Length > 0)) {
      builder.Append(this.valuePublicId != null && this.valuePublicId.Length >
        0 ? " \"" +this.valuePublicId.Replace("\n", "~~~~") + "\"" : " \"\"");
      builder.Append(this.valueSystemId != null && this.valueSystemId.Length >
        0 ? " \"" +this.valueSystemId.Replace("\n", "~~~~") + "\"" : " \"\"");
    }
    builder.Append(">\n");
    return builder.ToString();
  }
}
}
