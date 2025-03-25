using System;
using System.Text;

namespace Com.Upokecenter.Html {
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

    public Attr(IAttr attr) {
      this.valueNameString = attr.GetName();
      this.valueString = attr.GetValue();
      this.valuePrefix = attr.GetPrefix();
      this.valueLocalName = attr.GetLocalName();
      this.value_namespace = attr.GetNamespaceURI();
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
        this.valueName.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) |
          0xd800));
        this.valueName.Append((char)(((ch - 0x10000) & 0x3ff) | 0xdc00));
      }
    }

    public Attr(string valueName, string value) {
      this.valueNameString = valueName;
      this.valueString = value;
    }

    internal void AppendToName(int ch) {
      if (this.valueNameString != null) {
        throw new InvalidOperationException();
      }
      if (ch <= 0xffff) {
        { this.valueName.Append((char)ch);
        }
      } else if (ch <= 0x10ffff) {
        this.valueName.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) |
          0xd800));
        this.valueName.Append((char)(((ch - 0x10000) & 0x3ff) | 0xdc00));
      }
    }

    internal void AppendToValue(int ch) {
      if (this.valueString != null) {
        throw new InvalidOperationException();
      }
      if (ch <= 0xffff) {
        { this.value.Append((char)ch);
        }
      } else if (ch <= 0x10ffff) {
        this.value.Append((char)((((ch - 0x10000) >> 10) & 0x3ff) | 0xd800));
        this.value.Append((char)(((ch - 0x10000) & 0x3ff) | 0xdc00));
      }
    }

    internal void CommitValue() {
      if (this.value == null) {
        throw new InvalidOperationException();
      }
      this.valueString = this.value.ToString();
      this.value = null;
    }

    /* (non-Javadoc)
     * @see Com.Upokecenter.Html.IAttr#GetLocalName()
     */
    public string GetLocalName() {
      return (this.value_namespace == null) ? this.GetName() :
        this.valueLocalName;
    }

    /* (non-Javadoc)
     * @see Com.Upokecenter.Html.IAttr#GetName()
     */
    public string GetName() {
      return (this.valueNameString != null) ? this.valueNameString :
        this.valueName.ToString();
    }

    /* (non-Javadoc)
     * @see Com.Upokecenter.Html.IAttr#GetNamespaceURI()
     */
    public string GetNamespaceURI() {
      return this.value_namespace;
    }

    /* (non-Javadoc)
     * @see Com.Upokecenter.Html.IAttr#GetPrefix()
     */
    public string GetPrefix() {
      return this.valuePrefix;
    }
    /* (non-Javadoc)
     * @see Com.Upokecenter.Html.IAttr#GetValue()
     */
    public string GetValue() {
      return (this.valueString != null) ? this.valueString :
        this.value.ToString();
    }

    internal bool IsAttribute(string attrName, string value_namespace) {
      string thisname = this.GetLocalName();
      bool match = attrName == null ? thisname == null : attrName.Equals(
          thisname,
          StringComparison.Ordinal);
      if (!match) {
        return false;
      }
      match = value_namespace == null ? this.value_namespace == null :
        value_namespace.Equals(this.value_namespace, StringComparison.Ordinal);
      return match;
    }

    internal void SetName(string value2) {
      if (value2 == null) {
        throw new ArgumentException();
      }
      this.valueNameString = value2;
      this.valueName = null;
    }

    internal void SetNamespace(string value) {
      if (value == null) {
        throw new ArgumentException();
      }
      this.value_namespace = value;
      this.valueNameString = this.GetName();
      int io = this.valueNameString.IndexOf(':');
      if (io >= 1) {
        this.valuePrefix = this.valueNameString.Substring(0, io - 0);
        this.valueLocalName = this.valueNameString.Substring(io + 1);
      } else {
        this.valuePrefix = String.Empty;
        this.valueLocalName = this.GetName();
      }
    }

    /// <summary>NOTE: Set after SetNamespace, or it may be overwritten
    /// @param attrprefix.</summary>
    /// <param name='attrprefix'>The parameter <paramref
    /// name='attrprefix'/> is a text string.</param>
    public void SetPrefix(string attrprefix) {
      this.valuePrefix = attrprefix;
    }

    internal void SetValue(string value2) {
      if (value2 == null) {
        throw new ArgumentException();
      }
      this.valueString = value2;
      this.value = null;
    }

    public override string ToString() {
      return "[Attribute: " + this.GetName() + "=" + this.GetValue() + "]";
    }
  }
}
