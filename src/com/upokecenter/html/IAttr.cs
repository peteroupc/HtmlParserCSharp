namespace com.upokecenter.html {
using System;

/**
 * Represents one of the attributes within an HTML element.
 * 
 * @author Peter
 *
 */
public interface IAttr {

	 string getPrefix();

	 string getLocalName();

	 string getName();

	 string getNamespaceURI();

	 string getValue();

}
}
