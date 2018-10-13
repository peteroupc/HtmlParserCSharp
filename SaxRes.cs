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

    /// <summary>Not documented yet.</summary>
    CannotChangeExceptionId,

    /// <summary>Not documented yet.</summary>
    FeatureNotSupported,

    /// <summary>Not documented yet.</summary>
    FeatureReadNotSupported,

    /// <summary>Not documented yet.</summary>
    FeatureWriteNotSupported,

    /// <summary>Not documented yet.</summary>
    FeatureWhenParsing,

    /// <summary>Not documented yet.</summary>
    FeatureNotRecognized,

    /// <summary>Not documented yet.</summary>
    PropertyNotSupported,

    /// <summary>Not documented yet.</summary>
    PropertyNotRecognized,
    // for Org.System.Xml.Sax.Helpers namespace

    /// <summary>Not documented yet.</summary>
    CapacityTooSmall,

    /// <summary>Not documented yet.</summary>
    AttIndexOutOfBounds,

    /// <summary>Not documented yet.</summary>
    AttributeNotFound,

    /// <summary>Not documented yet.</summary>
    AttributeNotFoundNS,

    /// <summary>Not documented yet.</summary>
    NonEmptyStringRequired,

    /// <summary>Not documented yet.</summary>
    NoFilterParent,

    /// <summary>Not documented yet.</summary>
    NoXmlReaderInAssembly,

    /// <summary>Not documented yet.</summary>
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
