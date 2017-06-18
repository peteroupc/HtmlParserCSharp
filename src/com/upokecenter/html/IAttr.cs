namespace com.upokecenter.html {
using System;

/*
 * Represents one of the attributes within an HTML element.
 *
 * @author Peter
 *
 */
public interface IAttr {
    /// <summary>Gets the attribute name's local name (the part after the
    /// colon, if it's bound to a _namespace).</summary>
   string getLocalName();

    /// <summary>Gets the attribute's qualified name.</summary>
   string getName();

    /// <summary>Gets the attribute's _namespace URI, if it's bound to a
    /// _namespace.</summary>
   string getNamespaceURI();

    /// <summary>Gets the attribute name's prefix (the part before the
    /// colon, if it's bound to a _namespace).</summary>
   string getPrefix();

    /// <summary>Gets the attribute's value.</summary>
   string getValue();
}
}
