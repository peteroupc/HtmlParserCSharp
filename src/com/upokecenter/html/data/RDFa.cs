namespace com.upokecenter.html.data {
using System;
using System.Text;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using com.upokecenter.html;
using com.upokecenter.rdf;
using com.upokecenter.util;


public class RDFa : IRDFParser {

  internal enum ChainingDirection {
    None, Forward, Reverse
  }

  internal class EvalContext {
    public string baseURI;
    public RDFTerm parentSubject;
    public RDFTerm parentObject;
    public string language;
    public IDictionary<string,string> iriMap;
    public IList<IncompleteTriple> incompleteTriples;
    public IDictionary<string,IList<RDFTerm>> listMap;
    public IDictionary<string,string> termMap;
    public IDictionary<string,string> namespaces;
    public string defaultVocab;
    public EvalContext copy(){
      EvalContext ec=new EvalContext();
      ec.baseURI=this.baseURI;
      ec.parentSubject=this.parentSubject;
      ec.parentObject=this.parentObject;
      ec.language=this.language;
      ec.defaultVocab=this.defaultVocab;
      ec.incompleteTriples=new List<IncompleteTriple>(incompleteTriples);
      ec.listMap=(listMap==null) ? null : new PeterO.Support.LenientDictionary<string,IList<RDFTerm>>(listMap);
      ec.namespaces=(namespaces==null) ? null : new PeterO.Support.LenientDictionary<string,string>(namespaces);
      ec.termMap=(termMap==null) ? null : new PeterO.Support.LenientDictionary<string,string>(termMap);
      return ec;
    }
  }

  internal class IncompleteTriple {
    public IList<RDFTerm> list;
    public RDFTerm predicate;
    public ChainingDirection direction;
    public override string ToString() {
      return "IncompleteTriple [list=" + list + ", predicate="
          + predicate + ", direction=" + direction + "]";
    }
  }

  private static readonly string RDFA_DEFAULT_PREFIX = "http://www.w3.org/1999/xhtml/vocab#";
  private static string getTextNodeText(INode node){
    StringBuilder builder=new StringBuilder();
    foreach(var child in node.getChildNodes()){
      if(child.getNodeType()==NodeType.TEXT_NODE){
        builder.Append(((IText)child).getData());
      } else {
        builder.Append(getTextNodeText(child));
      }
    }
    return builder.ToString();
  }
  private static bool isHtmlElement(IElement element, string name){
    return element!=null &&
        "http://www.w3.org/1999/xhtml".Equals(element.getNamespaceURI()) &&
        name.Equals(element.getLocalName());
  }
  private static bool isNCNameChar(int c){
    return (c>='a' && c<='z') ||
        (c>='A' && c<='Z') ||
        c=='_' || c=='.' || c=='-' ||
        (c>='0' && c<='9') ||
        c==0xb7 ||
        (c>=0xc0 && c<=0xd6) ||
        (c>=0xd8 && c<=0xf6) ||
        (c>=0xf8 && c<=0x2ff) ||
        (c>=0x300 && c<=0x37d) ||
        (c>=0x37f && c<=0x1fff) ||
        (c>=0x200c && c<=0x200d) ||
        (c>=0x203f && c<=0x2040) ||
        (c>=0x2070 && c<=0x218f) ||
        (c>=0x2c00 && c<=0x2fef) ||
        (c>=0x3001 && c<=0xd7ff) ||
        (c>=0xf900 && c<=0xfdcf) ||
        (c>=0xfdf0 && c<=0xfffd) ||
        (c>=0x10000 && c<=0xeffff);
  }
  private static bool isNCNameStartChar(int c){
    return (c>='a' && c<='z') ||
        (c>='A' && c<='Z') ||
        c=='_' ||
        (c>=0xc0 && c<=0xd6) ||
        (c>=0xd8 && c<=0xf6) ||
        (c>=0xf8 && c<=0x2ff) ||
        (c>=0x370 && c<=0x37d) ||
        (c>=0x37f && c<=0x1fff) ||
        (c>=0x200c && c<=0x200d) ||
        (c>=0x2070 && c<=0x218f) ||
        (c>=0x2c00 && c<=0x2fef) ||
        (c>=0x3001 && c<=0xd7ff) ||
        (c>=0xf900 && c<=0xfdcf) ||
        (c>=0xfdf0 && c<=0xfffd) ||
        (c>=0x10000 && c<=0xeffff);
  }

  private static bool isTermChar(int c){
    return (c>='a' && c<='z') ||
        (c>='A' && c<='Z') ||
        c=='_' || c=='.' || c=='-' || c=='/' ||
        (c>='0' && c<='9') ||
        c==0xb7 ||
        (c>=0xc0 && c<=0xd6) ||
        (c>=0xd8 && c<=0xf6) ||
        (c>=0xf8 && c<=0x2ff) ||
        (c>=0x300 && c<=0x37d) ||
        (c>=0x37f && c<=0x1fff) ||
        (c>=0x200c && c<=0x200d) ||
        (c>=0x203f && c<=0x2040) ||
        (c>=0x2070 && c<=0x218f) ||
        (c>=0x2c00 && c<=0x2fef) ||
        (c>=0x3001 && c<=0xd7ff) ||
        (c>=0xf900 && c<=0xfdcf) ||
        (c>=0xfdf0 && c<=0xfffd) ||
        (c>=0x10000 && c<=0xeffff);
  }

  private IRDFParser parser;

  private EvalContext context;

  private ISet<RDFTriple> outputGraph;

  private IDocument document;

  private static bool xhtml_rdfa11=false;

  private static readonly RDFTerm RDFA_USES_VOCABULARY=
      RDFTerm.fromIRI("http://www.w3.org/ns/rdfa#usesVocabulary");


  private static readonly string RDF_XMLLITERAL="http://www.w3.org/1999/02/22-rdf-syntax-ns#XMLLiteral";
  private static readonly string[] emptyStringArray=new string[]{};
  private static int getCuriePrefixLength(string s, int offset, int length){
    if(s==null || length==0)return -1;
    if(s[offset]==':')return 0;
    if(!isNCNameStartChar(s[offset]))return -1;
    int index=offset+1;
    int sLength=offset+length;
    while(index<sLength){
      // Get the next Unicode character
      int c=s[index];
      if(c>=0xD800 && c<=0xDBFF && index+1<sLength &&
          s[index+1]>=0xDC00 && s[index+1]<=0xDFFF){
        // Get the Unicode code point for the surrogate pair
        c=0x10000+(c-0xD800)*0x400+(s[index+1]-0xDC00);
        index++;
      } else if(c>=0xD800 && c<=0xDFFF)
        // error
        return -1;
      if(c==':')return index-offset;
      else if(!isNCNameChar(c))return -1;
      index++;
    }
    return -1;
  }

  private static T getValueCaseInsensitive<T>(
      IDictionary<string,T> map,
      string key
      ){
    if(key==null)
      return map[null];
    key=StringUtility.toLowerCaseAscii(key);
    foreach(var k in map.Keys){
      if(key.Equals(StringUtility.toLowerCaseAscii(k)))
        return map[k];
    }
    return default(T);
  }

  private static bool isValidCurieReference(string s, int offset, int length){
    return URIUtility.isValidCurieReference(s, offset, length);
  }
  private static bool isValidTerm(string s){
    if(s==null || s.Length==0)return false;
    if(!isNCNameStartChar(s[0]))return false;
    int index=1;
    int sLength=s.Length;
    while(index<sLength){
      // Get the next Unicode character
      int c=s[index];
      if(c>=0xD800 && c<=0xDBFF && index+1<sLength &&
          s[index+1]>=0xDC00 && s[index+1]<=0xDFFF){
        // Get the Unicode code point for the surrogate pair
        c=0x10000+(c-0xD800)*0x400+(s[index+1]-0xDC00);
        index++;
      } else if(c>=0xD800 && c<=0xDFFF)
        // error
        return false;
      else if(!isTermChar(c))return false;
      index++;
    }
    return true;
  }

  private static string[] splitPrefixList(string s){
    if(s==null || s.Length==0)return emptyStringArray;
    int index=0;
    int sLength=s.Length;
    while(index<sLength){
      char c=s[index];
      if(c!=0x09 && c!=0x0a && c!=0x0d && c!=0x20){
        break;
      }
      index++;
    }
    if(index==s.Length)return emptyStringArray;
    StringBuilder prefix=new StringBuilder();
    StringBuilder iri=new StringBuilder();
    int state=0; // Before NCName state
    List<string> strings=new List<string>();
    while(index<sLength){
      // Get the next Unicode character
      int c=s[index];
      if(c>=0xD800 && c<=0xDBFF && index+1<sLength &&
          s[index+1]>=0xDC00 && s[index+1]<=0xDFFF){
        // Get the Unicode code point for the surrogate pair
        c=0x10000+(c-0xD800)*0x400+(s[index+1]-0xDC00);
        index++;
      } else if(c>=0xD800 && c<=0xDFFF){
        // error
        break;
      }
      if(state==0){ // Before NCName
        if(c==0x09 || c==0x0a || c==0x0d || c==0x20){
          // ignore whitespace
          index++;
        } else if(isNCNameStartChar(c)){
          // start of NCName
          if(c<=0xFFFF){ prefix.Append((char)(c)); }
else {
prefix.Append((char)((((c-0x10000)>>10)&0x3FF)+0xD800));
prefix.Append((char)((((c-0x10000))&0x3FF)+0xDC00));
}
          state=1;
          index++;
        } else {
          // error
          break;
        }
      } else if(state==1){ // NCName
        if(c==':'){
          state=2;
          index++;
        } else if(isNCNameChar(c)){
          // continuation of NCName
          if(c<=0xFFFF){ prefix.Append((char)(c)); }
else {
prefix.Append((char)((((c-0x10000)>>10)&0x3FF)+0xD800));
prefix.Append((char)((((c-0x10000))&0x3FF)+0xDC00));
}
          index++;
        } else {
          // error
          break;
        }
      } else if(state==2){ // After NCName
        if(c==' '){
          state=3;
          index++;
        } else {
          // error
          break;
        }
      } else if(state==3){ // Before IRI
        if(c==' '){
          index++;
        } else {
          // start of IRI
          if(c<=0xFFFF){ iri.Append((char)(c)); }
else {
iri.Append((char)((((c-0x10000)>>10)&0x3FF)+0xD800));
iri.Append((char)((((c-0x10000))&0x3FF)+0xDC00));
}
          state=4;
          index++;
        }
      } else if(state==4){ // IRI
        if(c==0x09 || c==0x0a || c==0x0d || c==0x20){
          string prefixString=StringUtility.toLowerCaseAscii(prefix.ToString());
          // add prefix only if it isn't empty;
          // empty prefixes will not have a mapping
          if(prefixString.Length>0){
            strings.Add(prefixString);
            strings.Add(iri.ToString());
          }
          prefix.Clear();
          iri.Clear();
          state=0;
          index++;
        } else {
          // continuation of IRI
          if(c<=0xFFFF){ iri.Append((char)(c)); }
else {
iri.Append((char)((((c-0x10000)>>10)&0x3FF)+0xD800));
iri.Append((char)((((c-0x10000))&0x3FF)+0xDC00));
}
          index++;
        }
      }
    }
    if(state==4){
      strings.Add(StringUtility.toLowerCaseAscii(prefix.ToString()));
      strings.Add(iri.ToString());
    }
    return strings.ToArray();
  }

  private int blankNode;
  private IDictionary<string,RDFTerm> bnodeLabels=new PeterO.Support.LenientDictionary<string,RDFTerm>();

  public RDFa(IDocument document){
    this.document=document;
    this.parser=null;
    this.context=new EvalContext();
    this.context.defaultVocab=null;
    this.context.baseURI=document.getBaseURI();
    if(!URIUtility.hasScheme(this.context.baseURI))
      throw new ArgumentException("baseURI: "+this.context.baseURI);
    this.context.parentSubject=RDFTerm.fromIRI(this.context.baseURI);
    this.context.parentObject=null;
    this.context.namespaces=new PeterO.Support.LenientDictionary<string,string>();
    this.context.iriMap=new PeterO.Support.LenientDictionary<string,string>();
    this.context.listMap=new PeterO.Support.LenientDictionary<string,IList<RDFTerm>>();
    this.context.termMap=new PeterO.Support.LenientDictionary<string,string>();
    this.context.incompleteTriples=new List<IncompleteTriple>();
    this.context.language=null;
    this.outputGraph=new HashSet<RDFTriple>();
    this.context.termMap.Add("describedby","http://www.w3.org/2007/05/powder-s#describedby");
    this.context.termMap.Add("license","http://www.w3.org/1999/xhtml/vocab#license");
    this.context.termMap.Add("role","http://www.w3.org/1999/xhtml/vocab#role");
    this.context.iriMap.Add("cc","http://creativecommons.org/ns#");
    this.context.iriMap.Add("ctag","http://commontag.org/ns#");
    this.context.iriMap.Add("dc","http://purl.org/dc/terms/");
    this.context.iriMap.Add("dcterms","http://purl.org/dc/terms/");
    this.context.iriMap.Add("dc11","http://purl.org/dc/elements/1.1/");
    this.context.iriMap.Add("foaf","http://xmlns.com/foaf/0.1/");
    this.context.iriMap.Add("gr","http://purl.org/goodrelations/v1#");
    this.context.iriMap.Add("ical","http://www.w3.org/2002/12/cal/icaltzd#");
    this.context.iriMap.Add("og","http://ogp.me/ns#");
    this.context.iriMap.Add("schema","http://schema.org/");
    this.context.iriMap.Add("rev","http://purl.org/stuff/rev#");
    this.context.iriMap.Add("sioc","http://rdfs.org/sioc/ns#");
    this.context.iriMap.Add("grddl","http://www.w3.org/2003/g/data-view#");
    this.context.iriMap.Add("ma","http://www.w3.org/ns/ma-ont#");
    this.context.iriMap.Add("owl","http://www.w3.org/2002/07/owl#");
    this.context.iriMap.Add("prov","http://www.w3.org/ns/prov#");
    this.context.iriMap.Add("rdf","http://www.w3.org/1999/02/22-rdf-syntax-ns#");
    this.context.iriMap.Add("rdfa","http://www.w3.org/ns/rdfa#");
    this.context.iriMap.Add("rdfs","http://www.w3.org/2000/01/rdf-schema#");
    this.context.iriMap.Add("rif","http://www.w3.org/2007/rif#");
    this.context.iriMap.Add("rr","http://www.w3.org/ns/r2rml#");
    this.context.iriMap.Add("sd","http://www.w3.org/ns/sparql-service-description#");
    this.context.iriMap.Add("skos","http://www.w3.org/2004/02/skos/core#");
    this.context.iriMap.Add("skosxl","http://www.w3.org/2008/05/skos-xl#");
    this.context.iriMap.Add("v","http://rdf.data-vocabulary.org/#");
    this.context.iriMap.Add("vcard","http://www.w3.org/2006/vcard/ns#");
    this.context.iriMap.Add("void","http://rdfs.org/ns/void#");
    this.context.iriMap.Add("wdr","http://www.w3.org/2007/05/powder#");
    this.context.iriMap.Add("wdrs","http://www.w3.org/2007/05/powder-s#");
    this.context.iriMap.Add("xhv","http://www.w3.org/1999/xhtml/vocab#");
    this.context.iriMap.Add("xml","http://www.w3.org/XML/1998/_namespace");
    this.context.iriMap.Add("xsd","http://www.w3.org/2001/XMLSchema#");
    IElement docElement=document.getDocumentElement();
    if(docElement!=null && isHtmlElement(docElement,"html")){
      xhtml_rdfa11=true;
      string version=docElement.getAttribute("version");
      if(version!=null && "XHTML+RDFa 1.1".Equals(version)){
        xhtml_rdfa11=true;
        string[] terms=new string[]{
            "alternate","appendix","cite",
            "bookmark","chapter","contents",
            "copyright","first","glossary",
            "help","icon","index","last",
            "license","meta","next","prev",
            "previous","section","start",
            "stylesheet","subsection","top",
            "up","p3pv1"
        };
        foreach(var term in terms){
          this.context.termMap.Add(term,"http://www.w3.org/1999/xhtml/vocab#"+term);
        }
      }
      if(version!=null && "XHTML+RDFa 1.0".Equals(version)){
        parser=new RDFa1(document);
      }
    }
    extraContext();
  }

  private void extraContext(){
    this.context.iriMap.Add("bibo","http://purl.org/ontology/bibo/");
    this.context.iriMap.Add("dbp","http://dbpedia.org/property/");
    this.context.iriMap.Add("dbp-owl","http://dbpedia.org/ontology/");
    this.context.iriMap.Add("dbr","http://dbpedia.org/resource/");
    this.context.iriMap.Add("ex","http://example.org/");
  }

  private RDFTerm generateBlankNode(){
    // Use "//" as the prefix; according to the CURIE syntax,
    // "//" can never begin a valid CURIE reference, so it can
    // be used to guarantee that generated blank nodes will never
    // conflict with those stated explicitly
    string blankNodeString="//"+Convert.ToString(blankNode,CultureInfo.InvariantCulture);
    blankNode++;
    RDFTerm term=RDFTerm.fromBlankNode(blankNodeString);
    bnodeLabels.Add(blankNodeString,term);
    return term;
  }

  private string getCurie(
      string attribute, int offset, int length,
      IDictionary<string,string> prefixMapping) {
    int refIndex=offset;
    int refLength=length;
    int prefix=getCuriePrefixLength(attribute,refIndex,refLength);
    string prefixIri=null;
    if(prefix>=0){
      string prefixName=StringUtility.toLowerCaseAscii(
          attribute.Substring(refIndex,(refIndex+prefix)-(refIndex)));
      refIndex+=(prefix+1);
      refLength-=(prefix+1);
      prefixIri=prefixMapping[prefixName];
      if(prefix==0) {
        prefixIri=RDFA_DEFAULT_PREFIX;
      } else {
        prefixIri=prefixMapping[prefixName];
      }
      if(prefixIri==null || "_".Equals(prefixName))
        return null;
    } else
      // RDFa doesn't define a mapping for an absent prefix
      return null;
    if(!isValidCurieReference(attribute,refIndex,refLength))
      return null;
    if(prefix>=0)
      return relativeResolve(prefixIri+attribute.Substring(refIndex,(refIndex+refLength)-(refIndex))).getValue();
    else
      return null;
  }

  private RDFTerm getCurieOrBnode(
      string attribute, int offset, int length,
      IDictionary<string,string> prefixMapping) {
    int refIndex=offset;
    int refLength=length;
    int prefix=getCuriePrefixLength(attribute,refIndex,refLength);
    string prefixIri=null;
    string prefixName=null;
    if(prefix>=0){
      prefixName=StringUtility.toLowerCaseAscii(
          attribute.Substring(refIndex,(refIndex+prefix)-(refIndex)));
      refIndex+=(prefix+1);
      refLength-=(prefix+1);
      if(prefix==0) {
        prefixIri=RDFA_DEFAULT_PREFIX;
      } else {
        prefixIri=prefixMapping[prefixName];
      }
      if(prefixIri==null && !"_".Equals(prefixName))return null;
    } else
      // RDFa doesn't define a mapping for an absent prefix
      return null;
    if(!isValidCurieReference(attribute,refIndex,refLength))
      return null;
    if(prefix>=0){
      if("_".Equals(prefixName)){
        #if DEBUG
if(!(refIndex>=0 ))throw new InvalidOperationException(attribute);
#endif
        #if DEBUG
if(!(refIndex+refLength<=attribute.Length ))throw new InvalidOperationException(attribute);
#endif
        if(refLength==0)
          // use an empty blank node: the CURIE syntax
          // allows an empty reference; see the comment
          // in generateBlankNode for why "//" appears
          // at the beginning
          return getNamedBlankNode("//empty");
        return getNamedBlankNode(attribute.Substring(refIndex,(refIndex+refLength)-(refIndex)));
      }
      #if DEBUG
if(!(refIndex>=0 ))throw new InvalidOperationException(attribute);
#endif
      #if DEBUG
if(!(refIndex+refLength<=attribute.Length ))throw new InvalidOperationException(attribute);
#endif
      return relativeResolve(prefixIri+attribute.Substring(refIndex,(refIndex+refLength)-(refIndex)));
    } else
      return null;
  }
  private RDFTerm getNamedBlankNode(string str){
    RDFTerm term=RDFTerm.fromBlankNode(str);
    bnodeLabels.Add(str,term);
    return term;
  }

  private RDFTerm getSafeCurieOrCurieOrIri(
      string attribute, IDictionary<string,string> prefixMapping) {
    if(attribute==null)return null;
    int lastIndex=attribute.Length-1;
    if(attribute.Length>=2 && attribute[0]=='[' && attribute[lastIndex]==']'){
      RDFTerm curie=getCurieOrBnode(attribute,1,attribute.Length-2,
          prefixMapping);
      return curie;
    } else {
      RDFTerm curie=getCurieOrBnode(attribute,0,attribute.Length,
          prefixMapping);
      if(curie==null)
        // evaluate as IRI
        return relativeResolve(attribute);
      return curie;
    }
  }

  private string getTermOrCurieOrAbsIri(
      string attribute,
      IDictionary<string,string> prefixMapping,
      IDictionary<string,string> termMapping,
      string defaultVocab) {
    if(attribute==null)return null;
    if(isValidTerm(attribute)){
      if(defaultVocab!=null)
        return relativeResolve(defaultVocab+attribute).getValue();
      else if(termMapping.ContainsKey(attribute))
        return termMapping[attribute];
      else {
        string value=getValueCaseInsensitive(termMapping,attribute);
        return value;
      }
    }
    string curie=getCurie(attribute,0,attribute.Length,
        prefixMapping);
    if(curie==null){
      // evaluate as IRI if it's absolute
      if(URIUtility.hasScheme(attribute))
        //Console.WriteLine("has scheme: %s",attribute);
        return relativeResolve(attribute).getValue();
      return null;
    }
    return curie;
  }

  public ISet<RDFTriple> parse()  {
    if(parser!=null)
      return parser.parse();
    process(document.getDocumentElement(),true);
    RDFInternal.replaceBlankNodes(outputGraph, bnodeLabels);
    return outputGraph;
  }

  private void process(IElement node, bool root){
    IList<IncompleteTriple> incompleteTriplesLocal=new List<IncompleteTriple>();
    string localLanguage=context.language;
    RDFTerm newSubject=null;
    bool skipElement=false;
    RDFTerm currentProperty=null;

    RDFTerm currentObject=null;
    RDFTerm typedResource=null;
    IDictionary<string,string> iriMapLocal=
        new PeterO.Support.LenientDictionary<string,string>(context.iriMap);
    IDictionary<string,string> namespacesLocal=
        new PeterO.Support.LenientDictionary<string,string>(context.namespaces);
    IDictionary<string,IList<RDFTerm>> listMapLocal=context.listMap;
    IDictionary<string,string> termMapLocal=
        new PeterO.Support.LenientDictionary<string,string>(context.termMap);
    string localDefaultVocab=context.defaultVocab;
    string attr=null;
    //Console.WriteLine("cur parobj[%s]=%s",node.getTagName(),context.parentObject);
    //Console.WriteLine("_base=%s",context.baseURI);
    attr=node.getAttribute("xml:base");
    if(attr!=null){
      context.baseURI=URIUtility.relativeResolve(attr, context.baseURI);
    }
    // Support deprecated XML namespaces
    foreach(var attrib in node.getAttributes()){
      string name=StringUtility.toLowerCaseAscii(attrib.getName());
      //Console.WriteLine(attrib);
      if(name.Equals("xmlns")){
        //Console.WriteLine("xmlns %s",attrib.getValue());
        iriMapLocal.Add("", attrib.getValue());
        namespacesLocal.Add("", attrib.getValue());
      } else if(name.StartsWith("xmlns:",StringComparison.Ordinal) && name.Length>6){
        string prefix=name.Substring(6);
        //Console.WriteLine("xmlns %s %s",prefix,attrib.getValue());
        if(!"_".Equals(prefix)){
          iriMapLocal.Add(prefix, attrib.getValue());
        }
        namespacesLocal.Add(prefix, attrib.getValue());
      }
    }
    attr=node.getAttribute("vocab");
    if(attr!=null){
      if(attr.Length==0){
        // set default vocabulary to null
        localDefaultVocab=null;
      } else {
        // set default vocabulary to vocab IRI
        RDFTerm defPrefix=relativeResolve(attr);
        localDefaultVocab=defPrefix.getValue();
        outputGraph.Add(new RDFTriple(
            RDFTerm.fromIRI(context.baseURI),
            RDFA_USES_VOCABULARY,defPrefix
            ));
      }
    }

    attr=node.getAttribute("prefix");
    if(attr!=null){
      string[] prefixList=splitPrefixList(attr);
      for(int i=0;i<prefixList.Length;i+=2){
        // Add prefix and IRI to the map, unless the prefix
        // is "_"
        if(!"_".Equals(prefixList[i])){
          iriMapLocal.Add(prefixList[i], prefixList[i+1]);
        }
      }
    }
    attr=node.getAttribute("lang");
    if(attr!=null){
      localLanguage=attr;
    }
    attr=node.getAttribute("xml:lang");
    if(attr!=null){
      localLanguage=attr;
    }
    string rel=node.getAttribute("rel");
    string rev=node.getAttribute("rev");
    string property=node.getAttribute("property");
    string content=node.getAttribute("content");
    string datatype=node.getAttribute("datatype");
    if(rel==null && rev==null){
      // Step 5
      //Console.WriteLine("%s %s",property,node.getTagName());
      if(property!=null && content==null && datatype==null){
        RDFTerm about=getSafeCurieOrCurieOrIri(
            node.getAttribute("about"),iriMapLocal);
        if(about!=null){
          newSubject=about;
        } else if(root){
          newSubject=getSafeCurieOrCurieOrIri("",iriMapLocal);
        } else if(context.parentObject!=null){
          newSubject=context.parentObject;
        }
        string _typeof=node.getAttribute("typeof");
        if(_typeof!=null){
          if(about!=null){
            typedResource=about;
          } else if(root){
            typedResource=getSafeCurieOrCurieOrIri("",iriMapLocal);
          } else {
            RDFTerm resource=getSafeCurieOrCurieOrIri(
                node.getAttribute("resource"),iriMapLocal);
            if(resource==null){
              resource=relativeResolve(node.getAttribute("href"));
            }
            if(resource==null){
              resource=relativeResolve(node.getAttribute("src"));
            }
            //Console.WriteLine("resource=%s",resource);
            if((resource==null || resource.getKind()!=RDFTerm.IRI) &&
                xhtml_rdfa11){
              if(isHtmlElement(node, "head") ||
                  isHtmlElement(node, "body")){
                newSubject=context.parentObject;
              }
            }
            if(resource==null){
              typedResource=generateBlankNode();
            } else {
              typedResource=resource;
            }
            currentObject=typedResource;
          }
        }
      } else {
        RDFTerm resource=getSafeCurieOrCurieOrIri(
            node.getAttribute("about"),iriMapLocal);
        if(resource==null){
          resource=getSafeCurieOrCurieOrIri(
              node.getAttribute("resource"),iriMapLocal);
          //Console.WriteLine("resource=%s %s %s",
          //  node.getAttribute("resource"),
          //resource,context.parentObject);
        }
        if(resource==null){
          resource=relativeResolve(node.getAttribute("href"));
        }
        if(resource==null){
          resource=relativeResolve(node.getAttribute("src"));
        }
        if((resource==null || resource.getKind()!=RDFTerm.IRI) &&
            xhtml_rdfa11){
          if(isHtmlElement(node, "head") ||
              isHtmlElement(node, "body")){
            resource=context.parentObject;
          }
        }
        if(resource==null){
          if(root){
            newSubject=getSafeCurieOrCurieOrIri("",iriMapLocal);
          } else if(node.getAttribute("typeof")!=null){
            newSubject=generateBlankNode();
          } else {
            if(context.parentObject!=null) {
              newSubject=context.parentObject;
            }
            if(node.getAttribute("property")==null){
              skipElement=true;
            }
          }
        } else {
          newSubject=resource;
        }
        if(node.getAttribute("typeof")!=null){
          typedResource=newSubject;
        }
      }
    } else {
      // Step 6
      RDFTerm about=getSafeCurieOrCurieOrIri(
          node.getAttribute("about"),iriMapLocal);
      if(about!=null){
        newSubject=about;
      }
      if(node.getAttribute("typeof")!=null){
        typedResource=newSubject;
      }
      if(about==null){
        if(root){
          about=getSafeCurieOrCurieOrIri("",iriMapLocal);
        } else if(context.parentObject!=null){
          newSubject=context.parentObject;
        }
      }
      RDFTerm resource=getSafeCurieOrCurieOrIri(
          node.getAttribute("resource"),iriMapLocal);
      if(resource==null){
        resource=relativeResolve(node.getAttribute("href"));
      }
      if(resource==null){
        resource=relativeResolve(node.getAttribute("src"));
      }
      if((resource==null || resource.getKind()!=RDFTerm.IRI) &&
          xhtml_rdfa11){
        if(isHtmlElement(node, "head") ||
            isHtmlElement(node, "body")){
          newSubject=context.parentObject;
        }
      }
      if(resource==null && node.getAttribute("typeof")!=null &&
          node.getAttribute("about")==null){
        currentObject=generateBlankNode();
      } else if(resource!=null){
        currentObject=resource;
      }
      if(node.getAttribute("typeof")!=null &&
          node.getAttribute("about")==null){
        typedResource=currentObject;
      }
    }
    // Step 7
    if(typedResource!=null){
      string[] types=StringUtility.splitAtNonFFSpaces(node.getAttribute("typeof"));
      foreach(var type in types){
        string iri=getTermOrCurieOrAbsIri(type,
            iriMapLocal,termMapLocal,localDefaultVocab);
        if(iri!=null){
          outputGraph.Add(new RDFTriple(
              typedResource,RDFTerm.A,
              RDFTerm.fromIRI(iri)
              ));
        }
      }
    }
    // Step 8
    if(newSubject!=null && !newSubject.Equals(context.parentObject)){
      context.listMap.Clear();
    }
    // Step 9
    if(currentObject!=null){
      string inlist=node.getAttribute("inlist");
      if(inlist!=null && rel!=null){
        string[] types=StringUtility.splitAtNonFFSpaces(rel);
        foreach(var type in types){
          string iri=getTermOrCurieOrAbsIri(type,
              iriMapLocal,termMapLocal,localDefaultVocab);
          if(iri!=null){
            if(!listMapLocal.ContainsKey(iri)){
              IList<RDFTerm> newList=new List<RDFTerm>();
              newList.Add(currentObject);
              listMapLocal.Add(iri,newList);
            } else {
              IList<RDFTerm> existingList=listMapLocal[iri];
              existingList.Add(currentObject);
            }
          }
        }
      } else {
        string[] types=StringUtility.splitAtNonFFSpaces(rel);
        #if DEBUG
if(!(newSubject!=null))throw new InvalidOperationException("doesn't satisfy newSubject!=null");
#endif
        foreach(var type in types){
          string iri=getTermOrCurieOrAbsIri(type,
              iriMapLocal,termMapLocal,localDefaultVocab);
          if(iri!=null){
            outputGraph.Add(new RDFTriple(
                newSubject,
                RDFTerm.fromIRI(iri),currentObject
                ));
          }
        }
        types=StringUtility.splitAtNonFFSpaces(rev);
        foreach(var type in types){
          string iri=getTermOrCurieOrAbsIri(type,
              iriMapLocal,termMapLocal,localDefaultVocab);
          if(iri!=null){
            outputGraph.Add(new RDFTriple(
                currentObject,
                RDFTerm.fromIRI(iri),
                newSubject
                ));
          }
        }
      }
    } else {
      // Step 10
      string[] types=StringUtility.splitAtNonFFSpaces(rel);
      bool inlist=(node.getAttribute("inlist"))!=null;
      bool hasPredicates=false;
      // Defines predicates
      foreach(var type in types){
        string iri=getTermOrCurieOrAbsIri(type,
            iriMapLocal,termMapLocal,localDefaultVocab);
        if(iri!=null){
          if(!hasPredicates){
            hasPredicates=true;
            currentObject=generateBlankNode();
          }
          IncompleteTriple inc=new IncompleteTriple();
          if(inlist){
            if(!listMapLocal.ContainsKey(iri)){
              IList<RDFTerm> newList=new List<RDFTerm>();
              listMapLocal.Add(iri, newList);
              //NOTE: Should not be a copy
              inc.list=newList;
            } else {
              IList<RDFTerm> existingList=listMapLocal[iri];
              inc.list=existingList;
            }
            inc.direction=ChainingDirection.None;
          } else {
            inc.predicate=RDFTerm.fromIRI(iri);
            inc.direction=ChainingDirection.Forward;
          }
          //Console.WriteLine(inc);
          incompleteTriplesLocal.Add(inc);
        }
      }
      types=StringUtility.splitAtNonFFSpaces(rev);
      foreach(var type in types){
        string iri=getTermOrCurieOrAbsIri(type,
            iriMapLocal,termMapLocal,localDefaultVocab);
        if(iri!=null){
          if(!hasPredicates){
            hasPredicates=true;
            currentObject=generateBlankNode();
          }
          IncompleteTriple inc=new IncompleteTriple();
          inc.predicate=RDFTerm.fromIRI(iri);
          inc.direction=ChainingDirection.Reverse;
          incompleteTriplesLocal.Add(inc);
        }
      }
    }
    // Step 11
    string[] preds=StringUtility.splitAtNonFFSpaces(property);
    string datatypeValue=getTermOrCurieOrAbsIri(datatype,
        iriMapLocal,termMapLocal,localDefaultVocab);
    if(datatype!=null && datatypeValue==null) {
      datatypeValue="";
    }
    //Console.WriteLine("datatype=[%s] prop=%s vocab=%s",
    //  datatype,property,localDefaultVocab);
    //Console.WriteLine("datatypeValue=[%s]",datatypeValue);
    foreach(var pred in preds){
      string iri=getTermOrCurieOrAbsIri(pred,
          iriMapLocal,termMapLocal,localDefaultVocab);
      if(iri!=null){
        //Console.WriteLine("iri=[%s]",iri);
        currentProperty=null;
        if(datatypeValue!=null && datatypeValue.Length>0 &&
            !datatypeValue.Equals(RDF_XMLLITERAL)){
          string literal=content;
          if(literal==null) {
            literal=getTextNodeText(node);
          }
          currentProperty=RDFTerm.fromTypedString(literal,datatypeValue);
        } else if(datatypeValue!=null && datatypeValue.Length==0){
          string literal=content;
          if(literal==null) {
            literal=getTextNodeText(node);
          }
          currentProperty=(!string.IsNullOrEmpty(localLanguage)) ?
              RDFTerm.fromLangString(literal, localLanguage) :
                RDFTerm.fromTypedString(literal);
        } else if(datatypeValue!=null && datatypeValue.Equals(RDF_XMLLITERAL)){
          // XML literal
          try {
            string literal=ExclusiveCanonicalXML.canonicalize(node,
                false, namespacesLocal);
            currentProperty=RDFTerm.fromTypedString(literal,datatypeValue);
          } catch(ArgumentException){
            // failure to canonicalize
          }
        } else if(content!=null){
          string literal=content;
          currentProperty=(!string.IsNullOrEmpty(localLanguage)) ?
              RDFTerm.fromLangString(literal, localLanguage) :
                RDFTerm.fromTypedString(literal);
        } else if(rel==null && content==null && rev==null){
          RDFTerm resource=getSafeCurieOrCurieOrIri(
              node.getAttribute("resource"),iriMapLocal);
          if(resource==null){
            resource=relativeResolve(node.getAttribute("href"));
          }
          if(resource==null){
            resource=relativeResolve(node.getAttribute("src"));
          }
          if(resource!=null){
            currentProperty=resource;
          }
        }
        if(currentProperty==null){
          if(node.getAttribute("typeof")!=null &&
              node.getAttribute("about")==null){
            currentProperty=typedResource;
          } else {
            string literal=content;
            if(literal==null) {
              literal=getTextNodeText(node);
            }
            currentProperty=(!string.IsNullOrEmpty(localLanguage)) ?
                RDFTerm.fromLangString(literal, localLanguage) :
                  RDFTerm.fromTypedString(literal);
          }
        }
        //Console.WriteLine("curprop: %s",currentProperty);
        if(node.getAttribute("inlist")!=null){
          if(!listMapLocal.ContainsKey(iri)){
            IList<RDFTerm> newList=new List<RDFTerm>();
            newList.Add(currentProperty);
            listMapLocal.Add(iri,newList);
          } else {
            IList<RDFTerm> existingList=listMapLocal[iri];
            existingList.Add(currentProperty);
          }
        } else {
          #if DEBUG
if(!(newSubject!=null))throw new InvalidOperationException("doesn't satisfy newSubject!=null");
#endif
          outputGraph.Add(new RDFTriple(
              newSubject,
              RDFTerm.fromIRI(iri),currentProperty
              ));
        }
      }
    }
    // Step 12
    if(!skipElement && newSubject!=null){
      foreach(var triple in context.incompleteTriples){
        if(triple.direction==ChainingDirection.None){
          IList<RDFTerm> list=triple.list;
          list.Add(newSubject);
        } else if(triple.direction==ChainingDirection.Forward){
          outputGraph.Add(new RDFTriple(
              context.parentSubject,
              triple.predicate,
              newSubject));
        } else {
          outputGraph.Add(new RDFTriple(
              newSubject,triple.predicate,
              context.parentSubject));
        }
      }
    }
    // Step 13
    foreach(var childNode in node.getChildNodes()){
      IElement childElement;
      EvalContext oldContext=context;
      if(childNode is IElement){
        childElement=((IElement)childNode);
        //Console.WriteLine("skip=%s vocab=%s local=%s",
        //  skipElement,context.defaultVocab,
        //localDefaultVocab);
        if(skipElement){
          EvalContext ec=oldContext.copy();
          ec.language=localLanguage;
          ec.iriMap=iriMapLocal;
          context=ec;
          process(childElement,false);
        } else {
          EvalContext ec=new EvalContext();
          ec.baseURI=oldContext.baseURI;
          ec.namespaces=namespacesLocal;
          ec.iriMap=iriMapLocal;
          ec.incompleteTriples=incompleteTriplesLocal;
          ec.listMap=listMapLocal;
          ec.termMap=termMapLocal;
          ec.parentSubject=((newSubject==null) ? oldContext.parentSubject :
            newSubject);
          ec.parentObject=((currentObject==null) ?
              ((newSubject==null) ? oldContext.parentSubject :
                newSubject) : currentObject);
          ec.defaultVocab=localDefaultVocab;
          ec.language=localLanguage;
          context=ec;
          process(childElement,false);
        }
      }
      context=oldContext;
    }
    // Step 14
    foreach(var iri in listMapLocal.Keys){
      if(!context.listMap.ContainsKey(iri)){
        IList<RDFTerm> list=listMapLocal[iri];
        if(list.Count==0){
          outputGraph.Add(new RDFTriple(
              (newSubject==null ? newSubject : context.parentSubject),
              RDFTerm.fromIRI(iri),RDFTerm.NIL
              ));
        } else {
          RDFTerm bnode=generateBlankNode();
          outputGraph.Add(new RDFTriple(
              (newSubject==null ? newSubject : context.parentSubject),
              RDFTerm.fromIRI(iri),bnode
              ));
          for(int i=0;i<list.Count;i++){
            RDFTerm nextBnode=(i==list.Count-1) ?
                generateBlankNode() : RDFTerm.NIL;
                outputGraph.Add(new RDFTriple(
                    bnode,RDFTerm.FIRST,list[i]
                    ));
                outputGraph.Add(new RDFTriple(
                    bnode,RDFTerm.REST,nextBnode
                    ));
                bnode=nextBnode;
          }
        }
      }
    }
  }

  private RDFTerm relativeResolve(string iri){
    if(iri==null)return null;
    if(URIUtility.splitIRI(iri)==null)
      return null;
    return RDFTerm.fromIRI(URIUtility.relativeResolve(iri, context.baseURI));
  }
}

}
