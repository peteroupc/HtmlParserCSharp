namespace com.upokecenter.html {
using System;

/**
 * 
 * Represents the HTML "!DOCTYPE" tag.
 * 
 * @author Peter
 *
 */
public interface IDocumentType : INode {
	/**
	 * Gets the name of this document type.  For HTML documents,
	 * this should be "html".
	 * 
	 * 
	 */
	 string getName();
	/**
	 * 
	 * Gets the  identifier of this document type.
	 * 
	 * 
	 */
	 string getPublicId();
	/**
	 * Gets the system identifier of this document type.
	 * 
	 * 
	 */
	 string getSystemId();
}

}
