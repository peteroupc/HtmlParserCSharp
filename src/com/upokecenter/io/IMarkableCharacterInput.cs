/*
Written in 2013 by Peter Occil.  Released to the public domain.
Public domain dedication: http://creativecommons.org/publicdomain/zero/1.0/
 */
namespace com.upokecenter.io {
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
