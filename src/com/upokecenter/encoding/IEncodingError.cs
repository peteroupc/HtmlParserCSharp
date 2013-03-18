namespace com.upokecenter.encoding {
using System;

using System.IO;


/**
 * 
 * An interface implemented by classes that handle errors that
 * occur when converting bytes to and from Unicode characters.
 * 
 * @author Peter
 *
 */
public interface IEncodingError {
	/**
	 * 
	 * Handles an error when decoding bytes into Unicode characters.
	 * 
	 * @param buffer an array to output Unicode characters
	 * @param offset the offset to the array to write characters
	 * @param length the number of characters available
	 * in the buffer
	 * @return the number of characters emitted.  Note that
	 * currently, the objects provided by this package do not
	 * fully support error handlers that emit more than one character
	 * as a decoder error, so that additional characters that
	 * would overflow the buffer passed to the decode methods
	 * may be ignored.
	 * @ if the method decides to handle the
	 * error by throwing an IOException or a derived class, or
	 * if another I/O error occurs.
	 */
	 int emitDecoderError(int[] buffer, int offset, int length) ;
	/**
	 * Handles an error when encoding Unicode characters into bytes.
	 * 
	 * @param stream a stream to write bytes
	 * @param codePoint the code point that caused the encoder error
	 * @ if the method decides to handle the
	 * error by throwing an IOException or a derived class, or
	 * if another I/O error occurs.
	 */
	 void emitEncoderError(Stream stream, int codePoint) ;
}

}
