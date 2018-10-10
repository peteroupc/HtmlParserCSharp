using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using PeterO;

namespace HtmlParserCSharpTest {
  class MainClass {
    private static string Implode(string[] arr, int index, int endIndex,
      string delim) {
      var sb = new StringBuilder();
      var i = index;
      while (i < endIndex) {
        if (i > index) {
 sb.Append(delim);
}
        sb.Append(arr[i]);
        ++i;
      }
      return sb.ToString();
    }
    private static string[] GetTestData(byte[] bytes,
                    int offset, int endOffset) {
      string str = DataUtilities.GetUtf8String(
        bytes,
        offset,
        endOffset - offset,
        true);
      string[] strarray = str.Split('\n');
      var ret = new string[2];
      if (strarray.Length == 0) {
 return null;
}
      if (strarray[0].Equals("#data")) {
        var state = 0;
        var index = 1;
        var lastIndex = 1;
        while (true) {
          if (state == 0) {
            if (index >= strarray.Length) {
 return null;
}
            if (strarray[index].Equals("#errors")) {
              string data = Implode(strarray, lastIndex, index, "\n");
              ret[0] = data;
              state = 1;
            }
          } else if (state == 1) {
            if (index >= strarray.Length) {
              string data = Implode(strarray, lastIndex, index, "\n");
              ret[0] = data;
              return ret;
            }
            string si = strarray[index];
            if (si.Length > 0 && si[0] == '#') {
              string data = Implode(strarray, lastIndex, index, "\n");
              ret[0] = data;
              return ret;
            }
          }
          ++index;
        }
      }
      return null;
    }
    public static List<string[]> ReadTestFile(string filename) {
      byte[] bytes;
      bytes = File.ReadAllBytes(filename);
      int lastIndex = 0;
      var ret = new List<string[]>();
      string[] data;
      for (var i = 0; i < bytes.Length; ++i) {
        if (i + 1 < bytes.Length && bytes[i] == 0x0a && bytes[i + 1] == 0x0a) {
          data = GetTestData(bytes, lastIndex, i);
          if ((data) == null) {
 Assert.Fail();
 }
          ret.Add(data);
          lastIndex = i + 2;
          ++i;
        }
      }
      data = GetTestData(bytes, lastIndex, bytes.Length);
      if ((data) == null) {
 Assert.Fail();
 }
      ret.Add(data);
      return ret;
    }
    public static void Main(string[] args) {
      Console.WriteLine("Hello World!");
    }
  }
}
