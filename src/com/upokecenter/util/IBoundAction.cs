/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
using System;

namespace com.upokecenter.util {
    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="T:com.upokecenter.util.IBoundAction`1"]/*'/>
public interface IBoundAction<T> {
    /// <include file='../../../../docs.xml'
    /// path='docs/doc[@name="M:com.upokecenter.util.IBoundAction`1.action(System.Object,`0[])"]/*'/>
   void action(Object thisObject, params T[] parameters);
}
}
