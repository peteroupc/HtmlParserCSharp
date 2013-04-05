/*
Written in 2013 by Peter Occil.  Released to the public domain.
Public domain dedication: http://creativecommons.org/publicdomain/zero/1.0/
*/
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
