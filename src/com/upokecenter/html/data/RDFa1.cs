namespace com.upokecenter.html.data {
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using com.upokecenter.html;
using com.upokecenter.rdf;
using com.upokecenter.util;
internal class RDFa1 : IRDFParser {
  private static string getTextNodeText(INode node) {
    StringBuilder builder = new StringBuilder();
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
    RDF_XMLLITERAL="http://www.w3.org/1999/02/22-rdf-syntax-ns#XMLLiteral" ;
  private static readonly string
    RDF_NAMESPACE="http://www.w3.org/1999/02/22-rdf-syntax-ns#" ;

  private static IList<string> relterms=(new string[] {
      "alternate","appendix","cite",
      "bookmark","chapter","contents",
      "copyright","first","glossary",
      "help","icon","index","last",
      "license","meta","next","prev",
      "role","section","start",
      "stylesheet","subsection","top",
      "up","p3pv1"
  });

  private static int getCuriePrefixLength(string s, int offset, int length) {
    if (s == null || length == 0) {
 return -1;
}
    if (s[offset]==':') {
 return 0;
}
    if (!isNCNameStartChar(s[offset])) {
 return -1;
}
    int index = offset + 1;
    int sLength = offset + length;
    while (index<sLength) {
      // Get the next Unicode character
      int c = s[index];
      if (c >= 0xd800 && c <= 0xdbff && index + 1<sLength &&
          s[index + 1]>= 0xdc00 && s[index + 1]<= 0xdfff) {
        // Get the Unicode code point for the surrogate pair
        c = 0x10000+(c-0xd800)*0x400+(s[index + 1]-0xdc00);
        ++index;
      } else if (c >= 0xd800 && c <= 0xdfff) {
 // error
        return -1;
 }
      if (c==':') {
 return index-offset;
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
    return (c>= 'a' && c<= 'z') || (c>= 'A' && c<= 'Z') ||
        c=='_' || c=='.' || c=='-' || (c>= '0' && c<= '9') ||
        c == 0xb7 || (c >= 0xc0 && c <= 0xd6) ||
        (c >= 0xd8 && c <= 0xf6) || (c >= 0xf8 && c <= 0x2ff) ||
        (c >= 0x300 && c <= 0x37d) || (c >= 0x37f && c <= 0x1fff) ||
        (c >= 0x200c && c <= 0x200d) || (c >= 0x203f && c <= 0x2040) ||
        (c >= 0x2070 && c <= 0x218f) || (c >= 0x2c00 && c <= 0x2fef) ||
        (c >= 0x3001 && c <= 0xd7ff) || (c >= 0xf900 && c <= 0xfdcf) ||
        (c >= 0xfdf0 && c <= 0xfffd) || (c >= 0x10000 && c <= 0xeffff);
  }

  private static bool isNCNameStartChar(int c) {
    return (c>= 'a' && c<= 'z') || (c>= 'A' && c<= 'Z') ||
        c=='_' || (c>= 0xc0 && c<= 0xd6) ||
        (c >= 0xd8 && c <= 0xf6) || (c >= 0xf8 && c <= 0x2ff) ||
        (c >= 0x370 && c <= 0x37d) || (c >= 0x37f && c <= 0x1fff) ||
        (c >= 0x200c && c <= 0x200d) || (c >= 0x2070 && c <= 0x218f) ||
        (c >= 0x2c00 && c <= 0x2fef) || (c >= 0x3001 && c <= 0xd7ff) ||
        (c >= 0xf900 && c <= 0xfdcf) || (c >= 0xfdf0 && c <= 0xfffd) ||
        (c >= 0x10000 && c <= 0xeffff);
  }
  private static bool isValidCurieReference(string s, int offset, int length) {
    if (s == null) {
 return false;
}
    if (length == 0) {
 return true;
}
    int[]
  indexes = URIUtility.splitIRI(s, offset, length, URIUtility.ParseMode.IRIStrict);
    if (indexes == null) {
 return false;
}
    if (indexes[0]!=-1) {
 // check if scheme component is present
      return false;
 }
    return true;
  }
  private int blankNode;

  private IDictionary<string, RDFTerm> bnodeLabels = new
    PeterO.Support.LenientDictionary<string, RDFTerm>();

  private static readonly string RDFA_DEFAULT_PREFIX =
    "http://www.w3.org/1999/xhtml/vocab#" ;

  public RDFa1(IDocument document) {
    this.document = document;
    this.context = new RDFa.EvalContext();
    this.context.baseURI = document.getBaseURI();
 this.context.namespaces = new
      PeterO.Support.LenientDictionary<string, string>();
    if (!URIUtility.hasScheme(this.context.baseURI)) {
 throw new ArgumentException("baseURI: "+this.context.baseURI);
}
    this.context.parentSubject = RDFTerm.fromIRI(this.context.baseURI);
    this.context.parentObject = null;
    this.context.iriMap = new PeterO.Support.LenientDictionary<string, string>();
    this.context.listMap = new
      PeterO.Support.LenientDictionary<string, IList<RDFTerm>>();
    this.context.incompleteTriples = new List<RDFa.IncompleteTriple>();
    this.context.language = null;
    this.outputGraph = new HashSet<RDFTriple>();
    if (isHtmlElement(document.getDocumentElement(),"html")) {
      xhtml = true;
    }
  }
  private RDFTerm generateBlankNode() {
    // Use "b:" as the prefix; according to the CURIE syntax,
    // "b:" can never begin a valid CURIE reference (in RDFa 1.0,
    // the reference has the broader production irelative-refValue),
    // so it can
    // be used to guarantee that generated blank nodes will never
    // conflict with those stated explicitly
    string blankNodeString="b:"
      +Convert.ToString(blankNode, CultureInfo.InvariantCulture);
    ++blankNode;
    RDFTerm term = RDFTerm.fromBlankNode(blankNodeString);
    bnodeLabels.Add(blankNodeString, term);
    return term;
  }

  private string getCurie(
      string attribute, int offset, int length,
      IDictionary<string, string> prefixMapping) {
    if (attribute == null) {
 return null;
}
    int refIndex = offset;
    int refLength = length;
    int prefix = getCuriePrefixLength(attribute, refIndex, refLength);
    string prefixIri = null;
    if (prefix >= 0) {
      string prefixName = StringUtility.toLowerCaseAscii(
          attribute.Substring(refIndex, (refIndex + prefix)-(refIndex)));
      refIndex+=(prefix + 1);
      refLength-=(prefix + 1);
      prefixIri = prefixMapping[prefixName];
      if (prefix == 0) {
        prefixIri = RDFA_DEFAULT_PREFIX;
      } else {
        prefixIri = prefixMapping[prefixName];
      }
      if (prefixIri==null || "_".Equals(prefixName)) {
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
  relativeResolve(prefixIri + attribute.Substring(refIndex, (refIndex + refLength)-(refIndex)))
   .getValue();
} else {
 return null;
}
  }

  private string getCurie(
      string attribute,
      IDictionary<string, string> prefixMapping) {
    return (attribute == null) ? (null) :
      (getCurie(attribute, 0, attribute.Length, prefixMapping));
  }

  private RDFTerm getCurieOrBnode(
      string attribute, int offset, int length,
      IDictionary<string, string> prefixMapping) {
    int refIndex = offset;
    int refLength = length;
    int prefix = getCuriePrefixLength(attribute, refIndex, refLength);
    string prefixIri = null;
    string prefixName = null;
    if (prefix >= 0) {
      prefixName = StringUtility.toLowerCaseAscii(
          attribute.Substring(refIndex, (refIndex + prefix)-(refIndex)));
      refIndex+=(prefix + 1);
      refLength-=(prefix + 1);
      if (prefix == 0) {
        prefixIri = RDFA_DEFAULT_PREFIX;
      } else {
        prefixIri = prefixMapping[prefixName];
      }
      if (prefixIri==null && !"_".Equals(prefixName)) {
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
            return getNamedBlankNode ("b:empty");
          }
        return
  getNamedBlankNode(attribute.Substring(refIndex, (refIndex + refLength)-(refIndex)));
      }
      #if DEBUG
if (!(refIndex >= 0)) {
 throw new InvalidOperationException(attribute);
}
if (!(refIndex + refLength <= attribute.Length)) {
 throw new InvalidOperationException(attribute);
}
#endif
      return
  relativeResolve(prefixIri + attribute.Substring(refIndex, (refIndex + refLength)-(refIndex)));
    } else {
 return null;
}
  }

  private RDFTerm getNamedBlankNode(string str) {
    RDFTerm term = RDFTerm.fromBlankNode(str);
    bnodeLabels.Add(str, term);
    return term;
  }

  private string getRelTermOrCurie(string attribute,
      IDictionary<string, string> prefixMapping) {
    return (relterms.Contains(StringUtility.toLowerCaseAscii(attribute))) ?
      ("http://www.w3.org/1999/xhtml/vocab#"
      +StringUtility.toLowerCaseAscii(attribute)) :
      (getCurie(attribute, prefixMapping));
  }

  private RDFTerm getSafeCurieOrCurieOrIri(
      string attribute, IDictionary<string, string> prefixMapping) {
    if (attribute == null) {
 return null;
}
    int lastIndex = attribute.Length-1;
    if (attribute.Length>= 2 && attribute[0]=='[' && attribute[lastIndex]==']') {
      RDFTerm curie = getCurieOrBnode(attribute, 1, attribute.Length-2,
          prefixMapping);
      return curie;
    } else {
      RDFTerm curie = getCurieOrBnode(attribute, 0, attribute.Length,
          prefixMapping);
      if (curie == null) {
 // evaluate as IRI
        return relativeResolve(attribute);
 }
      return curie;
    }
  }
  private void miniRdfXml(IElement node, RDFa.EvalContext context) {
    miniRdfXml(node, context, null);
  }

  // Processes a subset of RDF/XML metadata
  // Doesn't implement RDF/XML completely
  private void miniRdfXml(IElement node, RDFa.EvalContext context, RDFTerm
    subject) {
    string language = context.language;
    foreach (var child in node.getChildNodes()) {
      IElement childElement=(child is IElement) ?
          ((IElement)child) : null;
          if (childElement == null) {
            continue;
          }
          if (node.getAttribute("xml:lang")!=null) {
            language=node.getAttribute("xml:lang");
          } else {
            language = context.language;
          }
          if (childElement.getLocalName().Equals("Description") &&
              RDF_NAMESPACE.Equals(childElement.getNamespaceURI())) {
            RDFTerm
  about=relativeResolve(childElement.getAttributeNS(RDF_NAMESPACE,"about"
));
            //Console.WriteLine("about=%s [%s]"
            // ,about,childElement.getAttribute("about"));
            if (about == null) {
              about = subject;
              if (about == null) {
                continue;
              }
            }
            foreach (var child2 in child.getChildNodes()) {
              IElement childElement2= ((child2 is IElement) ?
                    ((IElement)child2) : null);
              if (childElement2 == null) {
                continue;
              }
              miniRdfXmlChild(childElement2, about, language);
            }
          } else if (RDF_NAMESPACE.Equals(childElement.getNamespaceURI())) {
 throw new NotSupportedException();
}
    }
  }
  private void miniRdfXmlChild(IElement node, RDFTerm subject, string language) {
    string nsname = node.getNamespaceURI();
    if (node.getAttribute("xml:lang")!=null) {
      language=node.getAttribute("xml:lang");
    }
    string localname = node.getLocalName();
    RDFTerm predicate = relativeResolve(nsname + localname);
    if (!hasNonTextChildNodes(node)) {
      string content = getTextNodeText(node);
      RDFTerm literal;
      if (!String.IsNullOrEmpty(language)) {
        literal = RDFTerm.fromLangString(content, language);
      } else {
        literal = RDFTerm.fromTypedString(content);
      }
      outputGraph.Add(new RDFTriple(subject, predicate, literal));
    } else {
      string parseType=node.getAttributeNS(RDF_NAMESPACE, "parseType");
      if ("Literal".Equals(parseType)) {
 throw new NotSupportedException();
}
      RDFTerm blank = generateBlankNode();
      context.language = language;
      miniRdfXml(node, context, blank);
      outputGraph.Add(new RDFTriple(subject, predicate, blank));
    }
  }

  public ISet<RDFTriple> parse() {
    process(document.getDocumentElement(), true);
    RDFInternal.replaceBlankNodes(outputGraph, bnodeLabels);
    return outputGraph;
  }

  private void process(IElement node, bool root) {
    IList<RDFa.IncompleteTriple> incompleteTriplesLocal = new
      List<RDFa.IncompleteTriple>();
    string localLanguage = context.language;
    RDFTerm newSubject = null;
    bool recurse = true;
    bool skipElement = false;
    RDFTerm currentObject = null;
    IDictionary<string, string> namespacesLocal=
        new PeterO.Support.LenientDictionary<string, string>(context.namespaces);
    IDictionary<string, string> iriMapLocal=
        new PeterO.Support.LenientDictionary<string, string>(context.iriMap);
    string attr = null;
    if (!xhtml) {
      attr=node.getAttribute("xml:base");
      if (attr != null) {
        context.baseURI = URIUtility.relativeResolve(attr, context.baseURI);
      }
    }
    // Support XML namespaces
    foreach (var attrib in node.getAttributes()) {
      string name = StringUtility.toLowerCaseAscii(attrib.getName());
      //Console.WriteLine(attrib);
      if (name.Equals("xmlns")) {
        //Console.WriteLine("xmlns %s",attrib.getValue());
        iriMapLocal.Add("", attrib.getValue());
        namespacesLocal.Add("", attrib.getValue());
} else if (name.StartsWith("xmlns:",StringComparison.Ordinal) &&
        name.Length>6) {
        string prefix = name.Substring(6);
        //Console.WriteLine("xmlns %s %s",prefix,attrib.getValue());
        if (!"_".Equals(prefix)) {
          iriMapLocal.Add(prefix, attrib.getValue());
        }
        namespacesLocal.Add(prefix, attrib.getValue());
      }
    }
    attr=node.getAttribute("xml:lang");
    if (attr != null) {
      localLanguage = attr;
    }
    // Support RDF/XML metadata
    if (node.getLocalName().Equals("RDF") &&
        RDF_NAMESPACE.Equals(node.getNamespaceURI())) {
      miniRdfXml(node, context);
      return;
    }
    string rel=node.getAttribute("rel");
    string rev=node.getAttribute("rev");
    string property=node.getAttribute("property");
    string content=node.getAttribute("content");
    string datatype=node.getAttribute("datatype");
    if (rel == null && rev == null) {
      // Step 4
      RDFTerm resource = getSafeCurieOrCurieOrIri(
          node.getAttribute("about"),
          iriMapLocal);
      if (resource == null) {
        resource = getSafeCurieOrCurieOrIri(
            node.getAttribute("resource"),
            iriMapLocal);
      }
      if (resource == null) {
        resource=relativeResolve(node.getAttribute("href"));
      }
      if (resource == null) {
        resource=relativeResolve(node.getAttribute("src"));
      }
      if (resource == null || resource.getKind() != RDFTerm.IRI) {
        string rdfTypeof=getCurie(node.getAttribute("typeof"),iriMapLocal);
        if (isHtmlElement(node, "head") || isHtmlElement(node, "body")) {
          resource=getSafeCurieOrCurieOrIri("",iriMapLocal);
        }
        if (resource == null && !xhtml && root) {
          resource=getSafeCurieOrCurieOrIri("",iriMapLocal);
        }
        if (resource == null && rdfTypeof != null) {
          resource = generateBlankNode();
        }
        if (resource == null) {
          if (context.parentObject != null) {
            resource = context.parentObject;
          }
          if (node.getAttribute("property")==null) {
            skipElement = true;
          }
        }
        newSubject = resource;
      } else {
        newSubject = resource;
      }
    } else {
      // Step 5
      RDFTerm resource = getSafeCurieOrCurieOrIri(
          node.getAttribute("about"),
          iriMapLocal);
      if (resource == null) {
        resource=relativeResolve(node.getAttribute("src"));
      }
      if (resource == null || resource.getKind() != RDFTerm.IRI) {
        string rdfTypeof=getCurie(node.getAttribute("typeof"),iriMapLocal);
        if (isHtmlElement(node, "head") || isHtmlElement(node, "body")) {
          resource=getSafeCurieOrCurieOrIri("",iriMapLocal);
        }
        if (resource == null && !xhtml && root) {
          resource=getSafeCurieOrCurieOrIri("",iriMapLocal);
        }
        if (resource == null && rdfTypeof != null) {
          resource = generateBlankNode();
        }
        if (resource == null) {
          if (context.parentObject != null) {
            resource = context.parentObject;
          }
        }
        newSubject = resource;
      } else {
        newSubject = resource;
      }
      resource = getSafeCurieOrCurieOrIri(
          node.getAttribute("resource"),
          iriMapLocal);
      if (resource == null) {
        resource=relativeResolve(node.getAttribute("href"));
      }
      currentObject = resource;
    }
    // Step 6
    if (newSubject != null) {
  string[] types=StringUtility.splitAtNonFFSpaces(node.getAttribute("typeof"
));
      foreach (var type in types) {
        string iri = getCurie(type, iriMapLocal);
        if (iri != null) {
          outputGraph.Add(new RDFTriple(
              newSubject,
              RDFTerm.A,
              RDFTerm.fromIRI(iri)));
        }
      }
    }
    // Step 7
    if (currentObject != null) {
      string[] types = StringUtility.splitAtNonFFSpaces(rel);
      foreach (var type in types) {
        string iri = getRelTermOrCurie(type,
            iriMapLocal);
        #if DEBUG
if (!(newSubject != null)) {
 throw new InvalidOperationException("doesn't satisfy newSubject!=null");
}
#endif
        if (iri != null) {
          outputGraph.Add(new RDFTriple(
              newSubject,
              RDFTerm.fromIRI(iri),
              currentObject));
        }
      }
      types = StringUtility.splitAtNonFFSpaces(rev);
      foreach (var type in types) {
        string iri = getRelTermOrCurie(type,
            iriMapLocal);
        if (iri != null) {
          outputGraph.Add(new RDFTriple(
              currentObject,
              RDFTerm.fromIRI(iri),
              newSubject));
        }
      }
    } else {
      // Step 8
      string[] types = StringUtility.splitAtNonFFSpaces(rel);
      bool hasPredicates = false;
      // Defines predicates
      foreach (var type in types) {
        string iri = getRelTermOrCurie(type,
            iriMapLocal);
        if (iri != null) {
          if (!hasPredicates) {
            hasPredicates = true;
            currentObject = generateBlankNode();
          }
          RDFa.IncompleteTriple inc = new RDFa.IncompleteTriple();
          inc.predicate = RDFTerm.fromIRI(iri);
          inc.direction = RDFa.ChainingDirection.Forward;
          incompleteTriplesLocal.Add(inc);
        }
      }
      types = StringUtility.splitAtNonFFSpaces(rev);
      foreach (var type in types) {
        string iri = getRelTermOrCurie(type,
            iriMapLocal);
        if (iri != null) {
          if (!hasPredicates) {
            hasPredicates = true;
            currentObject = generateBlankNode();
          }
          RDFa.IncompleteTriple inc = new RDFa.IncompleteTriple();
          inc.predicate = RDFTerm.fromIRI(iri);
          inc.direction = RDFa.ChainingDirection.Reverse;
          incompleteTriplesLocal.Add(inc);
        }
      }
    }
    // Step 9
    string[] preds = StringUtility.splitAtNonFFSpaces(property);
    string datatypeValue = getCurie(datatype,
        iriMapLocal);
    if (datatype != null && datatypeValue == null) {
      datatypeValue="";
    }
    //Console.WriteLine("datatype=[%s] prop=%s vocab=%s",
    // datatype, property, localDefaultVocab);
    //Console.WriteLine("datatypeValue=[%s]",datatypeValue);
    RDFTerm currentProperty = null;
    foreach (var pred in preds) {
      string iri = getCurie(pred,
          iriMapLocal);
      if (iri != null) {
        //Console.WriteLine("iri=[%s]",iri);
        currentProperty = null;
        if (datatypeValue != null && datatypeValue.Length>0 &&
            !datatypeValue.Equals(RDF_XMLLITERAL)) {
          string literal = content;
          if (literal == null) {
            literal = getTextNodeText(node);
          }
          currentProperty = RDFTerm.fromTypedString(literal, datatypeValue);
        } else if (node.getAttribute("content")!=null ||
            !hasNonTextChildNodes(node) ||
            (datatypeValue != null && datatypeValue.Length == 0)) {
          string literal=node.getAttribute("content");
          if (literal == null) {
            literal = getTextNodeText(node);
          }
          currentProperty=(!String.IsNullOrEmpty(localLanguage)) ?
              RDFTerm.fromLangString(literal, localLanguage) :
                RDFTerm.fromTypedString(literal);
        } else if (hasNonTextChildNodes(node) &&
            (datatypeValue == null || datatypeValue.Equals(RDF_XMLLITERAL))) {
          // XML literal
          recurse = false;
          if (datatypeValue == null) {
            datatypeValue = RDF_XMLLITERAL;
          }
          try {
            string literal = ExclusiveCanonicalXML.canonicalize(node,
                false, namespacesLocal);
            currentProperty = RDFTerm.fromTypedString(literal, datatypeValue);
          } catch (ArgumentException) {
            // failure to canonicalize
          }
        }
        #if DEBUG
if (!(newSubject != null)) {
 throw new InvalidOperationException("doesn't satisfy newSubject!=null");
}
#endif
        outputGraph.Add(new RDFTriple(
            newSubject,
            RDFTerm.fromIRI(iri),
            currentProperty));
      }
    }
    // Step 10
    if (!skipElement && newSubject != null) {
      foreach (var triple in context.incompleteTriples) {
        if (triple.direction == RDFa.ChainingDirection.Forward) {
          outputGraph.Add(new RDFTriple(
              context.parentSubject,
              triple.predicate,
              newSubject));
        } else {
          outputGraph.Add(new RDFTriple(
              newSubject,
              triple.predicate,
              context.parentSubject));
        }
      }
    }
    // Step 13
    if (recurse) {
      foreach (var childNode in node.getChildNodes()) {
        IElement childElement;
        RDFa.EvalContext oldContext = context;
        if (childNode is IElement) {
          childElement=((IElement)childNode);
          //Console.WriteLine("skip=%s vocab=%s local=%s",
          // skipElement, context.defaultVocab,
          //localDefaultVocab);
          if (skipElement) {
            RDFa.EvalContext ec = oldContext.copy();
            ec.language = localLanguage;
            ec.iriMap = iriMapLocal;
            ec.namespaces = namespacesLocal;
            context = ec;
            process(childElement, false);
          } else {
            RDFa.EvalContext ec = new RDFa.EvalContext();
            ec.baseURI = oldContext.baseURI;
            ec.iriMap = iriMapLocal;
            ec.namespaces = namespacesLocal;
            ec.incompleteTriples = incompleteTriplesLocal;
            ec.parentSubject=((newSubject == null) ? oldContext.parentSubject :
              newSubject);
            ec.parentObject=((currentObject == null) ?
                ((newSubject == null) ? oldContext.parentSubject :
                  newSubject) : currentObject);
            ec.language = localLanguage;
            context = ec;
            process(childElement, false);
          }
        }
        context = oldContext;
      }
    }
  }

  private RDFTerm relativeResolve(string iri) {
    if (iri == null) {
 return null;
}
    return (URIUtility.splitIRI(iri) == null) ? (null) :
      (RDFTerm.fromIRI(URIUtility.relativeResolve(iri, context.baseURI)));
  }
}
}
