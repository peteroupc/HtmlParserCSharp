namespace com.upokecenter.net {
using System;


using System.Collections.Generic;


public interface IHttpHeaders {
	 string getUrl();
	 string getRequestMethod();
	 string getHeaderField(string name);
	 string getHeaderField(int name);
	 string getHeaderFieldKey(int name);
	 int getResponseCode();
	 long getHeaderFieldDate(string field, long defaultValue);
	 IDictionary<string,IList<string>> getHeaderFields();
}

}
