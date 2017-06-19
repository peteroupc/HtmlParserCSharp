/*
Modified from the original version by Peter Occil in 2013. Still in the
  public domain.
 */
namespace com.upokecenter.util {
using System;
using System.Globalization;

    /// <summary>Subset of Robert Harder's Base64 library, whose homepage
    /// is: Homepage:
    /// <a href='http://iharder.net/base64'>http://iharder.net/base64</a>.
    /// The original library had this notice:
    /// <p>I am placing this code in the Public Domain. Do with it as you
    /// will. This software comes with no guarantees or warranties but with
    /// plenty of well-wishing instead! Please visit
    /// <a href='http://iharder.net/base64'>http://iharder.net/base64</a>
    /// periodically to check for updates or to contribute
    /// improvements.</p></summary>
public class Base64 {
  /* ******** P U B L I C F I E L D S ******** */

  /** No options specified. Value is zero. */
  private const int NO_OPTIONS = 0;

  /* ******** P R I V A T E F I E L D S ******** */

    /// <summary>Encode using Base64-like encoding that is URL- and
    /// Filename-safe as described in Section 4 of RFC3548:
    /// <a
    /// href='http://www.faqs.org/rfcs/rfc3548.html'>http://www.faqs.org/rfcs/rfc3548.html</a>
    /// . It is important to note that data encoded this way is
    /// <em>not</em> officially valid Base64, or at the very least should
    /// not be called Base64 without also specifying that is was encoded
    /// using the URL- and Filename-safe dialect.</summary>
  private const int URL_SAFE = 16;

    /// <summary>Encode using the special "ordered" dialect of Base64
    /// described here:
    /// <a
    /// href='http://www.faqs.org/qa/rfcc-1940.html'>http://www.faqs.org/qa/rfcc-1940.html</a></summary>
  private const int ORDERED = 32;

  /** The equals sign (=) as a byte. */
  private static readonly byte EQUALS_SIGN = (byte)'=';

  /* ******** S T A N D A R D B A S E 6 4 A L P H A B E T ******** */

  private static readonly byte WHITE_SPACE_ENC = 251;  
    // Indicates white
    // space in encoding

  private static readonly byte EQUALS_SIGN_ENC = 255;  
    // Indicates equals
    // sign in encoding

  /* ******** U R L S A F E B A S E 6 4 A L P H A B E T ******** */

    /// <summary>Translates a Base64 value to either its 6-bit
    /// reconstruction value or a negative number indicating some other
    /// meaning.</summary>
  private static readonly byte[] _STANDARD_DECODABET = {
    246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 0 - 8
  250, 250,  // Whitespace: Tab and Linefeed
    246, 246,  // Decimal 11 - 12
    250,  // Whitespace: Carriage Return
    246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 14 - 26
    246, 246, 246, 246, 246,  // Decimal 27 - 31
    250,  // Whitespace: Space
    246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 33 - 42
    62,  // Plus sign at decimal 43
    246, 246, 246,  // Decimal 44 - 46
    63,  // Slash at decimal 47
    52, 53, 54, 55, 56, 57, 58, 59, 60, 61,  // Numbers zero through nine
    246, 246, 246,  // Decimal 58 - 60
    254,  // Equals sign at decimal 61
    246, 246, 246,  // Decimal 62 - 64
    0,1,2,3,4,5,6,7,8,9,10,11,12,13,  // Letters 'A' through 'N'
    14,15,16,17,18,19,20,21,22,23,24,25,  // Letters 'O' through 'Z'
    246, 246, 246, 246, 246, 246,  // Decimal 91 - 96
    26,27,28,29,30,31,32,33,34,35,36,37,38,  // Letters 'a' through 'm'
    39,40,41,42,43,44,45,46,47,48,49,50,51,  // Letters 'n' through 'z'
    246, 246, 246, 246, 246  // Decimal 123 - 127
    , 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 128 - 139
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 140 - 152
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 153 - 165
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 166 - 178
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 179 - 191
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 192 - 204
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 205 - 217
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 218 - 230
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 231 - 243
    246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246  // Decimal 244 - 255
  };

  /* ******** O R D E R E D B A S E 6 4 A L P H A B E T ******** */

    /// <summary>Used in decoding URL- and Filename-safe dialects of
    /// Base64.</summary>
  private static readonly byte[] _URL_SAFE_DECODABET = {
    246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 0 - 8
  250, 250,  // Whitespace: Tab and Linefeed
    246, 246,  // Decimal 11 - 12
    250,  // Whitespace: Carriage Return
    246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 14 - 26
    246, 246, 246, 246, 246,  // Decimal 27 - 31
    250,  // Whitespace: Space
    246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 33 - 42
    246,  // Plus sign at decimal 43
    246,  // Decimal 44
    62,  // Minus sign at decimal 45
    246,  // Decimal 46
    246,  // Slash at decimal 47
    52, 53, 54, 55, 56, 57, 58, 59, 60, 61,  // Numbers zero through nine
    246, 246, 246,  // Decimal 58 - 60
    254,  // Equals sign at decimal 61
    246, 246, 246,  // Decimal 62 - 64
    0,1,2,3,4,5,6,7,8,9,10,11,12,13,  // Letters 'A' through 'N'
    14,15,16,17,18,19,20,21,22,23,24,25,  // Letters 'O' through 'Z'
    246, 246, 246, 246,  // Decimal 91 - 94
    63,  // Underscore at decimal 95
    246,  // Decimal 96
    26,27,28,29,30,31,32,33,34,35,36,37,38,  // Letters 'a' through 'm'
    39,40,41,42,43,44,45,46,47,48,49,50,51,  // Letters 'n' through 'z'
    246, 246, 246, 246, 246  // Decimal 123 - 127
    , 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 128 - 139
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 140 - 152
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 153 - 165
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 166 - 178
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 179 - 191
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 192 - 204
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 205 - 217
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 218 - 230
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 231 - 243
    246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246  // Decimal 244 - 255
  };

  /* ******** D E T E R M I N E W H I C H A L P H A B E T ******** */

    /// <summary>Used in decoding the "ordered" dialect of
    /// Base64.</summary>
  private static readonly byte[] _ORDERED_DECODABET = {
    246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 0 - 8
  250, 250,  // Whitespace: Tab and Linefeed
    246, 246,  // Decimal 11 - 12
    250,  // Whitespace: Carriage Return
    246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 14 - 26
    246, 246, 246, 246, 246,  // Decimal 27 - 31
    250,  // Whitespace: Space
    246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 33 - 42
    246,  // Plus sign at decimal 43
    246,  // Decimal 44
    0,  // Minus sign at decimal 45
    246,  // Decimal 46
    246,  // Slash at decimal 47
    1, 2, 3, 4, 5, 6, 7, 8, 9, 10,  // Numbers zero through nine
    246, 246, 246,  // Decimal 58 - 60
    254,  // Equals sign at decimal 61
    246, 246, 246,  // Decimal 62 - 64
    11,12,13,14,15,16,17,18,19,20,21,22,23,  // Letters 'A' through 'M'
    24,25,26,27,28,29,30,31,32,33,34,35,36,  // Letters 'N' through 'Z'
    246, 246, 246, 246,  // Decimal 91 - 94
    37,  // Underscore at decimal 95
    246,  // Decimal 96
    38,39,40,41,42,43,44,45,46,47,48,49,50,  // Letters 'a' through 'm'
    51,52,53,54,55,56,57,58,59,60,61,62,63,  // Letters 'n' through 'z'
    246, 246, 246, 246, 246  // Decimal 123 - 127
 , 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 128 - 139
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 140 - 152
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 153 - 165
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 166 - 178
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 179 - 191
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 192 - 204
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 205 - 217
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 218 - 230
  246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246,  // Decimal 231 - 243
    246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246  // Decimal 244 - 255
  };

    /// <summary>Low-level access to decoding ASCII characters in the form
    /// of a byte array.
    /// <strong>Ignores GUNZIP option, if it's set.</strong> This is not
    /// generally a recommended method, although it is used internally as
    /// part of the decoding process. Special case: if len = 0, an empty
    /// array is returned. Still, if you need more speed and reduced memory
    /// footprint (and aren't gzipping), consider this method. @param
    /// source The Base64 encoded data @return decoded data @since
    /// 2.3.1.</summary>
    /// <param name='source'>Not documented yet.</param>
    /// <returns>A byte array.</returns>
  public static byte[] decode(byte[] source) {
    byte[] decoded = null;
    decoded = decode(source, 0, source.Length, Base64.NO_OPTIONS);
    return decoded;
  }

    /// <summary>Low-level access to decoding ASCII characters in the form
    /// of a byte array.
    /// <strong>Ignores GUNZIP option, if it's set.</strong> This is not
    /// generally a recommended method, although it is used internally as
    /// part of the decoding process. Special case: if len = 0, an empty
    /// array is returned. Still, if you need more speed and reduced memory
    /// footprint (and aren't gzipping), consider this method. @param
    /// source The Base64 encoded data @param off The offset of where to
    /// begin decoding @param len The length of characters to decode @param
    /// options Can specify options such as alphabet type to use @return
    /// decoded data @ If bogus characters exist in source data @since
    /// 1.3.</summary>
    /// <param name='source'>Not documented yet.</param>
    /// <param name='off'>Not documented yet.</param>
    /// <param name='len'>Not documented yet. (3).</param>
    /// <param name='options'>Not documented yet. (4).</param>
    /// <returns>A byte array.</returns>
    /// <exception cref='ArgumentNullException'>The parameter is
    /// null.</exception>
  private static byte[] decode(byte[] source, int off, int len, int options) {
    // Lots of error checking and exception throwing
    if (source == null) {
 throw new ArgumentNullException("Cannot decode null source array.");
}
    if (off < 0 || off + len > source.Length) {
 throw new ArgumentException(
          "Source array with length "
            +Convert.ToString(source.Length, CultureInfo.InvariantCulture)+
  " cannot have offset of "
            +Convert.ToString(off, CultureInfo.InvariantCulture)+
 "and process "
            +Convert.ToString(len,CultureInfo.InvariantCulture)+" bytes.");
 }

    if (len == 0) {
 return new byte[0];
  } else if (len < 4) {
 throw new ArgumentException(
  "Base64-encoded _string must have at least four characters, but length specified was "
            +
              Convert.ToString(len, CultureInfo.InvariantCulture));
 }

    byte[] DECODABET = getDecodabet(options);

    int len34 = len * 3 / 4;  // Estimate on array size
    var outBuff = new byte[ len34 ];  // Upper limit on size of output
    var outBuffPosn = 0;  // Keep track of where we're writing

    var b4 = new byte[4];  
      // Four byte buffer from source, eliminating white
      // space
    var b4Posn = 0;  // Keep track of four byte input buffer
    var i = 0;  // Source array counter
    byte sbiDecode = 0;  // Special value from DECODABET

    for (i = off; i < off + len; ++i) {  // Loop through source

      sbiDecode = DECODABET[ source[i]&0xff ];

      // White space, Equals sign, or legit Base64 character
      // Note the values such as -5 and -9 in the
      // DECODABETs at the top of the file.
      if (sbiDecode >= WHITE_SPACE_ENC) {
        if (sbiDecode >= EQUALS_SIGN_ENC) {
          b4[ b4Posn++ ] = source[i];  // Save non-whitespace
          if (b4Posn > 3) {  // Time to decode?
            outBuffPosn += decode4to3(b4, 0, outBuff, outBuffPosn, options);
            b4Posn = 0;

            // If that was the equals sign, break out of 'for' loop
            if (source[i] == EQUALS_SIGN) {
              break;  // end if: equals sign
            }  // end if: quartet built
          }  // end if: equals sign or better
        }  // end if: white space, equals sign or better
      }
      else  // There's a bad input character in the Base64 stream.
        throw new System.IO.IOException(
            "Bad Base64 input character decimal "
  +Convert.ToString(source[i]&0xff, CultureInfo.InvariantCulture)+
      " in array position "
              +Convert.ToString(i, CultureInfo.InvariantCulture));  // each input character
    }

    var output = new byte[ outBuffPosn ];
    Array.Copy(outBuff, 0, output, 0, outBuffPosn);
    return output;  // end decode
  }

    /// <summary>Decodes four bytes from array
    /// <var>source</var> and writes the resulting bytes (up to three of
    /// them) to
    /// <var>destination</var>. The source and destination arrays can be
    /// manipulated anywhere along their length by specifying
    /// <var>srcOffset</var> and
    /// <var>destOffset</var>. This method does not check to make sure
    /// your arrays are large enough to accomodate
    /// <var>srcOffset</var> + 4 for the
    /// <var>source</var> array or
    /// <var>destOffset</var> + 3 for the
    /// <var>destination</var> array. This method returns the actual number
    /// of bytes that were converted from the Base64 encoding.
    /// <p>This is the lowest level of the decoding methods with all
    /// possible parameters.</p> @param source the array to convert @param
    /// srcOffset the index where conversion begins @param destination the
    /// array to hold the conversion @param destOffset the index where
    /// output will be put @param options alphabet type is pulled from this
    /// (standard, url-safe, ordered) @return the number of decoded bytes
    /// converted @ if source or destination arrays are null @ if srcOffset
    /// or destOffset are invalid or there is not enough room in the array.
    /// @since 1.3.</summary>
    /// <param name='source'>Not documented yet.</param>
    /// <param name='srcOffset'>Not documented yet.</param>
    /// <param name='destination'>Not documented yet. (3).</param>
    /// <param name='destOffset'>Not documented yet. (4).</param>
    /// <param name='options'>Not documented yet. (5).</param>
    /// <returns>A 32-bit signed integer.</returns>
    /// <exception cref='ArgumentNullException'>The parameter is
    /// null.</exception>
  private static int decode4to3(
      byte[] source,
      int srcOffset,
      byte[] destination,
      int destOffset,
      int options) {
    // Lots of error checking and exception throwing
    if (source == null) {
 throw new ArgumentNullException("Source array was null.");
}
    if (destination == null) {
 throw new ArgumentNullException("Destination array was null.");
}
    if (srcOffset < 0 || srcOffset + 3 >= source.Length) {
 throw new ArgumentException(
          "Source array with length "
            +Convert.ToString(source.Length, CultureInfo.InvariantCulture)+
          " cannot have offset of "
  +Convert.ToString(srcOffset,CultureInfo.InvariantCulture)+" and still process four bytes.");
 }
    if (destOffset < 0 || destOffset + 2 >= destination.Length) {
 throw new ArgumentException(
          "Destination array with length "
  +Convert.ToString(destination.Length, CultureInfo.InvariantCulture)+
          " cannot have offset of "
  +Convert.ToString(srcOffset,CultureInfo.InvariantCulture)+" and still store three bytes.");
 }

    byte[] DECODABET = getDecodabet(options);

    // Example: Dk==
    if (source[ srcOffset + 2] == EQUALS_SIGN) {
      int outBuff = ((DECODABET[ source[ srcOffset ] ] & 0xff) << 18)|
          ((DECODABET[ source[ srcOffset + 1] ] & 0xff) << 12);

      destination[ destOffset ] = (byte)((outBuff >> 16) & 0xff);
      return 1;
    }

    // Example: DkL=
    else if (source[ srcOffset + 3 ] == EQUALS_SIGN) {
      int outBuff = ((DECODABET[ source[ srcOffset ] ] & 0xff) << 18)|
          ((DECODABET[ source[ srcOffset + 1 ] ] & 0xff) << 12)|
          ((DECODABET[ source[ srcOffset + 2 ] ] & 0xff) << 6);

      destination[ destOffset ] = (byte)((outBuff >> 16) & 0xff);
      destination[ destOffset + 1 ] = (byte)((outBuff >> 8) & 0xff);
      return 2;
    }

    // Example: DkLE
    else {
      int outBuff = ((DECODABET[ source[ srcOffset ] ] & 0xff) << 18)|
          ((DECODABET[ source[ srcOffset + 1 ] ] & 0xff) << 12)|
          ((DECODABET[ source[ srcOffset + 2 ] ] & 0xff) << 6)|
          ((DECODABET[ source[ srcOffset + 3 ] ] & 0xff));

      destination[ destOffset ] = (byte)(outBuff >> 16);
      destination[ destOffset + 1 ] = (byte)(outBuff >> 8);
      destination[ destOffset + 2 ] = (byte)(outBuff);

      return 3;
    }  // end decodeToBytes
  }

  /* ******** D E C O D I N G M E T H O D S ******** */

  /* ******** I N N E R C L A S S I N P U T S T R E A M ******** */

    /// <summary>Returns one of the _SOMETHING_DECODABET byte arrays
    /// depending on the options specified. It's possible, though silly, to
    /// specify ORDERED and URL_SAFE in which case one of them will be
    /// picked, though there is no guarantee as to which one will be
    /// picked.</summary>
    /// <param name='options'>Not documented yet.</param>
    /// <returns>A byte array.</returns>
  private static byte[] getDecodabet(int options) {
    if ((options & URL_SAFE) == URL_SAFE) {
 return _URL_SAFE_DECODABET;
  } else if ((options & ORDERED) == ORDERED) {
 return _ORDERED_DECODABET;
} else {
 return _STANDARD_DECODABET;
}  // end getAlphabet
  }

  /* ******** I N N E R C L A S S O U T P U T S T R E A M ******** */

  /** Defeats instantiation. */
  private Base64() {}  // end class Base64
}
}
