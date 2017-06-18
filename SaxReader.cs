using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using Org.System.Xml.Sax;
using com.upokecenter.net;
using com.upokecenter.util;

namespace PeterO.Support {
  public sealed class SaxReader : IXmlReader {
    IContentHandler ch = null;
    IDtdHandler dtdh = null;
    ILexicalHandler lh = null;
    IDeclHandler declh = null;
    IErrorHandler errh = null;
    IEntityResolver entres = null;

    public IContentHandler ContentHandler {
      get {
        return ch;
      }
      set { ch = value;
      }
    }

    public IDtdHandler DtdHandler {
      get {
        return dtdh;
      }
      set {
        dtdh = value;
      }
    }

    public ILexicalHandler LexicalHandler {
      get {
        return lh;
      }
      set {
        lh = value;
      }
    }

    public IDeclHandler DeclHandler {
      get {
        return declh;
      }
      set {
        declh = value;
      }
    }

    public IEntityResolver EntityResolver {
      get {
        return entres;
      }
      set {
        entres = value;
      }
    }

    public IErrorHandler ErrorHandler {
      get {
        return errh;
      }
      set {
        errh = value;
      }
    }

    public XmlReaderStatus Status {
      get {
        return status;
      }
    }

    public bool GetFeature(string name) {
      throw new NotImplementedException();
    }

    public void SetFeature(string name, bool value) {
      // Ignore
    }

    public IProperty<T> GetProperty<T>(string name) {
      throw new NotImplementedException();
    }

    private class AttributeList : IAttributes {
      private IList<String[]> attributes;

      private AttributeList(bool blank) {
        if (blank) {
          attributes = new String[0][];
        } else {
          attributes = new List<String[]>();
        }
      }

      public AttributeList() : this(false) {}

      public static readonly AttributeList Empty = new AttributeList(true);

      public int Length {
        get {
          return attributes.Count;
        }
      }

      public string GetUri(int index) {
 return (index<0 || index >= attributes.Count) ? (null) :
          (attributes[index][1]);
      }

      public string GetLocalName(int index) {
 return (index<0 || index >= attributes.Count) ? (null) :
          (attributes[index][2]);
      }

      public string GetQName(int index) {
 return (index<0 || index >= attributes.Count) ? (null) :
          (attributes[index][0]);
      }

      public string GetType(int index) {
        throw new NotSupportedException();
      }

      public string GetValue(int index) {
 return (index<0 || index >= attributes.Count) ? (null) :
          (attributes[index][3]);
      }

      public int GetIndex(string uri, string localName) {
        int i = 0;
        foreach (var a in attributes) {
          if (a[1].Equals(uri) && a[2].Equals(localName)) {
 return i;
}
        }
        return -1;
      }

      public int GetIndex(string qName) {
        int i = 0;
        foreach (var a in attributes) {
          if (a[0].Equals(qName)) {
 return i;
}
        }
        return -1;
      }

      public string GetType(string uri, string localName) {
        throw new NotSupportedException();
      }

   internal void Add(string qname, string uri, string localName, string
        value) {
        attributes.Add(new String[] { qname, uri, localName, value});
      }

      public string GetType(string qName) {
        throw new NotSupportedException();
      }

      public string GetValue(string uri, string localName) {
        return GetValue(GetIndex(uri, localName));
      }

      public string GetValue(string qName) {
        return GetValue(GetIndex(qName));
      }

      public bool IsSpecified(int index) {
        throw new NotSupportedException();
      }

      public bool IsSpecified(string qName) {
        throw new NotSupportedException();
      }

      public bool IsSpecified(string uri, string localName) {
        throw new NotSupportedException();
      }
    }

    public class Resolver : XmlResolver {
      public override object GetEntity(Uri absoluteUri, string role, Type
        ofObjectToReturn) {
        return Stream.Null;
      }

      public override ICredentials Credentials {
        set {
          throw new NotSupportedException();
        }
      }
    }

    public void Parse(InputSource input) {
      if (input is InputSource<Stream>) {
        InputSource<Stream> iss=(InputSource<Stream>)input;
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.ProhibitDtd = false;
        settings.XmlResolver = new Resolver();
        XmlReader reader = XmlReader.Create(
          new StreamReader(new InputStreamWrapper(iss.Source), System.Text.Encoding.GetEncoding(input.Encoding)),
          settings);
        char[] charray = null;
        status = XmlReaderStatus.Parsing;
        bool startdoc = false;
        try {
          startdoc = true;
          if (ch != null) {
 ch.StartDocument();
}
          while (status == XmlReaderStatus.Parsing && reader.Read()) {
            switch(reader.NodeType) {
              case XmlNodeType.Comment:
                charray = reader.Value.ToCharArray();
                if (lh != null) {
 lh.Comment(charray, 0, charray.Length);
}
                break;
              case XmlNodeType.ProcessingInstruction:
                if (ch != null) {
 ch.ProcessingInstruction(
                  reader.Name,
                  reader.Value);
 }
                break;
              case XmlNodeType.EndElement:
                if (ch != null) {
 ch.EndElement(reader.NamespaceURI,
                    reader.LocalName, reader.Name);
 }
                break;
              case XmlNodeType.DocumentType:
                if (lh != null) {
 lh.StartDtd(reader.Name,
                    reader["PUBLIC"],
                    reader["SYSTEM"]);
 }
                if (lh != null && status == XmlReaderStatus.Parsing) {
 lh.EndDtd();
}
                break;
              case XmlNodeType.EntityReference:
                if (ch != null) {
 ch.SkippedEntity(reader.Name);
}
                break;
              case XmlNodeType.Element:
                if (ch != null) {
                  AttributeList attr = AttributeList.Empty;
                  int count = reader.AttributeCount;
                  if (count>0) {
                    attr = new AttributeList();
                    reader.MoveToFirstAttribute();
                    for (int i = 0; i < count; ++i) {
  attr.Add(reader.Name, reader.NamespaceURI, reader.LocalName, reader.Value);
                    reader.MoveToNextAttribute();
                    }
                    reader.MoveToElement();
                  }
                  ch.StartElement(reader.NamespaceURI,
                    reader.LocalName, reader.Name, attr);
                  if (reader.IsEmptyElement) {
                    ch.EndElement(reader.NamespaceURI,
                    reader.LocalName, reader.Name);
                  }
                }
                break;
              case XmlNodeType.Text:
              case XmlNodeType.SignificantWhitespace:
                charray = reader.Value.ToCharArray();
                if (ch != null) {
 ch.Characters(charray, 0, charray.Length);
}
                break;
              case XmlNodeType.Whitespace:
                charray = reader.Value.ToCharArray();
                if (ch != null) {
 ch.IgnorableWhitespace(charray, 0, charray.Length);
}
                break;
              case XmlNodeType.CDATA:
                lh.StartCData();
                charray = reader.Value.ToCharArray();
                if (ch != null && status == XmlReaderStatus.Parsing) {
 ch.Characters(charray, 0, charray.Length);
}
                if (lh != null && status == XmlReaderStatus.Parsing) {
 lh.EndCData();
}
                break;
            }
          }
        } finally {
          if (startdoc && ch != null) {
 ch.EndDocument();
}
        }
      }
    }
    XmlReaderStatus status;

    public void Parse(string systemId) {
      throw new NotSupportedException();
    }

    public void Suspend() {
      throw new NotSupportedException();
    }

    public void Abort() {
      status = XmlReaderStatus.Ready;
    }

    public void Resume() {
      throw new NotSupportedException();
    }
  }
}
