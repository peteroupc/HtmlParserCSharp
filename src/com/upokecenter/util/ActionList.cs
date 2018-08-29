/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
namespace com.upokecenter.util {
using System;
using System.Collections.Generic;

    /// <summary>A class for holding tasks that can be referred to by
    /// integer index.</summary>
    /// <typeparam name='T'>Type parameter not documented yet.</typeparam>
public sealed class ActionList<T> {
  private IList<IBoundAction<T>> actions;
  private IList<Object> boundObjects;
  private IList<T[]> postponeCall;
  private Object syncRoot = new Object();

    /// <summary>Initializes a new instance of the ActionList
    /// class.</summary>
  public ActionList() {
    this.actions = new List<IBoundAction<T>>();
    this.boundObjects = new List<Object>();
    this.postponeCall = new List<T[]>();
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='actionID'>Not documented yet.</param>
    /// <param name='boundObject'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
  public bool rebindAction(int actionID, Object boundObject) {
    // DebugUtility.Log("Rebinding action %d",actionID);
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
      // DebugUtility.Log("Calling postponed action %d",actionID);
      action.action(boundObject, postponed);
    }
    return true;
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='boundObject'>Not documented yet.</param>
    /// <param name='action'>Not documented yet.</param>
    /// <returns>A 32-bit signed integer.</returns>
  public int registerAction(Object boundObject, IBoundAction<T> action) {
    lock (this.syncRoot) {
      for (int i = 0; i < this.actions.Count; ++i) {
        if (this.actions[i] == null) {
          // DebugUtility.Log("Adding action %d",i);
          this.actions[i] = action;
          this.boundObjects[i] = boundObject;
          this.postponeCall[i] = null;
          return i;
        }
      }
      int ret = this.actions.Count;
      // DebugUtility.Log("Adding action %d",ret);
      this.actions.Add(action);
      this.boundObjects.Add(boundObject);
      this.postponeCall.Add(null);
      return ret;
    }
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='actionID'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
  public bool removeAction(int actionID) {
    // DebugUtility.Log("Removing action %d",actionID);
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
    /// <param name='actionID'>Not documented yet.</param>
    /// <param name='parameters'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
  public bool triggerActionOnce(int actionID, params T[] parameters) {
    // DebugUtility.Log("Triggering action %d",actionID);
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
        // DebugUtility.Log("Postponing action %d",actionID);
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
    action.action(boundObject, parameters);
    return true;
  }

    /// <summary>Not documented yet.</summary>
    /// <param name='actionID'>Not documented yet.</param>
    /// <returns>A Boolean object.</returns>
  public bool unbindAction(int actionID) {
    // DebugUtility.Log("Unbinding action %d",actionID);
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
