using System;
using System.Collections.Generic;
using System.Text;
using Com.Upokecenter.Html;
using Com.Upokecenter.Util;
using PeterO;
using PeterO.Rdf;

namespace Com.Upokecenter.Html.Data {
  internal class RDFa1 : IRDFParser {
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

    private RDFa.EvalContext context;
    private ISet<RDFTriple> outputGraph;

    private IDocument document;

    private bool xhtml = false;

    private static readonly string
    RDF_XMLLITERAL = "http://www.w3.org/1999/02/22-rdf-syntax-ns#XMLLiteral";

    private static readonly string
    RDF_NAMESPACE = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

    private static IList<string> relterms = (new string[] {
      "alternate",
      "appendix", "cite", "bookmark", "chapter", "contents", "copyright",
      "first", "glossary", "help", "icon", "index", "last",
      "license", "meta", "next", "prev",
      "role", "section", "start",
      "stylesheet", "subsection", "top",
      "up", "p3pv1",
    });

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

    private static bool HasNonTextChildNodes(INode node) {
      foreach (var child in node.GetChildNodes()) {
        if (child.GetNodeType() != NodeType.TEXT_NODE) {
          return true;
        }
      }
      return false;
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

    private static bool IsValidCurieReference(
      string s,
      int offset,
      int length) {
      if (s == null) {
        return false;
      }
      if (length == 0) {
        return true;
      }
      int[]
      indexes = URIUtility.SplitIRI(
          s,
          offset,
          length,
          URIUtility.ParseMode.IRIStrict);
      if (indexes == null) {
        return false;
      }
      if (indexes[0] != -1) {
        // check if scheme component is present
        return false;
      }
      return true;
    }

    private int blankNode;

    private IDictionary<string, RDFTerm> bnodeLabels = new
    Dictionary<string, RDFTerm>();

    private static readonly string RDFA_DEFAULT_PREFIX =
      "http://www.w3.org/1999/xhtml/vocab#";

    public RDFa1(IDocument document) {
      this.document = document;
      this.context = new RDFa.EvalContext();
      this.context.ValueBaseURI = document.GetBaseURI();
      this.context.ValueNamespaces = new Dictionary<string, string>();
      if (!URIUtility.HasScheme(this.context.ValueBaseURI)) {
        throw new ArgumentException("baseURI: " + this.context.ValueBaseURI);
      }
      this.context.ValueParentSubject = RDFTerm.FromIRI(
          this.context.ValueBaseURI);
      this.context.ValueParentObject = null;
      this.context.ValueIriMap = new Dictionary<string, string>();
      this.context.ValueListMap = new Dictionary<string, IList<RDFTerm>>();
      this.context.ValueIncompleteTriples = new List<RDFa.IncompleteTriple>();
      this.context.ValueLanguage = null;
      this.outputGraph = new HashSet<RDFTriple>();
      if (IsHtmlElement(document.GetDocumentElement(), "Html")) {
        this.xhtml = true;
      }
    }

    private RDFTerm GenerateBlankNode() {
      // Use "b:" as the prefix; according to the CURIE syntax,
      // "b:" can never begin a valid CURIE reference (in RDFa 1.0,
      // the reference has the broader production irelative-refValue),
      // so it can
      // be used to guarantee that generated blank nodes will never
      // conflict with those stated explicitly
      string blankNodeString = "b:" + IntToString(this.blankNode);
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
      if (attribute == null) {
        return null;
      }
      int refIndex = offset;
      int refLength = length;
      int prefix = GetCuriePrefixLength(attribute, refIndex, refLength);
      string prefixIri = null;
      if (prefix >= 0) {
        string prefixName = DataUtilities.ToLowerCaseAscii(
            attribute.Substring(
              refIndex,
              (refIndex + prefix) - (refIndex))); refIndex += prefix + 1;
refLength -= prefix + 1; prefixIri = prefixMapping[prefixName]; prefixIri =
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
              (refIndex + refLength) - refIndex))
          .GetValue();
      } else {
        return null;
      }
    }

    private string GetCurie(
      string attribute,
      IDictionary<string, string> prefixMapping) {
      return (attribute == null) ? null :
        this.GetCurie(attribute, 0, attribute.Length, prefixMapping);
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
              (refIndex + prefix) - (refIndex))); refIndex += prefix + 1;
refLength -= prefix + 1; prefixIri = (prefix == 0) ? RDFA_DEFAULT_PREFIX :
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
            // in GenerateBlankNode for why "b:" appears
            // at the beginning
            return this.GetdBlankNode("b:empty");
          }
          return this.GetdBlankNode(
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

    private string GetRelTermOrCurie(
      string attribute,
      IDictionary<string, string> prefixMapping) {
      return relterms.Contains(DataUtilities.ToLowerCaseAscii(attribute)) ?
        ("http://www.w3.org/1999/xhtml/vocab#" +
          DataUtilities.ToLowerCaseAscii(attribute)) :
        this.GetCurie(attribute, prefixMapping);
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

    private void MiniRdfXml(IElement node, RDFa.EvalContext evalContext) {
      this.MiniRdfXml(node, evalContext, null);
    }

    // Processes a subset of RDF/XML metadata
    // Doesn't implement RDF/XML completely
    private void MiniRdfXml(
      IElement node,
      RDFa.EvalContext evalContext,
      RDFTerm subject) {
      string language = evalContext.ValueLanguage;
      foreach (var child in node.GetChildNodes()) {
        IElement childElement = (child is IElement) ?
          ((IElement)child) : null;
        if (childElement == null) {
          continue;
        }
        language = (node.GetAttribute("xml:lang") != null) ?
          node.GetAttribute("xml:lang") : evalContext.ValueLanguage;
        if (childElement.GetLocalName().Equals("Description") &&
          RDF_NAMESPACE.Equals(childElement.GetNamespaceURI())) {
          RDFTerm about = this.RelativeResolve(childElement.GetAttributeNS(
            RDF_NAMESPACE,
            "about"));
          // Console.WriteLine("about=%s [%s]"
          // ,about,childElement.GetAttribute("about"));
          if (about == null) {
            about = subject;
            if (about == null) {
              continue;
            }
          }
          foreach (var child2 in child.GetChildNodes()) {
            IElement childElement2 = (child2 is IElement) ? ((IElement)child2) :
              null;
            if (childElement2 == null) {
              continue;
            }
            this.MiniRdfXmlChild(childElement2, about, language);
          }
        } else if (RDF_NAMESPACE.Equals(childElement.GetNamespaceURI())) {
          throw new NotSupportedException();
        }
      }
    }

    private void MiniRdfXmlChild(
      IElement node,
      RDFTerm subject,
      string language) {
      string nsname = node.GetNamespaceURI();
      if (node.GetAttribute("xml:lang") != null) {
        language = node.GetAttribute("xml:lang");
      }
      string localname = node.GetLocalName();
      RDFTerm predicate = this.RelativeResolve(nsname + localname);
      if (!HasNonTextChildNodes(node)) {
        string content = GetTextNodeText(node);
        RDFTerm literal;
        literal = (!String.IsNullOrEmpty(language)) ?
          RDFTerm.FromLangString(content, language) :
          RDFTerm.FromTypedString(content);
        this.outputGraph.Add(new RDFTriple(subject, predicate, literal));
      } else {
        string parseType = node.GetAttributeNS(RDF_NAMESPACE, "parseType");
        if ("Literal".Equals(parseType)) {
          throw new NotSupportedException();
        }
        RDFTerm blank = this.GenerateBlankNode();
        this.context.ValueLanguage = language;
        this.MiniRdfXml(node, this.context, blank);
        this.outputGraph.Add(new RDFTriple(subject, predicate, blank));
      }
    }

    public ISet<RDFTriple> Parse() {
      this.Process(this.document.GetDocumentElement(), true);
      RDFInternal.ReplaceBlankNodes(this.outputGraph, this.bnodeLabels);
      return this.outputGraph;
    }

    private void Process(IElement node, bool root) {
      IList<RDFa.IncompleteTriple> incompleteTriplesLocal = new
      List<RDFa.IncompleteTriple>();
      string localLanguage = this.context.ValueLanguage;
      RDFTerm newSubject = null;
      var recurse = true;
      var skipElement = false;
      RDFTerm currentObject = null;
      IDictionary<string, string> namespacesLocal =
        new Dictionary<string, string>(this.context.ValueNamespaces);
      IDictionary<string, string> iriMapLocal =
        new Dictionary<string, string>(this.context.ValueIriMap);
      string attr = null;
      if (!this.xhtml) {
        attr = node.GetAttribute("xml:base");
        if (attr != null) {
          this.context.ValueBaseURI = URIUtility.RelativeResolve(
              attr,
              this.context.ValueBaseURI);
        }
      }
      // Support XML namespaces
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
      attr = node.GetAttribute("xml:lang");
      if (attr != null) {
        localLanguage = attr;
      }
      // Support RDF/XML metadata
      if (node.GetLocalName().Equals("RDF") &&
        RDF_NAMESPACE.Equals(node.GetNamespaceURI())) {
        this.MiniRdfXml(node, this.context);
        return;
      }
      string rel = node.GetAttribute("rel");
      string rev = node.GetAttribute("rev");
      string property = node.GetAttribute("property");
      string content = node.GetAttribute("content");
      string datatype = node.GetAttribute("datatype");
      if (rel == null && rev == null) {
        // Step 4
        RDFTerm resource = this.GetSafeCurieOrCurieOrIri(
            node.GetAttribute("about"),
            iriMapLocal);
        if (resource == null) {
          resource = this.GetSafeCurieOrCurieOrIri(
              node.GetAttribute("resource"),
              iriMapLocal);
        }
        resource = resource ?? this.RelativeResolve(node.GetAttribute(
          "href"));
        resource = resource ?? this.RelativeResolve(node.GetAttribute("src"));
        if (resource == null || resource.GetKind() != RDFTerm.IRI) {
          string rdfTypeof = this.GetCurie(
              node.GetAttribute("typeof"),
              iriMapLocal);
          if (IsHtmlElement(node, "head") || IsHtmlElement(node, "body")) {
            resource = this.GetSafeCurieOrCurieOrIri(String.Empty,
                iriMapLocal);
          }
          if (resource == null && !this.xhtml && root) {
            resource = this.GetSafeCurieOrCurieOrIri(String.Empty,
                iriMapLocal);
          }
          if (resource == null && rdfTypeof != null) {
            resource = this.GenerateBlankNode();
          }
          if (resource == null) {
            if (this.context.ValueParentObject != null) {
              resource = this.context.ValueParentObject;
            }
            if (node.GetAttribute("property") == null) {
              skipElement = true;
            }
          }
          newSubject = resource;
        } else {
          newSubject = resource;
        }
      } else {
        // Step 5
        RDFTerm resource = this.GetSafeCurieOrCurieOrIri(
            node.GetAttribute("about"),
            iriMapLocal);
        resource = resource ?? this.RelativeResolve(node.GetAttribute("src"));
        if (resource == null || resource.GetKind() != RDFTerm.IRI) {
          string rdfTypeof = this.GetCurie(
              node.GetAttribute("typeof"),
              iriMapLocal);
          if (IsHtmlElement(node, "head") || IsHtmlElement(node, "body")) {
            resource = this.GetSafeCurieOrCurieOrIri(String.Empty,
                iriMapLocal);
          }
          if (resource == null && !this.xhtml && root) {
            resource = this.GetSafeCurieOrCurieOrIri(String.Empty,
                iriMapLocal);
          }
          if (resource == null && rdfTypeof != null) {
            resource = this.GenerateBlankNode();
          }
          if (resource == null) {
            if (this.context.ValueParentObject != null) {
              resource = this.context.ValueParentObject;
            }
          }
          newSubject = resource;
        } else {
          newSubject = resource;
        }
        resource = this.GetSafeCurieOrCurieOrIri(
            node.GetAttribute("resource"),
            iriMapLocal);
        resource = resource ?? this.RelativeResolve(node.GetAttribute(
          "href"));
        currentObject = resource;
      }
      // Step 6
      if (newSubject != null) {
        string[] types = StringUtility.SplitAtSpTabCrLf(node.GetAttribute(
          "typeof"));
        foreach (var type in types) {
          string iri = this.GetCurie(type, iriMapLocal);
          if (iri != null) {
            this.outputGraph.Add(new RDFTriple(
              newSubject,
              RDFTerm.A,
              RDFTerm.FromIRI(iri)));
          }
        }
      }
      // Step 7
      if (currentObject != null) {
        string[] types = StringUtility.SplitAtSpTabCrLf(rel);
        foreach (var type in types) {
          string iri = this.GetRelTermOrCurie(
              type,
              iriMapLocal);
          #if DEBUG
          if (!(newSubject != null)) {
            throw new InvalidOperationException("doesn't satisfy" +
              "\u0020newSubject!=null");
          }
          #endif
          if (iri != null) {
            this.outputGraph.Add(new RDFTriple(
              newSubject,
              RDFTerm.FromIRI(iri),
              currentObject));
          }
        }
        types = StringUtility.SplitAtSpTabCrLf(rev);
        foreach (var type in types) {
          string iri = this.GetRelTermOrCurie(
              type,
              iriMapLocal);
          if (iri != null) {
            this.outputGraph.Add(new RDFTriple(
              currentObject,
              RDFTerm.FromIRI(iri),
              newSubject));
          }
        }
      } else {
        // Step 8
        string[] types = StringUtility.SplitAtSpTabCrLf(rel);
        var hasPredicates = false;
        // Defines predicates
        foreach (var type in types) {
          string iri = this.GetRelTermOrCurie(
              type,
              iriMapLocal);
          if (iri != null) {
            if (!hasPredicates) {
              hasPredicates = true;
              currentObject = this.GenerateBlankNode();
            }
            var inc = new RDFa.IncompleteTriple();
            inc.ValuePredicate = RDFTerm.FromIRI(iri);
            inc.ValueDirection = RDFa.ChainingDirection.Forward;
            incompleteTriplesLocal.Add(inc);
          }
        }
        types = StringUtility.SplitAtSpTabCrLf(rev);
        foreach (var type in types) {
          string iri = this.GetRelTermOrCurie(
              type,
              iriMapLocal);
          if (iri != null) {
            if (!hasPredicates) {
              hasPredicates = true;
              currentObject = this.GenerateBlankNode();
            }
            var inc = new RDFa.IncompleteTriple();
            inc.ValuePredicate = RDFTerm.FromIRI(iri);
            inc.ValueDirection = RDFa.ChainingDirection.Reverse;
            incompleteTriplesLocal.Add(inc);
          }
        }
      }
      // Step 9
      string[] preds = StringUtility.SplitAtSpTabCrLf(property);
      string datatypeValue = this.GetCurie(
          datatype,
          iriMapLocal);
      if (datatype != null && datatypeValue == null) {
        datatypeValue = String.Empty;
      }
      // Console.WriteLine("datatype=[%s] prop=%s vocab=%s",
      // datatype, property, localDefaultVocab);
      // Console.WriteLine("datatypeValue=[%s]",datatypeValue);
      RDFTerm currentProperty = null;
      foreach (var pred in preds) {
        string iri = this.GetCurie(
            pred,
            iriMapLocal);
        if (iri != null) {
          // Console.WriteLine("iri=[%s]",iri);
          currentProperty = null;
          if (datatypeValue != null && datatypeValue.Length > 0 &&
            !datatypeValue.Equals(RDF_XMLLITERAL)) {
            string literal = content;
            literal = literal ?? GetTextNodeText(node);
            currentProperty = RDFTerm.FromTypedString(literal, datatypeValue);
          } else if (node.GetAttribute("content") != null ||
            !HasNonTextChildNodes(node) ||
            (datatypeValue != null && datatypeValue.Length == 0)) {
            string literal = node.GetAttribute("content");
            literal = literal ?? GetTextNodeText(node);
            currentProperty = (!String.IsNullOrEmpty(localLanguage)) ?
              RDFTerm.FromLangString(literal, localLanguage) :
              RDFTerm.FromTypedString(literal);
          } else if (HasNonTextChildNodes(node) && (datatypeValue == null ||
            datatypeValue.Equals(RDF_XMLLITERAL))) {
            // XML literal
            recurse = false;
            datatypeValue = datatypeValue ?? RDF_XMLLITERAL;
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
          }
          #if DEBUG
          if (!(newSubject != null)) {
            throw new InvalidOperationException("doesn't satisfy" +
              "\u0020newSubject!=null");
          }
          #endif
          this.outputGraph.Add(new RDFTriple(
            newSubject,
            RDFTerm.FromIRI(iri),
            currentProperty));
        }
      }
      // Step 10
      if (!skipElement && newSubject != null) {
        IList<RDFa.IncompleteTriple> triples =
          this.context.ValueIncompleteTriples;
        foreach (var triple in triples) {
          if (triple.ValueDirection == RDFa.ChainingDirection.Forward) {
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
      if (recurse) {
        IList<INode> childNodes = node.GetChildNodes();
        foreach (var childNode in childNodes) {
          IElement childElement;
          RDFa.EvalContext oldContext = this.context;
          if (childNode is IElement) {
            childElement = (IElement)childNode;
            // Console.WriteLine("skip=%s vocab=%s local=%s",
            // skipElement, context.defaultVocab,
            // localDefaultVocab);
            if (skipElement) {
              RDFa.EvalContext ec = oldContext.Copy();
              ec.ValueLanguage = localLanguage;
              ec.ValueIriMap = iriMapLocal;
              ec.ValueNamespaces = namespacesLocal;
              this.context = ec;
              this.Process(childElement, false);
            } else {
              var ec = new RDFa.EvalContext();
              ec.ValueBaseURI = oldContext.ValueBaseURI;
              ec.ValueIriMap = iriMapLocal;
              ec.ValueNamespaces = namespacesLocal;
              ec.ValueIncompleteTriples = incompleteTriplesLocal;
              ec.ValueParentSubject = (newSubject == null) ?
                oldContext.ValueParentSubject : newSubject;
              ec.ValueParentObject = (currentObject == null) ? ((newSubject
                == null) ? oldContext.ValueParentSubject : newSubject) :
                currentObject;
              ec.ValueLanguage = localLanguage;
              this.context = ec;
              this.Process(childElement, false);
            }
          }
          this.context = oldContext;
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
