using System;

namespace Com.Upokecenter.Html {
  /// <summary>Represents one of the attributes within an HTML
  /// element.</summary>
  public interface IAttr {
    /// <summary>Gets the attribute name's local name (the part after the
    /// colon, if it's bound to a _namespace).</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetLocalName();

    /// <summary>Gets the attribute's qualified name.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetName();

    /// <summary>Gets the attribute's _namespace URI, if it's bound to a
    /// _namespace.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetNamespaceURI();

    /// <summary>Gets the attribute name's prefix (the part before the
    /// colon, if it's bound to a _namespace).</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetPrefix();

    /// <summary>Gets the attribute's value.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetValue();
  }
}
