/*
If you like this, you should donate to Peter O.
at: http://upokecenter.com/d/



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
using com.upokecenter.util;

public sealed class HtmlDocument {
	private sealed class ParseURLListener : IResponseListener<IDocument> {
		public IDocument processResponse(string url, PeterO.Support.InputStream stream,
				IHttpHeaders headers)  {
			string contentType=headers.getHeaderField("content-type");
			return HtmlDocument.parseStream(stream,headers.getUrl(),contentType,
					headers.getHeaderField("content-language"));
		}
	}
	/**
	 * 
	 * Gets the absolute URL from an HTML element.
	 * 
	 * @param node A HTML element containing a URL
	 * @return an absolute URL of the element's SRC, DATA, or HREF, or an
	 * empty _string if none exists.
	 */
	public static string getHref(IElement node){
		string name=node.getTagName();
		string href="";
		if("A".Equals(name) || "LINK".Equals(name) || "AREA".Equals(name) ||
				"BASE".Equals(name)){
			href=node.getAttribute("href");
		} else if("OBJECT".Equals(name)){
			href=node.getAttribute("data");
		} else if("IMG".Equals(name) || "SCRIPT".Equals(name) ||
				"FRAME".Equals(name) || "SOURCE".Equals(name) ||
				"TRACK".Equals(name) ||
				"IFRAME".Equals(name) ||
				"AUDIO".Equals(name) ||
				"VIDEO".Equals(name) ||
				"EMBED".Equals(name)){
			href=node.getAttribute("src");
		} else
			return "";
		if(href==null || href.Length==0)
			return "";
		return HtmlDocument.resolveURL(node,href,null);
	}

	/**
	 * Utility method for converting a relative URL to an absolute
	 * one, using the _base URI and the encoding of the given node.
	 * 
	 * @param node An HTML node, usually an IDocument or IElement
	 * @param href A relative or absolute URL.
	 * @return An absolute URL.
	 */
	public static string getHref(INode node, string href){
		if(href==null || href.Length==0)
			return "";
		return HtmlDocument.resolveURL(node,href,null);
	}

	/**
	 * 
	 * Parses an HTML document from a file on the file system.
	 * Its address will correspond to that file.
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
			stream=new PeterO.Support.BufferedInputStream(new PeterO.Support.WrappedInputStream(new FileStream((file).ToString(),FileMode.Open)),8192);
			HtmlParser parser=new HtmlParser(stream,fileURL,null);
			return parser.parse();
		} finally {
			if(stream!=null) {
				stream.Close();
			}
		}
	}

	/**
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

	public static IDocument parseStream(
			PeterO.Support.InputStream stream, string address)
					 {
		return parseStream(stream,address,"text/html");
	}

	public static IDocument parseStream(
			PeterO.Support.InputStream stream, string address, string contentType)
					 {
		return parseStream(stream,address,contentType,null);
	}

	/**
	 * 
	 * Parses an HTML document from an input stream, using the given
	 * URL as its address.
	 * 
	 * @param stream an input stream representing an HTML document.
	 * @param address an absolute URL representing an address.
	 * @param contentType Desired MIME media type of the document, including the
	 *   charset parameter, if any.  Examples: "text/html" or
	 *  "application/xhtml+xml; charset=utf-8".
	 * @param contentLang Language tag from the Content-Language header
	 * @return an IDocument representing the HTML document.
	 * @ if an I/O error occurs
	 * @ if the given address
	 * is not an absolute URL.
	 */
	public static IDocument parseStream(
			PeterO.Support.InputStream stream, string address, string contentType, string contentLang)
					 {
		if((stream)==null)throw new ArgumentNullException("stream");
		if((address)==null)throw new ArgumentNullException("address");
		if((contentType)==null)throw new ArgumentNullException("contentType");
		if(!stream.markSupported()){
			stream=new PeterO.Support.BufferedInputStream(stream);
		}
		string mediatype=HeaderParser.getMediaType(contentType);
		string charset=HeaderParser.getCharset(contentType);
		if(mediatype.Equals("text/html")){
			// TODO: add lang
			HtmlParser parser=new HtmlParser(stream,address,charset,contentLang);
			return parser.parse();
		} else if(mediatype.Equals("application/xhtml+xml") ||
				mediatype.Equals("application/xml") ||
				mediatype.Equals("image/svg+xml") ||
				mediatype.Equals("text/xml")){
			XhtmlParser parser=new XhtmlParser(stream,address,charset,contentLang);
			return parser.parse();
		} else
			throw new ArgumentException("content type not supported: "+mediatype);
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
	public static string resolveURL(INode node, string url, string _base){
		string encoding=((node is IDocument) ?
				((IDocument)node).getCharacterSet() : node.getOwnerDocument().getCharacterSet());
		if("utf-16be".Equals(encoding) ||
				"utf-16le".Equals(encoding)){
			encoding="utf-8";
		}
		if(_base==null){
			_base=node.getBaseURI();
		}
		URL resolved=URL.parse(url,URL.parse(_base),encoding,true);
		if(resolved==null)
			return _base;
		return resolved.ToString();
	}

	private HtmlDocument(){}
}

}
