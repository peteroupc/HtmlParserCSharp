/*

Licensed under the Expat License.

Copyright (C) 2013 Peter Occil

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*/

namespace com.upokecenter.html {
using System;

using System.IO;





using com.upokecenter.net;




public sealed class HtmlDocument {
	private sealed class ParseURLListener : IResponseListener<IDocument> {
		public IDocument processResponse(string url, PeterO.Support.InputStream stream,
				IHttpHeaders headers)  {
			string charset=HeaderParser.getCharset(
					headers.getHeaderField("content-type"),0);
			HtmlParser parser=new HtmlParser(stream,headers.getUrl(),charset);
			return parser.parse();
		}
	}
	private HtmlDocument(){}

	/**
	 * 
	 * Gets the absolute URL from an HTML element.
	 * 
	 * @param node An IMG, A, AREA, LINK, BASE, FRAME, or SCRIPT element
	 * @return an absolute URL of the element's SRC or HREF, or an
	 * empty _string if none exists.
	 */
	public static string getHref(IElement node){
		string name=node.getTagName();
		string href="";
		if("A".Equals(name) || "LINK".Equals(name) || "AREA".Equals(name) ||
				"BASE".Equals(name)){
			href=node.getAttribute("href");
		} else if("IMG".Equals(name) || "SCRIPT".Equals(name) || "FRAME".Equals(name)){
			href=node.getAttribute("src");
		} else
			return "";
		if(href==null || href.Length==0)
			return "";
		return HtmlParser.resolveURL(node,href,null);
	}

	/**
	 * 
	 * Resolves a URL relative to an HTML element.
	 * 
	 * @param node an HTML element.
	 * @param href Absolute or relative URL.
	 * @return an absolute URL corresponding to the HTML element,
	 * or an empty _string if _href_ is null or empty.
	 */
	public static string getHref(IElement node, string href){
		if(href==null || href.Length==0)
			return "";
		return HtmlParser.resolveURL(node,href,null);
	}

	/**
	 * 
	 * Parses an HTML document from a URL.
	 * 
	 * @param url URL of the HTML document. In addition to HTTP
	 * and other URLs supported by URLConnection, this method also
	 * supports Data URLs.
	 * @return a document _object from the HTML document
	 * @ if an I/O error occurs, such as a network
	 * error, a download error, and so on.
	 */
	public static IDocument parseURL(string url)  {
		return DownloadHelper.downloadUrl(url,
				new ParseURLListener(), false);
	}

	/**
	 * 
	 * Parses an HTML document from an input stream, using "about:blank"
	 * as its address.
	 * 
	 * @param stream an input stream
	 * 
	 * @ if an I/O error occurs.
	 */
	public static IDocument parseStream(PeterO.Support.InputStream stream)
			 {
		return parseStream(stream,"about:blank");
	}

	/**
	 * 
	 * Parses an HTML document from an input stream, using the given
	 * URL as its address.
	 * 
	 * @param stream
	 * @param address
	 * 
	 * @
	 */
	public static IDocument parseStream(PeterO.Support.InputStream stream, string address)
			 {
		if(!stream.markSupported()){
			stream=new PeterO.Support.BufferedInputStream(stream);
		}
		HtmlParser parser=new HtmlParser(stream,address,null);
		return parser.parse();
	}
	/**
	 * 
	 * Parses an HTML document from a file on the file system.
	 * 
	 * @param file
	 * 
	 * @
	 */
	public static IDocument parseFile(string file)
			 {
		PeterO.Support.InputStream stream=null;
		try {
			string fileURL=new PeterO.Support.File(file).toURI().ToString();
			stream=new PeterO.Support.BufferedInputStream(new PeterO.Support.WrappedInputStream(new System.IO.FileStream((file).ToString(),System.IO.FileMode.Open)),8192);
			HtmlParser parser=new HtmlParser(stream,fileURL,null);
			return parser.parse();
		} finally {
			if(stream!=null) {
				stream.Close();
			}
		}
	}
}

}
