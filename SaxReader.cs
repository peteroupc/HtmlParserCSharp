using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using Org.System.Xml.Sax;
using com.upokecenter.net;
using com.upokecenter.util;

namespace PeterO.Support {
    /// <summary>Not documented yet.</summary>
  public sealed class SaxReader : IXmlReader {
    private IContentHandler valueCh = null;
    private IDtdHandler valueDtdh = null;
    private ILexicalHandler valueLh = null;
    private IDeclHandler valueDeclh = null;
    private IErrorHandler valueErrh = null;
    private IEntityResolver valueEntres = null;

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
    public IContentHandler ContentHandler {
      get {
        return this.valueCh;
      }

      set { this.valueCh = value;
      }
    }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
    public IDtdHandler DtdHandler {
      get {
        return this.valueDtdh;
      }

      set {
        this.valueDtdh = value;
      }
    }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
    public ILexicalHandler LexicalHandler {
      get {
        return this.valueLh;
      }

      set {
        this.valueLh = value;
      }
    }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
    public IDeclHandler DeclHandler {
      get {
        return this.valueDeclh;
      }

      set {
        this.valueDeclh = value;
      }
    }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
    public IEntityResolver EntityResolver {
      get {
        return this.valueEntres;
      }

      set {
        this.valueEntres = value;
      }
    }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
    public IErrorHandler ErrorHandler {
      get {
        return this.valueErrh;
      }

      set {
        this.valueErrh = value;
      }
    }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
    public XmlReaderStatus Status {
      get {
        return this.valueStatus;
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
    public bool GetFeature(string name) {
      throw new NotImplementedException();
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='value'>Not documented yet.</param>
    public void SetFeature(string name, bool value) {
      // Ignore
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
    /// <returns>An IProperty(T) object.</returns>
    public IProperty<T> GetProperty<T>(string name) {
      throw new NotImplementedException();
    }

    private class AttributeList : IAttributes {
      private IList<String[]> attributes;

      private AttributeList(bool blank) {
        this.attributes = new List<String[]>();
      }

      public AttributeList() : this(false) {
}

      public static readonly AttributeList Empty = new AttributeList(true);

      public int Length {
        get {
          return this.attributes.Count;
        }
      }

      public string GetUri(int index) {
 return (index < 0 || index >= this.attributes.Count) ? null :
          this.attributes[index][1];
      }

      public string GetLocalName(int index) {
 return (index < 0 || index >= this.attributes.Count) ? null :
          this.attributes[index][2];
      }

      public string GetQName(int index) {
 return (index < 0 || index >= this.attributes.Count) ? null :
          this.attributes[index][0];
      }

      public string GetType(int index) {
        throw new NotSupportedException();
      }

      public string GetValue(int index) {
 return (index < 0 || index >= this.attributes.Count) ? null :
          this.attributes[index][3];
      }

      public int GetIndex(string uri, string localName) {
        var i = 0;
        foreach (var a in this.attributes) {
          if (a[1].Equals(uri) && a[2].Equals(localName)) {
 return i;
}
        }
        return -1;
      }

      public int GetIndex(string valueQName) {
        var i = 0;
        foreach (var a in this.attributes) {
          if (a[0].Equals(valueQName)) {
 return i;
}
        }
        return -1;
      }

      public string GetType(string uri, string localName) {
        throw new NotSupportedException();
      }

   internal void Add(
  string qname,
  string uri,
  string localName,
  string value) {
        this.attributes.Add(new String[] { qname, uri, localName, value});
      }

      public string GetType(string valueQName) {
        throw new NotSupportedException();
      }

      public string GetValue(string uri, string localName) {
        return this.GetValue(this.GetIndex(uri, localName));
      }

      public string GetValue(string valueQName) {
        return this.GetValue(this.GetIndex(valueQName));
      }

      public bool IsSpecified(int index) {
        throw new NotSupportedException();
      }

      public bool IsSpecified(string valueQName) {
        throw new NotSupportedException();
      }

      public bool IsSpecified(string uri, string localName) {
        throw new NotSupportedException();
      }
    }
    /*
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
    */

    /// <summary>Not documented yet.</summary>
    /// <param name='input'>Not documented yet.</param>
    public void Parse(InputSource input) {
      if (input is InputSource<Stream>) {
        InputSource<Stream> iss = (InputSource<Stream>)input;
        var settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Ignore;
// settings.ProhibitDtd = false;
// settings.XmlResolver = new Resolver();
        XmlReader reader = XmlReader.Create(
      new StreamReader(null, System.Text.Encoding.GetEncoding(input.Encoding)),
      settings);
        char[] charray = null;
        this.valueStatus = XmlReaderStatus.Parsing;
        var startdoc = false;
        try {
          startdoc = true;
          if (this.valueCh != null) {
 this.valueCh.StartDocument();
}
          while (this.valueStatus == XmlReaderStatus.Parsing && reader.Read()) {
             switch (reader.NodeType) {
              case XmlNodeType.Comment:
                charray = reader.Value.ToCharArray();
                if (this.valueLh != null) {
 this.valueLh.Comment(charray, 0, charray.Length);
}
                break;
              case XmlNodeType.ProcessingInstruction:
                if (this.valueCh != null) {
 this.valueCh.ProcessingInstruction(
                  reader.Name,
                  reader.Value);
 }
                break;
              case XmlNodeType.EndElement:
                if (this.valueCh != null) {
 this.valueCh.EndElement(
  reader.NamespaceURI,
  reader.LocalName,
  reader.Name);
 }
                break;
              case XmlNodeType.DocumentType:
                if (this.valueLh != null) {
 this.valueLh.StartDtd(
  reader.Name,
  reader["PUBLIC"],
  reader["SYSTEM"]);
 }
     if (this.valueLh != null && this.valueStatus ==
                  XmlReaderStatus.Parsing) {
 this.valueLh.EndDtd();
}
                break;
              case XmlNodeType.EntityReference:
                if (this.valueCh != null) {
 this.valueCh.SkippedEntity(reader.Name);
}
                break;
              case XmlNodeType.Element:
                if (this.valueCh != null) {
                  AttributeList attr = AttributeList.Empty;
                  int count = reader.AttributeCount;
                  if (count > 0) {
                    attr = new AttributeList();
                    reader.MoveToFirstAttribute();
                    for (int i = 0; i < count; ++i) {
  attr.Add(reader.Name, reader.NamespaceURI, reader.LocalName, reader.Value);
                    reader.MoveToNextAttribute();
                    }
                    reader.MoveToElement();
                  }
                  this.valueCh.StartElement(
  reader.NamespaceURI,
  reader.LocalName,
  reader.Name,
  attr);
                  if (reader.IsEmptyElement) {
                    this.valueCh.EndElement(
  reader.NamespaceURI,
  reader.LocalName,
  reader.Name);
                  }
                }
                break;
              case XmlNodeType.Text:
              case XmlNodeType.SignificantWhitespace:
                charray = reader.Value.ToCharArray();
                if (this.valueCh != null) {
 this.valueCh.Characters(charray, 0, charray.Length);
}
                break;
              case XmlNodeType.Whitespace:
                charray = reader.Value.ToCharArray();
                if (this.valueCh != null) {
 this.valueCh.IgnorableWhitespace(charray, 0, charray.Length);
}
                break;
              case XmlNodeType.CDATA:
                this.valueLh.StartCData();
                charray = reader.Value.ToCharArray();
     if (this.valueCh != null && this.valueStatus ==
                  XmlReaderStatus.Parsing) {
 this.valueCh.Characters(charray, 0, charray.Length);
}
     if (this.valueLh != null && this.valueStatus ==
                  XmlReaderStatus.Parsing) {
 this.valueLh.EndCData();
}
                break;
            }
          }
        } finally {
          if (startdoc && this.valueCh != null) {
 this.valueCh.EndDocument();
}
        }
      }
    }

    private XmlReaderStatus valueStatus;

    /// <summary>Not documented yet.</summary>
    /// <param name='systemId'>Not documented yet.</param>
    public void Parse(string systemId) {
      throw new NotSupportedException();
    }

    /// <summary>Not documented yet.</summary>
    public void Suspend() {
      throw new NotSupportedException();
    }

    /// <summary>Not documented yet.</summary>
    public void Abort() {
      this.valueStatus = XmlReaderStatus.Ready;
    }

    /// <summary>Not documented yet.</summary>
    public void Resume() {
      throw new NotSupportedException();
    }
  }
}
