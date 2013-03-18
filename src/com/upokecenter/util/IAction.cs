namespace com.upokecenter.util {
using System;

public interface IAction<T> {
	 void action(params T[] parameters);
}

}
