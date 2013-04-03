namespace com.upokecenter.rdf {
using System;

public sealed class RDFTerm {
	public override sealed int GetHashCode(){unchecked{
		 int prime = 31;
		int result = 1;
		result = prime * result
				+ ((identifier == null) ? 0 : identifier.GetHashCode());
		result = prime * result + kind;
		result = prime * result
				+ ((languageTag == null) ? 0 : languageTag.GetHashCode());
		result = prime * result + ((lexicalForm == null) ? 0 : lexicalForm.GetHashCode());
		return result;
	}}
	public override sealed bool Equals(object obj) {
		if (this == obj)
			return true;
		if (obj == null)
			return false;
		if (GetType() != obj.GetType())
			return false;
		RDFTerm other = (RDFTerm) obj;
		if (identifier == null) {
			if (other.identifier != null)
				return false;
		} else if (!identifier.Equals(other.identifier))
			return false;
		if (kind != other.kind)
			return false;
		if (languageTag == null) {
			if (other.languageTag != null)
				return false;
		} else if (!languageTag.Equals(other.languageTag))
			return false;
		if (lexicalForm == null) {
			if (other.lexicalForm != null)
				return false;
		} else if (!lexicalForm.Equals(other.lexicalForm))
			return false;
		return true;
	}

	private static void escapeLanguageTag(string str, System.Text.StringBuilder builder){
		int length=str.Length;
		bool hyphen=false;
		for(int i=0;i<length;i++){
			int c=str[i];
			if((c>='A' && c<='Z')){
				builder.Append((char)(c+0x20));
			} else if(c>='a' && c<='z'){
				builder.Append((char)c);
			} else if(hyphen && c>='0' && c<='9'){
				builder.Append((char)c);
			} else if(c=='-'){
				builder.Append((char)c);
				hyphen=true;
				if(i+1<length && str[i+1]=='-') {
					builder.Append('x');
				}
			} else {
				builder.Append('x');
			}
		}
	}

	private static void escapeBlankNode(string str, System.Text.StringBuilder builder){
		int length=str.Length;
		string hex="0123456789ABCDEF";
		for(int i=0;i<length;i++){
			int c=str[i];
			if((c>='A' && c<='Z') || (c>='a' && c<='z') ||
					(c>0 && c>='0' && c<='9')){
				builder.Append((char)c);
			}
			else if(c>=0xD800 && c<=0xDBFF && i+1<length &&
					str[i+1]>=0xDC00 && str[i+1]<=0xDFFF){
				// Get the Unicode code point for the surrogate pair
				c=0x10000+(c-0xD800)*0x400+(str[i+1]-0xDC00);
				builder.Append("U00");
				builder.Append(hex[(c>>20)&15]);
				builder.Append(hex[(c>>16)&15]);
				builder.Append(hex[(c>>12)&15]);
				builder.Append(hex[(c>>8)&15]);
				builder.Append(hex[(c>>4)&15]);
				builder.Append(hex[(c)&15]);
				i++;
			}
			else {
				builder.Append("u");
				builder.Append(hex[(c>>12)&15]);
				builder.Append(hex[(c>>8)&15]);
				builder.Append(hex[(c>>4)&15]);
				builder.Append(hex[(c)&15]);
			}
		}
	}

	private static void escapeString(string str,
			System.Text.StringBuilder builder, bool uri){
		int length=str.Length;
		string hex="0123456789ABCDEF";
		for(int i=0;i<length;i++){
			int c=str[i];
			if(c==0x09){
				builder.Append("\\t");
			} else if(c==0x0a){
				builder.Append("\\n");
			} else if(c==0x0d){
				builder.Append("\\r");
			} else if(c==0x22){
				builder.Append("\\\"");
			} else if(c==0x5c){
				builder.Append("\\\\");
			} else if(uri && c=='>'){
				builder.Append("%3E");
			} else if(c>=0x20 && c<=0x7E){
				builder.Append((char)c);
			}
			else if(c>=0xD800 && c<=0xDBFF && i+1<length &&
					str[i+1]>=0xDC00 && str[i+1]<=0xDFFF){
				// Get the Unicode code point for the surrogate pair
				c=0x10000+(c-0xD800)*0x400+(str[i+1]-0xDC00);
				builder.Append("\\U00");
				builder.Append(hex[(c>>20)&15]);
				builder.Append(hex[(c>>16)&15]);
				builder.Append(hex[(c>>12)&15]);
				builder.Append(hex[(c>>8)&15]);
				builder.Append(hex[(c>>4)&15]);
				builder.Append(hex[(c)&15]);
				i++;
			}
			else {
				builder.Append("\\u");
				builder.Append(hex[(c>>12)&15]);
				builder.Append(hex[(c>>8)&15]);
				builder.Append(hex[(c>>4)&15]);
				builder.Append(hex[(c)&15]);
			}
		}
	}

	/**
	 * 
	 * Gets a _string representation of this RDF term
	 * in N-Triples format.  The _string will not end
	 * in a line break.
	 * 
	 */
	public override sealed string ToString(){
		System.Text.StringBuilder builder=null;
		if(this.kind==BLANK){
			builder=new System.Text.StringBuilder();
			builder.Append("_:");
			escapeBlankNode(identifier,builder);
		} else if(this.kind==LANGSTRING){
			builder=new System.Text.StringBuilder();
			builder.Append("\"");
			escapeString(lexicalForm,builder,false);
			builder.Append("\"@");
			escapeLanguageTag(languageTag,builder);
		} else if(this.kind==TYPEDSTRING){
			builder=new System.Text.StringBuilder();
			builder.Append("\"");
			escapeString(lexicalForm,builder,false);
			builder.Append("\"");
			if(!"http://www.w3.org/2001/XMLSchema#string".Equals(identifier)){
				builder.Append("^^<");
				escapeString(identifier,builder,true);
				builder.Append(">");
			}
		} else if(this.kind==IRI){
			builder=new System.Text.StringBuilder();
			builder.Append("<");
			escapeString(identifier,builder,true);
			builder.Append(">");
		} else
			return "<about:blank>";
		return builder.ToString();
	}
	/**
	 * A blank node.
	 */
	public static readonly int BLANK = 0; // type is blank node name, literal is blank
	/**
	 * An IRI (Internationalized Resource Identifier.)
	 */
	public static readonly int IRI = 1; // type is IRI, literal is blank
	/**
	 * A _string with a language tag.
	 */
	public static readonly int LANGSTRING = 2; // literal is given
	/**
	 * A piece of data serialized to a _string.
	 */
	public static readonly int TYPEDSTRING = 3; // type is IRI, literal is given
	private string identifier=null;
	private string languageTag=null;
	private string lexicalForm=null;
	/**
	 * 
	 * Gets the IRI or blank node label for this RDF
	 * literal. Supported by all kinds.
	 * 
	 * 
	 */
	public string getIdentifier() {
		return identifier;
	}
	/**
	 * Gets the language tag for this RDF literal.
	 * Supported by the LANGSTRING kind.
	 * 
	 * 
	 */
	public string getLanguageTag() {
		return languageTag;
	}
	/**
	 * Gets the lexical form of an RDF literal.
	 * Supported in the LANGSTRING and TYPEDSTRING kinds.
	 * 
	 * 
	 */
	public string getLexicalForm() {
		return lexicalForm;
	}
	public int getKind() {
		return kind;
	}
	private int kind;
	public static RDFTerm fromTypedString(string str, string iri){
		if((str)==null)throw new ArgumentNullException("str");
		if((iri)==null)throw new ArgumentNullException("iri");
		if((iri).Length==0)throw new ArgumentException("iri");
		RDFTerm ret=new RDFTerm();
		ret.kind=TYPEDSTRING;
		ret.identifier=iri;
		ret.lexicalForm=str;
		if(iri.Equals("http://www.w3.org/1999/02/22-rdf-syntax-ns#langString"))
			// this can't be a language _string
			// because there is no language tag
			throw new ArgumentException("iri");
		return ret;
	}
	public static RDFTerm fromBlankNode(string name){
		if((name)==null)throw new ArgumentNullException("name");
		if((name).Length==0)throw new ArgumentException("name");
		RDFTerm ret=new RDFTerm();
		ret.kind=BLANK;
		ret.identifier=name;
		ret.lexicalForm=null;
		return ret;
	}
	public static RDFTerm fromIRI(string iri){
		if((iri)==null)throw new ArgumentNullException("iri");
		RDFTerm ret=new RDFTerm();
		ret.kind=IRI;
		ret.identifier=iri;
		ret.lexicalForm=null;
		return ret;
	}
	public static RDFTerm fromTypedString(string str) {
		return fromTypedString(str,"http://www.w3.org/2001/XMLSchema#string");
	}
	public static RDFTerm fromLangString(string str, string languageTag) {
		if((str)==null)throw new ArgumentNullException("str");
		if((languageTag)==null)throw new ArgumentNullException("languageTag");
		if((languageTag).Length==0)throw new ArgumentException("languageTag");
		RDFTerm ret=new RDFTerm();
		ret.kind=LANGSTRING;
		ret.identifier="http://www.w3.org/1999/02/22-rdf-syntax-ns#langString";
		ret.languageTag=languageTag;
		ret.lexicalForm=str;
		return ret;
	}
}
}
