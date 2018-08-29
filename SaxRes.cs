// NO WARRANTY! This code is in the Public Domain.
// Written by Karl Waclawek (karl@waclawek.net).

using System;
using System.Reflection;
using System.Resources;

namespace Org.System.Xml.Sax {
  /**<summary>Identifies localized string constants.</summary> */

    /// <summary>Not documented yet.</summary>
  public enum RsId
  {
    // for Org.System.Xml.Sax namespace
    CannotChangeExceptionId,
    FeatureNotSupported,
    FeatureReadNotSupported,
    FeatureWriteNotSupported,
    FeatureWhenParsing,
    FeatureNotRecognized,
    PropertyNotSupported,
    PropertyNotRecognized,
    // for Org.System.Xml.Sax.Helpers namespace
    CapacityTooSmall,
    AttIndexOutOfBounds,
    AttributeNotFound,
    AttributeNotFoundNS,
    NonEmptyStringRequired,
    NoFilterParent,
    NoXmlReaderInAssembly,
    NoDefaultXmlReader
  }

  /**<summary>Enables access to localized resources.</summary> */

    /// <summary>Not documented yet.</summary>
  public class Resources {
    // NOTE: 'rm' deleted
    private Resources() {
}

    /// <summary>Returns localized string constants.</summary>
    /// <param name='id'>The parameter <paramref name='id'/> is not
    /// documented yet.</param>
    /// <returns>A string object.</returns>
    public static string GetString(RsId id) {
      // NOTE: modified
      return id.ToString();
    }
    // NOTE: Resources static initializer deleted
  }
}
