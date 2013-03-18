namespace com.upokecenter.net {
using System;

using System.IO;

public interface IOnFinishedListener<T> {
	/**
	 * Processes the data on the UI thread after it's downloaded.
	 * @param url URL of the data.
	 * @param value Data processed by 'processResponse'.
	 * @param exception If this value is non-null, an error has occurred
	 * and this exception contains further information on the error,
	 * and 'value' will be null.
	 */
	 void onFinished(string url, T value, IOException exception);
}
}
