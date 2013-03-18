namespace com.upokecenter.encoding {
using System;

using System.IO;


/**
 * Converts bytes to Unicode characters.
 * @author Peter
 *
 */
public interface ITextDecoder {
	/**
	 * Converts bytes from a byte stream into Unicode characters.
	 * @param stream an input stream containing the encoded bytes
	 * @param buffer an array
	 * @param offset offset into the array where the first Unicode
	 * character will be placed
	 * @param length maximum number of Unicode characters to output.
	 * @return the number of Unicode characters output, or -1 if the
	 * end of the stream has been reached.
	 * @ if the stream's current input cannot
	 * be converted to a Unicode character, or if another I/O error
	 * occurs
	 */
	 int decode(PeterO.Support.InputStream stream, int[] buffer, int offset, int length) ;
	/**
	 * Converts bytes from a byte stream into Unicode characters.
	 * @param stream an input stream containing the encoded bytes
	 * @param buffer an array
	 * @param offset offset into the array where the first Unicode
	 * character will be placed
	 * @param length maximum number of Unicode characters to output.
	 * @param error an error handler to use if the stream's
	 * current input cannot be converted to a Unicode character.
	 * When that happens, this _object's emitDecoderError method is called.
	 * Currently, for objects provided in this package that implement
	 * this interface, if emitDecoderError emits more than one Unicode
	 * character and the buffer has no space left to fit those characters,
	 * only as many characters as will fit will be placed in the buffer,
	 * and the remaining characters are ignored.
	 * @return the number of Unicode characters output, or -1 if the
	 * end of the stream has been reached.
	 * @ if an I/O error occurs.
	 */
	 int decode(PeterO.Support.InputStream stream, int[] buffer, int offset, int length, IEncodingError error) ;
	/**
	 * Gets a single Unicode character from the input stream.
	 * @param stream an input stream.
	 * @return a Unicode character, or -1 if the end of the
	 * stream is reached.
	 * @ if the stream's current input cannot
	 * be converted to a Unicode character, or if another I/O error
	 * occurs
	 */
	 int decode(PeterO.Support.InputStream stream) ;
	/**
	 * Gets a single Unicode character from the input stream.
	 * @param stream an input stream.
	 * @param error an error handler to use if the stream's
	 * current input cannot be converted to a Unicode character.
	 * When that happens, this _object's emitDecoderError method is called.
	 * Currently, for objects provided in this package that implement
	 * this interface, if emitDecoderError emits more than one Unicode
	 * character, this method returns only the first of those characters;
	 * the remaining characters are ignored.  If that method emits
	 * no characters, this method goes on with the next bytes in the input.
	 * @return a Unicode character, or -1 if the end of the
	 * stream is reached.
	 * @ if an I/O error occurs.
	 */
	 int decode(PeterO.Support.InputStream stream, IEncodingError error) ;
}

}
