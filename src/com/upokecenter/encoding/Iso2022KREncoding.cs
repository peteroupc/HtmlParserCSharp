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
namespace com.upokecenter.encoding {
using System;

using System.IO;



sealed class Iso2022KREncoding : ITextEncoder, ITextDecoder {


	public int decode(PeterO.Support.InputStream stream)  {
		return decode(stream, TextEncoding.ENCODING_ERROR_THROW);
	}

	public int decode(PeterO.Support.InputStream stream, IEncodingError error)  {
		int[] value=new int[1];
		int c=decode(stream,value,0,1, error);
		if(c<=0)return -1;
		return value[0];
	}

	int lead=0;
	int state=0;
	bool initialization=false;

	public int decode(PeterO.Support.InputStream stream, int[] buffer, int offset, int length)
			 {
		return decode(stream, buffer, offset, length, TextEncoding.ENCODING_ERROR_THROW);
	}

	public int decode(PeterO.Support.InputStream stream, int[] buffer, int offset, int length, IEncodingError error)
			 {
		if(stream==null || buffer==null || offset<0 || length<0 ||
				offset+length>buffer.Length)
			throw new ArgumentException();
		if(length==0)return 0;
		int count=0;
		while(length>0){
			int b=stream.ReadByte();
			if(state==0){ // ASCII state
				if(b==0x0e){
					state=5; // lead state
				} else if(b==0x0f){
					continue;
				} else if(b==0x1b){
					stream.mark(4);
					state=2; // escape start state
					continue;
				} else if(b<0){
					break;
				} else if(b<=0x7F){
					buffer[offset++]=(b);
					length--;
					count++;
				} else {
					int o=error.emitDecoderError(buffer, offset, length);
					offset+=o;
					count+=o;
					length-=o;

				}
			} else if(state==2){ // escape start state
				if(b==0x24){
					state=3; // escape middle state
					continue;
				} else {
					stream.reset(); // 'decrease by one'
					state=0;// ASCII state
					int o=error.emitDecoderError(buffer, offset, length);
					offset+=o;
					count+=o;
					length-=o;

				}
			} else if(state==3){ // escape middle state
				if(b==0x29){
					state=4;
					continue;
				} else {
					stream.reset();
					state=0;// ASCII state
					int o=error.emitDecoderError(buffer, offset, length);
					offset+=o;
					count+=o;
					length-=o;

				}
			} else if(state==4){ //  state
				if(b==0x43){
					state=0;
					continue;
				} else {
					stream.reset();
					state=0;// ASCII state
					int o=error.emitDecoderError(buffer, offset, length);
					offset+=o;
					count+=o;
					length-=o;

				}
			} else if(state==5){ // lead state
				if(b==0x0A){
					state=0;// ASCII state
					buffer[offset++]=0x0a;
					length--;
					count++;
				} else if(b==0x0e){
					continue;
				} else if(b==0x0f){
					state=0;
					continue;
				} else if(b<0){
					break;
				} else {
					lead=b;
					state=6;
					continue;
				}
			} else if(state==6){ // trail state
				state=5; // lead state
				if(b<0){
					int o=error.emitDecoderError(buffer, offset, length);
					offset+=o;
					count+=o;
					length-=o;

				} else {
					int cp=-1;
					if((lead>=0x21 && lead<=0x46) &&
							(b>=0x21 && b<=0x7e)){
						cp=Korean.indexToCodePoint((26+26+126)*(lead-1)+26+26+b-1);
					} else if((lead>=0x47 && lead<=0x7E) &&
							(b>=0x21 && b<=0x7e)){
						cp=Korean.indexToCodePoint((26+26+126)*(0xc7-0x81)+(lead-0x47)*94+(b-0x21));
					}
					if(cp<=0){
						int o=error.emitDecoderError(buffer, offset, length);
						offset+=o;
						count+=o;
						length-=o;

					} else {
						buffer[offset++]=(cp);
						length--;
						count++;
					}
				}
			}
		}
		return (count==0) ? -1 : count;
	}

	public void encode(Stream stream, int[] array, int offset, int length)
			 {
		encode(stream, array, offset, length, TextEncoding.ENCODING_ERROR_THROW);
	}

	public void encode(Stream stream, int[] array, int offset, int length, IEncodingError error)
			 {
		if(stream==null || array==null)throw new ArgumentException();
		if(offset<0 || length<0 || offset+length>array.Length)
			throw new ArgumentOutOfRangeException();
		for(int i=0;i<array.Length;i++){
			int cp=array[offset+i];
			if(cp<0 || cp>=0x110000){
				error.emitEncoderError(stream, cp);
				continue;
			}
			if(!initialization){
				initialization=true;
				stream.WriteByte(unchecked((byte)(0x1b)));
				stream.WriteByte(unchecked((byte)(0x24)));
				stream.WriteByte(unchecked((byte)(0x29)));
				stream.WriteByte(unchecked((byte)(0x43)));
			}
			if(state!=0 && cp<=0x7F){
				state=0;
				stream.WriteByte(unchecked((byte)(0x0F)));
			}
			if(cp<=0x7F){
				stream.WriteByte(unchecked((byte)(cp)));
				continue;
			}
			int pointer=Korean.codePointToIndex(cp);
			if(pointer<0){
				error.emitEncoderError(stream, cp);
				continue;
			}
			if(state!=5){
				state=5;
				stream.WriteByte(unchecked((byte)(0x0e)));
			}
			if(pointer<(26+26+126)*(0xc7-0x81)){
				int lead=pointer/(26+26+126)+1;
				int trail=pointer%(26+26+126)-26-26+1;
				if(lead<0x21 || trail<0x21){
					error.emitEncoderError(stream, cp);
					continue;
				}
				stream.WriteByte(unchecked((byte)(lead)));
				stream.WriteByte(unchecked((byte)(trail)));
			} else {
				pointer-=(26+26+126)*(0xc7-0x81);
				int lead=pointer/94+0x47;
				int trail=pointer%94+0x21;
				stream.WriteByte(unchecked((byte)(lead)));
				stream.WriteByte(unchecked((byte)(trail)));
			}
		}
		if(state!=0 && length>0){
			state=0;
			stream.WriteByte(unchecked((byte)(0x0F)));
		}
	}

}

}
