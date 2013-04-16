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



sealed class Utf8Encoding : ITextEncoder, ITextDecoder {

	int cp=0;
	int bytesSeen=0;
	int bytesNeeded=0;
	int lower=0x80;
	int upper=0xBF;


	public int decode(PeterO.Support.InputStream stream)  {
		return decode(stream, TextEncoding.ENCODING_ERROR_THROW);
	}

	public int decode(PeterO.Support.InputStream stream, IEncodingError error)  {
		if(stream==null)
			throw new ArgumentException();
		while(true){
			int b=stream.ReadByte();
			if(b<0 && bytesNeeded!=0){
				bytesNeeded=0;
				if(error.Equals(TextEncoding.ENCODING_ERROR_REPLACE))
					return 0xFFFD;
				else {
					int[] data=new int[1];
					int o=error.emitDecoderError(data,0,1);
					if(o>0)return data[0];
					continue;
				}
			} else if(b<0)
				return -1;
			if(bytesNeeded==0){
				if(b<0x80)
					return b;
				else if(b>=0xc2 && b<=0xdf){
					stream.mark(4);
					bytesNeeded=1;
					cp=b-0xc0;
				} else if(b>=0xe0 && b<=0xef){
					stream.mark(4);
					lower=(b==0xe0) ? 0xa0 : 0x80;
					upper=(b==0xed) ? 0x9f : 0xbf;
					bytesNeeded=2;
					cp=b-0xe0;
				} else if(b>=0xf0 && b<=0xf4){
					stream.mark(4);
					lower=(b==0xf0) ? 0x90 : 0x80;
					upper=(b==0xf4) ? 0x8f : 0xbf;
					bytesNeeded=3;
					cp=b-0xf0;
				} else {
					if(error.Equals(TextEncoding.ENCODING_ERROR_REPLACE))
						return 0xFFFD;
					else {
						int[] data=new int[1];
						int o=error.emitDecoderError(data,0,1);
						if(o>0)return data[0];
						continue;
					}
				}
				cp<<=(6*bytesNeeded);
				continue;
			}
			if(b<lower || b>upper){
				cp=bytesNeeded=bytesSeen=0;
				lower=0x80;
				upper=0xbf;
				stream.reset(); // 'Decrease the byte pointer by one.'
				if(error.Equals(TextEncoding.ENCODING_ERROR_REPLACE))
					return 0xFFFD;
				else {
					int[] data=new int[1];
					int o=error.emitDecoderError(data,0,1);
					if(o>0)return data[0];
					continue;
				}
			}
			lower=0x80;
			upper=0xbf;
			bytesSeen++;
			cp+=(b-0x80)<<(6*(bytesNeeded-bytesSeen));
			stream.mark(4);
			if(bytesSeen!=bytesNeeded) {
				continue;
			}
			int ret=cp;
			cp=0;
			bytesSeen=0;
			bytesNeeded=0;
			return ret;
		}
	}

	public int decode(PeterO.Support.InputStream stream, int[] buffer, int offset, int length)
			 {
		return decode(stream, buffer, offset, length, TextEncoding.ENCODING_ERROR_THROW);
	}

	public int decode(PeterO.Support.InputStream stream, int[] buffer, int offset, int length, IEncodingError error)
			 {
		if(stream==null || buffer==null || offset<0 || length<0 ||
				offset+length>buffer.Length)
			throw new ArgumentException();
		int count=0;
		while(length>0){
			int b=stream.ReadByte();
			if(b<0 && bytesNeeded!=0){
				bytesNeeded=0;
				int o=error.emitDecoderError(buffer,offset,length);
				offset+=o;
				count+=o;
				length-=o;
				break;
			} else if(b<0){
				break;
			}
			if(bytesNeeded==0){
				if(b<0x80){
					buffer[offset++]=(b);
					count++;
					length--;
					continue;
				} else if(b>=0xc2 && b<=0xdf){
					stream.mark(4);
					bytesNeeded=1;
					cp=b-0xc0;
				} else if(b>=0xe0 && b<=0xef){
					stream.mark(4);
					lower=(b==0xe0) ? 0xa0 : 0x80;
					upper=(b==0xed) ? 0x9f : 0xbf;
					bytesNeeded=2;
					cp=b-0xe0;
				} else if(b>=0xf0 && b<=0xf4){
					stream.mark(4);
					lower=(b==0xf0) ? 0x90 : 0x80;
					upper=(b==0xf4) ? 0x8f : 0xbf;
					bytesNeeded=3;
					cp=b-0xf0;
				} else {
					int o=error.emitDecoderError(buffer,offset,length);
					offset+=o;
					count+=o;
					length-=o;
					continue;
				}
				cp<<=(6*bytesNeeded);
				continue;
			}
			if(b<lower || b>upper){
				cp=bytesNeeded=bytesSeen=0;
				lower=0x80;
				upper=0xbf;
				stream.reset(); // 'Decrease the byte pointer by one.'
				int o=error.emitDecoderError(buffer,offset,length);
				offset+=o;
				count+=o;
				length-=o;
			}
			lower=0x80;
			upper=0xbf;
			bytesSeen++;
			cp+=(b-0x80)<<(6*(bytesNeeded-bytesSeen));
			stream.mark(4);
			if(bytesSeen!=bytesNeeded) {
				continue;
			}
			buffer[offset++]=(cp);
			count++;
			length--;
			cp=0;
			bytesSeen=0;
			bytesNeeded=0;
		}
		return (count<=0) ? -1 : count;
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
			if(cp<0 || cp>=0x10000 || (cp>=0xd800 && cp<=0xdfff)){
				error.emitEncoderError(stream, cp);
				continue;
			}
			if(cp<=0x7F){
				stream.WriteByte(unchecked((byte)(cp)));
			} else if(cp<=0x7FF){
				stream.WriteByte(unchecked((byte)((0xC0|((cp>>6)))&0x1F)));
				stream.WriteByte(unchecked((byte)((0x80|(cp   &0x3F)))));
			} else if(cp<=0xFFFF){
				stream.WriteByte(unchecked((byte)((0xE0|((cp>>12)))&0x0F)));
				stream.WriteByte(unchecked((byte)((0x80|((cp>>6 )))&0x3F)));
				stream.WriteByte(unchecked((byte)((0x80|(cp      &0x3F)))));
			} else {
				stream.WriteByte(unchecked((byte)((0xF0|((cp>>18)))&0x07)));
				stream.WriteByte(unchecked((byte)((0x80|((cp>>12)))&0x3F)));
				stream.WriteByte(unchecked((byte)((0x80|((cp>>6 )))&0x3F)));
				stream.WriteByte(unchecked((byte)((0x80|(cp      &0x3F)))));
			}
		}
	}

}

}
