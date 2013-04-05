/*
Written in 2013 by Peter Occil.  Released to the public domain.
Public domain dedication: http://creativecommons.org/publicdomain/zero/1.0/
*/
namespace com.upokecenter.util {
using System;

public sealed class IntList {
	int[] buffer;
	int ptr;
	public IntList(){
		buffer=new int[64];
		ptr=0;
	}

	public int this[int i] { get { return get(i); }
 set { set(i,value); }}
public int get(int index){
		return buffer[index];
	}

	public void set(int index, int value){
		buffer[index]=value;
	}

	public void appendInt(int v){
		if(ptr<buffer.Length){
			buffer[ptr++]=v;
		} else {
			int[] newbuffer=new int[buffer.Length*2];
			Array.Copy(buffer,0,newbuffer,0,buffer.Length);
			buffer=newbuffer;
			buffer[ptr++]=v;
		}
	}
	public void appendString(string str) {
		for(int i=0;i<str.Length;i++){
			int c=str[i];
			if(c>=0xD800 && c<=0xDBFF && i+1<str.Length &&
					str[i+1]>=0xDC00 && str[i+1]<=0xDFFF){
				// Append a UTF-16 surrogate pair
				int cp2=0x10000+(c-0xD800)*0x400+(str[i+1]-0xDC00);
				appendInt(cp2);
				i++;
			} else if(c>=0xD800 && c<=0xDFFF)
				// illegal surrogate
				throw new ArgumentException();
			else {
				appendInt(c);
			}
		}
	}
	public int[] array(){
		return buffer;
	}
	public void clearAll(){
		ptr=0;
	}
	public int Count { get { return size(); }}
public int size(){
		return ptr;
	}
	public override sealed string ToString(){
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		for(int i=0;i<ptr;i++){
			if(buffer[i]<=0xFFFF){
				builder.Append((char)buffer[i]);
			} else {
				int ch=buffer[i]-0x10000;
				int lead=ch/0x400+0xd800;
				int trail=(ch&0x3FF)+0xdc00;
				builder.Append((char)lead);
				builder.Append((char)trail);
			}
		}
		return builder.ToString();
	}
}
}
