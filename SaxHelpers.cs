// NO WARRANTY! This code is in the Public Domain.
// Based on Java source at www.saxproject.org.
// Ported and written by Karl Waclawek (karl@waclawek.net).

using System;
using System.Reflection;
using System.Text;
using PeterO.Support;

namespace Org.System.Xml.Sax.Helpers {
    /**<summary>Default implementation for <see cref="ParseError"/>.</summary>
     * <remarks>For some parsers a different implementation may be
       preferable.</remarks>
     */

    /// <summary>Not documented yet.</summary>
    public class ParseErrorImpl : ParseError
    {
        private Exception baseException;
        private string errorId;
        private string message;
        private string publicId;
        private string systemId;
        private long lineNumber;
        private long columnNumber;

    /// <summary>Set error id.</summary>
    /// <param name='id'>The parameter <paramref name='id'/> is not
    /// documented yet.</param>
    /// <remarks>Makes re-use of a
    /// <see cref='ParseError'/> instance possible, together with
    /// <see cref='Init'/> and
    /// <see cref='SetBaseException'/>.</remarks>
        protected void SetErrorId(string id) {
            this.errorId = id;
        }

    /// <summary>Set base exception.</summary>
    /// <param name='e'>The parameter <paramref name='e'/> is not
    /// documented yet.</param>
    /// <remarks>Makes re-use of a
    /// <see cref='ParseError'/> instance possible, together with
    /// <see cref='Init'/> and
    /// <see cref='SetErrorId'/>.</remarks>
        protected void SetBaseException(Exception e) {
            this.baseException = e;
        }

    /// <summary>Initialize instance data.</summary>
    /// <param name='message'>The parameter <paramref name='message'/> is
    /// not documented yet.</param>
    /// <param name='publicId'>The parameter <paramref name='publicId'/> is
    /// not documented yet.</param>
    /// <param name='systemId'>The parameter <paramref name='systemId'/> is
    /// not documented yet.</param>
    /// <param name='lineNumber'>The parameter <paramref
    /// name='lineNumber'/> is not documented yet.</param>
    /// <param name='columnNumber'>The parameter <paramref
    /// name='columnNumber'/> is not documented yet.</param>
    /// <remarks>Makes re-use of a
    /// <see cref='ParseError'/> instance possible, together with
    /// <see cref='SetErrorId'/> and
    /// <see cref='SetBaseException'/>.</remarks>
        protected void Init(
          string message,
          string publicId,
          string systemId,
          long lineNumber,
          long columnNumber) {
            this.message = message;
            this.publicId = publicId;
            this.systemId = systemId;
            this.lineNumber = lineNumber;
            this.columnNumber = columnNumber;
        }

    /// <summary>Initializes a new instance of the ParseErrorImpl
    /// class.</summary>
    /// <param name='message'>A string object.</param>
        public ParseErrorImpl(string message) {
            this.Init(message, null, null, -1, -1);
        }

    /// <summary>Initializes a new instance of the ParseErrorImpl
    /// class.</summary>
    /// <param name='message'>A string object.</param>
    /// <param name='e'>An Exception object.</param>
        public ParseErrorImpl(string message, Exception e) {
            this.Init(message, null, null, -1, -1);
            this.baseException = e;
        }

    /// <summary>Initializes a new instance of the ParseErrorImpl
    /// class.</summary>
    /// <param name='message'>A string object.</param>
    /// <param name='locator'>An ILocator object.</param>
    /// <param name='id'>Another string object.</param>
        public ParseErrorImpl(string message, ILocator locator, string id) {
            if (locator != null) {
                this.Init(
  message,
  locator.PublicId,
  locator.SystemId,
  locator.LineNumber,
  locator.ColumnNumber);
            } else {
                this.Init(message, null, null, -1, -1);
            }
            this.errorId = id;
        }

    /// <summary>Initializes a new instance of the ParseErrorImpl
    /// class.</summary>
    /// <param name='message'>A string object.</param>
    /// <param name='locator'>An ILocator object.</param>
    /// <param name='e'>An Exception object.</param>
        public ParseErrorImpl(string message, ILocator locator, Exception e) {
            if (locator != null) {
                this.Init(
  message,
  locator.PublicId,
  locator.SystemId,
  locator.LineNumber,
  locator.ColumnNumber);
            } else {
                this.Init(message, null, null, -1, -1);
            }
            this.baseException = e;
        }

    /// <summary>Initializes a new instance of the ParseErrorImpl
    /// class.</summary>
    /// <param name='message'>A string object.</param>
    /// <param name='publicId'>Another string object.</param>
    /// <param name='systemId'>A string object. (3).</param>
    /// <param name='lineNumber'>A 32-bit signed integer.</param>
    /// <param name='columnNumber'>Another 32-bit signed integer.</param>
    /// <param name='id'>A string object. (4).</param>
        public ParseErrorImpl(
  string message,
  string publicId,
  string systemId,
  int lineNumber,
  int columnNumber,
  string id) {
            this.Init(message, publicId, systemId, lineNumber, columnNumber);
            this.errorId = id;
        }

    /// <summary>Initializes a new instance of the ParseErrorImpl
    /// class.</summary>
    /// <param name='message'>A string object.</param>
    /// <param name='publicId'>Another string object.</param>
    /// <param name='systemId'>A string object. (3).</param>
    /// <param name='lineNumber'>A 32-bit signed integer.</param>
    /// <param name='columnNumber'>Another 32-bit signed integer.</param>
    /// <param name='e'>An Exception object.</param>
        public ParseErrorImpl(
  string message,
  string publicId,
  string systemId,
  int lineNumber,
  int columnNumber,
  Exception e) {
            this.Init(message, publicId, systemId, lineNumber, columnNumber);
            this.baseException = e;
        }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
        public override string Message {
            get {
                return this.message;
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
        public override string ErrorId {
            get {
                return this.errorId;
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
        public override string PublicId {
            get {
                return this.publicId;
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
        public override string SystemId {
            get {
                return this.systemId;
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
        public override long LineNumber {
            get {
                return this.lineNumber;
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
        public override long ColumnNumber {
            get {
                return this.columnNumber;
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <value>Value not documented yet.</value>
        public override Exception BaseException {
            get {
                return this.baseException;
            }
        }
    }

    /**<summary>Call-back delegate, useful when implementing <see
      cref="IProperty&lt;T>" />.</summary>
     * <remarks>One can re-use the same <see cref="IProperty&lt;T>" />
       implementation class
   * without the need to subclass it for a specific target property. One
       simply
     * registers an <c>OnPropertyChange</c> delegate with the <see
       cref="IProperty&lt;T>" />
     * instance which gets called whenever <see cref="IProperty&lt;T>.Value"
       /> changes.</remarks>
     */

    /// <summary>Not documented yet.</summary>
    /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
  public delegate void OnPropertyChange<T>(
  IProperty<T> property,
  T newValue);

    /**<summary>Implementaton of <see cref="IProperty&lt;T>" /> interface
      which calls back through
     * a delegate on every change of the property value.</summary>
     */

    /// <summary>Not documented yet.</summary>
    /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
    public abstract class PropertyImpl<T> : IProperty<T>
    {
        private T propValue;
        private OnPropertyChange<T> onChange;

    /// <summary>Initializes a new instance of the PropertyImpl
    /// class.</summary>
    /// <param name='onChange'>An OnPropertyChange object.</param>
    /// <param name='defaultValue'>A T object.</param>
        protected PropertyImpl(OnPropertyChange<T> onChange, T defaultValue) {
            this.onChange = onChange;
            this.propValue = defaultValue;
        }

    /// <summary>The Name property must be overriden in a derived
    /// class.</summary>
    /// <value>The Name property must be overriden in a derived
    /// class.</value>
    /// <remarks>This allows one to save the space needed for a name
    /// field.</remarks>
        public abstract string Name { get; }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public T Value {
            get {
                return this.propValue;
            }

            set {
                T newValue = value;
                if (this.onChange != null) {
                    this.onChange(this, newValue);
                }
                this.propValue = newValue;
            }
        }
    }

    /**<summary>Default implementation of the <see cref ="IAttributes" />
      interface.</summary>
     * <remarks>Differences to Java implementation: the <c>GetLocalName()</c>,
   * <c>GetQName()</c>, <c>GetType()</c>, <c>GetURI()</c> and
       <c>GetValue()</c>
  * methods throw an exception when no attribute matching the arguments is
       found.
     * In Java these methods return <c>null</c>, which is inconsistent since
       the same GetXXX()
     * methods in Java's Attributes2 and the SetXXX() methods in Java' s
       AttributesImpl
     * class do throw exceptions in the same situation.</remarks>
     * <seealso

  href="http://www.saxproject.org/apidoc/org/xml/ValueSax/helpers/AttributesImpl.html"
       >
     * AttributesImpl on www.saxproject.org</seealso>
     * <seealso

  href="http://www.saxproject.org/apidoc/org/xml/ValueSax/ext/Attributes2Impl.html"
       >
     * Attributes2Impl on www.saxproject.org</seealso>
     */

    /// <summary>Not documented yet.</summary>
    public class AttributesImpl : IAttributes
    {
    /// <summary>Holds all the fields for an attribute.</summary>
        protected struct Attribute {
            private string uri;
            private string valueQName;
            private string valueAType;
            private string valueAValue;

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
            public string Uri {
                get {
                    return this.uri;
                }

                set {
                    if (value == null) {
                    throw new ArgumentNullException(nameof(this.Uri));
                    }
                    this.uri = value;
                }
            }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
            public string QName {
                get {
                    return this.valueQName;
                }

                set {
                    if (value == null || value == String.Empty) {
                throw new ArgumentException(
  Resources.GetString(RsId.NonEmptyStringRequired),
  "QName");
                    }
                    this.valueQName = value;
                }
            }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
            public string AType {
                get {
                    return this.valueAType;
                }

                set {
                    if (value == null || value == String.Empty) {
                throw new ArgumentException(
  Resources.GetString(RsId.NonEmptyStringRequired),
  "AType");
                    }
                    this.valueAType = value;
                }
            }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
            public string AValue {
                get {
                    return this.valueAValue;
                }

                set {
                    if (value == null) {
                    throw new ArgumentNullException(nameof(this.AValue));
                    }
                    this.valueAValue = value;
                }
            }

    /// <summary>Not documented yet.</summary>
            public int PrefixLen;  // includes colon

    /// <summary>Not documented yet.</summary>
            public bool IsSpecified;
        }

        private Attribute[] atts;
        private int attCount;
        private StringBuilder strBuilder;

    /// <summary>All attributes are stored in this struct array.</summary>
    /// <value>All attributes are stored in this struct array.</value>
        protected Attribute[] Atts {
            get {
                return this.atts;
            }
        }

    /// <summary>String builder helper object.</summary>
    /// <value>String builder helper object.</value>
        protected StringBuilder StrBuilder {
            get {
                return this.strBuilder;
            }
        }

    /// <summary>Initializes a new instance of the AttributesImpl
    /// class.</summary>
        public AttributesImpl() {
            this.atts = new Attribute[4];  // default capacity = 4
        }

    /// <summary>Initializes a new instance of the AttributesImpl
    /// class.</summary>
    /// <param name='capacity'>A 32-bit signed integer.</param>
        public AttributesImpl(int capacity) {
            this.atts = new Attribute[capacity];
        }

    /// <summary>Returns clone of <c>AttributesImpl</c> instance.</summary>
    /// <returns>An AttributesImpl object.</returns>
    /// <remarks>Only
    /// <see cref='Attribute'/> fields are cloned, not the other fields
    /// like
    /// <see cref='Capacity'/>.</remarks>
        public AttributesImpl Clone() {
            var result = new AttributesImpl(this.attCount);
            // requires existing Attributes to be properly initialized
            Array.Copy(this.atts, 0, result.atts, 0, this.attCount);
            result.attCount = this.attCount;
            return result;
        }

    /// <summary>Capacity for holding
    /// <see cref='Attribute'/> instances.</summary>
    /// <value>Capacity for holding &lt;see cref=&#x27;Attribute&#x27;/&gt;
    /// instances.</value>
    /// <remarks>Can be initialized to avoid costly re-allocations when new
    /// attributes are added. Its value must not be less than the value of
    /// the
    /// <see cref='Length'/> property.</remarks>
        public int Capacity {
            get {
                return this.atts.Length;
            }

            set {
                if (value < this.attCount) {
                    string msg = Resources.GetString(RsId.CapacityTooSmall);
                    throw new ArgumentException(msg, "Capacity");
                }
                var tmpAtts = new Attribute[value];
                // requires existing Attributes to be properly initialized
                this.atts.CopyTo(tmpAtts, 0);
                this.atts = tmpAtts;
            }
        }

    /// <summary>Checks if index is in range, throws exception if
    /// not.</summary>
    /// <param name='index'>The parameter <paramref name='index'/> is not
    /// documented yet.</param>
        protected void CheckIndex(int index) {
            bool isBad = index < 0 || index >= this.attCount;
            if (isBad) {
                string msg = Resources.GetString(RsId.AttIndexOutOfBounds);
                throw new IndexOutOfRangeException(msg);
            }
        }

    /// <summary>Helper routine for throwing an
    /// <see cref='ArgumentException'/> when an attribute is not found,
    /// with message loaded from resources.</summary>
    /// <param name='valueQName'>Qualified name of attribute.</param>
        protected void NotFoundError(string valueQName) {
            string msg = Resources.GetString(RsId.AttributeNotFound);
     throw new ArgumentException(
  String.Format(msg, valueQName),
  "valueQName");
        }

    /// <summary>Helper routine for throwing an
    /// <see cref='ArgumentException'/> when an attribute is not found,
    /// with message loaded from resources.</summary>
    /// <param name='uri'>URI of attribute's qualified name.</param>
    /// <param name='localName'>Local part of attribute's qualified
    /// name.</param>
        protected void NotFoundError(string uri, string localName) {
            string msg = Resources.GetString(RsId.AttributeNotFoundNS);
string str13606 = "uri, localName";

            throw new ArgumentException(
  String.Format(msg, uri, localName),
  str13606);
        }

    /// <summary>Helper routine for quickly setting the fields of an
    /// attribute.</summary>
    /// <param name='att'>The parameter <paramref name='att'/> is not
    /// documented yet.</param>
    /// <param name='uri'>The parameter <paramref name='uri'/> is not
    /// documented yet.</param>
    /// <param name='localName'>The parameter <paramref name='localName'/>
    /// is not documented yet.</param>
    /// <param name='valueQName'>The parameter <paramref
    /// name='valueQName'/> is not documented yet.</param>
    /// <param name='valueAType'>The parameter <paramref
    /// name='valueAType'/> is not documented yet.</param>
    /// <param name='valueAValue'>The parameter <paramref
    /// name='valueAValue'/> is not documented yet.</param>
    /// <param name='isSpecified'>The parameter <paramref
    /// name='isSpecified'/> is not documented yet.</param>
        protected void InternalSetAttribute(
          ref Attribute att,
          string uri,
          string localName,
          string valueQName,
          string valueAType,
          string valueAValue,
          bool isSpecified) {
            att.Uri = uri;
            if (valueQName == null) {
                att.QName = localName;
                att.PrefixLen = 0;
            } else {
                att.QName = valueQName;
                int colPos = valueQName.IndexOf(Sax.Constants.XmlColon);
                att.PrefixLen = (colPos < 0) ? 0 : (colPos + 1);
            }
            att.AType = valueAType;
            att.AValue = valueAValue;
            att.IsSpecified = isSpecified;
        }

        // IAttributes

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public int Length {
            get {
                return this.attCount;
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A string object.</returns>
        public string GetUri(int index) {
            this.CheckIndex(index);
            return this.atts[index].Uri;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A string object.</returns>
        public string GetLocalName(int index) {
            this.CheckIndex(index);
            return this.atts[index].QName.Substring(this.atts[index].PrefixLen);
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A string object.</returns>
        public string GetQName(int index) {
            this.CheckIndex(index);
            return this.atts[index].QName;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A string object.</returns>
        public string GetType(int index) {
            this.CheckIndex(index);
            return this.atts[index].AType;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='uri'>Not documented yet.</param>
    /// <param name='localName'>Not documented yet.</param>
    /// <returns>A string object.</returns>
        public string GetType(string uri, string localName) {
            int index = this.GetIndex(uri, localName);
            if (index < 0) {
                this.NotFoundError(uri, localName);
            }
            return this.atts[index].AType;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='valueQName'>Not documented yet.</param>
    /// <returns>A string object.</returns>
        public string GetType(string valueQName) {
            int index = this.GetIndex(valueQName);
            if (index < 0) {
                this.NotFoundError(valueQName);
            }
            return this.atts[index].AType;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A string object.</returns>
        public string GetValue(int index) {
            this.CheckIndex(index);
            return this.atts[index].AValue;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='uri'>Not documented yet.</param>
    /// <param name='localName'>Not documented yet.</param>
    /// <returns>A string object.</returns>
        public string GetValue(string uri, string localName) {
            int index = this.GetIndex(uri, localName);
            if (index < 0) {
                this.NotFoundError(uri, localName);
            }
            return this.atts[index].AValue;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='valueQName'>Not documented yet.</param>
    /// <returns>A string object.</returns>
        public string GetValue(string valueQName) {
            int index = this.GetIndex(valueQName);
            if (index < 0) {
                this.NotFoundError(valueQName);
            }
            return this.atts[index].AValue;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='uri'>Not documented yet.</param>
    /// <param name='localName'>Not documented yet.</param>
    /// <returns>A 32-bit signed integer.</returns>
        public int GetIndex(string uri, string localName) {
            int result = this.attCount - 1;
            while (result >= 0) {
                if (String.Equals(uri, this.atts[result].Uri)) {
                    string valueQName = this.atts[result].QName;
                    bool equal = !(localName == null ^ valueQName == null);
                    if (equal && localName != null) {
                    equal = String.CompareOrdinal(
                    localName,
                    0,
                    valueQName,
                    this.atts[result].PrefixLen,
                    Int32.MaxValue) == 0;
                    }
                    if (equal) {
                    break;
                    }
                }
                --result;
            }
            return result;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='valueQName'>Not documented yet.</param>
    /// <returns>A 32-bit signed integer.</returns>
        public int GetIndex(string valueQName) {
            int result = this.attCount - 1;
            while (result >= 0) {
         if (valueQName != null &&
                  valueQName.Equals(this.atts[result].QName)) {
                    break;
                }
                --result;
            }
            return result;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
        public bool IsSpecified(int index) {
            this.CheckIndex(index);
            return this.atts[index].IsSpecified;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='valueQName'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
        public bool IsSpecified(string valueQName) {
            int index = this.GetIndex(valueQName);
            if (index < 0) {
                this.NotFoundError(valueQName);
            }
            return this.atts[index].IsSpecified;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='uri'>Not documented yet.</param>
    /// <param name='localName'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
        public bool IsSpecified(string uri, string localName) {
            int index = this.GetIndex(uri, localName);
            if (index < 0) {
                this.NotFoundError(uri, localName);
            }
            return this.atts[index].IsSpecified;
        }

        // modifier methods

    /// <summary>Add an attribute by specifying all its
    /// properties.</summary>
    /// <param name='uri'>The parameter <paramref name='uri'/> is not
    /// documented yet.</param>
    /// <param name='localName'>The parameter <paramref name='localName'/>
    /// is not documented yet.</param>
    /// <param name='valueQName'>The parameter <paramref
    /// name='valueQName'/> is not documented yet.</param>
    /// <param name='valueAType'>The parameter <paramref
    /// name='valueAType'/> is not documented yet.</param>
    /// <param name='valueAValue'>The parameter <paramref
    /// name='valueAValue'/> is not documented yet.</param>
    /// <param name='isSpecified'>The parameter <paramref
    /// name='isSpecified'/> is not documented yet.</param>
    /// <returns>Index of new attribute.</returns>
    /// <remarks>If there is no namespace, pass the empty string for the
    /// <c>uri</c> argument, and not <c>null</c>.</remarks>
        public int AddAttribute(
          string uri,
          string localName,
          string valueQName,
          string valueAType,
          string valueAValue,
          bool isSpecified) {
            if (this.attCount >= this.Capacity) {
                int newSize = this.attCount + 4 + (this.attCount >> 1);
                newSize = (newSize >> 2) << 2;  // align to 4 byte boundary
                this.Capacity = newSize;
            }
            this.InternalSetAttribute(
              ref this.atts[this.attCount],
              uri,
              localName,
              valueQName,
              valueAType,
              valueAValue,
              isSpecified);
            return this.attCount++;
        }

    /// <summary>Add an attribute taken from an existing set of
    /// attributes.</summary>
    /// <param name='atts'>The parameter <paramref name='atts'/> is not
    /// documented yet.</param>
    /// <param name='index'>The parameter <paramref name='index'/> is not
    /// documented yet.</param>
    /// <returns>Index of added attribute.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='atts'/> is null.</exception>
        public virtual int AddAttribute(IAttributes atts, int index) {
            if (atts == null) {
                throw new ArgumentNullException(nameof(atts));
            }
            return this.AddAttribute(
              atts.GetUri(index),
              atts.GetLocalName(index),
              atts.GetQName(index),
              atts.GetType(index),
              atts.GetValue(index),
              atts.IsSpecified(index));
        }

    /// <summary>Remove all attributes, but don't shrink
    /// capacity.</summary>
        public virtual void Clear() {
            this.attCount = 0;
        }

    /// <summary>Remove attribute at index.</summary>
    /// <param name='index'>The parameter <paramref name='index'/> is not
    /// documented yet.</param>
        public void RemoveAttribute(int index) {
            this.CheckIndex(index);
            int lastIndx = --this.attCount;
            for (; index < lastIndx; ++index) {
                this.atts[index] = this.atts[index + 1];
            }
        }

    /// <summary>Set attribute properties at index.</summary>
    /// <param name='index'>The parameter <paramref name='index'/> is not
    /// documented yet.</param>
    /// <param name='uri'>The parameter <paramref name='uri'/> is not
    /// documented yet.</param>
    /// <param name='localName'>The parameter <paramref name='localName'/>
    /// is not documented yet.</param>
    /// <param name='valueQName'>The parameter <paramref
    /// name='valueQName'/> is not documented yet.</param>
    /// <param name='valueAType'>The parameter <paramref
    /// name='valueAType'/> is not documented yet.</param>
    /// <param name='valueAValue'>The parameter <paramref
    /// name='valueAValue'/> is not documented yet.</param>
    /// <param name='isSpecified'>The parameter <paramref
    /// name='isSpecified'/> is not documented yet.</param>
        public void SetAttribute(
          int index,
          string uri,
          string localName,
          string valueQName,
          string valueAType,
          string valueAValue,
          bool isSpecified) {
            this.CheckIndex(index);
            this.InternalSetAttribute(
              ref this.atts[index],
              uri,
              localName,
              valueQName,
              valueAType,
              valueAValue,
              isSpecified);
        }

    /// <summary>Copy a whole set of attributes.</summary>
    /// <param name='atts'>The parameter <paramref name='atts'/> is not
    /// documented yet.</param>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='atts'/> is null.</exception>
        public virtual void SetAttributes(IAttributes atts) {
            if (atts == null) {
                throw new ArgumentNullException(nameof(atts));
            }
            this.Clear();
            int attLen = atts.Length;
            if (this.Capacity < attLen) {
                this.Capacity = attLen;
            }
            for (int attIndx = 0; attIndx < attLen; ++attIndx) {
                this.InternalSetAttribute(
                  ref this.atts[attIndx],
                  atts.GetUri(attIndx),
                  atts.GetLocalName(attIndx),
                  atts.GetQName(attIndx),
                  atts.GetType(attIndx),
                  atts.GetValue(attIndx),
                  atts.IsSpecified(attIndx));
            }
        }

    /// <summary>Set local name of attribute at index.</summary>
    /// <param name='index'>The parameter <paramref name='index'/> is not
    /// documented yet.</param>
    /// <param name='localName'>The parameter <paramref name='localName'/>
    /// is not documented yet.</param>
        public void SetLocalName(int index, string localName) {
            this.CheckIndex(index);
            if (localName == null || localName == String.Empty) {
                throw new ArgumentException(
  Resources.GetString(RsId.NonEmptyStringRequired),
  "localName");
            }
            int prefixLen = this.atts[index].PrefixLen;
            if (this.strBuilder == null) {
              this.strBuilder = new StringBuilder(
  this.atts[index].QName,
  0,
  prefixLen,
  localName.Length + prefixLen);
            } else {
                this.strBuilder.Length = 0;
                this.strBuilder.Append(this.atts[index].QName, 0, prefixLen);
            }
            this.strBuilder.Append(localName);
            this.atts[index].QName = this.strBuilder.ToString();
        }

    /// <summary>Set qualified name of attribute at index.</summary>
    /// <param name='index'>The parameter <paramref name='index'/> is not
    /// documented yet.</param>
    /// <param name='valueQName'>The parameter <paramref
    /// name='valueQName'/> is not documented yet.</param>
        public void SetQName(int index, string valueQName) {
            this.CheckIndex(index);
            this.atts[index].QName = valueQName;
        }

    /// <summary>Set type of attribute at index.</summary>
    /// <param name='index'>The parameter <paramref name='index'/> is not
    /// documented yet.</param>
    /// <param name='valueAType'>The parameter <paramref
    /// name='valueAType'/> is not documented yet.</param>
        public void SetType(int index, string valueAType) {
            this.CheckIndex(index);
            this.atts[index].AType = valueAType;
        }

    /// <summary>Set namespace URI of attribute at index.</summary>
    /// <param name='index'>The parameter <paramref name='index'/> is not
    /// documented yet.</param>
    /// <param name='uri'>The parameter <paramref name='uri'/> is not
    /// documented yet.</param>
    /// <remarks>For removing the namespace, pass the empty string for the
    /// <c>uri</c> argument, and not <c>null</c>.</remarks>
        public void SetURI(int index, string uri) {
            this.CheckIndex(index);
            this.atts[index].Uri = uri;
        }

    /// <summary>Set value of attribute at index.</summary>
    /// <param name='index'>The parameter <paramref name='index'/> is not
    /// documented yet.</param>
    /// <param name='valueAValue'>The parameter <paramref
    /// name='valueAValue'/> is not documented yet.</param>
        public void SetValue(int index, string valueAValue) {
            this.CheckIndex(index);
            this.atts[index].AValue = valueAValue;
        }

    /// <summary>Set if attribute at index is specified (not
    /// defaulted).</summary>
    /// <param name='index'>The parameter <paramref name='index'/> is not
    /// documented yet.</param>
    /// <param name='isSpecified'>The parameter <paramref
    /// name='isSpecified'/> is not documented yet.</param>
        public void SetIsSpecified(int index, bool isSpecified) {
            this.CheckIndex(index);
            this.atts[index].IsSpecified = isSpecified;
        }
    }

    /**<summary>Default implementation of the <see cref="ILocator" />
      interface.</summary>
     * <seealso

  href="http://www.saxproject.org/apidoc/org/xml/ValueSax/helpers/LocatorImpl.html"
       >
     * LocatorImpl on www.saxproject.org</seealso>
     * <seealso

  href="http://www.saxproject.org/apidoc/org/xml/ValueSax/ext/Locator2Impl.html"
       >
     * Locator2Impl on www.saxproject.org</seealso>
     */

    /// <summary>Not documented yet.</summary>
    public class LocatorImpl : ILocator
    {
        private string publicId;
        private string systemId;
        private long lineNumber;
        private long columnNumber;
        private string xmlVersion;
        private string encoding;
        private ParsedEntityType entityType;

    /// <summary>Initializes a new instance of the LocatorImpl
    /// class.</summary>
        public LocatorImpl() {
}

    /// <summary>Initializes a new instance of the LocatorImpl
    /// class.</summary>
    /// <param name='locator'>An ILocator object.</param>
        public LocatorImpl(ILocator locator) {
            this.publicId = locator.PublicId;
            this.systemId = locator.SystemId;
            this.lineNumber = locator.LineNumber;
            this.columnNumber = locator.ColumnNumber;
            this.xmlVersion = locator.XmlVersion;
            this.encoding = locator.Encoding;
            this.entityType = locator.EntityType;
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public string PublicId {
            get {
                return this.publicId;
            }

            set {
 this.publicId = value; }
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public string SystemId {
            get {
                return this.systemId;
            }

            set {
 this.systemId = value; }
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public long LineNumber {
            get {
                return this.lineNumber;
            }

            set {
 this.lineNumber = value; }
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public long ColumnNumber {
            get {
                return this.columnNumber;
            }

            set {
 this.columnNumber = value; }
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public string XmlVersion {
            get {
                return this.xmlVersion;
            }

            set {
 this.xmlVersion = value; }
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public string Encoding {
            get {
                return this.encoding;
            }

            set {
 this.encoding = value; }
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public ParsedEntityType EntityType {
            get {
                return this.entityType;
            }

            set {
 this.entityType = value; }
        }
    }

    /**<summary>No-op implementation of SAX interfaces, to be derived
      from.</summary>
     * <seealso

  href="http://www.saxproject.org/apidoc/org/xml/ValueSax/helpers/DefaultHandler.html"
       >
     * DefaultHandler on www.saxproject.org</seealso>
     * <seealso

  href="http://www.saxproject.org/apidoc/org/xml/ValueSax/ext/DefaultHandler2.html"
       >
     * DefaultHandler2 on www.saxproject.org</seealso>
     */

    /// <summary>Not documented yet.</summary>
    public class DefaultHandler : IContentHandler,
      IDtdHandler,
      ILexicalHandler,
      IDeclHandler,
      IEntityResolver,
      IErrorHandler
    {
        // IContentHandler

    /// <summary>Not documented yet.</summary>
    /// <param name='locator'>Not documented yet.</param>
        public virtual void SetDocumentLocator(ILocator locator) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
        public virtual void StartDocument() {
            // no op
        }

    /// <summary>Not documented yet.</summary>
        public virtual void EndDocument() {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='prefix'>Not documented yet.</param>
    /// <param name='uri'>Not documented yet.</param>
        public virtual void StartPrefixMapping(string prefix, string uri) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='prefix'>Not documented yet.</param>
        public virtual void EndPrefixMapping(string prefix) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='uri'>Not documented yet.</param>
    /// <param name='localName'>Not documented yet.</param>
    /// <param name='valueQName'>Not documented yet. (3).</param>
    /// <param name='atts'>Not documented yet. (4).</param>
        public virtual void StartElement(
          string uri,
          string localName,
          string valueQName,
          IAttributes atts) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='uri'>Not documented yet.</param>
    /// <param name='localName'>Not documented yet.</param>
    /// <param name='valueQName'>Not documented yet. (3).</param>
    public virtual void EndElement(
  string uri,
  string localName,
  string valueQName) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='ch'>Not documented yet.</param>
    /// <param name='start'>Not documented yet.</param>
    /// <param name='length'>Not documented yet. (3).</param>
        public virtual void Characters(char[] ch, int start, int length) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='ch'>Not documented yet.</param>
    /// <param name='start'>Not documented yet.</param>
    /// <param name='length'>Not documented yet. (3).</param>
    public virtual void IgnorableWhitespace(
  char[] ch,
  int start,
  int length) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='target'>Not documented yet.</param>
    /// <param name='data'>Not documented yet.</param>
        public virtual void ProcessingInstruction(string target, string data) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
        public virtual void SkippedEntity(string name) {
            // no op
        }

        // IDtdHandler

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='publicId'>Not documented yet.</param>
    /// <param name='systemId'>Not documented yet. (3).</param>
        public virtual void NotationDecl(
  string name,
  string publicId,
  string systemId) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='publicId'>Not documented yet.</param>
    /// <param name='systemId'>Not documented yet. (3).</param>
    /// <param name='notationName'>Not documented yet. (4).</param>
        public virtual void UnparsedEntityDecl(
          string name,
          string publicId,
          string systemId,
          string notationName) {
            // no op
        }

        // ILexicalHandler

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='publicId'>Not documented yet.</param>
    /// <param name='systemId'>Not documented yet. (3).</param>
   public virtual void StartDtd(
  string name,
  string publicId,
  string systemId) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
        public virtual void EndDtd() {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
        public virtual void StartEntity(string name) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
        public virtual void EndEntity(string name) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
        public virtual void StartCData() {
            // no op
        }

    /// <summary>Not documented yet.</summary>
        public virtual void EndCData() {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='ch'>Not documented yet.</param>
    /// <param name='start'>Not documented yet.</param>
    /// <param name='length'>Not documented yet. (3).</param>
        public virtual void Comment(char[] ch, int start, int length) {
            // no op
        }

        // IDeclHandler

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='model'>Not documented yet.</param>
        public virtual void ElementDecl(string name, string model) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='valueEName'>Not documented yet.</param>
    /// <param name='valueAName'>Not documented yet.</param>
    /// <param name='valueAType'>Not documented yet. (3).</param>
    /// <param name='mode'>Not documented yet. (4).</param>
    /// <param name='valueAValue'>Not documented yet. (5).</param>
        public virtual void AttributeDecl(
          string valueEName,
          string valueAName,
          string valueAType,
          string mode,
          string valueAValue) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='value'>Not documented yet.</param>
        public virtual void InternalEntityDecl(string name, string value) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='publicId'>Not documented yet.</param>
    /// <param name='systemId'>Not documented yet. (3).</param>
        public virtual void ExternalEntityDecl(
          string name,
          string publicId,
          string systemId) {
            // no op
        }

        // IEntityResolver

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='baseURI'>Not documented yet.</param>
    /// <returns>An InputSource object.</returns>
     public virtual InputSource GetExternalSubset(
  string name,
  string baseURI) {
            return null;
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='publicId'>Not documented yet.</param>
    /// <param name='baseURI'>Not documented yet. (3).</param>
    /// <param name='systemId'>Not documented yet. (4).</param>
    /// <returns>An InputSource object.</returns>
        public virtual InputSource ResolveEntity(
          string name,
          string publicId,
          string baseURI,
          string systemId) {
            return null;
        }

        // IErrorHandler

    /// <summary>Not documented yet.</summary>
    /// <param name='error'>Not documented yet.</param>
        public virtual void Warning(ParseError error) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='error'>Not documented yet.</param>
        public virtual void Error(ParseError error) {
            // no op
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='error'>Not documented yet.</param>
        public virtual void FatalError(ParseError error) {
            error.Throw();
        }
    }

    /**<summary>Base class for deriving an XML filter.</summary>
     * <seealso

  href="http://www.saxproject.org/apidoc/org/xml/ValueSax/helpers/XMLFilterImpl.html"
       >
     * JavaDoc on www.saxproject.org</seealso>
     */

    /// <summary>Not documented yet.</summary>
    public class XmlFilterImpl : IXmlFilter,
      IContentHandler,
      IDtdHandler,
      ILexicalHandler,
      IDeclHandler,
      IEntityResolver,
      IErrorHandler
    {
        private IXmlReader parent;
        private ILocator locator;
        private IContentHandler contentHandler;
        private IDtdHandler dtdHandler;
        private ILexicalHandler lexicalHandler;
        private IDeclHandler declHandler;
        private IEntityResolver resolver;
        private IErrorHandler errorHandler;

    /// <summary>Initializes a new instance of the XmlFilterImpl
    /// class.</summary>
        public XmlFilterImpl() {
}

    /// <summary>Initializes a new instance of the XmlFilterImpl
    /// class.</summary>
    /// <param name='parent'>An IXmlReader object.</param>
        public XmlFilterImpl(IXmlReader parent) {
            this.parent = parent;
        }

    /// <summary>Not documented yet.</summary>
        protected void CheckParent() {
            if (this.parent == null) {
            throw new SaxException(Resources.GetString(
                  RsId.NoFilterParent));
            }
        }

    /// <summary>Not documented yet.</summary>
        protected virtual void SetupParse() {
            this.CheckParent();
            this.parent.ContentHandler = this;
            this.parent.DtdHandler = this;
            this.parent.LexicalHandler = this;
            this.parent.DeclHandler = this;
            this.parent.EntityResolver = this;
            this.parent.ErrorHandler = this;
        }

        // IXmlReader

    /// <summary>See
    /// <see
    /// href='http://www.saxproject.org/apidoc/org/xml/ValueSax/helpers/XMLFilterImpl.html#getFeature(java.lang.String)'>getFeature(java.lang.String)</see>
    /// on www.saxproject.org.</summary>
    /// <param name='name'>The parameter <paramref name='name'/> is not
    /// documented yet.</param>
    /// <returns>A Boolean object.</returns>
    /// <remarks>Difference to Java: Will throw
    /// <see cref='SaxException'/> if parent is <c>null</c>.</remarks>
        public bool GetFeature(string name) {
            this.CheckParent();
            return this.parent.GetFeature(name);
        }

    /// <summary>See
    /// <see
    /// href='http://www.saxproject.org/apidoc/org/xml/ValueSax/helpers/XMLFilterImpl.html#setFeature(java.lang.String,
    /// boolean)'>setFeature(java.lang.String, boolean)</see> on
    /// www.saxproject.org.</summary>
    /// <param name='name'>The parameter <paramref name='name'/> is not
    /// documented yet.</param>
    /// <param name='value'>The parameter <paramref name='value'/> is not
    /// documented yet.</param>
    /// <remarks>Difference to Java: Will throw
    /// <see cref='SaxException'/> if parent is <c>null</c>.</remarks>
        public void SetFeature(string name, bool value) {
            this.CheckParent();
            this.parent.SetFeature(name, value);
        }

    /// <seealso
    /// href='http://www.saxproject.org/apidoc/org/xml/ValueSax/XMLReader.html#getProperty(java.lang.String)'>getProperty(java.lang.String)
    /// on www.saxproject.org</seealso>
    /// <summary>Returns an
    /// <see cref='IProperty&lt;T>'/> interface for the property identified
    /// by <c>name</c></summary>
    /// <param name='name'>The parameter <paramref name='name'/> is not
    /// documented yet.</param>
    /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
    /// <returns>An IProperty(T) object.</returns>
    /// <remarks>Difference to Java: Will throw
    /// <see cref='SaxException'/> if parent is <c>null</c>.</remarks>
        public IProperty<T> GetProperty<T>(string name) {
            this.CheckParent();
            return this.parent.GetProperty<T>(name);
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public IContentHandler ContentHandler {
            get {
                return this.contentHandler;
            }

            set {
 this.contentHandler = value; }
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public IDtdHandler DtdHandler {
            get {
                return this.dtdHandler;
            }

            set {
 this.dtdHandler = value; }
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public ILexicalHandler LexicalHandler {
            get {
                return this.lexicalHandler;
            }

            set {
 this.lexicalHandler = value; }
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public IDeclHandler DeclHandler {
            get {
                return this.declHandler;
            }

            set {
 this.declHandler = value; }
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public IEntityResolver EntityResolver {
            get {
                return this.resolver;
            }

            set {
 this.resolver = value; }
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public IErrorHandler ErrorHandler {
            get {
                return this.errorHandler;
            }

            set {
 this.errorHandler = value; }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='input'>Not documented yet.</param>
        public void Parse(InputSource input) {
            this.SetupParse();
            this.parent.Parse(input);
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='systemId'>Not documented yet.</param>
        public void Parse(string systemId) {
            this.Parse(new InputSource(systemId));
        }

    /// <summary>Not documented yet.</summary>
        public void Suspend() {
            this.CheckParent();
            this.parent.Suspend();
        }

    /// <summary>Not documented yet.</summary>
        public void Abort() {
            this.CheckParent();
            this.parent.Abort();
        }

    /// <summary>Not documented yet.</summary>
        public void Resume() {
            this.CheckParent();
            this.parent.Resume();
        }

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public XmlReaderStatus Status {
            get {
                this.CheckParent();
                return this.parent.Status;
            }
        }

        // IXmlFilter

    /// <summary>Gets a value not documented yet.</summary>
    /// <value>A value not documented yet.</value>
        public IXmlReader Parent {
            get {
                return this.parent;
            }

            set {
 this.parent = value; }
        }

        // IContentHandler

    /// <summary>Not documented yet.</summary>
    /// <param name='locator'>Not documented yet.</param>
        public virtual void SetDocumentLocator(ILocator locator) {
            this.locator = locator;
            if (this.contentHandler != null) {
                this.contentHandler.SetDocumentLocator(locator);
            }
        }

    /// <summary>Not documented yet.</summary>
        public virtual void StartDocument() {
            if (this.contentHandler != null) {
                this.contentHandler.StartDocument();
            }
        }

    /// <summary>Not documented yet.</summary>
        public virtual void EndDocument() {
            if (this.contentHandler != null) {
                this.contentHandler.EndDocument();
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='prefix'>Not documented yet.</param>
    /// <param name='uri'>Not documented yet.</param>
        public virtual void StartPrefixMapping(string prefix, string uri) {
            if (this.contentHandler != null) {
                this.contentHandler.StartPrefixMapping(prefix, uri);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='prefix'>Not documented yet.</param>
        public virtual void EndPrefixMapping(string prefix) {
            if (this.contentHandler != null) {
                this.contentHandler.EndPrefixMapping(prefix);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='uri'>Not documented yet.</param>
    /// <param name='localName'>Not documented yet.</param>
    /// <param name='valueQName'>Not documented yet. (3).</param>
    /// <param name='atts'>Not documented yet. (4).</param>
        public virtual void StartElement(
          string uri,
          string localName,
          string valueQName,
          IAttributes atts) {
            if (this.contentHandler != null) {
            this.contentHandler.StartElement(
  uri,
  localName,
  valueQName,
  atts);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='uri'>Not documented yet.</param>
    /// <param name='localName'>Not documented yet.</param>
    /// <param name='valueQName'>Not documented yet. (3).</param>
    public virtual void EndElement(
  string uri,
  string localName,
  string valueQName) {
            if (this.contentHandler != null) {
                this.contentHandler.EndElement(uri, localName, valueQName);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='ch'>Not documented yet.</param>
    /// <param name='start'>Not documented yet.</param>
    /// <param name='length'>Not documented yet. (3).</param>
        public virtual void Characters(char[] ch, int start, int length) {
            if (this.contentHandler != null) {
                this.contentHandler.Characters(ch, start, length);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='ch'>Not documented yet.</param>
    /// <param name='start'>Not documented yet.</param>
    /// <param name='length'>Not documented yet. (3).</param>
    public virtual void IgnorableWhitespace(
  char[] ch,
  int start,
  int length) {
            if (this.contentHandler != null) {
                this.contentHandler.IgnorableWhitespace(ch, start, length);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='target'>Not documented yet.</param>
    /// <param name='data'>Not documented yet.</param>
        public virtual void ProcessingInstruction(string target, string data) {
            if (this.contentHandler != null) {
                this.contentHandler.ProcessingInstruction(target, data);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
        public virtual void SkippedEntity(string name) {
            if (this.contentHandler != null) {
                this.contentHandler.SkippedEntity(name);
            }
        }

        // IDtdHandler

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='publicId'>Not documented yet.</param>
    /// <param name='systemId'>Not documented yet. (3).</param>
        public virtual void NotationDecl(
  string name,
  string publicId,
  string systemId) {
            if (this.dtdHandler != null) {
                this.dtdHandler.NotationDecl(name, publicId, systemId);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='publicId'>Not documented yet.</param>
    /// <param name='systemId'>Not documented yet. (3).</param>
    /// <param name='notationName'>Not documented yet. (4).</param>
        public virtual void UnparsedEntityDecl(
          string name,
          string publicId,
          string systemId,
          string notationName) {
            if (this.dtdHandler != null) {
        this.dtdHandler.UnparsedEntityDecl(
  name,
  publicId,
  systemId,
  notationName);
            }
        }

        // ILexicalHandler

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='publicId'>Not documented yet.</param>
    /// <param name='systemId'>Not documented yet. (3).</param>
   public virtual void StartDtd(
  string name,
  string publicId,
  string systemId) {
            if (this.lexicalHandler != null) {
                this.lexicalHandler.StartDtd(name, publicId, systemId);
            }
        }

    /// <summary>Not documented yet.</summary>
        public virtual void EndDtd() {
            if (this.lexicalHandler != null) {
                this.lexicalHandler.EndDtd();
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
        public virtual void StartEntity(string name) {
            if (this.lexicalHandler != null) {
                this.lexicalHandler.StartEntity(name);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
        public virtual void EndEntity(string name) {
            if (this.lexicalHandler != null) {
                this.lexicalHandler.EndEntity(name);
            }
        }

    /// <summary>Not documented yet.</summary>
        public virtual void StartCData() {
            if (this.lexicalHandler != null) {
                this.lexicalHandler.StartCData();
            }
        }

    /// <summary>Not documented yet.</summary>
        public virtual void EndCData() {
            if (this.lexicalHandler != null) {
                this.lexicalHandler.EndCData();
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='ch'>Not documented yet.</param>
    /// <param name='start'>Not documented yet.</param>
    /// <param name='length'>Not documented yet. (3).</param>
        public virtual void Comment(char[] ch, int start, int length) {
            if (this.lexicalHandler != null) {
                this.lexicalHandler.Comment(ch, start, length);
            }
        }

        // IDeclHandler

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='model'>Not documented yet.</param>
        public virtual void ElementDecl(string name, string model) {
            if (this.declHandler != null) {
                this.declHandler.ElementDecl(name, model);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='valueEName'>Not documented yet.</param>
    /// <param name='valueAName'>Not documented yet.</param>
    /// <param name='valueAType'>Not documented yet. (3).</param>
    /// <param name='mode'>Not documented yet. (4).</param>
    /// <param name='valueAValue'>Not documented yet. (5).</param>
        public virtual void AttributeDecl(
          string valueEName,
          string valueAName,
          string valueAType,
          string mode,
          string valueAValue) {
            if (this.declHandler != null) {
                this.declHandler.AttributeDecl(
  valueEName,
  valueAName,
  valueAType,
  mode,
  valueAValue);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='value'>Not documented yet.</param>
        public virtual void InternalEntityDecl(string name, string value) {
            if (this.declHandler != null) {
                this.declHandler.InternalEntityDecl(name, value);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='publicId'>Not documented yet.</param>
    /// <param name='systemId'>Not documented yet. (3).</param>
        public virtual void ExternalEntityDecl(
  string name,
  string publicId,
  string systemId) {
            if (this.declHandler != null) {
                this.declHandler.ExternalEntityDecl(name, publicId, systemId);
            }
        }

        // IEntityResolver

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='baseUri'>Not documented yet.</param>
    /// <returns>An InputSource object.</returns>
     public virtual InputSource GetExternalSubset(
  string name,
  string baseUri) {
            if (this.resolver != null) {
                return this.resolver.GetExternalSubset(name, baseUri);
            } else {
                return null;
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>Not documented yet.</param>
    /// <param name='publicId'>Not documented yet.</param>
    /// <param name='baseUri'>Not documented yet. (3).</param>
    /// <param name='systemId'>Not documented yet. (4).</param>
    /// <returns>An InputSource object.</returns>
        public virtual InputSource ResolveEntity(
  string name,
  string publicId,
  string baseUri,
  string systemId) {
            if (this.resolver != null) {
             return this.resolver.ResolveEntity(
  name,
  publicId,
  baseUri,
  systemId);
            } else {
                return null;
            }
        }

        // IErrorHandler

    /// <summary>Not documented yet.</summary>
    /// <param name='error'>Not documented yet.</param>
        public virtual void Warning(ParseError error) {
            if (this.errorHandler != null) {
                this.errorHandler.Warning(error);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='error'>Not documented yet.</param>
        public virtual void Error(ParseError error) {
            if (this.errorHandler != null) {
                this.errorHandler.Error(error);
            }
        }

    /// <summary>Not documented yet.</summary>
    /// <param name='error'>Not documented yet.</param>
        public virtual void FatalError(ParseError error) {
            if (this.errorHandler != null) {
                this.errorHandler.FatalError(error);
            }
        }
    }

  /**<summary>Factory class for creating new <see cref="IXmlReader" />
    instances.</summary>
   * <remarks>A default implementation of <see cref="IXmlReader" /> can be
     registered
   * in the system configuration file "machine.config", under the section
     appSettings.
   * The keys to be registered are "Org.System.Xml.Sax.ReaderClass" and
 * "Org.System.Xml.Sax.ReaderAssembly" . The class name must be fully
     qualified,
   * the assembly name can be a partial name.</remarks>
   */

    /// <summary>Not documented yet.</summary>
  public static class SaxReaderFactory {
    /* private static bool InterfaceFilter (Type typeObj, Object criteriaObj) {
            Type criteriaType = ((Type)criteriaObj);
         return typeObj.IsSubclassOf (criteriaType) || typeObj ==
              criteriaType;
        }
*/

    /// <summary>Creates an instance of <c>readerType</c> if it has a
    /// constructor matching the runtime types in the <c>args</c> array of
    /// parameters.</summary>
    /// <param name='readerType'>The parameter <paramref
    /// name='readerType'/> is not documented yet.</param>
    /// <param name='args'>The parameter <paramref name='args'/> is not
    /// documented yet.</param>
    /// <returns>An IXmlReader object.</returns>
    /*
     private static IXmlReader CreateInstance (Type readerType, Object []
          args) {
            IXmlReader result = null;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            Type [] argTypes;
      argTypes = (args != null) ? (new Type [args.Length]) :
              (Type.EmptyTypes);
            for (int indx = 0; indx < argTypes.Length; ++indx) {
                argTypes [indx] = args [indx].GetType();
            }
      ConstructorInfo cInfo = readerType.GetConstructor (flags, null,
              argTypes,
                    null);
            if (cInfo != null) {
                result = cInfo.Invoke (args) as IXmlReader;
            }
            return result;
        }
*/

    /// <summary>Returns the first class-type it can find in the
    /// <see cref='Assembly'/> argument that implements
    /// <see cref='IXmlReader'/>. Returns <c>null</c> if there is no such
    /// class.</summary>
    /// <param name='assem'>The parameter <paramref name='assem'/> is not
    /// documented yet.</param>
    /// <returns>A Type object.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='assem'/> is null.</exception>
    /// <remarks>This will not find classes that have unbound generic
    /// parameters.</remarks>
    private static Type FindReaderClass(Assembly assem) {
      /* if (assem == null) {
                throw new ArgumentNullException(nameof(assem));
            }
            Type readerType = typeof (IXmlReader);
            var filter = new TypeFilter (InterfaceFilter);

            Type [] types = assem.GetExportedTypes();
            for (int indx = 0; indx < types.Length; ++indx) {
                Type type = types [indx];
                if (type.IsClass) {
                    Type [] intfs = type.FindInterfaces (filter, readerType);
                    if (intfs.Length > 0) {
                    return type;
                    }
                }
            }
            return null;
            */ return typeof(PeterO.Support.SaxReader);
    }

    private const string ValueSax = "Org.System.Xml.Sax";

    /// <summary>Key name for registering the default parser assembly in
    /// the machine.config file.</summary>
    public const string ReaderAssembly = ValueSax + ".ReaderAssembly";

    /// <summary>Key name for registering the default parser class in the
    /// machine.config file.</summary>
    public const string ReaderClass = ValueSax + ".ReaderClass";

    /// <summary>Creates a new instance of
    /// <see cref='IXmlReader'/> based on the constructor arguments that
    /// are passed as parameters.</summary>
    /// <param name='args'>The parameter <paramref name='args'/> is not
    /// documented yet.</param>
    /// <returns>
    /// <see cref='IXmlReader'/> instance.</returns>
    /// <remarks>The assembly and class are determined by first checking
    /// the machine configuration file's appSettings section if a default
    /// parser is specified. If that fails, the loaded assemblies are
    /// searched for a class implementing
    /// <see cref='IXmlReader'/>. The types of the objects in the
    /// <c>args</c> array must match a constructor signature of the
    /// class.</remarks>
    public static IXmlReader CreateReader(Object[] args) {
      return new SaxReader();
      /*
            try {
                var confReader = new AppSettingsReader();
                var assemblyName = (string)confReader.GetValue (ReaderAssembly,
                    typeof (string));
                Assembly assem;
                assem = (File.Exists (assemblyName)) ? (Assembly.LoadFrom
                  (assemblyName)) : (Assembly.Load (assemblyName));
                var className = (string)confReader.GetValue (ReaderClass,
                    typeof (string));
                if (className == null || className == String.Empty) {
                    return CreateReader (assem, args);
                } else {
                    return CreateReader (assem, className, args);
                }
            } catch {
                // ignore exception, we want to check loaded assemblies
            }

            AppDomain domain = AppDomain.CurrentDomain;
            Assembly [] assems = domain.GetAssemblies();
            // ignore the XmlFilterImpl class in this assembly
            Type xmlFilterType = typeof (XmlFilterImpl);
            foreach (Assembly assem in assems) {
                IXmlReader reader = null;
                Type readerType = FindReaderClass (assem);
                if (readerType != null && readerType != xmlFilterType &&
                  !readerType.IsSubclassOf (xmlFilterType)) {
                    reader = CreateInstance (readerType, args);
                }
                if (reader != null) {
                    return reader;
                }
            }
            string msg = Resources.GetString (RsId.NoDefaultXmlReader);
            throw new SaxException (msg);
        }*/
    }
  }
}
