namespace com.upokecenter.util {
using System;

using System.IO;


public sealed class StringCharacterInput : ICharacterInput {

	string str=null;
	int pos=0;
	public StringCharacterInput(string str){
		if(str==null)
			throw new ArgumentException();
		this.str=str;
	}

	public int read(int[] buf, int offset, int unitCount)  {
		if(offset<0 || unitCount<0 || offset+unitCount>buf.Length)
			throw new ArgumentOutOfRangeException();
		if(unitCount==0)return 0;
		int count=0;
		while(pos<str.Length && unitCount>0){
			int c=str[pos];
			if(c>=0xD800 && c<=0xDBFF && pos+1<str.Length &&
					str[pos+1]>=0xDC00 && str[pos+1]<=0xDFFF){
				// Get the Unicode code point for the surrogate pair
				c=0x10000+(c-0xD800)*0x400+(str[pos+1]-0xDC00);
				pos++;
			}
			buf[offset]=c;
			offset++;
			unitCount--;
			count++;
			pos++;
		}
		return count==0 ? -1 : count;
	}

	public int read()  {
		if(pos<str.Length){
			int c=str[pos];
			if(c>=0xD800 && c<=0xDBFF && pos+1<str.Length &&
					str[pos+1]>=0xDC00 && str[pos+1]<=0xDFFF){
				// Get the Unicode code point for the surrogate pair
				c=0x10000+(c-0xD800)*0x400+(str[pos+1]-0xDC00);
				pos++;
			}
			pos++;
			return c;
		}
		return -1;
	}

}

}
