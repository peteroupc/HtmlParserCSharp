using System;
using System.Text;

namespace com.upokecenter.html {
internal class Attr : IAttr {
  private StringBuilder valueName;
  private StringBuilder value;
  private string valuePrefix = null;

  private string valueLocalName = null;

  private string valueNameString = null;
  private string valueString = null;
  private string value_namespace = null;

  public Attr() {
    this.valueName = new StringBuilder();
    this.value = new StringBuilder();
  }

  public Attr(Attr attr) {
    this.valueNameString = attr.getName();
    this.valueString = attr.getValue();
    this.valuePrefix = attr.valuePrefix;
    this.valueLocalName = attr.valueLocalName;
    this.value_namespace = attr.value_namespace;
  }

  public Attr(char ch) {
    this.valueName = new StringBuilder();
    this.value = new StringBuilder();
    this.valueName.Append(ch);
  }

  public Attr(int ch) {
    this.valueName = new StringBuilder();
    this.value = new StringBuilder();
    if (ch <= 0xffff) {
  { this.valueName.Append((char)ch);
}
  } else if (ch <= 0x10ffff) {
valueName.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) + 0xd800));
valueName.Append((char)(((ch - 0x10000) & 0x3ff) + 0xdc00));
}
  }

  public Attr(string valueName, string value) {
    this.valueNameString = valueName;
    this.valueString = value;
  }

  internal void appendToName(int ch) {
    if (this.valueNameString != null) {
 throw new InvalidOperationException();
}
    if (ch <= 0xffff) {
  { this.valueName.Append((char)ch);
}
  } else if (ch <= 0x10ffff) {
valueName.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) + 0xd800));
valueName.Append((char)(((ch - 0x10000) & 0x3ff) + 0xdc00));
}
  }

  internal void appendToValue(int ch) {
    if (this.valueString != null) {
 throw new InvalidOperationException();
}
    if (ch <= 0xffff) {
  { this.value.Append((char)ch);
}
  } else if (ch <= 0x10ffff) {
value.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) + 0xd800));
value.Append((char)(((ch - 0x10000) & 0x3ff) + 0xdc00));
}
  }

  internal void commitValue() {
    if (this.value == null) {
 throw new InvalidOperationException();
}
    this.valueString = this.value.ToString();
    this.value = null;
  }

  /* (non-Javadoc)
   * @see com.upokecenter.html.IAttr#getLocalName()
   */
  public string getLocalName() {
  return (this.value_namespace == null) ? this.getName() :
      this.valueLocalName;
  }

  /* (non-Javadoc)
   * @see com.upokecenter.html.IAttr#getName()
   */
  public string getName() {
    return (this.valueNameString != null) ? this.valueNameString :
      this.valueName.ToString();
  }

  /* (non-Javadoc)
   * @see com.upokecenter.html.IAttr#getNamespaceURI()
   */
  public string getNamespaceURI() {
    return this.value_namespace;
  }

  /* (non-Javadoc)
   * @see com.upokecenter.html.IAttr#getPrefix()
   */
  public string getPrefix() {
    return this.valuePrefix;
  }
  /* (non-Javadoc)
   * @see com.upokecenter.html.IAttr#getValue()
   */
  public string getValue() {
  return (this.valueString != null) ? this.valueString :
      this.value.ToString();
  }

    internal bool isAttribute(string attrName, string value_namespace) {
    string thisname = this.getLocalName();
  bool match = attrName == null ? thisname == null : attrName.Equals(thisname);
    if (!match) {
 return false;
}
    match = value_namespace == null ? this.value_namespace == null :
      value_namespace.Equals(this.value_namespace);
    return match;
  }

  internal void setName(string value2) {
    if (value2 == null) {
 throw new ArgumentException();
}
    this.valueNameString = value2;
    this.valueName = null;
  }

  internal void setNamespace(string value) {
    if (value == null) {
 throw new ArgumentException();
}
    this.value_namespace = value;
    this.valueNameString = this.getName();
    int io = this.valueNameString.IndexOf(':');
    if (io >= 1) {
      this.valuePrefix = this.valueNameString.Substring(0, (io)-0);
      this.valueLocalName = this.valueNameString.Substring(io + 1);
    } else {
      this.valuePrefix = String.Empty;
      this.valueLocalName = this.getName();
    }
  }

    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.html.Attr.setPrefix(System.String)"]/*'/>
  public void setPrefix(string attrprefix) {
    this.valuePrefix = attrprefix;
  }

  internal void setValue(string value2) {
    if (value2 == null) {
 throw new ArgumentException();
}
    this.valueString = value2;
    this.value = null;
  }

  public override string ToString() {
    return "[Attribute: " + this.getName() + "=" + this.getValue() + "]";
  }
}
}
