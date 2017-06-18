// Modified by Peter O. to use generics, among
// other things; also moved from org.json.
// Still in the public domain;
// public domain dedication: http://creativecommons.org/publicdomain/zero/1.0/

namespace com.upokecenter.json {
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class JSONArray {
    /// <summary>The getArrayList where the JSONArray's properties are
    /// kept.</summary>
  private List<Object> myArrayList;

    /// <summary>Initializes a new instance of the JSONArray class.
    /// Construct an empty JSONArray.</summary>
  public JSONArray() {
    myArrayList = new List<Object>();
  }

    /// <summary>Initializes a new instance of the JSONArray class.
    /// Construct a JSONArray from a Collection. @param collection A
    /// Collection.</summary>
    /// <param name='collection'>A /*covar*/ICollection object.</param>
  public JSONArray(/*covar*/ICollection<Object> collection) {
    myArrayList = new List<Object>(collection);
  }

    /// <summary>Initializes a new instance of the JSONArray class.
    /// Construct a JSONArray from a JSONTokener. @param x A JSONTokener
    /// @exception Json.InvalidJsonException A JSONArray must start with
    /// '[' @exception Json.InvalidJsonException Expected a ',' or
    /// ']'.</summary>
    /// <param name='x'>A JSONTokener object.</param>
  public JSONArray(JSONTokener x) : this() {
    if (x.nextClean() != '[') {
 throw x.syntaxError("A JSONArray must start with '['");
}
    if (x.nextClean() == ']') {
 return;
}
    x.back();
    while (true) {
      if (x.nextClean() == ',') {
        x.back();
        myArrayList.Add(null);
      } else {
        x.back();
        myArrayList.Add(x.nextValue());
      }
      switch (x.nextClean()) {
      case ',':
        if (x.nextClean() == ']') {
          if ((x.getOptions() & JSONObject.OPTION_TRAILING_COMMAS) == 0) {
           // 2013-05-24 -- Peter O. Disallow trailing comma.
           throw x.syntaxError("Trailing comma");
          } else {
            return;
          }
        }
        x.back();
        break;
      case ']':
        return;
      default:
        throw x.syntaxError("Expected a ',' or ']'");
      }
    }
  }

  public JSONArray(IList<string> collection) {
    myArrayList = new List<Object>();
    foreach (var str in collection) {
      myArrayList.Add(str);
    }
  }

    /// <summary>
    /// Initializes a new instance of the JSONArray class.
    /// Construct a JSONArray from a source _string. @param
    /// _string A _string that begins with
    /// <code>[</code>
    ///  &nbsp;
    /// <small>(left bracket)</small>
    ///  and ends with
    /// <code>]</code>
    ///  &nbsp;
    /// <small>(right bracket)</small>
    ///  . @exception Json.InvalidJsonException The _string
    /// must conform to JSON syntax.
    /// </summary>
    /// <param name='_string'>A string object.</param>
    /// <param name='options'>A 32-bit signed integer.</param>
  public JSONArray(string _string, int options) {
    this(new JSONTokener(_string, options));
  }

    /// <summary>
    /// Initializes a new instance of the JSONArray class.
    /// Construct a JSONArray from a source _string. @param
    /// _string A _string that begins with
    /// <code>[</code>
    ///  &nbsp;
    /// <small>(left bracket)</small>
    ///  and ends with
    /// <code>]</code>
    ///  &nbsp;
    /// <small>(right bracket)</small>
    ///  . @param option Options for parsing the _string.
    /// Currently JSONObject.OPTION_NO_DUPLICATES,
    /// JSONObject.OPTION_SHELL_COMMENTS, and/or
    /// JSONObject.OPTION_ADD_COMMENTS. @exception
    /// Json.InvalidJsonException The _string must conform to
    /// JSON syntax.
    /// </summary>
    /// <param name='_string'>A string object.</param>
  public JSONArray(string _string) : this(_string, 0) {
  }

  public JSONArray add(int index, bool value) {
    add(index, (value));
    return this;
  }

  public JSONArray add(int index, double value) {
    add(index, (value));
    return this;
  }

  public JSONArray add(int index, int value) {
    add(index, (value));
    return this;
  }

  public JSONArray add(int index, Object value) {
    if (index < 0) {
 throw new System.Collections.Generic.KeyNotFoundException("JSONArray["+
        index +
          "] not found.");
 }
    else if (value == null) {
 throw new ArgumentNullException();
} else {
      myArrayList.Insert(index, value);
    }
    return this;
  }

  public override bool Equals(object obj) {
    if (this == obj) {
 return true;
}
    if (obj == null) {
 return false;
}
    if (GetType() != obj.GetType()) {
 return false;
}
    var other = (JSONArray) obj;
    if (myArrayList == null) {
      if (other.myArrayList != null) {
 return false;
}
    } else {
 return !myArrayList.Equals(other.myArrayList);
}
  }

    /// <summary>Get the _object value associated with an index. @param
    /// index The index must be between 0 and length() - 1. @return An
    /// _object value. @exception
    /// System.Collections.Generic.KeyNotFoundException.</summary>
    /// <param name='i'>Not documented yet.</param>
    /// <returns>An arbitrary object.</returns>
  public Object this[int i] { get {
 return get(i);
} set { put(i, value); } }
public Object get(int index) {
    Object o = opt(index);
    if (o == null) {
 throw new System.Collections.Generic.KeyNotFoundException("JSONArray["+
        index +
          "] not found.");
 }
    return o;
  }

    /// <summary>Get the ArrayList which is holding the elements of the
    /// JSONArray. @return The ArrayList.</summary>
  List<Object> getArrayList() {
    return myArrayList;
  }

    /// <summary>Get the bool value associated with an index. The _string
    /// values "true" and "false" are converted to bool. @param index The
    /// index must be between 0 and length() - 1. @return The truth.
    /// @exception System.Collections.Generic.KeyNotFoundException if the
    /// index is not found @exception InvalidCastException.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
  public bool getBoolean(int index) {
    Object o = get(index);
    if (o == (object)false || o.Equals("false")) {
 return false;
  } else if (o == (object)true || o.Equals("true")) {
 return true;
}
    throw new InvalidCastException("JSONArray[" + index +
        "] not a Boolean.");
  }

    /// <summary>Get the double value associated with an index. @param
    /// index The index must be between 0 and length() - 1. @return The
    /// value. @exception System.Collections.Generic.KeyNotFoundException
    /// if the key is not found @exception FormatException if the value
    /// cannot be converted to a number.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A 64-bit floating-point number.</returns>
  public double getDouble(int index) {
    Object o = get(index);
    if (o is int || o is double || o is float || o is long || o is decimal||
      o is short || o is byte || o is UInt16 || o is UInt32 || o is
      UInt64) {
 return Convert.ToDouble(o);
}
    if (o is string) {
 return Double.Parse((string)
  o, NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint|NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
}
    throw new FormatException("JSONObject[" +
        index + "] is not a number.");
  }

    /// <summary>Get the int value associated with an index. @param index
    /// The index must be between 0 and length() - 1. @return The value.
    /// @exception System.Collections.Generic.KeyNotFoundException if the
    /// key is not found @exception FormatException if the value cannot be
    /// converted to a number.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A 32-bit signed integer.</returns>
  public int getInt(int index) {
    Object o = get(index);
    return (o is int || o is double || o is float || o is long || o is
      decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o
      is UInt64) ? (Convert.ToInt32(o)) : ((int)getDouble(index));
  }

    /// <summary>Get the JSONArray associated with an index. @param index
    /// The index must be between 0 and length() - 1. @return A JSONArray
    /// value. @exception System.Collections.Generic.KeyNotFoundException
    /// if the index is not found or if the value is not a
    /// JSONArray.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A JSONArray object.</returns>
  public JSONArray getJSONArray(int index) {
    Object o = get(index);
    if (o is JSONArray) {
 return (JSONArray)o;
}
    throw new System.Collections.Generic.KeyNotFoundException("JSONArray[" +
      index +
        "] is not a JSONArray.");
  }

    /// <summary>Get the JSONObject associated with an index. @param index
    /// subscript @return A JSONObject value. @exception
    /// System.Collections.Generic.KeyNotFoundException if the index is not
    /// found or if the value is not a JSONObject.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A JSONObject object.</returns>
  public JSONObject getJSONObject(int index) {
    Object o = get(index);
    if (o is JSONObject) {
 return (JSONObject)o;
}
    throw new System.Collections.Generic.KeyNotFoundException("JSONArray[" +
      index +
        "] is not a JSONObject.");
  }

    /// <summary>Get the _string associated with an index. @param index The
    /// index must be between 0 and length() - 1. @return A _string value.
    /// @exception
    /// System.Collections.Generic.KeyNotFoundException.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public string getString(int index) {
    return get(index).ToString();
  }

  public override int GetHashCode() {unchecked {
     var prime = 31;
    int result = prime * result+
        ((myArrayList == null) ? 0 : myArrayList.GetHashCode());
    return result;
  }}

    /// <summary>Determine if the value is null. @param index The index
    /// must be between 0 and length() - 1. @return true if the value at
    /// the index is null, or if there is no value.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
  public bool isNull(int index) {
    Object o = opt(index);
    // Peter O. 3/15/2013: Replaced equals(null) with equals(JSONObject.NULL)
    return o == null || o.Equals(JSONObject.NULL);
  }

    /// <summary>Make a _string from the contents of this JSONArray. The
    /// separator _string is inserted between each element. Warning: This
    /// method assumes that the data structure is acyclical. @param
    /// separator A _string that will be inserted between the elements.
    /// @return a _string.</summary>
    /// <param name='separator'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public string join(string separator) {
    int i;
    Object o;
    var sb = new StringBuilder();
    for (i = 0; i < myArrayList.Count; i += 1) {
      if (i > 0) {
        sb.Append(separator);
      }
      o = myArrayList[i];
      if (o == null) {
        sb.Append("null");
      } else if (o is string) {
        sb.Append(JSONObject.quote((string)o));
      } else if (o is int || o is double || o is float || o is long || o is
        decimal || o is short || o is byte || o is UInt16 || o is UInt32 ||
        o is UInt64) {
        sb.Append(JSONObject.numberToString(o));
      } else {
        sb.Append(o.ToString());
      }
    }
    return sb.ToString();
  }

    /// <summary>Get the length of the JSONArray. @return The length (or
    /// size).</summary>
    /// <value>Get the length of the JSONArray. @return The length (or
    /// size).</value>
  public int Length { get {
 return length();
}}
public int length() {
    return myArrayList.Count;
  }

    /// <summary>Get the optional _object value associated with an index.
    /// @param index The index must be between 0 and length() - 1. @return
    /// An _object value, or null if there is no _object at that
    /// index.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>An arbitrary object.</returns>
  public Object opt(int index) {
    if (index < 0 || index >= length()) {
 return null;
} else {
 return myArrayList[index];
}
  }

    /// <summary>Get the optional bool value associated with an index. It
    /// returns false if there is no value at that index, or if the value
    /// is not (object)true or the string "true". @param index The index
    /// must be between 0 and length() - 1. @return The truth.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
  public bool optBoolean(int index) {
    return optBoolean(index, false);
  }

    /// <summary>Get the optional bool value associated with an index. It
    /// returns the defaultValue if there is no value at that index or if
    /// it is not a Boolean or the string "true" or "false". @param index
    /// The index must be between 0 and length() - 1. @param defaultValue A
    /// bool default. @return The truth.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <param name='defaultValue'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
  public bool optBoolean(int index, bool defaultValue) {
    Object o = opt(index);
    if (o != null) {
      if (o == (object)false || o.Equals("false")) {
 return false;
  } else if (o == (object)true || o.Equals("true")) {
 return true;
}
    }
    return defaultValue;
  }

    /// <summary>Get the optional double value associated with an index.
    /// NaN is returned if the index is not found, or if the value is not a
    /// number and cannot be converted to a number. @param index The index
    /// must be between 0 and length() - 1. @return The value.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A 64-bit floating-point number.</returns>
  public double optDouble(int index) {
    return optDouble(index, Double.NaN);
  }

    /// <summary>Get the optional double value associated with an index.
    /// The defaultValue is returned if the index is not found, or if the
    /// value is not a number and cannot be converted to a number. @param
    /// index subscript @param defaultValue The default value. @return The
    /// value.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <param name='defaultValue'>Not documented yet.</param>
    /// <returns>A 64-bit floating-point number.</returns>
  public double optDouble(int index, double defaultValue) {
    Object o = opt(index);
    if (o != null) {
      if (o is int || o is double || o is float || o is long || o is decimal||
        o is short || o is byte || o is UInt16 || o is UInt32 || o is
        UInt64) {
 return Convert.ToDouble(o);
}
      try {
        return Double.Parse((string)
  o, NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint|NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
      } catch (FormatException) {}
    }
    return defaultValue;
  }

    /// <summary>Get the optional int value associated with an index. Zero
    /// is returned if the index is not found, or if the value is not a
    /// number and cannot be converted to a number. @param index The index
    /// must be between 0 and length() - 1. @return The value.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A 32-bit signed integer.</returns>
  public int optInt(int index) {
    return optInt(index, 0);
  }

    /// <summary>Get the optional int value associated with an index. The
    /// defaultValue is returned if the index is not found, or if the value
    /// is not a number and cannot be converted to a number. @param index
    /// The index must be between 0 and length() - 1. @param defaultValue
    /// The default value. @return The value.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <param name='defaultValue'>Not documented yet.</param>
    /// <returns>A 32-bit signed integer.</returns>
  public int optInt(int index, int defaultValue) {
    Object o = opt(index);
    if (o != null) {
      if (o is int || o is double || o is float || o is long || o is decimal||
        o is short || o is byte || o is UInt16 || o is UInt32 || o is
        UInt64) {
 return Convert.ToInt32(o);
}
      try {
        return
  Int32.Parse((string)o, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
      } catch (FormatException) {}
    }
    return defaultValue;
  }

    /// <summary>Get the optional JSONArray associated with an index.
    /// @param index subscript @return A JSONArray value, or null if the
    /// index has no value, or if the value is not a JSONArray.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A JSONArray object.</returns>
  public JSONArray optJSONArray(int index) {
    Object o = opt(index);
    return (o is JSONArray) ? ((JSONArray)o) : (null);
  }

    /// <summary>Get the optional JSONObject associated with an index. Null
    /// is returned if the key is not found, or null if the index has no
    /// value, or if the value is not a JSONObject. @param index The index
    /// must be between 0 and length() - 1. @return A JSONObject
    /// value.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A JSONObject object.</returns>
  public JSONObject optJSONObject(int index) {
    Object o = opt(index);
    return (o is JSONObject) ? ((JSONObject)o) : (null);
  }

    /// <summary>Get the optional _string value associated with an index.
    /// It returns an empty _string if there is no value at that index. If
    /// the value is not a _string and is not null, then it is coverted to
    /// a _string. @param index The index must be between 0 and length() -
    /// 1. @return A string value.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public string optString(int index) {
    return optString(index, "");
  }

    /// <summary>Get the optional _string associated with an index. The
    /// defaultValue is returned if the key is not found. @param index The
    /// index must be between 0 and length() - 1. @param defaultValue The
    /// default value. @return A string value.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <param name='defaultValue'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public string optString(int index, string defaultValue) {
    Object o = opt(index);
    return (o != null) ? (o.ToString()) : (defaultValue);
  }

    /// <summary>Append a bool value. @param value A bool value. @return
    /// this.</summary>
    /// <param name='value'>Not documented yet.</param>
    /// <returns>A JSONArray object.</returns>
  public JSONArray put(bool value) {
    put(value);
    return this;
  }

    /// <summary>Append a double value. @param value A double value.
    /// @return this.</summary>
    /// <param name='value'>Not documented yet.</param>
    /// <returns>A JSONArray object.</returns>
  public JSONArray put(double value) {
    put(value);
    return this;
  }

    /// <summary>Append an int value. @param value An int value. @return
    /// this.</summary>
    /// <param name='value'>Not documented yet.</param>
    /// <returns>A JSONArray object.</returns>
  public JSONArray put(int value) {
    put(value);
    return this;
  }

    /// <summary>Put or replace a bool value in the JSONArray. @param index
    /// subscript The subscript. If the index is greater than the length of
    /// the JSONArray, then null elements will be added as necessary to pad
    /// it out. @param value A bool value. @return this. @exception
    /// System.Collections.Generic.KeyNotFoundException The index must not
    /// be negative.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <param name='value'>Not documented yet.</param>
    /// <returns>A JSONArray object.</returns>
  public JSONArray put(int index, bool value) {
    put(index, (value));
    return this;
  }

    /// <summary>Put or replace a double value. @param index subscript The
    /// subscript. If the index is greater than the length of the
    /// JSONArray, then null elements will be added as necessary to pad it
    /// out. @param value A double value. @return this. @exception
    /// System.Collections.Generic.KeyNotFoundException The index must not
    /// be negative.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <param name='value'>Not documented yet.</param>
    /// <returns>A JSONArray object.</returns>
  public JSONArray put(int index, double value) {
    put(index, (value));
    return this;
  }

    /// <summary>Put or replace an int value. @param index subscript The
    /// subscript. If the index is greater than the length of the
    /// JSONArray, then null elements will be added as necessary to pad it
    /// out. @param value An int value. @return this. @exception
    /// System.Collections.Generic.KeyNotFoundException The index must not
    /// be negative.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <param name='value'>Not documented yet.</param>
    /// <returns>A JSONArray object.</returns>
  public JSONArray put(int index, int value) {
    put(index, (value));
    return this;
  }

    /// <summary>Put or replace an _object value in the JSONArray. @param
    /// index The subscript. If the index is greater than the length of the
    /// JSONArray, then null elements will be added as necessary to pad it
    /// out. @param value An _object value. @return this. @exception
    /// System.Collections.Generic.KeyNotFoundException The index must not
    /// be negative. @exception NullPointerException The value must not be
    /// null.</summary>
    /// <param name='index'>Not documented yet.</param>
    /// <param name='value'>Not documented yet.</param>
    /// <returns>A JSONArray object.</returns>
  public JSONArray put(int index, Object value) {
    if (index < 0) {
 throw new System.Collections.Generic.KeyNotFoundException("JSONArray["+
        index +
          "] not found.");
 }
    else if (value == null) {
 throw new ArgumentNullException();
  } else if (index < length()) {
      myArrayList[index]=value;
    } else {
      while (index != length()) {
        put(null);
      }
      put(value);
    }
    return this;
  }

    /// <summary>Append an _object value. @param value An _object value.
    /// The value should be a Boolean, Double, Integer, JSONArray,
    /// JSObject, or string, or the JSONObject.NULL _object. @return
    /// this.</summary>
    /// <param name='value'>Not documented yet.</param>
    /// <returns>A JSONArray object.</returns>
  public JSONArray put(Object value) {
    myArrayList.Add(value);
    return this;
  }

    /// <summary>* Removes the item at the specified index. Added by Peter
    /// O. 2013-04-05.</summary>
    /// <param name='index'>Not documented yet.</param>
  public void removeAt(int index) {
    myArrayList.RemoveAt(index);
  }

    /// <summary>Produce a JSONObject by combining a JSONArray of names
    /// with the values of this JSONArray. @param names A JSONArray
    /// containing a list of key strings. These will be paired with the
    /// values. @return A JSONObject, or null if there are no names or if
    /// this JSONArray has no values.</summary>
    /// <param name='names'>Not documented yet.</param>
    /// <returns>A JSONObject object.</returns>
  public JSONObject toJSONObject(JSONArray names) {
    if (names == null || names.Length == 0 || length() == 0) {
 return null;
}
    var jo = new JSONObject();
    for (int i = 0; i < names.Length; i += 1) {
      jo.put(names.getString(i), opt(i));
    }
    return jo;
  }

    /// <summary>Make an JSON external form _string of this JSONArray. For
    /// compactness, no unnecessary whitespace is added. Warning: This
    /// method assumes that the data structure is acyclical. @return a
    /// printable, displayable, transmittable representation of the
    /// array.</summary>
    /// <returns>A string object.</returns>
  public override string ToString() {
    return '[' + join(",") + ']';
  }

    /// <summary>
    /// Make a prettyprinted JSON _string of this JSONArray.
    /// Warning: This method assumes that the data structure is
    /// non-cyclical. @param indentFactor The number of spaces
    /// to add to each level of indentation. @return a
    /// printable, displayable, transmittable representation of
    /// the _object, beginning with
    /// <code>[</code>
    ///  &nbsp;
    /// <small>(left bracket)</small>
    ///  and ending with
    /// <code>]</code>
    ///  &nbsp;
    /// <small>(right bracket)</small>
    /// </summary>
    /// <param name='indentFactor'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  public string ToString(int indentFactor) {
    return ToString(indentFactor, 0);
  }

    /// <summary>Make a prettyprinted _string of this JSONArray. Warning:
    /// This method assumes that the data structure is non-cyclical. @param
    /// indentFactor The number of spaces to add to each level of
    /// indentation. @param indent The indention of the top level. @return
    /// a printable, displayable, transmittable representation of the
    /// array.</summary>
    /// <param name='indentFactor'>Not documented yet.</param>
    /// <param name='indent'>Not documented yet.</param>
    /// <returns>A string object.</returns>
  internal string ToString(int indentFactor, int indent) {
    int i;
    Object o;
    string pad = "";
    var sb = new StringBuilder();
    indent += indentFactor;
    for (i = 0; i < indent; i += 1) {
      pad += ' ';
    }
    sb.Append("[\n");
    for (i = 0; i < myArrayList.Count; i += 1) {
      if (i > 0) {
        sb.Append(",\n");
      }
      sb.Append(pad);
      o = myArrayList[i];
      if (o == null) {
        sb.Append("null");
      } else if (o is string) {
        sb.Append(JSONObject.quote((string) o));
      } else if (o is int || o is double || o is float || o is long || o is
        decimal || o is short || o is byte || o is UInt16 || o is UInt32 ||
        o is UInt64) {
        sb.Append(JSONObject.numberToString(o));
      } else if (o is JSONObject) {
        sb.Append(((JSONObject)o).ToString(indentFactor, indent));
      } else if (o is JSONArray) {
        sb.Append(((JSONArray)o).ToString(indentFactor, indent));
      } else {
        sb.Append(o.ToString());
      }
    }
    sb.Append(']');
    return sb.ToString();
  }
}
}
