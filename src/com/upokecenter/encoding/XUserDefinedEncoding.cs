/*
If you like this, you should donate to Peter O.
at: http://upokecenter.com/d/



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
using System.Globalization;
using System.IO;



sealed class XUserDefinedEncoding : ITextEncoder, ITextDecoder {

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

	public int decode(PeterO.Support.InputStream stream, int[] buffer, int offset, int length)
			 {
		return decode(stream, buffer, offset, length, TextEncoding.ENCODING_ERROR_THROW);
	}

	public int decode(PeterO.Support.InputStream stream, int[] buffer, int offset, int length, IEncodingError error)
			 {
		if((stream)==null)throw new ArgumentNullException("stream");
		if((error)==null)throw new ArgumentNullException("error");
		if((buffer)==null)throw new ArgumentNullException("buffer");
		if((offset)<0)throw new ArgumentOutOfRangeException("offset"+" not greater or equal to "+"0"+" ("+Convert.ToString(offset,CultureInfo.InvariantCulture)+")");
		if((length)<0)throw new ArgumentOutOfRangeException("length"+" not greater or equal to "+"0"+" ("+Convert.ToString(length,CultureInfo.InvariantCulture)+")");
		if((offset+length)>buffer.Length)throw new ArgumentOutOfRangeException("offset+length"+" not less or equal to "+Convert.ToString(buffer.Length,CultureInfo.InvariantCulture)+" ("+Convert.ToString(offset+length,CultureInfo.InvariantCulture)+")");
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

	public void encode(Stream stream, int[] buffer, int offset, int length)
			 {
		encode(stream, buffer, offset, length, TextEncoding.ENCODING_ERROR_THROW);
	}

	public void encode(Stream stream, int[] array, int offset, int length, IEncodingError error)
			 {
		if((stream)==null)throw new ArgumentNullException("stream");
		if((error)==null)throw new ArgumentNullException("error");
		if((array)==null)throw new ArgumentNullException("array");
		if((offset)<0)throw new ArgumentOutOfRangeException("offset"+" not greater or equal to "+"0"+" ("+Convert.ToString(offset,CultureInfo.InvariantCulture)+")");
		if((length)<0)throw new ArgumentOutOfRangeException("length"+" not greater or equal to "+"0"+" ("+Convert.ToString(length,CultureInfo.InvariantCulture)+")");
		if((offset+length)>array.Length)throw new ArgumentOutOfRangeException("offset+length"+" not less or equal to "+Convert.ToString(array.Length,CultureInfo.InvariantCulture)+" ("+Convert.ToString(offset+length,CultureInfo.InvariantCulture)+")");
		for(int i=0;i<length;i++){
			int c=array[offset++];
			if(c<0 || c>=0x110000){
				error.emitEncoderError(stream, c);
			} else if(c<0x80){
				stream.WriteByte(unchecked((byte)((byte)c)));
			} else if(c>=0xF780 && c<=0xF7FF){
				c=(c-0xf780+0x80)&0xFF;
				stream.WriteByte(unchecked((byte)((byte)c)));
			} else {
				error.emitEncoderError(stream, c);
			}
		}
	}


}

}
