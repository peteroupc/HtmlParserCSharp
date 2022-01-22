/*
Written in 2013 by Peter Occil.
Any copyright to this work is released to the Public Domain.
In case this is not possible, this work is also
licensed under Creative Commons Zero (CC0):
https://creativecommons.org/publicdomain/zero/1.0/

*/
using System;

namespace Com.Upokecenter.Util {
  /// <summary>Not documented yet.</summary>
  /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
  public interface IAction<T> {
    /// <summary>Does an arbitrary Action. @param parameters An array of
    /// parameters that the Action accepts.</summary>
    void Action (params T[] parameters);
  }
}
