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
namespace com.upokecenter.net {
using System;

using System.Collections.Generic;




using com.upokecenter.util;
internal class HttpHeadersFromMap : IHttpHeaders {

	IDictionary<string,IList<string>> map;
	IList<string> list;
	string requestMethod;
	string urlString;

	public HttpHeadersFromMap(string urlString, string requestMethod, IDictionary<string,IList<string>> map){
		this.map=map;
		this.urlString=urlString;
		this.requestMethod=requestMethod;
		list=new List<string>();
		List<string> keyset=new List<string>();
		foreach(string s in this.map.Keys){
			if(s==null){
				// Add status line (also has the side
				// effect that it will appear first in the list)
				IList<string> v=this.map[s];
				if(v!=null && v.Count>0){
					list.Add(v[0]);
				} else {
					list.Add("HTTP/1.1 200 OK");
				}
			} else {
				keyset.Add(s);
			}
		}
		keyset.Sort();
		// Add the remaining headers in sorted order
		foreach(string s in keyset){
			IList<string> v=this.map[s];
			if(v!=null && v.Count>0){
				foreach(string ss in v){
					list.Add(s);
					list.Add(ss);
				}
			}
		}
	}

	public string getRequestMethod() {
		return requestMethod;
	}

	public string getHeaderField(string name) {
		if(name==null)return list[0];
		name=StringUtility.toLowerCaseAscii(name);
		string last=null;
		for(int i=1;i<list.Count;i+=2){
			string key=list[i];
			if(name.Equals(key)) {
				last=list[i+1];
			}
		}
		return last;
	}
	public string getHeaderField(int index) {
		if(index==0)return list[0];
		if(index<0)return null;
		index=(index-1)*2+1+1;
		if(index<0 || index>=list.Count)
			return null;
		return list[index+1];
	}
	public string getHeaderFieldKey(int index) {
		if(index==0 || index<0)return null;
		index=(index-1)*2+1;
		if(index<0 || index>=list.Count)
			return null;
		return list[index];
	}
	public int getResponseCode() {
		string status=getHeaderField(null);
		if(status==null)return -1;
		return HeaderParser.getResponseCode(status);
	}
	public long getHeaderFieldDate(string field, long defaultValue) {
		return HeaderParser.parseHttpDate(getHeaderField(field),defaultValue);
	}

	public IDictionary<string, IList<string>> getHeaderFields() {
		return PeterO.Support.Collections.UnmodifiableMap(map);
	}

	public string getUrl() {
		return urlString;
	}
}
}
