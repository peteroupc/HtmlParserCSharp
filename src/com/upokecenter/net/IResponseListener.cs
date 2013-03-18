namespace com.upokecenter.net {
using System;

using System.IO;


public interface IResponseListener<T> {
	/**
	 * Processes the HTTP response on a background thread.
	 * Please note: For the response to be cacheable, the entire
	 * stream must be read to the end.
	 * @param url URL of the resource.
	 * @param stream Input stream for the response body.
	 *   The listener must not close the stream.
	 * @param headers
	 * 
	 * @
	 */
	 T processResponse(string url,
			PeterO.Support.InputStream stream, IHttpHeaders headers) ;
}
}
