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

namespace com.upokecenter.html {
using System;



sealed class DocumentType : Node, IDocumentType {

	internal string publicId;
	internal string systemId;
	internal string name;

	public DocumentType() : base(NodeType.DOCUMENT_TYPE_NODE) {
	}
	internal override sealed string toDebugString(){
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		builder.Append("<!DOCTYPE "+name);
		if((publicId!=null && publicId.Length>0) ||
				(systemId!=null && systemId.Length>0)){
			builder.Append(publicId!=null && publicId.Length>0 ? " \""+publicId.Replace("\n","~~~~")+"\"" : " \"\"");
			builder.Append(systemId!=null && systemId.Length>0 ? " \""+systemId.Replace("\n","~~~~")+"\"" : " \"\"");
		}
		builder.Append(">\n");
		return builder.ToString();
	}
	public string getName() {
		return name;
	}
	public string getPublicId() {
		return publicId;
	}
	public string getSystemId() {
		return systemId;
	}


	public override sealed string getTextContent(){
		return null;
	}
}
}
