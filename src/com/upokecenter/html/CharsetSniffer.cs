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

namespace com.upokecenter.html {
using System;
using System.Globalization;
using System.Text;
using System.IO;
using System.Collections.Generic;
using com.upokecenter.encoding;
using com.upokecenter.net;
using com.upokecenter.util;

sealed class CharsetSniffer {

  private static readonly int NoFeed=0;

  private static readonly int RSSFeed=1; // application/rss+xml
  private static readonly int AtomFeed=2; // application/atom+xml
  private static readonly byte[] rdfNamespace=new byte[]{
    0x68,0x74,0x74,0x70,0x3A,0x2F,0x2F,0x77,0x77,0x77,0x2E,
    0x77,0x33,0x2E,0x6F,0x72,0x67,0x2F,0x31,0x39,0x39,0x39,
    0x2F,0x30,0x32,0x2F,0x32,0x32,0x2D,0x72,0x64,0x66,0x2D,
    0x73,0x79,0x6E,0x74,0x61,0x78,0x2D,0x6E,0x73,0x23
  };

  private static readonly byte[] rssNamespace=new byte[]{
    0x68,0x74,0x74,0x70,0x3A,0x2F,0x2F,0x70,0x75,0x72,0x6C,
    0x2E,0x6F,0x72,0x67,0x2F,0x72,0x73,0x73,0x2F,0x31,0x2E,
    0x30,0x2F
  };

  static byte[][] patternsHtml=new byte[][]{
    new byte[]{0x3C,0x21,0x44,0x4F,0x43,0x54,0x59,0x50,0x45,0x20,0x48,0x54,0x4D,0x4C},
    new byte[]{(byte)255,(byte)255,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)255,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf},
    new byte[]{0x3C,0x48,0x54,0x4D,0x4C},new byte[]{(byte)255,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf},
    new byte[]{0x3C,0x48,0x45,0x41,0x44},new byte[]{(byte)255,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf},
    new byte[]{0x3C,0x53,0x43,0x52,0x49,0x50,0x54},new byte[]{(byte)255,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf},
    new byte[]{0x3C,0x49,0x46,0x52,0x41,0x4D,0x45},new byte[]{(byte)255,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf},
    new byte[]{0x3C,0x48,0x31},new byte[]{(byte)255,(byte)0xdf,(byte)255},
    new byte[]{0x3C,0x44,0x49,0x56},new byte[]{(byte)255,(byte)0xdf,(byte)0xdf,(byte)0xdf},
    new byte[]{0x3C,0x46,0x4F,0x4E,0x54},new byte[]{(byte)255,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf},
    new byte[]{0x3C,0x54,0x41,0x42,0x4C,0x45},new byte[]{(byte)255,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf},
    new byte[]{0x3C,0x41},new byte[]{(byte)255,(byte)0xdf},
    new byte[]{0x3C,0x53,0x54,0x59,0x4C,0x45},new byte[]{(byte)255,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf},
    new byte[]{0x3C,0x54,0x49,0x54,0x4C,0x45},new byte[]{(byte)255,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf},
    new byte[]{0x3C,0x42},new byte[]{(byte)255,(byte)0xdf},
    new byte[]{0x3C,0x42,0x4F,0x44,0x59},new byte[]{(byte)255,(byte)0xdf,(byte)0xdf,(byte)0xdf,(byte)0xdf},
    new byte[]{0x3C,0x42,0x52},new byte[]{(byte)255,(byte)0xdf,(byte)0xdf},
    new byte[]{0x3C,0x50},new byte[]{(byte)255,(byte)0xdf},
    new byte[]{0x3C,0x21,0x2D,0x2D},new byte[]{(byte)255,(byte)255,(byte)255,(byte)255},
  };
  static byte[][] patternsXml=new byte[][]{
    new byte[]{0x3C,0x3F,0x78,0x6D,0x6C},new byte[]{(byte)255,(byte)255,(byte)255,(byte)255,(byte)255},
  };

  static byte[][] patternsPdf=new byte[][]{
    new byte[]{0x25,0x50,0x44,0x46,0x2D},new byte[]{(byte)255,(byte)255,(byte)255,(byte)255,(byte)255}
  };

  static byte[][] patternsPs=new byte[][]{
    new byte[]{0x25,0x21,0x50,0x53,0x2d,0x41,
        0x64,0x6f,0x62,0x65,0x2d},null
  };
  internal static string extractCharsetFromMeta(string value){
    if(value==null)return value;
    // We assume value is lower-case here
    int index=0;
    int length=value.Length;
    char c=(char)0;
    while(true){
      index=value.IndexOf("charset",0,StringComparison.Ordinal);
      if(index<0)return null;
      index+=7;
      // skip whitespace
      while(index<length){
        c=value[index];
        if(c!=0x09 && c!=0x0c && c!=0x0d && c!=0x0a && c!=0x20) {
          break;
        }
        index++;
      }
      if(index>=length)return null;
      if(value[index]=='='){
        index++;
        break;
      }
    }
    // skip whitespace
    while(index<length){
      c=value[index];
      if(c!=0x09 && c!=0x0c && c!=0x0d && c!=0x0a && c!=0x20) {
        break;
      }
      index++;
    }
    if(index>=length)return null;
    c=value[index];
    if(c=='"' || c=='\''){
      index++;
      int nextIndex=index;
      while(nextIndex<length){
        char c2=value[nextIndex];
        if(c==c2)
          return TextEncoding.resolveEncoding(value.Substring(index,(nextIndex)-(index)));
        nextIndex++;
      }
      return null;
    } else {
      int nextIndex=index;
      while(nextIndex<length){
        char c2=value[nextIndex];
        if(c2==0x09 || c2==0x0c || c2==0x0d || c2==0x0a || c2==0x20 || c2==0x3b) {
          break;
        }
        nextIndex++;
      }
      return TextEncoding.resolveEncoding(value.Substring(index,(nextIndex)-(index)));
    }
  }
  private static int indexOfBytes(byte[] array, int offset, int count, byte[] pattern){
    int endIndex=Math.Min(offset+count, array.Length);
    endIndex-=pattern.Length-1;
    if(endIndex<0 || endIndex<offset)return -1;
    bool found=false;
    for(int i=offset;i<endIndex;i++){
      found=true;
      for(int j=0;j<pattern.Length;j++){
        if(pattern[j]!=array[i+j]){
          found=false;
          break;
        }
      }
      if(found)return i;
    }
    return -1;
  }
  private static bool matchesPattern(
      byte[] pattern,
      byte[] sequence, int seqIndex, int count){
    count=Math.Min(count,sequence.Length-seqIndex);
    int len=pattern.Length;
    if(len<=count){
      for(int i=0;i<len;i++,seqIndex++){
        if((sequence[seqIndex])!=pattern[i])
          return false;
      }
      return true;
    }
    return false;
  }

  private static bool matchesPattern(
      byte[][] patterns, int index,
      byte[] sequence, int seqIndex, int count){
    byte[] pattern=patterns[index];
    count=Math.Min(count,sequence.Length-seqIndex);
    byte[] mask=patterns[index+1];
    int len=pattern.Length;
    if(len<=count){
      if(mask==null){
        for(int i=0;i<len;i++,seqIndex++){
          if((sequence[seqIndex])!=pattern[i])
            return false;
        }
      } else {
        for(int i=0;i<len;i++,seqIndex++){
          if((sequence[seqIndex]&mask[i])!=pattern[i])
            return false;
        }

      }
      return true;
    }
    return false;
  }
  private static bool matchesPatternAndTagTerminator(
      byte[][] patterns, int index,
      byte[] sequence, int seqIndex, int count){
    byte[] pattern=patterns[index];
    count=Math.Min(count,sequence.Length-seqIndex);
    byte[] mask=patterns[index+1];
    int len=pattern.Length;
    if(len+1<=count){
      for(int i=0;i<len;i++,seqIndex++){
        if((sequence[seqIndex]&mask[i])!=pattern[i])
          return false;
      }
      if(sequence[seqIndex]!=0x20 &&
          sequence[seqIndex]!=0x3e)
        return false;
      return true;
    }
    return false;
  }
  private static int readAttribute(
      byte[] data,
      int length,
      int position,
      StringBuilder attrName,
      StringBuilder attrValue
      ){
    if(attrName!=null) {
      attrName.Clear();
    }
    if(attrValue!=null) {
      attrValue.Clear();
    }
    while(position<length &&
        (data[position]==0x09 ||
        data[position]==0x0A ||
        data[position]==0x0C ||
        data[position]==0x0D ||
        data[position]==0x20 ||
        data[position]==0x2F)){
      position++;
    }
    if(position>=length || data[position]==0x3F)
      return position;
    bool empty=true;
    bool tovalue=false;
    int b=0;
    // Skip attribute name
    while(true){
      if(position>=length){
        // end of stream reached, so clear
        // the attribute name to indicate failure
        if(attrName!=null) {
          attrName.Clear();
        }
        return position;
      }
      b=(data[position]&0xFF);
      if(b==0x3D && !empty){
        position++;
        tovalue=true;
        break;
      } else if(b==0x09 || b==0x0a || b==0x0c || b==0x0d || b==0x20){
        break;
      } else if(b==0x2F || b==0x3E)
        return position;
      else {
        if(attrName!=null){
          if(b>=0x41 && b<=0x5a){
            attrName.Append((char)(b+0x20));
          } else {
            attrName.Append((char)b);
          }
        }
        empty=false;
        position++;
      }
    }
    if(!tovalue){
      while(position<length){
        b=(data[position]&0xFF);
        if(b!=0x09 && b!=0x0a && b!=0x0c && b!=0x0d && b!=0x20) {
          break;
        }
        position++;
      }
      if(position>=length){
        // end of stream reached, so clear
        // the attribute name to indicate failure
        if(attrName!=null) {
          attrName.Clear();
        }
        return position;
      }
      if((data[position]&0xFF)!=0x3D)
        return position;
      position++;
    }
    while(position<length){
      b=(data[position]&0xFF);
      if(b!=0x09 && b!=0x0a && b!=0x0c && b!=0x0d && b!=0x20) {
        break;
      }
      position++;
    }
    // Skip value
    if(position>=length){
      // end of stream reached, so clear
      // the attribute name to indicate failure
      if(attrName!=null) {
        attrName.Clear();
      }
      return position;
    }
    b=(data[position]&0xFF);
    if(b==0x22 || b==0x27){ // have quoted _string
      position++;
      while(true){
        if(position>=length){
          // end of stream reached, so clear
          // the attribute name and value to indicate failure
          if(attrName!=null) {
            attrName.Clear();
          }
          if(attrValue!=null) {
            attrValue.Clear();
          }
          return position;
        }
        int b2=(data[position]&0xFF);
        if(b==b2) { // quote mark reached
          position++;
          break;
        }
        if(attrValue!=null){
          if(b2>=0x41 && b2<=0x5a){
            attrValue.Append((char)(b2+0x20));
          } else {
            attrValue.Append((char)b2);
          }
        }
        position++;
      }
      return position;
    } else if(b==0x3E)
      return position;
    else {
      if(attrValue!=null){
        if(b>=0x41 && b<=0x5a){
          attrValue.Append((char)(b+0x20));
        } else {
          attrValue.Append((char)b);
        }
      }
      position++;
    }
    while(true){
      if(position>=length){
        // end of stream reached, so clear
        // the attribute name and value to indicate failure
        if(attrName!=null) {
          attrName.Clear();
        }
        if(attrValue!=null) {
          attrValue.Clear();
        }
        return position;
      }
      b=(data[position]&0xFF);
      if(b==0x09 || b==0x0a || b==0x0c || b==0x0d || b==0x20 || b==0x3e)
        return position;
      if(attrValue!=null){
        if(b>=0x41 && b<=0x5a){
          attrValue.Append((char)(b+0x20));
        } else {
          attrValue.Append((char)b);
        }
      }
      position++;
    }
  }
  public static string sniffContentType(PeterO.Support.InputStream input, IHttpHeaders headers)
       {
    string contentType=headers.getHeaderField("content-type");
    if(contentType!=null && (contentType.Equals("text/plain") ||
        contentType.Equals("text/plain; charset=ISO-8859-1") ||
        contentType.Equals("text/plain; charset=iso-8859-1") ||
        contentType.Equals("text/plain; charset=UTF-8"))){
      string url=headers.getUrl();
      if(url!=null && url.Length>=5 &&
          (url[0]=='h' || url[0]=='H') &&
          (url[1]=='t' || url[0]=='T') &&
          (url[2]=='t' || url[0]=='T') &&
          (url[3]=='p' || url[0]=='P') &&
          (url[4]==':'))
        return sniffTextOrBinary(input);
    }
    return sniffContentType(input,contentType);

  }

  public static string sniffContentType(PeterO.Support.InputStream input, string mediaType) {
    if(mediaType!=null && HeaderParser.isValidMediaType(mediaType)){
      string type=HeaderParser.getMediaType(mediaType,0,mediaType.Length);
      if(type.Equals("text/xml") || type.Equals("application/xml") ||
          type.EndsWith("+xml",StringComparison.Ordinal))
        return mediaType;
      if(type.Equals("*/*") || type.Equals("unknown/unknown") ||
          type.Equals("application/unknown"))
        return sniffUnknownContentType(input,true);
      if(type.Equals("text/html")){
        byte[] header=new byte[512];
        input.mark(514);
        int count=0;
        try {
          count=input.Read(header,0,512);
        } finally {
          input.reset();
        }
        int feed=sniffFeed(header,0,count);
        if(feed==0)return "text/html";
        else if(feed==1)return "application/rss+xml";
        else if(feed==2)return "application/atom+xml";
      }
      return mediaType;
    } else
      return sniffUnknownContentType(input,true);
  }

  public static EncodingConfidence sniffEncoding(PeterO.Support.InputStream stream, string encoding)
      {
    stream.mark(3);
    int b=0;
    try {
      int b1=stream.ReadByte();
      int b2=stream.ReadByte();
      if(b1==0xFE && b2==0xFF)
        return EncodingConfidence.UTF16BE;
      if(b1==0xFF && b2==0xFE)
        return EncodingConfidence.UTF16LE;
      int b3=stream.ReadByte();
      if(b1==0xEF && b2==0xBB && b3==0xBF)
        return EncodingConfidence.UTF8;
    } finally {
      stream.reset();
    }
    if(encoding!=null && encoding.Length>0){
      encoding=TextEncoding.resolveEncoding(encoding);
      if(encoding!=null)
        return new EncodingConfidence(encoding,EncodingConfidence.Certain);
    }
    // At this point, the confidence is tentative
    byte[] data=new byte[1024];
    stream.mark(1028);
    int count=0;
    try {
      count=stream.Read(data,0,1024);
    } finally {
      stream.reset();
    }
    int position=0;
    while(position<count){
      if(position+4<=count &&
          data[position+0]==0x3c && (data[position+1]&0xFF)==0x21 &&
          (data[position+2]&0xFF)==0x2d && (data[position+3]&0xFF)==0x2d){
        // Skip comment
        int hyphenCount=2;
        position+=4;
        while(position<count){
          int c=(data[position]&0xFF);
          if(c=='-'){
            hyphenCount=Math.Min(2,hyphenCount+1);
          } else if(c=='>' && hyphenCount>=2){
            break;
          } else {
            hyphenCount=0;
          }
          position++;
        }
      }
      else if(position+6<=count &&
          data[position]==0x3C &&
          ((data[position+1]&0xFF)==0x4D || (data[position+1]&0xFF)==0x6D) &&
          ((data[position+2]&0xFF)==0x45 || (data[position+2]&0xFF)==0x65) &&
          ((data[position+3]&0xFF)==0x54 || (data[position+3]&0xFF)==0x74) &&
          (data[position+4]==0x41 || data[position+4]==0x61) &&
          (data[position+5]==0x09 || data[position+5]==0x0A || data[position+5]==0x0D ||
          data[position+5]==0x0C || data[position+5]==0x20 || data[position+5]==0x2F)
          ){
        // META tag
        bool haveHttpEquiv=false;
        bool haveContent=false;
        bool haveCharset=false;
        bool gotPragma=false;
        int needPragma=0; // need pragma null
        string charset=null;
        StringBuilder attrName=new StringBuilder();
        StringBuilder attrValue=new StringBuilder();
        position+=5;
        while(true){
          int newpos=CharsetSniffer.readAttribute(data,count,position,attrName,attrValue);
          if(newpos==position) {
            break;
          }
          string attrNameString=attrName.ToString();
          if(!haveHttpEquiv && attrNameString.Equals("http-equiv")){
            haveHttpEquiv=true;
            if(attrValue.ToString().Equals("content-type")){
              gotPragma=true;
            }
          } else if(!haveContent && attrNameString.Equals("content")){
            haveContent=true;
            if(charset==null){
              string newCharset=CharsetSniffer.extractCharsetFromMeta(attrValue.ToString());
              if(newCharset!=null){
                charset=newCharset;
                needPragma=2; // need pragma true
              }
            }
          } else if(!haveCharset && attrNameString.Equals("charset")){
            haveCharset=true;
            charset=TextEncoding.resolveEncoding(attrValue.ToString());
            needPragma=1; // need pragma false
          }
          position=newpos;
        }
        if(needPragma==0 || (needPragma==2 && !gotPragma) || charset==null){
          position++;
        } else {
          if("utf-16le".Equals(charset) || "utf-16be".Equals(charset)) {
            charset="utf-8";
          }
          return new EncodingConfidence(charset);
        }
      }
      else if((position+3<=count &&
          data[position]==0x3C &&
          (data[position+1]&0xFF)==0x2F &&
          (((data[position+2]&0xFF)>=0x41 && (data[position+2]&0xFF)<=0x5A) ||
              ((data[position+2]&0xFF)>=0x61 && (data[position+2]&0xFF)<=0x7A))) || // </X
              (position+2<=count &&
              data[position]==0x3C &&
              (((data[position+1]&0xFF)>=0x41 && (data[position+1]&0xFF)<=0x5A) ||
                  ((data[position+1]&0xFF)>=0x61 && (data[position+1]&0xFF)<=0x7A))) // <X
          ){
        // </X
        while(position<count){
          if(data[position]==0x09 ||
              data[position]==0x0A ||
              data[position]==0x0C ||
              data[position]==0x0D ||
              data[position]==0x20 ||
              data[position]==0x3E){
            break;
          }
          position++;
        }
        while(true){
          int newpos=CharsetSniffer.readAttribute(data,count,position,null,null);
          if(newpos==position) {
            break;
          }
          position=newpos;
        }
        position++;
      }
      else if(position+2<=count &&
          data[position]==0x3C &&
          ((data[position+1]&0xFF)==0x21 || (data[position+1]&0xFF)==0x3F ||
          (data[position+1]&0xFF)==0x2F)){
        // <! or </ or <?
        while(position<count){
          if(data[position]!=0x3E) {
            break;
          }
          position++;
        }
        position++;
      }
      else {
        position++;
      }
    }
    int maybeUtf8=0;
    // Check for UTF-8
    position=0;
    while(position<count){
      b=(data[position]&0xFF);
      if(b<0x80){
        position++;
        continue;
      }
      if(position+2<=count &&
          (b>=0xC2 && b<=0xDF) &&
          ((data[position+1]&0xFF)>=0x80 && (data[position+1]&0xFF)<=0xBF)
          ){
        //Console.WriteLine("%02X %02X",data[position],data[position+1]);
        position+=2;
        maybeUtf8=1;
      } else if(position+3<=count &&
          (b>=0xE0 && b<=0xEF) &&
          ((data[position+2]&0xFF)>=0x80 && (data[position+2]&0xFF)<=0xBF)){
        int startbyte=(b==0xE0) ? 0xA0 : 0x80;
        int endbyte=(b==0xED) ? 0x9F : 0xBF;
        //Console.WriteLine("%02X %02X %02X",data[position],data[position+1],data[position+2]);
        if((data[position+1]&0xFF)<startbyte || (data[position+1]&0xFF)>endbyte){
          maybeUtf8=-1;
          break;
        }
        position+=3;
        maybeUtf8=1;
      } else if(position+4<=count &&
          (b>=0xF0 && b<=0xF4) &&
          ((data[position+2]&0xFF)>=0x80 && (data[position+2]&0xFF)<=0xBF) &&
          ((data[position+3]&0xFF)>=0x80 && (data[position+3]&0xFF)<=0xBF)){
        int startbyte=(b==0xF0) ? 0x90 : 0x80;
        int endbyte=(b==0xF4) ? 0x8F : 0xBF;
        //Console.WriteLine("%02X %02X %02X %02X",data[position],data[position+1],data[position+2],
        //  data[position+3]);
        if((data[position+1]&0xFF)<startbyte || (data[position+1]&0xFF)>endbyte){
          maybeUtf8=-1;
          break;
        }
        position+=4;
        maybeUtf8=1;
      } else {
        if(position+4<count){
          // we check for position here because the data may
          // end within a UTF-8 byte sequence
          maybeUtf8=-1;
        }
        break;
      }
    }
    if(maybeUtf8==1)return EncodingConfidence.UTF8_TENTATIVE;
    // Check for other multi-byte encodings
    bool hasHighByte=false;
    bool notKREUC=false;
    bool notJPEUC=false;
    bool notShiftJIS=false;
    bool notBig5=false;
    int maybeHz=0;
    bool notGbk=false;
    int maybeIso2022=0;
    position=0;
    while(position<count){
      b=(data[position]&0xFF);
      if(b<0x80){
        if(maybeIso2022==0 && b==0x1b){
          maybeIso2022=1;
        }
        if(maybeHz==0 && b==0x7e){
          if(position+1>=count ||
              (data[position+1]==(byte)'{' ||
              data[position+1]==(byte)'}')){
            // maybe HZ if it has "~{" or "~}"
            maybeHz=1;
          } else {
            maybeHz=0;
          }
        }
        position++;
        continue;
      }
      hasHighByte=true;
      if(b>0xFC){
        notShiftJIS=true;
      }
      if((b>=0x80 && b<=0x8D) || (b==0xFF)){
        notJPEUC=true;
      }
      maybeIso2022=-1;
      maybeHz=-1;
      if(b==0xFF){
        notGbk=true;
      }
      if(b==0x80 || b==0xFF){
        notKREUC=true;
        notBig5=true;
      }
      position++;
    }
    if(maybeHz<=0 && maybeIso2022<=0 && !hasHighByte)
      return EncodingConfidence.UTF8_TENTATIVE;
    IList<string> decoders=new List<string>();
    if(hasHighByte && !notKREUC){
      decoders.Add("euc-kr");
    }
    if(hasHighByte && !notBig5){
      decoders.Add("big5");
    }
    if(hasHighByte && !notJPEUC){
      decoders.Add("euc-jp");
    }
    if(hasHighByte && !notGbk){
      decoders.Add("gbk");
    }
    if(hasHighByte && !notShiftJIS){
      decoders.Add("shift_jis");
    }
    if(maybeIso2022>0){
      decoders.Add("iso-2022-jp");
      decoders.Add("iso-2022-kr");
    }
    if(maybeHz>0){
      decoders.Add("hz-gb-2312");
    }
    if(decoders.Count>0){
      int[] kana=new int[decoders.Count];
      int[] nonascii=new int[decoders.Count];
      bool[] nowFailed=new bool[decoders.Count];
      PeterO.Support.ByteArrayInputStream[] streams=new PeterO.Support.ByteArrayInputStream[decoders.Count];
      ITextDecoder[] decoderObjects=new ITextDecoder[decoders.Count];
      for(int i=0;i<decoders.Count;i++){
        streams[i]=new PeterO.Support.ByteArrayInputStream(data,0,count);
        decoderObjects[i]=TextEncoding.getDecoder(decoders[i]);
      }
      int totalValid=streams.Length;
      string validEncoding=null;
      while(true){
        totalValid=0;
        int totalRunning=0;
        for(int i=0;i<streams.Length;i++){
          if(streams[i]==null){
            if(decoders[i]!=null){
              validEncoding=decoders[i];
              totalValid++;
            }
            continue;
          }
          try {
            int c=decoderObjects[i].decode(streams[i]);
            if(c<0){
              // reached end of stream successfully
              streams[i]=null;
            }
            if(c>=0x80){
              nonascii[i]++;
            }
            // if this is a hiragana or katakana
            if(c>=0x3041 && c<=0x30ff){
              kana[i]++;
            }
            //Console.WriteLine("%s %d",decoders[i],c);
            validEncoding=decoders[i];
            totalValid++;
            totalRunning++;
            nowFailed[i]=false;
          } catch(IOException){
            // Error
            if(streams[i].available()==0){
              // Reached the end of stream; the error
              // was probably due to an incomplete
              // byte sequence
              //Console.WriteLine("at end of stream");
            } else {
              //Console.WriteLine("error %s in %s",
              //  e.GetType().getName(),decoders[i]);
              streams[i]=null;
              nowFailed[i]=true;
            }
          }
        }
        //if(failedCount>0)
        //Console.WriteLine("failed: %d",failedCount);
        for(int i=0;i<streams.Length;i++){
          if(nowFailed[i]){
            nonascii[i]=0;
            kana[i]=0;
            decoderObjects[i]=null;
            decoders[i]=null;
          }
        }
        if(totalRunning==0 || totalValid<=1) {
          break;
        }
      }
      if(totalValid==1 && validEncoding!=null)
        return new EncodingConfidence(validEncoding);
      //Console.WriteLine(ArrayUtil.toIntList(kana));
      //Console.WriteLine(ArrayUtil.toIntList(nonascii));
      //Console.WriteLine(decoders);
      for(int i=0;i<decoders.Count;i++){
        string d=decoders[i];
        if(d!=null){
          // return this encoding if the ratio of
          // kana to non-ASCII characters is high
          if(kana[i]>=nonascii[i]/5 && !"gbk".Equals(d))
            return new EncodingConfidence(d);
        }
      }
    }
    // Fall back
    string lang=StringUtility.toLowerCaseAscii(CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
    string country=StringUtility.toUpperCaseAscii(CultureInfo.CurrentCulture.Name.IndexOf('-')<0 ? "" : 
CultureInfo.CurrentCulture.Name.Substring(
1+CultureInfo.CurrentCulture.Name.IndexOf('-')));
    if(lang.Equals("ar") || lang.Equals("fa"))
      return new EncodingConfidence("windows-1256");
    if(lang.Equals("bg") || lang.Equals("ru") || lang.Equals("uk") || lang.Equals("sr"))
      return new EncodingConfidence("windows-1251");
    if(lang.Equals("cs") || lang.Equals("hr") || lang.Equals("sk"))
      return new EncodingConfidence("windows-1250");
    if(lang.Equals("hu") || lang.Equals("pl") || lang.Equals("sl"))
      return new EncodingConfidence("iso-8859-2");
    if(lang.Equals("ja"))
      return new EncodingConfidence("shift_jis");
    if(lang.Equals("zh") && (country.Equals("CN") || 
          country.Equals("CHS") || country.Equals("HANS")))
      return new EncodingConfidence("gb18030");
    else if(lang.Equals("zh"))
      return new EncodingConfidence("big5");
    if(lang.Equals("th"))
      return new EncodingConfidence("windows-874");
    if(lang.Equals("ko"))
      return new EncodingConfidence("euc-kr");
    if(lang.Equals("ku") || lang.Equals("tr"))
      return new EncodingConfidence("windows-1254");
    if(lang.Equals("lt") || lang.Equals("et") || lang.Equals("lv"))
      return new EncodingConfidence("windows-1257");
    if(lang.Equals("vi"))
      return new EncodingConfidence("windows-1258");
    if(lang.Equals("iw") || lang.Equals("he"))
      // NOTE: iw is Java's two-letter code for Hebrew
      return new EncodingConfidence("windows-1255");
    return new EncodingConfidence("windows-1252");
  }

  private static int sniffFeed(byte[] header, int offset, int count){
    if(header==null || offset<0 || count<0 || offset+count>header.Length)
      throw new ArgumentException();
    int endPos=offset+count;
    int index=offset;
    if(index+3<=endPos && (header[index]&0xFF)==0xef &&
        (header[index+1]&0xFF)==0xbb &&
        (header[index+2]&0xFF)==0xbf) {
      index+=3;
    }
    while(index<endPos){
      while(index<endPos){
        if(header[index]=='<'){
          index++;
          break;
        } else if(header[index]==0x09 || header[index]==0x0a ||
            header[index]==0x0c || header[index]==0x0d ||
            header[index]==0x20){
          index++;
        } else
          return NoFeed;
      }
      while(index<endPos){
        if(index+3<=endPos &&(header[index]&0xFF)==0x21 &&
            (header[index+1]&0xFF)==0x2d && (header[index+2]&0xFF)==0x2d){
          // Skip comment
          int hyphenCount=0;
          index+=3;
          while(index<endPos){
            int c=(header[index]&0xFF);
            if(c=='-'){
              hyphenCount=Math.Min(2,hyphenCount+1);
            } else if(c=='>' && hyphenCount>=2){
              index++;
              break;
            } else {
              hyphenCount=0;
            }
            index++;
          }
          break;
        } else if(index+1<=endPos && (header[index]&0xFF)=='!'){
          index++;
          while(index<endPos){
            if(header[index]=='>'){
              index++;
              break;
            }
            index++;
          }
          break;
        } else if(index+1<=endPos && (header[index]&0xFF)=='?'){
          int charCount=0;
          index++;
          while(index<endPos){
            int c=(header[index]&0xFF);
            if(c=='?'){
              charCount=1;
            } else if(c=='>' && charCount==1){
              index++;
              break;
            } else {
              charCount=0;
            }
            index++;
          }
          break;
        } else if(index+3<=endPos && (header[index]&0xFF)=='r' &&
            (header[index+1]&0xFF)=='s' &&
            (header[index+2]&0xFF)=='s')
          return RSSFeed;
        else if(index+4<=endPos && (header[index]&0xFF)=='f' &&
            (header[index+1]&0xFF)=='e' &&
            (header[index+2]&0xFF)=='e' &&
            (header[index+3]&0xFF)=='d')
          return AtomFeed;
        else if(index+7<=endPos && (header[index]&0xFF)=='r' &&
            (header[index+1]&0xFF)=='d' &&
            (header[index+2]&0xFF)=='f' &&
            (header[index+3]&0xFF)==':' &&
            (header[index+4]&0xFF)=='R' &&
            (header[index+5]&0xFF)=='D' &&
            (header[index+6]&0xFF)=='F'){
          index+=7;
          if(indexOfBytes(header,index,endPos-index,rdfNamespace)>=0 &&
              indexOfBytes(header,index,endPos-index,rssNamespace)>=0)
            return RSSFeed;
          else
            return NoFeed;
        } else
          return NoFeed;
      }
    }
    return NoFeed;
  }


  private static string sniffTextOrBinary(PeterO.Support.InputStream input)  {
    byte[] header=new byte[512];
    input.mark(514);
    int count=0;
    try {
      count=input.Read(header,0,512);
    } finally {
      input.reset();
    }
    if(count>=4 && header[0]==(byte)0xfe && header[1]==(byte)0xff)
      return "text/plain";
    if(count>=4 && header[0]==(byte)0xff && header[1]==(byte)0xfe)
      return "text/plain";
    if(count>=4 && header[0]==(byte)0xef && header[1]==(byte)0xbb &&
        header[2]==(byte)0xbf)
      return "text/plain";
    bool binary=false;
    for(int i=0;i<count;i++){
      int b=(header[i]&0xFF);
      if(!(b>=0x20 || b==0x09 || b==0x0a || b==0x0c || b==0x0d || b==0x1b)){
        binary=true;
        break;
      }
    }
    if(!binary)return "text/plain";
    return sniffUnknownContentType(input,false);
  }

  private static string sniffUnknownContentType(PeterO.Support.InputStream input, bool sniffScriptable) {
    byte[] header=new byte[512];
    int count=0;
    input.mark(514);
    try {
      count=input.Read(header,0,512);
    } finally {
      input.reset();
    }
    if(sniffScriptable){
      int index=0;
      while(index<count){
        if(header[index]!=0x09 && header[index]!=0x0a &&
            header[index]!=0x0c && header[index]!=0x0d &&
            header[index]!=0x20){
          break;
        }
        index++;
      }
      if(index<count && header[index]==0x3c){
        for(int i=0;i<patternsHtml.Length;i+=2){
          if(matchesPatternAndTagTerminator(patternsHtml,
              i,header,index,count))
            return "text/html";
        }
        for(int i=0;i<patternsXml.Length;i+=2){
          if(matchesPattern(patternsXml,
              i,header,index,count))
            return "text/xml";
        }
      }
      for(int i=0;i<patternsPdf.Length;i+=2){
        if(matchesPattern(patternsPdf,
            i,header,0,count))
          return "text/xml";
      }
    }
    if(matchesPattern(patternsPs,0,header,0,count))
      return "application/postscript";
    if(count>=4 && header[0]==(byte)0xfe && header[1]==(byte)0xff)
      return "text/plain";
    if(count>=4 && header[0]==(byte)0xff && header[1]==(byte)0xfe)
      return "text/plain";
    if(count>=4 && header[0]==(byte)0xef && header[1]==(byte)0xbb &&
        header[2]==(byte)0xbf)
      return "text/plain";
    // Image types
    if(matchesPattern(new byte[]{0,0,1,0},header,0,count))
      return "image/x-icon"; // icon
    if(matchesPattern(new byte[]{0,0,2,0},header,0,count))
      return "image/x-icon"; // cursor
    if(matchesPattern(new byte[]{0x42,0x4d},header,0,count))
      return "image/bmp";
    if(matchesPattern(new byte[]{0x47,0x49,0x46,0x38,0x37,0x61},header,0,count))
      return "image/gif";
    if(matchesPattern(new byte[]{0x47,0x49,0x46,0x38,0x39,0x61},header,0,count))
      return "image/gif";
    if(matchesPattern(new byte[]{0x52,0x49,0x46,0x46},
        header,0,count) &&
        matchesPattern(new byte[]{0x57,0x45,0x42,0x50,0x56,0x50},
            header,8,count-8))
      return "image/webp";
    if(matchesPattern(new byte[]{(byte)0x89,0x50,0x4e,0x47,0x0d,0x0a,0x1a,0x0a},header,0,count))
      return "image/png";
    if(matchesPattern(new byte[]{(byte)0xff,(byte)0xd8,(byte)0xff},header,0,count))
      return "image/jpeg";
    // Audio and video types
    if(matchesPattern(new byte[]{0x1a,0x45,(byte)0xdf,(byte)0xa3},header,0,count))
      return "video/webm";
    if(matchesPattern(new byte[]{0x2e,0x7e,(byte)0x6e,(byte)0x64},header,0,count))
      return "audio/basic";
    if(matchesPattern(new byte[]{(byte)'F',(byte)'O',(byte)'R',(byte)'M'},header,0,count) &&
        matchesPattern(new byte[]{(byte)'A',(byte)'I',(byte)'F',(byte)'F'},header,8,count-8))
      return "audio/aiff";
    if(matchesPattern(new byte[]{(byte)'I',(byte)'D',(byte)'3'},header,0,count))
      return "audio/mpeg";
    if(matchesPattern(new byte[]{(byte)'O',(byte)'g',(byte)'g',(byte)'S',0},header,0,count))
      return "application/ogg";
    if(matchesPattern(new byte[]{(byte)'M',(byte)'T',(byte)'h',(byte)'d',0,0,0,6},header,0,count))
      return "audio/midi";
    if(matchesPattern(new byte[]{(byte)'R',(byte)'I',(byte)'F',(byte)'F'},header,0,count)){
      if(matchesPattern(new byte[]{(byte)'A',(byte)'V',(byte)'I',(byte)' '},header,8,count-8))
        return "video/avi";
      if(matchesPattern(new byte[]{(byte)'W',(byte)'A',(byte)'V',(byte)'E'},header,8,count-8))
        return "audio/wave";
    }
    if(count>=12){
      int boxSize=(header[0]&0xFF)<<24;
      boxSize|=(header[1]&0xFF)<<16;
      boxSize|=(header[2]&0xFF)<<8;
      boxSize|=(header[3]&0xFF);
      if((boxSize&3)==0 && boxSize>=0 && count>=boxSize &&
          header[4]==(byte)'f' &&
          header[5]==(byte)'t' &&
          header[6]==(byte)'y' &&
          header[7]==(byte)'p'){
        if(header[8]==(byte)'m' &&
            header[9]==(byte)'p' &&
            header[10]==(byte)'4')
          return "video/mp4";
        int index=16;
        while(index<boxSize){
          if((header[index]&0xFF)=='m' &&
              (header[index+1]&0xFF)=='p' &&
              (header[index+2]&0xFF)=='4')
            return "video/mp4";
          index+=4;
        }
      }
    }
    // Archive types
    if(matchesPattern(new byte[]{0x1f,(byte)0x8b,8},header,0,count))
      return "application/x-gzip";
    if(matchesPattern(new byte[]{(byte)'P',(byte)'K',3,4},header,0,count))
      return "application/zip";
    if(matchesPattern(new byte[]{(byte)'R',(byte)'a',(byte)'r',(byte)' ',0x1a,7,0},header,0,count))
      return "application/x-rar-compressed";
    bool binary=false;
    for(int i=0;i<count;i++){
      int b=(header[i]&0xFF);
      if(!(b>=0x20 || b==0x09 || b==0x0a || b==0x0c || b==0x0d || b==0x1b)){
        binary=true;
        break;
      }
    }
    if(!binary)return "text/plain";
    return "application/octet-stream";
  }

  private CharsetSniffer(){}

}

}
