using System.Text;

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
  internal sealed class DocumentType : Node, IDocumentType {
    public DocumentType(string name, string pub, string sys)
      : base(NodeType.DOCUMENT_TYPE_NODE) {
      this.Name = name;
      this.PublicId = pub;
      this.SystemId = sys;
    }

    public string SystemId {
      get;
      private set;
    }

    public string PublicId {
      get;
      private set;
    }

    public string Name {
      get;
      private set;
    }

    public override sealed string GetNodeName() {
      return this.GetName();
    }

    public string GetPublicId() {
      return this.PublicId;
    }

    public override sealed string GetTextContent() {
      return null;
    }

    public string GetSystemId() {
      return this.SystemId;
    }

    public string GetName() {
      return this.Name;
    }

    internal override sealed string ToDebugString() {
      var builder = new StringBuilder();
      builder.Append("<!DOCTYPE " + this.Name);
      if ((this.PublicId != null && this.PublicId.Length > 0) ||
        (this.SystemId != null && this.SystemId.Length > 0)) {
        builder.Append(this.PublicId != null && this.PublicId.Length >
          0 ? " \"" + this.PublicId.Replace("\n", "~~~~") + "\"" : " \"\"");
        builder.Append(this.SystemId != null && this.SystemId.Length >
          0 ? " \"" + this.SystemId.Replace("\n", "~~~~") + "\"" : " \"\"");
      }
      builder.Append(">\n");
      return builder.ToString();
    }
  }
}
