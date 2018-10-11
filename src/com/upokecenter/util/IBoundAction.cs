/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
using System;
namespace com.upokecenter.util {
    /// <summary>Not documented yet.</summary>
    /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
public interface IBoundAction<T> {
    /// <summary>Not documented yet.</summary>
   void action(Object thisObject, params T[] parameters);
}
}
