namespace com.upokecenter.html {
using System;

using com.upokecenter.util;
internal class Text : Node, IText {
	public IntList text=new IntList();
	public Text() : base(NodeType.TEXT_NODE) {
	}

	internal override string toDebugString(){
		return "\""+text.ToString().Replace("\n","~~~~")+"\"\n";
	}

	public override string getTextContent(){
		return text.ToString();
	}

	public string getData(){
		return text.ToString();
	}

}
}
