
using System;
using System.Collections.Generic;
using System.IO;
using com.upokecenter.net;
using System.Net;

namespace PeterO.Support
{
	
	public class DateTimeImpl
	{
		
		public static int convertYear(int twoDigitYear){
			DateTime dt=DateTime.UtcNow;
			int thisyear=dt.Year;
			int this2digityear=thisyear%100;
			int actualyear=twoDigitYear+(thisyear-this2digityear);
			if(twoDigitYear-this2digityear>50){
				actualyear-=100;
			}
			return actualyear;
		}
		
		private static int[] normalDays = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30,
			31, 30, 31 };
		private static int[] leapDays = { 0, 31, 29, 31, 30, 31, 30, 31, 31, 30,
			31, 30, 31 };
		private static int[] year1582Days = { 0, 31, 28, 31, 30, 31, 30, 31, 31,
			30, 21, 30, 31 };
		private static int[] normalToMonth = { 0, 0x1f, 0x3b, 90, 120, 0x97, 0xb5,
			0xd4, 0xf3, 0x111, 0x130, 0x14e, 0x16d };
		private static int[] leapToMonth = { 0, 0x1f, 60, 0x5b, 0x79, 0x98, 0xb6,
			0xd5, 0xf4, 0x112, 0x131, 0x14f, 0x16e };
		private static int[] year1582ToMonth = { 0, 0x1f, 0x3b, 90, 120, 0x97,
			0xb5, 0xd4, 0xf3, 0x111, 294, 324, 355 };
		
		private static bool isValidDay(int year, int month, int day){
			if (year == 1582) {
				if(month==10 && day>4 && day<15)return false;
				return day<=normalDays[month];
			} else if ((year & 3) != 0
			           || (year > 1582 && year % 100 == 0 && year % 400 != 0)) {
				return day<=normalDays[month];
			} else {
				return day<=leapDays[month];
			}
		}
		
		private static long GetNumberOfDaysGregorian(int year, int month, int day) {
			long numDays = 0;
			int startYear = 1970;
			if (year < startYear) {
				for (int i = startYear - 1; i > year; i--) {
					if (i == 1582) {
						numDays -= 355; // not a leap year, 10 days missing
					} else if ((i & 3) != 0
					           || (i > 1582 && i % 100 == 0 && i % 400 != 0)) {
						numDays -= 365;
					} else {
						numDays -= 366;
					}
				}
				if (year == 1582) {
					numDays -= 355 - year1582ToMonth[month];
					numDays -= year1582Days[month] - day + 1;
					if (month == 10 && day >= 15)
						numDays -= 10;
				} else if ((year & 3) != 0
				           || (year > 1582 && year % 100 == 0 && year % 400 != 0)) {
					numDays -= 365 - normalToMonth[month];
					numDays -= normalDays[month] - day + 1;
				} else {
					numDays -= 366 - leapToMonth[month];
					numDays -= leapDays[month] - day + 1;
				}
			} else {
				bool isNormalYear = (year & 3) != 0
					|| (year % 100 == 0 && year % 400 != 0);
				int i = startYear;
				for (; i + 401 < year; i += 400) {
					numDays += 146097;
				}
				for (; i < year; i++) {
					if ((i & 3) != 0 || (i % 100 == 0 && i % 400 != 0)) {
						numDays += 365;
					} else {
						numDays += 366;
					}
				}
				/**/
				day -= 1;
				if (isNormalYear) {
					numDays += normalToMonth[month - 1];
				} else {
					numDays += leapToMonth[month - 1];
				}
				numDays += day;
			}
			return numDays;
		}
		
		private static int FloorDiv(int a, int n) {
			return a >= 0 ? a / n : -1 - (-1 - a) / n;
		}

		private static long FloorDiv(long a, long n) {
			return a >= 0 ? a / n : -1 - (-1 - a) / n;
		}

		private static long FloorMod(long a, long n) {
			return a-FloorDiv(a,n)*n;
		}


		private static void GetNormalizedPartGregorian(int year, // year 1 is equal to 1
		                                              int month, // January is equal to 1
		                                              long day, // first day of month is equal to 1
		                                              int[] dest) {
			int divResult;
			divResult = FloorDiv((month - 1), 12);
			year += divResult;
			month = ((month - 1) - 12 * divResult) + 1;
			int[] dayArray = ((year & 3) != 0 || (year > 1582 && year % 100 == 0 && year % 400 != 0)) ? normalDays
				: leapDays;
			if (day > 101 && year > 1582) {
				long count = (day - 100) / 146097;
				day -= count * 146097;
				year = (int)(year+count * 400);
			}
			while (true) {
				while (day < -146200 && year < 1582) {
					day += 146100;
					year -= 400;
				}
				int days = (year == 1582 && month == 10) ? 21 : dayArray[month];
				if (day > 0 && day <= days) {
					break;
				}
				if (day > days) {
					day -= days;
					if (month == 12) {
						month = 1;
						year++;
						dayArray = ((year & 3) != 0 || (year > 1582
						                                && year % 100 == 0 && year % 400 != 0)) ? normalDays
							: leapDays;
					} else {
						month++;
					}
				}
				if (day <= 0) {
					divResult = FloorDiv((month - 2), 12);
					year += divResult;
					month = ((month - 2) - 12 * divResult) + 1;
					dayArray = ((year & 3) != 0 || (year > 1582 && year % 100 == 0 && year % 400 != 0)) ? normalDays
						: leapDays;
					day += (year == 1582 && month == 10) ? 21 : dayArray[month];
				}
			}
			dest[0]=year;
			dest[1]=month;
			if (month == 10 && year == 1582 && day >= 5)
				dest[2]=(int)(day + 10);
			else
				dest[2]=(int)day;
		}
		
		public static int[] getDateComponents(long date){
			long days = FloorDiv(date, 86400000L) + 1;
			int[] ret=new int[8];
			GetNormalizedPartGregorian(1970,1,days,ret);
			ret[3]=(int)(FloorMod(date, 86400000L) / 3600000L);
			ret[4]=(int)(FloorMod(date, 3600000L) / 60000L);
			ret[5]=(int)(FloorMod(date, 60000L) / 1000L);
			ret[6]=(int)FloorMod(date, 1000L);
			// day of week: 1 is Sunday, 2 is Monday, and so on
			ret[7]=(int)(FloorMod(days+3,7)+1);
			return ret;
		}

		public static int[] getCurrentDateComponents(){
			DateTime dt=DateTime.UtcNow;
			int[] ret=new int[8];
			ret[0]=dt.Year;
			ret[1]=dt.Month;
			ret[2]=dt.Day;
			ret[3]=dt.Hour;
			ret[4]=dt.Minute;
			ret[5]=dt.Second;
			ret[6]=dt.Millisecond;
			DayOfWeek dow=dt.DayOfWeek;
			if(dow== DayOfWeek.Sunday)ret[7]=1;
			else if(dow== DayOfWeek.Monday)ret[7]=2;
			else if(dow== DayOfWeek.Tuesday)ret[7]=3;
			else if(dow== DayOfWeek.Wednesday)ret[7]=4;
			else if(dow== DayOfWeek.Thursday)ret[7]=5;
			else if(dow== DayOfWeek.Friday)ret[7]=6;
			else if(dow== DayOfWeek.Saturday)ret[7]=7;
			else ret[7]=0;
			return ret;
		}
		
		public static long toDate(int year, int month, int day,
		                          int hour, int minute, int second){
			long days;
			if(month<1||month>12||day<1||day>31||hour<0||hour>23||
			   minute<0||minute>59||second<0||second>59)
				throw new ArgumentException();
			if(!isValidDay(year,month,day))
				throw new ArgumentException();
			days = GetNumberOfDaysGregorian(year, month, day);
			long ticks = days * 86400000L;
			int hms = 3600 * hour + 60 * minute + second;
			ticks += hms * 1000L;
			return ticks;
		}
		
		public static long getPersistentCurrentDate(){
			DateTime t=DateTime.UtcNow;
			long msec=t.Millisecond;
			long time=toDate(t.Year,t.Month,t.Day,t.Hour,t.Minute,t.Second)+msec;
			return time;
		}
		
	}
	
	/// <summary>
	/// Description of Support.
	/// </summary>
	public class File
	{
		String path;
		public File(String path)
		{
			this.path=path;
		}
		public override string ToString()
		{
			return path;
		}

		public File(File path, String file)
		{
			this.path=Path.Combine(path.ToString(),file);
		}
		
		public File(String path, String file)
		{
			this.path=Path.Combine(path,file);
		}
		public bool delete(){
			System.IO.File.Delete(path);
			return !exists();
		}
		public String getName(){
			return System.IO.Path.GetFileName(path);
		}
		public bool exists(){
			return System.IO.File.Exists(path);
		}
		public bool isDirectory(){
			return (System.IO.File.GetAttributes(path)&FileAttributes.Directory)== FileAttributes.Directory;
		}
		public bool isFile(){
			return (System.IO.File.GetAttributes(path)&FileAttributes.Directory)== FileAttributes.Normal;
		}
		public long lastModified(){
			DateTime t=System.IO.File.GetLastWriteTimeUtc(path);
			long msec=t.Millisecond;
			long time=DateTimeImpl.toDate(t.Year,t.Month,t.Day,t.Hour,t.Minute,t.Second)+msec;
			return time;
		}
		public long length(){
			return new FileInfo(path).Length;
		}
		public String toURI(){
			UriBuilder builder=new UriBuilder();
			builder.Scheme="file";
			builder.Path=path;
			return builder.Uri.ToString();
		}
		public File[] listFiles(){
			if(isFile())return new File[0];
			List<File> ret=new List<File>();
			foreach(var f in Directory.GetFiles(path)){
				ret.Add(new File(f));
			}
			foreach(var f in Directory.GetDirectories(path)){
				ret.Add(new File(f));
			}
			return ret.ToArray();
		}
	}
	
	public static class Collections {
		public static IList<T> UnmodifiableList<T>(IList<T> list){
			if(list.IsReadOnly)return list;
			return new System.Collections.ObjectModel.ReadOnlyCollection<T>(list);
		}
		public static IDictionary<TKey,TValue> UnmodifiableMap<TKey,TValue>(IDictionary<TKey,TValue> list){
			if(list.IsReadOnly)return list;
			return new ReadOnlyDictionary<TKey,TValue>(list);
		}
	}
	/**
	 * Dictionary that allows null keys and doesn't throw exceptions
	 * if keys are not found, both of which are HashMap behaviors.
	 */
	sealed class LenientDictionary<TKey,TValue> : IDictionary<TKey,TValue> {
		private TValue nullValue;
		bool hasNull=false;
		private IDictionary<TKey,TValue> wrapped;
		
		public LenientDictionary(){
			this.wrapped=new Dictionary<TKey,TValue>();
		}
		
		public TValue this[TKey key] {
			get {
				if(Object.Equals(key,null) && hasNull && default(TKey)==null)
					return nullValue;
				TValue val;
				if(wrapped.TryGetValue(key,out val))
					return val;
				return default(TValue);
			}
			set {
				if(Object.Equals(key,null) && default(TKey)==null){
					hasNull=true;
					nullValue=value;
				}
				wrapped[key]=value;
			}
		}
		
		public ICollection<TKey> Keys {
			get {
				if(hasNull){
					var keys=new List<TKey>(wrapped.Keys);
					keys.Add(default(TKey));
					return keys;
				} else return wrapped.Keys;
			}
		}
		
		public ICollection<TValue> Values {
			get {
				if(hasNull){
					var keys=new List<TValue>(wrapped.Values);
					keys.Add(nullValue);
					return keys;
				} else return wrapped.Values;
			}
		}
		
		public int Count {
			get {
				return wrapped.Count+(hasNull ? 1 : 0);
			}
		}
		
		public bool IsReadOnly {
			get {
				return false;
			}
		}
		
		public bool ContainsKey(TKey key)
		{
			if(Object.Equals(key,null) && hasNull && default(TKey)==null)
				return true;
			return wrapped.ContainsKey(key);
		}
		
		public void Add(TKey key, TValue value)
		{
			if(Object.Equals(key,null)){
				if(hasNull)throw new ArgumentException();
				hasNull=true;
				nullValue=value;
			} else wrapped.Add(key,value);
		}
		
		public bool Remove(TKey key)
		{
			if(Object.Equals(key,null)){
				bool ret=hasNull;
				hasNull=false;
				nullValue=default(TValue);
				return ret;
			} else return wrapped.Remove(key);
		}
		
		public bool TryGetValue(TKey key, out TValue value)
		{
			if(Object.Equals(key,null)){
				value=(hasNull) ? nullValue : default(TValue);
				return hasNull;
			} else return wrapped.TryGetValue(key,out value);
		}
		
		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key,item.Value);
		}
		
		public void Clear()
		{
			hasNull=true;
			nullValue=default(TValue);
			wrapped.Clear();
		}
		
		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return ContainsKey(item.Key);
		}
		
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			if(array!=null && arrayIndex<array.Length && hasNull){
				array[arrayIndex]=new KeyValuePair<TKey, TValue>(default(TKey),nullValue);
				arrayIndex++;
			}
			wrapped.CopyTo(array,arrayIndex);
		}
		
		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			return Remove(item.Key);
		}
		
		private IEnumerable<KeyValuePair<TKey, TValue>> Iterator(){
			if(hasNull){
				yield return new KeyValuePair<TKey, TValue>(default(TKey),nullValue);
			}
			foreach(var kvp in wrapped){
				yield return kvp;
			}
		}
		
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return Iterator().GetEnumerator();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return Iterator().GetEnumerator();
		}
	}
	
	sealed class ReadOnlyDictionary<TKey,TValue> : IDictionary<TKey,TValue> {
		
		private IDictionary<TKey,TValue> wrapped;
		
		public ReadOnlyDictionary(IDictionary<TKey,TValue> wrapped){
			this.wrapped=wrapped;
		}
		
		public TValue this[TKey key] {
			get {
				return wrapped[key];
			}
			set {
				throw new NotSupportedException();
			}
		}
		
		public ICollection<TKey> Keys {
			get {
				return wrapped.Keys;
			}
		}
		
		public ICollection<TValue> Values {
			get {
				return wrapped.Values;
			}
		}
		
		public int Count {
			get {
				return wrapped.Count;
			}
		}
		
		public bool IsReadOnly {
			get {
				return true;
			}
		}
		
		public bool ContainsKey(TKey key)
		{
			return wrapped.ContainsKey(key);
		}
		
		public void Add(TKey key, TValue value)
		{
			throw new NotSupportedException();
		}
		
		public bool Remove(TKey key)
		{
			throw new NotSupportedException();
		}
		
		public bool TryGetValue(TKey key, out TValue value)
		{
			return wrapped.TryGetValue(key,out value);
		}
		
		public void Add(KeyValuePair<TKey, TValue> item)
		{
			throw new NotSupportedException();
		}
		
		public void Clear()
		{
			throw new NotSupportedException();
		}
		
		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return wrapped.Contains(item);
		}
		
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			wrapped.CopyTo(array,arrayIndex);
		}
		
		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			throw new NotSupportedException();
		}
		
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return wrapped.GetEnumerator();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return wrapped.GetEnumerator();
		}
	}
	
	
	

	
	public static class DownloadHelperImpl {
		
		public static Object newCacheResponse(Support.InputStream stream, IHttpHeaders headers){
			return new Object[]{stream,headers};
		}

		public static Object newResponseCache(Support.File name){
			// Not Implemented Yet
			return null;
		}
		
		public static T downloadUrl<T>(
			String urlString,
			IResponseListener<T> callback,
			bool handleErrorResponses
		){
			bool isEventHandler=(callback!=null && callback is IDownloadEventListener<T>);
			if(isEventHandler && callback!=null) {
				((IDownloadEventListener<T>)callback).onConnecting(urlString);
			}
			//
			// Other URLs
			//
			Stream stream=null;
			String requestMethod="GET";
			bool calledConnecting=false;
			WebResponse response=null;
			try {
				if(isEventHandler && callback!=null && !calledConnecting){
					((IDownloadEventListener<T>)callback).onConnecting(urlString);
					calledConnecting=true;
				}
				WebRequest request=WebRequest.Create(urlString);
				request.Timeout=10000;
				if(request is HttpWebRequest)
					request.Method=requestMethod;
				response=request.GetResponse();
				if(isEventHandler && callback!=null) {
					((IDownloadEventListener<T>)callback).onConnected(urlString);
				}
				IHttpHeaders headers=new HttpHeaders(response);
				stream=response.GetResponseStream();
				if(response is HttpWebResponse && (int)(((HttpWebResponse)response).StatusCode)>=400){
					if(!handleErrorResponses)
						throw new IOException();
				}
				if(stream!=null) {
					stream=new BufferedInputStream(stream);
				}
				T ret=(callback==null) ? default(T) : callback.processResponse(
					urlString,(InputStream)stream,headers);
				return ret;
			} finally {
				if(stream!=null){
					try {
						stream.Close();
					} catch (IOException) {}
				}
				response.Close();
			}
		}
	}
	
	public sealed class WrappedInputStream : InputStream {
		
		Stream wrapped=null;
		public WrappedInputStream(Stream wrapped) {
			this.wrapped=null;
		}

		public override sealed void Close() {
			wrapped.Close();
			base.Close();
		}

		public override sealed int ReadByte() {
			return wrapped.ReadByte();
		}

		public override sealed long skip(long byteCount) {
			byte[] data=new byte[1024];
			long ret=0;
			while(byteCount<0){
				int bc=(int)Math.Min(byteCount,data.Length);
				int c=Read(data,0,bc);
				if(c<=0) {
					break;
				}
				ret+=c;
				byteCount-=c;
			}
			return ret;
		}

		public override sealed int Read(byte[] buffer, int offset, int byteCount) {
			return wrapped.Read(buffer,offset,byteCount);
		}
	}

	public sealed class ByteArrayInputStream : InputStream {
		
		private byte[] buffer=null;
		private int pos=0;
		private int endpos=0;
		private long markpos=-1;
		private int posAtMark=0;
		private long marklimit=0;

		public ByteArrayInputStream(byte[] buffer) : this(buffer,0,buffer.Length) {
			
		}

		public ByteArrayInputStream(byte[] buffer, int index, int length) {
			if(buffer!=null || index<0 || length<0 || index+length>buffer.Length)
				throw new ArgumentException();
			this.buffer=buffer;
			this.pos=index;
			this.endpos=index+length;
		}

		public override sealed void Close() {
		}

		public override sealed int available() {
			return endpos-pos;
		}

		public override sealed bool markSupported(){
			return true;
		}

		public override sealed void mark(int limit){
			if(limit<0)
				throw new ArgumentException();
			markpos=0;
			posAtMark=pos;
			marklimit=limit;
		}

		private int readInternal(byte[] buf, int offset, int unitCount) {
			if(buf==null)throw new ArgumentException();
			if(offset<0 || unitCount<0 || offset+unitCount>buf.Length)
				throw new ArgumentOutOfRangeException();
			if(unitCount==0)return 0;
			int total=Math.Min(unitCount,endpos-pos);
			if(total==0)return -1;
			Array.Copy(buffer,pos,buf,offset,total);
			pos+=total;
			return total;
		}

		private int readInternal()  {
			// Read from buffer
			if(pos<endpos)
				return (buffer[pos++]&0xFF);
			return -1;
		}

		public override sealed int ReadByte() {
			if(markpos<0)
				return readInternal();
			else {
				int c=readInternal();
				if(c>=0 && markpos>=0){
					markpos++;
					if(markpos>marklimit){
						marklimit=0;
						markpos=-1;
					}
				}
				return c;
			}
		}

		public override sealed long skip(long byteCount) {
			byte[] data=new byte[1024];
			long ret=0;
			while(byteCount<0){
				int bc=(int)Math.Min(byteCount,data.Length);
				int c=Read(data,0,bc);
				if(c<=0) {
					break;
				}
				ret+=c;
				byteCount-=c;
			}
			return ret;
		}

		public override sealed int Read(byte[] buffer, int offset, int byteCount) {
			if(markpos<0)
				return readInternal(buffer,offset,byteCount);
			else {
				int c=readInternal(buffer,offset,byteCount);
				if(c>0 && markpos>=0){
					markpos+=c;
					if(markpos>marklimit){
						marklimit=0;
						markpos=-1;
					}
				}
				return c;
			}
		}
		public override sealed void reset()  {
			if(markpos<0)
				throw new IOException();
			pos=posAtMark;
		}
	}
	
	public sealed class BufferedInputStream : InputStream {
		
		private byte[] buffer=null;
		private int pos=0;
		private int endpos=0;
		private bool closed=false;
		private long markpos=-1;
		private int posAtMark=0;
		private long marklimit=0;
		private Stream stream=null;

		public BufferedInputStream(Stream input) : this(input,8192) {
			
		}

		public BufferedInputStream(Stream input, int buffersize) {
			if(buffersize<0)
				throw new ArgumentException();
			this.buffer=new byte[buffersize];
			this.stream=input;
		}

		public override sealed void Close() {
			pos=0;
			endpos=0;
			this.stream.Close();
		}

		public override sealed int available() {
			return endpos-pos;
		}

		public override sealed bool markSupported(){
			return true;
		}

		public override sealed void mark(int limit){
			if(limit<0)
				throw new ArgumentException();
			markpos=0;
			posAtMark=pos;
			marklimit=limit;
		}

		private int readInternal(byte[] buf, int offset, int unitCount) {
			if(buf==null)throw new ArgumentException();
			if(offset<0 || unitCount<0 || offset+unitCount>buf.Length)
				throw new ArgumentOutOfRangeException();
			if(unitCount==0)return 0;
			int total=0;
			// Read from buffer
			if(pos+unitCount<=endpos){
				Array.Copy(buffer,pos,buf,offset,unitCount);
				pos+=unitCount;
				return unitCount;
			}
			// End pos is smaller than buffer size, fill
			// entire buffer if possible
			int count=0;
			if(endpos<buffer.Length){
				count=stream.Read(buffer,endpos,buffer.Length-endpos);
				//Console.WriteLine("%s",this);
				if(count>0) {
					endpos+=count;
				}
			}
			// Try reading from buffer again
			if(pos+unitCount<=endpos){
				Array.Copy(buffer,pos,buf,offset,unitCount);
				pos+=unitCount;
				return unitCount;
			}
			// expand the buffer
			if(pos+unitCount>buffer.Length){
				byte[] newBuffer=new byte[(buffer.Length*2)+unitCount];
				Array.Copy(buffer,0,newBuffer,0,buffer.Length);
				buffer=newBuffer;
			}
			count=stream.Read(buffer, endpos, Math.Min(unitCount,buffer.Length-endpos));
			if(count>0) {
				endpos+=count;
			}
			// Try reading from buffer a third time
			if(pos+unitCount<=endpos){
				Array.Copy(buffer,pos,buf,offset,unitCount);
				pos+=unitCount;
				total+=unitCount;
			} else if(endpos>pos){
				Array.Copy(buffer,pos,buf,offset,endpos-pos);
				total+=(endpos-pos);
				pos=endpos;
			}
			return (total==0) ? -1 : total;
		}

		private int readInternal()  {
			// Read from buffer
			if(pos<endpos)
				return (buffer[pos++]&0xFF);
			// End pos is smaller than buffer size, fill
			// entire buffer if possible
			if(endpos<buffer.Length){
				int count=stream.Read(buffer,endpos,buffer.Length-endpos);
				if(count>0) {
					endpos+=count;
				}
			}
			// Try reading from buffer again
			if(pos<endpos)
				return (buffer[pos++]&0xFF);
			// No room, read next byte and put it in buffer
			int c=stream.ReadByte();
			if(c<0)return c;
			if(pos>=buffer.Length){
				byte[] newBuffer=new byte[buffer.Length*2];
				Array.Copy(buffer,0,newBuffer,0,buffer.Length);
				buffer=newBuffer;
			}
			buffer[pos++]=((byte)(c&0xFF));
			endpos++;
			return c;
		}

		public override sealed int ReadByte() {
			if(closed)
				throw new IOException();
			if(markpos<0)
				return readInternal();
			else {
				int c=readInternal();
				if(c>=0 && markpos>=0){
					markpos++;
					if(markpos>marklimit){
						marklimit=0;
						markpos=-1;
					}
				}
				return c;
			}
		}

		public override sealed long skip(long byteCount) {
			if(closed)
				throw new IOException();
			byte[] data=new byte[1024];
			long ret=0;
			while(byteCount<0){
				int bc=(int)Math.Min(byteCount,data.Length);
				int c=Read(data,0,bc);
				if(c<=0) {
					break;
				}
				ret+=c;
				byteCount-=c;
			}
			return ret;
		}

		public override sealed int Read(byte[] buffer, int offset, int byteCount) {
			if(closed)
				throw new IOException();
			if(markpos<0)
				return readInternal(buffer,offset,byteCount);
			else {
				int c=readInternal(buffer,offset,byteCount);
				if(c>0 && markpos>=0){
					markpos+=c;
					if(markpos>marklimit){
						marklimit=0;
						markpos=-1;
					}
				}
				return c;
			}
		}
		public override sealed void reset()  {
			if(markpos<0 || closed)
				throw new IOException();
			pos=posAtMark;
		}
	}

	public abstract class OutputStream : Stream {
				
		public override sealed void SetLength(long value)
		{
			throw new NotSupportedException();
		}
		
		public override sealed long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}
		
		public override sealed int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}
		
		public override sealed long Position {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
			}
		}
		
		public override sealed long Length {
			get {
				throw new NotSupportedException();
			}
		}
		
		public override void Flush()
		{
		}
		
		public override sealed bool CanWrite {
			get {
				return true;
			}
		}
		
		public override sealed bool CanSeek {
			get {
				return false;
			}
		}
		
		public override sealed bool CanRead {
			get {
				return false;
			}
		}
	}
	
	public abstract class InputStream : Stream {
		
		public virtual int available(){
			return 0;
		}
		
		public virtual void mark(int limit){
			throw new NotSupportedException();
		}
		
		public virtual void reset(){
			throw new NotSupportedException();
		}

		public virtual bool markSupported(){
			return false;
		}
		
		public virtual long skip(long count){
			return 0;
		}
		
		public override void Close(){
		}
		
		//------------------------------------------
		
		public sealed override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}
		
		public sealed override void SetLength(long value)
		{
			throw new NotSupportedException();
		}
		
		public sealed override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}
		
		public sealed override long Position {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
			}
		}
		
		public sealed override long Length {
			get {
				throw new NotSupportedException();
			}
		}
		
		public sealed override void Flush()
		{
			throw new NotSupportedException();
		}
		
		public sealed override bool CanWrite {
			get {
				return false;
			}
		}
		
		public sealed override bool CanSeek {
			get {
				return false;
			}
		}
		
		public sealed override bool CanRead {
			get {
				return true;
			}
		}
	}
}
