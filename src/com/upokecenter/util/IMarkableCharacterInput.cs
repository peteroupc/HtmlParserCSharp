namespace com.upokecenter.util {
using System;

using System.IO;

public interface IMarkableCharacterInput : ICharacterInput {

	 int getMarkPosition();

	 void setMarkPosition(int pos) ;

	 int markIfNeeded();

	 int markToEnd();

	 void moveBack(int count) ;

}
}
