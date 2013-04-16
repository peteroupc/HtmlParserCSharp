/*
Written in 2013 by Peter Occil.  Released to the public domain.
Public domain dedication: http://creativecommons.org/publicdomain/zero/1.0/
 */
namespace com.upokecenter.util {
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using com.upokecenter.encoding;
using com.upokecenter.io;

public sealed class URL {

	public override sealed int GetHashCode() {
		 int prime = 31;
		int result = 1;
		result = prime * result
				+ ((fragment == null) ? 0 : fragment.GetHashCode());
		result = prime * result + ((host == null) ? 0 : host.GetHashCode());
		result = prime * result
				+ ((password == null) ? 0 : password.GetHashCode());
		result = prime * result + ((path == null) ? 0 : path.GetHashCode());
		result = prime * result + ((port == null) ? 0 : port.GetHashCode());
		result = prime * result + ((query == null) ? 0 : query.GetHashCode());
		result = prime * result + ((scheme == null) ? 0 : scheme.GetHashCode());
		result = prime * result
				+ ((schemeData == null) ? 0 : schemeData.GetHashCode());
		result = prime * result
				+ ((username == null) ? 0 : username.GetHashCode());
		return result;
	}

	public override sealed bool Equals(object obj) {
		if (this == obj)
			return true;
		if (obj == null)
			return false;
		if (GetType() != obj.GetType())
			return false;
		URL other = (URL) obj;
		if (fragment == null) {
			if (other.fragment != null)
				return false;
		} else if (!fragment.Equals(other.fragment))
			return false;
		if (host == null) {
			if (other.host != null)
				return false;
		} else if (!host.Equals(other.host))
			return false;
		if (password == null) {
			if (other.password != null)
				return false;
		} else if (!password.Equals(other.password))
			return false;
		if (path == null) {
			if (other.path != null)
				return false;
		} else if (!path.Equals(other.path))
			return false;
		if (port == null) {
			if (other.port != null)
				return false;
		} else if (!port.Equals(other.port))
			return false;
		if (query == null) {
			if (other.query != null)
				return false;
		} else if (!query.Equals(other.query))
			return false;
		if (scheme == null) {
			if (other.scheme != null)
				return false;
		} else if (!scheme.Equals(other.scheme))
			return false;
		if (schemeData == null) {
			if (other.schemeData != null)
				return false;
		} else if (!schemeData.Equals(other.schemeData))
			return false;
		if (username == null) {
			if (other.username != null)
				return false;
		} else if (!username.Equals(other.username))
			return false;
		return true;
	}

	private string scheme="";
	private string schemeData="";
	private string username="";
	private string password=null;
	private string host=null;
	private string path="";
	private string query=null;
	private string fragment=null;
	private string port="";

	public string getScheme(){
		return scheme;
	}

	public string getSchemeData(){
		return schemeData;
	}

	public string getPath(){
		return path;
	}

	public string getPort(){
		return port;
	}

	public string getProtocol(){
		return scheme + ":";
	}

	public string getUsername(){
		return username==null ? "" : username;
	}

	public string getPassword(){
		return password==null ? "" : password;
	}

	public string getQueryString(){
		return query==null ? "" : query;
	}

	public string getFragment(){
		return fragment==null ? "" : fragment;
	}

	public string getHash(){
		return (fragment==null || fragment.Length==0) ? "" : "#" + fragment;
	}

	public string getSearch(){
		return (query==null || query.Length==0) ? "" : "?" + query;
	}

	public string getHost(){
		if(port.Length==0)
			return hostSerialize(host);
		return hostSerialize(host) + ":" + port;
	}

	public string getHostname(){
		return hostSerialize(host);
	}

	public string getPathname(){
		if(schemeData.Length>0)
			return schemeData;
		else
			return path;
	}

	private sealed class QuerySerializerError : IEncodingError {
		public int emitDecoderError(int[] buffer, int offset, int length)
				 {
			return 0;
		}

		public void emitEncoderError(Stream stream, int codePoint)  {
			stream.WriteByte(unchecked((byte)(0x26)));
			stream.WriteByte(unchecked((byte)(0x23)));
			if(codePoint<0 || codePoint>=0x110000) {
				codePoint=0xFFFD;
			}
			if(codePoint==0){
				stream.WriteByte(unchecked((byte)('0')));
				stream.WriteByte(unchecked((byte)(0x3B)));
				return;
			}
			byte[] data=new byte[8];
			int count=data.Length;
			while(codePoint>0){
				count--;
				data[count]=(byte)('0'+(codePoint%10));
				codePoint/=10;
			}
			stream.Write(data,count,data.Length-count);
			stream.WriteByte(unchecked((byte)(0x3B)));
		}
	}

	private sealed class EncodingError : IEncodingError {
		public int emitDecoderError(int[] buffer, int offset, int length)
				 {
			return 0;
		}

		public void emitEncoderError(Stream stream, int codePoint)  {
			stream.WriteByte(unchecked((byte)('?')));
		}
	}

	private enum ParseState {
		SchemeStart,
		Scheme,
		SchemeData,
		NoScheme,
		RelativeOrAuthority,
		Relative,
		RelativeSlash,
		AuthorityFirstSlash,
		AuthoritySecondSlash,
		AuthorityIgnoreSlashes,
		Authority, Query, Fragment, Host, FileHost,
		RelativePathStart, RelativePath, HostName, Port
	}

	private static string hex="0123456789ABCDEF";

	private static IEncodingError encodingError=new EncodingError();

	private static IEncodingError querySerializerError=new QuerySerializerError();

	private static void percentEncode(IntList buffer, int b){
		buffer.appendInt('%');
		buffer.appendInt(hex[(b>>4)&0x0F]);
		buffer.appendInt(hex[(b)&0x0F]);
	}

	private static void percentEncodeUtf8(IntList buffer, int cp){
		if(cp<=0x7F){
			buffer.appendInt('%');
			buffer.appendInt(hex[(cp>>4)&0x0F]);
			buffer.appendInt(hex[(cp)&0x0F]);
		} else if(cp<=0x7FF){
			percentEncode(buffer,(0xC0|((cp>>6)&0x1F)));
			percentEncode(buffer,(0x80|(cp   &0x3F)));
		} else if(cp<=0xFFFF){
			percentEncode(buffer,(0xE0|((cp>>12)&0x0F)));
			percentEncode(buffer,(0x80|((cp>>6 )&0x3F)));
			percentEncode(buffer,(0x80|(cp      &0x3F)));
		} else {
			percentEncode(buffer,(0xF0|((cp>>18)&0x07)));
			percentEncode(buffer,(0x80|((cp>>12)&0x3F)));
			percentEncode(buffer,(0x80|((cp>>6 )&0x3F)));
			percentEncode(buffer,(0x80|(cp      &0x3F)));
		}
	}

	public override sealed string ToString(){
		StringBuilder builder=new StringBuilder();
		builder.Append(scheme);
		builder.Append(':');
		if(scheme.Equals("file") ||
				scheme.Equals("http") ||
				scheme.Equals("https") ||
				scheme.Equals("ftp") ||
				scheme.Equals("gopher") ||
				scheme.Equals("ws") ||
				scheme.Equals("wss")){
			// NOTE: We check relative schemes here
			// rather than have a relative flag,
			// as specified in the URL Standard
			// (since the protocol can't be changed
			// as this class is immutable, we can
			// do this variation).
			builder.Append("//");
			if(username.Length!=0 || password!=null){
				builder.Append(username);
				if(password!=null){
					builder.Append(':');
					builder.Append(password);
				}
				builder.Append('@');
			}
			builder.Append(hostSerialize(host));
			if(port.Length>0){
				builder.Append(':');
				builder.Append(port);
			}
			builder.Append(path);
		} else {
			builder.Append(schemeData);
		}
		if(query!=null){
			builder.Append('?');
			builder.Append(query);
		}
		if(fragment!=null){
			builder.Append('#');
			builder.Append(fragment);
		}
		return builder.ToString();
	}

	private static void appendOutputBytes(StringBuilder builder,
			MemoryOutputStream baos){
		for(int i=0;i<baos.Length;i++){
			int c=baos[i];
			if(c==0x20) {
				builder.Append((char)0x2b);
			} else if(c==0x2a || c==0x2d || c==0x2e ||
					(c>=0x30 && c<=0x39) ||
					(c>=0x41 && c<=0x5a) ||
					(c>=0x5f) || (c>=0x61 && c<=0x7a)){
				builder.Append((char)c);
			} else {
				builder.Append('%');
				builder.Append(hex[(c>>4)&0x0F]);
				builder.Append(hex[(c)&0x0F]);
			}
		}
	}
	private static string percentDecode(string str, string encoding)
			{
		int len=str.Length;
		bool percent=false;
		for(int i=0;i<len;i++){
			char c=str[i];
			if(c=='%') {
				percent=true;
			} else if(c>=0x80) // Non-ASCII characters not allowed
				return null;
		}
		if(!percent)return str;
		ITextDecoder decoder=TextEncoding.getDecoder(encoding);
		ByteList mos=new ByteList();
		for(int i=0;i<len;i++){
			int c=str[i];
			if(c=='%'){
				if(i+2<len){
					int a=toHexNumber(str[i+1]);
					int b=toHexNumber(str[i+2]);
					if(a>=0 && b>=0){
						mos.append((byte) (a*16+b));
						i+=2;
						continue;
					}
				}
			}
			mos.append((byte) (c&0xFF));
		}
		return TextEncoding.decodeString(mos.toInputStream(),
				decoder, TextEncoding.ENCODING_ERROR_REPLACE);
	}

	public static string toQueryString(IList<string[]> pairs,
			string delimiter, string encoding) {
		if(encoding==null) {
			encoding="utf-8";
		}
		ITextEncoder encoder=TextEncoding.getEncoder(encoding);
		if(encoder==null)
			throw new ArgumentException();
		StringBuilder builder=new StringBuilder();
		bool first=true;
		MemoryOutputStream baos=new MemoryOutputStream();
		foreach(string[] pair in pairs){
			if(!first){
				builder.Append(delimiter==null ? "&" : delimiter);
			}
			first=false;
			if(pair==null || pair.Length<2)
				throw new ArgumentException();
			baos.reset();
			TextEncoding.encodeString(pair[0], baos, encoder, querySerializerError);
			appendOutputBytes(builder,baos);
			builder.Append('=');
			baos.reset();
			TextEncoding.encodeString(pair[1], baos, encoder, querySerializerError);
			appendOutputBytes(builder,baos);
		}
		return builder.ToString();
	}

	public static IList<string[]> parseQueryString(
			string input, string delimiter, string encoding, bool useCharset, bool isindex){
		if(input==null)
			throw new ArgumentException();
		if(delimiter==null) {
			delimiter="&";
		}
		if(encoding==null) {
			encoding="utf-8";
		}
		for(int i=0;i<input.Length;i++){
			if(input[i]>0x7F)
				throw new ArgumentException();
		}
		string[] strings=StringUtility.splitAt(input,delimiter);
		IList<string[]> pairs=new List<string[]>();
		foreach(string str in strings){
			if(str.Length==0) {
				continue;
			}
			int index=str.IndexOf('=');
			string name=str;
			string value="";
			if(index>=0){
				name=str.Substring(0,(index)-(0));
				value=str.Substring(index+1);
			}
			name=name.Replace('+',' ');
			value=value.Replace('+',' ');
			if(useCharset && "_charset_".Equals(name)){
				string ch=TextEncoding.resolveEncoding(value);
				if(ch!=null){
					useCharset=false;
					encoding=ch;
				}
			}
			string[] pair=new string[]{name,value};
			pairs.Add(pair);
		}
		try {
			foreach(string[] pair in pairs){
				pair[0]=percentDecode(pair[0],encoding);
				pair[1]=percentDecode(pair[1],encoding);
			}
		} catch (IOException e) {
			throw e;
		}
		return pairs;
	}

	public static IList<string> pathList(string s){
		IList<string> str=new List<string>();
		if(s==null || s.Length==0)
			return str;
		if(s[0]!='/')
			throw new ArgumentException();
		int i=1;
		while(i<=s.Length){
			int io=s.IndexOf('/',i);
			if(io>=0){
				str.Add(s.Substring(i,(io)-(i)));
				i=io+1;
			} else {
				str.Add(s.Substring(i));
				break;
			}
		}
		return str;
	}

	public static URL parse(string s){
		return parse(s,null,null, false);
	}
	public static URL parse(string s, URL baseurl){
		return parse(s,baseurl,null, false);
	}
	public static URL parse(string s, URL baseurl, string encoding){
		return parse(s, baseurl, encoding, false);
	}

	public static URL parse(string s, URL baseurl, string encoding, bool strict){
		if(s==null)
			throw new ArgumentException();
		int beginning=0;
		int ending=s.Length-1;
		bool relative=false;
		URL url=new URL();
		ITextEncoder encoder=null;
		ParseState state=ParseState.SchemeStart;
		if(encoding!=null){
			encoder=TextEncoding.getEncoder(encoding);
		}
		if(s.IndexOf("http://",StringComparison.Ordinal)==0){
			state=ParseState.AuthorityIgnoreSlashes;
			url.scheme="http";
			beginning=7;
			relative=true;
		} else {
			while(beginning<s.Length){
				char c=s[beginning];
				if(c!=0x09 && c!=0x0a && c!=0x0c && c!=0x0d && c!=0x20){
					break;
				}
				beginning++;
			}
		}
		while(ending>=beginning){
			char c=s[ending];
			if(c!=0x09 && c!=0x0a && c!=0x0c && c!=0x0d && c!=0x20){
				ending++;
				break;
			}
			ending--;
		}
		if(ending<beginning) {
			ending=beginning;
		}
		bool atflag=false;
		bool bracketflag=false;
		IntList buffer=new IntList();
		IntList query=null;
		IntList fragment=null;
		IntList password=null;
		IntList username=null;
		IntList schemeData=null;
		bool error=false;
		IList<string> path=new List<string>();
		int index=beginning;
		int hostStart=-1;
		int portstate=0;
		while(index<=ending){
			int oldindex=index;
			int c=-1;
			if(index>=ending){
				c=-1;
				index++;
			} else {
				c=s[index];
				if(c>=0xD800 && c<=0xDBFF && index+1<ending &&
						s[index+1]>=0xDC00 && s[index+1]<=0xDFFF){
					// Get the Unicode code point for the surrogate pair
					c=0x10000+(c-0xD800)*0x400+(s[index+1]-0xDC00);
					index++;
				} else if(c>=0xD800 && c<=0xDFFF)
					// illegal surrogate
					throw new ArgumentException();
				index++;
			}
			switch(state){
			case ParseState.SchemeStart:
				if(c>='A' && c<='Z'){
					buffer.appendInt(c+0x20);
					state=ParseState.Scheme;
				} else if(c>='a' && c<='z'){
					buffer.appendInt(c);
					state=ParseState.Scheme;
				} else {
					index=oldindex;
					state=ParseState.NoScheme;
				}
				break;
			case ParseState.Scheme:
				if(c>='A' && c<='Z'){
					buffer.appendInt(c+0x20);
				} else if((c>='a' && c<='z') || c=='.' || c=='-' || c=='+'){
					buffer.appendInt(c);
				} else if(c==':'){
					url.scheme=buffer.ToString();
					buffer.clearAll();
					if(url.scheme.Equals("http") ||
							url.scheme.Equals("https") ||
							url.scheme.Equals("ftp") ||
							url.scheme.Equals("gopher") ||
							url.scheme.Equals("ws") ||
							url.scheme.Equals("wss") ||
							url.scheme.Equals("file")){
						relative=true;
					}
					if(url.scheme.Equals("file")){
						state=ParseState.Relative;
						relative=true;
					} else if(relative && baseurl!=null && url.scheme.Equals(baseurl.scheme)){
						state=ParseState.RelativeOrAuthority;
					} else if(relative){
						state=ParseState.AuthorityFirstSlash;
					} else {
						schemeData=new IntList();
						state=ParseState.SchemeData;
					}
				} else {
					buffer.clearAll();
					index=beginning;
					state=ParseState.NoScheme;
				}
				break;
			case ParseState.SchemeData:
				if(c=='?'){
					query=new IntList();
					state=ParseState.Query;
					break;
				} else if(c=='#'){
					fragment=new IntList();
					state=ParseState.Fragment;
					break;
				}
				if((c>=0 && (!isUrlCodePoint(c) && c!='%')  || (c=='%' &&
						(index+2>ending ||
								!isHexDigit(s[index]) ||
								!isHexDigit(s[index+1]))))){
					error=true;
				}
				if(c>=0 && c!=0x09 && c!=0x0a && c!=0x0d){
					if(c<0x20 || c==0x7F){
						percentEncode(schemeData,c);
					} else if(c<0x7F){
						schemeData.appendInt(c);
					} else {
						percentEncodeUtf8(schemeData,c);
					}
				}
				break;
			case ParseState.NoScheme:
				if(baseurl==null)
					return null;
				//Console.WriteLine("no scheme: [%s] [%s]",s,baseurl);
				if(!(baseurl.scheme.Equals("http") ||
						baseurl.scheme.Equals("https") ||
						baseurl.scheme.Equals("ftp") ||
						baseurl.scheme.Equals("gopher") ||
						baseurl.scheme.Equals("ws") ||
						baseurl.scheme.Equals("wss") ||
						baseurl.scheme.Equals("file")
						))
					return null;
				state=ParseState.Relative;
				index=oldindex;
				break;
			case ParseState.RelativeOrAuthority:
				if(c=='/' && index<ending && s[index]=='/'){
					index++;
					state=ParseState.AuthorityIgnoreSlashes;
				} else {
					error=true;
					state=ParseState.Relative;
					index=oldindex;
				}
				break;
			case ParseState.Relative:{
				relative=true;
				if(!"file".Equals(url.scheme)){
					url.scheme=baseurl.scheme;
				}
				if(c<0){
					url.host=baseurl.host;
					url.port=baseurl.port;
					path=pathList(baseurl.path);
					url.query=baseurl.query;
				} else if(c=='/' || c=='\\'){
					if(c=='\\') {
						error=true;
					}
					state=ParseState.RelativeSlash;
				} else if(c=='?'){
					url.host=baseurl.host;
					url.port=baseurl.port;
					path=pathList(baseurl.path);
					query=new IntList();
					state=ParseState.Query;
				} else if(c=='#'){
					url.host=baseurl.host;
					url.port=baseurl.port;
					path=pathList(baseurl.path);
					url.query=baseurl.query;
					fragment=new IntList();
					state=ParseState.Fragment;
				} else {
					url.host=baseurl.host;
					url.port=baseurl.port;
					path=pathList(baseurl.path);
					if(path.Count>0) { // Pop path
						path.RemoveAt(path.Count-1);
					}
					state=ParseState.RelativePath;
					index=oldindex;
				}
				break;
			}
			case ParseState.RelativeSlash:
				if(c=='/' || c=='\\'){
					if(c=='\\') {
						error=true;
					}
					if("file".Equals(url.scheme)){
						state=ParseState.FileHost;
					} else {
						state=ParseState.AuthorityIgnoreSlashes;
					}
				} else {
					url.host=baseurl.host;
					url.port=baseurl.port;
					state=ParseState.RelativePath;
					index=oldindex;
				}
				break;
			case ParseState.AuthorityFirstSlash:
				if(c=='/'){
					state=ParseState.AuthoritySecondSlash;
				} else {
					error=true;
					state=ParseState.AuthorityIgnoreSlashes;
					index=oldindex;
				}
				break;
			case ParseState.AuthoritySecondSlash:
				if(c=='/'){
					state=ParseState.AuthorityIgnoreSlashes;
				} else {
					error=true;
					state=ParseState.AuthorityIgnoreSlashes;
					index=oldindex;
				}
				break;
			case ParseState.AuthorityIgnoreSlashes:
				if(c!='/' && c!='\\'){
					username=new IntList();
					index=oldindex;
					hostStart=index;
					state=ParseState.Authority;
				} else {
					error=true;
				}
				break;
			case ParseState.Authority:
				if(c=='@'){
					if(atflag){
						IntList result=(password==null) ? username : password;
						error=true;
						result.appendInt('%');
						result.appendInt('4');
						result.appendInt('0');
					}
					atflag=true;
					int[] array=buffer.array();
					for(int i=0;i<buffer.Count;i++){
						int cp=array[i];
						if(cp==0x9 || cp==0xa || cp==0xd){
							error=true;
							continue;
						}
						if((!isUrlCodePoint(c) && c!='%')  || (cp=='%' &&
								(i+3>buffer.Count ||
										!isHexDigit(array[index+1]) ||
										!isHexDigit(array[index+2])))){
							error=true;
						}
						if(cp==':' && password==null){
							password=new IntList();
							continue;
						}
						IntList result=(password==null) ? username : password;
						if(cp<=0x20 || cp>=0x7F || ((cp&0x7F)==cp && "#<>?`\"".IndexOf((char)cp)>=0)){
							percentEncodeUtf8(result,cp);
						} else {
							result.appendInt(cp);
						}
					}
					
					//Console.WriteLine("username=%s",username);
					//Console.WriteLine("password=%s",password);
					buffer.clearAll();
					hostStart=index;
				} else if(c<0 || ((c&0x7F)==c && "/\\?#".IndexOf((char)c)>=0)){
					buffer.clearAll();
					state=ParseState.Host;
					index=hostStart;
				} else {
					buffer.appendInt(c);
				}
				break;
			case ParseState.FileHost:
				if(c<0 || ((c&0x7F)==c && "/\\?#".IndexOf((char)c)>=0)){
					index=oldindex;
					if(buffer.Count==2){
						int c1=buffer[0];
						int c2=buffer[1];
						if((c2=='|' || c2==':') && ((c1>='A' && c1<='Z') || (c1>='a' && c1<='z'))){
							state=ParseState.RelativePath;
							break;
						}
					}
					string host=hostParse(buffer.ToString());
					if(host==null)
						throw new ArgumentException();
					url.host=host;
					buffer.clearAll();
					state=ParseState.RelativePathStart;
				} else if(c==0x09 || c==0x0a || c==0x0d){
					error=true;
				} else {
					buffer.appendInt(c);
				}
				break;
			case ParseState.Host:
			case ParseState.HostName:
				if(c==':' && !bracketflag){
					string host=hostParse(buffer.ToString());
					if(host==null)
						return null;
					url.host=host;
					buffer.clearAll();
					state=ParseState.Port;
				} else if(c<0 || ((c&0x7F)==c && "/\\?#".IndexOf((char)c)>=0)){
					string host=hostParse(buffer.ToString());
					if(host==null)
						return null;
					url.host=host;
					buffer.clearAll();
					index=oldindex;
					state=ParseState.RelativePathStart;
				} else if(c==0x09 || c==0x0a || c==0x0d){
					error=true;
				} else {
					if(c=='[') {
						bracketflag=true;
					} else if(c==']') {
						bracketflag=false;
					}
					buffer.appendInt(c);
				}
				break;
			case ParseState.Port:
				if(c>='0' && c<='9'){
					if(c!='0') {
						portstate=2; // first non-zero found
					} else if(portstate==0){
						portstate=1; // have a port number
					}
					if(portstate==2) {
						buffer.appendInt(c);
					}
				} else if(c<0 || ((c&0x7F)==c && "/\\?#".IndexOf((char)c)>=0)){
					string bufport="";
					if(portstate==1) {
						bufport="0";
					} else if(portstate==2) {
						bufport=buffer.ToString();
					}
					//Console.WriteLine("port: [%s]",buffer.ToString());
					if((url.scheme.Equals("http") || url.scheme.Equals("ws"))
							&& bufport.Equals("80")) {
						bufport="";
					}
					if((url.scheme.Equals("https") || url.scheme.Equals("wss"))
							&& bufport.Equals("443")) {
						bufport="";
					}
					if((url.scheme.Equals("gopher"))
							&& bufport.Equals("70")) {
						bufport="";
					}
					if((url.scheme.Equals("ftp"))
							&& bufport.Equals("21")) {
						bufport="";
					}
					url.port=bufport;
					buffer.clearAll();
					state=ParseState.RelativePathStart;
					index=oldindex;
				} else if(c==0x09 || c==0x0a || c==0x0d){
					error=true;
				} else
					return null;
				break;
			case ParseState.Query:
				if(c<0 || c=='#'){
					bool utf8=true;
					if(relative){
						utf8=true;
					}
					if(utf8 || encoder==null){
						// NOTE: Encoder errors can never happen in
						// this case
						for(int i=0;i<buffer.Count;i++){
							int ch=buffer[i];
							if(ch<0x21 || ch>0x7e || ch==0x22 || ch==0x23 ||
									ch==0x3c || ch==0x3e || ch==0x60){
								percentEncodeUtf8(query,ch);
							} else {
								query.appendInt(ch);
							}
						}
					} else {
						try {
							MemoryOutputStream baos=new MemoryOutputStream();
							encoder.encode(baos,buffer.array(),0,buffer.Count,encodingError);
							byte[] bytes=baos.toByteArray();
							foreach(byte ch in bytes) {
								if(ch<0x21 || ch>0x7e || ch==0x22 || ch==0x23 ||
										ch==0x3c || ch==0x3e || ch==0x60){
									percentEncode(query,ch);
								} else {
									query.appendInt(ch);
								}
							}
							baos.Close();
						} catch (IOException e) {
							throw e;
						}
						throw new InvalidOperationException();
					}
					buffer.clearAll();
					if(c=='#'){
						fragment=new IntList();
						state=ParseState.Fragment;
					}
				} else if(c==0x09 || c==0x0a || c==0x0d){
					error=true;
				} else {
					if((!isUrlCodePoint(c) && c!='%')  || (c=='%' &&
							(index+2>ending ||
									!isHexDigit(s[index]) ||
									!isHexDigit(s[index+1])))){
						error=true;
					}
					buffer.appendInt(c);
				}
				break;
			case ParseState.RelativePathStart:
				if(c=='\\'){
					error=true;
				}
				state=ParseState.RelativePath;
				if((c!='/' && c!='\\')){
					index=oldindex;
				}
				break;
			case ParseState.RelativePath:
				if((c<0 || c=='/' || c=='\\') ||
						(c=='?' || c=='#')){
					if(c=='\\') {
						error=true;
					}
					if(buffer.Count==2 && buffer[0]=='.'
							&& buffer[1]=='.'){
						if(path.Count>0){
							path.RemoveAt(path.Count-1);
						}
						if((c!='/' && c!='\\')){
							path.Add("");
						}
					} else if(buffer.Count==1 && buffer[0]=='.'){
						if((c!='/' && c!='\\')){
							path.Add("");
						}
					} else {
						if("file".Equals(url.scheme) && path.Count==0 &&
								buffer.Count==2){
							int c1=buffer[0];
							int c2=buffer[1];
							if((c2=='|' || c2==':') && ((c1>='A' && c1<='Z') || (c1>='a' && c1<='z'))){
								buffer[1]=':';
							}
						}
						path.Add(buffer.ToString());
					}
					buffer.clearAll();
					if(c=='?'){
						query=new IntList();
						state=ParseState.Query;
					}
					if(c=='#'){
						fragment=new IntList();
						state=ParseState.Fragment;
					}
				} else if(c=='%' && index+2<=ending &&
						s[index]=='2' &&
						(s[index+1]=='e' || s[index+1]=='E')){
					index+=2;
					buffer.appendInt('.');
				} else if(c==0x09 || c==0x0a || c==0x0d){
					error=true;
				} else {
					if((!isUrlCodePoint(c) && c!='%') || (c=='%' &&
							(index+2>ending ||
									!isHexDigit(s[index]) ||
									!isHexDigit(s[index+1])))){
						error=true;
					}
					if(c<=0x20 || c>=0x7F || ((c&0x7F)==c && "#<>?`\"".IndexOf((char)c)>=0)){
						percentEncodeUtf8(buffer,c);
					} else {
						buffer.appendInt(c);
					}
				}
				break;
			case ParseState.Fragment:
				if(c<0) {
					break;
				}
				if(c==0x09 || c==0x0a || c==0x0d) {
					error=true;
				} else {
					if((!isUrlCodePoint(c) && c!='%')  || (c=='%' &&
							(index+2>ending ||
									!isHexDigit(s[index]) ||
									!isHexDigit(s[index+1])))){
						error=true;
					}
					if(c<0x20 || c==0x7F){
						percentEncode(fragment,c);
					} else if(c<0x7F){
						fragment.appendInt(c);
					} else {
						percentEncodeUtf8(fragment,c);
					}
				}
				break;
			default:
				throw new InvalidOperationException();
			}
		}
		if(error && strict)
			return null;
		if(schemeData!=null) {
			url.schemeData=schemeData.ToString();
		}
		StringBuilder builder=new StringBuilder();
		if(path.Count==0){
			builder.Append('/');
		} else {
			foreach(string segment in path){
				builder.Append('/');
				builder.Append(segment);
			}
		}
		url.path=builder.ToString();
		if(query!=null) {
			url.query=query.ToString();
		}
		if(fragment!=null) {
			url.fragment=fragment.ToString();
		}
		if(password!=null) {
			url.password=password.ToString();
		}
		if(username!=null) {
			url.username=username.ToString();
		}
		return url;
	}

	private static string hostSerialize(string _string) {
		if(_string==null)return "";
		return _string;
	}
	private static string hostParse(string _string) {
		if(_string.Length>0 && _string[0]=='['){
			if(_string[_string.Length-1]!=']'){
				int[] ipv6=new int[8];
				int piecePointer=0;
				int index=1;
				int compress=-1;
				int ending=_string.Length-1;
				int c=(index>=ending) ? -1 : _string[index];
				if(c==':'){
					if(index+1>=ending || _string[index+1]!=':')
						return null;
					index+=2;
					piecePointer++;
					compress=piecePointer;
				}
				while(index<ending){
					if(piecePointer>=8)return null;
					c=_string[index];
					if(c>=0xD800 && c<=0xDBFF && index+1<ending &&
							_string[index+1]>=0xDC00 && _string[index+1]<=0xDFFF){
						// Get the Unicode code point for the surrogate pair
						c=0x10000+(c-0xD800)*0x400+(_string[index+1]-0xDC00);
						index++;
					} else if(c>=0xD800 && c<=0xDFFF)
						// illegal surrogate
						throw new ArgumentException();
					index++;
					if(c==':'){
						if(compress>=0)return null;
						piecePointer++;
						compress=piecePointer;
						continue;
					}
					int value=0;
					int length=0;
					while(length<4){
						if(c>='A' && c<='F'){
							value=value*16+(c-'A')+10;
							index++;
							length++;
							c=(index>=ending) ? -1 : _string[index];
						} else if(c>='a' && c<='f'){
							value=value*16+(c-'a')+10;
							index++;
							length++;
							c=(index>=ending) ? -1 : _string[index];
						} else if(c>='0' && c<='9'){
							value=value*16+(c-'0');
							index++;
							length++;
							c=(index>=ending) ? -1 : _string[index];
						} else {
							break;
						}
					}
					if(c=='.'){
						if(length==0)return null;
						index-=length;
						break;
					} else if(c==':'){
						index++;
						c=(index>=ending) ? -1 : _string[index];
						if(c<0)return null;
					} else if(c>=0)
						return null;
					ipv6[piecePointer]=value;
					piecePointer++;
				}
				// IPv4
				if(c>=0){
					if(piecePointer>6)
						return null;
					int dotsSeen=0;
					while(index<ending){
						int value=0;
						while(c>='0' && c<='9'){
							value=value*10+(c-'0');
							if(value>255)return null;
							index++;
							c=(index>=ending) ? -1 : _string[index];
						}
						if(dotsSeen<3 && c!='.')
							return null;
						else if(dotsSeen==3 && c=='.')
							return null;
						ipv6[piecePointer]=ipv6[piecePointer]*256+value;
						if(dotsSeen==0 || dotsSeen==2){
							piecePointer++;
						}
						dotsSeen++;
					}
				}
				if(compress>=0){
					int swaps=piecePointer-compress;
					piecePointer=7;
					while(piecePointer!=0 && swaps!=0){
						int ptr=compress-swaps+1;
						int tmp=ipv6[piecePointer];
						ipv6[piecePointer]=ipv6[ptr];
						ipv6[ptr]=tmp;
						piecePointer--;
						swaps--;
					}
				} else if(compress<0 && piecePointer!=8)
					return null;
			}
		}
		try {
			//Console.WriteLine("was: %s",_string);
			_string=percentDecode(_string,"utf-8");
			//Console.WriteLine("now: %s",_string);
		} catch(IOException){
			return null;
		}
		return _string;
	}
	private static int toHexNumber(int c) {
		if(c>='A' && c<='Z')
			return 10+c-'A';
		else if(c>='a' && c<='z')
			return 10+c-'a';
		else if (c>='0' && c<='9')
			return c-'0';
		return -1;
	}

	private static bool isHexDigit(int c) {
		return (c>='A' && c<='Z') || (c>='a' && c<='z') || (c>='0' && c<='9');
	}

	private static bool isUrlCodePoint(int c) {
		if(c<=0x20)return false;
		if(c<0x80)
			return((c>='a' && c<='z') ||
					(c>='A' && c<='Z') ||
					(c>='0' && c<='9') ||
					((c&0x7F)==c && "!$&'()*+,-./:;=?@_~".IndexOf((char)c)>=0));
		else if((c&0xFFFE)==0xFFFE)
			return false;
		else if((c>=0xa0 && c<=0xd7ff) ||
				(c>=0xe000 && c<=0xfdcf) ||
				(c>=0xfdf0 && c<=0xffef) ||
				(c>=0x10000 && c<=0x10fffd))
			return true;
		return false;
	}

}

}
