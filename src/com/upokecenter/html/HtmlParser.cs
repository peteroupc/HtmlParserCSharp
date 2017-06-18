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

namespace com.upokecenter.html {
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using com.upokecenter.encoding;
using com.upokecenter.io;
using com.upokecenter.net;
using com.upokecenter.util;

sealed class HtmlParser {
  internal class CommentToken : IToken {
    IntList value;
    public CommentToken() {
      value = new IntList();
    }

    public void appendChar(int ch) {
      value.appendInt(ch);
    }

    public int getType() {
      return TOKEN_COMMENT;
    }

    public string getValue() {
      return value.ToString();
    }
  }
  internal class DocTypeToken : IToken {
    public IntList name;
    public IntList publicID;
    public IntList systemID;
    public bool forceQuirks;
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
      return TOKEN_END_TAG;
    }
  }
  private class FormattingElement {
    public bool marker;
    public Element element;
    public StartTagToken token;
    public bool isMarker() {
      return marker;
    }
    public override sealed string ToString() {
      return "FormattingElement [marker=" + marker + ", token=" + token + "]\n";
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
      return TOKEN_START_TAG;
    }
    public void setName(string _string) {
      builder.Clear();
      builder.Append(_string);
    }
  }
  internal abstract class TagToken : IToken {
    protected StringBuilder builder;
    IList<Attr> attributes = null;
    bool selfClosing = false;
    bool selfClosingAck = false;
    public TagToken(char ch) {
      builder = new StringBuilder();
      builder.Append(ch);
    }

    public TagToken(string name) {
      builder = new StringBuilder();
      builder.Append(name);
    }

    public void ackSelfClosing() {
      selfClosingAck = true;
    }

    public Attr addAttribute(char ch) {
      if (attributes == null) {
        attributes = new List<Attr>();
      }
      Attr a = new Attr(ch);
      attributes.Add(a);
      return a;
    }

    public Attr addAttribute(int ch) {
      if (attributes == null) {
        attributes = new List<Attr>();
      }
      Attr a = new Attr(ch);
      attributes.Add(a);
      return a;
    }

    public void append(int ch) {
      if (ch<0x10000) {
        builder.Append((char)ch);
      } else {
        ch-=0x10000;
        int lead = ch/0x400 + 0xd800;
        int trail=(ch & 0x3ff)+0xdc00;
        builder.Append((char)lead);
        builder.Append((char)trail);
      }
    }

    public void appendChar(char ch) {
      builder.Append(ch);
    }

    public bool checkAttributeName() {
      if (attributes == null) {
 return true;
}
      int size = attributes.Count;
      if (size >= 2) {
        string thisname = attributes[size-1].getName();
        for (int i = 0;i<size-1; ++i) {
          if (attributes[i].getName().Equals(thisname)) {
            // Attribute with this name already exists;
            // remove it
            attributes.RemoveAt(size-1);
            return false;
          }
        }
      }
      return true;
    }

    public string getAttribute(string name) {
      if (attributes == null) {
 return null;
}
      int size = attributes.Count;
      for (int i = 0; i < size; ++i) {
        IAttr a = attributes[i];
        string thisname = a.getName();
        if (thisname.Equals(name)) {
 return a.getValue();
}
      }
      return null;
    }

    public string getAttributeNS(string name, string _namespace) {
      if (attributes == null) {
 return null;
}
      int size = attributes.Count;
      for (int i = 0; i < size; ++i) {
        Attr a = attributes[i];
        if (a.isAttribute(name, _namespace)) {
 return a.getValue();
}
      }
      return null;
    }

    public IList<Attr> getAttributes() {
      if (attributes == null) {
 return new Attr[0];
} else {
 return attributes;
}
    }

    public string getName() {
      return builder.ToString();
    }

    public abstract int getType();
    public bool isAckSelfClosing() {
      return !selfClosing || selfClosingAck;
    }
    public bool isSelfClosing() {
      return selfClosing;
    }

    public bool isSelfClosingAck() {
      return selfClosingAck;
    }

    public void setAttribute(string attrname, string value) {
      if (attributes == null) {
        attributes = new List<Attr>();
        attributes.Add(new Attr(attrname, value));
      } else {
        int size = attributes.Count;
        for (int i = 0; i < size; ++i) {
          Attr a = attributes[i];
          string thisname = a.getName();
          if (thisname.Equals(attrname)) {
            a.setValue(value);
            return;
          }
        }
        attributes.Add(new Attr(attrname, value));
      }
    }

    public void setSelfClosing(bool selfClosing) {
      this.selfClosing = selfClosing;
    }
    public override sealed string ToString() {
      return "TagToken [" + builder.ToString() + ", "+
          attributes +(selfClosing ? (", selfClosingAck=" +
            selfClosingAck) : "") + "]" ;
    }
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

  public static readonly string MATHML_NAMESPACE =
    "http://www.w3.org/1998/Math/MathML" ;

  public static readonly string SVG_NAMESPACE = "http://www.w3.org/2000/svg";

  internal static int TOKEN_EOF = 0x10000000;

  internal static int TOKEN_START_TAG = 0x20000000;

  internal static int TOKEN_END_TAG = 0x30000000;

  internal static int TOKEN_COMMENT = 0x40000000;

  internal static int TOKEN_DOCTYPE = 0x50000000;
  internal static int TOKEN_TYPE_MASK = unchecked((int)0xf0000000);
  internal static int TOKEN_CHARACTER = 0x00000000;
  private static int TOKEN_INDEX_MASK = 0x0fffffff;
  public static readonly string HTML_NAMESPACE="http://www.w3.org/1999/xhtml";

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
  private IList<Element> integrationElements = new List<Element>();
  private IList<IToken> tokens = new List<IToken>();
  private TagToken lastStartTag = null;
  private Html5Decoder decoder = null;
  private TagToken currentEndTag = null;
  private TagToken currentTag = null;
  private Attr currentAttribute = null;
  private int bogusCommentCharacter = 0;
  private IntList tempBuffer = new IntList();
  private TokenizerState state = TokenizerState.Data;
  private bool framesetOk = true;
  private IList<int> tokenQueue = new List<int>();
  private InsertionMode insertionMode = InsertionMode.Initial;
  private InsertionMode originalInsertionMode = InsertionMode.Initial;
  private IList<Element> openElements = new List<Element>();
  private IList<FormattingElement> formattingElements = new
    List<FormattingElement>();
  private Element headElement = null;
  private Element formElement = null;
  private Element inputElement = null;
  private string baseurl = null;
  private bool hasForeignContent = false;
  internal Document document = null;
  private bool done = false;

  private IntList pendingTableCharacters = new IntList();
  private bool doFosterParent;
  private Element context;
  private bool noforeign;
  private string address;

  private string[] contentLanguage;

  public static readonly string XLINK_NAMESPACE="http://www.w3.org/1999/xlink";

  public static readonly string
    XML_NAMESPACE="http://www.w3.org/XML/1998/_namespace" ;
private static readonly string
    XMLNS_NAMESPACE="http://www.w3.org/2000/xmlns/" ;

  private static T removeAtIndex<T>(IList<T> array, int index) {
    T ret = array[index];
    array.RemoveAt(index);
    return ret;
  }

  public HtmlParser(PeterO.Support.InputStream s, string address) :
    this(s, address, null, null) {
  }

  public HtmlParser(PeterO.Support.InputStream s, string address, string
    charset) : this(s, address, charset, null) {
  }

  public HtmlParser(PeterO.Support.InputStream source, string address,
      string charset, string contentLanguage) {
    if (source == null) {
 throw new ArgumentException();
}
    if (address != null && address.Length>0) {
      URL url = URL.parse(address);
      if (url == null || url.getScheme().Length == 0) {
 throw new ArgumentException();
}
    }
    this.contentLanguage = HeaderParser.getLanguages(contentLanguage);
    this.address = address;
    initialize();
    inputStream = new ConditionalBufferInputStream(source);
    encoding = CharsetSniffer.sniffEncoding(inputStream, charset);
    inputStream.rewind();
    decoder = new Html5Decoder(TextEncoding.getDecoder(encoding.getEncoding()));
    charInput = new StackableCharacterInput(new
      DecoderCharacterInput(inputStream, decoder));
  }

  private void addCommentNodeToCurrentNode(int token) {
    insertInCurrentNode(createCommentNode(token));
  }

  private void addCommentNodeToDocument(int token) {
    document.appendChild(createCommentNode(token));
  }

  private void addCommentNodeToFirst(int token) {
    openElements[0].appendChild(createCommentNode(token));
  }

  private Element addHtmlElement(StartTagToken tag) {
    Element element = Element.fromToken(tag);
    Element currentNode = getCurrentNode();
    if (currentNode != null) {
      insertInCurrentNode(element);
    } else {
      document.appendChild(element);
    }
    openElements.Add(element);
    return element;
  }

  private Element addHtmlElementNoPush(StartTagToken tag) {
    Element element = Element.fromToken(tag);
    Element currentNode = getCurrentNode();
    if (currentNode != null) {
      insertInCurrentNode(element);
    }
    return element;
  }

  private void adjustForeignAttributes(StartTagToken token) {
    IList<Attr> attributes = token.getAttributes();
    foreach (var attr in attributes) {
      string name = attr.getName();
      if (name.Equals("xlink:actuate") || name.Equals("xlink:arcrole") ||
          name.Equals("xlink:href") || name.Equals("xlink:role") ||
          name.Equals("xlink:show") || name.Equals("xlink:title") ||
          name.Equals("xlink:type")) {
        attr.setNamespace(XLINK_NAMESPACE);
  } else if (name.Equals("xml:base") || name.Equals("xml:lang") ||
          name.Equals("xml:space")) {
        attr.setNamespace(XML_NAMESPACE);
  } else if (name.Equals("xmlns") || name.Equals("xmlns:xlink")) {
        attr.setNamespace(XMLNS_NAMESPACE);
      }
    }
  }

  private void adjustMathMLAttributes(StartTagToken token) {
    IList<Attr> attributes = token.getAttributes();
    foreach (var attr in attributes) {
      if (attr.getName().Equals("definitionurl")) {
        attr.setName("definitionURL");
      }
    }
  }

  private void adjustSvgAttributes(StartTagToken token) {
    IList<Attr> attributes = token.getAttributes();
    foreach (var attr in attributes) {
      string name = attr.getName();
      if (name.Equals("attributename")) { attr.setName("attributeName");
  } else if (name.Equals("attributetype")) { attr.setName("attributeType");
  } else if (name.Equals("basefrequency")) { attr.setName("baseFrequency");
  } else if (name.Equals("baseprofile")) { attr.setName("baseProfile");
  } else if (name.Equals("calcmode")) { attr.setName("calcMode");
  } else if (name.Equals("clippathunits")) { attr.setName("clipPathUnits");
  } else if (name.Equals("contentscripttype")) {
        attr.setName("contentScriptType");
  } else if (name.Equals("contentstyletype")) { attr.setName("contentStyleType");
  } else if (name.Equals("diffuseconstant")) { attr.setName("diffuseConstant");
  } else if (name.Equals("edgemode")) { attr.setName("edgeMode");
  } else if (name.Equals("externalresourcesrequired")) {
        attr.setName("externalResourcesRequired");
  } else if (name.Equals("filterres")) { attr.setName("filterRes");
  } else if (name.Equals("filterunits")) { attr.setName("filterUnits");
  } else if (name.Equals("glyphref")) { attr.setName("glyphRef");
  } else if (name.Equals("gradienttransform")) {
        attr.setName("gradientTransform");
  } else if (name.Equals("gradientunits")) { attr.setName("gradientUnits");
  } else if (name.Equals("kernelmatrix")) { attr.setName("kernelMatrix");
  } else if (name.Equals("kernelunitlength")) { attr.setName("kernelUnitLength");
  } else if (name.Equals("keypoints")) { attr.setName("keyPoints");
  } else if (name.Equals("keysplines")) { attr.setName("keySplines");
  } else if (name.Equals("keytimes")) { attr.setName("keyTimes");
  } else if (name.Equals("lengthadjust")) { attr.setName("lengthAdjust");
  } else if (name.Equals("limitingconeangle")) {
        attr.setName("limitingConeAngle");
  } else if (name.Equals("markerheight")) { attr.setName("markerHeight");
  } else if (name.Equals("markerunits")) { attr.setName("markerUnits");
  } else if (name.Equals("markerwidth")) { attr.setName("markerWidth");
  } else if (name.Equals("maskcontentunits")) { attr.setName("maskContentUnits");
  } else if (name.Equals("maskunits")) { attr.setName("maskUnits");
  } else if (name.Equals("numoctaves")) { attr.setName("numOctaves");
  } else if (name.Equals("pathlength")) { attr.setName("pathLength");
  } else if (name.Equals("patterncontentunits")) {
        attr.setName("patternContentUnits");
  } else if (name.Equals("patterntransform")) { attr.setName("patternTransform");
  } else if (name.Equals("patternunits")) { attr.setName("patternUnits");
  } else if (name.Equals("pointsatx")) { attr.setName("pointsAtX");
  } else if (name.Equals("pointsaty")) { attr.setName("pointsAtY");
  } else if (name.Equals("pointsatz")) { attr.setName("pointsAtZ");
  } else if (name.Equals("preservealpha")) { attr.setName("preserveAlpha");
  } else if (name.Equals("preserveaspectratio")) {
        attr.setName("preserveAspectRatio");
  } else if (name.Equals("primitiveunits")) { attr.setName("primitiveUnits");
  } else if (name.Equals("refx")) { attr.setName("refX");
  } else if (name.Equals("refy")) { attr.setName("refY");
  } else if (name.Equals("repeatcount")) { attr.setName("repeatCount");
  } else if (name.Equals("repeatdur")) { attr.setName("repeatDur");
  } else if (name.Equals("requiredextensions")) {
        attr.setName("requiredExtensions");
  } else if (name.Equals("requiredfeatures")) { attr.setName("requiredFeatures");
  } else if (name.Equals("specularconstant")) { attr.setName("specularConstant");
  } else if (name.Equals("specularexponent")) { attr.setName("specularExponent");
  } else if (name.Equals("spreadmethod")) { attr.setName("spreadMethod");
  } else if (name.Equals("startoffset")) { attr.setName("startOffset");
  } else if (name.Equals("stddeviation")) { attr.setName("stdDeviation");
  } else if (name.Equals("stitchtiles")) { attr.setName("stitchTiles");
  } else if (name.Equals("surfacescale")) { attr.setName("surfaceScale");
  } else if (name.Equals("systemlanguage")) { attr.setName("systemLanguage");
  } else if (name.Equals("tablevalues")) { attr.setName("tableValues");
  } else if (name.Equals("targetx")) { attr.setName("targetX");
  } else if (name.Equals("targety")) { attr.setName("targetY");
  } else if (name.Equals("textlength")) { attr.setName("textLength");
  } else if (name.Equals("viewbox")) { attr.setName("viewBox");
  } else if (name.Equals("viewtarget")) { attr.setName("viewTarget");
  } else if (name.Equals("xchannelselector")) { attr.setName("xChannelSelector");
  } else if (name.Equals("ychannelselector")) { attr.setName("yChannelSelector");
  } else if (name.Equals("zoomandpan")) { attr.setName("zoomAndPan"); }
    }
  }

  private bool applyEndTag(string name, InsertionMode? insMode) {
    return applyInsertionMode(getArtificialToken(TOKEN_END_TAG, name), insMode);
  }

  private bool applyForeignContext(int token) {
    if (token == 0) {
      error = true;
      insertCharacter(getCurrentNode(), 0xfffd);
      return true;
    } else if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
      insertCharacter(getCurrentNode(), token);
      if (token != 0x09 && token != 0x0c && token != 0x0a &&
          token != 0x0d && token != 0x20) {
        framesetOk = false;
      }
      return true;
    } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
      addCommentNodeToCurrentNode(token);
    } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
      error = true;
      return false;
    } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
      StartTagToken tag=(StartTagToken)getToken(token);
      string name = tag.getName();
      if (name.Equals("font")) {
        if (tag.getAttribute("color")!=null || tag.getAttribute("size")!=null ||
            tag.getAttribute("face")!=null) {
          error = true;
          while (true) {
            popCurrentNode();
            Element node = getCurrentNode();
            if (node.getNamespaceURI().Equals(HTML_NAMESPACE) ||
                isMathMLTextIntegrationPoint(node) ||
                isHtmlIntegrationPoint(node)) {
              break;
            }
          }
          return applyInsertionMode(token, null);
        }
      } else if (name.Equals("b") ||
          name.Equals("big") || name.Equals("blockquote") ||
            name.Equals("body") || name.Equals("br") ||
          name.Equals("center") || name.Equals("code") || name.Equals("dd"
) || name.Equals("div") ||
          name.Equals("dl") || name.Equals("dt") || name.Equals("em") ||
            name.Equals("embed") ||
          name.Equals("h1") || name.Equals("h2") || name.Equals("h3") ||
            name.Equals("h4") ||
          name.Equals("h5") || name.Equals("h6") || name.Equals("head")||
            name.Equals("hr") ||
          name.Equals("i") || name.Equals("img") || name.Equals("li") ||
            name.Equals("listing") ||
          name.Equals("menu") || name.Equals("meta") || name.Equals("nobr"
) || name.Equals("ol") ||
          name.Equals("p") || name.Equals("pre") || name.Equals("ruby")||
            name.Equals("s") ||
          name.Equals("small") || name.Equals("span") ||
            name.Equals("strong") || name.Equals("strike") ||
          name.Equals("sub") || name.Equals("sup") || name.Equals("table"
) || name.Equals("tt") ||
          name.Equals("u") || name.Equals("ul") || name.Equals("var")) {
        error = true;
        if (context != null && !hasNativeElementInScope()) {
          noforeign = true;
          bool ret = applyInsertionMode(token, InsertionMode.InBody);
          noforeign = false;
          return ret;
        }
        while (true) {
          popCurrentNode();
          Element node = getCurrentNode();
          if (node.getNamespaceURI().Equals(HTML_NAMESPACE) ||
              isMathMLTextIntegrationPoint(node) ||
              isHtmlIntegrationPoint(node)) {
            break;
          }
        }
        return applyInsertionMode(token, null);
      } else {
        string _namespace = getCurrentNode().getNamespaceURI();
        bool mathml = false;
        if (SVG_NAMESPACE.Equals(_namespace)) {
          if (name.Equals("altglyph")) {
            tag.setName("altGlyph");
          } else if (name.Equals("altglyphdef")) {
            tag.setName("altGlyphDef");
          } else if (name.Equals("altglyphitem")) {
            tag.setName("altGlyphItem");
          } else if (name.Equals("animatecolor")) {
            tag.setName("animateColor");
          } else if (name.Equals("animatemotion")) {
            tag.setName("animateMotion");
          } else if (name.Equals("animatetransform")) {
            tag.setName("animateTransform");
          } else if (name.Equals("clippath")) {
            tag.setName("clipPath");
          } else if (name.Equals("feblend")) {
            tag.setName("feBlend");
          } else if (name.Equals("fecolormatrix")) {
            tag.setName("feColorMatrix");
          } else if (name.Equals("fecomponenttransfer")) {
            tag.setName("feComponentTransfer");
          } else if (name.Equals("fecomposite")) {
            tag.setName("feComposite");
          } else if (name.Equals("feconvolvematrix")) {
            tag.setName("feConvolveMatrix");
          } else if (name.Equals("fediffuselighting")) {
            tag.setName("feDiffuseLighting");
          } else if (name.Equals("fedisplacementmap")) {
            tag.setName("feDisplacementMap");
          } else if (name.Equals("fedistantlight")) {
            tag.setName("feDistantLight");
          } else if (name.Equals("feflood")) {
            tag.setName("feFlood");
          } else if (name.Equals("fefunca")) {
            tag.setName("feFuncA");
          } else if (name.Equals("fefuncb")) {
            tag.setName("feFuncB");
          } else if (name.Equals("fefuncg")) {
            tag.setName("feFuncG");
          } else if (name.Equals("fefuncr")) {
            tag.setName("feFuncR");
          } else if (name.Equals("fegaussianblur")) {
            tag.setName("feGaussianBlur");
          } else if (name.Equals("feimage")) {
            tag.setName("feImage");
          } else if (name.Equals("femerge")) {
            tag.setName("feMerge");
          } else if (name.Equals("femergenode")) {
            tag.setName("feMergeNode");
          } else if (name.Equals("femorphology")) {
            tag.setName("feMorphology");
          } else if (name.Equals("feoffset")) {
            tag.setName("feOffset");
          } else if (name.Equals("fepointlight")) {
            tag.setName("fePointLight");
          } else if (name.Equals("fespecularlighting")) {
            tag.setName("feSpecularLighting");
          } else if (name.Equals("fespotlight")) {
            tag.setName("feSpotLight");
          } else if (name.Equals("fetile")) {
            tag.setName("feTile");
          } else if (name.Equals("feturbulence")) {
            tag.setName("feTurbulence");
          } else if (name.Equals("foreignobject")) {
            tag.setName("foreignObject");
          } else if (name.Equals("glyphref")) {
            tag.setName("glyphRef");
          } else if (name.Equals("lineargradient")) {
            tag.setName("linearGradient");
          } else if (name.Equals("radialgradient")) {
            tag.setName("radialGradient");
          } else if (name.Equals("textpath")) {
            tag.setName("textPath");
          }
          adjustSvgAttributes(tag);
        } else if (MATHML_NAMESPACE.Equals(_namespace)) {
          adjustMathMLAttributes(tag);
          mathml = true;
        }
        adjustForeignAttributes(tag);
        Element e = insertForeignElement(tag, _namespace);
        if (mathml && tag.getName().Equals("annotation-xml")) {
          string encoding=tag.getAttribute("encoding");
          if (encoding != null) {
            encoding = StringUtility.toLowerCaseAscii(encoding);
            if (encoding.Equals("text/html") ||
                encoding.Equals("application/xhtml+xml")) {
              integrationElements.Add(e);
            }
          }
        }
        if (tag.isSelfClosing()) {
          if (name.Equals("script")) {
            tag.ackSelfClosing();
            applyEndTag("script",null);
          } else {
            popCurrentNode();
            tag.ackSelfClosing();
          }
        }
        return true;
      }
      return false;
    } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
      EndTagToken tag=(EndTagToken)getToken(token);
      string name = tag.getName();
      if (name.Equals("script") &&
          getCurrentNode().getLocalName().Equals("script") &&
          SVG_NAMESPACE.Equals(getCurrentNode().getNamespaceURI())) {
        popCurrentNode();
      } else {
if (!StringUtility.toLowerCaseAscii(getCurrentNode() .getLocalName())
          .Equals(name)) {
          error = true;
        }
        int originalSize = openElements.Count;
        for (int i1 = originalSize-1;i1 >= 0; --i1) {
          if (i1 == 0) {
 return true;
}
          Element node = openElements[i1];
          if (i1<originalSize-1 &&
              HTML_NAMESPACE.Equals(node.getNamespaceURI())) {
            noforeign = true;
            return applyInsertionMode(token, null);
          }
          string nodeName = StringUtility.toLowerCaseAscii(node.getLocalName());
          if (name.Equals(nodeName)) {
            while (true) {
              Element node2 = popCurrentNode();
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
 return (token == TOKEN_EOF) ? (applyInsertionMode(token, null)) : (true);
}
  }

  private bool applyInsertionMode(int token, InsertionMode? insMode) {
    //Console.WriteLine("[[%08X %s %s %s(%s)"
    // , token, getToken(token), insMode == null ? insertionMode :
    //insMode, isForeignContext(token), noforeign);
    if (!noforeign && isForeignContext(token)) {
 return applyForeignContext(token);
}
    noforeign = false;
    if (insMode == null) {
      insMode = insertionMode;
    }
    switch(insMode) {
    case InsertionMode.Initial:{
      if (token == 0x09 || token == 0x0a ||
          token == 0x0c || token == 0x0d || token == 0x20) {
 return false;
}
      if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        DocTypeToken doctype=(DocTypeToken)getToken(token);
        string doctypeName=(doctype.name==null) ? "" : doctype.name.ToString();
        string doctypePublic=(doctype.publicID == null) ? null :
          doctype.publicID.ToString();
        string doctypeSystem=(doctype.systemID == null) ? null :
          doctype.systemID.ToString();
        bool matchesHtml="html".Equals(doctypeName);
        bool hasSystemId=(doctype.systemID != null);
        if (!matchesHtml || doctypePublic != null ||
        (doctypeSystem!=null && !"about:legacy-compat"
              .Equals(doctypeSystem))) {
 bool html4=(matchesHtml && "-//W3C//DTD HTML 4.0//EN"
            .Equals(doctypePublic) &&
              (doctypeSystem == null ||
                "http://www.w3.org/TR/REC-html40/strict.dtd"
                .Equals(doctypeSystem)));
          bool html401=(matchesHtml && "-//W3C//DTD HTML 4.01//EN"
            .Equals(doctypePublic) &&
              (doctypeSystem == null ||
                "http://www.w3.org/TR/html4/strict.dtd"
                .Equals(doctypeSystem)));
          bool xhtml=(matchesHtml && "-//W3C//DTD XHTML 1.0 Strict//EN"
            .Equals(doctypePublic) &&
  ("http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"
                .Equals(doctypeSystem)));
          bool xhtml11=(matchesHtml && "-//W3C//DTD XHTML 1.1//EN"
            .Equals(doctypePublic) &&
       ("http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd"
                .Equals(doctypeSystem)));
          if (!html4 && !html401 && !xhtml && !xhtml11) {
            error = true;
          }
        }
        if (doctypePublic == null) {
          doctypePublic="";
        }
        if (doctypeSystem == null) {
          doctypeSystem="";
        }
        DocumentType doctypeNode = new DocumentType();
        doctypeNode.name = doctypeName;
        doctypeNode.publicId = doctypePublic;
        doctypeNode.systemId = doctypeSystem;
        document.doctype = doctypeNode;
        document.appendChild(doctypeNode);
        string doctypePublicLC = null;
        if (!"about:srcdoc".Equals(document.address)) {
          if (!matchesHtml||doctype.forceQuirks) {
          document.setMode(DocumentMode.QuirksMode);
        } else {
          doctypePublicLC = StringUtility.toLowerCaseAscii(doctypePublic);
          if ("html".Equals(doctypePublicLC) ||
              "-//w3o//dtd w3 html strict 3.0//en//".Equals(doctypePublicLC) ||
              "-/w3c/dtd html 4.0 transitional/en".Equals(doctypePublicLC)
) {
            document.setMode(DocumentMode.QuirksMode);
  } else if (doctypePublic.Length>0) {
            foreach (var id in quirksModePublicIdPrefixes) {
              if (doctypePublicLC.StartsWith(id, StringComparison.Ordinal)) {
                document.setMode(DocumentMode.QuirksMode);
                break;
              }
            }
          }
        }
        if (document.getMode() != DocumentMode.QuirksMode) {
          if (doctypePublicLC == null) {
            doctypePublicLC = StringUtility.toLowerCaseAscii(doctypePublic);
          }
        if ("http://www.ibm.com/data/dtd/v11/ibmxhtml1-transitional.dtd"
            .Equals(
              StringUtility.toLowerCaseAscii(doctypeSystem)) ||
              (!hasSystemId &&
  doctypePublicLC.StartsWith("-//w3c//dtd html 4.01 frameset//",
 StringComparison.Ordinal)) ||
              (!hasSystemId &&
  doctypePublicLC.StartsWith("-//w3c//dtd html 4.01 transitional//",
 StringComparison.Ordinal))) {
            document.setMode(DocumentMode.QuirksMode);
          }
        }
        if (document.getMode() != DocumentMode.QuirksMode) {
          if (doctypePublicLC == null) {
            doctypePublicLC = StringUtility.toLowerCaseAscii(doctypePublic);
          }
          if (doctypePublicLC.StartsWith("-//w3c//dtd xhtml 1.0 frameset//",
 StringComparison.Ordinal) ||
  doctypePublicLC.StartsWith("-//w3c//dtd xhtml 1.0 transitional//",
 StringComparison.Ordinal) ||
              (hasSystemId &&
  doctypePublicLC.StartsWith("-//w3c//dtd html 4.01 frameset//",
 StringComparison.Ordinal)) ||
              (hasSystemId &&
  doctypePublicLC.StartsWith("-//w3c//dtd html 4.01 transitional//",
 StringComparison.Ordinal))) {
            document.setMode(DocumentMode.LimitedQuirksMode);
          }
        }
      }
        insertionMode = InsertionMode.BeforeHtml;
        return true;
      }
      if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToDocument(token);

        return true;
      }
      if (!"about:srcdoc".Equals(document.address)) {
        error = true;
        document.setMode(DocumentMode.QuirksMode);
      }
      insertionMode = InsertionMode.BeforeHtml;
      return applyInsertionMode(token, null);
    }
    case InsertionMode.BeforeHtml:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        error = true;
        return false;
      }
      if (token == 0x09 || token == 0x0a ||
          token == 0x0c || token == 0x0d || token == 0x20) {
 return false;
}
      if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToDocument(token);

        return true;
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if ("html".Equals(name)) {
          addHtmlElement(tag);
          insertionMode = InsertionMode.BeforeHead;
          return true;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        TagToken tag=(TagToken)getToken(token);
        string name = tag.getName();
        if (!"html".Equals(name) && !"br".Equals(name) &&
            !"head".Equals(name) && !"body".Equals(name)) {
          error = true;
          return false;
        }
      }
      Element element = new Element();
      element.setLocalName("html");
      element.setNamespace(HTML_NAMESPACE);
      document.appendChild(element);
      openElements.Add(element);
      insertionMode = InsertionMode.BeforeHead;
      return applyInsertionMode(token, null);
    }
    case InsertionMode.BeforeHead:{
      if (token == 0x09 || token == 0x0a ||
          token == 0x0c || token == 0x0d || token == 0x20) {
 return false;
}
      if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToCurrentNode(token);
        return true;
      }
      if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        error = true;
        return false;
      }
      if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if ("html".Equals(name)) {
          applyInsertionMode(token, InsertionMode.InBody);
          return true;
        } else if ("head".Equals(name)) {
          Element element = addHtmlElement(tag);
          headElement = element;
          insertionMode = InsertionMode.InHead;
          return true;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        TagToken tag=(TagToken)getToken(token);
        string name = tag.getName();
        if ("head".Equals(name) || "br".Equals(name) ||
            "body".Equals(name) || "html".Equals(name)) {
          applyStartTag("head",insMode);
          return applyInsertionMode(token, null);
        } else {
          error = true;
          return false;
        }
      }
      applyStartTag("head",insMode);
      return applyInsertionMode(token, null);
    }
    case InsertionMode.InHead:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToCurrentNode(token);
        return true;
      }
      if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        error = true;
        return false;
      }
      if (token == 0x09 || token == 0x0a ||
          token == 0x0c || token == 0x0d || token == 0x20) {
        insertCharacter(getCurrentNode(), token);
        return true;
      }
      if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if ("html".Equals(name)) {
          applyInsertionMode(token, InsertionMode.InBody);
          return true;
        } else if ("base".Equals(name)||
            "bgsound".Equals(name)|| "basefont".Equals(name)||
            "link".Equals(name)) {
          Element e = addHtmlElementNoPush(tag);
          if (baseurl==null && "base".Equals(name)) {
            // Get the document _base URL
            baseurl=e.getAttribute("href");
          }
          tag.ackSelfClosing();
          return true;
        } else if ("meta".Equals(name)) {
          Element element = addHtmlElementNoPush(tag);
          tag.ackSelfClosing();
          if (encoding.getConfidence() == EncodingConfidence.Tentative) {
            string charset=element.getAttribute("charset");
            if (charset != null) {
              charset = TextEncoding.resolveEncoding(charset);
              if (TextEncoding.isAsciiCompatible(charset) ||
                "utf-16be".Equals(charset) || "utf-16le".Equals(charset)) {
                changeEncoding(charset);
                if (encoding.getConfidence() == EncodingConfidence.Certain) {
                  inputStream.disableBuffer();
                }
                return true;
              }
            }
            string value = StringUtility.toLowerCaseAscii(
                element.getAttribute("http-equiv"));
            if ("content-type".Equals(value)) {
              value=element.getAttribute("content");
              if (value != null) {
                value = StringUtility.toLowerCaseAscii(value);
                charset = CharsetSniffer.extractCharsetFromMeta(value);
                if (TextEncoding.isAsciiCompatible(charset) ||
                "utf-16be".Equals(charset) || "utf-16le".Equals(charset)) {
                  changeEncoding(charset);
                  if (encoding.getConfidence() == EncodingConfidence.Certain) {
                    inputStream.disableBuffer();
                  }
                  return true;
                }
              }
            } else if ("content-language".Equals(value)) {
              // HTML5 requires us to use this algorithm
              // to parse the Content-Language, rather than
              // use HTTP parsing (with HeaderParser.getLanguages)
              // NOTE: this pragma is non-conforming
              value=element.getAttribute("content");
              if (!String.IsNullOrEmpty(value) &&
                  value.IndexOf(',')< 0) {
                string[] data = StringUtility.splitAtSpaces(value);
                string deflang=(data.Length==0) ? "" : data[0];
                if (!String.IsNullOrEmpty(deflang)) {
                  document.defaultLanguage = deflang;
                }
              }
            }
          }
          if (encoding.getConfidence() == EncodingConfidence.Certain) {
            inputStream.disableBuffer();
          }
          return true;
        } else if ("title".Equals(name)) {
          addHtmlElement(tag);
          state = TokenizerState.RcData;
          originalInsertionMode = insertionMode;
          insertionMode = InsertionMode.Text;
          return true;
        } else if ("noframes".Equals(name) ||
            "style".Equals(name)) {
          addHtmlElement(tag);
          state = TokenizerState.RawText;
          originalInsertionMode = insertionMode;
          insertionMode = InsertionMode.Text;
          return true;
        } else if ("noscript".Equals(name)) {
          addHtmlElement(tag);
          insertionMode = InsertionMode.InHeadNoscript;
          return true;
        } else if ("script".Equals(name)) {
          addHtmlElement(tag);
          state = TokenizerState.ScriptData;
          originalInsertionMode = insertionMode;
          insertionMode = InsertionMode.Text;
          return true;
        } else if ("head".Equals(name)) {
          error = true;
          return false;
        } else {
          applyEndTag("head",insMode);
          return applyInsertionMode(token, null);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        TagToken tag=(TagToken)getToken(token);
        string name = tag.getName();
        if ("head".Equals(name)) {
          openElements.RemoveAt(openElements.Count-1);
          insertionMode = InsertionMode.AfterHead;
          return true;
        } else if (!(
            "br".Equals(name) ||
            "body".Equals(name) || "html".Equals(name))) {
          error = true;
          return false;
        }
        applyEndTag("head",insMode);
        return applyInsertionMode(token, null);
      } else {
        applyEndTag("head",insMode);
        return applyInsertionMode(token, null);
      }
    }
    case InsertionMode.AfterHead:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        if (token == 0x20 || token == 0x09 || token == 0x0a ||
            token == 0x0c || token == 0x0d) {
          insertCharacter(getCurrentNode(), token);
        } else {
          applyStartTag("body",insMode);
          framesetOk = true;
          return applyInsertionMode(token, null);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        error = true;
        return false;
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("html")) {
          applyInsertionMode(token, InsertionMode.InBody);
          return true;
        } else if (name.Equals("body")) {
          addHtmlElement(tag);
          framesetOk = false;
          insertionMode = InsertionMode.InBody;
          return true;
        } else if (name.Equals("frameset")) {
          addHtmlElement(tag);
          insertionMode = InsertionMode.InFrameset;
          return true;
        } else if ("base".Equals(name)|| "bgsound".Equals(name)||
            "basefont".Equals(name)|| "link".Equals(name)||
            "noframes".Equals(name)|| "script".Equals(name)||
            "style".Equals(name)|| "title".Equals(name)||
            "meta".Equals(name)) {
          error = true;
          openElements.Add(headElement);
          applyInsertionMode(token, InsertionMode.InHead);
          openElements.Remove(headElement);
          return true;
        } else if ("head".Equals(name)) {
          error = true;
          return false;
        } else {
          applyStartTag("body",insMode);
          framesetOk = true;
          return applyInsertionMode(token, null);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("body") || name.Equals("html")||
            name.Equals("br")) {
          applyStartTag("body",insMode);
          framesetOk = true;
          return applyInsertionMode(token, null);
        } else {
          error = true;
          return false;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToCurrentNode(token);

        return true;
      } else if (token == TOKEN_EOF) {
        applyStartTag("body",insMode);
        framesetOk = true;
        return applyInsertionMode(token, null);
      }
      return true;
    }
    case InsertionMode.Text:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        if (insMode != insertionMode) {
          insertCharacter(getCurrentNode(), token);
        } else {
          Text textNode = getTextNodeToInsert(getCurrentNode());
          int ch = token;
          if (textNode == null) {
 throw new InvalidOperationException();
}
          while (true) {
            textNode.text.appendInt(ch);
            token = parserRead();
            if ((token&TOKEN_TYPE_MASK) != TOKEN_CHARACTER) {
              tokenQueue.Insert(0, token);
              break;
            }
            ch = token;
          }
        }
        return true;
      } else if (token == TOKEN_EOF) {
        error = true;
        openElements.RemoveAt(openElements.Count-1);
        insertionMode = originalInsertionMode;
        return applyInsertionMode(token, null);
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        openElements.RemoveAt(openElements.Count-1);
        insertionMode = originalInsertionMode;
      }
      return true;
    }
    case InsertionMode.InBody:{
      if (token == 0) {
        error = true;
        return true;
      }
      if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToCurrentNode(token);

        return true;
      }
      if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        error = true;
        return true;
      }
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        //Console.WriteLine("%c %s",token,getCurrentNode().getTagName());
        reconstructFormatting();
        Text textNode = getTextNodeToInsert(getCurrentNode());
        int ch = token;
        if (textNode == null) {
 throw new InvalidOperationException();
}
        while (true) {
          // Read multiple characters at once
          if (ch == 0) {
            error = true;
          } else {
            textNode.text.appendInt(ch);
          }
          if (framesetOk && token != 0x20 && token != 0x09 &&
              token != 0x0a && token != 0x0c && token != 0x0d) {
            framesetOk = false;
          }
          // If we're only processing under a different
          // insertion mode then break
          if (insMode != insertionMode) {
            break;
          }
          token = parserRead();
          if ((token&TOKEN_TYPE_MASK) != TOKEN_CHARACTER) {
            tokenQueue.Insert(0, token);
            break;
          }
          ch = token;
        }
        return true;
      } else if (token == TOKEN_EOF) {
        foreach (var e in openElements) {
          string name = e.getLocalName();
          if (!"dd".Equals(name) &&
              !"dt".Equals(name) && !"li".Equals(name) &&
              !"p".Equals(name) && !"tbody".Equals(name) &&
              !"td".Equals(name) && !"tfoot".Equals(name) &&
              !"th".Equals(name) && !"tr".Equals(name) &&
              !"thead".Equals(name) && !"body".Equals(name) &&
              !"html".Equals(name)) {
            error = true;
          }
        }
        stopParsing();
      }
      if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        //
        // START TAGS
        //
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if ("html".Equals(name)) {
          error = true;
          openElements[0].mergeAttributes(tag);
          return true;
        } else if ("base".Equals(name)||
            "bgsound".Equals(name)|| "basefont".Equals(name)||
            "link".Equals(name)|| "noframes".Equals(name)||
            "script".Equals(name)|| "style".Equals(name)||
            "title".Equals(name)|| "meta".Equals(name)) {
          applyInsertionMode(token, InsertionMode.InHead);
          return true;
        } else if ("body".Equals(name)) {
          error = true;
          if (openElements.Count <= 1 ||
              !openElements[1].isHtmlElement("body")) {
 return false;
}
          framesetOk = false;
          openElements[1].mergeAttributes(tag);
          return true;
        } else if ("frameset".Equals(name)) {
          error = true;
          if (!framesetOk ||
              openElements.Count<= 1 || !openElements[1].isHtmlElement("body")) {
 return false;
}
          Node parent=(Node) openElements[1].getParentNode();
          if (parent != null) {
            parent.removeChild(openElements[1]);
          }
          while (openElements.Count>1) {
            popCurrentNode();
          }
          addHtmlElement(tag);
          insertionMode = InsertionMode.InFrameset;
          return true;
        } else if ("address".Equals(name) ||
            "article".Equals(name) || "aside".Equals(name) ||
            "blockquote".Equals(name) || "center".Equals(name) ||
            "details".Equals(name) || "dialog".Equals(name) ||
            "dir".Equals(name) || "div".Equals(name) ||
            "dl".Equals(name) || "fieldset".Equals(name) ||
            "figcaption".Equals(name) || "figure".Equals(name) ||
            "footer".Equals(name) || "header".Equals(name) ||
            "hgroup".Equals(name) || "menu".Equals(name) ||
            "nav".Equals(name) || "ol".Equals(name) ||
            "p".Equals(name) || "section".Equals(name) ||
            "summary".Equals(name) || "ul".Equals(name)
) {
          closeParagraph(insMode);
          addHtmlElement(tag);
          return true;
        } else if ("h1".Equals(name) || "h2".Equals(name) ||
            "h3".Equals(name) || "h4".Equals(name) ||
            "h5".Equals(name) || "h6".Equals(name)
) {
          closeParagraph(insMode);
          Element node = getCurrentNode();
          string name1 = node.getLocalName();
          if ("h1".Equals(name1) || "h2".Equals(name1) ||
              "h3".Equals(name1) || "h4".Equals(name1) ||
              "h5".Equals(name1) || "h6".Equals(name1)
) {
            error = true;
            openElements.RemoveAt(openElements.Count-1);
          }
          addHtmlElement(tag);
          return true;
        } else if ("pre".Equals(name)||
            "listing".Equals(name)) {
          closeParagraph(insMode);
          addHtmlElement(tag);
          skipLineFeed();
          framesetOk = false;
          return true;
        } else if ("form".Equals(name)) {
          if (formElement != null) {
            error = true;
            return true;
          }
          closeParagraph(insMode);
          formElement = addHtmlElement(tag);
          return true;
        } else if ("li".Equals(name)) {
          framesetOk = false;
          for (int i = openElements.Count-1;i >= 0; --i) {
            Element node = openElements[i];
            string nodeName = node.getLocalName();
            if (nodeName.Equals("li")) {
              applyInsertionMode(
                  getArtificialToken(TOKEN_END_TAG,"li"),
                  insMode);
              break;
            }
            if (isSpecialElement(node) && !"address".Equals(nodeName) &&
                !"div".Equals(nodeName) && !"p".Equals(nodeName)) {
              break;
            }
          }
          closeParagraph(insMode);
          addHtmlElement(tag);
          return true;
        } else if ("dd".Equals(name) || "dt".Equals(name)) {
          framesetOk = false;
          for (int i = openElements.Count-1;i >= 0; --i) {
            Element node = openElements[i];
            string nodeName = node.getLocalName();
            //Console.WriteLine("looping through %s",nodeName);
            if (nodeName.Equals("dd") || nodeName.Equals("dt")) {
              applyEndTag(nodeName, insMode);
              break;
            }
            if (isSpecialElement(node) &&
                !"address".Equals(nodeName) && !"div".Equals(nodeName) &&
                !"p".Equals(nodeName)) {
              break;
            }
          }
          closeParagraph(insMode);
          addHtmlElement(tag);
          return true;
        } else if ("plaintext".Equals(name)) {
          closeParagraph(insMode);
          addHtmlElement(tag);
          state = TokenizerState.PlainText;
          return true;
        } else if ("button".Equals(name)) {
          if (hasHtmlElementInScope("button")) {
            error = true;
            applyEndTag("button",insMode);
            return applyInsertionMode(token, null);
          }
          reconstructFormatting();
          addHtmlElement(tag);
          framesetOk = false;
          return true;
        } else if ("a".Equals(name)) {
          while (true) {
            Element node = null;
            for (int i = formattingElements.Count-1; i >= 0; --i) {
              FormattingElement fe = formattingElements[i];
              if (fe.isMarker()) {
                break;
              }
              if (fe.element.getLocalName().Equals("a")) {
                node = fe.element;
                break;
              }
            }
            if (node != null) {
              error = true;
              applyEndTag("a",insMode);
              removeFormattingElement(node);
              openElements.Remove(node);
            } else {
              break;
            }
          }
          reconstructFormatting();
          pushFormattingElement(tag);
        } else if ("b".Equals(name) ||
            "big".Equals(name)|| "code".Equals(name)||
            "em".Equals(name)|| "font".Equals(name)||
            "i".Equals(name)|| "s".Equals(name)||
            "small".Equals(name)|| "strike".Equals(name)||
            "strong".Equals(name)|| "tt".Equals(name)||
            "u".Equals(name)) {
          reconstructFormatting();
          pushFormattingElement(tag);
        } else if ("nobr".Equals(name)) {
          reconstructFormatting();
          if (hasHtmlElementInScope("nobr")) {
            error = true;
            applyEndTag("nobr",insMode);
            reconstructFormatting();
          }
          pushFormattingElement(tag);
        } else if ("table".Equals(name)) {
          if (document.getMode() != DocumentMode.QuirksMode) {
            closeParagraph(insMode);
          }
          addHtmlElement(tag);
          framesetOk = false;
          insertionMode = InsertionMode.InTable;
          return true;
        } else if ("area".Equals(name)|| "br".Equals(name)||
            "embed".Equals(name)|| "img".Equals(name)||
            "keygen".Equals(name)|| "wbr".Equals(name)
) {
          reconstructFormatting();
          addHtmlElementNoPush(tag);
          tag.ackSelfClosing();
          framesetOk = false;
        } else if ("input".Equals(name)) {
          reconstructFormatting();
          inputElement = addHtmlElementNoPush(tag);
          tag.ackSelfClosing();
          string attr=inputElement.getAttribute("type");
      if (attr==null || !"hidden"
            .Equals(StringUtility.toLowerCaseAscii(attr))) {
            framesetOk = false;
          }
        } else if ("param".Equals(name)|| "source".Equals(name)||
            "menuitem".Equals(name)|| "track".Equals(name)
) {
          addHtmlElementNoPush(tag);
          tag.ackSelfClosing();
        } else if ("hr".Equals(name)) {
          closeParagraph(insMode);
          addHtmlElementNoPush(tag);
          tag.ackSelfClosing();
          framesetOk = false;
        } else if ("image".Equals(name)) {
          error = true;
          tag.setName("img");
          return applyInsertionMode(token, null);
        } else if ("isindex".Equals(name)) {
          error = true;
          if (formElement != null) {
 return false;
}
          tag.ackSelfClosing();
          applyStartTag("form",insMode);
          string action=tag.getAttribute("action");
          if (action != null) {
            formElement.setAttribute("action",action);
          }
          applyStartTag("hr",insMode);
          applyStartTag("label",insMode);
          StartTagToken isindex=new StartTagToken("input");
          foreach (var attr in tag.getAttributes()) {
            string attrname = attr.getName();
            if (!"name".Equals(attrname) &&
                !"action".Equals(attrname) && !"prompt".Equals(attrname)) {
              isindex.setAttribute(attrname, attr.getValue());
            }
          }
          string prompt=tag.getAttribute("prompt");
          // NOTE: Because of the inserted hr elements,
          // the frameset-ok flag should have been set
          // to not-ok already, so we don't need to check
          // for whitespace here
          if (prompt != null) {
            reconstructFormatting();
            insertString(getCurrentNode(), prompt);
          } else {
            reconstructFormatting();
            insertString(getCurrentNode(),"Enter search keywords:");
          }
          int isindexToken = tokens.Count|isindex.getType();
          tokens.Add(isindex);
          applyInsertionMode(isindexToken, insMode);
          inputElement.setAttribute("name","isindex");
          applyEndTag("label",insMode);
          applyStartTag("hr",insMode);
          applyEndTag("form",insMode);
        } else if ("textarea".Equals(name)) {
          addHtmlElement(tag);
          skipLineFeed();
          state = TokenizerState.RcData;
          originalInsertionMode = insertionMode;
          framesetOk = false;
          insertionMode = InsertionMode.Text;
        } else if ("xmp".Equals(name)) {
          closeParagraph(insMode);
          reconstructFormatting();
          framesetOk = false;
          addHtmlElement(tag);
          state = TokenizerState.RawText;
          originalInsertionMode = insertionMode;
          insertionMode = InsertionMode.Text;
        } else if ("iframe".Equals(name)) {
          framesetOk = false;
          addHtmlElement(tag);
          state = TokenizerState.RawText;
          originalInsertionMode = insertionMode;
          insertionMode = InsertionMode.Text;
        } else if ("noembed".Equals(name)) {
          addHtmlElement(tag);
          state = TokenizerState.RawText;
          originalInsertionMode = insertionMode;
          insertionMode = InsertionMode.Text;
        } else if ("select".Equals(name)) {
          reconstructFormatting();
          addHtmlElement(tag);
          framesetOk = false;
          if (insertionMode == InsertionMode.InTable ||
              insertionMode == InsertionMode.InCaption ||
              insertionMode == InsertionMode.InTableBody ||
              insertionMode == InsertionMode.InRow ||
              insertionMode == InsertionMode.InCell) {
            insertionMode = InsertionMode.InSelectInTable;
          } else {
            insertionMode = InsertionMode.InSelect;
          }
        } else if ("option".Equals(name) || "optgroup".Equals(name)) {
          if (getCurrentNode().getLocalName().Equals("option")) {
            applyEndTag("option",insMode);
          }
          reconstructFormatting();
          addHtmlElement(tag);
        } else if ("rp".Equals(name) || "rt".Equals(name)) {
          if (hasHtmlElementInScope("ruby")) {
            generateImpliedEndTags();
            if (!getCurrentNode().getLocalName().Equals("ruby")) {
              error = true;
            }
          }
          addHtmlElement(tag);
        } else if ("applet".Equals(name) || "marquee".Equals(name) ||
            "object".Equals(name)) {
          reconstructFormatting();
          Element e = addHtmlElement(tag);
          insertFormattingMarker(tag, e);
          framesetOk = false;
        } else if ("math".Equals(name)) {
          reconstructFormatting();
          adjustMathMLAttributes(tag);
          adjustForeignAttributes(tag);
          insertForeignElement(tag, MATHML_NAMESPACE);
          if (tag.isSelfClosing()) {
            tag.ackSelfClosing();
            popCurrentNode();
          } else {
            hasForeignContent = true;
          }
        } else if ("svg".Equals(name)) {
          reconstructFormatting();
          adjustSvgAttributes(tag);
          adjustForeignAttributes(tag);
          insertForeignElement(tag, SVG_NAMESPACE);
          if (tag.isSelfClosing()) {
            tag.ackSelfClosing();
            popCurrentNode();
          } else {
            hasForeignContent = true;
          }
        } else if ("caption".Equals(name) ||
            "col".Equals(name) || "colgroup".Equals(name) ||
            "frame".Equals(name) || "head".Equals(name) ||
            "tbody".Equals(name) || "td".Equals(name) ||
            "tfoot".Equals(name) || "th".Equals(name) ||
            "thead".Equals(name) || "tr".Equals(name)
) {
          error = true;
          return false;
        } else {
          //Console.WriteLine("ordinary: %s",tag);
          reconstructFormatting();
          addHtmlElement(tag);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        //
        // END TAGS
        // NOTE: Have all cases
        //
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("body")) {
          if (!hasHtmlElementInScope("body")) {
            error = true;
            return false;
          }
          foreach (var e in openElements) {
            string name2 = e.getLocalName();
            if (!"dd".Equals(name2) && !"dt".Equals(name2) &&
                !"li".Equals(name2) && !"option".Equals(name2) &&
                !"optgroup".Equals(name2) && !"p".Equals(name2) &&
                !"rb".Equals(name2) && !"tbody".Equals(name2) &&
                !"td".Equals(name2) && !"tfoot".Equals(name2) &&
                !"th".Equals(name2) && !"tr".Equals(name2) &&
                !"thead".Equals(name2) && !"body".Equals(name2) &&
                !"html".Equals(name2)) {
              error = true;
              // token not ignored here
            }
          }
          insertionMode = InsertionMode.AfterBody;
        } else if (name.Equals("a") ||
            name.Equals("b") || name.Equals("big") ||
            name.Equals("code") || name.Equals("em") ||
            name.Equals("b") || name.Equals("font") ||
            name.Equals("i") || name.Equals("nobr") ||
            name.Equals("s") || name.Equals("small") ||
            name.Equals("strike") || name.Equals("strong") ||
            name.Equals("tt") || name.Equals("u")
) {
          for (int i = 0; i < 8; ++i) {
            FormattingElement formatting = null;
            for (int j = formattingElements.Count-1; j >= 0; --j) {
              FormattingElement fe = formattingElements[j];
              if (fe.isMarker()) {
                break;
              }
              if (fe.element.getLocalName().Equals(name)) {
                formatting = fe;
                break;
              }
            }
            if (formatting == null) {
              // NOTE: Steps for "any other end tag"
              //  Console.WriteLine("no such formatting element");
              for (int i1 = openElements.Count-1;i1 >= 0; --i1) {
                Element node = openElements[i1];
                if (name.Equals(node.getLocalName())) {
                  generateImpliedEndTagsExcept(name);
                  if (!name.Equals(getCurrentNode().getLocalName())) {
                    error = true;
                  }
                  while (true) {
                    Element node2 = popCurrentNode();
                    if (node2.Equals(node)) {
                    break;
                    }
                  }
                  break;
                } else if (isSpecialElement(node)) {
                  error = true;
                  return false;
                }
              }
              break;
            }
            int formattingElementPos = openElements.IndexOf(formatting.element);
            if (formattingElementPos< 0) {  // not found
              error = true;
              //  Console.WriteLine("Not in stack of open elements");
              formattingElements.Remove(formatting);
              break;
            }
            //  Console.WriteLine("Open elements[%s]:",i);
            // Console.WriteLine(openElements);
            //  Console.WriteLine("Formatting elements:");
            // Console.WriteLine(formattingElements);
            if (!hasHtmlElementInScope(formatting.element)) {
              error = true;
              return false;
            }
            if (!formatting.element.Equals(getCurrentNode())) {
              error = true;
            }
            Element furthestBlock = null;
            int furthestBlockPos=-1;
            for (int j = openElements.Count-1;j>formattingElementPos; --j) {
              Element e = openElements[j];
              if (isSpecialElement(e)) {
                furthestBlock = e;
                furthestBlockPos = j;
              }
            }
            //  Console.WriteLine("furthest block: %s",furthestBlock);
            if (furthestBlock == null) {
              // Pop up to and including the
              // formatting element
              while (openElements.Count>formattingElementPos) {
                popCurrentNode();
              }
              formattingElements.Remove(formatting);
              //Console.WriteLine("Open elements now [%s]:",i);
              //Console.WriteLine(openElements);
              //Console.WriteLine("Formatting elements now:");
              //Console.WriteLine(formattingElements);
              break;
            }
            Element commonAncestor = openElements[formattingElementPos-1];
            //  Console.WriteLine("common ancestor: %s",commonAncestor);
            int bookmark = formattingElements.IndexOf(formatting);
            //  Console.WriteLine("bookmark=%d",bookmark);
            Element myNode = furthestBlock;
            Element superiorNode = openElements[furthestBlockPos-1];
            Element lastNode = furthestBlock;
            for (int j = 0; j < 3; ++j) {
              myNode = superiorNode;
              FormattingElement nodeFE = getFormattingElement(myNode);
              if (nodeFE == null) {
                //  Console.WriteLine("node not a formatting element");
                superiorNode = openElements[openElements.IndexOf(myNode)-1];
                openElements.Remove(myNode);
                continue;
              } else if (myNode.Equals(formatting.element)) {
                //  Console.WriteLine("node is the formatting element");
                break;
              }
              Element e = Element.fromToken(nodeFE.token);
              nodeFE.element = e;
              int io = openElements.IndexOf(myNode);
              superiorNode = openElements[io-1];
              openElements[io]=e;
              myNode = e;
              if (lastNode.Equals(furthestBlock)) {
                bookmark = formattingElements.IndexOf(nodeFE)+1;
              }
              // NOTE: Because 'node' can only be a formatting
              // element, the foster parenting rule doesn't
              // apply here
              if (lastNode.getParentNode() != null) {
                ((Node) lastNode.getParentNode()).removeChild(lastNode);
              }
              myNode.appendChild(lastNode);
              lastNode = myNode;
            }
            //  Console.WriteLine("node: %s",node);
            //  Console.WriteLine("lastNode: %s",lastNode);
            if (commonAncestor.getLocalName().Equals("table") ||
                commonAncestor.getLocalName().Equals("tr") ||
                commonAncestor.getLocalName().Equals("tbody") ||
                commonAncestor.getLocalName().Equals("thead") ||
                commonAncestor.getLocalName().Equals("tfoot")
) {
              if (lastNode.getParentNode() != null) {
                ((Node) lastNode.getParentNode()).removeChild(lastNode);
              }
              fosterParent(lastNode);
            } else {
              if (lastNode.getParentNode() != null) {
                ((Node) lastNode.getParentNode()).removeChild(lastNode);
              }
              commonAncestor.appendChild(lastNode);
            }
            Element e2 = Element.fromToken(formatting.token);
   foreach (var child in new
              List<Node>(furthestBlock.getChildNodesInternal())) {
              furthestBlock.removeChild(child);
              // NOTE: Because 'e' can only be a formatting
              // element, the foster parenting rule doesn't
              // apply here
              e2.appendChild(child);
            }
            // NOTE: Because intervening elements, including
            // formatting elements, are cleared between table
            // and tbody/thead/tfoot and between those three
            // elements and tr, the foster parenting rule
            // doesn't apply here
            furthestBlock.appendChild(e2);
            FormattingElement newFE = new FormattingElement();
            newFE.marker = false;
            newFE.element = e2;
            newFE.token = formatting.token;
            //  Console.WriteLine("Adding formatting element at %d",bookmark);
            formattingElements.Insert(bookmark, newFE);
            formattingElements.Remove(formatting);
            // Console.WriteLine("Replacing open element at %d"
            // , openElements.IndexOf(furthestBlock)+1);
            int idx = openElements.IndexOf(furthestBlock)+1;
            openElements.Insert(idx, e2);
            openElements.Remove(formatting.element);
          }
        } else if ("applet".Equals(name) ||
            "marquee".Equals(name) || "object".Equals(name)) {
          if (!hasHtmlElementInScope(name)) {
            error = true;
            return false;
          } else {
            generateImpliedEndTags();
            if (!getCurrentNode().getLocalName().Equals(name)) {
              error = true;
            }
            while (true) {
              Element node = popCurrentNode();
              if (node.getLocalName().Equals(name)) {
                break;
              }
            }
            clearFormattingToMarker();
          }
        } else if (name.Equals("html")) {
          return (applyEndTag("body",insMode)) ?
            (applyInsertionMode(token, null)) : (false);
        } else if ("address".Equals(name) ||
            "article".Equals(name) || "aside".Equals(name) ||
            "blockquote".Equals(name) || "button".Equals(name) ||
            "center".Equals(name) || "details".Equals(name) ||
            "dialog".Equals(name) || "dir".Equals(name) ||
            "div".Equals(name) || "dl".Equals(name) ||
            "fieldset".Equals(name) || "figcaption".Equals(name) ||
            "figure".Equals(name) || "footer".Equals(name) ||
            "header".Equals(name) || "hgroup".Equals(name) ||
            "listing".Equals(name) || "main".Equals(name) ||
            "menu".Equals(name) || "nav".Equals(name) ||
            "ol".Equals(name) || "pre".Equals(name) ||
            "section".Equals(name) || "summary".Equals(name) ||
            "ul".Equals(name)) {
          if (!hasHtmlElementInScope(name)) {
            error = true;
            return true;
          } else {
            generateImpliedEndTags();
            if (!getCurrentNode().getLocalName().Equals(name)) {
              error = true;
            }
            while (true) {
              Element node = popCurrentNode();
              if (node.getLocalName().Equals(name)) {
                break;
              }
            }
          }
        } else if (name.Equals("form")) {
          Element node = formElement;
          formElement = null;
          if (node == null || hasHtmlElementInScope(node)) {
            error = true;
            return true;
          }
          generateImpliedEndTags();
          if (getCurrentNode() != node) {
            error = true;
          }
          openElements.Remove(node);
        } else if (name.Equals("p")) {
          if (!hasHtmlElementInButtonScope(name)) {
            error = true;
            applyStartTag("p",insMode);
            return applyInsertionMode(token, null);
          }
          generateImpliedEndTagsExcept(name);
          if (!getCurrentNode().getLocalName().Equals(name)) {
            error = true;
          }
          while (true) {
            Element node = popCurrentNode();
            if (node.getLocalName().Equals(name)) {
              break;
            }
          }
        } else if (name.Equals("li")) {
          if (!hasHtmlElementInListItemScope(name)) {
            error = true;
            return false;
          }
          generateImpliedEndTagsExcept(name);
          if (!getCurrentNode().getLocalName().Equals(name)) {
            error = true;
          }
          while (true) {
            Element node = popCurrentNode();
            if (node.getLocalName().Equals(name)) {
              break;
            }
          }
        } else if (name.Equals("h1") || name.Equals("h2") ||
            name.Equals("h3") || name.Equals("h4") ||
            name.Equals("h5") || name.Equals("h6")) {
          if (!hasHtmlHeaderElementInScope()) {
            error = true;
            return false;
          }
          generateImpliedEndTags();
          if (!getCurrentNode().getLocalName().Equals(name)) {
            error = true;
          }
          while (true) {
            Element node = popCurrentNode();
            string name2 = node.getLocalName();
            if (name2.Equals("h1") ||
                name2.Equals("h2") || name2.Equals("h3") ||
                name2.Equals("h4") || name2.Equals("h5") ||
                name2.Equals("h6")) {
              break;
            }
          }
          return true;
        } else if (name.Equals("dd") || name.Equals("dt")) {
          if (!hasHtmlElementInScope(name)) {
            error = true;
            return false;
          }
          generateImpliedEndTagsExcept(name);
          if (!getCurrentNode().getLocalName().Equals(name)) {
            error = true;
          }
          while (true) {
            Element node = popCurrentNode();
            if (node.getLocalName().Equals(name)) {
              break;
            }
          }
        } else if ("br".Equals(name)) {
          error = true;
          applyStartTag("br",insMode);
          return false;
        } else {
          for (int i = openElements.Count-1;i >= 0; --i) {
            Element node = openElements[i];
            if (name.Equals(node.getLocalName())) {
              generateImpliedEndTagsExcept(name);
              if (!name.Equals(getCurrentNode().getLocalName())) {
                error = true;
              }
              while (true) {
                Element node2 = popCurrentNode();
                if (node2.Equals(node)) {
                  break;
                }
              }
              break;
            } else if (isSpecialElement(node)) {
              error = true;
              return false;
            }
          }
        }
      }
      return true;
    }
    case InsertionMode.InHeadNoscript:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        if (token == 0x09 || token == 0x0a || token == 0x0c ||
            token == 0x0d || token == 0x20) {
 return applyInsertionMode(token, InsertionMode.InBody);
} else {
          error = true;
          applyEndTag("noscript",insMode);
          return applyInsertionMode(token, null);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        error = true;
        return false;
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("html")) {
 return applyInsertionMode(token, InsertionMode.InBody);
  } else if (name.Equals("basefont") || name.Equals("bgsound") ||
            name.Equals("link") || name.Equals("meta") ||
            name.Equals("noframes") || name.Equals("style")
) {
 return applyInsertionMode(token, InsertionMode.InHead);
  } else if (name.Equals("head") || name.Equals("noscript")) {
          error = true;
          return false;
        } else {
          error = true;
          applyEndTag("noscript",insMode);
          return applyInsertionMode(token, null);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("noscript")) {
          popCurrentNode();
          insertionMode = InsertionMode.InHead;
        } else if (name.Equals("br")) {
          error = true;
          applyEndTag("noscript",insMode);
          return applyInsertionMode(token, null);
        } else {
          error = true;
          return false;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
 return applyInsertionMode(token, InsertionMode.InHead);
  } else if (token == TOKEN_EOF) {
        error = true;
        applyEndTag("noscript",insMode);
        return applyInsertionMode(token, null);
      }
      return true;
    }
    case InsertionMode.InTable:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        Element currentNode = getCurrentNode();
        if (currentNode.getLocalName().Equals("table") ||
            currentNode.getLocalName().Equals("tbody") ||
            currentNode.getLocalName().Equals("tfoot") ||
            currentNode.getLocalName().Equals("thead") ||
            currentNode.getLocalName().Equals("tr")) {
          pendingTableCharacters.clearAll();
          originalInsertionMode = insertionMode;
          insertionMode = InsertionMode.InTableText;
          return applyInsertionMode(token, null);
        } else {
          // NOTE: Foster parenting rules don't apply here, since
          // the current node isn't table, tbody, tfoot, thead, or
          // tr and won't change while In Body is being applied
          error = true;
          return applyInsertionMode(token, InsertionMode.InBody);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        error = true;
        return false;
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("table")) {
          error = true;
          return (applyEndTag("table",insMode)) ?
            (applyInsertionMode(token, null)) : (false);
        } else if (name.Equals("caption")) {
          while (true) {
            Element node = getCurrentNode();
            if (node == null ||
                node.getLocalName().Equals("table") ||
                node.getLocalName().Equals("html")) {
              break;
            }
            popCurrentNode();
          }
          insertFormattingMarker(tag, addHtmlElement(tag));
          insertionMode = InsertionMode.InCaption;
          return true;
        } else if (name.Equals("colgroup")) {
          while (true) {
            Element node = getCurrentNode();
            if (node == null ||
                node.getLocalName().Equals("table") ||
                node.getLocalName().Equals("html")) {
              break;
            }
            popCurrentNode();
          }
          addHtmlElement(tag);
          insertionMode = InsertionMode.InColumnGroup;
          return true;
        } else if (name.Equals("col")) {
          applyStartTag("colgroup",insMode);
          return applyInsertionMode(token, null);
        } else if (name.Equals("tbody") || name.Equals("tfoot") ||
            name.Equals("thead")) {
          while (true) {
            Element node = getCurrentNode();
            if (node == null ||
                node.getLocalName().Equals("table") ||
                node.getLocalName().Equals("html")) {
              break;
            }
            popCurrentNode();
          }
          addHtmlElement(tag);
          insertionMode = InsertionMode.InTableBody;
        } else if (name.Equals("td") || name.Equals("th") ||
            name.Equals("tr")) {
          applyStartTag("tbody",insMode);
          return applyInsertionMode(token, null);
        } else if (name.Equals("style") ||
            name.Equals("script")) {
          applyInsertionMode(token, InsertionMode.InHead);
        } else if (name.Equals("input")) {
          string attr=tag.getAttribute("type");
      if (attr==null || !"hidden"
            .Equals(StringUtility.toLowerCaseAscii(attr))) {
            error = true;
            doFosterParent = true;
            applyInsertionMode(token, InsertionMode.InBody);
            doFosterParent = false;
          } else {
            error = true;
            addHtmlElementNoPush(tag);
            tag.ackSelfClosing();
          }
        } else if (name.Equals("form")) {
          error = true;
          if (formElement != null) {
 return false;
}
          formElement = addHtmlElementNoPush(tag);
        } else {
          error = true;
          doFosterParent = true;
          applyInsertionMode(token, InsertionMode.InBody);
          doFosterParent = false;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("table")) {
          if (!hasHtmlElementInTableScope(name)) {
            error = true;
            return false;
          } else {
            while (true) {
              Element node = popCurrentNode();
              if (node.getLocalName().Equals(name)) {
                break;
              }
            }
            resetInsertionMode();
          }
        } else if (name.Equals("body") || name.Equals("caption") ||
            name.Equals("col") || name.Equals("colgroup") ||
            name.Equals("html") || name.Equals("tbody") ||
            name.Equals("td") || name.Equals("tfoot") ||
            name.Equals("th") || name.Equals("thead") ||
            name.Equals("tr")) {
          error = true;
          return false;
        } else {
          doFosterParent = true;
          applyInsertionMode(token, InsertionMode.InBody);
          doFosterParent = false;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToCurrentNode(token);
        return true;
      } else if (token == TOKEN_EOF) {
 if (getCurrentNode()==null || !getCurrentNode().getLocalName().Equals("html"
)) {
          error = true;
        }
        stopParsing();
      }
      return true;
    }
    case InsertionMode.InTableText:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        if (token == 0) {
          error = true;
          return false;
        } else {
          pendingTableCharacters.appendInt(token);
        }
      } else {
        bool nonspace = false;
        int[] array = pendingTableCharacters.array();
        int size = pendingTableCharacters.Count;
        for (int i = 0; i < size; ++i) {
          int c = array[i];
          if (c != 0x9 && c != 0xa && c != 0xc && c != 0xd && c != 0x20) {
            nonspace = true;
            break;
          }
        }
        if (nonspace) {
          // See 'anything else' for 'in table'
          error = true;
          doFosterParent = true;
          for (int i = 0; i < size; ++i) {
            int c = array[i];
            applyInsertionMode(c, InsertionMode.InBody);
          }
          doFosterParent = false;
        } else {
          insertString(getCurrentNode(), pendingTableCharacters.ToString());
        }
        insertionMode = originalInsertionMode;
        return applyInsertionMode(token, null);
      }
      return true;
    }
    case InsertionMode.InCaption:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("caption") ||
            name.Equals("col") || name.Equals("colgroup") ||
            name.Equals("tbody") || name.Equals("thead") ||
            name.Equals("td") || name.Equals("tfoot") ||
            name.Equals("th") || name.Equals("tr")
) {
          error = true;
          if (applyEndTag("caption",insMode)) {
 return applyInsertionMode(token, null);
}
        } else {
 return applyInsertionMode(token, InsertionMode.InBody);
}
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("caption")) {
          if (!hasHtmlElementInScope(name)) {
            error = true;
            return false;
          }
          generateImpliedEndTags();
          if (!getCurrentNode().getLocalName().Equals("caption")) {
            error = true;
          }
          while (true) {
            Element node = popCurrentNode();
            if (node.getLocalName().Equals("caption")) {
              break;
            }
          }
          clearFormattingToMarker();
          insertionMode = InsertionMode.InTable;
        } else if (name.Equals("table")) {
          error = true;
          if (applyEndTag("caption",insMode)) {
 return applyInsertionMode(token, null);
}
        } else if (name.Equals("body") ||
            name.Equals("col") || name.Equals("colgroup") ||
            name.Equals("tbody") || name.Equals("thead") ||
            name.Equals("td") || name.Equals("tfoot") ||
            name.Equals("th") || name.Equals("tr") ||
            name.Equals("html")) {
          error = true;
          return false;
        } else {
 return applyInsertionMode(token, InsertionMode.InBody);
}
      } else {
 return applyInsertionMode(token, InsertionMode.InBody);
}
      return true;
    }
    case InsertionMode.InColumnGroup:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
   if (token == 0x20 || token == 0x0c || token == 0x0a || token == 0x0d ||
          token == 0x09) {
          insertCharacter(getCurrentNode(), token);
        } else {
          if (applyEndTag("colgroup",insMode)) {
 return applyInsertionMode(token, null);
}
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        error = true;
        return false;
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("html")) {
 return applyInsertionMode(token, InsertionMode.InBody);
  } else if (name.Equals("col")) {
          addHtmlElementNoPush(tag);
          tag.ackSelfClosing();
        } else {
          if (applyEndTag("colgroup",insMode)) {
 return applyInsertionMode(token, null);
}
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("colgroup")) {
          if (getCurrentNode().getLocalName().Equals("html")) {
            error = true;
            return false;
          }
          popCurrentNode();
          insertionMode = InsertionMode.InTable;
        } else if (name.Equals("col")) {
          error = true;
          return false;
        } else {
          if (applyEndTag("colgroup",insMode)) {
 return applyInsertionMode(token, null);
}
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        if (applyEndTag("colgroup",insMode)) {
 return applyInsertionMode(token, null);
}
      } else if (token == TOKEN_EOF) {
        if (getCurrentNode().getLocalName().Equals("html")) {
          stopParsing();
          return true;
        }
        if (applyEndTag("colgroup",insMode)) {
 return applyInsertionMode(token, null);
}
      }
      return true;
    }
    case InsertionMode.InTableBody:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("tr")) {
          while (true) {
            Element node = getCurrentNode();
            if (node==null || node.getLocalName().Equals("tbody") ||
                node.getLocalName().Equals("tfoot") ||
                node.getLocalName().Equals("thead") ||
                node.getLocalName().Equals("html")) {
              break;
            }
            popCurrentNode();
          }
          addHtmlElement(tag);
          insertionMode = InsertionMode.InRow;
        } else if (name.Equals("th") || name.Equals("td")) {
          error = true;
          applyStartTag("tr",insMode);
          return applyInsertionMode(token, null);
        } else if (name.Equals("caption") ||
            name.Equals("col") || name.Equals("colgroup") ||
            name.Equals("tbody") || name.Equals("tfoot") ||
            name.Equals("thead")) {
          if (!hasHtmlElementInTableScope("tbody") &&
              !hasHtmlElementInTableScope("thead") &&
              !hasHtmlElementInTableScope("tfoot")
) {
            error = true;
            return false;
          }
          while (true) {
            Element node = getCurrentNode();
            if (node==null || node.getLocalName().Equals("tbody") ||
                node.getLocalName().Equals("tfoot") ||
                node.getLocalName().Equals("thead") ||
                node.getLocalName().Equals("html")) {
              break;
            }
            popCurrentNode();
          }
          applyEndTag(getCurrentNode().getLocalName(), insMode);
          return applyInsertionMode(token, null);
        } else {
 return applyInsertionMode(token, InsertionMode.InTable);
}
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("tbody") ||
            name.Equals("tfoot") || name.Equals("thead")) {
          if (!hasHtmlElementInScope(name)) {
            error = true;
            return false;
          }
          while (true) {
            Element node = getCurrentNode();
            if (node==null || node.getLocalName().Equals("tbody") ||
                node.getLocalName().Equals("tfoot") ||
                node.getLocalName().Equals("thead") ||
                node.getLocalName().Equals("html")) {
              break;
            }
            popCurrentNode();
          }
          popCurrentNode();
          insertionMode = InsertionMode.InTable;
        } else if (name.Equals("table")) {
          if (!hasHtmlElementInTableScope("tbody") &&
              !hasHtmlElementInTableScope("thead") &&
              !hasHtmlElementInTableScope("tfoot")
) {
            error = true;
            return false;
          }
          while (true) {
            Element node = getCurrentNode();
            if (node==null || node.getLocalName().Equals("tbody") ||
                node.getLocalName().Equals("tfoot") ||
                node.getLocalName().Equals("thead") ||
                node.getLocalName().Equals("html")) {
              break;
            }
            popCurrentNode();
          }
          applyEndTag(getCurrentNode().getLocalName(), insMode);
          return applyInsertionMode(token, null);
        } else if (name.Equals("body") ||
            name.Equals("caption") || name.Equals("col") ||
            name.Equals("colgroup") || name.Equals("html") ||
            name.Equals("td") || name.Equals("th") ||
            name.Equals("tr")) {
          error = true;
          return false;
        } else {
 return applyInsertionMode(token, InsertionMode.InTable);
}
      } else {
 return applyInsertionMode(token, InsertionMode.InTable);
}
      return true;
    }
    case InsertionMode.InRow:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        applyInsertionMode(token, InsertionMode.InTable);
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        applyInsertionMode(token, InsertionMode.InTable);
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("th")||name.Equals("td")) {
          while (!getCurrentNode().getLocalName().Equals("tr") &&
              !getCurrentNode().getLocalName().Equals("html")) {
            popCurrentNode();
          }
          insertionMode = InsertionMode.InCell;
          insertFormattingMarker(tag, addHtmlElement(tag));
        } else if (name.Equals("caption")|| name.Equals("col")||
            name.Equals("colgroup")|| name.Equals("tbody")||
            name.Equals("tfoot")|| name.Equals("thead")||
            name.Equals("tr")) {
          if (applyEndTag("tr",insMode)) {
 return applyInsertionMode(token, null);
}
        } else {
          applyInsertionMode(token, InsertionMode.InTable);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("tr")) {
          if (!hasHtmlElementInTableScope(name)) {
            error = true;
            return false;
          }
          while (!getCurrentNode().getLocalName().Equals("tr") &&
              !getCurrentNode().getLocalName().Equals("html")) {
            popCurrentNode();
          }
          popCurrentNode();
          insertionMode = InsertionMode.InTableBody;
        } else if (name.Equals("tbody") || name.Equals("tfoot") ||
            name.Equals("thead")) {
          if (!hasHtmlElementInTableScope(name)) {
            error = true;
            return false;
          }
          applyEndTag("tr",insMode);
          return applyInsertionMode(token, null);
        } else if (name.Equals("caption")||
            name.Equals("col")|| name.Equals("colgroup")||
            name.Equals("html")|| name.Equals("body")||
            name.Equals("td")|| name.Equals("th")) {
          error = true;
        } else {
          applyInsertionMode(token, InsertionMode.InTable);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        applyInsertionMode(token, InsertionMode.InTable);
      } else if (token == TOKEN_EOF) {
        applyInsertionMode(token, InsertionMode.InTable);
      }
      return true;
    }
    case InsertionMode.InCell:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        applyInsertionMode(token, InsertionMode.InBody);
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        applyInsertionMode(token, InsertionMode.InBody);
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("caption")||
            name.Equals("col")|| name.Equals("colgroup")||
            name.Equals("tbody")|| name.Equals("td")||
            name.Equals("tfoot")|| name.Equals("th")||
            name.Equals("thead")|| name.Equals("tr")) {
          if (!hasHtmlElementInTableScope("td") &&
              !hasHtmlElementInTableScope("th")) {
            error = true;
            return false;
          }
          applyEndTag(hasHtmlElementInTableScope("td") ? "td" : "th",insMode);
          return applyInsertionMode(token, null);
        } else {
          applyInsertionMode(token, InsertionMode.InBody);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("td") || name.Equals("th")) {
          if (!hasHtmlElementInTableScope(name)) {
            error = true;
            return false;
          }
          generateImpliedEndTags();
          if (!getCurrentNode().getLocalName().Equals(name)) {
            error = true;
          }
          while (true) {
            Element node = popCurrentNode();
            if (node.getLocalName().Equals(name)) {
              break;
            }
          }
          clearFormattingToMarker();
          insertionMode = InsertionMode.InRow;
        } else if (name.Equals("caption")|| name.Equals("col")||
            name.Equals("colgroup")|| name.Equals("body")||
            name.Equals("html")) {
          error = true;
          return false;
        } else if (name.Equals("table")||
            name.Equals("tbody")|| name.Equals("tfoot")||
            name.Equals("thead")|| name.Equals("tr")) {
          if (!hasHtmlElementInTableScope(name)) {
            error = true;
            return false;
          }
          applyEndTag(hasHtmlElementInTableScope("td") ? "td" : "th",insMode);
          return applyInsertionMode(token, null);
        } else {
          applyInsertionMode(token, InsertionMode.InBody);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        applyInsertionMode(token, InsertionMode.InBody);
      } else if (token == TOKEN_EOF) {
        applyInsertionMode(token, InsertionMode.InBody);
      }
      return true;
    }
    case InsertionMode.InSelect:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        if (token == 0) {
          error = true; return false;
        } else {
          insertCharacter(getCurrentNode(), token);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        error = true; return false;
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("html")) {
          applyInsertionMode(token, InsertionMode.InBody);
        } else if (name.Equals("option")) {
          if (getCurrentNode().getLocalName().Equals("option")) {
            applyEndTag("option",insMode);
          }
          addHtmlElement(tag);
        } else if (name.Equals("optgroup")) {
          if (getCurrentNode().getLocalName().Equals("option")) {
            applyEndTag("option",insMode);
          }
          if (getCurrentNode().getLocalName().Equals("optgroup")) {
            applyEndTag("optgroup",insMode);
          }
          addHtmlElement(tag);
        } else if (name.Equals("select")) {
          error = true;
          return applyEndTag("select",insMode);
        } else if (name.Equals("input") || name.Equals("keygen") ||
            name.Equals("textarea")) {
          error = true;
          if (!hasHtmlElementInSelectScope("select")) {
 return false;
}
          applyEndTag("select",insMode);
          return applyInsertionMode(token, null);
        } else if (name.Equals("script")) {
 return applyInsertionMode(token, InsertionMode.InHead);
} else {
          error = true; return false;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("optgroup")) {
          if (getCurrentNode().getLocalName().Equals("option") &&
              openElements.Count >= 2 &&
         openElements[openElements.Count-2].getLocalName().Equals("optgroup"
)) {
            applyEndTag("option",insMode);
          }
          if (getCurrentNode().getLocalName().Equals("optgroup")) {
            popCurrentNode();
          } else {
            error = true;
            return false;
          }
        } else if (name.Equals("option")) {
          if (getCurrentNode().getLocalName().Equals("option")) {
            popCurrentNode();
          } else {
            error = true;
            return false;
          }
        } else if (name.Equals("select")) {
          if (!hasHtmlElementInScope(name)) {
            error = true;
            return false;
          }
          while (true) {
            Element node = popCurrentNode();
            if (node.getLocalName().Equals(name)) {
              break;
            }
          }
          resetInsertionMode();
        } else {
          error = true; return false;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToCurrentNode(token);
      } else if (token == TOKEN_EOF) {
 if (getCurrentNode()==null || !getCurrentNode().getLocalName().Equals("html"
)) {
          error = true;
        }
        stopParsing();
      }
      return true;
    }
    case InsertionMode.InSelectInTable:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
 return applyInsertionMode(token, InsertionMode.InSelect);
  } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
 return applyInsertionMode(token, InsertionMode.InSelect);
  } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("caption") ||
            name.Equals("table") || name.Equals("tbody") ||
            name.Equals("tfoot") || name.Equals("thead") ||
            name.Equals("tr") || name.Equals("td") ||
            name.Equals("th")) {
          error = true;
          applyEndTag("select",insMode);
          return applyInsertionMode(token, null);
        }
        return applyInsertionMode(token, InsertionMode.InSelect);
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("caption") ||
            name.Equals("table") || name.Equals("tbody") ||
            name.Equals("tfoot") || name.Equals("thead") ||
            name.Equals("tr") || name.Equals("td") ||
            name.Equals("th")) {
          error = true;
          if (!hasHtmlElementInTableScope(name)) {
 return false;
}
          applyEndTag("select",insMode);
          return applyInsertionMode(token, null);
        }
        return applyInsertionMode(token, InsertionMode.InSelect);
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
 return applyInsertionMode(token, InsertionMode.InSelect);
} else {
 return (token == TOKEN_EOF) ?
   (applyInsertionMode(token, InsertionMode.InSelect)) : (true);
}
    }
    case InsertionMode.AfterBody:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
   if (token == 0x09 || token == 0x0a || token == 0x0c || token == 0x0d ||
          token == 0x20) {
          applyInsertionMode(token, InsertionMode.InBody);
        } else {
          error = true;
          insertionMode = InsertionMode.InBody;
          return applyInsertionMode(token, null);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        error = true;
        return true;
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("html")) {
          applyInsertionMode(token, InsertionMode.InBody);
        } else {
          error = true;
          insertionMode = InsertionMode.InBody;
          return applyInsertionMode(token, null);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("html")) {
          if (context != null) {
            error = true;
            return false;
          }
          insertionMode = InsertionMode.AfterAfterBody;
        } else {
          error = true;
          insertionMode = InsertionMode.InBody;
          return applyInsertionMode(token, null);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToFirst(token);
      } else if (token == TOKEN_EOF) {
        stopParsing();

        return true;
      }
      return true;
    }
    case InsertionMode.InFrameset:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        if (token == 0x09 || token == 0x0a || token == 0x0c ||
            token == 0x0d || token == 0x20) {
          insertCharacter(getCurrentNode(), token);
        } else {
          error = true;
          return false;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        error = true;
        return false;
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("html")) {
          applyInsertionMode(token, InsertionMode.InBody);
        } else if (name.Equals("frameset")) {
          addHtmlElement(tag);
        } else if (name.Equals("frame")) {
          addHtmlElementNoPush(tag);
          tag.ackSelfClosing();
        } else if (name.Equals("noframes")) {
          applyInsertionMode(token, InsertionMode.InHead);
        } else {
          error = true;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        if (getCurrentNode().getLocalName().Equals("html")) {
          error = true;
          return false;
        }
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("frameset")) {
          popCurrentNode();
          if (context == null &&
              !getCurrentNode().isHtmlElement("frameset")) {
            insertionMode = InsertionMode.AfterFrameset;
          }
        } else {
          error = true;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToCurrentNode(token);
      } else if (token == TOKEN_EOF) {
        if (!getCurrentNode().isHtmlElement("html")) {
          error = true;
        }
        stopParsing();
      }
      return true;
    }
    case InsertionMode.AfterFrameset:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
   if (token == 0x09 || token == 0x0a || token == 0x0c || token == 0x0d ||
          token == 0x20) {
          insertCharacter(getCurrentNode(), token);
        } else {
          error = true;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        error = true;
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("html")) {
 return applyInsertionMode(token, InsertionMode.InBody);
  } else if (name.Equals("noframes")) {
 return applyInsertionMode(token, InsertionMode.InHead);
} else {
          error = true;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        EndTagToken tag=(EndTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("html")) {
          insertionMode = InsertionMode.AfterAfterFrameset;
        } else {
          error = true;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToCurrentNode(token);
      } else if (token == TOKEN_EOF) {
        stopParsing();
      }
      return true;
    }
    case InsertionMode.AfterAfterBody:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
   if (token == 0x09 || token == 0x0a || token == 0x0c || token == 0x0d ||
          token == 0x20) {
          applyInsertionMode(token, InsertionMode.InBody);
        } else {
          error = true;
          insertionMode = InsertionMode.InBody;
          return applyInsertionMode(token, null);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        applyInsertionMode(token, InsertionMode.InBody);
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if (name.Equals("html")) {
          applyInsertionMode(token, InsertionMode.InBody);
        } else {
          error = true;
          insertionMode = InsertionMode.InBody;
          return applyInsertionMode(token, null);
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        error = true;
        insertionMode = InsertionMode.InBody;
        return applyInsertionMode(token, null);
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToDocument(token);
      } else if (token == TOKEN_EOF) {
        stopParsing();
      }
      return true;
    }
    case InsertionMode.AfterAfterFrameset:{
      if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
   if (token == 0x09 || token == 0x0a || token == 0x0c || token == 0x0d ||
          token == 0x20) {
          applyInsertionMode(token, InsertionMode.InBody);
        } else {
          error = true;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_DOCTYPE) {
        applyInsertionMode(token, InsertionMode.InBody);
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = tag.getName();
        if ("html".Equals(name)) {
          applyInsertionMode(token, InsertionMode.InBody);
        } else if ("noframes".Equals(name)) {
          applyInsertionMode(token, InsertionMode.InHead);
        } else {
          error = true;
        }
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_END_TAG) {
        error = true;
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_COMMENT) {
        addCommentNodeToDocument(token);
      } else if (token == TOKEN_EOF) {
        stopParsing();
      }
      return true;
    }
    default: throw new InvalidOperationException();
    }
  }

  private bool applyStartTag(string name, InsertionMode? insMode) {
    return applyInsertionMode(getArtificialToken(TOKEN_START_TAG, name), insMode);
  }

  private void changeEncoding(string charset) {
    string currentEncoding = encoding.getEncoding();
    if (currentEncoding.Equals("utf-16le") ||
        currentEncoding.Equals("utf-16be")) {
   encoding = new
        EncodingConfidence(currentEncoding, EncodingConfidence.Certain);
      return;
    }
    if (charset.Equals("utf-16le")) {
      charset="utf-8";
    } else if (charset.Equals("utf-16be")) {
      charset="utf-8";
    }
    if (charset.Equals(currentEncoding)) {
   encoding = new
        EncodingConfidence(currentEncoding, EncodingConfidence.Certain);
      return;
    }
    // Reinitialize all parser state
    initialize();
    // Rewind the input stream and set the new encoding
    inputStream.rewind();
    encoding = new EncodingConfidence(charset, EncodingConfidence.Certain);
    decoder = new Html5Decoder(TextEncoding.getDecoder(encoding.getEncoding()));
    charInput = new StackableCharacterInput(new
      DecoderCharacterInput(inputStream, decoder));
  }

  private void clearFormattingToMarker() {
    while (formattingElements.Count>0) {
      FormattingElement
        fe = removeAtIndex(formattingElements, formattingElements.Count-1);
      if (fe.isMarker()) {
        break;
      }
    }
  }

  private void closeParagraph(InsertionMode? insMode) {
    if (hasHtmlElementInButtonScope("p")) {
      applyEndTag("p",insMode);
    }
  }

  private Comment createCommentNode(int token) {
    CommentToken comment=(CommentToken)getToken(token);
    Comment node = new Comment();
    node.setData(comment.getValue());
    return node;
  }

  private int emitCurrentTag() {
    int ret = tokens.Count|currentTag.getType();
    tokens.Add(currentTag);
    if (currentTag.getType() == TOKEN_START_TAG) {
      lastStartTag = currentTag;
    } else {
      if (currentTag.getAttributes().Count>0 ||
          currentTag.isSelfClosing()) {
        error = true;
      }
    }
    currentTag = null;
    return ret;
  }

  private void fosterParent(Node element) {
    if (openElements.Count == 0) {
 return;
}
    Node fosterParent = openElements[0];
    for (int i = openElements.Count-1;i >= 0; --i) {
      Element e = openElements[i];
      if (e.getLocalName().Equals("table")) {
        Node parent=(Node) e.getParentNode();
  bool isElement=(parent != null &&
          parent.getNodeType() == NodeType.ELEMENT_NODE);
        if (!isElement) {  // the parent is not an element
          if (i <= 1) {
 // This usually won't happen
            throw new InvalidOperationException();
 }
          // append to the element before this table
          fosterParent = openElements[i-1];
          break;
        } else {
          // Parent of the table, insert before the table
          parent.insertBefore(element, e);
          return;
        }
      }
    }
    fosterParent.appendChild(element);
  }

  private void generateImpliedEndTags() {
    while (true) {
      Element node = getCurrentNode();
      string name = node.getLocalName();
      if ("dd".Equals(name)||
          "dd".Equals(name)|| "dt".Equals(name)||
          "li".Equals(name)|| "option".Equals(name)||
          "optgroup".Equals(name)|| "p".Equals(name)||
          "rp".Equals(name)|| "rt".Equals(name)) {
        popCurrentNode();
      } else {
        break;
      }
    }
  }

  private void generateImpliedEndTagsExcept(string _string) {
    while (true) {
      Element node = getCurrentNode();
      string name = node.getLocalName();
      if (_string.Equals(name)) {
        break;
      }
      if ("dd".Equals(name)|| "dd".Equals(name)||
          "dt".Equals(name)|| "li".Equals(name)||
          "option".Equals(name)|| "optgroup".Equals(name)||
          "p".Equals(name)|| "rp".Equals(name)||
          "rt".Equals(name)) {
        popCurrentNode();
      } else {
        break;
      }
    }
  }

  private int getArtificialToken(int type, string name) {
    if (type == TOKEN_END_TAG) {
      EndTagToken token = new EndTagToken(name);
      int ret = tokens.Count|type;
      tokens.Add(token);
      return ret;
    }
    if (type == TOKEN_START_TAG) {
      StartTagToken token = new StartTagToken(name);
      int ret = tokens.Count|type;
      tokens.Add(token);
      return ret;
    }
    throw new ArgumentException();
  }

  private Element getCurrentNode() {
return (openElements.Count == 0) ? (null) :
      (openElements[openElements.Count-1]);
  }

  private FormattingElement getFormattingElement(Element node) {
    foreach (var fe in formattingElements) {
      if (!fe.isMarker() && node.Equals(fe.element)) {
 return fe;
}
    }
    return null;
  }

  private Text getFosterParentedTextNode() {
    if (openElements.Count == 0) {
 return null;
}
    Node fosterParent = openElements[0];
    IList<Node> childNodes;
    for (int i = openElements.Count-1;i >= 0; --i) {
      Element e = openElements[i];
      if (e.getLocalName().Equals("table")) {
        Node parent=(Node) e.getParentNode();
  bool isElement=(parent != null &&
          parent.getNodeType() == NodeType.ELEMENT_NODE);
        if (!isElement) {  // the parent is not an element
          if (i <= 1) {
 // This usually won't happen
            throw new InvalidOperationException();
 }
          // append to the element before this table
          fosterParent = openElements[i-1];
          break;
        } else {
          // Parent of the table, insert before the table
          childNodes = parent.getChildNodesInternal();
          if (childNodes.Count == 0) {
 throw new InvalidOperationException();
}
          for (int j = 0;j<childNodes.Count; ++j) {
            if (childNodes[j].Equals(e)) {
              if (j>0 && childNodes[j-1].getNodeType() == NodeType.TEXT_NODE) {
 return (Text)childNodes[j-1];
} else {
                Text textNode = new Text();
                parent.insertBefore(textNode, e);
                return textNode;
              }
            }
          }
          throw new InvalidOperationException();
        }
      }
    }
    childNodes = fosterParent.getChildNodesInternal();
  Node lastChild=(childNodes.Count == 0) ? null :
      childNodes[childNodes.Count-1];
    if (lastChild == null || lastChild.getNodeType() != NodeType.TEXT_NODE) {
      Text textNode = new Text();
      fosterParent.appendChild(textNode);
      return textNode;
    } else {
 return (Text)lastChild;
}
  }
  private Text getTextNodeToInsert(Node node) {
    if (doFosterParent && node.Equals(getCurrentNode())) {
      string name=((Element)node).getLocalName();
      if ("table".Equals(name) || "tbody".Equals(name) ||
          "tfoot".Equals(name) || "thead".Equals(name) ||
          "tr".Equals(name)) {
 return getFosterParentedTextNode();
}
    }
    IList<Node> childNodes = node.getChildNodesInternal();
  Node lastChild=(childNodes.Count == 0) ? null :
      childNodes[childNodes.Count-1];
    if (lastChild == null || lastChild.getNodeType() != NodeType.TEXT_NODE) {
      Text textNode = new Text();
      node.appendChild(textNode);
      return textNode;
    } else {
 return (Text)lastChild;
}
  }
  internal IToken getToken(int token) {
    if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER ||
        (token&TOKEN_TYPE_MASK) == TOKEN_EOF) {
 return null;
} else {
 return tokens[token&TOKEN_INDEX_MASK];
}
  }
  private bool hasHtmlElementInButtonScope(string name) {
    bool found = false;
    foreach (var e in openElements) {
      if (e.getLocalName().Equals(name)) {
        found = true;
      }
    }
    if (!found) {
 return false;
}
    for (int i = openElements.Count-1;i >= 0; --i) {
      Element e = openElements[i];
      string _namespace = e.getNamespaceURI();
      string thisName = e.getLocalName();
      if (HTML_NAMESPACE.Equals(_namespace)) {
        if (thisName.Equals(name)) {
 return true;
}
        if (thisName.Equals("applet")|| thisName.Equals("caption")||
            thisName.Equals("html")|| thisName.Equals("table")||
            thisName.Equals("td")|| thisName.Equals("th")||
            thisName.Equals("marquee")|| thisName.Equals("object")||
            thisName.Equals("button")) {
  //Console.WriteLine("not in scope: %s",thisName);
}
          return false;
      }
      if (MATHML_NAMESPACE.Equals(_namespace)) {
        if (thisName.Equals("mi")||
            thisName.Equals("mo")|| thisName.Equals("mn")||
            thisName.Equals("ms")|| thisName.Equals("mtext")||
            thisName.Equals("annotation-xml")) {
 return false;
}
      }
      if (SVG_NAMESPACE.Equals(_namespace)) {
        if (thisName.Equals("foreignObject")|| thisName.Equals("desc")||
            thisName.Equals("title")) {
 return false;
}
      }
    }
    return false;
  }
  private bool hasHtmlElementInListItemScope(string name) {
    for (int i = openElements.Count-1;i >= 0; --i) {
      Element e = openElements[i];
      if (e.isHtmlElement(name)) {
 return true;
}
      if (e.isHtmlElement("applet")||
          e.isHtmlElement("caption")|| e.isHtmlElement("html")||
          e.isHtmlElement("table")|| e.isHtmlElement("td")||
          e.isHtmlElement("th")|| e.isHtmlElement("ol")||
          e.isHtmlElement("ul")|| e.isHtmlElement("marquee")||
          e.isHtmlElement("object")|| e.isMathMLElement("mi")||
          e.isMathMLElement("mo")|| e.isMathMLElement("mn")||
          e.isMathMLElement("ms")|| e.isMathMLElement("mtext")||
          e.isMathMLElement("annotation-xml")||
          e.isSvgElement("foreignObject")||
          e.isSvgElement("desc")|| e.isSvgElement("title")
) {
 return false;
}
    }
    return false;
  }
  private bool hasHtmlElementInScope(Element node) {
    for (int i = openElements.Count-1;i >= 0; --i) {
      Element e = openElements[i];
      if (e == node) {
 return true;
}
      if (e.isHtmlElement("applet")||
          e.isHtmlElement("caption")|| e.isHtmlElement("html")||
          e.isHtmlElement("table")|| e.isHtmlElement("td")||
          e.isHtmlElement("th")|| e.isHtmlElement("marquee")||
          e.isHtmlElement("object")|| e.isMathMLElement("mi")||
          e.isMathMLElement("mo")|| e.isMathMLElement("mn")||
          e.isMathMLElement("ms")|| e.isMathMLElement("mtext")||
          e.isMathMLElement("annotation-xml")||
          e.isSvgElement("foreignObject")||
          e.isSvgElement("desc")|| e.isSvgElement("title")
) {
 return false;
}
    }
    return false;
  }
  private bool hasHtmlElementInScope(string name) {
    for (int i = openElements.Count-1;i >= 0; --i) {
      Element e = openElements[i];
      if (e.isHtmlElement(name)) {
 return true;
}
      if (e.isHtmlElement("applet")||
          e.isHtmlElement("caption")|| e.isHtmlElement("html")||
          e.isHtmlElement("table")|| e.isHtmlElement("td")||
          e.isHtmlElement("th")|| e.isHtmlElement("marquee")||
          e.isHtmlElement("object")|| e.isMathMLElement("mi")||
          e.isMathMLElement("mo")|| e.isMathMLElement("mn")||
          e.isMathMLElement("ms")|| e.isMathMLElement("mtext")||
          e.isMathMLElement("annotation-xml")||
          e.isSvgElement("foreignObject")||
          e.isSvgElement("desc")|| e.isSvgElement("title")
) {
 return false;
}
    }
    return false;
  }
  private bool hasHtmlElementInSelectScope(string name) {
    for (int i = openElements.Count-1;i >= 0; --i) {
      Element e = openElements[i];
      if (e.isHtmlElement(name)) {
 return true;
}
      if (!e.isHtmlElement("optgroup") && !e.isHtmlElement("option")) {
 return false;
}
    }
    return false;
  }

  private bool hasHtmlElementInTableScope(string name) {
    for (int i = openElements.Count-1;i >= 0; --i) {
      Element e = openElements[i];
      if (e.isHtmlElement(name)) {
 return true;
}
      if (e.isHtmlElement("html")||
          e.isHtmlElement("table")) {
 return false;
}
    }
    return false;
  }

  private bool hasHtmlHeaderElementInScope() {
    for (int i = openElements.Count-1;i >= 0; --i) {
      Element e = openElements[i];
      if (e.isHtmlElement("h1")||
          e.isHtmlElement("h2")|| e.isHtmlElement("h3")||
          e.isHtmlElement("h4")|| e.isHtmlElement("h5")||
          e.isHtmlElement("h6")) {
 return true;
}
      if (e.isHtmlElement("applet")|| e.isHtmlElement("caption")||
          e.isHtmlElement("html")|| e.isHtmlElement("table")||
          e.isHtmlElement("td")|| e.isHtmlElement("th")||
          e.isHtmlElement("marquee")|| e.isHtmlElement("object")||
          e.isMathMLElement("mi")|| e.isMathMLElement("mo")||
          e.isMathMLElement("mn")|| e.isMathMLElement("ms")||
          e.isMathMLElement("mtext")|| e.isMathMLElement("annotation-xml")||
          e.isSvgElement("foreignObject")|| e.isSvgElement("desc")||
          e.isSvgElement("title")) {
 return false;
}
    }
    return false;
  }

  private bool hasNativeElementInScope() {
    for (int i = openElements.Count-1;i >= 0; --i) {
      Element e = openElements[i];
      //Console.WriteLine("%s %s",e.getLocalName(),e.getNamespaceURI());
      if (e.getNamespaceURI().Equals(HTML_NAMESPACE) ||
          isMathMLTextIntegrationPoint(e) || isHtmlIntegrationPoint(e)) {
 return true;
}
      if (e.isHtmlElement("applet")||
          e.isHtmlElement("caption")|| e.isHtmlElement("html")||
          e.isHtmlElement("table")|| e.isHtmlElement("td")||
          e.isHtmlElement("th")|| e.isHtmlElement("marquee")||
          e.isHtmlElement("object")|| e.isMathMLElement("mi")||
          e.isMathMLElement("mo")|| e.isMathMLElement("mn")||
          e.isMathMLElement("ms")|| e.isMathMLElement("mtext")||
          e.isMathMLElement("annotation-xml")||
          e.isSvgElement("foreignObject")||
          e.isSvgElement("desc")|| e.isSvgElement("title")
) {
 return false;
}
    }
    return false;
  }

  private void initialize() {
    noforeign = false;
    document = new Document();
    document.address = address;
    document.setBaseURI(address);
    context = null;
    openElements.Clear();
    error = false;
    baseurl = null;
    hasForeignContent = false;  // performance optimization
    lastState = TokenizerState.Data;
    lastComment = null;
    docTypeToken = null;
    tokens.Clear();
    lastStartTag = null;
    currentEndTag = null;
    currentTag = null;
    currentAttribute = null;
    bogusCommentCharacter = 0;
    tempBuffer.clearAll();
    state = TokenizerState.Data;
    framesetOk = true;
    integrationElements.Clear();
    tokenQueue.Clear();
    insertionMode = InsertionMode.Initial;
    originalInsertionMode = InsertionMode.Initial;
    formattingElements.Clear();
    doFosterParent = false;
    headElement = null;
    formElement = null;
    inputElement = null;
    done = false;
    pendingTableCharacters.clearAll();
  }

  private void insertCharacter(Node node, int ch) {
    Text textNode = getTextNodeToInsert(node);
    if (textNode != null) {
      textNode.text.appendInt(ch);
    }
  }

  private Element insertForeignElement(StartTagToken tag, string _namespace) {
    Element element = Element.fromToken(tag, _namespace);
    string xmlns=element.getAttributeNS(XMLNS_NAMESPACE,"xmlns");
    string xlink=element.getAttributeNS(XMLNS_NAMESPACE,"xlink");
    if (xmlns != null && !xmlns.Equals(_namespace)) {
      error = true;
    }
    if (xlink != null && !xlink.Equals(XLINK_NAMESPACE)) {
      error = true;
    }
    Element currentNode = getCurrentNode();
    if (currentNode != null) {
      insertInCurrentNode(element);
    } else {
      document.appendChild(element);
    }
    openElements.Add(element);
    return element;
  }

  private void insertFormattingMarker(StartTagToken tag,
      Element addHtmlElement) {
    FormattingElement fe = new FormattingElement();
    fe.marker = true;
    fe.element = addHtmlElement;
    fe.token = tag;
    formattingElements.Add(fe);
  }

  private void insertInCurrentNode(Node element) {
    Element node = getCurrentNode();
    if (doFosterParent) {
      string name = node.getLocalName();
      if ("table".Equals(name) || "tbody".Equals(name) ||
          "tfoot".Equals(name) || "thead".Equals(name) ||
          "tr".Equals(name)) {
        fosterParent(element);
      } else {
        node.appendChild(element);
      }
    } else {
      node.appendChild(element);
    }
  }

  private void insertString(Node node, string str) {
    Text textNode = getTextNodeToInsert(node);
    if (textNode != null) {
      textNode.text.appendString(str);
    }
  }
  private bool isAppropriateEndTag() {
    if (lastStartTag == null || currentEndTag == null) {
 return false;
}
    //Console.WriteLine("lastStartTag=%s",lastStartTag.getName());
    //Console.WriteLine("currentEndTag=%s",currentEndTag.getName());
    return currentEndTag.getName().Equals(lastStartTag.getName());
  }

  public bool isError() {
    return error;
  }
  private bool isForeignContext(int token) {
    if (hasForeignContent && token != TOKEN_EOF) {
      Element element=(context != null && openElements.Count == 1) ?
          context : getCurrentNode();  // adjusted current node
      if (element == null) {
 return false;
}
      if (element.getNamespaceURI().Equals(HTML_NAMESPACE)) {
 return false;
}
      if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        string name = element.getLocalName();
        if (isMathMLTextIntegrationPoint(element)) {
          string tokenName = tag.getName();
          if (!"mglyph".Equals(tokenName) &&
              !"malignmark".Equals(tokenName)) {
 return false;
}
        }
        if (MATHML_NAMESPACE.Equals(element.getNamespaceURI()) && (
            name.Equals("annotation-xml")) && "svg".Equals(tag.getName())) {
 return false;
}
        return isHtmlIntegrationPoint(element);
      } else if ((token&TOKEN_TYPE_MASK) == TOKEN_CHARACTER) {
        return isMathMLTextIntegrationPoint(element) ||
            isHtmlIntegrationPoint(element);
      } else {
 return true;
}
    }
    return false;
  }
  private bool isHtmlIntegrationPoint(Element element) {
    if (integrationElements.Contains(element)) {
 return true;
}
    string name = element.getLocalName();
    return SVG_NAMESPACE.Equals(element.getNamespaceURI()) && (
        name.Equals("foreignObject") || name.Equals("desc") ||
        name.Equals("title"));
  }

  private bool isMathMLTextIntegrationPoint(Element element) {
    string name = element.getLocalName();
    return MATHML_NAMESPACE.Equals(element.getNamespaceURI()) && (
        name.Equals("mi") || name.Equals("mo") ||
        name.Equals("mn") || name.Equals("ms") ||
        name.Equals("mtext"));
  }

  private bool isSpecialElement(Element node) {
    if (node.isHtmlElement("address") || node.isHtmlElement("applet") ||
      node.isHtmlElement("area") || node.isHtmlElement("article") ||
      node.isHtmlElement("aside") || node.isHtmlElement("base") ||
      node.isHtmlElement("basefont") || node.isHtmlElement("bgsound") ||
      node.isHtmlElement("blockquote") || node.isHtmlElement("body") ||
      node.isHtmlElement("br") || node.isHtmlElement("button") ||
      node.isHtmlElement("caption") || node.isHtmlElement("center") ||
      node.isHtmlElement("col") || node.isHtmlElement("colgroup") ||
      node.isHtmlElement("dd") || node.isHtmlElement("details") ||
      node.isHtmlElement("dir") || node.isHtmlElement("div") ||
      node.isHtmlElement("dl") || node.isHtmlElement("dt") ||
      node.isHtmlElement("embed") || node.isHtmlElement("fieldset") ||
      node.isHtmlElement("figcaption") || node.isHtmlElement("figure")||
        node.isHtmlElement("footer") || node.isHtmlElement("form") ||
          node.isHtmlElement("frame") || node.isHtmlElement("frameset") ||
          node.isHtmlElement("h1") || node.isHtmlElement("h2") ||
          node.isHtmlElement("h3") || node.isHtmlElement("h4") ||
          node.isHtmlElement("h5") || node.isHtmlElement("h6") ||
          node.isHtmlElement("head") || node.isHtmlElement("header") ||
          node.isHtmlElement("hgroup") || node.isHtmlElement("hr") ||
          node.isHtmlElement("html") || node.isHtmlElement("iframe") ||
          node.isHtmlElement("img") || node.isHtmlElement("input") ||
          node.isHtmlElement("isindex") || node.isHtmlElement("li") ||
          node.isHtmlElement("link") ||
        node.isHtmlElement("listing") || node.isHtmlElement("main") ||
          node.isHtmlElement("marquee") || node.isHtmlElement("menu") ||
          node.isHtmlElement("menuitem") || node.isHtmlElement("meta") ||
          node.isHtmlElement("nav") || node.isHtmlElement("noembed") ||
          node.isHtmlElement("noframes") || node.isHtmlElement("noscript")||
          node.isHtmlElement("object") || node.isHtmlElement("ol") ||
          node.isHtmlElement("p") || node.isHtmlElement("param") ||
          node.isHtmlElement("plaintext") || node.isHtmlElement("pre") ||
          node.isHtmlElement("script") || node.isHtmlElement("section") ||
        node.isHtmlElement("select") || node.isHtmlElement("source") ||
          node.isHtmlElement("style") || node.isHtmlElement("summary") ||
          node.isHtmlElement("table") || node.isHtmlElement("tbody") ||
          node.isHtmlElement("td") || node.isHtmlElement("textarea") ||
          node.isHtmlElement("tfoot") || node.isHtmlElement("th") ||
          node.isHtmlElement("thead") || node.isHtmlElement("title") ||
          node.isHtmlElement("tr") || node.isHtmlElement("track") ||
          node.isHtmlElement("ul") || node.isHtmlElement("wbr") ||
          node.isHtmlElement("xmp")) {
 return true;
}
    if (node.isMathMLElement("mi") || node.isMathMLElement("mo") ||
      node.isMathMLElement("mn") || node.isMathMLElement("ms") ||
      node.isMathMLElement("mtext") ||
      node.isMathMLElement("annotation-xml")) {
 return true;
}
    return (node.isSvgElement("foreignObject") || node.isSvgElement("desc"
) || node.isSvgElement("title")) ? (true) : (false);
  }
  internal string nodesToDebugString(IList<Node> nodes) {
    StringBuilder builder = new StringBuilder();
    foreach (var node in nodes) {
      string str = node.toDebugString();
      string[] strarray=StringUtility.splitAt(str,"\n");
      int len = strarray.Length;
      if (len>0 && strarray[len-1].Length == 0) {
        --len;  // ignore trailing empty _string
      }
      for (int i = 0; i < len; ++i) {
        string el = strarray[i];
        builder.Append("| ");
        builder.Append(el.Replace("~~~~","\n"));
        builder.Append("\n");
      }
    }
    return builder.ToString();
  }
  public IDocument parse() {
    while (true) {
      int token = parserRead();
      applyInsertionMode(token, null);
      if ((token&TOKEN_TYPE_MASK) == TOKEN_START_TAG) {
        StartTagToken tag=(StartTagToken)getToken(token);
        // Console.WriteLine(tag);
        if (!tag.isAckSelfClosing()) {
          error = true;
        }
      }
      // Console.WriteLine("token=%08X, insertionMode=%s, error=%s"
      // , token, insertionMode, error);
      if (done) {
        break;
      }
    }
    return document;
  }

  private int parseCharacterReference(int allowedCharacter) {
    int markStart = charInput.setSoftMark();
    int c1 = charInput.read();
    if (c1<0 || c1 == 0x09 || c1 == 0x0a || c1 == 0x0c ||
        c1 == 0x20 || c1 == 0x3c || c1 == 0x26 || (allowedCharacter >= 0 &&
          c1 == allowedCharacter)) {
      charInput.setMarkPosition(markStart);
      return 0x26;  // emit ampersand
    } else if (c1 == 0x23) {
      c1 = charInput.read();
      int value = 0;
      bool haveHex = false;
      if (c1 == 0x78 || c1 == 0x58) {
        // Hex number
        while (true) {  // skip zeros
          int c = charInput.read();
          if (c!='0') {
            if (c >= 0) {
              charInput.moveBack(1);
            }
            break;
          }
          haveHex = true;
        }
        bool overflow = false;
        while (true) {
          int number = charInput.read();
          if (number>= '0' && number<= '9') {
            if (!overflow) {
              value=(value<< 4)+(number-'0');
            }
            haveHex = true;
          } else if (number>= 'a' && number<= 'f') {
            if (!overflow) {
              value=(value<< 4)+(number-'a')+10;
            }
            haveHex = true;
          } else if (number>= 'A' && number<= 'F') {
            if (!overflow) {
              value=(value<< 4)+(number-'A')+10;
            }
            haveHex = true;
          } else {
            if (number >= 0) {
              // move back character (except if it's EOF)
              charInput.moveBack(1);
            }
            break;
          }
          if (value>0x10ffff) {
            value = 0x110000; overflow = true;
          }
        }
      } else {
        if (c1>0) {
          charInput.moveBack(1);
        }
        // Digits
        while (true) {  // skip zeros
          int c = charInput.read();
          if (c!='0') {
            if (c >= 0) {
              charInput.moveBack(1);
            }
            break;
          }
          haveHex = true;
        }
        bool overflow = false;
        while (true) {
          int number = charInput.read();
          if (number>= '0' && number<= '9') {
            if (!overflow) {
              value=(value*10)+(number-'0');
            }
            haveHex = true;
          } else {
            if (number >= 0) {
              // move back character (except if it's EOF)
              charInput.moveBack(1);
            }
            break;
          }
          if (value>0x10ffff) {
            value = 0x110000; overflow = true;
          }
        }
      }
      if (!haveHex) {
        // No digits: parse error
        error = true;
        charInput.setMarkPosition(markStart);
        return 0x26;  // emit ampersand
      }
      c1 = charInput.read();
      if (c1 != 0x3b) {  // semicolon
        error = true;
        if (c1 >= 0) {
          charInput.moveBack(1);  // parse error
        }
      }
      if (value>0x10ffff || (value >= 0xd800 && value <= 0xdfff)) {
        error = true;
        value = 0xfffd;  // parse error
      } else if (value >= 0x80 && value<0xa0) {
        error = true;
        // parse error
        int[] replacements = new int[] { 0x20ac, 0x81, 0x201a, 0x192, 0x201e,
          0x2026, 0x2020, 0x2021, 0x2c6, 0x2030, 0x160, 0x2039, 0x152, 0x8d, 0x17d,
          0x8f, 0x90, 0x2018, 0x2019, 0x201c, 0x201d,
          0x2022, 0x2013, 0x2014, 0x2dc, 0x2122,
          0x161, 0x203a, 0x153, 0x9d, 0x17e, 0x178 };
        value = replacements[value-0x80];
      } else if (value == 0x0d) {
        // parse error
        error = true;
      } else if (value == 0x00) {
        // parse error
        error = true;
        value = 0xfffd;
      }
      if (value == 0x08 || value == 0x0b ||
          (value & 0xfffe) == 0xfffe || (value >= 0x0e && value <= 0x1f) ||
          value == 0x7f || (value >= 0xfdd0 && value <= 0xfdef)) {
        // parse error
        error = true;
      }
      return value;
    } else if ((c1>= 'A' && c1<= 'Z') || (c1>= 'a' && c1<= 'z') ||
        (c1>= '0' && c1<= '9')) {
      int[] data = null;
      // check for certain well-known entities
      if (c1=='g') {
        if (charInput.read()=='t' && charInput.read()==';') {
 return '>';
}
        charInput.setMarkPosition(markStart + 1);
      } else if (c1=='l') {
        if (charInput.read()=='t' && charInput.read()==';') {
 return '<';
}
        charInput.setMarkPosition(markStart + 1);
      } else if (c1=='a') {
 if (charInput.read()=='m' && charInput.read()=='p' && charInput.read()==';'
) {
 return '&';
}
        charInput.setMarkPosition(markStart + 1);
      } else if (c1=='n') {
        if (charInput.read()=='b' && charInput.read()=='s' &&
          charInput.read()=='p' && charInput.read()==';') {
 return 0xa0;
}
        charInput.setMarkPosition(markStart + 1);
      }
      int count = 0;
      for (int index = 0;index<HtmlEntities.entities.Length; ++index) {
        string entity = HtmlEntities.entities[index];
        if (entity[0]==c1) {
          if (data == null) {
            // Read the rest of the character reference
            // (the entities are sorted by length, so
            // we get the maximum length possible starting
            // with the first matching character)
            data = new int[entity.Length-1];
            count = charInput.read(data, 0, data.Length);
            //Console.WriteLine("markposch=%c",(char)data[0]);
          }
          // if fewer bytes were read than the
          // entity's remaining length, this
          // can't match
          //Console.WriteLine("data count=%s %s"
          // , count, stream.getMarkPosition());
          if (count<entity.Length-1) {
            continue;
          }
          bool matched = true;
          for (int i = 1;i<entity.Length; ++i) {
            //Console.WriteLine("%c %c | markpos=%d",
            // (char)data[i-1], entity[i], stream.getMarkPosition());
            if (data[i-1]!=entity[i]) {
              matched = false;
              break;
            }
          }
          if (matched) {
            // Move back the difference between the
            // number of bytes actually read and
            // this entity's length
            charInput.moveBack(count-(entity.Length-1));
            //Console.WriteLine("lastchar=%c",entity[entity.Length-1]);
            if (allowedCharacter>= 0 && entity[entity.Length-1]!=';') {
              // Get the next character after the entity
              int ch2 = charInput.read();
              if (ch2=='=' || (ch2>= 'A' && ch2<= 'Z') ||
                (ch2>= 'a' && ch2<= 'z') || (ch2>= '0' && ch2<= '9')) {
                if (ch2=='=') {
                  error = true;
                }
                charInput.setMarkPosition(markStart);
                return 0x26;  // return ampersand rather than entity
              } else {
                if (ch2 >= 0) {
                  charInput.moveBack(1);
                }
                if (entity[entity.Length-1]!=';') {
                  error = true;
                }
              }
            } else {
              if (entity[entity.Length-1]!=';') {
                error = true;
              }
            }
            return HtmlEntities.entityValues[index];
          }
        }
      }
      // no match
      charInput.setMarkPosition(markStart);
      while (true) {
        int ch2 = charInput.read();
        if (ch2==';') {
          error = true;
          break;
        } else if (!((ch2>= 'A' && ch2<= 'Z') || (ch2>= 'a' && ch2<= 'z') ||
            (ch2>= '0' && ch2<= '9'))) {
          break;
        }
      }
      charInput.setMarkPosition(markStart);
      return 0x26;
    } else {
      // not a character reference
      charInput.setMarkPosition(markStart);
      return 0x26;  // emit ampersand
    }
  }

  public IList<Node> parseFragment(Element context) {
    if (context == null) {
 throw new ArgumentException();
}
    initialize();
    document = new Document();
    INode ownerDocument = context;
    INode lastForm = null;
    while (ownerDocument != null) {
      if (lastForm == null && ownerDocument.getNodeType() == NodeType.ELEMENT_NODE) {
        string name=((Element)ownerDocument).getLocalName();
        if (name.Equals("form")) {
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
      ownerDoc=(Document)ownerDocument;
      document.setMode(ownerDoc.getMode());
    }
    string name2 = context.getLocalName();
    state = TokenizerState.Data;
    if (name2.Equals("title")||name2.Equals("textarea")) {
      state = TokenizerState.RcData;
    } else if (name2.Equals("style") || name2.Equals("xmp") ||
        name2.Equals("iframe") || name2.Equals("noembed") ||
        name2.Equals("noframes")) {
      state = TokenizerState.RawText;
    } else if (name2.Equals("script")) {
      state = TokenizerState.ScriptData;
    } else if (name2.Equals("noscript")) {
      state = TokenizerState.Data;
    } else if (name2.Equals("plaintext")) {
      state = TokenizerState.PlainText;
    }
    Element element = new Element();
    element.setLocalName("html");
    element.setNamespace(HTML_NAMESPACE);
    document.appendChild(element);
    done = false;
    openElements.Clear();
    openElements.Add(element);
    this.context = context;
    resetInsertionMode();
    formElement=(lastForm == null) ? null : ((Element)lastForm);
    if (encoding.getConfidence() != EncodingConfidence.Irrelevant) {
      encoding = new EncodingConfidence(encoding.getEncoding(),
          EncodingConfidence.Irrelevant);
    }
    parse();
    return new List<Node>(element.getChildNodesInternal());
  }

  public IList<Node> parseFragment(string contextName) {
    Element element = new Element();
    element.setLocalName(contextName);
    element.setNamespace(HTML_NAMESPACE);
    return parseFragment(element);
  }

  internal int parserRead() {
    int token = parserReadInternal();
    //Console.WriteLine("token=%08X [%c]",token,token&0xFF);
    if (decoder.isError()) {
      error = true;
    }
    return token;
  }

  private int parserReadInternal() {
    if (tokenQueue.Count>0) {
 return removeAtIndex(tokenQueue, 0);
}
    while (true) {
      //Console.WriteLine(state);
      switch(state) {
      case TokenizerState.Data:
        int c = charInput.read();
        if (c == 0x26) {
          state = TokenizerState.CharacterRefInData;
        } else if (c == 0x3c) {
          state = TokenizerState.TagOpen;
        } else if (c == 0) {
          error = true;
          return c;
        } else if (c< 0) {
 return TOKEN_EOF;
} else {
          int ret = c;
          // Keep reading characters to
          // reduce the need to re-call
          // this method
          int mark = charInput.setSoftMark();
          for (int i = 0; i < 100; ++i) {
            c = charInput.read();
            if (c>0 && c != 0x26 && c != 0x3c) {
              tokenQueue.Add(c);
            } else {
              charInput.setMarkPosition(mark + i);
              break;
            }
          }
          return ret;
        }
        break;
      case TokenizerState.CharacterRefInData:{
        state = TokenizerState.Data;
        int charref = parseCharacterReference(-1);
        if (charref< 0) {
          // more than one character in this reference
          int index = Math.Abs(charref + 1);
          tokenQueue.Add(HtmlEntities.entityDoubles[index*2 + 1]);
          return HtmlEntities.entityDoubles[index*2];
        }
        return charref;
      }
      case TokenizerState.CharacterRefInRcData:{
        state = TokenizerState.RcData;
        int charref = parseCharacterReference(-1);
        if (charref< 0) {
          // more than one character in this reference
          int index = Math.Abs(charref + 1);
          tokenQueue.Add(HtmlEntities.entityDoubles[index*2 + 1]);
          return HtmlEntities.entityDoubles[index*2];
        }
        return charref;
      }
      case TokenizerState.RcData:
        int c1 = charInput.read();
        if (c1 == 0x26) {
          state = TokenizerState.CharacterRefInRcData;
        } else if (c1 == 0x3c) {
          state = TokenizerState.RcDataLessThan;
        } else if (c1 == 0) {
          error = true;
          return 0xfffd;
  } else if (c1< 0) {
 return TOKEN_EOF;
} else {
 return c1;
}
        break;
      case TokenizerState.RawText:
      case TokenizerState.ScriptData:{
        int c11 = charInput.read();
        if (c11 == 0x3c) {
          state=(state == TokenizerState.RawText) ?
              TokenizerState.RawTextLessThan :
                TokenizerState.ScriptDataLessThan;
        } else if (c11 == 0) {
          error = true;
          return 0xfffd;
  } else if (c11< 0) {
 return TOKEN_EOF;
} else {
 return c11;
}
        break;
      }
      case TokenizerState.ScriptDataLessThan:{
        charInput.setHardMark();
        int c11 = charInput.read();
        if (c11 == 0x2f) {
          tempBuffer.clearAll();
          state = TokenizerState.ScriptDataEndTagOpen;
        } else if (c11 == 0x21) {
          state = TokenizerState.ScriptDataEscapeStart;
          tokenQueue.Add(0x21);
          return '<';
        } else {
          state = TokenizerState.ScriptData;
          if (c11 >= 0) {
            charInput.moveBack(1);
          }
          return 0x3c;
        }
        break;
      }
      case TokenizerState.ScriptDataEndTagOpen:
      case TokenizerState.ScriptDataEscapedEndTagOpen:{
        charInput.setHardMark();
        int ch = charInput.read();
        if (ch>= 'A' && ch<= 'Z') {
          EndTagToken token = new EndTagToken((char) (ch + 0x20));
          tempBuffer.appendInt(ch);
          currentTag = token;
          currentEndTag = token;
          if (state == TokenizerState.ScriptDataEndTagOpen) {
            state = TokenizerState.ScriptDataEndTagName;
          } else {
            state = TokenizerState.ScriptDataEscapedEndTagName;
          }
        } else if (ch>= 'a' && ch<= 'z') {
          EndTagToken token = new EndTagToken((char)ch);
          tempBuffer.appendInt(ch);
          currentTag = token;
          currentEndTag = token;
          if (state == TokenizerState.ScriptDataEndTagOpen) {
            state = TokenizerState.ScriptDataEndTagName;
          } else {
            state = TokenizerState.ScriptDataEscapedEndTagName;
          }
        } else {
          if (state == TokenizerState.ScriptDataEndTagOpen) {
            state = TokenizerState.ScriptData;
          } else {
            state = TokenizerState.ScriptDataEscaped;
          }
          tokenQueue.Add(0x2f);
          if (ch >= 0) {
            charInput.moveBack(1);
          }
          return 0x3c;
        }
        break;
      }
      case TokenizerState.ScriptDataEndTagName:
      case TokenizerState.ScriptDataEscapedEndTagName:{
        charInput.setHardMark();
        int ch = charInput.read();
        if ((ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) &&
            isAppropriateEndTag()) {
          state = TokenizerState.BeforeAttributeName;
        } else if (ch == 0x2f && isAppropriateEndTag()) {
          state = TokenizerState.SelfClosingStartTag;
        } else if (ch == 0x3e && isAppropriateEndTag()) {
          state = TokenizerState.Data;
          return emitCurrentTag();
        } else if (ch>= 'A' && ch<= 'Z') {
          currentTag.appendChar((char) (ch + 0x20));
          tempBuffer.appendInt(ch);
        } else if (ch>= 'a' && ch<= 'z') {
          currentTag.appendChar((char)ch);
          tempBuffer.appendInt(ch);
        } else {
          if (state == TokenizerState.ScriptDataEndTagName) {
            state = TokenizerState.ScriptData;
          } else {
            state = TokenizerState.ScriptDataEscaped;
          }
          tokenQueue.Add(0x2f);
          int[] array = tempBuffer.array();
          for (int i = 0;i<tempBuffer.Count; ++i) {
            tokenQueue.Add(array[i]);
          }
          if (ch >= 0) {
            charInput.moveBack(1);
          }
          return '<';
        }
        break;
      }
      case TokenizerState.ScriptDataDoubleEscapeStart:{
        charInput.setHardMark();
        int ch = charInput.read();
        if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20 ||
            ch == 0x2f || ch == 0x3e) {
          string bufferString = tempBuffer.ToString();
          if (bufferString.Equals("script")) {
            state = TokenizerState.ScriptDataDoubleEscaped;
          } else {
            state = TokenizerState.ScriptDataEscaped;
          }
          return ch;
        } else if (ch>= 'A' && ch<= 'Z') {
          tempBuffer.appendInt(ch + 0x20);
          return ch;
        } else if (ch>= 'a' && ch<= 'z') {
          tempBuffer.appendInt(ch);
          return ch;
        } else {
          state = TokenizerState.ScriptDataEscaped;
          if (ch >= 0) {
            charInput.moveBack(1);
          }
        }
        break;
      }
      case TokenizerState.ScriptDataDoubleEscapeEnd:{
        charInput.setHardMark();
        int ch = charInput.read();
        if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20 ||
            ch == 0x2f || ch == 0x3e) {
          string bufferString = tempBuffer.ToString();
          if (bufferString.Equals("script")) {
            state = TokenizerState.ScriptDataEscaped;
          } else {
            state = TokenizerState.ScriptDataDoubleEscaped;
          }
          return ch;
        } else if (ch>= 'A' && ch<= 'Z') {
          tempBuffer.appendInt(ch + 0x20);
          return ch;
        } else if (ch>= 'a' && ch<= 'z') {
          tempBuffer.appendInt(ch);
          return ch;
        } else {
          state = TokenizerState.ScriptDataDoubleEscaped;
          if (ch >= 0) {
            charInput.moveBack(1);
          }
        }
        break;
      }
      case TokenizerState.ScriptDataEscapeStart:
      case TokenizerState.ScriptDataEscapeStartDash:{
        charInput.setHardMark();
        int ch = charInput.read();
        if (ch == 0x2d) {
          if (state == TokenizerState.ScriptDataEscapeStart) {
            state = TokenizerState.ScriptDataEscapeStartDash;
          } else {
            state = TokenizerState.ScriptDataEscapedDashDash;
          }
          return '-';
        } else {
          if (ch >= 0) {
            charInput.moveBack(1);
          }
          state = TokenizerState.ScriptData;
        }
        break;
      }
      case TokenizerState.ScriptDataEscaped:{
        int ch = charInput.read();
        if (ch == 0x2d) {
          state = TokenizerState.ScriptDataEscapedDash;
          return '-';
        } else if (ch == 0x3c) {
          state = TokenizerState.ScriptDataEscapedLessThan;
        } else if (ch == 0) {
          error = true;
          return 0xfffd;
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
 return ch;
}
        break;
      }
      case TokenizerState.ScriptDataDoubleEscaped:{
        int ch = charInput.read();
        if (ch == 0x2d) {
          state = TokenizerState.ScriptDataDoubleEscapedDash;
          return '-';
        } else if (ch == 0x3c) {
          state = TokenizerState.ScriptDataDoubleEscapedLessThan;
          return '<';
        } else if (ch == 0) {
          error = true;
          return 0xfffd;
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
 return ch;
}
        break;
      }
      case TokenizerState.ScriptDataEscapedDash:{
        int ch = charInput.read();
        if (ch == 0x2d) {
          state = TokenizerState.ScriptDataEscapedDashDash;
          return '-';
        } else if (ch == 0x3c) {
          state = TokenizerState.ScriptDataEscapedLessThan;
        } else if (ch == 0) {
          error = true;
          state = TokenizerState.ScriptDataEscaped;
          return 0xfffd;
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          state = TokenizerState.ScriptDataEscaped;
          return ch;
        }
        break;
      }
      case TokenizerState.ScriptDataDoubleEscapedDash:{
        int ch = charInput.read();
        if (ch == 0x2d) {
          state = TokenizerState.ScriptDataDoubleEscapedDashDash;
          return '-';
        } else if (ch == 0x3c) {
          state = TokenizerState.ScriptDataDoubleEscapedLessThan;
          return '<';
        } else if (ch == 0) {
          error = true;
          state = TokenizerState.ScriptDataDoubleEscaped;
          return 0xfffd;
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          state = TokenizerState.ScriptDataDoubleEscaped;
          return ch;
        }
        break;
      }
      case TokenizerState.ScriptDataEscapedDashDash:{
        int ch = charInput.read();
        if (ch == 0x2d) {
 return '-';
  } else if (ch == 0x3c) {
          state = TokenizerState.ScriptDataEscapedLessThan;
        } else if (ch == 0x3e) {
          state = TokenizerState.ScriptData;
          return '>';
        } else if (ch == 0) {
          error = true;
          state = TokenizerState.ScriptDataEscaped;
          return 0xfffd;
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          state = TokenizerState.ScriptDataEscaped;
          return ch;
        }
        break;
      }
      case TokenizerState.ScriptDataDoubleEscapedDashDash:{
        int ch = charInput.read();
        if (ch == 0x2d) {
 return '-';
  } else if (ch == 0x3c) {
          state = TokenizerState.ScriptDataDoubleEscapedLessThan;
          return '<';
        } else if (ch == 0x3e) {
          state = TokenizerState.ScriptData;
          return '>';
        } else if (ch == 0) {
          error = true;
          state = TokenizerState.ScriptDataDoubleEscaped;
          return 0xfffd;
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          state = TokenizerState.ScriptDataDoubleEscaped;
          return ch;
        }
        break;
      }
      case TokenizerState.ScriptDataDoubleEscapedLessThan:{
        charInput.setHardMark();
        int ch = charInput.read();
        if (ch == 0x2f) {
          tempBuffer.clearAll();
          state = TokenizerState.ScriptDataDoubleEscapeEnd;
          return 0x2f;
        } else {
          state = TokenizerState.ScriptDataDoubleEscaped;
          if (ch >= 0) {
            charInput.moveBack(1);
          }
        }
        break;
      }
      case TokenizerState.ScriptDataEscapedLessThan:{
        charInput.setHardMark();
        int ch = charInput.read();
        if (ch == 0x2f) {
          tempBuffer.clearAll();
          state = TokenizerState.ScriptDataEscapedEndTagOpen;
        } else if (ch>= 'A' && ch<= 'Z') {
          tempBuffer.clearAll();
          tempBuffer.appendInt(ch + 0x20);
          state = TokenizerState.ScriptDataDoubleEscapeStart;
          tokenQueue.Add(ch);
          return 0x3c;
        } else if (ch>= 'a' && ch<= 'z') {
          tempBuffer.clearAll();
          tempBuffer.appendInt(ch);
          state = TokenizerState.ScriptDataDoubleEscapeStart;
          tokenQueue.Add(ch);
          return 0x3c;
        } else {
          state = TokenizerState.ScriptDataEscaped;
          if (ch >= 0) {
            charInput.moveBack(1);
          }
          return 0x3c;
        }
        break;
      }
      case TokenizerState.PlainText:{
        int c11 = charInput.read();
        if (c11 == 0) {
          error = true;
          return 0xfffd;
  } else if (c11< 0) {
 return TOKEN_EOF;
} else {
 return c11;
}
      }
      case TokenizerState.TagOpen:{
        charInput.setHardMark();
        int c11 = charInput.read();
        if (c11 == 0x21) {
          state = TokenizerState.MarkupDeclarationOpen;
        } else if (c11 == 0x2f) {
          state = TokenizerState.EndTagOpen;
        } else if (c11>= 'A' && c11<= 'Z') {
          TagToken token = new StartTagToken((char) (c11 + 0x20));
          currentTag = token;
          state = TokenizerState.TagName;
  } else if (c11>= 'a' && c11<= 'z') {
          TagToken token = new StartTagToken((char) (c11));
          currentTag = token;
          state = TokenizerState.TagName;
  } else if (c11 == 0x3f) {
          error = true;
          bogusCommentCharacter = c11;
          state = TokenizerState.BogusComment;
        } else {
          error = true;
          state = TokenizerState.Data;
          if (c11 >= 0) {
            charInput.moveBack(1);
          }
          return '<';
        }
        break;
      }
      case TokenizerState.EndTagOpen:{
        int ch = charInput.read();
        if (ch>= 'A' && ch<= 'Z') {
          TagToken token = new EndTagToken((char) (ch + 0x20));
          currentEndTag = token;
          currentTag = token;
          state = TokenizerState.TagName;
  } else if (ch>= 'a' && ch<= 'z') {
          TagToken token = new EndTagToken((char) (ch));
          currentEndTag = token;
          currentTag = token;
          state = TokenizerState.TagName;
  } else if (ch == 0x3e) {
          error = true;
          state = TokenizerState.Data;
  } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
          tokenQueue.Add(0x2f);  // solidus
          return 0x3c;  // Less than
        } else {
          error = true;
          bogusCommentCharacter = ch;
          state = TokenizerState.BogusComment;
        }
        break;
      }
      case TokenizerState.RcDataEndTagOpen:
      case TokenizerState.RawTextEndTagOpen:{
        charInput.setHardMark();
        int ch = charInput.read();
        if (ch>= 'A' && ch<= 'Z') {
          TagToken token = new EndTagToken((char) (ch + 0x20));
          tempBuffer.appendInt(ch);
          currentEndTag = token;
          currentTag = token;
          state=(state == TokenizerState.RcDataEndTagOpen) ?
              TokenizerState.RcDataEndTagName :
                TokenizerState.RawTextEndTagName;
  } else if (ch>= 'a' && ch<= 'z') {
          TagToken token = new EndTagToken((char) (ch));
          tempBuffer.appendInt(ch);
          currentEndTag = token;
          currentTag = token;
          state=(state == TokenizerState.RcDataEndTagOpen) ?
              TokenizerState.RcDataEndTagName :
                TokenizerState.RawTextEndTagName;
        } else {
          if (ch >= 0) {
            charInput.moveBack(1);
          }
          state = TokenizerState.RcData;
          tokenQueue.Add(0x2f);  // solidus
          return 0x3c;  // Less than
        }
        break;
      }
      case TokenizerState.RcDataEndTagName:
      case TokenizerState.RawTextEndTagName:{
        charInput.setHardMark();
        int ch = charInput.read();
   if ((ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) &&
          isAppropriateEndTag()) {
          state = TokenizerState.BeforeAttributeName;
        } else if (ch == 0x2f && isAppropriateEndTag()) {
          state = TokenizerState.SelfClosingStartTag;
        } else if (ch == 0x3e && isAppropriateEndTag()) {
          state = TokenizerState.Data;
          return emitCurrentTag();
        } else if (ch>= 'A' && ch<= 'Z') {
          currentTag.append(ch + 0x20);
          tempBuffer.appendInt(ch + 0x20);
        } else if (ch>= 'a' && ch<= 'z') {
          currentTag.append(ch);
          tempBuffer.appendInt(ch);
        } else {
          if (ch >= 0) {
            charInput.moveBack(1);
          }
          state=(state == TokenizerState.RcDataEndTagName) ?
              TokenizerState.RcData : TokenizerState.RawText;
          tokenQueue.Add(0x2f);  // solidus
          int[] array = tempBuffer.array();
          for (int i = 0;i<tempBuffer.Count; ++i) {
            tokenQueue.Add(array[i]);
          }
          return 0x3c;  // Less than
        }
        break;
      }
      case TokenizerState.BeforeAttributeName:{
        int ch = charInput.read();
        if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
          // ignored
        } else if (ch == 0x2f) {
          state = TokenizerState.SelfClosingStartTag;
        } else if (ch == 0x3e) {
          state = TokenizerState.Data;
          return emitCurrentTag();
        } else if (ch>= 'A' && ch<= 'Z') {
          currentAttribute = currentTag.addAttribute((char)(ch + 0x20));
          state = TokenizerState.AttributeName;
        } else if (ch == 0) {
          error = true;
          currentAttribute = currentTag.addAttribute((char)(0xfffd));
          state = TokenizerState.AttributeName;
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          if (ch == 0x22 || ch == 0x27 || ch == 0x3c || ch == 0x3d) {
            error = true;
          }
          currentAttribute = currentTag.addAttribute(ch);
          state = TokenizerState.AttributeName;
        }
        break;
      }
      case TokenizerState.AttributeName:{
        int ch = charInput.read();
        if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
          if (!currentTag.checkAttributeName()) {
            error = true;
          }
          state = TokenizerState.AfterAttributeName;
        } else if (ch == 0x2f) {
          if (!currentTag.checkAttributeName()) {
            error = true;
          }
          state = TokenizerState.SelfClosingStartTag;
        } else if (ch == 0x3d) {
          if (!currentTag.checkAttributeName()) {
            error = true;
          }
          state = TokenizerState.BeforeAttributeValue;
        } else if (ch == 0x3e) {
          if (!currentTag.checkAttributeName()) {
            error = true;
          }
          state = TokenizerState.Data;
          return emitCurrentTag();
        } else if (ch>= 'A' && ch<= 'Z') {
          currentAttribute.appendToName(ch + 0x20);
        } else if (ch == 0) {
          error = true;
          currentAttribute.appendToName(0xfffd);
        } else if (ch< 0) {
          error = true;
          if (!currentTag.checkAttributeName()) {
            error = true;
          }
          state = TokenizerState.Data;
        } else if (ch == 0x22 || ch == 0x27 || ch == 0x3c) {
          error = true;
          currentAttribute.appendToName(ch);
        } else {
          currentAttribute.appendToName(ch);
        }
        break;
      }
      case TokenizerState.AfterAttributeName:{
        int ch = charInput.read();
        while (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
          ch = charInput.read();
        }
        if (ch == 0x2f) {
          state = TokenizerState.SelfClosingStartTag;
        } else if (ch=='=') {
          state = TokenizerState.BeforeAttributeValue;
        } else if (ch=='>') {
          state = TokenizerState.Data;
          return emitCurrentTag();
        } else if (ch>= 'A' && ch<= 'Z') {
          currentAttribute = currentTag.addAttribute((char)(ch + 0x20));
          state = TokenizerState.AttributeName;
        } else if (ch == 0) {
          error = true;
          currentAttribute = currentTag.addAttribute((char)(0xfffd));
          state = TokenizerState.AttributeName;
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          if (ch == 0x22 || ch == 0x27 || ch == 0x3c) {
            error = true;
          }
          currentAttribute = currentTag.addAttribute(ch);
          state = TokenizerState.AttributeName;
        }
        break;
      }
      case TokenizerState.BeforeAttributeValue:{
        charInput.setHardMark();
        int ch = charInput.read();
        while (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
          ch = charInput.read();
        }
        if (ch == 0x22) {
          state = TokenizerState.AttributeValueDoubleQuoted;
        } else if (ch == 0x26) {
          charInput.moveBack(1);
          state = TokenizerState.AttributeValueUnquoted;
        } else if (ch == 0x27) {
          state = TokenizerState.AttributeValueSingleQuoted;
        } else if (ch == 0) {
          error = true;
          currentAttribute.appendToValue(0xfffd);
          state = TokenizerState.AttributeValueUnquoted;
        } else if (ch == 0x3e) {
          error = true;
          state = TokenizerState.Data;
          return emitCurrentTag();
        } else if (ch == 0x3c || ch == 0x3d || ch == 0x60) {
          error = true;
          currentAttribute.appendToValue(ch);
          state = TokenizerState.AttributeValueUnquoted;
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          currentAttribute.appendToValue(ch);
          state = TokenizerState.AttributeValueUnquoted;
        }
        break;
      }
      case TokenizerState.AttributeValueDoubleQuoted:{
        int ch = charInput.read();
        if (ch == 0x22) {
          currentAttribute.commitValue();
          state = TokenizerState.AfterAttributeValueQuoted;
        } else if (ch == 0x26) {
          lastState = state;
          state = TokenizerState.CharacterRefInAttributeValue;
        } else if (ch == 0) {
          error = true;
          currentAttribute.appendToValue(0xfffd);
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          currentAttribute.appendToValue(ch);
          // Keep reading characters to
          // reduce the need to re-call
          // this method
          int mark = charInput.setSoftMark();
          for (int i = 0; i < 100; ++i) {
            ch = charInput.read();
            if (ch>0 && ch != 0x26 && ch != 0x22) {
              currentAttribute.appendToValue(ch);
            } else if (ch == 0x22) {
              currentAttribute.commitValue();
              state = TokenizerState.AfterAttributeValueQuoted;
              break;
            } else {
              charInput.setMarkPosition(mark + i);
              break;
            }
          }
        }
        break;
      }
      case TokenizerState.AttributeValueSingleQuoted:{
        int ch = charInput.read();
        if (ch == 0x27) {
          currentAttribute.commitValue();
          state = TokenizerState.AfterAttributeValueQuoted;
        } else if (ch == 0x26) {
          lastState = state;
          state = TokenizerState.CharacterRefInAttributeValue;
        } else if (ch == 0) {
          error = true;
          currentAttribute.appendToValue(0xfffd);
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          currentAttribute.appendToValue(ch);
          // Keep reading characters to
          // reduce the need to re-call
          // this method
          int mark = charInput.setSoftMark();
          for (int i = 0; i < 100; ++i) {
            ch = charInput.read();
            if (ch>0 && ch != 0x26 && ch != 0x27) {
              currentAttribute.appendToValue(ch);
            } else if (ch == 0x27) {
              currentAttribute.commitValue();
              state = TokenizerState.AfterAttributeValueQuoted;
              break;
            } else {
              charInput.setMarkPosition(mark + i);
              break;
            }
          }
        }
        break;
      }
      case TokenizerState.AttributeValueUnquoted:{
        int ch = charInput.read();
        if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
          currentAttribute.commitValue();
          state = TokenizerState.BeforeAttributeName;
        } else if (ch == 0x26) {
          lastState = state;
          state = TokenizerState.CharacterRefInAttributeValue;
        } else if (ch == 0x3e) {
          currentAttribute.commitValue();
          state = TokenizerState.Data;
          return emitCurrentTag();
        } else if (ch == 0) {
          error = true;
          currentAttribute.appendToValue(0xfffd);
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          if (ch == 0x22||ch == 0x27||ch == 0x3c||ch == 0x3d||ch == 0x60) {
            error = true;
          }
          currentAttribute.appendToValue(ch);
        }
        break;
      }
      case TokenizerState.AfterAttributeValueQuoted:{
        int mark = charInput.setSoftMark();
        int ch = charInput.read();
        if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
          state = TokenizerState.BeforeAttributeName;
        } else if (ch == 0x2f) {
          state = TokenizerState.SelfClosingStartTag;
        } else if (ch == 0x3e) {
          state = TokenizerState.Data;
          return emitCurrentTag();
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          error = true;
          state = TokenizerState.BeforeAttributeName;
          charInput.setMarkPosition(mark);
        }
        break;
      }
      case TokenizerState.SelfClosingStartTag:{
        int mark = charInput.setSoftMark();
        int ch = charInput.read();
        if (ch == 0x3e) {
          currentTag.setSelfClosing(true);
          state = TokenizerState.Data;
          return emitCurrentTag();
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          error = true;
          state = TokenizerState.BeforeAttributeName;
          charInput.setMarkPosition(mark);
        }
        break;
      }
      case TokenizerState.MarkupDeclarationOpen:{
        int mark = charInput.setSoftMark();
        int ch = charInput.read();
        if (ch=='-' && charInput.read()=='-') {
          CommentToken token = new CommentToken();
          lastComment = token;
          state = TokenizerState.CommentStart;
          break;
        } else if (ch=='D' || ch=='d') {
          if (((ch=charInput.read())=='o' || ch=='O') &&
              ((ch=charInput.read())=='c' || ch=='C') &&
              ((ch=charInput.read())=='t' || ch=='T') &&
              ((ch=charInput.read())=='y' || ch=='Y') &&
              ((ch=charInput.read())=='p' || ch=='P') &&
              ((ch=charInput.read())=='e' || ch=='E')) {
            state = TokenizerState.DocType;
            break;
          }
        } else if (ch=='[' && true) {
          if (charInput.read()=='C' && charInput.read()=='D' &&
              charInput.read()=='A' && charInput.read()=='T' &&
              charInput.read()=='A' && charInput.read()=='[' &&
              getCurrentNode() != null &&
              !HTML_NAMESPACE.Equals(getCurrentNode().getNamespaceURI())
) {
            state = TokenizerState.CData;
            break;
          }
        }
        error = true;
        charInput.setMarkPosition(mark);
        bogusCommentCharacter=-1;
        state = TokenizerState.BogusComment;
        break;
      }
      case TokenizerState.CommentStart:{
        int ch = charInput.read();
        if (ch=='-') {
          state = TokenizerState.CommentStartDash;
        } else if (ch == 0) {
          error = true;
          lastComment.appendChar(0xfffd);
          state = TokenizerState.Comment;
        } else if (ch == 0x3e || ch< 0) {
          error = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|lastComment.getType();
          tokens.Add(lastComment);
          return ret;
        } else {
          lastComment.appendChar(ch);
          state = TokenizerState.Comment;
        }
        break;
      }
      case TokenizerState.CommentStartDash:{
        int ch = charInput.read();
        if (ch=='-') {
          state = TokenizerState.CommentEnd;
        } else if (ch == 0) {
          error = true;
          lastComment.appendChar('-');
          lastComment.appendChar(0xfffd);
          state = TokenizerState.Comment;
        } else if (ch == 0x3e || ch< 0) {
          error = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|lastComment.getType();
          tokens.Add(lastComment);
          return ret;
        } else {
          lastComment.appendChar('-');
          lastComment.appendChar(ch);
          state = TokenizerState.Comment;
        }
        break;
      }
      case TokenizerState.Comment:{
        int ch = charInput.read();
        if (ch=='-') {
          state = TokenizerState.CommentEndDash;
        } else if (ch == 0) {
          error = true;
          lastComment.appendChar(0xfffd);
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|lastComment.getType();
          tokens.Add(lastComment);
          return ret;
        } else {
          lastComment.appendChar(ch);
        }
        break;
      }
      case TokenizerState.CommentEndDash:{
        int ch = charInput.read();
        if (ch=='-') {
          state = TokenizerState.CommentEnd;
        } else if (ch == 0) {
          error = true;
          lastComment.appendChar('-');
          lastComment.appendChar(0xfffd);
          state = TokenizerState.Comment;
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|lastComment.getType();
          tokens.Add(lastComment);
          return ret;
        } else {
          lastComment.appendChar('-');
          lastComment.appendChar(ch);
          state = TokenizerState.Comment;
        }
        break;
      }
      case TokenizerState.CommentEnd:{
        int ch = charInput.read();
        if (ch == 0x3e) {
          state = TokenizerState.Data;
          int ret = tokens.Count|lastComment.getType();
          tokens.Add(lastComment);
          return ret;
        } else if (ch == 0) {
          error = true;
          lastComment.appendChar('-');
          lastComment.appendChar('-');
          lastComment.appendChar(0xfffd);
          state = TokenizerState.Comment;
        } else if (ch == 0x21) {  // --!>
          error = true;
          state = TokenizerState.CommentEndBang;
        } else if (ch == 0x2d) {
          error = true;
          lastComment.appendChar('-');
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|lastComment.getType();
          tokens.Add(lastComment);
          return ret;
        } else {
          error = true;
          lastComment.appendChar('-');
          lastComment.appendChar('-');
          lastComment.appendChar(ch);
          state = TokenizerState.Comment;
        }
        break;
      }
      case TokenizerState.CommentEndBang:{
        int ch = charInput.read();
        if (ch == 0x3e) {
          state = TokenizerState.Data;
          int ret = tokens.Count|lastComment.getType();
          tokens.Add(lastComment);
          return ret;
        } else if (ch == 0) {
          error = true;
          lastComment.appendChar('-');
          lastComment.appendChar('-');
          lastComment.appendChar('!');
          lastComment.appendChar(0xfffd);
          state = TokenizerState.Comment;
        } else if (ch == 0x2d) {
          lastComment.appendChar('-');
          lastComment.appendChar('-');
          lastComment.appendChar('!');
          state = TokenizerState.CommentEndDash;
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|lastComment.getType();
          tokens.Add(lastComment);
          return ret;
        } else {
          error = true;
          lastComment.appendChar('-');
          lastComment.appendChar('-');
          lastComment.appendChar('!');
          lastComment.appendChar(ch);
          state = TokenizerState.Comment;
        }
        break;
      }
      case TokenizerState.CharacterRefInAttributeValue:{
        int allowed = 0x3e;
        if (lastState == TokenizerState.AttributeValueDoubleQuoted) {
          allowed='"';
        }
        if (lastState == TokenizerState.AttributeValueSingleQuoted) {
          allowed='\'';
        }
        int ch = parseCharacterReference(allowed);
        if (ch< 0) {
          // more than one character in this reference
          int index = Math.Abs(ch + 1);
          currentAttribute.appendToValue(HtmlEntities.entityDoubles[index*2]);
          currentAttribute.appendToValue(HtmlEntities.entityDoubles[index*2 + 1]);
        } else {
          currentAttribute.appendToValue(ch);
        }
        state = lastState;
        break;
      }
      case TokenizerState.TagName:{
        int ch = charInput.read();
        if (ch == 0x09 || ch == 0x0a || ch == 0x0c || ch == 0x20) {
          state = TokenizerState.BeforeAttributeName;
        } else if (ch == 0x2f) {
          state = TokenizerState.SelfClosingStartTag;
        } else if (ch == 0x3e) {
          state = TokenizerState.Data;
          return emitCurrentTag();
        } else if (ch>= 'A' && ch<= 'Z') {
          currentTag.appendChar((char)(ch + 0x20));
        } else if (ch == 0) {
          error = true;
          currentTag.appendChar((char)0xfffd);
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
        } else {
          currentTag.append(ch);
        }
        break;
      }
      case TokenizerState.RawTextLessThan:{
        charInput.setHardMark();
        int ch = charInput.read();
        if (ch == 0x2f) {
          tempBuffer.clearAll();
          state = TokenizerState.RawTextEndTagOpen;
        } else {
          state = TokenizerState.RawText;
          if (ch >= 0) {
            charInput.moveBack(1);
          }
          return 0x3c;
        }
        break;
      }
      case TokenizerState.BogusComment:{
        CommentToken comment = new CommentToken();
        if (bogusCommentCharacter >= 0) {
 comment.appendChar(bogusCommentCharacter == 0 ? 0xfffd :
            bogusCommentCharacter);
        }
        while (true) {
          int ch = charInput.read();
          if (ch<0 || ch=='>') {
            break;
          }
          if (ch == 0) {
            ch = 0xfffd;
          }
          comment.appendChar(ch);
        }
        int ret = tokens.Count|comment.getType();
        tokens.Add(comment);
        state = TokenizerState.Data;
        return ret;
      }
      case TokenizerState.DocType:{
        charInput.setHardMark();
        int ch = charInput.read();
        if (ch == 0x09||ch == 0x0a||ch == 0x0c||ch == 0x20) {
          state = TokenizerState.BeforeDocTypeName;
        } else if (ch< 0) {
          error = true;
          state = TokenizerState.Data;
          DocTypeToken token = new DocTypeToken();
          token.forceQuirks = true;
          int ret = tokens.Count|token.getType();
          tokens.Add(token);
          return ret;
        } else {
          error = true;
          charInput.moveBack(1);
          state = TokenizerState.BeforeDocTypeName;
        }
        break;
      }
      case TokenizerState.BeforeDocTypeName:{
        int ch = charInput.read();
        if (ch == 0x09||ch == 0x0a||ch == 0x0c||ch == 0x20) {
          break;
        } else if (ch>= 'A' && ch<= 'Z') {
          docTypeToken = new DocTypeToken();
          docTypeToken.name = new IntList();
          docTypeToken.name.appendInt(ch + 0x20);
          state = TokenizerState.DocTypeName;
        } else if (ch == 0) {
          error = true;
          docTypeToken = new DocTypeToken();
          docTypeToken.name = new IntList();
          docTypeToken.name.appendInt(0xfffd);
          state = TokenizerState.DocTypeName;
        } else if (ch == 0x3e || ch< 0) {
          error = true;
          state = TokenizerState.Data;
          DocTypeToken token = new DocTypeToken();
          token.forceQuirks = true;
          int ret = tokens.Count|token.getType();
          tokens.Add(token);
          return ret;
        } else {
          docTypeToken = new DocTypeToken();
          docTypeToken.name = new IntList();
          docTypeToken.name.appendInt(ch);
          state = TokenizerState.DocTypeName;
        }
        break;
      }
      case TokenizerState.DocTypeName:{
        int ch = charInput.read();
        if (ch == 0x09||ch == 0x0a||ch == 0x0c||ch == 0x20) {
          state = TokenizerState.AfterDocTypeName;
        } else if (ch == 0x3e) {
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        } else if (ch>= 'A' && ch<= 'Z') {
          docTypeToken.name.appendInt(ch + 0x20);
        } else if (ch == 0) {
          error = true;
          docTypeToken.name.appendInt(0xfffd);
        } else if (ch< 0) {
          error = true;
          docTypeToken.forceQuirks = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        } else {
          docTypeToken.name.appendInt(ch);
        }
        break;
      }
      case TokenizerState.AfterDocTypeName:{
        int ch = charInput.read();
        if (ch == 0x09||ch == 0x0a||ch == 0x0c||ch == 0x20) {
          break;
        } else if (ch == 0x3e) {
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        } else if (ch< 0) {
          error = true;
          docTypeToken.forceQuirks = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        } else {
          int ch2 = 0;
          int pos = charInput.setSoftMark();
          if (ch=='P' || ch=='p') {
            if (((ch2=charInput.read())=='u' || ch2=='U') &&
                ((ch2=charInput.read())=='b' || ch2=='B') &&
                ((ch2=charInput.read())=='l' || ch2=='L') &&
                ((ch2=charInput.read())=='i' || ch2=='I') &&
                ((ch2=charInput.read())=='c' || ch2=='C')
) {
              state = TokenizerState.AfterDocTypePublic;
            } else {
              error = true;
              charInput.setMarkPosition(pos);
              docTypeToken.forceQuirks = true;
              state = TokenizerState.BogusDocType;
            }
          } else if (ch=='S' || ch=='s') {
            if (((ch2=charInput.read())=='y' || ch2=='Y') &&
                ((ch2=charInput.read())=='s' || ch2=='S') &&
                ((ch2=charInput.read())=='t' || ch2=='T') &&
                ((ch2=charInput.read())=='e' || ch2=='E') &&
                ((ch2=charInput.read())=='m' || ch2=='M')
) {
              state = TokenizerState.AfterDocTypeSystem;
            } else {
              error = true;
              charInput.setMarkPosition(pos);
              docTypeToken.forceQuirks = true;
              state = TokenizerState.BogusDocType;
            }
          } else {
            error = true;
            charInput.setMarkPosition(pos);
            docTypeToken.forceQuirks = true;
            state = TokenizerState.BogusDocType;
          }
        }
        break;
      }
      case TokenizerState.AfterDocTypePublic:
      case TokenizerState.BeforeDocTypePublicID:{
        int ch = charInput.read();
        if (ch == 0x09||ch == 0x0a||ch == 0x0c||ch == 0x20) {
          if (state == TokenizerState.AfterDocTypePublic) {
            state = TokenizerState.BeforeDocTypePublicID;
          }
        } else if (ch == 0x22) {
          docTypeToken.publicID = new IntList();
          if (state == TokenizerState.AfterDocTypePublic) {
            error = true;
          }
          state = TokenizerState.DocTypePublicIDDoubleQuoted;
        } else if (ch == 0x27) {
          docTypeToken.publicID = new IntList();
          if (state == TokenizerState.AfterDocTypePublic) {
            error = true;
          }
          state = TokenizerState.DocTypePublicIDSingleQuoted;
        } else if (ch == 0x3e || ch< 0) {
          error = true;
          docTypeToken.forceQuirks = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        } else {
          error = true;
          docTypeToken.forceQuirks = true;
          state = TokenizerState.BogusDocType;
        }
        break;
      }
      case TokenizerState.AfterDocTypeSystem:
      case TokenizerState.BeforeDocTypeSystemID:{
        int ch = charInput.read();
        if (ch == 0x09||ch == 0x0a||ch == 0x0c||ch == 0x20) {
          if (state == TokenizerState.AfterDocTypeSystem) {
            state = TokenizerState.BeforeDocTypeSystemID;
          }
        } else if (ch == 0x22) {
          docTypeToken.systemID = new IntList();
          if (state == TokenizerState.AfterDocTypeSystem) {
            error = true;
          }
          state = TokenizerState.DocTypeSystemIDDoubleQuoted;
        } else if (ch == 0x27) {
          docTypeToken.systemID = new IntList();
          if (state == TokenizerState.AfterDocTypeSystem) {
            error = true;
          }
          state = TokenizerState.DocTypeSystemIDSingleQuoted;
        } else if (ch == 0x3e || ch< 0) {
          error = true;
          docTypeToken.forceQuirks = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        } else {
          error = true;
          docTypeToken.forceQuirks = true;
          state = TokenizerState.BogusDocType;
        }
        break;
      }
      case TokenizerState.DocTypePublicIDDoubleQuoted:
      case TokenizerState.DocTypePublicIDSingleQuoted:{
        int ch = charInput.read();
     if (ch==(state == TokenizerState.DocTypePublicIDDoubleQuoted ? 0x22 :
          0x27)) {
          state = TokenizerState.AfterDocTypePublicID;
        } else if (ch == 0) {
          error = true;
          docTypeToken.publicID.appendInt(0xfffd);
        } else if (ch == 0x3e || ch< 0) {
          error = true;
          docTypeToken.forceQuirks = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        } else {
          docTypeToken.publicID.appendInt(ch);
        }
        break;
      }
      case TokenizerState.DocTypeSystemIDDoubleQuoted:
      case TokenizerState.DocTypeSystemIDSingleQuoted:{
        int ch = charInput.read();
     if (ch==(state == TokenizerState.DocTypeSystemIDDoubleQuoted ? 0x22 :
          0x27)) {
          state = TokenizerState.AfterDocTypeSystemID;
        } else if (ch == 0) {
          error = true;
          docTypeToken.systemID.appendInt(0xfffd);
        } else if (ch == 0x3e || ch< 0) {
          error = true;
          docTypeToken.forceQuirks = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        } else {
          docTypeToken.systemID.appendInt(ch);
        }
        break;
      }
      case TokenizerState.AfterDocTypePublicID:
      case TokenizerState.BetweenDocTypePublicAndSystem:{
        int ch = charInput.read();
        if (ch == 0x09||ch == 0x0a||ch == 0x0c||ch == 0x20) {
          if (state == TokenizerState.AfterDocTypePublicID) {
            state = TokenizerState.BetweenDocTypePublicAndSystem;
          }
        } else if (ch == 0x3e) {
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        } else if (ch == 0x22) {
          docTypeToken.systemID = new IntList();
          if (state == TokenizerState.AfterDocTypePublicID) {
            error = true;
          }
          state = TokenizerState.DocTypeSystemIDDoubleQuoted;
        } else if (ch == 0x27) {
          docTypeToken.systemID = new IntList();
          if (state == TokenizerState.AfterDocTypePublicID) {
            error = true;
          }
          state = TokenizerState.DocTypeSystemIDSingleQuoted;
        } else if (ch< 0) {
          error = true;
          docTypeToken.forceQuirks = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        } else {
          error = true;
          docTypeToken.forceQuirks = true;
          state = TokenizerState.BogusDocType;
        }
        break;
      }
      case TokenizerState.AfterDocTypeSystemID:{
        int ch = charInput.read();
        if (ch == 0x09||ch == 0x0a||ch == 0x0c||ch == 0x20) {
          break;
        } else if (ch == 0x3e) {
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        } else if (ch< 0) {
          error = true;
          docTypeToken.forceQuirks = true;
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        } else {
          error = true;
          state = TokenizerState.BogusDocType;
        }
        break;
      }
      case TokenizerState.BogusDocType:{
        int ch = charInput.read();
        if (ch == 0x3e || ch< 0) {
          state = TokenizerState.Data;
          int ret = tokens.Count|docTypeToken.getType();
          tokens.Add(docTypeToken);
          return ret;
        }
        break;
      }
      case TokenizerState.CData:{
        IntList buffer = new IntList();
        int phase = 0;
        state = TokenizerState.Data;
        while (true) {
          int ch = charInput.read();
          if (ch< 0) {
            break;
          }
          buffer.appendInt(ch);
          if (phase == 0) {
            if (ch==']') {
              ++phase;
            } else {
              phase = 0;
            }
          } else if (phase == 1) {
            if (ch==']') {
              ++phase;
            } else {
              phase = 0;
            }
          } else if (phase == 2) {
            if (ch=='>') {
              ++phase;
              break;
            } else if (ch==']') {
              phase = 2;
            } else {
              phase = 0;
            }
          }
        }
        int[] arr = buffer.array();
        int size = buffer.Count;
        if (phase == 3) {
          size-=3;  // don't count the ']]>'
        }
        if (size>0) {
          // Emit the tokens
          int ret1 = arr[0];
          for (int i = 1; i < size; ++i) {
            tokenQueue.Add(arr[i]);
          }
          return ret1;
        }
        break;
      }
      case TokenizerState.RcDataLessThan:{
        charInput.setHardMark();
        int ch = charInput.read();
        if (ch == 0x2f) {
          tempBuffer.clearAll();
          state = TokenizerState.RcDataEndTagOpen;
        } else {
          state = TokenizerState.RcData;
          if (ch >= 0) {
            charInput.moveBack(1);
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

  private Element popCurrentNode() {
    return (openElements.Count>0) ?
      (removeAtIndex(openElements, openElements.Count-1)) : (null);
  }

  private void pushFormattingElement(StartTagToken tag) {
    Element element = addHtmlElement(tag);
    int matchingElements = 0;
    int lastMatchingElement=-1;
    string name = element.getLocalName();
    for (int i = formattingElements.Count-1;i >= 0; --i) {
      FormattingElement fe = formattingElements[i];
      if (fe.isMarker()) {
        break;
      }
      if (fe.element.getLocalName().Equals(name) &&
          fe.element.getNamespaceURI().Equals(element.getNamespaceURI())) {
        IList<IAttr> attribs = fe.element.getAttributes();
        IList<IAttr> myAttribs = element.getAttributes();
        if (attribs.Count == myAttribs.Count) {
          bool match = true;
          for (int j = 0;j<myAttribs.Count; ++j) {
            string name1 = myAttribs[j].getName();
            string _namespace = myAttribs[j].getNamespaceURI();
            string value = myAttribs[j].getValue();
            string otherValue = fe.element.getAttributeNS(_namespace, name1);
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
      formattingElements.RemoveAt(lastMatchingElement);
    }
    FormattingElement fe2 = new FormattingElement();
    fe2.marker = false;
    fe2.token = tag;
    fe2.element = element;
    formattingElements.Add(fe2);
  }
  private void reconstructFormatting() {
    if (formattingElements.Count == 0) {
 return;
}
    //Console.WriteLine("reconstructing elements");
    //Console.WriteLine(formattingElements);
    FormattingElement fe = formattingElements[formattingElements.Count-1];
    if (fe.isMarker() || openElements.Contains(fe.element)) {
 return;
}
    int i = formattingElements.Count-1;
    while (i>0) {
      fe = formattingElements[i-1];
      --i;
      if (!fe.isMarker() && !openElements.Contains(fe.element)) {
        continue;
      }
      ++i;
      break;
    }
    for (int j = i;j<formattingElements.Count; ++j) {
      fe = formattingElements[j];
      Element element = addHtmlElement(fe.token);
      fe.element = element;
      fe.marker = false;
    }
  }
  private void removeFormattingElement(Element aElement) {
    FormattingElement f = null;
    foreach (var fe in formattingElements) {
      if (!fe.isMarker() && aElement.Equals(fe.element)) {
        f = fe;
        break;
      }
    }
    if (f != null) {
      formattingElements.Remove(f);
    }
  }
  private void resetInsertionMode() {
    bool last = false;
    for (int i = openElements.Count-1;i >= 0; --i) {
      Element e = openElements[i];
      if (context != null && i == 0) {
        e = context;
        last = true;
      }
      string name = e.getLocalName();
      if (!last && (name.Equals("th") || name.Equals("td"))) {
        insertionMode = InsertionMode.InCell;
        break;
      }
      if (name.Equals("select")) {
        insertionMode = InsertionMode.InSelect;
        break;
      }
      if (name.Equals("colgroup")) {
        insertionMode = InsertionMode.InColumnGroup;
        break;
      }
      if (name.Equals("tr")) {
        insertionMode = InsertionMode.InRow;
        break;
      }
      if (name.Equals("caption")) {
        insertionMode = InsertionMode.InCaption;
        break;
      }
      if (name.Equals("table")) {
        insertionMode = InsertionMode.InTable;
        break;
      }
      if (name.Equals("frameset")) {
        insertionMode = InsertionMode.InFrameset;
        break;
      }
      if (name.Equals("html")) {
        insertionMode = InsertionMode.BeforeHead;
        break;
      }
      if (name.Equals("head") || name.Equals("body")) {
        insertionMode = InsertionMode.InBody;
        break;
      }
      if (name.Equals("thead")||name.Equals("tbody")||name.Equals("tfoot")) {
        insertionMode = InsertionMode.InTableBody;
        break;
      }
      if (last) {
        insertionMode = InsertionMode.InBody;
        break;
      }
    }
  }

  internal void setCData() {
    state = TokenizerState.CData;
  }

  internal void setPlainText() {
    state = TokenizerState.PlainText;
  }

  internal void setRawText() {
    state = TokenizerState.RawText;
  }

  internal void setRcData() {
    state = TokenizerState.RcData;
  }

  private void skipLineFeed() {
    int mark = charInput.setSoftMark();
    int nextToken = charInput.read();
    if (nextToken == 0x0a) {
 return;  // ignore the token if it's 0x0A
}
    else if (nextToken == 0x26) {  // start of character reference
      int charref = parseCharacterReference(-1);
      if (charref< 0) {
        // more than one character in this reference
        int index = Math.Abs(charref + 1);
        tokenQueue.Add(HtmlEntities.entityDoubles[index*2]);
        tokenQueue.Add(HtmlEntities.entityDoubles[index*2 + 1]);
      } else if (charref == 0x0a) {
 return;  // ignore the token
}
      else {
        tokenQueue.Add(charref);
      }
    } else {
      // anything else; reset the input stream
      charInput.setMarkPosition(mark);
    }
  }

  private void stopParsing() {
    done = true;
    if (String.IsNullOrEmpty(document.defaultLanguage)) {
      if (contentLanguage.Length == 1) {
        // set the fallback language if there is
        // only one language defined and no meta element
        // defines the language
        document.defaultLanguage = contentLanguage[0];
      }
    }
    document.encoding = encoding.getEncoding();
    string docbase = document.getBaseURI();
    if (docbase == null || docbase.Length == 0) {
      docbase = baseurl;
    } else {
      if (baseurl != null && baseurl.Length>0) {
        document.setBaseURI(HtmlDocument.resolveURL(document, baseurl, docbase));
      }
    }
    openElements.Clear();
    formattingElements.Clear();
  }
}
}
