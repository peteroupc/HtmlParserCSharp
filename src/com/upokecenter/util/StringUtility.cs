namespace com.upokecenter.util {
using System;

using System.Collections.Generic;

public sealed class StringUtility {
	private StringUtility(){}

	public static bool isChar(int c, string asciiChars){
		return (c>=0 && c<=0x7F && asciiChars.IndexOf((char)c)>=0);
	}

	public static bool startsWith(string str, string o, int index){
		if(str==null || o==null || index<0 || index>=str.Length)
			throw new ArgumentException();
		int endpos=o.Length+index;
		if(endpos>str.Length)return false;
		return str.Substring(index,(endpos)-(index)).Equals(o);
	}

	public static string toLowerCaseAscii(string s){
		if(s==null)return null;
		int len=s.Length;
		char c=(char)0;
		bool hasUpperCase=false;
		for(int i=0;i<len;i++){
			c=s[i];
			if(c>='A' && c<='Z'){
				hasUpperCase=true;
				break;
			}
		}
		if(!hasUpperCase)
			return s;
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		for(int i=0;i<len;i++){
			c=s[i];
			if(c>='A' && c<='Z'){
				builder.Append((char)(c+0x20));
			} else {
				builder.Append(c);
			}
		}
		return builder.ToString();
	}

	private static readonly string[] emptyStringArray=new string[0];

	public static string[] splitAt(string s, string delimiter){
		if(delimiter==null)throw new ArgumentException();
		if(s==null || s.Length==0)return emptyStringArray;
		int index=0;
		bool first=true;
		List<string> strings=null;
		int delimLength=delimiter.Length;
		if(delimLength==0)return emptyStringArray;
		while(true){
			int index2=s.IndexOf(delimiter,index,StringComparison.Ordinal);
			if(index2<0){
				if(first)return emptyStringArray;
				strings.Add(s.Substring(index));
				break;
			} else {
				if(first) {
					strings=new List<string>();
					first=false;
				}
				strings.Add(s.Substring(index,(index2)-(index)));
				index=index2+delimLength;
			}
		}
		return strings.ToArray();
	}


	public static string toUpperCaseAscii(string s) {
		if(s==null)return null;
		int len=s.Length;
		char c=(char)0;
		bool hasLowerCase=false;
		for(int i=0;i<len;i++){
			c=s[i];
			if(c>='a' && c<='z'){
				hasLowerCase=true;
				break;
			}
		}
		if(!hasLowerCase)
			return s;
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		for(int i=0;i<len;i++){
			c=s[i];
			if(c>='a' && c<='z'){
				builder.Append((char)(c-0x20));
			} else {
				builder.Append(c);
			}
		}
		return builder.ToString();
	}
}

}
