using System;
using System.Collections.Generic;
using System.Globalization;
using PeterO.Rdf;

namespace Com.Upokecenter.Html.Data {
  internal sealed class RDFInternal {
    private static TValue ValueOrDefault<TKey, TValue>(
      IDictionary<TKey, TValue> dict,
      TKey key,
      TValue defValue) {
      if (dict == null) {
        throw new ArgumentNullException(nameof(dict));
      }
      return dict.ContainsKey(key) ? dict[key] : defValue;
    }

    private static TValue ValueOrNull<TKey, TValue>(
      IDictionary<TKey, TValue> dict,
      TKey key) {
      return ValueOrDefault(dict, key, default(TValue));
    }

    /// <summary>Replaces certain blank nodes with blank nodes whose names
    /// meet the N-Triples requirements.</summary>
    /// <param name='triples'>A set of RDF triples.</param>
    /// <param name='bnodeLabels'>A mapping of blank node names already
    /// allocated. This method will modify this object as needed to
    /// allocate new blank nodes.</param>
    internal static void ReplaceBlankNodes(
      ISet<RDFTriple> triples,
      IDictionary<string, RDFTerm> bnodeLabels) {
      if (bnodeLabels.Count == 0) {
        return;
      }
      IDictionary<string, RDFTerm> newBlankNodes = new
      Dictionary<string, RDFTerm>();
      IList<RDFTriple[]> changedTriples = new List<RDFTriple[]>();
      var nodeindex = new int[] { 0 };
      foreach (var triple in triples) {
        var changed = false;
        RDFTerm subj = triple.GetSubject();
        if (subj.GetKind() == RDFTerm.BLANK) {
          string oldname = subj.GetValue();
          string newname = SuggestBlankNodeName(
              oldname,
              nodeindex,
              bnodeLabels);
          if (!newname.Equals(oldname)) {
            RDFTerm newNode = ValueOrDefault(newBlankNodes, oldname, null);
            if (newNode == null) {
              newNode = RDFTerm.FromBlankNode(newname);
              bnodeLabels[newname] = newNode;
              newBlankNodes[oldname] = newNode;
            }
            subj = newNode;
            changed = true;
          }
        }
        RDFTerm obj = triple.GetObject();
        if (obj.GetKind() == RDFTerm.BLANK) {
          string oldname = obj.GetValue();
          string newname = SuggestBlankNodeName(
              oldname,
              nodeindex,
              bnodeLabels);
          if (!newname.Equals(oldname)) {
            RDFTerm newNode = ValueOrDefault(newBlankNodes, oldname, null);
            if (newNode == null) {
              newNode = RDFTerm.FromBlankNode(newname);
              bnodeLabels[newname] = newNode;
              newBlankNodes[oldname] = newNode;
            }
            obj = newNode;
            changed = true;
          }
        }
        if (changed) {
          var newTriple = new RDFTriple[] {
            triple,
            new RDFTriple(subj, triple.GetPredicate(), obj),
          };
          changedTriples.Add(newTriple);
        }
      }
      foreach (var triple2 in changedTriples) {
        RDFTriple[] t2 = triple2;
        triples.Remove(t2[0]);
        triples.Add(t2[1]);
      }
    }

    public static string IntToString(int value) {
      string digits = "0123456789";
      if (value == Int32.MinValue) {
        return "-2147483648";
      }
      if (value == 0) {
        return "0";
      }
      bool neg = value < 0;
      var chars = new char[12];
      var count = 11;
      if (neg) {
        value = -value;
      }
      while (value > 43698) {
        int intdivvalue = value / 10;
        char digit = digits[(int)(value - (intdivvalue * 10))];
        chars[count--] = digit;
        value = intdivvalue;
      }
      while (value > 9) {
        int intdivvalue = (value * 26215) >> 18;
        char digit = digits[(int)(value - (intdivvalue * 10))];
        chars[count--] = digit;
        value = intdivvalue;
      }
      if (value != 0) {
        chars[count--] = digits[(int)value];
      }
      if (neg) {
        chars[count] = '-';
      } else {
        ++count;
      }
      return new String(chars, count, 12 - count);
    }

    private static string SuggestBlankNodeName(
      string node,
      int[] nodeindex,
      IDictionary<string, RDFTerm> bnodeLabels) {
      bool validnode = node.Length > 0;
      // Check if the blank node label is valid
      // under N-Triples
      for (int i = 0; i < node.Length; ++i) {
        int c = node[i];
        if (i == 0 && !((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))) {
          validnode = false;
          break;
        }
        if (i >= 0 && !((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ||
          (c >= 'a' && c <= 'z'))) {
          validnode = false;
          break;
        }
      }
      if (validnode) {
        return node;
      }
      while (true) {
        // Generate a new blank node label,
        // and ensure it's unique
        node = "b" + IntToString(nodeindex[0]);
        if (!bnodeLabels.ContainsKey(node)) {
          return node;
        }
        ++nodeindex[0];
      }
    }

    private RDFInternal() {
    }
  }
}
