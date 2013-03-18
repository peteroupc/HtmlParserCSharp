namespace com.upokecenter.html {
using System;

/**
 * 
 * Represents a comment on an HTML document.
 * 
 * @author Peter
 *
 */
public interface IComment : INode {
	/**
	 * Gets the comment's text.
	 */
	 string getData();
}

}
