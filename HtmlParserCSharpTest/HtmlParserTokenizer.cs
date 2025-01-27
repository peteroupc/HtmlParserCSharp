using System;
using System.Collections.Generic;
using System.IO;
using Com.Upokecenter.Html;
using NUnit.Framework;
using PeterO;
using PeterO.Cbor;

namespace Test {
  [TestFixture]
  public class HtmlParserTokenizer {
    /// <param name='o'>The parameter <paramref name='o'/> is not
    /// documented yet.</param>
    /// <param name='key'>The parameter <paramref name='key'/> is not
    /// documented yet.</param>
    /// <param name='defValue'>The parameter <paramref name='defValue'/> is
    /// not documented yet.</param>
    /// <returns>The return value is not documented yet.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='o'/> is null.</exception>
    public static CBORObject VOD(
      CBORObject o,
      string key,
      CBORObject defValue) {
      if (o == null) {
        throw new ArgumentNullException(nameof(o));
      }
      return o.ContainsKey(key) ? o[key] : defValue;
    }

    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='expected'/> or <paramref name='actual'/> is null.</exception>
    /// <param name='expected'>Not documented yet.</param>
    /// <param name='actual'>Not documented yet.</param>
    public void CheckOutput(CBORObject expected, IList<string[]> actual) {
      string actualString = CBORObject.FromObject(actual).ToJSONString();
      if (expected == null) {
        throw new ArgumentNullException(nameof(expected));
      }
      string expectedString = expected.ToJSONString();
      string msg = expectedString + "\n" + actualString;
      if (actual == null) {
        throw new ArgumentNullException(nameof(actual));
      }
      Assert.AreEqual(expected.Count, actual.Count, msg);
      for (var i = 0; i < expected.Count; ++i) {
        CBORObject ei = expected[i];
        string[] ai = actual[i];
        Assert.AreEqual(ei[0].AsString(), ai[0], msg);
        if (ei[0].AsString().Equals("StartTag", StringComparison.Ordinal)) {
          Assert.AreEqual(ei[1].AsString(), ai[1], msg);
          int keys = (ai.Length % 2 == 1) ?
            ((ai.Length - 3) / 2) : ((ai.Length - 2) / 2);
          if (ei.Count == 4 && ei[3].IsTrue) {
            Assert.IsTrue(ai.Length % 2 == 1, msg);
            Assert.AreEqual("true", ai[ai.Length - 1], msg);
          }
          Assert.AreEqual(ei[2].Count, keys, msg);
          for (var j = 0; j < keys; ++j) {
            string key = ai[2 + (j * 2)];
            string value = ai[3 + (j * 2)];
            Assert.IsTrue(ei[2].ContainsKey(key));
            CBORObject eivalue = ei[2][key];
            if (eivalue.IsNull) {
              Assert.IsNull(value, msg);
            } else {
              Assert.AreEqual(eivalue.AsString(), value, msg);
            }
          }
        } else {
          Assert.AreEqual(ei.Count, ai.Length, msg);
          for (var j = 1; j < ei.Count; ++j) {
            if (ei[j].IsTrue) {
              Assert.AreEqual("true", ai[j], msg);
            } else if (ei[j].IsFalse) {
              Assert.AreEqual("false", ai[j], msg);
            } else if (ei[j].IsNull) {
              Assert.IsNull(ai[j], msg);
            } else {
              Assert.AreEqual(ei[j].AsString(), ai[j], msg);
            }
          }
        }
      }
    }

    [Test]
    public void SetUp() {
      foreach (string f in Directory.GetFiles(
        "../Debug",
        "*.test")) {
        using (
          var fs = new FileStream(
          f,
          FileMode.Open)) {
          Console.WriteLine(f);
          CBORObject o = CBORObject.ReadJSON(fs);
          o = o["tests"];
          if (o == null) {
            continue;
          }
          var initStates = CBORObject.NewArray().Add("Data state");
          foreach (CBORObject value in o.Values) {
            Console.WriteLine(value["description"].AsString());
            if (!VOD(value, "doubleEscaped", CBORObject.False).AsBoolean()) {
              string input = value["input"].AsString();
              CBORObject output = value["output"];
              CBORObject initState = VOD(value, "initialStates", initStates);
              CBORObject lastStartTag = VOD(value, "lastStartTag", null);
              foreach (CBORObject tstate in initState.Values) {
                IList<string[]> tokens = HtmlDocument.ParseTokens(
                    input,
                    tstate.AsString(),
                    lastStartTag == null ? null : lastStartTag.AsString());
                this.CheckOutput(output, tokens);
              }
            }
          }
        }
      }
    }
  }
}
