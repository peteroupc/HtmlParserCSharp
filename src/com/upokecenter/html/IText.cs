namespace com.upokecenter.html {
using System;

/**
 * 
 * Represents text within an HTML document.
 * 
 * @author Peter
 *
 */
public interface IText : INode {
	/**
	 * 
	 * Gets this node's text.
	 * 
	 * 
	 */
	 string getData();
}

}
