using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Com.Upokecenter.Html;
using Com.Upokecenter.Html.Data;
using NUnit.Framework;
using PeterO;
using PeterO.Cbor;
using PeterO.Rdf;

namespace Test {
[TestFixture]
public class RdfTest {
[Test]
public void TestTurtle() {
   this.TestTurtleOne("[ a 66.841 ] . (526) a 77.355 .");
   this.TestTurtleOne("_:b0 a <a> . _:b1 a <b> .");
   this.TestTurtleOne("_:b0 a <a> . _:b1 a _:b0 .");
   this.TestTurtleOne("_:b1 a <a> . _:b2 a <b> .");
   this.TestTurtleOne("_:bu00e0 a <a> . _:b\u00e0 a <b> .");
   this.TestTurtleOne("_:bu0030 a <a> . _:b\u0030 a <b> .");
}
public void TestTurtleOne(string str) {
    ISet<RDFTriple> triples = null;
    try {
       triples = new TurtleParser(str, "https://example.com/").Parse();
    } catch (ParserException) {
       Assert.Fail();
    }
    var sb = new StringBuilder();
    foreach (var triple in triples) {
      sb.Append(triple);
      sb.Append("\n");
    }
    var triples2 = new TurtleParser(sb.ToString(),
  "https://example.com/").Parse();
    Assert.IsTrue(RDFHelper.AreIsomorphic(triples, triples2));
    var triples3 = new NTriplesParser(sb.ToString()).Parse();
    Assert.IsTrue(RDFHelper.AreIsomorphic(triples, triples3));
}
}
}
