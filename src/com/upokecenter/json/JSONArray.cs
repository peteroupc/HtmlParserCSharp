// Modified by Peter O. to use generics, among
// other things; also moved from org.json.  
// Still in the public domain;
// public domain dedication: http://creativecommons.org/publicdomain/zero/1.0/

namespace com.upokecenter.json {
using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;




/**
 * A JSONArray is an ordered sequence of values. Its external form is a _string
 * wrapped in square brackets with commas between the values. The internal form
 * is an _object having get() and opt() methods for accessing the values by
 * index, and put() methods for adding or replacing values. The values can be
 * any of these types: Boolean, JSONArray, JSONObject, Number, string, or the
 * JSONObject.NULL _object.
 * <p>
 * The constructor can convert a JSON external form _string into an
 * internal form Java _object. The ToString() method creates an external
 * form _string.
 * <p>
 * A get() method returns a value if one can be found, and  exception
 * if one cannot be found. An opt() method returns a default value instead of
 * throwing an exception, and so is useful for obtaining optional values.
 * <p>
 * The generic get() and opt() methods return an _object which you can cast or
 * query for type. There are also typed get() and opt() methods that do typing
 * checking and type coersion for you.
 * <p>
 * The texts produced by the ToString() methods are very strict.
 * The constructors are more forgiving in the texts they will accept.
 * <ul>
 * <li>An extra <code>,</code>&nbsp;<small>(comma)</small> may appear just before the closing bracket.</li>
 * <li>The value null will be inserted when there is <code>,</code>&nbsp;<small>(comma)</small> elision.</li>
 * <li>Strings may be quoted with <code>'</code>&nbsp;<small>(single quote)</small>.</li>
 * <li>Strings do not need to be quoted at all if they do not contain leading
 *     or trailing spaces, and if they do not contain any of these characters:
 *     <code>{ } [ ] / \ : , ' "</code></li>
 * <li>Numbers may have the 0- (octal) or 0x- (hex) prefix.</li>
 * </ul>
 * <p>
 * Public Domain 2002 JSON.org
 * @author JSON.org
 * @version 0.1
 */
public class JSONArray {

  /**
   * The getArrayList where the JSONArray's properties are kept.
   */
  private List<Object> myArrayList;


  /**
   * Construct an empty JSONArray.
   */
  public JSONArray() {
    myArrayList = new List<Object>();
  }


  /**
   * Construct a JSONArray from a Collection.
   * @param collection     A Collection.
   */
  public JSONArray(/*covar*/ICollection<Object> collection) {
    myArrayList = new List<Object>(collection);
  }


  /**
   * Construct a JSONArray from a JSONTokener.
   * @param x A JSONTokener
   * @exception Json.InvalidJsonException A JSONArray must start with '['
   * @exception Json.InvalidJsonException Expected a ',' or ']'
   */
  public JSONArray(JSONTokener x) : this() {
    if (x.nextClean() != '[')
      throw x.syntaxError("A JSONArray must start with '['");
    if (x.nextClean() == ']')
      return;
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
        if (x.nextClean() == ']'){
          if((x.getOptions() & JSONObject.OPTION_TRAILING_COMMAS)==0){
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
    foreach(var str in collection){
      myArrayList.Add(str);
    }
  }
  /**
   * Construct a JSONArray from a source _string.
   * @param _string     A _string that begins with
   * <code>[</code>&nbsp;<small>(left bracket)</small>
   *  and ends with <code>]</code>&nbsp;<small>(right bracket)</small>.
   * @exception Json.InvalidJsonException The _string must conform to JSON syntax.
   */
  public JSONArray(string _string, int options)  {
    this(new JSONTokener(_string,options));
  }

  /**
   * Construct a JSONArray from a source _string.
   * @param _string     A _string that begins with
   * <code>[</code>&nbsp;<small>(left bracket)</small>
   *  and ends with <code>]</code>&nbsp;<small>(right bracket)</small>.
   * @param option Options for parsing the _string. Currently
   * JSONObject.OPTION_NO_DUPLICATES, JSONObject.OPTION_SHELL_COMMENTS, and/or
   * JSONObject.OPTION_ADD_COMMENTS.
   * @exception Json.InvalidJsonException The _string must conform to JSON syntax.
   */
  public JSONArray(string _string) : this(_string,0) {
  }

  public JSONArray add(int index, bool value) {
    add(index,(value));
    return this;
  }


  public JSONArray add(int index, double value) {
    add(index,(value));
    return this;
  }


  public JSONArray add(int index, int value) {
    add(index,(value));
    return this;
  }


  public JSONArray add(int index, Object value) {
    if (index < 0)
      throw new System.Collections.Generic.KeyNotFoundException("JSONArray[" + index +
          "] not found.");
    else if (value == null)
      throw new ArgumentNullException();
    else {
      myArrayList.Insert(index,value);
    }
    return this;
  }


  public override bool Equals(object obj) {
    if (this == obj)
      return true;
    if (obj == null)
      return false;
    if (GetType() != obj.GetType())
      return false;
    JSONArray other = (JSONArray) obj;
    if (myArrayList == null) {
      if (other.myArrayList != null)
        return false;
    } else if (!myArrayList.Equals(other.myArrayList))
      return false;
    return true;
  }


  /**
   * Get the _object value associated with an index.
   * @param index
   *  The index must be between 0 and length() - 1.
   * @return An _object value.
   * @exception System.Collections.Generic.KeyNotFoundException
   */
  public Object this[int i] { get { return get(i); } set { put(i,value); } }
public Object get(int index)  {
    Object o = opt(index);
    if (o == null)
      throw new System.Collections.Generic.KeyNotFoundException("JSONArray[" + index +
          "] not found.");
    return o;
  }


  /**
   * Get the ArrayList which is holding the elements of the JSONArray.
   * @return      The ArrayList.
   */
  List<Object> getArrayList() {
    return myArrayList;
  }


  /**
   * Get the bool value associated with an index.
   * The _string values "true" and "false" are converted to bool.
   * @param index The index must be between 0 and length() - 1.
   * @return      The truth.
   * @exception System.Collections.Generic.KeyNotFoundException if the index is not found
   * @exception InvalidCastException
   */
  public bool getBoolean(int index)
       {
    Object o = get(index);
    if (o == (object)false || o.Equals("false"))
      return false;
    else if (o == (object)true || o.Equals("true"))
      return true;
    throw new InvalidCastException("JSONArray[" + index +
        "] not a Boolean.");
  }


  /**
   * Get the double value associated with an index.
   * @param index The index must be between 0 and length() - 1.
   * @return      The value.
   * @exception System.Collections.Generic.KeyNotFoundException if the key is not found
   * @exception FormatException
   *  if the value cannot be converted to a number.
   *
   */
  public double getDouble(int index)
       {
    Object o = get(index);
    if (o is int || o is double || o is float || o is long || o is decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o is UInt64)
      return Convert.ToDouble( o);
    if (o is string)
      return Double.Parse((string) o,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint|NumberStyles.AllowExponent,CultureInfo.InvariantCulture);
    throw new FormatException("JSONObject[" +
        index + "] is not a number.");
  }


  /**
   * Get the int value associated with an index.
   *
   * @param index The index must be between 0 and length() - 1.
   * @return      The value.
   * @exception System.Collections.Generic.KeyNotFoundException if the key is not found
   * @exception FormatException
   *  if the value cannot be converted to a number.
   *
   */
  public int getInt(int index)
       {
    Object o = get(index);
    if (o is int || o is double || o is float || o is long || o is decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o is UInt64)
      return Convert.ToInt32(o);
    return (int)getDouble(index);
  }

  /**
   * Get the JSONArray associated with an index.
   * @param index The index must be between 0 and length() - 1.
   * @return      A JSONArray value.
   * @exception System.Collections.Generic.KeyNotFoundException if the index is not found or if the
   * value is not a JSONArray
   */
  public JSONArray getJSONArray(int index)  {
    Object o = get(index);
    if (o is JSONArray)
      return (JSONArray)o;
    throw new System.Collections.Generic.KeyNotFoundException("JSONArray[" + index +
        "] is not a JSONArray.");
  }


  /**
   * Get the JSONObject associated with an index.
   * @param index subscript
   * @return      A JSONObject value.
   * @exception System.Collections.Generic.KeyNotFoundException if the index is not found or if the
   * value is not a JSONObject
   */
  public JSONObject getJSONObject(int index)  {
    Object o = get(index);
    if (o is JSONObject)
      return (JSONObject)o;
    throw new System.Collections.Generic.KeyNotFoundException("JSONArray[" + index +
        "] is not a JSONObject.");
  }


  /**
   * Get the _string associated with an index.
   * @param index The index must be between 0 and length() - 1.
   * @return      A _string value.
   * @exception System.Collections.Generic.KeyNotFoundException
   */
  public string getString(int index)  {
    return get(index).ToString();
  }


  public override int GetHashCode(){unchecked{
     int prime = 31;
    int result = 1;
    result = prime * result
        + ((myArrayList == null) ? 0 : myArrayList.GetHashCode());
    return result;
  }}


  /**
   * Determine if the value is null.
   * @param index The index must be between 0 and length() - 1.
   * @return true if the value at the index is null, or if there is no value.
   */
  public bool isNull(int index) {
    Object o = opt(index);
    // Peter O. 3/15/2013: Replaced equals(null) with equals(JSONObject.NULL)
    return o == null || o.Equals(JSONObject.NULL);
  }


  /**
   * Make a _string from the contents of this JSONArray. The separator _string
   * is inserted between each element.
   * Warning: This method assumes that the data structure is acyclical.
   * @param separator A _string that will be inserted between the elements.
   * @return a _string.
   */
  public string join(string separator) {
    int i;
    Object o;
    StringBuilder sb = new StringBuilder();
    for (i = 0; i < myArrayList.Count; i += 1) {
      if (i > 0) {
        sb.Append(separator);
      }
      o = myArrayList[i];
      if (o == null) {
        sb.Append("null");
      } else if (o is string) {
        sb.Append(JSONObject.quote((string)o));
      } else if (o is int || o is double || o is float || o is long || o is decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o is UInt64) {
        sb.Append(JSONObject.numberToString(o));
      } else {
        sb.Append(o.ToString());
      }
    }
    return sb.ToString();
  }


  /**
   * Get the length of the JSONArray.
   *
   * @return The length (or size).
   */
  public int Length { get { return length(); }}
public int length() {
    return myArrayList.Count;
  }


  /**
   * Get the optional _object value associated with an index.
   * @param index The index must be between 0 and length() - 1.
   * @return      An _object value, or null if there is no
   *              _object at that index.
   */
  public Object opt(int index) {
    if (index < 0 || index >= length())
      return null;
    else
      return myArrayList[index];
  }


  /**
   * Get the optional bool value associated with an index.
   * It returns false if there is no value at that index,
   * or if the value is not (object)true or the string "true".
   *
   * @param index The index must be between 0 and length() - 1.
   * @return      The truth.
   */
  public bool optBoolean(int index)  {
    return optBoolean(index, false);
  }


  /**
   * Get the optional bool value associated with an index.
   * It returns the defaultValue if there is no value at that index or if it is not
   * a Boolean or the string "true" or "false".
   *
   * @param index The index must be between 0 and length() - 1.
   * @param defaultValue     A bool default.
   * @return      The truth.
   */
  public bool optBoolean(int index, bool defaultValue)  {
    Object o = opt(index);
    if (o != null) {
      if (o == (object)false || o.Equals("false"))
        return false;
      else if (o == (object)true || o.Equals("true"))
        return true;
    }
    return defaultValue;
  }


  /**
   * Get the optional double value associated with an index.
   * NaN is returned if the index is not found,
   * or if the value is not a number and cannot be converted to a number.
   *
   * @param index The index must be between 0 and length() - 1.
   * @return      The value.
   */
  public double optDouble(int index) {
    return optDouble(index, Double.NaN);
  }


  /**
   * Get the optional double value associated with an index.
   * The defaultValue is returned if the index is not found,
   * or if the value is not a number and cannot be converted to a number.
   *
   * @param index subscript
   * @param defaultValue     The default value.
   * @return      The value.
   */
  public double optDouble(int index, double defaultValue) {
    Object o = opt(index);
    if (o != null) {
      if (o is int || o is double || o is float || o is long || o is decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o is UInt64)
        return Convert.ToDouble( o);
      try {
        return Double.Parse((string) o,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint|NumberStyles.AllowExponent,CultureInfo.InvariantCulture);
      }
      catch (FormatException){}
    }
    return defaultValue;
  }


  /**
   * Get the optional int value associated with an index.
   * Zero is returned if the index is not found,
   * or if the value is not a number and cannot be converted to a number.
   *
   * @param index The index must be between 0 and length() - 1.
   * @return      The value.
   */
  public int optInt(int index) {
    return optInt(index, 0);
  }


  /**
   * Get the optional int value associated with an index.
   * The defaultValue is returned if the index is not found,
   * or if the value is not a number and cannot be converted to a number.
   * @param index The index must be between 0 and length() - 1.
   * @param defaultValue     The default value.
   * @return      The value.
   */
  public int optInt(int index, int defaultValue) {
    Object o = opt(index);
    if (o != null) {
      if (o is int || o is double || o is float || o is long || o is decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o is UInt64)
        return Convert.ToInt32(o);
      try {
        return Int32.Parse((string)o,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
      }
      catch (FormatException){}
    }
    return defaultValue;
  }


  /**
   * Get the optional JSONArray associated with an index.
   * @param index subscript
   * @return      A JSONArray value, or null if the index has no value,
   * or if the value is not a JSONArray.
   */
  public JSONArray optJSONArray(int index) {
    Object o = opt(index);
    if (o is JSONArray)
      return (JSONArray)o;
    return null;
  }


  /**
   * Get the optional JSONObject associated with an index.
   * Null is returned if the key is not found, or null if the index has
   * no value, or if the value is not a JSONObject.
   *
   * @param index The index must be between 0 and length() - 1.
   * @return      A JSONObject value.
   */
  public JSONObject optJSONObject(int index) {
    Object o = opt(index);
    if (o is JSONObject)
      return (JSONObject)o;
    return null;
  }


  /**
   * Get the optional _string value associated with an index. It returns an
   * empty _string if there is no value at that index. If the value
   * is not a _string and is not null, then it is coverted to a _string.
   *
   * @param index The index must be between 0 and length() - 1.
   * @return      A string value.
   */
  public string optString(int index){
    return optString(index, "");
  }


  /**
   * Get the optional _string associated with an index.
   * The defaultValue is returned if the key is not found.
   *
   * @param index The index must be between 0 and length() - 1.
   * @param defaultValue     The default value.
   * @return      A string value.
   */
  public string optString(int index, string defaultValue){
    Object o = opt(index);
    if (o != null)
      return o.ToString();
    return defaultValue;
  }


  /**
   * Append a bool value.
   *
   * @param value A bool value.
   * @return this.
   */
  public JSONArray put(bool value) {
    put((value));
    return this;
  }


  /**
   * Append a double value.
   *
   * @param value A double value.
   * @return this.
   */
  public JSONArray put(double value) {
    put((value));
    return this;
  }

  /**
   * Append an int value.
   *
   * @param value An int value.
   * @return this.
   */
  public JSONArray put(int value) {
    put((value));
    return this;
  }
  /**
   * Put or replace a bool value in the JSONArray.
   * @param index subscript The subscript. If the index is greater than the length of
   *  the JSONArray, then null elements will be added as necessary to pad
   *  it out.
   * @param value A bool value.
   * @return this.
   * @exception System.Collections.Generic.KeyNotFoundException The index must not be negative.
   */
  public JSONArray put(int index, bool value) {
    put(index, (value));
    return this;
  }
  /**
   * Put or replace a double value.
   * @param index subscript The subscript. If the index is greater than the length of
   *  the JSONArray, then null elements will be added as necessary to pad
   *  it out.
   * @param value A double value.
   * @return this.
   * @exception System.Collections.Generic.KeyNotFoundException The index must not be negative.
   */
  public JSONArray put(int index, double value) {
    put(index, (value));
    return this;
  }
  /**
   * Put or replace an int value.
   * @param index subscript The subscript. If the index is greater than the length of
   *  the JSONArray, then null elements will be added as necessary to pad
   *  it out.
   * @param value An int value.
   * @return this.
   * @exception System.Collections.Generic.KeyNotFoundException The index must not be negative.
   */
  public JSONArray put(int index, int value) {
    put(index, (value));
    return this;
  }

  /**
   * Put or replace an _object value in the JSONArray.
   * @param index The subscript. If the index is greater than the length of
   *  the JSONArray, then null elements will be added as necessary to pad
   *  it out.
   * @param value An _object value.
   * @return this.
   * @exception System.Collections.Generic.KeyNotFoundException The index must not be negative.
   * @exception NullPointerException The value must not be null
   */
  public JSONArray put(int index, Object value)
       {
    if (index < 0)
      throw new System.Collections.Generic.KeyNotFoundException("JSONArray[" + index +
          "] not found.");
    else if (value == null)
      throw new ArgumentNullException();
    else if (index < length()) {
      myArrayList[index]=value;
    } else {
      while (index != length()) {
        put(null);
      }
      put(value);
    }
    return this;
  }


  /**
   * Append an _object value.
   * @param value An _object value.  The value should be a
   *  Boolean, Double, Integer, JSONArray, JSObject, or string, or the
   *  JSONObject.NULL _object.
   * @return this.
   */
  public JSONArray put(Object value) {
    myArrayList.Add(value);
    return this;
  }


  /**
   * 
   * Removes the item at the specified index.
   * Added by Peter O. 2013-04-05
   * 
   */
  public void removeAt(int index){
    myArrayList.RemoveAt(index);
  }


  /**
   * Produce a JSONObject by combining a JSONArray of names with the values
   * of this JSONArray.
   * @param names A JSONArray containing a list of key strings. These will be
   * paired with the values.
   * @return A JSONObject, or null if there are no names or if this JSONArray
   * has no values.
   */
  public JSONObject toJSONObject(JSONArray names) {
    if (names == null || names.Length == 0 || length() == 0)
      return null;
    JSONObject jo = new JSONObject();
    for (int i = 0; i < names.Length; i += 1) {
      jo.put(names.getString(i), opt(i));
    }
    return jo;
  }

  /**
   * Make an JSON external form _string of this JSONArray. For compactness, no
   * unnecessary whitespace is added.
   * Warning: This method assumes that the data structure is acyclical.
   *
   * @return a printable, displayable, transmittable
   *  representation of the array.
   */
  public override string ToString() {
    return '[' + join(",") + ']';
  }


  /**
   * Make a prettyprinted JSON _string of this JSONArray.
   * Warning: This method assumes that the data structure is non-cyclical.
   * @param indentFactor The number of spaces to add to each level of
   *  indentation.
   * @return a printable, displayable, transmittable
   *  representation of the _object, beginning
   *  with <code>[</code>&nbsp;<small>(left bracket)</small> and ending
   *  with <code>]</code>&nbsp;<small>(right bracket)</small>.
   */
  public string ToString(int indentFactor) {
    return ToString(indentFactor, 0);
  }


  /**
   * Make a prettyprinted _string of this JSONArray.
   * Warning: This method assumes that the data structure is non-cyclical.
   * @param indentFactor The number of spaces to add to each level of
   *  indentation.
   * @param indent The indention of the top level.
   * @return a printable, displayable, transmittable
   *  representation of the array.
   */
  internal string ToString(int indentFactor, int indent) {
    int i;
    Object o;
    string pad = "";
    StringBuilder sb = new StringBuilder();
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
      } else if (o is int || o is double || o is float || o is long || o is decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o is UInt64) {
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
