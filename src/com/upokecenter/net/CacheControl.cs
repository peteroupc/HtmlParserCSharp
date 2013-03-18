namespace com.upokecenter.net {
using System;

using System.IO;






using System.Collections.Generic;






using com.upokecenter.util;

internal class CacheControl {

	public override string ToString() {
		return "CacheControl [cacheability=" + cacheability + ", noStore="
				+ noStore + ", noTransform=" + noTransform
				+ ", mustRevalidate=" + mustRevalidate + ", requestTime="
				+ requestTime + ", responseTime=" + responseTime + ", maxAge="
				+ maxAge + ", date=" + date + ", age=" + age + ", code=" + code
				+ ", headerFields=" + headers + "]";
	}
	private int cacheability=0;
	// Client must not store the response
	// to disk and must remove it from memory
	// as soon as it's finished with it
	private bool noStore=false;
	// Client must not convert the response
	// to a different format before caching it
	private bool noTransform=false;
	// Client must re-check the server
	// after the response becomes stale
	private bool mustRevalidate=false;
	private long requestTime=0;
	private long responseTime=0;
	private long maxAge=0;
	private long date=0;
	private long age=0;
	private int code=0;
	private string uri="";
	private string requestMethod="";
	private IList<string> headers;

	public int getCacheability() {
		return cacheability;
	}
	public bool isNoStore() {
		return noStore;
	}
	public bool isNoTransform() {
		return noTransform;
	}
	public bool isMustRevalidate() {
		return mustRevalidate;
	}

	private long getAge(){
		long now=PeterO.Support.DateTimeImpl.getPersistentCurrentDate();
		long age=Math.Max(0,Math.Max(now-date,this.age));
		age+=(responseTime-requestTime);
		age+=(now-responseTime);
		age=(age>Int32.MaxValue) ? Int32.MaxValue : (int)age;
		return age;
	}

	public bool isFresh() {
		if(cacheability==0 || noStore)return false;
		return (maxAge>getAge());
	}

	private CacheControl(){
		headers=new List<string>();
	}

	public static CacheControl getCacheControl(IHttpHeaders headers, long requestTime){
		CacheControl cc=new CacheControl();
		bool proxyRevalidate=false;
		int sMaxAge=0;
		bool publicCache=false;
		bool privateCache=false;
		bool noCache=false;
		long expires=0;
		bool hasExpires=false;
		cc.uri=headers.getUrl();
		string cacheControl=headers.getHeaderField("cache-control");
		if(cacheControl!=null){
			int index=0;
			int[] intval=new int[1];
			while(index<cacheControl.Length){
				int current=index;
				if((index=HeaderParser.parseToken(cacheControl,current,"private",true))!=current){
					privateCache=true;
				} else if((index=HeaderParser.parseToken(cacheControl,current,"no-cache",true))!=current){
					noCache=true;
					//Console.WriteLine("returning early because it saw no-cache");
					return null; // return immediately, this is not cacheable
				} else if((index=HeaderParser.parseToken(
						cacheControl,current,"no-store",false))!=current){
					cc.noStore=true;
					//Console.WriteLine("returning early because it saw no-store");
					return null; // return immediately, this is not cacheable or storable
				} else if((index=HeaderParser.parseToken(
						cacheControl,current,"public",false))!=current){
					publicCache=true;
				} else if((index=HeaderParser.parseToken(
						cacheControl,current,"no-transform",false))!=current){
					cc.noTransform=true;
				} else if((index=HeaderParser.parseToken(
						cacheControl,current,"must-revalidate",false))!=current){
					cc.mustRevalidate=true;
				} else if((index=HeaderParser.parseToken(
						cacheControl,current,"proxy-revalidate",false))!=current){
					proxyRevalidate=true;
				} else if((index=HeaderParser.parseTokenWithDelta(
						cacheControl,current,"max-age",intval))!=current){
					cc.maxAge=intval[0];
				} else if((index=HeaderParser.parseTokenWithDelta(
						cacheControl,current,"s-maxage",intval))!=current){
					sMaxAge=intval[0];
				} else {
					index=HeaderParser.skipDirective(cacheControl,current);
				}
			}
			if(!publicCache && !privateCache && !noCache){
				noCache=true;
			}
		} else {
			int code=headers.getResponseCode();
			if((code==200 || code==203 || code==300 || code==301 || code==410) &&
					headers.getHeaderField("authorization")==null){
				publicCache=true;
				privateCache=false;
			} else {
				noCache=true;
			}
		}
		if(headers.getResponseCode()==206) {
			noCache=true;
		}
		string pragma=headers.getHeaderField("pragma");
		if(pragma!=null && "no-cache".Equals(StringUtility.toLowerCaseAscii(pragma))){
			noCache=true;
			//Console.WriteLine("returning early because it saw pragma no-cache");
			return null;
		}
		long now=PeterO.Support.DateTimeImpl.getPersistentCurrentDate();
		cc.code=headers.getResponseCode();
		cc.date=now;
		cc.responseTime=now;
		cc.requestTime=requestTime;
		if(proxyRevalidate){
			// Enable must-revalidate for simplicity;
			// proxyRevalidate usually only applies to shared caches
			cc.mustRevalidate=true;
		}
		if(headers.getHeaderField("date")!=null){
			cc.date=headers.getHeaderFieldDate("date",Int64.MinValue);
			if(cc.date==Int64.MinValue) {
				noCache=true;
			}
		} else {
			noCache=true;
		}
		string expiresHeader=headers.getHeaderField("expires");
		if(expiresHeader!=null){
			expires=headers.getHeaderFieldDate("expires",Int64.MinValue);
			hasExpires=(cc.date!=Int64.MinValue);
		}
		if(headers.getHeaderField("age")!=null){
			try {
				cc.age=Int32.Parse(headers.getHeaderField("age"),System.Globalization.CultureInfo.InvariantCulture);
				if(cc.age<0) {
					cc.age=0;
				}
			} catch(FormatException){
				cc.age=-1;
			}
		}
		if(cc.maxAge>0 || sMaxAge>0){
			long maxAge=cc.maxAge; // max age in seconds
			if(maxAge==0) {
				maxAge=sMaxAge;
			}
			if(cc.maxAge>0 && sMaxAge>0){
				maxAge=Math.Max(cc.maxAge,sMaxAge);
			}
			cc.maxAge=maxAge*1000L; // max-age and s-maxage are in seconds
			hasExpires=false;
		} else if(hasExpires && !noCache){
			long maxAge=expires-cc.date;
			cc.maxAge=(maxAge>Int32.MaxValue) ? Int32.MaxValue : (int)maxAge;
		} else if(noCache || cc.noStore){
			cc.maxAge=0;
		} else {
			cc.maxAge=24L*3600L*1000L;
		}
		string reqmethod=headers.getRequestMethod();
		if(reqmethod==null || (
				!StringUtility.toLowerCaseAscii(reqmethod).Equals("get")))
			// caching responses other than GET responses not supported
			return null;
		cc.requestMethod=StringUtility.toLowerCaseAscii(reqmethod);
		cc.cacheability=2;
		if(noCache) {
			cc.cacheability=0;
		} else if(privateCache) {
			cc.cacheability=1;
		}
		int i=0;
		cc.headers.Add(headers.getHeaderField(null));
		while(true){
			string newValue=headers.getHeaderField(i);
			if(newValue==null) {
				break;
			}
			string key=headers.getHeaderFieldKey(i);
			i++;
			if(key==null){
				//Console.WriteLine("null key");
				continue;
			}
			key=StringUtility.toLowerCaseAscii(key);
			// to simplify matters, don't include Age header fields;
			// so-called hop-by-hop headers are also not included
			if(!"age".Equals(key) &&
					!"connection".Equals(key) &&
					!"keep-alive".Equals(key) &&
					!"proxy-authenticate".Equals(key) &&
					!"proxy-authorization".Equals(key) &&
					!"te".Equals(key) &&
					!"trailer".Equals(key) && // NOTE: NOT Trailers
					!"transfer-encoding".Equals(key) &&
					!"upgrade".Equals(key)){
				cc.headers.Add(key);
				cc.headers.Add(newValue);
			}
		}
		//Console.WriteLine(" cc: %s",cc);
		return cc;
	}

	public static CacheControl fromFile(PeterO.Support.File f) {
		PeterO.Support.WrappedInputStream fs=new PeterO.Support.WrappedInputStream(new FileStream(f.ToString(),FileMode.Open));
		try {
			return new CacheControlSerializer().readObjectFromStream(fs);
		} finally {
			if(fs!=null) {
				fs.Close();
			}
		}
	}

	public static void toFile(CacheControl o, PeterO.Support.File file) {
		Stream fs=new FileStream((file).ToString(),FileMode.Create);
		try {
			new CacheControlSerializer().writeObjectToStream(o,fs);
		} finally {
			if(fs!=null) {
				fs.Close();
			}
		}
	}

	private class CacheControlSerializer {
		public CacheControl readObjectFromStream(PeterO.Support.InputStream stream)  {
			try {
				System.Collections.Generic.IDictionary<string, object> jsonobj=Json.JsonParser.FromJson(StreamUtility.streamToString(stream));
				CacheControl cc=new CacheControl();
				cc.cacheability=(int)jsonobj["cacheability"];
				cc.noStore=(bool)jsonobj["noStore"];
				cc.noTransform=(bool)jsonobj["noTransform"];
				cc.mustRevalidate=(bool)jsonobj["mustRevalidate"];
				cc.requestTime=Int64.Parse((string)jsonobj["requestTime"],System.Globalization.CultureInfo.InvariantCulture);
				cc.responseTime=Int64.Parse((string)jsonobj["responseTime"],System.Globalization.CultureInfo.InvariantCulture);
				cc.maxAge=Int64.Parse((string)jsonobj["maxAge"],System.Globalization.CultureInfo.InvariantCulture);
				cc.date=Int64.Parse((string)jsonobj["date"],System.Globalization.CultureInfo.InvariantCulture);
				cc.code=(int)jsonobj["code"];
				cc.age=Int64.Parse((string)jsonobj["age"],System.Globalization.CultureInfo.InvariantCulture);
				cc.uri=(string)jsonobj["uri"];
				cc.requestMethod=(string)jsonobj["requestMethod"];
				if(cc.requestMethod!=null) {
					cc.requestMethod=StringUtility.toLowerCaseAscii(cc.requestMethod);
				}
				cc.headers=new List<string>();
				System.Collections.Generic.IList<object> jsonarr=(System.Collections.Generic.IList<object>)jsonobj["headers"];
				for(int i=0;i<jsonarr.Count;i++){
					string str=(string)jsonarr[i];
					if(str!=null && (i%2)!=0){
						str=StringUtility.toLowerCaseAscii(str);
						if("age".Equals(str) ||
								"connection".Equals(str) ||
								"keep-alive".Equals(str) ||
								"proxy-authenticate".Equals(str) ||
								"proxy-authorization".Equals(str) ||
								"te".Equals(str) ||
								"trailers".Equals(str) ||
								"transfer-encoding".Equals(str) ||
								"upgrade".Equals(str)){
							// Skip "age" header field and
							// hop-by-hop header fields
							i++;
							continue;
						}
					}
					cc.headers.Add(str);
				}
				return cc;
			} catch(InvalidCastException e){
				Console.WriteLine(e.StackTrace);
				return null;
			} catch(FormatException e){
				Console.WriteLine(e.StackTrace);
				return null;
			} catch (Json.InvalidJsonException e) {
				Console.WriteLine(e.StackTrace);
				return null;
			}
		}
		public void writeObjectToStream(CacheControl o, Stream stream)
				 {
			System.Collections.Generic.IDictionary<string, object> jsonobj=new System.Collections.Generic.Dictionary<string, object>();
			jsonobj["cacheability"]=o.cacheability;
			jsonobj["noStore"]=o.noStore;
			jsonobj["noTransform"]=o.noTransform;
			jsonobj["mustRevalidate"]=o.mustRevalidate;
			jsonobj["requestTime"]=Convert.ToString(o.requestTime,System.Globalization.CultureInfo.InvariantCulture);
			jsonobj["responseTime"]=Convert.ToString(o.responseTime,System.Globalization.CultureInfo.InvariantCulture);
			jsonobj["maxAge"]=Convert.ToString(o.maxAge,System.Globalization.CultureInfo.InvariantCulture);
			jsonobj["date"]=Convert.ToString(o.date,System.Globalization.CultureInfo.InvariantCulture);
			jsonobj["uri"]=o.uri;
			jsonobj["requestMethod"]=o.requestMethod;
			jsonobj["code"]=o.code;
			jsonobj["age"]=Convert.ToString(o.age,System.Globalization.CultureInfo.InvariantCulture);
			System.Collections.Generic.IList<object> jsonarr=new System.Collections.Generic.List<object>();
			foreach(string header in o.headers){
				jsonarr.Add(header);
			}
			jsonobj["headers"]=jsonarr;
			StreamUtility.stringToStream(Json.JsonParser.ToJson(jsonobj),stream);
		}
	}

	public IHttpHeaders getHeaders(long length) {
		return new AgedHeaders(this,getAge(),length);
	}

	private class AgedHeaders : IHttpHeaders {

		CacheControl cc=null;
		long age=0;
		IList<string> list=new List<string>();

		public AgedHeaders(CacheControl cc, long age, long length){
			list.Add(cc.headers[0]);
			for(int i=1;i<cc.headers.Count;i+=2){
				string key=cc.headers[i];
				if(key!=null){
					key=StringUtility.toLowerCaseAscii(key);
					if("content-length".Equals(key)||"age".Equals(key)) {
						continue;
					}
				}
				list.Add(cc.headers[i]);
				list.Add(cc.headers[i+1]);
			}
			this.age=age/1000; // convert age to seconds
			list.Add("age");
			list.Add(Convert.ToString(this.age,System.Globalization.CultureInfo.InvariantCulture));
			list.Add("content-length");
			list.Add(Convert.ToString(length,System.Globalization.CultureInfo.InvariantCulture));
			//Console.WriteLine("aged=%s",list);
			this.cc=cc;
		}

		public string getRequestMethod() {
			return cc.requestMethod;
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
			index=(index)*2+1+1;
			if(index<0 || index>=list.Count)
				return null;
			return list[index+1];
		}
		public string getHeaderFieldKey(int index) {
			index=(index)*2+1;
			if(index<0 || index>=list.Count)
				return null;
			return list[index];
		}
		public int getResponseCode() {
			return cc.code;
		}
		public long getHeaderFieldDate(string field, long defaultValue) {
			return HeaderParser.parseHttpDate(getHeaderField(field),defaultValue);
		}
		public IDictionary<string, IList<string>> getHeaderFields() {
			IDictionary<string, IList<string>> map=new PeterO.Support.LenientDictionary<string, IList<string>>();
			map.Add(null,(new string[]{list[0]}));
			for(int i=1;i<list.Count;i+=2){
				string key=list[i];
				IList<string> templist=map[key];
				if(templist==null){
					templist=new List<string>();
					map.Add(key,templist);
				}
				templist.Add(list[i+1]);
			}
			// Make lists unmodifiable
			foreach(string key in new List<string>(map.Keys)){
				map.Add(key,PeterO.Support.Collections.UnmodifiableList(map[key]));
			}
			return PeterO.Support.Collections.UnmodifiableMap(map);
		}

		public string getUrl() {
			return cc.uri;
		}
	}

	public string getRequestMethod() {
		return requestMethod;
	}
	public string getUri() {
		return uri;
	}
}
}
