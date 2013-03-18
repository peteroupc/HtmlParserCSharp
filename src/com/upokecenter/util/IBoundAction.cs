namespace com.upokecenter.util {
using System;

public interface IBoundAction<T> {
	 void action(Object thisObject, params T[] parameters);
}

}
