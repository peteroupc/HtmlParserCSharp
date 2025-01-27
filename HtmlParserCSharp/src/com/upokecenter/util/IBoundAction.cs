/*
Written in 2013 by Peter Occil.
Any copyright to this work is released to the Public Domain.
In case this is not possible, this work is also
licensed under the Unlicense: https://unlicense.org/

*/
using System;

namespace Com.Upokecenter.Util {
  /// <summary>Not documented yet.</summary>
  /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
  public interface IBoundAction<T> {
    /// <summary>Not documented yet.</summary>
    void Action(Object thisObject, params T[] parameters);
  }
}
