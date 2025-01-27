using System;
using System.Collections.Generic;
using System.Text;
using Com.Upokecenter.Util;
using PeterO;
using PeterO.Rdf;

namespace Com.Upokecenter.Html.Data {
  /// <summary>Not documented yet.</summary>
  public class RDFa : IRDFParser {
    internal enum ChainingDirection {
      None,
      Forward,
      Reverse,
    }

    internal class EvalContext {
      public string ValueBaseURI {
        get;
        set;
      }

      public RDFTerm ValueParentSubject {
        get;
        set;
      }

      public RDFTerm ValueParentObject {
        get;
        set;
      }

      public string ValueLanguage {
        get;
        set;
      }

      public IDictionary<string, string> ValueIriMap {
        get;
        set;
      }

      public IList<IncompleteTriple> ValueIncompleteTriples {
        get;
        set;
      }

      public IDictionary<string, IList<RDFTerm >> ValueListMap {
        get;
        set;
      }

      public IDictionary<string, string> ValueTermMap {
        get;
        set;
      }

      public IDictionary<string, string> ValueNamespaces {
        get;
        set;
      }

      public string DefaultVocab {
        get;
        set;
      }

      public EvalContext Copy() {
        var ec = new EvalContext();
        ec.ValueBaseURI = this.ValueBaseURI;
        ec.ValueParentSubject = this.ValueParentSubject;
        ec.ValueParentObject = this.ValueParentObject;
        ec.ValueLanguage = this.ValueLanguage;
        ec.DefaultVocab = this.DefaultVocab;
        ec.ValueIncompleteTriples = new
        List<IncompleteTriple>(this.ValueIncompleteTriples);
        ec.ValueListMap = (this.ValueListMap == null) ? null : new
          Dictionary<string, IList<RDFTerm>>(this.ValueListMap);
        ec.ValueNamespaces = (this.ValueNamespaces == null) ? null : new
          Dictionary<string, string>(this.ValueNamespaces);
        ec.ValueTermMap = (this.ValueTermMap == null) ? null : new
          Dictionary<string, string>(this.ValueTermMap);
        return ec;
      }
    }

    internal class IncompleteTriple {
      public IList<RDFTerm> TripleList {
        get;
        set;
      }

      public RDFTerm ValuePredicate {
        get;
        set;
      }

      public ChainingDirection ValueDirection {
        get;
        set;
      }

      public override string ToString() {
        return "IncompleteTriple [this.TripleList=" +
          this.TripleList + ", ValuePredicate=" +
          this.ValuePredicate + ", this.ValueDirection=" +
          this.ValueDirection + "]";
      }
    }

    private static readonly string RDFA_DEFAULT_PREFIX =
      "http://www.w3.org/1999/xhtml/vocab#";

    private static string GetTextNodeText(INode node) {
      var builder = new StringBuilder();
      foreach (var child in node.GetChildNodes()) {
        if (child.GetNodeType() == NodeType.TEXT_NODE) {
          builder.Append(((IText)child).GetData());
        } else {
          builder.Append(GetTextNodeText(child));
        }
      }
      return builder.ToString();
    }

    private static bool IsHtmlElement(IElement element, string name) {
      return element != null &&
        "http://www.w3.org/1999/xhtml".Equals(element.GetNamespaceURI()) &&
        name.Equals(element.GetLocalName());
    }

    private static bool IsNCNameChar(int c) {
      return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
        c == '_' || c == '.' || c == '-' || (c >= '0' && c <= '9') ||
        c == 0xb7 || (c >= 0xc0 && c <= 0xd6) ||
        (c >= 0xd8 && c <= 0xf6) || (c >= 0xf8 && c <= 0x2ff) ||
        (c >= 0x300 && c <= 0x37d) || (c >= 0x37f && c <= 0x1fff) ||
        (c >= 0x200c && c <= 0x200d) || (c >= 0x203f && c <= 0x2040) ||
        (c >= 0x2070 && c <= 0x218f) || (c >= 0x2c00 && c <= 0x2fef) ||
        (c >= 0x3001 && c <= 0xd7ff) || (c >= 0xf900 && c <= 0xfdcf) ||
        (c >= 0xfdf0 && c <= 0xfffd) || (c >= 0x10000 && c <= 0xeffff);
    }

    private static bool IsNCNameStartChar(int c) {
      return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
        c == '_' || (c >= 0xc0 && c <= 0xd6) ||
        (c >= 0xd8 && c <= 0xf6) || (c >= 0xf8 && c <= 0x2ff) ||
        (c >= 0x370 && c <= 0x37d) || (c >= 0x37f && c <= 0x1fff) ||
        (c >= 0x200c && c <= 0x200d) || (c >= 0x2070 && c <= 0x218f) ||
        (c >= 0x2c00 && c <= 0x2fef) || (c >= 0x3001 && c <= 0xd7ff) ||
        (c >= 0xf900 && c <= 0xfdcf) || (c >= 0xfdf0 && c <= 0xfffd) ||
        (c >= 0x10000 && c <= 0xeffff);
    }

    private static bool IsTermChar(int c) {
      return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
        c == '_' || c == '.' || c == '-' || c == '/' || (c >= '0' && c <= '9'
        ) || c == 0xb7 || (c >= 0xc0 && c <= 0xd6) ||
        (c >= 0xd8 && c <= 0xf6) || (c >= 0xf8 && c <= 0x2ff) ||
        (c >= 0x300 && c <= 0x37d) || (c >= 0x37f && c <= 0x1fff) ||
        (c >= 0x200c && c <= 0x200d) || (c >= 0x203f && c <= 0x2040) ||
        (c >= 0x2070 && c <= 0x218f) || (c >= 0x2c00 && c <= 0x2fef) ||
        (c >= 0x3001 && c <= 0xd7ff) || (c >= 0xf900 && c <= 0xfdcf) ||
        (c >= 0xfdf0 && c <= 0xfffd) || (c >= 0x10000 && c <= 0xeffff);
    }

    private IRDFParser parser;

    private EvalContext context;

    private ISet<RDFTriple> outputGraph;

    private IDocument document;

    private static bool xhtml_rdfa11 = false;

    private static readonly RDFTerm RDFA_USES_VOCABULARY =
      RDFTerm.FromIRI("http://www.w3.org/ns/rdfa#usesVocabulary");

    private static readonly string
    RDF_XMLLITERAL = "http://www.w3.org/1999/02/22-rdf-syntax-ns#XMLLiteral";

    private static readonly string[] ValueEmptyStringArray = new string[0];

    private static int GetCuriePrefixLength(string s, int offset, int length) {
      if (s == null || length == 0) {
        return -1;
      }
      if (s[offset] == ':') {
        return 0;
      }
      if (!IsNCNameStartChar(s[offset])) {
        return -1;
      }
      int index = offset + 1;
      int valueSLength = offset + length;
      while (index < valueSLength) {
        // Get the next Unicode character
        int c = s[index];
        if ((c & 0xfc00) == 0xd800 && index + 1 < valueSLength &&
          (s[index + 1] & 0xfc00) == 0xdc00) {
          // Get the Unicode code point for the surrogate pair
          c = 0x10000 + ((c & 0x3ff) << 10) + (s[index + 1] & 0x3ff);
          ++index;
        } else if ((c & 0xf800) == 0xd800) {
          // error
          return -1;
        }
        if (c == ':') {
          return index - offset;
        } else if (!IsNCNameChar(c)) {
          return -1;
        }
        ++index;
      }
      return -1;
    }

    private static T GetueCaseInsensitive<T>(
      IDictionary<string, T> map,
      string key) {
      if (key == null) {
        return map[null];
      }
      key = DataUtilities.ToLowerCaseAscii(key);
      foreach (var k in map.Keys) {
        if (key.Equals(DataUtilities.ToLowerCaseAscii(k))) {
          return map[k];
        }
      }
      return default(T);
    }

    private static bool IsValidCurieReference(
      string s,
      int offset,
      int length) {
      return URIUtility.IsValidCurieReference(s, offset, length);
    }

    private static bool IsValidTerm(string s) {
      if (s == null || s.Length == 0) {
        return false;
      }
      if (!IsNCNameStartChar(s[0])) {
        return false;
      }
      var index = 1;
      int valueSLength = s.Length;
      while (index < valueSLength) {
        // Get the next Unicode character
        int c = s[index];
        if ((c & 0xfc00) == 0xd800 && index + 1 < valueSLength &&
          (s[index + 1] & 0xfc00) == 0xdc00) {
          // Get the Unicode code point for the surrogate pair
          c = 0x10000 + ((c & 0x3ff) << 10) + (s[index + 1] & 0x3ff);
          ++index;
        } else if ((c & 0xf800) == 0xd800) {
          // error
          return false;
        } else if (!IsTermChar(c)) {
          return false;
        }
        ++index;
      }
      return true;
    }

    private static string[] SplitPrefixList(string s) {
      if (s == null || s.Length == 0) {
        return ValueEmptyStringArray;
      }
      var index = 0;
      int valueSLength = s.Length;
      while (index < valueSLength) {
        char c = s[index];
        if (c != 0x09 && c != 0x0a && c != 0x0d && c != 0x20) {
          break;
        }
        ++index;
      }
      if (index == s.Length) {
        return ValueEmptyStringArray;
      }
      var prefix = new StringBuilder();
      var iri = new StringBuilder();
      var state = 0; // Before NCName state
      var strings = new List<string>();
      while (index < valueSLength) {
        // Get the next Unicode character
        int c = s[index];
        if ((c & 0xfc00) == 0xd800 && index + 1 < valueSLength &&
          (s[index + 1] & 0xfc00) == 0xdc00) {
          // Get the Unicode code point for the surrogate pair
          c = 0x10000 + ((c & 0x3ff) << 10) + (s[index + 1] & 0x3ff);
          ++index;
        } else if ((c & 0xf800) == 0xd800) {
          // error
          break;
        }
        if (state == 0) { // Before NCName
          if (c == 0x09 || c == 0x0a || c == 0x0d || c == 0x20) {
            // ignore whitespace
            ++index;
          } else if (IsNCNameStartChar(c)) {
            // start of NCName
            if (c <= 0xffff) {
              {
                prefix.Append((char)c);
              }
            } else if (c <= 0x10ffff) {
              prefix.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
              prefix.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
            }
            state = 1;
            ++index;
          } else {
            // error
            break;
          }
        } else if (state == 1) { // NCName
          if (c == ':') {
            state = 2;
            ++index;
          } else if (IsNCNameChar(c)) {
            // continuation of NCName
            if (c <= 0xffff) {
              {
                prefix.Append((char)c);
              }
            } else if (c <= 0x10ffff) {
              prefix.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
              prefix.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
            }
            ++index;
          } else {
            // error
            break;
          }
        } else if (state == 2) { // After NCName
          if (c == ' ') {
            state = 3;
            ++index;
          } else {
            // error
            break;
          }
        } else if (state == 3) { // Before IRI
          if (c == ' ') {
            ++index;
          } else {
            // start of IRI
            if (c <= 0xffff) {
              {
                iri.Append((char)c);
              }
            } else if (c <= 0x10ffff) {
              iri.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
              iri.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
            }
            state = 4;
            ++index;
          }
        } else if (state == 4) { // IRI
          if (c == 0x09 || c == 0x0a || c == 0x0d || c == 0x20) {
            string prefixString = DataUtilities.ToLowerCaseAscii(
                prefix.ToString());
            // add prefix only if it isn't empty;
            // empty prefixes will not have a mapping
            if (prefixString.Length > 0) {
              strings.Add(prefixString);
              strings.Add(iri.ToString());
            }
            prefix.Clear();
            iri.Clear();
            state = 0;
            ++index;
          } else {
            // continuation of IRI
            if (c <= 0xffff) {
              {
                iri.Append((char)c);
              }
            } else if (c <= 0x10ffff) {
              iri.Append((char)((((c - 0x10000) >> 10) & 0x3ff) | 0xd800));
              iri.Append((char)(((c - 0x10000) & 0x3ff) | 0xdc00));
            }
            ++index;
          }
        }
      }
      if (state == 4) {
        strings.Add(DataUtilities.ToLowerCaseAscii(prefix.ToString()));
        strings.Add(iri.ToString());
      }
      return strings.ToArray();
    }

    private int blankNode;
    private IDictionary<string, RDFTerm> bnodeLabels = new
    Dictionary<string, RDFTerm>();

    /// <param name='document'>The parameter <paramref name='document'/> is
    /// an IDocument object.</param>
    /// <summary>Initializes a new instance of the RDFa class.</summary>
    /// <summary>Initializes a new instance of the RDFa class.</summary>
    public RDFa(IDocument document) {
      this.document = document;
      this.parser = null;
      this.context = new EvalContext();
      this.context.DefaultVocab = null;
      this.context.ValueBaseURI = document.GetBaseURI();
      if (!URIUtility.HasScheme(this.context.ValueBaseURI)) {
        throw new ArgumentException("ValueBaseURI: " +
          this.context.ValueBaseURI);
      }
      this.context.ValueParentSubject = RDFTerm.FromIRI(
          this.context.ValueBaseURI);
      this.context.ValueParentObject = null;
      this.context.ValueNamespaces = new Dictionary<string, string>();
      this.context.ValueIriMap = new Dictionary<string, string>();
      this.context.ValueListMap = new Dictionary<string, IList<RDFTerm>>();
      this.context.ValueTermMap = new Dictionary<string, string>();
      this.context.ValueIncompleteTriples = new List<IncompleteTriple>();
      this.context.ValueLanguage = null;
      this.outputGraph = new HashSet<RDFTriple>();
      this.context.ValueTermMap.Add(
        "describedby",
        "http://www.w3.org/2007/05/powder-s#describedby");
      this.context.ValueTermMap.Add(
        "license",
        "http://www.w3.org/1999/xhtml/vocab#license");
      this.context.ValueTermMap.Add(
        "role",
        "http://www.w3.org/1999/xhtml/vocab#role");
      this.context.ValueIriMap.Add("cc", "https://creativecommons.org/ns#");
      this.context.ValueIriMap.Add("ctag", "http://commontag.org/ns#");
      this.context.ValueIriMap.Add("dc", "http://purl.org/dc/terms/");
      this.context.ValueIriMap.Add("dcterms", "http://purl.org/dc/terms/");
      this.context.ValueIriMap.Add("dc11", "http://purl.org/dc/elements/1.1/");
      this.context.ValueIriMap.Add("foaf", "http://xmlns.Com/foaf/0.1/");
      this.context.ValueIriMap.Add("gr", "http://purl.org/goodrelations/v1#");
      this.context.ValueIriMap.Add(
        "ical",
        "http://www.w3.org/2002/12/cal/icaltzd#");
      this.context.ValueIriMap.Add("og", "http://ogp.me/ns#");
      this.context.ValueIriMap.Add("schema", "http://schema.org/");
      this.context.ValueIriMap.Add("rev", "http://purl.org/stuff/rev#");
      this.context.ValueIriMap.Add("sioc", "http://rdfs.org/sioc/ns#");
      this.context.ValueIriMap.Add(
        "grddl",
        "http://www.w3.org/2003/g/Data-view#");
      this.context.ValueIriMap.Add("ma", "http://www.w3.org/ns/ma-ont#");
      this.context.ValueIriMap.Add("owl", "http://www.w3.org/2002/07/owl#");
      this.context.ValueIriMap.Add("prov", "http://www.w3.org/ns/prov#");
      this.context.ValueIriMap.Add(
        "rdf",
        "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
      this.context.ValueIriMap.Add("rdfa", "http://www.w3.org/ns/rdfa#");
      this.context.ValueIriMap.Add(
        "rdfs",
        "http://www.w3.org/2000/01/rdf-schema#");
      this.context.ValueIriMap.Add("rif", "http://www.w3.org/2007/rif#");
      this.context.ValueIriMap.Add("rr", "http://www.w3.org/ns/r2rml#");
      this.context.ValueIriMap.Add(
        "sd",
        "http://www.w3.org/ns/sparql-service-description#");
      this.context.ValueIriMap.Add(
        "skos",
        "http://www.w3.org/2004/02/skos/core#");
      this.context.ValueIriMap.Add(
        "skosxl",
        "http://www.w3.org/2008/05/skos-xl#");
      this.context.ValueIriMap.Add("v", "http://rdf.Data-vocabulary.org/#");
      this.context.ValueIriMap.Add("vcard",
        "http://www.w3.org/2006/vcard/ns#");
      this.context.ValueIriMap.Add("void", "http://rdfs.org/ns/void#");
      this.context.ValueIriMap.Add("wdr", "http://www.w3.org/2007/05/powder#");
      this.context.ValueIriMap.Add(
        "wdrs",
        "http://www.w3.org/2007/05/powder-s#");
      this.context.ValueIriMap.Add(
        "xhv",
        "http://www.w3.org/1999/xhtml/vocab#");
      this.context.ValueIriMap.Add(
        "xml",
        "http://www.w3.org/XML/1998/_namespace");
      this.context.ValueIriMap.Add("xsd", "http://www.w3.org/2001/XMLSchema#");
      IElement docElement = document.GetDocumentElement();
      if (docElement != null && IsHtmlElement(docElement, "Html")) {
        xhtml_rdfa11 = true;
        string version = docElement.GetAttribute("version");
        if (version != null && "XHTML+RDFa 1.1".Equals(version)) {
          xhtml_rdfa11 = true;
          var terms = new string[] {
            "alternate", "appendix", "cite",
            "bookmark", "chapter", "contents",
            "copyright", "first", "glossary",
            "help", "icon", "index", "last",
            "license", "meta", "next", "prev",
            "previous", "section", "start",
            "stylesheet", "subsection", "top",
            "up", "p3pv1",
          };
          foreach (var term in terms) {
            this.context.ValueTermMap.Add(
              term,
              "http://www.w3.org/1999/xhtml/vocab#" + term);
          }
        }
        if (version != null && "XHTML+RDFa 1.0".Equals(version)) {
          this.parser = new RDFa1(document);
        }
      }
      this.ExtraContext();
    }

    private void ExtraContext() {
      this.context.ValueIriMap.Add("bibo", "http://purl.org/ontology/bibo/");
      this.context.ValueIriMap.Add("dbp", "http://dbpedia.org/property/");
      this.context.ValueIriMap.Add("dbp-owl", "http://dbpedia.org/ontology/");
      this.context.ValueIriMap.Add("dbr", "http://dbpedia.org/resource/");
      this.context.ValueIriMap.Add("ex", "http://example.org/");
    }

    private RDFTerm GenerateBlankNode() {
      // Use "//" as the prefix; according to the CURIE syntax,
      // "//" can never begin a valid CURIE reference, so it can
      // be used to guarantee that generated blank nodes will never
      // conflict with those stated explicitly
      string blankNodeString = "//" +
        Convert.ToString(
          this.blankNode,
          System.Globalization.CultureInfo.InvariantCulture);
      ++this.blankNode;
      RDFTerm term = RDFTerm.FromBlankNode(blankNodeString);
      this.bnodeLabels.Add(blankNodeString, term);
      return term;
    }

    private string GetCurie(
      string attribute,
      int offset,
      int length,
      IDictionary<string, string> prefixMapping) {
      int refIndex = offset;
      int refLength = length;
      int prefix = GetCuriePrefixLength(attribute, refIndex, refLength);
      string prefixIri = null;
      if (prefix >= 0) {
        string prefixName = DataUtilities.ToLowerCaseAscii(
            attribute.Substring(
              refIndex,
              (refIndex + prefix) - (refIndex)));
        refIndex += prefix + 1;
        refLength -= prefix + 1;
        prefixIri = prefixMapping[prefixName];
        prefixIri =
          (prefix == 0) ? RDFA_DEFAULT_PREFIX : prefixMapping[prefixName];
        if (prefixIri == null || "_".Equals(prefixName)) {
          return null;
        }
      } else
        // RDFa doesn't define a mapping for an absent prefix
      {
        return null;
      }
      if (!IsValidCurieReference(attribute, refIndex, refLength)) {
        return null;
      }
      if (prefix >= 0) {
        return
          this.RelativeResolve(
            prefixIri + attribute.Substring(
              refIndex,
              (refIndex + refLength) - refIndex)).GetValue();
      } else {
        return null;
      }
    }

    private RDFTerm GetCurieOrBnode(
      string attribute,
      int offset,
      int length,
      IDictionary<string, string> prefixMapping) {
      int refIndex = offset;
      int refLength = length;
      int prefix = GetCuriePrefixLength(attribute, refIndex, refLength);
      string prefixIri = null;
      string prefixName = null;
      if (prefix >= 0) {
        string blank = "_";
        prefixName = DataUtilities.ToLowerCaseAscii(
            attribute.Substring(
              refIndex,
              (refIndex + prefix) - (refIndex)));
        refIndex += prefix + 1;
        refLength -= prefix + 1;
        prefixIri = (prefix == 0) ? RDFA_DEFAULT_PREFIX :
          prefixMapping[prefixName];
        if (prefixIri == null && !blank.Equals(prefixName)) {
          return null;
        }
      } else
        // RDFa doesn't define a mapping for an absent prefix
      {
        return null;
      }
      if (!IsValidCurieReference(attribute, refIndex, refLength)) {
        return null;
      }
      if (prefix >= 0) {
        if ("_".Equals(prefixName)) {
          #if DEBUG
          if (!(refIndex >= 0)) {
            throw new InvalidOperationException(attribute);
          }
          if (!(refIndex + refLength <= attribute.Length)) {
            throw new InvalidOperationException(attribute);
          }
          #endif
          if (refLength == 0) {
            // use an empty blank node: the CURIE syntax
            // allows an empty reference;
            // see the comment
            // in GenerateBlankNode for why "//" appears
            // at the beginning
            return this.GetdBlankNode("//empty");
          }
          return
            this.GetdBlankNode(
              attribute.Substring(
                refIndex,
                (refIndex + refLength) - refIndex));
        }
        if (!(refIndex >= 0)) {
          throw new InvalidOperationException(attribute);
        }
        if (!(refIndex + refLength <= attribute.Length)) {
          throw new InvalidOperationException(attribute);
        }
        return
          this.RelativeResolve(
            prefixIri + attribute.Substring(
              refIndex,
              (refIndex + refLength) - refIndex));
      } else {
        return null;
      }
    }

    private RDFTerm GetdBlankNode(string str) {
      RDFTerm term = RDFTerm.FromBlankNode(str);
      this.bnodeLabels.Add(str, term);
      return term;
    }

    private RDFTerm GetSafeCurieOrCurieOrIri(
      string attribute,
      IDictionary<string, string> prefixMapping) {
      if (attribute == null) {
        return null;
      }
      int lastIndex = attribute.Length - 1;
      if (attribute.Length >= 2 && attribute[0] == '[' && attribute[lastIndex]
        == ']') {
        RDFTerm curie = this.GetCurieOrBnode(
            attribute,
            1,
            attribute.Length - 2,
            prefixMapping);
        return curie;
      } else {
        RDFTerm curie = this.GetCurieOrBnode(
            attribute,
            0,
            attribute.Length,
            prefixMapping);
        if (curie == null) {
          // evaluate as IRI
          return this.RelativeResolve(attribute);
        }
        return curie;
      }
    }

    private string GetTermOrCurieOrAbsIri(
      string attribute,
      IDictionary<string, string> prefixMapping,
      IDictionary<string, string> termMapping,
      string DefaultVocab) {
      if (attribute == null) {
        return null;
      }
      if (IsValidTerm(attribute)) {
        if (DefaultVocab != null) {
          return this.RelativeResolve(DefaultVocab +
              attribute).GetValue();
        } else if (termMapping.ContainsKey(attribute)) {
          return termMapping[attribute];
        } else {
          string value = GetueCaseInsensitive(termMapping, attribute);
          return value;
        }
      }
      string curie = this.GetCurie(
          attribute,
          0,
          attribute.Length,
          prefixMapping);
      if (curie == null) {
        // evaluate as IRI if it's absolute
        if (URIUtility.HasScheme(attribute)) {
          // Console.WriteLine("has scheme: %s",attribute)
          return this.RelativeResolve(attribute).GetValue();
        }
        return null;
      }
      return curie;
    }

    /// <summary>Not documented yet.</summary>
    /// <returns>An ISet(RDFTriple) object.</returns>
    public ISet<RDFTriple> Parse() {
      if (this.parser != null) {
        return this.parser.Parse();
      }
      this.Process(this.document.GetDocumentElement(), true);
      RDFInternal.ReplaceBlankNodes(this.outputGraph, this.bnodeLabels);
      return this.outputGraph;
    }

    private void Process(IElement node, bool root) {
      IList<IncompleteTriple> incompleteTriplesLocal = new
      List<IncompleteTriple>();
      string localLanguage = this.context.ValueLanguage;
      RDFTerm newSubject = null;
      var skipElement = false;
      RDFTerm currentProperty = null;

      RDFTerm currentObject = null;
      RDFTerm typedResource = null;
      IDictionary<string, string> iriMapLocal =
        new Dictionary<string, string>(this.context.ValueIriMap);
      IDictionary<string, string> namespacesLocal =
        new Dictionary<string, string>(this.context.ValueNamespaces);
      IDictionary<string, IList<RDFTerm >> listMapLocal =
        this.context.ValueListMap;
      IDictionary<string, string> termMapLocal =
        new Dictionary<string, string>(this.context.ValueTermMap);
      string localDefaultVocab = this.context.DefaultVocab;
      string attr = null;
      // Console.WriteLine("cur parobj[%s]=%s"
      // , node.GetTagName(), context.ValueParentObject);
      // Console.WriteLine("_base=%s",context.ValueBaseURI);
      attr = node.GetAttribute("xml:base");
      if (attr != null) {
        this.context.ValueBaseURI = URIUtility.RelativeResolve(
            attr,
            this.context.ValueBaseURI);
      }
      // Support deprecated XML ValueNamespaces
      foreach (var attrib in node.GetAttributes()) {
        string name = DataUtilities.ToLowerCaseAscii(attrib.GetName());
        // Console.WriteLine(attrib);
        if (name.Equals("xmlns")) {
          // Console.WriteLine("xmlns %s",attrib.GetValue());
          iriMapLocal.Add(String.Empty, attrib.GetValue());
          namespacesLocal.Add(String.Empty, attrib.GetValue());
        } else if (name.StartsWith("xmlns:", StringComparison.Ordinal) &&
          name.Length > 6) {
          string prefix = name.Substring(6);
          // Console.WriteLine("xmlns %s %s",prefix,attrib.GetValue());
          if (!"_".Equals(prefix)) {
            iriMapLocal.Add(prefix, attrib.GetValue());
          }
          namespacesLocal.Add(prefix, attrib.GetValue());
        }
      }
      attr = node.GetAttribute("vocab");
      if (attr != null) {
        if (attr.Length == 0) {
          // set default vocabulary to null
          localDefaultVocab = null;
        } else {
          // set default vocabulary to vocab IRI
          RDFTerm defPrefix = this.RelativeResolve(attr);
          localDefaultVocab = defPrefix.GetValue();
          this.outputGraph.Add(new RDFTriple(
            RDFTerm.FromIRI(this.context.ValueBaseURI),
            RDFA_USES_VOCABULARY,
            defPrefix));
        }
      }

      attr = node.GetAttribute("prefix");
      if (attr != null) {
        string[] prefixList = SplitPrefixList(attr);
        for (int i = 0; i < prefixList.Length; i += 2) {
          // Add prefix and IRI to the map, unless the prefix
          // is "_"
          if (!"_".Equals(prefixList[i])) {
            iriMapLocal.Add(prefixList[i], prefixList[i + 1]);
          }
        }
      }
      attr = node.GetAttribute("lang");
      if (attr != null) {
        localLanguage = attr;
      }
      attr = node.GetAttribute("xml:lang");
      if (attr != null) {
        localLanguage = attr;
      }
      string rel = node.GetAttribute("rel");
      string rev = node.GetAttribute("rev");
      string property = node.GetAttribute("property");
      string content = node.GetAttribute("content");
      string datatype = node.GetAttribute("datatype");
      if (rel == null && rev == null) {
        // Step 5
        // Console.WriteLine("%s %s",property,node.GetTagName());
        if (property != null && content == null && datatype == null) {
          RDFTerm about = this.GetSafeCurieOrCurieOrIri(
              node.GetAttribute("about"),
              iriMapLocal);
          if (about != null) {
            newSubject = about;
          } else if (root) {
            newSubject = this.GetSafeCurieOrCurieOrIri(
                String.Empty,
                iriMapLocal);
          } else if (this.context.ValueParentObject != null) {
            newSubject = this.context.ValueParentObject;
          }
          string _typeof = node.GetAttribute("typeof");
          if (_typeof != null) {
            if (about != null) {
              typedResource = about;
            } else if (root) {
              typedResource = this.GetSafeCurieOrCurieOrIri(
                  String.Empty,
                  iriMapLocal);
            } else {
              RDFTerm resource = this.GetSafeCurieOrCurieOrIri(
                  node.GetAttribute("resource"),
                  iriMapLocal);
              resource = resource ?? this.RelativeResolve(node.GetAttribute(
                "href"));
              resource = resource ?? this.RelativeResolve(node.GetAttribute(
                "src"));
              // Console.WriteLine("resource=%s",resource);
              if ((resource == null || resource.GetKind() != RDFTerm.IRI) &&
                xhtml_rdfa11) {
                if (IsHtmlElement(node, "head") ||
                  IsHtmlElement(node, "body")) {
                  newSubject = this.context.ValueParentObject;
                }
              }
              typedResource = (resource == null) ? this.GenerateBlankNode() :
                resource;
              currentObject = typedResource;
            }
          }
        } else {
          RDFTerm resource = this.GetSafeCurieOrCurieOrIri(
              node.GetAttribute("about"),
              iriMapLocal);
          if (resource == null) {
            resource = this.GetSafeCurieOrCurieOrIri(
                node.GetAttribute("resource"),
                iriMapLocal);
            // Console.WriteLine("resource=%s %s %s",
            // node.GetAttribute("resource"),
            // resource, context.ValueParentObject);
          }
          resource = resource ?? this.RelativeResolve(node.GetAttribute(
            "href"));
          resource = resource ?? this.RelativeResolve(node.GetAttribute(
            "src"));
          if ((resource == null || resource.GetKind() != RDFTerm.IRI) &&
            xhtml_rdfa11) {
            if (IsHtmlElement(node, "head") ||
              IsHtmlElement(node, "body")) {
              resource = this.context.ValueParentObject;
            }
          }
          if (resource == null) {
            if (root) {
              newSubject = this.GetSafeCurieOrCurieOrIri(
                  String.Empty,
                  iriMapLocal);
            } else if (node.GetAttribute("typeof") != null) {
              newSubject = this.GenerateBlankNode();
            } else {
              if (this.context.ValueParentObject != null) {
                newSubject = this.context.ValueParentObject;
              }
              if (node.GetAttribute("property") == null) {
                skipElement = true;
              }
            }
          } else {
            newSubject = resource;
          }
          if (node.GetAttribute("typeof") != null) {
            typedResource = newSubject;
          }
        }
      } else {
        // Step 6
        RDFTerm about = this.GetSafeCurieOrCurieOrIri(
            node.GetAttribute("about"),
            iriMapLocal);
        if (about != null) {
          newSubject = about;
        }
        if (node.GetAttribute("typeof") != null) {
          typedResource = newSubject;
        }
        if (about == null) {
          if (root) {
            about = this.GetSafeCurieOrCurieOrIri(
                String.Empty,
                iriMapLocal);
          } else if (this.context.ValueParentObject != null) {
            newSubject = this.context.ValueParentObject;
          }
        }
        RDFTerm resource = this.GetSafeCurieOrCurieOrIri(
            node.GetAttribute("resource"),
            iriMapLocal);
        resource = resource ?? this.RelativeResolve(node.GetAttribute(
          "href"));
        resource = resource ?? this.RelativeResolve(node.GetAttribute(
          "src"));
        if ((resource == null || resource.GetKind() != RDFTerm.IRI) &&
          xhtml_rdfa11) {
          if (IsHtmlElement(node, "head") ||
            IsHtmlElement(node, "body")) {
            newSubject = this.context.ValueParentObject;
          }
        }
        if (resource == null && node.GetAttribute("typeof") != null &&
          node.GetAttribute("about") == null) {
          currentObject = this.GenerateBlankNode();
        } else if (resource != null) {
          currentObject = resource;
        }
        if (node.GetAttribute("typeof") != null &&
          node.GetAttribute("about") == null) {
          typedResource = currentObject;
        }
      }
      // Step 7
      if (typedResource != null) {
        string[] types = StringUtility.SplitAtSpTabCrLf(node.GetAttribute(
          "typeof"));
        foreach (var type in types) {
          string iri = this.GetTermOrCurieOrAbsIri(
              type,
              iriMapLocal,
              termMapLocal,
              localDefaultVocab);
          if (iri != null) {
            this.outputGraph.Add(new RDFTriple(
              typedResource,
              RDFTerm.A,
              RDFTerm.FromIRI(iri)));
          }
        }
      }
      // Step 8
      if (newSubject != null &&
        !newSubject.Equals(this.context.ValueParentObject)) {
        this.context.ValueListMap.Clear();
      }
      // Step 9
      if (currentObject != null) {
        string inlist = node.GetAttribute("inlist");
        if (inlist != null && rel != null) {
          string[] types = StringUtility.SplitAtSpTabCrLf(rel);
          foreach (var type in types) {
            string iri = this.GetTermOrCurieOrAbsIri(
                type,
                iriMapLocal,
                termMapLocal,
                localDefaultVocab);
            if (iri != null) {
              if (!listMapLocal.ContainsKey(iri)) {
                IList<RDFTerm> newList = new List<RDFTerm>();
                newList.Add(currentObject);
                listMapLocal.Add(iri, newList);
              } else {
                IList<RDFTerm> existingList = listMapLocal[iri];
                existingList.Add(currentObject);
              }
            }
          }
        } else {
          string[] types = StringUtility.SplitAtSpTabCrLf(rel);
          #if DEBUG
          if (!(newSubject != null)) {
            throw new InvalidOperationException(
              "doesn't satisfy newSubject!=null");
          }
          #endif
          foreach (var type in types) {
            string iri = this.GetTermOrCurieOrAbsIri(
                type,
                iriMapLocal,
                termMapLocal,
                localDefaultVocab);
            if (iri != null) {
              this.outputGraph.Add(new RDFTriple(
                newSubject,
                RDFTerm.FromIRI(iri),
                currentObject));
            }
          }
          types = StringUtility.SplitAtSpTabCrLf(rev);
          foreach (var type in types) {
            string iri = this.GetTermOrCurieOrAbsIri(
                type,
                iriMapLocal,
                termMapLocal,
                localDefaultVocab);
            if (iri != null) {
              this.outputGraph.Add(new RDFTriple(
                currentObject,
                RDFTerm.FromIRI(iri),
                newSubject));
            }
          }
        }
      } else {
        // Step 10
        string[] types = StringUtility.SplitAtSpTabCrLf(rel);
        bool inlist = node.GetAttribute("inlist") != null;
        var hasPredicates = false;
        // Defines predicates
        foreach (var type in types) {
          string iri = this.GetTermOrCurieOrAbsIri(
              type,
              iriMapLocal,
              termMapLocal,
              localDefaultVocab);
          if (iri != null) {
            if (!hasPredicates) {
              hasPredicates = true;
              currentObject = this.GenerateBlankNode();
            }
            var inc = new IncompleteTriple();
            if (inlist) {
              if (!listMapLocal.ContainsKey(iri)) {
                IList<RDFTerm> newList = new List<RDFTerm>();
                listMapLocal.Add(iri, newList);
                // NOTE: Should not be a Copy
                inc.TripleList = newList;
              } else {
                IList<RDFTerm> existingList = listMapLocal[iri];
                inc.TripleList = existingList;
              }
              inc.ValueDirection = ChainingDirection.None;
            } else {
              inc.ValuePredicate = RDFTerm.FromIRI(iri);
              inc.ValueDirection = ChainingDirection.Forward;
            }
            // Console.WriteLine(inc);
            incompleteTriplesLocal.Add(inc);
          }
        }
        types = StringUtility.SplitAtSpTabCrLf(rev);
        foreach (var type in types) {
          string iri = this.GetTermOrCurieOrAbsIri(
              type,
              iriMapLocal,
              termMapLocal,
              localDefaultVocab);
          if (iri != null) {
            if (!hasPredicates) {
              hasPredicates = true;
              currentObject = this.GenerateBlankNode();
            }
            var inc = new IncompleteTriple();
            inc.ValuePredicate = RDFTerm.FromIRI(iri);
            inc.ValueDirection = ChainingDirection.Reverse;
            incompleteTriplesLocal.Add(inc);
          }
        }
      }
      // Step 11
      string[] preds = StringUtility.SplitAtSpTabCrLf(property);
      string datatypeValue = this.GetTermOrCurieOrAbsIri(
          datatype,
          iriMapLocal,
          termMapLocal,
          localDefaultVocab);
      if (datatype != null && datatypeValue == null) {
        datatypeValue = String.Empty;
      }
      // Console.WriteLine("datatype=[%s] prop=%s vocab=%s",
      // datatype, property, localDefaultVocab);
      // Console.WriteLine("datatypeValue=[%s]",datatypeValue);
      foreach (var pred in preds) {
        string iri = this.GetTermOrCurieOrAbsIri(
            pred,
            iriMapLocal,
            termMapLocal,
            localDefaultVocab);
        if (iri != null) {
          // Console.WriteLine("iri=[%s]",iri);
          currentProperty = null;
          if (datatypeValue != null && datatypeValue.Length > 0 &&
            !datatypeValue.Equals(RDF_XMLLITERAL)) {
            string literal = content;
            literal = literal ?? GetTextNodeText(node);
            currentProperty = RDFTerm.FromTypedString(literal, datatypeValue);
          } else if (datatypeValue != null && datatypeValue.Length == 0) {
            string literal = content;
            literal = literal ?? GetTextNodeText(node);
            currentProperty = (!String.IsNullOrEmpty(localLanguage)) ?
              RDFTerm.FromLangString(literal, localLanguage) :
              RDFTerm.FromTypedString(literal);
          } else if (datatypeValue != null &&
            datatypeValue.Equals(RDF_XMLLITERAL)) {
            // XML literal
            try {
              string literal = ExclusiveCanonicalXML.Canonicalize(
                  node,
                  false,
                  namespacesLocal);
              currentProperty = RDFTerm.FromTypedString(literal,
                  datatypeValue);
            } catch (ArgumentException) {
              // failure to canonicalize
            }
          } else if (content != null) {
            string literal = content;
            currentProperty = (!String.IsNullOrEmpty(localLanguage)) ?
              RDFTerm.FromLangString(literal, localLanguage) :
              RDFTerm.FromTypedString(literal);
          } else if (rel == null && content == null && rev == null) {
            RDFTerm resource = this.GetSafeCurieOrCurieOrIri(
                node.GetAttribute("resource"),
                iriMapLocal);
            resource = resource ?? this.RelativeResolve(node.GetAttribute(
              "href"));
            resource = resource ?? this.RelativeResolve(node.GetAttribute(
              "src"));
            if (resource != null) {
              currentProperty = resource;
            }
          }
          if (currentProperty == null) {
            if (node.GetAttribute("typeof") != null &&
              node.GetAttribute("about") == null) {
              currentProperty = typedResource;
            } else {
              string literal = content;
              literal = literal ?? GetTextNodeText(node);
              currentProperty = (!String.IsNullOrEmpty(localLanguage)) ?
                RDFTerm.FromLangString(literal, localLanguage) :
                RDFTerm.FromTypedString(literal);
            }
          }
          // Console.WriteLine("curprop: %s",currentProperty);
          if (node.GetAttribute("inlist") != null) {
            if (!listMapLocal.ContainsKey(iri)) {
              IList<RDFTerm> newList = new List<RDFTerm>();
              newList.Add(currentProperty);
              listMapLocal.Add(iri, newList);
            } else {
              IList<RDFTerm> existingList = listMapLocal[iri];
              existingList.Add(currentProperty);
            }
          } else {
            #if DEBUG
            if (!(newSubject != null)) {
              throw new InvalidOperationException(
                "doesn't satisfy newSubject!=null");
            }
            #endif
            this.outputGraph.Add(new RDFTriple(
              newSubject,
              RDFTerm.FromIRI(iri),
              currentProperty));
          }
        }
      }
      // Step 12
      if (!skipElement && newSubject != null) {
        foreach (var triple in this.context.ValueIncompleteTriples) {
          if (triple.ValueDirection == ChainingDirection.None) {
            IList<RDFTerm> TripleList = triple.TripleList;
            TripleList.Add(newSubject);
          } else if (triple.ValueDirection == ChainingDirection.Forward) {
            this.outputGraph.Add(new RDFTriple(
              this.context.ValueParentSubject,
              triple.ValuePredicate,
              newSubject));
          } else {
            this.outputGraph.Add(new RDFTriple(
              newSubject,
              triple.ValuePredicate,
              this.context.ValueParentSubject));
          }
        }
      }
      // Step 13
      foreach (var childNode in node.GetChildNodes()) {
        IElement childElement;
        EvalContext oldContext = this.context;
        if (childNode is IElement) {
          childElement = (IElement)childNode;
          // Console.WriteLine("skip=%s vocab=%s local=%s",
          // skipElement, context.DefaultVocab,
          // localDefaultVocab);
          if (skipElement) {
            EvalContext ec = oldContext.Copy();
            ec.ValueLanguage = localLanguage;
            ec.ValueIriMap = iriMapLocal;
            this.context = ec;
            this.Process(childElement, false);
          } else {
            var ec = new EvalContext();
            ec.ValueBaseURI = oldContext.ValueBaseURI;
            ec.ValueNamespaces = namespacesLocal;
            ec.ValueIriMap = iriMapLocal;
            ec.ValueIncompleteTriples = incompleteTriplesLocal;
            ec.ValueListMap = listMapLocal;
            ec.ValueTermMap = termMapLocal;
            ec.ValueParentSubject = (newSubject == null) ?
              oldContext.ValueParentSubject : newSubject;
            ec.ValueParentObject = (currentObject == null) ? ((newSubject ==
              null) ? oldContext.ValueParentSubject : newSubject) :
              currentObject;
            ec.DefaultVocab = localDefaultVocab;
            ec.ValueLanguage = localLanguage;
            this.context = ec;
            this.Process(childElement, false);
          }
        }
        this.context = oldContext;
      }
      // Step 14
      foreach (var iri in listMapLocal.Keys) {
        if (!this.context.ValueListMap.ContainsKey(iri)) {
          IList<RDFTerm> TripleList = listMapLocal[iri];
          if (TripleList.Count == 0) {
            this.outputGraph.Add(new RDFTriple(
              newSubject == null ? newSubject : this.context.ValueParentSubject,
              RDFTerm.FromIRI(iri),
              RDFTerm.NIL));
          } else {
            RDFTerm bnode = this.GenerateBlankNode();
            this.outputGraph.Add(new RDFTriple(
              newSubject == null ? newSubject : this.context.ValueParentSubject,
              RDFTerm.FromIRI(iri),
              bnode));
            for (int i = 0; i < TripleList.Count; ++i) {
              RDFTerm nextBnode = (i == TripleList.Count - 1) ?
                this.GenerateBlankNode() : RDFTerm.NIL;
              this.outputGraph.Add(new RDFTriple(
                bnode,
                RDFTerm.FIRST,
                TripleList[i]));
              this.outputGraph.Add(new RDFTriple(
                bnode,
                RDFTerm.REST,
                nextBnode));
              bnode = nextBnode;
            }
          }
        }
      }
    }

    private RDFTerm RelativeResolve(string iri) {
      if (iri == null) {
        return null;
      }
      return (URIUtility.SplitIRI(iri) == null) ? null :
        RDFTerm.FromIRI(
          URIUtility.RelativeResolve(
            iri,
            this.context.ValueBaseURI));
    }
  }
}
