namespace com.upokecenter.rdf {
using System;

using System.IO;
using System.Collections.Generic;

public interface IRDFParser {
	 ISet<RDFTriple> parse() ;
}

}
