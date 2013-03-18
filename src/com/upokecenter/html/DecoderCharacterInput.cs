namespace com.upokecenter.html {
using System;

using System.IO;


using com.upokecenter.encoding;


using com.upokecenter.util;

sealed class DecoderCharacterInput : ICharacterInput {
	private PeterO.Support.InputStream input;
	private ITextDecoder decoder;
	private IEncodingError error=TextEncoding.ENCODING_ERROR_REPLACE;

	public DecoderCharacterInput(PeterO.Support.InputStream input, ITextDecoder decoder) {
		this.input=input;
		this.decoder=decoder;
	}

	public DecoderCharacterInput(PeterO.Support.InputStream input, ITextDecoder decoder, IEncodingError error) {
		this.input=input;
		this.decoder=decoder;
		this.error=error;
	}

	public int read(int[] buf, int offset, int unitCount)  {
		return decoder.decode(input,buf,offset,unitCount,error);
	}

	public int read()  {
		return decoder.decode(input,error);
	}

}

}
