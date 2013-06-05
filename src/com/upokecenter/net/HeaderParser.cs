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
namespace com.upokecenter.net {
using System;
using System.Text;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using com.upokecenter.util;






/**
 * 
 * Contains methods useful for parsing header fields.
 * 
 * @author Peter
 *
 */
public sealed class HeaderParser {

  internal enum QuotedStringRule {
    Http,
    Rfc5322,
    Smtp // RFC5321
  }
  private static string[] emptyStringArray=new string[0];

  private static void appendParameterValue(string str, StringBuilder builder){
    // if _string is a valid MIME token, the return value
    // will be the end of the _string
    if(skipMimeToken(str,0,str.Length,null,false)==str.Length){
      // append the _string as is
      builder.Append(str);
      return;
    } else {
      // otherwise, we must quote the _string
      builder.Append('"');
      int endIndex=str.Length;
      for(int i=0;i<endIndex;i++){
        char c=str[i];
        if(c=='"' || c==0x7F || c<0x20){
          builder.Append('\\');
        }
        builder.Append(c);
      }
      builder.Append('"');
    }
  }

  /**
   * Formats a date and time to a _string that complies
   * with HTTP/1.1 (RFC2616).
   * 
   * @param date the number of milliseconds since midnight,
   * January 1, 1970 GMT.
   * @return a _string formatted under the rules of HTTP/1.1.
   */
  public static string formatHttpDate(long date){
    int[] components=DateTimeUtility.getGmtDateComponents(date);
    int dow=components[7]; // 1 to 7
    int month=components[1]; // 1 to 12
    string dayofweek=null;
    if(dow==1) {
      dayofweek="Sun, ";
    }
    else if(dow==2) {
      dayofweek="Mon, ";
    }
    else if(dow==3) {
      dayofweek="Tue, ";
    }
    else if(dow==4) {
      dayofweek="Wed, ";
    }
    else if(dow==5) {
      dayofweek="Thu, ";
    }
    else if(dow==6) {
      dayofweek="Fri, ";
    }
    else if(dow==7) {
      dayofweek="Sat, ";
    }
    #if DEBUG
if(!(dayofweek!=null ))throw new InvalidOperationException(Convert.ToString(dow,CultureInfo.InvariantCulture));
#endif
    string[] months={
        ""," Jan "," Feb "," Mar "," Apr ",
        " May "," Jun "," Jul "," Aug ",
        " Sep "," Oct "," Nov "," Dec "
    };
    #if DEBUG
if(!(month>=1 && month<=12 ))throw new InvalidOperationException(Convert.ToString(month,CultureInfo.InvariantCulture));
#endif
    string monthstr=months[month];
    StringBuilder builder=new StringBuilder();
    builder.Append(dayofweek);
    builder.Append((char)('0'+((components[2]/10)%10)));
    builder.Append((char)('0'+((components[2])%10)));
    builder.Append(monthstr);
    builder.Append((char)('0'+((components[0]/1000)%10)));
    builder.Append((char)('0'+((components[0]/100)%10)));
    builder.Append((char)('0'+((components[0]/10)%10)));
    builder.Append((char)('0'+((components[0])%10)));
    builder.Append(' ');
    builder.Append((char)('0'+((components[3]/10)%10)));
    builder.Append((char)('0'+((components[3])%10)));
    builder.Append(':');
    builder.Append((char)('0'+((components[4]/10)%10)));
    builder.Append((char)('0'+((components[4])%10)));
    builder.Append(':');
    builder.Append((char)('0'+((components[5]/10)%10)));
    builder.Append((char)('0'+((components[5])%10)));
    builder.Append(" GMT");
    return builder.ToString();
  }

  public static string getCharset(string data){
    if(data==null)return "us-ascii";
    return getCharset(data, 0,data.Length);
  }

  /**
   * Extracts the charset parameter from a MIME media
   * type.  For example, in the _string "text/plain;charset=utf-8",
   * returns "utf-8".  This method skips folding whitespace and
   * comments where allowed under RFC5322.  For example,
   * a _string like "text/plain;\r\n  charset=utf-8" is allowed.
   * @param index the index into the _string where the
   * media type begins.
   * @param endIndex an index into the end of the _string.
   * @param data a _string containing a MIME media type.
   * 
   * @return the charset parameter, converted to ASCII lower-case,
   * if it exists, or "us-ascii" if the media type is null, absent, or
   * ill-formed (RFC2045 sec. 5.2), or if the media type is
   * "text/plain" or "text/xml" and doesn't have a charset parameter
   * (see RFC2046 and RFC3023, respectively),
   * or the empty _string otherwise.
   */
  public static string getCharset(string data, int index, int endIndex){
    if(data==null)
      return "us-ascii";
    string mediaType=getMediaType(data,index,endIndex);
    if(mediaType.Length==0)
      return "us-ascii";
    string charset=getMimeParameter(data,index,data.Length, "charset");
    if(charset!=null)return StringUtility.toLowerCaseAscii(charset);
    if("text/plain".Equals(mediaType) || "text/xml".Equals(mediaType))
      return "us-ascii";
    return "";
  }

  public static byte[] getDataURLBytes(string dataURL){
    int[] components=URIUtility.splitIRI(dataURL);
    // check if the scheme is "data"
    if(components==null || components[0]<0 ||
        !StringUtility.equalsIgnoreCaseAscii(
            "data",
            dataURL.Substring(components[0],(components[1])-(components[0]))))
      return null;
    // get just the path, not the query too
    // (it's ambiguous whether the "data path" should consist of just a path
    // or both a path and query, now that RFC3986 allows query strings
    // in all URIs)
    string path=dataURL.Substring(components[4],(components[5])-(components[4]));
    return getDataURLBytesInternal(path);
  }


  private static byte[] getDataURLBytesInternal(string dataPath){
    // assumes "data" consists of just the path extracted from a URL/URI
    int index=HeaderParser.skipDataUrlContentType(dataPath, 0,dataPath.Length,null);
    bool base64=false;
    if(com.upokecenter.util.StringUtility.startsWith(dataPath,";base64,",index)){
      index+=7;
      base64=true;
    }
    if(index<dataPath.Length && dataPath[index]==','){
      index++;
      ByteList mos=new ByteList();
      int len=dataPath.Length;
      for(int j=index;j<len;j++){
        int c=dataPath[j];
        // matches productions "unreserved" and
        // "reserved" of RFC2396, including
        // '?' (even though it delimits
        // a query _string, which is allowed in all
        // URIs as of RFC3986)
        if(!((c&0x7F)==c && "-_.!~*'();/:@&=+$,?".IndexOf((char)c)>=0) &&
            !(c>='A' && c<='Z') &&
            !(c>='a' && c<='z') &&
            !(c>='0' && c<='9'))
          return null;
        // matches percent-encoded characters
        // (production "escaped" of RFC2396)
        if(c=='%'){
          if(index+2<len){
            int a=HeaderParser.toHexNumber(dataPath[index+1]);
            int b=HeaderParser.toHexNumber(dataPath[index+2]);
            if(a>=0 && b>=0){
              mos.append((byte) (a*16+b));
              index+=2;
              continue;
            }
          }
        }
        mos.append((byte) (c&0xFF));
      }
      byte[] retval=mos.toByteArray();
      if(base64){
        try {
          return Base64.decode(retval);
        } catch(IOException){
          return null;
        }
      }
      return retval;
    } else
      return null;
  }

  /**
   * Extracts the MIME media type, including its parameters,
   * from a Data URL (RFC2397). This function should be used
   * before calling getMediaType or getMimeParameter because
   * there are several differences in the MIME media type in
   * data URLs than in Content-Type headers:
   * <ul>
   * <li>Each part of a MIME content type can be URL-encoded
   * in a data URL, while they can't in a Content-Type header.</li>
   * <li>The type and subtype can be left out. If left out, the media
   * type "text/plain" is assumed.</li>
   * <li>No whitespace is allowed between semicolons of
   * a MIME media type in a data URL.</li>
   * </ul>
   * @param _string a _string containing a data URL
   * Example: "data:,test" or "data:text/plain,test"
   * or "data:text/html;charset=utf-8,test"
   * @return the data URL's content type. If the _string is null or
   *  is not a valid Internationalized Resource Identifier, if the
   *  _string isn't a data URL, or the _string's MIME media type
   *  is ill-formed, returns an empty _string. If the data URL's
   * tMIME type is blank, the return value will be equal to
   * "text/plain;charset=us-ascii".
   */
  public static string getDataURLContentType(string dataURL){
    int[] components=URIUtility.splitIRI(dataURL);
    // check if the scheme is "data"
    if(components==null || components[0]<0 ||
        !StringUtility.equalsIgnoreCaseAscii(
            "data",
            dataURL.Substring(components[0],(components[1])-(components[0]))))
      return "";
    // get just the path, not the query too
    // (it's ambiguous whether the "data path" should consist of just a path
    // or both a path and query, now that RFC3986 allows query strings
    // in all URIs)
    string path=dataURL.Substring(components[4],(components[5])-(components[4]));
    return getDataURLContentTypeInternal(path);
  }

  private static string getDataURLContentTypeInternal(string dataPath){
    StringBuilder builder=new StringBuilder();
    HeaderParser.skipDataUrlContentType(dataPath,0,dataPath.Length,builder);
    return builder.ToString();
  }
  private static int getDecOctetSMTPLength(string s, int index,
      int endOffset, int c, int delim){
    if(c>='0' && c<='9' && index+2<endOffset &&
        (s[index+1]>='0' && s[index+1]<='9') &&
        s[index+2]==delim)
      return 3;
    else if(c=='2' && index+3<endOffset &&
        (s[index+1]=='5') &&
        (s[index+2]>='0' && s[index+2]<='5') &&
        s[index+3]==delim)
      return 4;
    else if(c=='2' && index+3<endOffset &&
        (s[index+1]>='0' && s[index+1]<='4') &&
        (s[index+2]>='0' && s[index+2]<='9') &&
        s[index+3]==delim)
      return 4;
    else if((c=='0' || c=='1') && index+3<endOffset &&
        (s[index+1]>='0' && s[index+1]<='9') &&
        (s[index+2]>='0' && s[index+2]<='9') &&
        s[index+3]==delim)
      return 4;
    else if(c>='0' && c<='9' && index+1<endOffset &&
        s[index+1]==delim)
      return 2;
    else return 0;
  }
  /**
   * 
   * Parses a _string consisting of language tags under
   * Best Current Practice 47.  Examples include "en"
   * for English, or "fr-ca" for Canadian French.
   * 
   * The _string is treated as a Content-Language header
   * value under RFC 3282.
   * 
   * @param str a _string.
   * @return an array of language tags within the given
   * _string, or an empty
   * array if str is null, if there are no language tags,
   * or at least one language tag in the given
   * _string is invalid under Best Current Practice 47.
   * The language tags will be converted to ASCII lower-case.
   */
  public static string[] getLanguages(string str){
    if(str==null)return emptyStringArray;
    return getLanguages(str,0,str.Length,false);
  }

  private static string[] getLanguages(string str, int index, int endIndex, bool httpRules){
    if(index==endIndex || str==null)
      return emptyStringArray;
    IList<string> strings=new List<string>();
    if(!httpRules) {
      index=skipCFWS(str,index,endIndex,null);
    }
    while(true){
      int i2=skipLanguageTag(str,index,endIndex);
      if(i2==index)return emptyStringArray;
      string tag=StringUtility.toLowerCaseAscii(str.Substring(index,(i2)-(index)));
      i2=index;
      if(!isValidLanguageTag(tag))return emptyStringArray;
      strings.Add(tag);
      if(!httpRules){ // RFC 3282 rules
        index=skipCFWS(str,index,endIndex,null);
        if(index>=endIndex) {
          break;
        }
        if(str[index]!=',')return emptyStringArray;
        index++;
        index=skipCFWS(str,index,endIndex,null);
      } else { // HTTP/1.1 rules
        i2=skipLws(str,index,endIndex,null);
        if(i2!=index && i2>=endIndex)return emptyStringArray;
        else if(i2>=endIndex) {
          break;
        }
        index=i2;
        if(str[index]!=',')return emptyStringArray;
        index++;
        index=skipLws(str,index,endIndex,null);
      }
    }
    return strings.ToArray();
  }

  public static string getMediaType(string str){
    if(str==null)return "";
    return getMediaType(str,0,str.Length);
  }

  /**
   * Extracts the type and subtype from a MIME media
   * type.  For example, in the _string "text/plain;charset=utf-8",
   * returns "text/plain".
   * <br><br>
   * Note that the default media type according to RFC2045
   * section 2 is "text/plain"; this function will not return that
   * value if the media type is ill-formed; rather, this function
   * is useful more to check if a media type is well-formed.
   * <br><br>
   * @param str a _string containing a MIME media type.
   * @param index the index into the _string where the
   * media type begins. Specify 0 for the beginning of the
   * _string.
   * @param endIndex the index for the end of the _string.
   * @return the type and subtype, or an empty _string
   * if the _string is not a valid MIME media type.
   * The _string will be normalized to ASCII lower-case.
   */
  public static string getMediaType(string str, int index, int endIndex){
    if(str==null)return "";
    int i=skipMimeTypeSubtype(str,index,endIndex,null);
    if(i==index || i>=endIndex || str[i]!='/')
      return "";
    i++;
    int i2=skipMimeTypeSubtype(str,i,endIndex,null);
    if(i==i2)
      return "";
    if(i2<endIndex){
      // if not at end
      int i3=skipCFWS(str,i2,endIndex,null);
      if(i3==endIndex || (i3<endIndex && str[i3]!=';' && str[i3]!=','))
        // at end, or not followed by ";" or ",", so not a media type
        return "";
    }
    return StringUtility.toLowerCaseAscii(str.Substring(index,(i2)-(index)));
  }


  public static string getMimeParameter(string data, int index, int endIndex, string parameter){
    if(data==null)return null;
    return getMimeParameter(data, index, data.Length, parameter,false);
  }

  /**
   * Extracts a parameter from a MIME media
   * type.  For example, in the _string "text/plain;charset=utf-8",
   * returns "utf-8" if the parameter is "charset".
   * This method either skips folding whitespace and comments
   * where allowed under RFC5322, or skips linear whitespace
   * where allowed under HTTP/1.1.  For example,
   * a _string like "text/plain;\r\n  charset=utf-8" is allowed.
   * @param index the index into the _string where the
   * media type begins.
   * @param endIndex an index into the end of the _string.
   * @param parameter a parameter name.
   * @param str a _string containing a MIME media type.
   * Parameters are compared case-insensitively.
   * @param httpRules If false, the whitespace rules of RFC5322
   * are used. If true, the whitespace rules of HTTP/1.1 (RFC2616)
   * are used, and parameter continuations under RFC2231 sec. 3
   * are supported.
   * @return the parameter, or null if the parameter
   * doesn't exist or the media type _string is ill-formed.
   */
  private static string getMimeParameter(
      string data, int index, int endIndex, string parameter, bool httpRules){
    if(data==null || parameter==null)
      return null;
    string ret=getMimeParameterRaw(data,index,endIndex,parameter,httpRules);
    if(!httpRules && ret==null){
      ret=getMimeParameterRaw(data,index,endIndex,parameter+"*0",httpRules);
      if(ret!=null){
        int pindex=1;
        // Support parameter continuations under RFC2184 sec. 3
        while(true){
          string ret2=getMimeParameterRaw(
              data,index,endIndex,
              parameter+"*"+Convert.ToString(pindex,CultureInfo.InvariantCulture),httpRules);
          if(ret2==null) {
            break;
          }
          pindex++;
          ret+=ret2;
        }
      }
    }
    return ret;
  }

  /**
   * Extracts a parameter from a MIME media
   * type.  For example, in the _string "text/plain;charset=utf-8",
   * returns "utf-8" if the parameter is "charset".
   * This method skips folding whitespace and comments
   * where allowed under RFC5322.  For example,
   * a _string like "text/plain;\r\n  charset=utf-8" is allowed.
   * 
   * @param str a _string containing a MIME media type.
   * Parameters are compared case-insensitively.
   * @param index the index into the _string where the
   * media type begins.
   * @param parameter a parameter name.
   * @return the parameter, or null if the parameter
   * doesn't exist or the media type _string is ill-formed.
   */
  public static string getMimeParameter(string data, int index, string parameter){
    if(data==null)return null;
    return getMimeParameter(data, index, data.Length, parameter);
  }


  private static string getMimeParameterRaw(
      string data, int index, int endIndex,
      string parameter, bool httpRules){
    if(data==null || parameter==null)
      return null;
    if((endIndex-index)<parameter.Length)
      return null;
    parameter=StringUtility.toLowerCaseAscii(parameter);
    string mediaType=getMediaType(data,index,endIndex);
    index+=mediaType.Length;
    while(true){
      // RFC5322 uses skipCFWS when skipping whitespace;
      // HTTP currently uses skipLws, though that may change
      // to skipWsp in a future revision of HTTP
      if(httpRules) {
        index=skipLws(data,index,endIndex,null);
      } else {
        index=skipCFWS(data,index,endIndex,null);
      }
      if(index>=endIndex || data[index]!=';')
        return null;
      index++;
      if(httpRules) {
        index=skipLws(data,index,endIndex,null);
      } else {
        index=skipCFWS(data,index,endIndex,null);
      }
      StringBuilder builder=new StringBuilder();
      int afteratt=skipMimeTypeSubtype(data,index,endIndex,builder);
      if(afteratt==index) // ill-formed attribute
        return null;
      string attribute=builder.ToString();
      index=afteratt;
      if(index>=endIndex)
        return null;
      if(data[index]!='=')
        return null;
      bool isToken=StringUtility.toLowerCaseAscii(attribute).Equals(parameter);
      index++;
      if(index>=endIndex)
        return "";
      builder.Clear();
      // try getting the value quoted
      int qs=skipQuotedString(
          data,index,endIndex,isToken ? builder : null,
              httpRules ? QuotedStringRule.Http : QuotedStringRule.Rfc5322);
      if(qs!=index){
        if(isToken)
          return builder.ToString();
        index=qs;
        continue;
      }
      builder.Clear();
      // try getting the value unquoted
      // Note we don't use getAtom
      qs=skipMimeToken(data,index,endIndex,isToken ? builder : null,httpRules);
      if(qs!=index){
        if(isToken)
          return builder.ToString();
        index=qs;
        continue;
      }
      // no valid value, return
      return null;
    }
  }

  private static int getPositiveNumber(string v, int index){
    int length=v.Length;
    char c=(char)0;
    bool haveNumber=false;
    int startIndex=index;
    string number=null;
    while(index<length){ // skip whitespace
      c=v[index];
      if(c<'0' || c>'9'){
        if(!haveNumber)return -1;
        try {
          number=v.Substring(startIndex,(index)-(startIndex));
          return Int32.Parse(number,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
        } catch(FormatException){
          return Int32.MaxValue;
        }
      } else {
        haveNumber=true;
      }
      index++;
    }
    try {
      number=v.Substring(startIndex,(length)-(startIndex));
      return Int32.Parse(number,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
    } catch(FormatException){
      return Int32.MaxValue;
    }
  }

  internal static int getResponseCode(string s){
    int index=0;
    int length=s.Length;
    if(s.IndexOf("HTTP/",index,StringComparison.Ordinal)!=index)
      return -1;
    index+=5;
    index=skipZeros(s,index);
    if(index>=length || s[index]!='1')
      return -1;
    index++;
    if(index>=length || s[index]!='.')
      return -1;
    index++;
    index=skipZeros(s,index);
    if(index<length && s[index]=='1') {
      index++;
    }
    if(index>=length || s[index]!=' ')
      return -1;
    index++;
    if(index+3>=length)return -1;
    if(skipDigits(s,index)!=index+3 ||
        s[index+3]!=' ')return -1;
    int num=getPositiveNumber(s,index);
    return num;
  }

  private static bool isAtext(char c){
    return (c>='A' && c<='Z') ||
        (c>='a' && c<='z')  ||
        ((c&0x7F)==c && "0123456789!#$%&'*+-/=?^_`{}|~".IndexOf(c)>=0);
  }

  private static bool isHexChar(char c) {
    return ((c>='a' && c<='f') ||
        (c>='A' && c<='F') ||
        (c>='0' && c<='9'));
  }


  static bool isValidAddrSpecRfc5322(string s){
    if(s==null)return false;
    StringBuilder loc=new StringBuilder();
    StringBuilder dom=new StringBuilder();
    int index=skipAddrSpec(s,0,s.Length,loc,dom);
    if(index!=s.Length)return false;
    string locString=loc.ToString();
    string domString=dom.ToString();
    if(locString.Length==0)
      return false;
    if(domString.Length==0)
      return false;
    return true;
  }

  public static bool isValidLanguageTag(string str){
    int index=0;
    int endIndex=str.Length;
    int startIndex=index;
    if(index+1<endIndex){
      char c1=str[index];
      char c2=str[index+1];
      if(
          ((c1>='A' && c1<='Z') || (c1>='a' && c1<='z')) &&
          ((c2>='A' && c2<='Z') || (c2>='a' && c2<='z'))
          ){
        index+=2;
        if(index==endIndex)return true; // case AA
        index+=2;
        // convert the language tag to lower case
        // to simplify handling
        str=StringUtility.toLowerCaseAscii(str);
        c1=str[index];
        // Straightforward cases
        if((c1>='a' && c1<='z')){
          index++;
          // case AAA
          if(index==endIndex)return true;
          c1=str[index]; // get the next character
        }
        if(c1=='-'){ // case AA- or AAA-
          index++;
          if(index+2==endIndex){ // case AA-?? or AAA-??
            c1=str[index];
            c2=str[index];
            if(((c1>='a' && c1<='z')) && ((c2>='a' && c2<='z')))
              return true; // case AA-BB or AAA-BB
          }
        }
        // match grandfathered language tags
        if(str.Equals("sgn-be-fr") || str.Equals("sgn-be-nl") || str.Equals("sgn-ch-de") ||
            str.Equals("en-gb-oed"))return true;
        // More complex cases
        string[] splitString=StringUtility.splitAt(
            str.Substring(startIndex,(endIndex)-(startIndex)),"-");
        if(splitString.Length==0)return false;
        int splitIndex=0;
        int splitLength=splitString.Length;
        int len=lengthIfAllAlpha(splitString[splitIndex]);
        if(len<2 || len>8)return false;
        if(len==2 || len==3){
          splitIndex++;
          // skip optional extended language subtags
          for(int i=0;i<3;i++){
            if(splitIndex<splitLength && lengthIfAllAlpha(splitString[splitIndex])==3){
              if(i>=1)
                // point 4 in section 2.2.2 renders two or
                // more extended language subtags invalid
                return false;
              splitIndex++;
            } else {
              break;
            }
          }
        }
        // optional script
        if(splitIndex<splitLength && lengthIfAllAlpha(splitString[splitIndex])==4) {
          splitIndex++;
        }
        // optional region
        if(splitIndex<splitLength && lengthIfAllAlpha(splitString[splitIndex])==2) {
          splitIndex++;
        } else if(splitIndex<splitLength && lengthIfAllDigit(splitString[splitIndex])==3) {
          splitIndex++;
        }
        // variant, any number
        IList<string> variants=null;
        while(splitIndex<splitLength){
          string curString=splitString[splitIndex];
          len=lengthIfAllAlphaNum(curString);
          if(len>=5 && len<=8){
            if(variants==null){
              variants=new List<string>();
            }
            if(!variants.Contains(curString)) {
              variants.Add(curString);
            } else return false; // variant already exists; see point 5 in section 2.2.5
            splitIndex++;
          } else if(len==4 && (curString[0]>='0' && curString[0]<='9')){
            if(variants==null){
              variants=new List<string>();
            }
            if(!variants.Contains(curString)) {
              variants.Add(curString);
            } else return false; // variant already exists; see point 5 in section 2.2.5
            splitIndex++;
          } else {
            break;
          }
        }
        // extension, any number
        if(variants!=null) {
          variants.Clear();
        }
        while(splitIndex<splitLength){
          string curString=splitString[splitIndex];
          int curIndex=splitIndex;
          if(lengthIfAllAlphaNum(curString)==1 &&
              !curString.Equals("x")){
            if(variants==null){
              variants=new List<string>();
            }
            if(!variants.Contains(curString)) {
              variants.Add(curString);
            } else return false; // extension already exists
            splitIndex++;
            bool havetoken=false;
            while(splitIndex<splitLength){
              curString=splitString[splitIndex];
              len=lengthIfAllAlphaNum(curString);
              if(len>=2 && len<=8){
                havetoken=true;
                splitIndex++;
              } else {
                break;
              }
            }
            if(!havetoken){
              splitIndex=curIndex;
              break;
            }
          } else {
            break;
          }
        }
        // optional private use
        if(splitIndex<splitLength){
          int curIndex=splitIndex;
          if(splitString[splitIndex].Equals("x")){
            splitIndex++;
            bool havetoken=false;
            while(splitIndex<splitLength){
              len=lengthIfAllAlphaNum(splitString[splitIndex]);
              if(len>=1 && len<=8){
                havetoken=true;
                splitIndex++;
              } else {
                break;
              }
            }
            if(!havetoken) {
              splitIndex=curIndex;
            }
          }
        }
        // check if all the tokens were used
        return (splitIndex==splitLength);
      } else if(c2=='-' && (c1=='x' || c1=='X')){
        // private use
        index++;
        while(index<endIndex){
          int count=0;
          if(str[index]!='-')return false;
          index++;
          while(index<endIndex){
            c1=str[index];
            if(((c1>='A' && c1<='Z') || (c1>='a' && c1<='z') || (c1>='0' && c1<='9'))){
              count++;
              if(count>8)return false;
            } else if(c1=='-') {
              break;
            } else return false;
            index++;
          }
          if(count<1)return false;
        }
        return true;
      } else if(c2=='-' && (c1=='i' || c1=='I')){
        // grandfathered language tags
        str=StringUtility.toLowerCaseAscii(str);
        return (str.Equals("i-ami") || str.Equals("i-bnn") ||
            str.Equals("i-default") || str.Equals("i-enochian") ||
            str.Equals("i-hak") || str.Equals("i-klingon") ||
            str.Equals("i-lux") || str.Equals("i-navajo") ||
            str.Equals("i-mingo") || str.Equals("i-pwn") ||
            str.Equals("i-tao") || str.Equals("i-tay") ||
            str.Equals("i-tsu"));
      } else return false;
    } else
      return false;
  }

  public static bool isValidMediaType(string data){
    if(data==null)
      return false;
    return isValidMediaType(data,0,data.Length,true);
  }

  public static bool isValidMediaType(
      string data,
      int index,
      int endIndex,
      bool httpRules // true: use RFC2616 (HTTP/1.1) rules; false: use RFC5322 rules
      ){
    if(data==null)
      return false;
    string mediaType=getMediaType(data,index,endIndex);
    index+=mediaType.Length;
    while(true){
      if(index>=endIndex)
        return true;
      // RFC5322 uses skipCFWS when skipping whitespace;
      // HTTP currently uses skipLws, though that may change
      // to skipWsp in a future revision of HTTP
      if(httpRules) {
        index=skipLws(data,index,endIndex,null);
      } else {
        index=skipCFWS(data,index,endIndex,null);
      }
      if(index>=endIndex)
        return false;
      if(data[index]!=';')
        return false;
      index++;
      if(httpRules) {
        index=skipLws(data,index,endIndex,null);
      } else {
        index=skipCFWS(data,index,endIndex,null);
      }
      int afteratt=skipMimeTypeSubtype(data,index,endIndex,null);
      if(afteratt==index) // ill-formed attribute
        return false;
      index=afteratt;
      if(index>=endIndex)
        return false;
      if(data[index]!='=')
        return false;
      index++;
      if(index>=endIndex)
        return false;
      // try getting the value quoted
      int qs=skipQuotedString(data,index,endIndex,null,
          httpRules ? QuotedStringRule.Http : QuotedStringRule.Rfc5322);
      if(qs!=index){
        index=qs;
        continue;
      }
      // try getting the value unquoted
      qs=skipMimeToken(data,index,endIndex,null,httpRules);
      if(qs!=index){
        index=qs;
        continue;
      }
      // no valid value, return
      return false;
    }
  }
  /**
   * Determines whether the _string is a well-formed
   * email address under the Simple Mail Transfer Protocol,
   * RFC5321 (matching the production "Mailbox" in section
   * 4.1.2).  Note that only ASCII characters are allowed
   * in a mailbox _string under that specification.
   * Length restrictions on "local parts" and "domains"
   * under section 4.5.3 are checked.
   * 
   * @param s a _string to check
   * @return true if the _string is a well-formed
   * mailbox under SMTP, or false otherwise.
   */
  public static bool isWellFormedMailbox(string s){
    if(s==null)return false;
    int idx=skipMailboxRfc5321(s,0,s.Length,null);
    return (idx==s.Length);
  }

  private static int lengthIfAllAlpha(string str){
    int len=(str==null) ? 0 : str.Length;
    for(int i=0;i<len;i++){
      char c1=str[i];
      if(!((c1>='A' && c1<='Z') || (c1>='a' && c1<='z')))
        return 0;
    }
    return len;
  }

  private static int lengthIfAllAlphaNum(string str){
    int len=(str==null) ? 0 : str.Length;
    for(int i=0;i<len;i++){
      char c1=str[i];
      if(!((c1>='A' && c1<='Z') || (c1>='a' && c1<='z') || (c1>='0' && c1<='9')))
        return 0;
    }
    return len;
  }

  private static int lengthIfAllDigit(string str){
    int len=(str==null) ? 0 : str.Length;
    for(int i=0;i<len;i++){
      char c1=str[i];
      if(!((c1>='0' && c1<='9')))
        return 0;
    }
    return len;
  }

  private static int parse2Digit(string v, int index){
    int value=0;
    char c=(char)0;
    if(index<v.Length && (c=v[index])>='0' && c<='9'){
      value+=10*(c-'0'); index++;
    } else return -1;
    if(index<v.Length && (c=v[index])>='0' && c<='9'){
      value+=(c-'0'); index++;
    } else return -1;
    return value;
  }

  private static int parse4Digit(string v, int index){
    int value=0;
    char c=(char)0;
    if(index<v.Length && (c=v[index])>='0' && c<='9'){
      value+=1000*(c-'0'); index++;
    } else return -1;
    if(index<v.Length && (c=v[index])>='0' && c<='9'){
      value+=100*(c-'0'); index++;
    } else return -1;
    if(index<v.Length && (c=v[index])>='0' && c<='9'){
      value+=10*(c-'0'); index++;
    } else return -1;
    if(index<v.Length && (c=v[index])>='0' && c<='9'){
      value+=(c-'0'); index++;
    } else return -1;
    return value;
  }
  private static int parseDecOctetSMTP(string s, int index,
      int endOffset, int c, int delim){
    if(c>='0' && c<='9' && index+2<endOffset &&
        (s[index+1]>='0' && s[index+1]<='9') &&
        s[index+2]==delim)
      return (c-'0')*10+(s[index+1]-'0');
    else if(c=='2' && index+3<endOffset &&
        (s[index+1]=='5') &&
        (s[index+2]>='0' && s[index+2]<='5') &&
        s[index+3]==delim)
      return 250+(s[index+2]-'0');
    else if(c=='2' && index+3<endOffset &&
        (s[index+1]>='0' && s[index+1]<='4') &&
        (s[index+2]>='0' && s[index+2]<='9') &&
        s[index+3]==delim)
      return 200+(s[index+1]-'0')*10+(s[index+2]-'0');
    else if((c=='0' || c=='1') && index+3<endOffset &&
        (s[index+1]>='0' && s[index+1]<='9') &&
        (s[index+2]>='0' && s[index+2]<='9') &&
        s[index+3]==delim)
      return 100+(s[index+1]-'0')*10+(s[index+2]-'0');
    else if(c>='0' && c<='9' && index+1<endOffset &&
        s[index+1]==delim)
      return (c-'0');
    else return -1;
  }

  /**
   * Parses a date _string in one of the three formats
   * allowed by RFC2616 (HTTP/1.1).
   * 
   * @param v a _string to parse.
   * @param defaultValue a value to return if the _string
   * isn't a valid date.
   * @return number of milliseconds since midnight, January 1, 1970.
   */
  public static long parseHttpDate(string v, long defaultValue){
    if(v==null)return defaultValue;
    int index=0;
    bool rfc850=false;
    bool asctime=false;
    if(v.StartsWith("Mon",StringComparison.Ordinal)||v.StartsWith("Sun",StringComparison.Ordinal)||v.StartsWith("Fri",StringComparison.Ordinal)){
      if(com.upokecenter.util.StringUtility.startsWith(v,"day,",3)){
        rfc850=true;
        index=8;
      } else {
        index=3;
      }
    } else if(v.StartsWith("Tue",StringComparison.Ordinal)){
      if(com.upokecenter.util.StringUtility.startsWith(v,"sday,",3)){
        rfc850=true;
        index=9;
      } else {
        index=3;
      }
    } else if(v.StartsWith("Wed",StringComparison.Ordinal)){
      if(com.upokecenter.util.StringUtility.startsWith(v,"nesday,",3)){
        rfc850=true;
        index=11;
      } else {
        index=3;
      }
    } else if(v.StartsWith("Thu",StringComparison.Ordinal)){
      if(com.upokecenter.util.StringUtility.startsWith(v,"rsday,",3)){
        rfc850=true;
        index=10;
      } else {
        index=3;
      }
    } else if(v.StartsWith("Sat",StringComparison.Ordinal)){
      if(com.upokecenter.util.StringUtility.startsWith(v,"urday,",3)){
        rfc850=true;
        index=11;
      } else {
        index=3;
      }
    } else return defaultValue;
    int length=v.Length;
    int month=0,day=0,year=0;
    int hour=0,minute=0,second=0;
    if(rfc850){
      day=parse2Digit(v,index);
      if(day<0)return defaultValue;
      index+=2;
      if(index<length && v[index]!='-')return defaultValue;
      index++;
      month=parseMonth(v,index);
      if(month<0)return defaultValue;
      index+=3;
      if(index<length && v[index]!='-')return defaultValue;
      index++;
      year=parse2Digit(v,index);
      if(day<0)return defaultValue;
      index+=2;
      if(index<length && v[index]!=' ')return defaultValue;
      index++;
      year=DateTimeUtility.convertYear(year);
    } else if(com.upokecenter.util.StringUtility.startsWith(v,",",index)){
      index+=2;
      day=parse2Digit(v,index);
      if(day<0)return defaultValue;
      index+=2;
      if(index<length && v[index]!=' ')return defaultValue;
      index++;
      month=parseMonth(v,index);
      index+=3;
      if(month<0)return defaultValue;
      if(index<length && v[index]!=' ')return defaultValue;
      index++;
      year=parse4Digit(v,index);
      if(day<0)return defaultValue;
      index+=4;
      if(index<length && v[index]!=' ')return defaultValue;
      index++;
    } else if(com.upokecenter.util.StringUtility.startsWith(v," ",index)){
      index+=1;
      asctime=true;
      month=parseMonth(v,index);
      if(month<0)return defaultValue;
      index+=3;
      if(index<length && v[index]!=' ')return defaultValue;
      index++;
      day=parsePadded2Digit(v,index);
      if(day<0)return defaultValue;
      index+=2;
      if(index<length && v[index]!=' ')return defaultValue;
      index++;
    } else return defaultValue;
    hour=parse2Digit(v,index);
    if(hour<0)return defaultValue;
    index+=2;
    if(index<length && v[index]!=':')return defaultValue;
    index++;
    minute=parse2Digit(v,index);
    if(minute<0)return defaultValue;
    index+=2;
    if(index<length && v[index]!=':')return defaultValue;
    index++;
    second=parse2Digit(v,index);
    if(second<0)return defaultValue;
    index+=2;
    if(index<length && v[index]!=' ')return defaultValue;
    index++;
    if(asctime){
      year=parse4Digit(v,index);
      if(day<0)return defaultValue;
      index+=4;
    } else {
      if(!com.upokecenter.util.StringUtility.startsWith(v,"GMT",index))return defaultValue;
      index+=3;
    }
    if(index!=length)return defaultValue;
    // NOTE: Here, the month is one-based
    return DateTimeUtility.toGmtDate(year,month,day,hour,minute,second);
  }
  internal static int parseIPLiteralSMTP(string s, int offset, int endOffset){
    int index=offset;
    if(offset==endOffset)
      return -1;
    // Assumes that the character before offset
    // is a '['
    if(index+5<endOffset &&
        (s[index]=='i' ||s[index]=='I') &&
        (s[index+1]=='p' ||s[index+1]=='P') &&
        (s[index+2]=='v' ||s[index+2]=='V') &&
        (s[index+3]=='6' ||s[index+3]=='6') &&
        (s[index+4]==':') &&
        (s[index+5]==':' || isHexChar(s[index+5]))){
      // IPv6 Address
      int phase1=0;
      int phase2=0;
      bool phased=false;
      bool expectHex=false;
      bool expectColon=false;
      index+=5;
      while(index<endOffset){
        char c=s[index];
        //Console.WriteLine("%c %d",c,(phase1+(phased ? 1 : 0)+phase2));
        if(c==':' && !expectHex){
          if((phase1+(phased ? 2 : 0)+phase2)>=8)
            return -1;
          index++;
          if(index<endOffset && s[index]==':'){
            if(phased)return -1;
            phased=true;
            index++;
          }
          expectHex=true;
          expectColon=false;
          //    Console.WriteLine("colon %d [%d %d] %s",
          //    phase1+(phased ? 1 : 0)+phase2,phase1,phase2,s.Substring(index));
          continue;
        } else if((c>='0' && c<='9') && !expectColon &&
            (phased || (phase1+(phased ? 2 : 0)+phase2)==6)){
          // Check for IPv4 address
          int decOctet=parseDecOctetSMTP(s,index,endOffset,c,'.');
          if(decOctet>=0){
            if((phase1+(phased ? 2 : 0)+phase2)>6)
              // IPv4 address illegal at this point
              //Console.WriteLine("Illegal IPv4");
              return -1;
            else {
              // Parse the rest of the IPv4 address
              phase2+=2;
              if(decOctet>=100) {
                index+=4;
              } else if(decOctet>=10) {
                index+=3;
              } else {
                index+=2;
              }
              decOctet=parseDecOctetSMTP(s,index,endOffset,
                  (index<endOffset) ? s[index] : '\0','.');
              if(decOctet>=100) {
                index+=4;
              } else if(decOctet>=10) {
                index+=3;
              } else if(decOctet>=0) {
                index+=2;
              } else return -1;
              decOctet=parseDecOctetSMTP(s,index,endOffset,
                  (index<endOffset) ? s[index] : '\0','.');
              if(decOctet>=100) {
                index+=4;
              } else if(decOctet>=10) {
                index+=3;
              } else if(decOctet>=0) {
                index+=2;
              } else return -1;
              decOctet=parseDecOctetSMTP(s,index,endOffset,
                  (index<endOffset) ? s[index] : '\0',']');
              if(decOctet>=100) {
                index+=3;
              } else if(decOctet>=10) {
                index+=2;
              } else if(decOctet>=0) {
                index+=1;
              } else return -1;
              break;
            }
          }
        }
        if(isHexChar(c) && !expectColon){
          if(phased){
            phase2++;
          } else {
            phase1++;
          }
          index++;
          for(int i=0;i<3;i++){
            if(index<endOffset && isHexChar(s[index])) {
              index++;
            } else {
              break;
            }
          }
          expectHex=false;
          expectColon=true;
        } else {
          break;
        }
      }
      //Console.WriteLine("%s %s %s | %s",phased,phase1,phase2,s);
      if((phase1+phase2)!=8 && !phased)
        return -1;
      if((phase1+2+phase2)>8 && phased)
        return -1;
      if(index>=endOffset)return -1;
      if(s[index]!=']')
        return -1;
      index++;
      return index;
    }
    int i2=skipSubdomain(s,index,endOffset,null,true);
    if(i2!=index){
      if(i2<endOffset && s[i2]==':'){
        i2=i2+1;
        // future extension
        bool haveString=false;
        while(i2<endOffset){
          char c=s[i2];
          if(c==']'){
            if(haveString)return i2+1;
            break;
          } else if((c>=33 && c<=90) || (c>=94 && c<=126)){
            haveString=true;
            i2++;
          } else {
            break;
          }
        }
      }
    }
    if(s[index]>='0' && s[index]<='9'){
      // IPv4 address
      char c=s[index];
      int decOctet=parseDecOctetSMTP(s,index,endOffset,c,'.');
      if(decOctet<0)return -1;
      index+=getDecOctetSMTPLength(s,index,endOffset,c,'.');
      decOctet=parseDecOctetSMTP(s,index,endOffset,
          (index<endOffset) ? s[index] : '\0','.');
      if(decOctet<0)return -1;
      index+=getDecOctetSMTPLength(s,index,endOffset,
          (index<endOffset) ? s[index] : '\0','.');
      decOctet=parseDecOctetSMTP(s,index,endOffset,
          (index<endOffset) ? s[index] : '\0','.');
      if(decOctet<0)return -1;
      index+=getDecOctetSMTPLength(s,index,endOffset,
          (index<endOffset) ? s[index] : '\0','.');
      decOctet=parseDecOctetSMTP(s,index,endOffset,
          (index<endOffset) ? s[index] : '\0',']');
      if(decOctet<0)return -1;
      index+=getDecOctetSMTPLength(s,index,endOffset,
          (index<endOffset) ? s[index] : '\0',']');
      return index;
    }
    return -1;
  }
  private static int parseMonth(string v, int index){
    if(com.upokecenter.util.StringUtility.startsWith(v,"Jan",index))return 1;
    if(com.upokecenter.util.StringUtility.startsWith(v,"Feb",index))return 2;
    if(com.upokecenter.util.StringUtility.startsWith(v,"Mar",index))return 3;
    if(com.upokecenter.util.StringUtility.startsWith(v,"Apr",index))return 4;
    if(com.upokecenter.util.StringUtility.startsWith(v,"May",index))return 5;
    if(com.upokecenter.util.StringUtility.startsWith(v,"Jun",index))return 6;
    if(com.upokecenter.util.StringUtility.startsWith(v,"Jul",index))return 7;
    if(com.upokecenter.util.StringUtility.startsWith(v,"Aug",index))return 8;
    if(com.upokecenter.util.StringUtility.startsWith(v,"Sep",index))return 9;
    if(com.upokecenter.util.StringUtility.startsWith(v,"Oct",index))return 10;
    if(com.upokecenter.util.StringUtility.startsWith(v,"Nov",index))return 11;
    if(com.upokecenter.util.StringUtility.startsWith(v,"Dec",index))return 12;
    return -1;
  }
  private static int parsePadded2Digit(string v, int index){
    int value=0;
    char c=(char)0;
    if(index<v.Length && v[index]==' '){
      value=0; index++;
    } else if(index<v.Length && (c=v[index])>='0' && c<='9'){
      value+=10*(c-'0'); index++;
    } else return -1;
    if(index<v.Length && (c=v[index])>='0' && c<='9'){
      value+=(c-'0'); index++;
    } else return -1;
    return value;
  }
  internal static int parseToken(string str, int index, string token, bool optionalQuoted){
    int length=str.Length;
    int j=0;
    int startIndex=index;
    for(int i=index;i<length && j<token.Length;i++,j++){
      char c=str[i];
      char cj=token[j];
      if(c!=cj && c!=(cj>='a' && cj<='z' ? cj-0x20 : cj))
        return startIndex;
    }
    index+=token.Length;
    index=skipLws(str,index,length,null);
    if(optionalQuoted){
      if(index<length && str[index]=='='){
        index++;
        index=skipLws(str,index,length,null);
        if(index<length && str[index]=='"'){
          index=skipQuotedString(str,index,length,null,
              QuotedStringRule.Http);
        } else return startIndex;
        index=skipLws(str,index,length,null);
      }
    }
    if(index>=length)return index;
    if(str[index]==','){
      index++;
      index=skipLws(str,index,length,null);
      return index;
    }
    return startIndex;
  }
  internal static int parseTokenWithDelta(string str, int index, string token, int[] result){
    int length=str.Length;
    int j=0;
    int startIndex=index;
    result[0]=-1;
    for(int i=index;i<length && j<token.Length;i++,j++){
      char c=str[i];
      char cj=token[j];
      if(c!=cj && c!=(cj>='a' && cj<='z' ? cj-0x20 : cj))
        return startIndex;
    }
    index+=token.Length;
    index=skipLws(str,index,length,null);
    if(index<length && str[index]=='='){
      index++;
      index=skipLws(str,index,length,null);
      int number=getPositiveNumber(str,index);
      while(index<length){
        char c=str[index];
        if(c<'0' || c>'9') {
          break;
        }
        index++;
      }
      result[0]=number;
      if(number<-1)
        return startIndex;
      index=skipLws(str,index,length,null);
    } else
      return startIndex;
    if(index>=length)return index;
    if(str[index]==','){
      index++;
      index=skipLws(str,index,length,null);
      return index;
    }
    return startIndex;
  }

  /* addr-spec (RFC5322 sec 3.41) */
  internal static int skipAddrSpec(
      string s,
      int index,
      int endIndex,
      StringBuilder builderLocal,
      StringBuilder builderDomain
      ){
    int startIndex=index;
    int bLength=(builderLocal==null) ? 0 : builderLocal.Length;
    int domLength=(builderDomain==null) ? 0 : builderDomain.Length;
    int i2=skipLocalPart(s,index,endIndex,builderLocal);
    if(i2==index)return startIndex;
    index=i2;
    if(index>=endIndex || s[index]!='@'){
      if(builderLocal!=null) {
        builderLocal.Length=(bLength);
      }
      if(builderDomain!=null) {
        builderDomain.Length=(domLength);
      }
      return startIndex;
    }
    index++;
    // NOTE: if builderDomain contains a domain literal,
    // the "\\" escapes characters not allowed in the
    // production "dtext" (except obs-dtext) under RFC5322
    i2=skipDomain(s,index,endIndex,builderDomain);
    if(i2==index){
      if(builderLocal!=null) {
        builderLocal.Length=(bLength);
      }
      if(builderDomain!=null) {
        builderDomain.Length=(domLength);
      }
      return startIndex;
    }
    return i2;
  }

  /* atom (RFC5322 sec. 3.2.3) */
  internal static int skipAtom(string s, int index,
      int endIndex, StringBuilder builder){
    int startIndex=index;
    index=skipCFWS(s,index,endIndex,null);
    bool haveAtom=false;
    while(index<endIndex){
      char c=s[index];
      if(isAtext(c)){
        if(builder!=null) {
          builder.Append(c);
        }
        index++;
        haveAtom=true;
      }else {
        if(!haveAtom)return startIndex;
        return skipCFWS(s,index,endIndex,null);
      }
    }
    return (haveAtom) ? index : startIndex;
  }

  /**
   * Skips comments and folding whitespace (CFWS) in a _string,
   * as specified in RFC5322 section 3.2.1.
   *
   * @param index the index into the beginning of the _string
   * for the purposes of this method.
   * @param endIndex the index into the end of the _string
   * for the purposes of this method.
   * @param the index where CFWS ends.  Will be the same
   * as _index_ if _index_ doesn't point to a comment or folding
   * whitespace.
   */
  internal static int skipCFWS(string s, int index, int endIndex){
    int retIndex=index;
    int startIndex=index;
    while(index<endIndex){
      index=skipFws(s,index,endIndex);
      #if DEBUG
if(!(index>=startIndex))throw new InvalidOperationException("doesn't satisfy index>=startIndex");
#endif
      retIndex=index;
      int oldIndex=index;
      index=skipComment(s,index,endIndex);
      #if DEBUG
if(!(index>=startIndex))throw new InvalidOperationException("doesn't satisfy index>=startIndex");
#endif
      if(index==oldIndex)return retIndex;
      retIndex=index;
    }
    return retIndex;
  }

  internal static int skipCFWS(string s, int index, int endIndex, StringBuilder builder){
    int ret=skipCFWS(s,index,endIndex);
    if(builder!=null && ret!=index){
      builder.Append(' ');
    }
    return ret;
  }



  /* comment (RFC5322 sec. 3.2.1) */
  internal static int skipComment(string s, int index, int endIndex){
    int startIndex=index;
    if(!(index<endIndex && s[index]=='('))
      return index;
    index++;
    while(index<endIndex){
      index=skipFws(s,index,endIndex);
      char c=s[index];
      if(c==')')return index+1;
      int oldIndex=index;
      index=skipCtextOrQuotedPairOrComment(s,index,endIndex);
      if(index==oldIndex)return startIndex;
    }
    return startIndex;
  }

  private static int skipCrLf(string s, int index, int endIndex){
    if(index+1<endIndex && s[index]==0x0d && s[index+1]==0x0a)
      return index+2;
    else
      return index;
  }

  /* ctext (RFC5322 sec. 3.2.1) */
  internal static int skipCtext(string s, int index, int endIndex){
    if(index<endIndex){
      char c=s[index];
      if(c>=33 && c<=126 && c!='(' && c!=')' && c!='\\')
        return index+1;
      // obs-ctext
      if((c<0x20 && c!=0x00 && c!=0x09 && c!=0x0a && c!=0x0d)  || c==0x7F)
        return index+2;
    }
    return index;
  }

  private static int skipCtextOrQuotedPairOrComment(string s, int index, int endIndex){
    if(index>=endIndex)return index;
    int i2;
    i2=skipCtext(s,index,endIndex);
    if(index!=i2)return i2;
    index=i2;
    i2=skipQuotedPair(s,index,endIndex);
    if(index!=i2)return i2;
    index=i2;
    i2=skipComment(s,index,endIndex);
    if(index!=i2)return i2;
    return i2;
  }
  internal static int skipDataUrlContentType(
      string str, int index, int endIndex, StringBuilder builder){
    if(str==null)return index;
    int startIndex=index;
    int oldpos=(builder==null) ? 0 : builder.Length;
    StringBuilder tmpbuilder=(builder==null) ? null : new StringBuilder();
    // Get the type
    int i2=skipEncodedMimeWord(str,index,endIndex,tmpbuilder,0);
    if(index!=i2){
      index=i2;
      if(index<endIndex && str[index]=='/'){
        index++;
        if(builder!=null){
          // append type to builder
          builder.Append(tmpbuilder.ToString());
          builder.Append('/');
          tmpbuilder.Remove(0,(tmpbuilder.Length)-(0));
        }
        // Get the subtype
        i2=skipEncodedMimeWord(str,index,endIndex,tmpbuilder,0);
        if(index!=i2){
          index=i2;
          if(builder!=null){
            // append subtype to builder
            builder.Append(tmpbuilder.ToString());
            tmpbuilder.Remove(0,(tmpbuilder.Length)-(0));
          }
          return skipDataUrlParameters(str,index,endIndex,builder,false);
        } else {
          // invalid media type
          if(builder!=null) {
            builder.Length=(oldpos);
          }
          return startIndex;
        }
      } else {
        // invalid media type
        if(builder!=null) {
          builder.Length=(oldpos);
        }
        return startIndex;
      }
    } else {
      // No media type, try checking if it really is blank
      if(index<endIndex && (str[index]==',' || str[index]==';'))
        // it's blank; assume text/plain
        return skipDataUrlParameters(str,index,endIndex,builder,true);
      else {
        if(builder!=null) {
          builder.Length=(oldpos);
        }
        return startIndex;
      }
    }
  }

  internal static int skipDataUrlContentType(
      string str, int index, StringBuilder builder){
    if(str==null)return index;
    return skipDataUrlContentType(str,index,str.Length,builder);
  }

  private static int skipDataUrlParameters(
      string str, int index, int endIndex, StringBuilder builder, bool plain){
    #if DEBUG
if(!(str!=null))throw new InvalidOperationException("doesn't satisfy str!=null");
#endif
    if(plain && builder!=null){
      builder.Append("text/plain");
    }
    StringBuilder tmpbuilder=(builder==null) ? null : new StringBuilder();
    int builderStartPos=(builder==null) ? 0 : builder.Length;
    int retval=-1;
    while(true){
      int oldindex=index;
      int builderOldPos=(builder==null) ? 0 : builder.Length;
      if(index>=endIndex){
        retval=oldindex;
        break;
      }
      char c=str[index];
      if(c!=';'){
        // reached end of content type
        if(builder!=null && builder.Length==0){
          // no content type given; provide default
          builder.Append("text/plain;charset=us-ascii");
        }
        retval=index;
        break;
      }
      index++;
      if(builder!=null) {
        builder.Append(';');
      }
      // get parameter name
      int index2=skipEncodedMimeWord(str,index,endIndex,tmpbuilder,1);
      if(index==index2){
        if(builder!=null) {
          builder.Remove(builderOldPos,(builder.Length)-(builderOldPos));
        }
        retval=oldindex;
        break;
      }
      if(builder!=null){
        // append parameter name to builder
        builder.Append(tmpbuilder.ToString());
        tmpbuilder.Remove(0,(tmpbuilder.Length)-(0));
      }
      index=index2;
      if(index>=endIndex || str[index]!='='){
        if(builder!=null) {
          builder.Remove(builderOldPos,(builder.Length)-(builderOldPos));
        }
        retval=oldindex;
        break;
      }
      index++;
      if(builder!=null) {
        builder.Append('=');
      }
      if(index>=endIndex){
        if(builder!=null) {
          builder.Remove(builderOldPos,(builder.Length)-(builderOldPos));
        }
        retval=oldindex;
        break;
      }
      // get parameter value
      index2=skipEncodedMimeWord(str,index,endIndex,tmpbuilder,2);
      if(index==index2){
        if(builder!=null) {
          builder.Remove(builderOldPos,(builder.Length)-(builderOldPos));
        }
        retval=oldindex;
        break;
      }
      if(builder!=null){
        // append parameter value to builder
        appendParameterValue(tmpbuilder.ToString(),builder);
        tmpbuilder.Remove(0,(tmpbuilder.Length)-(0));
      }
      index=index2;
    }
    if(plain && builder!=null && builder.Length==builderStartPos){
      // nothing, so append default charset
      builder.Append(";charset=us-ascii");
    }
    return retval;
  }

  private static int skipDigits(string v, int index){
    char c=(char)0;
    int length=v.Length;
    while(index<length){
      c=v[index];
      if(c<'0' || c>'9')return index;
      index++;
    }
    return index;
  }

  internal static int skipDirective(string str, int io){
    int length=str.Length;
    char c=(char)0;
    while(io<length){ // skip non-separator
      c=str[io];
      if(c=='=' || c==',' || c==127 || c<32) {
        break;
      }
      io++;
    }
    io=skipLws(str,io,length,null);
    if(io<length && str[io]=='='){
      io++;
      io=skipLws(str,io,length,null);
      if(io<length && str[io]=='"') {
        io=skipQuotedString(str,io,length,null,QuotedStringRule.Http);
      } else {
        while(io<length){ // skip non-separator
          c=str[io];
          if(c==',' || c==127 || c<32) {
            break;
          }
          io++;
        }
      }
      io=skipLws(str,io,length,null);
    }
    if(io<length && str[io]==','){
      io++;
      io=skipLws(str,io,length,null);
    } else {
      io=length;
    }
    return io;
  }

  /* domain (RFC5322 sec 3.4.1) */
  static int skipDomain(string s, int index, int endIndex, StringBuilder builder){
    int i2=skipDomainLiteral(s,index,endIndex,builder,true);
    if(i2!=index)return i2;
    // NOTE: obs-domain includes dot-atom
    i2=skipObsDomain(s,index,endIndex,builder);
    return i2;
  }

  /*
   */
  /* domain-literal (RFC5322 sec. 3.2.3) */
  internal static int skipDomainLiteral(string s, int index,
      int endIndex, StringBuilder builder, bool allowObsolete){
    int startIndex=index;
    int bLength=(builder==null) ? 0 : builder.Length;
    index=skipCFWS(s,index,endIndex,null);
    if(index>=endIndex || s[index]!='[')
      return startIndex;
    if(builder!=null) {
      builder.Append('[');
    }
    index++;
    while(index<endIndex){
      index=skipFws(s,index,endIndex);
      char c=s[index];
      if(c==']'){
        if(builder!=null) {
          builder.Append(']');
        }
        return skipCFWS(s,index,endIndex,null);
      }
      // dtext
      if((c>=33 && c<=90) ||
          (c>=94 && c<=126) ){
        if(builder!=null) {
          builder.Append(c);
        }
        index++;
        continue;
      }
      if(allowObsolete){
        // obs-dtext
        if(c=='\\'){
          int i2=skipQuotedPair(s,index,endIndex);
          if(index==i2){
            if(builder!=null) {
              builder.Length=(bLength);
            }
            return startIndex;
          }
          if(builder!=null) {
            c=s[i2-1]; // get quoted character
            // escape '[', ']', whitespace, control
            // characters, and '\'
            if(c<33 || c==127 || c=='[' || c==']' || c=='\\'){
              builder.Append('\\');
            }
            builder.Append(c);
          }
          index=i2;
          continue;
        } else if(c==127 || (c<0x20 && c!=9 && c!=10 && c!=13)){
          // control character other than whitespace
          if(builder!=null) {
            builder.Append('\\'); // would be escaped
            builder.Append(c);
          }
          index++;
          continue;
        }
      }
      // not a valid domain-literal
      break;
    }
    // not a valid domain-literal
    if(builder!=null) {
      builder.Length=(bLength);
    }
    return startIndex;
  }


  static int skipDomainSMTP(string s, int index, int endIndex, StringBuilder builder){
    int startIndex=index;
    int i2=skipSubdomain(s,index,endIndex,builder,false);
    if(i2==index)return startIndex;
    StringBuilder tmpbuilder=(builder==null) ? null : new StringBuilder();
    while(true){
      index=i2;
      if(index>=endIndex || s[index]!='.')
        return index;
      int i3=index+1;
      if(tmpbuilder!=null) {
        tmpbuilder.Clear();
      }
      i2=skipSubdomain(s,i3,endIndex,tmpbuilder,false);
      if(i2==i3)return index;
      if(builder!=null){
        builder.Append('.');
        builder.Append(tmpbuilder.ToString());
      }
    }
  }

  static int skipDotAtom(string s, int index,
      int endIndex, StringBuilder builder){
    return skipDotAtom(s,index,endIndex,builder,true);
  }

  /* dot-atom (RFC5322 sec. 3.2.3) */
  static int skipDotAtom(string s, int index,
      int endIndex, StringBuilder builder,bool withCFWS){
    int startIndex=index;
    index=(withCFWS) ? skipCFWS(s,index,endIndex,null) : index;
    bool haveAtom=false;
    bool haveDot=false;
    while(index<endIndex){
      char c=s[index];
      if(c=='.'){
        // in case of "x..y"
        if(haveDot){
          if(builder!=null) {
            builder.Length=(builder.Length-1);
          }
          return index-1; // index of previous dot
        }
        // in case of ".y"
        if(!haveAtom)return startIndex;
        if(builder!=null) {
          builder.Append(c);
        }
        haveDot=true;
        index++;
        continue;
      }
      if(isAtext(c)){
        if(builder!=null) {
          builder.Append(c);
        }
        index++;
        haveAtom=true;
        haveDot=false;
      } else {
        if(!haveAtom)return startIndex;
        if(haveDot){
          // move index to the dot
          if(builder!=null) {
            builder.Length=(builder.Length-1);
          }
          return index-1;
        }
        return (withCFWS) ? skipCFWS(s,index,endIndex,null) : index;
      }
    }
    if(haveDot && haveAtom){
      if(builder!=null) {
        builder.Length=(builder.Length-1);
      }
      index--;
    }
    return (haveAtom) ? index : startIndex;
  }

  internal static int skipEncodedMimeWord(
      string str, int index, int endIndex,
      StringBuilder builder, int kind
      ){
    int i=index;
    bool start=true;
    bool quoted=false;
    int startIndex=index;
    int count=0;
    while(i<endIndex){
      char c=str[i];
      // check for percent-encoded characters
      if(i+2<endIndex && c=='%' &&
          toHexNumber(str[i+1])>=0 &&
          toHexNumber(str[i+2])>=0){
        int c2=toHexNumber(str[i+1])*16+toHexNumber(str[i+2]);
        if(c2<=0x7F){ // this is an encoded ASCII character
          c=(char)c2;
          i+=2;
        }
      }
      if(start && c==0x22 && kind==2){ // if kind is parameter value
        // this is the start of a quoted _string
        i++;
        start=false;
        quoted=true;
        continue;
      }
      start=false;
      if(quoted){
        // quoted _string case
        if(c=='\\'){
          if(i+1>=endIndex)
            return startIndex;
          else {
            // get the next character of the
            // quoted pair
            i++;
            c=str[i];
            // check for percent-encoded characters
            if(i+2<endIndex && c=='%' &&
                toHexNumber(str[i+1])>=0 &&
                toHexNumber(str[i+2])>=0){
              int c2=toHexNumber(str[i+1])*16+toHexNumber(str[i+2]);
              if(c2<=0x7F){ // this is an encoded ASCII character
                c=(char)c2;
                i+=2;
              }
            }
            if(builder!=null) {
              builder.Append(c);
            }
          }
        } else if(c=='"')
          // end of quoted _string
          return i+1;
        else if(c==127 || (c<32 && c!='\t'))
          // ill-formed
          return startIndex;
        else {
          if(builder!=null) {
            builder.Append(c);
          }
        }
      } else {
        if(kind==1 || kind==2){ // kind is parameter name or parameter value
          // unquoted _string case
          if(c<=0x20 || c>=0x7F || ((c&0x7F)==c && "()<>@,;:\\\"/[]?=".IndexOf(c)>=0)) {
            break;
          }
          if(builder!=null) {
            builder.Append(c);
          }
        } else { // kind is 0, type or subtype
          // See RFC6838
          if((c>='A' && c<='Z') || (c>='a' && c<='z') || (c>='0' && c<='9')){
            if(builder!=null) {
              builder.Append(c);
            }
            count++;
          } else if(count>0 && ((c&0x7F)==c && "!#$&-^_.+".IndexOf(c)>=0)){
            if(builder!=null) {
              builder.Append(c);
            }
            count++;
          } else {
            break;
          }
          // type or subtype too long
          if(count>127)return startIndex;
        }
      }
      i++;
    }
    return i;
  }

  /* Folding white space (RFC5322 sec. 3.2.2) */
  internal static int skipFws(string s, int index, int endIndex){
    int startIndex=index;
    int i2=skipWsp(s,index,endIndex);
    int i2crlf=skipCrLf(s,i2,endIndex);
    if(i2crlf!=i2){// means a CRLF was seen
      int i3=skipWsp(s,i2crlf,endIndex);
      if(i3==i2crlf)
        return skipObsFws(s,startIndex,endIndex);
      else
        return Math.Max(i3,skipObsFws(s,startIndex,endIndex));
    } else
      return Math.Max(i2,skipObsFws(s,startIndex,endIndex));
  }


  /* Folding white space (RFC5322 sec. 3.2.2) */
  internal static int skipFws(string s, int index, int endIndex, StringBuilder builder){
    int ret=skipFws(s,index,endIndex);
    if(builder!=null && ret!=index){
      while(index<ret){
        // get the whitespace other than CR and LF
        char c=s[index];
        if(c!='\r' && c!='\n') {
          builder.Append(c);
        }
        index++;
      }
    }
    return ret;
  }
  private static int skipLanguageTag(string str, int index, int endIndex){
    if(index==endIndex || str==null)return index;
    char c=str[index];
    if(!((c>='A' && c<='Z') || (c>='a' && c<='z')))
      return index; // not a valid language tag
    index++;
    while(index<endIndex){
      c=str[index];
      if(!((c>='A' && c<='Z') || (c>='a' && c<='z') || (c>='0' && c<='9') || c=='-')){
        break;
      }
      index++;
    }
    return index;
  }

  /* local-part (RFC5322 sec 3.4.1) */
  static int skipLocalPart(string s, int index, int endIndex, StringBuilder builder){
    int i2=skipDotAtom(s,index,endIndex,builder);
    if(i2!=index)return i2;
    // NOTE: obs-local-part includes quoted-_string
    i2=skipObsLocalPart(s,index,endIndex,builder);
    return i2;
  }
  /* Local-part (RFC5321 sec 4.1.2) */
  static int skipLocalPartSMTP(string s, int index, int endIndex, StringBuilder builder){
    int i2=skipDotAtom(s,index,endIndex,builder,false);
    if(i2!=index)return i2;
    i2=skipQuotedString(s,index,endIndex,builder,QuotedStringRule.Smtp);
    return i2;
  }
  internal static int skipLws(string s, int index, int endIndex, StringBuilder builder){
    int ret;
    // While HTTP usually only allows CRLF, it also allows
    // us to be tolerant here
    int i2=skipNewLine(s,index,endIndex);
    ret=skipWsp(s,i2,endIndex);
    if(ret!=i2){
      if(builder!=null) {
        // Note that folding LWS into a space is
        // currently optional under HTTP/1.1 sec. 2.2
        builder.Append(' ');
      }
      return ret;
    }
    return index;
  }
  static int skipMailboxRfc5321(string s, int index, int endIndex, StringBuilder builder){
    int i2=index;
    int startIndex=index;
    int bLength=(builder==null) ? 0 : builder.Length;
    StringBuilder tmpbuilder=(builder==null) ? null : new StringBuilder();
    i2=skipLocalPartSMTP(s,index,endIndex,tmpbuilder);
    if(i2==index)
      return startIndex;
    if(i2-index>64)
      // local part too long
      return startIndex;
    if(i2>=endIndex || s[i2]!='@')
      // local part not followed by '@'
      return startIndex;
    i2++;
    index=i2;
    if(builder!=null){
      appendDotAtomOrStringSMTP(tmpbuilder.ToString(),builder);
      builder.Append('@');
    }
    i2=skipDomainSMTP(s,index,endIndex,builder);
    if(i2!=index){
      if(i2-index>255){
        // domain too long
        if(builder!=null) {
          builder.Length=(bLength);
        }
        return startIndex;
      }
      return i2;
    }
    int afterAt=i2;
    if(i2>=endIndex || s[i2]!='['){
      if(builder!=null) {
        builder.Length=(bLength);
      }
      return startIndex;
    }
    i2++;
    index=i2;
    i2=parseIPLiteralSMTP(s,index,endIndex);
    if(i2<0){
      if(builder!=null) {
        builder.Length=(bLength);
      }
      return startIndex;
    }
    if(i2-afterAt>255){
      // domain too long
      if(builder!=null) {
        builder.Length=(bLength);
      }
      return startIndex;
    }
    if(builder!=null){
      // append domain literal
      builder.Append('[');
      builder.Append(s.Substring(index,(i2)-(index)));
    }
    return i2;
  }
  internal static int skipMimeToken(string str, int index, int endIndex, 
     StringBuilder builder, bool httpRules){
    int i=index;
    while(i<endIndex){
      char c=str[i];
      if(c<=0x20 || c>=0x7F || ((c&0x7F)==c && "()<>@,;:\\\"/[]?=".IndexOf(c)>=0)) {
        break;
      }
      if(httpRules && (c=='{' || c=='}')){
        break;
      }
      if(builder!=null) {
        builder.Append(c);
      }
      i++;
    }
    return i;
  }
  private static int skipMimeTypeSubtype(string str, int index, int endIndex, StringBuilder builder){
    int i=index;
    int count=0;
    while(i<str.Length){
      char c=str[i];
      // See RFC6838
      if((c>='A' && c<='Z') || (c>='a' && c<='z') || (c>='0' && c<='9')){
        if(builder!=null) {
          builder.Append(c);
        }
        i++;
        count++;
      } else if(count>0 && ((c&0x7F)==c && "!#$&-^_.+".IndexOf(c)>=0)){
        if(builder!=null) {
          builder.Append(c);
        }
        i++;
        count++;
      } else {
        break;
      }
      // type or subtype too long
      if(count>127)return index;
    }
    return i;
  }
  private static int skipNewLine(string s, int index, int endIndex){
    if(index+1<endIndex && s[index]==0x0d && s[index+1]==0x0a)
      return index+2;
    else if(index<endIndex && (s[index]==0x0d || s[index]==0x0a))
      return index+1;
    else
      return index;
  }

  /* obs-domain (RFC5322 sec 4.4) */
  static int skipObsDomain(string s, int index, int endIndex, StringBuilder builder){
    int startIndex=index;
    int i2=skipAtom(s,index,endIndex,builder);
    if(i2==index)return startIndex;
    StringBuilder tmpbuilder=(builder==null) ? null : new StringBuilder();
    while(true){
      index=i2;
      if(index>=endIndex || s[index]!='.')
        return index;
      int i3=index+1;
      if(tmpbuilder!=null) {
        tmpbuilder.Clear();
      }
      i2=skipAtom(s,i3,endIndex,tmpbuilder);
      if(i2==i3)return index;
      if(builder!=null){
        builder.Append('.');
        builder.Append(tmpbuilder.ToString());
      }
    }
  }

  /* obs-fws under RFC5322, same as LWSP in RFC5234*/
  private static int skipObsFws(string s, int index, int endIndex){
    // parse obs-fws (according to errata)
    while(true){
      int i2=skipCrLf(s,index,endIndex);
      if(i2<endIndex && (s[i2]==0x20 || s[i2]==0x09)){
        index=i2+1;
      } else
        return index;
    }
  }
  /* obs-local-part (RFC5322 sec 4.4) */
  static int skipObsLocalPart(string s, int index, int endIndex, StringBuilder builder){
    int startIndex=index;
    int i2=skipWord(s,index,endIndex,builder);
    if(i2==index)return startIndex;
    StringBuilder tmpbuilder=(builder==null) ? null : new StringBuilder();
    while(true){
      index=i2;
      if(index>=endIndex || s[index]!='.')
        return index;
      int i3=index+1;
      if(tmpbuilder!=null) {
        tmpbuilder.Clear();
      }
      i2=skipWord(s,i3,endIndex,tmpbuilder);
      if(i2==i3)return index;
      if(builder!=null){
        builder.Append('.');
        builder.Append(tmpbuilder.ToString());
      }
    }
  }
  /* qtext (RFC5322 sec. 3.2.1) */
  internal static int skipQtext(string s, int index, int endIndex){
    if(index<endIndex){
      char c=s[index];
      if(c>=33 && c<=126 && c!='\\' && c!='"')
        return index+1;
      // obs-ctext
      if((c<0x20 && c!=0x00 && c!=0x09 && c!=0x0a && c!=0x0d)  || c==0x7F)
        return index+1;
    }
    return index;
  }

  private static void appendDotAtomOrStringSMTP(string s, StringBuilder b){
    if(b==null)return;
    int i2=skipDotAtom(s,0,s.Length,null);
    if(i2==s.Length){
      b.Append(s);
      return;
    }
    b.Append('"');
    int index=0;
    int endIndex=s.Length;
    if(index<endIndex){
      char c=s[index];
      if(c>=32 && c<=126 && c!='\\' && c!='"'){
        b.Append(c);
      } else {
        b.Append('\\');
        b.Append(c);
      }
      index++;
    }
    b.Append('"');
  }


  private static int skipQtextOrQuotedPair(
      string s, int index, int endIndex, QuotedStringRule rule){
    if(index>=endIndex)return index;
    int i2;
    if(rule==QuotedStringRule.Http){
      char c=s[index];
      if(c<0x100 && c>=0x21 && c!='\\' && c!='"')
        return index+1;
      i2=skipQuotedPair(s,index,endIndex);
      if(index!=i2)return i2;
      return i2;
    } else if(rule==QuotedStringRule.Rfc5322){
      i2=skipQtext(s,index,endIndex);
      if(index!=i2)return i2;
      index=i2;
      i2=skipQuotedPair(s,index,endIndex);
      if(index!=i2)return i2;
      return i2;
    } else if(rule==QuotedStringRule.Smtp){
      char c=s[index];
      if(c>=0x20 && c<=0x7E && c!='\\' && c!='"')
        return index+1;
      i2=skipQuotedPairSMTP(s,index,endIndex);
      if(index!=i2)return i2;
      return i2;
    } else
      throw new ArgumentException(rule.ToString());
  }

  /* quoted-pair (RFC5322 sec. 3.2.1) */
  internal static int skipQuotedPair(string s, int index, int endIndex){
    if(index+1<endIndex && s[index]=='\\'){
      char c=s[index+1];
      if(c==0x20 || c==0x09 || (c>=0x21 && c<=0x7e))
        return index+2;
      // obs-qp
      if((c<0x20 && c!=0x09)  || c==0x7F)
        return index+2;
    }
    return index;
  }

  internal static int skipQuotedPairSMTP(string s, int index, int endIndex){
    if(index+1<endIndex && s[index]=='\\'){
      char c=s[index+1];
      if((c>=0x20 && c<=0x7e))
        return index+2;
    }
    return index;
  }
  /* quoted-_string (RFC5322 sec. 3.2.4) */
  internal static int skipQuotedString(string s, int index,
      int endIndex, StringBuilder builder){
    return skipQuotedString(s,index,endIndex,builder,QuotedStringRule.Rfc5322);
  }


  internal static int skipQuotedString(
      string s,
      int index,
      int endIndex,
      StringBuilder builder, // receives the unescaped version of the _string
      QuotedStringRule rule // rule to follow for quoted _string
      ){
    int startIndex=index;
    int bLength=(builder==null) ? 0 : builder.Length;
    index=(rule!=QuotedStringRule.Rfc5322) ? index : skipCFWS(s,index,endIndex,null);
    if(!(index<endIndex && s[index]=='"')){
      if(builder!=null) {
        builder.Length=(bLength);
      }
      return startIndex; // not a valid quoted-_string
    }
    index++;
    while(index<endIndex){
      int i2=index;
      if(rule==QuotedStringRule.Http) {
        i2=skipLws(s,index,endIndex,builder);
      } else if(rule==QuotedStringRule.Rfc5322) {
        i2=skipFws(s,index,endIndex,builder);
      }
      index=i2;
      char c=s[index];
      if(c=='"'){ // end of quoted-_string
        index++;
        if(rule==QuotedStringRule.Rfc5322)
          return skipCFWS(s,index,endIndex,null);
        else
          return index;
      }
      int oldIndex=index;
      index=skipQtextOrQuotedPair(s,index,endIndex,rule);
      if(index==oldIndex){
        if(builder!=null) {
          builder.Remove(bLength,(builder.Length)-(bLength));
        }
        return startIndex;
      }
      if(builder!=null){
        // this is a qtext or quoted-pair, so
        // append the last character read
        builder.Append(s[index-1]);
      }
    }
    if(builder!=null) {
      builder.Remove(bLength,(builder.Length)-(bLength));
    }
    return startIndex; // not a valid quoted-_string
  }
  private static int skipSubdomain(
      string s, int index, int endIndex, StringBuilder builder,
      bool canBeginWithHyphen){
    if(index>=endIndex)return index;
    bool hyphen=false;
    bool haveString=false;
    while(index<endIndex){
      char c=s[index];
      if(c=='-'){
        hyphen=true;
        if(!haveString && !canBeginWithHyphen)return index;
        if(builder!=null) {
          builder.Append(c);
        }
        index++;
        haveString=true;
      } else if((c>='A' && c<='Z') || (c>='a' && c<='z') || (c>='0' && c<='9')){
        hyphen=false;
        if(builder!=null) {
          builder.Append(c);
        }
        index++;
        haveString=true;
      } else {
        break;
      }
    }
    if(hyphen){
      if(builder!=null) {
        builder.Length=(builder.Length-1);
      }
      index--;
    }
    return index;
  }
  /* word (RFC5322 sec 3.2.5) */
  static int skipWord(string s, int index,
      int endIndex, StringBuilder builder){
    int i2=skipAtom(s,index,endIndex,builder);
    if(i2!=index)return i2;
    i2=skipQuotedString(s,index,endIndex,builder,QuotedStringRule.Rfc5322);
    return i2;
  }
  /* skip space and tab characters */
  private static int skipWsp(string s, int index, int endIndex){
    while(index<endIndex){
      char c=s[index];
      if(c!=0x20 && c!=0x09)return index;
      index++;
    }
    return index;
  }
  private static int skipZeros(string v, int index){
    char c=(char)0;
    int length=v.Length;
    while(index<length){
      c=v[index];
      if(c!='0')return index;
      index++;
    }
    return index;
  }

  internal static int toHexNumber(int c) {
    if(c>='A' && c<='Z')
      return 10+c-'A';
    else if(c>='a' && c<='z')
      return 10+c-'a';
    else if (c>='0' && c<='9')
      return c-'0';
    return -1;
  }

  private HeaderParser(){}

}

}
