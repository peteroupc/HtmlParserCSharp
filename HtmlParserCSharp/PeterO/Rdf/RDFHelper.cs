using System;
using System.Collections.Generic;
using System.Text;

namespace PeterO.Rdf {
/// <summary>Not documented yet.</summary>
public static class RDFHelper {
    private static readonly RDFTerm CanonicalBlank = RDFTerm.FromBlankNode("_");

    private static string BlankValue(RDFTriple triple) {
        if (triple.GetSubject().GetKind() == RDFTerm.BLANK) {
          return triple.GetSubject().GetValue();
        }
        return (triple.GetObject().GetKind() == RDFTerm.BLANK) ?
               triple.GetObject().GetValue() : String.Empty;
    }

    private static bool IsSimpleBlank(RDFTriple triple) {
        // Has a blank subject or a blank object,
        // or has both with the same value
        RDFTerm rdfsubj = triple.GetSubject();
        RDFTerm rdfobj = triple.GetObject();
        bool subjBlank = rdfsubj.GetKind() == RDFTerm.BLANK;
        bool objBlank = rdfobj.GetKind() == RDFTerm.BLANK;
        return (subjBlank != objBlank) || (subjBlank && objBlank &&

                rdfsubj.GetValue().Equals(rdfobj.GetValue()));
    }

    private class TermComparer: IComparer<RDFTerm>
    {
        internal static int CompareRDFTerm(RDFTerm x, RDFTerm y) {
            if (x.GetKind() != y.GetKind()) {
              return x.GetKind() < y.GetKind() ? -1 : 1;
            }
            int cmp;
            cmp = String.Compare(
                      x.GetValue(),
                      y.GetValue(),
                      StringComparison.Ordinal);
            if (cmp != 0) {
              return cmp;
            }

            cmp = String.Compare(
                      x.GetTypeOrLanguage(),
                      y.GetTypeOrLanguage(),
                      StringComparison.Ordinal);
            return cmp;
        }
        public int Compare(RDFTerm x, RDFTerm y) {
            if (x == null) {
              if (y == null) {
                return 0;
              } else {
                    return -1;
                }
            } else {
                if (y == null) {
                  return 1;
                } else {
                    return CompareRDFTerm(x, y);
                }
            }
        }
    }

    private class TripleComparer: IComparer<RDFTriple>
    {
        public int Compare(RDFTriple x, RDFTriple y) {
            if (x == null) {
              if (y == null) {
                return 0;
              } else {
                    return -1;
                }
            } else {
                if (y == null) {
                  return 1;
                } else {
                    int cmp;
                    cmp = TermComparer.CompareRDFTerm(x.GetSubject(),
                                                      y.GetSubject());
                    if (cmp != 0) {
                      return cmp;
                    }
                    cmp = TermComparer.CompareRDFTerm(x.GetPredicate(),
                                                      y.GetPredicate());
                    if (cmp != 0) {
                      return cmp;
                    }
                    cmp = TermComparer.CompareRDFTerm(x.GetObject(),
                                                      y.GetObject());
                    return cmp;
                }
            }
        }
    }

    private class NonUniqueMapping {
        public int Index {
            get;
            set;
        }
        public List<RDFTerm> Mappings {
            get;
            private set;
        }
        public NonUniqueMapping() {
            this.Mappings = new List<RDFTerm>();
            this.Index = 0;
        }
        public NonUniqueMapping AddTerm(RDFTerm term) {
            this.Mappings.Add(term);
            return this;
        }
    }

    private class ListAndHash<T> {
        private readonly List<T> list;
        private int hash;
        private bool dirty;
        public ListAndHash() {
            this.list = new List<T>();
            this.hash = 0;
            this.dirty = true;
        }
        public void Add(T v) {
            this.list.Add(v);
            this.dirty = true;
        }
        public void Sort(IComparer<T> comparer) {
            this.list.Sort(comparer);
            this.dirty = true;
        }
        public override bool Equals(Object obj) {
            var other = obj as ListAndHash<T>;
            if (obj == null) {
              return false;
            }
            if (this.list.Count != other.list.Count) {
              return false;
            }
            for (var i = 0; i < this.list.Count; ++i) {
              if (!this.list[i].Equals(other.list[i])) {
                return false;
              }
            }
            return true;
        }
        public override int GetHashCode() {
            if (this.dirty) {
                this.hash = this.list.Count;
                for (var i = 0; i < this.list.Count; ++i) {
                    this.hash = unchecked(this.hash * 23);
                    this.hash = unchecked(this.hash +
                                          this.list[i].GetHashCode());
                }
                this.dirty = false;
            }
            return this.hash;
        }
    }

    private static int BlankNodeHash(
      RDFTerm term,
      IDictionary<RDFTerm, IList<RDFTriple>> triplesByTerm,
      IDictionary<RDFTerm, int> hashes) {
        var stack = new List<RDFTerm>();
        return BlankNodeHash(term, triplesByTerm, hashes, stack);
    }

    private static int BlankNodeHash(
      RDFTerm term,
      IDictionary<RDFTerm, IList<RDFTriple>> triplesByTerm,
      IDictionary<RDFTerm, int> hashes,
      IList<RDFTerm> stack) {
        if (term.GetKind() != RDFTerm.BLANK) {
          return term.GetHashCode();
        } else if (stack.Contains(term)) {
            // Avoid cycles
            return hashes[term];
        }
        // TODO: Rewrite to nonrecursive version
        stack.Add(term);
        // Console.WriteLine("" + stack.Count + " -> " + (term));
        int hash = unchecked((int)0xddff0001);
        int termHashCode = term.GetHashCode();
        IList<RDFTriple> triples = triplesByTerm[term];
        foreach (var triple in triples) {
            bool subjectBlank = triple.GetSubject().GetKind() == RDFTerm.BLANK;
            bool objectBlank = triple.GetObject().GetKind() == RDFTerm.BLANK;
            if ((subjectBlank && triple.GetSubject().Equals(term)) ||
                    (objectBlank && triple.GetObject().Equals(term))) {
                var h = 0;
                // Hashes are combined by sum for commutativity,
                // so they won't be sensitive to order of the triples.
                if (!subjectBlank || !triple.GetSubject().Equals(term)) {
                    h = unchecked(
                            h + 23 * BlankNodeHash(triple.GetSubject(),
                                                   triplesByTerm,
                                                   hashes,
                                                   stack));
                }
                h = unchecked(h + (29 * triple.GetPredicate().GetHashCode()));
                if (!objectBlank || !triple.GetObject().Equals(term)) {
                    h = unchecked(
                            h + 31 * BlankNodeHash(triple.GetObject(),
                                                   triplesByTerm,
                                                   hashes,
                                                   stack));
                }
                hash = unchecked(hash + h);
            }
        }
        stack.RemoveAt(stack.Count - 1);
        return hash;
    }

    private static RDFTriple CanonicalTriple(RDFTriple triple) {
        RDFTerm rdfsubj = triple.GetSubject();
        RDFTerm rdfobj = triple.GetObject();
        if (rdfsubj.GetKind() != RDFTerm.BLANK &&
                rdfobj.GetKind() != RDFTerm.BLANK) {
          return triple;
        }
        return new RDFTriple(
          rdfsubj.GetKind() == RDFTerm.BLANK ? CanonicalBlank : rdfsubj,
          triple.GetPredicate(),
          rdfobj.GetKind() == RDFTerm.BLANK ? CanonicalBlank : rdfobj);
    }

    private class IntAndTerm {
        public int Num {
            get;
            set;
        }
        public RDFTerm Term {
            get;
            set;
        }
    }

    /// <summary>Returns whether two RDF graphs are isomorphic; that is,
    /// they match apart from their blank node, and there is a one-to-one
    /// mapping from one graph's blank nodes to the other's such that the
    /// graphs match exactly when one graph's blank nodes are replaced with
    /// the other's.</summary>
    /// <returns>The return value is not documented yet.</returns>
    /// <param name='triples1'>Not documented yet.</param>
    /// <param name='triples2'>Not documented yet.</param>
    public static bool AreIsomorphic(
        ISet<RDFTriple> triples1,
        ISet<RDFTriple> triples2) {
        if (triples1.Count != triples2.Count) {
          return false;
        }
        var blanks1 = new List<RDFTriple>();
        var blanks2 = new List<RDFTriple>();
        var uniqueBlank1 = true;
        string blankName1 = null;
        var uniqueBlank2 = true;
        string blankName2 = null;
        foreach (var triple in triples1) {
            RDFTerm rdfsubj = triple.GetSubject();
            RDFTerm rdfobj = triple.GetObject();
            if (rdfsubj.GetKind() == RDFTerm.BLANK ||
                    rdfobj.GetKind() == RDFTerm.BLANK) {
              if (uniqueBlank1 && rdfsubj.GetKind() == RDFTerm.BLANK) {
                    if (blankName1 != null &&
                            !rdfsubj.GetValue().Equals(blankName1)) {
                      uniqueBlank1 = false;
                    } else {
                        blankName1 = rdfsubj.GetValue();
                    }
                }
                if (uniqueBlank1 && rdfobj.GetKind() == RDFTerm.BLANK) {
                  if (blankName1 != null &&
                            !rdfobj.GetValue().Equals(blankName1)) {
                    uniqueBlank1 = false;
                  } else {
                        blankName1 = rdfobj.GetValue();
                    }
                }
                blanks1.Add(triple);
            } else {
                if (!triples2.Contains(triple)) {
                  return false;
                }
            }
        }
        foreach (var triple in triples2) {
            RDFTerm rdfsubj = triple.GetSubject();
            RDFTerm rdfobj = triple.GetObject();
            if (rdfsubj.GetKind() == RDFTerm.BLANK ||
                    rdfobj.GetKind() == RDFTerm.BLANK) {
              if (uniqueBlank2 && rdfsubj.GetKind() == RDFTerm.BLANK) {
                    if (blankName2 != null &&
                            !rdfsubj.GetValue().Equals(blankName2)) {
                      uniqueBlank2 = false;
                    } else {
                        blankName2 = rdfsubj.GetValue();
                    }
                }
                if (uniqueBlank2 && rdfobj.GetKind() == RDFTerm.BLANK) {
                  if (blankName2 != null &&
                            !rdfobj.GetValue().Equals(blankName2)) {
                    uniqueBlank2 = false;
                  } else {
                        blankName2 = rdfobj.GetValue();
                    }
                }
                blanks2.Add(triple);
            } else {
                if (!triples1.Contains(triple)) {
                  return false;
                }
            }
        }
        if (blanks1.Count != blanks2.Count) {
          return false;
        }
        if (uniqueBlank1 != uniqueBlank2) {
          return false;
        }
        if (blanks1.Count == 0) {
// No RDF term has blanks
            return true;
        }
        if (blanks1.Count == 1) {
          RDFTriple blank1 = blanks1[0];
          RDFTriple blank2 = blanks2[0];
          if (!blank1.GetPredicate().Equals(blank2.GetPredicate())) {
            return false;
          }
          var subjectBlank = false;
          var objectBlank = false;
          if (blank1.GetSubject().GetKind() == RDFTerm.BLANK) {
            if (blank1.GetSubject().GetKind() != RDFTerm.BLANK) {
              return false;
            }
              subjectBlank = true;
            } else {
                if (!blank1.GetSubject().Equals(blank2.GetSubject())) {
                  return false;
                }
            }
            if (blank1.GetObject().GetKind() == RDFTerm.BLANK) {
              if (blank1.GetObject().GetKind() != RDFTerm.BLANK) {
                return false;
              }
              objectBlank = false;
            } else {
                if (!blank1.GetObject().Equals(blank2.GetObject())) {
                  return false;
                }
            }
            if (subjectBlank && objectBlank) {
                return blank1.GetSubject().Equals(blank1.GetObject()) ==
                       blank2.GetSubject().Equals(blank2.GetObject());
            } else {
                return true;
            }
        }
        if (uniqueBlank1) {
            // One unique blank node in each graph
            var blanks2Canonical = new HashSet<RDFTriple>();
            foreach (var blank2 in blanks2) {
              blanks2Canonical.Add(CanonicalTriple(blank2));
            }
            if (blanks2Canonical.Count != blanks2.Count) {
              throw new InvalidOperationException();
            }
            foreach (var blank1 in blanks1) {
              if (!blanks2Canonical.Contains(CanonicalTriple(blank1))) {
                return false;
              }
            }
            return true;
        }
        // Nontrivial cases: More than one triple with a blank node, and
        // more than one unique blank node.
        var simpleBlanks1 = new Dictionary<string, ListAndHash<RDFTriple>>();
        var simpleBlanks2 = new Dictionary<string, ListAndHash<RDFTriple>>();
        var complexBlankCount1 = 0;
        var complexBlankCount2 = 0;
        foreach (var blank2 in blanks2) {
          if (IsSimpleBlank(blank2)) {
                if (complexBlankCount1 == 0 && complexBlankCount2 == 0) {
                    string bv = BlankValue(blank2);
                    if (!simpleBlanks2.ContainsKey(bv)) {
                      simpleBlanks2[bv] = new ListAndHash<RDFTriple>();
                    }
                    simpleBlanks2[bv].Add(CanonicalTriple(blank2));
                }
            } else {
                // Complex blank
                ++complexBlankCount2;
            }
        }
        foreach (var blank1 in blanks1) {
          if (IsSimpleBlank(blank1)) {
                if (complexBlankCount1 == 0 && complexBlankCount2 == 0) {
                    string bv = BlankValue(blank1);
                    if (!simpleBlanks1.ContainsKey(bv)) {
                      simpleBlanks1[bv] = new ListAndHash<RDFTriple>();
                    }
                    simpleBlanks1[bv].Add(CanonicalTriple(blank1));
                }
            } else {
                // Complex blank
                ++complexBlankCount1;
            }
        }
        var comparer = new TripleComparer();
        var blank2To1 = new Dictionary<string, string>();
        if (complexBlankCount1 == 0 && complexBlankCount2 == 0) {
          if (simpleBlanks1.Count != simpleBlanks2.Count) {
            return false;
          }
          foreach (var k in simpleBlanks1.Keys) {
            simpleBlanks1[k].Sort(comparer);
          }
          foreach (var k in simpleBlanks2.Keys) {
                var sb = simpleBlanks2[k];
                sb.Sort(comparer);
                string foundKey = null;
                foreach (var k1 in simpleBlanks1.Keys) {
                  if (simpleBlanks1[k1].GetHashCode() == sb.GetHashCode() &&
                            simpleBlanks1[k1].Equals(sb)) {
                        foundKey = k1;
                        break;
                    }
                }
                if (foundKey != null) {
                  simpleBlanks1.Remove(foundKey);
                blank2To1[k] = foundKey;
                } else {
                    return false;
                }
            }
            return true;
        }
        if (complexBlankCount1 != complexBlankCount2) {
          return false;
        }
        // Implement the isomorphism check in Jeremy J. Carroll,
        // "Matching RDF Graphs", 2001.
        var triplesByTerm1 = new Dictionary<RDFTerm, IList<RDFTriple>>();
        var triplesByTerm2 = new Dictionary<RDFTerm, IList<RDFTriple>>();
        foreach (var blank1 in blanks1) {
            RDFTerm subject = blank1.GetSubject();
            RDFTerm rdfObject = blank1.GetObject();
            var hasTerm = false;
            if (subject.GetKind() == RDFTerm.BLANK) {
              if (!triplesByTerm1.ContainsKey(subject)) {
                    triplesByTerm1[subject] = new List<RDFTriple> { blank1 };
                } else {
                    triplesByTerm1[subject].Add(blank1);
                }
                hasTerm = subject.Equals(rdfObject);
            }
            if (rdfObject.GetKind() == RDFTerm.BLANK) {
              if (!triplesByTerm1.ContainsKey(rdfObject)) {
                    triplesByTerm1[rdfObject] = new List<RDFTriple> { blank1 };
                } else if (!hasTerm) {
                  triplesByTerm1[rdfObject].Add(blank1);
                }
            }
        }
        foreach (var blank2 in blanks2) {
            RDFTerm subject = blank2.GetSubject();
            RDFTerm rdfObject = blank2.GetObject();
            var hasTerm = false;
            if (subject.GetKind() == RDFTerm.BLANK) {
              if (!triplesByTerm2.ContainsKey(subject)) {
                    triplesByTerm2[subject] = new List<RDFTriple> { blank2 };
                } else {
                    triplesByTerm2[subject].Add(blank2);
                }
                hasTerm = subject.Equals(rdfObject);
            }
            if (rdfObject.GetKind() == RDFTerm.BLANK) {
              if (!triplesByTerm2.ContainsKey(rdfObject)) {
                    triplesByTerm2[rdfObject] = new List<RDFTriple> { blank2 };
                } else if (!hasTerm) {
                  triplesByTerm2[rdfObject].Add(blank2);
                }
            }
        }
        if (triplesByTerm1.Count != triplesByTerm2.Count) {
          return false;
        }
        var tc = new TermComparer();
        var blankTerms1Hashes = new Dictionary<RDFTerm, int>();
        var blankTerms2Hashes = new Dictionary<RDFTerm, int>();
        foreach (var b in triplesByTerm1.Keys) {
          blankTerms1Hashes[b] = triplesByTerm1[b].Count;
        }
        foreach (var b in triplesByTerm2.Keys) {
          blankTerms2Hashes[b] = triplesByTerm2[b].Count;
        }
        var blankTerms1Hashes2 = new Dictionary<RDFTerm, int>();
        var blankTerms2Hashes2 = new Dictionary<RDFTerm, int>();
        var hashClassCounts1 = new Dictionary<int, NonUniqueMapping>();
        var hashClassCounts2 = new Dictionary<int, NonUniqueMapping>();
        var maxClassSize1 = 0;
        var maxClassSize2 = 0;
        foreach (var b in triplesByTerm1.Keys) {
            int h = BlankNodeHash(b, triplesByTerm1, blankTerms1Hashes);
            blankTerms1Hashes2[b] = h;
            if (!hashClassCounts1.ContainsKey(h)) {
              hashClassCounts1[h] = new NonUniqueMapping().AddTerm(b);
            } else {
                hashClassCounts1[h].AddTerm(b);
            }
            maxClassSize1 = Math.Max(maxClassSize1,
                                     hashClassCounts1[h].Mappings.Count);
            // Console.WriteLine(String.Empty + b + "=" + h +":
            // "+DateTime.Now.Ticks/10000000.0);
        }
        foreach (var b in triplesByTerm2.Keys) {
            int h = BlankNodeHash(b, triplesByTerm2, blankTerms2Hashes);
            blankTerms2Hashes2[b] = h;
            // if (!hashClassCounts1.ContainsKey(h)) {
            // return false;
            // }
            if (!hashClassCounts2.ContainsKey(h)) {
              hashClassCounts2[h] = new NonUniqueMapping().AddTerm(b);
            } else {
                hashClassCounts2[h].AddTerm(b);
            }
            maxClassSize2 = Math.Max(maxClassSize2,
                                     hashClassCounts2[h].Mappings.Count);
            // Console.WriteLine(String.Empty + b + "=" + h +":
            // "+DateTime.Now.Ticks/10000000.0);
        }
        if (maxClassSize1 != maxClassSize2) {
          return false;
        }
        if (hashClassCounts1.Count != hashClassCounts2.Count) {
          return false;
        }
        {
            var uniqueMapping = new Dictionary<RDFTerm, RDFTerm>();
            var nonUniqueMappings1 = new List<NonUniqueMapping>();
            var nonUniqueMappings2 = new List<NonUniqueMapping>();
            foreach (int hash in hashClassCounts1.Keys) {
              if (!hashClassCounts2.ContainsKey(hash)) {
                return false;
              }
              var iat1 = hashClassCounts1[hash];
              var iat2 = hashClassCounts2[hash];
              if (iat1.Mappings.Count != iat2.Mappings.Count) {
                return false;
              }
              for (var i = 0; i < iat1.Mappings.Count; ++i) {
                    RDFTerm term1 = iat1.Mappings[i];
                    RDFTerm term2 = iat2.Mappings[i];
                    uniqueMapping[term1] = term2;
                }
                if (iat1.Mappings.Count > 1) {
                  nonUniqueMappings1.Add(iat1);
                nonUniqueMappings2.Add(iat2);
                }
            }
            while (true) {
                var failed = false;
                foreach (var blank in blanks1) {
                    RDFTerm rdfSubj = blank.GetSubject();
                    RDFTerm rdfObj = blank.GetObject();
                    if (rdfSubj.GetKind() == RDFTerm.BLANK) {
                      rdfSubj = uniqueMapping[rdfSubj];
                    }
                    if (rdfObj.GetKind() == RDFTerm.BLANK) {
                      rdfObj = uniqueMapping[rdfObj];
                    }
                    var triple = new RDFTriple(
                        rdfSubj,
                        blank.GetPredicate(),
                        rdfObj);
                    if (!blanks2.Contains(triple)) {
                        failed = true;
                        break;
                    }
                }
                if (failed) {
                    // TODO: Choose next mapping to try
                    throw new NotImplementedException();
                } else {
                    break;
                }
            }
            return true;
        }
    }
}
}
