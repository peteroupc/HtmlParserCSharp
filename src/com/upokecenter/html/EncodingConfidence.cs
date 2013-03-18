namespace com.upokecenter.html {
using System;

sealed class EncodingConfidence {
	public override sealed string ToString() {
		return "EncodingConfidence [confidence=" + confidence + ", encoding="
				+ encoding + "]";
	}
	int confidence;
	public int getConfidence() {
		return confidence;
	}
	public string getEncoding() {
		return encoding;
	}
	string encoding;
	public static readonly int Irrelevant=0;
	public static readonly int Tentative=1;
	public static readonly int Certain=2;

	public EncodingConfidence(string e){
		encoding=e;
		confidence=Tentative;
	}
	public EncodingConfidence(string e, int c){
		encoding=e;
		confidence=c;
	}
	public static readonly EncodingConfidence UTF16BE=
			new EncodingConfidence("utf-16be",Certain);
	public static readonly EncodingConfidence UTF16LE=
			new EncodingConfidence("utf-16le",Certain);
	public static readonly EncodingConfidence UTF8=
			new EncodingConfidence("utf-8",Certain);
	public static readonly EncodingConfidence UTF8_TENTATIVE=
			new EncodingConfidence("utf-8",Tentative);
}
}
