namespace com.upokecenter.encoding {
using System;

using System.IO;



sealed class SingleByteEncoding : ITextEncoder, ITextDecoder {

	int[] indexes;
	int maxValue;
	int minValue;
	public SingleByteEncoding(int[] indexes){
		if(indexes==null || indexes.Length<0x80)
			throw new ArgumentException();
		for(int i=0;i<indexes.Length;i++){
			maxValue=(i==0) ? indexes[i] : Math.Max(maxValue,indexes[i]);
			minValue=(i==0) ? indexes[i] : Math.Min(minValue,indexes[i]);
		}
		this.indexes=indexes;
	}


	public int decode(PeterO.Support.InputStream stream)  {
		return decode(stream, TextEncoding.ENCODING_ERROR_THROW);
	}


	public int decode(PeterO.Support.InputStream stream, IEncodingError error)  {
		if(stream==null)throw new ArgumentException();
		while(true){
			int c=stream.ReadByte();
			if(c<0)return -1;
			if(c<0x80)
				return c;
			else {
				int cp=indexes[(c)&0x7F];
				if(cp!=0)return cp;
				if(error.Equals(TextEncoding.ENCODING_ERROR_REPLACE))
					return 0xFFFD;
				else {
					int[] data=new int[1];
					int o=error.emitDecoderError(data,0,1);
					if(o>0)return data[0];
				}
			}
		}
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
				int cp=indexes[(c)&0x7F];
				if(cp==0){
					if(error.Equals(TextEncoding.ENCODING_ERROR_REPLACE))
						cp=0xFFFD;
					else {
						int[] data=new int[1];
						int o=error.emitDecoderError(data,0,1);
						if(o>0)return data[0];
					}
				}
				buffer[offset++]=cp;
				total++;
			}
		}
		return (total==0) ? -1 : total;
	}
	public void encode(Stream stream, int[] array, int offset, int length)
			 {
		encode(stream, array, offset, length, TextEncoding.ENCODING_ERROR_THROW);
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
			} else {
				if(c<minValue){
					error.emitEncoderError(stream, c);
					continue;
				}
				int pointer=-1;
				for(int k=0;k<0x80;k++){
					if(indexes[k]==c){
						pointer=k+0x80;
					}
				}
				if(pointer>=0){
					stream.WriteByte(unchecked((byte)((byte)pointer)));
				} else {
					error.emitEncoderError(stream, c);
				}
			}
		}
	}
}

}
