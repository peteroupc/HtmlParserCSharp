namespace com.upokecenter.encoding {
using System;

using System.IO;

internal class ReplacementDecoder : ITextDecoder {

	bool endofstream=false;

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
		if(!endofstream){
			endofstream=true;
			int o=error.emitDecoderError(buffer, offset, length);
			return o;
		}
		return -1;
	}

	public int decode(PeterO.Support.InputStream stream)  {
		return decode(stream, TextEncoding.ENCODING_ERROR_THROW);
	}

	public int decode(PeterO.Support.InputStream stream, IEncodingError error)  {
		if(!endofstream){
			endofstream=true;
			if(error.Equals(TextEncoding.ENCODING_ERROR_REPLACE))
				return 0xFFFD;
			else {
				int[] data=new int[1];
				int o=error.emitDecoderError(data,0,1);
				return (o==0) ? -1 : data[0];
			}
		}
		return -1;
	}
}

}
