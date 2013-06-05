/*
Written in 2013 by Peter Occil.  
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://upokecenter.com/d/
*/
namespace com.upokecenter.util {
using System;
using System.Collections.Generic;


public sealed class IndexedObjectList<T> {
  private IList<T> strongrefs=new List<T>();
  private IList<WeakReference> weakrefs=new List<WeakReference>();
  private Object syncRoot=new Object();


  // Remove the strong reference, but keep the weak
  // reference; the index becomes no good when the
  // _object is garbage collected
  public T receiveObject(int index){
    if(index<0)return default(T);
    T ret=default(T);
    lock(syncRoot){
      if(index>=strongrefs.Count)return default(T);
      ret=strongrefs[index];
      if(ret==null)throw new InvalidOperationException();
      strongrefs[index]=default(T);
    }
    return ret;
  }

  // Keep a strong reference and a weak reference
  public int sendObject(T value){
    if(value==null)return -1; // Special case for null
    lock(syncRoot){
      for(int i=0;i<strongrefs.Count;i++){
        if(strongrefs[i]==null){
          if(weakrefs[i]==null ||
              weakrefs[i].Target==null){
            // If the _object is garbage collected
            // the index is available for use again
            //Console.WriteLine("Adding _object %d",i);
            strongrefs[i]=value;
            weakrefs[i]=new WeakReference(value);
            return i;
          }
        }
      }
      // Keep a strong and weak reference of
      // the same _object
      int ret=strongrefs.Count;
      //Console.WriteLine("Adding _object %d",ret);
      strongrefs.Add(value);
      weakrefs.Add(new WeakReference(value));
      return ret;
    }
  }
}

}
