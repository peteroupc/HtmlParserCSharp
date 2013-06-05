namespace com.upokecenter.html {
using System;

/*
 * Represents one of the attributes within an HTML element.
 * 
 * @author Peter
 *
 */
public interface IAttr {

  /**
   * Gets the attribute name's local name (the part after the colon,
   * if it's bound to a _namespace).
   */
   string getLocalName();

  /**
   * Gets the attribute's qualified name.
   */
   string getName();

  /**
   * Gets the attribute's _namespace URI, if it's bound to a _namespace.
   */
   string getNamespaceURI();

  /**
   * Gets the attribute name's prefix (the part before the colon,
   * if it's bound to a _namespace).
   */
   string getPrefix();

  /**
   * Gets the attribute's value.
   */
   string getValue();

}
}
