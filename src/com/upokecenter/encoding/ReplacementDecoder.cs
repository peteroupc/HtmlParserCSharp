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
using System.IO;
internal class ReplacementDecoder : ITextDecoder {

  bool endofstream=false;

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
}

}
