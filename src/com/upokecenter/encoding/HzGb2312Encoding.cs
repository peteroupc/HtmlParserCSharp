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



sealed class HzGb2312Encoding : ITextEncoder, ITextDecoder {


  bool flag=false;
  int lead=0;


  public int decode(PeterO.Support.InputStream stream)  {
    return decode(stream, TextEncoding.ENCODING_ERROR_THROW);
  }
  public int decode(PeterO.Support.InputStream stream, IEncodingError error)  {
    int[] value=new int[1];
    int c=decode(stream,value,0,1, error);
    if(c<=0)return -1;
    return value[0];
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
      int b=stream.ReadByte();
      if(b<0 && lead==0){
        break;
      } else if(b<0){
        lead=0;
        int o=error.emitDecoderError(buffer, offset, length);
        offset+=o;
        count+=o;
        length-=o;

        break;
      }
      if(lead==0x7e){
        lead=0;
        if(b==0x7b){
          flag=true;
          continue;
        } else if(b==0x7d){
          flag=false;
          continue;
        } else if(b==0x7e){
          buffer[offset++]=0x7e;
          length--;
          count++;
          continue;
        } else if(b==0x0a){
          continue;
        } else {
          stream.reset();
          int o=error.emitDecoderError(buffer, offset, length);
          offset+=o;
          count+=o;
          length-=o;

          continue;
        }
      }
      if(lead!=0){
        int thislead=lead;
        int cp=-1;
        lead=0;
        if(b>=0x21 && b<=0x7e){
          cp=(thislead-1)*190+(b+0x3f);
          cp=GBK.indexToCodePoint(cp);
        }
        if(b==0x0a) {
          flag=false;
        }
        if(cp<0){
          int o=error.emitDecoderError(buffer, offset, length);
          offset+=o;
          count+=o;
          length-=o;

          continue;
        } else {
          buffer[offset++]=cp;
          length--;
          count++;
          continue;
        }
      }
      if(b==0x7e){
        lead=0x7e;
        stream.mark(2);
        continue;
      }
      if(flag){
        if(b>=0x20 && b<=0x7f){
          lead=b;
          continue;
        }
        if(b==0x0a){
          flag=false;
        }
        int o=error.emitDecoderError(buffer, offset, length);
        offset+=o;
        count+=o;
        length-=o;

        continue;
      }
      if(b<=0x7f){
        buffer[offset++]=b;
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
      if(cp<=0x7F && flag){
        flag=false;
        stream.WriteByte(unchecked((byte)(0x7E)));
        stream.WriteByte(unchecked((byte)(0x7D)));
      }
      if(cp==0x7E){
        stream.WriteByte(unchecked((byte)(0x7E)));
        stream.WriteByte(unchecked((byte)(0x7E)));
      }
      if(cp<=0x7F){
        stream.WriteByte(unchecked((byte)(cp)));
        break;
      }
      int pointer=GBK.codePointToIndex(cp);
      if(pointer<0){
        error.emitEncoderError(stream, cp);
        continue;
      }
      if(!flag){
        flag=true;
        stream.WriteByte(unchecked((byte)(0x7E)));
        stream.WriteByte(unchecked((byte)(0x7B)));
      }
      int lead=pointer/190+1;
      int trail=pointer%190-0x3f;
      if(lead<0x21 || trail<0x21){
        error.emitEncoderError(stream, cp);
        continue;
      }
      stream.WriteByte(unchecked((byte)(lead)));
      stream.WriteByte(unchecked((byte)(trail)));
    }
  }
}


}
