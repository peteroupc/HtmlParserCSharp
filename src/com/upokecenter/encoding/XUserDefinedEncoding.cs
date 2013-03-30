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
		if((stream)==null)throw new ArgumentNullException("stream");
		if((error)==null)throw new ArgumentNullException("error");
		if((buffer)==null)throw new ArgumentNullException("buffer");
if((offset)<0)throw new ArgumentOutOfRangeException("offset"+" not greater or equal to "+"0"+" ("+Convert.ToString(offset,System.Globalization.CultureInfo.InvariantCulture)+")");
if((length)<0)throw new ArgumentOutOfRangeException("length"+" not greater or equal to "+"0"+" ("+Convert.ToString(length,System.Globalization.CultureInfo.InvariantCulture)+")");
if((offset+length)>buffer.Length)throw new ArgumentOutOfRangeException("offset+length"+" not less or equal to "+Convert.ToString(buffer.Length,System.Globalization.CultureInfo.InvariantCulture)+" ("+Convert.ToString(offset+length,System.Globalization.CultureInfo.InvariantCulture)+")");
		if(length==0)return 0;
		int total=0;
		for(int i=0;i<length;i++){
			int c=stream.ReadByte();
			if(c<0){
				break;
			} else if(c<0x80){
				buffer[offset++]=c;
				total++;
			} else {
				buffer[offset++]=(0xF780+(c&0xFF)-0x80);
				total++;
			}
		}
		return (total==0) ? -1 : total;
	}

	public void encode(Stream stream, int[] array, int offset, int length, IEncodingError error)
			 {
		if((stream)==null)throw new ArgumentNullException("stream");
		if((error)==null)throw new ArgumentNullException("error");
		if((array)==null)throw new ArgumentNullException("array");
if((offset)<0)throw new ArgumentOutOfRangeException("offset"+" not greater or equal to "+"0"+" ("+Convert.ToString(offset,System.Globalization.CultureInfo.InvariantCulture)+")");
if((length)<0)throw new ArgumentOutOfRangeException("length"+" not greater or equal to "+"0"+" ("+Convert.ToString(length,System.Globalization.CultureInfo.InvariantCulture)+")");
if((offset+length)>array.Length)throw new ArgumentOutOfRangeException("offset+length"+" not less or equal to "+Convert.ToString(array.Length,System.Globalization.CultureInfo.InvariantCulture)+" ("+Convert.ToString(offset+length,System.Globalization.CultureInfo.InvariantCulture)+")");
		for(int i=0;i<length;i++){
			int c=array[offset++];
			if(c<0 || c>=0x110000){
				error.emitEncoderError(stream, c);
			} else if(c<0x80){
				stream.WriteByte(unchecked((byte)((byte)c)));
			} else if(c>=0xF780 && c<=0xF7FF){
				stream.WriteByte(unchecked((byte)((byte)))(c-0xF780+0x80));
			} else {
				error.emitEncoderError(stream, c);
			}
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
