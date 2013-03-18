namespace com.upokecenter.html {
using System;

using System.IO;


using com.upokecenter.encoding;


internal class Html5Decoder : ITextDecoder {

	ITextDecoder decoder=null;
	bool havebom=false;
	bool havecr=false;
	bool iserror=false;
	public Html5Decoder(ITextDecoder decoder){
		if(decoder==null)throw new ArgumentException();
		this.decoder=decoder;
	}

	public bool isError(){
		return iserror;
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
		if(length==0)return 0;
		int count=0;
		while(length>0){
			int c=decoder.decode(stream, error);
			if(!havebom && !havecr && c>=0x20 && c<=0x7E){
				buffer[offset]=c;
				offset++;
				count++;
				length--;
				continue;
			}
			if(c<0) {
				break;
			}
			if(c==0x0D){
				// CR character
				havecr=true;
				c=0x0A;
			} else if(c==0x0A && havecr){
				havecr=false;
				continue;
			} else {
				havecr=false;
			}
			if(c==0xFEFF && !havebom){
				// leading BOM
				havebom=true;
				continue;
			} else if(c!=0xFEFF){
				havebom=false;
			}
			if(c<0x09 || (c>=0x0E && c<=0x1F) || (c>=0x7F && c<=0x9F) ||
					(c&0xFFFE)==0xFFFE || c>0x10FFFF || c==0x0B || (c>=0xFDD0 && c<=0xFDEF)){
				// control character or noncharacter
				iserror=true;
			}
			buffer[offset]=c;
			offset++;
			count++;
			length--;
		}
		return count==0 ? -1 : count;
	}

	public int decode(PeterO.Support.InputStream stream)  {
		return decode(stream, TextEncoding.ENCODING_ERROR_THROW);
	}

	public int decode(PeterO.Support.InputStream stream, IEncodingError error)  {
		int[] value=new int[1];
		int c=decode(stream,value,0,1, error);
		if(c<=0)return -1;
		return value[0];
	}
}

}
