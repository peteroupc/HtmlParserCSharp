namespace com.upokecenter.util {
using System;

using System.IO;

/**
 * An abstract stream of Unicode characters.
 * 
 * @author Peter
 *
 */
public interface ICharacterInput {

	/**
	 * Reads multiple Unicode characters into a buffer.
	 * 
	 * @param buf
	 * @param offset
	 * @param unitCount
	 * @return The number of Unicode characters read,
	 * or -1 if the end of the input is reached
	 * @ if an I/O error occurs.
	 */
	 int read(int[] buf, int offset, int unitCount)
			;

	/**
	 * 
	 * Reads the next Unicode character.
	 * 
	 * @return A Unicode code point or -1 if the end of
	 * the input is reached
	 * @ if an I/O error occurs.
	 */
	 int read() ;
}

}
