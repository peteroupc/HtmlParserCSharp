/*
If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/

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

using System;
using System.Collections.Generic;
using System.Text;
using PeterO;
using PeterO.Text;
using com.upokecenter.io;
using com.upokecenter.net;
using com.upokecenter.util;

namespace com.upokecenter.html {
  internal sealed class HtmlParser {
    internal class CommentToken : IToken {
      public StringBuilder value { get; set; }

      public CommentToken() {
        this.value = new StringBuilder();
      }

      public void Append(char ch) {
        this.value.Append(ch);
      }

      public void Append(string str) {
        this.value.Append(str);
      }

      public void appendChar(int ch) {
        if (ch <= 0xffff) {
          {
            this.value.Append((char)ch);
          }
        } else if (ch <= 0x10ffff) {
          this.value.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) + 0xd800));
          this.value.Append((char)(((ch - 0x10000) & 0x3ff) + 0xdc00));
        }
      }

      public int getType() {
        return TOKEN_COMMENT;
      }

      public string getValue() {
        return this.value.ToString();
      }
    }

    internal class DocTypeToken : IToken {
      public StringBuilder Name { get; set; }

      public StringBuilder ValuePublicID { get; set; }

      public StringBuilder ValueSystemID { get; set; }

      public bool ValueForceQuirks { get; set; }

      public int getType() {
        return TOKEN_DOCTYPE;
      }
    }

    internal class EndTagToken : TagToken {
      public EndTagToken(char c) : base(c) {
      }

      public EndTagToken(string name) : base(name) {
      }

      public override sealed int getType() {
        return HtmlParser.TOKEN_END_TAG;
      }
    }

    private class FormattingElement {
      public bool ValueMarker { get; set; }

      public IElement Element { get; set; }

      public StartTagToken Token { get; set; }

      public bool isMarker() {
        return this.ValueMarker;
      }

      public override sealed string ToString() {
        return "FormattingElement [this.ValueMarker=" + this.ValueMarker +
          ", this.valueToken=" + this.Token + "]\n";
      }
    }

    private enum InsertionMode {
      Initial,
      BeforeHtml,
      BeforeHead,
      InHead,
      InHeadNoscript,
      AfterHead,
      InBody,
      InTemplate,
      Text,
      InTable,
      InTableText,
      InCaption,
      InColumnGroup,
      InTableBody,
      InRow,
      InCell,
      InSelect,
      InSelectInTable,
      AfterBody,
      InFrameset,
      AfterFrameset,
      AfterAfterBody,
      AfterAfterFrameset
    }

    internal interface IToken {
      int getType();
    }

    internal class StartTagToken : TagToken {
      public StartTagToken(char c) : base(c) {
      }

      public StartTagToken(string name) : base(name) {
      }

      public override sealed int getType() {
        return HtmlParser.TOKEN_START_TAG;
      }

      public void setName(string _string) {
        builder.Clear();
        builder.Append(_string);
      }
    }

    internal abstract class TagToken : IToken, INameAndAttributes {
      protected StringBuilder builder;

      public IList<Attr> valueAttributes { get; set; }

      public bool valueSelfClosing { get; set; }

      public bool ValueSelfClosingAck { get; set; }

      public TagToken(char ch) {
        this.builder = new StringBuilder();
        this.builder.Append(ch);
        this.valueAttributes = null;
        this.valueSelfClosing = false;
        this.ValueSelfClosingAck = false;
      }

      public TagToken(string valueName) {
        this.builder = new StringBuilder();
        this.builder.Append(valueName);
      }

      public void ackSelfClosing() {
        this.ValueSelfClosingAck = true;
      }

      public Attr addAttribute(char ch) {
        this.valueAttributes = this.valueAttributes ?? (new List<Attr>());
        var a = new Attr(ch);
        this.valueAttributes.Add(a);
        return a;
      }

      public Attr addAttribute(int ch) {
        this.valueAttributes = this.valueAttributes ?? (new List<Attr>());
        var a = new Attr(ch);
        this.valueAttributes.Add(a);
        return a;
      }

      public void append(int ch) {
        if (ch < 0x10000) {
          this.builder.Append((char)ch);
        } else {
          ch -= 0x10000;
          int lead = (ch >> 10) + 0xd800;
          int trail = (ch & 0x3ff) + 0xdc00;
          this.builder.Append((char)lead);
          this.builder.Append((char)trail);
        }
      }

      public void appendChar(char ch) {
        this.builder.Append(ch);
      }

      public bool checkAttributeName() {
        if (this.valueAttributes == null) {
          return true;
        }
        int size = this.valueAttributes.Count;
        if (size >= 2) {
          string thisname = this.valueAttributes[size - 1].getName();
          for (int i = 0; i < size - 1; ++i) {
            if (this.valueAttributes[i].getName().Equals(thisname)) {
              // Attribute with this valueName already exists;
              // remove it
              this.valueAttributes.RemoveAt(size - 1);
              return false;
            }
          }
        }
        return true;
      }

      public string getAttribute(string valueName) {
        if (this.valueAttributes == null) {
          return null;
        }
        int size = this.valueAttributes.Count;
        for (int i = 0; i < size; ++i) {
          IAttr a = this.valueAttributes[i];
          string thisname = a.getName();
          if (thisname.Equals(valueName)) {
            return a.getValue();
          }
        }
        return null;
      }

      public string getAttributeNS(string valueName, string _namespace) {
        if (this.valueAttributes == null) {
          return null;
        }
        int size = this.valueAttributes.Count;
        for (int i = 0; i < size; ++i) {
          Attr a = this.valueAttributes[i];
          if (a.isAttribute(valueName, _namespace)) {
            return a.getValue();
          }
        }
        return null;
      }

      public IList<Attr> getAttributes() {
        if (this.valueAttributes == null) {
          return new Attr[0];
        } else {
          return this.valueAttributes;
        }
      }

      public string getName() {
        return this.builder.ToString();
      }

      public abstract int getType();

      public bool isAckSelfClosing() {
        return !this.valueSelfClosing || this.ValueSelfClosingAck;
      }

      public bool isSelfClosing() {
        return this.valueSelfClosing;
      }

      public bool isSelfClosingAck() {
        return this.ValueSelfClosingAck;
      }

      public void setAttribute(string attrname, string value) {
        if (this.valueAttributes == null) {
          this.valueAttributes = new List<Attr>();
          this.valueAttributes.Add(new Attr(attrname, value));
        } else {
          int size = this.valueAttributes.Count;
          for (int i = 0; i < size; ++i) {
            Attr a = this.valueAttributes[i];
            string thisname = a.getName();
            if (thisname.Equals(attrname)) {
              a.setValue(value);
              return;
            }
          }
          this.valueAttributes.Add(new Attr(attrname, value));
        }
      }

      public void setSelfClosing(bool valueSelfClosing) {
        this.valueSelfClosing = valueSelfClosing;
      }

      public override sealed string ToString() {
        return "TagToken [" + this.builder.ToString() + ", " +
    this.valueAttributes + (this.valueSelfClosing ?
              (", ValueSelfClosingAck=" +
              this.ValueSelfClosingAck) : String.Empty) + "]";
      }
    }

    private sealed class Html5Encoding : ICharacterEncoding {
      private ICharacterDecoder decoder;

      public Html5Encoding(EncodingConfidence ec) {
        ICharacterDecoder icd = ec == null ? null :
          Encodings.GetEncoding(ec.getEncoding()).GetDecoder();
        this.decoder = new Html5Decoder(icd);
      }

      public ICharacterEncoder GetEncoder() {
        throw new NotSupportedException();
      }

      public ICharacterDecoder GetDecoder() {
        return this.decoder;
      }
    }

    internal interface IInputStream {
      void rewind();

      void disableBuffer();
    }

    private enum TokenizerState {
      Data,
      CharacterRefInData,
      RcData,
      CharacterRefInRcData,
      RawText,
      ScriptData,
      PlainText,
      TagOpen,
      EndTagOpen,
      TagName,
      RcDataLessThan,
      RcDataEndTagOpen,
      RcDataEndTagName,
      RawTextLessThan,
      RawTextEndTagOpen,
      RawTextEndTagName,
      ScriptDataLessThan,
      ScriptDataEndTagOpen,
      ScriptDataEndTagName,
      ScriptDataEscapeStart,
      ScriptDataEscapeStartDash,
      ScriptDataEscaped,
      ScriptDataEscapedDash,
      ScriptDataEscapedDashDash,
      ScriptDataEscapedLessThan,
      ScriptDataEscapedEndTagOpen,
      ScriptDataEscapedEndTagName,
      ScriptDataDoubleEscapeStart,
      ScriptDataDoubleEscaped,
      ScriptDataDoubleEscapedDash,
      ScriptDataDoubleEscapedDashDash,
      ScriptDataDoubleEscapedLessThan,
      ScriptDataDoubleEscapeEnd,
      BeforeAttributeName,
      AttributeName,
      AfterAttributeName,
      BeforeAttributeValue,
      AttributeValueDoubleQuoted,
      AttributeValueSingleQuoted,
      AttributeValueUnquoted,
      CharacterRefInAttributeValue,
      AfterAttributeValueQuoted,
      SelfClosingStartTag,
      BogusComment,
      MarkupDeclarationOpen,
      CommentStart,
      CommentStartDash,
      Comment,
      CommentEndDash,
      CommentEnd,
      CommentEndBang,
      DocType,
      BeforeDocTypeName,
      DocTypeName,
      AfterDocTypeName,
      AfterDocTypePublic,
      BeforeDocTypePublicID,
      DocTypePublicIDDoubleQuoted,
      DocTypePublicIDSingleQuoted,
      AfterDocTypePublicID,
      BetweenDocTypePublicAndSystem,
      AfterDocTypeSystem,
      BeforeDocTypeSystemID,
      DocTypeSystemIDDoubleQuoted,
      DocTypeSystemIDSingleQuoted,
      AfterDocTypeSystemID,
      BogusDocType,
      CData
    }

    private const int TOKEN_EOF = 0x10000000;

    private const int TOKEN_START_TAG = 0x20000000;

    private const int TOKEN_END_TAG = 0x30000000;

    private const int TOKEN_COMMENT = 0x40000000;

    private const int TOKEN_DOCTYPE = 0x50000000;
    private const int TOKEN_TYPE_MASK = unchecked((int)0xf0000000);
    private const int TOKEN_CHARACTER = 0x00000000;
    private const int TOKEN_INDEX_MASK = 0x0fffffff;

    private static string[] quirksModePublicIdPrefixes = new string[] {
    "+//silmaril//dtd html pro v0r11 19970101//",
    "-//advasoft ltd//dtd html 3.0 aswedit + extensions//",
    "-//as//dtd html 3.0 aswedit + extensions//",
    "-//ietf//dtd html 2.0 level 1//",
    "-//ietf//dtd html 2.0 level 2//",
    "-//ietf//dtd html 2.0 strict level 1//",
    "-//ietf//dtd html 2.0 strict level 2//",
    "-//ietf//dtd html 2.0 strict//",
    "-//ietf//dtd html 2.0//",
    "-//ietf//dtd html 2.1e//",
    "-//ietf//dtd html 3.0//",
    "-//ietf//dtd html 3.2 final//",
    "-//ietf//dtd html 3.2//",
    "-//ietf//dtd html 3//",
    "-//ietf//dtd html level 0//",
    "-//ietf//dtd html level 1//",
    "-//ietf//dtd html level 2//",
    "-//ietf//dtd html level 3//",
    "-//ietf//dtd html strict level 0//",
    "-//ietf//dtd html strict level 1//",
    "-//ietf//dtd html strict level 2//",
    "-//ietf//dtd html strict level 3//",
    "-//ietf//dtd html strict//",
    "-//ietf//dtd html//",
    "-//metrius//dtd metrius presentational//",
    "-//microsoft//dtd internet explorer 2.0 html strict//",
    "-//microsoft//dtd internet explorer 2.0 html//",
    "-//microsoft//dtd internet explorer 2.0 tables//",
    "-//microsoft//dtd internet explorer 3.0 html strict//",
    "-//microsoft//dtd internet explorer 3.0 html//",
    "-//microsoft//dtd internet explorer 3.0 tables//",
    "-//netscape comm. corp.//dtd html//",
    "-//netscape comm. corp.//dtd strict html//",
    "-//o'reilly and associates//dtd html 2.0//",
    "-//o'reilly and associates//dtd html extended 1.0//",
    "-//o'reilly and associates//dtd html extended relaxed 1.0//",

  "-//softquad software//dtd hotmetal pro 6.0::19990601::extensions to html 4.0//",

    "-//softquad//dtd hotmetal pro 4.0::19971010::extensions to html 4.0//",
    "-//spyglass//dtd html 2.0 extended//",
    "-//sq//dtd html 2.0 hotmetal + extensions//",
    "-//sun microsystems corp.//dtd hotjava html//",
    "-//sun microsystems corp.//dtd hotjava strict html//",
    "-//w3c//dtd html 3 1995-03-24//",
    "-//w3c//dtd html 3.2 draft//",
    "-//w3c//dtd html 3.2 final//",
    "-//w3c//dtd html 3.2//",
    "-//w3c//dtd html 3.2s draft//",
    "-//w3c//dtd html 4.0 frameset//",
    "-//w3c//dtd html 4.0 transitional//",
    "-//w3c//dtd html experimental 19960712//",
    "-//w3c//dtd html experimental 970421//",
    "-//w3c//dtd w3 html//",
    "-//w3o//dtd w3 html 3.0//",
    "-//webtechs//dtd mozilla html 2.0//",
    "-//webtechs//dtd mozilla html//"
  };

    private ConditionalBufferInputStream inputStream;
    private IMarkableCharacterInput charInput = null;
    private EncodingConfidence encoding = null;

    private bool error = false;
    private TokenizerState lastState = TokenizerState.Data;
    private CommentToken lastComment;
    private DocTypeToken docTypeToken;
    private IList<IElement> integrationElements = new List<IElement>();
    private IList<IToken> tokens = new List<IToken>();
    private TagToken lastStartTag = null;
    private TagToken currentEndTag = null;
    private TagToken currentTag = null;
    private Attr currentAttribute = null;
    private int bogusCommentCharacter = 0;
    private StringBuilder tempBuilder = new StringBuilder();
    private TokenizerState state = TokenizerState.Data;
    private bool framesetOk = true;
    private IList<int> tokenQueue = new List<int>();
    private InsertionMode insertionMode = InsertionMode.Initial;
    private InsertionMode originalInsertionMode = InsertionMode.Initial;
    private IList<InsertionMode> templateModes = new List<InsertionMode>();
    private IList<IElement> openElements = new List<IElement>();
    private IList<FormattingElement> formattingElements = new
      List<FormattingElement>();

    private Element headElement = null;
    private Element formElement = null;
    private Element inputElement = null;
    private string baseurl = null;
    private bool hasForeignContent = false;
    private Document valueDocument = null;
    private bool done = false;

    private StringBuilder pendingTableCharacters = new StringBuilder();
    private bool doFosterParent;
    private IElement context;
    private bool noforeign;
    private string address;

    private string[] contentLanguage;

    private static T removeAtIndex<T>(IList<T> array, int index) {
      T ret = array[index];
      array.RemoveAt(index);
      return ret;
    }

    public HtmlParser(IReader s, string address) :
      this(s, address, null, null) {
    }

    public HtmlParser(
      IReader s,
      string address,
      string charset) : this(s, address, charset, null) {
    }

    public HtmlParser(
  IReader source,
  string address,
  string charset,
  string contentLanguage) {
      if (source == null) {
        throw new ArgumentException();
      }
      if (address != null && address.Length > 0) {
        URL url = URL.parse(address);
        if (url == null || url.getScheme().Length == 0) {
          throw new ArgumentException();
        }
      }
      // TODO: Use amore sophisticated language parser here
      this.contentLanguage = new string[] { contentLanguage };
      this.address = address;
      this.initialize();
      this.inputStream = new ConditionalBufferInputStream(source);  // TODO: ???
      this.encoding = new EncodingConfidence(
     charset,
     EncodingConfidence.Certain);
      // TODO: Use the following below
      // this.encoding = CharsetSniffer.sniffEncoding(this.inputStream,
      // charset);
      this.inputStream.rewind();
      ICharacterEncoding henc = new Html5Encoding(this.encoding);
      this.charInput = new StackableCharacterInput(
        Encodings.GetDecoderInput(henc, this.inputStream));
    }

    private void addCommentNodeToCurrentNode(int valueToken) {
      this.insertInCurrentNode(this.createCommentNode(valueToken));
    }

    private void addCommentNodeToDocument(int valueToken) {
      ((Document)this.valueDocument)
              .appendChild(this.createCommentNode(valueToken));
    }

    private void addCommentNodeToFirst(int valueToken) {
      ((Node)this.openElements[0])
              .appendChild(this.createCommentNode(valueToken));
    }

    private Element addHtmlElement(StartTagToken tag) {
      Element valueElement = Element.fromToken(tag);
      IElement currentNode = this.getCurrentNode();
      if (currentNode != null) {
        this.insertInCurrentNode(valueElement);
      } else {
        this.valueDocument.appendChild(valueElement);
      }
      this.openElements.Add(valueElement);
      return valueElement;
    }

    private Element addHtmlElementNoPush(StartTagToken tag) {
      Element valueElement = Element.fromToken(tag);
      IElement currentNode = this.getCurrentNode();
      if (currentNode != null) {
        this.insertInCurrentNode(valueElement);
      }
      return valueElement;
    }

    private void adjustForeignAttributes(StartTagToken valueToken) {
      IList<Attr> valueAttributes = valueToken.getAttributes();
      foreach (var attr in valueAttributes) {
        string valueName = attr.getName();
        if (valueName.Equals("xlink:actuate") || valueName.Equals(
        "xlink:arcrole") || valueName.Equals("xlink:href") || valueName.Equals(
  "xlink:role") || valueName.Equals("xlink:show") || valueName.Equals(
  "xlink:title") || valueName.Equals("xlink:type")) {
          attr.setNamespace(HtmlCommon.XLINK_NAMESPACE);
        } else if (valueName.Equals("xml:base") || valueName.Equals(
     "xml:lang") || valueName.Equals("xml:space")) {
          attr.setNamespace(HtmlCommon.XML_NAMESPACE);
        } else if (valueName.Equals("xmlns") || valueName.Equals(
     "xmlns:xlink")) {
          attr.setNamespace(HtmlCommon.XMLNS_NAMESPACE);
        }
      }
    }

private bool hasHtmlOpenElement(string name) {
  foreach (var e in this.openElements) {
    if (HtmlCommon.isHtmlElement(e, name)) {
 return true;
}
  }
  return false;
}

    private void adjustMathMLAttributes(StartTagToken valueToken) {
      IList<Attr> valueAttributes = valueToken.getAttributes();
      foreach (var attr in valueAttributes) {
        if (attr.getName().Equals("definitionurl")) {
          attr.setName("definitionURL");
        }
      }
    }

    private void adjustSvgAttributes(StartTagToken valueToken) {
      IList<Attr> valueAttributes = valueToken.getAttributes();
      foreach (var attr in valueAttributes) {
        string valueName = attr.getName();
        if (valueName.Equals("attributename")) {
          {
            attr.setName("attributeName");
          }
        } else if (valueName.Equals("attributetype")) {
          {
            attr.setName("attributeType");
          }
        } else if (valueName.Equals("basefrequency")) {
          {
            attr.setName("baseFrequency");
          }
        } else if (valueName.Equals("baseprofile")) {
          {
            attr.setName("baseProfile");
          }
        } else if (valueName.Equals("calcmode")) {
          {
            attr.setName("calcMode");
          }
        } else if (valueName.Equals("clippathunits")) {
          {
            attr.setName("clipPathUnits");
          }
        } else if (valueName.Equals("contentscripttype")) {
          attr.setName("contentScriptType");
        } else if (valueName.Equals("contentstyletype")) {
          {
            attr.setName("contentStyleType");
          }
        } else if (valueName.Equals("diffuseconstant")) {
          {
            attr.setName("diffuseConstant");
          }
        } else if (valueName.Equals("edgemode")) {
          {
            attr.setName("edgeMode");
          }
        } else if (valueName.Equals("externalresourcesrequired")) {
          attr.setName("externalResourcesRequired");
        } else if (valueName.Equals("filterres")) {
          {
            attr.setName("filterRes");
          }
        } else if (valueName.Equals("filterunits")) {
          {
            attr.setName("filterUnits");
          }
        } else if (valueName.Equals("glyphref")) {
          {
            attr.setName("glyphRef");
          }
        } else if (valueName.Equals("gradienttransform")) {
          attr.setName("gradientTransform");
        } else if (valueName.Equals("gradientunits")) {
          {
            attr.setName("gradientUnits");
          }
        } else if (valueName.Equals("kernelmatrix")) {
          {
            attr.setName("kernelMatrix");
          }
        } else if (valueName.Equals("kernelunitlength")) {
          {
            attr.setName("kernelUnitLength");
          }
        } else if (valueName.Equals("keypoints")) {
          {
            attr.setName("keyPoints");
          }
        } else if (valueName.Equals("keysplines")) {
          {
            attr.setName("keySplines");
          }
        } else if (valueName.Equals("keytimes")) {
          {
            attr.setName("keyTimes");
          }
        } else if (valueName.Equals("lengthadjust")) {
          {
            attr.setName("lengthAdjust");
          }
        } else if (valueName.Equals("limitingconeangle")) {
          attr.setName("limitingConeAngle");
        } else if (valueName.Equals("markerheight")) {
          {
            attr.setName("markerHeight");
          }
        } else if (valueName.Equals("markerunits")) {
          {
            attr.setName("markerUnits");
          }
        } else if (valueName.Equals("markerwidth")) {
          {
            attr.setName("markerWidth");
          }
        } else if (valueName.Equals("maskcontentunits")) {
          {
            attr.setName("maskContentUnits");
          }
        } else if (valueName.Equals("maskunits")) {
          {
            attr.setName("maskUnits");
          }
        } else if (valueName.Equals("numoctaves")) {
          {
            attr.setName("numOctaves");
          }
        } else if (valueName.Equals("pathlength")) {
          {
            attr.setName("pathLength");
          }
        } else if (valueName.Equals("patterncontentunits")) {
          attr.setName("patternContentUnits");
        } else if (valueName.Equals("patterntransform")) {
          {
            attr.setName("patternTransform");
          }
        } else if (valueName.Equals("patternunits")) {
          {
            attr.setName("patternUnits");
          }
        } else if (valueName.Equals("pointsatx")) {
          {
            attr.setName("pointsAtX");
          }
        } else if (valueName.Equals("pointsaty")) {
          {
            attr.setName("pointsAtY");
          }
        } else if (valueName.Equals("pointsatz")) {
          {
            attr.setName("pointsAtZ");
          }
        } else if (valueName.Equals("preservealpha")) {
          {
            attr.setName("preserveAlpha");
          }
        } else if (valueName.Equals("preserveaspectratio")) {
          attr.setName("preserveAspectRatio");
        } else if (valueName.Equals("primitiveunits")) {
          {
            attr.setName("primitiveUnits");
          }
        } else if (valueName.Equals("refx")) {
          {
            attr.setName("refX");
          }
        } else if (valueName.Equals("refy")) {
          {
            attr.setName("refY");
          }
        } else if (valueName.Equals("repeatcount")) {
          {
            attr.setName("repeatCount");
          }
        } else if (valueName.Equals("repeatdur")) {
          {
            attr.setName("repeatDur");
          }
        } else if (valueName.Equals("requiredextensions")) {
          attr.setName("requiredExtensions");
        } else if (valueName.Equals("requiredfeatures")) {
          {
            attr.setName("requiredFeatures");
          }
        } else if (valueName.Equals("specularconstant")) {
          {
            attr.setName("specularConstant");
          }
        } else if (valueName.Equals("specularexponent")) {
          {
            attr.setName("specularExponent");
          }
        } else if (valueName.Equals("spreadmethod")) {
          {
            attr.setName("spreadMethod");
          }
        } else if (valueName.Equals("startoffset")) {
          {
            attr.setName("startOffset");
          }
        } else if (valueName.Equals("stddeviation")) {
          {
            attr.setName("stdDeviation");
          }
        } else if (valueName.Equals("stitchtiles")) {
          {
            attr.setName("stitchTiles");
          }
        } else if (valueName.Equals("surfacescale")) {
          {
            attr.setName("surfaceScale");
          }
        } else if (valueName.Equals("systemlanguage")) {
          {
            attr.setName("systemLanguage");
          }
        } else if (valueName.Equals("tablevalues")) {
          {
            attr.setName("tableValues");
          }
        } else if (valueName.Equals("targetx")) {
          {
            attr.setName("targetX");
          }
        } else if (valueName.Equals("targety")) {
          {
            attr.setName("targetY");
          }
        } else if (valueName.Equals("textlength")) {
          {
            attr.setName("textLength");
          }
        } else if (valueName.Equals("viewbox")) {
          {
            attr.setName("viewBox");
          }
        } else if (valueName.Equals("viewtarget")) {
          {
            attr.setName("viewTarget");
          }
        } else if (valueName.Equals("xchannelselector")) {
          {
            attr.setName("xChannelSelector");
          }
        } else if (valueName.Equals("ychannelselector")) {
          {
            attr.setName("yChannelSelector");
          }
        } else if (valueName.Equals("zoomandpan")) {
          {
            attr.setName("zoomAndPan");
          }
        }
      }
    }

    private bool applyEndTag(string valueName, InsertionMode? insMode) {
      return this.applyInsertionMode(
  this.getArtificialToken(TOKEN_END_TAG, valueName),
  insMode);
    }

    private bool applyForeignContext(int valueToken) {
      if (valueToken == 0) {
        this.error = true;
        this.insertCharacter(this.getCurrentNode(), 0xfffd);
        return true;
      } else if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        this.insertCharacter(this.getCurrentNode(), valueToken);
        if (valueToken != 0x09 && valueToken != 0x0c && valueToken != 0x0a &&
            valueToken != 0x0d && valueToken != 0x20) {
          this.framesetOk = false;
        }
        return true;
      } else if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        this.addCommentNodeToCurrentNode(valueToken);
      } else if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        this.error = true;
        return false;
      } else if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        var tag = (StartTagToken)this.getToken(valueToken);
        string valueName = tag.getName();
        if (valueName.Equals("font")) {
          if (tag.getAttribute("color") != null || tag.getAttribute("size") !=
                   null || tag.getAttribute("face") != null) {
            this.error = true;
            while (true) {
              this.popCurrentNode();
              IElement node = this.getCurrentNode();
              if (node.getNamespaceURI().Equals(HtmlCommon.HTML_NAMESPACE) ||
                  this.isMathMLTextIntegrationPoint(node) ||
                  this.isHtmlIntegrationPoint(node)) {
                break;
              }
            }
            return this.applyInsertionMode(valueToken, null);
          }
        } else if (valueName.Equals("b") ||
            valueName.Equals("big") || valueName.Equals("blockquote") ||
              valueName.Equals("body") || valueName.Equals("br") ||
    valueName.Equals("center") || valueName.Equals("code") ||
              valueName.Equals(
    "dd") || valueName.Equals("div") ||
  valueName.Equals("dl") || valueName.Equals("dt") ||
              valueName.Equals("em") || valueName.Equals("embed") ||
  valueName.Equals("h1") || valueName.Equals("h2") ||
              valueName.Equals("h3") || valueName.Equals("h4") ||
valueName.Equals("h5") || valueName.Equals("h6") ||
              valueName.Equals("head") || valueName.Equals("hr") ||
  valueName.Equals("i") || valueName.Equals("img") ||
              valueName.Equals("li") || valueName.Equals("listing") ||
      valueName.Equals("menu") || valueName.Equals("meta") ||
              valueName.Equals(
    "nobr") || valueName.Equals("ol") ||
valueName.Equals("p") || valueName.Equals("pre") ||
              valueName.Equals("ruby") ||
              valueName.Equals("s") || valueName.Equals("small") ||
                valueName.Equals("span") ||
              valueName.Equals("strong") || valueName.Equals("strike") ||
        valueName.Equals("sub") || valueName.Equals("sup") ||
              valueName.Equals(
    "table") || valueName.Equals("tt") ||
  valueName.Equals("u") || valueName.Equals("ul") ||
              valueName.Equals("var")) {
          this.error = true;
          if (this.context != null && !this.hasNativeElementInScope()) {
            this.noforeign = true;
            bool ret = this.applyInsertionMode(
         valueToken,
         InsertionMode.InBody);
            this.noforeign = false;
            return ret;
          }
          while (true) {
            this.popCurrentNode();
            IElement node = this.getCurrentNode();
            if (node.getNamespaceURI().Equals(HtmlCommon.HTML_NAMESPACE) ||
                this.isMathMLTextIntegrationPoint(node) ||
                this.isHtmlIntegrationPoint(node)) {
              break;
            }
          }
          return this.applyInsertionMode(valueToken, null);
        } else {
          string _namespace = this.getCurrentNode().getNamespaceURI();
          var mathml = false;
          if (HtmlCommon.SVG_NAMESPACE.Equals(_namespace)) {
            if (valueName.Equals("altglyph")) {
              tag.setName("altGlyph");
            } else if (valueName.Equals("altglyphdef")) {
              tag.setName("altGlyphDef");
            } else if (valueName.Equals("altglyphitem")) {
              tag.setName("altGlyphItem");
            } else if (valueName.Equals("animatecolor")) {
              tag.setName("animateColor");
            } else if (valueName.Equals("animatemotion")) {
              tag.setName("animateMotion");
            } else if (valueName.Equals("animatetransform")) {
              tag.setName("animateTransform");
            } else if (valueName.Equals("clippath")) {
              tag.setName("clipPath");
            } else if (valueName.Equals("feblend")) {
              tag.setName("feBlend");
            } else if (valueName.Equals("fecolormatrix")) {
              tag.setName("feColorMatrix");
            } else if (valueName.Equals("fecomponenttransfer")) {
              tag.setName("feComponentTransfer");
            } else if (valueName.Equals("fecomposite")) {
              tag.setName("feComposite");
            } else if (valueName.Equals("feconvolvematrix")) {
              tag.setName("feConvolveMatrix");
            } else if (valueName.Equals("fediffuselighting")) {
              tag.setName("feDiffuseLighting");
            } else if (valueName.Equals("fedisplacementmap")) {
              tag.setName("feDisplacementMap");
            } else if (valueName.Equals("fedistantlight")) {
              tag.setName("feDistantLight");
            } else if (valueName.Equals("feflood")) {
              tag.setName("feFlood");
            } else if (valueName.Equals("fefunca")) {
              tag.setName("feFuncA");
            } else if (valueName.Equals("fefuncb")) {
              tag.setName("feFuncB");
            } else if (valueName.Equals("fefuncg")) {
              tag.setName("feFuncG");
            } else if (valueName.Equals("fefuncr")) {
              tag.setName("feFuncR");
            } else if (valueName.Equals("fegaussianblur")) {
              tag.setName("feGaussianBlur");
            } else if (valueName.Equals("feimage")) {
              tag.setName("feImage");
            } else if (valueName.Equals("femerge")) {
              tag.setName("feMerge");
            } else if (valueName.Equals("femergenode")) {
              tag.setName("feMergeNode");
            } else if (valueName.Equals("femorphology")) {
              tag.setName("feMorphology");
            } else if (valueName.Equals("feoffset")) {
              tag.setName("feOffset");
            } else if (valueName.Equals("fepointlight")) {
              tag.setName("fePointLight");
            } else if (valueName.Equals("fespecularlighting")) {
              tag.setName("feSpecularLighting");
            } else if (valueName.Equals("fespotlight")) {
              tag.setName("feSpotLight");
            } else if (valueName.Equals("fetile")) {
              tag.setName("feTile");
            } else if (valueName.Equals("feturbulence")) {
              tag.setName("feTurbulence");
            } else if (valueName.Equals("foreignobject")) {
              tag.setName("foreignObject");
            } else if (valueName.Equals("glyphref")) {
              tag.setName("glyphRef");
            } else if (valueName.Equals("lineargradient")) {
              tag.setName("linearGradient");
            } else if (valueName.Equals("radialgradient")) {
              tag.setName("radialGradient");
            } else if (valueName.Equals("textpath")) {
              tag.setName("textPath");
            }
            this.adjustSvgAttributes(tag);
          } else if (HtmlCommon.MATHML_NAMESPACE.Equals(_namespace)) {
            this.adjustMathMLAttributes(tag);
            mathml = true;
          }
          this.adjustForeignAttributes(tag);
          Element e = this.insertForeignElement(tag, _namespace);
          if (mathml && tag.getName().Equals("annotation-xml")) {
            string encoding = tag.getAttribute("encoding");
            if (encoding != null) {
              encoding = DataUtilities.ToLowerCaseAscii(encoding);
              if (encoding.Equals("text/html") ||
                  encoding.Equals("application/xhtml+xml")) {
                this.integrationElements.Add(e);
              }
            }
          }
          if (tag.isSelfClosing()) {
            if (valueName.Equals("script")) {
              tag.ackSelfClosing();
              this.applyEndTag("script", null);
            } else {
              this.popCurrentNode();
              tag.ackSelfClosing();
            }
          }
          return true;
        }
        return false;
      } else if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        var tag = (EndTagToken)this.getToken(valueToken);
        string valueName = tag.getName();
        if (valueName.Equals("script") &&
            this.getCurrentNode().getLocalName().Equals("script") &&
HtmlCommon.SVG_NAMESPACE.Equals(this.getCurrentNode().getNamespaceURI())) {
          this.popCurrentNode();
        } else {
          if
(!DataUtilities.ToLowerCaseAscii(this.getCurrentNode().getLocalName())
                    .Equals(valueName)) {
            this.error = true;
          }
          int originalSize = this.openElements.Count;
          for (int i1 = originalSize - 1; i1 >= 0; --i1) {
            if (i1 == 0) {
              return true;
            }
            IElement node = this.openElements[i1];
            if (i1 < originalSize - 1 &&
                HtmlCommon.HTML_NAMESPACE.Equals(node.getNamespaceURI())) {
              this.noforeign = true;
              return this.applyInsertionMode(valueToken, null);
            }
            string nodeName =
                 DataUtilities.ToLowerCaseAscii(node.getLocalName());
            if (valueName.Equals(nodeName)) {
              while (true) {
                IElement node2 = this.popCurrentNode();
                if (node2.Equals(node)) {
                  break;
                }
              }
              break;
            }
          }
        }
        return false;
      } else {
        return (valueToken == TOKEN_EOF) ?
          this.applyInsertionMode(valueToken, null) :
            true;
      }
      throw new InvalidOperationException();
    }

    private const string XhtmlStrict =
      "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd";

 private const string Xhtml11 = "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd";

    private bool applyInsertionMode(int token, InsertionMode? insMode) {
      DebugUtility.Log("[[" + token + " " + this.getToken(token) + " " +
          (insMode == null ? this.insertionMode :
         insMode) + " " + this.isForeignContext(token) + "(" +
         this.noforeign + ")");
      // if (this.openElements.Count>0) {
      // DebugUtility.Log(((Element)this.openElements[0]).toDebugString());
      // }
      if (!this.noforeign && this.isForeignContext(token)) {
        return this.applyForeignContext(token);
      }
      this.noforeign = false;
      insMode = insMode ?? this.insertionMode;
      switch (insMode) {
        case InsertionMode.Initial: {
            if (token == 0x09 || token == 0x0a ||
              token == 0x0c || token == 0x0d || token ==
                  0x20) {
              return false;
            }
            if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              var doctype = (DocTypeToken)this.getToken(token);
              string doctypeName = (doctype.Name == null) ? String.Empty :
                    doctype.Name.ToString();
              string doctypePublic = (doctype.ValuePublicID == null) ? null :
                doctype.ValuePublicID.ToString();
              string doctypeSystem = (doctype.ValueSystemID == null) ? null :
                doctype.ValueSystemID.ToString();
              bool matchesHtml = "html".Equals(doctypeName);
              bool hasSystemId = doctype.ValueSystemID != null;
              if (!matchesHtml || doctypePublic != null ||
              (doctypeSystem != null && !"about:legacy-compat"
                    .Equals(doctypeSystem))) {
                string h4public = "-//W3C//DTD HTML 4.0//EN";
                string html401public = "-//W3C//DTD HTML 4.01//EN";;
                string xhtmlstrictpublic = "-//W3C//DTD XHTML 1.0 Strict//EN";
                bool html4 = (matchesHtml && h4public.Equals(doctypePublic)&&
                  (doctypeSystem == null ||
                  "http://www.w3.org/TR/REC-html40/strict.dtd"
                    .Equals(doctypeSystem)));
                bool html401 = (matchesHtml &&
                  html401public.Equals(doctypePublic) && (doctypeSystem ==
                  null || "http://www.w3.org/TR/html4/strict.dtd"
                    .Equals(doctypeSystem)));
        bool xhtml = (matchesHtml && xhtmlstrictpublic.Equals(doctypePublic)&&
          (XhtmlStrict.Equals(doctypeSystem))); string xhtmlPublic =
                    "-//W3C//DTD XHTML 1.1//EN";
            bool xhtml11 = (matchesHtml && xhtmlPublic.Equals(doctypePublic)&&
              Xhtml11.Equals(doctypeSystem)); if (!html4 && !html401 &&
                !xhtml && !xhtml11) {
                  this.error = true;
                }
              }
              doctypePublic = doctypePublic ?? String.Empty;
              doctypeSystem = doctypeSystem ?? String.Empty;
              var doctypeNode = new DocumentType(
              doctypeName,
              doctypePublic,
              doctypeSystem);
              this.valueDocument.Doctype = doctypeNode;
              this.valueDocument.appendChild(doctypeNode);
              string doctypePublicLC = null;
              if (!"about:srcdoc".Equals(this.valueDocument.Address)) {
                if (!matchesHtml || doctype.ValueForceQuirks) {
                  this.valueDocument.setMode(DocumentMode.QuirksMode);
                } else {
                  doctypePublicLC =
                    DataUtilities.ToLowerCaseAscii(doctypePublic);
                  if ("html".Equals(doctypePublicLC) ||
              "-//w3o//dtd w3 html strict 3.0//en//"
                    .Equals(doctypePublicLC) ||
                    "-/w3c/dtd html 4.0 transitional/en".Equals(doctypePublicLC)
) {
                    this.valueDocument.setMode(DocumentMode.QuirksMode);
                  } else if (doctypePublic.Length > 0) {
                    foreach (var id in quirksModePublicIdPrefixes) {
                    if (
        doctypePublicLC.StartsWith(
        id,
        StringComparison.Ordinal)) {
                    this.valueDocument.setMode(DocumentMode.QuirksMode);
                    break;
                    }
                    }
                  }
                }
                if (this.valueDocument.getMode() != DocumentMode.QuirksMode) {
                  doctypePublicLC = doctypePublicLC ??
                    DataUtilities.ToLowerCaseAscii(doctypePublic);
                  if
     ("http://www.ibm.com/data/dtd/v11/ibmxhtml1-transitional.dtd"
                    .Equals(
                    DataUtilities.ToLowerCaseAscii(doctypeSystem)) ||
                (!hasSystemId && doctypePublicLC.StartsWith(
  "-//w3c//dtd html 4.01 frameset//",
  StringComparison.Ordinal)) || (!hasSystemId &&
  doctypePublicLC.StartsWith(
  "-//w3c//dtd html 4.01 transitional//",
  StringComparison.Ordinal))) {
                    this.valueDocument.setMode(DocumentMode.QuirksMode);
                  }
                }
                if (this.valueDocument.getMode() != DocumentMode.QuirksMode) {
                  doctypePublicLC = doctypePublicLC ??
                    DataUtilities.ToLowerCaseAscii(doctypePublic);
                  if (
    doctypePublicLC.StartsWith(
    "-//w3c//dtd xhtml 1.0 frameset//",
    StringComparison.Ordinal) ||
    doctypePublicLC.StartsWith(
    "-//w3c//dtd xhtml 1.0 transitional//",
    StringComparison.Ordinal) || (hasSystemId &&
                 doctypePublicLC.StartsWith(
    "-//w3c//dtd html 4.01 frameset//",
    StringComparison.Ordinal)) || (hasSystemId &&
    doctypePublicLC.StartsWith(
    "-//w3c//dtd html 4.01 transitional//",
    StringComparison.Ordinal))) {
                    this.valueDocument.setMode(DocumentMode.LimitedQuirksMode);
                  }
                }
              }
              this.insertionMode = InsertionMode.BeforeHtml;
              return true;
            }
            if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToDocument(token);

              return true;
            }
            if (!"about:srcdoc".Equals(this.valueDocument.Address)) {
              this.error = true;
              this.valueDocument.setMode(DocumentMode.QuirksMode);
            }
            this.insertionMode = InsertionMode.BeforeHtml;
            return this.applyInsertionMode(token, null);
          }
        case InsertionMode.BeforeHtml: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.error = true;
              return false;
            }
            if (token == 0x09 || token == 0x0a ||
              token == 0x0c || token == 0x0d || token ==
                  0x20) {
              return false;
            }
            if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToDocument(token);

              return true;
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if ("html".Equals(valueName)) {
                this.addHtmlElement(tag);
                this.insertionMode = InsertionMode.BeforeHead;
                return true;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (TagToken)this.getToken(token);
              string valueName = tag.getName();
              if (!"html".Equals(valueName) && !"br".Equals(valueName) &&
                  !"head".Equals(valueName) && !"body".Equals(valueName)) {
                this.error = true;
                return false;
              }
            }
            var valueElement = new Element();
            valueElement.setLocalName("html");
            valueElement.setNamespace(HtmlCommon.HTML_NAMESPACE);
            this.valueDocument.appendChild(valueElement);
            this.openElements.Add(valueElement);
            this.insertionMode = InsertionMode.BeforeHead;
            return this.applyInsertionMode(token, null);
          }
        case InsertionMode.BeforeHead: {
            if (token == 0x09 || token == 0x0a ||
              token == 0x0c || token == 0x0d || token ==
                  0x20) {
              return false;
            }
            if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToCurrentNode(token);
              return true;
            }
            if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.error = true;
              return false;
            }
            if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if ("html".Equals(valueName)) {
                this.applyInsertionMode(token, InsertionMode.InBody);
                return true;
              } else if ("head".Equals(valueName)) {
                Element valueElement = this.addHtmlElement(tag);
                this.headElement = valueElement;
                this.insertionMode = InsertionMode.InHead;
                return true;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (TagToken)this.getToken(token);
              string valueName = tag.getName();
              if ("head".Equals(valueName) || "br".Equals(valueName) ||
                  "body".Equals(valueName) || "html".Equals(valueName)) {
                this.applyStartTag("head", insMode);
                return this.applyInsertionMode(token, null);
              } else {
                this.error = true;
                return false;
              }
            }
            this.applyStartTag("head", insMode);
            return this.applyInsertionMode(token, null);
          }
        case InsertionMode.InHead: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToCurrentNode(token);
              return true;
            }
            if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.error = true;
              return false;
            }
            if (token == 0x09 || token == 0x0a ||
              token == 0x0c || token == 0x0d || token ==
                  0x20) {
              this.insertCharacter(this.getCurrentNode(), token);
              return true;
            }
            if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if ("html".Equals(valueName)) {
                this.applyInsertionMode(token, InsertionMode.InBody);
                return true;
              } else if ("base".Equals(valueName) ||
                  "bgsound".Equals(valueName) || "basefont".Equals(valueName) ||
                  "link".Equals(valueName)) {
                Element e = this.addHtmlElementNoPush(tag);
                if (this.baseurl == null && "base".Equals(valueName)) {
                  // Get the valueDocument _base URL
                  this.baseurl = e.getAttribute("href");
                }
                tag.ackSelfClosing();
                return true;
              } else if ("meta".Equals(valueName)) {
                Element valueElement = this.addHtmlElementNoPush(tag);
                tag.ackSelfClosing();
                if (this.encoding.getConfidence() ==
                    EncodingConfidence.Tentative) {
                  string charset = valueElement.getAttribute("charset");
                  if (charset != null) {
                    charset = Encodings.ResolveAlias(charset);
                    // if (TextEncoding.isAsciiCompatible(charset) ||
                    // "utf-16be" .Equals(charset) || "utf-16le"
                    // .Equals(charset)) {
                    this.changeEncoding(charset);
                    if (this.encoding.getConfidence() ==
                    EncodingConfidence.Certain) {
                    this.inputStream.disableBuffer();
                    }
                    return true;
                    // }
                  }
                  string value = DataUtilities.ToLowerCaseAscii(
                    valueElement.getAttribute("http-equiv"));
                  if ("content-type".Equals(value)) {
                    value = valueElement.getAttribute("content");
                    if (value != null) {
                    value = DataUtilities.ToLowerCaseAscii(value);
                    charset = CharsetSniffer.extractCharsetFromMeta(value);
                    if (true) {
                    // TODO
                    this.changeEncoding(charset);
                    if (this.encoding.getConfidence() ==
                    EncodingConfidence.Certain) {
                    this.inputStream.disableBuffer();
                    }
                    return true;
                    }
                    }
                  } else if ("content-language".Equals(value)) {
                    // HTML5 requires us to use this algorithm
                    // to parse the Content-Language, rather than
                    // use HTTP parsing (with HeaderParser.getLanguages)
                    // NOTE: this pragma is non-conforming
                    value = valueElement.getAttribute("content");
                    if (!String.IsNullOrEmpty(value) && value.IndexOf(',') <
                    0) {
                    string[] data = StringUtility.SplitAtSpTabCrLfFf(value);
                    string deflang = (data.Length == 0) ? String.Empty :
                    data[0];
                    if (!String.IsNullOrEmpty(deflang)) {
                    this.valueDocument.DefaultLanguage = deflang;
                    }
                    }
                  }
                }
             if (this.encoding.getConfidence() == EncodingConfidence.Certain) {
                  this.inputStream.disableBuffer();
                }
                return true;
              } else if ("title".Equals(valueName)) {
                this.addHtmlElement(tag);
                this.state = TokenizerState.RcData;
                this.originalInsertionMode = this.insertionMode;
                this.insertionMode = InsertionMode.Text;
                return true;
              } else if ("noframes".Equals(valueName) ||
                  "style".Equals(valueName)) {
                this.addHtmlElement(tag);
                this.state = TokenizerState.RawText;
                this.originalInsertionMode = this.insertionMode;
                this.insertionMode = InsertionMode.Text;
                return true;
              } else if ("noscript".Equals(valueName)) {
                this.addHtmlElement(tag);
                this.insertionMode = InsertionMode.InHeadNoscript;
                return true;
              } else if ("script".Equals(valueName)) {
                this.addHtmlElement(tag);
                this.state = TokenizerState.ScriptData;
                this.originalInsertionMode = this.insertionMode;
                this.insertionMode = InsertionMode.Text;
                return true;
} else if ("template".Equals(valueName)) {
Element e = this.addHtmlElement(tag);
this.insertFormattingMarker(tag, e);
this.framesetOk = false;
                this.insertionMode = InsertionMode.InTemplate;
this.templateModes.Add(InsertionMode.InTemplate);
return true;
                return true;
              } else if ("head".Equals(valueName)) {
                this.error = true;
                return false;
              } else {
                this.applyEndTag("head", insMode);
                return this.applyInsertionMode(token, null);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (TagToken)this.getToken(token);
              string valueName = tag.getName();
              if ("head".Equals(valueName)) {
                this.openElements.RemoveAt(this.openElements.Count - 1);
                this.insertionMode = InsertionMode.AfterHead;
                return true;
} else if ("template".Equals(valueName)) {
if (!this.hasHtmlOpenElement("template")) {
this.error = true;
return false;
}
this.generateImpliedEndTagsThoroughly();
IElement ie = this.getCurrentNode();
if (!HtmlCommon.isHtmlElement(ie, "template")) {
 this.error = true;
}
this.PopUntilHtmlElementPopped("template");
this.clearFormattingToMarker();
if (this.templateModes.Count > 0) {
 this.templateModes.RemoveAt(this.templateModes.Count - 1);
}
                  this.resetInsertionMode();
return true;
              } else if (!(
                  "br".Equals(valueName) ||
                  "body".Equals(valueName) || "html".Equals(valueName))) {
                this.error = true;
                return false;
              }
              this.applyEndTag("head", insMode);
              return this.applyInsertionMode(token, null);
            } else {
              this.applyEndTag("head", insMode);
              return this.applyInsertionMode(token, null);
            }
          }
        case InsertionMode.AfterHead: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              if (token == 0x20 || token == 0x09 || token == 0x0a ||
                    token == 0x0c || token == 0x0d) {
                this.insertCharacter(
     this.getCurrentNode(),
     token);
              } else {
                this.applyStartTag("body", insMode);
                this.framesetOk = true;
                return this.applyInsertionMode(token, null);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.error = true;
              return false;
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("html")) {
                this.applyInsertionMode(token, InsertionMode.InBody);
                return true;
              } else if (valueName.Equals("body")) {
                this.addHtmlElement(tag);
                this.framesetOk = false;
                this.insertionMode = InsertionMode.InBody;
                return true;
              } else if (valueName.Equals("frameset")) {
                this.addHtmlElement(tag);
                this.insertionMode = InsertionMode.InFrameset;
                return true;
              } else if ("base".Equals(valueName) || "bgsound"
                    .Equals(valueName) ||
                    "basefont".Equals(valueName) || "link".Equals(valueName) ||
                "noframes".Equals(valueName) || "script".Equals(valueName) ||
                    "style".Equals(valueName) || "title".Equals(valueName) ||
                    "meta".Equals(valueName)) {
                this.error = true;
                this.openElements.Add(this.headElement);
                this.applyInsertionMode(token, InsertionMode.InHead);
                this.openElements.Remove(this.headElement);
                return true;
              } else if ("head".Equals(valueName)) {
                this.error = true;
                return false;
              } else {
                this.applyStartTag("body", insMode);
                this.framesetOk = true;
                return this.applyInsertionMode(token, null);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("body") || valueName.Equals("html") ||
                  valueName.Equals("br")) {
                this.applyStartTag("body", insMode);
                this.framesetOk = true;
                return this.applyInsertionMode(token, null);
              } else {
                this.error = true;
                return false;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToCurrentNode(token);

              return true;
            } else if (token == TOKEN_EOF) {
              this.applyStartTag("body", insMode);
              this.framesetOk = true;
              return this.applyInsertionMode(token, null);
            }
            return true;
          }
        case InsertionMode.Text: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              if (insMode != this.insertionMode) {
                this.insertCharacter(this.getCurrentNode(), token);
              } else {
                Text textNode =
                    this.getTextNodeToInsert(this.getCurrentNode());
                int ch = token;
                if (textNode == null) {
                  throw new InvalidOperationException();
                }
                while (true) {
                  StringBuilder sb = textNode.ValueText;
                  if (ch <= 0xffff) {
                    {
                    sb.Append((char)ch);
                    }
                  } else if (ch <= 0x10ffff) {
                  sb.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) +
                    0xd800));
                    sb.Append((char)(((ch - 0x10000) & 0x3ff) + 0xdc00));
                  }
                  token = this.parserRead();
                  if ((token & TOKEN_TYPE_MASK) != TOKEN_CHARACTER) {
                    this.tokenQueue.Insert(0, token);
                    break;
                  }
                  ch = token;
                }
              }
              return true;
            } else if (token == TOKEN_EOF) {
              this.error = true;
              this.openElements.RemoveAt(this.openElements.Count - 1);
              this.insertionMode = this.originalInsertionMode;
              return this.applyInsertionMode(token, null);
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              this.openElements.RemoveAt(this.openElements.Count - 1);
              this.insertionMode = this.originalInsertionMode;
            }
            return true;
          }
case InsertionMode.InTemplate:{
  if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE ||
      (token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER ||
      (token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
                return this.applyInsertionMode(
         token,
         InsertionMode.InBody);
  }
  if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
if (valueName.Equals("base") ||
valueName.Equals("title") ||
valueName.Equals("template") ||
valueName.Equals("basefont") ||
valueName.Equals("bgsound") ||
valueName.Equals("meta") ||
valueName.Equals("link") ||
valueName.Equals("noframes") ||
valueName.Equals("style") ||
valueName.Equals("script")) {
                return this.applyInsertionMode(
         token,
         InsertionMode.InHead);
}
InsertionMode newMode = InsertionMode.InBody;
if (valueName.Equals("caption") ||
valueName.Equals("tbody") ||
valueName.Equals("thead") ||
valueName.Equals("tfoot") ||
valueName.Equals("colgroup")) {
  newMode = InsertionMode.InTable;
} else if (valueName.Equals("col")) {
  newMode = InsertionMode.InColumnGroup;
} else if (valueName.Equals("tr")) {
  newMode = InsertionMode.InTableBody;
} else if (valueName.Equals("td") || valueName.Equals("th")) {
  newMode = InsertionMode.InRow;
}

  if (this.templateModes.Count > 0) {
    this.templateModes.RemoveAt(this.templateModes.Count - 1);
  }
  this.templateModes.Add(newMode);
  this.insertionMode = newMode;
  return this.applyInsertionMode(token, null);
  }
  if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
if (valueName.Equals("template")) {
                return this.applyInsertionMode(
         token,
         InsertionMode.InHead);
} else {
this.error = true;
return true;
}
}
  if (token == TOKEN_EOF) {
if (!this.hasHtmlOpenElement("template")) {
 this.stopParsing();
 return true;
} else {
 this.error = true;
}
this.PopUntilHtmlElementPopped("trmplate");
this.clearFormattingToMarker();
  if (this.templateModes.Count > 0) {
    this.templateModes.RemoveAt(this.templateModes.Count - 1);
  }
this.resetInsertionMode();
  return this.applyInsertionMode(token, null);
  }
return false;
}

        case InsertionMode.InBody: {
            if (token == 0) {
              this.error = true;
              return true;
            }
            if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToCurrentNode(token);

              return true;
            }
            if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.error = true;
              return true;
            }
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
  DebugUtility.Log(String.Empty + ((char)token) + " " +
           (this.getCurrentNode().getTagName())); int ch = token;
              if (ch != 0) {
 this.reconstructFormatting();
}
              Text textNode = this.getTextNodeToInsert(this.getCurrentNode());
              if (textNode == null) {
                throw new InvalidOperationException();
              }
              while (true) {
                // Read multiple characters at once
                if (ch == 0) {
                  this.error = true;
                } else {
                  StringBuilder sb = textNode.ValueText;
                  if (ch <= 0xffff) {
                    {
                    sb.Append((char)ch);
                    }
                  } else if (ch <= 0x10ffff) {
                  sb.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) +
                    0xd800));
                    sb.Append((char)(((ch - 0x10000) & 0x3ff) + 0xdc00));
                  }
                }
                if (this.framesetOk && token != 0x20 && token != 0x09 &&
                    token != 0x0a && token != 0x0c && token != 0x0d) {
                  this.framesetOk = false;
                }
                // If we're only processing under a different
                // insertion mode then break
                if (insMode != this.insertionMode) {
                  break;
                }
                token = this.parserRead();
                if ((token & TOKEN_TYPE_MASK) != TOKEN_CHARACTER) {
                  this.tokenQueue.Insert(0, token);
                  break;
                }
                // DebugUtility.Log("{0} {1}"
                // , (char)token, getCurrentNode().getTagName());
                ch = token;
              }
              return true;
            } else if (token == TOKEN_EOF) {
              if (this.templateModes.Count > 0) {
return this.applyInsertionMode(token, InsertionMode.InTemplate);
            } else {
              foreach (var e in this.openElements) {
                if (!HtmlCommon.isHtmlElement(e, "dd") &&
      !HtmlCommon.isHtmlElement(e, "dt") && !HtmlCommon.isHtmlElement(e,"li"
) &&
                    !HtmlCommon.isHtmlElement(e, "option") &&
                    !HtmlCommon.isHtmlElement(e, "optgroup") &&
    !HtmlCommon.isHtmlElement(e, "p") && !HtmlCommon.isHtmlElement(e,"tbody"
) &&
   !HtmlCommon.isHtmlElement(e, "td") && !HtmlCommon.isHtmlElement(e,"tfoot"
) &&
      !HtmlCommon.isHtmlElement(e, "th") && !HtmlCommon.isHtmlElement(e,"tr"
) &&
 !HtmlCommon.isHtmlElement(e, "thead") && !HtmlCommon.isHtmlElement(e,"body"
) &&
                    !HtmlCommon.isHtmlElement(e, "html")) {
                  this.error = true;
                }
              }
              this.stopParsing();
              }
            }
            if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              // START TAGS
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if ("html".Equals(valueName)) {
                this.error = true;
                foreach (var e in this.openElements) {
                  string name2 = e.getLocalName();
                  if ("template".Equals(name2)) {
                    return false;  // ignore
                  }
                }
                ((Element)this.openElements[0]).mergeAttributes(tag);
                return true;
              } else if ("base".Equals(valueName) ||
                  "template".Equals(valueName) ||
                  "bgsound".Equals(valueName) || "basefont".Equals(valueName) ||
                  "link".Equals(valueName) || "noframes".Equals(valueName) ||
                  "script".Equals(valueName) || "style".Equals(valueName) ||
                  "title".Equals(valueName) || "meta".Equals(valueName)) {
                this.applyInsertionMode(token, InsertionMode.InHead);
                return true;
              } else if ("body".Equals(valueName)) {
                this.error = true;
                if (this.openElements.Count <= 1 ||
                    !HtmlCommon.isHtmlElement(this.openElements[1], "body")) {
                  return false;
                }
                this.framesetOk = false;
                ((Element)this.openElements[1]).mergeAttributes(tag);
                return true;
              } else if ("frameset".Equals(valueName)) {
                this.error = true;
                if (!this.framesetOk ||
                  this.openElements.Count <= 1 ||
                    !HtmlCommon.isHtmlElement(this.openElements[1], "body")) {
                  return false;
                }
                var parent = (Node)this.openElements[1].getParentNode();
                if (parent != null) {
                  parent.removeChild((Node)this.openElements[1]);
                }
                while (this.openElements.Count > 1) {
                  this.popCurrentNode();
                }
                this.addHtmlElement(tag);
                this.insertionMode = InsertionMode.InFrameset;
                return true;
              } else if ("address".Equals(valueName) ||
                  "article".Equals(valueName) || "aside".Equals(valueName) ||
                "blockquote".Equals(valueName) || "center"
                    .Equals(valueName) ||
                  "details".Equals(valueName) || "dialog".Equals(valueName) ||
                  "dir".Equals(valueName) || "div".Equals(valueName) ||
                  "dl".Equals(valueName) || "fieldset".Equals(valueName) ||
                "figcaption".Equals(valueName) || "figure"
                    .Equals(valueName) ||
                  "footer".Equals(valueName) || "header".Equals(valueName) ||
                  "main".Equals(valueName) ||
                  "nav".Equals(valueName) || "ol".Equals(valueName) ||
                  "p".Equals(valueName) || "section".Equals(valueName) ||
                  "summary".Equals(valueName) || "ul".Equals(valueName)
) {
                this.closeParagraph();
                this.addHtmlElement(tag);
                return true;
              } else if ("h1".Equals(valueName) || "h2".Equals(valueName) ||
                  "h3".Equals(valueName) || "h4".Equals(valueName) ||
                "h5".Equals(valueName) || "h6".Equals(valueName)) {
                this.closeParagraph();
                IElement node = this.getCurrentNode();
                string name1 = node.getLocalName();
                if ("h1".Equals(name1) || "h2".Equals(name1) ||
                    "h3".Equals(name1) || "h4".Equals(name1) ||
                "h5".Equals(name1) || "h6".Equals(name1)) {
                  this.error = true;
                  this.openElements.RemoveAt(this.openElements.Count - 1);
                }
                this.addHtmlElement(tag);
                return true;
              } else if ("pre".Equals(valueName) ||
                  "listing".Equals(valueName)) {
                this.closeParagraph();
                this.addHtmlElement(tag);
                this.skipLineFeed();
                this.framesetOk = false;
                return true;
              } else if ("form".Equals(valueName)) {
                if (this.formElement != null) {
                  this.error = true;
                  return true;
                }
                this.closeParagraph();
                this.formElement = this.addHtmlElement(tag);
                return true;
              } else if ("li".Equals(valueName)) {
                this.framesetOk = false;
                for (int i = this.openElements.Count - 1; i >= 0; --i) {
                  IElement node = this.openElements[i];
                  string nodeName = node.getLocalName();
                  if (nodeName.Equals("li")) {
                    this.applyInsertionMode(
                    this.getArtificialToken(TOKEN_END_TAG, "li"),
                    insMode);
                    break;
                  }
                  if (this.isSpecialElement(node) && !"address"
                    .Equals(nodeName) &&
                    !"div".Equals(nodeName) && !"p".Equals(nodeName)) {
                    break;
                  }
                }
                this.closeParagraph();
                this.addHtmlElement(tag);
                return true;
              } else if ("dd".Equals(valueName) || "dt".Equals(valueName)) {
                this.framesetOk = false;
                for (int i = this.openElements.Count - 1; i >= 0; --i) {
                  IElement node = this.openElements[i];
                  string nodeName = node.getLocalName();
                  // DebugUtility.Log("looping through %s",nodeName);
                  if (nodeName.Equals("dd") || nodeName.Equals("dt")) {
                    this.applyEndTag(nodeName, insMode);
                    break;
                  }
                  if (this.isSpecialElement(node) &&
                    !"address".Equals(nodeName) && !"div".Equals(nodeName) &&
                    !"p".Equals(nodeName)) {
                    break;
                  }
                }
                this.closeParagraph();
                this.addHtmlElement(tag);
                return true;
              } else if ("plaintext".Equals(valueName)) {
                this.closeParagraph();
                this.addHtmlElement(tag);
                this.state = TokenizerState.PlainText;
                return true;
              } else if ("button".Equals(valueName)) {
                if (this.hasHtmlElementInScope("button")) {
                  this.error = true;
                  this.applyEndTag("button", insMode);
                  return this.applyInsertionMode(token, null);
                }
                this.reconstructFormatting();
                this.addHtmlElement(tag);
                this.framesetOk = false;
                return true;
              } else if ("a".Equals(valueName)) {
                while (true) {
                  IElement node = null;
                  for (int i = this.formattingElements.Count - 1; i >= 0; --i) {
                    FormattingElement fe = this.formattingElements[i];
                    if (fe.isMarker()) {
                    break;
                    }
                    if (fe.Element.getLocalName().Equals("a")) {
                    node = fe.Element;
                    break;
                    }
                  }
                  if (node != null) {
                    this.error = true;
                    this.applyEndTag("a", insMode);
                    this.removeFormattingElement(node);
                    this.openElements.Remove(node);
                  } else {
                    break;
                  }
                }
                this.reconstructFormatting();
                this.pushFormattingElement(tag);
              } else if ("b".Equals(valueName) ||
                  "big".Equals(valueName) || "code".Equals(valueName) ||
                  "em".Equals(valueName) || "font".Equals(valueName) ||
                  "i".Equals(valueName) || "s".Equals(valueName) ||
                  "small".Equals(valueName) || "strike".Equals(valueName) ||
                  "strong".Equals(valueName) || "tt".Equals(valueName) ||
                  "u".Equals(valueName)) {
                this.reconstructFormatting();
                this.pushFormattingElement(tag);
              } else if ("nobr".Equals(valueName)) {
                this.reconstructFormatting();
                if (this.hasHtmlElementInScope("nobr")) {
                  this.error = true;
                  this.applyEndTag("nobr", insMode);
                  this.reconstructFormatting();
                }
                this.pushFormattingElement(tag);
              } else if ("table".Equals(valueName)) {
                if (this.valueDocument.getMode() != DocumentMode.QuirksMode) {
                  this.closeParagraph();
                }
                this.addHtmlElement(tag);
                this.framesetOk = false;
                this.insertionMode = InsertionMode.InTable;
                return true;
              } else if ("area".Equals(valueName) || "br".Equals(valueName) ||
                  "embed".Equals(valueName) || "img".Equals(valueName) ||
                  "keygen".Equals(valueName) || "wbr".Equals(valueName)
) {
                this.reconstructFormatting();
                this.addHtmlElementNoPush(tag);
                tag.ackSelfClosing();
                this.framesetOk = false;
              } else if ("input".Equals(valueName)) {
                this.reconstructFormatting();
                this.inputElement = this.addHtmlElementNoPush(tag);
                tag.ackSelfClosing();
                string attr = this.inputElement.getAttribute("type");
                if (attr == null || !"hidden"
                    .Equals(DataUtilities.ToLowerCaseAscii(attr))) {
                  this.framesetOk = false;
                }
              } else if ("param".Equals(valueName) || "source"
                    .Equals(valueName) ||
                    "menuitem".Equals(valueName) || "track".Equals(valueName)
) {
                this.addHtmlElementNoPush(tag);
                tag.ackSelfClosing();
              } else if ("hr".Equals(valueName)) {
                this.closeParagraph();
                this.addHtmlElementNoPush(tag);
                tag.ackSelfClosing();
                this.framesetOk = false;
              } else if ("image".Equals(valueName)) {
                this.error = true;
                tag.setName("img");
                return this.applyInsertionMode(token, null);
              /*} else if ("isindex".Equals(valueName)) {
                this.error = true;
                if (this.formElement != null) {
                  return false;
                }
                tag.ackSelfClosing();
                this.applyStartTag("form", insMode);
                string action = tag.getAttribute("action");
                if (action != null) {
                  this.formElement.setAttribute("action", action);
                }
                this.applyStartTag("hr", insMode);
                this.applyStartTag("label", insMode);
                var isindex = new StartTagToken("input");
                foreach (var attr in tag.getAttributes()) {
                  string attrname = attr.getName();
                  if (!"valueName".Equals(attrname) &&
                    !"action".Equals(attrname) && !"prompt".Equals(attrname)) {
                    isindex.setAttribute(attrname, attr.getValue());
                  }
                }
                string prompt = tag.getAttribute("prompt");
                // NOTE: Because of the inserted hr elements,
                // the frameset-ok flag should have been set
                // to not-ok already, so we don't need to check
                // for whitespace here
                if (prompt != null) {
                  this.reconstructFormatting();
                  this.insertString(this.getCurrentNode(), prompt);
                } else {
                  this.reconstructFormatting();
                  this.insertString(
                  this.getCurrentNode(),
                  "Enter search keywords:");
                }
                int isindexToken = this.tokens.Count | isindex.getType();
                this.tokens.Add(isindex);
                this.applyInsertionMode(isindexToken, insMode);
                this.inputElement.setAttribute("valueName", "isindex");
                this.applyEndTag("label", insMode);
                this.applyStartTag("hr", insMode);
                this.applyEndTag("form", insMode);*/
              } else if ("textarea".Equals(valueName)) {
                this.addHtmlElement(tag);
                this.skipLineFeed();
                this.state = TokenizerState.RcData;
                this.originalInsertionMode = this.insertionMode;
                this.framesetOk = false;
                this.insertionMode = InsertionMode.Text;
              } else if ("xmp".Equals(valueName)) {
                this.closeParagraph();
                this.reconstructFormatting();
                this.framesetOk = false;
                this.addHtmlElement(tag);
                this.state = TokenizerState.RawText;
                this.originalInsertionMode = this.insertionMode;
                this.insertionMode = InsertionMode.Text;
              } else if ("iframe".Equals(valueName)) {
                this.framesetOk = false;
                this.addHtmlElement(tag);
                this.state = TokenizerState.RawText;
                this.originalInsertionMode = this.insertionMode;
                this.insertionMode = InsertionMode.Text;
              } else if ("noembed".Equals(valueName)) {
                this.addHtmlElement(tag);
                this.state = TokenizerState.RawText;
                this.originalInsertionMode = this.insertionMode;
                this.insertionMode = InsertionMode.Text;
              } else if ("select".Equals(valueName)) {
                this.reconstructFormatting();
                this.addHtmlElement(tag);
                this.framesetOk = false;
           this.insertionMode = (this.insertionMode == InsertionMode.InTable ||
                this.insertionMode == InsertionMode.InCaption ||
                    this.insertionMode == InsertionMode.InTableBody ||
                    this.insertionMode == InsertionMode.InRow ||
                    this.insertionMode == InsertionMode.InCell) ?
                    InsertionMode.InSelectInTable : InsertionMode.InSelect;
              } else if ("option".Equals(valueName) || "optgroup"
                    .Equals(valueName)) {
                if (this.getCurrentNode().getLocalName().Equals(
    "option")) {
                  this.applyEndTag("option", insMode);
                }
                this.reconstructFormatting();
                this.addHtmlElement(tag);
              } else if ("rp".Equals(valueName) || "rt".Equals(valueName)) {
                if (this.hasHtmlElementInScope("ruby")) {
                  this.generateImpliedEndTags();
                  if (!this.getCurrentNode().getLocalName().Equals(
     "ruby")) {
                    this.error = true;
                  }
                }
                this.addHtmlElement(tag);
              } else if ("applet".Equals(valueName) || "marquee"
                    .Equals(valueName) || "object".Equals(valueName)) {
                this.reconstructFormatting();
                Element e = this.addHtmlElement(tag);
                this.insertFormattingMarker(tag, e);
                this.framesetOk = false;
              } else if ("math".Equals(valueName)) {
                this.reconstructFormatting();
                this.adjustMathMLAttributes(tag);
                this.adjustForeignAttributes(tag);
                this.insertForeignElement(
    tag,
    HtmlCommon.MATHML_NAMESPACE);
                if (tag.isSelfClosing()) {
                  tag.ackSelfClosing();
                  this.popCurrentNode();
                } else {
                  this.hasForeignContent = true;
                }
              } else if ("svg".Equals(valueName)) {
                this.reconstructFormatting();
                this.adjustSvgAttributes(tag);
                this.adjustForeignAttributes(tag);
                this.insertForeignElement(tag, HtmlCommon.SVG_NAMESPACE);
                if (tag.isSelfClosing()) {
                  tag.ackSelfClosing();
                  this.popCurrentNode();
                } else {
                  this.hasForeignContent = true;
                }
              } else if ("caption".Equals(valueName) ||
                  "col".Equals(valueName) || "colgroup".Equals(valueName) ||
                  "frame".Equals(valueName) || "head".Equals(valueName) ||
                  "tbody".Equals(valueName) || "td".Equals(valueName) ||
                  "tfoot".Equals(valueName) || "th".Equals(valueName) ||
                  "thead".Equals(valueName) || "tr".Equals(valueName)
) {
                this.error = true;
                return false;
              } else {
                // DebugUtility.Log("ordinary: %s",tag);
                this.reconstructFormatting();
                this.addHtmlElement(tag);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              // END TAGS
              // NOTE: Have all cases
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("template")) {
               this.applyInsertionMode(token, InsertionMode.InHead);
               return true;
              }
              if (valueName.Equals("body")) {
                if (!this.hasHtmlElementInScope("body")) {
                  this.error = true;
                  return false;
                }
                foreach (var e in this.openElements) {
                  string name2 = e.getLocalName();
                  if (!"dd".Equals(name2) && !"dt".Equals(name2) &&
                    !"li".Equals(name2) && !"option".Equals(name2) &&
                    !"optgroup".Equals(name2) && !"p".Equals(name2) &&
                    !"rb".Equals(name2) && !"tbody".Equals(name2) &&
                    !"td".Equals(name2) && !"tfoot".Equals(name2) &&
                    !"th".Equals(name2) && !"tr".Equals(name2) &&
                    !"thead".Equals(name2) && !"body".Equals(name2) &&
                    !"html".Equals(name2)) {
                    this.error = true;
                    // token not ignored here
                  }
                }
                this.insertionMode = InsertionMode.AfterBody;
              } else if (valueName.Equals("a") ||
                  valueName.Equals("b") || valueName.Equals("big") ||
                  valueName.Equals("code") || valueName.Equals("em") ||
                  valueName.Equals("b") || valueName.Equals("font") ||
                  valueName.Equals("i") || valueName.Equals("nobr") ||
                  valueName.Equals("s") || valueName.Equals("small") ||
                  valueName.Equals("strike") || valueName.Equals("strong") ||
                valueName.Equals("tt") || valueName.Equals("u")) {
                for (int i = 0; i < 8; ++i) {
                  FormattingElement formatting = null;
                  for (int j = this.formattingElements.Count - 1; j >= 0; --j) {
                    FormattingElement fe = this.formattingElements[j];
                    if (fe.isMarker()) {
                    break;
                    }
                    if (fe.Element.getLocalName().Equals(valueName)) {
                    formatting = fe;
                    break;
                    }
                  }
                  if (formatting == null) {
                    // NOTE: Steps for "any other end tag"
                    // DebugUtility.Log("no such formatting valueElement");
                    for (int i1 = this.openElements.Count - 1; i1 >= 0; --i1) {
                    IElement node = this.openElements[i1];
                    if (valueName.Equals(node.getLocalName())) {
                    this.generateImpliedEndTagsExcept(valueName);
                if (!valueName.Equals(this.getCurrentNode().getLocalName())) {
                    this.error = true;
                    }
                    while (true) {
                    IElement node2 = this.popCurrentNode();
                    if (node2.Equals(node)) {
                    break;
                    }
                    }
                    break;
                    } else if (this.isSpecialElement(node)) {
                    this.error = true;
                    return false;
                    }
                    }
                    break;
                  }
                  int formattingElementPos =
                    this.openElements.IndexOf(formatting.Element);
                  if (formattingElementPos < 0) {  // not found
                    this.error = true;
                    // DebugUtility.Log("Not in stack of open elements");
                    this.formattingElements.Remove(formatting);
                    break;
                  }
                  // DebugUtility.Log("Open elements[%s]:",i);
                  // DebugUtility.Log(openElements);
                  // DebugUtility.Log("Formatting elements:");
                  // DebugUtility.Log(formattingElements);
                  if
     (!this.hasHtmlElementInScope(formatting.Element)) {
                    this.error = true;
                    return false;
                  }
                  if
       (!formatting.Element.Equals(this.getCurrentNode())) {
                    this.error = true;
                  }
                  IElement furthestBlock = null;
                  var furthestBlockPos = -1;
                  for (int j = this.openElements.Count - 1; j >
                    formattingElementPos;
                    --j) {
                    IElement e = this.openElements[j];
                    if (this.isSpecialElement(e)) {
                    furthestBlock = e;
                    furthestBlockPos = j;
                    }
                  }
                  // DebugUtility.Log("furthest block: %s",furthestBlock);
                  if (furthestBlock == null) {
                    // Pop up to and including the
                    // formatting valueElement
                    while (this.openElements.Count > formattingElementPos) {
                    this.popCurrentNode();
                    }
                    this.formattingElements.Remove(formatting);
                    // DebugUtility.Log("Open elements now [%s]:",i);
                    // DebugUtility.Log(openElements);
                    // DebugUtility.Log("Formatting elements now:");
                    // DebugUtility.Log(formattingElements);
                    break;
                  }
             IElement commonAncestor = this.openElements[formattingElementPos -
                    1];
                  // DebugUtility.Log("common ancestor: %s",commonAncestor);
                  int bookmark = this.formattingElements.IndexOf(formatting);
                  // DebugUtility.Log("bookmark=%d",bookmark);
                  IElement myNode = furthestBlock;
                  IElement superiorNode = this.openElements[furthestBlockPos -
                    1];
                  IElement lastNode = furthestBlock;
                  for (int j = 0; j < 3; ++j) {
                    myNode = superiorNode;
                FormattingElement nodeFE = this.getFormattingElement(myNode);
                    if (nodeFE == null) {
                    // DebugUtility.Log("node not a formatting valueElement");
           superiorNode = this.openElements[this.openElements.IndexOf(myNode) -
                    1];
                    this.openElements.Remove(myNode);
                    continue;
                    } else if (myNode.Equals(formatting.Element)) {
                    // DebugUtility.Log("node is the formatting valueElement");
                    break;
                    }
                    IElement e = Element.fromToken(nodeFE.Token);
                    nodeFE.Element = e;
                    int io = this.openElements.IndexOf(myNode);
                    superiorNode = this.openElements[io - 1];
                    this.openElements[io] = e;
                    myNode = e;
                    if (lastNode.Equals(furthestBlock)) {
                    bookmark = this.formattingElements.IndexOf(nodeFE) + 1;
                    }
                    // NOTE: Because 'node' can only be a formatting
                    // element, the foster parenting rule doesn't
                    // apply here
                    if (lastNode.getParentNode() != null) {
((Node)lastNode.getParentNode()).removeChild((Node)lastNode);
                    }
                    myNode.appendChild(lastNode);
                    lastNode = myNode;
                  }
                  // DebugUtility.Log("node: %s",node);
                  // DebugUtility.Log("lastNode: %s",lastNode);
                  if (commonAncestor.getLocalName().Equals("table") ||
                    commonAncestor.getLocalName().Equals("tr") ||
                    commonAncestor.getLocalName().Equals("tbody") ||
                    commonAncestor.getLocalName().Equals("thead") ||
                    commonAncestor.getLocalName().Equals("tfoot")
) {
                    if (lastNode.getParentNode() != null) {
((Node)lastNode.getParentNode()).removeChild((Node)lastNode);
                    }
                    this.fosterParent(lastNode);
                  } else {
                    if (lastNode.getParentNode() != null) {
((Node)lastNode.getParentNode()).removeChild((Node)lastNode);
                    }
                    commonAncestor.appendChild(lastNode);
                  }
                  Element e2 = Element.fromToken(formatting.Token);
                  foreach (var child in new
                    List<INode>(furthestBlock.getChildNodes())) {
                    furthestBlock.removeChild((Node)child);
                    // NOTE: Because 'e' can only be a formatting
                    // valueElement, the foster parenting rule doesn't
                    // apply here
                    e2.appendChild(child);
                  }
                  // NOTE: Because intervening elements, including
                  // formatting elements, are cleared between table
                  // and tbody/thead/tfoot and between those three
                  // elements and tr, the foster parenting rule
                  // doesn't apply here
                  furthestBlock.appendChild(e2);
                  var newFE = new FormattingElement();
                  newFE.ValueMarker = false;
                  newFE.Element = e2;
                  newFE.Token = formatting.Token;
                  // DebugUtility.Log("Adding formatting valueElement at %d"
                  // , bookmark);
                  this.formattingElements.Insert(bookmark, newFE);
                  this.formattingElements.Remove(formatting);
                  // DebugUtility.Log("Replacing open valueElement at %d"
                  // , openElements.IndexOf(furthestBlock)+1);
                  int idx = this.openElements.IndexOf(furthestBlock) + 1;
                  this.openElements.Insert(idx, e2);
                  this.openElements.Remove(formatting.Element);
                }
              } else if ("applet".Equals(valueName) ||
                  "marquee".Equals(valueName) || "object".Equals(valueName)) {
                if (!this.hasHtmlElementInScope(valueName)) {
                  this.error = true;
                  return false;
                } else {
                  this.generateImpliedEndTags();
                  if (!this.getCurrentNode().getLocalName().Equals(valueName)) {
                    this.error = true;
                  }
                  while (true) {
                    IElement node = this.popCurrentNode();
                    if (node.getLocalName().Equals(valueName)) {
                    break;
                    }
                  }
                  this.clearFormattingToMarker();
                }
              } else if (valueName.Equals("html")) {
                return this.applyEndTag("body", insMode) ?
                  this.applyInsertionMode(token, null) : false;
              } else if ("address".Equals(valueName) ||
                  "article".Equals(valueName) || "aside".Equals(valueName) ||
                "blockquote".Equals(valueName) || "button"
                    .Equals(valueName) ||
                  "center".Equals(valueName) || "details".Equals(valueName) ||
                  "dialog".Equals(valueName) || "dir".Equals(valueName) ||
                  "div".Equals(valueName) || "dl".Equals(valueName) ||
              "fieldset".Equals(valueName) || "figcaption"
                    .Equals(valueName) ||
                  "figure".Equals(valueName) || "footer".Equals(valueName) ||
                  "header".Equals(valueName) ||
                  "listing".Equals(valueName) || "main".Equals(valueName) ||
                  "nav".Equals(valueName) ||
                  "ol".Equals(valueName) || "pre".Equals(valueName) ||
                  "section".Equals(valueName) || "summary".Equals(valueName) ||
                  "ul".Equals(valueName)) {
                if (!this.hasHtmlElementInScope(valueName)) {
                  this.error = true;
                  return true;
                } else {
                  this.generateImpliedEndTags();
                  if (!this.getCurrentNode().getLocalName().Equals(valueName)) {
                    this.error = true;
                  }
                  while (true) {
                    IElement node = this.popCurrentNode();
                    if (node.getLocalName().Equals(valueName)) {
                    break;
                    }
                  }
                }
              } else if (valueName.Equals("form")) {
                IElement node = this.formElement;
                this.formElement = null;
                if (node == null || this.hasHtmlElementInScope(node)) {
                  this.error = true;
                  return true;
                }
                this.generateImpliedEndTags();
                if (this.getCurrentNode() != node) {
                  this.error = true;
                }
                this.openElements.Remove(node);
              } else if (valueName.Equals("p")) {
                if (!this.hasHtmlElementInButtonScope(valueName)) {
                  this.error = true;
                  this.applyStartTag("p", insMode);
                  return this.applyInsertionMode(token, null);
                }
                this.generateImpliedEndTagsExcept(valueName);
                if (!this.getCurrentNode().getLocalName().Equals(valueName)) {
                  this.error = true;
                }
                while (true) {
                  IElement node = this.popCurrentNode();
                  if (node.getLocalName().Equals(valueName)) {
                    break;
                  }
                }
              } else if (valueName.Equals("li")) {
                if (!this.hasHtmlElementInListItemScope(valueName)) {
                  this.error = true;
                  return false;
                }
                this.generateImpliedEndTagsExcept(valueName);
                if (!this.getCurrentNode().getLocalName().Equals(valueName)) {
                  this.error = true;
                }
                while (true) {
                  IElement node = this.popCurrentNode();
                  if (node.getLocalName().Equals(valueName)) {
                    break;
                  }
                }
              } else if (valueName.Equals("h1") || valueName.Equals("h2") ||
                  valueName.Equals("h3") || valueName.Equals("h4") ||
                  valueName.Equals("h5") || valueName.Equals("h6")) {
                if (!this.hasHtmlHeaderElementInScope()) {
                  this.error = true;
                  return false;
                }
                this.generateImpliedEndTags();
                if (!this.getCurrentNode().getLocalName().Equals(valueName)) {
                  this.error = true;
                }
                while (true) {
                  IElement node = this.popCurrentNode();
                  string name2 = node.getLocalName();
                  if (name2.Equals("h1") ||
                    name2.Equals("h2") || name2.Equals("h3") ||
                    name2.Equals("h4") || name2.Equals("h5") ||
                    name2.Equals("h6")) {
                    break;
                  }
                }
                return true;
              } else if (valueName.Equals("dd") || valueName.Equals("dt")) {
                if (!this.hasHtmlElementInScope(valueName)) {
                  this.error = true;
                  return false;
                }
                this.generateImpliedEndTagsExcept(valueName);
                if (!this.getCurrentNode().getLocalName().Equals(valueName)) {
                  this.error = true;
                }
                while (true) {
                  IElement node = this.popCurrentNode();
                  if (node.getLocalName().Equals(valueName)) {
                    break;
                  }
                }
              } else if ("br".Equals(valueName)) {
                this.error = true;
                this.applyStartTag("br", insMode);
                return false;
              } else {
                for (int i = this.openElements.Count - 1; i >= 0; --i) {
                  IElement node = this.openElements[i];
                  if (valueName.Equals(node.getLocalName())) {
                    this.generateImpliedEndTagsExcept(valueName);
                    if
     (!valueName.Equals(this.getCurrentNode().getLocalName())) {
                    this.error = true;
                    }
                    while (true) {
                    IElement node2 = this.popCurrentNode();
                    if (node2.Equals(node)) {
                    break;
                    }
                    }
                    break;
                  } else if (this.isSpecialElement(node)) {
                    this.error = true;
                    return false;
                  }
                }
              }
            }
            return true;
          }
        case InsertionMode.InHeadNoscript: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              if (token == 0x09 || token == 0x0a || token == 0x0c ||
                    token == 0x0d || token == 0x20) {
                return this.applyInsertionMode(
         token,
         InsertionMode.InBody);
              } else {
                this.error = true;
                this.applyEndTag("noscript", insMode);
                return this.applyInsertionMode(token, null);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.error = true;
              return false;
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("html")) {
                return this.applyInsertionMode(
         token,
         InsertionMode.InBody);
              } else if (valueName.Equals("basefont") || valueName.Equals(
          "bgsound") || valueName.Equals("link") || valueName.Equals("meta") ||
                    valueName.Equals("noframes") || valueName.Equals("style")
) {
                return this.applyInsertionMode(
         token,
         InsertionMode.InHead);
              } else if (valueName.Equals("head") || valueName.Equals(
       "noscript")) {
                this.error = true;
                return false;
              } else {
                this.error = true;
                this.applyEndTag("noscript", insMode);
                return this.applyInsertionMode(token, null);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("noscript")) {
                this.popCurrentNode();
                this.insertionMode = InsertionMode.InHead;
              } else if (valueName.Equals("br")) {
                this.error = true;
                this.applyEndTag("noscript", insMode);
                return this.applyInsertionMode(token, null);
              } else {
                this.error = true;
                return false;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              return this.applyInsertionMode(
       token,
       InsertionMode.InHead);
            } else if (token == TOKEN_EOF) {
              this.error = true;
              this.applyEndTag("noscript", insMode);
              return this.applyInsertionMode(token, null);
            }
            return true;
          }
        case InsertionMode.InTable: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              IElement currentNode = this.getCurrentNode();
              if (currentNode.getLocalName().Equals("table") ||
                  currentNode.getLocalName().Equals("tbody") ||
                  currentNode.getLocalName().Equals("tfoot") ||
                  currentNode.getLocalName().Equals("thead") ||
                  currentNode.getLocalName().Equals("tr")) {
                this.pendingTableCharacters.Remove(
             0,
             this.pendingTableCharacters.Length);
                this.originalInsertionMode = this.insertionMode;
                this.insertionMode = InsertionMode.InTableText;
                return this.applyInsertionMode(token, null);
              } else {
                // NOTE: Foster parenting rules don't apply here, since
                // the current node isn't table, tbody, tfoot, thead, or
                // tr and won't change while In Body is being applied
                this.error = true;
                return this.applyInsertionMode(
         token,
         InsertionMode.InBody);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.error = true;
              return false;
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("table")) {
                this.error = true;
                return this.applyEndTag("table", insMode) ?
                  this.applyInsertionMode(token, null) : false;
              } else if (valueName.Equals("caption")) {
                while (true) {
                  IElement node = this.getCurrentNode();
                  if (node == null || node.getLocalName().Equals("table") ||
                    node.getLocalName().Equals("html")) {
                    break;
                  }
                  this.popCurrentNode();
                }
                this.insertFormattingMarker(
        tag,
        this.addHtmlElement(tag));
                this.insertionMode = InsertionMode.InCaption;
                return true;
              } else if (valueName.Equals("colgroup")) {
                while (true) {
                  IElement node = this.getCurrentNode();
                  if (node == null || node.getLocalName().Equals("table") ||
                    node.getLocalName().Equals("html")) {
                    break;
                  }
                  this.popCurrentNode();
                }
                this.addHtmlElement(tag);
                this.insertionMode = InsertionMode.InColumnGroup;
                return true;
              } else if (valueName.Equals("col")) {
                this.applyStartTag("colgroup", insMode);
                return this.applyInsertionMode(token, null);
              } else if (valueName.Equals("tbody") || valueName.Equals(
     "tfoot") || valueName.Equals("thead")) {
                while (true) {
                  IElement node = this.getCurrentNode();
                  if (node == null || node.getLocalName().Equals("table") ||
                    node.getLocalName().Equals("html")) {
                    break;
                  }
                  this.popCurrentNode();
                }
                this.addHtmlElement(tag);
                this.insertionMode = InsertionMode.InTableBody;
              } else if (valueName.Equals("td") || valueName.Equals("th") ||
                  valueName.Equals("tr")) {
                this.applyStartTag("tbody", insMode);
                return this.applyInsertionMode(token, null);
              } else if (valueName.Equals("style") ||
                  valueName.Equals("script")) {
                this.applyInsertionMode(token, InsertionMode.InHead);
              } else if (valueName.Equals("input")) {
                string attr = tag.getAttribute("type");
                if (attr == null || !"hidden"
                    .Equals(DataUtilities.ToLowerCaseAscii(attr))) {
                  this.error = true;
                  this.doFosterParent = true;
                  this.applyInsertionMode(
    token,
    InsertionMode.InBody);
                  this.doFosterParent = false;
                } else {
                  this.error = true;
                  this.addHtmlElementNoPush(tag);
                  tag.ackSelfClosing();
                }
              } else if (valueName.Equals("form")) {
                this.error = true;
                if (this.formElement != null) {
                  return false;
                }
                this.formElement = this.addHtmlElementNoPush(tag);
              } else {
                this.error = true;
                this.doFosterParent = true;
                this.applyInsertionMode(token, InsertionMode.InBody);
                this.doFosterParent = false;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("table")) {
                if (!this.hasHtmlElementInTableScope(valueName)) {
                  this.error = true;
                  return false;
                } else {
                  while (true) {
                    IElement node = this.popCurrentNode();
                    if (node.getLocalName().Equals(valueName)) {
                    break;
                    }
                  }
                  this.resetInsertionMode();
                }
              } else if (valueName.Equals("body") || valueName.Equals(
      "caption") || valueName.Equals("col") || valueName.Equals("colgroup") ||
                    valueName.Equals("html") || valueName.Equals("tbody") ||
                    valueName.Equals("td") || valueName.Equals("tfoot") ||
                    valueName.Equals("th") || valueName.Equals("thead") ||
                    valueName.Equals("tr")) {
                this.error = true;
                return false;
              } else {
                this.doFosterParent = true;
                this.applyInsertionMode(token, InsertionMode.InBody);
                this.doFosterParent = false;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToCurrentNode(token);
              return true;
            } else if (token == TOKEN_EOF) {
              this.error |= this.getCurrentNode() == null ||
                !this.getCurrentNode().getLocalName().Equals("html");
              this.stopParsing();
            }
            return true;
          }
        case InsertionMode.InTableText: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              if (token == 0) {
                this.error = true;
                return false;
              } else {
                if (token <= 0xffff) {
                  this.pendingTableCharacters.Append((char)token);
                } else if (token <= 0x10ffff) {
               this.pendingTableCharacters.Append((char)((((token - 0x10000) >>
                10) & 0x3ff) + 0xd800));
                  this.pendingTableCharacters.Append((char)(((token -
                    0x10000) & 0x3ff) + 0xdc00));
                }
              }
            } else {
              var nonspace = false;
              string str = this.pendingTableCharacters.ToString();
              for (int i = 0; i < str.Length; ++i) {
                int c = DataUtilities.CodePointAt(str, i);
                if (c >= 0x10000) {
                  ++c;
                }
                if (c != 0x9 && c != 0xa && c != 0xc && c != 0xd && c != 0x20) {
                  nonspace = true;
                  break;
                }
              }
              if (nonspace) {
                // See 'anything else' for 'in table'
                this.error = true;
                this.doFosterParent = true;
                for (int i = 0; i < str.Length; ++i) {
                  int c = DataUtilities.CodePointAt(str, i);
                  if (c >= 0x10000) {
                    ++c;
                  }
                  this.applyInsertionMode(c, InsertionMode.InBody);
                }
                this.doFosterParent = false;
              } else {
                this.insertString(
    this.getCurrentNode(),
    this.pendingTableCharacters.ToString());
              }
              this.insertionMode = this.originalInsertionMode;
              return this.applyInsertionMode(token, null);
            }
            return true;
          }
        case InsertionMode.InCaption: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("caption") ||
                  valueName.Equals("col") || valueName.Equals("colgroup") ||
                  valueName.Equals("tbody") || valueName.Equals("thead") ||
                  valueName.Equals("td") || valueName.Equals("tfoot") ||
                valueName.Equals("th") || valueName.Equals("tr")) {
                this.error = true;
                if (this.applyEndTag("caption", insMode)) {
                  return this.applyInsertionMode(token, null);
                }
              } else {
                return this.applyInsertionMode(
         token,
         InsertionMode.InBody);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("caption")) {
                if (!this.hasHtmlElementInScope(valueName)) {
                  this.error = true;
                  return false;
                }
                this.generateImpliedEndTags();
                if (!this.getCurrentNode().getLocalName().Equals(
      "caption")) {
                  this.error = true;
                }
                while (true) {
                  IElement node = this.popCurrentNode();
                  if (node.getLocalName().Equals("caption")) {
                    break;
                  }
                }
                this.clearFormattingToMarker();
                this.insertionMode = InsertionMode.InTable;
              } else if (valueName.Equals("table")) {
                this.error = true;
                if (this.applyEndTag("caption", insMode)) {
                  return this.applyInsertionMode(token, null);
                }
              } else if (valueName.Equals("body") ||
                  valueName.Equals("col") || valueName.Equals("colgroup") ||
                  valueName.Equals("tbody") || valueName.Equals("thead") ||
                  valueName.Equals("td") || valueName.Equals("tfoot") ||
                  valueName.Equals("th") || valueName.Equals("tr") ||
                  valueName.Equals("html")) {
                this.error = true;
                return false;
              } else {
                return this.applyInsertionMode(
         token,
         InsertionMode.InBody);
              }
            } else {
              return this.applyInsertionMode(
       token,
       InsertionMode.InBody);
            }
            return true;
          }
        case InsertionMode.InColumnGroup: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              if (token == 0x20 || token == 0x0c || token ==
                0x0a || token == 0x0d || token == 0x09) {
                this.insertCharacter(
     this.getCurrentNode(),
     token);
              } else {
                if (this.applyEndTag("colgroup", insMode)) {
                  return this.applyInsertionMode(token, null);
                }
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.error = true;
              return false;
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("html")) {
                return this.applyInsertionMode(
         token,
         InsertionMode.InBody);
              } else if (valueName.Equals("col")) {
                this.addHtmlElementNoPush(tag);
                tag.ackSelfClosing();
              } else {
                if (this.applyEndTag("colgroup", insMode)) {
                  return this.applyInsertionMode(token, null);
                }
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("colgroup")) {
                if (this.getCurrentNode().getLocalName().Equals("html")) {
                  this.error = true;
                  return false;
                }
                this.popCurrentNode();
                this.insertionMode = InsertionMode.InTable;
              } else if (valueName.Equals("col")) {
                this.error = true;
                return false;
              } else {
                if (this.applyEndTag("colgroup", insMode)) {
                  return this.applyInsertionMode(token, null);
                }
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              if (this.applyEndTag("colgroup", insMode)) {
                return this.applyInsertionMode(token, null);
              }
            } else if (token == TOKEN_EOF) {
              if (this.getCurrentNode().getLocalName().Equals("html")) {
                this.stopParsing();
                return true;
              }
              if (this.applyEndTag("colgroup", insMode)) {
                return this.applyInsertionMode(token, null);
              }
            }
            return true;
          }
        case InsertionMode.InTableBody: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("tr")) {
                while (true) {
                  IElement node = this.getCurrentNode();
                  if (node == null || node.getLocalName().Equals("tbody") ||
                    node.getLocalName().Equals("tfoot") ||
                    node.getLocalName().Equals("thead") ||
                    node.getLocalName().Equals("html")) {
                    break;
                  }
                  this.popCurrentNode();
                }
                this.addHtmlElement(tag);
                this.insertionMode = InsertionMode.InRow;
              } else if (valueName.Equals("th") || valueName.Equals("td")) {
                this.error = true;
                this.applyStartTag("tr", insMode);
                return this.applyInsertionMode(token, null);
              } else if (valueName.Equals("caption") ||
                  valueName.Equals("col") || valueName.Equals("colgroup") ||
                  valueName.Equals("tbody") || valueName.Equals("tfoot") ||
                  valueName.Equals("thead")) {
                if (!this.hasHtmlElementInTableScope("tbody") &&
                    !this.hasHtmlElementInTableScope("thead") &&
                    !this.hasHtmlElementInTableScope("tfoot")
) {
                  this.error = true;
                  return false;
                }
                while (true) {
                  IElement node = this.getCurrentNode();
                  if (node == null || node.getLocalName().Equals("tbody") ||
                    node.getLocalName().Equals("tfoot") ||
                    node.getLocalName().Equals("thead") ||
                    node.getLocalName().Equals("html")) {
                    break;
                  }
                  this.popCurrentNode();
                }
                this.applyEndTag(
             this.getCurrentNode().getLocalName(),
             insMode);
                return this.applyInsertionMode(token, null);
              } else {
                return this.applyInsertionMode(
          token,
          InsertionMode.InTable);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("tbody") ||
                  valueName.Equals("tfoot") || valueName.Equals("thead")) {
                if (!this.hasHtmlElementInScope(valueName)) {
                  this.error = true;
                  return false;
                }
                while (true) {
                  IElement node = this.getCurrentNode();
                  if (node == null || node.getLocalName().Equals("tbody") ||
                    node.getLocalName().Equals("tfoot") ||
                    node.getLocalName().Equals("thead") ||
                    node.getLocalName().Equals("html")) {
                    break;
                  }
                  this.popCurrentNode();
                }
                this.popCurrentNode();
                this.insertionMode = InsertionMode.InTable;
              } else if (valueName.Equals("table")) {
                if (!this.hasHtmlElementInTableScope("tbody") &&
                    !this.hasHtmlElementInTableScope("thead") &&
                    !this.hasHtmlElementInTableScope("tfoot")
) {
                  this.error = true;
                  return false;
                }
                while (true) {
                  IElement node = this.getCurrentNode();
                  if (node == null || node.getLocalName().Equals("tbody") ||
                    node.getLocalName().Equals("tfoot") ||
                    node.getLocalName().Equals("thead") ||
                    node.getLocalName().Equals("html")) {
                    break;
                  }
                  this.popCurrentNode();
                }
                this.applyEndTag(
             this.getCurrentNode().getLocalName(),
             insMode);
                return this.applyInsertionMode(token, null);
              } else if (valueName.Equals("body") ||
                  valueName.Equals("caption") || valueName.Equals("col") ||
                  valueName.Equals("colgroup") || valueName.Equals("html") ||
                  valueName.Equals("td") || valueName.Equals("th") ||
                  valueName.Equals("tr")) {
                this.error = true;
                return false;
              } else {
                return this.applyInsertionMode(
          token,
          InsertionMode.InTable);
              }
            } else {
              return this.applyInsertionMode(
        token,
        InsertionMode.InTable);
            }
            return true;
          }
        case InsertionMode.InRow: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              this.applyInsertionMode(token, InsertionMode.InTable);
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.applyInsertionMode(token, InsertionMode.InTable);
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("th") || valueName.Equals("td")) {
                while (!this.getCurrentNode().getLocalName().Equals(
    "tr") && !this.getCurrentNode().getLocalName().Equals("html")) {
                  this.popCurrentNode();
                }
                this.insertionMode = InsertionMode.InCell;
                this.insertFormattingMarker(
        tag,
        this.addHtmlElement(tag));
              } else if (valueName.Equals("caption") || valueName.Equals(
     "col") || valueName.Equals("colgroup") || valueName.Equals("tbody") ||
                    valueName.Equals("tfoot") || valueName.Equals("thead") ||
                    valueName.Equals("tr")) {
                if (this.applyEndTag("tr", insMode)) {
                  return this.applyInsertionMode(token, null);
                }
              } else {
                this.applyInsertionMode(token, InsertionMode.InTable);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("tr")) {
                if (!this.hasHtmlElementInTableScope(valueName)) {
                  this.error = true;
                  return false;
                }
                while (!this.getCurrentNode().getLocalName().Equals(
    "tr") && !this.getCurrentNode().getLocalName().Equals("html")) {
                  this.popCurrentNode();
                }
                this.popCurrentNode();
                this.insertionMode = InsertionMode.InTableBody;
              } else if (valueName.Equals("tbody") || valueName.Equals(
     "tfoot") || valueName.Equals("thead")) {
                if (!this.hasHtmlElementInTableScope(valueName)) {
                  this.error = true;
                  return false;
                }
                this.applyEndTag("tr", insMode);
                return this.applyInsertionMode(token, null);
              } else if (valueName.Equals("caption") ||
                  valueName.Equals("col") || valueName.Equals("colgroup") ||
                  valueName.Equals("html") || valueName.Equals("body") ||
                  valueName.Equals("td") || valueName.Equals("th")) {
                this.error = true;
              } else {
                this.applyInsertionMode(token, InsertionMode.InTable);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.applyInsertionMode(token, InsertionMode.InTable);
            } else if (token == TOKEN_EOF) {
              this.applyInsertionMode(token, InsertionMode.InTable);
            }
            return true;
          }
        case InsertionMode.InCell: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              this.applyInsertionMode(token, InsertionMode.InBody);
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.applyInsertionMode(token, InsertionMode.InBody);
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("caption") ||
                  valueName.Equals("col") || valueName.Equals("colgroup") ||
                  valueName.Equals("tbody") || valueName.Equals("td") ||
                  valueName.Equals("tfoot") || valueName.Equals("th") ||
                  valueName.Equals("thead") || valueName.Equals("tr")) {
                if (!this.hasHtmlElementInTableScope("td") &&
                    !this.hasHtmlElementInTableScope("th")) {
                  this.error = true;
                  return false;
                }
                this.applyEndTag(
    this.hasHtmlElementInTableScope("td") ? "td" : "th",
    insMode);
                return this.applyInsertionMode(token, null);
              } else {
                this.applyInsertionMode(token, InsertionMode.InBody);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("td") || valueName.Equals("th")) {
                if (!this.hasHtmlElementInTableScope(valueName)) {
                  this.error = true;
                  return false;
                }
                this.generateImpliedEndTags();
                if (!this.getCurrentNode().getLocalName().Equals(valueName)) {
                  this.error = true;
                }
                while (true) {
                  IElement node = this.popCurrentNode();
                  if (node.getLocalName().Equals(valueName)) {
                    break;
                  }
                }
                this.clearFormattingToMarker();
                this.insertionMode = InsertionMode.InRow;
              } else if (valueName.Equals("caption") || valueName.Equals(
     "col") || valueName.Equals("colgroup") || valueName.Equals("body") ||
                    valueName.Equals("html")) {
                this.error = true;
                return false;
              } else if (valueName.Equals("table") ||
                  valueName.Equals("tbody") || valueName.Equals("tfoot") ||
                  valueName.Equals("thead") || valueName.Equals("tr")) {
                if (!this.hasHtmlElementInTableScope(valueName)) {
                  this.error = true;
                  return false;
                }
                this.applyEndTag(
    this.hasHtmlElementInTableScope("td") ? "td" : "th",
    insMode);
                return this.applyInsertionMode(token, null);
              } else {
                this.applyInsertionMode(token, InsertionMode.InBody);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.applyInsertionMode(token, InsertionMode.InBody);
            } else if (token == TOKEN_EOF) {
              this.applyInsertionMode(token, InsertionMode.InBody);
            }
            return true;
          }
        case InsertionMode.InSelect: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              if (token == 0) {
                this.error = true; return false;
              } else {
                this.insertCharacter(
     this.getCurrentNode(),
     token);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.error = true; return false;
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("html")) {
                this.applyInsertionMode(token, InsertionMode.InBody);
              } else if (valueName.Equals("option")) {
                if (this.getCurrentNode().getLocalName().Equals(
    "option")) {
                  this.applyEndTag("option", insMode);
                }
                this.addHtmlElement(tag);
              } else if (valueName.Equals("optgroup")) {
                if (this.getCurrentNode().getLocalName().Equals(
    "option")) {
                  this.applyEndTag("option", insMode);
                }
                if (this.getCurrentNode().getLocalName().Equals(
      "optgroup")) {
                  this.applyEndTag("optgroup", insMode);
                }
                this.addHtmlElement(tag);
              } else if (valueName.Equals("select")) {
                this.error = true;
                return this.applyEndTag("select", insMode);
              } else if (valueName.Equals("input") || valueName.Equals(
      "keygen") || valueName.Equals("textarea")) {
                this.error = true;
                if (!this.hasHtmlElementInSelectScope("select")) {
                  return false;
                }
                this.applyEndTag("select", insMode);
                return this.applyInsertionMode(token, null);
       } else if (valueName.Equals("script") || valueName.Equals(
  "template")) {
                return this.applyInsertionMode(
         token,
         InsertionMode.InHead);
              } else {
                this.error = true; return false;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("optgroup")) {
                if (this.getCurrentNode().getLocalName().Equals(
    "option") && this.openElements.Count >= 2 &&
            this.openElements[this.openElements.Count -
                   2].getLocalName().Equals(
          "optgroup")) {
                  this.applyEndTag("option", insMode);
                }
                if (this.getCurrentNode().getLocalName().Equals(
      "optgroup")) {
                  this.popCurrentNode();
                } else {
                  this.error = true;
                  return false;
                }
              } else if (valueName.Equals("option")) {
                if (this.getCurrentNode().getLocalName().Equals(
    "option")) {
                  this.popCurrentNode();
                } else {
                  this.error = true;
                  return false;
                }
              } else if (valueName.Equals("select")) {
                if (!this.hasHtmlElementInScope(valueName)) {
                  this.error = true;
                  return false;
                }
                while (true) {
                  IElement node = this.popCurrentNode();
                  if (node.getLocalName().Equals(valueName)) {
                    break;
                  }
                }
                this.resetInsertionMode();
              } else if (valueName.Equals("template")) {
                return this.applyInsertionMode(
         token,
         InsertionMode.InHead);
              } else {
                this.error = true;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToCurrentNode(token);
            } else if (token == TOKEN_EOF) {
                return this.applyInsertionMode(
         token,
         InsertionMode.InBody);
            } else {
                this.error = true;
            }
            return true;
          }
        case InsertionMode.InSelectInTable: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              return this.applyInsertionMode(
         token,
         InsertionMode.InSelect);
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              return this.applyInsertionMode(
         token,
         InsertionMode.InSelect);
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("caption") ||
                  valueName.Equals("table") || valueName.Equals("tbody") ||
                  valueName.Equals("tfoot") || valueName.Equals("thead") ||
                  valueName.Equals("tr") || valueName.Equals("td") ||
                  valueName.Equals("th")) {
                this.error = true;
                this.applyEndTag("select", insMode);
                return this.applyInsertionMode(token, null);
              }
              return this.applyInsertionMode(
         token,
         InsertionMode.InSelect);
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("caption") ||
                  valueName.Equals("table") || valueName.Equals("tbody") ||
                  valueName.Equals("tfoot") || valueName.Equals("thead") ||
                  valueName.Equals("tr") || valueName.Equals("td") ||
                  valueName.Equals("th")) {
                this.error = true;
                if (!this.hasHtmlElementInTableScope(valueName)) {
                  return false;
                }
                this.applyEndTag("select", insMode);
                return this.applyInsertionMode(token, null);
              }
              return this.applyInsertionMode(
         token,
         InsertionMode.InSelect);
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              return this.applyInsertionMode(
         token,
         InsertionMode.InSelect);
            } else {
              return (token == TOKEN_EOF) ?
     this.applyInsertionMode(token, InsertionMode.InSelect) :
                  true;
            }
          }
        case InsertionMode.AfterBody: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              if (token == 0x09 || token == 0x0a || token ==
                0x0c || token == 0x0d || token == 0x20) {
                this.applyInsertionMode(token, InsertionMode.InBody);
              } else {
                this.error = true;
                this.insertionMode = InsertionMode.InBody;
                return this.applyInsertionMode(token, null);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.error = true;
              return true;
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("html")) {
                this.applyInsertionMode(token, InsertionMode.InBody);
              } else {
                this.error = true;
                this.insertionMode = InsertionMode.InBody;
                return this.applyInsertionMode(token, null);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("html")) {
                if (this.context != null) {
                  this.error = true;
                  return false;
                }
                this.insertionMode = InsertionMode.AfterAfterBody;
              } else {
                this.error = true;
                this.insertionMode = InsertionMode.InBody;
                return this.applyInsertionMode(token, null);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToFirst(token);
            } else if (token == TOKEN_EOF) {
              this.stopParsing();

              return true;
            }
            return true;
          }
        case InsertionMode.InFrameset: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              if (token == 0x09 || token == 0x0a || token == 0x0c ||
                    token == 0x0d || token == 0x20) {
                this.insertCharacter(
     this.getCurrentNode(),
     token);
              } else {
                this.error = true;
                return false;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.error = true;
              return false;
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("html")) {
                this.applyInsertionMode(token, InsertionMode.InBody);
              } else if (valueName.Equals("frameset")) {
                this.addHtmlElement(tag);
              } else if (valueName.Equals("frame")) {
                this.addHtmlElementNoPush(tag);
                tag.ackSelfClosing();
              } else if (valueName.Equals("noframes")) {
                this.applyInsertionMode(token, InsertionMode.InHead);
              } else {
                this.error = true;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              if (this.getCurrentNode().getLocalName().Equals("html")) {
                this.error = true;
                return false;
              }
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("frameset")) {
                this.popCurrentNode();
                if (this.context == null &&
           !HtmlCommon.isHtmlElement(this.getCurrentNode(), "frameset"
)) {
                  this.insertionMode = InsertionMode.AfterFrameset;
                }
              } else {
                this.error = true;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToCurrentNode(token);
            } else if (token == TOKEN_EOF) {
              if (!HtmlCommon.isHtmlElement(this.getCurrentNode(), "html"
)) {
                this.error = true;
              }
              this.stopParsing();
            }
            return true;
          }
        case InsertionMode.AfterFrameset: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              if (token == 0x09 || token == 0x0a || token ==
                0x0c || token == 0x0d || token == 0x20) {
                this.insertCharacter(
     this.getCurrentNode(),
     token);
              } else {
                this.error = true;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.error = true;
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("html")) {
                return this.applyInsertionMode(
         token,
         InsertionMode.InBody);
              } else if (valueName.Equals("noframes")) {
                return this.applyInsertionMode(
         token,
         InsertionMode.InHead);
              } else {
                this.error = true;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              var tag = (EndTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("html")) {
                this.insertionMode = InsertionMode.AfterAfterFrameset;
              } else {
                this.error = true;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToCurrentNode(token);
            } else if (token == TOKEN_EOF) {
              this.stopParsing();
            }
            return true;
          }
        case InsertionMode.AfterAfterBody: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              if (token == 0x09 || token == 0x0a || token ==
                0x0c || token == 0x0d || token == 0x20) {
                this.applyInsertionMode(token, InsertionMode.InBody);
              } else {
                this.error = true;
                this.insertionMode = InsertionMode.InBody;
                return this.applyInsertionMode(token, null);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.applyInsertionMode(token, InsertionMode.InBody);
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if (valueName.Equals("html")) {
                this.applyInsertionMode(token, InsertionMode.InBody);
              } else {
                this.error = true;
                this.insertionMode = InsertionMode.InBody;
                return this.applyInsertionMode(token, null);
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              this.error = true;
              this.insertionMode = InsertionMode.InBody;
              return this.applyInsertionMode(token, null);
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToDocument(token);
            } else if (token == TOKEN_EOF) {
              this.stopParsing();
            }
            return true;
          }
        case InsertionMode.AfterAfterFrameset: {
            if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
              if (token == 0x09 || token == 0x0a || token ==
                0x0c || token == 0x0d || token == 0x20) {
                this.applyInsertionMode(token, InsertionMode.InBody);
              } else {
                this.error = true;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
              this.applyInsertionMode(token, InsertionMode.InBody);
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
              var tag = (StartTagToken)this.getToken(token);
              string valueName = tag.getName();
              if ("html".Equals(valueName)) {
                this.applyInsertionMode(token, InsertionMode.InBody);
              } else if ("noframes".Equals(valueName)) {
                this.applyInsertionMode(token, InsertionMode.InHead);
              } else {
                this.error = true;
              }
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
              this.error = true;
            } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
              this.addCommentNodeToDocument(token);
            } else if (token == TOKEN_EOF) {
              this.stopParsing();
            }
            return true;
          }
        default: throw new InvalidOperationException();
      }
    }

    private bool applyStartTag(string valueName, InsertionMode? insMode) {
      return this.applyInsertionMode(
  this.getArtificialToken(TOKEN_START_TAG, valueName),
  insMode);
    }

    private void changeEncoding(string charset) {
      string currentEncoding = this.encoding.getEncoding();
      if (currentEncoding.Equals("utf-16le") ||
          currentEncoding.Equals("utf-16be")) {
        this.encoding = new
             EncodingConfidence(currentEncoding, EncodingConfidence.Certain);
        return;
      }
      if (charset.Equals("utf-16le")) {
        charset = "utf-8";
      } else if (charset.Equals("utf-16be")) {
        charset = "utf-8";
      }
      if (charset.Equals(currentEncoding)) {
        this.encoding = new
             EncodingConfidence(currentEncoding, EncodingConfidence.Certain);
        return;
      }
      // Reinitialize all parser state
      this.initialize();
      // Rewind the input stream and set the new encoding
      this.inputStream.rewind();
      this.encoding = new EncodingConfidence(
     charset,
     EncodingConfidence.Certain);
      ICharacterEncoding henc = new Html5Encoding(this.encoding);
      this.charInput = new StackableCharacterInput(
        Encodings.GetDecoderInput(henc, this.inputStream));
    }

    private void clearFormattingToMarker() {
      while (this.formattingElements.Count > 0) {
        FormattingElement
  fe = removeAtIndex(
  this.formattingElements,
  this.formattingElements.Count - 1);
        if (fe.isMarker()) {
          break;
        }
      }
    }

private void PopUntilHtmlElementPopped(string name) {
        while (!HtmlCommon.isHtmlElement(this.getCurrentNode(), name)) {
          this.popCurrentNode();
        }
        this.popCurrentNode();
}

    private void closeParagraph() {
      if (this.hasHtmlElementInButtonScope("p")) {
        this.generateImpliedEndTagsExcept("p");
        IElement node = this.getCurrentNode();
        if (!HtmlCommon.isHtmlElement(node, "p")) {
 this.error = true;
}
this.PopUntilHtmlElementPopped("p");
      }
    }

    private Comment createCommentNode(int valueToken) {
      var comment = (CommentToken)this.getToken(valueToken);
      var node = new Comment();
      node.setData(comment.getValue());
      return node;
    }

    private int emitCurrentTag() {
      int ret = this.tokens.Count | this.currentTag.getType();
      this.tokens.Add(this.currentTag);
      if (this.currentTag.getType() == TOKEN_START_TAG) {
        this.lastStartTag = this.currentTag;
      } else {
        if (this.currentTag.getAttributes().Count > 0 ||
            this.currentTag.isSelfClosing()) {
          this.error = true;
        }
      }
      this.currentTag = null;
      return ret;
    }

    private void fosterParent(INode valueElement) {
      if (this.openElements.Count == 0) {
        return;
      }
      INode fosterParent = this.openElements[0];
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (e.getLocalName().Equals("table")) {
          var parent = (Node)e.getParentNode();
          bool isElement = parent != null && parent.getNodeType() ==
            NodeType.ELEMENT_NODE;
          if (!isElement) {  // the parent is not an element
            if (i <= 1) {
              // This usually won't happen
              throw new InvalidOperationException();
            }
            // append to the valueElement before this table
            fosterParent = this.openElements[i - 1];
            break;
          } else {
            // Parent of the table, insert before the table
            parent.insertBefore((Node)valueElement, (Node)e);
            return;
          }
        }
      }
      ((Node)fosterParent).appendChild(valueElement);
    }

    private void generateImpliedEndTags() {
      while (true) {
        IElement node = this.getCurrentNode();
        if (HtmlCommon.isHtmlElement(node, "dd") ||
          HtmlCommon.isHtmlElement(node, "dt") ||
            HtmlCommon.isHtmlElement(node, "li") ||
              HtmlCommon.isHtmlElement(node, "option") ||
            HtmlCommon.isHtmlElement(node, "optgroup") ||
              HtmlCommon.isHtmlElement(node, "p") ||
HtmlCommon.isHtmlElement(node, "rp") || HtmlCommon.isHtmlElement(node, "rt"
) ||
            HtmlCommon.isHtmlElement(node, "rb") ||
              HtmlCommon.isHtmlElement(node, "rtc")) {
          this.popCurrentNode();
        } else {
          break;
        }
      }
    }

    private void generateImpliedEndTagsThoroughly() {
      while (true) {
        IElement node = this.getCurrentNode();
        if (HtmlCommon.isHtmlElement(node, "dd") ||
            HtmlCommon.isHtmlElement(node, "dd") ||
            HtmlCommon.isHtmlElement(node, "dt") ||
            HtmlCommon.isHtmlElement(node, "li") ||
              HtmlCommon.isHtmlElement(node, "option") ||
            HtmlCommon.isHtmlElement(node, "optgroup") ||
              HtmlCommon.isHtmlElement(node, "p") ||
            HtmlCommon.isHtmlElement(node, "rp") ||
            HtmlCommon.isHtmlElement(node, "caption") ||
            HtmlCommon.isHtmlElement(node, "colgroup") ||
            HtmlCommon.isHtmlElement(node, "tbody") ||
            HtmlCommon.isHtmlElement(node, "tfoot") ||
            HtmlCommon.isHtmlElement(node, "thead") ||
            HtmlCommon.isHtmlElement(node, "td") ||
            HtmlCommon.isHtmlElement(node, "th") ||
            HtmlCommon.isHtmlElement(node, "tr") ||
HtmlCommon.isHtmlElement(node, "rt") ||
            HtmlCommon.isHtmlElement(node, "rb") ||
              HtmlCommon.isHtmlElement(node, "rtc")) {
          this.popCurrentNode();
        } else {
          break;
        }
      }
    }

    private void generateImpliedEndTagsExcept(string _string) {
      while (true) {
        IElement node = this.getCurrentNode();
        if (HtmlCommon.isHtmlElement(node, _string)) {
          break;
        }
        if (HtmlCommon.isHtmlElement(node, "dd") ||
HtmlCommon.isHtmlElement(node, "dt") || HtmlCommon.isHtmlElement(node, "li"
) ||
            HtmlCommon.isHtmlElement(node, "rb") ||
              HtmlCommon.isHtmlElement(node, "rtc") ||
            HtmlCommon.isHtmlElement(node, "option") ||
              HtmlCommon.isHtmlElement(node, "optgroup") ||
   HtmlCommon.isHtmlElement(
  node,
  "p") || HtmlCommon.isHtmlElement(
  node,
  "rp") || HtmlCommon.isHtmlElement(node, "rt")) {
          this.popCurrentNode();
        } else {
          break;
        }
      }
    }

    private int getArtificialToken(int type, string valueName) {
      if (type == TOKEN_END_TAG) {
        var valueToken = new EndTagToken(valueName);
        int ret = this.tokens.Count | type;
        this.tokens.Add(valueToken);
        return ret;
      }
      if (type == TOKEN_START_TAG) {
        var valueToken = new StartTagToken(valueName);
        int ret = this.tokens.Count | type;
        this.tokens.Add(valueToken);
        return ret;
      }
      throw new ArgumentException();
    }

    private IElement getCurrentNode() {
      return (this.openElements.Count == 0) ? null :
            this.openElements[this.openElements.Count - 1];
    }

    private FormattingElement getFormattingElement(IElement node) {
      foreach (var fe in this.formattingElements) {
        if (!fe.isMarker() && node.Equals(fe.Element)) {
          return fe;
        }
      }
      return null;
    }

    private Text getFosterParentedTextNode() {
      if (this.openElements.Count == 0) {
        return null;
      }
      INode fosterParent = this.openElements[0];
      IList<INode> childNodes;
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (e.getLocalName().Equals("table")) {
          var parent = (Node)e.getParentNode();
          bool isElement = parent != null && parent.getNodeType() ==
            NodeType.ELEMENT_NODE;
          if (!isElement) {  // the parent is not an valueElement
            if (i <= 1) {
              // This usually won't happen
              throw new InvalidOperationException();
            }
            // append to the valueElement before this table
            fosterParent = this.openElements[i - 1];
            break;
          } else {
            // Parent of the table, insert before the table
            childNodes = parent.getChildNodesInternal();
            if (childNodes.Count == 0) {
              throw new InvalidOperationException();
            }
            for (int j = 0; j < childNodes.Count; ++j) {
              if (childNodes[j].Equals(e)) {
                if (j > 0 && childNodes[j - 1].getNodeType() ==
                    NodeType.TEXT_NODE) {
                  return (Text)childNodes[j - 1];
                } else {
                  var textNode = new Text();
                  parent.insertBefore(textNode, (Node)e);
                  return textNode;
                }
              }
            }
            throw new InvalidOperationException();
          }
        }
      }
      childNodes = fosterParent.getChildNodes();
      INode lastChild = (childNodes.Count == 0) ? null :
          childNodes[childNodes.Count - 1];
      if (lastChild == null || lastChild.getNodeType() != NodeType.TEXT_NODE) {
        var textNode = new Text();
        fosterParent.appendChild(textNode);
        return textNode;
      } else {
        return (Text)lastChild;
      }
    }

    private Text getTextNodeToInsert(INode node) {
      if (this.doFosterParent && node.Equals(this.getCurrentNode())) {
        string valueName = ((Element)node).getLocalName();
        if ("table".Equals(valueName) || "tbody".Equals(valueName) ||
            "tfoot".Equals(valueName) || "thead".Equals(valueName) ||
            "tr".Equals(valueName)) {
          return this.getFosterParentedTextNode();
        }
      }
      IList<INode> childNodes = ((Node)node).getChildNodesInternal();
      INode lastChild = (childNodes.Count == 0) ? null :
          childNodes[childNodes.Count - 1];
      if (lastChild == null || lastChild.getNodeType() != NodeType.TEXT_NODE) {
        var textNode = new Text();
        node.appendChild(textNode);
        return textNode;
      } else {
        return (Text)lastChild;
      }
    }

    internal IToken getToken(int valueToken) {
      if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_CHARACTER ||
          (valueToken & TOKEN_TYPE_MASK) == TOKEN_EOF) {
        return null;
      } else {
        return this.tokens[valueToken & TOKEN_INDEX_MASK];
      }
    }

    private bool hasHtmlElementInButtonScope(string valueName) {
      var found = false;
      foreach (var e in this.openElements) {
        if (e.getLocalName().Equals(valueName)) {
          found = true;
        }
      }
      if (!found) {
        return false;
      }
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        string _namespace = e.getNamespaceURI();
        string thisName = e.getLocalName();
        if (HtmlCommon.HTML_NAMESPACE.Equals(_namespace)) {
          if (thisName.Equals(valueName)) {
            return true;
          }
          if (thisName.Equals("applet") || thisName.Equals("caption") ||
              thisName.Equals("html") || thisName.Equals("table") ||
              thisName.Equals("td") || thisName.Equals("th") ||
              thisName.Equals("marquee") || thisName.Equals("object") ||
              thisName.Equals("button")) {
            // DebugUtility.Log("not in scope: %s",thisName);
            return false;
          }
        }
        if (HtmlCommon.MATHML_NAMESPACE.Equals(_namespace)) {
          if (thisName.Equals("mi") ||
              thisName.Equals("mo") || thisName.Equals("mn") ||
              thisName.Equals("ms") || thisName.Equals("mtext") ||
              thisName.Equals("annotation-xml")) {
            return false;
          }
        }
        if (HtmlCommon.SVG_NAMESPACE.Equals(_namespace)) {
          if (thisName.Equals("foreignObject") || thisName.Equals("desc") ||
              thisName.Equals("title")) {
            return false;
          }
        }
      }
      return false;
    }

    private bool hasHtmlElementInListItemScope(string valueName) {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (HtmlCommon.isHtmlElement(e, valueName)) {
          return true;
        }
        if (HtmlCommon.isHtmlElement(e, "applet") ||
            HtmlCommon.isHtmlElement(e, "caption") ||
              HtmlCommon.isHtmlElement(e, "html") ||
   HtmlCommon.isHtmlElement(e, "table") || HtmlCommon.isHtmlElement(e, "td"
) || HtmlCommon.isHtmlElement(e, "th") || HtmlCommon.isHtmlElement(e, "ol"
) ||
  HtmlCommon.isHtmlElement(e, "ul") || HtmlCommon.isHtmlElement(e, "marquee"
) ||
  HtmlCommon.isHtmlElement(e, "object") || HtmlCommon.isMathMLElement(e, "mi"
) ||
  HtmlCommon.isMathMLElement(e, "mo") || HtmlCommon.isMathMLElement(e, "mn"
) || HtmlCommon.isMathMLElement(e, "ms") ||
              HtmlCommon.isMathMLElement(e, "mtext") ||
            HtmlCommon.isMathMLElement(e, "annotation-xml") ||
            HtmlCommon.isSvgElement(e, "foreignObject") ||
      HtmlCommon.isSvgElement(
  e,
  "desc") || HtmlCommon.isSvgElement(
  e,
  "title")
) {
          return false;
        }
      }
      return false;
    }

    private bool hasHtmlElementInScope(IElement node) {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (e == node) {
          return true;
        }
        if (HtmlCommon.isHtmlElement(e, "applet") ||
            HtmlCommon.isHtmlElement(e, "caption") ||
              HtmlCommon.isHtmlElement(e, "html") ||
   HtmlCommon.isHtmlElement(e, "table") || HtmlCommon.isHtmlElement(e, "td"
) ||
  HtmlCommon.isHtmlElement(e, "th") || HtmlCommon.isHtmlElement(e, "marquee"
) ||
  HtmlCommon.isHtmlElement(e, "object") || HtmlCommon.isMathMLElement(e, "mi"
) ||
  HtmlCommon.isMathMLElement(e, "mo") || HtmlCommon.isMathMLElement(e, "mn"
) || HtmlCommon.isMathMLElement(e, "ms") ||
              HtmlCommon.isMathMLElement(e, "mtext") ||
            HtmlCommon.isMathMLElement(e, "annotation-xml") ||
            HtmlCommon.isSvgElement(e, "foreignObject") ||
      HtmlCommon.isSvgElement(
  e,
  "desc") || HtmlCommon.isSvgElement(
  e,
  "title")
) {
          return false;
        }
      }
      return false;
    }

    private bool hasHtmlElementInScope(string valueName) {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (HtmlCommon.isHtmlElement(e, valueName)) {
          return true;
        }
        if (HtmlCommon.isHtmlElement(e, "applet") ||
            HtmlCommon.isHtmlElement(e, "caption") ||
              HtmlCommon.isHtmlElement(e, "html") ||
   HtmlCommon.isHtmlElement(e, "table") || HtmlCommon.isHtmlElement(e, "td"
) ||
  HtmlCommon.isHtmlElement(e, "th") || HtmlCommon.isHtmlElement(e, "marquee"
) ||
  HtmlCommon.isHtmlElement(e, "object") || HtmlCommon.isMathMLElement(e, "mi"
) ||
  HtmlCommon.isMathMLElement(e, "mo") || HtmlCommon.isMathMLElement(e, "mn"
) || HtmlCommon.isMathMLElement(e, "ms") ||
              HtmlCommon.isMathMLElement(e, "mtext") ||
            HtmlCommon.isMathMLElement(e, "annotation-xml") ||
            HtmlCommon.isSvgElement(e, "foreignObject") ||
      HtmlCommon.isSvgElement(
  e,
  "desc") || HtmlCommon.isSvgElement(
  e,
  "title")
) {
          return false;
        }
      }
      return false;
    }

    private bool hasHtmlElementInSelectScope(string valueName) {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (HtmlCommon.isHtmlElement(e, valueName)) {
          return true;
        }
        if (!HtmlCommon.isHtmlElement(e, "optgroup") &&
          !HtmlCommon.isHtmlElement(e, "option")) {
          return false;
        }
      }
      return false;
    }

    private bool hasHtmlElementInTableScope(string valueName) {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (HtmlCommon.isHtmlElement(e, valueName)) {
          return true;
        }
        if (HtmlCommon.isHtmlElement(e, "html") ||
          HtmlCommon.isHtmlElement(e, "table")) {
          return false;
        }
      }
      return false;
    }

    private bool hasHtmlHeaderElementInScope() {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (HtmlCommon.isHtmlElement(e, "h1") ||
      HtmlCommon.isHtmlElement(e, "h2") || HtmlCommon.isHtmlElement(e, "h3"
) || HtmlCommon.isHtmlElement(e, "h4") || HtmlCommon.isHtmlElement(e, "h5"
) || HtmlCommon.isHtmlElement(e, "h6")) {
          return true;
        }
        if (HtmlCommon.isHtmlElement(e, "applet") ||
          HtmlCommon.isHtmlElement(e, "caption") ||
  HtmlCommon.isHtmlElement(e, "html") || HtmlCommon.isHtmlElement(e, "table"
) || HtmlCommon.isHtmlElement(e, "td") || HtmlCommon.isHtmlElement(e, "th"
) || HtmlCommon.isHtmlElement(e, "marquee") ||
              HtmlCommon.isHtmlElement(e, "object") ||
  HtmlCommon.isMathMLElement(e, "mi") || HtmlCommon.isMathMLElement(e, "mo"
) ||
  HtmlCommon.isMathMLElement(e, "mn") || HtmlCommon.isMathMLElement(e, "ms"
) || HtmlCommon.isMathMLElement(e, "mtext") ||
              HtmlCommon.isMathMLElement(e, "annotation-xml") ||
            HtmlCommon.isSvgElement(e, "foreignObject") ||
              HtmlCommon.isSvgElement(e, "desc") ||
            HtmlCommon.isSvgElement(e, "title")) {
          return false;
        }
      }
      return false;
    }

    private bool hasNativeElementInScope() {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        // DebugUtility.Log("%s %s",e.getLocalName(),e.getNamespaceURI());
        if (e.getNamespaceURI().Equals(HtmlCommon.HTML_NAMESPACE) ||
            this.isMathMLTextIntegrationPoint(e) ||
              this.isHtmlIntegrationPoint(e)) {
          return true;
        }
        if (HtmlCommon.isHtmlElement(e, "applet") ||
            HtmlCommon.isHtmlElement(e, "caption") ||
              HtmlCommon.isHtmlElement(e, "html") ||
   HtmlCommon.isHtmlElement(e, "table") || HtmlCommon.isHtmlElement(e, "td"
) ||
  HtmlCommon.isHtmlElement(e, "th") || HtmlCommon.isHtmlElement(e, "marquee"
) ||
  HtmlCommon.isHtmlElement(e, "object") || HtmlCommon.isMathMLElement(e, "mi"
) ||
  HtmlCommon.isMathMLElement(e, "mo") || HtmlCommon.isMathMLElement(e, "mn"
) || HtmlCommon.isMathMLElement(e, "ms") ||
              HtmlCommon.isMathMLElement(e, "mtext") ||
            HtmlCommon.isMathMLElement(e, "annotation-xml") ||
            HtmlCommon.isSvgElement(e, "foreignObject") ||
      HtmlCommon.isSvgElement(
  e,
  "desc") || HtmlCommon.isSvgElement(
  e,
  "title")
) {
          return false;
        }
      }
      return false;
    }

    private void initialize() {
      this.noforeign = false;
      this.valueDocument = new Document();
      this.valueDocument.Address = this.address;
      this.valueDocument.setBaseURI(this.address);
      this.context = null;
      this.openElements.Clear();
      this.error = false;
      this.baseurl = null;
      this.hasForeignContent = false;  // performance optimization
      this.lastState = TokenizerState.Data;
      this.lastComment = null;
      this.docTypeToken = null;
      this.tokens.Clear();
      this.lastStartTag = null;
      this.currentEndTag = null;
      this.currentTag = null;
      this.currentAttribute = null;
      this.bogusCommentCharacter = 0;
      this.tempBuilder.Remove(0, this.tempBuilder.Length);
      this.state = TokenizerState.Data;
      this.framesetOk = true;
      this.integrationElements.Clear();
      this.tokenQueue.Clear();
      this.insertionMode = InsertionMode.Initial;
      this.originalInsertionMode = InsertionMode.Initial;
      this.formattingElements.Clear();
      this.doFosterParent = false;
      this.headElement = null;
      this.formElement = null;
      this.inputElement = null;
      this.done = false;
      this.pendingTableCharacters.Remove(0, this.pendingTableCharacters.Length);
    }

    private void insertCharacter(INode node, int ch) {
      Text textNode = this.getTextNodeToInsert(node);
      if (textNode != null) {
        StringBuilder builder = textNode.ValueText;
        if (ch <= 0xffff) {
          builder.Append((char)ch);
        } else if (ch <= 0x10ffff) {
          builder.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) + 0xd800));
          builder.Append((char)(((ch - 0x10000) & 0x3ff) + 0xdc00));
        }
      }
    }

    private Element insertForeignElement(StartTagToken tag, string _namespace) {
      Element valueElement = Element.fromToken(tag, _namespace);
      string xmlns = valueElement.getAttributeNS(
  HtmlCommon.XMLNS_NAMESPACE,
  "xmlns");
      string xlink = valueElement.getAttributeNS(
  HtmlCommon.XMLNS_NAMESPACE,
  "xlink");
      if (xmlns != null && !xmlns.Equals(_namespace)) {
        this.error = true;
      }
      if (xlink != null && !xlink.Equals(HtmlCommon.XLINK_NAMESPACE)) {
        this.error = true;
      }
      IElement currentNode = this.getCurrentNode();
      if (currentNode != null) {
        this.insertInCurrentNode(valueElement);
      } else {
        this.valueDocument.appendChild(valueElement);
      }
      this.openElements.Add(valueElement);
      return valueElement;
    }

    private void insertFormattingMarker(
  StartTagToken tag,
  Element addHtmlElement) {
      var fe = new FormattingElement();
      fe.ValueMarker = true;
      fe.Element = addHtmlElement;
      fe.Token = tag;
      this.formattingElements.Add(fe);
    }

    private void insertInCurrentNode(Node valueElement) {
      IElement node = this.getCurrentNode();
      if (this.doFosterParent) {
        string valueName = node.getLocalName();
        if ("table".Equals(valueName) || "tbody".Equals(valueName) ||
            "tfoot".Equals(valueName) || "thead".Equals(valueName) ||
            "tr".Equals(valueName)) {
          this.fosterParent(valueElement);
        } else {
          node.appendChild(valueElement);
        }
      } else {
        node.appendChild(valueElement);
      }
    }

    private void insertString(INode node, string str) {
      Text textNode = this.getTextNodeToInsert(node);
      if (textNode != null) {
        textNode.ValueText.Append(str);
      }
    }

    private bool isAppropriateEndTag() {
      if (this.lastStartTag == null || this.currentEndTag == null) {
        return false;
      }
      // DebugUtility.Log("lastStartTag=%s",lastStartTag.getName());
      // DebugUtility.Log("currentEndTag=%s",currentEndTag.getName());
      return this.currentEndTag.getName().Equals(this.lastStartTag.getName());
    }

    public bool isError() {
      return this.error;
    }

    private bool isForeignContext(int valueToken) {
      if (valueToken == TOKEN_EOF) {
 return false;
}
      if (this.openElements.Count == 0) {
 return false;
}
      IElement valueElement = (this.context != null &&
          this.openElements.Count == 1) ?
            this.context : this.getCurrentNode();  // adjusted current node
      if (valueElement == null) {
        return false;
      }
      if (valueElement.getNamespaceURI().Equals(HtmlCommon.HTML_NAMESPACE)) {
        return false;
      }
      if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        var tag = (StartTagToken)this.getToken(valueToken);
        string valueName = valueElement.getLocalName();
        if (this.isMathMLTextIntegrationPoint(valueElement)) {
          string tokenName = tag.getName();
          if (!"mglyph".Equals(tokenName) &&
              !"malignmark".Equals(tokenName)) {
            return false;
          }
        }
        bool annotationSVG =
          HtmlCommon.MATHML_NAMESPACE.Equals(valueElement.getNamespaceURI()) &&
            valueName.Equals("annotation-xml") && "svg"
            .Equals(tag.getName());
        return !annotationSVG && !this.isHtmlIntegrationPoint(valueElement);
      } else if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        return !this.isMathMLTextIntegrationPoint(valueElement) &&
          !this.isHtmlIntegrationPoint(valueElement);
      } else {
        return true;
      }
    }

    private bool isHtmlIntegrationPoint(IElement valueElement) {
      if (this.integrationElements.Contains(valueElement)) {
        return true;
      }
      string valueName = valueElement.getLocalName();
return HtmlCommon.SVG_NAMESPACE.Equals(valueElement.getNamespaceURI()) && (
                    valueName.Equals("foreignObject") || valueName.Equals(
        "desc") || valueName.Equals("title"));
    }

    private bool isMathMLTextIntegrationPoint(IElement valueElement) {
      string valueName = valueElement.getLocalName();
return HtmlCommon.MATHML_NAMESPACE.Equals(valueElement.getNamespaceURI()) && (
                    valueName.Equals("mi") || valueName.Equals("mo") ||
                    valueName.Equals("mn") || valueName.Equals("ms") ||
                    valueName.Equals("mtext"));
    }

    private bool isSpecialElement(IElement node) {
      if (HtmlCommon.isHtmlElement(node, "address") ||
        HtmlCommon.isHtmlElement(node, "applet") ||
        HtmlCommon.isHtmlElement(node, "area") ||
          HtmlCommon.isHtmlElement(node, "article") ||
        HtmlCommon.isHtmlElement(node, "aside") ||
          HtmlCommon.isHtmlElement(node, "base") ||
        HtmlCommon.isHtmlElement(node, "basefont") ||
          HtmlCommon.isHtmlElement(node, "bgsound") ||
        HtmlCommon.isHtmlElement(node, "blockquote") ||
          HtmlCommon.isHtmlElement(node, "body") ||
        HtmlCommon.isHtmlElement(node, "br") ||
          HtmlCommon.isHtmlElement(node, "button") ||
        HtmlCommon.isHtmlElement(node, "caption") ||
          HtmlCommon.isHtmlElement(node, "center") ||
        HtmlCommon.isHtmlElement(node, "col") ||
          HtmlCommon.isHtmlElement(node, "colgroup") ||
        HtmlCommon.isHtmlElement(node, "dd") ||
          HtmlCommon.isHtmlElement(node, "details") ||
        HtmlCommon.isHtmlElement(node, "dir") ||
          HtmlCommon.isHtmlElement(node, "div") ||
  HtmlCommon.isHtmlElement(node, "dl") || HtmlCommon.isHtmlElement(node, "dt"
) || HtmlCommon.isHtmlElement(node, "embed") ||
          HtmlCommon.isHtmlElement(node, "fieldset") ||
        HtmlCommon.isHtmlElement(node, "figcaption") ||
          HtmlCommon.isHtmlElement(node, "figure") ||
          HtmlCommon.isHtmlElement(node, "footer") ||
            HtmlCommon.isHtmlElement(node, "form") ||
            HtmlCommon.isHtmlElement(node, "frame") ||
              HtmlCommon.isHtmlElement(node, "frameset") ||
  HtmlCommon.isHtmlElement(node, "h1") || HtmlCommon.isHtmlElement(node, "h2"
) ||
  HtmlCommon.isHtmlElement(node, "h3") || HtmlCommon.isHtmlElement(node, "h4"
) ||
  HtmlCommon.isHtmlElement(node, "h5") || HtmlCommon.isHtmlElement(node, "h6"
) || HtmlCommon.isHtmlElement(node, "head") ||
              HtmlCommon.isHtmlElement(node, "header") ||
            HtmlCommon.isHtmlElement(node, "hgroup") ||
              HtmlCommon.isHtmlElement(node, "hr") ||
            HtmlCommon.isHtmlElement(node, "html") ||
              HtmlCommon.isHtmlElement(node, "iframe") ||
            HtmlCommon.isHtmlElement(node, "img") ||
              HtmlCommon.isHtmlElement(node, "input") ||
            HtmlCommon.isHtmlElement(node, "isindex") ||
              HtmlCommon.isHtmlElement(node, "li") ||
            HtmlCommon.isHtmlElement(node, "link") ||
          HtmlCommon.isHtmlElement(node, "listing") ||
            HtmlCommon.isHtmlElement(node, "main") ||
            HtmlCommon.isHtmlElement(node, "marquee") ||
              HtmlCommon.isHtmlElement(node, "menu") ||
            HtmlCommon.isHtmlElement(node, "menuitem") ||
              HtmlCommon.isHtmlElement(node, "meta") ||
            HtmlCommon.isHtmlElement(node, "nav") ||
              HtmlCommon.isHtmlElement(node, "noembed") ||
            HtmlCommon.isHtmlElement(node, "noframes") ||
              HtmlCommon.isHtmlElement(node, "noscript") ||
            HtmlCommon.isHtmlElement(node, "object") ||
              HtmlCommon.isHtmlElement(node, "ol") ||
            HtmlCommon.isHtmlElement(node, "p") ||
              HtmlCommon.isHtmlElement(node, "param") ||
            HtmlCommon.isHtmlElement(node, "plaintext") ||
              HtmlCommon.isHtmlElement(node, "pre") ||
            HtmlCommon.isHtmlElement(node, "script") ||
              HtmlCommon.isHtmlElement(node, "section") ||
          HtmlCommon.isHtmlElement(node, "select") ||
            HtmlCommon.isHtmlElement(node, "source") ||
            HtmlCommon.isHtmlElement(node, "style") ||
              HtmlCommon.isHtmlElement(node, "summary") ||
            HtmlCommon.isHtmlElement(node, "table") ||
              HtmlCommon.isHtmlElement(node, "tbody") ||
            HtmlCommon.isHtmlElement(node, "td") ||
              HtmlCommon.isHtmlElement(node, "textarea") ||
            HtmlCommon.isHtmlElement(node, "tfoot") ||
              HtmlCommon.isHtmlElement(node, "th") ||
            HtmlCommon.isHtmlElement(node, "thead") ||
              HtmlCommon.isHtmlElement(node, "title") ||
            HtmlCommon.isHtmlElement(node, "tr") ||
              HtmlCommon.isHtmlElement(node, "track") ||
            HtmlCommon.isHtmlElement(node, "ul") ||
              HtmlCommon.isHtmlElement(node, "wbr") ||
            HtmlCommon.isHtmlElement(node, "xmp")) {
        return true;
      }
      if (HtmlCommon.isMathMLElement(node, "mi") ||
        HtmlCommon.isMathMLElement(node, "mo") ||
        HtmlCommon.isMathMLElement(node, "mn") ||
          HtmlCommon.isMathMLElement(node, "ms") ||
  HtmlCommon.isMathMLElement(node, "mtext") ||
       HtmlCommon.isMathMLElement(
  node,
  "annotation-xml")) {
        return true;
      }
      return (HtmlCommon.isSvgElement(node, "foreignObject") ||
        HtmlCommon.isSvgElement(
  node,
  "desc") || HtmlCommon.isSvgElement(node, "title")) ? (true) : false; }
    internal string nodesToDebugString(IList<Node> nodes) {
      var builder = new StringBuilder();
      foreach (var node in nodes) {
        string str = node.toDebugString();
        string[] strarray = StringUtility.splitAt(str, "\n");
        int len = strarray.Length;
        if (len > 0 && strarray[len - 1].Length == 0) {
          --len;  // ignore trailing empty string
        }
        for (int i = 0; i < len; ++i) {
          string el = strarray[i];
          builder.Append("| ");
          builder.Append(el.Replace("~~~~", "\n"));
          builder.Append("\n");
        }
      }
      return builder.ToString();
    }

    public IDocument parse() {
      while (true) {
        int valueToken = this.parserRead();
        this.applyInsertionMode(valueToken, null);
        if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
          var tag = (StartTagToken)this.getToken(valueToken);
          // DebugUtility.Log(tag);
          if (!tag.isAckSelfClosing()) {
            this.error = true;
          }
        }
        // DebugUtility.Log("valueToken=%08X, insertionMode=%s, error=%s"
        // , valueToken, insertionMode, error);
        if (this.done) {
          break;
        }
      }
      return this.valueDocument;
    }

    private int parseCharacterReference(int allowedCharacter) {
      int markStart = this.charInput.setSoftMark();
      int c1 = this.charInput.ReadChar();
      if (c1 < 0 || c1 == 0x09 || c1 == 0x0a || c1 == 0x0c ||
          c1 == 0x20 || c1 == 0x3c || c1 == 0x26 || (allowedCharacter >= 0 &&
            c1 == allowedCharacter)) {
        this.charInput.setMarkPosition(markStart);
        return 0x26;  // emit ampersand
      } else if (c1 == 0x23) {
        c1 = this.charInput.ReadChar();
        var value = 0;
        var haveHex = false;
        if (c1 == 0x78 || c1 == 0x58) {
          // Hex number
          while (true) {  // skip zeros
            int c = this.charInput.ReadChar();
            if (c != '0') {
              if (c >= 0) {
                this.charInput.moveBack(1);
              }
              break;
            }
            haveHex = true;
          }
          var overflow = false;
          while (true) {
            int number = this.charInput.ReadChar();
            if (number >= '0' && number <= '9') {
              if (!overflow) {
                value = (value << 4) + (number - '0');
              }
              haveHex = true;
            } else if (number >= 'a' && number <= 'f') {
              if (!overflow) {
                value = (value << 4) + (number - 'a') + 10;
              }
              haveHex = true;
            } else if (number >= 'A' && number <= 'F') {
              if (!overflow) {
                value = (value << 4) + (number - 'A') + 10;
              }
              haveHex = true;
            } else {
              if (number >= 0) {
                // move back character (except if it's EOF)
                this.charInput.moveBack(1);
              }
              break;
            }
            if (value > 0x10ffff) {
              value = 0x110000; overflow = true;
            }
          }
        } else {
          if (c1 > 0) {
            this.charInput.moveBack(1);
          }
          // Digits
          while (true) {  // skip zeros
            int c = this.charInput.ReadChar();
            if (c != '0') {
              if (c >= 0) {
                this.charInput.moveBack(1);
              }
              break;
            }
            haveHex = true;
          }
          var overflow = false;
          while (true) {
            int number = this.charInput.ReadChar();
            if (number >= '0' && number <= '9') {
              if (!overflow) {
                value = (value * 10) + (number - '0');
              }
              haveHex = true;
            } else {
              if (number >= 0) {
                // move back character (except if it's EOF)
                this.charInput.moveBack(1);
              }
              break;
            }
            if (value > 0x10ffff) {
              value = 0x110000; overflow = true;
            }
          }
        }
        if (!haveHex) {
          // No digits: parse error
          this.error = true;
          this.charInput.setMarkPosition(markStart);
          return 0x26;  // emit ampersand
        }
        c1 = this.charInput.ReadChar();
        if (c1 != 0x3b) {  // semicolon
          this.error = true;
          if (c1 >= 0) {
            this.charInput.moveBack(1);  // parse error
          }
        }
        if (value > 0x10ffff || ((value & 0xf800) == 0xd800)) {
          this.error = true;
          value = 0xfffd;  // parse error
        } else if (value >= 0x80 && value < 0xa0) {
          this.error = true;
          // parse error
          var replacements = new int[] { 0x20ac, 0x81, 0x201a, 0x192,
            0x201e, 0x2026, 0x2020, 0x2021, 0x2c6, 0x2030, 0x160, 0x2039,
            0x152, 0x8d, 0x17d, 0x8f, 0x90, 0x2018, 0x2019, 0x201c, 0x201d,
            0x2022, 0x2013, 0x2014, 0x2dc, 0x2122, 0x161, 0x203a, 0x153,
            0x9d, 0x17e, 0x178 };
          value = replacements[value - 0x80];
        } else if (value == 0x0d) {
          // parse error
          this.error = true;
        } else if (value == 0x00) {
          // parse error
          this.error = true;
          value = 0xfffd;
        }
        if (value == 0x08 || value == 0x0b ||
            (value & 0xfffe) == 0xfffe || (value >= 0x0e && value <= 0x1f) ||
            value == 0x7f || (value >= 0xfdd0 && value <= 0xfdef)) {
          // parse error
          this.error = true;
        }
        return value;
      } else if ((c1 >= 'A' && c1 <= 'Z') || (c1 >= 'a' && c1 <= 'z') ||
          (c1 >= '0' && c1 <= '9')) {
        int[] data = null;
        // check for certain well-known entities
        if (c1 == 'g') {
          if (this.charInput.ReadChar() == 't' && this.charInput.ReadChar() ==
               ';'
) {
            return '>';
          }
          this.charInput.setMarkPosition(markStart + 1);
        } else if (c1 == 'l') {
          if (this.charInput.ReadChar() == 't' && this.charInput.ReadChar() ==
               ';'
) {
            return '<';
          }
          this.charInput.setMarkPosition(markStart + 1);
        } else if (c1 == 'a') {
    if (this.charInput.ReadChar() == 'm' && this.charInput.ReadChar() == 'p' &&
            this.charInput.ReadChar() == ';') {
            return '&';
          }
          this.charInput.setMarkPosition(markStart + 1);
        } else if (c1 == 'n') {
          if (this.charInput.ReadChar() == 'b' && this.charInput.ReadChar() ==
               's' &&
              this.charInput.ReadChar() == 'p' && this.charInput.ReadChar() ==
                   ';'
) {
            return 0xa0;
          }
          this.charInput.setMarkPosition(markStart + 1);
        }
        var count = 0;
        for (int index = 0; index < HtmlEntities.Entities.Length; ++index) {
          string entity = HtmlEntities.Entities[index];
          if (entity[0] == c1) {
            if (data == null) {
              // Read the rest of the character reference
              // (the entities are sorted by length, so
              // we get the maximum length possible starting
              // with the first matching character)
              data = new int[entity.Length - 1];
              count = this.charInput.Read(data, 0, data.Length);
              // DebugUtility.Log("markposch=%c",(char)data[0]);
            }
            // if fewer bytes were read than the
            // entity's remaining length, this
            // can't match
            // DebugUtility.Log("data count=%s %s"
            // , count, stream.getMarkPosition());
            if (count < entity.Length - 1) {
              continue;
            }
            var matched = true;
            for (int i = 1; i < entity.Length; ++i) {
              // DebugUtility.Log("%c %c | markpos=%d",
              // (char)data[i-1], entity[i], stream.getMarkPosition());
              if (data[i - 1] != entity[i]) {
                matched = false;
                break;
              }
            }
            if (matched) {
              // Move back the difference between the
              // number of bytes actually read and
              // this entity's length
              this.charInput.moveBack(count - (entity.Length - 1));
              // DebugUtility.Log("lastchar=%c",entity[entity.Length-1]);
              if (allowedCharacter >= 0 && entity[entity.Length - 1] != ';') {
                // Get the next character after the entity
                int ch2 = this.charInput.ReadChar();
                if (ch2 == '=' || (ch2 >= 'A' && ch2 <= 'Z') ||
                  (ch2 >= 'a' && ch2 <= 'z') || (ch2 >= '0' && ch2 <= '9')) {
                  if (ch2 == '=') {
                    this.error = true;
                  }
                  this.charInput.setMarkPosition(markStart);
                  return 0x26;  // return ampersand rather than entity
                } else {
                  if (ch2 >= 0) {
                    this.charInput.moveBack(1);
                  }
                  if (entity[entity.Length - 1] != ';') {
                    this.error = true;
                  }
                }
              } else {
                if (entity[entity.Length - 1] != ';') {
                  this.error = true;
                }
              }
              return HtmlEntities.EntityValues[index];
            }
          }
        }
        // no match
        this.charInput.setMarkPosition(markStart);
        while (true) {
          int ch2 = this.charInput.ReadChar();
          if (ch2 == ';') {
            this.error = true;
            break;
          } else if (!((ch2 >= 'A' && ch2 <= 'Z') || (ch2 >= 'a' && ch2 <= 'z'
) || (ch2 >= '0' && ch2 <= '9'))) {
            break;
          }
        }
        this.charInput.setMarkPosition(markStart);
        return 0x26;
      } else {
        // not a character reference
        this.charInput.setMarkPosition(markStart);
        return 0x26;  // emit ampersand
      }
    }

    public IList<INode> parseFragment(Element context) {
      if (context == null) {
        throw new ArgumentException();
      }
      this.initialize();
      this.valueDocument = new Document();
      INode ownerDocument = context;
      INode lastForm = null;
      while (ownerDocument != null) {
        if (lastForm == null && ownerDocument.getNodeType() ==
                NodeType.ELEMENT_NODE) {
          string valueName = ((Element)ownerDocument).getLocalName();
          if (valueName.Equals("form")) {
            lastForm = ownerDocument;
          }
        }
        ownerDocument = ownerDocument.getParentNode();
        if (ownerDocument == null ||
            ownerDocument.getNodeType() == NodeType.DOCUMENT_NODE) {
          break;
        }
      }
      Document ownerDoc = null;
      if (ownerDocument != null &&
            ownerDocument.getNodeType() == NodeType.DOCUMENT_NODE) {
        ownerDoc = (Document)ownerDocument;
        this.valueDocument.setMode(ownerDoc.getMode());
      }
      string name2 = context.getLocalName();
      this.state = TokenizerState.Data;
      if (name2.Equals("title") || name2.Equals("textarea")) {
        this.state = TokenizerState.RcData;
      } else if (name2.Equals("style") || name2.Equals("xmp") ||
          name2.Equals("iframe") || name2.Equals("noembed") ||
          name2.Equals("noframes")) {
        this.state = TokenizerState.RawText;
      } else if (name2.Equals("script")) {
        this.state = TokenizerState.ScriptData;
      } else if (name2.Equals("noscript")) {
        this.state = TokenizerState.Data;
      } else if (name2.Equals("plaintext")) {
        this.state = TokenizerState.PlainText;
      }
      var valueElement = new Element();
      valueElement.setLocalName("html");
      valueElement.setNamespace(HtmlCommon.HTML_NAMESPACE);
      this.valueDocument.appendChild(valueElement);
      this.done = false;
      this.openElements.Clear();
      this.openElements.Add(valueElement);
      this.context = context;
      this.resetInsertionMode();
      this.formElement = (lastForm == null) ? null : ((Element)lastForm);
      if (this.encoding.getConfidence() != EncodingConfidence.Irrelevant) {
        this.encoding = new EncodingConfidence(
  this.encoding.getEncoding(),
  EncodingConfidence.Irrelevant);
      }
      this.parse();
      return new List<INode>(valueElement.getChildNodes());
    }

    public IList<INode> parseFragment(string contextName) {
      var valueElement = new Element();
      valueElement.setLocalName(contextName);
      valueElement.setNamespace(HtmlCommon.HTML_NAMESPACE);
      return this.parseFragment(valueElement);
    }

    internal int parserRead() {
      int valueToken = this.parserReadInternal();
      // DebugUtility.Log("valueToken=%08X [%c]",valueToken,valueToken&0xFF);
      if (valueToken <= -2) {
        this.error = true;
        return 0xfffd;
      }
      return valueToken;
    }

    private int parserReadInternal() {
      if (this.tokenQueue.Count > 0) {
        return removeAtIndex(this.tokenQueue, 0);
      }
      while (true) {
        // DebugUtility.Log(state);
        switch (this.state) {
          case TokenizerState.Data:
            int c = this.charInput.ReadChar();
            if (c == 0x26) {
              this.state = TokenizerState.CharacterRefInData;
            } else if (c == 0x3c) {
              state = TokenizerState.TagOpen;
            } else if (c == 0) {
              error = true;
              return c;
            } else if (c < 0) {
              return TOKEN_EOF;
            } else {
              int ret = c;
              // Keep reading characters to
              // reduce the need to re-call
              // this method
              int mark = charInput.setSoftMark();
              for (int i = 0; i < 100; ++i) {
                c = charInput.ReadChar();
                if (c > 0 && c != 0x26 && c != 0x3c) {
                  tokenQueue.Add(c);
                } else {
                  charInput.setMarkPosition(mark + i);
                  break;
                }
              }
              return ret;
            }
            break;
          case TokenizerState.CharacterRefInData: {
              this.state = TokenizerState.Data;
              int charref = this.parseCharacterReference(-1);
              if (charref < 0) {
                // more than one character in this reference
                int index = Math.Abs(charref + 1);
                this.tokenQueue.Add(HtmlEntities.EntityDoubles[(index * 2) +
                    1]);
                return HtmlEntities.EntityDoubles[index * 2];
              }
              return charref;
            }
          case TokenizerState.CharacterRefInRcData: {
              this.state = TokenizerState.RcData;
              int charref = this.parseCharacterReference(-1);
              if (charref < 0) {
                // more than one character in this reference
                int index = Math.Abs(charref + 1);
                this.tokenQueue.Add(HtmlEntities.EntityDoubles[(index * 2) +
                    1]);
                return HtmlEntities.EntityDoubles[index * 2];
              }
              return charref;
            }
          case TokenizerState.RcData:
            int c1 = this.charInput.ReadChar();
            if (c1 == 0x26) {
              this.state = TokenizerState.CharacterRefInRcData;
            } else if (c1 == 0x3c) {
              state = TokenizerState.RcDataLessThan;
            } else if (c1 == 0) {
              error = true;
              return 0xfffd;
            } else if (c1 < 0) {
              return TOKEN_EOF;
            } else {
              return c1;
            }
            break;
          case TokenizerState.RawText:
          case TokenizerState.ScriptData: {
              int c11 = this.charInput.ReadChar();
              if (c11 == 0x3c) {
                this.state = (this.state == TokenizerState.RawText) ?
                    TokenizerState.RawTextLessThan :
                    TokenizerState.ScriptDataLessThan;
              } else if (c11 == 0) {
                this.error = true;
                return 0xfffd;
              } else if (c11 < 0) {
                return TOKEN_EOF;
              } else {
                return c11;
              }
              break;
            }
          case TokenizerState.ScriptDataLessThan: {
              this.charInput.setHardMark();
              int c11 = this.charInput.ReadChar();
              if (c11 == 0x2f) {
                this.tempBuilder.Remove(0, this.tempBuilder.Length);
                this.state = TokenizerState.ScriptDataEndTagOpen;
              } else if (c11 == 0x21) {
                this.state = TokenizerState.ScriptDataEscapeStart;
                this.tokenQueue.Add(0x21);
                return '<';
              } else {
                this.state = TokenizerState.ScriptData;
                if (c11 >= 0) {
                  this.charInput.moveBack(1);
                }
                return 0x3c;
              }
              break;
            }
          case TokenizerState.ScriptDataEndTagOpen:
          case TokenizerState.ScriptDataEscapedEndTagOpen: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              if (ch >= 'A' && ch <= 'Z') {
                var valueToken = new EndTagToken((char)(ch + 0x20));
                if (ch <= 0xffff) {
                  this.tempBuilder.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.tempBuilder.Append((char)((((ch - 0x10000) >> 10) &
                    0x3ff) + 0xd800));
                  this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) +
                    0xdc00));
                }
                this.currentTag = valueToken;
                this.currentEndTag = valueToken;
             this.state = (this.state == TokenizerState.ScriptDataEndTagOpen) ?
                    TokenizerState.ScriptDataEndTagName :
                    TokenizerState.ScriptDataEscapedEndTagName;
              } else if (ch >= 'a' && ch <= 'z') {
                var valueToken = new EndTagToken((char)ch);
                if (ch <= 0xffff) {
                  this.tempBuilder.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.tempBuilder.Append((char)((((ch - 0x10000) >> 10) &
                    0x3ff) + 0xd800));
                  this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) +
                    0xdc00));
                }
                this.currentTag = valueToken;
                this.currentEndTag = valueToken;
             this.state = (this.state == TokenizerState.ScriptDataEndTagOpen) ?
                    TokenizerState.ScriptDataEndTagName :
                    TokenizerState.ScriptDataEscapedEndTagName;
              } else {
                this.state = (this.state ==
                    TokenizerState.ScriptDataEndTagOpen) ?
                 TokenizerState.ScriptData : TokenizerState.ScriptDataEscaped;
                this.tokenQueue.Add(0x2f);
                if (ch >= 0) {
                  this.charInput.moveBack(1);
                }
                return 0x3c;
              }
              break;
            }
          case TokenizerState.ScriptDataEndTagName:
          case TokenizerState.ScriptDataEscapedEndTagName: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              if ((ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) &&
                  this.isAppropriateEndTag()) {
                this.state = TokenizerState.BeforeAttributeName;
              } else if (ch == 0x2f && this.isAppropriateEndTag()) {
                this.state = TokenizerState.SelfClosingStartTag;
              } else if (ch == 0x3e && this.isAppropriateEndTag()) {
                this.state = TokenizerState.Data;
                return this.emitCurrentTag();
              } else if (ch >= 'A' && ch <= 'Z') {
                this.currentTag.appendChar((char)(ch + 0x20));
                this.tempBuilder.Append((char)ch);
              } else if (ch >= 'a' && ch <= 'z') {
                this.currentTag.appendChar((char)ch);
                this.tempBuilder.Append((char)ch);
              } else {
                this.state = (this.state ==
                    TokenizerState.ScriptDataEndTagName) ?
                 TokenizerState.ScriptData : TokenizerState.ScriptDataEscaped;
                this.tokenQueue.Add(0x2f);
                string tbs = this.tempBuilder.ToString();
                for (int i = 0; i < tbs.Length; ++i) {
                  int c2 = DataUtilities.CodePointAt(tbs, i);
                  if (c2 >= 0x10000) {
                    ++i;
                  }
                  this.tokenQueue.Add(c2);
                }
                if (ch >= 0) {
                  this.charInput.moveBack(1);
                }
                return '<';
              }
              break;
            }
          case TokenizerState.ScriptDataDoubleEscapeStart: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20 ||
                  ch == 0x2f || ch == 0x3e) {
                string bufferString = this.tempBuilder.ToString();
                this.state = bufferString.Equals("script") ?
                  TokenizerState.ScriptDataDoubleEscaped :
                  TokenizerState.ScriptDataEscaped;
                return ch;
              } else if (ch >= 'A' && ch <= 'Z') {
                if (ch + 0x20 <= 0xffff) {
                  this.tempBuilder.Append((char)(ch + 0x20));
                } else if (ch + 0x20 <= 0x10ffff) {
                  this.tempBuilder.Append((char)((((ch + 0x20 - 0x10000) >>
                  10) & 0x3ff) + 0xd800));
                  this.tempBuilder.Append((char)(((ch + 0x20 - 0x10000) &
                   0x3ff) + 0xdc00));
                }
                return ch;
              } else if (ch >= 'a' && ch <= 'z') {
                if (ch <= 0xffff) {
                  this.tempBuilder.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.tempBuilder.Append((char)((((ch - 0x10000) >> 10) &
                    0x3ff) + 0xd800));
                  this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) +
                    0xdc00));
                }
                return ch;
              } else {
                this.state = TokenizerState.ScriptDataEscaped;
                if (ch >= 0) {
                  this.charInput.moveBack(1);
                }
              }
              break;
            }
          case TokenizerState.ScriptDataDoubleEscapeEnd: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20 ||
                  ch == 0x2f || ch == 0x3e) {
                string bufferString = this.tempBuilder.ToString();
                this.state = bufferString.Equals("script") ?
                  TokenizerState.ScriptDataEscaped :
                  TokenizerState.ScriptDataDoubleEscaped;
                return ch;
              } else if (ch >= 'A' && ch <= 'Z') {
                if (ch + 0x20 <= 0xffff) {
                  this.tempBuilder.Append((char)(ch + 0x20));
                } else if (ch + 0x20 <= 0x10ffff) {
                  this.tempBuilder.Append((char)((((ch + 0x20 - 0x10000) >>
                  10) & 0x3ff) + 0xd800));
                  this.tempBuilder.Append((char)(((ch + 0x20 - 0x10000) &
                   0x3ff) + 0xdc00));
                }
                return ch;
              } else if (ch >= 'a' && ch <= 'z') {
                if (ch <= 0xffff) {
                  this.tempBuilder.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.tempBuilder.Append((char)((((ch - 0x10000) >> 10) &
                    0x3ff) + 0xd800));
                  this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) +
                    0xdc00));
                }
                return ch;
              } else {
                this.state = TokenizerState.ScriptDataDoubleEscaped;
                if (ch >= 0) {
                  this.charInput.moveBack(1);
                }
              }
              break;
            }
          case TokenizerState.ScriptDataEscapeStart:
          case TokenizerState.ScriptDataEscapeStartDash: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              if (ch == 0x2d) {
                this.state = (this.state ==
                    TokenizerState.ScriptDataEscapeStart) ?
                    TokenizerState.ScriptDataEscapeStartDash :
                    TokenizerState.ScriptDataEscapedDashDash;
                return '-';
              } else {
                if (ch >= 0) {
                  this.charInput.moveBack(1);
                }
                this.state = TokenizerState.ScriptData;
              }
              break;
            }
          case TokenizerState.ScriptDataEscaped: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x2d) {
                this.state = TokenizerState.ScriptDataEscapedDash;
                return '-';
              } else if (ch == 0x3c) {
                this.state = TokenizerState.ScriptDataEscapedLessThan;
              } else if (ch == 0) {
                this.error = true;
                return 0xfffd;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                return ch;
              }
              break;
            }
          case TokenizerState.ScriptDataDoubleEscaped: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x2d) {
                this.state = TokenizerState.ScriptDataDoubleEscapedDash;
                return '-';
              } else if (ch == 0x3c) {
                this.state = TokenizerState.ScriptDataDoubleEscapedLessThan;
                return '<';
              } else if (ch == 0) {
                this.error = true;
                return 0xfffd;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                return ch;
              }
              break;
            }
          case TokenizerState.ScriptDataEscapedDash: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x2d) {
                this.state = TokenizerState.ScriptDataEscapedDashDash;
                return '-';
              } else if (ch == 0x3c) {
                this.state = TokenizerState.ScriptDataEscapedLessThan;
              } else if (ch == 0) {
                this.error = true;
                this.state = TokenizerState.ScriptDataEscaped;
                return 0xfffd;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                this.state = TokenizerState.ScriptDataEscaped;
                return ch;
              }
              break;
            }
          case TokenizerState.ScriptDataDoubleEscapedDash: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x2d) {
                this.state = TokenizerState.ScriptDataDoubleEscapedDashDash;
                return '-';
              } else if (ch == 0x3c) {
                this.state = TokenizerState.ScriptDataDoubleEscapedLessThan;
                return '<';
              } else if (ch == 0) {
                this.error = true;
                this.state = TokenizerState.ScriptDataDoubleEscaped;
                return 0xfffd;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                this.state = TokenizerState.ScriptDataDoubleEscaped;
                return ch;
              }
              break;
            }
          case TokenizerState.ScriptDataEscapedDashDash: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x2d) {
                return '-';
              } else if (ch == 0x3c) {
                this.state = TokenizerState.ScriptDataEscapedLessThan;
              } else if (ch == 0x3e) {
                this.state = TokenizerState.ScriptData;
                return '>';
              } else if (ch == 0) {
                this.error = true;
                this.state = TokenizerState.ScriptDataEscaped;
                return 0xfffd;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                this.state = TokenizerState.ScriptDataEscaped;
                return ch;
              }
              break;
            }
          case TokenizerState.ScriptDataDoubleEscapedDashDash: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x2d) {
                return '-';
              } else if (ch == 0x3c) {
                this.state = TokenizerState.ScriptDataDoubleEscapedLessThan;
                return '<';
              } else if (ch == 0x3e) {
                this.state = TokenizerState.ScriptData;
                return '>';
              } else if (ch == 0) {
                this.error = true;
                this.state = TokenizerState.ScriptDataDoubleEscaped;
                return 0xfffd;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                this.state = TokenizerState.ScriptDataDoubleEscaped;
                return ch;
              }
              break;
            }
          case TokenizerState.ScriptDataDoubleEscapedLessThan: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              if (ch == 0x2f) {
                this.tempBuilder.Remove(0, this.tempBuilder.Length);
                this.state = TokenizerState.ScriptDataDoubleEscapeEnd;
                return 0x2f;
              } else {
                this.state = TokenizerState.ScriptDataDoubleEscaped;
                if (ch >= 0) {
                  this.charInput.moveBack(1);
                }
              }
              break;
            }
          case TokenizerState.ScriptDataEscapedLessThan: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              if (ch == 0x2f) {
                this.tempBuilder.Remove(0, this.tempBuilder.Length);
                this.state = TokenizerState.ScriptDataEscapedEndTagOpen;
              } else if (ch >= 'A' && ch <= 'Z') {
                this.tempBuilder.Remove(0, this.tempBuilder.Length);
                this.tempBuilder.Append((char)(ch + 0x20));
                this.state = TokenizerState.ScriptDataDoubleEscapeStart;
                this.tokenQueue.Add(ch);
                return 0x3c;
              } else if (ch >= 'a' && ch <= 'z') {
                this.tempBuilder.Remove(0, this.tempBuilder.Length);
                this.tempBuilder.Append((char)ch);
                this.state = TokenizerState.ScriptDataDoubleEscapeStart;
                this.tokenQueue.Add(ch);
                return 0x3c;
              } else {
                this.state = TokenizerState.ScriptDataEscaped;
                if (ch >= 0) {
                  this.charInput.moveBack(1);
                }
                return 0x3c;
              }
              break;
            }
          case TokenizerState.PlainText: {
              int c11 = this.charInput.ReadChar();
              if (c11 == 0) {
                this.error = true;
                return 0xfffd;
              } else if (c11 < 0) {
                return TOKEN_EOF;
              } else {
                return c11;
              }
            }
          case TokenizerState.TagOpen: {
              this.charInput.setHardMark();
              int c11 = this.charInput.ReadChar();
              if (c11 == 0x21) {
                this.state = TokenizerState.MarkupDeclarationOpen;
              } else if (c11 == 0x2f) {
                this.state = TokenizerState.EndTagOpen;
              } else if (c11 >= 'A' && c11 <= 'Z') {
                TagToken valueToken = new StartTagToken((char)(c11 + 0x20));
                this.currentTag = valueToken;
                this.state = TokenizerState.TagName;
              } else if (c11 >= 'a' && c11 <= 'z') {
                TagToken valueToken = new StartTagToken((char)c11);
                this.currentTag = valueToken;
                this.state = TokenizerState.TagName;
              } else if (c11 == 0x3f) {
                this.error = true;
                this.bogusCommentCharacter = c11;
                this.state = TokenizerState.BogusComment;
              } else {
                this.error = true;
                this.state = TokenizerState.Data;
                if (c11 >= 0) {
                  this.charInput.moveBack(1);
                }
                return '<';
              }
              break;
            }
          case TokenizerState.EndTagOpen: {
              int ch = this.charInput.ReadChar();
              if (ch >= 'A' && ch <= 'Z') {
                TagToken valueToken = new EndTagToken((char)(ch + 0x20));
                this.currentEndTag = valueToken;
                this.currentTag = valueToken;
                this.state = TokenizerState.TagName;
              } else if (ch >= 'a' && ch <= 'z') {
                TagToken valueToken = new EndTagToken((char)ch);
                this.currentEndTag = valueToken;
                this.currentTag = valueToken;
                this.state = TokenizerState.TagName;
              } else if (ch == 0x3e) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
                this.tokenQueue.Add(0x2f);  // solidus
                return 0x3c;  // Less than
              } else {
                this.error = true;
                this.bogusCommentCharacter = ch;
                this.state = TokenizerState.BogusComment;
              }
              break;
            }
          case TokenizerState.RcDataEndTagOpen:
          case TokenizerState.RawTextEndTagOpen: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              if (ch >= 'A' && ch <= 'Z') {
                TagToken valueToken = new EndTagToken((char)(ch + 0x20));
                if (ch <= 0xffff) {
                  this.tempBuilder.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.tempBuilder.Append((char)((((ch - 0x10000) >> 10) &
                    0x3ff) + 0xd800));
                  this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) +
                    0xdc00));
                }
                this.currentEndTag = valueToken;
                this.currentTag = valueToken;
                this.state = (this.state == TokenizerState.RcDataEndTagOpen) ?
                    TokenizerState.RcDataEndTagName :
                    TokenizerState.RawTextEndTagName;
              } else if (ch >= 'a' && ch <= 'z') {
                TagToken valueToken = new EndTagToken((char)ch);
                if (ch <= 0xffff) {
                  this.tempBuilder.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.tempBuilder.Append((char)((((ch - 0x10000) >> 10) &
                    0x3ff) + 0xd800));
                  this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) +
                    0xdc00));
                }
                this.currentEndTag = valueToken;
                this.currentTag = valueToken;
                this.state = (this.state == TokenizerState.RcDataEndTagOpen) ?
                    TokenizerState.RcDataEndTagName :
                    TokenizerState.RawTextEndTagName;
              } else {
                if (ch >= 0) {
                  this.charInput.moveBack(1);
                }
                this.state = TokenizerState.RcData;
                this.tokenQueue.Add(0x2f);  // solidus
                return 0x3c;  // Less than
              }
              break;
            }
          case TokenizerState.RcDataEndTagName:
          case TokenizerState.RawTextEndTagName: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              if ((ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) &&
                    this.isAppropriateEndTag()) {
                this.state = TokenizerState.BeforeAttributeName;
              } else if (ch == 0x2f && this.isAppropriateEndTag()) {
                this.state = TokenizerState.SelfClosingStartTag;
              } else if (ch == 0x3e && this.isAppropriateEndTag()) {
                this.state = TokenizerState.Data;
                return this.emitCurrentTag();
              } else if (ch >= 'A' && ch <= 'Z') {
                this.currentTag.append(ch + 0x20);
                if (ch + 0x20 <= 0xffff) {
                  this.tempBuilder.Append((char)(ch + 0x20));
                } else if (ch + 0x20 <= 0x10ffff) {
                  this.tempBuilder.Append((char)((((ch + 0x20 - 0x10000) >>
                  10) & 0x3ff) + 0xd800));
                  this.tempBuilder.Append((char)(((ch + 0x20 - 0x10000) &
                   0x3ff) + 0xdc00));
                }
              } else if (ch >= 'a' && ch <= 'z') {
                this.currentTag.append(ch);
                if (ch <= 0xffff) {
                  this.tempBuilder.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.tempBuilder.Append((char)((((ch - 0x10000) >> 10) &
                    0x3ff) + 0xd800));
                  this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) +
                    0xdc00));
                }
              } else {
                if (ch >= 0) {
                  this.charInput.moveBack(1);
                }
                this.state = (this.state == TokenizerState.RcDataEndTagName) ?
                    TokenizerState.RcData : TokenizerState.RawText;
                this.tokenQueue.Add(0x2f);  // solidus
                string tbs = this.tempBuilder.ToString();
                for (int i = 0; i < tbs.Length; ++i) {
                  int c2 = DataUtilities.CodePointAt(tbs, i);
                  if (c2 >= 0x10000) {
                    ++i;
                  }
                  this.tokenQueue.Add(c2);
                }
                return 0x3c;  // Less than
              }
              break;
            }
          case TokenizerState.BeforeAttributeName: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                // ignored
              } else if (ch == 0x2f) {
                this.state = TokenizerState.SelfClosingStartTag;
              } else if (ch == 0x3e) {
                this.state = TokenizerState.Data;
                return this.emitCurrentTag();
              } else if (ch >= 'A' && ch <= 'Z') {
                this.currentAttribute = this.currentTag.addAttribute((char)(ch +
                    0x20));
                this.state = TokenizerState.AttributeName;
              } else if (ch == 0) {
                this.error = true;
            this.currentAttribute = this.currentTag.addAttribute((char)0xfffd);
                this.state = TokenizerState.AttributeName;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                if (ch == 0x22 || ch == 0x27 || ch == 0x3c || ch == 0x3d) {
                  this.error = true;
                }
                this.currentAttribute = this.currentTag.addAttribute(ch);
                this.state = TokenizerState.AttributeName;
              }
              break;
            }
          case TokenizerState.AttributeName: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                if (!this.currentTag.checkAttributeName()) {
                  this.error = true;
                }
                this.state = TokenizerState.AfterAttributeName;
              } else if (ch == 0x2f) {
                if (!this.currentTag.checkAttributeName()) {
                  this.error = true;
                }
                this.state = TokenizerState.SelfClosingStartTag;
              } else if (ch == 0x3d) {
                if (!this.currentTag.checkAttributeName()) {
                  this.error = true;
                }
                this.state = TokenizerState.BeforeAttributeValue;
              } else if (ch == 0x3e) {
                if (!this.currentTag.checkAttributeName()) {
                  this.error = true;
                }
                this.state = TokenizerState.Data;
                return this.emitCurrentTag();
              } else if (ch >= 'A' && ch <= 'Z') {
                this.currentAttribute.appendToName(ch + 0x20);
              } else if (ch == 0) {
                this.error = true;
                this.currentAttribute.appendToName(0xfffd);
              } else if (ch < 0) {
                this.error = true;
                if (!this.currentTag.checkAttributeName()) {
                  this.error = true;
                }
                this.state = TokenizerState.Data;
              } else if (ch == 0x22 || ch == 0x27 || ch == 0x3c) {
                this.error = true;
                this.currentAttribute.appendToName(ch);
              } else {
                this.currentAttribute.appendToName(ch);
              }
              break;
            }
          case TokenizerState.AfterAttributeName: {
              int ch = this.charInput.ReadChar();
              while (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                ch = this.charInput.ReadChar();
              }
              if (ch == 0x2f) {
                this.state = TokenizerState.SelfClosingStartTag;
              } else if (ch == '=') {
                this.state = TokenizerState.BeforeAttributeValue;
              } else if (ch == '>') {
                this.state = TokenizerState.Data;
                return this.emitCurrentTag();
              } else if (ch >= 'A' && ch <= 'Z') {
                this.currentAttribute = this.currentTag.addAttribute((char)(ch +
                    0x20));
                this.state = TokenizerState.AttributeName;
              } else if (ch == 0) {
                this.error = true;
            this.currentAttribute = this.currentTag.addAttribute((char)0xfffd);
                this.state = TokenizerState.AttributeName;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                if (ch == 0x22 || ch == 0x27 || ch == 0x3c) {
                  this.error = true;
                }
                this.currentAttribute = this.currentTag.addAttribute(ch);
                this.state = TokenizerState.AttributeName;
              }
              break;
            }
          case TokenizerState.BeforeAttributeValue: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              while (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                ch = this.charInput.ReadChar();
              }
              if (ch == 0x22) {
                this.state = TokenizerState.AttributeValueDoubleQuoted;
              } else if (ch == 0x26) {
                this.charInput.moveBack(1);
                this.state = TokenizerState.AttributeValueUnquoted;
              } else if (ch == 0x27) {
                this.state = TokenizerState.AttributeValueSingleQuoted;
              } else if (ch == 0) {
                this.error = true;
                this.currentAttribute.appendToValue(0xfffd);
                this.state = TokenizerState.AttributeValueUnquoted;
              } else if (ch == 0x3e) {
                this.error = true;
                this.state = TokenizerState.Data;
                return this.emitCurrentTag();
              } else if (ch == 0x3c || ch == 0x3d || ch == 0x60) {
                this.error = true;
                this.currentAttribute.appendToValue(ch);
                this.state = TokenizerState.AttributeValueUnquoted;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                this.currentAttribute.appendToValue(ch);
                this.state = TokenizerState.AttributeValueUnquoted;
              }
              break;
            }
          case TokenizerState.AttributeValueDoubleQuoted: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x22) {
                this.currentAttribute.commitValue();
                this.state = TokenizerState.AfterAttributeValueQuoted;
              } else if (ch == 0x26) {
                this.lastState = this.state;
                this.state = TokenizerState.CharacterRefInAttributeValue;
              } else if (ch == 0) {
                this.error = true;
                this.currentAttribute.appendToValue(0xfffd);
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                this.currentAttribute.appendToValue(ch);
                // Keep reading characters to
                // reduce the need to re-call
                // this method
                int mark = this.charInput.setSoftMark();
                for (int i = 0; i < 100; ++i) {
                  ch = this.charInput.ReadChar();
                  if (ch > 0 && ch != 0x26 && ch != 0x22) {
                    this.currentAttribute.appendToValue(ch);
                  } else if (ch == 0x22) {
                    this.currentAttribute.commitValue();
                    this.state = TokenizerState.AfterAttributeValueQuoted;
                    break;
                  } else {
                    this.charInput.setMarkPosition(mark + i);
                    break;
                  }
                }
              }
              break;
            }
          case TokenizerState.AttributeValueSingleQuoted: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x27) {
                this.currentAttribute.commitValue();
                this.state = TokenizerState.AfterAttributeValueQuoted;
              } else if (ch == 0x26) {
                this.lastState = this.state;
                this.state = TokenizerState.CharacterRefInAttributeValue;
              } else if (ch == 0) {
                this.error = true;
                this.currentAttribute.appendToValue(0xfffd);
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                this.currentAttribute.appendToValue(ch);
                // Keep reading characters to
                // reduce the need to re-call
                // this method
                int mark = this.charInput.setSoftMark();
                for (int i = 0; i < 100; ++i) {
                  ch = this.charInput.ReadChar();
                  if (ch > 0 && ch != 0x26 && ch != 0x27) {
                    this.currentAttribute.appendToValue(ch);
                  } else if (ch == 0x27) {
                    this.currentAttribute.commitValue();
                    this.state = TokenizerState.AfterAttributeValueQuoted;
                    break;
                  } else {
                    this.charInput.setMarkPosition(mark + i);
                    break;
                  }
                }
              }
              break;
            }
          case TokenizerState.AttributeValueUnquoted: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                this.currentAttribute.commitValue();
                this.state = TokenizerState.BeforeAttributeName;
              } else if (ch == 0x26) {
                this.lastState = this.state;
                this.state = TokenizerState.CharacterRefInAttributeValue;
              } else if (ch == 0x3e) {
                this.currentAttribute.commitValue();
                this.state = TokenizerState.Data;
                return this.emitCurrentTag();
              } else if (ch == 0) {
                this.error = true;
                this.currentAttribute.appendToValue(0xfffd);
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                if (ch == 0x22 || ch == 0x27 || ch == 0x3c || ch == 0x3d || ch
                == 0x60) {
                  this.error = true;
                }
                this.currentAttribute.appendToValue(ch);
              }
              break;
            }
          case TokenizerState.AfterAttributeValueQuoted: {
              int mark = this.charInput.setSoftMark();
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                this.state = TokenizerState.BeforeAttributeName;
              } else if (ch == 0x2f) {
                this.state = TokenizerState.SelfClosingStartTag;
              } else if (ch == 0x3e) {
                this.state = TokenizerState.Data;
                return this.emitCurrentTag();
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                this.error = true;
                this.state = TokenizerState.BeforeAttributeName;
                this.charInput.setMarkPosition(mark);
              }
              break;
            }
          case TokenizerState.SelfClosingStartTag: {
              int mark = this.charInput.setSoftMark();
              int ch = this.charInput.ReadChar();
              if (ch == 0x3e) {
                this.currentTag.setSelfClosing(true);
                this.state = TokenizerState.Data;
                return this.emitCurrentTag();
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                this.error = true;
                this.state = TokenizerState.BeforeAttributeName;
                this.charInput.setMarkPosition(mark);
              }
              break;
            }
          case TokenizerState.MarkupDeclarationOpen: {
              int mark = this.charInput.setSoftMark();
              int ch = this.charInput.ReadChar();
              if (ch == '-' && this.charInput.ReadChar() == '-') {
                var valueToken = new CommentToken();
                this.lastComment = valueToken;
                this.state = TokenizerState.CommentStart;
                break;
              } else if (ch == 'D' || ch == 'd') {
                if (((ch = this.charInput.ReadChar()) == 'o' || ch == 'O') &&
                    ((ch = this.charInput.ReadChar()) == 'c' || ch == 'C') &&
                    ((ch = this.charInput.ReadChar()) == 't' || ch == 'T') &&
                    ((ch = this.charInput.ReadChar()) == 'y' || ch == 'Y') &&
                    ((ch = this.charInput.ReadChar()) == 'p' || ch == 'P') &&
                    ((ch = this.charInput.ReadChar()) == 'e' || ch == 'E')) {
                  this.state = TokenizerState.DocType;
                  break;
                }
              } else if (ch == '[' && true) {
              if (this.charInput.ReadChar() == 'C' && this.charInput.ReadChar()
                    == 'D' &&
               this.charInput.ReadChar() == 'A' && this.charInput.ReadChar()
                == 'T' &&
        this.charInput.ReadChar() == 'A' && this.charInput.ReadChar() == '[' &&
            this.getCurrentNode() != null &&
                !HtmlCommon.HTML_NAMESPACE.Equals(this.getCurrentNode()
                    .getNamespaceURI())) {
                  this.state = TokenizerState.CData;
                  break;
                }
              }
              this.error = true;
              this.charInput.setMarkPosition(mark);
              this.bogusCommentCharacter = -1;
              this.state = TokenizerState.BogusComment;
              break;
            }
          case TokenizerState.CommentStart: {
              int ch = this.charInput.ReadChar();
              if (ch == '-') {
                this.state = TokenizerState.CommentStartDash;
              } else if (ch == 0) {
                this.error = true;
                this.lastComment.Append((char)0xfffd);
                this.state = TokenizerState.Comment;
              } else if (ch == 0x3e || ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.lastComment.getType();
                this.tokens.Add(this.lastComment);
                return ret;
              } else {
                if (ch <= 0xffff) {
                  this.lastComment.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.lastComment.Append((char)((((ch - 0x10000) >> 10) &
                    0x3ff) + 0xd800));
                  this.lastComment.Append((char)(((ch - 0x10000) & 0x3ff) +
                    0xdc00));
                }
                this.state = TokenizerState.Comment;
              }
              break;
            }
          case TokenizerState.CommentStartDash: {
              int ch = this.charInput.ReadChar();
              if (ch == '-') {
                this.state = TokenizerState.CommentEnd;
              } else if (ch == 0) {
                this.error = true;
                this.lastComment.Append((char)'-');
                this.lastComment.Append((char)0xfffd);
                this.state = TokenizerState.Comment;
              } else if (ch == 0x3e || ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.lastComment.getType();
                this.tokens.Add(this.lastComment);
                return ret;
              } else {
                this.lastComment.Append((char)'-');
                if (ch <= 0xffff) {
                  this.lastComment.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.lastComment.Append((char)((((ch - 0x10000) >> 10) &
                    0x3ff) + 0xd800));
                  this.lastComment.Append((char)(((ch - 0x10000) & 0x3ff) +
                    0xdc00));
                }
                this.state = TokenizerState.Comment;
              }
              break;
            }
          case TokenizerState.Comment: {
              int ch = this.charInput.ReadChar();
              if (ch == '-') {
                this.state = TokenizerState.CommentEndDash;
              } else if (ch == 0) {
                this.error = true;
                this.lastComment.Append((char)0xfffd);
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.lastComment.getType();
                this.tokens.Add(this.lastComment);
                return ret;
              } else {
                if (ch <= 0xffff) {
                  this.lastComment.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.lastComment.Append((char)((((ch - 0x10000) >> 10) &
                    0x3ff) + 0xd800));
                  this.lastComment.Append((char)(((ch - 0x10000) & 0x3ff) +
                    0xdc00));
                }
              }
              break;
            }
          case TokenizerState.CommentEndDash: {
              int ch = this.charInput.ReadChar();
              if (ch == '-') {
                this.state = TokenizerState.CommentEnd;
              } else if (ch == 0) {
                this.error = true;
                this.lastComment.Append("-\ufffd");
                this.state = TokenizerState.Comment;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.lastComment.getType();
                this.tokens.Add(this.lastComment);
                return ret;
              } else {
                this.lastComment.Append((char)'-');
                this.lastComment.Append((char)ch);
                this.state = TokenizerState.Comment;
              }
              break;
            }
          case TokenizerState.CommentEnd: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x3e) {
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.lastComment.getType();
                this.tokens.Add(this.lastComment);
                return ret;
              } else if (ch == 0) {
                this.error = true;
                this.lastComment.Append("--\ufffd");
                this.state = TokenizerState.Comment;
              } else if (ch == 0x21) {  // --!>
                this.error = true;
                this.state = TokenizerState.CommentEndBang;
              } else if (ch == 0x2d) {
                this.error = true;
                this.lastComment.Append((char)'-');
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.lastComment.getType();
                this.tokens.Add(this.lastComment);
                return ret;
              } else {
                this.error = true;
                this.lastComment.Append((char)'-');
                this.lastComment.Append((char)'-');
                if (ch <= 0xffff) {
                  this.lastComment.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.lastComment.Append((char)((((ch - 0x10000) >> 10) &
                    0x3ff) + 0xd800));
                  this.lastComment.Append((char)(((ch - 0x10000) & 0x3ff) +
                    0xdc00));
                }
                this.state = TokenizerState.Comment;
              }
              break;
            }
          case TokenizerState.CommentEndBang: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x3e) {
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.lastComment.getType();
                this.tokens.Add(this.lastComment);
                return ret;
              } else if (ch == 0) {
                this.error = true;
                this.lastComment.Append("--!\ufffd");
                this.state = TokenizerState.Comment;
              } else if (ch == 0x2d) {
                this.lastComment.Append("--!");
                this.state = TokenizerState.CommentEndDash;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.lastComment.getType();
                this.tokens.Add(this.lastComment);
                return ret;
              } else {
                this.error = true;
                this.lastComment.Append("--!");
                if (ch <= 0xffff) {
                  this.lastComment.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.lastComment.Append((char)((((ch - 0x10000) >> 10) &
                    0x3ff) + 0xd800));
                  this.lastComment.Append((char)(((ch - 0x10000) & 0x3ff) +
                    0xdc00));
                }
                this.state = TokenizerState.Comment;
              }
              break;
            }
          case TokenizerState.CharacterRefInAttributeValue: {
              var allowed = 0x3e;
              if (this.lastState == TokenizerState.AttributeValueDoubleQuoted) {
                allowed = '"';
              }
              if (this.lastState == TokenizerState.AttributeValueSingleQuoted) {
                allowed = '\'';
              }
              int ch = this.parseCharacterReference(allowed);
              if (ch < 0) {
                // more than one character in this reference
                int index = Math.Abs(ch + 1);
  this.currentAttribute.appendToValue(HtmlEntities.EntityDoubles[index *
                    2]);
                this.currentAttribute.appendToValue(
                    HtmlEntities.EntityDoubles[(index * 2) + 1]);
              } else {
                this.currentAttribute.appendToValue(ch);
              }
              this.state = this.lastState;
              break;
            }
          case TokenizerState.TagName: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                this.state = TokenizerState.BeforeAttributeName;
              } else if (ch == 0x2f) {
                this.state = TokenizerState.SelfClosingStartTag;
              } else if (ch == 0x3e) {
                this.state = TokenizerState.Data;
                return this.emitCurrentTag();
              } else if (ch >= 'A' && ch <= 'Z') {
                this.currentTag.appendChar((char)(ch + 0x20));
              } else if (ch == 0) {
                this.error = true;
                this.currentTag.appendChar((char)0xfffd);
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
              } else {
                this.currentTag.append(ch);
              }
              break;
            }
          case TokenizerState.RawTextLessThan: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              if (ch == 0x2f) {
                this.tempBuilder.Remove(0, this.tempBuilder.Length);
                this.state = TokenizerState.RawTextEndTagOpen;
              } else {
                this.state = TokenizerState.RawText;
                if (ch >= 0) {
                  this.charInput.moveBack(1);
                }
                return 0x3c;
              }
              break;
            }
          case TokenizerState.BogusComment: {
              var comment = new CommentToken();
              if (this.bogusCommentCharacter >= 0) {
                var bogusChar = this.bogusCommentCharacter == 0 ? 0xfffd :
                  this.bogusCommentCharacter;
                if (bogusChar <= 0xffff) {
                  {
                    comment.Append((char)bogusChar);
                  }
                } else if (bogusChar <= 0x10ffff) {
                  comment.Append((char)((((bogusChar - 0x10000) >> 10) &
                0x3ff) + 0xd800));
                  comment.Append((char)(((bogusChar - 0x10000) & 0x3ff) +
                    0xdc00));
                }
              }
              while (true) {
                int ch = this.charInput.ReadChar();
                if (ch < 0 || ch == '>') {
                  break;
                }
                if (ch == 0) {
                  ch = 0xfffd;
                }
                if (ch <= 0xffff) {
                  comment.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  comment.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) +
                    0xd800));
                  comment.Append((char)(((ch - 0x10000) & 0x3ff) + 0xdc00));
                }
              }
              int ret = this.tokens.Count | comment.getType();
              this.tokens.Add(comment);
              this.state = TokenizerState.Data;
              return ret;
            }
          case TokenizerState.DocType: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                this.state = TokenizerState.BeforeDocTypeName;
              } else if (ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
                var valueToken = new DocTypeToken();
                valueToken.ValueForceQuirks = true;
                int ret = this.tokens.Count | valueToken.getType();
                this.tokens.Add(valueToken);
                return ret;
              } else {
                this.error = true;
                this.charInput.moveBack(1);
                this.state = TokenizerState.BeforeDocTypeName;
              }
              break;
            }
          case TokenizerState.BeforeDocTypeName: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                break;
              } else if (ch >= 'A' && ch <= 'Z') {
                this.docTypeToken = new DocTypeToken();
                this.docTypeToken.Name = new StringBuilder();
                if (ch + 0x20 <= 0xffff) {
                  this.docTypeToken.Name.Append((char)(ch +
  0x20));
                } else if (ch + 0x20 <= 0x10ffff) {
                this.docTypeToken.Name.Append((char)((((ch + 0x20 - 0x10000) >>
                10) & 0x3ff) + 0xd800));
                  this.docTypeToken.Name.Append((char)(((ch + 0x20 -
                    0x10000) & 0x3ff) + 0xdc00));
                }
                this.state = TokenizerState.DocTypeName;
              } else if (ch == 0) {
                this.error = true;
                this.docTypeToken = new DocTypeToken();
                this.docTypeToken.Name = new StringBuilder();
                this.docTypeToken.Name.Append((char)0xfffd);
                this.state = TokenizerState.DocTypeName;
              } else if (ch == 0x3e || ch < 0) {
                this.error = true;
                this.state = TokenizerState.Data;
                var valueToken = new DocTypeToken();
                valueToken.ValueForceQuirks = true;
                int ret = this.tokens.Count | valueToken.getType();
                this.tokens.Add(valueToken);
                return ret;
              } else {
                this.docTypeToken = new DocTypeToken();
                this.docTypeToken.Name = new StringBuilder();
                if (ch <= 0xffff) {
                  this.docTypeToken.Name.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.docTypeToken.Name.Append((char)((((ch - 0x10000) >>
                    10) & 0x3ff) + 0xd800));
                  this.docTypeToken.Name.Append((char)(((ch - 0x10000) &
                    0x3ff) + 0xdc00));
                }
                this.state = TokenizerState.DocTypeName;
              }
              break;
            }
          case TokenizerState.DocTypeName: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                this.state = TokenizerState.AfterDocTypeName;
              } else if (ch == 0x3e) {
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              } else if (ch >= 'A' && ch <= 'Z') {
                if (ch + 0x20 <= 0xffff) {
                  this.docTypeToken.Name.Append((char)(ch +
  0x20));
                } else if (ch + 0x20 <= 0x10ffff) {
                this.docTypeToken.Name.Append((char)((((ch + 0x20 - 0x10000) >>
                10) & 0x3ff) + 0xd800));
                  this.docTypeToken.Name.Append((char)(((ch + 0x20 -
                    0x10000) & 0x3ff) + 0xdc00));
                }
              } else if (ch == 0) {
                this.error = true;
                this.docTypeToken.Name.Append((char)0xfffd);
              } else if (ch < 0) {
                this.error = true;
                this.docTypeToken.ValueForceQuirks = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              } else {
                if (ch <= 0xffff) {
                  this.docTypeToken.Name.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  this.docTypeToken.Name.Append((char)((((ch - 0x10000) >>
                    10) & 0x3ff) + 0xd800));
                  this.docTypeToken.Name.Append((char)(((ch - 0x10000) &
                    0x3ff) + 0xdc00));
                }
              }
              break;
            }
          case TokenizerState.AfterDocTypeName: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                break;
              } else if (ch == 0x3e) {
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              } else if (ch < 0) {
                this.error = true;
                this.docTypeToken.ValueForceQuirks = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              } else {
                var ch2 = 0;
                int pos = this.charInput.setSoftMark();
                if (ch == 'P' || ch == 'p') {
                  if (((ch2 = this.charInput.ReadChar()) == 'u' || ch2 == 'U'
) && ((ch2 = this.charInput.ReadChar()) == 'b' || ch2 == 'B') &&
                    ((ch2 = this.charInput.ReadChar()) == 'l' || ch2 == 'L') &&
                    ((ch2 = this.charInput.ReadChar()) == 'i' || ch2 == 'I') &&
                    ((ch2 = this.charInput.ReadChar()) == 'c' || ch2 == 'C')
) {
                    this.state = TokenizerState.AfterDocTypePublic;
                  } else {
                    this.error = true;
                    this.charInput.setMarkPosition(pos);
                    this.docTypeToken.ValueForceQuirks = true;
                    this.state = TokenizerState.BogusDocType;
                  }
                } else if (ch == 'S' || ch == 's') {
                  if (((ch2 = this.charInput.ReadChar()) == 'y' || ch2 == 'Y'
) && ((ch2 = this.charInput.ReadChar()) == 's' || ch2 == 'S') &&
                    ((ch2 = this.charInput.ReadChar()) == 't' || ch2 == 'T') &&
                    ((ch2 = this.charInput.ReadChar()) == 'e' || ch2 == 'E') &&
                    ((ch2 = this.charInput.ReadChar()) == 'm' || ch2 == 'M')
) {
                    this.state = TokenizerState.AfterDocTypeSystem;
                  } else {
                    this.error = true;
                    this.charInput.setMarkPosition(pos);
                    this.docTypeToken.ValueForceQuirks = true;
                    this.state = TokenizerState.BogusDocType;
                  }
                } else {
                  this.error = true;
                  this.charInput.setMarkPosition(pos);
                  this.docTypeToken.ValueForceQuirks = true;
                  this.state = TokenizerState.BogusDocType;
                }
              }
              break;
            }
          case TokenizerState.AfterDocTypePublic:
          case TokenizerState.BeforeDocTypePublicID: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                if (this.state == TokenizerState.AfterDocTypePublic) {
                  this.state = TokenizerState.BeforeDocTypePublicID;
                }
              } else if (ch == 0x22) {
                this.docTypeToken.ValuePublicID = new StringBuilder();
                if (this.state == TokenizerState.AfterDocTypePublic) {
                  this.error = true;
                }
                this.state = TokenizerState.DocTypePublicIDDoubleQuoted;
              } else if (ch == 0x27) {
                this.docTypeToken.ValuePublicID = new StringBuilder();
                if (this.state == TokenizerState.AfterDocTypePublic) {
                  this.error = true;
                }
                this.state = TokenizerState.DocTypePublicIDSingleQuoted;
              } else if (ch == 0x3e || ch < 0) {
                this.error = true;
                this.docTypeToken.ValueForceQuirks = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              } else {
                this.error = true;
                this.docTypeToken.ValueForceQuirks = true;
                this.state = TokenizerState.BogusDocType;
              }
              break;
            }
          case TokenizerState.AfterDocTypeSystem:
          case TokenizerState.BeforeDocTypeSystemID: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                if (this.state == TokenizerState.AfterDocTypeSystem) {
                  this.state = TokenizerState.BeforeDocTypeSystemID;
                }
              } else if (ch == 0x22) {
                this.docTypeToken.ValueSystemID = new StringBuilder();
                if (this.state == TokenizerState.AfterDocTypeSystem) {
                  this.error = true;
                }
                this.state = TokenizerState.DocTypeSystemIDDoubleQuoted;
              } else if (ch == 0x27) {
                this.docTypeToken.ValueSystemID = new StringBuilder();
                if (this.state == TokenizerState.AfterDocTypeSystem) {
                  this.error = true;
                }
                this.state = TokenizerState.DocTypeSystemIDSingleQuoted;
              } else if (ch == 0x3e || ch < 0) {
                this.error = true;
                this.docTypeToken.ValueForceQuirks = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              } else {
                this.error = true;
                this.docTypeToken.ValueForceQuirks = true;
                this.state = TokenizerState.BogusDocType;
              }
              break;
            }
          case TokenizerState.DocTypePublicIDDoubleQuoted:
          case TokenizerState.DocTypePublicIDSingleQuoted: {
              int ch = this.charInput.ReadChar();
              if (ch == (this.state ==
                    TokenizerState.DocTypePublicIDDoubleQuoted ?
                    0x22 : 0x27)) {
                this.state = TokenizerState.AfterDocTypePublicID;
              } else if (ch == 0) {
                this.error = true;
                this.docTypeToken.ValuePublicID.Append((char)0xfffd);
              } else if (ch == 0x3e || ch < 0) {
                this.error = true;
                this.docTypeToken.ValueForceQuirks = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              } else {
                if (ch <= 0xffff) {
                  this.docTypeToken.ValuePublicID.Append((char)ch);
                } else if (ch <= 0x10ffff) {
              this.docTypeToken.ValuePublicID.Append((char)((((ch - 0x10000) >>
                10) & 0x3ff) + 0xd800));
                  this.docTypeToken.ValuePublicID.Append((char)(((ch -
                  0x10000) & 0x3ff) + 0xdc00));
                }
              }
              break;
            }
          case TokenizerState.DocTypeSystemIDDoubleQuoted:
          case TokenizerState.DocTypeSystemIDSingleQuoted: {
              int ch = this.charInput.ReadChar();
              if (ch == (this.state ==
                    TokenizerState.DocTypeSystemIDDoubleQuoted ?
                    0x22 : 0x27)) {
                this.state = TokenizerState.AfterDocTypeSystemID;
              } else if (ch == 0) {
                this.error = true;
                this.docTypeToken.ValueSystemID.Append((char)0xfffd);
              } else if (ch == 0x3e || ch < 0) {
                this.error = true;
                this.docTypeToken.ValueForceQuirks = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              } else {
                if (ch <= 0xffff) {
                  this.docTypeToken.ValueSystemID.Append((char)ch);
                } else if (ch <= 0x10ffff) {
              this.docTypeToken.ValueSystemID.Append((char)((((ch - 0x10000) >>
                10) & 0x3ff) + 0xd800));
                  this.docTypeToken.ValueSystemID.Append((char)(((ch -
                  0x10000) & 0x3ff) + 0xdc00));
                }
              }
              break;
            }
          case TokenizerState.AfterDocTypePublicID:
          case TokenizerState.BetweenDocTypePublicAndSystem: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                if (this.state == TokenizerState.AfterDocTypePublicID) {
                  this.state = TokenizerState.BetweenDocTypePublicAndSystem;
                }
              } else if (ch == 0x3e) {
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              } else if (ch == 0x22) {
                this.docTypeToken.ValueSystemID = new StringBuilder();
                if (this.state == TokenizerState.AfterDocTypePublicID) {
                  this.error = true;
                }
                this.state = TokenizerState.DocTypeSystemIDDoubleQuoted;
              } else if (ch == 0x27) {
                this.docTypeToken.ValueSystemID = new StringBuilder();
                if (this.state == TokenizerState.AfterDocTypePublicID) {
                  this.error = true;
                }
                this.state = TokenizerState.DocTypeSystemIDSingleQuoted;
              } else if (ch < 0) {
                this.error = true;
                this.docTypeToken.ValueForceQuirks = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              } else {
                this.error = true;
                this.docTypeToken.ValueForceQuirks = true;
                this.state = TokenizerState.BogusDocType;
              }
              break;
            }
          case TokenizerState.AfterDocTypeSystemID: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
                break;
              } else if (ch == 0x3e) {
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              } else if (ch < 0) {
                this.error = true;
                this.docTypeToken.ValueForceQuirks = true;
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              } else {
                this.error = true;
                this.state = TokenizerState.BogusDocType;
              }
              break;
            }
          case TokenizerState.BogusDocType: {
              int ch = this.charInput.ReadChar();
              if (ch == 0x3e || ch < 0) {
                this.state = TokenizerState.Data;
                int ret = this.tokens.Count | this.docTypeToken.getType();
                this.tokens.Add(this.docTypeToken);
                return ret;
              }
              break;
            }
          case TokenizerState.CData: {
              var buffer = new StringBuilder();
              var phase = 0;
              this.state = TokenizerState.Data;
              while (true) {
                int ch = this.charInput.ReadChar();
                if (ch < 0) {
                  break;
                }
                if (ch <= 0xffff) {
                  buffer.Append((char)ch);
                } else if (ch <= 0x10ffff) {
                  buffer.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) +
                    0xd800));
                  buffer.Append((char)(((ch - 0x10000) & 0x3ff) + 0xdc00));
                }
                if (phase == 0) {
                  if (ch == ']') {
                    ++phase;
                  } else {
                    phase = 0;
                  }
                } else if (phase == 1) {
                  if (ch == ']') {
                    ++phase;
                  } else {
                    phase = 0;
                  }
                } else if (phase == 2) {
                  if (ch == '>') {
                    ++phase;
                    break;
                  } else {
                    phase = (ch == ']') ? 2 : 0;
                  }
                }
              }
              string str = buffer.ToString();
              int size = buffer.Length;
              if (phase == 3) {
                if (size < 0) {
                  throw new InvalidOperationException();
                }
                size -= 3;  // don't count the ']]>'
              }
              if (size > 0) {
                // Emit the tokens
                var ret1 = 0;
                for (int i = 0; i < size; ++i) {
                  int c2 = DataUtilities.CodePointAt(str, i);
                  if (i > 0) {
                    this.tokenQueue.Add(c2);
                  } else {
                    ret1 = c2;
                  }
                  if (c2 >= 0x10000) {
                    ++i;
                  }
                }
                return ret1;
              }
              break;
            }
          case TokenizerState.RcDataLessThan: {
              this.charInput.setHardMark();
              int ch = this.charInput.ReadChar();
              if (ch == 0x2f) {
                this.tempBuilder.Remove(0, this.tempBuilder.Length);
                this.state = TokenizerState.RcDataEndTagOpen;
              } else {
                this.state = TokenizerState.RcData;
                if (ch >= 0) {
                  this.charInput.moveBack(1);
                }
                return 0x3c;
              }
              break;
            }
          default: throw new InvalidOperationException();
        }
      }
    }

    private IElement popCurrentNode() {
      return (this.openElements.Count > 0) ?
        removeAtIndex(this.openElements, this.openElements.Count - 1) : null;
    }

    private void pushFormattingElement(StartTagToken tag) {
      Element valueElement = this.addHtmlElement(tag);
      var matchingElements = 0;
      var lastMatchingElement = -1;
      string valueName = valueElement.getLocalName();
      for (int i = this.formattingElements.Count - 1; i >= 0; --i) {
        FormattingElement fe = this.formattingElements[i];
        if (fe.isMarker()) {
          break;
        }
        if (fe.Element.getLocalName().Equals(valueName) &&
  fe.Element.getNamespaceURI().Equals(valueElement.getNamespaceURI())) {
          IList<IAttr> attribs = fe.Element.getAttributes();
          IList<IAttr> myAttribs = valueElement.getAttributes();
          if (attribs.Count == myAttribs.Count) {
            var match = true;
            for (int j = 0; j < myAttribs.Count; ++j) {
              string name1 = myAttribs[j].getName();
              string _namespace = myAttribs[j].getNamespaceURI();
              string value = myAttribs[j].getValue();
              string otherValue = fe.Element.getAttributeNS(
       _namespace,
       name1);
              if (otherValue == null || !otherValue.Equals(value)) {
                match = false;
              }
            }
            if (match) {
              ++matchingElements;
              lastMatchingElement = i;
            }
          }
        }
      }
      if (matchingElements >= 3) {
        this.formattingElements.RemoveAt(lastMatchingElement);
      }
      var fe2 = new FormattingElement();
      fe2.ValueMarker = false;
      fe2.Token = tag;
      fe2.Element = valueElement;
      this.formattingElements.Add(fe2);
    }

    private void reconstructFormatting() {
      if (this.formattingElements.Count == 0) {
        return;
      }
      // DebugUtility.Log("reconstructing elements");
      // DebugUtility.Log(formattingElements);
      FormattingElement fe =
        this.formattingElements[this.formattingElements.Count - 1];
      if (fe.isMarker() || this.openElements.Contains(fe.Element)) {
        return;
      }
      int i = this.formattingElements.Count - 1;
      while (i > 0) {
        fe = this.formattingElements[i - 1];
        --i;
        if (!fe.isMarker() && !this.openElements.Contains(fe.Element)) {
          continue;
        }
        ++i;
        break;
      }
      for (int j = i; j < this.formattingElements.Count; ++j) {
        fe = this.formattingElements[j];
        Element valueElement = this.addHtmlElement(fe.Token);
        fe.Element = valueElement;
        fe.ValueMarker = false;
      }
    }

    private void removeFormattingElement(IElement valueAElement) {
      FormattingElement f = null;
      foreach (var fe in this.formattingElements) {
        if (!fe.isMarker() && valueAElement.Equals(fe.Element)) {
          f = fe;
          break;
        }
      }
      if (f != null) {
        this.formattingElements.Remove(f);
      }
    }

    private void resetInsertionMode() {
      var last = false;
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (this.context != null && i == 0) {
          e = this.context;
          last = true;
        }
        string valueName = e.getLocalName();
        if (!last && (valueName.Equals("th") || valueName.Equals("td"))) {
          this.insertionMode = InsertionMode.InCell;
          break;
        }
        if (valueName.Equals("select")) {
          this.insertionMode = InsertionMode.InSelect;
          break;
        }
        if (valueName.Equals("colgroup")) {
          this.insertionMode = InsertionMode.InColumnGroup;
          break;
        }
        if (valueName.Equals("tr")) {
          this.insertionMode = InsertionMode.InRow;
          break;
        }
        if (valueName.Equals("caption")) {
          this.insertionMode = InsertionMode.InCaption;
          break;
        }
        if (valueName.Equals("table")) {
          this.insertionMode = InsertionMode.InTable;
          break;
        }
        if (valueName.Equals("frameset")) {
          this.insertionMode = InsertionMode.InFrameset;
          break;
        }
        if (valueName.Equals("html")) {
          this.insertionMode = InsertionMode.BeforeHead;
          break;
        }
        if (valueName.Equals("head") || valueName.Equals("body")) {
          this.insertionMode = InsertionMode.InBody;
          break;
        }
        if (valueName.Equals("thead") || valueName.Equals("tbody") ||
              valueName.Equals("tfoot")) {
          this.insertionMode = InsertionMode.InTableBody;
          break;
        }
        if (last) {
          this.insertionMode = InsertionMode.InBody;
          break;
        }
      }
    }

    internal void setCData() {
      this.state = TokenizerState.CData;
    }

    internal void setPlainText() {
      this.state = TokenizerState.PlainText;
    }

    internal void setRawText() {
      this.state = TokenizerState.RawText;
    }

    internal void setRcData() {
      this.state = TokenizerState.RcData;
    }

    private void skipLineFeed() {
      int mark = this.charInput.setSoftMark();
      int nextToken = this.charInput.ReadChar();
      if (nextToken == 0x0a) {
        return;  // ignore the valueToken if it's 0x0A
      } else if (nextToken == 0x26) {  // start of character reference
        int charref = this.parseCharacterReference(-1);
        if (charref < 0) {
          // more than one character in this reference
          int index = Math.Abs(charref + 1);
          this.tokenQueue.Add(HtmlEntities.EntityDoubles[index * 2]);
          this.tokenQueue.Add(HtmlEntities.EntityDoubles[(index * 2) + 1]);
        } else if (charref == 0x0a) {
          return;  // ignore the valueToken
        } else {
          this.tokenQueue.Add(charref);
        }
      } else {
        // anything else; reset the input stream
        this.charInput.setMarkPosition(mark);
      }
    }

    private void stopParsing() {
      this.done = true;
      if (String.IsNullOrEmpty(this.valueDocument.DefaultLanguage)) {
        if (this.contentLanguage.Length == 1) {
          // set the fallback language if there is
          // only one language defined and no meta valueElement
          // defines the language
          this.valueDocument.DefaultLanguage = this.contentLanguage[0];
        }
      }
      this.valueDocument.Encoding = this.encoding.getEncoding();
      string docbase = this.valueDocument.getBaseURI();
      if (docbase == null || docbase.Length == 0) {
        docbase = this.baseurl;
      } else {
        if (this.baseurl != null && this.baseurl.Length > 0) {
          this.valueDocument.setBaseURI(
  HtmlCommon.resolveURL(
  this.valueDocument,
  this.baseurl,
  docbase));
        }
      }
      this.openElements.Clear();
      this.formattingElements.Clear();
    }
  }
}
