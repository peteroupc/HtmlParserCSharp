/*
Written in 2013 by Peter Occil.  
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://upokecenter.com/d/
 */

namespace com.upokecenter.util {
using System;
using System.Text;
using System.Globalization;
using System.IO;
using System.Collections.Generic;




/**
 * An _object represented in BEncode, a serialization
 * format used in the BitTorrent protocol.
 * For more information, see:
 * https://wiki.theory.org/BitTorrentSpecification#bencoding
 *
 * This class accepts BEncoded strings in UTF-8, and outputs
 * BEncoded strings in UTF-8.
 */
public sealed class BEncodeObject {

	/**
	 *  Creates a new BEncoded _object as an empty dictionary.
	 */
	public static BEncodeObject newDictionary(){
		return new BEncodeObject(new PeterO.Support.LenientDictionary<string,BEncodeObject>());
	}

	private Object obj=null;
	public static readonly int TYPE_INTEGER=0;
	public static readonly int TYPE_STRING=1;
	public static readonly int TYPE_LIST=2;

	public static readonly int TYPE_DICTIONARY=3;

	public static BEncodeObject fromByteArray(byte[] buf){
		return fromByteArray(buf,0,buf.Length);
	}

	/**
	 * Gets a BEncoded _object from parsing a byte array
	 * of data in BEncoding.
	 * 
	 * @param buf
	 * @param off
	 * @param len
	 * 
	 * @ if an error occurs when
	 * parsing the _object.
	 */
	public static BEncodeObject fromByteArray(byte[] buf, int off, int len){
		try {
			return read(new PeterO.Support.ByteArrayInputStream(buf,off,len));
		} catch (IOException e) {
			throw new BEncodeException("Internal error",e);
		}
	}

	static long getUtf8Length(string s){
		if(s==null)throw new ArgumentNullException();
		long size=0;
		for(int i=0;i<s.Length;i++){
			int c=s[i];
			if(c<=0x7F) {
				size++;
			} else if(c<=0x7FF) {
				size+=2;
			} else if(c<=0xD7FF || c>=0xE000) {
				size+=3;
			} else if(c<=0xDBFF){ // UTF-16 low surrogate
				i++;
				if(i>=s.Length || s[i]<0xDC00 || s[i]>0xDFFF)
					return -1;
				size+=4;
			} else
				return -1;
		}
		return size;
	}

	/**
	 *  Creates a new BEncoded _object as an empty list.
	 */
	public static BEncodeObject newList(){
		return new BEncodeObject(new List<BEncodeObject>());
	}

	/**
	 * Parses a BEncoded _object from an input stream.
	 * 
	 * @param stream An input stream.  This stream
	 * must support marking.
	 * @return A BEncoded _object.
	 * @ if marking is not supported on the supplied stream.
	 * @ if an I/O error occurs.
	 */
	public static BEncodeObject read(PeterO.Support.InputStream stream) {
		if(!stream.markSupported())throw new ArgumentException();
		return new BEncodeObject(readObject(stream));
	}

	private static IDictionary<string,BEncodeObject> readDictionary(PeterO.Support.InputStream stream) {
		IDictionary<string,BEncodeObject> map=new PeterO.Support.LenientDictionary<string,BEncodeObject>();
		while(true){
			stream.mark(2);
			int c=stream.ReadByte();
			if(c=='e') {
				break;
			}
			stream.reset();
			string s=readString(stream);
			Object o=readObject(stream);
			map.Add(s,new BEncodeObject(o));
		}
		return map;
	}
	private static long readInteger(PeterO.Support.InputStream stream) {
		bool haveHex=false;
		char[] buffer=new char[21]; // enough space for the biggest long long
		int bufOffset=0;
		bool negative=false;
		stream.mark(2);
		if(stream.ReadByte()=='-'){
			buffer[bufOffset++]='-';
			negative=true;
		} else {
			stream.reset();
		}
		while(true){ // skip zeros
			stream.mark(2);
			int c=stream.ReadByte();
			if(c!='0'){
				if(c>=0) {
					stream.reset();
				}
				break;
			}
			haveHex=true;
		}
		while(true){
			stream.mark(2);
			int number=stream.ReadByte();
			if(number>='0' && number<='9'){
				if(bufOffset>=buffer.Length)
					throw new BEncodeException(negative ? "Integer too small" : "Integer too big");
				buffer[bufOffset++]=(char)number;
				haveHex=true;
			} else if(number=='e'){
				break;
			} else {
				if(number>=0) {
					stream.reset();
				}
				throw new BEncodeException("'e' expected");
			}
		}
		if(!haveHex)
			throw new BEncodeException("Positive integer expected");
		if(bufOffset==(negative ? 1 : 0))
			return 0;
		try {
			string retstr=new String(buffer,0,bufOffset);
			return Int64.Parse(retstr,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
		} catch(FormatException){
			throw new BEncodeException(negative ? "Integer too small" : "Integer too big");
		}
	}
	private static IList<BEncodeObject> readList(PeterO.Support.InputStream stream) {
		IList<BEncodeObject> list=new List<BEncodeObject>();
		while(true){
			stream.mark(2);
			int c=stream.ReadByte();
			if(c=='e') {
				break;
			}
			stream.reset();
			Object o=readObject(stream);
			list.Add(new BEncodeObject(o));
		}
		return list;
	}
	private static Object readObject(PeterO.Support.InputStream stream) {
		stream.mark(2);
		int c=stream.ReadByte();
		if(c=='d')
			return readDictionary(stream);
		else if(c=='l')
			return readList(stream);
		else if(c=='i')
			return readInteger(stream);
		else if(c>='0' && c<='9'){
			stream.reset();
			return readString(stream);
		} else {
			if(c>=0) {
				stream.reset();
			}
			throw new BEncodeException("Object expected");
		}
	}

	private static int readPositiveInteger(PeterO.Support.InputStream stream) {
		bool haveNumber=false;
		while(true){ // skip zeros
			stream.mark(2);
			int c=stream.ReadByte();
			if(c!='0'){
				if(c>=0) {
					stream.reset();
				}
				break;
			}
			haveNumber=true;
		}
		long value=0;
		while(true){
			stream.mark(2);
			int number=stream.ReadByte();
			if(number>='0' && number<='9'){
				value=(value*10)+(number-'0');
				haveNumber=true;
			} else {
				if(number>=0) {
					stream.reset();
				}
				break;
			}
			if(value>Int32.MaxValue)
				throw new BEncodeException("Integer too big");
		}
		if(!haveNumber)
			throw new BEncodeException("Positive integer expected");
		return (int)value;
	}

	private static string readString(PeterO.Support.InputStream stream)  {
		int length=readPositiveInteger(stream);
		if(stream.ReadByte()!=':')
			throw new BEncodeException("Colon expected");
		return readUtf8(stream,length);
	}
	private static string readUtf8(PeterO.Support.InputStream stream, int byteLength)  {
		StringBuilder builder=new StringBuilder();
		int cp=0;
		int bytesSeen=0;
		int bytesNeeded=0;
		int lower=0x80;
		int upper=0xBF;
		int pointer=0;
		int markedPointer=-1;
		while(pointer<byteLength || byteLength<0){
			int b=stream.ReadByte();
			if(b<0 && bytesNeeded!=0){
				bytesNeeded=0;
				throw new BEncodeException("Invalid UTF-8");
			} else if(b<0){
				if(byteLength>0 && pointer>=byteLength)
					throw new BEncodeException("Premature end of stream");
				break; // end of stream
			}
			if(byteLength>0) {
				pointer++;
			}
			if(bytesNeeded==0){
				if(b<0x80){
					builder.Append((char)b);
				}
				else if(b>=0xc2 && b<=0xdf){
					stream.mark(4);
					markedPointer=pointer;
					bytesNeeded=1;
					cp=b-0xc0;
				} else if(b>=0xe0 && b<=0xef){
					stream.mark(4);
					markedPointer=pointer;
					lower=(b==0xe0) ? 0xa0 : 0x80;
					upper=(b==0xed) ? 0x9f : 0xbf;
					bytesNeeded=2;
					cp=b-0xe0;
				} else if(b>=0xf0 && b<=0xf4){
					stream.mark(4);
					markedPointer=pointer;
					lower=(b==0xf0) ? 0x90 : 0x80;
					upper=(b==0xf4) ? 0x8f : 0xbf;
					bytesNeeded=3;
					cp=b-0xf0;
				} else
					throw new BEncodeException("Invalid UTF-8");
				cp<<=(6*bytesNeeded);
				continue;
			}
			if(b<lower || b>upper){
				cp=bytesNeeded=bytesSeen=0;
				lower=0x80;
				upper=0xbf;
				stream.reset();
				pointer=markedPointer;
				throw new BEncodeException("Invalid UTF-8");
			}
			lower=0x80;
			upper=0xbf;
			bytesSeen++;
			cp+=(b-0x80)<<(6*(bytesNeeded-bytesSeen));
			stream.mark(4);
			markedPointer=pointer;
			if(bytesSeen!=bytesNeeded) {
				continue;
			}
			int ret=cp;
			cp=0;
			bytesSeen=0;
			bytesNeeded=0;
			if(ret<=0xFFFF){
				builder.Append((char)ret);
			} else {
				int ch=ret-0x10000;
				int lead=ch/0x400+0xd800;
				int trail=(ch&0x3FF)+0xdc00;
				builder.Append((char)lead);
				builder.Append((char)trail);
			}
		}
		return builder.ToString();
	}
	/**
	 * 
	 * Gets a BEncoded _object with the value of the given integer.
	 * 
	 * @param value A 32-bit integer.
	 */
	public static BEncodeObject valueOf(int value){
		return new BEncodeObject((long)value);
	}
	/**
	 * 
	 * Gets a BEncoded _object with the value of the given long.
	 * 
	 * @param value A 64-bit integer.
	 */
	public static BEncodeObject valueOf(long value){
		return new BEncodeObject(value);
	}
	/**
	 * Gets a BEncoded _object with the value of the given _string.
	 * 
	 * @param value A _string.  Cannot be null.
	 */
	public static BEncodeObject valueOf(string value){
		return new BEncodeObject(value);
	}
	private static void writeInteger(long value, Stream stream)  {
		string value1=Convert.ToString(value,CultureInfo.InvariantCulture);
		for(int i=0;i<value1.Length;i++){
			int c=value1[i];
			stream.WriteByte(unchecked((byte)((c&0x7F))));
		}
	}
	private static void writeUtf8(string s, Stream stream) {
		for(int i=0;i<s.Length;i++){
			int c=s[i];
			if(c<=0x7F){
				stream.WriteByte(unchecked((byte)(c)));
				continue;
			} else if(c<=0x7FF){
				stream.WriteByte(unchecked((byte)((0xC0|((c>>6)))&0x1F)));
				stream.WriteByte(unchecked((byte)((0x80|(c   &0x3F)))));
				continue;
			} else if(c>=0xD800 && c<=0xDBFF){ // UTF-16 lead surrogate
				i++;
				if(i>=s.Length || s[i]<0xDC00 || s[i]>0xDFFF)
					throw new BEncodeException("invalid surrogate");
				c=0x10000+(c-0xD800)*0x400+(s[i]-0xDC00);
			} else if(c>=0xDC00 && c<=0xDFFF)
				throw new BEncodeException("invalid surrogate");
			if(c<=0xFFFF){
				stream.WriteByte(unchecked((byte)((0xE0|((c>>12)))&0x0F)));
				stream.WriteByte(unchecked((byte)((0x80|((c>>6 )))&0x3F)));
				stream.WriteByte(unchecked((byte)((0x80|(c      &0x3F)))));
			} else {
				stream.WriteByte(unchecked((byte)((0xF0|((c>>18)))&0x07)));
				stream.WriteByte(unchecked((byte)((0x80|((c>>12)))&0x3F)));
				stream.WriteByte(unchecked((byte)((0x80|((c>>6 )))&0x3F)));
				stream.WriteByte(unchecked((byte)((0x80|(c      &0x3F)))));
			}
		}
	}
	private BEncodeObject(){}
	private BEncodeObject(Object o){
		if(o==null)throw new ArgumentException();
		this.obj=o;
	}
	public void add(BEncodeObject value){
		getList().Add(value);
	}
	public void add(int value){
		getList().Add(BEncodeObject.valueOf(value));
	}
	public void add(int key,BEncodeObject value){
		getList().Insert(key,value);
	}
	public void add(int key,int value){
		add(key,BEncodeObject.valueOf(value));
	}
	public void add(int key,long value){
		add(key,BEncodeObject.valueOf(value));
	}
	public void add(int key,string value){
		add(key,BEncodeObject.valueOf(value));
	}
	public void add(long value){
		getList().Add(BEncodeObject.valueOf(value));
	}
	public void add(string value){
		getList().Add(BEncodeObject.valueOf(value));
	}
	/**
	 * Creates a shallow copy of this _object.
	 * For lists and dictionaries, the values of
	 * the new copy are the same as those of this
	 * _object; they are not copies.
	 * 
	 * @return If this is a dictionary or list,
	 * then a new BEncoded _object with the same type
	 * as this _object.  If this
	 * is a _string or integer, then returns this _object.
	 */
	
	public BEncodeObject copy(){
		BEncodeObject beo=this;
		if(beo.obj is long || beo.obj is string)
			return beo; // integer and _string objects are immutable
		if(beo.obj is IDictionary<string,BEncodeObject>){
			BEncodeObject newbeo=BEncodeObject.newDictionary();
			foreach(var key in ((IDictionary<string,BEncodeObject>)beo.obj).Keys){
				newbeo.getDictionary().Add(key,
						((IDictionary<string,BEncodeObject>)beo.obj)[key]);
			}
			return newbeo;
		}
		if(beo.obj is IList<BEncodeObject>){
			BEncodeObject newbeo=BEncodeObject.newList();
			foreach(var value in ((IList<BEncodeObject>)beo.obj)){
				newbeo.getList().Add(value);
			}
			return newbeo;
		}
		return null;
	}
	public BEncodeObject this[int i] { get { return get(i); } set { set(i,value); } }
public BEncodeObject get(int key){
		return getList()[key];
	}
	public BEncodeObject this[string i] { get { return get(i); } set { put(i,value); } }
public BEncodeObject get(string key){
		return getDictionary()[key];
	}
	
	public IDictionary<string,BEncodeObject> getDictionary(){
		return (IDictionary<string,BEncodeObject>)obj;
	}
	/**
	 * Gets the integer represented by this _object, if possible.
	 * 
	 * @return the 32-bit integer for this _object.
	 * @ if this _object isn't an Integer
	 * or its value exceeds the range of int.
	 */
	public int getInteger(){
		long ret=(long)obj;
		if(ret<Int32.MinValue || ret>Int32.MaxValue)
			throw new InvalidCastException();
		return (int)ret;
	}
	
	public IList<BEncodeObject> getList(){
		return (IList<BEncodeObject>)obj;
	}
	/**
	 * Gets the long represented by this _object, if possible.
	 * 
	 * @return the 64-bit integer for this _object.
	 * @ if this _object isn't a Long.
	 */
	public long getLong(){
		return (long)obj;
	}
	public int getObjectType(){
		if(obj is long)
			return TYPE_INTEGER;
		if(obj is string)
			return TYPE_STRING;
		if(obj is IDictionary<string,BEncodeObject>)
			return TYPE_DICTIONARY;
		if(obj is IList<BEncodeObject>)
			return TYPE_LIST;
		throw new InvalidOperationException();
	}
	public string getString(){
		return (string)obj;
	}
	public void put(string key,BEncodeObject value){
		getDictionary().Add(key,value);
	}
	public void put(string key,int value){
		put(key,BEncodeObject.valueOf(value));
	}
	public void put(string key,long value){
		put(key,BEncodeObject.valueOf(value));
	}
	public void put(string key,string value){
		put(key,BEncodeObject.valueOf(value));
	}
	public void set(int key,BEncodeObject value){
		getList()[key]=value;
	}
	public void set(int key,int value){
		set(key,BEncodeObject.valueOf(value));
	}


	public void set(int key,long value){
		set(key,BEncodeObject.valueOf(value));
	}

	public void set(int key,string value){
		set(key,BEncodeObject.valueOf(value));
	}

	public int Count { get { return size(); }}
public int size(){
		if(obj is IDictionary<string,BEncodeObject>)
			return ((IDictionary<string,BEncodeObject>)obj).Count;
		else if(obj is IList<BEncodeObject>)
			return ((IList<BEncodeObject>)obj).Count;
		return 0;
	}

	public byte[] toByteArray(){
		com.upokecenter.io.MemoryOutputStream os=new com.upokecenter.io.MemoryOutputStream();
		try {
			write(os);
		} catch (IOException e) {
			throw new BEncodeException("Internal error",e);
		}
		return os.toByteArray();
	}

	
	public void write(Stream stream) {
		if(obj is long){
			stream.WriteByte(unchecked((byte)((byte)'i')));
			writeInteger((long)obj,stream);
			stream.WriteByte(unchecked((byte)((byte)'e')));
		} else if(obj is string){
			string s=(string)obj;
			long length=getUtf8Length(s);
			if(length<0)
				throw new BEncodeException("invalid string");
			writeInteger(length,stream);
			stream.WriteByte(unchecked((byte)((byte)':')));
			writeUtf8(s,stream);
		} else if(obj is IDictionary<string,BEncodeObject>){
			stream.WriteByte(unchecked((byte)((byte)'d')));
			IDictionary<string,BEncodeObject> map=(IDictionary<string,BEncodeObject>)obj;
			foreach(var key in map.Keys){
				long length=getUtf8Length(key);
				if(length<0)
					throw new BEncodeException("invalid string");
				writeInteger(length,stream);
				stream.WriteByte(unchecked((byte)((byte)':')));
				writeUtf8(key,stream);
				map[key].write(stream);
			}
			stream.WriteByte(unchecked((byte)((byte)'e')));
		} else if(obj is List/*<BEncodeObject><Object>*/){
			stream.WriteByte(unchecked((byte)((byte)'l')));
			IList<BEncodeObject> list=(IList<BEncodeObject>)obj;
			foreach(var value in list){
				value.write(stream);
			}
			stream.WriteByte(unchecked((byte)((byte)'e')));
		} else
			throw new BEncodeException("unexpected _object type");
	}
}


}
