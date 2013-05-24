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
 * A JSONObject is an unordered collection of name/value pairs. Its
 * external form is a _string wrapped in curly braces with colons between the
 * names and values, and commas between the values and names. The internal form
 * is an _object having get() and opt() methods for accessing the values by name,
 * and put() methods for adding or replacing values by name. The values can be
 * any of these types: Boolean, JSONArray, JSONObject, Number, string, or the
 * JSONObject.NULL _object.
 * <p>
 * The constructor can convert an external form _string into an internal form
 * Java _object. The ToString() method creates an external form _string.
 * <p>
 * A get() method returns a value if one can be found, and  exception
 * if one cannot be found. An opt() method returns a default value instead of
 * throwing an exception, and so is useful for obtaining optional values.
 * <p>
 * The generic get() and opt() methods return an _object, which you can cast or
 * query for type. There are also typed get() and opt() methods that do type
 * checking and type coersion for you.
 * <p>
 * The texts produced by the ToString() methods are very strict.
 * The constructors are more forgiving in the texts they will accept:
 * <ul>
 * <li>An extra <code>,</code>&nbsp;<small>(comma)</small> may appear just before the closing brace.</li>
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
public class JSONObject {

	/**
	 * JSONObject.NULL is equivalent to the value that JavaScript calls null,
	 * whereas Java's null is equivalent to the value that JavaScript calls
	 * undefined.
	 */
	private sealed class Null {

		/**
		 * Make a Null _object.
		 */
		public Null() {
		}


		/**
		 * There is only intended to be a single instance of the NULL _object,
		 * so the clone method returns itself.
		 * @return     NULL.
		 */
		public Object clone() {
			return this;
		}

		/**
		 * A Null _object is equal to the null value and to itself.
		 * @param _object    An _object to test for nullness.
		 * @return true if the _object parameter is the JSONObject.NULL _object
		 *  or null.
		 */
		public override sealed bool Equals(object _object) {
			return _object == null || _object == this;
		}


		public override sealed int GetHashCode(){unchecked{
			return 0;
		}}


		/**
		 * Get the "null" _string value.
		 * @return The _string "null".
		 */
		public override sealed string ToString() {
			return "null";
		}
	}


	/**
	 * Produce a _string in double quotes with backslash sequences in all the
	 * right places. (Modified so that the slash character is not escaped.)
	 * @param _string A string
	 * @return  A string correctly formatted for insertion in a JSON message.
	 */
	public static string quote(string _string) {
		if (_string == null || _string.Length == 0)
			return "\"\"";

		char         c;
		int          i;
		int          len = _string.Length;
		StringBuilder sb = new StringBuilder(len + 4);
		string       t;

		sb.Append('"');
		for (i = 0; i < len; i += 1) {
			c = _string[i];
			switch (c) {
			case '\\':
			case '"':// Peter O: '/' removed as needing escaping
				sb.Append('\\');
				sb.Append(c);
				break;
			case '\b':
				sb.Append("\\b");
				break;
			case '\t':
				sb.Append("\\t");
				break;
			case '\n':
				sb.Append("\\n");
				break;
			case '\f':
				sb.Append("\\f");
				break;
			case '\r':
				sb.Append("\\r");
				break;
			default:
				if (c < ' ') {
					t = "000" + ((int)(c)).ToString("x",CultureInfo.InvariantCulture);
					sb.Append("\\u" + t.Substring(t.Length - 4));
				} else {
					sb.Append(c);
				}
				break;
			}
		}
		sb.Append('"');
		return sb.ToString();
	}

	/**
	 * The hash map where the JSONObject's properties are kept.
	 */
	private PeterO.Support.LenientDictionary<string, Object> myHashMap;

	/**
	 * It is sometimes more convenient and less ambiguous to have a NULL
	 * _object than to use Java's null value.
	 * JSONObject.NULL.Equals(null) returns true.
	 * JSONObject.NULL.ToString() returns "null".
	 */
	public static readonly Object NULL = new Null();

	/**
	 * Produce a _string from a number.
	 * @param  n A Number
	 * @return A string.
	 * @exception ArithmeticException JSON can only serialize finite numbers.
	 */
	static public string numberToString(Object n)  {
		if (
				(n is float &&
						(Single.IsInfinity((float)n) || Single.IsNaN((float)n))) ||
						(n is double &&
								(Double.IsInfinity((double)n) || Double.IsNaN((double)n))))
			throw new ArithmeticException(
					"JSON can only serialize finite numbers.");

		// Shave off trailing zeros and decimal point, if possible.

		string s = toLowerCaseAscii(n.ToString());
		if (s.IndexOf('e') < 0 && s.IndexOf('.') > 0) {
			while (s.EndsWith("0",StringComparison.Ordinal)) {
				s = s.Substring(0,(s.Length - 1)-(0));
			}
			if (s.EndsWith(".",StringComparison.Ordinal)) {
				s = s.Substring(0,(s.Length - 1)-(0));
			}
		}
		return s;
	}


	public static string toLowerCaseAscii(string s){
		if(s==null)return null;
		int len=s.Length;
		char c=(char)0;
		bool hasUpperCase=false;
		for(int i=0;i<len;i++){
			c=s[i];
			if(c>='A' && c<='Z'){
				hasUpperCase=true;
				break;
			}
		}
		if(!hasUpperCase)
			return s;
		StringBuilder builder=new StringBuilder();
		for(int i=0;i<len;i++){
			c=s[i];
			if(c>='A' && c<='Z'){
				builder.Append((char)(c+0x20));
			} else {
				builder.Append(c);
			}
		}
		return builder.ToString();
	}


	/**
	 * Construct an empty JSONObject.
	 */
	public JSONObject() {
		myHashMap = new PeterO.Support.LenientDictionary<string, Object>();
	}


	private void addCommentIfAny(JSONTokener x) {
		if((x.getOptions()&OPTION_ADD_COMMENTS)!=0){
			// Parse and add the comment if any
			string comment=x.nextComment();
			if(comment.Length>0){
				myHashMap.Add("@comment", comment);
			}
		}		
	}

	/**
	 * Construct a JSONObject from a JSONTokener.
	 * @param x A JSONTokener _object containing the source _string.
	 * @ if there is a syntax error in the source _string.
	 */
	public JSONObject(JSONTokener x) : this() {
		int c;
		string key;
		if (x.next() == '%') {
			x.unescape();
		}
		x.back();
		if (x.nextClean() != '{')
			throw x.syntaxError("A JSONObject must begin with '{'");
		while (true) {
			addCommentIfAny(x);
			c = x.nextClean();
			switch (c) {
			case -1:
				throw x.syntaxError("A JSONObject must end with '}'");
			case '}':
				return;
			default:
				x.back();
				addCommentIfAny(x);
				key = x.nextValue().ToString();
				if((x.getOptions() & OPTION_NO_DUPLICATES)!=0 &&
						myHashMap.ContainsKey(key)){
					throw x.syntaxError("Key already exists: "+key);
				}
				break;
			}
			addCommentIfAny(x);
			if (x.nextClean() != ':')
				throw x.syntaxError("Expected a ':' after a key");
			// NOTE: Will overwrite existing value. --Peter O.
			addCommentIfAny(x);
			myHashMap.Add(key, x.nextValue());
			addCommentIfAny(x);
			switch (x.nextClean()) {
			case ',':
				addCommentIfAny(x);
				if (x.nextClean() == '}')
					return;
				x.back();
				break;
			case '}':
				return;
			default:
				throw x.syntaxError("Expected a ',' or '}'");
			}
		}
	}


	/**
	 * Construct a JSONObject from a Map.
	 * @param map A map _object that can be used to initialize the contents of
	 *  the JSONObject.
	 */
	public JSONObject(IDictionary<string, Object> map) {
		myHashMap = new PeterO.Support.LenientDictionary<string, Object>(map);
	}


	/**
	 * No duplicates are allowed in the JSON _string.
	 */
	public static readonly int OPTION_NO_DUPLICATES = 1;
	/**
	 * Will parse Shell-style comments (beginning with "#").
	 */
	public static readonly int OPTION_SHELL_COMMENTS = 2;
	/**
	 * Will add a "@comment" property to all objects with
	 * comments associated with them. Only applies to JSON
	 * objects, not JSON arrays.
	 */
	public static readonly int OPTION_ADD_COMMENTS = 4;


	/**
	 * Construct a JSONObject from a _string.
	 *
	 * @param _string    A _string beginning
	 *  with <code>{</code>&nbsp;<small>(left brace)</small> and ending
	 *  with <code>}</code>&nbsp;<small>(right brace)</small>.
	 * @param option Options for parsing the _string. Currently
	 * OPTION_NO_DUPLICATES, OPTION_SHELL_COMMENTS, and/or
	 * OPTION_ADD_COMMENTS.
	 *  @exception Json.InvalidJsonException The _string must be properly formatted.
	 */
	public JSONObject(string _string, int options)  {
		this(new JSONTokener(_string,options));
	}

	/**
	 * Construct a JSONObject from a _string.
	 *
	 * @param _string    A _string beginning
	 *  with <code>{</code>&nbsp;<small>(left brace)</small> and ending
	 *  with <code>}</code>&nbsp;<small>(right brace)</small>.
	 *  @exception Json.InvalidJsonException The _string must be properly formatted.
	 */
	public JSONObject(string _string) : this(_string,0) {
	}

	/**
	 * Accumulate values under a key. It is similar to the put method except
	 * that if there is already an _object stored under the key then a
	 * JSONArray is stored under the key to hold all of the accumulated values.
	 * If there is already a JSONArray, then the new value is appended to it.
	 * In contrast, the put method replaces the previous value.
	 *
	 * @param key   A key _string.
	 * @param value An _object to be accumulated under the key.
	 * @return this.
	 * @ if the key is null
	 */
	public JSONObject accumulate(string key, Object value)
			 {
		JSONArray a;
		Object o = opt(key);
		if (o == null) {
			put(key, value);
		} else if (o is JSONArray) {
			a = (JSONArray)o;
			a.put(value);
		} else {
			a = new JSONArray();
			a.put(o);
			a.put(value);
			put(key, a);
		}
		return this;
	}


	public override sealed bool Equals(object obj) {
		if (this == obj)
			return true;
		if (obj == null)
			return false;
		if (GetType() != obj.GetType())
			return false;
		JSONObject other = (JSONObject) obj;
		if (myHashMap == null) {
			if (other.myHashMap != null)
				return false;
		} else if (!myHashMap.Equals(other.myHashMap))
			return false;
		return true;
	}


	/**
	 * Get the value _object associated with a key.
	 *
	 *
	 * @param key   A key _string.
	 * @return      The _object associated with the key.
	 * @exception System.Collections.Generic.KeyNotFoundException if the key is not found.
	 */
	public Object this[string i] { get { return get(i); } set { put(i,value); } }
public Object get(string key)  {
		Object o = opt(key);
		if (o == null)
			throw new System.Collections.Generic.KeyNotFoundException("JSONObject[" +
					quote(key) + "] not found.");
		return o;
	}


	/**
	 * Get the bool value associated with a key.
	 * @param key   A key _string.
	 * @return      The truth.
	 *
	 * @exception System.Collections.Generic.KeyNotFoundException if the key is not found.
	 * @exception InvalidCastException
	 *  if the value is not a Boolean or the string "true" or "false".
	 */
	public bool getBoolean(string key)
			 {
		Object o = get(key);
		if (o == (object)false || o.Equals("false"))
			return false;
		else if (o == (object)true || o.Equals("true"))
			return true;
		throw new InvalidCastException("JSONObject[" +
				quote(key) + "] is not a Boolean.");
	}


	/**
	 * Get the double value associated with a key.
	 * @param key   A key _string.
	 * @return      The numeric value.
	 * @exception System.Collections.Generic.KeyNotFoundException if the key is not found or
	 *  if the value is a Number _object.
	 * @exception FormatException if the value cannot be converted to a
	 *  number.
	 */
	public double getDouble(string key)
			 {
		Object o = get(key);
		if (o is int || o is double || o is float || o is long || o is decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o is UInt64)
			return Convert.ToDouble(o);
		if (o is string)
			return Double.Parse((string) o,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint|NumberStyles.AllowExponent,CultureInfo.InvariantCulture);
		throw new FormatException("JSONObject[" +
				quote(key) + "] is not a number.");
	}


	/**
	 * Get the HashMap the holds that contents of the JSONObject.
	 * @return The getHashMap.
	 */
	internal PeterO.Support.LenientDictionary<string, Object> getHashMap() {
		return myHashMap;
	}


	/**
	 * Get the int value associated with a key.
	 * @param key   A key _string.
	 * @return      The integer value.
	 * @exception System.Collections.Generic.KeyNotFoundException if the key is not found
	 * @exception FormatException if the value cannot be converted to a number.
	 */
	public int getInt(string key)
			 {
		Object o = get(key);
		if (o is int || o is double || o is float || o is long || o is decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o is UInt64)
			return Convert.ToInt32(o);
		return (int)getDouble(key);
	}


	/**
	 * Get the JSONArray value associated with a key.
	 * @param key   A key _string.
	 * @return      A JSONArray which is the value.
	 * @exception System.Collections.Generic.KeyNotFoundException if the key is not found or
	 *  if the value is not a JSONArray.
	 */
	public JSONArray getJSONArray(string key)  {
		Object o = get(key);
		if (o is JSONArray)
			return (JSONArray)o;
		throw new System.Collections.Generic.KeyNotFoundException("JSONObject[" +
				quote(key) + "] is not a JSONArray.");
	}


	/**
	 * Get the JSONObject value associated with a key.
	 *
	 * @param key   A key _string.
	 * @return      A JSONObject which is the value.
	 * @exception System.Collections.Generic.KeyNotFoundException if the key is not found or
	 *  if the value is not a JSONObject.
	 */
	public JSONObject getJSONObject(string key)  {
		Object o = get(key);
		if (o is JSONObject)
			return (JSONObject)o;
		throw new System.Collections.Generic.KeyNotFoundException("JSONObject[" +
				quote(key) + "] is not a JSONObject.");
	}


	/**
	 * Get the _string associated with a key.
	 * @param key   A key _string.
	 * @return      A _string which is the value.
	 * @exception System.Collections.Generic.KeyNotFoundException if the key is not found.
	 */
	public string getString(string key)  {
		return get(key).ToString();
	}


	/**
	 * Determine if the JSONObject contains a specific key.
	 * @param key   A key _string.
	 * @return      true if the key exists in the JSONObject.
	 */
	public bool has(string key) {
		return myHashMap.ContainsKey(key);
	}


	public override sealed int GetHashCode(){unchecked{
		 int prime = 31;
		int result = 1;
		result = prime * result
				+ ((myHashMap == null) ? 0 : myHashMap.GetHashCode());
		return result;
	}}


	/**
	 * Determine if the value associated with the key is null or if there is
	 *  no value.
	 * @param key   A key _string.
	 * @return      true if there is no value associated with the key or if
	 *  the value is the JSONObject.NULL _object.
	 */
	public bool isNull(string key) {
		return JSONObject.NULL.Equals(opt(key));
	}


	/**
	 * Get an enumeration of the keys of the JSONObject.
	 *
	 * PeterO: Changed to Iterable
	 *
	 * @return An iterator of the keys.
	 */
	public IEnumerable<string> keys() {
		return myHashMap.Keys;
	}

	/**
	 * Get the number of keys stored in the JSONObject.
	 *
	 * @return The number of keys in the JSONObject.
	 */
	public int Length { get { return length(); }}
public int length() {
		return myHashMap.Count;
	}


	/**
	 * Produce a JSONArray containing the names of the elements of this
	 * JSONObject.
	 * @return A JSONArray containing the key strings, or null if the JSONObject
	 * is empty.
	 */
	public JSONArray names() {
		JSONArray ja = new JSONArray();
		foreach(var key in keys()) {
			ja.put(key);
		}
		if (ja.Length == 0)
			return null;
		return ja;
	}


	/**
	 * Get an optional value associated with a key.
	 * @param key   A key _string.
	 * @return      An _object which is the value, or null if there is no value.
	 * @exception NullPointerException  The key must not be null.
	 */
	public Object opt(string key)  {
		if (key == null)
			throw new ArgumentNullException("Null key");
		return myHashMap[key];
	}


	/**
	 * Get an optional bool associated with a key.
	 * It returns false if there is no such key, or if the value is not
	 * (object)true or the string "true".
	 *
	 * @param key   A key _string.
	 * @return      The truth.
	 */
	public bool optBoolean(string key) {
		return optBoolean(key, false);
	}


	/**
	 * Get an optional bool associated with a key.
	 * It returns the defaultValue if there is no such key, or if it is not
	 * a Boolean or the string "true" or "false".
	 *
	 * @param key              A key _string.
	 * @param defaultValue     The default.
	 * @return      The truth.
	 */
	public bool optBoolean(string key, bool defaultValue) {
		Object o = opt(key);
		if (o != null) {
			if (o == (object)false || o.Equals("false"))
				return false;
			else if (o == (object)true || o.Equals("true"))
				return true;
		}
		return defaultValue;
	}


	/**
	 * Get an optional double associated with a key,
	 * or NaN if there is no such key or if its value is not a number.
	 * If the value is a _string, an attempt will be made to evaluate it as
	 * a number.
	 *
	 * @param key   A _string which is the key.
	 * @return      An _object which is the value.
	 */
	public double optDouble(string key)  {
		return optDouble(key, Double.NaN);
	}


	/**
	 * Get an optional double associated with a key, or the
	 * defaultValue if there is no such key or if its value is not a number.
	 * If the value is a _string, an attempt will be made to evaluate it as
	 * a number.
	 *
	 * @param key   A key _string.
	 * @param defaultValue     The default.
	 * @return      An _object which is the value.
	 */
	public double optDouble(string key, double defaultValue)  {
		Object o = opt(key);
		if (o != null) {
			if (o is int || o is double || o is float || o is long || o is decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o is UInt64)
				return Convert.ToDouble(o);
			try {
				return Double.Parse((string) o,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint|NumberStyles.AllowExponent,CultureInfo.InvariantCulture);
			}
			catch(FormatException){}
		}
		return defaultValue;
	}


	/**
	 * Get an optional int value associated with a key,
	 * or zero if there is no such key or if the value is not a number.
	 * If the value is a _string, an attempt will be made to evaluate it as
	 * a number.
	 *
	 * @param key   A key _string.
	 * @return      An _object which is the value.
	 */
	public int optInt(string key) {
		return optInt(key, 0);
	}


	/**
	 * Get an optional int value associated with a key,
	 * or the default if there is no such key or if the value is not a number.
	 * If the value is a _string, an attempt will be made to evaluate it as
	 * a number.
	 *
	 * @param key   A key _string.
	 * @param defaultValue     The default.
	 * @return      An _object which is the value.
	 */
	public int optInt(string key, int defaultValue) {
		Object o = opt(key);
		if (o != null) {
			if (o is int || o is double || o is float || o is long || o is decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o is UInt64)
				return Convert.ToInt32(o);
			try {
				return Int32.Parse((string)o,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
			} catch (FormatException){}
		}
		return defaultValue;
	}


	/**
	 * Get an optional JSONArray associated with a key.
	 * It returns null if there is no such key, or if its value is not a
	 * JSONArray.
	 *
	 * @param key   A key _string.
	 * @return      A JSONArray which is the value.
	 */
	public JSONArray optJSONArray(string key) {
		Object o = opt(key);
		if (o is JSONArray)
			return (JSONArray) o;
		return null;
	}


	/**
	 * Get an optional JSONObject associated with a key.
	 * It returns null if there is no such key, or if its value is not a
	 * JSONObject.
	 *
	 * @param key   A key _string.
	 * @return      A JSONObject which is the value.
	 */
	public JSONObject optJSONObject(string key) {
		Object o = opt(key);
		if (o is JSONObject)
			return (JSONObject)o;
		return null;
	}


	/**
	 * Get an optional _string associated with a key.
	 * It returns an empty _string if there is no such key. If the value is not
	 * a _string and is not null, then it is coverted to a _string.
	 *
	 * @param key   A key _string.
	 * @return      A _string which is the value.
	 */
	public string optString(string key) {
		return optString(key, "");
	}


	/**
	 * Get an optional _string associated with a key.
	 * It returns the defaultValue if there is no such key.
	 *
	 * @param key   A key _string.
	 * @param defaultValue     The default.
	 * @return      A _string which is the value.
	 */
	public string optString(string key, string defaultValue) {
		Object o = opt(key);
		if (o != null)
			return o.ToString();
		return defaultValue;
	}


	/**
	 * Put a key/bool pair in the JSONObject.
	 *
	 * @param key   A key _string.
	 * @param value A bool which is the value.
	 * @return this.
	 */
	public JSONObject put(string key, bool value) {
		put(key, (value));
		return this;
	}


	/**
	 * Put a key/double pair in the JSONObject.
	 *
	 * @param key   A key _string.
	 * @param value A double which is the value.
	 * @return this.
	 */
	public JSONObject put(string key, double value) {
		put(key, (value));
		return this;
	}


	/**
	 * Put a key/int pair in the JSONObject.
	 *
	 * @param key   A key _string.
	 * @param value An int which is the value.
	 * @return this.
	 */
	public JSONObject put(string key, int value) {
		put(key, (value));
		return this;
	}


	/**
	 * Put a key/value pair in the JSONObject. If the value is null,
	 * then the key will be removed from the JSONObject if it is present.
	 * @param key   A key _string.
	 * @param value An _object which is the value. It should be of one of these
	 *  types: Boolean, Double, Integer, JSONArray, JSONObject, string, or the
	 *  JSONObject.NULL _object.
	 * @return this.
	 * @exception NullPointerException The key must be non-null.
	 */
	public JSONObject put(string key, Object value)  {
		if (key == null)
			throw new ArgumentNullException("Null key.");
		if (value != null) {
			myHashMap.Add(key, value);
		} else {
			remove(key);
		}
		return this;
	}


	/**
	 * Put a key/value pair in the JSONObject, but only if the
	 * value is non-null.
	 * @param key   A key _string.
	 * @param value An _object which is the value. It should be of one of these
	 *  types: Boolean, Double, Integer, JSONArray, JSONObject, string, or the
	 *  JSONObject.NULL _object.
	 * @return this.
	 * @exception NullPointerException The key must be non-null.
	 */
	public JSONObject putOpt(string key, Object value)  {
		if (value != null) {
			put(key, value);
		}
		return this;
	}

	/**
	 * Remove a name and its value, if present.
	 * @param key The name to be removed.
	 * @return The value that was associated with the name,
	 * or null if there was no value.
	 */
	public Object remove(string key) {
		return myHashMap.Remove(key);
	}

	/**
	 * Produce a JSONArray containing the values of the members of this
	 * JSONObject.
	 * @param names A JSONArray containing a list of key strings. This
	 * determines the sequence of the values in the result.
	 * @return A JSONArray of values.
	 */
	public JSONArray toJSONArray(JSONArray names) {
		if (names == null || names.Length == 0)
			return null;
		JSONArray ja = new JSONArray();
		for (int i = 0; i < names.Length; i += 1) {
			ja.put(opt(names.getString(i)));
		}
		return ja;
	}

	/**
	 * Make an JSON external form _string of this JSONObject. For compactness, no
	 * unnecessary whitespace is added.
	 * <p>
	 * Warning: This method assumes that the data structure is acyclical.
	 *
	 * @return a printable, displayable, portable, transmittable
	 *  representation of the _object, beginning
	 *  with <code>{</code>&nbsp;<small>(left brace)</small> and ending
	 *  with <code>}</code>&nbsp;<small>(right brace)</small>.
	 */
	public override sealed string ToString() {
		Object       o = null;
		StringBuilder sb = new StringBuilder();

		sb.Append('{');
		foreach(var s in keys()){
			if (o != null) {
				sb.Append(',');
			}
			o = myHashMap[s];
			if (o != null) {
				sb.Append(quote(s));
				sb.Append(':');
				if (o is string) {
					sb.Append(quote((string)o));
				} else if (o is int || o is double || o is float || o is long || o is decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o is UInt64) {
					sb.Append(numberToString(o));
				} else {
					sb.Append(o.ToString());
				}
			}
		}
		sb.Append('}');
		return sb.ToString();
	}


	/**
	 * Make a prettyprinted JSON external form _string of this JSONObject.
	 * <p>
	 * Warning: This method assumes that the data structure is acyclical.
	 * @param indentFactor The number of spaces to add to each level of
	 *  indentation.
	 * @return a printable, displayable, portable, transmittable
	 *  representation of the _object, beginning
	 *  with <code>{</code>&nbsp;<small>(left brace)</small> and ending
	 *  with <code>}</code>&nbsp;<small>(right brace)</small>.
	 */
	public string ToString(int indentFactor) {
		return ToString(indentFactor, 0);
	}


	/**
	 * Make a prettyprinted JSON _string of this JSONObject.
	 * <p>
	 * Warning: This method assumes that the data structure is acyclical.
	 * @param indentFactor The number of spaces to add to each level of
	 *  indentation.
	 * @param indent The indentation of the top level.
	 * @return a printable, displayable, transmittable
	 *  representation of the _object, beginning
	 *  with <code>{</code>&nbsp;<small>(left brace)</small> and ending
	 *  with <code>}</code>&nbsp;<small>(right brace)</small>.
	 */
	internal string ToString(int indentFactor, int indent) {
		int          i;
		string       pad = "";
		StringBuilder sb = new StringBuilder();
		indent += indentFactor;
		for (i = 0; i < indent; i += 1) {
			pad += ' ';
		}
		sb.Append("{\n");
		foreach(var s in keys()) {
			Object o = myHashMap[s];
			if (o != null) {
				if (sb.Length > 2) {
					sb.Append(",\n");
				}
				sb.Append(pad);
				sb.Append(quote(s));
				sb.Append(": ");
				if (o is string) {
					sb.Append(quote((string)o));
				} else if (o is int || o is double || o is float || o is long || o is decimal || o is short || o is byte || o is UInt16 || o is UInt32 || o is UInt64) {
					sb.Append(numberToString(o));
				} else if (o is JSONObject) {
					sb.Append(((JSONObject)o).ToString(indentFactor, indent));
				} else if (o is JSONArray) {
					sb.Append(((JSONArray)o).ToString(indentFactor, indent));
				} else {
					sb.Append(o.ToString());
				}
			}
		}
		sb.Append('}');
		return sb.ToString();
	}

	public static void main(string[] args) {
		string json="["+
			      "{ # foo\n\"foo-key\":\"foo-value\"},\n"+
			      "{ /* This is a\n # multiline comment.*/\n\"bar-key\":\"bar-value\"}]";
		Console.WriteLine(json);
	    JSONArray obj=new JSONArray(json,
	            JSONObject.OPTION_SHELL_COMMENTS | // Support SHELL-style comments
	            JSONObject.OPTION_ADD_COMMENTS // Incorporate comments in the JSON _object
	    );
	    Console.WriteLine(obj); // Output the JSON _object
		// Objects with comments associated with them will
		// now contain a "@comment" key; get the JSON Pointers
		// (RFC6901) to these objects and remove the "@comment" keys.
		IDictionary<string,Object> pointers=JSONPointer.getPointersWithKeyAndRemove(obj,"@comment");
		// For each JSON Pointer, get its corresponding _object.
		// They will always be JSONObjects.
		foreach(var pointer in pointers.Keys){
			JSONObject subobj=(JSONObject)JSONPointer.getObject(obj,pointer);
			Console.WriteLine(subobj); // Output the _object
			Console.WriteLine(pointers[pointer]); // Output the key's value
		}
	}
}
}
