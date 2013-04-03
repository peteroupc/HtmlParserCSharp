namespace com.upokecenter.util {
using System;

using System.IO;

public interface IMarkableCharacterInput : ICharacterInput {

	 int getMarkPosition();

	 void setMarkPosition(int pos) ;

	 int setSoftMark();

	 int setHardMark();

	 void moveBack(int count) ;

}
}
