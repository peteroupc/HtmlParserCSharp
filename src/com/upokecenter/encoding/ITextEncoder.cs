namespace com.upokecenter.encoding {
using System;

using System.IO;

/**
 * 
 * Converts Unicode characters to bytes.
 * @author Peter
 *
 */
public interface ITextEncoder {
	/**
	 * Writes Unicode characters as bytes in an output stream.
	 * 
	 * @param stream stream where bytes will be written
	 * @param buffer an array of Unicode characters
	 * @param offset offset into the array
	 * @param length number of characters to write
	 * @ if there are characters that can't be
	 * converted to bytes, or if another I/O error occurs.
	 */
	 void encode(Stream stream, int[] buffer, int offset, int length) ;

	/**
	 * Writes Unicode characters as bytes in an output stream.
	 * 
	 * @param stream stream where bytes will be written
	 * @param buffer an array of Unicode characters
	 * @param offset offset into the array
	 * @param length number of characters to write
	 * @param error error handler to use.  If there are characters
	 * that can't be converted to bytes, this _object's emitEncoderError
	 * method is called.
	 * @ if an I/O error occurs
	 */
	 void encode(Stream stream, int[] buffer, int offset, int length, IEncodingError error) ;
}

}
