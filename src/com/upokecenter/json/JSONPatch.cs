/*
Written in 2013 by Peter Occil.  
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://upokecenter.com/d/
*/
namespace com.upokecenter.json {
using System;




public class JSONPatch {

  private static Object addOperation(
      Object o,
      string opStr,
      string path,
      Object value
      ){
    if(path==null)throw new ArgumentException("patch "+opStr);
    if(path.Length==0){
      o=value;
    } else {
      JSONPointer pointer=JSONPointer.fromPointer(o, path);
      if(pointer.getParent() is JSONArray){
        int index=pointer.getIndex();
        if(index<0)
          throw new ArgumentException("patch "+opStr+" path");
        ((JSONArray)pointer.getParent()).add(index,value);
      } else if(pointer.getParent() is JSONObject){
        string key=pointer.getKey();
        ((JSONObject)pointer.getParent()).put(key,value);
      } else
        throw new ArgumentException("patch "+opStr+" path");
    }
    return o;
  }

  private static Object cloneJsonObject(Object o){
    try {
      if(o is JSONArray)
        return new JSONArray(o.ToString());
      if(o is JSONObject)
        return new JSONObject(o.ToString());
    } catch(Json.InvalidJsonException){
      return o;
    }
    return o;
  }

  private static string getString(JSONObject o, string key){
    try {
      return o.getString(key);
    } catch(System.Collections.Generic.KeyNotFoundException){
      return null;
    }
  }

  public static Object patch(Object o, JSONArray patch){
    // clone the _object in case of failure
    o=cloneJsonObject(o);
    for(int i=0;i<patch.Length;i++){
      Object op=patch[i];
      if(!(op is JSONObject))
        throw new ArgumentException("patch");
      if(o==null)
        throw new InvalidOperationException("patch");
      JSONObject patchOp=(JSONObject)op;
      // NOTE: This algorithm requires "op" to exist
      // only once; the JSONObject, however, does not
      // allow duplicates
      string opStr=getString(patchOp,"op");
      if(opStr==null)
        throw new ArgumentException("patch");
      if("add".Equals(opStr)){
        // operation
        Object value=null;
        try {
          value=patchOp["value"];
        } catch(System.Collections.Generic.KeyNotFoundException){
          throw new ArgumentException("patch "+opStr+" value");
        }
        o=addOperation(o,opStr,getString(patchOp,"path"),value);
      } else if("replace".Equals(opStr)){
        // operation
        Object value=null;
        try {
          value=patchOp["value"];
        } catch(System.Collections.Generic.KeyNotFoundException){
          throw new ArgumentException("patch "+opStr+" value");
        }
        o=replaceOperation(o,opStr,getString(patchOp,"path"),value);
      } else if("remove".Equals(opStr)){
        // Remove operation
        string path=patchOp.getString("path");
        if(path==null)throw new ArgumentException("patch "+opStr+" path");
        if(path.Length==0) {
          o=null;
        } else {
          removeOperation(o,opStr,getString(patchOp,"path"));
        }
      } else if("move".Equals(opStr)){
        string path=patchOp.getString("path");
        if(path==null)throw new ArgumentException("patch "+opStr+" path");
        string fromPath=patchOp.getString("from");
        if(fromPath==null)throw new ArgumentException("patch "+opStr+" from");
        if(path.StartsWith(fromPath,StringComparison.Ordinal))
          throw new ArgumentException("patch "+opStr);
        Object movedObj=removeOperation(o,opStr,fromPath);
        o=addOperation(o,opStr,path,cloneJsonObject(movedObj));
      } else if("copy".Equals(opStr)){
        string path=patchOp.getString("path");
        if(path==null)throw new ArgumentException("patch "+opStr+" path");
        string fromPath=patchOp.getString("from");
        if(fromPath==null)throw new ArgumentException("patch "+opStr+" from");
        JSONPointer pointer=JSONPointer.fromPointer(o, path);
        if(!pointer.exists())
          throw new System.Collections.Generic.KeyNotFoundException("patch "+opStr+" "+fromPath);
        Object copiedObj=pointer.getValue();
        o=addOperation(o,opStr,path,cloneJsonObject(copiedObj));
      } else if("test".Equals(opStr)){
        string path=patchOp.getString("path");
        if(path==null)throw new ArgumentException("patch "+opStr+" path");
        Object value=null;
        try {
          value=patchOp["value"];
        } catch(System.Collections.Generic.KeyNotFoundException){
          throw new ArgumentException("patch "+opStr+" value");
        }
        JSONPointer pointer=JSONPointer.fromPointer(o, path);
        if(!pointer.exists())
          throw new System.Collections.Generic.KeyNotFoundException("patch "+opStr+" "+path);
        Object testedObj=pointer.getValue();
        if((testedObj==null) ? (value!=null) : !testedObj.Equals(value))
          throw new InvalidOperationException("patch "+opStr);
      }
    }
    return (o==null) ? JSONObject.NULL : o;
  }

  private static Object removeOperation(
      Object o,
      string opStr,
      string path){
    if(path==null)throw new ArgumentException("patch "+opStr);
    if(path.Length==0)
      return o;
    else {
      JSONPointer pointer=JSONPointer.fromPointer(o, path);
      if(!pointer.exists())
        throw new System.Collections.Generic.KeyNotFoundException("patch "+opStr+" "+path);
      o=pointer.getValue();
      if(pointer.getParent() is JSONArray){
        ((JSONArray)pointer.getParent()).removeAt(pointer.getIndex());
      } else if(pointer.getParent() is JSONObject){
        ((JSONObject)pointer.getParent()).remove(pointer.getKey());
      }
      return o;
    }
  }

  private static Object replaceOperation(
      Object o,
      string opStr,
      string path,
      Object value
      ){
    if(path==null)throw new ArgumentException("patch "+opStr);
    if(path.Length==0){
      o=value;
    } else {
      JSONPointer pointer=JSONPointer.fromPointer(o, path);
      if(!pointer.exists())
        throw new System.Collections.Generic.KeyNotFoundException("patch "+opStr+" "+path);
      if(pointer.getParent() is JSONArray){
        int index=pointer.getIndex();
        if(index<0)
          throw new ArgumentException("patch "+opStr+" path");
        ((JSONArray)pointer.getParent()).put(index,value);
      } else if(pointer.getParent() is JSONObject){
        string key=pointer.getKey();
        ((JSONObject)pointer.getParent()).put(key,value);
      } else
        throw new ArgumentException("patch "+opStr+" path");
    }
    return o;
  }
}

}
