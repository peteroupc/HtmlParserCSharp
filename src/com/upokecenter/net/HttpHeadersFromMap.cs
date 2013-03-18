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
