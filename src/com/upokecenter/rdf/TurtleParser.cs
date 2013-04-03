namespace com.upokecenter.rdf {
using System;

using System.IO;

using System.Collections.Generic;






using com.upokecenter.util;




/**
 * 
 * A parser for Turtle, a syntax for writing
 * Resource Description Framework (RDF) graphs
 * in text.
 * 
 * For more information, visit http://www.w3.org/TR/turtle/
 * 
 * @author Peter
 *
 */
public class TurtleParser : IRDFParser {

	private IDictionary<string,RDFTerm> bnodeLabels;
	private IDictionary<string,string> namespaces;
	private string baseURI;
	private TurtleObject curSubject;
	private RDFTerm curPredicate;

	private StackableCharacterInput input;

	public TurtleParser(PeterO.Support.InputStream stream, string baseURI){
		if((stream)==null)throw new ArgumentNullException("stream");
		if(baseURI==null)throw new ArgumentNullException("baseURI");
		if(!UriResolver.hasScheme(baseURI))
			throw new ArgumentException("baseURI");
		this.input=new StackableCharacterInput(
				new Utf8CharacterInput(stream));
		this.baseURI=baseURI;
		bnodeLabels=new PeterO.Support.LenientDictionary<string,RDFTerm>();
		namespaces=new PeterO.Support.LenientDictionary<string,string>();
	}

	public TurtleParser(PeterO.Support.InputStream stream) : this(stream,"about:blank") {
	}


	private static bool isChar(int c, string asciiChars){
		return (c>=0 && c<=0x7F && asciiChars.IndexOf((char)c)>=0);
	}
	public TurtleParser(string str, string baseURI){
		if((str)==null)throw new ArgumentNullException("str");
		if(baseURI==null)throw new ArgumentNullException("baseURI");
		if(!UriResolver.hasScheme(baseURI))
			throw new ArgumentException("baseURI");
		this.input=new StackableCharacterInput(
				new StringCharacterInput(str,true));
		this.baseURI=baseURI;
		bnodeLabels=new PeterO.Support.LenientDictionary<string,RDFTerm>();
		namespaces=new PeterO.Support.LenientDictionary<string,string>();
	}

	public TurtleParser(string str) : this(str,"about:blank") {
	}

	private bool skipWhitespace()  {
		bool haveWhitespace=false;
		input.setSoftMark();
		while(true){
			int ch=input.read();
			if(ch=='#'){
				while(true){
					ch=input.read();
					if(ch<0)return true;
					if(ch==0x0d || ch==0x0a) {
						break;
					}
				}
			} else if(ch!=0x09 && ch!=0x0a && ch!=0x0d && ch!=0x20){
				if(ch>=0) {
					input.moveBack(1);
				}
				return haveWhitespace;
			}
			haveWhitespace=true;
		}
	}

	private TurtleObject readCollection()  {
		TurtleObject obj=TurtleObject.newCollection();
		while(true){
			skipWhitespace();
			input.setHardMark();
			int ch=input.read();
			if(ch==')'){
				break;
			} else {
				if(ch>=0) {
					input.moveBack(1);
				}
				TurtleObject subobj=readObject(true);
				obj.getObjects().Add(subobj);
			}
		}
		return obj;
	}

	private class TurtleProperty {
		public RDFTerm pred;
		public TurtleObject obj;
	}
	private class TurtleObject {
		public static readonly int SIMPLE=0;
		public static readonly int COLLECTION=1;
		public static readonly int PROPERTIES=2;
		public RDFTerm term;
		public int kind;
		IList<TurtleObject> objects;
		IList<TurtleProperty> properties;

		public IList<TurtleObject> getObjects(){
			return objects;
		}
		public IList<TurtleProperty> getProperties(){
			return properties;
		}

		public static TurtleObject fromTerm(RDFTerm term){
			TurtleObject tobj=new TurtleObject();
			tobj.term=term;
			tobj.kind=TurtleObject.SIMPLE;
			return tobj;
		}
		public static TurtleObject newPropertyList(){
			TurtleObject tobj=new TurtleObject();
			tobj.properties=new List<TurtleProperty>();
			tobj.kind=TurtleObject.PROPERTIES;
			return tobj;
		}
		public static TurtleObject newCollection(){
			TurtleObject tobj=new TurtleObject();
			tobj.objects=new List<TurtleObject>();
			tobj.kind=TurtleObject.COLLECTION;
			return tobj;
		}
	}

	private TurtleObject readObject(bool acceptLiteral)  {
		int ch=input.read();
		int mark=input.setSoftMark();
		if(ch<0)
			throw new ParserException();
		else if(ch=='<')
			return TurtleObject.fromTerm(
					RDFTerm.fromIRI(readIriReference()));
		else if(acceptLiteral && (ch=='-' || ch=='+' || ch=='.' || (ch>='0' && ch<='9')))
			return TurtleObject.fromTerm(readNumberLiteral(ch));
		else if(acceptLiteral && (ch=='\'' || ch=='\"')){ // start of quote literal
			string str=readStringLiteral(ch);
			return TurtleObject.fromTerm(finishStringLiteral(str));
		} else if(ch=='_'){ // Blank Node Label
			if(input.read()!=':')
				throw new ParserException();
			string label=readBlankNodeLabel();
			RDFTerm term=bnodeLabels[label];
			if(term==null){
				term=RDFTerm.fromBlankNode(label);
				bnodeLabels.Add(label,term);
			}
			return TurtleObject.fromTerm(term);
		} else if(ch=='[')
			return readBlankNodePropertyList();
		else if(ch=='(')
			return readCollection();
		else if(ch==':'){ // prefixed name with current prefix
			string scope=namespaces[""];
			if(scope==null)throw new ParserException();
			return TurtleObject.fromTerm(
					RDFTerm.fromIRI(scope+readOptionalLocalName()));
		} else if(isNameStartChar(ch)){ // prefix
			if(acceptLiteral && (ch=='t' || ch=='f')){
				mark=input.setHardMark();
				if(ch=='t' && input.read()=='r' && input.read()=='u' &&
						input.read()=='e' && skipWhitespace())
					return TurtleObject.fromTerm(RDF_TRUE);
				else if(ch=='f' && input.read()=='a' && input.read()=='l' &&
						input.read()=='s' && input.read()=='e' && skipWhitespace())
					return TurtleObject.fromTerm(RDF_FALSE);
				else {
					input.setMarkPosition(mark);
				}
			}
			string prefix=readPrefix(ch);
			string scope=namespaces[prefix];
			if(scope==null)throw new ParserException();
			return TurtleObject.fromTerm(
					RDFTerm.fromIRI(scope+readOptionalLocalName()));
		} else {
			input.setMarkPosition(mark);
			return null;
		}
	}

	private RDFTerm finishStringLiteral(string str)  {
		int mark=input.setHardMark();
		int ch=input.read();
		if(ch=='@')
			return RDFTerm.fromLangString(str,readLanguageTag());
		else if(ch=='^' && input.read()=='^'){
			ch=input.read();
			if(ch=='<')
				return RDFTerm.fromTypedString(str,readIriReference());
			else if(ch==':'){ // prefixed name with current prefix
				string scope=namespaces[""];
				if(scope==null)throw new ParserException();
				return RDFTerm.fromTypedString(str,scope+readOptionalLocalName());
			} else if(isNameStartChar(ch)){ // prefix
				string prefix=readPrefix(ch);
				string scope=namespaces[prefix];
				if(scope==null)throw new ParserException();
				return RDFTerm.fromTypedString(str,scope+readOptionalLocalName());
			} else throw new ParserException();
		} else {
			input.setMarkPosition(mark);
			return RDFTerm.fromTypedString(str);
		}
	}

	private string readLanguageTag()  {
		IntList ilist=new IntList();
		bool hyphen=false;
		bool haveHyphen=false;
		bool haveString=false;
		input.setSoftMark();
		while(true){
			int c2=input.read();
			if(c2>='A' && c2<='Z'){
				ilist.appendInt(c2);
				haveString=true;
				hyphen=false;
			} else if(c2>='a' && c2<='z'){
				ilist.appendInt(c2);
				haveString=true;
				hyphen=false;
			} else if(haveHyphen && (c2>='0' && c2<='9')){
				ilist.appendInt(c2);
				haveString=true;
				hyphen=false;
			} else if(c2=='-'){
				if(hyphen||!haveString)throw new ParserException();
				ilist.appendInt(c2);
				hyphen=true;
				haveHyphen=true;
				haveString=true;
			} else {
				if(c2>=0) {
					input.moveBack(1);
				}
				if(hyphen||!haveString)throw new ParserException();
				return ilist.ToString();
			}
		}
	}

	private string readStringLiteral(int ch)  {
		IntList ilist=new IntList();
		bool first=true;
		bool longQuote=false;
		int quotecount=0;
		while(true){
			int c2=input.read();
			if(first && c2==ch){
				input.setHardMark();
				c2=input.read();
				if(c2!=ch){
					if(c2>=0) {
						input.moveBack(1);
					}
					return "";
				}
				longQuote=true;
				c2=input.read();
			}
			first=false;
			if(!longQuote && (c2==0x0a || c2==0x0d))
				throw new ParserException();
			else if(c2=='\\'){
				c2=readUnicodeEscape(true);
				if(quotecount>=2) {
					ilist.appendInt(ch);
				}
				if(quotecount>=1) {
					ilist.appendInt(ch);
				}
				ilist.appendInt(c2);
				quotecount=0;
			} else if(c2==ch){
				if(!longQuote)
					return ilist.ToString();
				quotecount++;
				if(quotecount>=3)
					return ilist.ToString();
			} else {
				if(c2<0)
					throw new ParserException();
				if(quotecount>=2) {
					ilist.appendInt(ch);
				}
				if(quotecount>=1) {
					ilist.appendInt(ch);
				}
				ilist.appendInt(c2);
				quotecount=0;
			}
		}
	}

	// Reads a number literal starting with
	// the given character (assumes it's plus, minus,
	// a dot, or a digit)
	private RDFTerm readNumberLiteral(int ch)  {
		// buffer to hold the literal
		IntList ilist=new IntList();
		// include the first character
		ilist.appendInt(ch);
		bool haveDigits=(ch>='0' && ch<='9');
		bool haveDot=(ch=='.');
		input.setHardMark();
		while(true){
			int ch1=input.read();
			if(haveDigits && (ch1=='e' || ch1=='E')){
				// Parse exponent
				ilist.appendInt(ch1);
				ch1=input.read();
				haveDigits=false;
				if(ch1=='+' || ch1=='-' || (ch1>='0' && ch1<='9')){
					ilist.appendInt(ch1);
					if(ch1>='0' && ch1<='9') {
						haveDigits=true;
					}
				} else
					throw new ParserException();
				input.setHardMark();
				while(true){
					ch1=input.read();
					if(ch1>='0' && ch1<='9'){
						haveDigits=true;
						ilist.appendInt(ch1);
					} else {
						if(ch1>=0) {
							input.moveBack(1);
						}
						if(!haveDigits)throw new ParserException();
						return RDFTerm.fromTypedString(ilist.ToString(),
								"http://www.w3.org/2001/XMLSchema#double");
					}
				}
			} else if(ch1>='0' && ch1<='9'){
				haveDigits=true;
				ilist.appendInt(ch1);
			} else if(!haveDot && ch1=='.'){
				haveDot=true;
				// check for non-digit and non-E
				int markpos=input.getMarkPosition();
				int ch2=input.read();
				if(ch2!='e' && ch2!='E' && (ch2<'0' || ch2>'9')){
					// move to just at the period and return
					input.setMarkPosition(markpos-1);
					if(!haveDigits)
						throw new ParserException();
					return RDFTerm.fromTypedString(ilist.ToString(),
							haveDot ? "http://www.w3.org/2001/XMLSchema#decimal" :
							"http://www.w3.org/2001/XMLSchema#integer");
				} else {
					input.moveBack(1);
				}
				ilist.appendInt(ch1);
			} else { // no more digits
				if(ch1>=0) {
					input.moveBack(1);
				}
				if(!haveDigits)
					throw new ParserException();
				return RDFTerm.fromTypedString(ilist.ToString(),
						haveDot ? "http://www.w3.org/2001/XMLSchema#decimal" :
						"http://www.w3.org/2001/XMLSchema#integer");
			}
		}
	}

	private string readBlankNodeLabel()  {
		IntList ilist=new IntList();
		int startChar=input.read();
		if(!isNameStartCharU(startChar) &&
				(startChar<'0' || startChar>'9'))
			throw new ParserException();
		ilist.appendInt(startChar);
		bool lastIsPeriod=false;
		input.setSoftMark();
		while(true){
			int ch=input.read();
			if(ch=='.'){
				int position=input.getMarkPosition();
				int ch2=input.read();
				if(!isNameChar(ch2) && ch2!=':' && ch2!='.'){
					input.setMarkPosition(position-1);
					return ilist.ToString();
				} else {
					input.moveBack(1);
				}
				ilist.appendInt(ch);
				lastIsPeriod=true;
			} else if(isNameChar(ch)){
				ilist.appendInt(ch);
				lastIsPeriod=false;
			} else {
				if(ch>=0) {
					input.moveBack(1);
				}
				if(lastIsPeriod)
					throw new ParserException();
				return ilist.ToString();
			}
		}
	}

	private string readIriReference()  {
		IntList ilist=new IntList();
		while(true){
			int ch=input.read();
			if(ch<0)
				throw new ParserException();
			if(ch=='>'){
				string iriref=ilist.ToString();
				// Resolve the IRI reference relative
				// to the _base URI
				iriref=UriResolver.relativeResolve(iriref, baseURI);
				return iriref;
			}
			else if(ch=='\\'){
				ch=readUnicodeEscape(false);
			}
			if(ch<=0x20 || isChar(ch, "><\\\"{}|^`"))
				throw new ParserException();
			ilist.appendInt(ch);
		}
	}
	private string readPrefix(int startChar)  {
		IntList ilist=new IntList();
		bool lastIsPeriod=false;
		bool first=true;
		if(startChar>=0){
			ilist.appendInt(startChar);
			first=false;
		}
		while(true){
			int ch=input.read();
			if(ch<0)
				throw new ParserException();
			if(ch==':'){
				if(lastIsPeriod)
					throw new ParserException();
				return ilist.ToString();
			}
			else if(first && !isNameStartChar(ch))
				throw new ParserException();
			else if(ch!='.' && !isNameChar(ch))
				throw new ParserException();
			first=false;
			ilist.appendInt(ch);
			lastIsPeriod=(ch=='.');
		}
	}

	private int readUnicodeEscape(bool extended)  {
		int ch=input.read();
		if(ch=='U'){
			if(input.read()!='0')
				throw new ParserException();
			if(input.read()!='0')
				throw new ParserException();
			int a=toHexValue(input.read());
			int b=toHexValue(input.read());
			int c=toHexValue(input.read());
			int d=toHexValue(input.read());
			int e=toHexValue(input.read());
			int f=toHexValue(input.read());
			if(a<0||b<0||c<0||d<0||e<0||f<0)
				throw new ParserException();
			ch=(a<<20)|(b<<16)|(c<<12)|(d<<8)|(e<<4)|(f);
		} else if(ch=='u'){
			int a=toHexValue(input.read());
			int b=toHexValue(input.read());
			int c=toHexValue(input.read());
			int d=toHexValue(input.read());
			if(a<0||b<0||c<0||d<0)
				throw new ParserException();
			ch=(a<<12)|(b<<8)|(c<<4)|(d);
		} else if(extended && ch=='t')
			return '\t';
		else if(extended && ch=='b')
			return '\b';
		else if(extended && ch=='n')
			return '\n';
		else if(extended && ch=='r')
			return '\r';
		else if(extended && ch=='f')
			return '\f';
		else if(extended && ch=='\'')
			return '\'';
		else if(extended && ch=='\\')
			return '\\';
		else if(extended && ch=='"')
			return '\"';
		else throw new ParserException();
		// Reject surrogate code points
		// as Unicode escapes
		if(ch>=0xD800 && ch<=0xDFFF)
			throw new ParserException();
		return ch;
	}

	private void readObjectList(ISet<RDFTriple> triples)  {
		bool haveObject=false;
		while(true){
			input.setSoftMark();
			int ch;
			if(haveObject){
				ch=input.read();
				if(ch!=','){
					if(ch>=0) {
						input.moveBack(1);
					}
					break;
				}
				skipWhitespace();
			}
			// Read _object
			TurtleObject obj=readObject(true);
			if(obj==null){
				if(!haveObject)
					throw new ParserException();
				else
					return;
			}
			haveObject=true;
			emitRDFTriple(curSubject,curPredicate,obj,triples);
			skipWhitespace();
		}
		if(!haveObject)
			throw new ParserException();
		return;
	}

	private void readObjectListToProperties(
			RDFTerm predicate,
			TurtleObject propertyList
			)  {
		bool haveObject=false;
		while(true){
			input.setSoftMark();
			int ch;
			if(haveObject){
				ch=input.read();
				if(ch!=','){
					if(ch>=0) {
						input.moveBack(1);
					}
					break;
				}
				skipWhitespace();
			}
			// Read _object
			TurtleObject obj=readObject(true);
			if(obj==null){
				if(!haveObject)
					throw new ParserException();
				else
					return;
			}
			TurtleProperty prop=new TurtleProperty();
			prop.pred=predicate;
			prop.obj=obj;
			propertyList.getProperties().Add(prop);
			skipWhitespace();
			haveObject=true;
		}
		if(!haveObject)
			throw new ParserException();
		return;
	}
	private void readTriples(ISet<RDFTriple> triples)  {
		int mark=input.setHardMark();
		int ch=input.read();
		if(ch<0)
			return;
		input.setMarkPosition(mark);
		TurtleObject subject=readObject(false);
		if(subject==null)
			throw new ParserException();
		curSubject=subject;
		if(!(subject.kind==TurtleObject.PROPERTIES &&
				subject.getProperties().Count>0)){
			skipWhitespace();
			readPredicateObjectList(triples);
		} else {
			skipWhitespace();
			input.setHardMark();
			ch=input.read();
			if(ch=='.'){
				// just a blank node property list;
				// generate a blank node as the subject
				RDFTerm blankNode=allocateBlankNode();
				foreach(TurtleProperty prop in subject.getProperties()){
					emitRDFTriple(blankNode,prop.pred,prop.obj,triples);
				}
				return;
			} else if(ch<0)
				throw new ParserException();
			input.moveBack(1);
			readPredicateObjectList(triples);
		}
		skipWhitespace();
		if(input.read()!='.')
			throw new ParserException();
	}

	private string readOptionalLocalName()  {
		IntList ilist=new IntList();
		bool lastIsPeriod=false;
		bool first=true;
		input.setSoftMark();
		while(true){
			int ch=input.read();
			if(ch<0)
				return ilist.ToString();
			if(ch=='%'){
				int a=input.read();
				int b=input.read();
				if(toHexValue(a)<0 ||
						toHexValue(b)<0)throw new ParserException();
				ilist.appendInt(ch);
				ilist.appendInt(a);
				ilist.appendInt(b);
				lastIsPeriod=false;
				first=false;
				continue;
			} else if(ch=='\\'){
				ch=input.read();
				if(isChar(ch,"_~.-!$&'()*+,;=/?#@%")){
					ilist.appendInt(ch);
				} else throw new ParserException();
				lastIsPeriod=false;
				first=false;
				continue;
			}
			if(first){
				if(!isNameStartCharU(ch) && ch!=':' && (ch<'0' || ch>'9')){
					input.moveBack(1);
					return ilist.ToString();
				}
			} else {
				if(!isNameChar(ch) && ch!=':' && ch!='.'){
					input.moveBack(1);
					if(lastIsPeriod)throw new ParserException();
					return ilist.ToString();
				}
			}
			lastIsPeriod=(ch=='.');
			if(lastIsPeriod && !first){
				// if a period was just read, check
				// if the next character is valid before
				// adding the period.
				int position=input.getMarkPosition();
				int ch2=input.read();
				if(!isNameChar(ch2) && ch2!=':' && ch2!='.'){
					input.setMarkPosition(position-1);
					return ilist.ToString();
				} else {
					input.moveBack(1);
				}
			}
			first=false;
			ilist.appendInt(ch);
		}

	}

	private int toHexValue(int a) {
		if(a>='0' && a<='9')return a-'0';
		if(a>='a' && a<='f')return a+10-'a';
		if(a>='A' && a<='F')return a+10-'A';
		return -1;
	}

	private void emitRDFTriple(RDFTerm subj, RDFTerm pred, RDFTerm obj, ISet<RDFTriple> triples){
		RDFTriple triple=new RDFTriple(subj,pred,obj);
		triples.Add(triple);
	}

	private RDFTerm readPredicate()  {
		int mark=input.setHardMark();
		int ch=input.read();
		RDFTerm predicate=null;
		if(ch=='a'){
			mark=input.setHardMark();
			if(skipWhitespace())
				return RDF_TYPE;
			else {
				input.setMarkPosition(mark);
				string prefix=readPrefix('a');
				string scope=namespaces[prefix];
				if(scope==null)throw new ParserException();
				predicate=RDFTerm.fromIRI(scope+readOptionalLocalName());
				skipWhitespace();
				return predicate;
			}
		} else if(ch=='<'){
			predicate=RDFTerm.fromIRI(readIriReference());
			skipWhitespace();
			return predicate;
		} else if(ch==':'){ // prefixed name with current prefix
			string scope=namespaces[""];
			if(scope==null)throw new ParserException();
			predicate=RDFTerm.fromIRI(scope+readOptionalLocalName());
			skipWhitespace();
			return predicate;
		} else if(isNameStartChar(ch)){ // prefix
			string prefix=readPrefix(ch);
			string scope=namespaces[prefix];
			if(scope==null)throw new ParserException();
			predicate=RDFTerm.fromIRI(scope+readOptionalLocalName());
			skipWhitespace();
			return predicate;
		} else {
			input.setMarkPosition(mark);
			return null;
		}
	}



	private TurtleObject readBlankNodePropertyList()  {
		TurtleObject obj=TurtleObject.newPropertyList();
		bool havePredObject=false;
		while(true){
			skipWhitespace();
			int ch;
			if(havePredObject){
				bool haveSemicolon=false;
				while(true){
					input.setSoftMark();
					ch=input.read();
					if(ch==';'){
						skipWhitespace();
						haveSemicolon=true;
					} else {
						if(ch>=0) {
							input.moveBack(1);
						}
						break;
					}
				}
				if(!haveSemicolon) {
					break;
				}
			}
			RDFTerm pred=readPredicate();
			if(pred==null){
				break;
			}
			havePredObject=true;
			readObjectListToProperties(pred,obj);
		}
		if(input.read()!=']')
			throw new ParserException();
		return obj;
	}


	private void readPredicateObjectList(ISet<RDFTriple> triples)  {
		bool havePredObject=false;
		while(true){
			int ch;
			skipWhitespace();
			if(havePredObject){
				bool haveSemicolon=false;
				while(true){
					input.setSoftMark();
					ch=input.read();
					//Console.WriteLine("nextchar %c",(char)ch);
					if(ch==';'){
						skipWhitespace();
						haveSemicolon=true;
					} else {
						if(ch>=0) {
							input.moveBack(1);
						}
						break;
					}
				}
				if(!haveSemicolon) {
					break;
				}
			}
			curPredicate=readPredicate();
			//Console.WriteLine("predobjlist %s",curPredicate);
			if(curPredicate==null){
				if(!havePredObject)
					throw new ParserException();
				else {
					break;
				}
			}
			// Read _object
			havePredObject=true;
			readObjectList(triples);
		}
		if(!havePredObject)
			throw new ParserException();
		return;
	}

	private int curBlankNode=0;
	private static readonly RDFTerm RDF_TYPE=RDFTerm.fromIRI(
			"http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
	private static readonly RDFTerm RDF_FALSE=RDFTerm.fromTypedString(
			"false","http://www.w3.org/2001/XMLSchema#bool");
	private static readonly RDFTerm RDF_TRUE=RDFTerm.fromTypedString(
			"true","http://www.w3.org/2001/XMLSchema#bool");
	private static readonly RDFTerm RDF_FIRST=RDFTerm.fromIRI(
			"http://www.w3.org/1999/02/22-rdf-syntax-ns#first");
	private static readonly RDFTerm RDF_REST=RDFTerm.fromIRI(
			"http://www.w3.org/1999/02/22-rdf-syntax-ns#rest");
	private static readonly RDFTerm RDF_NIL=RDFTerm.fromIRI(
			"http://www.w3.org/1999/02/22-rdf-syntax-ns#nil");

	private RDFTerm allocateBlankNode(){
		curBlankNode++;
		// A period is included so as not to conflict
		// with user-defined blank node labels (this is allowed
		// because the syntax for blank node identifiers is
		// not concretely defined)
		string label="."+Convert.ToString(curBlankNode,System.Globalization.CultureInfo.InvariantCulture);
		RDFTerm node=RDFTerm.fromBlankNode(label);
		bnodeLabels.Add(label,node);
		return node;
	}

	private string suggestBlankNodeName(string node, int[] nodeindex){
		bool validnode=(node.Length>0);
		// Check if the blank node label is valid
		// under N-Triples
		for(int i=0;i<node.Length;i++){
			int c=node[i];
			if(i==0 && !((c>='A' && c<='Z') || (c>='a' && c<='z'))){
				validnode=false;
				break;
			}
			if(i>=0 && !((c>='A' && c<='Z') || (c>='0' && c<='9') ||
					(c>='a' && c<='z'))){
				validnode=false;
				break;
			}
		}
		if(validnode)return node;
		while(true){
			// Generate a new blank node label,
			// and ensure it's unique
			node="b"+Convert.ToString(nodeindex[0],System.Globalization.CultureInfo.InvariantCulture);
			if(!bnodeLabels.ContainsKey(node))
				return node;
			nodeindex[0]++;
		}
	}

	// Replaces certain blank nodes with blank nodes whose
	// names meet the N-Triples requirements
	private void replaceBlankNodes(ISet<RDFTriple> triples){
		if(bnodeLabels.Count==0)
			return;
		IDictionary<string,RDFTerm> newBlankNodes=new PeterO.Support.LenientDictionary<string,RDFTerm>();
		IList<RDFTriple[]> changedTriples=new List<RDFTriple[]>();
		int[] nodeindex=new int[]{0};
		foreach(RDFTriple triple in triples){
			bool changed=false;
			RDFTerm subj=triple.getSubject();
			if(subj.getKind()==RDFTerm.BLANK){
				string oldname=subj.getIdentifier();
				string newname=suggestBlankNodeName(oldname,nodeindex);
				if(!newname.Equals(oldname)){
					RDFTerm newNode=newBlankNodes[oldname];
					if(newNode==null){
						newNode=RDFTerm.fromBlankNode(newname);
						bnodeLabels.Add(newname, newNode);
						newBlankNodes.Add(oldname, newNode);
					}
					subj=newNode;
					changed=true;
				}
			}
			RDFTerm obj=triple.getObject();
			if(obj.getKind()==RDFTerm.BLANK){
				string oldname=obj.getIdentifier();
				string newname=suggestBlankNodeName(oldname,nodeindex);
				if(!newname.Equals(oldname)){
					RDFTerm newNode=newBlankNodes[oldname];
					if(newNode==null){
						newNode=RDFTerm.fromBlankNode(newname);
						bnodeLabels.Add(newname, newNode);
						newBlankNodes.Add(oldname, newNode);
					}
					obj=newNode;
					changed=true;
				}
			}
			if(changed){
				RDFTriple[] newTriple=new RDFTriple[]{triple,
						new RDFTriple(subj,triple.getPredicate(),obj)
				};
				changedTriples.Add(newTriple);
			}
		}
		foreach(RDFTriple[] triple in changedTriples){
			triples.Remove(triple[0]);
			triples.Add(triple[1]);
		}
	}

	private void emitRDFTriple(RDFTerm subj, RDFTerm pred,
			TurtleObject obj, ISet<RDFTriple> triples){
		if(obj.kind==TurtleObject.SIMPLE){
			emitRDFTriple(subj,pred,obj.term,triples);
		} else if(obj.kind==TurtleObject.PROPERTIES){
			IList<TurtleProperty> props=obj.getProperties();
			if(props.Count==0){
				emitRDFTriple(subj,pred,allocateBlankNode(),triples);
			} else {
				RDFTerm blank=allocateBlankNode();
				emitRDFTriple(subj,pred,blank,triples);
				for(int i=0;i<props.Count;i++){
					emitRDFTriple(blank,props[i].pred,props[i].obj,triples);
				}
			}
		} else if(obj.kind==TurtleObject.COLLECTION){
			IList<TurtleObject> objs=obj.getObjects();
			if(objs.Count==0){
				emitRDFTriple(subj,pred,RDF_NIL,triples);
			} else {
				RDFTerm curBlank=allocateBlankNode();
				RDFTerm firstBlank=curBlank;
				emitRDFTriple(curBlank,RDF_FIRST,objs[0],triples);
				for(int i=1;i<=objs.Count;i++){
					if(i==objs.Count){
						emitRDFTriple(curBlank,RDF_REST,RDF_NIL,triples);
					} else {
						RDFTerm nextBlank=allocateBlankNode();
						emitRDFTriple(curBlank,RDF_REST,nextBlank,triples);
						emitRDFTriple(nextBlank,RDF_FIRST,objs[i],triples);
						curBlank=nextBlank;
					}
				}
				emitRDFTriple(subj,pred,firstBlank,triples);
			}
		}
	}

	private void emitRDFTriple(TurtleObject subj, RDFTerm pred,
			TurtleObject obj, ISet<RDFTriple> triples){
		if(subj.kind==TurtleObject.SIMPLE){
			emitRDFTriple(subj.term,pred,obj,triples);
		} else if(subj.kind==TurtleObject.PROPERTIES){
			IList<TurtleProperty> props=subj.getProperties();
			if(props.Count==0){
				emitRDFTriple(allocateBlankNode(),pred,obj,triples);
			} else {
				RDFTerm blank=allocateBlankNode();
				emitRDFTriple(blank,pred,obj,triples);
				for(int i=0;i<props.Count;i++){
					emitRDFTriple(blank,props[i].pred,props[i].obj,triples);
				}
			}
		} else if(subj.kind==TurtleObject.COLLECTION){
			IList<TurtleObject> objs=subj.getObjects();
			if(objs.Count==0){
				emitRDFTriple(RDF_NIL,pred,obj,triples);
			} else {
				RDFTerm curBlank=allocateBlankNode();
				RDFTerm firstBlank=curBlank;
				emitRDFTriple(curBlank,RDF_FIRST,objs[0],triples);
				for(int i=1;i<=objs.Count;i++){
					if(i==objs.Count){
						emitRDFTriple(curBlank,RDF_REST,RDF_NIL,triples);
					} else {
						RDFTerm nextBlank=allocateBlankNode();
						emitRDFTriple(curBlank,RDF_REST,nextBlank,triples);
						emitRDFTriple(nextBlank,RDF_FIRST,objs[i],triples);
						curBlank=nextBlank;
					}
				}
				emitRDFTriple(firstBlank,pred,obj,triples);
			}
		}
	}

	private bool isNameStartChar(int ch) {
		return (ch>='a' && ch<='z') ||
				(ch>='A' && ch<='Z') ||
				(ch>=0xc0 && ch<=0xd6) ||
				(ch>=0xd8 && ch<=0xf6) ||
				(ch>=0xf8 && ch<=0x2ff) ||
				(ch>=0x370 && ch<=0x37d) ||
				(ch>=0x37f && ch<=0x1fff) ||
				(ch>=0x200c && ch<=0x200d) ||
				(ch>=0x2070 && ch<=0x218f) ||
				(ch>=0x2c00 && ch<=0x2fef) ||
				(ch>=0x3001 && ch<=0xd7ff) ||
				(ch>=0xf900 && ch<=0xfdcf) ||
				(ch>=0xfdf0 && ch<=0xfffd) ||
				(ch>=0x10000 && ch<=0xeffff);
	}
	private bool isNameStartCharU(int ch) {
		return (ch>='a' && ch<='z') ||
				(ch>='A' && ch<='Z') || ch=='_' ||
				(ch>=0xc0 && ch<=0xd6) ||
				(ch>=0xd8 && ch<=0xf6) ||
				(ch>=0xf8 && ch<=0x2ff) ||
				(ch>=0x370 && ch<=0x37d) ||
				(ch>=0x37f && ch<=0x1fff) ||
				(ch>=0x200c && ch<=0x200d) ||
				(ch>=0x2070 && ch<=0x218f) ||
				(ch>=0x2c00 && ch<=0x2fef) ||
				(ch>=0x3001 && ch<=0xd7ff) ||
				(ch>=0xf900 && ch<=0xfdcf) ||
				(ch>=0xfdf0 && ch<=0xfffd) ||
				(ch>=0x10000 && ch<=0xeffff);
	}
	private bool isNameChar(int ch) {
		return (ch>='a' && ch<='z') ||
				(ch>='0' && ch<='9') ||
				(ch>='A' && ch<='Z') || ch=='_' || ch=='-' ||
				ch==0xb7 ||
				(ch>=0xc0 && ch<=0xd6) ||
				(ch>=0xd8 && ch<=0xf6) ||
				(ch>=0xf8 && ch<=0x37d) ||
				(ch>=0x37f && ch<=0x1fff) ||
				(ch>=0x200c && ch<=0x200d) ||
				ch==0x203f || ch==0x2040 ||
				(ch>=0x2070 && ch<=0x218f) ||
				(ch>=0x2c00 && ch<=0x2fef) ||
				(ch>=0x3001 && ch<=0xd7ff) ||
				(ch>=0xf900 && ch<=0xfdcf) ||
				(ch>=0xfdf0 && ch<=0xfffd) ||
				(ch>=0x10000 && ch<=0xeffff);
	}

	public ISet<RDFTriple> parse()  {
		ISet<RDFTriple> triples=new HashSet<RDFTriple>();
		while(true){
			skipWhitespace();
			int mark=input.setHardMark();
			int ch=input.read();
			if(ch<0){
				replaceBlankNodes(triples);
				return triples;
			}
			if(ch=='@'){
				ch=input.read();
				if(ch=='p' && input.read()=='r' && input.read()=='e' &&
						input.read()=='f' && input.read()=='i' &&
						input.read()=='x' && skipWhitespace()){
					readPrefixStatement(false);
					continue;
				}
				else if(ch=='b' && input.read()=='a' && input.read()=='s' &&
						input.read()=='e' && skipWhitespace()){
					readBase(false);
					continue;
				}
				else throw new ParserException();
			} else if(ch=='b' || ch=='B'){
				int c2=0;
				if(((c2=input.read())=='A' || c2=='a') &&
						((c2=input.read())=='S' || c2=='s') &&
						((c2=input.read())=='E' || c2=='e') && skipWhitespace()){
					readBase(true);
					continue;
				} else {
					input.setMarkPosition(mark);
				}
			} else if(ch=='p' || ch=='P'){
				int c2=0;
				if(((c2=input.read())=='R' || c2=='r') &&
						((c2=input.read())=='E' || c2=='e') &&
						((c2=input.read())=='F' || c2=='f') &&
						((c2=input.read())=='I' || c2=='i') &&
						((c2=input.read())=='X' || c2=='x') && skipWhitespace()){
					readPrefixStatement(true);
					continue;
				} else {
					input.setMarkPosition(mark);
				}
			} else {
				input.setMarkPosition(mark);
			}
			readTriples(triples);
		}
	}

	private void readBase(bool sparql)  {
		if(input.read()!='<')
			throw new ParserException();
		baseURI=readIriReference();
		if(!sparql){
			skipWhitespace();
			if(input.read()!='.')
				throw new ParserException();
		} else {
			skipWhitespace();
		}
	}

	private void readPrefixStatement(bool sparql)  {
		string prefix=readPrefix(-1);
		skipWhitespace();
		if(input.read()!='<')
			throw new ParserException();
		string iri=readIriReference();
		namespaces.Add(prefix, iri);
		if(!sparql){
			skipWhitespace();
			if(input.read()!='.')
				throw new ParserException();
		} else {
			skipWhitespace();
		}
	}
}

}
