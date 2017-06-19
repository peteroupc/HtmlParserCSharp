/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
namespace com.upokecenter.json {
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public sealed class JSONPointer {
  public static JSONPointer fromPointer(Object obj, string pointer) {
    var index = 0;
    if (pointer == null) {
 throw new ArgumentNullException("pointer");
}
    if (pointer.Length == 0) {
 return new JSONPointer(obj, pointer);
}
    while (true) {
      if (obj is PeterO.Cbor.CBORObject) {
        if (index>= pointer.Length || pointer[index]!='/') {
 throw new ArgumentException(pointer);
}
        ++index;
        var value = new int[] { 0 };
        int newIndex = readPositiveInteger(pointer, index, value);
        if (value[0]< 0) {
          if (index<pointer.Length && pointer[index]=='-' &&
              (index+1==pointer.Length || pointer[index+1]=='/')) {
 // Index at the end of the array
            return new JSONPointer(obj,"-");
 }
          throw new ArgumentException(pointer);
        }
        if (newIndex == pointer.Length) {
 return new JSONPointer(obj, pointer.Substring(index));
} else {
          obj=((PeterO.Cbor.CBORObject)obj)[value[0]];
          index = newIndex;
        }
        index = newIndex;
      } else if (obj is PeterO.Cbor.CBORObject) {
        if (obj.Equals(PeterO.Cbor.CBORObject.NULL)) {
 throw new System.Collections.Generic.KeyNotFoundException(pointer);
}
        if (index>= pointer.Length || pointer[index]!='/') {
 throw new ArgumentException(pointer);
}
        ++index;
        string key = null;
        int oldIndex = index;
        var tilde = false;
        while (index<pointer.Length) {
          int c = pointer[index];
          if (c=='/') {
            break;
          }
          if (c=='~') {
            tilde = true;
            break;
          }
          ++index;
        }
        if (!tilde) {
          key = pointer.Substring(oldIndex, (index)-(oldIndex));
        } else {
          index = oldIndex;
          var sb = new StringBuilder();
          while (index<pointer.Length) {
            int c = pointer[index];
            if (c=='/') {
              break;
            }
            if (c=='~') {
              if (index + 1<pointer.Length) {
                if (pointer[index+1]=='1') {
                  index+=2;
                  sb.Append('/');
                  continue;
                } else if (pointer[index+1]=='0') {
                  index+=2;
                  sb.Append('~');
                  continue;
                }
              }
              throw new ArgumentException(pointer);
            } else {
              sb.Append((char)c);
            }
            ++index;
          }
          key = sb.ToString();
        }
        if (index == pointer.Length) {
 return new JSONPointer(obj, key);
} else {
          obj=((PeterO.Cbor.CBORObject)obj)[key];
        }
      } else {
 throw new System.Collections.Generic.KeyNotFoundException(pointer);
}
    }
  }

    /// <summary>* Gets the JSON _object referred to by a JSON Pointer
    /// according to RFC6901. The syntax for pointers is:
    /// <pre>'/' KEY '/' KEY [...]</pre> where KEY represents a key into
    /// the JSON _object or its sub-objects in the hierarchy. For example,
    /// <pre>/foo/2/bar</pre> means the same as
    /// <pre>obj['foo'][2]['bar']</pre> in JavaScript. If "~" and "/" occur
    /// in a key, they must be escaped with "~0" and "~1", respectively, in
    /// a JSON pointer. @param obj An _object, especially a PeterO.Cbor.CBORObject or
    /// PeterO.Cbor.CBORObject @param pointer A JSON pointer according to RFC 6901.
    /// @return An _object within the specified JSON _object, or _obj_ if
    /// pointer is the empty _string. @ if the pointer is null. @ if the
    /// pointer is invalid @ if there is no JSON _object at the given
    /// pointer, or if _obj_ is not of type PeterO.Cbor.CBORObject or PeterO.Cbor.CBORObject, unless
    /// pointer is the empty _string.</summary>
    /// <param name='obj'>The parameter <paramref name='obj'/> is not
    /// documented yet.</param>
    /// <param name='pointer'>The parameter <paramref name='pointer'/> is
    /// not documented yet.</param>
    /// <returns>An arbitrary object.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='pointer'/> is null.</exception>
  public static Object getObject(Object obj, string pointer) {
    if (pointer == null) {
 throw new ArgumentNullException("pointer");
}
    return (pointer.Length == 0) ? (obj) :
      (JSONPointer.fromPointer(obj, pointer).getValue());
  }
  private static int readPositiveInteger(
      string str,
      int index,
      int[] result) {
    var haveNumber = false;
    var haveZeros = false;
    int oldIndex = index;
    result[0]=-1;
    while (index<str.Length) {  // skip zeros
      int c = str[index++];
      if (c!='0') {
        --index;
        break;
      }
      if (haveZeros) {
        --index;
        return index;
      }
      haveNumber = true;
      haveZeros = true;
    }
    long value = 0;
    while (index<str.Length) {
      int number = str[index++];
      if (number>= '0' && number<= '9') {
        value=(value*10)+(number-'0');
        haveNumber = true;
        if (haveZeros) {
 return oldIndex + 1;
}
      } else {
        --index;
        break;
      }
      if (value>Int32.MaxValue) {
 return index-1;
}
    }
    if (!haveNumber) {
 return index;
}
    result[0]=(int)value;
    return index;
  }

  private string refValue;

  private Object jsonobj;

  private JSONPointer(Object jsonobj, string refValue) {
    #if DEBUG
if (!(refValue != null)) {
 throw new InvalidOperationException("doesn't satisfy refValue!=null");
}
#endif
    this.jsonobj = jsonobj;
    this.refValue = refValue;
  }

  public bool exists() {
    if (jsonobj is PeterO.Cbor.CBORObject) {
      if (refValue.Equals("-")) {
 return false;
}
      int
  value = Int32.Parse(refValue, NumberStyles.AllowLeadingSign,
    CultureInfo.InvariantCulture);
      return value >= 0 && value<((PeterO.Cbor.CBORObject)jsonobj).Length;
    } else if (jsonobj is PeterO.Cbor.CBORObject) {
 return ((PeterO.Cbor.CBORObject)jsonobj).has(refValue);
} else {
 return refValue.Length == 0;
}
  }

    /// <summary>* Gets an index into the specified _object, if the _object
    /// is an array and is not greater than the array's length. @return The
    /// index contained in this instance, or -1 if the _object isn't a JSON
    /// array or is greater than the array's length.</summary>
    /// <returns>A 32-bit signed integer.</returns>
  public int getIndex() {
    if (jsonobj is PeterO.Cbor.CBORObject) {
      if (refValue.Equals("-")) {
 return ((PeterO.Cbor.CBORObject)jsonobj).Length;
}
      int
  value = Int32.Parse(refValue, NumberStyles.AllowLeadingSign,
    CultureInfo.InvariantCulture);
      return (value< 0) ? (-1) : ((value>((PeterO.Cbor.CBORObject)jsonobj).Length) ? (-1):
        (value));
    } else {
 return -1;
}
  }

  public string getKey() {
    return refValue;
  }

  public Object getParent() {
    return jsonobj;
  }

  public Object getValue() {
    if (refValue.Length == 0) {
 return jsonobj;
}
    if (jsonobj is PeterO.Cbor.CBORObject) {
      int index = getIndex();
      if (index >= 0 && index<((PeterO.Cbor.CBORObject)jsonobj).Length) {
 return ((PeterO.Cbor.CBORObject)jsonobj)[index];
} else {
 return null;
}
    } else if (jsonobj is PeterO.Cbor.CBORObject) {
 return ((PeterO.Cbor.CBORObject)jsonobj)[refValue];
} else {
 return (refValue.Length == 0) ? jsonobj : null;
}
  }

    /// <summary>Gets all children of the specified JSON _object that
    /// contain the specified key. The method will not remove matching
    /// keys. As an example, consider this _object:
    /// <pre>[{"key":"value1","foo":"foovalue"},
    /// {"key":"value2","bar":"barvalue"}, {"baz":"bazvalue"}]</pre> If
    /// getPointersToKey is called on this _object with a keyToFind called
    /// "key", we get the following Map as the return value:
    /// <pre>{ "/0" => "value1", // "/0" points to {"foo":"foovalue"} "/1"
    /// => "value2" // "/1" points to {"bar":"barvalue"} }</pre> and the
    /// JSON _object will change to the following:
    /// <pre>[{"foo":"foovalue"}, {"bar":"barvalue"},
    /// {"baz","bazvalue"}]</pre> @param root _object to search @param
    /// keyToFind the key to search for. @return a map:
    /// <list>
    /// <item>The keys in the map are JSON Pointers to the objects within
    /// <i>root</i> that contained a key named
    /// <i>keyToFind</i>. To get the actual JSON _object, call
    /// JSONPointer.getObject, passing
    /// <i>root</i> and the pointer as arguments.</item>
    /// <item>The values in the map are the values of each of those keys
    /// named
    /// <i>keyToFind</i>.</item></list> The JSON Pointers are relative to
    /// the root _object.</summary>
    /// <param name='root'>The parameter <paramref name='root'/> is not
    /// documented yet.</param>
    /// <param name='keyToFind'>The parameter <paramref name='keyToFind'/>
    /// is not documented yet.</param>
    /// <returns>An IDictionary(string, Object) object.</returns>
  public static IDictionary<string, Object>
    getPointersWithKeyAndRemove(Object root, string keyToFind) {
    IDictionary<string, Object> list = new
      PeterO.Support.LenientDictionary<string, Object>();
    getPointersWithKey(root,keyToFind,"",list,true);
    return list;
  }

    /// <summary>Gets all children of the specified JSON _object that
    /// contain the specified key. The method will remove matching keys. As
    /// an example, consider this _object:
    /// <pre>[{"key":"value1","foo":"foovalue"},
    /// {"key":"value2","bar":"barvalue"}, {"baz":"bazvalue"}]</pre> If
    /// getPointersToKey is called on this _object with a keyToFind called
    /// "key", we get the following Map as the return value:
    /// <pre>{ "/0" => "value1", // "/0" points to
    /// {"key":"value1","foo":"foovalue"} "/1" => "value2" // "/1" points
    /// to {"key":"value2","bar":"barvalue"} }</pre> and the JSON _object
    /// will remain unchanged. @param root _object to search @param
    /// keyToFind the key to search for. @return a map:
    /// <list>
    /// <item>The keys in the map are JSON Pointers to the objects within
    /// <i>root</i> that contained a key named
    /// <i>keyToFind</i>. To get the actual JSON _object, call
    /// JSONPointer.getObject, passing
    /// <i>root</i> and the pointer as arguments.</item>
    /// <item>The values in the map are the values of each of those keys
    /// named
    /// <i>keyToFind</i>.</item></list> The JSON Pointers are relative to
    /// the root _object.</summary>
    /// <param name='root'>The parameter <paramref name='root'/> is not
    /// documented yet.</param>
    /// <param name='keyToFind'>The parameter <paramref name='keyToFind'/>
    /// is not documented yet.</param>
    /// <returns>An IDictionary(string, Object) object.</returns>
  public static IDictionary<string, Object> getPointersWithKey(Object root,
    string keyToFind) {
    IDictionary<string, Object> list = new
      PeterO.Support.LenientDictionary<string, Object>();
    getPointersWithKey(root,keyToFind,"",list,false);
    return list;
  }

  private static void getPointersWithKey(
      Object root,
      string keyToFind,
      string currentPointer,
      IDictionary<string, Object> pointerList,
      bool remove) {
    if (root is PeterO.Cbor.CBORObject) {
      PeterO.Cbor.CBORObject rootObj=((PeterO.Cbor.CBORObject)root);
      if (rootObj.has(keyToFind)) {
        // Key found in this _object,
        // add this _object's JSON pointer
        Object pointerKey = rootObj[keyToFind];
        pointerList.Add(currentPointer, pointerKey);
        // and remove the key from the _object
        // if necessary
        if (remove) {
 rootObj.remove(keyToFind);
}
      }
      // Search the key's values
      foreach (var key in rootObj.keys()) {
        string ptrkey = key;
        ptrkey=ptrkey.Replace("~","~0");
        ptrkey=ptrkey.Replace("/","~1");
        getPointersWithKey(rootObj[key], keyToFind,
            currentPointer+"/"+ptrkey,pointerList,remove);
      }
  } else if (root is PeterO.Cbor.CBORObject) {
      for (int i = 0;i<((PeterO.Cbor.CBORObject)root).Length; ++i) {
        string ptrkey = Convert.ToString(i, CultureInfo.InvariantCulture);
        getPointersWithKey(((PeterO.Cbor.CBORObject)root)[i], keyToFind,
            currentPointer+"/"+ptrkey,pointerList,remove);
      }
    }
  }
}
}
