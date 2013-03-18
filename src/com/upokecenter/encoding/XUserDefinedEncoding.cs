namespace com.upokecenter.encoding {
using System;

using System.IO;



sealed class XUserDefinedEncoding : ITextEncoder, ITextDecoder {

	public int decode(PeterO.Support.InputStream stream, int[] buffer, int offset, int length)
			 {
		return decode(stream, buffer, offset, length, TextEncoding.ENCODING_ERROR_THROW);
	}

	public int decode(PeterO.Support.InputStream stream, int[] buffer, int offset, int length, IEncodingError error)
			 {
		if(stream==null || buffer==null || offset<0 || length<0 ||
				offset+length>buffer.Length)
			throw new ArgumentException();
		byte[] tmp=new byte[1024];
		int i=length;
		int total=0;
		while(i>0){
			int count=stream.Read(tmp,0,Math.Min(i,tmp.Length));
			if(count<0) {
				break;
			}
			total+=count;
			for(int j=0;j<count;j++){
				int c=(tmp[j]&0xFF);
				if(c<0x80){
					buffer[offset++]=(c);
				} else {
					buffer[offset++]=(0xF780+(c&0xFF)-0x80);
				}
			}
			i-=count;
		}
		return (total==0) ? -1 : total;
	}

	public void encode(Stream stream, int[] array, int offset, int length, IEncodingError error)
			 {
		if(stream==null || array==null)throw new ArgumentException();
		if(offset<0 || length<0 || offset+length>array.Length)
			throw new ArgumentOutOfRangeException();
		byte[] buffer=new byte[1024];
		int i=length;
		while(i>0){
			int count=Math.Min(i,buffer.Length);
			for(int j=0;j<count;j++){
				int c=array[offset++];
				if(c<0 || c>=0x110000){
					error.emitEncoderError(stream, c);
					continue;
				} else if(c<0x80){
					buffer[j]=(byte)(c&0xFF);
				} else if(c>=0xF780 && c<=0xF7FF){
					buffer[j]=(byte)(c-0xF780+0x80);
				} else {
					error.emitEncoderError(stream, c);
					continue;
				}
			}
			i-=count;
			stream.Write(buffer,0,count);
		}
	}

	public int decode(PeterO.Support.InputStream stream)  {
		return decode(stream, TextEncoding.ENCODING_ERROR_THROW);
	}

	public int decode(PeterO.Support.InputStream stream, IEncodingError error)  {
		if(stream==null)throw new ArgumentException();
		int c=stream.ReadByte();
		if(c<0)return -1;
		if(c<0x80)
			return c;
		else
			return 0xF780+c-0x80;
	}

	public void encode(Stream stream, int[] buffer, int offset, int length)
			 {
		encode(stream, buffer, offset, length, TextEncoding.ENCODING_ERROR_THROW);
	}


}

}
