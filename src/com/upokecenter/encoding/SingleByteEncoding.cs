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
		if(stream==null || buffer==null || offset<0 || length<0 ||
				offset+length>buffer.Length)
			throw new ArgumentException();
		byte[] tmp=new byte[1024];
		int i=length;
		int total=0;
		if(TextEncoding.ENCODING_ERROR_REPLACE.Equals(error)){
			while(i>0){
				int count=stream.Read(tmp,0,Math.Min(i,buffer.Length));
				if(count<0) {
					break;
				}
				total+=count;
				for(int j=0;j<count;j++){
					int c=(tmp[j]&0xFF);
					if(c<0x80){
						buffer[offset++]=(c);
					} else {
						int cp=indexes[(c)&0x7F];
						buffer[offset++]=((cp==0) ? 0xFFFD : cp);
					}
				}
				i-=count;
			}
		} else {
			int[] data=new int[1];
			while(length>0){
				int c=stream.ReadByte();
				if(c<0) {
					break;
				}
				if(c<0x80){
					buffer[offset++]=c;
					total++;
					length--;
				} else {
					int cp=indexes[(c)&0x7F];
					if(cp==0){
						int o=error.emitDecoderError(data,offset,length);
						offset+=o;
						length-=o;
						total+=o;
					} else {
						buffer[offset++]=cp;
						length--;
						total++;
					}
				}
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
		if(stream==null || array==null)throw new ArgumentException();
		if(offset<0 || length<0 || offset+length>array.Length)
			throw new ArgumentOutOfRangeException();
		byte[] buffer=null;
		int bufferLength=1024;
		int i=length;
		while(i>0){
			int count=Math.Min(i,bufferLength);
			for(int j=0;j<count;j++){
				int c=array[offset];
				if(c<0 || c>maxValue){
					error.emitEncoderError(stream, c);
					continue;
				}
				else if(c<0x80){
					if(buffer==null) {
						buffer=new byte[1024];
					}
					buffer[j]=(byte)(c&0xFF);
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
						if(buffer==null) {
							buffer=new byte[1024];
						}
						buffer[j]=(byte)(pointer&0xFF);
					} else {
						error.emitEncoderError(stream, c);
						continue;
					}
				}
				offset++;
			}
			i-=count;
			stream.Write(buffer,0,count);
		}
	}
}

}
