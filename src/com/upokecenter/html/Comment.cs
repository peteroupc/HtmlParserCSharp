namespace com.upokecenter.html {
using System;
internal class Comment : Node, IComment {
	string data;

	public string getData(){
		return data;
	}


	internal void setData(string data){
		this.data=data;
	}

	internal Comment() : base(NodeType.COMMENT_NODE) {
	}

	internal override string toDebugString(){
		return "<!-- "+getData().ToString()+" -->\n";
	}


	public override string getTextContent(){
		return null;
	}

}
}
