namespace com.upokecenter.net {
using System;

public interface IDownloadEventListener<T> : IResponseListener<T> {
	 void onConnecting(string url);
	 void onConnected(string url);
}

}
