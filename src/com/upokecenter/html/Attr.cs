namespace com.upokecenter.html {
using System;
using System.Text;

internal class Attr : IAttr {
  StringBuilder name;
  StringBuilder value;
  string prefix = null;

  string localName = null;

  string nameString = null;
  string valueString = null;
  string _namespace = null;
  public Attr() {
    name = new StringBuilder();
    value = new StringBuilder();
  }

  public Attr(Attr attr) {
    nameString = attr.getName();
    valueString = attr.getValue();
    prefix = attr.prefix;
    localName = attr.localName;
    _namespace = attr._namespace;
  }
  public Attr(char ch) {
    name = new StringBuilder();
    value = new StringBuilder();
    name.Append(ch);
  }

  public Attr(int ch) {
    name = new StringBuilder();
    value = new StringBuilder();
    if (ch <= 0xffff) { name.Append((char)(ch));
  } else if (ch <= 0x10ffff) {
name.Append((char)((((ch-0x10000) >> 10) & 0x3ff)+0xd800));
name.Append((char)(((ch-0x10000) & 0x3ff)+0xdc00));
}
  }

  public Attr(string name, string value) {
    nameString = name;
    valueString = value;
  }
  internal void appendToName(int ch) {
    if (nameString != null) {
 throw new InvalidOperationException();
}
    if (ch <= 0xffff) { name.Append((char)(ch));
  } else if (ch <= 0x10ffff) {
name.Append((char)((((ch-0x10000) >> 10) & 0x3ff)+0xd800));
name.Append((char)(((ch-0x10000) & 0x3ff)+0xdc00));
}
  }

  internal void appendToValue(int ch) {
    if (valueString != null) {
 throw new InvalidOperationException();
}
    if (ch <= 0xffff) { value.Append((char)(ch));
  } else if (ch <= 0x10ffff) {
value.Append((char)((((ch-0x10000) >> 10) & 0x3ff)+0xd800));
value.Append((char)(((ch-0x10000) & 0x3ff)+0xdc00));
}
  }

  internal void commitValue() {
    if (value == null) {
 throw new InvalidOperationException();
}
    valueString = value.ToString();
    value = null;
  }

  /* (non-Javadoc)
   * @see com.upokecenter.html.IAttr#getLocalName()
   */
  public string getLocalName() {
    return (_namespace == null) ? getName() : localName;
  }

  /* (non-Javadoc)
   * @see com.upokecenter.html.IAttr#getName()
   */
  public string getName() {
    return (nameString != null) ? nameString : name.ToString();
  }

  /* (non-Javadoc)
   * @see com.upokecenter.html.IAttr#getNamespaceURI()
   */
  public string getNamespaceURI() {
    return _namespace;
  }

  /* (non-Javadoc)
   * @see com.upokecenter.html.IAttr#getPrefix()
   */
  public string getPrefix() {
    return prefix;
  }
  /* (non-Javadoc)
   * @see com.upokecenter.html.IAttr#getValue()
   */
  public string getValue() {
    return (valueString != null) ? valueString : value.ToString();
  }

  internal bool isAttribute(string name, string _namespace) {
    string thisname = getLocalName();
    bool match=(name == null ? thisname == null : name.Equals(thisname));
    if (!match) {
 return false;
}
    match=(_namespace == null ? this._namespace == null :
      _namespace.Equals(this._namespace));
    return match;
  }

  internal void setName(string value2) {
    if (value2 == null) {
 throw new ArgumentException();
}
    nameString = value2;
    name = null;
  }

  internal void setNamespace(string value) {
    if (value == null) {
 throw new ArgumentException();
}
    _namespace = value;
    nameString = getName();
    int io=nameString.IndexOf(':');
    if (io >= 1) {
      prefix = nameString.Substring(0, (io)-(0));
      localName = nameString.Substring(io + 1);
    } else {
      prefix="";
      localName = getName();
    }
  }

    /// <summary>NOTE: Set after setNamespace, or it may be overwritten
    /// @param attrprefix.</summary>
    /// <param name='attrprefix'>Not documented yet.</param>
  public void setPrefix(string attrprefix) {
    prefix = attrprefix;
  }

  internal void setValue(string value2) {
    if (value2 == null) {
 throw new ArgumentException();
}
    valueString = value2;
    value = null;
  }
  public override string ToString() {
    return "[Attribute: "+getName()+"="+getValue()+"]";
  }
}
}
