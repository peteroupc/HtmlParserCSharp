/*
Written in 2013 by Peter Occil.  
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://upokecenter.com/d/
*/
namespace com.upokecenter.util {
using System;
using System.IO;


public sealed class ByteList {
	byte[] buffer;
	int ptr;
	public ByteList(){
		buffer=new byte[64];
		ptr=0;
	}

	public void append(byte v){
		if(ptr<buffer.Length){
			buffer[ptr++]=v;
		} else {
			byte[] newbuffer=new byte[buffer.Length*2];
			Array.Copy(buffer,0,newbuffer,0,buffer.Length);
			buffer=newbuffer;
			buffer[ptr++]=v;
		}
	}

	public byte[] array(){
		return buffer;
	}

	public void clear(){
		ptr=0;
	}

	public int this[int i] { get { return get(i); }
 set { set(i,(byte)(value&0xFF)); }}
public int get(int index){
		return buffer[index];
	}
	public void set(int index, byte value){
		buffer[index]=value;
	}
	public int Count { get { return size(); }}
public int size(){
		return ptr;
	}

	public byte[] toByteArray(){
		byte[] ret=new byte[ptr];
		Array.Copy(buffer,0,ret,0,ptr);
		return ret;
	}

	public PeterO.Support.InputStream toInputStream(){
		return new PeterO.Support.ByteArrayInputStream(buffer,0,ptr);
	}
}

}
