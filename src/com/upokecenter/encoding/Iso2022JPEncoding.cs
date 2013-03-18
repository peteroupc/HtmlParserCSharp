namespace com.upokecenter.encoding {
using System;

using System.IO;



sealed class Iso2022JPEncoding : ITextEncoder, ITextDecoder {

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
	bool jis0212=false;
	int state=0;
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
			if(state==0){ // ASCII state
				if(b==0x1b){
					stream.mark(4);
					state=2; // escape start state
					continue;
				} else if(b<0){
					break;
				} else if(b<=0x7F){
					buffer[offset++]=(b);
					length--;
					count++;
					continue;
				} else {
					int o=error.emitDecoderError(buffer, offset, length);
					offset+=o;
					count+=o;
					length-=o;
					continue;
				}
			} else if(state==2){ // escape start state
				if(b==0x24 || b==0x28){
					lead=b;
					state=3; // escape middle state
					continue;
				} else {
					stream.reset(); // 'decrease by one'
					state=0;// ASCII state
					int o=error.emitDecoderError(buffer, offset, length);
					offset+=o;
					count+=o;
					length-=o;
					continue;
				}
			} else if(state==3){ // escape middle state
				if(lead==0x24 && (b==0x40 || b==0x42)){
					jis0212=false;
					state=5; // lead state
					continue;
				} else if(lead==0x24 && b==0x28){
					state=4; // escape  state
					continue;
				} else if(lead==0x28 && (b==0x42 || b==0x4a)){
					state=0; // ASCII state
					continue;
				} else if(lead==0x28 && (b==0x49)){
					state=7; // Katakana state
					continue;
				} else {
					stream.reset();
					state=0;// ASCII state
					int o=error.emitDecoderError(buffer, offset, length);
					offset+=o;
					count+=o;
					length-=o;
					continue;
				}
			} else if(state==4){ //  state
				if(b==0x44){
					jis0212=true;
					state=5;
					continue;
				} else {
					stream.reset();
					state=0;// ASCII state
					int o=error.emitDecoderError(buffer, offset, length);
					offset+=o;
					count+=o;
					length-=o;
					continue;
				}
			} else if(state==5){ // lead state
				if(b==0x0A){
					state=0;// ASCII state
					buffer[offset++]=0x0a;
					length--;
					count++;
					continue;
				} else if(b==0x1B){
					state=1; // escape start state
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
					continue;
				} else {
					int cp=-1;
					int index=(lead-0x21)*94+b-0x21;
					if((lead>=0x21 && lead<=0x7e) &&
							(b>=0x21 && b<=0x7e)){
						if(jis0212){
							cp=JIS0212.indexToCodePoint(index);
						} else {
							cp=JIS0208.indexToCodePoint(index);
						}
					}
					if(cp<=0){
						int o=error.emitDecoderError(buffer, offset, length);
						offset+=o;
						count+=o;
						length-=o;
						continue;
					} else {
						buffer[offset++]=(cp);
						length--;
						count++;
						continue;
					}
				}
			} else { // Katakana state
				if(b==0x1b){
					state=1; // escape start state
					continue;
				} else if((b>=0x21 && b<=0x5F)){
					buffer[offset++]=(0xFF61+b-0x21);
					length--;
					count++;
					continue;
				} else if(b<0){
					break;
				} else {
					int o=error.emitDecoderError(buffer, offset, length);
					offset+=o;
					count+=o;
					length-=o;
					continue;
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
			if((cp<=0x7F || cp==0xa5 || cp==0x203e) && state!=0){
				// ASCII state
				state=0;
				stream.WriteByte(unchecked((byte)(0x1b)));
				stream.WriteByte(unchecked((byte)(0x28)));
				stream.WriteByte(unchecked((byte)(0x42)));
			}
			if(cp<=0x7F){
				stream.WriteByte(unchecked((byte)(cp)));
				continue;
			} else if(cp==0xa5){
				stream.WriteByte(unchecked((byte)(0x5c)));
				continue;
			} else if(cp==0x203e){
				stream.WriteByte(unchecked((byte)(0x7e)));
				continue;
			}
			if(cp>=0xFF61 && cp<=0xFF9F && state!=7){
				// Katakana state
				state=7;
				stream.WriteByte(unchecked((byte)(0x1b)));
				stream.WriteByte(unchecked((byte)(0x28)));
				stream.WriteByte(unchecked((byte)(0x49)));
			}
			if(cp>=0xFF61 && cp<=0xFF9F){
				stream.WriteByte(unchecked((byte)(cp-0xFF61+0x21)));
				continue;
			}
			int pointer=JIS0208.codePointToIndex(cp);
			if(pointer<0){
				error.emitEncoderError(stream, cp);
				continue;
			}
			if(state!=5){ // lead state
				state=5;
				stream.WriteByte(unchecked((byte)(0x1b)));
				stream.WriteByte(unchecked((byte)(0x24)));
				stream.WriteByte(unchecked((byte)(0x42)));
			}
			int lead=pointer/94+0x21;
			int trail=pointer%94+0x21;
			stream.WriteByte(unchecked((byte)(lead)));
			stream.WriteByte(unchecked((byte)(trail)));
		}
	}
}

}
