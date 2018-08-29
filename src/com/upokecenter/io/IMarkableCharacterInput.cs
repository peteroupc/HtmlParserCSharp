/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/
namespace com.upokecenter.io {
using System;
using System.IO;
using PeterO.Text;

    /// <summary>Not documented yet.</summary>
public interface IMarkableCharacterInput : ICharacterInput {
    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
   int getMarkPosition();

    /// <summary>Not documented yet.</summary>
    /// <param name='count'>Not documented yet.</param>
   void moveBack(int count);

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
   int setHardMark();

    /// <summary>Not documented yet.</summary>
    /// <param name='pos'>Not documented yet.</param>
   void setMarkPosition(int pos);

    /// <summary>Not documented yet.</summary>
    /// <returns>Not documented yet.</returns>
   int setSoftMark();
}
}
