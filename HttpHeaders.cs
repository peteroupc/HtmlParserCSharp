/*
 * Created by SharpDevelop.
 * User: Peter
 * Date: 3/16/2013
 * Time: 10:17 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using com.upokecenter.net;
using System.Net;

namespace PeterO.Support
{
	public sealed class HttpHeaders : com.upokecenter.net.IHttpHeaders {
		
		public WebResponse response;
		public HttpHeaders(WebResponse response){
			this.response=response;
		}
		
		public string getUrl()
		{
			return this.response.ResponseUri.ToString();
		}
		
		public string getRequestMethod()
		{
			if(response is HttpWebResponse)
				return ((HttpWebResponse)this.response).Method;
			return "";
		}
		
		public string getHeaderField(string name)
		{
			if(this.response is HttpWebResponse){
				if(name==null)return GetStatusLine((HttpWebResponse)this.response);
			}
			return this.response.Headers.Get(name);
		}
		
		private string GetStatusLine(HttpWebResponse resp){
			Version vers=resp.ProtocolVersion;
			if(vers==HttpVersion.Version10)
				return "HTTP/1.0 "+Convert.ToString(
					resp.StatusCode,System.Globalization.CultureInfo.InvariantCulture)+" "+resp.StatusDescription;
			else
				return "HTTP/1.1 "+Convert.ToString(
					resp.StatusCode,System.Globalization.CultureInfo.InvariantCulture)+" "+resp.StatusDescription;				
		}
		
		public string getHeaderField(int name)
		{
			if(name<0)return null;
			if(this.response is HttpWebResponse){
				if(name==0)return GetStatusLine((HttpWebResponse)this.response);
				name--;
			}
			if(name>this.response.Headers.Count)return null;
			return this.response.Headers.Get(name-1);
		}
		
		public string getHeaderFieldKey(int name)
		{
			if(name<0)return null;
			if(this.response is HttpWebResponse){
				if(name==0)return null;
				name--;
			}
			if(name>this.response.Headers.Count)return null;
			return this.response.Headers.GetKey(name-1);
		}
		
		public int getResponseCode()
		{
			if(response is FtpWebResponse)
				return (int)((FtpWebResponse)response).StatusCode;
			if(response is HttpWebResponse)
				return (int)((HttpWebResponse)response).StatusCode;
			return 0;
		}
		
		public long getHeaderFieldDate(string field, long defaultValue)
		{
			String value=getHeaderField(field);
			if(value==null)return defaultValue;
			return HeaderParser.parseHttpDate(value,defaultValue);
		}
		
		public IDictionary<string, IList<string>> getHeaderFields()
		{
			IDictionary<string, IList<string>> map=new LenientDictionary<string, IList<string>>();
			if(this.response is HttpWebResponse)
				map.Add(null,Collections.UnmodifiableList(new String[]{GetStatusLine((HttpWebResponse)this.response)}));
			foreach(String k in this.response.Headers.AllKeys){
				map.Add(k,Collections.UnmodifiableList(this.response.Headers.GetValues(k)));
			}
			return Collections.UnmodifiableMap(map);
		}
	}
}
