namespace com.upokecenter.html {
using System;

/*
 * Represents one of the attributes within an HTML element.
 *
 * @author Peter
 *
 */

    /// <summary>Not documented yet.</summary>
public interface IAttr {
    /// <summary>Gets the attribute name's local name (the part after the
    /// colon, if it's bound to a _namespace).</summary>
    /// <returns>Not documented yet.</returns>
   string getLocalName();

    /// <summary>Gets the attribute's qualified name.</summary>
    /// <returns>Not documented yet.</returns>
   string getName();

    /// <summary>Gets the attribute's _namespace URI, if it's bound to a
    /// _namespace.</summary>
    /// <returns>Not documented yet.</returns>
   string getNamespaceURI();

    /// <summary>Gets the attribute name's prefix (the part before the
    /// colon, if it's bound to a _namespace).</summary>
    /// <returns>Not documented yet.</returns>
   string getPrefix();

    /// <summary>Gets the attribute's value.</summary>
    /// <returns>Not documented yet.</returns>
   string getValue();
}
}
