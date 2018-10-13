using System;
using System.Collections.Generic;
using System.Text;
using PeterO;
using PeterO.Rdf;
using com.upokecenter.util;

namespace com.upokecenter.html.data {
  internal class RDFa1 : IRDFParser {
    private static string getTextNodeText(INode node) {
      var builder = new StringBuilder();
      foreach (var child in node.getChildNodes()) {
        if (child.getNodeType() == NodeType.TEXT_NODE) {
          builder.Append(((IText)child).getData());
        } else {
          builder.Append(getTextNodeText(child));
        }
      }
      return builder.ToString();
    }

    private static bool isHtmlElement(IElement element, string name) {
      return element != null &&
          "http://www.w3.org/1999/xhtml".Equals(element.getNamespaceURI()) &&
          name.Equals(element.getLocalName());
    }

    private RDFa.EvalContext context;
    private ISet<RDFTriple> outputGraph;

    private IDocument document;

    private bool xhtml = false;

    private static readonly string
      RDF_XMLLITERAL = "http://www.w3.org/1999/02/22-rdf-syntax-ns#XMLLiteral";

    private static readonly string
      RDF_NAMESPACE = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

    private static IList<string> relterms = (new string[] { "alternate",
      "appendix" ,"cite", "bookmark", "chapter", "contents",
      "copyright", "first", "glossary",
      "help", "icon", "index", "last",
      "license", "meta", "next", "prev",
      "role", "section", "start",
      "stylesheet", "subsection", "top",
      "up", "p3pv1"
  });

    private static int getCuriePrefixLength(string s, int offset, int length) {
      if (s == null || length == 0) {
        return -1;
      }
      if (s[offset] == ':') {
        return 0;
      }
      if (!isNCNameStartChar(s[offset])) {
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
          c = 0x10000 + ((c - 0xd800) << 10) + (s[index + 1] - 0xdc00);
          ++index;
        } else if ((c & 0xf800) == 0xd800) {
          // error
          return -1;
        }
        if (c == ':') {
          return index - offset;
        } else if (!isNCNameChar(c)) {
          return -1;
        }
        ++index;
      }
      return -1;
    }

    private static bool hasNonTextChildNodes(INode node) {
      foreach (var child in node.getChildNodes()) {
        if (child.getNodeType() != NodeType.TEXT_NODE) {
          return true;
        }
      }
      return false;
    }

    private static bool isNCNameChar(int c) {
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

    private static bool isNCNameStartChar(int c) {
      return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
          c == '_' || (c >= 0xc0 && c <= 0xd6) ||
          (c >= 0xd8 && c <= 0xf6) || (c >= 0xf8 && c <= 0x2ff) ||
          (c >= 0x370 && c <= 0x37d) || (c >= 0x37f && c <= 0x1fff) ||
          (c >= 0x200c && c <= 0x200d) || (c >= 0x2070 && c <= 0x218f) ||
          (c >= 0x2c00 && c <= 0x2fef) || (c >= 0x3001 && c <= 0xd7ff) ||
          (c >= 0xf900 && c <= 0xfdcf) || (c >= 0xfdf0 && c <= 0xfffd) ||
          (c >= 0x10000 && c <= 0xeffff);
    }

    private static bool isValidCurieReference(
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
    indexes = URIUtility.splitIRI(
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
      this.context.ValueBaseURI = document.getBaseURI();
      this.context.ValueNamespaces = new Dictionary<string, string>();
      if (!URIUtility.hasScheme(this.context.ValueBaseURI)) {
        throw new ArgumentException("baseURI: " + this.context.ValueBaseURI);
      }
  this.context.ValueParentSubject = RDFTerm.fromIRI(this.context.ValueBaseURI);
      this.context.ValueParentObject = null;
      this.context.ValueIriMap = new Dictionary<string,
          string>();
      this.context.ValueListMap = new
        Dictionary<string, IList<RDFTerm>>();
      this.context.ValueIncompleteTriples = new List<RDFa.IncompleteTriple>();
      this.context.ValueLanguage = null;
      this.outputGraph = new HashSet<RDFTriple>();
      if (isHtmlElement(document.getDocumentElement(), "html")) {
        this.xhtml = true;
      }
    }

    private RDFTerm generateBlankNode() {
      // Use "b:" as the prefix; according to the CURIE syntax,
      // "b:" can never begin a valid CURIE reference (in RDFa 1.0,
      // the reference has the broader production irelative-refValue),
      // so it can
      // be used to guarantee that generated blank nodes will never
      // conflict with those stated explicitly
      string blankNodeString = "b:" +
   Convert.ToString(
  this.blankNode,
  System.Globalization.CultureInfo.InvariantCulture);
      ++this.blankNode;
      RDFTerm term = RDFTerm.fromBlankNode(blankNodeString);
      this.bnodeLabels.Add(blankNodeString, term);
      return term;
    }

    private string getCurie(
        string attribute,
   int offset,
   int length,
        IDictionary<string, string> prefixMapping) {
      if (attribute == null) {
        return null;
      }
      int refIndex = offset;
      int refLength = length;
      int prefix = getCuriePrefixLength(attribute, refIndex, refLength);
      string prefixIri = null;
      if (prefix >= 0) {
        string prefixName = DataUtilities.ToLowerCaseAscii(
            attribute.Substring(
  refIndex,
  (refIndex + prefix) - (refIndex))); refIndex += prefix + 1; refLength -=
    prefix + 1; prefixIri = prefixMapping[prefixName];
        prefixIri = (prefix == 0) ? RDFA_DEFAULT_PREFIX :
          prefixMapping[prefixName];
        if (prefixIri == null || "_".Equals(prefixName)) {
          return null;
        }
      } else
        // RDFa doesn't define a mapping for an absent prefix
        return null;
      if (!isValidCurieReference(attribute, refIndex, refLength)) {
        return null;
      }
      if (prefix >= 0) {
        return
         this.relativeResolve(
         prefixIri + attribute.Substring(
         refIndex,
         (refIndex + refLength) - refIndex))
          .getValue();
      } else {
        return null;
      }
    }

    private string getCurie(
        string attribute,
        IDictionary<string, string> prefixMapping) {
      return (attribute == null) ? null :
        this.getCurie(attribute, 0, attribute.Length, prefixMapping);
    }

    private RDFTerm getCurieOrBnode(
        string attribute,
   int offset,
   int length,
        IDictionary<string, string> prefixMapping) {
      int refIndex = offset;
      int refLength = length;
      int prefix = getCuriePrefixLength(attribute, refIndex, refLength);
      string prefixIri = null;
      string prefixName = null;
      if (prefix >= 0) {
        prefixName = DataUtilities.ToLowerCaseAscii(
            attribute.Substring(
  refIndex,
  (refIndex + prefix) - (refIndex))); refIndex += prefix + 1; refLength -=
    prefix + 1; prefixIri = (prefix == 0) ? RDFA_DEFAULT_PREFIX :
          prefixMapping[prefixName];
        if (prefixIri == null && !"_".Equals(prefixName)) {
          return null;
        }
      } else
        // RDFa doesn't define a mapping for an absent prefix
        return null;
      if (!isValidCurieReference(attribute, refIndex, refLength)) {
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
            // in generateBlankNode for why "b:" appears
            // at the beginning
            return this.getNamedBlankNode("b:empty");
          }
          return
    this.getNamedBlankNode(
    attribute.Substring(
    refIndex,
    (refIndex + refLength) - refIndex)); } if (!(refIndex >= 0)) {
          throw new InvalidOperationException(attribute);
        }
        if (!(refIndex + refLength <= attribute.Length)) {
          throw new InvalidOperationException(attribute);
        }
        return
    this.relativeResolve(
    prefixIri + attribute.Substring(
    refIndex,
    (refIndex + refLength) - refIndex));
      } else {
        return null;
      }
    }

    private RDFTerm getNamedBlankNode(string str) {
      RDFTerm term = RDFTerm.fromBlankNode(str);
      this.bnodeLabels.Add(str, term);
      return term;
    }

    private string getRelTermOrCurie(
    string attribute,
        IDictionary<string, string> prefixMapping) {
      return relterms.Contains(DataUtilities.ToLowerCaseAscii(attribute)) ?
        ("http://www.w3.org/1999/xhtml/vocab#" +
   DataUtilities.ToLowerCaseAscii(attribute)) :
        this.getCurie(attribute, prefixMapping);
    }

    private RDFTerm getSafeCurieOrCurieOrIri(
        string attribute, IDictionary<string, string> prefixMapping) {
      if (attribute == null) {
        return null;
      }
      int lastIndex = attribute.Length - 1;
      if (attribute.Length >= 2 && attribute[0] == '[' && attribute[lastIndex]
          == ']') {
        RDFTerm curie = this.getCurieOrBnode(
    attribute,
    1,
    attribute.Length - 2,
    prefixMapping);
        return curie;
      } else {
        RDFTerm curie = this.getCurieOrBnode(
    attribute,
    0,
    attribute.Length,
    prefixMapping);
        if (curie == null) {
          // evaluate as IRI
          return this.relativeResolve(attribute);
        }
        return curie;
      }
    }

    private void miniRdfXml(IElement node, RDFa.EvalContext evalContext) {
      this.miniRdfXml(node, evalContext, null);
    }

    // Processes a subset of RDF/XML metadata
    // Doesn't implement RDF/XML completely
    private void miniRdfXml(
    IElement node,
    RDFa.EvalContext evalContext,
    RDFTerm subject) {
      string language = evalContext.ValueLanguage;
      foreach (var child in node.getChildNodes()) {
        IElement childElement = (child is IElement) ?
            ((IElement)child) : null;
        if (childElement == null) {
          continue;
        }
        language = (node.getAttribute("xml:lang") != null) ?
          node.getAttribute("xml:lang") : evalContext.ValueLanguage;
        if (childElement.getLocalName().Equals("Description") &&
            RDF_NAMESPACE.Equals(childElement.getNamespaceURI())) {
          RDFTerm
about =
this.relativeResolve(childElement.getAttributeNS(RDF_NAMESPACE, "about"
));
          // DebugUtility.Log("about=%s [%s]"
          // ,about,childElement.getAttribute("about"));
          if (about == null) {
            about = subject;
            if (about == null) {
              continue;
            }
          }
          foreach (var child2 in child.getChildNodes()) {
     IElement childElement2 = (child2 is IElement) ? ((IElement)child2) :
              null;
            if (childElement2 == null) {
              continue;
            }
            this.miniRdfXmlChild(childElement2, about, language);
          }
        } else if (RDF_NAMESPACE.Equals(childElement.getNamespaceURI())) {
          throw new NotSupportedException();
        }
      }
    }

    private void miniRdfXmlChild(
      IElement node,
      RDFTerm subject,
      string language) {
      string nsname = node.getNamespaceURI();
      if (node.getAttribute("xml:lang") != null) {
        language = node.getAttribute("xml:lang");
      }
      string localname = node.getLocalName();
      RDFTerm predicate = this.relativeResolve(nsname + localname);
      if (!hasNonTextChildNodes(node)) {
        string content = getTextNodeText(node);
        RDFTerm literal;
        literal = (!String.IsNullOrEmpty(language)) ?
          RDFTerm.fromLangString(content, language) :
          RDFTerm.fromTypedString(content);
        this.outputGraph.Add(new RDFTriple(subject, predicate, literal));
      } else {
        string parseType = node.getAttributeNS(RDF_NAMESPACE, "parseType");
        if ("Literal".Equals(parseType)) {
          throw new NotSupportedException();
        }
        RDFTerm blank = this.generateBlankNode();
        this.context.ValueLanguage = language;
        this.miniRdfXml(node, this.context, blank);
        this.outputGraph.Add(new RDFTriple(subject, predicate, blank));
      }
    }

    public ISet<RDFTriple> Parse() {
      this.process(this.document.getDocumentElement(), true);
      RDFInternal.replaceBlankNodes(this.outputGraph, this.bnodeLabels);
      return this.outputGraph;
    }

    private void process(IElement node, bool root) {
      IList<RDFa.IncompleteTriple> incompleteTriplesLocal = new
        List<RDFa.IncompleteTriple>();
      string localLanguage = this.context.ValueLanguage;
      RDFTerm newSubject = null;
      var recurse = true;
      var skipElement = false;
      RDFTerm currentObject = null;
      IDictionary<string, string> namespacesLocal =
        new Dictionary<string,
            string>(this.context.ValueNamespaces);
      IDictionary<string, string> iriMapLocal =
       new Dictionary<string,
            string>(this.context.ValueIriMap);
      string attr = null;
      if (!this.xhtml) {
        attr = node.getAttribute("xml:base");
        if (attr != null) {
          this.context.ValueBaseURI = URIUtility.relativeResolve(
           attr,
           this.context.ValueBaseURI);
        }
      }
      // Support XML namespaces
      foreach (var attrib in node.getAttributes()) {
        string name = DataUtilities.ToLowerCaseAscii(attrib.getName());
        // DebugUtility.Log(attrib);
        if (name.Equals("xmlns")) {
          // DebugUtility.Log("xmlns %s",attrib.getValue());
          iriMapLocal.Add(String.Empty, attrib.getValue());
          namespacesLocal.Add(String.Empty, attrib.getValue());
        } else if (name.StartsWith("xmlns:", StringComparison.Ordinal) &&
                name.Length > 6) {
          string prefix = name.Substring(6);
          // DebugUtility.Log("xmlns %s %s",prefix,attrib.getValue());
          if (!"_".Equals(prefix)) {
            iriMapLocal.Add(prefix, attrib.getValue());
          }
          namespacesLocal.Add(prefix, attrib.getValue());
        }
      }
      attr = node.getAttribute("xml:lang");
      if (attr != null) {
        localLanguage = attr;
      }
      // Support RDF/XML metadata
      if (node.getLocalName().Equals("RDF") &&
          RDF_NAMESPACE.Equals(node.getNamespaceURI())) {
        this.miniRdfXml(node, this.context);
        return;
      }
      string rel = node.getAttribute("rel");
      string rev = node.getAttribute("rev");
      string property = node.getAttribute("property");
      string content = node.getAttribute("content");
      string datatype = node.getAttribute("datatype");
      if (rel == null && rev == null) {
        // Step 4
        RDFTerm resource = this.getSafeCurieOrCurieOrIri(
            node.getAttribute("about"),
            iriMapLocal);
        if (resource == null) {
          resource = this.getSafeCurieOrCurieOrIri(
              node.getAttribute("resource"),
              iriMapLocal);
        }
        resource = resource ?? this.relativeResolve(node.getAttribute("href"));
        resource = resource ?? this.relativeResolve(node.getAttribute("src"));
        if (resource == null || resource.getKind() != RDFTerm.IRI) {
          string rdfTypeof = this.getCurie(
            node.getAttribute("typeof"),
            iriMapLocal);
          if (isHtmlElement(node, "head") || isHtmlElement(node, "body")) {
            resource = this.getSafeCurieOrCurieOrIri(String.Empty, iriMapLocal);
          }
          if (resource == null && !this.xhtml && root) {
            resource = this.getSafeCurieOrCurieOrIri(String.Empty, iriMapLocal);
          }
          if (resource == null && rdfTypeof != null) {
            resource = this.generateBlankNode();
          }
          if (resource == null) {
            if (this.context.ValueParentObject != null) {
              resource = this.context.ValueParentObject;
            }
            if (node.getAttribute("property") == null) {
              skipElement = true;
            }
          }
          newSubject = resource;
        } else {
          newSubject = resource;
        }
      } else {
        // Step 5
        RDFTerm resource = this.getSafeCurieOrCurieOrIri(
            node.getAttribute("about"),
            iriMapLocal);
        resource = resource ?? this.relativeResolve(node.getAttribute("src"));
        if (resource == null || resource.getKind() != RDFTerm.IRI) {
          string rdfTypeof = this.getCurie(
            node.getAttribute("typeof"),
            iriMapLocal);
          if (isHtmlElement(node, "head") || isHtmlElement(node, "body")) {
            resource = this.getSafeCurieOrCurieOrIri(String.Empty, iriMapLocal);
          }
          if (resource == null && !this.xhtml && root) {
            resource = this.getSafeCurieOrCurieOrIri(String.Empty, iriMapLocal);
          }
          if (resource == null && rdfTypeof != null) {
            resource = this.generateBlankNode();
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
        resource = this.getSafeCurieOrCurieOrIri(
            node.getAttribute("resource"),
            iriMapLocal);
        resource = resource ?? this.relativeResolve(node.getAttribute("href"));
        currentObject = resource;
      }
      // Step 6
      if (newSubject != null) {
        string[] types = StringUtility.SplitAtSpTabCrLf(node.getAttribute(
        "typeof"));
        foreach (var type in types) {
          string iri = this.getCurie(type, iriMapLocal);
          if (iri != null) {
            this.outputGraph.Add(new RDFTriple(
                newSubject,
                RDFTerm.A,
                RDFTerm.fromIRI(iri)));
          }
        }
      }
      // Step 7
      if (currentObject != null) {
        string[] types = StringUtility.SplitAtSpTabCrLf(rel);
        foreach (var type in types) {
          string iri = this.getRelTermOrCurie(
    type,
    iriMapLocal);
#if DEBUG
          if (!(newSubject != null)) {
            throw new
  InvalidOperationException("doesn't satisfy newSubject!=null");
          }
#endif
          if (iri != null) {
            this.outputGraph.Add(new RDFTriple(
                newSubject,
                RDFTerm.fromIRI(iri),
                currentObject));
          }
        }
        types = StringUtility.SplitAtSpTabCrLf(rev);
        foreach (var type in types) {
          string iri = this.getRelTermOrCurie(
    type,
    iriMapLocal);
          if (iri != null) {
            this.outputGraph.Add(new RDFTriple(
                currentObject,
                RDFTerm.fromIRI(iri),
                newSubject));
          }
        }
      } else {
        // Step 8
        string[] types = StringUtility.SplitAtSpTabCrLf(rel);
        var hasPredicates = false;
        // Defines predicates
        foreach (var type in types) {
          string iri = this.getRelTermOrCurie(
    type,
    iriMapLocal);
          if (iri != null) {
            if (!hasPredicates) {
              hasPredicates = true;
              currentObject = this.generateBlankNode();
            }
            var inc = new RDFa.IncompleteTriple();
            inc.ValuePredicate = RDFTerm.fromIRI(iri);
            inc.ValueDirection = RDFa.ChainingDirection.Forward;
            incompleteTriplesLocal.Add(inc);
          }
        }
        types = StringUtility.SplitAtSpTabCrLf(rev);
        foreach (var type in types) {
          string iri = this.getRelTermOrCurie(
    type,
    iriMapLocal);
          if (iri != null) {
            if (!hasPredicates) {
              hasPredicates = true;
              currentObject = this.generateBlankNode();
            }
            var inc = new RDFa.IncompleteTriple();
            inc.ValuePredicate = RDFTerm.fromIRI(iri);
            inc.ValueDirection = RDFa.ChainingDirection.Reverse;
            incompleteTriplesLocal.Add(inc);
          }
        }
      }
      // Step 9
      string[] preds = StringUtility.SplitAtSpTabCrLf(property);
      string datatypeValue = this.getCurie(
    datatype,
    iriMapLocal);
      if (datatype != null && datatypeValue == null) {
        datatypeValue = String.Empty;
      }
      // DebugUtility.Log("datatype=[%s] prop=%s vocab=%s",
      // datatype, property, localDefaultVocab);
      // DebugUtility.Log("datatypeValue=[%s]",datatypeValue);
      RDFTerm currentProperty = null;
      foreach (var pred in preds) {
        string iri = this.getCurie(
    pred,
    iriMapLocal);
        if (iri != null) {
          // DebugUtility.Log("iri=[%s]",iri);
          currentProperty = null;
          if (datatypeValue != null && datatypeValue.Length > 0 &&
              !datatypeValue.Equals(RDF_XMLLITERAL)) {
            string literal = content;
            literal = literal ?? getTextNodeText(node);
            currentProperty = RDFTerm.fromTypedString(literal, datatypeValue);
          } else if (node.getAttribute("content") != null ||
              !hasNonTextChildNodes(node) ||
              (datatypeValue != null && datatypeValue.Length == 0)) {
            string literal = node.getAttribute("content");
            literal = literal ?? getTextNodeText(node);
            currentProperty = (!String.IsNullOrEmpty(localLanguage)) ?
                RDFTerm.fromLangString(literal, localLanguage) :
                  RDFTerm.fromTypedString(literal);
          } else if (hasNonTextChildNodes(node) &&
              (datatypeValue == null || datatypeValue.Equals(RDF_XMLLITERAL))) {
            // XML literal
            recurse = false;
            datatypeValue = datatypeValue ?? RDF_XMLLITERAL;
            try {
              string literal = ExclusiveCanonicalXML.canonicalize(
    node,
    false,
    namespacesLocal);
              currentProperty = RDFTerm.fromTypedString(literal, datatypeValue);
            } catch (ArgumentException) {
              // failure to canonicalize
            }
          }
#if DEBUG
          if (!(newSubject != null)) {
            throw new
  InvalidOperationException("doesn't satisfy newSubject!=null");
          }
#endif
          this.outputGraph.Add(new RDFTriple(
              newSubject,
              RDFTerm.fromIRI(iri),
              currentProperty));
        }
      }
      // Step 10
      if (!skipElement && newSubject != null) {
        foreach (var triple in this.context.ValueIncompleteTriples) {
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
        foreach (var childNode in node.getChildNodes()) {
          IElement childElement;
          RDFa.EvalContext oldContext = this.context;
          if (childNode is IElement) {
            childElement = (IElement)childNode;
            // DebugUtility.Log("skip=%s vocab=%s local=%s",
            // skipElement, context.defaultVocab,
            // localDefaultVocab);
            if (skipElement) {
              RDFa.EvalContext ec = oldContext.copy();
              ec.ValueLanguage = localLanguage;
              ec.ValueIriMap = iriMapLocal;
              ec.ValueNamespaces = namespacesLocal;
              this.context = ec;
              this.process(childElement, false);
            } else {
              var ec = new RDFa.EvalContext();
              ec.ValueBaseURI = oldContext.ValueBaseURI;
              ec.ValueIriMap = iriMapLocal;
              ec.ValueNamespaces = namespacesLocal;
              ec.ValueIncompleteTriples = incompleteTriplesLocal;
              ec.ValueParentSubject = (newSubject == null) ?
                oldContext.ValueParentSubject : newSubject;
              ec.ValueParentObject = ((currentObject == null) ? ((newSubject
                == null) ? oldContext.ValueParentSubject : newSubject) :
                currentObject);
              ec.ValueLanguage = localLanguage;
              this.context = ec;
              this.process(childElement, false);
            }
          }
          this.context = oldContext;
        }
      }
    }

    private RDFTerm relativeResolve(string iri) {
      if (iri == null) {
        return null;
      }
      return (URIUtility.splitIRI(iri) == null) ? null :
   RDFTerm.fromIRI(
  URIUtility.relativeResolve(
  iri,
  this.context.ValueBaseURI));
    }
  }
}
