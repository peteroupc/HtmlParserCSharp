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


/**
 * A class for holding tasks that can be referred to by integer index.
 */
public sealed class ActionList<T> {

	private IList<IBoundAction<T>> actions;
	private IList<Object> boundObjects;
	private IList<T[]> postponeCall;
	private Object syncRoot=new Object();

	public ActionList(){
		actions=new List<IBoundAction<T>>();
		boundObjects=new List<Object>();
		postponeCall=new List<T[]>();
	}

	public bool rebindAction(int actionID, Object boundObject){
		//Console.WriteLine("Rebinding action %d",actionID);
		IBoundAction<T> action=null;
		if(actionID<0 || boundObject==null)return false;
		T[] postponed=null;
		lock(syncRoot){
			if(actionID>=actions.Count)
				return false;
			action=actions[actionID];
			if(action==null)
				return false;
			boundObjects[actionID]=boundObject;
			postponed=postponeCall[actionID];
			if(postponed!=null){
				actions[actionID]=null;
				postponeCall[actionID]=null;
				boundObjects[actionID]=null;
			}
		}
		if(postponed!=null){
			//Console.WriteLine("Calling postponed action %d",actionID);
			action.action(boundObject,postponed);
		}
		return true;
	}

	public int registerAction(Object boundObject, IBoundAction<T> action){
		lock(syncRoot){
			for(int i=0;i<actions.Count;i++){
				if(actions[i]==null){
					//Console.WriteLine("Adding action %d",i);
					actions[i]=action;
					boundObjects[i]=boundObject;
					postponeCall[i]=null;
					return i;
				}
			}
			int ret=actions.Count;
			//Console.WriteLine("Adding action %d",ret);
			actions.Add(action);
			boundObjects.Add(boundObject);
			postponeCall.Add(null);
			return ret;
		}
	}

	public bool removeAction(int actionID){
		//Console.WriteLine("Removing action %d",actionID);
		if(actionID<0)return false;
		lock(syncRoot){
			if(actionID>=actions.Count)
				return false;
			actions[actionID]=null;
			boundObjects[actionID]=null;
			postponeCall[actionID]=null;
		}
		return true;
	}

	public bool triggerActionOnce(int actionID, params T[] parameters){
		//Console.WriteLine("Triggering action %d",actionID);
		IBoundAction<T> action=null;
		if(actionID<0)return false;
		Object boundObject=null;
		lock(syncRoot){
			if(actionID>=actions.Count)
				return false;
			boundObject=boundObjects[actionID];
			if(boundObject==null){
				//Console.WriteLine("Postponing action %d",actionID);
				postponeCall[actionID]=parameters;
				return false;
			}
			action=actions[actionID];
			actions[actionID]=null;
			boundObjects[actionID]=null;
			postponeCall[actionID]=null;
		}
		if(action==null)return false;
		action.action(boundObject,parameters);
		return true;
	}

	public bool unbindAction(int actionID){
		//Console.WriteLine("Unbinding action %d",actionID);
		IBoundAction<T> action=null;
		if(actionID<0)return false;
		lock(syncRoot){
			if(actionID>=actions.Count)
				return false;
			action=actions[actionID];
			if(action==null)
				return false;
			boundObjects[actionID]=null;
		}
		return true;
	}
}

}
