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
