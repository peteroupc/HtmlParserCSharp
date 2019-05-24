using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Org.System.Xml.Sax;
using Org.System.Xml.Sax.Helpers;
using PeterO.Text;
using com.upokecenter.net;
using com.upokecenter.util
namespace com.upokecenter.html {
internal class XhtmlParser {
  internal class ProcessingInstruction : Node, IProcessingInstruction {
    public string target, data;

  public ProcessingInstruction() : base(NodeType.PROCESSING_INSTRUCTION_NODE) {
    }

    public string getData() {
      return data;
    }

    public string getTarget() {
      return target;
    }
  }
  internal class XhtmlContentHandler : DefaultHandler
  {
    private IList<Element> elements;
    private IList<Element> xmlBaseElements;
    private Document document;
    internal string baseurl;
    internal string encoding;
    internal bool useEntities = false;
    public XhtmlContentHandler(XhtmlParser parser) {
      elements = new List<Element>();
      xmlBaseElements = new List<Element>();
    }
    public override void Characters(char[] arg0, int arg1, int arg2) {
      getTextNodeToInsert(getCurrentNode()).text.appendString(new
        String(arg0, arg1, arg2));
    }

    public override void Comment(char[] arg0, int arg1, int arg2) {
      var cmt = new Comment();
      cmt.setData(new String(arg0, arg1, arg2));
      getCurrentNode().appendChild(cmt);
    }

    public override void EndDocument() {
      stopParsing();
    }

    public override void EndElement(string arg0, string arg1, string arg2) {
      elements.RemoveAt(elements.Count-1);
    }

    private Node getCurrentNode() {
        return (elements.Count == 0) ?
          (Node)(document) : (Node)(elements[elements.Count-1]);
    }

    internal Document getDocument() {
      return this.document;
    }

    private string getPrefix(string qname) {
      string prefix="";
      if (qname != null) {
        int prefixIndex=qname.IndexOf(':');
        if (prefixIndex >= 0) {
          prefix = qname.Substring(0, (prefixIndex)-(0));
        }
      }
      return prefix;
    }

    private Text getTextNodeToInsert(Node node) {
      IList<Node> childNodes = node.getChildNodesInternal();
  Node lastChild=(childNodes.Count == 0) ? null :
        childNodes[childNodes.Count-1];
      if (lastChild == null || lastChild.getNodeType() != NodeType.TEXT_NODE) {
        var textNode = new Text();
        node.appendChild(textNode);
        return textNode;
      } else {
 return (Text)lastChild;
}
    }
    public override void IgnorableWhitespace(char[] arg0, int arg1, int arg2) {
      getTextNodeToInsert(getCurrentNode()).text.appendString(new
        String(arg0, arg1, arg2));
    }

    public override void ProcessingInstruction(string arg0, string arg1) {
      var pi = new ProcessingInstruction();
      pi.target = arg0;
      pi.data = arg1;
      getCurrentNode().appendChild(pi);
    }

    public InputSource<Stream> resolveEntity(string name, string publicId,
      string baseURI, string systemId) {
      // Always load a blank external entity
      return new InputSource<Stream>(new MemoryStream(new byte[] { }));
    }

    internal void setDocument(Document doc) {
      this.document = doc;
    }

    public override void SkippedEntity(string arg0) {
      //DebugUtility.Log(arg0);
      if (useEntities) {
        int entity = HtmlEntities.getHtmlEntity(arg0);
          StringBuilder builder = getTextNodeToInsert(getCurrentNode()).text;
        if (entity< 0) {
          int[] twoChars = HtmlEntities.getTwoCharacterEntity(entity);
            if (twoChars[0] <= 0xffff) {
  { builder.Append((char)(twoChars[0]));
}
  } else if (twoChars[0] <= 0x10ffff) {
builder.Append((char)((((twoChars[0] - 0x10000) >> 10) & 0x3ff) + 0xd800));
builder.Append((char)(((twoChars[0] - 0x10000) & 0x3ff) + 0xdc00));
}
            if (twoChars[1] <= 0xffff) {
  { builder.Append((char)(twoChars[1]));
}
  } else if (twoChars[1] <= 0x10ffff) {
builder.Append((char)((((twoChars[1] - 0x10000) >> 10) & 0x3ff) + 0xd800));
builder.Append((char)(((twoChars[1] - 0x10000) & 0x3ff) + 0xdc00));
}
        } else if (entity<0x110000) {
            if (entity <= 0xffff) {
  { builder.Append((char)(entity));
}
  } else if (entity <= 0x10ffff) {
builder.Append((char)((((entity - 0x10000) >> 10) & 0x3ff) + 0xd800));
builder.Append((char)(((entity - 0x10000) & 0x3ff) + 0xdc00));
}
        }
      }
      throw new SaxException("Unrecognized entity: "+arg0);
    }

    public override void StartDtd(string name, string pubid, string sysid) {
        var doctype = new DocumentType();
      doctype.name = name;
      doctype.publicId = pubid;
      doctype.systemId = sysid;
      document.appendChild(doctype);
      if ("-//W3C//DTD XHTML 1.0 Transitional//EN".Equals(pubid) ||
          "-//W3C//DTD XHTML 1.1//EN".Equals(pubid) ||
          "-//W3C//DTD XHTML 1.0 Strict//EN".Equals(pubid) ||
          "-//W3C//DTD XHTML 1.0 Frameset//EN".Equals(pubid) ||
          "-//W3C//DTD XHTML Basic 1.0//EN".Equals(pubid) ||
          "-//W3C//DTD XHTML 1.1 plus MathML 2.0//EN".Equals(pubid) ||
      "-//W3C//DTD XHTML 1.1 plus MathML 2.0 plus SVG 1.1//EN"
            .Equals(pubid) || "-//W3C//DTD MathML 2.0//EN".Equals(pubid) ||
          "-//WAPFORUM//DTD XHTML Mobile 1.0//EN".Equals(pubid)) {
        useEntities = true;
      }
    }

    public override void StartElement(string uri, string localName, string arg2,
        IAttributes arg3) {
      string prefix = getPrefix(arg2);
      var element = new Element();
      element.setLocalName(localName);
      if (prefix.Length>0) {
        element.setPrefix(prefix);
      }
      if (uri != null && uri.Length>0) {
        element.setNamespace(uri);
      }
      getCurrentNode().appendChild(element);
      for (int i = 0;i<arg3.Length; ++i) {
        string _namespace = arg3.GetUri(i);
        var attr = new Attr();
        attr.setName(arg3.GetQName(i));  // Sets prefix and local name
        attr.setNamespace(_namespace);
        attr.setValue(arg3.GetValue(i));
        element.addAttribute(attr);
        if ("xml:base".Equals(arg3.GetQName(i))) {
          xmlBaseElements.Add(element);
        }
      }
      if ("http://www.w3.org/1999/xhtml".Equals(uri) &&
          "base".Equals(localName)) {
        string href=element.getAttributeNS("", "href");
        if (href != null) {
          baseurl = href;
        }
      }
      elements.Add(element);
    }

    private void stopParsing() {
      document.encoding = encoding;
      string docbase = document.getBaseURI();
      if (docbase == null || docbase.Length == 0) {
        docbase = baseurl;
      } else {
        if (baseurl != null && baseurl.Length>0) {
          document.setBaseURI(HtmlDocument.resolveURL(
              document,
              baseurl,
              document.getBaseURI()));
        }
      }
      foreach (var baseElement in xmlBaseElements) {
        string xmlbase=baseElement.getAttribute("xml:base");
        if (!String.IsNullOrEmpty(xmlbase)) {
          baseElement.setBaseURI(xmlbase);
        }
      }
      elements.Clear();
    }
  }
  private static string sniffEncoding(PeterO.Support.InputStream s) {
    var data = new byte[4];
    var count = 0;
    s.mark(data.Length + 2);
    try {
      count = s.Read(data, 0, data.Length);
    } finally {
      s.reset();
    }
    if (count >= 2 && (data[0]&0xff) == 0xfe && (data[1]&0xff) == 0xff) {
 return "utf-16be";
}
    if (count >= 2 && (data[0]&0xff) == 0xff && (data[1]&0xff) == 0xfe) {
 return "utf-16le";
}
    if (count >= 3 && (data[0]&0xff) == 0xef && (data[1]&0xff) == 0xbb &&
        (data[2]&0xff) == 0xbf) {
 return "utf-8";
}
    if (count >= 4 && (data[0]&0xff) == 0x00 && data[1]==0x3c &&
        data[2]==0x00 && data[3]==0x3f) {
 return "utf-16be";
}
    if (count >= 4 && data[0]==0x3c && data[1]==0x00 &&
        data[2]==0x3f && data[3]==0x00) {
 return "utf-16le";
}
    if (count >= 4 && data[0]==0x3c && data[1]==0x3f &&
        data[2]==0x78 && data[3]==0x6d) {  // <?xm
      data = new byte[128];
      s.mark(data.Length + 2);
      try {
        count = s.Read(data, 0, data.Length);
      } finally {
        s.reset();
      }
      var i = 4;
      if (i + 1>count) {
 return "utf-8";
}
      if (data[i++]!='l') {
 return "utf-8";  // l in <?xml
}
      var space = false;
      while (i<count) {
        if (data[i]==0x09||data[i]==0x0a||data[i]==0x0d||data[i]==0x20) {
  { space = true;
} i++; } else {
          break;
        }
      }
      if (!space || i + 7>count) {
 return "utf-8";
}
      if (!(data[i]=='v' && data[i+1]=='e' && data[i+2]=='r' &&
          data[i+3]=='s' && data[i+4]=='i' && data[i+5]=='o' &&
          data[i+6]=='n')) {
 return "utf-8";
}
      i+=7;
      while (i<count) {
        if (data[i]==0x09||data[i]==0x0a||data[i]==0x0d||data[i]==0x20) {
          ++i;
        } else {
          break;
        }
      }
      if (i+1>count || data[i++]!='=') {
 return "utf-8";
}
      while (i<count) {
        if (data[i]==0x09||data[i]==0x0a||data[i]==0x0d||data[i]==0x20) {
          ++i;
        } else {
          break;
        }
      }
      if (i + 1>count) {
 return "utf-8";
}
      int ch = data[i++];
      if (ch!='"' && ch!='\'') {
 return "utf-8";
}
      while (i<count) {
        if (data[i]==ch) {
  { i++;
} break; }
        ++i;
      }
      space = false;
      while (i<count) {
        if (data[i]==0x09||data[i]==0x0a||data[i]==0x0d||data[i]==0x20) {
  { space = true;
} i++; } else {
          break;
        }
      }
      if (i + 8>count) {
 return "utf-8";
}
      if (!(data[i]=='e' && data[i+1]=='n' && data[i+2]=='c' &&
          data[i+3]=='o' && data[i+4]=='d' && data[i+5]=='i' &&
          data[i+6]=='n' && data[i+7]=='g')) {
 return "utf-8";
}
      i+=8;
      while (i<count) {
        if (data[i]==0x09||data[i]==0x0a||data[i]==0x0d||data[i]==0x20) {
          ++i;
        } else {
          break;
        }
      }
      if (i+1>count || data[i++]!='=') {
 return "utf-8";
}
      while (i<count) {
        if (data[i]==0x09||data[i]==0x0a||data[i]==0x0d||data[i]==0x20) {
          ++i;
        } else {
          break;
        }
      }
      if (i + 1>count) {
 return "utf-8";
}
      ch = data[i++];
      if (ch!='"' && ch!='\'') {
 return "utf-8";
}
      var builder = new StringBuilder();
      while (i<count) {
        if (data[i]==ch) {
            string encoding = Encodings.ResolveAlias(builder.ToString());
          if (encoding == null) {
 return null;
}
          return (encoding.Equals("UTF-16LE") || encoding.Equals(
  "UTF-16BE")) ? (null) : (builder.ToString());
        }
        builder.Append((char)data[i]);
        ++i;
      }
      return "UTF-8";
    }
    return "UTF-8";
  }

  private string address;

  private IXmlReader reader;

  private InputSource<Stream> isource;

  private XhtmlContentHandler handler;
  private string encoding;

  private string[] contentLang;
  public XhtmlParser(PeterO.Support.InputStream s, string _string) :
    this(s, _string, null, null) {
  }
  public XhtmlParser(PeterO.Support.InputStream s, string _string, string
    charset) : this(s, _string, charset, null) {
  }

  public XhtmlParser(PeterO.Support.InputStream source, string address,
    string charset, string lang) {
    if (source == null) {
 throw new ArgumentException();
}
    if (address != null && address.Length>0) {
      URL url = URL.parse(address);
      if (url == null || url.getScheme().Length == 0) {
 throw new ArgumentException();
}
    }
    this.contentLang = HeaderParser.getLanguages(lang);
    this.address = address;
    try {
      this.reader = new PeterO.Support.SaxReader();
    } catch (SaxException e) {
      if (e.InnerException is IOException) {
 throw (IOException)(e.InnerException);
}
      throw new IOException("",e);
    }
    handler = new XhtmlContentHandler(this);
    try {
      reader.SetFeature("http://xml.org/sax/features/namespaces",true);
    reader.SetFeature("http://xml.org/sax/features/use-entity-resolver2",
 true);
      reader.SetFeature("http://xml.org/sax/features/namespace-prefixes",true);
      reader.LexicalHandler=(handler);
    } catch (SaxException e) {
      throw new NotSupportedException("",e);
    }
    reader.ContentHandler=(handler);
    reader.EntityResolver=(handler);
    charset = Encodings.ResolveAlias(charset);
    if (charset == null) {
      charset = sniffEncoding(source);
      if (charset == null) {
        charset="utf-8";
      }
    }
            charset = Encodings.ResolveAlias (charset);
    this.isource = new InputSource<Stream>(source);
    this.isource.Encoding=(charset);
    this.encoding = charset;
  }

  public IDocument parse() {
    var doc = new Document();
    doc.address = address;
    handler.baseurl = doc.address;
    handler.setDocument(doc);
    handler.encoding = encoding;
    try {
      reader.Parse(isource);
    } catch (SaxException e) {
      if (e.InnerException is IOException) {
 throw (IOException)(e.InnerException);
}
      throw new IOException("",e);
    }
    if (contentLang.Length == 1) {
      doc.defaultLanguage = contentLang[0];
    }
    return handler.getDocument();
  }
}
}
