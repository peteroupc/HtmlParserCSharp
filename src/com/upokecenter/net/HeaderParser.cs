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
namespace com.upokecenter.net {
using System;
using System.Text;
using System.Globalization;
using com.upokecenter.util;




public sealed class HeaderParser {

	private HeaderParser(){}
	public static string formatHttpDate(long date){
		int[] components=DateTimeUtility.getGmtDateComponents(date);
		int dow=components[7]; // 1 to 7
		int month=components[1]; // 1 to 12
		string dayofweek=null;
		if(dow==1) {
			dayofweek="Sun, ";
		}
		else if(dow==2) {
			dayofweek="Mon, ";
		}
		else if(dow==3) {
			dayofweek="Tue, ";
		}
		else if(dow==4) {
			dayofweek="Wed, ";
		}
		else if(dow==5) {
			dayofweek="Thu, ";
		}
		else if(dow==6) {
			dayofweek="Fri, ";
		}
		else if(dow==7) {
			dayofweek="Sat, ";
		}
		if(dayofweek==null)return "";
		string[] months={
				""," Jan "," Feb "," Mar "," Apr ",
				" May "," Jun "," Jul "," Aug ",
				" Sep "," Oct "," Nov "," Dec "
		};
		if(month<1||month>12)return "";
		string monthstr=months[month];
		StringBuilder builder=new StringBuilder();
		builder.Append(dayofweek);
		builder.Append((char)('0'+((components[2]/10)%10)));
		builder.Append((char)('0'+((components[2])%10)));
		builder.Append(monthstr);
		builder.Append((char)('0'+((components[0]/1000)%10)));
		builder.Append((char)('0'+((components[0]/100)%10)));
		builder.Append((char)('0'+((components[0]/10)%10)));
		builder.Append((char)('0'+((components[0])%10)));
		builder.Append(' ');
		builder.Append((char)('0'+((components[3]/10)%10)));
		builder.Append((char)('0'+((components[3])%10)));
		builder.Append(':');
		builder.Append((char)('0'+((components[4]/10)%10)));
		builder.Append((char)('0'+((components[4])%10)));
		builder.Append(':');
		builder.Append((char)('0'+((components[5]/10)%10)));
		builder.Append((char)('0'+((components[5])%10)));
		builder.Append(" GMT");
		return builder.ToString();
	}

	private static int parseMonth(string v, int index){
		if(com.upokecenter.util.StringUtility.startsWith(v,"Jan",index))return 1;
		if(com.upokecenter.util.StringUtility.startsWith(v,"Feb",index))return 2;
		if(com.upokecenter.util.StringUtility.startsWith(v,"Mar",index))return 3;
		if(com.upokecenter.util.StringUtility.startsWith(v,"Apr",index))return 4;
		if(com.upokecenter.util.StringUtility.startsWith(v,"May",index))return 5;
		if(com.upokecenter.util.StringUtility.startsWith(v,"Jun",index))return 6;
		if(com.upokecenter.util.StringUtility.startsWith(v,"Jul",index))return 7;
		if(com.upokecenter.util.StringUtility.startsWith(v,"Aug",index))return 8;
		if(com.upokecenter.util.StringUtility.startsWith(v,"Sep",index))return 9;
		if(com.upokecenter.util.StringUtility.startsWith(v,"Oct",index))return 10;
		if(com.upokecenter.util.StringUtility.startsWith(v,"Nov",index))return 11;
		if(com.upokecenter.util.StringUtility.startsWith(v,"Dec",index))return 12;
		return -1;
	}

	private static int parse2Digit(string v, int index){
		int value=0;
		char c=(char)0;
		if(index<v.Length && (c=v[index])>='0' && c<='9'){
			value+=10*(c-'0'); index++;
		} else return -1;
		if(index<v.Length && (c=v[index])>='0' && c<='9'){
			value+=(c-'0'); index++;
		} else return -1;
		return value;
	}

	private static int parse4Digit(string v, int index){
		int value=0;
		char c=(char)0;
		if(index<v.Length && (c=v[index])>='0' && c<='9'){
			value+=1000*(c-'0'); index++;
		} else return -1;
		if(index<v.Length && (c=v[index])>='0' && c<='9'){
			value+=100*(c-'0'); index++;
		} else return -1;
		if(index<v.Length && (c=v[index])>='0' && c<='9'){
			value+=10*(c-'0'); index++;
		} else return -1;
		if(index<v.Length && (c=v[index])>='0' && c<='9'){
			value+=(c-'0'); index++;
		} else return -1;
		return value;
	}

	private static int parsePadded2Digit(string v, int index){
		int value=0;
		char c=(char)0;
		if(index<v.Length && v[index]==' '){
			value=0; index++;
		} else if(index<v.Length && (c=v[index])>='0' && c<='9'){
			value+=10*(c-'0'); index++;
		} else return -1;
		if(index<v.Length && (c=v[index])>='0' && c<='9'){
			value+=(c-'0'); index++;
		} else return -1;
		return value;
	}

	public static long parseHttpDate(string v, long defaultValue){
		if(v==null)return defaultValue;
		int index=0;
		bool rfc850=false;
		bool asctime=false;
		if(v.StartsWith("Mon",StringComparison.Ordinal)||v.StartsWith("Sun",StringComparison.Ordinal)||v.StartsWith("Fri",StringComparison.Ordinal)){
			if(com.upokecenter.util.StringUtility.startsWith(v,"day,",3)){
				rfc850=true;
				index=8;
			} else {
				index=3;
			}
		} else if(v.StartsWith("Tue",StringComparison.Ordinal)){
			if(com.upokecenter.util.StringUtility.startsWith(v,"sday,",3)){
				rfc850=true;
				index=9;
			} else {
				index=3;
			}
		} else if(v.StartsWith("Wed",StringComparison.Ordinal)){
			if(com.upokecenter.util.StringUtility.startsWith(v,"nesday,",3)){
				rfc850=true;
				index=11;
			} else {
				index=3;
			}
		} else if(v.StartsWith("Thu",StringComparison.Ordinal)){
			if(com.upokecenter.util.StringUtility.startsWith(v,"rsday,",3)){
				rfc850=true;
				index=10;
			} else {
				index=3;
			}
		} else if(v.StartsWith("Sat",StringComparison.Ordinal)){
			if(com.upokecenter.util.StringUtility.startsWith(v,"urday,",3)){
				rfc850=true;
				index=11;
			} else {
				index=3;
			}
		} else return defaultValue;
		int length=v.Length;
		int month=0,day=0,year=0;
		int hour=0,minute=0,second=0;
		if(rfc850){
			day=parse2Digit(v,index);
			if(day<0)return defaultValue;
			index+=2;
			if(index<length && v[index]!='-')return defaultValue;
			index++;
			month=parseMonth(v,index);
			if(month<0)return defaultValue;
			index+=3;
			if(index<length && v[index]!='-')return defaultValue;
			index++;
			year=parse2Digit(v,index);
			if(day<0)return defaultValue;
			index+=2;
			if(index<length && v[index]!=' ')return defaultValue;
			index++;
			year=DateTimeUtility.convertYear(year);
		} else if(com.upokecenter.util.StringUtility.startsWith(v,",",index)){
			index+=2;
			day=parse2Digit(v,index);
			if(day<0)return defaultValue;
			index+=2;
			if(index<length && v[index]!=' ')return defaultValue;
			index++;
			month=parseMonth(v,index);
			index+=3;
			if(month<0)return defaultValue;
			if(index<length && v[index]!=' ')return defaultValue;
			index++;
			year=parse4Digit(v,index);
			if(day<0)return defaultValue;
			index+=4;
			if(index<length && v[index]!=' ')return defaultValue;
			index++;
		} else if(com.upokecenter.util.StringUtility.startsWith(v," ",index)){
			index+=1;
			asctime=true;
			month=parseMonth(v,index);
			if(month<0)return defaultValue;
			index+=3;
			if(index<length && v[index]!=' ')return defaultValue;
			index++;
			day=parsePadded2Digit(v,index);
			if(day<0)return defaultValue;
			index+=2;
			if(index<length && v[index]!=' ')return defaultValue;
			index++;
		} else return defaultValue;
		hour=parse2Digit(v,index);
		if(hour<0)return defaultValue;
		index+=2;
		if(index<length && v[index]!=':')return defaultValue;
		index++;
		minute=parse2Digit(v,index);
		if(minute<0)return defaultValue;
		index+=2;
		if(index<length && v[index]!=':')return defaultValue;
		index++;
		second=parse2Digit(v,index);
		if(second<0)return defaultValue;
		index+=2;
		if(index<length && v[index]!=' ')return defaultValue;
		index++;
		if(asctime){
			year=parse4Digit(v,index);
			if(day<0)return defaultValue;
			index+=4;
		} else {
			if(!com.upokecenter.util.StringUtility.startsWith(v,"GMT",index))return defaultValue;
			index+=3;
		}
		if(index!=length)return defaultValue;
		// NOTE: Month is one-based
		return DateTimeUtility.toGmtDate(year,month,day,hour,minute,second);
	}

	private static int skipQuotedString(string v, int index){
		// assumes index points to quotation mark
		index++;
		int length=v.Length;
		char c=(char)0;
		while(index<length){
			c=v[index];
			if(c=='\\'){
				if(index+1>=length)
					return length;
				else {
					index++;
				}
			} else if(c=='"')
				return index+1;
			else if(c=='\r'){
				if(index+2>=length ||
						v[index+1]!='\n' ||
						(v[index+2]!=' ' && v[index+2]!='\t'))
					// ill-formed whitespace
					return length;
				index+=2;
			} else if(c=='\n'){
				if(index+1>=length ||
						(v[index+1]!=' ' && v[index+1]!='\t'))
					// ill-formed whitespace
					return length;
				index+=1;
			} else if(c==127 || (c<32 && c!='\t' && c!=' '))
				// ill-formed
				return length;
			index++;
		}
		return index;
	}

	private static int skipQuotedStringNoLws(string v, int index){
		// assumes index points to quotation mark
		index++;
		int length=v.Length;
		char c=(char)0;
		while(index<length){
			c=v[index];
			if(c=='\\'){
				if(index+1>=length)
					return length;
				else {
					index++;
				}
			} else if(c=='"')
				return index+1;
			else if(c==127 || (c<32))
				// ill-formed
				return length;
			index++;
		}
		return index;
	}


	private static int getPositiveNumber(string v, int index){
		int length=v.Length;
		char c=(char)0;
		bool haveNumber=false;
		int startIndex=index;
		string number=null;
		while(index<length){ // skip whitespace
			c=v[index];
			if(c<'0' || c>'9'){
				if(!haveNumber)return -1;
				try {
					number=v.Substring(startIndex,(index)-(startIndex));
					return Int32.Parse(number,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
				} catch(FormatException){
					return Int32.MaxValue;
				}
			} else {
				haveNumber=true;
			}
			index++;
		}
		try {
			number=v.Substring(startIndex,(length)-(startIndex));
			return Int32.Parse(number,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
		} catch(FormatException){
			return Int32.MaxValue;
		}
	}

	internal static int getResponseCode(string s){
		int index=0;
		int length=s.Length;
		if(s.IndexOf("HTTP/",index,StringComparison.Ordinal)!=index)
			return -1;
		index+=5;
		index=skipZeros(s,index);
		if(index>=length || s[index]!='1')
			return -1;
		index++;
		if(index>=length || s[index]!='.')
			return -1;
		index++;
		index=skipZeros(s,index);
		if(index<length && s[index]=='1') {
			index++;
		}
		if(index>=length || s[index]!=' ')
			return -1;
		index++;
		if(index+3>=length)return -1;
		if(skipDigits(s,index)!=index+3 ||
				s[index+3]!=' ')return -1;
		int num=getPositiveNumber(s,index);
		return num;
	}

	static int skipZeros(string v, int index){
		char c=(char)0;
		int length=v.Length;
		while(index<length){
			c=v[index];
			if(c!='0')return index;
			index++;
		}
		return index;
	}
	static int skipDigits(string v, int index){
		char c=(char)0;
		int length=v.Length;
		while(index<length){
			c=v[index];
			if(c<'0' || c>'9')return index;
			index++;
		}
		return index;
	}
	static int skipSpace(string v, int index){
		char c=(char)0;
		int length=v.Length;
		while(index<length){
			c=v[index];
			if(c!=' ')return index;
			index++;
		}
		return index;
	}
	static int skipSpaceOrTab(string v, int index){
		char c=(char)0;
		int length=v.Length;
		while(index<length){
			c=v[index];
			if(c!=' ' && c!='\t')return index;
			index++;
		}
		return index;
	}
	static int skipLinearWhitespace(string v, int index){
		char c=(char)0;
		int length=v.Length;
		while(index<length){ // skip whitespace
			c=v[index];
			if(c=='\r'){
				if(index+2>=length ||
						v[index+1]!='\n' ||
						(v[index+2]!=' ' && v[index+2]!='\t'))
					return index;
				index+=2;
			} else if(c=='\n'){
				// HTTP usually allows only '\r\n' in linear whitespace,
				// but we're being tolerant here
				if(index+1>=length ||
						(v[index+1]!=' ' && v[index+1]!='\t'))
					return index;
				index+=1;

			} else if(c!='\t' && c!=' ')
				return index;
			index++;
		}
		return index;
	}


	internal static int skipDirective(string str, int io){
		int length=str.Length;
		char c=(char)0;
		while(io<length){ // skip non-separator
			c=str[io];
			if(c=='=' || c==',' || c==127 || c<32) {
				break;
			}
			io++;
		}
		io=skipLinearWhitespace(str,io);
		if(io<length && str[io]=='='){
			io++;
			io=skipLinearWhitespace(str,io);
			if(io<length && str[io]=='"') {
				io=skipQuotedString(str,io);
			} else {
				while(io<length){ // skip non-separator
					c=str[io];
					if(c==',' || c==127 || c<32) {
						break;
					}
					io++;
				}
			}
			io=skipLinearWhitespace(str,io);
		}
		if(io<length && str[io]==','){
			io++;
			io=skipLinearWhitespace(str,io);
		} else {
			io=length;
		}
		return io;
	}

	internal static int parseTokenWithDelta(string str, int index, string token, int[] result){
		int length=str.Length;
		int j=0;
		int startIndex=index;
		result[0]=-1;
		for(int i=index;i<length && j<token.Length;i++,j++){
			char c=str[i];
			char cj=token[j];
			if(c!=cj && c!=(cj>='a' && cj<='z' ? cj-0x20 : cj))
				return startIndex;
		}
		index+=token.Length;
		index=skipLinearWhitespace(str,index);
		if(index<length && str[index]=='='){
			index++;
			index=skipLinearWhitespace(str,index);
			int number=getPositiveNumber(str,index);
			while(index<length){
				char c=str[index];
				if(c<'0' || c>'9') {
					break;
				}
				index++;
			}
			result[0]=number;
			if(number<-1)
				return startIndex;
			index=skipLinearWhitespace(str,index);
		} else
			return startIndex;
		if(index>=length)return index;
		if(str[index]==','){
			index++;
			index=skipLinearWhitespace(str,index);
			return index;
		}
		return startIndex;
	}

	internal static int parseToken(string str, int index, string token, bool optionalQuoted){
		int length=str.Length;
		int j=0;
		int startIndex=index;
		for(int i=index;i<length && j<token.Length;i++,j++){
			char c=str[i];
			char cj=token[j];
			if(c!=cj && c!=(cj>='a' && cj<='z' ? cj-0x20 : cj))
				return startIndex;
		}
		index+=token.Length;
		index=skipLinearWhitespace(str,index);
		if(optionalQuoted){
			if(index<length && str[index]=='='){
				index++;
				index=skipLinearWhitespace(str,index);
				if(index<length && str[index]=='"'){
					index=skipQuotedString(str,index);
				} else return startIndex;
				index=skipLinearWhitespace(str,index);
			}
		}
		if(index>=length)return index;
		if(str[index]==','){
			index++;
			index=skipLinearWhitespace(str,index);
			return index;
		}
		return startIndex;
	}

	private static string getQuotedString(string v, int index){
		// assumes index points to quotation mark
		index++;
		int length=v.Length;
		char c=(char)0;
		StringBuilder builder=new StringBuilder();
		while(index<length){
			c=v[index];
			if(c=='\\'){
				if(index+1>=length)
					// ill-formed
					return "";
				builder.Append(v[index+1]);
				index+=2;
				continue;
			} else if(c=='\r' || c=='\n' || c==' ' || c=='\t'){
				int newIndex=skipLinearWhitespace(v,index);
				if(newIndex==index)
					// ill-formed whitespace
					return "";
				builder.Append(' ');
				index=newIndex;
				continue;
			} else if(c=='"')
				// done
				return builder.ToString();
			else if(c==127 || c<32)
				// ill-formed
				return "";
			else {
				builder.Append(c);
				index++;
				continue;
			}
		}
		// ill-formed
		return "";
	}

	private static string getDefaultCharset(string contentType){
		if(contentType.Length>=5){
			char c;
			c=contentType[0];
			if(c!='T' && c!='t')return "";
			c=contentType[1];
			if(c!='E' && c!='e')return "";
			c=contentType[2];
			if(c!='X' && c!='x')return "";
			c=contentType[3];
			if(c!='T' && c!='t')return "";
			c=contentType[4];
			if(c!='/')return "";
			return "ISO-8859-1";
		}
		return "";
	}

	static int skipMimeToken(string str, int index){
		int i=index;
		// type
		while(i<str.Length){
			char c=str[i];
			if(c<=0x20 || c>=0x7F || ((c&0x7F)==c && "()<>@,;:\\\"/[]?=".IndexOf((char)c)>=0)) {
				break;
			}
			i++;
		}
		return i;

	}
	static string getMimeToken(string str, int index){
		int i=skipMimeToken(str,index);
		return str.Substring(index,(i)-(index));
	}
	/**
	 * Extracts the type and subtype from a MIME media
	 * type.  For example, in the _string "text/plain;charset=utf-8",
	 * returns "text/plain".
	 * 
	 * @param str a _string containing a MIME media type.
	 * @param index the index into the _string where the
	 * media type begins. Specify 0 for the beginning of the
	 * _string.
	 * @return the type and subtype, or an empty _string
	 * if the _string is not a valid MIME media type.
	 * The _string will be normalized to ASCII lower-case.
	 */
	public static string getMediaType(string str, int index){
		int i=skipMimeToken(str,index);
		if(i>=str.Length || str[i]!='/')
			return "";
		i++;
		i=skipMimeToken(str,i);
		return StringUtility.toLowerCaseAscii(str.Substring(index,(i)-(index)));
	}

	public static int skipContentType(string data, int index){
		string mediaType=getMediaType(data,index);
		// NOTE: Media type can be omitted
		index+=mediaType.Length;
		while(true){
			int oldindex=index;
			if(index>=data.Length || data[index]!=';')
				return oldindex;
			index++;
			int index2=skipMimeToken(data,index);
			if(index==index2)
				return oldindex;
			index=index2;
			if(index>=data.Length || data[index]!='=')
				return oldindex;
			index++;
			if(index>=data.Length || data[index]=='\"'){
				index=skipQuotedStringNoLws(data,index);
			} else {
				index2=skipMimeToken(data,index);
				if(index==index2)
					return oldindex;
				index=index2;
			}
		}
	}


	internal static int toHexNumber(int c) {
		if(c>='A' && c<='Z')
			return 10+c-'A';
		else if(c>='a' && c<='z')
			return 10+c-'a';
		else if (c>='0' && c<='9')
			return c-'0';
		return -1;
	}

	private static int skipAndAppendQuoted(
			string str, int index, StringBuilder builder){
		int i=index;
		bool slash=false;
		while(i<str.Length){
			char c=str[i];
			//Console.WriteLine(c);
			if(c=='%' && i+2<str.Length){
				int hex1=toHexNumber(str[i+1]);
				int hex2=toHexNumber(str[i+2]);
				c=(char)(hex1*16+hex2);
				if(i==index && c!='"')
					return index;
				if(!slash){
					if(i!=index && c=='"'){
						builder.Append('"');
						return i+1;
					}
					if(c<=0x20 || c>=0x7F)
						return index;
				}
				if(c=='\\' && !slash){
					slash=true;
				} else if(c=='\\'){
					slash=false;
				}
				builder.Append(c);
				i+=3;
				continue;
			}
			if(c<=0x20 || c>=0x7F)
				return index;
			if(!((c&0x7F)==c && "-_.!~*'()".IndexOf((char)c)>=0) &&
					!(c>='A' && c<='Z') &&
					!(c>='a' && c<='z') &&
					!(c>='0' && c<='9'))
				return index;
			// NOTE: Impossible for '"' and '\' to appear
			// here
			if(i==index)
				return index;
			builder.Append(c);
			i++;
		}
		return index;
	}

	private static bool appendUnescapedValue(
			string str, int index, int length, StringBuilder builder){
		int i=index;
		int io=str.IndexOf('%',index);
		bool doquote=true;
		if(io<0 || io>=index+length){
			doquote=false;
		}
		if(doquote)
		{
			builder.Append('\"'); // quote the _string for convenience
		}
		while(i<str.Length){
			char c=str[i];
			//Console.WriteLine(c);
			if(c=='%' && i+2<str.Length){
				int hex1=toHexNumber(str[i+1]);
				int hex2=toHexNumber(str[i+2]);
				c=(char)(hex1*16+hex2);
				if(c<=0x20 || c>=0x7F)
					return false;
				if(doquote && (c=='\\' || c=='"')) {
					builder.Append('\\');
				}
				builder.Append(c);
				i+=3;
				continue;
			}
			if(c<=0x20 || c>=0x7F || ((c&0x7F)==c && "()<>@,;:\\\"/[]?=".IndexOf((char)c)>=0)){
				if(doquote) {
					builder.Append('\"');
				}
				return true;
			}
			if(!((c&0x7F)==c && "-_.!~*'()".IndexOf((char)c)>=0) &&
					!(c>='A' && c<='Z') &&
					!(c>='a' && c<='z') &&
					!(c>='0' && c<='9'))
				return false;
			builder.Append(c);
			i++;
		}
		if(doquote) {
			builder.Append('\"');
		}
		return true;
	}

	private static bool appendUnescaped(
			string str, int index, int length, StringBuilder builder){
		int i=index;
		// type
		while(i<str.Length){
			char c=str[i];
			if(c=='%' && i+2<str.Length){
				int hex1=toHexNumber(str[i+1]);
				int hex2=toHexNumber(str[i+2]);
				c=(char)(hex1*16+hex2);
				builder.Append(c);
				if(c<=0x20 || c>=0x7F || ((c&0x7F)==c && "()<>@,;:\\\"/[]?=".IndexOf((char)c)>=0))
					return false;
				i+=3;
				continue;
			}
			if(c<=0x20 || c>=0x7F || ((c&0x7F)==c && "()<>@,;:\\\"/[]?=".IndexOf((char)c)>=0))
				return true;
			if(!((c&0x7F)==c && "-_.!~*'()".IndexOf((char)c)>=0) &&
					!(c>='A' && c<='Z') &&
					!(c>='a' && c<='z') &&
					!(c>='0' && c<='9'))
				return false;
			builder.Append(c);
			i++;
		}
		return true;
	}

	public static string unescapeContentType(string data, int index){
		int index2=skipMimeToken(data,index);
		int indexlast=-1;
		StringBuilder builder=new StringBuilder();
		if(index2<data.Length && data[index2]=='/'){
			index2++;
			indexlast=index2;
			index2=skipMimeToken(data,index2);
		} else {
			index2=index;
		}
		if(index!=index2){
			if(!appendUnescaped(data,index,indexlast-1-index,builder))
				return "";
			builder.Append('/');
			if(!appendUnescaped(data,indexlast,index2-indexlast,builder))
				return "";
		}
		index=index2;
		while(true){
			if(index>=data.Length || data[index]!=';')
				return builder.ToString();
			index++;
			index2=skipMimeToken(data,index);
			if(index==index2)
				return builder.ToString();
			int currentLength=builder.Length;
			builder.Append(';');
			if(!appendUnescaped(data,index,index2-index,builder)){
				builder.Length=(currentLength);
				return builder.ToString();
			}
			index=index2;
			if(index>=data.Length || data[index]!='='){
				builder.Length=(currentLength);
				return builder.ToString();
			}
			builder.Append('=');
			index++;
			if(com.upokecenter.util.StringUtility.startsWith(data,"%22",index)){
				index2=skipAndAppendQuoted(data,index,builder);
				if(index==index2){
					builder.Length=(currentLength);
					return builder.ToString();
				}
			} else {
				index2=skipMimeToken(data,index);
				if(index==index2){
					builder.Length=(currentLength);
					return builder.ToString();
				}
				if(!appendUnescapedValue(data,index,index2-index,builder)){
					builder.Length=(currentLength);
					return builder.ToString();
				}
				index=index2;
			}
		}
	}

	/**
	 * Extracts the charset parameter from a MIME media
	 * type.  For example, in the _string "text/plain;charset=utf-8",
	 * returns "utf-8". This method skips linear whitespace
	 * where allowed in the HTTP/1.1 specification.  For example,
	 * a _string like "text/plain;\n  charset=utf-8" is allowed.
	 * 
	 * @param str a _string containing a MIME media type.
	 * @param index the index into the _string where the
	 * media type begins.
	 * @return the charset parameter, or "ISO-8859-1" if the _string
	 * is a "text" media type without a charset parameter
	 * or if the media type is omitted and there is no charset
	 * parameter, or an empty _string otherwise.  
	 */
	public static string getCharset(string data, int index){
		if(data==null)
			return "";
		string mediaType=getMediaType(data,index);
		// NOTE: if media type is omitted,
		// text/plain is assumed by default
		index+=mediaType.Length;
		while(true){
			// Note that we skip linear whitespace here,
			// since it doesn't appear to be disallowed
			// in HTTP/1.1 (unlike whitespace between the
			// type/subtype and between attribute/value
			// of a media type)
			index=skipLinearWhitespace(data,index);
			if(index>=data.Length || data[index]!=';')
				return getDefaultCharset(mediaType);
			index++;
			index=skipLinearWhitespace(data,index);
			string attribute=getMimeToken(data,index);
			if(attribute.Length==0)
				return getDefaultCharset(mediaType);
			index+=attribute.Length;
			if(index>=data.Length || data[index]!='=')
				return getDefaultCharset(mediaType);
			bool isCharset=(attribute.Length==7 &&
					(attribute[0]=='c' || attribute[0]=='C') ||
					(attribute[1]=='h' || attribute[1]=='H') ||
					(attribute[2]=='a' || attribute[2]=='A') ||
					(attribute[3]=='r' || attribute[3]=='R') ||
					(attribute[4]=='s' || attribute[4]=='S') ||
					(attribute[5]=='e' || attribute[5]=='E') ||
					(attribute[6]=='t' || attribute[6]=='T')
					);
			index++;
			if(index>=data.Length || data[index]=='\"'){
				if(isCharset){
					string str=getQuotedString(data,index);
					return (str.Length>0) ? str :  getDefaultCharset(mediaType);
				} else {
					index=skipQuotedString(data,index);
				}
			} else {
				if(isCharset){
					string str=getMimeToken(data,index);
					return (str.Length>0) ? str :  getDefaultCharset(mediaType);
				} else {
					int index2=skipMimeToken(data,index);
					if(index==index2)
						return getDefaultCharset(mediaType);
					index=index2;
				}
			}
		}
	}
}

}
