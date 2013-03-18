namespace com.upokecenter.util {
using System;

using System.IO;











public sealed class StreamUtility {
	private StreamUtility(){}

	public static void skipToEnd(PeterO.Support.InputStream stream){
		if(stream==null)return;
		while(true){
			byte[] x=new byte[1024];
			try {
				int c=stream.Read(x,0,x.Length);
				if(c<0) {
					break;
				}
			} catch(IOException){
				break; // maybe this stream is already closed
			}
		}
	}

	public static void copyStream(PeterO.Support.InputStream stream, Stream output)
			 {
		byte[] buffer=new byte[8192];
		while(true){
			int count=stream.Read(buffer,0,buffer.Length);
			if(count<0) {
				break;
			}
			output.Write(buffer,0,count);
		}
	}

	public static void inputStreamToFile(PeterO.Support.InputStream stream, PeterO.Support.File file)
			 {
		FileStream output=null;
		try {
			output=new FileStream((file).ToString(),FileMode.Create);
			copyStream(stream,output);
		} finally {
			if(output!=null) {
				output.Close();
			}
		}
	}

	public static string streamToString(PeterO.Support.InputStream stream)
			 {
		return streamToString("UTF-8",stream);
	}

	public static string streamToString(string charset, PeterO.Support.InputStream stream)
			 {
		TextReader reader = new StreamReader(stream,System.Text.Encoding.GetEncoding(charset));
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		char[] buffer = new char[4096];
		while(true){
			int count=reader.Read(buffer,0,(buffer).Length);
			if(count<0) {
				break;
			}
			builder.Append(buffer,0,count);
		}
		return builder.ToString();
	}


	public static void stringToStream(string s, Stream stream) {
		TextWriter writer=null;
		try {
			writer=new StreamWriter(stream);
			writer.Write(s);
		} finally {
			if(writer!=null) {
				writer.Close();
			}
		}
	}

	public static void stringToFile(string s, PeterO.Support.File file) {
		StreamWriter writer=null;
		try {
			writer=new StreamWriter(file.ToString());
			writer.Write(s);
		} finally {
			if(writer!=null) {
				writer.Close();
			}
		}
	}

	public static string fileToString(PeterO.Support.File file)
			 {
		StreamReader reader = new StreamReader(file.ToString());
		try {
			System.Text.StringBuilder builder=new System.Text.StringBuilder();
			char[] buffer = new char[4096];
			while(true){
				int count=reader.Read(buffer,0,(buffer).Length);
				if(count<0) {
					break;
				}
				builder.Append(buffer,0,count);
			}
			return builder.ToString();
		} finally {
			if(reader!=null) {
				reader.Close();
			}
		}
	}

}

}
