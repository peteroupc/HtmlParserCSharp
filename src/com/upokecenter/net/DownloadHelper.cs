namespace com.upokecenter.net {
using System;

using System.IO;





using System.Collections.Generic;






using com.upokecenter.util;




public sealed class DownloadHelper {

	private DownloadHelper(){}

	private sealed class CacheFilter {
		private string cacheFileName;

		public CacheFilter(string cacheFileName) {
			this.cacheFileName = cacheFileName;
		}

		public bool accept(PeterO.Support.File dir, string filename) {
			return filename.StartsWith(cacheFileName+"-",StringComparison.Ordinal) &&
					!filename.EndsWith(".cache",StringComparison.Ordinal);
		}
	}

	private static string getCacheFileName(string uri, bool[] incomplete){
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		for(int i=0;i<uri.Length;i++){
			char c=uri[i];
			if(c<=0x20 || c==127 ||
					c=='$' || c=='/' || c=='\\' || c==':' ||
					c=='"' || c=='\'' || c=='|' || c=='<' ||
					c=='>' || c=='*' || c=='?'){
				builder.Append('$');
				builder.Append("0123456789ABCDEF"[(c>>4)&15]);
				builder.Append("0123456789ABCDEF"[(c)&15]);
			} else {
				builder.Append(c);
			}
			if(builder.Length>=190){
				if(incomplete!=null) {
					incomplete[0]=true;
				}
				return builder.ToString();
			}
		}
		if(incomplete!=null) {
			incomplete[0]=false;
		}
		return builder.ToString();
	}

	internal class FileBasedHeaders : IHttpHeaders {

		long date,length;
		string urlString;

		public FileBasedHeaders(string urlString, long length){
			date=PeterO.Support.DateTimeImpl.getPersistentCurrentDate();
			this.length=length;
			this.urlString=urlString;
		}

		public string getRequestMethod() {
			return "GET";
		}

		public string getHeaderField(string name) {
			if(name==null)return "HTTP/1.1 200 OK";
			if("date".Equals(StringUtility.toLowerCaseAscii(name)))
				return HeaderParser.formatHttpDate(date);
			if("content-length".Equals(StringUtility.toLowerCaseAscii(name)))
				return Convert.ToString(length,System.Globalization.CultureInfo.InvariantCulture);
			return null;
		}

		public string getHeaderField(int name) {
			if(name==0)
				return getHeaderField(null);
			else if(name==1)
				return getHeaderField("date");
			else if(name==2)
				return getHeaderField("content-length");
			return null;
		}

		public string getHeaderFieldKey(int name) {
			if(name==0)
				return null;
			if(name==1)
				return "date";
			if(name==2)
				return "content-length";
			return null;
		}

		public int getResponseCode() {
			return 200;
		}

		public long getHeaderFieldDate(string field, long defaultValue) {
			if(field!=null && "date".Equals(StringUtility.toLowerCaseAscii(field)))
				return date;
			return defaultValue;
		}

		private IList<string> asReadOnlyList(string[] a){
			return PeterO.Support.Collections.UnmodifiableList((a));
		}

		public IDictionary<string, IList<string>> getHeaderFields() {
			IDictionary<string, IList<string>> map=new PeterO.Support.LenientDictionary<string, IList<string>>();
			map.Add(null,asReadOnlyList(new string[]{getHeaderField(null)}));
			map.Add("date",asReadOnlyList(new string[]{getHeaderField("date")}));
			map.Add("content-length",asReadOnlyList(new string[]{getHeaderField("content-length")}));
			return PeterO.Support.Collections.UnmodifiableMap(map);
		}

		public string getUrl() {
			return urlString;
		}
	}

	private static void recursiveListFiles(PeterO.Support.File file, IList<PeterO.Support.File> files){
		foreach(PeterO.Support.File f in file.listFiles()){
			if(f.isDirectory()){
				recursiveListFiles(f,files);
			}
			files.Add(f);
		}
	}

	public static void pruneCache(PeterO.Support.File cache, long maximumSize){
		if(cache==null || !cache.isDirectory())return;
		while(true){
			long length=0;
			bool exceeded=false;
			long oldest=Int64.MaxValue;
			int count=0;
			IList<PeterO.Support.File> files=new List<PeterO.Support.File>();
			recursiveListFiles(cache,files);
			foreach(PeterO.Support.File file in files){
				if(file.isFile()){
					length+=file.length();
					if(length>maximumSize){
						exceeded=true;
					}
					oldest=file.lastModified();
					count++;
				}
			}
			if(count<=1||!exceeded)return;
			long threshold=oldest+Math.Abs(oldest-PeterO.Support.DateTimeImpl.getPersistentCurrentDate())/2;
			count=0;
			foreach(PeterO.Support.File file in files){
				if(file.lastModified()<threshold){
					if(file.isDirectory()){
						if(file.delete()) {
							count++;
						}
					} else {
						length-=file.length();
						if(file.delete()) {
							count++;
						}
						if(length<maximumSize)
							return;
					}
				}
			}
			if(count==0)return;
		}
	}




	public static Object getLegacyResponseCache(PeterO.Support.File cachePath){
		return PeterO.Support.DownloadHelperImpl.newResponseCache(cachePath);
	}


	internal class CacheResponseInfo {
		public Object cr=null;
		public PeterO.Support.File trueCachedFile=null;
		public PeterO.Support.File trueCacheInfoFile=null;
	}
	internal static CacheResponseInfo getCachedResponse(
			string urlString,
			PeterO.Support.File pathForCache,
			bool getStream
			){
		bool[] incompleteName=new bool[1];
		 string cacheFileName=getCacheFileName(urlString,incompleteName)+".htm";
		PeterO.Support.File trueCachedFile=null;
		PeterO.Support.File trueCacheInfoFile=null;
		CacheResponseInfo crinfo=new CacheResponseInfo();
		if(pathForCache!=null && pathForCache.isDirectory()){
			PeterO.Support.File[] cacheFiles=new PeterO.Support.File[]{
					new PeterO.Support.File(pathForCache,cacheFileName)
			};
			if(incompleteName[0]){
				List<PeterO.Support.File> list=new List<PeterO.Support.File>();
				CacheFilter filter=new CacheFilter(cacheFileName);
				foreach(PeterO.Support.File f in pathForCache.listFiles()){
					if(filter.accept(pathForCache,f.getName())){
						list.Add(f);
					}
				}
				cacheFiles=list.ToArray();
			} else if(!getStream){
				crinfo.trueCachedFile=cacheFiles[0];
				crinfo.trueCacheInfoFile=new PeterO.Support.File(crinfo.trueCachedFile.ToString()+".cache");
				return crinfo;
			}
			//Console.WriteLine("%s, getStream=%s",(cacheFiles),getStream);
			foreach(PeterO.Support.File cacheFile in cacheFiles){
				if(cacheFile.isFile() && getStream){
					bool fresh=false;
					IHttpHeaders headers=null;
					PeterO.Support.File cacheInfoFile=new PeterO.Support.File(cacheFile.ToString()+".cache");
					if(cacheInfoFile.isFile()){
						try {
							CacheControl cc=CacheControl.fromFile(cacheInfoFile);
							//Console.WriteLine("havecache: %s",cc!=null);
							if(cc==null){
								fresh=false;
							} else {
								fresh=(cc==null) ? false : cc.isFresh();
								if(!urlString.Equals(cc.getUri())){
									// Wrong URI
									continue;
								}
								//Console.WriteLine("reqmethod: %s",cc.getRequestMethod());
								if(!"get".Equals(cc.getRequestMethod())){
									fresh=false;
								}
							}
							headers=(cc==null) ? new FileBasedHeaders(urlString,cacheFile.length()) : cc.getHeaders(cacheFile.length());
						} catch (IOException e) {
							Console.WriteLine(e.StackTrace);
							fresh=false;
							headers=new FileBasedHeaders(urlString,cacheFile.length());
						}
					} else {
						long maxAgeMillis=24L*3600L*1000L;
						long timeDiff=Math.Abs(cacheFile.lastModified()-(PeterO.Support.DateTimeImpl.getPersistentCurrentDate()));
						fresh=(timeDiff<=maxAgeMillis);
						headers=new FileBasedHeaders(urlString,cacheFile.length());
					}
					//Console.WriteLine("fresh=%s",fresh);
					if(!fresh){
						// Too old, download again
						trueCachedFile=cacheFile;
						trueCacheInfoFile=cacheInfoFile;
						trueCachedFile.delete();
						trueCacheInfoFile.delete();
						break;
					} else {
						PeterO.Support.InputStream stream=null;
						try {
							stream=new PeterO.Support.BufferedInputStream(new PeterO.Support.WrappedInputStream(new FileStream(cacheFile.ToString(),FileMode.Open)),8192);
							crinfo.cr=PeterO.Support.DownloadHelperImpl.newCacheResponse(stream,
									headers);
							//Console.WriteLine("headerfields: %s",headers.getHeaderFields());
						} catch(IOException){
							// if we get an exception here, we download again
							crinfo.cr=null;
						} finally {
							if(stream!=null) {
								try { stream.Close(); } catch(IOException){}
							}
						}
					}
				}
			}
		}
		if(pathForCache!=null){
			if(trueCachedFile==null){
				if(incompleteName[0]){
					int i=0;
					do {
						trueCachedFile=new PeterO.Support.File(pathForCache,
								cacheFileName+"-"+Convert.ToString(i,System.Globalization.CultureInfo.InvariantCulture));
						i++;
					} while(trueCachedFile.exists());
				} else {
					trueCachedFile=new PeterO.Support.File(pathForCache,cacheFileName);
				}
			}
			trueCacheInfoFile=new PeterO.Support.File(trueCachedFile.ToString()+".cache");
		}
		crinfo.trueCachedFile=trueCachedFile;
		crinfo.trueCacheInfoFile=trueCacheInfoFile;
		return crinfo;
	}

	internal class ErrorHeader : IHttpHeaders {
		string message;
		int code;
		string urlString;

		public ErrorHeader(string urlString, int code, string message){
			this.urlString=urlString;
			this.code=code;
			this.message=message;
		}

		public string getRequestMethod() {
			return "GET";
		}

		public string getHeaderField(string name) {
			if(name==null)return "HTTP/1.1 "+Convert.ToString(code,System.Globalization.CultureInfo.InvariantCulture)+" "+message;
			return null;
		}

		public string getHeaderField(int name) {
			if(name==0)
				return getHeaderField(null);
			return null;
		}

		public string getHeaderFieldKey(int name) {
			if(name==0)
				return null;
			return null;
		}



		public int getResponseCode() {
			return code;
		}

		public long getHeaderFieldDate(string field, long defaultValue) {
			return defaultValue;
		}

		private IList<string> asReadOnlyList(string[] a){
			return PeterO.Support.Collections.UnmodifiableList((a));
		}

		public IDictionary<string, IList<string>> getHeaderFields() {
			IDictionary<string, IList<string>> map=new PeterO.Support.LenientDictionary<string, IList<string>>();
			map.Add(null,asReadOnlyList(new string[]{getHeaderField(null)}));
			return PeterO.Support.Collections.UnmodifiableMap(map);
		}

		public string getUrl() {
			return urlString;
		}

	}

	internal class DataURLHeaders : IHttpHeaders {

		string urlString;
		string contentType;

		public DataURLHeaders(string urlString, long length, string contentType){
			this.urlString=urlString;
			this.contentType=contentType;
		}

		public string getRequestMethod() {
			return "GET";
		}

		public string getHeaderField(string name) {
			if(name==null)return "HTTP/1.1 200 OK";
			if("content-type".Equals(StringUtility.toLowerCaseAscii(name)))
				return contentType;
			return null;
		}

		public string getHeaderField(int name) {
			if(name==0)
				return getHeaderField(null);
			if(name==1)
				return getHeaderField("content-type");
			return null;
		}

		public string getHeaderFieldKey(int name) {
			if(name==0)
				return null;
			if(name==1)
				return "content-type";
			return null;
		}



		public int getResponseCode() {
			return 200;
		}

		public long getHeaderFieldDate(string field, long defaultValue) {
			return defaultValue;
		}

		private IList<string> asReadOnlyList(string[] a){
			return PeterO.Support.Collections.UnmodifiableList((a));
		}

		public IDictionary<string, IList<string>> getHeaderFields() {
			IDictionary<string, IList<string>> map=new PeterO.Support.LenientDictionary<string, IList<string>>();
			map.Add(null,asReadOnlyList(new string[]{getHeaderField(null)}));
			map.Add("content-type",asReadOnlyList(new string[]{getHeaderField("content-type")}));
			return PeterO.Support.Collections.UnmodifiableMap(map);
		}

		public string getUrl() {
			return urlString;
		}
	}

	private static string getDataURLContentType(string data){
		int index=HeaderParser.skipContentType(data, 0);
		string ctype=null;
		if(index==0)
			return "text/plain;charset=US-ASCII";
		else if(data[0]==';'){
			ctype="text/plain"+data.Substring(0,(index)-(0));
		} else {
			ctype=data.Substring(0,(index)-(0));
		}
		if(ctype.IndexOf('%')<0)
			return ctype;
		ctype=HeaderParser.unescapeContentType(ctype,0);
		if(ctype==null || ctype.Length==0)
			return "text/plain;charset=US-ASCII";
		return ctype;
	}


	private static byte[] getDataURLBytes(string data){
		int index=HeaderParser.skipContentType(data, 0);
		if(com.upokecenter.util.StringUtility.startsWith(data,";base64,",index)){
			index+=8;
			try {
				data=data.Substring(index);
				return Convert.FromBase64String(data);
			} catch(IOException){
				return null;
			}
		} else if(index<data.Length && data[index]==','){
			index++;
			ByteList mos=new ByteList();
			int len=data.Length;
			for(int j=index;j<len;j++){
				int c=data[j];
				if(!StringUtility.isChar(c,"-_.!~*'()") &&
						!(c>='A' && c<='Z') &&
						!(c>='a' && c<='z') &&
						!(c>='0' && c<='9'))
					return null;
				if(c=='%'){
					if(index+2<len){
						int a=HeaderParser.toHexNumber(data[index+1]);
						int b=HeaderParser.toHexNumber(data[index+2]);
						if(a>=0 && b>=0){
							mos.append((byte) (a*16+b));
							index+=2;
							continue;
						}
					}
				}
				mos.append((byte) (c&0xFF));
			}
			return mos.toByteArray();
		} else
			return null;
	}



	/**
	 * 
	 * Connects to a URL to download data from that URL.
	 * 
	 * @param urlString a URL _string.  All schemes (protocols)
	 * supported by Java's URLConnection are supported.  Data
	 * URLs are also supported.
	 * @param callback an _object to call back on, particularly
	 * when the data is ready to be downloaded. Can be null.  If the
	 * _object also implements IDownloadEventListener, it will also
	 * have its onConnecting and onConnected methods called.
	 * @return the _object returned by the callback's processResponse
	 * method.
	 * @ if an I/O error occurs, particularly
	 * network errors.
	 * @ if urlString is null.
	 */
	public static T downloadUrl<T>(
			string urlString,
			 IResponseListener<T> callback
			) {
		return downloadUrl(urlString, callback, false);
	}

	/**
	 * 
	 * Connects to a URL to download data from that URL.
	 * 
	 * @param urlString a URL _string.  All schemes (protocols)
	 * supported by Java's URLConnection are supported.  Data
	 * URLs are also supported.
	 * @param callback an _object to call back on, particularly
	 * when the data is ready to be downloaded. Can be null.  If the
	 * _object also implements IDownloadEventListener, it will also
	 * have its onConnecting and onConnected methods called.
	 * @param handleErrorResponses if true, the processResponse method
	 * of the supplied callback _object
	 * will also be called if an error response is returned. In this
	 * case, the _stream_ argument of that method will contain the error
	 * response body, if any, or null otherwise. If false and
	 * an error response is received, an IOException may be thrown instead
	 * of calling the processResponse method.
	 * This parameter does not affect whether an exception is thrown
	 * if the connection fails.
	 * @return the _object returned by the callback's processResponse
	 * method.
	 * @ if an I/O error occurs, particularly
	 * network errors.
	 * @ if urlString is null.
	 */
	public static T downloadUrl<T>(
			string urlString,
			 IResponseListener<T> callback,
			bool handleErrorResponses
			) {
		if(urlString==null)throw new ArgumentNullException();
		 bool isEventHandler=(callback!=null && callback is IDownloadEventListener<T>);
		URL uri=null;
		if(isEventHandler && callback!=null) {
			((IDownloadEventListener<T>)callback).onConnecting(urlString);
		}
		uri=URL.parse(urlString);
		if(uri==null)
			throw new ArgumentException();
		//
		// About URIs
		//
		if("about".Equals(uri.getScheme())){
			string ssp=uri.getSchemeData();
			if(!"blank".Equals(ssp)){
				if(!handleErrorResponses)
					throw new IOException();
				if(isEventHandler && callback!=null) {
					((IDownloadEventListener<T>)callback).onConnected(urlString);
				}
				T ret=(callback==null) ? default(T) : callback.processResponse(urlString,null,
						new ErrorHeader(urlString,400,"Bad Request"));
				return ret;
			} else {
				string contentType="text/html;charset=utf-8";
				PeterO.Support.InputStream stream=null;
				try {
					stream = new PeterO.Support.ByteArrayInputStream(new byte[]{});
					if(isEventHandler && callback!=null) {
						((IDownloadEventListener<T>)callback).onConnected(urlString);
					}
					T ret=(callback==null) ? default(T) : callback.processResponse(urlString,stream,
							new DataURLHeaders(urlString,0,contentType));
					return ret;
				} finally {
					if(stream!=null){
						try {
							stream.Close();
						} catch(IOException){}
					}
				}
			}
		}
		//
		// Data URLs
		//
		if("data".Equals(uri.getScheme())){
			// NOTE: Only "GET" is allowed here
			string ssp=uri.getSchemeData();
			byte[] bytes=getDataURLBytes(ssp);
			if(bytes==null){
				if(!handleErrorResponses)
					throw new IOException();
				if(isEventHandler && callback!=null) {
					((IDownloadEventListener<T>)callback).onConnected(urlString);
				}
				T ret=(callback==null) ? default(T) : callback.processResponse(urlString,null,
						new ErrorHeader(urlString,400,"Bad Request"));
				return ret;
			} else {
				string contentType=getDataURLContentType(ssp);
				PeterO.Support.InputStream stream=null;
				try {
					stream = new PeterO.Support.BufferedInputStream(
							new PeterO.Support.ByteArrayInputStream(bytes),
							Math.Max(32,Math.Min(8192, bytes.Length)));
					if(isEventHandler && callback!=null) {
						((IDownloadEventListener<T>)callback).onConnected(urlString);
					}
					T ret=(callback==null) ? default(T) : callback.processResponse(urlString,stream,
							new DataURLHeaders(urlString,bytes.Length,contentType));
					return ret;
				} finally {
					if(stream!=null){
						try {
							stream.Close();
						} catch(IOException){}
					}
				}
			}
		}
		//
		// Other URLs
		//
		return PeterO.Support.DownloadHelperImpl.downloadUrl(urlString, callback, handleErrorResponses);
	}

}

}
