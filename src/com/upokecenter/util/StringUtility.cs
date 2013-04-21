/*
Written in 2013 by Peter Occil.  Released to the public domain.
Public domain dedication: http://creativecommons.org/publicdomain/zero/1.0/
 */
namespace com.upokecenter.util {
using System;
using System.Text;
using System.Collections.Generic;

/**
 * 
 * Contains utility methods for working with strings.
 * 
 * @author Peter
 *
 */
public sealed class StringUtility {
	private StringUtility(){}

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
		StringBuilder builder=new StringBuilder();
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

	/**
	 * 
	 * Splits a _string by a delimiter.  If the _string ends
	 * with the delimiter, the result will end with an
	 * empty _string.  If the _string begins with the
	 * delimiter, the result will start with an empty _string.
	 * If the delimiter is null or empty,  exception.
	 * 
	 * 
	 * @param s a _string to split.
	 * @param delimiter a _string to signal where each substring
	 * begins and ends.
	 * 
	 */
	public static string[] splitAt(string s, string delimiter){
		if(delimiter==null ||
				delimiter.Length==0)throw new ArgumentException();
		if(s==null || s.Length==0)return emptyStringArray;
		int index=0;
		bool first=true;
		List<string> strings=null;
		int delimLength=delimiter.Length;
		if(delimLength==0)return emptyStringArray;
		while(true){
			int index2=s.IndexOf(delimiter,index,StringComparison.Ordinal);
			if(index2<0){
				if(first)return new string[]{s};
        strings.Add(s.Substring(index));
				break;
			} else {
				if(first) {
					strings=new List<string>();
					first=false;
				}
				string newstr=s.Substring(index,(index2)-(index));
				strings.Add(newstr);
				index=index2+delimLength;
			}
		}
		return PeterO.Support.Collections.ToArray(strings);
	}
	/**
	 * Returns true if this _string is null or empty.
	 */
	public static bool isNullOrEmpty(string s){
		return (s==null || s.Length==0);
	}

	/**
	 * Returns true if this _string is null, empty, or consists
	 * entirely of space characters. The space characters are
	 * U+0009, U+000A, U+000C, U+000D, and U+0020.
	 */
	public static bool isNullOrSpaces(string s){
		if(s==null)return true;
		int len=s.Length;
		int index=0;
		while(index<len){
			char c=s[index];
			if(c!=0x09 && c!=0x0a && c!=0x0c && c!=0x0d && c!=0x20)
				return false;
			index++;
		}
		return true;
	}
	/**
	 * Returns a _string with the leading and
	 * trailing space characters removed.  The space characters are
	 * U+0009, U+000A, U+000C, U+000D, and U+0020.
	 * @param s a _string. Can be null.
	 * 
	 */
	public static string trimSpaces(string s){
		if(s==null || s.Length==0)return s;
		int index=0;
		int sLength=s.Length;
		while(index<sLength){
			char c=s[index];
			if(c!=0x09 && c!=0x0a && c!=0x0c && c!=0x0d && c!=0x20){
				break;
			}
			index++;
		}
		if(index==sLength)return "";
		int startIndex=index;
		index=sLength-1;
		while(index>=0){
			char c=s[index];
			if(c!=0x09 && c!=0x0a && c!=0x0c && c!=0x0d && c!=0x20)
				return s.Substring(startIndex,(index+1)-(startIndex));
			index--;
		}
		return "";
	}

	/**
	 * 
	 * Splits a _string separated by space characters.
	 * This method acts as though it strips leading and
	 * trailing space
	 * characters from the _string before splitting it.
	 * The space characters are
	 * U+0009, U+000A, U+000C, U+000D, and U+0020.
	 * 
	 * @param s a _string. Can be null.
	 * @return an array of all items separated by spaces. If _string
	 * is null or empty, returns an empty array.
	 */
	public static string[] splitAtSpaces(string s){
		if(s==null || s.Length==0)return emptyStringArray;
		int index=0;
		int sLength=s.Length;
		while(index<sLength){
			char c=s[index];
			if(c!=0x09 && c!=0x0a && c!=0x0c && c!=0x0d && c!=0x20){
				break;
			}
			index++;
		}
		if(index==s.Length)return emptyStringArray;
		List<string> strings=null;
		int lastIndex=index;
		while(index<sLength){
			char c=s[index];
			if(c==0x09 || c==0x0a || c==0x0c || c==0x0d || c==0x20){
				if(lastIndex>=0) {
					if(strings==null) {
						strings=new List<string>();
					}
					strings.Add(s.Substring(lastIndex,(index)-(lastIndex)));
					lastIndex=-1;
				}
			} else {
				if(lastIndex<0) {
					lastIndex=index;
				}
			}
			index++;
		}
		if(lastIndex>=0){
			if(strings==null)
				return new string[]{s.Substring(lastIndex,(index)-(lastIndex))};
			strings.Add(s.Substring(lastIndex,(index)-(lastIndex)));
		}
		return PeterO.Support.Collections.ToArray(strings);
	}

	/**
	 * 
	 * Splits a _string separated by space characters other than
	 * form feed. This method acts as though it strips
	 * leading and trailing space
	 * characters from the _string before splitting it.
	 * The space characters used
	 * here are U+0009, U+000A, U+000D, and U+0020.
	 * 
	 * @param s a _string. Can be null.
	 * @return an array of all items separated by spaces. If _string
	 * is null or empty, returns an empty array.
	 */
	public static string[] splitAtNonFFSpaces(string s){
		if(s==null || s.Length==0)return emptyStringArray;
		int index=0;
		int sLength=s.Length;
		while(index<sLength){
			char c=s[index];
			if(c!=0x09 && c!=0x0a && c!=0x0d && c!=0x20){
				break;
			}
			index++;
		}
		if(index==s.Length)return emptyStringArray;
		List<string> strings=null;
		int lastIndex=index;
		while(index<sLength){
			char c=s[index];
			if(c==0x09 || c==0x0a || c==0x0d || c==0x20){
				if(lastIndex>=0) {
					if(strings==null) {
						strings=new List<string>();
					}
					strings.Add(s.Substring(lastIndex,(index)-(lastIndex)));
					lastIndex=-1;
				}
			} else {
				if(lastIndex<0) {
					lastIndex=index;
				}
			}
			index++;
		}
		if(lastIndex>=0){
			if(strings==null)
				return new string[]{s.Substring(lastIndex,(index)-(lastIndex))};
			strings.Add(s.Substring(lastIndex,(index)-(lastIndex)));
		}
		return PeterO.Support.Collections.ToArray(strings);
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
		StringBuilder builder=new StringBuilder();
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

	/**
	 * Compares two strings in Unicode code point order. Unpaired
	 * surrogates are treated as individual code points.
	 * @param a The first _string
	 * @param b The second _string
	 * @return A value indicating which _string is "less" or "greater".
	 *  0: Both strings are equal or null.
	 *  Less than 0: a is null and b isn't; or the first code point that's
	 *  different is less in A than in B; or b starts with a.
	 *  Greater than 0: b is null and a isn't; or the first code point that's
	 *  different is greater in A than in B; or a starts with b.
	 */
	public static int codePointCompare(string a, string b){
		if(a==null)return (b==null) ? 0 : -1;
		if(b==null)return 1;
		int len=Math.Min(a.Length,b.Length);
		for(int i=0;i<len;i++){
			int ca=a[i];
			int cb=b[i];
			if(ca==cb){
				// normal code units and illegal surrogates
				// are treated as single code points
				if((ca&0xF800)!=0xD800) {
					continue;
				}
				bool incindex=false;
				if(i+1<a.Length && a[i+1]>=0xDC00 && a[i+1]<=0xDFFF){
					ca=0x10000+(ca-0xD800)*0x400+(a[i+1]-0xDC00);
					incindex=true;
				}
				if(i+1<b.Length && b[i+1]>=0xDC00 && b[i+1]<=0xDFFF){
					cb=0x10000+(cb-0xD800)*0x400+(b[i+1]-0xDC00);
					incindex=true;
				}
				if(ca!=cb)return ca-cb;
				if(incindex) {
					i++;
				}
			} else {
				if((ca&0xF800)!=0xD800 && (cb&0xF800)!=0xD800)
					return ca-cb;
				if(ca>=0xd800 && ca<=0xdbff && i+1<a.Length && a[i+1]>=0xDC00 && a[i+1]<=0xDFFF){
					ca=0x10000+(ca-0xD800)*0x400+(a[i+1]-0xDC00);
				}
				if(cb>=0xd800 && cb<=0xdbff && i+1<b.Length && b[i+1]>=0xDC00 && b[i+1]<=0xDFFF){
					cb=0x10000+(cb-0xD800)*0x400+(b[i+1]-0xDC00);
				}
				return ca-cb;
			}
		}
		if(a.Length==b.Length)return 0;
		return (a.Length<b.Length) ? -1 : 1;
	}
}

}
