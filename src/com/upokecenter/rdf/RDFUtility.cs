namespace com.upokecenter.rdf {
using System;

using System.Collections.Generic;

public sealed class RDFUtility {
	private RDFUtility(){}

	/**
	 * A lax comparer of RDF triples which doesn't compare
	 * blank node labels
	 * 
	 * @param a
	 * @param b
	 * 
	 */
	private static bool laxEqual(RDFTriple a, RDFTriple b){
		if(a==null)return (b==null);
		if(a.Equals(b))return true;
		if(a.getSubject().getKind()!=b.getSubject().getKind())
			return false;
		if(a.getObject().getKind()!=b.getObject().getKind())
			return false;
		if(!a.getPredicate().Equals(b.getPredicate()))
			return false;
		if(a.getSubject().getKind()!=RDFTerm.BLANK){
			if(!a.getSubject().Equals(b.getSubject()))
				return false;
		}
		if(a.getObject().getKind()!=RDFTerm.BLANK){
			if(!a.getObject().Equals(b.getObject()))
				return false;
		}
		return true;
	}

	public static bool areIsomorphic(ISet<RDFTriple> graph1, ISet<RDFTriple> graph2){
		if(graph1==null)return graph2==null;
		if(graph1.Equals(graph2))return true;
		// Graphs must have the same size to be isomorphic
		if(graph1.Count!=graph2.Count)return false;
		foreach(RDFTriple triple in graph1){
			// do a strict comparison
			if(triple.getSubject().getKind()!=RDFTerm.BLANK &&
					triple.getObject().getKind()!=RDFTerm.BLANK){
				if(!graph2.Contains(triple))
					return false;
			} else {
				// do a lax comparison
				bool found=false;
				foreach(RDFTriple triple2 in graph2){
					if(laxEqual(triple,triple2)){
						found=true;
						break;
					}
				}
				if(!found)return false;
			}
		}
		return true;
	}
}

}
