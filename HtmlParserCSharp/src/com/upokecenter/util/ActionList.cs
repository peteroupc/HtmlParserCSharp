/*
Written in 2013 by Peter Occil.
Any copyright to this work is released to the Public Domain.
In case this is not possible, this work is also
licensed under the Unlicense: https://unlicense.org/

*/
using System;
using System.Collections.Generic;

namespace Com.Upokecenter.Util {
  /// <summary>A class for holding tasks that can be referred to by
  /// integer index.</summary>
  /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
  public sealed class ActionList<T> {
    private IList<IBoundAction<T>> actions;
    private IList<Object> boundObjects;
    private IList<T[]> postponeCall;
    private Object syncRoot = new Object();

    /// <summary>Initializes a new instance of the
    /// <see cref='ActionList'/> class.</summary>
    public ActionList() {
      this.actions = new List<IBoundAction<T>>();
      this.boundObjects = new List<Object>();
      this.postponeCall = new List<T[]>();
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='actionID'>The parameter <paramref name='actionID'/> is
    /// a 32-bit signed integer.</param>
    /// <param name='boundObject'>The parameter <paramref
    /// name='boundObject'/> is a Object object.</param>
    /// <returns>Either <c>true</c> or <c>false</c>.</returns>
    public bool RebindAction(int actionID, Object boundObject) {
      // Console.WriteLine("Rebinding action %d",actionID);
      IBoundAction<T> action = null;
      if (actionID < 0 || boundObject == null) {
        return false;
      }
      T[] postponed = null;
      lock (this.syncRoot) {
        if (actionID >= this.actions.Count) {
          return false;
        }
        action = this.actions[actionID];
        if (action == null) {
          return false;
        }
        this.boundObjects[actionID] = boundObject;
        postponed = this.postponeCall[actionID];
        if (postponed != null) {
          this.actions[actionID] = null;
          this.postponeCall[actionID] = null;
          this.boundObjects[actionID] = null;
        }
      }
      if (postponed != null) {
        // Console.WriteLine("Calling postponed action %d",actionID);
        action.Action(boundObject, postponed);
      }
      return true;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='boundObject'>The parameter <paramref
    /// name='boundObject'/> is a Object object.</param>
    /// <param name='action'>The parameter <paramref name='action'/> is
    /// a.Upokecenter.Util.IBoundAction{`0} object.</param>
    /// <returns>A 32-bit signed integer.</returns>
    public int RegisterAction(Object boundObject, IBoundAction<T> action) {
      lock (this.syncRoot) {
        for (int i = 0; i < this.actions.Count; ++i) {
          if (this.actions[i] == null) {
            // Console.WriteLine("Adding action %d",i);
            this.actions[i] = action;
            this.boundObjects[i] = boundObject;
            this.postponeCall[i] = null;
            return i;
          }
        }
        int ret = this.actions.Count;
        // Console.WriteLine("Adding action %d",ret);
        this.actions.Add(action);
        this.boundObjects.Add(boundObject);
        this.postponeCall.Add(null);
        return ret;
      }
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='actionID'>The parameter <paramref name='actionID'/> is
    /// a 32-bit signed integer.</param>
    /// <returns>Either <c>true</c> or <c>false</c>.</returns>
    public bool RemoveAction(int actionID) {
      // Console.WriteLine("Removing action %d",actionID);
      if (actionID < 0) {
        return false;
      }
      lock (this.syncRoot) {
        if (actionID >= this.actions.Count) {
          return false;
        }
        this.actions[actionID] = null;
        this.boundObjects[actionID] = null;
        this.postponeCall[actionID] = null;
      }
      return true;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='actionID'>The parameter <paramref name='actionID'/> is
    /// a 32-bit signed integer.</param>
    /// <param name='parameters'>The parameter <paramref
    /// name='parameters'/> is a `0[] object.</param>
    /// <returns>Either <c>true</c> or <c>false</c>.</returns>
    public bool TriggerActionOnce(int actionID, params T[] parameters) {
      // Console.WriteLine("Triggering action %d",actionID);
      IBoundAction<T> action = null;
      if (actionID < 0) {
        return false;
      }
      Object boundObject = null;
      lock (this.syncRoot) {
        if (actionID >= this.actions.Count) {
          return false;
        }
        boundObject = this.boundObjects[actionID];
        if (boundObject == null) {
          // Console.WriteLine("Postponing action %d",actionID);
          this.postponeCall[actionID] = parameters;
          return false;
        }
        action = this.actions[actionID];
        this.actions[actionID] = null;
        this.boundObjects[actionID] = null;
        this.postponeCall[actionID] = null;
      }
      if (action == null) {
        return false;
      }
      action.Action(boundObject, parameters);
      return true;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='actionID'>The parameter <paramref name='actionID'/> is
    /// a 32-bit signed integer.</param>
    /// <returns>Either <c>true</c> or <c>false</c>.</returns>
    public bool UnbindAction(int actionID) {
      // Console.WriteLine("Unbinding action %d",actionID);
      IBoundAction<T> action = null;
      if (actionID < 0) {
        return false;
      }
      lock (this.syncRoot) {
        if (actionID >= this.actions.Count) {
          return false;
        }
        action = this.actions[actionID];
        if (action == null) {
          return false;
        }
        this.boundObjects[actionID] = null;
      }
      return true;
    }
  }
}
