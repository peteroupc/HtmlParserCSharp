/*
Written in 2013 by Peter Occil.  Released to the public domain.
Public domain dedication: http://creativecommons.org/publicdomain/zero/1.0/
 */
namespace com.upokecenter.util {
using System;

public interface IAction<T> {
	 void action(params T[] parameters);
}

}
