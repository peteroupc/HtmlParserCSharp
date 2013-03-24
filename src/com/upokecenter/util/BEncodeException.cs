namespace com.upokecenter.util {
using System;

using System.IO;

public class BEncodeException : Exception {

	public BEncodeException(string _string) : base(_string) {
	}

	public BEncodeException(string s, IOException e) : base(s,e) {
	}

	/**
	 * 
	 */
	

}

}
