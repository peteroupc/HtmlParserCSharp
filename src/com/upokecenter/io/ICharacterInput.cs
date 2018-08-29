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

    /// <summary>An abstract stream of Unicode characters. @author
    /// Peter.</summary>
public interface ICharacterInput {
    /// <summary>* Reads the next Unicode character. @return A Unicode code
    /// point or -1 if the end of the input is reached @ if an I/O error
    /// occurs.</summary>
    /// <returns>Not documented yet.</returns>
   int read();

    /// <summary>Reads multiple Unicode characters into a buffer. @param
    /// buf @param offset @param unitCount @return The number of Unicode
    /// characters read, or -1 if the end of the input is reached @ if an
    /// I/O error occurs.</summary>
    /// <returns>Not documented yet.</returns>
   int read(int[] buf, int offset, int unitCount);
}
}
