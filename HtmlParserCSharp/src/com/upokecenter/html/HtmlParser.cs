/*

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
using Com.Upokecenter.Io;
using Com.Upokecenter.Net;
using Com.Upokecenter.Util;
using PeterO;
using PeterO.Text;

namespace Com.Upokecenter.Html {
  internal sealed class HtmlParser {
    public class CommentToken : IToken {
      public StringBuilder CommentValue {
        get;
        set;
      }

      public CommentToken() {
        this.CommentValue = new StringBuilder();
      }

      public void Append(string str) {
        this.CommentValue.Append(str);
      }

      public void AppendChar(int ch) {
        if (ch <= 0xffff) {
          this.CommentValue.Append((char)ch);
        } else if (ch <= 0x10ffff) {
          this.CommentValue.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) |
            0xd800));
          this.CommentValue.Append((char)(((ch - 0x10000) & 0x3ff) | 0xdc00));
        }
      }

      public int GetTokenType() {
        return TOKEN_COMMENT;
      }
    }

    internal class DocTypeToken : IToken {
      public StringBuilder Name {
        get;
      }

      public StringBuilder ValuePublicID {
        get;
      }

      public StringBuilder ValueSystemID {
        get;
      }

      public DocTypeToken() : this(String.Empty, String.Empty, String.Empty) {
      }

      public DocTypeToken(string name, string pid, string sid) {
        this.Name = new StringBuilder().Append(name);
        this.ValuePublicID = new StringBuilder().Append(pid);
        this.ValueSystemID = new StringBuilder().Append(sid);
      }

      public bool ForceQuirks {
        get;
        set;
      }

      public int GetTokenType() {
        return TOKEN_DOCTYPE;
      }
    }

    internal class EndTagToken : TagToken {
      public EndTagToken(char c) : base(c) {
      }

      public EndTagToken(string name) : base(name) {
      }

      public override sealed int GetTokenType() {
        return HtmlParser.TOKEN_END_TAG;
      }
    }

    private class FormattingElement {
      public bool ValueMarker {
        get;
        set;
      }

      public IElement Element {
        get;
        set;
      }

      public StartTagToken Token {
        get;
        set;
      }

      public bool IsMarker() {
        return this.ValueMarker;
      }

      public override sealed string ToString() {
        return "FormattingElement [" + "ValueMarker" + "=" + this.ValueMarker +
          ", Token" + "=" + this.Token + "]\n";
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
      AfterAfterFrameset,
    }

    internal interface IToken {
      int GetTokenType();
    }

    internal class StartTagToken : TagToken {
      public StartTagToken(char c) : base(c) {
      }

      public StartTagToken(string name) : base(name) {
      }

      public override sealed int GetTokenType() {
        return HtmlParser.TOKEN_START_TAG;
      }

      public void SetName(string stringValue) {
        this.builder.Clear();
        this.builder.Append(stringValue);
      }
    }

    internal abstract class TagToken : IToken, INameAndAttributes {
      protected StringBuilder builder;

      public IList<Attr> Attributes {
        get;
        set;
      }

      public bool SelfClosing {
        get;
        set;
      }

      public bool ValueSelfClosingAck {
        get;
        set;
      }

      public TagToken(char ch) {
        this.builder = new StringBuilder();
        this.builder.Append(ch);
        this.Attributes = null;
        this.SelfClosing = false;
        this.ValueSelfClosingAck = false;
      }

      public TagToken(string valueName) {
        this.builder = new StringBuilder();
        this.builder.Append(valueName);
      }

      public void AckSelfClosing() {
        this.ValueSelfClosingAck = true;
      }

      public Attr AddAttribute(char ch) {
        this.Attributes = this.Attributes ?? new List<Attr>();
        var a = new Attr(ch);
        this.Attributes.Add(a);
        return a;
      }

      public Attr AddAttribute(int ch) {
        this.Attributes = this.Attributes ?? new List<Attr>();
        var a = new Attr(ch);
        this.Attributes.Add(a);
        return a;
      }

      public void Append(int ch) {
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

      public void AppendChar(char ch) {
        this.builder.Append(ch);
      }

      public bool CheckAttributeName() {
        if (this.Attributes == null) {
          return true;
        }
        int size = this.Attributes.Count;
        if (size >= 2) {
          string thisname = this.Attributes[size - 1].GetName();
          for (int i = 0; i < size - 1; ++i) {
            if (this.Attributes[i].GetName().Equals(thisname,
              StringComparison.Ordinal)) {
              // Attribute with this valueName already exists;
              // remove it
              this.Attributes.RemoveAt(size - 1);
              return false;
            }
          }
        }
        return true;
      }

      public string GetAttribute(string valueName) {
        if (this.Attributes == null) {
          return null;
        }
        int size = this.Attributes.Count;
        for (int i = 0; i < size; ++i) {
          IAttr a = this.Attributes[i];
          string thisname = a.GetName();
          if (thisname.Equals(valueName, StringComparison.Ordinal)) {
            return a.GetValue();
          }
        }
        return null;
      }

      public string GetAttributeNS(string valueName, string namespaceValue) {
        if (this.Attributes == null) {
          return null;
        }
        int size = this.Attributes.Count;
        for (int i = 0; i < size; ++i) {
          Attr a = this.Attributes[i];
          if (a.IsAttribute(valueName, namespaceValue)) {
            return a.GetValue();
          }
        }
        return null;
      }

      public IList<Attr> GetAttributes() {
        if (this.Attributes == null) {
          return new Attr[0];
        } else {
          return this.Attributes;
        }
      }

      public string GetName() {
        return this.builder.ToString();
      }

      public abstract int GetTokenType();

      public bool IsAckSelfClosing() {
        return !this.SelfClosing || this.ValueSelfClosingAck;
      }

      public bool IsSelfClosing() {
        return this.SelfClosing;
      }

      public bool IsSelfClosingAck() {
        return this.ValueSelfClosingAck;
      }

      public void SetAttribute(string attrname, string value) {
        if (this.Attributes == null) {
          this.Attributes = new List<Attr>();
          this.Attributes.Add(new Attr(attrname, value));
        } else {
          int size = this.Attributes.Count;
          for (int i = 0; i < size; ++i) {
            Attr a = this.Attributes[i];
            string thisname = a.GetName();
            if (thisname.Equals(attrname, StringComparison.Ordinal)) {
              a.SetValue(value);
              return;
            }
          }
          this.Attributes.Add(new Attr(attrname, value));
        }
      }

      public void SetSelfClosing(bool SelfClosing) {
        this.SelfClosing = SelfClosing;
      }

      public override sealed string ToString() {
        return "TagToken [" + this.builder.ToString() + ", " +
          this.Attributes + (this.SelfClosing ? (", ValueSelfClosingAck=" +
          this.ValueSelfClosingAck) : String.Empty) + "]";
      }
    }

    private sealed class Html5Encoding : ICharacterEncoding {
      private ICharacterDecoder decoder;

      public Html5Encoding(EncodingConfidence ec) {
        ICharacterDecoder icd = ec == null ? null :
          Encodings.GetEncoding(ec.GetEncoding()).GetDecoder();
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
      void Rewind();

      void DisableBuffer();
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
      CData,
    }

    private const int TOKEN_EOF = 0x10000000;

    private const int TOKEN_START_TAG = 0x20000000;

    private const int TOKEN_END_TAG = 0x30000000;

    private const int TOKEN_COMMENT = 0x40000000;

    private const int TOKEN_DOCTYPE = 0x50000000;
    private const int TOKEN_TYPE_MASK = unchecked((int)0xf0000000);
    private const int TOKEN_CHARACTER = 0x00000000;
    private const int TOKEN_INDEX_MASK = 0x0fffffff;

    private bool checkErrorVar = false;

    private void AddToken(IToken token) {
      if (this.tokens.Count > TOKEN_INDEX_MASK) {
        throw new InvalidOperationException();
      }
      this.tokens.Add(token);
    }

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
      "-//softquad software//dtd hotmetal pro 6.0::" +
      "19990601::extensions to Html 4.0//",
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
      "-//webtechs//dtd mozilla html//",
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

    private IElement headElement = null;
    private IElement formElement = null;
    private IElement inputElement = null;
    private string baseurl = null;
    private Document valueDocument = null;
    private bool done = false;

    private StringBuilder pendingTableCharacters = new StringBuilder();
    private bool doFosterParent;
    private IElement context;
    private bool noforeign;
    private string address;

    private string[] contentLanguage;

    private static T RemoveAtIndex<T>(IList<T> array, int index) {
      T ret = array[index];
      array.RemoveAt(index);
      return ret;
    }

    public HtmlParser(IReader s, string address)
      : this(s, address, null, null) {
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
        URL url = URL.Parse(address);
        if (url == null || url.GetScheme().Length == 0) {
          throw new ArgumentException();
        }
      }
      // TODO: Use a more sophisticated language parser here
      this.contentLanguage = new string[] { contentLanguage };
      this.address = address;
      this.Initialize();
      this.inputStream = new ConditionalBufferInputStream(source); // TODO: ???
      this.encoding = new EncodingConfidence(
        charset,
        EncodingConfidence.Certain);
      // TODO: Use the following below
      // this.encoding = CharsetSniffer.sniffEncoding(this.inputStream,
      // charset);
      this.inputStream.Rewind();
      ICharacterEncoding henc = new Html5Encoding(this.encoding);
      this.charInput = new StackableCharacterInput(
        Encodings.GetDecoderInput(henc, this.inputStream));
    }

    private void AddCommentNodeToCurrentNode(int valueToken) {
      this.InsertInCurrentNode(this.CreateCommentNode(valueToken));
    }

    private void AddCommentNodeToDocument(int valueToken) {
      ((Document)this.valueDocument)
      .AppendChild(this.CreateCommentNode(valueToken));
    }

    private void AddCommentNodeToFirst(int valueToken) {
      ((Node)this.openElements[0])
      .AppendChild(this.CreateCommentNode(valueToken));
    }

    private Element AddHtmlElement(StartTagToken tag) {
      Element valueElement = Element.FromToken(tag);
      IElement currentNode = this.GetCurrentNode();
      if (currentNode != null) {
        this.InsertInCurrentNode(valueElement);
      } else {
        this.valueDocument.AppendChild(valueElement);
      }
      this.openElements.Add(valueElement);
      return valueElement;
    }

    private Element AddHtmlElementNoPush(StartTagToken tag) {
      Element valueElement = Element.FromToken(tag);
      IElement currentNode = this.GetCurrentNode();
      if (currentNode != null) {
        this.InsertInCurrentNode(valueElement);
      }
      return valueElement;
    }

    private void AdjustForeignAttributes(StartTagToken valueToken) {
      IList<Attr> Attributes = valueToken.GetAttributes();
      foreach (var attr in Attributes) {
        string valueName = attr.GetName();
        if (valueName.Equals("xlink:actuate", StringComparison.Ordinal) ||
          valueName.Equals(
            "xlink:arcrole",
            StringComparison.Ordinal) ||
          valueName.Equals("xlink:href", StringComparison.Ordinal) ||
          valueName.Equals(
            "xlink:role",
            StringComparison.Ordinal) ||
          valueName.Equals("xlink:show", StringComparison.Ordinal) ||
          valueName.Equals(
            "xlink:title",
            StringComparison.Ordinal) ||
          valueName.Equals("xlink:type", StringComparison.Ordinal)) {
          attr.SetNamespace(HtmlCommon.XLINK_NAMESPACE);
        } else if (valueName.Equals("xml:base", StringComparison.Ordinal) ||
          valueName.Equals(
            "xml:lang",
            StringComparison.Ordinal) ||
          valueName.Equals("xml:space", StringComparison.Ordinal)) {
          attr.SetNamespace(HtmlCommon.XML_NAMESPACE);
        } else if (valueName.Equals("xmlns", StringComparison.Ordinal) ||
          valueName.Equals(
            "xmlns:xlink",
            StringComparison.Ordinal)) {
          attr.SetNamespace(HtmlCommon.XMLNS_NAMESPACE);
        }
      }
    }

    private bool HasHtmlOpenElement(string name) {
      foreach (var e in this.openElements) {
        if (HtmlCommon.IsHtmlElement(e, name)) {
          return true;
        }
      }
      return false;
    }

    private void AdjustMathMLAttributes(StartTagToken valueToken) {
      IList<Attr> Attributes = valueToken.GetAttributes();
      foreach (var attr in Attributes) {
        if (attr.GetName().Equals("definitionurl", StringComparison.Ordinal)) {
          attr.SetName("definitionURL");
        }
      }
    }

    private void AdjustSvgAttributes(StartTagToken valueToken) {
      IList<Attr> attributeList = valueToken.GetAttributes();
      foreach (var attr in attributeList) {
        string valueName = attr.GetName();
        if (valueName.Equals("attributename", StringComparison.Ordinal)) {
          {
            attr.SetName("attributeName");
          }
        } else if (valueName.Equals("attributetype",
          StringComparison.Ordinal)) {
          {
            attr.SetName("attributeType");
          }
        } else if (valueName.Equals("basefrequency",
          StringComparison.Ordinal)) {
          {
            attr.SetName("baseFrequency");
          }
        } else if (valueName.Equals("baseprofile", StringComparison.Ordinal)) {
          {
            attr.SetName("baseProfile");
          }
        } else if (valueName.Equals("calcmode", StringComparison.Ordinal)) {
          {
            attr.SetName("calcMode");
          }
        } else if (valueName.Equals("clippathunits",
          StringComparison.Ordinal)) {
          {
            attr.SetName("clipPathUnits");
          }
        } else if (valueName.Equals("diffuseconstant",
          StringComparison.Ordinal)) {
          {
            attr.SetName("diffuseConstant");
          }
        } else if (valueName.Equals("edgemode", StringComparison.Ordinal)) {
          {
            attr.SetName("edgeMode");
          }
        } else if (valueName.Equals("filterunits", StringComparison.Ordinal)) {
          {
            attr.SetName("filterUnits");
          }
        } else if (valueName.Equals("glyphref", StringComparison.Ordinal)) {
          {
            attr.SetName("glyphRef");
          }
        } else if (valueName.Equals("gradienttransform",
          StringComparison.Ordinal)) {
          attr.SetName("gradientTransform");
        } else if (valueName.Equals("gradientunits",
          StringComparison.Ordinal)) {
          {
            attr.SetName("gradientUnits");
          }
        } else if (valueName.Equals("kernelmatrix",
          StringComparison.Ordinal)) {
          {
            attr.SetName("kernelMatrix");
          }
        } else if (valueName.Equals("kernelunitlength",
          StringComparison.Ordinal)) {
          {
            attr.SetName("kernelUnitLength");
          }
        } else if (valueName.Equals("keypoints", StringComparison.Ordinal)) {
          {
            attr.SetName("keyPoints");
          }
        } else if (valueName.Equals("keysplines", StringComparison.Ordinal)) {
          {
            attr.SetName("keySplines");
          }
        } else if (valueName.Equals("keytimes", StringComparison.Ordinal)) {
          {
            attr.SetName("keyTimes");
          }
        } else if (valueName.Equals("lengthadjust",
          StringComparison.Ordinal)) {
          {
            attr.SetName("lengthAdjust");
          }
        } else if (valueName.Equals("limitingconeangle",
          StringComparison.Ordinal)) {
          attr.SetName("limitingConeAngle");
        } else if (valueName.Equals("markerheight",
          StringComparison.Ordinal)) {
          {
            attr.SetName("markerHeight");
          }
        } else if (valueName.Equals("markerunits", StringComparison.Ordinal)) {
          {
            attr.SetName("markerUnits");
          }
        } else if (valueName.Equals("markerwidth", StringComparison.Ordinal)) {
          {
            attr.SetName("markerWidth");
          }
        } else if (valueName.Equals("maskcontentunits",
          StringComparison.Ordinal)) {
          {
            attr.SetName("maskContentUnits");
          }
        } else if (valueName.Equals("maskunits", StringComparison.Ordinal)) {
          {
            attr.SetName("maskUnits");
          }
        } else if (valueName.Equals("numoctaves", StringComparison.Ordinal)) {
          {
            attr.SetName("numOctaves");
          }
        } else if (valueName.Equals("pathlength", StringComparison.Ordinal)) {
          {
            attr.SetName("pathLength");
          }
        } else if (valueName.Equals("patterncontentunits",
          StringComparison.Ordinal)) {
          attr.SetName("patternContentUnits");
        } else if (valueName.Equals("patterntransform",
          StringComparison.Ordinal)) {
          {
            attr.SetName("patternTransform");
          }
        } else if (valueName.Equals("patternunits",
          StringComparison.Ordinal)) {
          {
            attr.SetName("patternUnits");
          }
        } else if (valueName.Equals("pointsatx", StringComparison.Ordinal)) {
          {
            attr.SetName("pointsAtX");
          }
        } else if (valueName.Equals("pointsaty", StringComparison.Ordinal)) {
          {
            attr.SetName("pointsAtY");
          }
        } else if (valueName.Equals("pointsatz", StringComparison.Ordinal)) {
          {
            attr.SetName("pointsAtZ");
          }
        } else if (valueName.Equals("preservealpha",
          StringComparison.Ordinal)) {
          {
            attr.SetName("preserveAlpha");
          }
        } else if (valueName.Equals("preserveaspectratio",
          StringComparison.Ordinal)) {
          attr.SetName("preserveAspectRatio");
        } else if (valueName.Equals("primitiveunits",
          StringComparison.Ordinal)) {
          {
            attr.SetName("primitiveUnits");
          }
        } else if (valueName.Equals("refx", StringComparison.Ordinal)) {
          {
            attr.SetName("refX");
          }
        } else if (valueName.Equals("refy", StringComparison.Ordinal)) {
          {
            attr.SetName("refY");
          }
        } else if (valueName.Equals("repeatcount", StringComparison.Ordinal)) {
          {
            attr.SetName("repeatCount");
          }
        } else if (valueName.Equals("repeatdur", StringComparison.Ordinal)) {
          {
            attr.SetName("repeatDur");
          }
        } else if (valueName.Equals("requiredextensions",
          StringComparison.Ordinal)) {
          attr.SetName("requiredExtensions");
        } else if (valueName.Equals("requiredfeatures",
          StringComparison.Ordinal)) {
          {
            attr.SetName("requiredFeatures");
          }
        } else if (valueName.Equals("specularconstant",
          StringComparison.Ordinal)) {
          {
            attr.SetName("specularConstant");
          }
        } else if (valueName.Equals("specularexponent",
          StringComparison.Ordinal)) {
          {
            attr.SetName("specularExponent");
          }
        } else if (valueName.Equals("spreadmethod",
          StringComparison.Ordinal)) {
          {
            attr.SetName("spreadMethod");
          }
        } else if (valueName.Equals("startoffset", StringComparison.Ordinal)) {
          {
            attr.SetName("startOffset");
          }
        } else if (valueName.Equals("stddeviation",
          StringComparison.Ordinal)) {
          {
            attr.SetName("stdDeviation");
          }
        } else if (valueName.Equals("stitchtiles", StringComparison.Ordinal)) {
          {
            attr.SetName("stitchTiles");
          }
        } else if (valueName.Equals("surfacescale",
          StringComparison.Ordinal)) {
          {
            attr.SetName("surfaceScale");
          }
        } else if (valueName.Equals("systemlanguage",
          StringComparison.Ordinal)) {
          {
            attr.SetName("systemLanguage");
          }
        } else if (valueName.Equals("tablevalues", StringComparison.Ordinal)) {
          {
            attr.SetName("tableValues");
          }
        } else if (valueName.Equals("targetx", StringComparison.Ordinal)) {
          {
            attr.SetName("targetX");
          }
        } else if (valueName.Equals("targety", StringComparison.Ordinal)) {
          {
            attr.SetName("targetY");
          }
        } else if (valueName.Equals("textlength", StringComparison.Ordinal)) {
          {
            attr.SetName("textLength");
          }
        } else if (valueName.Equals("viewbox", StringComparison.Ordinal)) {
          {
            attr.SetName("viewBox");
          }
        } else if (valueName.Equals("viewtarget", StringComparison.Ordinal)) {
          {
            attr.SetName("viewTarget");
          }
        } else if (valueName.Equals("xchannelselector",
          StringComparison.Ordinal)) {
          {
            attr.SetName("xChannelSelector");
          }
        } else if (valueName.Equals("ychannelselector",
          StringComparison.Ordinal)) {
          {
            attr.SetName("yChannelSelector");
          }
        } else if (valueName.Equals("zoomandpan", StringComparison.Ordinal)) {
          {
            attr.SetName("zoomAndPan");
          }
        }
      }
    }

    private bool ApplyEndTag(string valueName, InsertionMode insMode) {
      return this.ApplyInsertionMode(
          this.GetArtificialToken(TOKEN_END_TAG, valueName),
          insMode);
    }

    private bool ApplyForeignContext(int valueToken) {
      if (valueToken == 0) {
        this.ParseError();
        this.InsertCharacter(this.GetCurrentNode(), 0xfffd);
        return true;
      } else if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        this.InsertCharacter(this.GetCurrentNode(), valueToken);
        if (valueToken != 0x09 && valueToken != 0x0c && valueToken != 0x0a &&
          valueToken != 0x0d && valueToken != 0x20) {
          this.framesetOk = false;
        }
        return true;
      } else if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        this.AddCommentNodeToCurrentNode(valueToken);
        return true;
      } else if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        this.ParseError();
        return false;
      } else if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        var tag = (StartTagToken)this.GetToken(valueToken);
        string valueName = tag.GetName();
        var specialStartTag = false;
        if (valueName.Equals("font",
          StringComparison.Ordinal) && (tag.GetAttribute("color") != null ||
            tag.GetAttribute("size") !=

            null || tag.GetAttribute("face") != null)) {
          specialStartTag = true;
          this.ParseError();
        } else if (valueName.Equals("b", StringComparison.Ordinal) ||
          valueName.Equals("big", StringComparison.Ordinal) ||
          valueName.Equals("blockquote", StringComparison.Ordinal) ||
          valueName.Equals("body", StringComparison.Ordinal) ||
          valueName.Equals("br", StringComparison.Ordinal) ||
          valueName.Equals("center", StringComparison.Ordinal) ||
          valueName.Equals("code", StringComparison.Ordinal) ||
          valueName.Equals(
            "dd",
            StringComparison.Ordinal) ||
          valueName.Equals("div", StringComparison.Ordinal) ||
          valueName.Equals("dl", StringComparison.Ordinal) ||
          valueName.Equals("dt", StringComparison.Ordinal) ||
          valueName.Equals("em", StringComparison.Ordinal) ||
          valueName.Equals("embed", StringComparison.Ordinal) ||
          valueName.Equals("h1", StringComparison.Ordinal) ||
          valueName.Equals("h2", StringComparison.Ordinal) ||
          valueName.Equals("h3", StringComparison.Ordinal) ||
          valueName.Equals("h4", StringComparison.Ordinal) ||
          valueName.Equals("h5", StringComparison.Ordinal) ||
          valueName.Equals("h6", StringComparison.Ordinal) ||
          valueName.Equals("head", StringComparison.Ordinal) ||
          valueName.Equals("hr", StringComparison.Ordinal) ||
          valueName.Equals("i", StringComparison.Ordinal) ||
          valueName.Equals("img", StringComparison.Ordinal) ||
          valueName.Equals("li", StringComparison.Ordinal) ||
          valueName.Equals("listing", StringComparison.Ordinal) ||
          valueName.Equals("meta", StringComparison.Ordinal) ||
          valueName.Equals(
            "nobr",
            StringComparison.Ordinal) ||
          valueName.Equals("ol", StringComparison.Ordinal) ||
          valueName.Equals("p", StringComparison.Ordinal) ||
          valueName.Equals("pre", StringComparison.Ordinal) ||
          valueName.Equals("ruby", StringComparison.Ordinal) ||
          valueName.Equals("s", StringComparison.Ordinal) ||
          valueName.Equals("small", StringComparison.Ordinal) ||
          valueName.Equals("span", StringComparison.Ordinal) ||
          valueName.Equals("strong", StringComparison.Ordinal) ||
          valueName.Equals("strike", StringComparison.Ordinal) ||
          valueName.Equals("sub", StringComparison.Ordinal) ||
          valueName.Equals("sup", StringComparison.Ordinal) ||
          valueName.Equals(
            "table",
            StringComparison.Ordinal) ||
          valueName.Equals("tt", StringComparison.Ordinal) ||
          valueName.Equals("u", StringComparison.Ordinal) ||
          valueName.Equals("ul", StringComparison.Ordinal) ||
          valueName.Equals("var", StringComparison.Ordinal)) {
          specialStartTag = true;
          this.ParseError();
        }
        if (specialStartTag && this.context == null) {
          while (true) {
            this.PopCurrentNode();
            IElement node = this.GetCurrentNode();
            if (node.GetNamespaceURI().Equals(HtmlCommon.HTML_NAMESPACE,
              StringComparison.Ordinal) ||
              this.IsMathMLTextIntegrationPoint(node) ||
              this.IsHtmlIntegrationPoint(node)) {
              break;
            }
          }
          return this.ApplyThisInsertionMode(valueToken);
        }
        IElement adjustedCurrentNode = (this.context != null &&
            this.openElements.Count == 1) ?
          this.context : this.GetCurrentNode(); // adjusted current node

        string namespaceValue = adjustedCurrentNode.GetNamespaceURI();
        var mathml = false;
        if (HtmlCommon.SVG_NAMESPACE.Equals(namespaceValue,
          StringComparison.Ordinal)) {
          if (valueName.Equals("altglyph", StringComparison.Ordinal)) {
            tag.SetName("altGlyph");
          } else if (valueName.Equals("altglyphdef",
            StringComparison.Ordinal)) {
            tag.SetName("altGlyphDef");
          } else if (valueName.Equals("altglyphitem",
            StringComparison.Ordinal)) {
            tag.SetName("altGlyphItem");
          } else if (valueName.Equals("animatecolor",
            StringComparison.Ordinal)) {
            tag.SetName("animateColor");
          } else if (valueName.Equals("animatemotion",
            StringComparison.Ordinal)) {
            tag.SetName("animateMotion");
          } else if (valueName.Equals("animatetransform",
            StringComparison.Ordinal)) {
            tag.SetName("animateTransform");
          } else if (valueName.Equals("clippath", StringComparison.Ordinal)) {
            tag.SetName("clipPath");
          } else if (valueName.Equals("feblend", StringComparison.Ordinal)) {
            tag.SetName("feBlend");
          } else if (valueName.Equals("fecolormatrix",
            StringComparison.Ordinal)) {
            tag.SetName("feColorMatrix");
          } else if (valueName.Equals("fecomponenttransfer",
            StringComparison.Ordinal)) {
            tag.SetName("feComponentTransfer");
          } else if (valueName.Equals("fecomposite",
            StringComparison.Ordinal)) {
            tag.SetName("feComposite");
          } else if (valueName.Equals("feconvolvematrix",
            StringComparison.Ordinal)) {
            tag.SetName("feConvolveMatrix");
          } else if (valueName.Equals("fediffuselighting",
            StringComparison.Ordinal)) {
            tag.SetName("feDiffuseLighting");
          } else if (valueName.Equals("fedisplacementmap",
            StringComparison.Ordinal)) {
            tag.SetName("feDisplacementMap");
          } else if (valueName.Equals("fedistantlight",
            StringComparison.Ordinal)) {
            tag.SetName("feDistantLight");
          } else if (valueName.Equals("feflood", StringComparison.Ordinal)) {
            tag.SetName("feFlood");
          } else if (valueName.Equals("fefunca", StringComparison.Ordinal)) {
            tag.SetName("feFuncA");
          } else if (valueName.Equals("fefuncb", StringComparison.Ordinal)) {
            tag.SetName("feFuncB");
          } else if (valueName.Equals("fefuncg", StringComparison.Ordinal)) {
            tag.SetName("feFuncG");
          } else if (valueName.Equals("fefuncr", StringComparison.Ordinal)) {
            tag.SetName("feFuncR");
          } else if (valueName.Equals("fegaussianblur",
            StringComparison.Ordinal)) {
            tag.SetName("feGaussianBlur");
          } else if (valueName.Equals("feimage", StringComparison.Ordinal)) {
            tag.SetName("feImage");
          } else if (valueName.Equals("femerge", StringComparison.Ordinal)) {
            tag.SetName("feMerge");
          } else if (valueName.Equals("femergenode",
            StringComparison.Ordinal)) {
            tag.SetName("feMergeNode");
          } else if (valueName.Equals("femorphology",
            StringComparison.Ordinal)) {
            tag.SetName("feMorphology");
          } else if (valueName.Equals("feoffset", StringComparison.Ordinal)) {
            tag.SetName("feOffset");
          } else if (valueName.Equals("fepointlight",
            StringComparison.Ordinal)) {
            tag.SetName("fePointLight");
          } else if (valueName.Equals("fespecularlighting",
            StringComparison.Ordinal)) {
            tag.SetName("feSpecularLighting");
          } else if (valueName.Equals("fespotlight",
            StringComparison.Ordinal)) {
            tag.SetName("feSpotLight");
          } else if (valueName.Equals("fetile", StringComparison.Ordinal)) {
            tag.SetName("feTile");
          } else if (valueName.Equals("feturbulence",
            StringComparison.Ordinal)) {
            tag.SetName("feTurbulence");
          } else if (valueName.Equals("foreignobject",
            StringComparison.Ordinal)) {
            tag.SetName("foreignObject");
          } else if (valueName.Equals("glyphref", StringComparison.Ordinal)) {
            tag.SetName("glyphRef");
          } else if (valueName.Equals("lineargradient",
            StringComparison.Ordinal)) {
            tag.SetName("linearGradient");
          } else if (valueName.Equals("radialgradient",
            StringComparison.Ordinal)) {
            tag.SetName("radialGradient");
          } else if (valueName.Equals("textpath", StringComparison.Ordinal)) {
            tag.SetName("textPath");
          }
          this.AdjustSvgAttributes(tag);
        } else if (HtmlCommon.MATHML_NAMESPACE.Equals(namespaceValue,
          StringComparison.Ordinal)) {
          this.AdjustMathMLAttributes(tag);
          mathml = true;
        }
        this.AdjustForeignAttributes(tag);
        // Console.WriteLine("openel " + (Implode(openElements)));
        // Console.WriteLine("Inserting " + tag + ", " + namespaceValue);
        Element e = this.InsertForeignElement(tag, namespaceValue);
        if (mathml && tag.GetName().Equals("annotation-xml",
          StringComparison.Ordinal)) {
          string encoding = tag.GetAttribute("encoding");
          if (encoding != null) {
            encoding = DataUtilities.ToLowerCaseAscii(encoding);
            if (encoding.Equals("text/Html", StringComparison.Ordinal) ||
              encoding.Equals("application/xhtml+xml",
                StringComparison.Ordinal)) {
              this.integrationElements.Add(e);
            }
          }
        }
        if (tag.IsSelfClosing()) {
          if (valueName.Equals("script", StringComparison.Ordinal) &&
            this.GetCurrentNode().GetNamespaceURI()
            .Equals(HtmlCommon.SVG_NAMESPACE)) {
            tag.AckSelfClosing();
            this.ApplyEndTag("script", this.insertionMode);
          } else {
            this.PopCurrentNode();
            tag.AckSelfClosing();
          }
        }
        return true;
      } else if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        var tag = (EndTagToken)this.GetToken(valueToken);
        string valueName = tag.GetName();
        if (valueName.Equals("script", StringComparison.Ordinal) &&
          HtmlCommon.IsSvgElement(this.GetCurrentNode(), "script")) {
          this.PopCurrentNode();
        } else {
          if (!DataUtilities.ToLowerCaseAscii(
            this.GetCurrentNode().GetLocalName()).Equals(valueName)) {
            this.ParseError();
          }
          int originalSize = this.openElements.Count;
          for (int i1 = originalSize - 1; i1 >= 0; --i1) {
            if (i1 == 0) {
              return true;
            }
            IElement node = this.openElements[i1];
            if (i1 < originalSize - 1 &&
              HtmlCommon.HTML_NAMESPACE.Equals(node.GetNamespaceURI(),
              StringComparison.Ordinal)) {
              this.noforeign = true;
              return this.ApplyThisInsertionMode(valueToken);
            }
            string nodeName =
              DataUtilities.ToLowerCaseAscii(node.GetLocalName());
            if (valueName.Equals(nodeName, StringComparison.Ordinal)) {
              while (true) {
                IElement node2 = this.PopCurrentNode();
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
          this.ApplyThisInsertionMode(valueToken) :
          true;
      }
      throw new InvalidOperationException();
    }

    private const string XhtmlStrict =
      "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd";

    private static string Implode<T>(IList<T> list) {
      var b = new StringBuilder();
      for (var i = 0; i < list.Count; ++i) {
        if (i > 0) {
          b.Append(", ");
        }
        b.Append(list[i].ToString());
      }
      return b.ToString();
    }

    private const string Xhtml11 =
      "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd";

    private bool ApplyThisInsertionMode(int token) {
      return this.ApplyInsertionMode(token, this.insertionMode);
    }

    private bool ApplyInsertionMode(int token, InsertionMode insMode) {
      /*Console.WriteLine("[[" + String.Format("{0:X8}" , token) + " " +
        this.GetToken(token) + " " + (insMode == null ? this.insertionMode :
         insMode) + " " + this.IsForeignContext(token) + "(" +
         this.noforeign + ")");
       if (this.openElements.Count > 0) {
      // Console.WriteLine(Implode(this.openElements));
       }*/ if (!this.noforeign && this.IsForeignContext(token)) {
        return this.ApplyForeignContext(token);
      }
      this.noforeign = false;
      switch (insMode) {
        case InsertionMode.Initial: {
          if (token == 0x09 || token == 0x0a ||
            token == 0x0c || token == 0x0d || token ==
            0x20) {
            return false;
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            var doctype = (DocTypeToken)this.GetToken(token);
            string doctypeName = doctype.Name;
            string doctypePublic = doctype.ValuePublicID;
            string doctypeSystem = doctype.ValueSystemID;
            doctypeName = (doctypeName == null) ? String.Empty :
              doctypeName.ToString();
            doctypePublic = doctypePublic?.ToString();
            doctypeSystem = doctypeSystem?.ToString();
            bool matchesHtml = "html".Equals(doctypeName,
                StringComparison.Ordinal);
            bool hasSystemId = doctype.ValueSystemID != null;
            if (!matchesHtml || doctypePublic != null ||
              (doctypeSystem != null && !"about:legacy-compat"
                .Equals(doctypeSystem))) {
              string h4public = "-//W3C//dtd html 4.0//EN";
              string html401public = "-//W3C//dtd html 4.01//EN";
              string xhtmlstrictpublic = "-//W3C//DTD XHTML 1.0 Strict//EN";
              string html4system = "http://www.w3.org/TR/REC-html40/strict.dtd";
              string html401system = "http://www.w3.org/TR/html4/strict.dtd";
              bool html4 = matchesHtml && h4public.Equals(doctypePublic,
                  StringComparison.Ordinal) && (doctypeSystem == null ||
                  html4system.Equals(doctypeSystem,
                    StringComparison.Ordinal));
              bool html401 = matchesHtml && html401public.Equals(doctypePublic,
                  StringComparison.Ordinal) && (doctypeSystem ==
                  null || html401system.Equals(doctypeSystem,
                    StringComparison.Ordinal));
              bool xhtml = matchesHtml &&
                xhtmlstrictpublic.Equals(doctypePublic,
                  StringComparison.Ordinal) &&
                XhtmlStrict.Equals(doctypeSystem, StringComparison.Ordinal);
              string xhtmlPublic = "-//W3C//DTD XHTML 1.1//EN";
              bool xhtml11 = matchesHtml &&
                xhtmlPublic.Equals(doctypePublic, StringComparison.Ordinal) &&
                Xhtml11.Equals(doctypeSystem, StringComparison.Ordinal);
              if (!html4 && !html401 && !xhtml && !xhtml11) {
                this.ParseError();
              }
            }
            doctypePublic = doctypePublic ?? String.Empty;
            doctypeSystem = doctypeSystem ?? String.Empty;
            var doctypeNode = new DocumentType(
              doctypeName,
              doctypePublic,
              doctypeSystem);
            this.valueDocument.Doctype = doctypeNode;
            this.valueDocument.AppendChild(doctypeNode);
            string doctypePublicLC = null;
            if (!"about:srcdoc".Equals(this.valueDocument.Address,
              StringComparison.Ordinal)) {
              if (!matchesHtml || doctype.ForceQuirks) {
                this.valueDocument.SetMode(DocumentMode.QuirksMode);
              } else {
                doctypePublicLC =
                  DataUtilities.ToLowerCaseAscii(doctypePublic);
                if ("html".Equals(doctypePublicLC,
                  StringComparison.Ordinal) ||
                  "-//w3o//dtd w3 html strict 3.0//en//"
                  .Equals(doctypePublicLC) ||
                  "-/w3c/dtd html 4.0 transitional/en".Equals(
                    doctypePublicLC,
                    StringComparison.Ordinal)) {
                  this.valueDocument.SetMode(DocumentMode.QuirksMode);
                } else if (doctypePublic.Length > 0) {
                  foreach (var id in quirksModePublicIdPrefixes) {
                    if (
                      doctypePublicLC.StartsWith(
                        id,
                        StringComparison.Ordinal)) {
                      this.valueDocument.SetMode(DocumentMode.QuirksMode);
                      break;
                    }
                  }
                }
              }
              if (this.valueDocument.GetMode() != DocumentMode.QuirksMode) {
                doctypePublicLC = doctypePublicLC ??
                  DataUtilities.ToLowerCaseAscii(doctypePublic);
                if ("http://www.ibm.com/data/dtd/v11/ibmxhtml1-transitional.dtd"
                  .Equals(
                    DataUtilities.ToLowerCaseAscii(doctypeSystem)) ||
                  (!hasSystemId && doctypePublicLC.StartsWith(
                  "-//w3c//dtd html 4.01 frameset//",
                  StringComparison.Ordinal)) || (!hasSystemId &&
                    doctypePublicLC.StartsWith(
                      "-//w3c//dtd html 4.01 transitional//",
                      StringComparison.Ordinal))) {
                  this.valueDocument.SetMode(DocumentMode.QuirksMode);
                }
              }
              if (this.valueDocument.GetMode() != DocumentMode.QuirksMode) {
                doctypePublicLC = doctypePublicLC ??
                  DataUtilities.ToLowerCaseAscii(doctypePublic);
                if (
                  doctypePublicLC.StartsWith(
                    "-//w3c//dtd xhtml 1.0 frameset//",
                    StringComparison.Ordinal) || doctypePublicLC.StartsWith(
                    "-//w3c//dtd xhtml 1.0 transitional//",
                    StringComparison.Ordinal) || (hasSystemId &&
                    doctypePublicLC.StartsWith(
                      "-//w3c//dtd html 4.01 frameset//",
                      StringComparison.Ordinal)) || (hasSystemId &&
                    doctypePublicLC.StartsWith(
                      "-//w3c//dtd html 4.01 transitional//",
                      StringComparison.Ordinal))) {
                  this.valueDocument.SetMode(DocumentMode.LimitedQuirksMode);
                }
              }
            }
            this.insertionMode = InsertionMode.BeforeHtml;
            return true;
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToDocument(token);

            return true;
          }
          if (!"about:srcdoc".Equals(this.valueDocument.Address,
            StringComparison.Ordinal)) {
            this.ParseError();
            this.valueDocument.SetMode(DocumentMode.QuirksMode);
          }
          this.insertionMode = InsertionMode.BeforeHtml;
          return this.ApplyThisInsertionMode(token);
        }
        case InsertionMode.BeforeHtml: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ParseError();
            return false;
          }
          if (token == 0x09 || token == 0x0a ||
            token == 0x0c || token == 0x0d || token ==
            0x20) {
            return false;
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToDocument(token);

            return true;
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if ("html".Equals(valueName, StringComparison.Ordinal)) {
              this.AddHtmlElement(tag);
              this.insertionMode = InsertionMode.BeforeHead;
              return true;
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (TagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (!"html".Equals(valueName, StringComparison.Ordinal) &&
              !"br".Equals(valueName, StringComparison.Ordinal) &&
              !"head".Equals(valueName, StringComparison.Ordinal) &&
              !"body".Equals(valueName, StringComparison.Ordinal)) {
              this.ParseError();
              return false;
            }
          }
          var valueElement = new Element();
          valueElement.SetLocalName("html");
          valueElement.SetNamespace(HtmlCommon.HTML_NAMESPACE);
          this.valueDocument.AppendChild(valueElement);
          this.openElements.Add(valueElement);
          this.insertionMode = InsertionMode.BeforeHead;
          return this.ApplyThisInsertionMode(token);
        }
        case InsertionMode.BeforeHead: {
          if (token == 0x09 || token == 0x0a ||
            token == 0x0c || token == 0x0d || token ==
            0x20) {
            return false;
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToCurrentNode(token);
            return true;
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ParseError();
            return false;
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if ("html".Equals(valueName, StringComparison.Ordinal)) {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
              return true;
            } else if ("head".Equals(valueName, StringComparison.Ordinal)) {
              Element valueElement = this.AddHtmlElement(tag);
              this.headElement = valueElement;
              this.insertionMode = InsertionMode.InHead;
              return true;
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (TagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if ("head".Equals(valueName, StringComparison.Ordinal) ||
              "br".Equals(valueName, StringComparison.Ordinal) ||
              "body".Equals(valueName, StringComparison.Ordinal) ||
              "html".Equals(valueName, StringComparison.Ordinal)) {
              this.ApplyStartTag("head", insMode);
              return this.ApplyThisInsertionMode(token);
            } else {
              this.ParseError();
              return false;
            }
          }
          this.ApplyStartTag("head", insMode);
          return this.ApplyThisInsertionMode(token);
        }
        case InsertionMode.InHead: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToCurrentNode(token);
            return true;
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ParseError();
            return false;
          }
          if (token == 0x09 || token == 0x0a ||
            token == 0x0c || token == 0x0d || token ==
            0x20) {
            this.InsertCharacter(this.GetCurrentNode(), token);
            return true;
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if ("html".Equals(valueName, StringComparison.Ordinal)) {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
              return true;
            } else if ("base".Equals(valueName, StringComparison.Ordinal) ||
              "bgsound".Equals(valueName, StringComparison.Ordinal) ||
              "basefont".Equals(valueName, StringComparison.Ordinal) ||
              "link".Equals(valueName, StringComparison.Ordinal)) {
              Element e = this.AddHtmlElementNoPush(tag);
              if (this.baseurl == null && "base".Equals(valueName,
                StringComparison.Ordinal)) {
                // Get the valueDocument _base URL
                this.baseurl = e.GetAttribute("href");
              }
              tag.AckSelfClosing();
              return true;
            } else if ("meta".Equals(valueName, StringComparison.Ordinal)) {
              Element valueElement = this.AddHtmlElementNoPush(tag);
              tag.AckSelfClosing();
              if (this.encoding.GetConfidence() ==
                EncodingConfidence.Tentative) {
                string charset = valueElement.GetAttribute("charset");
                if (charset != null) {
                  charset = Encodings.ResolveAlias(charset);
                  /*(TextEncoding.isAsciiCompatible(charset) ||
                  "utf-16be".Equals(charset) ||
                  "utf-16le".Equals(charset)) */ this.ChangeEncoding(charset);
                  if (this.encoding.GetConfidence() ==
                    EncodingConfidence.Certain) {
                    this.inputStream.DisableBuffer();
                  }
                  return true;
                }
                string value = DataUtilities.ToLowerCaseAscii(
                    valueElement.GetAttribute("http-equiv"));
                if ("content-type".Equals(value, StringComparison.Ordinal)) {
                  value = valueElement.GetAttribute("content");
                  if (value != null) {
                    value = DataUtilities.ToLowerCaseAscii(value);
                    charset = CharsetSniffer.ExtractCharsetFromMeta(value);
                    if (true) {
                      // TODO
                      this.ChangeEncoding(charset);
                      if (this.encoding.GetConfidence() ==
                        EncodingConfidence.Certain) {
                        this.inputStream.DisableBuffer();
                      }
                      return true;
                    }
                  }
                } else if ("content-language".Equals(value,
                  StringComparison.Ordinal)) {
                  // HTML5 requires us to use this algorithm
                  // to Parse the Content-Language, rather than
                  // use HTTP parsing (with HeaderParser.GetLanguages)
                  // NOTE: this pragma is nonconforming
                  value = valueElement.GetAttribute("content");
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
              if (this.encoding.GetConfidence() == EncodingConfidence.Certain) {
                this.inputStream.DisableBuffer();
              }
              return true;
            } else if ("title".Equals(valueName, StringComparison.Ordinal)) {
              this.AddHtmlElement(tag);
              this.state = TokenizerState.RcData;
              this.originalInsertionMode = this.insertionMode;
              this.insertionMode = InsertionMode.Text;
              return true;
            } else if ("noframes".Equals(valueName,
              StringComparison.Ordinal) ||
              "style".Equals(valueName, StringComparison.Ordinal)) {
              this.AddHtmlElement(tag);
              this.state = TokenizerState.RawText;
              this.originalInsertionMode = this.insertionMode;
              this.insertionMode = InsertionMode.Text;
              return true;
            } else if ("noscript".Equals(valueName,
              StringComparison.Ordinal)) {
              this.AddHtmlElement(tag);
              this.insertionMode = InsertionMode.InHeadNoscript;
              return true;
            } else if ("script".Equals(valueName, StringComparison.Ordinal)) {
              this.AddHtmlElement(tag);
              this.state = TokenizerState.ScriptData;
              this.originalInsertionMode = this.insertionMode;
              this.insertionMode = InsertionMode.Text;
              return true;
            } else if ("template".Equals(valueName,
              StringComparison.Ordinal)) {
              Element e = this.AddHtmlElement(tag);
              this.InsertFormattingMarker(tag, e);
              this.framesetOk = false;
              this.insertionMode = InsertionMode.InTemplate;
              this.templateModes.Add(InsertionMode.InTemplate);
              return true;
            } else if ("head".Equals(valueName, StringComparison.Ordinal)) {
              this.ParseError();
              return false;
            } else {
              this.ApplyEndTag("head", insMode);
              return this.ApplyThisInsertionMode(token);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (TagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if ("head".Equals(valueName, StringComparison.Ordinal)) {
              this.openElements.RemoveAt(this.openElements.Count - 1);
              this.insertionMode = InsertionMode.AfterHead;
              return true;
            } else if ("template".Equals(valueName,
              StringComparison.Ordinal)) {
              if (!this.HasHtmlOpenElement("template")) {
                this.ParseError();
                return false;
              }
              this.GenerateImpliedEndTagsThoroughly();
              IElement ie = this.GetCurrentNode();
              if (!HtmlCommon.IsHtmlElement(ie, "template")) {
                this.ParseError();
              }
              this.PopUntilHtmlElementPopped("template");
              this.ClearFormattingToMarker();
              if (this.templateModes.Count > 0) {
                this.templateModes.RemoveAt(this.templateModes.Count - 1);
              }
              this.ResetInsertionMode();
              return true;
            } else if (!(
              "br".Equals(valueName, StringComparison.Ordinal) ||
              "body".Equals(valueName, StringComparison.Ordinal) ||
              "html".Equals(valueName, StringComparison.Ordinal))) {
              this.ParseError();
              return false;
            }
            this.ApplyEndTag("head", insMode);
            return this.ApplyThisInsertionMode(token);
          } else {
            this.ApplyEndTag("head", insMode);
            return this.ApplyThisInsertionMode(token);
          }
        }
        case InsertionMode.AfterHead: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            if (token == 0x20 || token == 0x09 || token == 0x0a ||
              token == 0x0c || token == 0x0d) {
              this.InsertCharacter(
                this.GetCurrentNode(),
                token);
            } else {
              this.ApplyStartTag("body", insMode);
              this.framesetOk = true;
              return this.ApplyThisInsertionMode(token);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ParseError();
            return false;
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("html", StringComparison.Ordinal)) {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
              return true;
            } else if (valueName.Equals("body", StringComparison.Ordinal)) {
              this.AddHtmlElement(tag);
              this.framesetOk = false;
              this.insertionMode = InsertionMode.InBody;
              return true;
            } else if (valueName.Equals("frameset",
              StringComparison.Ordinal)) {
              this.AddHtmlElement(tag);
              this.insertionMode = InsertionMode.InFrameset;
              return true;
            } else if ("base".Equals(valueName, StringComparison.Ordinal) ||
              "bgsound".Equals(valueName) ||
              "basefont".Equals(valueName, StringComparison.Ordinal) ||
              "link".Equals(valueName, StringComparison.Ordinal) ||
              "noframes".Equals(valueName, StringComparison.Ordinal) ||
              "script".Equals(valueName, StringComparison.Ordinal) ||
              "template".Equals(valueName, StringComparison.Ordinal) ||
              "style".Equals(valueName, StringComparison.Ordinal) ||
              "title".Equals(valueName, StringComparison.Ordinal) ||
              "meta".Equals(valueName, StringComparison.Ordinal)) {
              this.ParseError();
              this.openElements.Add(this.headElement);
              this.ApplyInsertionMode(token, InsertionMode.InHead);
              this.openElements.Remove(this.headElement);
              return true;
            } else if ("head".Equals(valueName, StringComparison.Ordinal)) {
              this.ParseError();
              return false;
            } else {
              this.ApplyStartTag("body", insMode);
              this.framesetOk = true;
              return this.ApplyThisInsertionMode(token);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("body", StringComparison.Ordinal) ||
              valueName.Equals("html", StringComparison.Ordinal) ||
              valueName.Equals("br", StringComparison.Ordinal)) {
              this.ApplyStartTag("body", insMode);
              this.framesetOk = true;
              return this.ApplyThisInsertionMode(token);
            } else if (valueName.Equals("template",
              StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(token, InsertionMode.InHead);
            } else {
              this.ParseError();
              return false;
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToCurrentNode(token);

            return true;
          } else if (token == TOKEN_EOF) {
            this.ApplyStartTag("body", insMode);
            this.framesetOk = true;
            return this.ApplyThisInsertionMode(token);
          }
          return true;
        }
        case InsertionMode.Text: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            if (insMode != this.insertionMode) {
              this.InsertCharacter(this.GetCurrentNode(), token);
            } else {
              Text textNode =
                this.GetTextNodeToInsert(this.GetCurrentNode());
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
                  sb.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) |
                    0xd800));
                  sb.Append((char)(((ch - 0x10000) & 0x3ff) | 0xdc00));
                }
                token = this.ParserRead();
                if ((token & TOKEN_TYPE_MASK) != TOKEN_CHARACTER) {
                  this.tokenQueue.Insert(0, token);
                  break;
                }
                ch = token;
              }
            }
            return true;
          } else if (token == TOKEN_EOF) {
            this.ParseError();
            this.openElements.RemoveAt(this.openElements.Count - 1);
            this.insertionMode = this.originalInsertionMode;
            return this.ApplyThisInsertionMode(token);
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            this.openElements.RemoveAt(this.openElements.Count - 1);
            this.insertionMode = this.originalInsertionMode;
          }
          return true;
        }
        case InsertionMode.InTemplate: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE ||
            (token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER ||
            (token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            return this.ApplyInsertionMode(
                token,
                InsertionMode.InBody);
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("base", StringComparison.Ordinal) ||
              valueName.Equals("title", StringComparison.Ordinal) ||
              valueName.Equals("template", StringComparison.Ordinal) ||
              valueName.Equals("basefont", StringComparison.Ordinal) ||
              valueName.Equals("bgsound", StringComparison.Ordinal) ||
              valueName.Equals("meta", StringComparison.Ordinal) ||
              valueName.Equals("link", StringComparison.Ordinal) ||
              valueName.Equals("noframes", StringComparison.Ordinal) ||
              valueName.Equals("style", StringComparison.Ordinal) ||
              valueName.Equals("script", StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InHead);
            }
            InsertionMode newMode = InsertionMode.InBody;
            if (valueName.Equals("caption", StringComparison.Ordinal) ||
              valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal) ||
              valueName.Equals("tfoot", StringComparison.Ordinal) ||
              valueName.Equals("colgroup", StringComparison.Ordinal)) {
              newMode = InsertionMode.InTable;
            } else if (valueName.Equals("col", StringComparison.Ordinal)) {
              newMode = InsertionMode.InColumnGroup;
            } else if (valueName.Equals("tr", StringComparison.Ordinal)) {
              newMode = InsertionMode.InTableBody;
            } else if (valueName.Equals("td", StringComparison.Ordinal) ||
              valueName.Equals("th", StringComparison.Ordinal)) {
              newMode = InsertionMode.InRow;
            }
            if (this.templateModes.Count > 0) {
              this.templateModes.RemoveAt(this.templateModes.Count - 1);
            }
            this.templateModes.Add(newMode);
            this.insertionMode = newMode;
            return this.ApplyThisInsertionMode(token);
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("template", StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InHead);
            } else {
              this.ParseError();
              return true;
            }
          }
          if (token == TOKEN_EOF) {
            if (!this.HasHtmlOpenElement("template")) {
              this.StopParsing();
              return true;
            } else {
              this.ParseError();
            }
            this.PopUntilHtmlElementPopped("template");
            this.ClearFormattingToMarker();
            if (this.templateModes.Count > 0) {
              this.templateModes.RemoveAt(this.templateModes.Count - 1);
            }
            this.ResetInsertionMode();
            return this.ApplyThisInsertionMode(token);
          }
          return false;
        }

        case InsertionMode.InBody: {
          if (token == 0) {
            this.ParseError();
            return true;
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToCurrentNode(token);

            return true;
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ParseError();
            return true;
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            // Console.WriteLine(String.Empty + ((char)token) + " " +
            // (this.GetCurrentNode().GetTagName()));
            int ch = token;
            if (ch !=
              0) {
              this.ReconstructFormatting();
            }
            Text textNode = this.GetTextNodeToInsert(this.GetCurrentNode());
            if (textNode == null) {
              throw new InvalidOperationException();
            }
            while (true) {
              // Read multiple characters at once
              if (ch == 0) {
                this.ParseError();
              } else {
                StringBuilder sb = textNode.ValueText;
                if (ch <= 0xffff) {
                  {
                    sb.Append((char)ch);
                  }
                } else if (ch <= 0x10ffff) {
                  sb.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) |
                    0xd800));
                  sb.Append((char)(((ch - 0x10000) & 0x3ff) | 0xdc00));
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
              token = this.ParserRead();
              if ((token & TOKEN_TYPE_MASK) != TOKEN_CHARACTER) {
                this.tokenQueue.Insert(0, token);
                break;
              }
              // Console.WriteLine("{0} {1}"
              // , (char)token, GetCurrentNode().GetTagName());
              ch = token;
            }
            return true;
          } else if (token == TOKEN_EOF) {
            if (this.templateModes.Count > 0) {
              return this.ApplyInsertionMode(token, InsertionMode.InTemplate);
            } else {
              foreach (var e in this.openElements) {
                if (!HtmlCommon.IsHtmlElement(e, "dd") &&
                  !HtmlCommon.IsHtmlElement(e, "dt") &&
                  !HtmlCommon.IsHtmlElement(e, "li") &&
                  !HtmlCommon.IsHtmlElement(e, "option") &&
                  !HtmlCommon.IsHtmlElement(e, "optgroup") &&
                  !HtmlCommon.IsHtmlElement(e, "p") &&
                  !HtmlCommon.IsHtmlElement(e, "tbody") &&
                  !HtmlCommon.IsHtmlElement(e, "td") &&
                  !HtmlCommon.IsHtmlElement(e, "tfoot") &&
                  !HtmlCommon.IsHtmlElement(e, "th") &&
                  !HtmlCommon.IsHtmlElement(e, "tr") &&
                  !HtmlCommon.IsHtmlElement(e, "thead") &&
                  !HtmlCommon.IsHtmlElement(e, "body") &&
                  !HtmlCommon.IsHtmlElement(e, "html")) {
                  this.ParseError();
                }
              }
              this.StopParsing();
            }
          }
          if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            // START TAGS
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if ("html".Equals(valueName, StringComparison.Ordinal)) {
              this.ParseError();
              if (this.HasHtmlOpenElement("template")) {
                return false;
              }
              ((Element)this.openElements[0]).MergeAttributes(tag);

              return true;
            } else if ("base".Equals(valueName, StringComparison.Ordinal) ||
              "template".Equals(valueName, StringComparison.Ordinal) ||
              "bgsound".Equals(valueName, StringComparison.Ordinal) ||
              "basefont".Equals(valueName, StringComparison.Ordinal) ||
              "link".Equals(valueName, StringComparison.Ordinal) ||
              "noframes".Equals(valueName, StringComparison.Ordinal) ||
              "script".Equals(valueName, StringComparison.Ordinal) ||
              "style".Equals(valueName, StringComparison.Ordinal) ||
              "title".Equals(valueName, StringComparison.Ordinal) ||
              "meta".Equals(valueName, StringComparison.Ordinal)) {
              this.ApplyInsertionMode(token, InsertionMode.InHead);
              return true;
            } else if ("body".Equals(valueName, StringComparison.Ordinal)) {
              this.ParseError();
              if (this.openElements.Count <= 1 ||
                !HtmlCommon.IsHtmlElement(this.openElements[1], "body")) {
                return false;
              }
              if (this.HasHtmlOpenElement("template")) {
                return false;
              }
              this.framesetOk = false;
              ((Element)this.openElements[1]).MergeAttributes(tag);

              return true;
            } else if ("frameset".Equals(valueName,
              StringComparison.Ordinal)) {
              this.ParseError();
              if (!this.framesetOk || this.openElements.Count <= 1 ||
                !HtmlCommon.IsHtmlElement(this.openElements[1], "body")) {
                return false;
              }
              var parent = (Node)this.openElements[1].GetParentNode();
              if (parent != null) {
                parent.RemoveChild((Node)this.openElements[1]);
              }
              while (this.openElements.Count > 1) {
                this.PopCurrentNode();
              }
              this.AddHtmlElement(tag);
              this.insertionMode = InsertionMode.InFrameset;
              return true;
            } else if ("address".Equals(valueName,
              StringComparison.Ordinal) ||
              "article".Equals(valueName, StringComparison.Ordinal) ||
              "aside".Equals(valueName, StringComparison.Ordinal) ||
              "blockquote".Equals(valueName, StringComparison.Ordinal) ||
              "center".Equals(valueName) ||
              "details".Equals(valueName, StringComparison.Ordinal) ||
              "dialog".Equals(valueName, StringComparison.Ordinal) ||
              "dir".Equals(valueName, StringComparison.Ordinal) ||
              "div".Equals(valueName, StringComparison.Ordinal) ||
              "dl".Equals(valueName, StringComparison.Ordinal) ||
              "fieldset".Equals(valueName, StringComparison.Ordinal) ||
              "figcaption".Equals(valueName, StringComparison.Ordinal) ||
              "figure".Equals(valueName) ||
              "footer".Equals(valueName, StringComparison.Ordinal) ||
              "header".Equals(valueName, StringComparison.Ordinal) ||
              "main".Equals(valueName, StringComparison.Ordinal) ||
              "nav".Equals(valueName, StringComparison.Ordinal) ||
              "ol".Equals(valueName, StringComparison.Ordinal) ||
              "p".Equals(valueName, StringComparison.Ordinal) ||
              "section".Equals(valueName, StringComparison.Ordinal) ||
              "summary".Equals(valueName, StringComparison.Ordinal) ||
              "ul".Equals(valueName, StringComparison.Ordinal)
            ) {
              this.CloseParagraph();
              this.AddHtmlElement(tag);
              return true;
            } else if ("h1".Equals(valueName, StringComparison.Ordinal) ||
              "h2".Equals(valueName, StringComparison.Ordinal) ||
              "h3".Equals(valueName, StringComparison.Ordinal) ||
              "h4".Equals(valueName, StringComparison.Ordinal) ||
              "h5".Equals(valueName, StringComparison.Ordinal) ||
              "h6".Equals(valueName, StringComparison.Ordinal)) {
              this.CloseParagraph();
              IElement node = this.GetCurrentNode();
              string name1 = node.GetLocalName();
              if ("h1".Equals(name1, StringComparison.Ordinal) ||
                "h2".Equals(name1, StringComparison.Ordinal) ||
                "h3".Equals(name1, StringComparison.Ordinal) ||
                "h4".Equals(name1, StringComparison.Ordinal) ||
                "h5".Equals(name1, StringComparison.Ordinal) ||
                "h6".Equals(name1, StringComparison.Ordinal)) {
                this.ParseError();
                this.openElements.RemoveAt(this.openElements.Count - 1);
              }
              this.AddHtmlElement(tag);
              return true;
            } else if ("pre".Equals(valueName, StringComparison.Ordinal) ||
              "listing".Equals(valueName, StringComparison.Ordinal)) {
              this.CloseParagraph();
              this.AddHtmlElement(tag);
              this.SkipLineFeed();
              this.framesetOk = false;
              return true;
            } else if ("form".Equals(valueName, StringComparison.Ordinal)) {
              if (this.formElement != null && !this.HasHtmlOpenElement(
                "template")) {
                this.ParseError();
                return false;
              }
              this.CloseParagraph();
              Element formElem = this.AddHtmlElement(tag);
              if (!this.HasHtmlOpenElement("template")) {
                this.formElement = formElem;
              }
              return true;
            } else if ("li".Equals(valueName, StringComparison.Ordinal)) {
              this.framesetOk = false;
              for (int i = this.openElements.Count - 1; i >= 0; --i) {
                IElement node = this.openElements[i];
                string nodeName = node.GetLocalName();
                if (HtmlCommon.IsHtmlElement(node, "li")) {
                  this.ApplyInsertionMode(
                    this.GetArtificialToken(TOKEN_END_TAG, "li"),
                    insMode);
                  break;
                }
                if (this.IsSpecialElement(node) &&
                  !"address".Equals(nodeName) &&
                  !"div".Equals(nodeName, StringComparison.Ordinal) &&
                  !"p".Equals(nodeName, StringComparison.Ordinal)) {
                  break;
                }
              }
              this.CloseParagraph();
              this.AddHtmlElement(tag);
              return true;
            } else if ("dd".Equals(valueName, StringComparison.Ordinal) ||
              "dt".Equals(valueName, StringComparison.Ordinal)) {
              this.framesetOk = false;
              for (int i = this.openElements.Count - 1; i >= 0; --i) {
                IElement node = this.openElements[i];
                string nodeName = node.GetLocalName();
                // Console.WriteLine("looping through %s",nodeName);
                if (nodeName.Equals("dd", StringComparison.Ordinal) ||
                  nodeName.Equals("dt", StringComparison.Ordinal)) {
                  this.ApplyEndTag(nodeName, insMode);
                  break;
                }
                if (this.IsSpecialElement(node) &&
                  !"address".Equals(nodeName, StringComparison.Ordinal) &&
                  !"div".Equals(nodeName, StringComparison.Ordinal) &&
                  !"p".Equals(nodeName, StringComparison.Ordinal)) {
                  break;
                }
              }
              this.CloseParagraph();
              this.AddHtmlElement(tag);
              return true;
            } else if ("plaintext".Equals(valueName,
              StringComparison.Ordinal)) {
              this.CloseParagraph();
              this.AddHtmlElement(tag);
              this.state = TokenizerState.PlainText;
              return true;
            } else if ("button".Equals(valueName, StringComparison.Ordinal)) {
              if (this.HasHtmlElementInScope("button")) {
                this.ParseError();
                this.ApplyEndTag("button", insMode);
                return this.ApplyThisInsertionMode(token);
              }
              this.ReconstructFormatting();
              this.AddHtmlElement(tag);
              this.framesetOk = false;
              return true;
            } else if ("a".Equals(valueName, StringComparison.Ordinal)) {
              while (true) {
                IElement node = null;
                for (int i = this.formattingElements.Count - 1; i >= 0; --i) {
                  FormattingElement fe = this.formattingElements[i];
                  if (fe.IsMarker()) {
                    break;
                  }
                  if (fe.Element.GetLocalName().Equals("a",
                    StringComparison.Ordinal)) {
                    node = fe.Element;
                    break;
                  }
                }
                if (node != null) {
                  this.ParseError();
                  this.ApplyEndTag("a", insMode);
                  this.RemoveFormattingElement(node);
                  this.openElements.Remove(node);
                } else {
                  break;
                }
              }
              this.ReconstructFormatting();
              this.PushFormattingElement(tag);
            } else if ("b".Equals(valueName, StringComparison.Ordinal) ||
              "big".Equals(valueName, StringComparison.Ordinal) ||
              "code".Equals(valueName, StringComparison.Ordinal) ||
              "em".Equals(valueName, StringComparison.Ordinal) ||
              "font".Equals(valueName, StringComparison.Ordinal) ||
              "i".Equals(valueName, StringComparison.Ordinal) ||
              "s".Equals(valueName, StringComparison.Ordinal) ||
              "small".Equals(valueName, StringComparison.Ordinal) ||
              "strike".Equals(valueName, StringComparison.Ordinal) ||
              "strong".Equals(valueName, StringComparison.Ordinal) ||
              "tt".Equals(valueName, StringComparison.Ordinal) ||
              "u".Equals(valueName, StringComparison.Ordinal)) {
              this.ReconstructFormatting();
              this.PushFormattingElement(tag);
            } else if ("nobr".Equals(valueName, StringComparison.Ordinal)) {
              this.ReconstructFormatting();
              if (this.HasHtmlElementInScope("nobr")) {
                this.ParseError();
                this.ApplyEndTag("nobr", insMode);
                this.ReconstructFormatting();
              }
              this.PushFormattingElement(tag);
            } else if ("table".Equals(valueName, StringComparison.Ordinal)) {
              if (this.valueDocument.GetMode() != DocumentMode.QuirksMode) {
                this.CloseParagraph();
              }
              this.AddHtmlElement(tag);
              this.framesetOk = false;
              this.insertionMode = InsertionMode.InTable;
              return true;
            } else if ("area".Equals(valueName, StringComparison.Ordinal) ||
              "br".Equals(valueName, StringComparison.Ordinal) ||
              "embed".Equals(valueName, StringComparison.Ordinal) ||
              "img".Equals(valueName, StringComparison.Ordinal) ||
              "keygen".Equals(valueName, StringComparison.Ordinal) ||
              "wbr".Equals(valueName, StringComparison.Ordinal)
            ) {
              this.ReconstructFormatting();
              this.AddHtmlElementNoPush(tag);
              tag.AckSelfClosing();
              this.framesetOk = false;
            } else if ("input".Equals(valueName, StringComparison.Ordinal)) {
              this.ReconstructFormatting();
              this.inputElement = this.AddHtmlElementNoPush(tag);
              tag.AckSelfClosing();
              string attr = this.inputElement.GetAttribute("type");
              if (attr == null || !"hidden"
                .Equals(DataUtilities.ToLowerCaseAscii(attr))) {
                this.framesetOk = false;
              }
            } else if ("param".Equals(valueName, StringComparison.Ordinal) ||
              "source".Equals(valueName) ||
              "track".Equals(valueName, StringComparison.Ordinal)
            ) {
              this.AddHtmlElementNoPush(tag);
              tag.AckSelfClosing();
            } else if ("hr".Equals(valueName, StringComparison.Ordinal)) {
              this.CloseParagraph();
              this.AddHtmlElementNoPush(tag);
              tag.AckSelfClosing();
              this.framesetOk = false;
            } else if ("image".Equals(valueName, StringComparison.Ordinal)) {
              this.ParseError();
              tag.SetName("img");
              return this.ApplyThisInsertionMode(token);
            } else if ("textarea".Equals(valueName,
              StringComparison.Ordinal)) {
              this.AddHtmlElement(tag);
              this.SkipLineFeed();
              this.state = TokenizerState.RcData;
              this.originalInsertionMode = this.insertionMode;
              this.framesetOk = false;
              this.insertionMode = InsertionMode.Text;
            } else if ("xmp".Equals(valueName, StringComparison.Ordinal)) {
              this.CloseParagraph();
              this.ReconstructFormatting();
              this.framesetOk = false;
              this.AddHtmlElement(tag);
              this.state = TokenizerState.RawText;
              this.originalInsertionMode = this.insertionMode;
              this.insertionMode = InsertionMode.Text;
            } else if ("iframe".Equals(valueName, StringComparison.Ordinal)) {
              this.framesetOk = false;
              this.AddHtmlElement(tag);
              this.state = TokenizerState.RawText;
              this.originalInsertionMode = this.insertionMode;
              this.insertionMode = InsertionMode.Text;
            } else if ("noembed".Equals(valueName,
              StringComparison.Ordinal)) {
              this.AddHtmlElement(tag);
              this.state = TokenizerState.RawText;
              this.originalInsertionMode = this.insertionMode;
              this.insertionMode = InsertionMode.Text;
            } else if ("select".Equals(valueName, StringComparison.Ordinal)) {
              this.ReconstructFormatting();
              this.AddHtmlElement(tag);
              this.framesetOk = false;
              this.insertionMode = (this.insertionMode ==
                  InsertionMode.InTable ||
                  this.insertionMode == InsertionMode.InCaption ||
                  this.insertionMode == InsertionMode.InTableBody ||
                  this.insertionMode == InsertionMode.InRow ||
                  this.insertionMode == InsertionMode.InCell) ?
                InsertionMode.InSelectInTable : InsertionMode.InSelect;
            } else if ("option".Equals(valueName,
              StringComparison.Ordinal) || "optgroup".Equals(valueName)) {
              if (this.GetCurrentNode().GetLocalName().Equals(
                "option",
                StringComparison.Ordinal)) {
                this.ApplyEndTag("option", insMode);
              }
              this.ReconstructFormatting();
              this.AddHtmlElement(tag);
            } else if ("rp".Equals(valueName, StringComparison.Ordinal) ||
              "rt".Equals(valueName, StringComparison.Ordinal)) {
              if (this.HasHtmlElementInScope("ruby")) {
                this.GenerateImpliedEndTagsExcept("rtc");
                if (!this.GetCurrentNode().GetLocalName().Equals(
                  "ruby",
                  StringComparison.Ordinal) &&
                  !this.GetCurrentNode().GetLocalName().Equals(
                  "rtc",
                  StringComparison.Ordinal)) {
                  this.ParseError();
                }
              }
              this.AddHtmlElement(tag);
            } else if ("rb".Equals(valueName, StringComparison.Ordinal) ||
              "rtc".Equals(valueName, StringComparison.Ordinal)) {
              if (this.HasHtmlElementInScope("ruby")) {
                this.GenerateImpliedEndTags();
                if (!this.GetCurrentNode().GetLocalName().Equals(
                  "ruby",
                  StringComparison.Ordinal)) {
                  this.ParseError();
                }
              }
              this.AddHtmlElement(tag);
            } else if ("applet".Equals(valueName, StringComparison.Ordinal) ||
              "marquee".Equals(valueName) ||
              "object".Equals(valueName, StringComparison.Ordinal)) {
              this.ReconstructFormatting();
              Element e = this.AddHtmlElement(tag);
              this.InsertFormattingMarker(tag, e);
              this.framesetOk = false;
            } else if ("math".Equals(valueName, StringComparison.Ordinal)) {
              this.ReconstructFormatting();
              this.AdjustMathMLAttributes(tag);
              this.AdjustForeignAttributes(tag);
              this.InsertForeignElement(
                tag,
                HtmlCommon.MATHML_NAMESPACE);
              if (tag.IsSelfClosing()) {
                tag.AckSelfClosing();
                this.PopCurrentNode();
              } else {
                // this.hasForeignContent = true;
              }
            } else if ("svg".Equals(valueName, StringComparison.Ordinal)) {
              this.ReconstructFormatting();
              this.AdjustSvgAttributes(tag);
              this.AdjustForeignAttributes(tag);
              this.InsertForeignElement(tag, HtmlCommon.SVG_NAMESPACE);
              if (tag.IsSelfClosing()) {
                tag.AckSelfClosing();
                this.PopCurrentNode();
              } else {
                // this.hasForeignContent = true;
              }
            } else if ("caption".Equals(valueName,
              StringComparison.Ordinal) ||
              "col".Equals(valueName, StringComparison.Ordinal) ||
              "colgroup".Equals(valueName, StringComparison.Ordinal) ||
              "frame".Equals(valueName, StringComparison.Ordinal) ||
              "head".Equals(valueName, StringComparison.Ordinal) ||
              "tbody".Equals(valueName, StringComparison.Ordinal) ||
              "td".Equals(valueName, StringComparison.Ordinal) ||
              "tfoot".Equals(valueName, StringComparison.Ordinal) ||
              "th".Equals(valueName, StringComparison.Ordinal) ||
              "thead".Equals(valueName, StringComparison.Ordinal) ||
              "tr".Equals(valueName, StringComparison.Ordinal)
            ) {
              this.ParseError();
              return false;
            } else {
              // Console.WriteLine("ordinary: " + tag);
              this.ReconstructFormatting();
              this.AddHtmlElement(tag);
            }
            return true;
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            // END TAGS
            // NOTE: Have all cases
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("template", StringComparison.Ordinal)) {
              this.ApplyInsertionMode(token, InsertionMode.InHead);
              return true;
            }
            if (valueName.Equals("body", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInScope("body")) {
                this.ParseError();
                return false;
              }
              foreach (var e in this.openElements) {
                string name2 = e.GetLocalName();
                if (!"dd".Equals(name2, StringComparison.Ordinal) &&
                  !"dt".Equals(name2, StringComparison.Ordinal) &&
                  !"li".Equals(name2, StringComparison.Ordinal) &&
                  !"option".Equals(name2, StringComparison.Ordinal) &&
                  !"optgroup".Equals(name2, StringComparison.Ordinal) &&
                  !"p".Equals(name2, StringComparison.Ordinal) &&
                  !"rb".Equals(name2, StringComparison.Ordinal) &&
                  !"tbody".Equals(name2, StringComparison.Ordinal) &&
                  !"td".Equals(name2, StringComparison.Ordinal) &&
                  !"tfoot".Equals(name2, StringComparison.Ordinal) &&
                  !"th".Equals(name2, StringComparison.Ordinal) &&
                  !"tr".Equals(name2, StringComparison.Ordinal) &&
                  !"thead".Equals(name2, StringComparison.Ordinal) &&
                  !"body".Equals(name2, StringComparison.Ordinal) &&
                  !"html".Equals(name2, StringComparison.Ordinal)) {
                  this.ParseError();
                  // token not ignored here
                }
              }
              this.insertionMode = InsertionMode.AfterBody;
            } else if (valueName.Equals("a", StringComparison.Ordinal) ||
              valueName.Equals("b", StringComparison.Ordinal) ||
              valueName.Equals("big", StringComparison.Ordinal) ||
              valueName.Equals("code", StringComparison.Ordinal) ||
              valueName.Equals("em", StringComparison.Ordinal) ||
              valueName.Equals("b", StringComparison.Ordinal) ||
              valueName.Equals("font", StringComparison.Ordinal) ||
              valueName.Equals("i", StringComparison.Ordinal) ||
              valueName.Equals("nobr", StringComparison.Ordinal) ||
              valueName.Equals("s", StringComparison.Ordinal) ||
              valueName.Equals("small", StringComparison.Ordinal) ||
              valueName.Equals("strike", StringComparison.Ordinal) ||
              valueName.Equals("strong", StringComparison.Ordinal) ||
              valueName.Equals("tt", StringComparison.Ordinal) ||
              valueName.Equals("u", StringComparison.Ordinal)) {
              if (
                HtmlCommon.IsHtmlElement(
                  this.GetCurrentNode(),
                  valueName)) {
                var found = false;
                for (int j = this.formattingElements.Count - 1; j >= 0; --j) {
                  FormattingElement fe = this.formattingElements[j];
                  if (this.GetCurrentNode().Equals(fe.Element)) {
                    found = true;
                  }
                }
                if (!found) {
                  this.PopCurrentNode();
                  return true;
                }
              }
              for (int i = 0; i < 8; ++i) {
                // Console.WriteLine("i=" + i);
                // Console.WriteLine("format before=" +
                // this.openElements[0].GetOwnerDocument());
                FormattingElement formatting = null;
                for (int j = this.formattingElements.Count - 1; j >= 0; --j) {
                  FormattingElement fe = this.formattingElements[j];
                  if (fe.IsMarker()) {
                    break;
                  }
                  if (fe.Element.GetLocalName().Equals(valueName,
                    StringComparison.Ordinal)) {
                    formatting = fe;
                    break;
                  }
                }
                if (formatting == null) {
                  // NOTE: Steps for "any other end tag"
                  // Console.WriteLine("no such formatting element");
                  for (int k = this.openElements.Count - 1;
                    k >= 0; --k) {
                    IElement node = this.openElements[k];
                    if (HtmlCommon.IsHtmlElement(node, valueName)) {
                      this.GenerateImpliedEndTagsExcept(valueName);
                      if (!node.Equals(this.GetCurrentNode())) {
                        this.ParseError();
                      }
                      while (true) {
                        IElement node2 = this.PopCurrentNode();
                        if (node2.Equals(node)) {
                          break;
                        }
                      }
                      break;
                    } else if (this.IsSpecialElement(node)) {
                      this.ParseError();
                      return false;
                    }
                  }
                  return true;
                }
                int formattingElementPos =
                  this.openElements.IndexOf(formatting.Element);
                // Console.WriteLine("Formatting Element: // " +
                // formatting.Element);
                if (formattingElementPos < 0) { // not found
                  this.ParseError();
                  // Console.WriteLine("Not in stack of open elements");
                  this.formattingElements.Remove(formatting);
                  return true;
                }
                // Console.WriteLine("Open elements[" + i + "]:");
                // Console.WriteLine(Implode(openElements));
                // Console.WriteLine("Formatting elements:");
                // Console.WriteLine(Implode(formattingElements));
                if (!this.HasHtmlElementInScope(formatting.Element)) {
                  this.ParseError();
                  return true;
                }
                if (!formatting.Element.Equals(this.GetCurrentNode())) {
                  this.ParseError();
                }
                IElement furthestBlock = null;
                var furthestBlockPos = -1;
                for (int j = formattingElementPos + 1;
                  j < this.openElements.Count; ++j) {
                  IElement e = this.openElements[j];
                  // Console.WriteLine("is special: // " + (// e) + "// " +
                  // (this.IsSpecialElement(e)));
                  if (this.IsSpecialElement(e)) {
                    furthestBlock = e;
                    furthestBlockPos = j;
                    break;
                  }
                }
                // Console.WriteLine("furthest block: // " + furthestBlock);
                if (furthestBlock == null) {
                  // Pop up to and including the
                  // formatting element
                  while (this.openElements.Count > formattingElementPos) {
                    this.PopCurrentNode();
                  }
                  this.formattingElements.Remove(formatting);
                  // Console.WriteLine("Open elements now [" + i + "]:");
                  // Console.WriteLine(Implode(openElements));
                  // Console.WriteLine("Formatting elements now:");
                  // Console.WriteLine(Implode(formattingElements));
                  break;
                }
                IElement commonAncestor =
                  this.openElements[formattingElementPos -
                    1];
                int bookmark = this.formattingElements.IndexOf(formatting);
                // Console.WriteLine("formel: {0}"
                // , this.openElements[formattingElementPos]);
                // Console.WriteLine("common ancestor: " + commonAncestor);
                // Console.WriteLine("Setting bookmark to {0} [len={1}]"
                // , bookmark, this.formattingElements.Count);
                IElement myNode = furthestBlock;
                IElement superiorNode = this.openElements[furthestBlockPos -
                    1];
                IElement lastNode = furthestBlock;
                for (int j = 0; ; j = Math.Min(j + 1, 4)) {
                  myNode = superiorNode;
                  FormattingElement nodeFE =
                    this.GetFormattingElement(myNode);
                  // Console.WriteLine("j="+j);
                  // Console.WriteLine("nodeFE="+nodeFE);
                  if (nodeFE == null) {
                    // Console.WriteLine("node not a formatting element");
                    superiorNode =
                      this.openElements[this.openElements.IndexOf(myNode) -
                        1];
                    this.openElements.Remove(myNode);
                    continue;
                  } else if (myNode.Equals(formatting.Element)) {
                    // Console.WriteLine("node is the formatting element");
                    break;
                  } else if (j >= 3) {
                    int nodeFEIndex = this.formattingElements.IndexOf(nodeFE);
                    this.formattingElements.Remove(nodeFE);
                    if (nodeFEIndex >= 0 && nodeFEIndex <= bookmark) {
                      --bookmark;
                    }
                    superiorNode =
                      this.openElements[this.openElements.IndexOf(myNode) -
                        1];
                    this.openElements.Remove(myNode);
                    continue;
                  }
                  IElement e = Element.FromToken(nodeFE.Token);
                  nodeFE.Element = e;
                  int io = this.openElements.IndexOf(myNode);
                  superiorNode = this.openElements[io - 1];
                  this.openElements[io] = e;
                  myNode = e;
                  if (lastNode.Equals(furthestBlock)) {
                    bookmark = this.formattingElements.IndexOf(nodeFE) + 1;
                    // Console.WriteLine("Moving bookmark to {0} [len={1}]"
                    // , bookmark, this.formattingElements.Count);
                  }
                  // NOTE: Because 'node' can only be a formatting
                  // element, the foster parenting rule doesn't
                  // apply here
                  if (lastNode.GetParentNode() != null) {
                    ((Node)lastNode.GetParentNode()).RemoveChild(
                      (Node)lastNode);
                  }
                  myNode.AppendChild(lastNode);
                  // Console.WriteLine("lastNode now: "+myNode);
                  lastNode = myNode;
                }
                // Console.WriteLine("lastNode: "+lastNode);
                if (HtmlCommon.IsHtmlElement(commonAncestor, "table") ||
                  HtmlCommon.IsHtmlElement(commonAncestor, "tr") ||
                  HtmlCommon.IsHtmlElement(commonAncestor, "tbody") ||
                  HtmlCommon.IsHtmlElement(commonAncestor, "thead") ||
                  HtmlCommon.IsHtmlElement(commonAncestor, "tfoot")
                ) {
                  if (lastNode.GetParentNode() != null) {
                    ((Node)lastNode.GetParentNode()).RemoveChild(
                      (Node)lastNode);
                  }
                  this.FosterParent(lastNode);
                } else {
                  if (lastNode.GetParentNode() != null) {
                    ((Node)lastNode.GetParentNode()).RemoveChild(
                      (Node)lastNode);
                  }
                  commonAncestor.AppendChild(lastNode);
                }
                Element e2 = Element.FromToken(formatting.Token);
                foreach (var child in new
                  List<INode>(furthestBlock.GetChildNodes())) {
                  furthestBlock.RemoveChild((Node)child);
                  // NOTE: Because 'e' can only be a formatting
                  // element, the foster parenting rule doesn't
                  // apply here
                  e2.AppendChild(child);
                }
                // NOTE: Because intervening elements, including
                // formatting elements, are cleared between table
                // and tbody/thead/tfoot and between those three
                // elements and tr, the foster parenting rule
                // doesn't apply here
                furthestBlock.AppendChild(e2);
                var newFE = new FormattingElement();
                newFE.ValueMarker = false;
                newFE.Element = e2;
                newFE.Token = formatting.Token;

                // Console.WriteLine("Adding formatting element at {0} [len={1}]"
                // , bookmark, this.formattingElements.Count);
                this.formattingElements.Insert(bookmark, newFE);
                this.formattingElements.Remove(formatting);
                // Console.WriteLine("Replacing open element at %d"
                // , openElements.IndexOf(furthestBlock)+1);
                int idx = this.openElements.IndexOf(furthestBlock) + 1;
                this.openElements.Insert(idx, e2);
                this.openElements.Remove(formatting.Element);
              }
              // Console.WriteLine("format after="
              // +this.openElements[0].GetOwnerDocument());
            } else if ("applet".Equals(valueName, StringComparison.Ordinal) ||
              "marquee".Equals(valueName, StringComparison.Ordinal) ||
              "object".Equals(valueName, StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInScope(valueName)) {
                this.ParseError();
                return false;
              } else {
                this.GenerateImpliedEndTags();
                if (!this.GetCurrentNode().GetLocalName().Equals(valueName,
                  StringComparison.Ordinal)) {
                  this.ParseError();
                }
                this.PopUntilHtmlElementPopped(valueName);
                this.ClearFormattingToMarker();
              }
            } else if (valueName.Equals("html", StringComparison.Ordinal)) {
              return this.ApplyEndTag("body", insMode) ?
                this.ApplyThisInsertionMode(token) : false;
            } else if ("address".Equals(valueName,
              StringComparison.Ordinal) ||
              "article".Equals(valueName, StringComparison.Ordinal) ||
              "aside".Equals(valueName, StringComparison.Ordinal) ||
              "blockquote".Equals(valueName, StringComparison.Ordinal) ||
              "button".Equals(valueName) ||
              "center".Equals(valueName, StringComparison.Ordinal) ||
              "details".Equals(valueName, StringComparison.Ordinal) ||
              "dialog".Equals(valueName, StringComparison.Ordinal) ||
              "dir".Equals(valueName, StringComparison.Ordinal) ||
              "div".Equals(valueName, StringComparison.Ordinal) ||
              "dl".Equals(valueName, StringComparison.Ordinal) ||
              "fieldset".Equals(valueName, StringComparison.Ordinal) ||
              "figcaption".Equals(valueName) ||
              "figure".Equals(valueName, StringComparison.Ordinal) ||
              "footer".Equals(valueName, StringComparison.Ordinal) ||
              "header".Equals(valueName, StringComparison.Ordinal) ||
              "listing".Equals(valueName, StringComparison.Ordinal) ||
              "main".Equals(valueName, StringComparison.Ordinal) ||
              "nav".Equals(valueName, StringComparison.Ordinal) ||
              "ol".Equals(valueName, StringComparison.Ordinal) ||
              "pre".Equals(valueName, StringComparison.Ordinal) ||
              "section".Equals(valueName, StringComparison.Ordinal) ||
              "summary".Equals(valueName, StringComparison.Ordinal) ||
              "ul".Equals(valueName, StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInScope(valueName)) {
                this.ParseError();
                return true;
              } else {
                this.GenerateImpliedEndTags();
                if (!this.GetCurrentNode().GetLocalName().Equals(valueName,
                  StringComparison.Ordinal)) {
                  this.ParseError();
                }
                this.PopUntilHtmlElementPopped(valueName);
              }
            } else if (valueName.Equals("form", StringComparison.Ordinal)) {
              if (this.HasHtmlOpenElement("template")) {
                if (!this.HasHtmlElementInScope("form")) {
                  this.ParseError();
                  return false;
                }
                this.GenerateImpliedEndTags();
                if (!HtmlCommon.IsHtmlElement(this.GetCurrentNode(),
                  "form")) {
                  this.ParseError();
                }
                this.PopUntilHtmlElementPopped("form");
              } else {
                IElement node = this.formElement;
                this.formElement = null;
                if (node == null || !this.HasHtmlElementInScope(node)) {
                  this.ParseError();
                  return false;
                }
                this.GenerateImpliedEndTags();
                if (this.GetCurrentNode() != node) {
                  this.ParseError();
                }
                this.openElements.Remove(node);
              }
            } else if (valueName.Equals("p", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInButtonScope(valueName)) {
                this.ParseError();
                this.ApplyStartTag("p", insMode);
                return this.ApplyThisInsertionMode(token);
              }
              this.GenerateImpliedEndTagsExcept(valueName);
              if (!this.GetCurrentNode().GetLocalName().Equals(valueName,
                StringComparison.Ordinal)) {
                this.ParseError();
              }
              this.PopUntilHtmlElementPopped(valueName);
            } else if (valueName.Equals("li", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInListItemScope(valueName)) {
                this.ParseError();
                return false;
              }
              this.GenerateImpliedEndTagsExcept(valueName);
              if (!this.GetCurrentNode().GetLocalName().Equals(valueName,
                StringComparison.Ordinal)) {
                this.ParseError();
              }
              this.PopUntilHtmlElementPopped(valueName);
            } else if (valueName.Equals("h1", StringComparison.Ordinal) ||
              valueName.Equals("h2", StringComparison.Ordinal) ||
              valueName.Equals("h3", StringComparison.Ordinal) ||
              valueName.Equals("h4", StringComparison.Ordinal) ||
              valueName.Equals("h5", StringComparison.Ordinal) ||
              valueName.Equals("h6", StringComparison.Ordinal)) {
              if (!this.HasHtmlHeaderElementInScope()) {
                this.ParseError();
                return false;
              }
              this.GenerateImpliedEndTags();
              if (!this.GetCurrentNode().GetLocalName().Equals(valueName,
                StringComparison.Ordinal)) {
                this.ParseError();
              }
              while (true) {
                IElement node = this.PopCurrentNode();
                if (HtmlCommon.IsHtmlElement(node, "h1") ||
                  HtmlCommon.IsHtmlElement(node, "h2") ||
                  HtmlCommon.IsHtmlElement(node, "h3") ||
                  HtmlCommon.IsHtmlElement(node, "h4") ||
                  HtmlCommon.IsHtmlElement(node, "h5") ||
                  HtmlCommon.IsHtmlElement(node, "h6")) {
                  break;
                }
              }
              return true;
            } else if (valueName.Equals("dd", StringComparison.Ordinal) ||
              valueName.Equals("dt", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInScope(valueName)) {
                this.ParseError();
                return false;
              }
              this.GenerateImpliedEndTagsExcept(valueName);
              if (!this.GetCurrentNode().GetLocalName().Equals(valueName,
                StringComparison.Ordinal)) {
                this.ParseError();
              }
              this.PopUntilHtmlElementPopped(valueName);
            } else if ("br".Equals(valueName, StringComparison.Ordinal)) {
              this.ParseError();
              this.ApplyStartTag("br", insMode);
              return false;
            } else {
              for (int i = this.openElements.Count - 1; i >= 0; --i) {
                IElement node = this.openElements[i];
                if (HtmlCommon.IsHtmlElement(node, valueName)) {
                  this.GenerateImpliedEndTagsExcept(valueName);
                  if (!node.Equals(this.GetCurrentNode())) {
                    this.ParseError();
                  }
                  while (true) {
                    IElement node2 = this.PopCurrentNode();
                    if (node2.Equals(node)) {
                      break;
                    }
                  }
                  break;
                } else if (this.IsSpecialElement(node)) {
                  this.ParseError();
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
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InBody);
            } else {
              this.ParseError();
              this.PopCurrentNode();
              this.insertionMode = InsertionMode.InHead;
              return this.ApplyThisInsertionMode(token);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ParseError();
            return false;
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("html", StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InBody);
            } else if (valueName.Equals("basefont",
              StringComparison.Ordinal) ||
              valueName.Equals(
                "bgsound",
                StringComparison.Ordinal) ||
              valueName.Equals("link", StringComparison.Ordinal) ||
              valueName.Equals("meta", StringComparison.Ordinal) ||
              valueName.Equals("noframes", StringComparison.Ordinal) ||
              valueName.Equals("style", StringComparison.Ordinal)
            ) {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InHead);
            } else if (valueName.Equals("head", StringComparison.Ordinal) ||
              valueName.Equals(
                "noscript",
                StringComparison.Ordinal)) {
              this.ParseError();
              return true;
            } else {
              this.ParseError();
              this.PopCurrentNode();
              this.insertionMode = InsertionMode.InHead;
              return this.ApplyThisInsertionMode(token);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("noscript", StringComparison.Ordinal)) {
              this.PopCurrentNode();
              this.insertionMode = InsertionMode.InHead;
            } else if (valueName.Equals("br", StringComparison.Ordinal)) {
              this.ParseError();
              this.PopCurrentNode();
              this.insertionMode = InsertionMode.InHead;
              return this.ApplyThisInsertionMode(token);
            } else {
              this.ParseError();
              return true;
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            return this.ApplyInsertionMode(
                token,
                InsertionMode.InHead);
          } else if (token == TOKEN_EOF) {
            this.ParseError();
            this.PopCurrentNode();
            this.insertionMode = InsertionMode.InHead;
            return this.ApplyThisInsertionMode(token);
          }
          return true;
        }
        case InsertionMode.InTable: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            IElement currentNode = this.GetCurrentNode();
            if (HtmlCommon.IsHtmlElement(currentNode, "table") ||
              HtmlCommon.IsHtmlElement(currentNode, "tbody") ||
              HtmlCommon.IsHtmlElement(currentNode, "tfoot") ||
              HtmlCommon.IsHtmlElement(currentNode, "thead") ||
              HtmlCommon.IsHtmlElement(currentNode, "tr")) {
              this.pendingTableCharacters.Remove(
                0,
                this.pendingTableCharacters.Length);
              this.originalInsertionMode = this.insertionMode;
              this.insertionMode = InsertionMode.InTableText;
              return this.ApplyThisInsertionMode(token);
            } else {
              // NOTE: Foster parenting rules don't apply here, since
              // the current node isn't table, tbody, tfoot, thead, or
              // tr and won't change while In Body is being applied
              this.ParseError();
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InBody);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ParseError();
            return false;
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("table", StringComparison.Ordinal)) {
              this.ParseError();
              return this.ApplyEndTag("table", insMode) ?
                this.ApplyThisInsertionMode(token) : false;
            } else if (valueName.Equals("caption",
              StringComparison.Ordinal)) {
              while (true) {
                IElement node = this.GetCurrentNode();
                if (node == null || HtmlCommon.IsHtmlElement(node, "table") ||
                  HtmlCommon.IsHtmlElement(node, "html") ||
                  HtmlCommon.IsHtmlElement(node, "template")) {
                  break;
                }
                this.PopCurrentNode();
              }
              this.InsertFormattingMarker(
                tag,
                this.AddHtmlElement(tag));
              this.insertionMode = InsertionMode.InCaption;
              return true;
            } else if (valueName.Equals("colgroup",
              StringComparison.Ordinal)) {
              while (true) {
                IElement node = this.GetCurrentNode();
                if (node == null || HtmlCommon.IsHtmlElement(node, "table") ||
                  HtmlCommon.IsHtmlElement(node, "html") ||
                  HtmlCommon.IsHtmlElement(node, "template")) {
                  break;
                }
                this.PopCurrentNode();
              }
              this.AddHtmlElement(tag);
              this.insertionMode = InsertionMode.InColumnGroup;
              return true;
            } else if (valueName.Equals("col", StringComparison.Ordinal)) {
              this.ApplyStartTag("colgroup", insMode);
              return this.ApplyThisInsertionMode(token);
            } else if (valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals(
                "tfoot",
                StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal)) {
              while (true) {
                IElement node = this.GetCurrentNode();
                if (node == null || HtmlCommon.IsHtmlElement(node, "table") ||
                  HtmlCommon.IsHtmlElement(node, "html") ||
                  HtmlCommon.IsHtmlElement(node, "template")) {
                  break;
                }
                this.PopCurrentNode();
              }
              this.AddHtmlElement(tag);
              this.insertionMode = InsertionMode.InTableBody;
            } else if (valueName.Equals("td", StringComparison.Ordinal) ||
              valueName.Equals("th", StringComparison.Ordinal) ||
              valueName.Equals("tr", StringComparison.Ordinal)) {
              this.ApplyStartTag("tbody", insMode);
              return this.ApplyThisInsertionMode(token);
            } else if (valueName.Equals("style", StringComparison.Ordinal) ||
              valueName.Equals("script", StringComparison.Ordinal) ||
              valueName.Equals("template", StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(token, InsertionMode.InHead);
            } else if (valueName.Equals("input", StringComparison.Ordinal)) {
              string attr = tag.GetAttribute("type");
              if (attr == null || !"hidden"
                .Equals(DataUtilities.ToLowerCaseAscii(attr))) {
                this.ParseError();
                this.doFosterParent = true;
                this.ApplyInsertionMode(
                  token,
                  InsertionMode.InBody);
                this.doFosterParent = false;
              } else {
                this.ParseError();
                this.AddHtmlElementNoPush(tag);
                tag.AckSelfClosing();
              }
            } else if (valueName.Equals("form", StringComparison.Ordinal)) {
              this.ParseError();
              if (this.formElement != null) {
                return false;
              }
              this.formElement = this.AddHtmlElementNoPush(tag);
            } else {
              this.ParseError();
              this.doFosterParent = true;
              this.ApplyInsertionMode(token, InsertionMode.InBody);
              this.doFosterParent = false;
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("table", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInTableScope(valueName)) {
                this.ParseError();
                return false;
              } else {
                this.PopUntilHtmlElementPopped(valueName);
                this.ResetInsertionMode();
              }
            } else if (valueName.Equals("body", StringComparison.Ordinal) ||
              valueName.Equals(
                "caption",
                StringComparison.Ordinal) ||
              valueName.Equals("col", StringComparison.Ordinal) ||
              valueName.Equals("colgroup", StringComparison.Ordinal) ||
              valueName.Equals("html", StringComparison.Ordinal) ||
              valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals("td", StringComparison.Ordinal) ||
              valueName.Equals("tfoot", StringComparison.Ordinal) ||
              valueName.Equals("th", StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal) ||
              valueName.Equals("tr", StringComparison.Ordinal)) {
              this.ParseError();
              return false;
            } else if (valueName.Equals("template",
              StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(token, InsertionMode.InHead);
            } else {
              this.doFosterParent = true;
              this.ApplyInsertionMode(token, InsertionMode.InBody);
              this.doFosterParent = false;
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToCurrentNode(token);
            return true;
          } else {
            return (token == TOKEN_EOF) ?
              this.ApplyInsertionMode(token, InsertionMode.InBody) : true;
          }
          return true;
        }
        case InsertionMode.InTableText: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            if (token == 0) {
              this.ParseError();
              return false;
            } else {
              if (token <= 0xffff) {
                this.pendingTableCharacters.Append((char)token);
              } else if (token <= 0x10ffff) {
                this.pendingTableCharacters.Append((char)((((token -
                  0x10000) >> 10) & 0x3ff) | 0xd800));
                this.pendingTableCharacters.Append((char)(((token -
                  0x10000) & 0x3ff) | 0xdc00));
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
              this.ParseError();
              this.doFosterParent = true;
              for (int i = 0; i < str.Length; ++i) {
                int c = DataUtilities.CodePointAt(str, i);
                if (c >= 0x10000) {
                  ++c;
                }
                this.ApplyInsertionMode(c, InsertionMode.InBody);
              }
              this.doFosterParent = false;
            } else {
              this.InsertString(
                this.GetCurrentNode(),
                this.pendingTableCharacters.ToString());
            }
            this.insertionMode = this.originalInsertionMode;
            return this.ApplyThisInsertionMode(token);
          }
          return true;
        }
        case InsertionMode.InCaption: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("caption", StringComparison.Ordinal) ||
              valueName.Equals("col", StringComparison.Ordinal) ||
              valueName.Equals("colgroup", StringComparison.Ordinal) ||
              valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal) ||
              valueName.Equals("td", StringComparison.Ordinal) ||
              valueName.Equals("tfoot", StringComparison.Ordinal) ||
              valueName.Equals("th", StringComparison.Ordinal) ||
              valueName.Equals("tr", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInTableScope("caption")) {
                this.ParseError();
                return false;
              }
              this.GenerateImpliedEndTags();
              if (!HtmlCommon.IsHtmlElement(this.GetCurrentNode(),
                "caption")) {
                this.ParseError();
              }
              this.PopUntilHtmlElementPopped("caption");
              this.ClearFormattingToMarker();
              this.insertionMode = InsertionMode.InTable;
              return this.ApplyThisInsertionMode(token);
            } else {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InBody);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("caption", StringComparison.Ordinal) ||
              valueName.Equals("table", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInTableScope(valueName)) {
                this.ParseError();
                return false;
              }
              this.GenerateImpliedEndTags();
              if (!HtmlCommon.IsHtmlElement(this.GetCurrentNode(),
                "caption")) {
                this.ParseError();
              }
              this.PopUntilHtmlElementPopped("caption");
              this.ClearFormattingToMarker();
              this.insertionMode = InsertionMode.InTable;
              if (valueName.Equals("table", StringComparison.Ordinal)) {
                return this.ApplyThisInsertionMode(token);
              }
            } else if (valueName.Equals("body", StringComparison.Ordinal) ||
              valueName.Equals("col", StringComparison.Ordinal) ||
              valueName.Equals("colgroup", StringComparison.Ordinal) ||
              valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal) ||
              valueName.Equals("td", StringComparison.Ordinal) ||
              valueName.Equals("tfoot", StringComparison.Ordinal) ||
              valueName.Equals("th", StringComparison.Ordinal) ||
              valueName.Equals("tr", StringComparison.Ordinal) ||
              valueName.Equals("html", StringComparison.Ordinal)) {
              this.ParseError();
            } else {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InBody);
            }
          } else {
            return this.ApplyInsertionMode(
                token,
                InsertionMode.InBody);
          }
          return true;
        }
        case InsertionMode.InColumnGroup: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER &&
            (token == 0x20 || token == 0x0c || token ==
              0x0a || token == 0x0d || token == 0x09)) {
            this.InsertCharacter(
              this.GetCurrentNode(),
              token);
            return true;
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ParseError();
            return true;
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToCurrentNode(token);
            return true;
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("html", StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InBody);
            } else if (valueName.Equals("col", StringComparison.Ordinal)) {
              this.AddHtmlElementNoPush(tag);
              tag.AckSelfClosing();
              return true;
            } else if (valueName.Equals("template",
              StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InHead);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("colgroup", StringComparison.Ordinal)) {
              if (!HtmlCommon.IsHtmlElement(this.GetCurrentNode(),
                "colgroup")) {
                this.ParseError();
                return false;
              }
              this.PopCurrentNode();
              this.insertionMode = InsertionMode.InTable;
              return true;
            } else if (valueName.Equals("col", StringComparison.Ordinal)) {
              this.ParseError();
              return true;
            } else if (valueName.Equals("template",
              StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InHead);
            }
          } else if (token == TOKEN_EOF) {
            return this.ApplyInsertionMode(token, InsertionMode.InBody);
          }
          if (!HtmlCommon.IsHtmlElement(this.GetCurrentNode(), "colgroup")) {
            this.ParseError();
            return false;
          }
          this.PopCurrentNode();
          this.insertionMode = InsertionMode.InTable;
          return this.ApplyThisInsertionMode(token);
        }
        case InsertionMode.InTableBody: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("tr", StringComparison.Ordinal)) {
              while (true) {
                IElement node = this.GetCurrentNode();
                if (node == null || HtmlCommon.IsHtmlElement(node, "tbody") ||
                  HtmlCommon.IsHtmlElement(node, "tfoot") ||
                  HtmlCommon.IsHtmlElement(node, "thead") ||
                  HtmlCommon.IsHtmlElement(node, "template") ||
                  HtmlCommon.IsHtmlElement(node, "html")) {
                  break;
                }
                this.PopCurrentNode();
              }
              this.AddHtmlElement(tag);
              this.insertionMode = InsertionMode.InRow;
            } else if (valueName.Equals("th", StringComparison.Ordinal) ||
              valueName.Equals("td", StringComparison.Ordinal)) {
              this.ParseError();
              this.ApplyStartTag("tr", insMode);
              return this.ApplyThisInsertionMode(token);
            } else if (valueName.Equals("caption",
              StringComparison.Ordinal) ||
              valueName.Equals("col", StringComparison.Ordinal) ||
              valueName.Equals("colgroup", StringComparison.Ordinal) ||
              valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals("tfoot", StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInTableScope("tbody") &&
                !this.HasHtmlElementInTableScope("thead") &&
                !this.HasHtmlElementInTableScope("tfoot")
              ) {
                this.ParseError();
                return false;
              }
              while (true) {
                IElement node = this.GetCurrentNode();
                if (node == null || HtmlCommon.IsHtmlElement(node, "tbody") ||
                  HtmlCommon.IsHtmlElement(node, "tfoot") ||
                  HtmlCommon.IsHtmlElement(node, "thead") ||
                  HtmlCommon.IsHtmlElement(node, "template") ||
                  HtmlCommon.IsHtmlElement(node, "html")) {
                  break;
                }
                this.PopCurrentNode();
              }
              this.ApplyEndTag(
                this.GetCurrentNode().GetLocalName(),
                insMode);
              return this.ApplyThisInsertionMode(token);
            } else {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InTable);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals("tfoot", StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInTableScope(valueName)) {
                this.ParseError();
                return false;
              }
              while (true) {
                IElement node = this.GetCurrentNode();
                if (node == null ||
                  HtmlCommon.IsHtmlElement(node, "tbody") ||
                  HtmlCommon.IsHtmlElement(node, "tfoot") ||
                  HtmlCommon.IsHtmlElement(node, "thead") ||
                  HtmlCommon.IsHtmlElement(node, "template") ||
                  HtmlCommon.IsHtmlElement(node, "html")) {
                  break;
                }
                this.PopCurrentNode();
              }
              this.PopCurrentNode();
              this.insertionMode = InsertionMode.InTable;
            } else if (valueName.Equals("table", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInTableScope("tbody") &&
                !this.HasHtmlElementInTableScope("thead") &&
                !this.HasHtmlElementInTableScope("tfoot")
              ) {
                this.ParseError();
                return false;
              }
              while (true) {
                IElement node = this.GetCurrentNode();
                if (node == null || HtmlCommon.IsHtmlElement(node, "tbody") ||
                  HtmlCommon.IsHtmlElement(node, "tfoot") ||
                  HtmlCommon.IsHtmlElement(node, "thead") ||
                  HtmlCommon.IsHtmlElement(node, "template") ||
                  HtmlCommon.IsHtmlElement(node, "html")) {
                  break;
                }
                this.PopCurrentNode();
              }
              this.ApplyEndTag(
                this.GetCurrentNode().GetLocalName(),
                insMode);
              return this.ApplyThisInsertionMode(token);
            } else if (valueName.Equals("body", StringComparison.Ordinal) ||
              valueName.Equals("caption", StringComparison.Ordinal) ||
              valueName.Equals("col", StringComparison.Ordinal) ||
              valueName.Equals("colgroup", StringComparison.Ordinal) ||
              valueName.Equals("html", StringComparison.Ordinal) ||
              valueName.Equals("td", StringComparison.Ordinal) ||
              valueName.Equals("th", StringComparison.Ordinal) ||
              valueName.Equals("tr", StringComparison.Ordinal)) {
              this.ParseError();
              return false;
            } else {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InTable);
            }
          } else {
            return this.ApplyInsertionMode(
                token,
                InsertionMode.InTable);
          }
          return true;
        }
        case InsertionMode.InRow: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            this.ApplyInsertionMode(token, InsertionMode.InTable);
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ApplyInsertionMode(token, InsertionMode.InTable);
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("th", StringComparison.Ordinal) ||
              valueName.Equals("td", StringComparison.Ordinal)) {
              while (!HtmlCommon.IsHtmlElement(this.GetCurrentNode(), "tr") &&
                !HtmlCommon.IsHtmlElement(this.GetCurrentNode(), "html") &&
                !HtmlCommon.IsHtmlElement(this.GetCurrentNode(), "template")) {
                this.PopCurrentNode();
              }
              this.insertionMode = InsertionMode.InCell;
              this.InsertFormattingMarker(
                tag,
                this.AddHtmlElement(tag));
            } else if (valueName.Equals("caption",
              StringComparison.Ordinal) ||
              valueName.Equals(
                "col",
                StringComparison.Ordinal) ||
              valueName.Equals("colgroup", StringComparison.Ordinal) ||
              valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals("tfoot", StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal) ||
              valueName.Equals("tr", StringComparison.Ordinal)) {
              if (this.ApplyEndTag("tr", insMode)) {
                return this.ApplyThisInsertionMode(token);
              }
            } else {
              this.ApplyInsertionMode(token, InsertionMode.InTable);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("tr", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInTableScope(valueName)) {
                this.ParseError();
                return false;
              }
              while (!HtmlCommon.IsHtmlElement(this.GetCurrentNode(), "tr") &&
                !HtmlCommon.IsHtmlElement(this.GetCurrentNode(), "html") &&
                !HtmlCommon.IsHtmlElement(this.GetCurrentNode(), "template")) {
                this.PopCurrentNode();
              }
              this.PopCurrentNode();
              this.insertionMode = InsertionMode.InTableBody;
            } else if (valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals(
                "tfoot",
                StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInTableScope(valueName)) {
                this.ParseError();
                return false;
              }
              this.ApplyEndTag("tr", insMode);
              return this.ApplyThisInsertionMode(token);
            } else if (valueName.Equals("caption",
              StringComparison.Ordinal) ||
              valueName.Equals("col", StringComparison.Ordinal) ||
              valueName.Equals("colgroup", StringComparison.Ordinal) ||
              valueName.Equals("html", StringComparison.Ordinal) ||
              valueName.Equals("body", StringComparison.Ordinal) ||
              valueName.Equals("td", StringComparison.Ordinal) ||
              valueName.Equals("th", StringComparison.Ordinal)) {
              this.ParseError();
            } else {
              this.ApplyInsertionMode(token, InsertionMode.InTable);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.ApplyInsertionMode(token, InsertionMode.InTable);
          } else if (token == TOKEN_EOF) {
            this.ApplyInsertionMode(token, InsertionMode.InTable);
          }
          return true;
        }
        case InsertionMode.InCell: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            this.ApplyInsertionMode(token, InsertionMode.InBody);
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ApplyInsertionMode(token, InsertionMode.InBody);
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("caption", StringComparison.Ordinal) ||
              valueName.Equals("col", StringComparison.Ordinal) ||
              valueName.Equals("colgroup", StringComparison.Ordinal) ||
              valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals("td", StringComparison.Ordinal) ||
              valueName.Equals("tfoot", StringComparison.Ordinal) ||
              valueName.Equals("th", StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal) ||
              valueName.Equals("tr", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInTableScope("td") &&
                !this.HasHtmlElementInTableScope("th")) {
                this.ParseError();
                return false;
              }
              this.ApplyEndTag(
                this.HasHtmlElementInTableScope("td") ? "td" : "th",
                insMode);
              return this.ApplyThisInsertionMode(token);
            } else {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("td", StringComparison.Ordinal) ||
              valueName.Equals("th", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInTableScope(valueName)) {
                this.ParseError();
                return false;
              }
              this.GenerateImpliedEndTags();
              if (!this.GetCurrentNode().GetLocalName().Equals(valueName,
                StringComparison.Ordinal)) {
                this.ParseError();
              }
              this.PopUntilHtmlElementPopped(valueName);
              this.ClearFormattingToMarker();
              this.insertionMode = InsertionMode.InRow;
            } else if (valueName.Equals("caption",
              StringComparison.Ordinal) ||
              valueName.Equals(
                "col",
                StringComparison.Ordinal) ||
              valueName.Equals("colgroup", StringComparison.Ordinal) ||
              valueName.Equals("body", StringComparison.Ordinal) ||
              valueName.Equals("html", StringComparison.Ordinal)) {
              this.ParseError();
              return false;
            } else if (valueName.Equals("table", StringComparison.Ordinal) ||
              valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals("tfoot", StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal) ||
              valueName.Equals("tr", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInTableScope(valueName)) {
                this.ParseError();
                return false;
              }
              this.ApplyEndTag(
                this.HasHtmlElementInTableScope("td") ? "td" : "th",
                insMode);
              return this.ApplyThisInsertionMode(token);
            } else {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.ApplyInsertionMode(token, InsertionMode.InBody);
          } else if (token == TOKEN_EOF) {
            this.ApplyInsertionMode(token, InsertionMode.InBody);
          }
          return true;
        }
        case InsertionMode.InSelect: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            if (token == 0) {
              this.ParseError();
              return false;
            } else {
              this.InsertCharacter(
                this.GetCurrentNode(),
                token);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ParseError();
            return false;
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("html", StringComparison.Ordinal)) {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
            } else if (valueName.Equals("option", StringComparison.Ordinal)) {
              if (this.GetCurrentNode().GetLocalName().Equals(
                "option",
                StringComparison.Ordinal)) {
                this.ApplyEndTag("option", insMode);
              }
              this.AddHtmlElement(tag);
            } else if (valueName.Equals("optgroup",
              StringComparison.Ordinal)) {
              if (this.GetCurrentNode().GetLocalName().Equals(
                "option",
                StringComparison.Ordinal)) {
                this.ApplyEndTag("option", insMode);
              }
              if (this.GetCurrentNode().GetLocalName().Equals(
                "optgroup",
                StringComparison.Ordinal)) {
                this.ApplyEndTag("optgroup", insMode);
              }
              this.AddHtmlElement(tag);
            } else if (valueName.Equals("select", StringComparison.Ordinal)) {
              this.ParseError();
              return this.ApplyEndTag("select", insMode);
            } else if (valueName.Equals("input", StringComparison.Ordinal) ||
              valueName.Equals(
                "keygen",
                StringComparison.Ordinal) ||
              valueName.Equals("textarea", StringComparison.Ordinal)) {
              this.ParseError();
              if (!this.HasHtmlElementInSelectScope("select")) {
                return false;
              }
              this.ApplyEndTag("select", insMode);
              return this.ApplyThisInsertionMode(token);
            } else if (valueName.Equals("script",
              StringComparison.Ordinal) || valueName.Equals(
                "template",
                StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InHead);
            } else {
              this.ParseError();
              return false;
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("optgroup", StringComparison.Ordinal)) {
              if (this.GetCurrentNode().GetLocalName().Equals(
                "option",
                StringComparison.Ordinal) && this.openElements.Count >= 2 &&
                this.openElements[this.openElements.Count -
                  2].GetLocalName().Equals(
                "optgroup")) {
                this.ApplyEndTag("option", insMode);
              }
              if (this.GetCurrentNode().GetLocalName().Equals(
                "optgroup",
                StringComparison.Ordinal)) {
                this.PopCurrentNode();
              } else {
                this.ParseError();
                return false;
              }
            } else if (valueName.Equals("option", StringComparison.Ordinal)) {
              if (this.GetCurrentNode().GetLocalName().Equals(
                "option",
                StringComparison.Ordinal)) {
                this.PopCurrentNode();
              } else {
                this.ParseError();
                return false;
              }
            } else if (valueName.Equals("select", StringComparison.Ordinal)) {
              if (!this.HasHtmlElementInScope(valueName)) {
                this.ParseError();
                return false;
              }
              this.PopUntilHtmlElementPopped(valueName);
              this.ResetInsertionMode();
            } else if (valueName.Equals("template",
              StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InHead);
            } else {
              this.ParseError();
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToCurrentNode(token);
          } else if (token == TOKEN_EOF) {
            return this.ApplyInsertionMode(
                token,
                InsertionMode.InBody);
          } else {
            this.ParseError();
          }
          return true;
        }
        case InsertionMode.InSelectInTable: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            return this.ApplyInsertionMode(
                token,
                InsertionMode.InSelect);
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            return this.ApplyInsertionMode(
                token,
                InsertionMode.InSelect);
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("caption", StringComparison.Ordinal) ||
              valueName.Equals("table", StringComparison.Ordinal) ||
              valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals("tfoot", StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal) ||
              valueName.Equals("tr", StringComparison.Ordinal) ||
              valueName.Equals("td", StringComparison.Ordinal) ||
              valueName.Equals("th", StringComparison.Ordinal)) {
              this.ParseError();
              this.PopUntilHtmlElementPopped("select");
              this.ResetInsertionMode();
              return this.ApplyThisInsertionMode(token);
            }
            return this.ApplyInsertionMode(
                token,
                InsertionMode.InSelect);
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("caption", StringComparison.Ordinal) ||
              valueName.Equals("table", StringComparison.Ordinal) ||
              valueName.Equals("tbody", StringComparison.Ordinal) ||
              valueName.Equals("tfoot", StringComparison.Ordinal) ||
              valueName.Equals("thead", StringComparison.Ordinal) ||
              valueName.Equals("tr", StringComparison.Ordinal) ||
              valueName.Equals("td", StringComparison.Ordinal) ||
              valueName.Equals("th", StringComparison.Ordinal)) {
              this.ParseError();
              if (!this.HasHtmlElementInTableScope(valueName)) {
                return false;
              }
              this.ApplyEndTag("select", insMode);
              return this.ApplyThisInsertionMode(token);
            }
            return this.ApplyInsertionMode(
                token,
                InsertionMode.InSelect);
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            return this.ApplyInsertionMode(
                token,
                InsertionMode.InSelect);
          } else {
            return (token == TOKEN_EOF) ?
              this.ApplyInsertionMode(token, InsertionMode.InSelect) :
              true;
          }
        }
        case InsertionMode.AfterBody: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            if (token == 0x09 || token == 0x0a || token ==
              0x0c || token == 0x0d || token == 0x20) {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
            } else {
              this.ParseError();
              this.insertionMode = InsertionMode.InBody;
              return this.ApplyThisInsertionMode(token);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ParseError();
            return true;
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("html", StringComparison.Ordinal)) {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
            } else {
              this.ParseError();
              this.insertionMode = InsertionMode.InBody;
              return this.ApplyThisInsertionMode(token);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("html", StringComparison.Ordinal)) {
              if (this.context != null) {
                this.ParseError();
                return false;
              }
              this.insertionMode = InsertionMode.AfterAfterBody;
            } else {
              this.ParseError();
              this.insertionMode = InsertionMode.InBody;
              return this.ApplyThisInsertionMode(token);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToFirst(token);
          } else if (token == TOKEN_EOF) {
            this.StopParsing();

            return true;
          }
          return true;
        }
        case InsertionMode.InFrameset: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            if (token == 0x09 || token == 0x0a || token == 0x0c ||
              token == 0x0d || token == 0x20) {
              this.InsertCharacter(
                this.GetCurrentNode(),
                token);
            } else {
              this.ParseError();
              return false;
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ParseError();
            return false;
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("html", StringComparison.Ordinal)) {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
            } else if (valueName.Equals("frameset",
              StringComparison.Ordinal)) {
              this.AddHtmlElement(tag);
            } else if (valueName.Equals("frame", StringComparison.Ordinal)) {
              this.AddHtmlElementNoPush(tag);
              tag.AckSelfClosing();
            } else if (valueName.Equals("noframes",
              StringComparison.Ordinal)) {
              this.ApplyInsertionMode(token, InsertionMode.InHead);
            } else {
              this.ParseError();
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            if (this.GetCurrentNode().GetLocalName().Equals("html",
              StringComparison.Ordinal)) {
              this.ParseError();
              return false;
            }
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("frameset", StringComparison.Ordinal)) {
              this.PopCurrentNode();
              if (this.context == null &&
                !HtmlCommon.IsHtmlElement(this.GetCurrentNode(),
                "frameset")) {
                this.insertionMode = InsertionMode.AfterFrameset;
              }
            } else {
              this.ParseError();
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToCurrentNode(token);
          } else if (token == TOKEN_EOF) {
            if (!HtmlCommon.IsHtmlElement(this.GetCurrentNode(), "html")) {
              this.ParseError();
            }
            this.StopParsing();
          }
          return true;
        }
        case InsertionMode.AfterFrameset: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            if (token == 0x09 || token == 0x0a || token ==
              0x0c || token == 0x0d || token == 0x20) {
              this.InsertCharacter(
                this.GetCurrentNode(),
                token);
            } else {
              this.ParseError();
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ParseError();
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("html", StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InBody);
            } else if (valueName.Equals("noframes",
              StringComparison.Ordinal)) {
              return this.ApplyInsertionMode(
                  token,
                  InsertionMode.InHead);
            } else {
              this.ParseError();
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            var tag = (EndTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("html", StringComparison.Ordinal)) {
              this.insertionMode = InsertionMode.AfterAfterFrameset;
            } else {
              this.ParseError();
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToCurrentNode(token);
          } else if (token == TOKEN_EOF) {
            this.StopParsing();
          }
          return true;
        }
        case InsertionMode.AfterAfterBody: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            if (token == 0x09 || token == 0x0a || token ==
              0x0c || token == 0x0d || token == 0x20) {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
            } else {
              this.ParseError();
              this.insertionMode = InsertionMode.InBody;
              return this.ApplyThisInsertionMode(token);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ApplyInsertionMode(token, InsertionMode.InBody);
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if (valueName.Equals("html", StringComparison.Ordinal)) {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
            } else {
              this.ParseError();
              this.insertionMode = InsertionMode.InBody;
              return this.ApplyThisInsertionMode(token);
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            this.ParseError();
            this.insertionMode = InsertionMode.InBody;
            return this.ApplyThisInsertionMode(token);
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToDocument(token);
          } else if (token == TOKEN_EOF) {
            this.StopParsing();
          }
          return true;
        }
        case InsertionMode.AfterAfterFrameset: {
          if ((token & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
            if (token == 0x09 || token == 0x0a || token ==
              0x0c || token == 0x0d || token == 0x20) {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
            } else {
              this.ParseError();
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
            this.ApplyInsertionMode(token, InsertionMode.InBody);
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
            var tag = (StartTagToken)this.GetToken(token);
            string valueName = tag.GetName();
            if ("html".Equals(valueName, StringComparison.Ordinal)) {
              this.ApplyInsertionMode(token, InsertionMode.InBody);
            } else if ("noframes".Equals(valueName,
              StringComparison.Ordinal)) {
              this.ApplyInsertionMode(token, InsertionMode.InHead);
            } else {
              this.ParseError();
            }
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
            this.ParseError();
          } else if ((token & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
            this.AddCommentNodeToDocument(token);
          } else if (token == TOKEN_EOF) {
            this.StopParsing();
          }
          return true;
        }
        default:
          throw new InvalidOperationException();
      }
    }

    private bool ApplyStartTag(string valueName, InsertionMode insMode) {
      return this.ApplyInsertionMode(
          this.GetArtificialToken(TOKEN_START_TAG, valueName),
          insMode);
    }

    private void ChangeEncoding(string charset) {
      string currentEncoding = this.encoding.GetEncoding();
      if (currentEncoding.Equals("utf-16le", StringComparison.Ordinal) ||
        currentEncoding.Equals("utf-16be", StringComparison.Ordinal)) {
        this.encoding = new EncodingConfidence(currentEncoding,
          EncodingConfidence.Certain);
        return;
      }
      if (charset.Equals("utf-16le", StringComparison.Ordinal)) {
        charset = "utf-8";
      } else if (charset.Equals("utf-16be", StringComparison.Ordinal)) {
        charset = "utf-8";
      }
      if (charset.Equals(currentEncoding, StringComparison.Ordinal)) {
        this.encoding = new EncodingConfidence(currentEncoding,
          EncodingConfidence.Certain);
        return;
      }
      // Reinitialize all parser state
      this.Initialize();
      // Rewind the input stream and set the new encoding
      this.inputStream.Rewind();
      this.encoding = new EncodingConfidence(
        charset,
        EncodingConfidence.Certain);
      ICharacterEncoding henc = new Html5Encoding(this.encoding);
      // TODO
      // this.charInput = new StackableCharacterInput(
      // Encodings.GetDecoderInput(henc, this.inputStream));
    }

    private void ClearFormattingToMarker() {
      while (this.formattingElements.Count > 0) {
        FormattingElement fe = RemoveAtIndex(
            this.formattingElements,
            this.formattingElements.Count - 1);
        if (fe.IsMarker()) {
          break;
        }
      }
    }

    private void PopUntilHtmlElementPopped(string name) {
      while (!HtmlCommon.IsHtmlElement(this.GetCurrentNode(), name)) {
        this.PopCurrentNode();
      }
      this.PopCurrentNode();
    }

    private void CloseParagraph() {
      if (this.HasHtmlElementInButtonScope("p")) {
        this.GenerateImpliedEndTagsExcept("p");
        IElement node = this.GetCurrentNode();
        if (!HtmlCommon.IsHtmlElement(node, "p")) {
          this.ParseError();
        }
        this.PopUntilHtmlElementPopped("p");
      }
    }

    private Comment CreateCommentNode(int valueToken) {
      var comment = (CommentToken)this.GetToken(valueToken);
      var node = new Comment();
      StringBuilder cv = comment.CommentValue;
      node.SetData(cv.ToString());
      return node;
    }

    private int EmitCurrentTag() {
      int ret = this.tokens.Count | this.currentTag.GetTokenType();
      this.AddToken(this.currentTag);
      if (this.currentTag.GetTokenType() == TOKEN_START_TAG) {
        this.lastStartTag = this.currentTag;
      } else {
        if (this.currentTag.GetAttributes().Count > 0 ||
          this.currentTag.IsSelfClosing()) {
          this.ParseError();
        }
      }
      this.currentTag = null;
      return ret;
    }

    private void FosterParent(INode valueElement) {
      if (this.openElements.Count == 0) {
        return;
      }
      // Console.WriteLine("Foster Parenting: " + valueElement);
      INode FosterParent = this.openElements[0];
      var lastTemplate = -1;
      var lastTable = -1;
      IElement e;
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        if (lastTemplate >= 0 && lastTable >= 0) {
          break;
        }
        e = this.openElements[i];
        if (lastTable < 0 && HtmlCommon.IsHtmlElement(e, "table")) {
          lastTable = i;
        }
        if (lastTemplate < 0 && HtmlCommon.IsHtmlElement(e, "template")) {
          lastTemplate = i;
        }
      }
      if (lastTemplate >= 0 && (lastTable < 0 || lastTemplate > lastTable)) {
        FosterParent = this.openElements[lastTemplate];
        ((Node)FosterParent).AppendChild(valueElement);

        return;
      }
      if (lastTable < 0) {
        FosterParent = this.openElements[0];
        ((Node)FosterParent).AppendChild(valueElement);

        return;
      }
      e = this.openElements[lastTable];
      var parent = (Node)e.GetParentNode();
      bool isElement = parent != null && parent.GetNodeType() ==
        NodeType.ELEMENT_NODE;
      if (!isElement) { // the parent is not an element
        if (lastTable <= 1) {
          // This usually won't happen
          throw new InvalidOperationException();
        }
        // Append to the element before this table
        FosterParent = this.openElements[lastTable - 1];
        ((Node)FosterParent).AppendChild(valueElement);
      } else {
        // Parent of the table, insert before the table
        parent.InsertBefore((Node)valueElement, (Node)e);
      }
    }

    private void GenerateImpliedEndTags() {
      while (true) {
        IElement node = this.GetCurrentNode();
        if (HtmlCommon.IsHtmlElement(node, "dd") ||
          HtmlCommon.IsHtmlElement(node, "dt") ||
          HtmlCommon.IsHtmlElement(node, "li") ||
          HtmlCommon.IsHtmlElement(node, "option") ||
          HtmlCommon.IsHtmlElement(node, "optgroup") ||
          HtmlCommon.IsHtmlElement(node, "p") ||
          HtmlCommon.IsHtmlElement(node, "rp") ||
          HtmlCommon.IsHtmlElement(node, "rt") ||
          HtmlCommon.IsHtmlElement(node, "rb") ||
          HtmlCommon.IsHtmlElement(node, "rtc")) {
          this.PopCurrentNode();
        } else {
          break;
        }
      }
    }

    private void GenerateImpliedEndTagsThoroughly() {
      while (true) {
        IElement node = this.GetCurrentNode();
        if (HtmlCommon.IsHtmlElement(node, "dd") ||
          HtmlCommon.IsHtmlElement(node, "dd") ||
          HtmlCommon.IsHtmlElement(node, "dt") ||
          HtmlCommon.IsHtmlElement(node, "li") ||
          HtmlCommon.IsHtmlElement(node, "option") ||
          HtmlCommon.IsHtmlElement(node, "optgroup") ||
          HtmlCommon.IsHtmlElement(node, "p") ||
          HtmlCommon.IsHtmlElement(node, "rp") ||
          HtmlCommon.IsHtmlElement(node, "caption") ||
          HtmlCommon.IsHtmlElement(node, "colgroup") ||
          HtmlCommon.IsHtmlElement(node, "tbody") ||
          HtmlCommon.IsHtmlElement(node, "tfoot") ||
          HtmlCommon.IsHtmlElement(node, "thead") ||
          HtmlCommon.IsHtmlElement(node, "td") ||
          HtmlCommon.IsHtmlElement(node, "th") ||
          HtmlCommon.IsHtmlElement(node, "tr") ||
          HtmlCommon.IsHtmlElement(node, "rt") ||
          HtmlCommon.IsHtmlElement(node, "rb") ||
          HtmlCommon.IsHtmlElement(node, "rtc")) {
          this.PopCurrentNode();
        } else {
          break;
        }
      }
    }

    private void GenerateImpliedEndTagsExcept(string stringValue) {
      while (true) {
        IElement node = this.GetCurrentNode();
        if (HtmlCommon.IsHtmlElement(node, stringValue)) {
          break;
        }
        if (HtmlCommon.IsHtmlElement(node, "dd") ||
          HtmlCommon.IsHtmlElement(node, "dt") ||
          HtmlCommon.IsHtmlElement(node, "li") ||
          HtmlCommon.IsHtmlElement(node, "rb") ||
          HtmlCommon.IsHtmlElement(node, "rtc") ||
          HtmlCommon.IsHtmlElement(node, "option") ||
          HtmlCommon.IsHtmlElement(node, "optgroup") ||
          HtmlCommon.IsHtmlElement(
            node,
            "p") ||
          HtmlCommon.IsHtmlElement(
            node,
            "rp") ||
          HtmlCommon.IsHtmlElement(node, "rt")) {
          this.PopCurrentNode();
        } else {
          break;
        }
      }
    }

    private int GetArtificialToken(int type, string valueName) {
      if (type == TOKEN_END_TAG) {
        var valueToken = new EndTagToken(valueName);
        int ret = this.tokens.Count | type;
        this.AddToken(valueToken);
        return ret;
      }
      if (type == TOKEN_START_TAG) {
        var valueToken = new StartTagToken(valueName);
        int ret = this.tokens.Count | type;
        this.AddToken(valueToken);
        return ret;
      }
      throw new ArgumentException();
    }

    private IElement GetCurrentNode() {
      return (this.openElements.Count == 0) ? null :
        this.openElements[this.openElements.Count - 1];
    }

    private FormattingElement GetFormattingElement(IElement node) {
      foreach (var fe in this.formattingElements) {
        if (!fe.IsMarker() && node.Equals(fe.Element)) {
          return fe;
        }
      }
      return null;
    }

    private Text GetFosterParentedTextNode() {
      if (this.openElements.Count == 0) {
        return null;
      }
      INode FosterParent = this.openElements[0];
      IList<INode> childNodes;
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (e.GetLocalName().Equals("table", StringComparison.Ordinal)) {
          var parent = (Node)e.GetParentNode();
          bool isElement = parent != null && parent.GetNodeType() ==
            NodeType.ELEMENT_NODE;
          if (!isElement) { // the parent is not an valueElement
            if (i <= 1) {
              // This usually won't happen
              throw new InvalidOperationException();
            }
            // Append to the valueElement before this table
            FosterParent = this.openElements[i - 1];
            break;
          } else {
            // Parent of the table, insert before the table
            childNodes = parent.GetChildNodesInternal();
            if (childNodes.Count == 0) {
              throw new InvalidOperationException();
            }
            for (int j = 0; j < childNodes.Count; ++j) {
              if (childNodes[j].Equals(e)) {
                if (j > 0 && childNodes[j - 1].GetNodeType() ==
                  NodeType.TEXT_NODE) {
                  return (Text)childNodes[j - 1];
                } else {
                  var textNode = new Text();
                  parent.InsertBefore(textNode, (Node)e);
                  return textNode;
                }
              }
            }
            throw new InvalidOperationException();
          }
        }
      }
      childNodes = FosterParent.GetChildNodes();
      INode lastChild = (childNodes.Count == 0) ? null :
        childNodes[childNodes.Count - 1];
      if (lastChild == null || lastChild.GetNodeType() != NodeType.TEXT_NODE) {
        var textNode = new Text();
        FosterParent.AppendChild(textNode);
        return textNode;
      } else {
        return (Text)lastChild;
      }
    }

    private Text GetTextNodeToInsert(INode node) {
      if (this.doFosterParent && node.Equals(this.GetCurrentNode())) {
        string valueName = ((IElement)node).GetLocalName();
        if ("table".Equals(valueName, StringComparison.Ordinal) ||
          "tbody".Equals(valueName, StringComparison.Ordinal) ||
          "tfoot".Equals(valueName, StringComparison.Ordinal) ||
          "thead".Equals(valueName, StringComparison.Ordinal) ||
          "tr".Equals(valueName, StringComparison.Ordinal)) {
          return this.GetFosterParentedTextNode();
        }
      }
      IList<INode> childNodes = ((INode)node).GetChildNodes();
      INode lastChild = (childNodes.Count == 0) ? null :
        childNodes[childNodes.Count - 1];
      if (lastChild == null || lastChild.GetNodeType() != NodeType.TEXT_NODE) {
        var textNode = new Text();
        node.AppendChild(textNode);
        return textNode;
      } else {
        return (Text)lastChild;
      }
    }

    internal IToken GetToken(int valueToken) {
      if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_CHARACTER ||
        (valueToken & TOKEN_TYPE_MASK) == TOKEN_EOF) {
        return null;
      } else {
        return this.tokens[valueToken & TOKEN_INDEX_MASK];
      }
    }

    private bool HasHtmlElementInButtonScope(string valueName) {
      var found = false;
      foreach (var e in this.openElements) {
        if (e.GetLocalName().Equals(valueName, StringComparison.Ordinal)) {
          found = true;
        }
      }
      if (!found) {
        return false;
      }
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        string namespaceValue = e.GetNamespaceURI();
        string thisName = e.GetLocalName();
        if (HtmlCommon.HTML_NAMESPACE.Equals(namespaceValue,
          StringComparison.Ordinal)) {
          if (thisName.Equals(valueName, StringComparison.Ordinal)) {
            return true;
          }
          if (thisName.Equals("applet", StringComparison.Ordinal) ||
            thisName.Equals("caption", StringComparison.Ordinal) ||
            thisName.Equals("html", StringComparison.Ordinal) ||
            thisName.Equals("table", StringComparison.Ordinal) ||
            thisName.Equals("td", StringComparison.Ordinal) ||
            thisName.Equals("th", StringComparison.Ordinal) ||
            thisName.Equals("marquee", StringComparison.Ordinal) ||
            thisName.Equals("object", StringComparison.Ordinal) ||
            thisName.Equals("button", StringComparison.Ordinal)) {
            // Console.WriteLine("not in scope: %s",thisName);
            return false;
          }
        }
        if (HtmlCommon.MATHML_NAMESPACE.Equals(namespaceValue,
          StringComparison.Ordinal)) {
          if (thisName.Equals("mi", StringComparison.Ordinal) ||
            thisName.Equals("mo", StringComparison.Ordinal) ||
            thisName.Equals("mn", StringComparison.Ordinal) ||
            thisName.Equals("ms", StringComparison.Ordinal) ||
            thisName.Equals("mtext", StringComparison.Ordinal) ||
            thisName.Equals("annotation-xml", StringComparison.Ordinal)) {
            return false;
          }
        }
        if (HtmlCommon.SVG_NAMESPACE.Equals(namespaceValue,
          StringComparison.Ordinal)) {
          if (thisName.Equals("foreignObject", StringComparison.Ordinal) ||
            thisName.Equals("desc", StringComparison.Ordinal) ||
            thisName.Equals("title", StringComparison.Ordinal)) {
            return false;
          }
        }
      }
      return false;
    }

    private bool HasHtmlElementInListItemScope(string valueName) {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (HtmlCommon.IsHtmlElement(e, valueName)) {
          return true;
        }
        if (HtmlCommon.IsHtmlElement(e, "applet") ||
          HtmlCommon.IsHtmlElement(e, "caption") ||
          HtmlCommon.IsHtmlElement(e, "html") ||
          HtmlCommon.IsHtmlElement(e, "table") ||
          HtmlCommon.IsHtmlElement(e, "td") ||
          HtmlCommon.IsHtmlElement(e, "th") ||
          HtmlCommon.IsHtmlElement(e, "ol") ||
          HtmlCommon.IsHtmlElement(e, "ul") ||
          HtmlCommon.IsHtmlElement(e, "marquee") ||
          HtmlCommon.IsHtmlElement(e, "object") ||
          HtmlCommon.IsMathMLElement(e, "mi") ||
          HtmlCommon.IsMathMLElement(e, "mo") ||
          HtmlCommon.IsMathMLElement(e, "mn") ||
          HtmlCommon.IsMathMLElement(e, "ms") ||
          HtmlCommon.IsMathMLElement(e, "mtext") ||
          HtmlCommon.IsMathMLElement(e, "annotation-xml") ||
          HtmlCommon.IsSvgElement(e, "foreignObject") ||
          HtmlCommon.IsSvgElement(
            e,
            "desc") ||
          HtmlCommon.IsSvgElement(
            e,
            "title")
        ) {
          return false;
        }
      }
      return false;
    }

    private bool HasHtmlElementInScope(IElement node) {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (e == node) {
          return true;
        }
        if (HtmlCommon.IsHtmlElement(e, "applet") ||
          HtmlCommon.IsHtmlElement(e, "caption") ||
          HtmlCommon.IsHtmlElement(e, "html") ||
          HtmlCommon.IsHtmlElement(e, "table") ||
          HtmlCommon.IsHtmlElement(e, "td") ||
          HtmlCommon.IsHtmlElement(e, "th") ||
          HtmlCommon.IsHtmlElement(e, "marquee") ||
          HtmlCommon.IsHtmlElement(e, "object") ||
          HtmlCommon.IsMathMLElement(e, "mi") ||
          HtmlCommon.IsMathMLElement(e, "mo") ||
          HtmlCommon.IsMathMLElement(e, "mn") ||
          HtmlCommon.IsMathMLElement(e, "ms") ||
          HtmlCommon.IsMathMLElement(e, "mtext") ||
          HtmlCommon.IsMathMLElement(e, "annotation-xml") ||
          HtmlCommon.IsSvgElement(e, "foreignObject") ||
          HtmlCommon.IsSvgElement(
            e,
            "desc") ||
          HtmlCommon.IsSvgElement(
            e,
            "title")
        ) {
          return false;
        }
      }
      return false;
    }

    private bool HasHtmlElementInScope(string valueName) {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (HtmlCommon.IsHtmlElement(e, valueName)) {
          return true;
        }
        if (HtmlCommon.IsHtmlElement(e, "applet") ||
          HtmlCommon.IsHtmlElement(e, "caption") ||
          HtmlCommon.IsHtmlElement(e, "html") ||
          HtmlCommon.IsHtmlElement(e, "table") ||
          HtmlCommon.IsHtmlElement(e, "td") ||
          HtmlCommon.IsHtmlElement(e, "th") ||
          HtmlCommon.IsHtmlElement(e, "marquee") ||
          HtmlCommon.IsHtmlElement(e, "object") ||
          HtmlCommon.IsMathMLElement(e, "mi") ||
          HtmlCommon.IsMathMLElement(e, "mo") ||
          HtmlCommon.IsMathMLElement(e, "mn") ||
          HtmlCommon.IsMathMLElement(e, "ms") ||
          HtmlCommon.IsMathMLElement(e, "mtext") ||
          HtmlCommon.IsMathMLElement(e, "annotation-xml") ||
          HtmlCommon.IsSvgElement(e, "foreignObject") ||
          HtmlCommon.IsSvgElement(
            e,
            "desc") ||
          HtmlCommon.IsSvgElement(
            e,
            "title")
        ) {
          return false;
        }
      }
      return false;
    }

    private bool HasHtmlElementInSelectScope(string valueName) {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (HtmlCommon.IsHtmlElement(e, valueName)) {
          return true;
        }
        if (!HtmlCommon.IsHtmlElement(e, "optgroup") &&
          !HtmlCommon.IsHtmlElement(e, "option")) {
          return false;
        }
      }
      return false;
    }

    private bool HasHtmlElementInTableScope(string valueName) {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (HtmlCommon.IsHtmlElement(e, valueName)) {
          return true;
        }
        if (HtmlCommon.IsHtmlElement(e, "html") ||
          HtmlCommon.IsHtmlElement(e, "table")) {
          return false;
        }
      }
      return false;
    }

    private bool HasHtmlHeaderElementInScope() {
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (HtmlCommon.IsHtmlElement(e, "h1") ||
          HtmlCommon.IsHtmlElement(e, "h2") ||
          HtmlCommon.IsHtmlElement(e, "h3") ||
          HtmlCommon.IsHtmlElement(e, "h4") ||
          HtmlCommon.IsHtmlElement(e, "h5") ||
          HtmlCommon.IsHtmlElement(e, "h6")) {
          return true;
        }
        if (HtmlCommon.IsHtmlElement(e, "applet") ||
          HtmlCommon.IsHtmlElement(e, "caption") ||
          HtmlCommon.IsHtmlElement(e, "html") ||
          HtmlCommon.IsHtmlElement(e, "table") ||
          HtmlCommon.IsHtmlElement(e, "td") ||
          HtmlCommon.IsHtmlElement(e, "th") ||
          HtmlCommon.IsHtmlElement(e, "marquee") ||
          HtmlCommon.IsHtmlElement(e, "object") ||
          HtmlCommon.IsMathMLElement(e, "mi") ||
          HtmlCommon.IsMathMLElement(e, "mo") ||
          HtmlCommon.IsMathMLElement(e, "mn") ||
          HtmlCommon.IsMathMLElement(e, "ms") ||
          HtmlCommon.IsMathMLElement(e, "mtext") ||
          HtmlCommon.IsMathMLElement(e, "annotation-xml") ||
          HtmlCommon.IsSvgElement(e, "foreignObject") ||
          HtmlCommon.IsSvgElement(e, "desc") ||
          HtmlCommon.IsSvgElement(e, "title")) {
          return false;
        }
      }
      return false;
    }

    private void Initialize() {
      this.noforeign = false;
      this.templateModes.Clear();
      this.valueDocument = new Document();
      this.valueDocument.Address = this.address;
      this.valueDocument.SetBaseURI(this.address);
      this.context = null;
      this.openElements.Clear();
      this.error = false;
      this.baseurl = null;
      // this.hasForeignContent = false; // performance optimization
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
      this.pendingTableCharacters.Remove(0,
        this.pendingTableCharacters.Length);
    }

    private void InsertCharacter(INode node, int ch) {
      Text textNode = this.GetTextNodeToInsert(node);
      if (textNode != null) {
        StringBuilder builder = textNode.ValueText;
        if (ch <= 0xffff) {
          builder.Append((char)ch);
        } else if (ch <= 0x10ffff) {
          builder.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) | 0xd800));
          builder.Append((char)(((ch - 0x10000) & 0x3ff) | 0xdc00));
        }
      }
    }

    private Element InsertForeignElement(StartTagToken tag,
      string namespaceValue) {
      Element valueElement = Element.FromToken(tag, namespaceValue);
      string xmlns = valueElement.GetAttributeNS(
          HtmlCommon.XMLNS_NAMESPACE,
          "xmlns");
      string xlink = valueElement.GetAttributeNS(
          HtmlCommon.XMLNS_NAMESPACE,
          "xlink");
      if (xmlns != null && !xmlns.Equals(namespaceValue,
        StringComparison.Ordinal)) {
        this.ParseError();
      }
      if (xlink != null && !xlink.Equals(HtmlCommon.XLINK_NAMESPACE,
        StringComparison.Ordinal)) {
        this.ParseError();
      }
      IElement currentNode = this.GetCurrentNode();
      if (currentNode != null) {
        this.InsertInCurrentNode(valueElement);
      } else {
        this.valueDocument.AppendChild(valueElement);
      }
      this.openElements.Add(valueElement);
      return valueElement;
    }

    private void InsertFormattingMarker(
      StartTagToken tag,
      Element AddHtmlElement) {
      var fe = new FormattingElement();
      fe.ValueMarker = true;
      fe.Element = AddHtmlElement;
      fe.Token = tag;
      this.formattingElements.Add(fe);
    }

    private void InsertInCurrentNode(Node valueElement) {
      IElement node = this.GetCurrentNode();
      if (this.doFosterParent) {
        string valueName = node.GetLocalName();
        if ("table".Equals(valueName, StringComparison.Ordinal) ||
          "tbody".Equals(valueName, StringComparison.Ordinal) ||
          "tfoot".Equals(valueName, StringComparison.Ordinal) ||
          "thead".Equals(valueName, StringComparison.Ordinal) ||
          "tr".Equals(valueName, StringComparison.Ordinal)) {
          this.FosterParent(valueElement);
        } else {
          node.AppendChild(valueElement);
        }
      } else {
        node.AppendChild(valueElement);
      }
    }

    private void InsertString(INode node, string str) {
      Text textNode = this.GetTextNodeToInsert(node);
      if (textNode != null) {
        textNode.ValueText.Append(str);
      }
    }

    private bool IsAppropriateEndTag() {
      if (this.lastStartTag == null || this.currentEndTag == null) {
        return false;
      }
      return this.currentEndTag.GetName().Equals(this.lastStartTag.GetName(),
        StringComparison.Ordinal);
    }

    public HtmlParser CheckError(bool ce) {
      this.checkErrorVar = ce;
      return this;
    }

    private void ParseError() {
      this.error = true;
      if (this.checkErrorVar) {
        throw new InvalidOperationException();
      }
    }

    public bool IsError() {
      return this.error;
    }

    private bool IsForeignContext(int valueToken) {
      if (valueToken == TOKEN_EOF) {
        return false;
      }
      if (this.openElements.Count == 0) {
        return false;
      }
      IElement valueElement = (this.context != null &&
          this.openElements.Count == 1) ?
        this.context : this.GetCurrentNode(); // adjusted current node
      if (valueElement == null) {
        return false;
      }
      if (valueElement.GetNamespaceURI().Equals(HtmlCommon.HTML_NAMESPACE,
        StringComparison.Ordinal)) {
        return false;
      }
      if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        var tag = (StartTagToken)this.GetToken(valueToken);
        string valueName = valueElement.GetLocalName();
        // Console.WriteLine("start tag " +valueName+","
        // +valueElement.GetNamespaceURI()+"," +tag);
        if (this.IsMathMLTextIntegrationPoint(valueElement)) {
          string tokenName = tag.GetName();
          if (!"mglyph".Equals(tokenName, StringComparison.Ordinal) &&
            !"malignmark".Equals(tokenName, StringComparison.Ordinal)) {
            return false;
          }
        }
        bool annotationSVG =
          HtmlCommon.MATHML_NAMESPACE.Equals(valueElement.GetNamespaceURI(),
          StringComparison.Ordinal) &&
          valueName.Equals("annotation-xml", StringComparison.Ordinal) &&
          "svg".Equals(tag.GetName());
        return !annotationSVG && !this.IsHtmlIntegrationPoint(valueElement);
      } else if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        return !this.IsMathMLTextIntegrationPoint(valueElement) &&
          !this.IsHtmlIntegrationPoint(valueElement);
      } else {
        return true;
      }
    }

    private bool IsHtmlIntegrationPoint(IElement valueElement) {
      if (this.integrationElements.Contains(valueElement)) {
        return true;
      }
      string valueName = valueElement.GetLocalName();
      return HtmlCommon.SVG_NAMESPACE.Equals(valueElement.GetNamespaceURI(),
        StringComparison.Ordinal) && (
          valueName.Equals("foreignObject",
            StringComparison.Ordinal) || valueName.Equals(
            "desc",
            StringComparison.Ordinal) ||
          valueName.Equals("title", StringComparison.Ordinal));
    }

    private bool IsMathMLTextIntegrationPoint(IElement valueElement) {
      string valueName = valueElement.GetLocalName();
      return HtmlCommon.MATHML_NAMESPACE.Equals(valueElement.GetNamespaceURI(),
        StringComparison.Ordinal) && (
          valueName.Equals("mi", StringComparison.Ordinal) ||
          valueName.Equals("mo", StringComparison.Ordinal) ||
          valueName.Equals("mn", StringComparison.Ordinal) ||
          valueName.Equals("ms", StringComparison.Ordinal) ||
          valueName.Equals("mtext", StringComparison.Ordinal));
    }

    private bool IsSpecialElement(IElement node) {
      if (HtmlCommon.IsHtmlElement(node, "address") ||
        HtmlCommon.IsHtmlElement(node, "applet") ||
        HtmlCommon.IsHtmlElement(node, "area") ||
        HtmlCommon.IsHtmlElement(node, "article") ||
        HtmlCommon.IsHtmlElement(node, "aside") ||
        HtmlCommon.IsHtmlElement(node, "base") ||
        HtmlCommon.IsHtmlElement(node, "basefont") ||
        HtmlCommon.IsHtmlElement(node, "bgsound") ||
        HtmlCommon.IsHtmlElement(node, "blockquote") ||
        HtmlCommon.IsHtmlElement(node, "body") ||
        HtmlCommon.IsHtmlElement(node, "br") ||
        HtmlCommon.IsHtmlElement(node, "button") ||
        HtmlCommon.IsHtmlElement(node, "caption") ||
        HtmlCommon.IsHtmlElement(node, "center") ||
        HtmlCommon.IsHtmlElement(node, "col") ||
        HtmlCommon.IsHtmlElement(node, "colgroup") ||
        HtmlCommon.IsHtmlElement(node, "dd") ||
        HtmlCommon.IsHtmlElement(node, "details") ||
        HtmlCommon.IsHtmlElement(node, "dir") ||
        HtmlCommon.IsHtmlElement(node, "div") ||
        HtmlCommon.IsHtmlElement(node, "dl") ||
        HtmlCommon.IsHtmlElement(node, "dt") ||
        HtmlCommon.IsHtmlElement(node, "embed") ||
        HtmlCommon.IsHtmlElement(node, "fieldset") ||
        HtmlCommon.IsHtmlElement(node, "figcaption") ||
        HtmlCommon.IsHtmlElement(node, "figure") ||
        HtmlCommon.IsHtmlElement(node, "footer") ||
        HtmlCommon.IsHtmlElement(node, "form") ||
        HtmlCommon.IsHtmlElement(node, "frame") ||
        HtmlCommon.IsHtmlElement(node, "frameset") ||
        HtmlCommon.IsHtmlElement(node, "h1") ||
        HtmlCommon.IsHtmlElement(node, "h2") ||
        HtmlCommon.IsHtmlElement(node, "h3") ||
        HtmlCommon.IsHtmlElement(node, "h4") ||
        HtmlCommon.IsHtmlElement(node, "h5") ||
        HtmlCommon.IsHtmlElement(node, "h6") ||
        HtmlCommon.IsHtmlElement(node, "head") ||
        HtmlCommon.IsHtmlElement(node, "header") ||
        HtmlCommon.IsHtmlElement(node, "hr") ||
        HtmlCommon.IsHtmlElement(node, "html") ||
        HtmlCommon.IsHtmlElement(node, "iframe") ||
        HtmlCommon.IsHtmlElement(node, "img") ||
        HtmlCommon.IsHtmlElement(node, "input") ||
        HtmlCommon.IsHtmlElement(node, "isindex") ||
        HtmlCommon.IsHtmlElement(node, "li") ||
        HtmlCommon.IsHtmlElement(node, "link") ||
        HtmlCommon.IsHtmlElement(node, "listing") ||
        HtmlCommon.IsHtmlElement(node, "main") ||
        HtmlCommon.IsHtmlElement(node, "marquee") ||
        HtmlCommon.IsHtmlElement(node, "meta") ||
        HtmlCommon.IsHtmlElement(node, "nav") ||
        HtmlCommon.IsHtmlElement(node, "noembed") ||
        HtmlCommon.IsHtmlElement(node, "noframes") ||
        HtmlCommon.IsHtmlElement(node, "noscript") ||
        HtmlCommon.IsHtmlElement(node, "object") ||
        HtmlCommon.IsHtmlElement(node, "ol") ||
        HtmlCommon.IsHtmlElement(node, "p") ||
        HtmlCommon.IsHtmlElement(node, "param") ||
        HtmlCommon.IsHtmlElement(node, "plaintext") ||
        HtmlCommon.IsHtmlElement(node, "pre") ||
        HtmlCommon.IsHtmlElement(node, "script") ||
        HtmlCommon.IsHtmlElement(node, "section") ||
        HtmlCommon.IsHtmlElement(node, "select") ||
        HtmlCommon.IsHtmlElement(node, "source") ||
        HtmlCommon.IsHtmlElement(node, "style") ||
        HtmlCommon.IsHtmlElement(node, "summary") ||
        HtmlCommon.IsHtmlElement(node, "table") ||
        HtmlCommon.IsHtmlElement(node, "tbody") ||
        HtmlCommon.IsHtmlElement(node, "td") ||
        HtmlCommon.IsHtmlElement(node, "textarea") ||
        HtmlCommon.IsHtmlElement(node, "tfoot") ||
        HtmlCommon.IsHtmlElement(node, "th") ||
        HtmlCommon.IsHtmlElement(node, "thead") ||
        HtmlCommon.IsHtmlElement(node, "title") ||
        HtmlCommon.IsHtmlElement(node, "tr") ||
        HtmlCommon.IsHtmlElement(node, "track") ||
        HtmlCommon.IsHtmlElement(node, "ul") ||
        HtmlCommon.IsHtmlElement(node, "wbr") ||
        HtmlCommon.IsHtmlElement(node, "xmp")) {
        return true;
      }
      if (HtmlCommon.IsMathMLElement(node, "mi") ||
        HtmlCommon.IsMathMLElement(node, "mo") ||
        HtmlCommon.IsMathMLElement(node, "mn") ||
        HtmlCommon.IsMathMLElement(node, "ms") ||
        HtmlCommon.IsMathMLElement(node, "mtext") ||
        HtmlCommon.IsMathMLElement(
          node,
          "annotation-xml")) {
        return true;
      }
      return (HtmlCommon.IsSvgElement(node, "foreignObject") ||
        HtmlCommon.IsSvgElement(
          node,
          "desc") || HtmlCommon.IsSvgElement(
          node,
          "title")) ? true : false;
    }
    internal string NodesToDebugString(IList<Node>
      nodes) {
      var builder = new StringBuilder();
      foreach (var node in nodes) {
        string str = node.ToDebugString();
        string[] strarray = StringUtility.SplitAt(str, "\n");
        int len = strarray.Length;
        if (len > 0 && strarray[len - 1].Length == 0) {
          --len; // ignore trailing empty string
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

    public IDocument Parse() {
      while (true) {
        int valueToken = this.ParserRead();
        this.ApplyThisInsertionMode(valueToken);
        if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
          var tag = (StartTagToken)this.GetToken(valueToken);
          // Console.WriteLine(tag);
          if (!tag.IsAckSelfClosing()) {
            this.ParseError();
          }
        }
        // Console.WriteLine("valueToken=%08X, insertionMode=%s, error=%s"
        // , valueToken, insertionMode, error);
        if (this.done) {
          break;
        }
      }
      return this.valueDocument;
    }

    private int ParseCharacterReference(int allowedCharacter) {
      int markStart = this.charInput.SetSoftMark();
      int c1 = this.charInput.ReadChar();
      if (c1 < 0 || c1 == 0x09 || c1 == 0x0a || c1 == 0x0c ||
        c1 == 0x20 || c1 == 0x3c || c1 == 0x26 || (allowedCharacter >= 0 &&
          c1 == allowedCharacter)) {
        this.charInput.SetMarkPosition(markStart);
        return 0x26; // emit ampersand
      } else if (c1 == 0x23) {
        c1 = this.charInput.ReadChar();
        var value = 0;
        var haveHex = false;
        if (c1 == 0x78 || c1 == 0x58) {
          // Hex number
          while (true) { // skip zeros
            int c = this.charInput.ReadChar();
            if (c != '0') {
              if (c >= 0) {
                this.charInput.MoveBack(1);
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
                this.charInput.MoveBack(1);
              }
              break;
            }
            if (value > 0x10ffff) {
              value = 0x110000;
              overflow = true;
            }
          }
        } else {
          if (c1 > 0) {
            this.charInput.MoveBack(1);
          }
          // Digits
          while (true) { // skip zeros
            int c = this.charInput.ReadChar();
            if (c != '0') {
              if (c >= 0) {
                this.charInput.MoveBack(1);
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
                this.charInput.MoveBack(1);
              }
              break;
            }
            if (value > 0x10ffff) {
              value = 0x110000;
              overflow = true;
            }
          }
        }
        if (!haveHex) {
          // No digits: Parse error
          this.ParseError();
          this.charInput.SetMarkPosition(markStart);
          return 0x26; // emit ampersand
        }
        c1 = this.charInput.ReadChar();
        if (c1 != 0x3b) { // semicolon
          this.ParseError();
          if (c1 >= 0) {
            this.charInput.MoveBack(1); // Parse error
          }
        }
        if (value > 0x10ffff || ((value & 0xf800) == 0xd800)) {
          this.ParseError();
          value = 0xfffd; // Parse error
        } else if (value >= 0x80 && value < 0xa0) {
          this.ParseError();
          // Parse error
          var replacements = new int[] {
            0x20ac, 0x81, 0x201a, 0x192,
            0x201e, 0x2026, 0x2020, 0x2021, 0x2c6, 0x2030, 0x160, 0x2039, 0x152, 0x8d,
            0x17d, 0x8f, 0x90, 0x2018, 0x2019, 0x201c, 0x201d, 0x2022, 0x2013, 0x2014,
            0x2dc, 0x2122, 0x161, 0x203a, 0x153, 0x9d, 0x17e, 0x178,
          };
          value = replacements[value - 0x80];
        } else if (value == 0x0d) {
          // Parse error
          this.ParseError();
        } else if (value == 0x00) {
          // Parse error
          this.ParseError();
          value = 0xfffd;
        }
        if (value == 0x08 || value == 0x0b ||
          (value & 0xfffe) == 0xfffe || (value >= 0x0e && value <= 0x1f) ||
          value == 0x7f || (value >= 0xfdd0 && value <= 0xfdef)) {
          // Parse error
          this.ParseError();
        }
        return value;
      } else if ((c1 >= 'A' && c1 <= 'Z') || (c1 >= 'a' && c1 <= 'z') ||
        (c1 >= '0' && c1 <= '9')) {
        int[] data = null;
        // check for certain well-known entities
        if (c1 == 'g') {
          if (this.charInput.ReadChar() == 't' && this.charInput.ReadChar() ==
            ';') {
            return '>';
          }
          this.charInput.SetMarkPosition(markStart + 1);
        } else if (c1 == 'l') {
          if (this.charInput.ReadChar() == 't' && this.charInput.ReadChar() ==
            ';') {
            return '<';
          }
          this.charInput.SetMarkPosition(markStart + 1);
        } else if (c1 == 'a') {
          if (this.charInput.ReadChar() == 'm' && this.charInput.ReadChar()
            == 'p' && this.charInput.ReadChar() == ';') {
            return '&';
          }
          this.charInput.SetMarkPosition(markStart + 1);
        } else if (c1 == 'n') {
          if (this.charInput.ReadChar() == 'b' && this.charInput.ReadChar() ==
            's' &&
            this.charInput.ReadChar() == 'p' && this.charInput.ReadChar() ==
            ';') {
            return 0xa0;
          }
          this.charInput.SetMarkPosition(markStart + 1);
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
              // Console.WriteLine("markposch=%c",(char)data[0]);
            }
            // if fewer bytes were read than the
            // entity's remaining length, this
            // can't match
            // Console.WriteLine("data count=%s %s"
            // , count, stream.getMarkPosition());
            if (count < entity.Length - 1) {
              continue;
            }
            var matched = true;
            for (int i = 1; i < entity.Length; ++i) {
              // Console.WriteLine("%c %c | markpos=%d",
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
              this.charInput.MoveBack(count - (entity.Length - 1));
              // Console.WriteLine("lastchar=%c",entity[entity.Length-1]);
              if (allowedCharacter >= 0 && entity[entity.Length - 1] != ';') {
                // Get the next character after the entity
                int ch2 = this.charInput.ReadChar();
                if (ch2 == '=' || (ch2 >= 'A' && ch2 <= 'Z') ||
                  (ch2 >= 'a' && ch2 <= 'z') || (ch2 >= '0' && ch2 <= '9')) {
                  if (ch2 == '=') {
                    this.ParseError();
                  }
                  this.charInput.SetMarkPosition(markStart);
                  return 0x26; // return ampersand rather than entity
                } else {
                  if (ch2 >= 0) {
                    this.charInput.MoveBack(1);
                  }
                  if (entity[entity.Length - 1] != ';') {
                    this.ParseError();
                  }
                }
              } else {
                if (entity[entity.Length - 1] != ';') {
                  this.ParseError();
                }
              }
              return HtmlEntities.EntityValues[index];
            }
          }
        }
        // no match
        this.charInput.SetMarkPosition(markStart);
        while (true) {
          int ch2 = this.charInput.ReadChar();
          if (ch2 == ';') {
            this.ParseError();
            break;
          } else if (!((ch2 >= 'A' && ch2 <= 'Z') || (ch2 >= 'a' && ch2 <= 'z'
            ) || (ch2 >= '0' && ch2 <= '9'))) {
            break;
          }
        }
        this.charInput.SetMarkPosition(markStart);
        return 0x26;
      } else {
        // not a character reference
        this.charInput.SetMarkPosition(markStart);
        return 0x26; // emit ampersand
      }
    }

    public IList<INode> ParseFragment(IElement context) {
      if (context == null) {
        throw new ArgumentException();
      }
      this.Initialize();
      this.valueDocument = new Document();
      INode ownerDocument = context;
      INode lastForm = null;
      while (ownerDocument != null) {
        if (lastForm == null && ownerDocument.GetNodeType() ==
          NodeType.ELEMENT_NODE) {
          if (HtmlCommon.IsHtmlElement((IElement)ownerDocument, "form")) {
            lastForm = ownerDocument;
          }
        }
        ownerDocument = ownerDocument.GetParentNode();
        if (ownerDocument == null ||
          ownerDocument.GetNodeType() == NodeType.DOCUMENT_NODE) {
          break;
        }
      }
      Document ownerDoc = null;
      if (ownerDocument != null &&
        ownerDocument.GetNodeType() == NodeType.DOCUMENT_NODE) {
        ownerDoc = (Document)ownerDocument;
        this.valueDocument.SetMode(ownerDoc.GetMode());
      }
      this.state = TokenizerState.Data;
      if (HtmlCommon.IsHtmlElement(context, "title") ||
        HtmlCommon.IsHtmlElement(context, "textarea")) {
        this.state = TokenizerState.RcData;
      } else if (HtmlCommon.IsHtmlElement(context, "style") ||
        HtmlCommon.IsHtmlElement(context, "xmp") ||
        HtmlCommon.IsHtmlElement(context, "iframe") ||
        HtmlCommon.IsHtmlElement(context, "noembed") ||
        HtmlCommon.IsHtmlElement(context, "noframes")) {
        this.state = TokenizerState.RawText;
      } else if (HtmlCommon.IsHtmlElement(context, "script")) {
        this.state = TokenizerState.ScriptData;
      } else if (HtmlCommon.IsHtmlElement(context, "noscript")) {
        this.state = TokenizerState.Data;
      } else if (HtmlCommon.IsHtmlElement(context, "plaintext")) {
        this.state = TokenizerState.PlainText;
      }
      var valueElement = new Element();
      valueElement.SetLocalName("html");
      valueElement.SetNamespace(HtmlCommon.HTML_NAMESPACE);
      this.valueDocument.AppendChild(valueElement);
      this.done = false;
      this.openElements.Clear();
      this.openElements.Add(valueElement);
      if (HtmlCommon.IsHtmlElement(context, "template")) {
        this.templateModes.Add(InsertionMode.InTemplate);
      }
      this.context = context;
      this.ResetInsertionMode();
      this.formElement = (lastForm == null) ? null : ((Element)lastForm);
      if (this.encoding.GetConfidence() != EncodingConfidence.Irrelevant) {
        this.encoding = new EncodingConfidence(
          this.encoding.GetEncoding(),
          EncodingConfidence.Irrelevant);
      }
      this.Parse();
      return new List<INode>(valueElement.GetChildNodes());
    }

    public IList<INode> ParseFragment(string contextName) {
      var valueElement = new Element();
      valueElement.SetLocalName(contextName);
      valueElement.SetNamespace(HtmlCommon.HTML_NAMESPACE);
      return this.ParseFragment(valueElement);
    }

    public IList<string[]> ParseTokens(string s, string lst) {
      this.Initialize();
      var ret = new List<string[]>();
      var characters = new StringBuilder();
      if (lst != null) {
        this.lastStartTag = new StartTagToken(lst);
      }
      if (s.Equals("PLAINTEXT state", StringComparison.Ordinal)) {
        this.state = TokenizerState.PlainText;
      }
      if (s.Equals("RCDATA state", StringComparison.Ordinal)) {
        this.state = TokenizerState.RcData;
      }
      if (s.Equals("RAWTEXT state", StringComparison.Ordinal)) {
        this.state = TokenizerState.RawText;
      }
      if (s.Equals("Script data state", StringComparison.Ordinal)) {
        this.state = TokenizerState.ScriptData;
      }
      if (s.Equals("CDATA section state", StringComparison.Ordinal)) {
        this.state = TokenizerState.CData;
      }
      // Console.WriteLine("tok state="+this.state+ ","+s);
      // Console.WriteLine("" + (this.lastStartTag));
      while (true) {
        int valueToken = this.ParserRead();
        if ((valueToken & TOKEN_TYPE_MASK) != TOKEN_CHARACTER) {
          if (characters.Length > 0) {
            ret.Add(new string[] { "Character", characters.ToString() });
            characters.Remove(0, characters.Length);
          }
        } else {
          if (valueToken <= 0xffff) {
            {
              characters.Append((char)valueToken);
            }
          } else if (valueToken <= 0x10ffff) {
            characters.Append((char)((((valueToken - 0x10000) >> 10) &
              0x3ff) | 0xd800));
            characters.Append((char)(((valueToken - 0x10000) & 0x3ff) |
              0xdc00));
          }
          continue;
        }
        if (valueToken == TOKEN_EOF) {
          break;
        }
        if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
          var tag = (StartTagToken)this.GetToken(valueToken);
          IList<Attr> attributes = tag.GetAttributes();
          var stlen = 2 + (attributes.Count * 2);
          if (tag.IsSelfClosing()) {
            ++stlen;
          }
          var tagarray = new string[stlen];
          tagarray[0] = "StartTag";
          tagarray[1] = tag.GetName();
          var index = 2;
          foreach (Attr attribute in attributes) {
            tagarray[index] = attribute.GetName();
            tagarray[index + 1] = attribute.GetValue();
            index += 2;
          }
          if (tag.IsSelfClosing()) {
            tagarray[index] = "true";
          }
          ret.Add(tagarray);
          continue;
        }
        if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
          var tag = (EndTagToken)this.GetToken(valueToken);
          ret.Add(new string[] { "EndTag", tag.GetName() });
          continue;
        }
        if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
          var tag = (DocTypeToken)this.GetToken(valueToken);
          string doctypeName = tag.Name;
          string doctypePublic = tag.ValuePublicID;
          string doctypeSystem = tag.ValueSystemID;
          doctypeName = (doctypeName == null) ? String.Empty :
            doctypeName.ToString();
          doctypePublic = doctypePublic?.ToString();
          doctypeSystem = doctypeSystem?.ToString();
          ret.Add(new string[] {
            "DOCTYPE", doctypeName, doctypePublic, doctypeSystem,
            tag.ForceQuirks ? "false" : "true",
          });
          continue;
        }
        if ((valueToken & TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
          var tag = (CommentToken)this.GetToken(valueToken);
          StringBuilder cv = tag.CommentValue;
          ret.Add(new string[] { "Comment", cv.ToString() });
          continue;
        }
        throw new InvalidOperationException();
      }
      return ret;
    }

    internal int ParserRead() {
      int valueToken = this.ParserReadInternal();
      // Console.WriteLine("valueToken=%08X [%c]",valueToken,valueToken&0xFF);
      if (valueToken <= -2) {
        this.ParseError();
        return 0xfffd;
      }
      return valueToken;
    }

    private int ParserReadInternal() {
      if (this.tokenQueue.Count > 0) {
        return RemoveAtIndex(this.tokenQueue, 0);
      }
      while (true) {
        // Console.WriteLine("" + state);
        switch (this.state) {
          case TokenizerState.Data:
            int c = this.charInput.ReadChar();
            if (c == 0x26) {
              this.state = TokenizerState.CharacterRefInData;
            } else if (c == 0x3c) {
              this.state = TokenizerState.TagOpen;
            } else if (c == 0) {
              this.error = true;
              return c;
            } else if (c < 0) {
              return TOKEN_EOF;
            } else {
              int ret = c;
              // Keep reading characters to
              // reduce the need to re-call
              // this method
              int mark = this.charInput.SetSoftMark();
              for (int i = 0; i < 100; ++i) {
                c = this.charInput.ReadChar();
                if (c > 0 && c != 0x26 && c != 0x3c) {
                  this.tokenQueue.Add(c);
                } else {
                  this.charInput.SetMarkPosition(mark + i);
                  break;
                }
              }
              return ret;
            }
            break;
          case TokenizerState.CharacterRefInData: {
            this.state = TokenizerState.Data;
            int charref = this.ParseCharacterReference(-1);
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
            int charref = this.ParseCharacterReference(-1);
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
              this.state = TokenizerState.RcDataLessThan;
            } else if (c1 == 0) {
              this.error = true;
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
              this.ParseError();
              return 0xfffd;
            } else if (c11 < 0) {
              return TOKEN_EOF;
            } else {
              return c11;
            }
            break;
          }
          case TokenizerState.ScriptDataLessThan: {
            this.charInput.SetHardMark();
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
                this.charInput.MoveBack(1);
              }
              return 0x3c;
            }
            break;
          }
          case TokenizerState.ScriptDataEndTagOpen:
          case TokenizerState.ScriptDataEscapedEndTagOpen: {
            this.charInput.SetHardMark();
            int ch = this.charInput.ReadChar();
            if (ch >= 'A' && ch <= 'Z') {
              var valueToken = new EndTagToken((char)(ch + 0x20));
              if (ch <= 0xffff) {
                this.tempBuilder.Append((char)ch);
              } else if (ch <= 0x10ffff) {
                this.tempBuilder.Append((char)((((ch - 0x10000) >> 10) &
                  0x3ff) | 0xd800));
                this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) |
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
                  0x3ff) | 0xd800));
                this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) |
                  0xdc00));
              }
              this.currentTag = valueToken;
              this.currentEndTag = valueToken;
              this.state = (
                  this.state == TokenizerState.ScriptDataEndTagOpen) ?
                TokenizerState.ScriptDataEndTagName :
                TokenizerState.ScriptDataEscapedEndTagName;
            } else {
              this.state = (this.state ==
                  TokenizerState.ScriptDataEndTagOpen) ?
                TokenizerState.ScriptData : TokenizerState.ScriptDataEscaped;
              this.tokenQueue.Add(0x2f);
              if (ch >= 0) {
                this.charInput.MoveBack(1);
              }
              return 0x3c;
            }
            break;
          }
          case TokenizerState.ScriptDataEndTagName:
          case TokenizerState.ScriptDataEscapedEndTagName: {
            this.charInput.SetHardMark();
            int ch = this.charInput.ReadChar();
            if ((ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) &&
              this.IsAppropriateEndTag()) {
              this.state = TokenizerState.BeforeAttributeName;
            } else if (ch == 0x2f && this.IsAppropriateEndTag()) {
              this.state = TokenizerState.SelfClosingStartTag;
            } else if (ch == 0x3e && this.IsAppropriateEndTag()) {
              this.state = TokenizerState.Data;
              return this.EmitCurrentTag();
            } else if (ch >= 'A' && ch <= 'Z') {
              this.currentTag.AppendChar((char)(ch + 0x20));
              this.tempBuilder.Append((char)ch);
            } else if (ch >= 'a' && ch <= 'z') {
              this.currentTag.AppendChar((char)ch);
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
                this.charInput.MoveBack(1);
              }
              return '<';
            }
            break;
          }
          case TokenizerState.ScriptDataDoubleEscapeStart: {
            this.charInput.SetHardMark();
            int ch = this.charInput.ReadChar();
            if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20 ||
              ch == 0x2f || ch == 0x3e) {
              string bufferString = this.tempBuilder.ToString();
              this.state = bufferString.Equals("script",
                  StringComparison.Ordinal) ?
                TokenizerState.ScriptDataDoubleEscaped :
                TokenizerState.ScriptDataEscaped;
              return ch;
            } else if (ch >= 'A' && ch <= 'Z') {
              if (ch + 0x20 <= 0xffff) {
                this.tempBuilder.Append((char)(ch + 0x20));
              } else if (ch + 0x20 <= 0x10ffff) {
                this.tempBuilder.Append((char)((((ch + 0x20 - 0x10000) >>
                  10) & 0x3ff) | 0xd800));
                this.tempBuilder.Append((char)(((ch + 0x20 - 0x10000) &
                  0x3ff) | 0xdc00));
              }
              return ch;
            } else if (ch >= 'a' && ch <= 'z') {
              if (ch <= 0xffff) {
                this.tempBuilder.Append((char)ch);
              } else if (ch <= 0x10ffff) {
                this.tempBuilder.Append((char)((((ch - 0x10000) >> 10) &
                  0x3ff) | 0xd800));
                this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) |
                  0xdc00));
              }
              return ch;
            } else {
              this.state = TokenizerState.ScriptDataEscaped;
              if (ch >= 0) {
                this.charInput.MoveBack(1);
              }
            }
            break;
          }
          case TokenizerState.ScriptDataDoubleEscapeEnd: {
            this.charInput.SetHardMark();
            int ch = this.charInput.ReadChar();
            if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20 ||
              ch == 0x2f || ch == 0x3e) {
              string bufferString = this.tempBuilder.ToString();
              this.state = bufferString.Equals("script",
                  StringComparison.Ordinal) ? TokenizerState.ScriptDataEscaped :
                TokenizerState.ScriptDataDoubleEscaped;
              return ch;
            } else if (ch >= 'A' && ch <= 'Z') {
              if (ch + 0x20 <= 0xffff) {
                this.tempBuilder.Append((char)(ch + 0x20));
              } else if (ch + 0x20 <= 0x10ffff) {
                this.tempBuilder.Append((char)((((ch + 0x20 - 0x10000) >>
                  10) & 0x3ff) | 0xd800));
                this.tempBuilder.Append((char)(((ch + 0x20 - 0x10000) &
                  0x3ff) | 0xdc00));
              }
              return ch;
            } else if (ch >= 'a' && ch <= 'z') {
              if (ch <= 0xffff) {
                this.tempBuilder.Append((char)ch);
              } else if (ch <= 0x10ffff) {
                this.tempBuilder.Append((char)((((ch - 0x10000) >> 10) &
                  0x3ff) | 0xd800));
                this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) |
                  0xdc00));
              }
              return ch;
            } else {
              this.state = TokenizerState.ScriptDataDoubleEscaped;
              if (ch >= 0) {
                this.charInput.MoveBack(1);
              }
            }
            break;
          }
          case TokenizerState.ScriptDataEscapeStart:
          case TokenizerState.ScriptDataEscapeStartDash: {
            this.charInput.SetHardMark();
            int ch = this.charInput.ReadChar();
            if (ch == 0x2d) {
              this.state = (this.state ==
                  TokenizerState.ScriptDataEscapeStart) ?
                TokenizerState.ScriptDataEscapeStartDash :
                TokenizerState.ScriptDataEscapedDashDash;
              return '-';
            } else {
              if (ch >= 0) {
                this.charInput.MoveBack(1);
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
              this.ParseError();
              return 0xfffd;
            } else if (ch < 0) {
              this.ParseError();
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
              this.ParseError();
              return 0xfffd;
            } else if (ch < 0) {
              this.ParseError();
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
              this.ParseError();
              this.state = TokenizerState.ScriptDataEscaped;
              return 0xfffd;
            } else if (ch < 0) {
              this.ParseError();
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
              this.ParseError();
              this.state = TokenizerState.ScriptDataDoubleEscaped;
              return 0xfffd;
            } else if (ch < 0) {
              this.ParseError();
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
              this.ParseError();
              this.state = TokenizerState.ScriptDataEscaped;
              return 0xfffd;
            } else if (ch < 0) {
              this.ParseError();
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
              this.ParseError();
              this.state = TokenizerState.ScriptDataDoubleEscaped;
              return 0xfffd;
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
            } else {
              this.state = TokenizerState.ScriptDataDoubleEscaped;
              return ch;
            }
            break;
          }
          case TokenizerState.ScriptDataDoubleEscapedLessThan: {
            this.charInput.SetHardMark();
            int ch = this.charInput.ReadChar();
            if (ch == 0x2f) {
              this.tempBuilder.Remove(0, this.tempBuilder.Length);
              this.state = TokenizerState.ScriptDataDoubleEscapeEnd;
              return 0x2f;
            } else {
              this.state = TokenizerState.ScriptDataDoubleEscaped;
              if (ch >= 0) {
                this.charInput.MoveBack(1);
              }
            }
            break;
          }
          case TokenizerState.ScriptDataEscapedLessThan: {
            this.charInput.SetHardMark();
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
                this.charInput.MoveBack(1);
              }
              return 0x3c;
            }
            break;
          }
          case TokenizerState.PlainText: {
            int c11 = this.charInput.ReadChar();
            if (c11 == 0) {
              this.ParseError();
              return 0xfffd;
            } else if (c11 < 0) {
              return TOKEN_EOF;
            } else {
              return c11;
            }
          }
          case TokenizerState.TagOpen: {
            this.charInput.SetHardMark();
            int c11 = this.charInput.ReadChar();
            // Console.WriteLine("In tagopen " + ((char)c11));
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
              this.ParseError();
              this.bogusCommentCharacter = c11;
              this.state = TokenizerState.BogusComment;
            } else {
              this.ParseError();
              this.state = TokenizerState.Data;
              if (c11 >= 0) {
                this.charInput.MoveBack(1);
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
              this.ParseError();
              this.state = TokenizerState.Data;
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
              this.tokenQueue.Add(0x2f); // solidus
              return 0x3c; // Less than
            } else {
              this.ParseError();
              this.bogusCommentCharacter = ch;
              this.state = TokenizerState.BogusComment;
            }
            break;
          }
          case TokenizerState.RcDataEndTagOpen:
          case TokenizerState.RawTextEndTagOpen: {
            this.charInput.SetHardMark();
            int ch = this.charInput.ReadChar();
            if (ch >= 'A' && ch <= 'Z') {
              TagToken valueToken = new EndTagToken((char)(ch + 0x20));
              if (ch <= 0xffff) {
                this.tempBuilder.Append((char)ch);
              } else if (ch <= 0x10ffff) {
                this.tempBuilder.Append((char)((((ch - 0x10000) >> 10) &
                  0x3ff) | 0xd800));
                this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) |
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
                  0x3ff) | 0xd800));
                this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) |
                  0xdc00));
              }
              this.currentEndTag = valueToken;
              this.currentTag = valueToken;
              this.state = (
                  this.state == TokenizerState.RcDataEndTagOpen) ?
                TokenizerState.RcDataEndTagName :
                TokenizerState.RawTextEndTagName;
            } else {
              if (ch >= 0) {
                this.charInput.MoveBack(1);
              }
              this.state = TokenizerState.RcData;
              this.tokenQueue.Add(0x2f); // solidus
              return 0x3c; // Less than
            }
            break;
          }
          case TokenizerState.RcDataEndTagName:
          case TokenizerState.RawTextEndTagName: {
            this.charInput.SetHardMark();
            int ch = this.charInput.ReadChar();
            if ((ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) &&
              this.IsAppropriateEndTag()) {
              this.state = TokenizerState.BeforeAttributeName;
            } else if (ch == 0x2f && this.IsAppropriateEndTag()) {
              this.state = TokenizerState.SelfClosingStartTag;
            } else if (ch == 0x3e && this.IsAppropriateEndTag()) {
              this.state = TokenizerState.Data;
              return this.EmitCurrentTag();
            } else if (ch >= 'A' && ch <= 'Z') {
              this.currentTag.Append(ch + 0x20);
              if (ch + 0x20 <= 0xffff) {
                this.tempBuilder.Append((char)(ch + 0x20));
              } else if (ch + 0x20 <= 0x10ffff) {
                this.tempBuilder.Append((char)((((ch + 0x20 - 0x10000) >>
                  10) & 0x3ff) | 0xd800));
                this.tempBuilder.Append((char)(((ch + 0x20 - 0x10000) &
                  0x3ff) | 0xdc00));
              }
            } else if (ch >= 'a' && ch <= 'z') {
              this.currentTag.Append(ch);
              if (ch <= 0xffff) {
                this.tempBuilder.Append((char)ch);
              } else if (ch <= 0x10ffff) {
                this.tempBuilder.Append((char)((((ch - 0x10000) >> 10) &
                  0x3ff) | 0xd800));
                this.tempBuilder.Append((char)(((ch - 0x10000) & 0x3ff) |
                  0xdc00));
              }
            } else {
              if (ch >= 0) {
                this.charInput.MoveBack(1);
              }
              this.state = (this.state == TokenizerState.RcDataEndTagName) ?
                TokenizerState.RcData : TokenizerState.RawText;
              this.tokenQueue.Add(0x2f); // solidus
              string tbs = this.tempBuilder.ToString();
              for (int i = 0; i < tbs.Length; ++i) {
                int c2 = DataUtilities.CodePointAt(tbs, i);
                if (c2 >= 0x10000) {
                  ++i;
                }
                this.tokenQueue.Add(c2);
              }
              return 0x3c; // Less than
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
              return this.EmitCurrentTag();
            } else if (ch >= 'A' && ch <= 'Z') {
              this.currentAttribute = this.currentTag.AddAttribute((char)(ch +
                0x20));
              this.state = TokenizerState.AttributeName;
            } else if (ch == 0) {
              this.ParseError();
              this.currentAttribute =
                this.currentTag.AddAttribute((char)0xfffd);
              this.state = TokenizerState.AttributeName;
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
            } else {
              if (ch == 0x22 || ch == 0x27 || ch == 0x3c || ch == 0x3d) {
                this.ParseError();
              }
              this.currentAttribute = this.currentTag.AddAttribute(ch);
              this.state = TokenizerState.AttributeName;
            }
            break;
          }
          case TokenizerState.AttributeName: {
            int ch = this.charInput.ReadChar();
            if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
              if (!this.currentTag.CheckAttributeName()) {
                this.ParseError();
              }
              this.state = TokenizerState.AfterAttributeName;
            } else if (ch == 0x2f) {
              if (!this.currentTag.CheckAttributeName()) {
                this.ParseError();
              }
              this.state = TokenizerState.SelfClosingStartTag;
            } else if (ch == 0x3d) {
              if (!this.currentTag.CheckAttributeName()) {
                this.ParseError();
              }
              this.state = TokenizerState.BeforeAttributeValue;
            } else if (ch == 0x3e) {
              if (!this.currentTag.CheckAttributeName()) {
                this.ParseError();
              }
              this.state = TokenizerState.Data;
              return this.EmitCurrentTag();
            } else if (ch >= 'A' && ch <= 'Z') {
              this.currentAttribute.AppendToName(ch + 0x20);
            } else if (ch == 0) {
              this.ParseError();
              this.currentAttribute.AppendToName(0xfffd);
            } else if (ch < 0) {
              this.ParseError();
              if (!this.currentTag.CheckAttributeName()) {
                this.ParseError();
              }
              this.state = TokenizerState.Data;
            } else if (ch == 0x22 || ch == 0x27 || ch == 0x3c) {
              this.ParseError();
              this.currentAttribute.AppendToName(ch);
            } else {
              this.currentAttribute.AppendToName(ch);
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
              return this.EmitCurrentTag();
            } else if (ch >= 'A' && ch <= 'Z') {
              this.currentAttribute = this.currentTag.AddAttribute((char)(ch +
                0x20));
              this.state = TokenizerState.AttributeName;
            } else if (ch == 0) {
              this.ParseError();
              this.currentAttribute =
                this.currentTag.AddAttribute((char)0xfffd);
              this.state = TokenizerState.AttributeName;
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
            } else {
              if (ch == 0x22 || ch == 0x27 || ch == 0x3c) {
                this.ParseError();
              }
              this.currentAttribute = this.currentTag.AddAttribute(ch);
              this.state = TokenizerState.AttributeName;
            }
            break;
          }
          case TokenizerState.BeforeAttributeValue: {
            this.charInput.SetHardMark();
            int ch = this.charInput.ReadChar();
            while (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
              ch = this.charInput.ReadChar();
            }
            if (ch == 0x22) {
              this.state = TokenizerState.AttributeValueDoubleQuoted;
            } else if (ch == 0x26) {
              this.charInput.MoveBack(1);
              this.state = TokenizerState.AttributeValueUnquoted;
            } else if (ch == 0x27) {
              this.state = TokenizerState.AttributeValueSingleQuoted;
            } else if (ch == 0) {
              this.ParseError();
              this.currentAttribute.AppendToValue(0xfffd);
              this.state = TokenizerState.AttributeValueUnquoted;
            } else if (ch == 0x3e) {
              this.ParseError();
              this.state = TokenizerState.Data;
              return this.EmitCurrentTag();
            } else if (ch == 0x3c || ch == 0x3d || ch == 0x60) {
              this.ParseError();
              this.currentAttribute.AppendToValue(ch);
              this.state = TokenizerState.AttributeValueUnquoted;
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
            } else {
              this.currentAttribute.AppendToValue(ch);
              this.state = TokenizerState.AttributeValueUnquoted;
            }
            break;
          }
          case TokenizerState.AttributeValueDoubleQuoted: {
            int ch = this.charInput.ReadChar();
            if (ch == 0x22) {
              this.currentAttribute.CommitValue();
              this.state = TokenizerState.AfterAttributeValueQuoted;
            } else if (ch == 0x26) {
              this.lastState = this.state;
              this.state = TokenizerState.CharacterRefInAttributeValue;
            } else if (ch == 0) {
              this.ParseError();
              this.currentAttribute.AppendToValue(0xfffd);
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
            } else {
              this.currentAttribute.AppendToValue(ch);
              // Keep reading characters to
              // reduce the need to re-call
              // this method
              int mark = this.charInput.SetSoftMark();
              for (int i = 0; i < 100; ++i) {
                ch = this.charInput.ReadChar();
                if (ch > 0 && ch != 0x26 && ch != 0x22) {
                  this.currentAttribute.AppendToValue(ch);
                } else if (ch == 0x22) {
                  this.currentAttribute.CommitValue();
                  this.state = TokenizerState.AfterAttributeValueQuoted;
                  break;
                } else {
                  this.charInput.SetMarkPosition(mark + i);
                  break;
                }
              }
            }
            break;
          }
          case TokenizerState.AttributeValueSingleQuoted: {
            int ch = this.charInput.ReadChar();
            if (ch == 0x27) {
              this.currentAttribute.CommitValue();
              this.state = TokenizerState.AfterAttributeValueQuoted;
            } else if (ch == 0x26) {
              this.lastState = this.state;
              this.state = TokenizerState.CharacterRefInAttributeValue;
            } else if (ch == 0) {
              this.ParseError();
              this.currentAttribute.AppendToValue(0xfffd);
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
            } else {
              this.currentAttribute.AppendToValue(ch);
              // Keep reading characters to
              // reduce the need to re-call
              // this method
              int mark = this.charInput.SetSoftMark();
              for (int i = 0; i < 100; ++i) {
                ch = this.charInput.ReadChar();
                if (ch > 0 && ch != 0x26 && ch != 0x27) {
                  this.currentAttribute.AppendToValue(ch);
                } else if (ch == 0x27) {
                  this.currentAttribute.CommitValue();
                  this.state = TokenizerState.AfterAttributeValueQuoted;
                  break;
                } else {
                  this.charInput.SetMarkPosition(mark + i);
                  break;
                }
              }
            }
            break;
          }
          case TokenizerState.AttributeValueUnquoted: {
            int ch = this.charInput.ReadChar();
            if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
              this.currentAttribute.CommitValue();
              this.state = TokenizerState.BeforeAttributeName;
            } else if (ch == 0x26) {
              this.lastState = this.state;
              this.state = TokenizerState.CharacterRefInAttributeValue;
            } else if (ch == 0x3e) {
              this.currentAttribute.CommitValue();
              this.state = TokenizerState.Data;
              return this.EmitCurrentTag();
            } else if (ch == 0) {
              this.ParseError();
              this.currentAttribute.AppendToValue(0xfffd);
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
            } else {
              if (ch == 0x22 || ch == 0x27 || ch == 0x3c || ch == 0x3d || ch
                == 0x60) {
                this.ParseError();
              }
              this.currentAttribute.AppendToValue(ch);
            }
            break;
          }
          case TokenizerState.AfterAttributeValueQuoted: {
            int mark = this.charInput.SetSoftMark();
            int ch = this.charInput.ReadChar();
            if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
              this.state = TokenizerState.BeforeAttributeName;
            } else if (ch == 0x2f) {
              this.state = TokenizerState.SelfClosingStartTag;
            } else if (ch == 0x3e) {
              this.state = TokenizerState.Data;
              return this.EmitCurrentTag();
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
            } else {
              this.ParseError();
              this.state = TokenizerState.BeforeAttributeName;
              this.charInput.SetMarkPosition(mark);
            }
            break;
          }
          case TokenizerState.SelfClosingStartTag: {
            int mark = this.charInput.SetSoftMark();
            int ch = this.charInput.ReadChar();
            if (ch == 0x3e) {
              this.currentTag.SetSelfClosing(true);
              this.state = TokenizerState.Data;
              return this.EmitCurrentTag();
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
            } else {
              this.ParseError();
              this.state = TokenizerState.BeforeAttributeName;
              this.charInput.SetMarkPosition(mark);
            }
            break;
          }
          case TokenizerState.MarkupDeclarationOpen: {
            int mark = this.charInput.SetSoftMark();
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
                == 'D' && this.charInput.ReadChar() == 'A' &&
                this.charInput.ReadChar() == 'T' &&
                this.charInput.ReadChar() == 'A' && this.charInput.ReadChar() ==
                '[' && this.GetCurrentNode() != null &&
                !HtmlCommon.HTML_NAMESPACE.Equals(this.GetCurrentNode()
                .GetNamespaceURI())) {
                this.state = TokenizerState.CData;
                break;
              }
            }
            this.ParseError();
            this.charInput.SetMarkPosition(mark);
            this.bogusCommentCharacter = -1;
            this.state = TokenizerState.BogusComment;
            break;
          }
          case TokenizerState.CommentStart: {
            int ch = this.charInput.ReadChar();
            if (ch == '-') {
              this.state = TokenizerState.CommentStartDash;
            } else if (ch == 0) {
              this.ParseError();
              this.lastComment.AppendChar((char)0xfffd);
              this.state = TokenizerState.Comment;
            } else if (ch == 0x3e || ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.lastComment.GetTokenType();
              this.AddToken(this.lastComment);
              return ret;
            } else {
              this.lastComment.AppendChar(ch);
              this.state = TokenizerState.Comment;
            }
            break;
          }
          case TokenizerState.CommentStartDash: {
            int ch = this.charInput.ReadChar();
            if (ch == '-') {
              this.state = TokenizerState.CommentEnd;
            } else if (ch == 0) {
              this.ParseError();
              this.lastComment.AppendChar('-');
              this.lastComment.AppendChar(0xfffd);
              this.state = TokenizerState.Comment;
            } else if (ch == 0x3e || ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.lastComment.GetTokenType();
              this.AddToken(this.lastComment);
              return ret;
            } else {
              this.lastComment.AppendChar('-');
              this.lastComment.AppendChar(ch);
              this.state = TokenizerState.Comment;
            }
            break;
          }
          case TokenizerState.Comment: {
            int ch = this.charInput.ReadChar();
            if (ch == '-') {
              this.state = TokenizerState.CommentEndDash;
            } else if (ch == 0) {
              this.ParseError();
              this.lastComment.AppendChar(0xfffd);
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.lastComment.GetTokenType();
              this.AddToken(this.lastComment);
              return ret;
            } else {
              this.lastComment.AppendChar(ch);
            }
            break;
          }
          case TokenizerState.CommentEndDash: {
            int ch = this.charInput.ReadChar();
            if (ch == '-') {
              this.state = TokenizerState.CommentEnd;
            } else if (ch == 0) {
              this.ParseError();
              this.lastComment.Append("-\ufffd");
              this.state = TokenizerState.Comment;
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.lastComment.GetTokenType();
              this.AddToken(this.lastComment);
              return ret;
            } else {
              this.lastComment.AppendChar('-');
              this.lastComment.AppendChar(ch);
              this.state = TokenizerState.Comment;
            }
            break;
          }
          case TokenizerState.CommentEnd: {
            int ch = this.charInput.ReadChar();
            if (ch == 0x3e) {
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.lastComment.GetTokenType();
              this.AddToken(this.lastComment);
              return ret;
            } else if (ch == 0) {
              this.ParseError();
              this.lastComment.Append("--\ufffd");
              this.state = TokenizerState.Comment;
            } else if (ch == 0x21) { // --!>
              this.ParseError();
              this.state = TokenizerState.CommentEndBang;
            } else if (ch == 0x2d) {
              this.ParseError();
              this.lastComment.AppendChar('-');
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.lastComment.GetTokenType();
              this.AddToken(this.lastComment);
              return ret;
            } else {
              this.ParseError();
              this.lastComment.AppendChar('-');
              this.lastComment.AppendChar('-');
              this.lastComment.AppendChar(ch);
              this.state = TokenizerState.Comment;
            }
            break;
          }
          case TokenizerState.CommentEndBang: {
            int ch = this.charInput.ReadChar();
            if (ch == 0x3e) {
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.lastComment.GetTokenType();
              this.AddToken(this.lastComment);
              return ret;
            } else if (ch == 0) {
              this.ParseError();
              this.lastComment.Append("--!\ufffd");
              this.state = TokenizerState.Comment;
            } else if (ch == 0x2d) {
              this.lastComment.Append("--!");
              this.state = TokenizerState.CommentEndDash;
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.lastComment.GetTokenType();
              this.AddToken(this.lastComment);
              return ret;
            } else {
              this.ParseError();
              this.lastComment.Append("--!");
              this.lastComment.AppendChar(ch);
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
            int ch = this.ParseCharacterReference(allowed);
            if (ch < 0) {
              // more than one character in this reference
              int index = Math.Abs(ch + 1);

              this.currentAttribute.AppendToValue(
                HtmlEntities.EntityDoubles[index
                  * 2]);
              this.currentAttribute.AppendToValue(
                HtmlEntities.EntityDoubles[(index * 2) + 1]);
            } else {
              this.currentAttribute.AppendToValue(ch);
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
              return this.EmitCurrentTag();
            } else if (ch >= 'A' && ch <= 'Z') {
              this.currentTag.AppendChar((char)(ch + 0x20));
            } else if (ch == 0) {
              this.ParseError();
              this.currentTag.AppendChar((char)0xfffd);
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
            } else {
              this.currentTag.Append(ch);
            }
            break;
          }
          case TokenizerState.RawTextLessThan: {
            this.charInput.SetHardMark();
            int ch = this.charInput.ReadChar();
            if (ch == 0x2f) {
              this.tempBuilder.Remove(0, this.tempBuilder.Length);
              this.state = TokenizerState.RawTextEndTagOpen;
            } else {
              this.state = TokenizerState.RawText;
              if (ch >= 0) {
                this.charInput.MoveBack(1);
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
              comment.AppendChar(bogusChar);
            }
            while (true) {
              int ch = this.charInput.ReadChar();
              if (ch < 0 || ch == '>') {
                break;
              }
              if (ch == 0) {
                ch = 0xfffd;
              }
              comment.AppendChar(ch);
            }
            int ret = this.tokens.Count | comment.GetTokenType();
            this.AddToken(comment);
            this.state = TokenizerState.Data;
            return ret;
          }
          case TokenizerState.DocType: {
            this.charInput.SetHardMark();
            int ch = this.charInput.ReadChar();
            if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
              this.state = TokenizerState.BeforeDocTypeName;
            } else if (ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
              var valueToken = new DocTypeToken();
              valueToken.ForceQuirks = true;
              int ret = this.tokens.Count | valueToken.GetTokenType();
              this.AddToken(valueToken);
              return ret;
            } else {
              this.ParseError();
              this.charInput.MoveBack(1);
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
              if (ch + 0x20 <= 0xffff) {
                this.docTypeToken.Name.Append((char)(ch +
                  0x20));
              } else if (ch + 0x20 <= 0x10ffff) {
                this.docTypeToken.Name.Append((char)((((ch + 0x20 -
                  0x10000) >> 10) & 0x3ff) | 0xd800));
                this.docTypeToken.Name.Append((char)(((ch + 0x20 -
                  0x10000) & 0x3ff) | 0xdc00));
              }
              this.state = TokenizerState.DocTypeName;
            } else if (ch == 0) {
              this.ParseError();
              this.docTypeToken = new DocTypeToken();
              this.docTypeToken.Name.Append((char)0xfffd);
              this.state = TokenizerState.DocTypeName;
            } else if (ch == 0x3e || ch < 0) {
              this.ParseError();
              this.state = TokenizerState.Data;
              var valueToken = new DocTypeToken();
              valueToken.ForceQuirks = true;
              int ret = this.tokens.Count | valueToken.GetTokenType();
              this.AddToken(valueToken);
              return ret;
            } else {
              this.docTypeToken = new DocTypeToken();
              if (ch <= 0xffff) {
                this.docTypeToken.Name.Append((char)ch);
              } else if (ch <= 0x10ffff) {
                this.docTypeToken.Name.Append((char)((((ch - 0x10000) >>
                  10) & 0x3ff) | 0xd800));
                this.docTypeToken.Name.Append((char)(((ch - 0x10000) &
                  0x3ff) | 0xdc00));
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
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
              return ret;
            } else if (ch >= 'A' && ch <= 'Z') {
              if (ch + 0x20 <= 0xffff) {
                this.docTypeToken.Name.Append((char)(ch +
                  0x20));
              } else if (ch + 0x20 <= 0x10ffff) {
                this.docTypeToken.Name.Append((char)((((ch + 0x20 -
                  0x10000) >> 10) & 0x3ff) | 0xd800));
                this.docTypeToken.Name.Append((char)(((ch + 0x20 -
                  0x10000) & 0x3ff) | 0xdc00));
              }
            } else if (ch == 0) {
              this.ParseError();
              this.docTypeToken.Name.Append((char)0xfffd);
            } else if (ch < 0) {
              this.ParseError();
              this.docTypeToken.ForceQuirks = true;
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
              return ret;
            } else {
              if (ch <= 0xffff) {
                this.docTypeToken.Name.Append((char)ch);
              } else if (ch <= 0x10ffff) {
                this.docTypeToken.Name.Append((char)((((ch - 0x10000) >>
                  10) & 0x3ff) | 0xd800));
                this.docTypeToken.Name.Append((char)(((ch - 0x10000) &
                  0x3ff) | 0xdc00));
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
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
              return ret;
            } else if (ch < 0) {
              this.ParseError();
              this.docTypeToken.ForceQuirks = true;
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
              return ret;
            } else {
              var ch2 = 0;
              int pos = this.charInput.SetSoftMark();
              if (ch == 'P' || ch == 'p') {
                if (((ch2 = this.charInput.ReadChar()) == 'u' || ch2 == 'U'
                  ) && ((ch2 = this.charInput.ReadChar()) == 'b' || ch2 ==
                    'B') &&
                  ((ch2 = this.charInput.ReadChar()) == 'l' || ch2 == 'L') &&
                  ((ch2 = this.charInput.ReadChar()) == 'i' || ch2 == 'I') &&
                  ((ch2 = this.charInput.ReadChar()) == 'c' || ch2 == 'C')
                ) {
                  this.state = TokenizerState.AfterDocTypePublic;
                } else {
                  this.ParseError();
                  this.charInput.SetMarkPosition(pos);
                  this.docTypeToken.ForceQuirks = true;
                  this.state = TokenizerState.BogusDocType;
                }
              } else if (ch == 'S' || ch == 's') {
                if (((ch2 = this.charInput.ReadChar()) == 'y' || ch2 == 'Y'
                  ) && ((ch2 = this.charInput.ReadChar()) == 's' || ch2 ==
                    'S') &&
                  ((ch2 = this.charInput.ReadChar()) == 't' || ch2 == 'T') &&
                  ((ch2 = this.charInput.ReadChar()) == 'e' || ch2 == 'E') &&
                  ((ch2 = this.charInput.ReadChar()) == 'm' || ch2 == 'M')
                ) {
                  this.state = TokenizerState.AfterDocTypeSystem;
                } else {
                  this.ParseError();
                  this.charInput.SetMarkPosition(pos);
                  this.docTypeToken.ForceQuirks = true;
                  this.state = TokenizerState.BogusDocType;
                }
              } else {
                this.ParseError();
                this.charInput.SetMarkPosition(pos);
                this.docTypeToken.ForceQuirks = true;
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
              if (this.state == TokenizerState.AfterDocTypePublic) {
                this.ParseError();
              }
              this.state = TokenizerState.DocTypePublicIDDoubleQuoted;
            } else if (ch == 0x27) {
              if (this.state == TokenizerState.AfterDocTypePublic) {
                this.ParseError();
              }
              this.state = TokenizerState.DocTypePublicIDSingleQuoted;
            } else if (ch == 0x3e || ch < 0) {
              this.ParseError();
              this.docTypeToken.ForceQuirks = true;
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
              return ret;
            } else {
              this.ParseError();
              this.docTypeToken.ForceQuirks = true;
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
              if (this.state == TokenizerState.AfterDocTypeSystem) {
                this.ParseError();
              }
              this.state = TokenizerState.DocTypeSystemIDDoubleQuoted;
            } else if (ch == 0x27) {
              if (this.state == TokenizerState.AfterDocTypeSystem) {
                this.ParseError();
              }
              this.state = TokenizerState.DocTypeSystemIDSingleQuoted;
            } else if (ch == 0x3e || ch < 0) {
              this.ParseError();
              this.docTypeToken.ForceQuirks = true;
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
              return ret;
            } else {
              this.ParseError();
              this.docTypeToken.ForceQuirks = true;
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
              this.ParseError();
              this.docTypeToken.ValuePublicID.Append((char)0xfffd);
            } else if (ch == 0x3e || ch < 0) {
              this.ParseError();
              this.docTypeToken.ForceQuirks = true;
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
              return ret;
            } else {
              if (ch <= 0xffff) {
                this.docTypeToken.ValuePublicID.Append((char)ch);
              } else if (ch <= 0x10ffff) {
                this.docTypeToken.ValuePublicID.Append((char)((((ch -
                  0x10000) >> 10) & 0x3ff) | 0xd800));
                this.docTypeToken.ValuePublicID.Append((char)(((ch -
                  0x10000) & 0x3ff) | 0xdc00));
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
              this.ParseError();
              this.docTypeToken.ValueSystemID.Append((char)0xfffd);
            } else if (ch == 0x3e || ch < 0) {
              this.ParseError();
              this.docTypeToken.ForceQuirks = true;
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
              return ret;
            } else {
              if (ch <= 0xffff) {
                this.docTypeToken.ValueSystemID.Append((char)ch);
              } else if (ch <= 0x10ffff) {
                this.docTypeToken.ValueSystemID.Append((char)((((ch -
                  0x10000) >> 10) & 0x3ff) | 0xd800));
                this.docTypeToken.ValueSystemID.Append((char)(((ch -
                  0x10000) & 0x3ff) | 0xdc00));
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
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
              return ret;
            } else if (ch == 0x22) {
              if (this.state == TokenizerState.AfterDocTypePublicID) {
                this.ParseError();
              }
              this.state = TokenizerState.DocTypeSystemIDDoubleQuoted;
            } else if (ch == 0x27) {
              if (this.state == TokenizerState.AfterDocTypePublicID) {
                this.ParseError();
              }
              this.state = TokenizerState.DocTypeSystemIDSingleQuoted;
            } else if (ch < 0) {
              this.ParseError();
              this.docTypeToken.ForceQuirks = true;
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
              return ret;
            } else {
              this.ParseError();
              this.docTypeToken.ForceQuirks = true;
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
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
              return ret;
            } else if (ch < 0) {
              this.ParseError();
              this.docTypeToken.ForceQuirks = true;
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
              return ret;
            } else {
              this.ParseError();
              this.state = TokenizerState.BogusDocType;
            }
            break;
          }
          case TokenizerState.BogusDocType: {
            int ch = this.charInput.ReadChar();
            if (ch == 0x3e || ch < 0) {
              this.state = TokenizerState.Data;
              int ret = this.tokens.Count | this.docTypeToken.GetTokenType();
              this.AddToken(this.docTypeToken);
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
                buffer.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) |
                  0xd800));
                buffer.Append((char)(((ch - 0x10000) & 0x3ff) | 0xdc00));
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
              size -= 3; // don't count the ']]>'
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
            this.charInput.SetHardMark();
            int ch = this.charInput.ReadChar();
            if (ch == 0x2f) {
              this.tempBuilder.Remove(0, this.tempBuilder.Length);
              this.state = TokenizerState.RcDataEndTagOpen;
            } else {
              this.state = TokenizerState.RcData;
              if (ch >= 0) {
                this.charInput.MoveBack(1);
              }
              return 0x3c;
            }
            break;
          }
          default:
            throw new InvalidOperationException();
        }
      }
    }

    private IElement PopCurrentNode() {
      return (this.openElements.Count > 0) ?
        RemoveAtIndex(this.openElements, this.openElements.Count - 1) : null;
    }

    private void PushFormattingElement(StartTagToken tag) {
      Element valueElement = this.AddHtmlElement(tag);
      var matchingElements = 0;
      var lastMatchingElement = -1;
      string valueName = valueElement.GetLocalName();
      for (int i = this.formattingElements.Count - 1; i >= 0; --i) {
        FormattingElement fe = this.formattingElements[i];
        if (fe.IsMarker()) {
          break;
        }
        if (fe.Element.GetLocalName().Equals(valueName,
          StringComparison.Ordinal) &&
          fe.Element.GetNamespaceURI().Equals(valueElement.GetNamespaceURI(),
          StringComparison.Ordinal)) {
          IList<IAttr> attribs = fe.Element.GetAttributes();
          IList<IAttr> myAttribs = valueElement.GetAttributes();
          if (attribs.Count == myAttribs.Count) {
            var match = true;
            for (int j = 0; j < myAttribs.Count; ++j) {
              string name1 = myAttribs[j].GetName();
              string namespaceValue = myAttribs[j].GetNamespaceURI();
              string value = myAttribs[j].GetValue();
              string otherValue = fe.Element.GetAttributeNS(
                  namespaceValue,
                  name1);
              if (otherValue == null || !otherValue.Equals(value,
                StringComparison.Ordinal)) {
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

    private void ReconstructFormatting() {
      if (this.formattingElements.Count == 0) {
        return;
      }
      // Console.WriteLine("reconstructing elements");
      // Console.WriteLine(formattingElements);
      FormattingElement fe =
        this.formattingElements[this.formattingElements.Count - 1];
      if (fe.IsMarker() || this.openElements.Contains(fe.Element)) {
        return;
      }
      int i = this.formattingElements.Count - 1;
      while (i > 0) {
        fe = this.formattingElements[i - 1];
        --i;
        if (!fe.IsMarker() && !this.openElements.Contains(fe.Element)) {
          continue;
        }
        ++i;
        break;
      }
      for (int j = i; j < this.formattingElements.Count; ++j) {
        fe = this.formattingElements[j];
        Element valueElement = this.AddHtmlElement(fe.Token);
        fe.Element = valueElement;
        fe.ValueMarker = false;
      }
    }

    private void RemoveFormattingElement(IElement valueAElement) {
      FormattingElement f = null;
      foreach (var fe in this.formattingElements) {
        if (!fe.IsMarker() && valueAElement.Equals(fe.Element)) {
          f = fe;
          break;
        }
      }
      if (f != null) {
        this.formattingElements.Remove(f);
      }
    }

    private void ResetInsertionMode() {
      var last = false;
      for (int i = this.openElements.Count - 1; i >= 0; --i) {
        IElement e = this.openElements[i];
        if (this.context != null && i == 0) {
          e = this.context;
          last = true;
        }
        if (!last && (HtmlCommon.IsHtmlElement(e, "th") ||
          HtmlCommon.IsHtmlElement(e, "td"))) {
          this.insertionMode = InsertionMode.InCell;
          break;
        }
        if (HtmlCommon.IsHtmlElement(e, "select")) {
          this.insertionMode = InsertionMode.InSelect;
          if (!last) {
            for (int j = i - 1; j >= 0; --j) {
              e = this.openElements[j];
              if (HtmlCommon.IsHtmlElement(e, "template")) {
                break;
              }
              if (HtmlCommon.IsHtmlElement(e, "table")) {
                this.insertionMode = InsertionMode.InSelectInTable;
                break;
              }
            }
          }
          break;
        }
        if (HtmlCommon.IsHtmlElement(e, "colgroup")) {
          this.insertionMode = InsertionMode.InColumnGroup;
          break;
        }
        if (HtmlCommon.IsHtmlElement(e, "tr")) {
          this.insertionMode = InsertionMode.InRow;
          break;
        }
        if (HtmlCommon.IsHtmlElement(e, "caption")) {
          this.insertionMode = InsertionMode.InCaption;
          break;
        }
        if (HtmlCommon.IsHtmlElement(e, "table")) {
          this.insertionMode = InsertionMode.InTable;
          break;
        }
        if (HtmlCommon.IsHtmlElement(e, "template")) {
          this.insertionMode = this.templateModes[this.templateModes.Count - 1];
          break;
        }
        if (HtmlCommon.IsHtmlElement(e, "frameset")) {
          this.insertionMode = InsertionMode.InFrameset;
          break;
        }
        if (HtmlCommon.IsHtmlElement(e, "html")) {
          this.insertionMode = (this.headElement == null) ?
            InsertionMode.BeforeHead : InsertionMode.AfterHead;
          break;
        }
        if (HtmlCommon.IsHtmlElement(e, "head")) {
          this.insertionMode = InsertionMode.InHead;
          break;
        }

        if (HtmlCommon.IsHtmlElement(e, "body")) {
          this.insertionMode = InsertionMode.InBody;
          break;
        }
        if (HtmlCommon.IsHtmlElement(e, "thead") ||
          HtmlCommon.IsHtmlElement(e, "tbody") ||
          HtmlCommon.IsHtmlElement(e, "tfoot")) {
          this.insertionMode = InsertionMode.InTableBody;
          break;
        }
        if (last) {
          this.insertionMode = InsertionMode.InBody;
          break;
        }
      }
    }

    internal void SetCData() {
      this.state = TokenizerState.CData;
    }

    internal void SetPlainText() {
      this.state = TokenizerState.PlainText;
    }

    internal void SetRawText() {
      this.state = TokenizerState.RawText;
    }

    internal void SetRcData() {
      this.state = TokenizerState.RcData;
    }

    private void SkipLineFeed() {
      int mark = this.charInput.SetSoftMark();
      int nextToken = this.charInput.ReadChar();
      if (nextToken == 0x0a) {
        return; // ignore the valueToken if it's 0x0A
      } else if (nextToken == 0x26) { // start of character reference
        int charref = this.ParseCharacterReference(-1);
        if (charref < 0) {
          // more than one character in this reference
          int index = Math.Abs(charref + 1);
          this.tokenQueue.Add(HtmlEntities.EntityDoubles[index * 2]);
          this.tokenQueue.Add(HtmlEntities.EntityDoubles[(index * 2) + 1]);
        } else if (charref == 0x0a) {
          return; // ignore the valueToken
        } else {
          this.tokenQueue.Add(charref);
        }
      } else {
        // anything else; reset the input stream
        this.charInput.SetMarkPosition(mark);
      }
    }

    private void StopParsing() {
      this.done = true;
      if (String.IsNullOrEmpty(this.valueDocument.DefaultLanguage)) {
        if (this.contentLanguage.Length == 1) {
          // set the fallback language if there is
          // only one language defined and no meta valueElement
          // defines the language
          this.valueDocument.DefaultLanguage = this.contentLanguage[0];
        }
      }
      this.valueDocument.Encoding = this.encoding.GetEncoding();
      string docbase = this.valueDocument.GetBaseURI();
      if (docbase == null || docbase.Length == 0) {
        docbase = this.baseurl;
      } else {
        if (this.baseurl != null && this.baseurl.Length > 0) {
          this.valueDocument.SetBaseURI(
            HtmlCommon.ResolveURL(
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
