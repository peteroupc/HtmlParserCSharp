/*
Written in 2013 by Peter Occil.  Released to the public domain.
Public domain dedication: http://creativecommons.org/publicdomain/zero/1.0/
 */
namespace com.upokecenter.json {
using System;
using System.Text;
using System.Globalization;



public sealed class JSONPointer {

	public static JSONPointer fromPointer(Object obj, string pointer){
		int index=0;
		if(pointer==null)throw new ArgumentNullException("pointer");
		if(pointer.Length==0)
			return new JSONPointer(obj,pointer);
		while(true){
			if(obj is JSONArray){
				if(index>=pointer.Length || pointer[index]!='/')
					throw new ArgumentException(pointer);
				index++;
				int[] value=new int[]{0};
				int newIndex=readPositiveInteger(pointer,index,value);
				if(value[0]<0){
					if(index<pointer.Length && pointer[index]=='-' &&
							(index+1==pointer.Length || pointer[index+1]=='/'))
						// Index at the end of the array
						return new JSONPointer(obj,"-");
					throw new ArgumentException(pointer);
				}
				if(newIndex==pointer.Length)
					return new JSONPointer(obj,pointer.Substring(index));
				else {
					obj=((JSONArray)obj)[value[0]];
					index=newIndex;
				}
				index=newIndex;
			} else if(obj is JSONObject){
				if(obj.Equals(JSONObject.NULL))
					throw new System.Collections.Generic.KeyNotFoundException(pointer);
				if(index>=pointer.Length || pointer[index]!='/')
					throw new ArgumentException(pointer);
				index++;
				string key=null;
				int oldIndex=index;
				bool tilde=false;
				while(index<pointer.Length){
					int c=pointer[index];
					if(c=='/') {
						break;
					}
					if(c=='~'){
						tilde=true;
						break;
					}
					index++;
				}
				if(!tilde){
					key=pointer.Substring(oldIndex,(index)-(oldIndex));
				} else {
					index=oldIndex;
					StringBuilder sb=new StringBuilder();
					while(index<pointer.Length){
						int c=pointer[index];
						if(c=='/') {
							break;
						}
						if(c=='~'){
							if(index+1<pointer.Length){
								if(pointer[index+1]=='1'){
									index+=2;
									sb.Append('/');
									continue;
								} else if(pointer[index+1]=='0'){
									index+=2;
									sb.Append('~');
									continue;
								}
							}
							throw new ArgumentException(pointer);
						} else {
							sb.Append((char)c);
						}
						index++;
					}
					key=sb.ToString();
				}
				if(index==pointer.Length)
					return new JSONPointer(obj,key);
				else {
					obj=((JSONObject)obj)[key];
				}
			} else
				throw new System.Collections.Generic.KeyNotFoundException(pointer);
		}
	}

	/**
	 * 
	 * Gets the JSON _object referred to by a JSON Pointer
	 * according to RFC6901.
	 * 
	 * The syntax for pointers is:
	 * <pre>
	 *  '/' KEY '/' KEY [...]
	 * </pre>
	 * where KEY represents a key into the JSON _object
	 * or its sub-objects in the hierarchy. For example,
	 * <pre>
	 *  /foo/2/bar
	 * </pre>
	 * means the same as
	 * <pre>
	 *  obj['foo'][2]['bar']
	 * </pre>
	 * in JavaScript.
	 * 
	 * If "~" and "/" occur in a key, they must be escaped
	 * with "~0" and "~1", respectively, in a JSON pointer.
	 * 
	 * @param obj An _object, especially a JSONObject or JSONArray
	 * @param pointer A JSON pointer according to RFC 6901.
	 * @return An _object within the specified JSON _object,
	 * or _obj_ if pointer is the empty _string.
	 * @ if the pointer is null.
	 * @ if the pointer is invalid
	 * @ if there is no JSON _object
	 *  at the given pointer, or if _obj_ is not of type
	 *  JSONObject or JSONArray, unless pointer is the empty _string
	 */
	public static Object getObject(Object obj, string pointer){
		if(pointer==null)throw new ArgumentNullException("pointer");
		if(pointer.Length==0)return obj;
		return JSONPointer.fromPointer(obj,pointer).getValue();
	}
	private static int readPositiveInteger(
			string str, int index, int[] result){
		bool haveNumber=false;
		bool haveZeros=false;
		int oldIndex=index;
		result[0]=-1;
		while(index<str.Length){ // skip zeros
			int c=str[index++];
			if(c!='0'){
				index--;
				break;
			}
			if(haveZeros){
				index--;
				return index;
			}
			haveNumber=true;
			haveZeros=true;
		}
		long value=0;
		while(index<str.Length){
			int number=str[index++];
			if(number>='0' && number<='9'){
				value=(value*10)+(number-'0');
				haveNumber=true;
				if(haveZeros)
					return oldIndex+1;
			} else {
				index--;
				break;
			}
			if(value>Int32.MaxValue)
				return index-1;
		}
		if(!haveNumber)
			return index;
		result[0]=(int)value;
		return index;
	}

	private string refValue;

	private Object jsonobj;

	private JSONPointer(Object jsonobj, string refValue){
		#if DEBUG
if(!(refValue!=null))throw new InvalidOperationException("doesn't satisfy refValue!=null");
#endif
		this.jsonobj=jsonobj;
		this.refValue=refValue;
	}

	public bool exists(){
		if(jsonobj is JSONArray){
			if(refValue.Equals("-"))return false;
			int value=Int32.Parse(refValue,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
			return (value>=0 && value<((JSONArray)jsonobj).Length);
		} else if(jsonobj is JSONObject)
			return ((JSONObject)jsonobj).has(refValue);
		else
			return (refValue.Length==0);
	}

	/**
	 * 
	 * Gets an index into the specified _object, if the _object
	 * is an array and is not greater than the array's length.
	 * 
	 * @return The index contained in this instance, or -1 if
	 * the _object isn't a JSON array or is greater than the
	 * array's length.
	 */
	public int getIndex(){
		if(jsonobj is JSONArray){
			if(refValue.Equals("-"))return ((JSONArray)jsonobj).Length;
			int value=Int32.Parse(refValue,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
			if(value<0)return -1;
			if(value>((JSONArray)jsonobj).Length)return -1;
			return value;
		} else
			return -1;
	}

	public string getKey(){
		return refValue;
	}

	public Object getParent(){
		return jsonobj;
	}

	public Object getValue(){
		if(refValue.Length==0)
			return jsonobj;
		if(jsonobj is JSONArray){
			int index=getIndex();
			if(index>=0 && index<((JSONArray)jsonobj).Length)
				return ((JSONArray)jsonobj)[index];
			else
				return null;
		} else if(jsonobj is JSONObject)
			return ((JSONObject)jsonobj)[refValue];
		else
			return (refValue.Length==0) ? jsonobj : null;
	}
}

}
