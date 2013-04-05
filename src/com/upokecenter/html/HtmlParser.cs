/*

Licensed under the Expat License.

Copyright (C) 2013 Peter Occil

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*/

namespace com.upokecenter.html {
using System;

using System.IO;

using System.Collections.Generic;



using com.upokecenter.encoding;



using com.upokecenter.util;


sealed class HtmlParser {

	internal class Attrib {
		System.Text.StringBuilder name;
		IntList value;
		string prefix=null;
		string localName=null;
		string nameString=null;
		string valueString=null;
		string _namespace=null;

		public override sealed string ToString(){
			return "[Attribute: "+getName()+"="+getValue()+"]";
		}

		public Attrib(char ch){
			name=new System.Text.StringBuilder();
			value=new IntList();
			name.Append(ch);
		}

		public Attrib(int ch){
			name=new System.Text.StringBuilder();
			value=new IntList();
			if(ch<0x10000){
				name.Append((char)ch);
			} else {
				ch-=0x10000;
				int lead=ch/0x400+0xd800;
				int trail=(ch&0x3FF)+0xdc00;
				name.Append((char)lead);
				name.Append((char)trail);
			}
		}
		public Attrib(Attrib attr){
			nameString=attr.getName();
			valueString=attr.getValue();
			prefix=attr.prefix;
			localName=attr.localName;
			_namespace=attr._namespace;
		}

		public Attrib(string name, string value){
			nameString=name;
			valueString=value;
		}

		public void appendToName(int ch){
			if(nameString!=null)
				throw new InvalidOperationException();
			if(ch<0x10000){
				name.Append((char)ch);
			} else {
				ch-=0x10000;
				int lead=ch/0x400+0xd800;
				int trail=(ch&0x3FF)+0xdc00;
				name.Append((char)lead);
				name.Append((char)trail);
			}
		}

		public void appendToValue(int ch){
			if(valueString!=null)
				throw new InvalidOperationException();
			value.appendInt(ch);
		}

		internal void commitValue(){
			if(value==null)
				throw new InvalidOperationException();
			valueString=value.ToString();
			value=null;
		}

		public string getName(){
			return (nameString!=null) ? nameString : name.ToString();
		}

		public string getValue() {
			return (valueString!=null) ? valueString : value.ToString();
		}
		public string getNamespace(){
			return _namespace;
		}

		public string getLocalName(){
			return (_namespace==null) ? getName() : localName;
		}

		public bool isAttribute(string name, string _namespace){
			string thisname=getLocalName();
			bool match=(name==null ? thisname==null : name.Equals(thisname));
			if(!match)return false;
			match=(_namespace==null ? this._namespace==null : _namespace.Equals(this._namespace));
			return match;
		}

		public void setNamespace(string value){
			if(value==null)
				throw new ArgumentException();
			_namespace=value;
			nameString=getName();
			int io=nameString.IndexOf(':');
			if(io>=1){
				prefix=nameString.Substring(0,(io)-(0));
				localName=nameString.Substring(io+1);
			} else {
				prefix="";
				localName=getName();
			}
		}

		public void setName(string value2) {
			if(value2==null)
				throw new ArgumentException();
			nameString=value2;
			name=null;
		}

		public void setValue(string value2) {
			if(value2==null)
				throw new ArgumentException();
			valueString=value2;
			value=null;
		}

	}

	internal class CommentToken : IToken {
		IntList value;
		public CommentToken(){
			value=new IntList();
		}

		public void appendChar(int ch){
			value.appendInt(ch);
		}

		public int getType() {
			return TOKEN_COMMENT;
		}

		public string getValue(){
			return value.ToString();
		}

	}
	internal class DocTypeToken : IToken {
		public IntList name;
		public IntList publicID;
		public IntList systemID;
		public bool forceQuirks;
		public int getType() {
			return TOKEN_DOCTYPE;
		}
	}
	internal class EndTagToken : TagToken {
		public EndTagToken(char c) : base(c) {
		}
		public EndTagToken(string name) : base(name) {
		}
		public override sealed int getType() {
			return TOKEN_END_TAG;
		}
	}
	private class FormattingElement {
		public bool marker;
		public Element element;
		public StartTagToken token;
		public override sealed string ToString() {
			return "FormattingElement [marker=" + marker + ", token=" + token + "]\n";
		}
		public bool isMarker() {
			return marker;
		}
	}


	private enum InsertionMode {
		Initial,
		BeforeHtml,
		BeforeHead,
		InHead,
		InHeadNoscript,
		AfterHead,
		InBody,
		Text,
		InTable,
		InTableText,
		InCaption,
		InColumnGroup,
		InTableBody,
		InRow,
		InCell,
		InSelect,
		InSelectInTable,
		AfterBody,
		InFrameset,
		AfterFrameset,
		AfterAfterBody,
		AfterAfterFrameset
	}

	internal interface IToken {
		 int getType();
	}

	internal class StartTagToken : TagToken {
		public StartTagToken(char c) : base(c) {
		}
		public StartTagToken(string name) : base(name) {
		}
		public override sealed int getType() {
			return TOKEN_START_TAG;
		}
		public void setName(string _string) {
			builder.Length=(0);
			builder.Append(_string);
		}
	}
	internal abstract class TagToken : IToken {

		protected System.Text.StringBuilder builder;
		IList<Attrib> attributes=null;
		bool selfClosing=false;
		bool selfClosingAck=false;
		public TagToken(char ch){
			builder=new System.Text.StringBuilder();
			builder.Append(ch);
		}

		public TagToken(string name){
			builder=new System.Text.StringBuilder();
			builder.Append(name);
		}

		public void ackSelfClosing(){
			selfClosingAck=true;
		}

		public bool isAckSelfClosing() {
			return !selfClosing || selfClosingAck;
		}

		public Attrib addAttribute(char ch){
			if(attributes==null){
				attributes=new List<Attrib>();
			}
			Attrib a=new Attrib(ch);
			attributes.Add(a);
			return a;
		}

		public Attrib addAttribute(int ch){
			if(attributes==null){
				attributes=new List<Attrib>();
			}
			Attrib a=new Attrib(ch);
			attributes.Add(a);
			return a;
		}

		public void append(int ch) {
			if(ch<0x10000){
				builder.Append((char)ch);
			} else {
				ch-=0x10000;
				int lead=ch/0x400+0xd800;
				int trail=(ch&0x3FF)+0xdc00;
				builder.Append((char)lead);
				builder.Append((char)trail);
			}
		}

		public void appendChar(char ch) {
			builder.Append(ch);
		}

		public bool checkAttributeName(){
			if(attributes==null)return true;
			int size=attributes.Count;
			if(size>=2){
				string thisname=attributes[size-1].getName();
				for(int i=0;i<size-1;i++){
					if(attributes[i].getName().Equals(thisname)){
						// Attribute with this name already exists;
						// remove it
						attributes.RemoveAt(size-1);
						return false;
					}
				}
			}
			return true;
		}

		public string getAttribute(string name){
			if(attributes==null)return null;
			int size=attributes.Count;
			for(int i=0;i<size;i++){
				Attrib a=attributes[i];
				string thisname=a.getName();
				if(thisname.Equals(name))
					return a.getValue();
			}
			return null;
		}


		public string getAttributeNS(string name, string _namespace){
			if(attributes==null)return null;
			int size=attributes.Count;
			for(int i=0;i<size;i++){
				Attrib a=attributes[i];
				if(a.isAttribute(name,_namespace))
					return a.getValue();
			}
			return null;
		}

		public IList<Attrib> getAttributes(){
			if(attributes==null)
				return (new Attrib[0]);
			else
				return attributes;
		}

		public string getName(){
			return builder.ToString();
		}
		public abstract int getType();
		public bool isSelfClosing() {
			return selfClosing;
		}

		public bool isSelfClosingAck(){
			return selfClosingAck;
		}


		public void setAttribute(string attrname, string value) {
			if(attributes==null){
				attributes=new List<Attrib>();
				attributes.Add(new Attrib(attrname,value));
			} else {
				int size=attributes.Count;
				for(int i=0;i<size;i++){
					Attrib a=attributes[i];
					string thisname=a.getName();
					if(thisname.Equals(attrname)){
						a.setValue(value);
						return;
					}
				}
				attributes.Add(new Attrib(attrname,value));
			}
		}

		public void setSelfClosing(bool selfClosing) {
			this.selfClosing = selfClosing;
		}
		public override sealed string ToString() {
			return "TagToken [" + builder.ToString() + ", "
					+ attributes +(selfClosing ? (", selfClosingAck=" + selfClosingAck) : "") + "]";
		}

	}
	private enum TokenizerState {
		Data,
		CharacterRefInData,
		RcData,
		CharacterRefInRcData,
		RawText,
		ScriptData,
		PlainText,
		TagOpen,
		EndTagOpen,
		TagName,
		RcDataLessThan,
		RcDataEndTagOpen,
		RcDataEndTagName,
		RawTextLessThan,
		RawTextEndTagOpen,
		RawTextEndTagName,
		ScriptDataLessThan,
		ScriptDataEndTagOpen,
		ScriptDataEndTagName,
		ScriptDataEscapeStart,
		ScriptDataEscapeStartDash,
		ScriptDataEscaped,
		ScriptDataEscapedDash,
		ScriptDataEscapedDashDash,
		ScriptDataEscapedLessThan,
		ScriptDataEscapedEndTagOpen,
		ScriptDataEscapedEndTagName,
		ScriptDataDoubleEscapeStart,
		ScriptDataDoubleEscaped,
		ScriptDataDoubleEscapedDash,
		ScriptDataDoubleEscapedDashDash,
		ScriptDataDoubleEscapedLessThan,
		ScriptDataDoubleEscapeEnd,
		BeforeAttributeName,
		AttributeName,
		AfterAttributeName,
		BeforeAttributeValue,
		AttributeValueDoubleQuoted,
		AttributeValueSingleQuoted,
		AttributeValueUnquoted,
		CharacterRefInAttributeValue,
		AfterAttributeValueQuoted,
		SelfClosingStartTag,
		BogusComment,
		MarkupDeclarationOpen,
		CommentStart,
		CommentStartDash,
		Comment,
		CommentEndDash,
		CommentEnd,
		CommentEndBang,
		DocType,
		BeforeDocTypeName,
		DocTypeName,
		AfterDocTypeName,
		AfterDocTypePublic,
		BeforeDocTypePublicID,
		DocTypePublicIDDoubleQuoted,
		DocTypePublicIDSingleQuoted,
		AfterDocTypePublicID,
		BetweenDocTypePublicAndSystem,
		AfterDocTypeSystem,
		BeforeDocTypeSystemID,
		DocTypeSystemIDDoubleQuoted,
		DocTypeSystemIDSingleQuoted,
		AfterDocTypeSystemID,
		BogusDocType,
		CData
	}
	private static string[] entities=new string[]{"CounterClockwiseContourIntegral;", "ClockwiseContourIntegral;", "DoubleLongLeftRightArrow;", "NotNestedGreaterGreater;", "DiacriticalDoubleAcute;", "NotSquareSupersetEqual;", "CloseCurlyDoubleQuote;", "DoubleContourIntegral;", "FilledVerySmallSquare;", "NegativeVeryThinSpace;", "NotPrecedesSlantEqual;", "NotRightTriangleEqual;", "NotSucceedsSlantEqual;", "CapitalDifferentialD;", "DoubleLeftRightArrow;", "DoubleLongRightArrow;", "EmptyVerySmallSquare;", "NestedGreaterGreater;", "NotDoubleVerticalBar;", "NotGreaterSlantEqual;", "NotLeftTriangleEqual;", "NotSquareSubsetEqual;", "OpenCurlyDoubleQuote;", "ReverseUpEquilibrium;", "DoubleLongLeftArrow;", "DownLeftRightVector;", "LeftArrowRightArrow;", "NegativeMediumSpace;", "NotGreaterFullEqual;", "NotRightTriangleBar;", "RightArrowLeftArrow;", "SquareSupersetEqual;", "leftrightsquigarrow;", "DownRightTeeVector;", "DownRightVectorBar;", "LongLeftRightArrow;", "Longleftrightarrow;", "NegativeThickSpace;", "NotLeftTriangleBar;", "PrecedesSlantEqual;", "ReverseEquilibrium;", "RightDoubleBracket;", "RightDownTeeVector;", "RightDownVectorBar;", "RightTriangleEqual;", "SquareIntersection;", "SucceedsSlantEqual;", "blacktriangleright;", "longleftrightarrow;", "DoubleUpDownArrow;", "DoubleVerticalBar;", "DownLeftTeeVector;", "DownLeftVectorBar;", "FilledSmallSquare;", "GreaterSlantEqual;", "LeftDoubleBracket;", "LeftDownTeeVector;", "LeftDownVectorBar;", "LeftTriangleEqual;", "NegativeThinSpace;", "NotGreaterGreater;", "NotLessSlantEqual;", "NotNestedLessLess;", "NotReverseElement;", "NotSquareSuperset;", "NotTildeFullEqual;", "RightAngleBracket;", "RightUpDownVector;", "SquareSubsetEqual;", "VerticalSeparator;", "blacktriangledown;", "blacktriangleleft;", "leftrightharpoons;", "rightleftharpoons;", "twoheadrightarrow;", "DiacriticalAcute;", "DiacriticalGrave;", "DiacriticalTilde;", "DoubleRightArrow;", "DownArrowUpArrow;", "EmptySmallSquare;", "GreaterEqualLess;", "GreaterFullEqual;", "LeftAngleBracket;", "LeftUpDownVector;", "LessEqualGreater;", "NonBreakingSpace;", "NotPrecedesEqual;", "NotRightTriangle;", "NotSucceedsEqual;", "NotSucceedsTilde;", "NotSupersetEqual;", "RightTriangleBar;", "RightUpTeeVector;", "RightUpVectorBar;", "UnderParenthesis;", "UpArrowDownArrow;", "circlearrowright;", "downharpoonright;", "ntrianglerighteq;", "rightharpoondown;", "rightrightarrows;", "twoheadleftarrow;", "vartriangleright;", "CloseCurlyQuote;", "ContourIntegral;", "DoubleDownArrow;", "DoubleLeftArrow;", "DownRightVector;", "LeftRightVector;", "LeftTriangleBar;", "LeftUpTeeVector;", "LeftUpVectorBar;", "LowerRightArrow;", "NotGreaterEqual;", "NotGreaterTilde;", "NotHumpDownHump;", "NotLeftTriangle;", "NotSquareSubset;", "OverParenthesis;", "RightDownVector;", "ShortRightArrow;", "UpperRightArrow;", "bigtriangledown;", "circlearrowleft;", "curvearrowright;", "downharpoonleft;", "leftharpoondown;", "leftrightarrows;", "nLeftrightarrow;", "nleftrightarrow;", "ntrianglelefteq;", "rightleftarrows;", "rightsquigarrow;", "rightthreetimes;", "straightepsilon;", "trianglerighteq;", "vartriangleleft;", "DiacriticalDot;", "DoubleRightTee;", "DownLeftVector;", "GreaterGreater;", "HorizontalLine;", "InvisibleComma;", "InvisibleTimes;", "LeftDownVector;", "LeftRightArrow;", "Leftrightarrow;", "LessSlantEqual;", "LongRightArrow;", "Longrightarrow;", "LowerLeftArrow;", "NestedLessLess;", "NotGreaterLess;", "NotLessGreater;", "NotSubsetEqual;", "NotVerticalBar;", "OpenCurlyQuote;", "ReverseElement;", "RightTeeVector;", "RightVectorBar;", "ShortDownArrow;", "ShortLeftArrow;", "SquareSuperset;", "TildeFullEqual;", "UpperLeftArrow;", "ZeroWidthSpace;", "curvearrowleft;", "doublebarwedge;", "downdownarrows;", "hookrightarrow;", "leftleftarrows;", "leftrightarrow;", "leftthreetimes;", "longrightarrow;", "looparrowright;", "nshortparallel;", "ntriangleright;", "rightarrowtail;", "rightharpoonup;", "trianglelefteq;", "upharpoonright;", "ApplyFunction;", "DifferentialD;", "DoubleLeftTee;", "DoubleUpArrow;", "LeftTeeVector;", "LeftVectorBar;", "LessFullEqual;", "LongLeftArrow;", "Longleftarrow;", "NotEqualTilde;", "NotTildeEqual;", "NotTildeTilde;", "Poincareplane;", "PrecedesEqual;", "PrecedesTilde;", "RightArrowBar;", "RightTeeArrow;", "RightTriangle;", "RightUpVector;", "SucceedsEqual;", "SucceedsTilde;", "SupersetEqual;", "UpEquilibrium;", "VerticalTilde;", "VeryThinSpace;", "bigtriangleup;", "blacktriangle;", "divideontimes;", "fallingdotseq;", "hookleftarrow;", "leftarrowtail;", "leftharpoonup;", "longleftarrow;", "looparrowleft;", "measuredangle;", "ntriangleleft;", "shortparallel;", "smallsetminus;", "triangleright;", "upharpoonleft;", "varsubsetneqq;", "varsupsetneqq;", "DownArrowBar;", "DownTeeArrow;", "ExponentialE;", "GreaterEqual;", "GreaterTilde;", "HilbertSpace;", "HumpDownHump;", "Intersection;", "LeftArrowBar;", "LeftTeeArrow;", "LeftTriangle;", "LeftUpVector;", "NotCongruent;", "NotHumpEqual;", "NotLessEqual;", "NotLessTilde;", "Proportional;", "RightCeiling;", "RoundImplies;", "ShortUpArrow;", "SquareSubset;", "UnderBracket;", "VerticalLine;", "blacklozenge;", "exponentiale;", "risingdotseq;", "triangledown;", "triangleleft;", "varsubsetneq;", "varsupsetneq;", "CircleMinus;", "CircleTimes;", "Equilibrium;", "GreaterLess;", "LeftCeiling;", "LessGreater;", "MediumSpace;", "NotLessLess;", "NotPrecedes;", "NotSucceeds;", "NotSuperset;", "OverBracket;", "RightVector;", "Rrightarrow;", "RuleDelayed;", "SmallCircle;", "SquareUnion;", "SubsetEqual;", "UpDownArrow;", "Updownarrow;", "VerticalBar;", "backepsilon;", "blacksquare;", "circledcirc;", "circleddash;", "curlyeqprec;", "curlyeqsucc;", "diamondsuit;", "eqslantless;", "expectation;", "nRightarrow;", "nrightarrow;", "preccurlyeq;", "precnapprox;", "quaternions;", "straightphi;", "succcurlyeq;", "succnapprox;", "thickapprox;", "updownarrow;", "Bernoullis;", "CirclePlus;", "EqualTilde;", "Fouriertrf;", "ImaginaryI;", "Laplacetrf;", "LeftVector;", "Lleftarrow;", "NotElement;", "NotGreater;", "Proportion;", "RightArrow;", "RightFloor;", "Rightarrow;", "ThickSpace;", "TildeEqual;", "TildeTilde;", "UnderBrace;", "UpArrowBar;", "UpTeeArrow;", "circledast;", "complement;", "curlywedge;", "eqslantgtr;", "gtreqqless;", "lessapprox;", "lesseqqgtr;", "lmoustache;", "longmapsto;", "mapstodown;", "mapstoleft;", "nLeftarrow;", "nleftarrow;", "nsubseteqq;", "nsupseteqq;", "precapprox;", "rightarrow;", "rmoustache;", "sqsubseteq;", "sqsupseteq;", "subsetneqq;", "succapprox;", "supsetneqq;", "upuparrows;", "varepsilon;", "varnothing;", "Backslash;", "CenterDot;", "CircleDot;", "Congruent;", "Coproduct;", "DoubleDot;", "DownArrow;", "DownBreve;", "Downarrow;", "HumpEqual;", "LeftArrow;", "LeftFloor;", "Leftarrow;", "LessTilde;", "Mellintrf;", "MinusPlus;", "NotCupCap;", "NotExists;", "NotSubset;", "OverBrace;", "PlusMinus;", "Therefore;", "ThinSpace;", "TripleDot;", "UnionPlus;", "backprime;", "backsimeq;", "bigotimes;", "centerdot;", "checkmark;", "complexes;", "dotsquare;", "downarrow;", "gtrapprox;", "gtreqless;", "gvertneqq;", "heartsuit;", "leftarrow;", "lesseqgtr;", "lvertneqq;", "ngeqslant;", "nleqslant;", "nparallel;", "nshortmid;", "nsubseteq;", "nsupseteq;", "pitchfork;", "rationals;", "spadesuit;", "subseteqq;", "subsetneq;", "supseteqq;", "supsetneq;", "therefore;", "triangleq;", "varpropto;", "DDotrahd;", "DotEqual;", "Integral;", "LessLess;", "NotEqual;", "NotTilde;", "PartialD;", "Precedes;", "RightTee;", "Succeeds;", "SuchThat;", "Superset;", "Uarrocir;", "UnderBar;", "andslope;", "angmsdaa;", "angmsdab;", "angmsdac;", "angmsdad;", "angmsdae;", "angmsdaf;", "angmsdag;", "angmsdah;", "angrtvbd;", "approxeq;", "awconint;", "backcong;", "barwedge;", "bbrktbrk;", "bigoplus;", "bigsqcup;", "biguplus;", "bigwedge;", "boxminus;", "boxtimes;", "bsolhsub;", "capbrcup;", "circledR;", "circledS;", "cirfnint;", "clubsuit;", "cupbrcap;", "curlyvee;", "cwconint;", "doteqdot;", "dotminus;", "drbkarow;", "dzigrarr;", "elinters;", "emptyset;", "eqvparsl;", "fpartint;", "geqslant;", "gesdotol;", "gnapprox;", "hksearow;", "hkswarow;", "imagline;", "imagpart;", "infintie;", "integers;", "intercal;", "intlarhk;", "laemptyv;", "ldrushar;", "leqslant;", "lesdotor;", "llcorner;", "lnapprox;", "lrcorner;", "lurdshar;", "mapstoup;", "multimap;", "naturals;", "ncongdot;", "notindot;", "otimesas;", "parallel;", "plusacir;", "pointint;", "precneqq;", "precnsim;", "profalar;", "profline;", "profsurf;", "raemptyv;", "realpart;", "rppolint;", "rtriltri;", "scpolint;", "setminus;", "shortmid;", "smeparsl;", "sqsubset;", "sqsupset;", "subseteq;", "succneqq;", "succnsim;", "supseteq;", "thetasym;", "thicksim;", "timesbar;", "triangle;", "triminus;", "trpezium;", "ulcorner;", "urcorner;", "varkappa;", "varsigma;", "vartheta;", "Because;", "Cayleys;", "Cconint;", "Cedilla;", "Diamond;", "DownTee;", "Element;", "Epsilon;", "Implies;", "LeftTee;", "NewLine;", "NoBreak;", "NotLess;", "Omicron;", "OverBar;", "Product;", "UpArrow;", "Uparrow;", "Upsilon;", "alefsym;", "angrtvb;", "angzarr;", "asympeq;", "backsim;", "because;", "bemptyv;", "between;", "bigcirc;", "bigodot;", "bigstar;", "bnequiv;", "boxplus;", "ccupssm;", "cemptyv;", "cirscir;", "coloneq;", "congdot;", "cudarrl;", "cudarrr;", "cularrp;", "curarrm;", "dbkarow;", "ddagger;", "ddotseq;", "demptyv;", "diamond;", "digamma;", "dotplus;", "dwangle;", "epsilon;", "eqcolon;", "equivDD;", "gesdoto;", "gtquest;", "gtrless;", "harrcir;", "intprod;", "isindot;", "larrbfs;", "larrsim;", "lbrksld;", "lbrkslu;", "ldrdhar;", "lesdoto;", "lessdot;", "lessgtr;", "lesssim;", "lotimes;", "lozenge;", "ltquest;", "luruhar;", "maltese;", "minusdu;", "napprox;", "natural;", "nearrow;", "nexists;", "notinva;", "notinvb;", "notinvc;", "notniva;", "notnivb;", "notnivc;", "npolint;", "npreceq;", "nsqsube;", "nsqsupe;", "nsubset;", "nsucceq;", "nsupset;", "nvinfin;", "nvltrie;", "nvrtrie;", "nwarrow;", "olcross;", "omicron;", "orderof;", "orslope;", "pertenk;", "planckh;", "pluscir;", "plussim;", "plustwo;", "precsim;", "quatint;", "questeq;", "rarrbfs;", "rarrsim;", "rbrksld;", "rbrkslu;", "rdldhar;", "realine;", "rotimes;", "ruluhar;", "searrow;", "simplus;", "simrarr;", "subedot;", "submult;", "subplus;", "subrarr;", "succsim;", "supdsub;", "supedot;", "suphsol;", "suphsub;", "suplarr;", "supmult;", "supplus;", "swarrow;", "topfork;", "triplus;", "tritime;", "uparrow;", "upsilon;", "uwangle;", "vzigzag;", "zigrarr;", "Aacute;", "Abreve;", "Agrave;", "Assign;", "Atilde;", "Barwed;", "Bumpeq;", "Cacute;", "Ccaron;", "Ccedil;", "Colone;", "Conint;", "CupCap;", "Dagger;", "Dcaron;", "DotDot;", "Dstrok;", "Eacute;", "Ecaron;", "Egrave;", "Exists;", "ForAll;", "Gammad;", "Gbreve;", "Gcedil;", "HARDcy;", "Hstrok;", "Iacute;", "Igrave;", "Itilde;", "Jsercy;", "Kcedil;", "Lacute;", "Lambda;", "Lcaron;", "Lcedil;", "Lmidot;", "Lstrok;", "Nacute;", "Ncaron;", "Ncedil;", "Ntilde;", "Oacute;", "Odblac;", "Ograve;", "Oslash;", "Otilde;", "Otimes;", "Racute;", "Rarrtl;", "Rcaron;", "Rcedil;", "SHCHcy;", "SOFTcy;", "Sacute;", "Scaron;", "Scedil;", "Square;", "Subset;", "Supset;", "Tcaron;", "Tcedil;", "Tstrok;", "Uacute;", "Ubreve;", "Udblac;", "Ugrave;", "Utilde;", "Vdashl;", "Verbar;", "Vvdash;", "Yacute;", "Zacute;", "Zcaron;", "aacute;", "abreve;", "agrave;", "andand;", "angmsd;", "angsph;", "apacir;", "approx;", "atilde;", "barvee;", "barwed;", "becaus;", "bernou;", "bigcap;", "bigcup;", "bigvee;", "bkarow;", "bottom;", "bowtie;", "boxbox;", "bprime;", "brvbar;", "bullet;", "bumpeq;", "cacute;", "capand;", "capcap;", "capcup;", "capdot;", "ccaron;", "ccedil;", "circeq;", "cirmid;", "colone;", "commat;", "compfn;", "conint;", "coprod;", "copysr;", "cularr;", "cupcap;", "cupcup;", "cupdot;", "curarr;", "curren;", "cylcty;", "dagger;", "daleth;", "dcaron;", "dfisht;", "divide;", "divonx;", "dlcorn;", "dlcrop;", "dollar;", "drcorn;", "drcrop;", "dstrok;", "eacute;", "easter;", "ecaron;", "ecolon;", "egrave;", "egsdot;", "elsdot;", "emptyv;", "emsp13;", "emsp14;", "eparsl;", "eqcirc;", "equals;", "equest;", "female;", "ffilig;", "ffllig;", "forall;", "frac12;", "frac13;", "frac14;", "frac15;", "frac16;", "frac18;", "frac23;", "frac25;", "frac34;", "frac35;", "frac38;", "frac45;", "frac56;", "frac58;", "frac78;", "gacute;", "gammad;", "gbreve;", "gesdot;", "gesles;", "gtlPar;", "gtrarr;", "gtrdot;", "gtrsim;", "hairsp;", "hamilt;", "hardcy;", "hearts;", "hellip;", "hercon;", "homtht;", "horbar;", "hslash;", "hstrok;", "hybull;", "hyphen;", "iacute;", "igrave;", "iiiint;", "iinfin;", "incare;", "inodot;", "intcal;", "iquest;", "isinsv;", "itilde;", "jsercy;", "kappav;", "kcedil;", "kgreen;", "lAtail;", "lacute;", "lagran;", "lambda;", "langle;", "larrfs;", "larrhk;", "larrlp;", "larrpl;", "larrtl;", "latail;", "lbrace;", "lbrack;", "lcaron;", "lcedil;", "ldquor;", "lesdot;", "lesges;", "lfisht;", "lfloor;", "lharul;", "llhard;", "lmidot;", "lmoust;", "loplus;", "lowast;", "lowbar;", "lparlt;", "lrhard;", "lsaquo;", "lsquor;", "lstrok;", "lthree;", "ltimes;", "ltlarr;", "ltrPar;", "mapsto;", "marker;", "mcomma;", "midast;", "midcir;", "middot;", "minusb;", "minusd;", "mnplus;", "models;", "mstpos;", "nVDash;", "nVdash;", "nacute;", "nbumpe;", "ncaron;", "ncedil;", "nearhk;", "nequiv;", "nesear;", "nexist;", "nltrie;", "notinE;", "nparsl;", "nprcue;", "nrarrc;", "nrarrw;", "nrtrie;", "nsccue;", "nsimeq;", "ntilde;", "numero;", "nvDash;", "nvHarr;", "nvdash;", "nvlArr;", "nvrArr;", "nwarhk;", "nwnear;", "oacute;", "odblac;", "odsold;", "ograve;", "ominus;", "origof;", "oslash;", "otilde;", "otimes;", "parsim;", "percnt;", "period;", "permil;", "phmmat;", "planck;", "plankv;", "plusdo;", "plusdu;", "plusmn;", "preceq;", "primes;", "prnsim;", "propto;", "prurel;", "puncsp;", "qprime;", "rAtail;", "racute;", "rangle;", "rarrap;", "rarrfs;", "rarrhk;", "rarrlp;", "rarrpl;", "rarrtl;", "ratail;", "rbrace;", "rbrack;", "rcaron;", "rcedil;", "rdquor;", "rfisht;", "rfloor;", "rharul;", "rmoust;", "roplus;", "rpargt;", "rsaquo;", "rsquor;", "rthree;", "rtimes;", "sacute;", "scaron;", "scedil;", "scnsim;", "searhk;", "seswar;", "sfrown;", "shchcy;", "sigmaf;", "sigmav;", "simdot;", "smashp;", "softcy;", "solbar;", "spades;", "sqcaps;", "sqcups;", "sqsube;", "sqsupe;", "square;", "squarf;", "ssetmn;", "ssmile;", "sstarf;", "subdot;", "subset;", "subsim;", "subsub;", "subsup;", "succeq;", "supdot;", "supset;", "supsim;", "supsub;", "supsup;", "swarhk;", "swnwar;", "target;", "tcaron;", "tcedil;", "telrec;", "there4;", "thetav;", "thinsp;", "thksim;", "timesb;", "timesd;", "topbot;", "topcir;", "tprime;", "tridot;", "tstrok;", "uacute;", "ubreve;", "udblac;", "ufisht;", "ugrave;", "ulcorn;", "ulcrop;", "urcorn;", "urcrop;", "utilde;", "vangrt;", "varphi;", "varrho;", "veebar;", "vellip;", "verbar;", "vsubnE;", "vsubne;", "vsupnE;", "vsupne;", "wedbar;", "wedgeq;", "weierp;", "wreath;", "xoplus;", "xotime;", "xsqcup;", "xuplus;", "xwedge;", "yacute;", "zacute;", "zcaron;", "zeetrf;", "AElig;", "Aacute", "Acirc;", "Agrave", "Alpha;", "Amacr;", "Aogon;", "Aring;", "Atilde", "Breve;", "Ccedil", "Ccirc;", "Colon;", "Cross;", "Dashv;", "Delta;", "Eacute", "Ecirc;", "Egrave", "Emacr;", "Eogon;", "Equal;", "Gamma;", "Gcirc;", "Hacek;", "Hcirc;", "IJlig;", "Iacute", "Icirc;", "Igrave", "Imacr;", "Iogon;", "Iukcy;", "Jcirc;", "Jukcy;", "Kappa;", "Ntilde", "OElig;", "Oacute", "Ocirc;", "Ograve", "Omacr;", "Omega;", "Oslash", "Otilde", "Prime;", "RBarr;", "Scirc;", "Sigma;", "THORN;", "TRADE;", "TSHcy;", "Theta;", "Tilde;", "Uacute", "Ubrcy;", "Ucirc;", "Ugrave", "Umacr;", "Union;", "Uogon;", "UpTee;", "Uring;", "VDash;", "Vdash;", "Wcirc;", "Wedge;", "Yacute", "Ycirc;", "aacute", "acirc;", "acute;", "aelig;", "agrave", "aleph;", "alpha;", "amacr;", "amalg;", "angle;", "angrt;", "angst;", "aogon;", "aring;", "asymp;", "atilde", "awint;", "bcong;", "bdquo;", "bepsi;", "blank;", "blk12;", "blk14;", "blk34;", "block;", "boxDL;", "boxDR;", "boxDl;", "boxDr;", "boxHD;", "boxHU;", "boxHd;", "boxHu;", "boxUL;", "boxUR;", "boxUl;", "boxUr;", "boxVH;", "boxVL;", "boxVR;", "boxVh;", "boxVl;", "boxVr;", "boxdL;", "boxdR;", "boxdl;", "boxdr;", "boxhD;", "boxhU;", "boxhd;", "boxhu;", "boxuL;", "boxuR;", "boxul;", "boxur;", "boxvH;", "boxvL;", "boxvR;", "boxvh;", "boxvl;", "boxvr;", "breve;", "brvbar", "bsemi;", "bsime;", "bsolb;", "bumpE;", "bumpe;", "caret;", "caron;", "ccaps;", "ccedil", "ccirc;", "ccups;", "cedil;", "check;", "clubs;", "colon;", "comma;", "crarr;", "cross;", "csube;", "csupe;", "ctdot;", "cuepr;", "cuesc;", "cupor;", "curren", "cuvee;", "cuwed;", "cwint;", "dashv;", "dblac;", "ddarr;", "delta;", "dharl;", "dharr;", "diams;", "disin;", "divide", "doteq;", "dtdot;", "dtrif;", "duarr;", "duhar;", "eDDot;", "eacute", "ecirc;", "efDot;", "egrave", "emacr;", "empty;", "eogon;", "eplus;", "epsiv;", "eqsim;", "equiv;", "erDot;", "erarr;", "esdot;", "exist;", "fflig;", "filig;", "fjlig;", "fllig;", "fltns;", "forkv;", "frac12", "frac14", "frac34", "frasl;", "frown;", "gamma;", "gcirc;", "gescc;", "gimel;", "gneqq;", "gnsim;", "grave;", "gsime;", "gsiml;", "gtcir;", "gtdot;", "harrw;", "hcirc;", "hoarr;", "iacute", "icirc;", "iexcl;", "igrave", "iiint;", "iiota;", "ijlig;", "imacr;", "image;", "imath;", "imped;", "infin;", "iogon;", "iprod;", "iquest", "isinE;", "isins;", "isinv;", "iukcy;", "jcirc;", "jmath;", "jukcy;", "kappa;", "lAarr;", "lBarr;", "langd;", "laquo;", "larrb;", "lates;", "lbarr;", "lbbrk;", "lbrke;", "lceil;", "ldquo;", "lescc;", "lhard;", "lharu;", "lhblk;", "llarr;", "lltri;", "lneqq;", "lnsim;", "loang;", "loarr;", "lobrk;", "lopar;", "lrarr;", "lrhar;", "lrtri;", "lsime;", "lsimg;", "lsquo;", "ltcir;", "ltdot;", "ltrie;", "ltrif;", "mDDot;", "mdash;", "micro;", "middot", "minus;", "mumap;", "nabla;", "napid;", "napos;", "natur;", "nbump;", "ncong;", "ndash;", "neArr;", "nearr;", "nedot;", "nesim;", "ngeqq;", "ngsim;", "nhArr;", "nharr;", "nhpar;", "nlArr;", "nlarr;", "nleqq;", "nless;", "nlsim;", "nltri;", "notin;", "notni;", "npart;", "nprec;", "nrArr;", "nrarr;", "nrtri;", "nsime;", "nsmid;", "nspar;", "nsubE;", "nsube;", "nsucc;", "nsupE;", "nsupe;", "ntilde", "numsp;", "nvsim;", "nwArr;", "nwarr;", "oacute", "ocirc;", "odash;", "oelig;", "ofcir;", "ograve", "ohbar;", "olarr;", "olcir;", "oline;", "omacr;", "omega;", "operp;", "oplus;", "orarr;", "order;", "oslash", "otilde", "ovbar;", "parsl;", "phone;", "plusb;", "pluse;", "plusmn", "pound;", "prcue;", "prime;", "prnap;", "prsim;", "quest;", "rAarr;", "rBarr;", "radic;", "rangd;", "range;", "raquo;", "rarrb;", "rarrc;", "rarrw;", "ratio;", "rbarr;", "rbbrk;", "rbrke;", "rceil;", "rdquo;", "reals;", "rhard;", "rharu;", "rlarr;", "rlhar;", "rnmid;", "roang;", "roarr;", "robrk;", "ropar;", "rrarr;", "rsquo;", "rtrie;", "rtrif;", "sbquo;", "sccue;", "scirc;", "scnap;", "scsim;", "sdotb;", "sdote;", "seArr;", "searr;", "setmn;", "sharp;", "sigma;", "simeq;", "simgE;", "simlE;", "simne;", "slarr;", "smile;", "smtes;", "sqcap;", "sqcup;", "sqsub;", "sqsup;", "srarr;", "starf;", "strns;", "subnE;", "subne;", "supnE;", "supne;", "swArr;", "swarr;", "szlig;", "theta;", "thkap;", "thorn;", "tilde;", "times;", "trade;", "trisb;", "tshcy;", "twixt;", "uacute", "ubrcy;", "ucirc;", "udarr;", "udhar;", "ugrave", "uharl;", "uharr;", "uhblk;", "ultri;", "umacr;", "uogon;", "uplus;", "upsih;", "uring;", "urtri;", "utdot;", "utrif;", "uuarr;", "vBarv;", "vDash;", "varpi;", "vdash;", "veeeq;", "vltri;", "vnsub;", "vnsup;", "vprop;", "vrtri;", "wcirc;", "wedge;", "xcirc;", "xdtri;", "xhArr;", "xharr;", "xlArr;", "xlarr;", "xodot;", "xrArr;", "xrarr;", "xutri;", "yacute", "ycirc;", "AElig", "Acirc", "Aopf;", "Aring", "Ascr;", "Auml;", "Barv;", "Beta;", "Bopf;", "Bscr;", "CHcy;", "COPY;", "Cdot;", "Copf;", "Cscr;", "DJcy;", "DScy;", "DZcy;", "Darr;", "Dopf;", "Dscr;", "Ecirc", "Edot;", "Eopf;", "Escr;", "Esim;", "Euml;", "Fopf;", "Fscr;", "GJcy;", "Gdot;", "Gopf;", "Gscr;", "Hopf;", "Hscr;", "IEcy;", "IOcy;", "Icirc", "Idot;", "Iopf;", "Iota;", "Iscr;", "Iuml;", "Jopf;", "Jscr;", "KHcy;", "KJcy;", "Kopf;", "Kscr;", "LJcy;", "Lang;", "Larr;", "Lopf;", "Lscr;", "Mopf;", "Mscr;", "NJcy;", "Nopf;", "Nscr;", "Ocirc", "Oopf;", "Oscr;", "Ouml;", "Popf;", "Pscr;", "QUOT;", "Qopf;", "Qscr;", "Rang;", "Rarr;", "Ropf;", "Rscr;", "SHcy;", "Sopf;", "Sqrt;", "Sscr;", "Star;", "THORN", "TScy;", "Topf;", "Tscr;", "Uarr;", "Ucirc", "Uopf;", "Upsi;", "Uscr;", "Uuml;", "Vbar;", "Vert;", "Vopf;", "Vscr;", "Wopf;", "Wscr;", "Xopf;", "Xscr;", "YAcy;", "YIcy;", "YUcy;", "Yopf;", "Yscr;", "Yuml;", "ZHcy;", "Zdot;", "Zeta;", "Zopf;", "Zscr;", "acirc", "acute", "aelig", "andd;", "andv;", "ange;", "aopf;", "apid;", "apos;", "aring", "ascr;", "auml;", "bNot;", "bbrk;", "beta;", "beth;", "bnot;", "bopf;", "boxH;", "boxV;", "boxh;", "boxv;", "bscr;", "bsim;", "bsol;", "bull;", "bump;", "caps;", "cdot;", "cedil", "cent;", "chcy;", "cirE;", "circ;", "cire;", "comp;", "cong;", "copf;", "copy;", "cscr;", "csub;", "csup;", "cups;", "dArr;", "dHar;", "darr;", "dash;", "diam;", "djcy;", "dopf;", "dscr;", "dscy;", "dsol;", "dtri;", "dzcy;", "eDot;", "ecir;", "ecirc", "edot;", "emsp;", "ensp;", "eopf;", "epar;", "epsi;", "escr;", "esim;", "euml;", "euro;", "excl;", "flat;", "fnof;", "fopf;", "fork;", "fscr;", "gdot;", "geqq;", "gesl;", "gjcy;", "gnap;", "gneq;", "gopf;", "gscr;", "gsim;", "gtcc;", "gvnE;", "hArr;", "half;", "harr;", "hbar;", "hopf;", "hscr;", "icirc", "iecy;", "iexcl", "imof;", "iocy;", "iopf;", "iota;", "iscr;", "isin;", "iuml;", "jopf;", "jscr;", "khcy;", "kjcy;", "kopf;", "kscr;", "lArr;", "lHar;", "lang;", "laquo", "larr;", "late;", "lcub;", "ldca;", "ldsh;", "leqq;", "lesg;", "ljcy;", "lnap;", "lneq;", "lopf;", "lozf;", "lpar;", "lscr;", "lsim;", "lsqb;", "ltcc;", "ltri;", "lvnE;", "macr;", "male;", "malt;", "micro", "mlcp;", "mldr;", "mopf;", "mscr;", "nGtv;", "nLtv;", "nang;", "napE;", "nbsp;", "ncap;", "ncup;", "ngeq;", "nges;", "ngtr;", "nisd;", "njcy;", "nldr;", "nleq;", "nles;", "nmid;", "nopf;", "npar;", "npre;", "nsce;", "nscr;", "nsim;", "nsub;", "nsup;", "ntgl;", "ntlg;", "nvap;", "nvge;", "nvgt;", "nvle;", "nvlt;", "oast;", "ocir;", "ocirc", "odiv;", "odot;", "ogon;", "oint;", "omid;", "oopf;", "opar;", "ordf;", "ordm;", "oror;", "oscr;", "osol;", "ouml;", "para;", "part;", "perp;", "phiv;", "plus;", "popf;", "pound", "prap;", "prec;", "prnE;", "prod;", "prop;", "pscr;", "qint;", "qopf;", "qscr;", "quot;", "rArr;", "rHar;", "race;", "rang;", "raquo", "rarr;", "rcub;", "rdca;", "rdsh;", "real;", "rect;", "rhov;", "ring;", "ropf;", "rpar;", "rscr;", "rsqb;", "rtri;", "scap;", "scnE;", "sdot;", "sect;", "semi;", "sext;", "shcy;", "sime;", "simg;", "siml;", "smid;", "smte;", "solb;", "sopf;", "spar;", "squf;", "sscr;", "star;", "subE;", "sube;", "succ;", "sung;", "sup1;", "sup2;", "sup3;", "supE;", "supe;", "szlig", "tbrk;", "tdot;", "thorn", "times", "tint;", "toea;", "topf;", "tosa;", "trie;", "tscr;", "tscy;", "uArr;", "uHar;", "uarr;", "ucirc", "uopf;", "upsi;", "uscr;", "utri;", "uuml;", "vArr;", "vBar;", "varr;", "vert;", "vopf;", "vscr;", "wopf;", "wscr;", "xcap;", "xcup;", "xmap;", "xnis;", "xopf;", "xscr;", "xvee;", "yacy;", "yicy;", "yopf;", "yscr;", "yucy;", "yuml;", "zdot;", "zeta;", "zhcy;", "zopf;", "zscr;", "zwnj;", "AMP;", "Acy;", "Afr;", "And;", "Auml", "Bcy;", "Bfr;", "COPY", "Cap;", "Cfr;", "Chi;", "Cup;", "Dcy;", "Del;", "Dfr;", "Dot;", "ENG;", "ETH;", "Ecy;", "Efr;", "Eta;", "Euml", "Fcy;", "Ffr;", "Gcy;", "Gfr;", "Hat;", "Hfr;", "Icy;", "Ifr;", "Int;", "Iuml", "Jcy;", "Jfr;", "Kcy;", "Kfr;", "Lcy;", "Lfr;", "Lsh;", "Map;", "Mcy;", "Mfr;", "Ncy;", "Nfr;", "Not;", "Ocy;", "Ofr;", "Ouml", "Pcy;", "Pfr;", "Phi;", "Psi;", "QUOT", "Qfr;", "REG;", "Rcy;", "Rfr;", "Rho;", "Rsh;", "Scy;", "Sfr;", "Sub;", "Sum;", "Sup;", "Tab;", "Tau;", "Tcy;", "Tfr;", "Ucy;", "Ufr;", "Uuml", "Vcy;", "Vee;", "Vfr;", "Wfr;", "Xfr;", "Ycy;", "Yfr;", "Zcy;", "Zfr;", "acE;", "acd;", "acy;", "afr;", "amp;", "and;", "ang;", "apE;", "ape;", "ast;", "auml", "bcy;", "bfr;", "bne;", "bot;", "cap;", "cent", "cfr;", "chi;", "cir;", "copy", "cup;", "dcy;", "deg;", "dfr;", "die;", "div;", "dot;", "ecy;", "efr;", "egs;", "ell;", "els;", "eng;", "eta;", "eth;", "euml", "fcy;", "ffr;", "gEl;", "gap;", "gcy;", "gel;", "geq;", "ges;", "gfr;", "ggg;", "glE;", "gla;", "glj;", "gnE;", "gne;", "hfr;", "icy;", "iff;", "ifr;", "int;", "iuml", "jcy;", "jfr;", "kcy;", "kfr;", "lEg;", "lap;", "lat;", "lcy;", "leg;", "leq;", "les;", "lfr;", "lgE;", "lnE;", "lne;", "loz;", "lrm;", "lsh;", "macr", "map;", "mcy;", "mfr;", "mho;", "mid;", "nGg;", "nGt;", "nLl;", "nLt;", "nap;", "nbsp", "ncy;", "nfr;", "ngE;", "nge;", "ngt;", "nis;", "niv;", "nlE;", "nle;", "nlt;", "not;", "npr;", "nsc;", "num;", "ocy;", "ofr;", "ogt;", "ohm;", "olt;", "ord;", "ordf", "ordm", "orv;", "ouml", "par;", "para", "pcy;", "pfr;", "phi;", "piv;", "prE;", "pre;", "psi;", "qfr;", "quot", "rcy;", "reg;", "rfr;", "rho;", "rlm;", "rsh;", "scE;", "sce;", "scy;", "sect", "sfr;", "shy;", "sim;", "smt;", "sol;", "squ;", "sub;", "sum;", "sup1", "sup2", "sup3", "sup;", "tau;", "tcy;", "tfr;", "top;", "ucy;", "ufr;", "uml;", "uuml", "vcy;", "vee;", "vfr;", "wfr;", "xfr;", "ycy;", "yen;", "yfr;", "yuml", "zcy;", "zfr;", "zwj;", "AMP", "DD;", "ETH", "GT;", "Gg;", "Gt;", "Im;", "LT;", "Ll;", "Lt;", "Mu;", "Nu;", "Or;", "Pi;", "Pr;", "REG", "Re;", "Sc;", "Xi;", "ac;", "af;", "amp", "ap;", "dd;", "deg", "ee;", "eg;", "el;", "eth", "gE;", "ge;", "gg;", "gl;", "gt;", "ic;", "ii;", "in;", "it;", "lE;", "le;", "lg;", "ll;", "lt;", "mp;", "mu;", "ne;", "ni;", "not", "nu;", "oS;", "or;", "pi;", "pm;", "pr;", "reg", "rx;", "sc;", "shy", "uml", "wp;", "wr;", "xi;", "yen", "GT", "LT", "gt", "lt"};
	private static int[] entityValues=new int[]{8755, 8754, 10234, -1, 733, 8931, 8221, 8751, 9642, 8203, 8928, 8941, 8929, 8517, 8660, 10233, 9643, 8811, 8742, -2, 8940, 8930, 8220, 10607, 10232, 10576, 8646, 8203, -3, -4, 8644, 8850, 8621, 10591, 10583, 10231, 10234, 8203, -5, 8828, 8651, 10215, 10589, 10581, 8885, 8851, 8829, 9656, 10231, 8661, 8741, 10590, 10582, 9724, 10878, 10214, 10593, 10585, 8884, 8203, -6, -7, -8, 8716, -9, 8775, 10217, 10575, 8849, 10072, 9662, 9666, 8651, 8652, 8608, 180, 96, 732, 8658, 8693, 9723, 8923, 8807, 10216, 10577, 8922, 160, -10, 8939, -11, -12, 8841, 10704, 10588, 10580, 9181, 8645, 8635, 8642, 8941, 8641, 8649, 8606, 8883, 8217, 8750, 8659, 8656, 8641, 10574, 10703, 10592, 10584, 8600, 8817, 8821, -13, 8938, -14, 9180, 8642, 8594, 8599, 9661, 8634, 8631, 8643, 8637, 8646, 8654, 8622, 8940, 8644, 8605, 8908, 1013, 8885, 8882, 729, 8872, 8637, 10914, 9472, 8291, 8290, 8643, 8596, 8660, 10877, 10230, 10233, 8601, 8810, 8825, 8824, 8840, 8740, 8216, 8715, 10587, 10579, 8595, 8592, 8848, 8773, 8598, 8203, 8630, 8966, 8650, 8618, 8647, 8596, 8907, 10230, 8620, 8742, 8939, 8611, 8640, 8884, 8638, 8289, 8518, 10980, 8657, 10586, 10578, 8806, 10229, 10232, -15, 8772, 8777, 8460, 10927, 8830, 8677, 8614, 8883, 8638, 10928, 8831, 8839, 10606, 8768, 8202, 9651, 9652, 8903, 8786, 8617, 8610, 8636, 10229, 8619, 8737, 8938, 8741, 8726, 9657, 8639, -16, -17, 10515, 8615, 8519, 8805, 8819, 8459, 8782, 8898, 8676, 8612, 8882, 8639, 8802, -18, 8816, 8820, 8733, 8969, 10608, 8593, 8847, 9141, 124, 10731, 8519, 8787, 9663, 9667, -19, -20, 8854, 8855, 8652, 8823, 8968, 8822, 8287, -21, 8832, 8833, -22, 9140, 8640, 8667, 10740, 8728, 8852, 8838, 8597, 8661, 8739, 1014, 9642, 8858, 8861, 8926, 8927, 9830, 10901, 8496, 8655, 8603, 8828, 10937, 8461, 981, 8829, 10938, 8776, 8597, 8492, 8853, 8770, 8497, 8520, 8466, 8636, 8666, 8713, 8815, 8759, 8594, 8971, 8658, -23, 8771, 8776, 9183, 10514, 8613, 8859, 8705, 8911, 10902, 10892, 10885, 10891, 9136, 10236, 8615, 8612, 8653, 8602, -24, -25, 10935, 8594, 9137, 8849, 8850, 10955, 10936, 10956, 8648, 1013, 8709, 8726, 183, 8857, 8801, 8720, 168, 8595, 785, 8659, 8783, 8592, 8970, 8656, 8818, 8499, 8723, 8813, 8708, -26, 9182, 177, 8756, 8201, 8411, 8846, 8245, 8909, 10754, 183, 10003, 8450, 8865, 8595, 10886, 8923, -27, 9829, 8592, 8922, -28, -29, -30, 8742, 8740, 8840, 8841, 8916, 8474, 9824, 10949, 8842, 10950, 8843, 8756, 8796, 8733, 10513, 8784, 8747, 10913, 8800, 8769, 8706, 8826, 8866, 8827, 8715, 8835, 10569, 95, 10840, 10664, 10665, 10666, 10667, 10668, 10669, 10670, 10671, 10653, 8778, 8755, 8780, 8965, 9142, 10753, 10758, 10756, 8896, 8863, 8864, 10184, 10825, 174, 9416, 10768, 9827, 10824, 8910, 8754, 8785, 8760, 10512, 10239, 9191, 8709, 10725, 10765, 10878, 10884, 10890, 10533, 10534, 8464, 8465, 10717, 8484, 8890, 10775, 10676, 10571, 10877, 10883, 8990, 10889, 8991, 10570, 8613, 8888, 8469, -31, -32, 10806, 8741, 10787, 10773, 10933, 8936, 9006, 8978, 8979, 10675, 8476, 10770, 10702, 10771, 8726, 8739, 10724, 8847, 8848, 8838, 10934, 8937, 8839, 977, 8764, 10801, 9653, 10810, 9186, 8988, 8989, 1008, 962, 977, 8757, 8493, 8752, 184, 8900, 8868, 8712, 917, 8658, 8867, 10, 8288, 8814, 927, 8254, 8719, 8593, 8657, 933, 8501, 8894, 9084, 8781, 8765, 8757, 10672, 8812, 9711, 10752, 9733, -33, 8862, 10832, 10674, 10690, 8788, 10861, 10552, 10549, 10557, 10556, 10511, 8225, 10871, 10673, 8900, 989, 8724, 10662, 949, 8789, 10872, 10882, 10876, 8823, 10568, 10812, 8949, 10527, 10611, 10639, 10637, 10599, 10881, 8918, 8822, 8818, 10804, 9674, 10875, 10598, 10016, 10794, 8777, 9838, 8599, 8708, 8713, 8951, 8950, 8716, 8958, 8957, 10772, -34, 8930, 8931, -35, -36, -37, 10718, -38, -39, 8598, 10683, 959, 8500, 10839, 8241, 8462, 10786, 10790, 10791, 8830, 10774, 8799, 10528, 10612, 10638, 10640, 10601, 8475, 10805, 10600, 8600, 10788, 10610, 10947, 10945, 10943, 10617, 8831, 10968, 10948, 10185, 10967, 10619, 10946, 10944, 8601, 10970, 10809, 10811, 8593, 965, 10663, 10650, 8669, 193, 258, 192, 8788, 195, 8966, 8782, 262, 268, 199, 10868, 8751, 8781, 8225, 270, 8412, 272, 201, 282, 200, 8707, 8704, 988, 286, 290, 1066, 294, 205, 204, 296, 1032, 310, 313, 923, 317, 315, 319, 321, 323, 327, 325, 209, 211, 336, 210, 216, 213, 10807, 340, 10518, 344, 342, 1065, 1068, 346, 352, 350, 9633, 8912, 8913, 356, 354, 358, 218, 364, 368, 217, 360, 10982, 8214, 8874, 221, 377, 381, 225, 259, 224, 10837, 8737, 8738, 10863, 8776, 227, 8893, 8965, 8757, 8492, 8898, 8899, 8897, 10509, 8869, 8904, 10697, 8245, 166, 8226, 8783, 263, 10820, 10827, 10823, 10816, 269, 231, 8791, 10991, 8788, 64, 8728, 8750, 8720, 8471, 8630, 10822, 10826, 8845, 8631, 164, 9005, 8224, 8504, 271, 10623, 247, 8903, 8990, 8973, 36, 8991, 8972, 273, 233, 10862, 283, 8789, 232, 10904, 10903, 8709, 8196, 8197, 10723, 8790, 61, 8799, 9792, 64259, 64260, 8704, 189, 8531, 188, 8533, 8537, 8539, 8532, 8534, 190, 8535, 8540, 8536, 8538, 8541, 8542, 501, 989, 287, 10880, 10900, 10645, 10616, 8919, 8819, 8202, 8459, 1098, 9829, 8230, 8889, 8763, 8213, 8463, 295, 8259, 8208, 237, 236, 10764, 10716, 8453, 305, 8890, 191, 8947, 297, 1112, 1008, 311, 312, 10523, 314, 8466, 955, 10216, 10525, 8617, 8619, 10553, 8610, 10521, 123, 91, 318, 316, 8222, 10879, 10899, 10620, 8970, 10602, 10603, 320, 9136, 10797, 8727, 95, 10643, 10605, 8249, 8218, 322, 8907, 8905, 10614, 10646, 8614, 9646, 10793, 42, 10992, 183, 8863, 8760, 8723, 8871, 8766, 8879, 8878, 324, -40, 328, 326, 10532, 8802, 10536, 8708, 8940, -41, -42, 8928, -43, -44, 8941, 8929, 8772, 241, 8470, 8877, 10500, 8876, 10498, 10499, 10531, 10535, 243, 337, 10684, 242, 8854, 8886, 248, 245, 8855, 10995, 37, 46, 8240, 8499, 8463, 8463, 8724, 10789, 177, 10927, 8473, 8936, 8733, 8880, 8200, 8279, 10524, 341, 10217, 10613, 10526, 8618, 8620, 10565, 8611, 10522, 125, 93, 345, 343, 8221, 10621, 8971, 10604, 9137, 10798, 10644, 8250, 8217, 8908, 8906, 347, 353, 351, 8937, 10533, 10537, 8994, 1097, 962, 962, 10858, 10803, 1100, 9023, 9824, -45, -46, 8849, 8850, 9633, 9642, 8726, 8995, 8902, 10941, 8834, 10951, 10965, 10963, 10928, 10942, 8835, 10952, 10964, 10966, 10534, 10538, 8982, 357, 355, 8981, 8756, 977, 8201, 8764, 8864, 10800, 9014, 10993, 8244, 9708, 359, 250, 365, 369, 10622, 249, 8988, 8975, 8989, 8974, 361, 10652, 981, 1009, 8891, 8942, 124, -47, -48, -49, -50, 10847, 8793, 8472, 8768, 10753, 10754, 10758, 10756, 8896, 253, 378, 382, 8488, 198, 193, 194, 192, 913, 256, 260, 197, 195, 728, 199, 264, 8759, 10799, 10980, 916, 201, 202, 200, 274, 280, 10869, 915, 284, 711, 292, 306, 205, 206, 204, 298, 302, 1030, 308, 1028, 922, 209, 338, 211, 212, 210, 332, 937, 216, 213, 8243, 10512, 348, 931, 222, 8482, 1035, 920, 8764, 218, 1038, 219, 217, 362, 8899, 370, 8869, 366, 8875, 8873, 372, 8896, 221, 374, 225, 226, 180, 230, 224, 8501, 945, 257, 10815, 8736, 8735, 197, 261, 229, 8776, 227, 10769, 8780, 8222, 1014, 9251, 9618, 9617, 9619, 9608, 9559, 9556, 9558, 9555, 9574, 9577, 9572, 9575, 9565, 9562, 9564, 9561, 9580, 9571, 9568, 9579, 9570, 9567, 9557, 9554, 9488, 9484, 9573, 9576, 9516, 9524, 9563, 9560, 9496, 9492, 9578, 9569, 9566, 9532, 9508, 9500, 728, 166, 8271, 8909, 10693, 10926, 8783, 8257, 711, 10829, 231, 265, 10828, 184, 10003, 9827, 58, 44, 8629, 10007, 10961, 10962, 8943, 8926, 8927, 10821, 164, 8910, 8911, 8753, 8867, 733, 8650, 948, 8643, 8642, 9830, 8946, 247, 8784, 8945, 9662, 8693, 10607, 10871, 233, 234, 8786, 232, 275, 8709, 281, 10865, 1013, 8770, 8801, 8787, 10609, 8784, 8707, 64256, 64257, -51, 64258, 9649, 10969, 189, 188, 190, 8260, 8994, 947, 285, 10921, 8503, 8809, 8935, 96, 10894, 10896, 10874, 8919, 8621, 293, 8703, 237, 238, 161, 236, 8749, 8489, 307, 299, 8465, 305, 437, 8734, 303, 10812, 191, 8953, 8948, 8712, 1110, 309, 567, 1108, 954, 8666, 10510, 10641, 171, 8676, -52, 10508, 10098, 10635, 8968, 8220, 10920, 8637, 8636, 9604, 8647, 9722, 8808, 8934, 10220, 8701, 10214, 10629, 8646, 8651, 8895, 10893, 10895, 8216, 10873, 8918, 8884, 9666, 8762, 8212, 181, 183, 8722, 8888, 8711, -53, 329, 9838, -54, 8775, 8211, 8663, 8599, -55, -56, -57, 8821, 8654, 8622, 10994, 8653, 8602, -58, 8814, 8820, 8938, 8713, 8716, -59, 8832, 8655, 8603, 8939, 8772, 8740, 8742, -60, 8840, 8833, -61, 8841, 241, 8199, -62, 8662, 8598, 243, 244, 8861, 339, 10687, 242, 10677, 8634, 10686, 8254, 333, 969, 10681, 8853, 8635, 8500, 248, 245, 9021, 11005, 9742, 8862, 10866, 177, 163, 8828, 8242, 10937, 8830, 63, 8667, 10511, 8730, 10642, 10661, 187, 8677, 10547, 8605, 8758, 10509, 10099, 10636, 8969, 8221, 8477, 8641, 8640, 8644, 8652, 10990, 10221, 8702, 10215, 10630, 8649, 8217, 8885, 9656, 8218, 8829, 349, 10938, 8831, 8865, 10854, 8664, 8600, 8726, 9839, 963, 8771, 10912, 10911, 8774, 8592, 8995, -63, 8851, 8852, 8847, 8848, 8594, 9733, 175, 10955, 8842, 10956, 8843, 8665, 8601, 223, 952, 8776, 254, 732, 215, 8482, 10701, 1115, 8812, 250, 1118, 251, 8645, 10606, 249, 8639, 8638, 9600, 9720, 363, 371, 8846, 978, 367, 9721, 8944, 9652, 8648, 10985, 8872, 982, 8866, 8794, 8882, -64, -65, 8733, 8883, 373, 8743, 9711, 9661, 10234, 10231, 10232, 10229, 10752, 10233, 10230, 9651, 253, 375, 198, 194, 120120, 197, 119964, 196, 10983, 914, 120121, 8492, 1063, 169, 266, 8450, 119966, 1026, 1029, 1039, 8609, 120123, 119967, 202, 278, 120124, 8496, 10867, 203, 120125, 8497, 1027, 288, 120126, 119970, 8461, 8459, 1045, 1025, 206, 304, 120128, 921, 8464, 207, 120129, 119973, 1061, 1036, 120130, 119974, 1033, 10218, 8606, 120131, 8466, 120132, 8499, 1034, 8469, 119977, 212, 120134, 119978, 214, 8473, 119979, 34, 8474, 119980, 10219, 8608, 8477, 8475, 1064, 120138, 8730, 119982, 8902, 222, 1062, 120139, 119983, 8607, 219, 120140, 978, 119984, 220, 10987, 8214, 120141, 119985, 120142, 119986, 120143, 119987, 1071, 1031, 1070, 120144, 119988, 376, 1046, 379, 918, 8484, 119989, 226, 180, 230, 10844, 10842, 10660, 120146, 8779, 39, 229, 119990, 228, 10989, 9141, 946, 8502, 8976, 120147, 9552, 9553, 9472, 9474, 119991, 8765, 92, 8226, 8782, -66, 267, 184, 162, 1095, 10691, 710, 8791, 8705, 8773, 120148, 169, 119992, 10959, 10960, -67, 8659, 10597, 8595, 8208, 8900, 1106, 120149, 119993, 1109, 10742, 9663, 1119, 8785, 8790, 234, 279, 8195, 8194, 120150, 8917, 949, 8495, 8770, 235, 8364, 33, 9837, 402, 120151, 8916, 119995, 289, 8807, -68, 1107, 10890, 10888, 120152, 8458, 8819, 10919, -69, 8660, 189, 8596, 8463, 120153, 119997, 238, 1077, 161, 8887, 1105, 120154, 953, 119998, 8712, 239, 120155, 119999, 1093, 1116, 120156, 120000, 8656, 10594, 10216, 171, 8592, 10925, 123, 10550, 8626, 8806, -70, 1113, 10889, 10887, 120157, 10731, 40, 120001, 8818, 91, 10918, 9667, -71, 175, 9794, 10016, 181, 10971, 8230, 120158, 120002, -72, -73, -74, -75, 160, 10819, 10818, 8817, -76, 8815, 8954, 1114, 8229, 8816, -77, 8740, 120159, 8742, -78, -79, 120003, 8769, 8836, 8837, 8825, 8824, -80, -81, -82, -83, -84, 8859, 8858, 244, 10808, 8857, 731, 8750, 10678, 120160, 10679, 170, 186, 10838, 8500, 8856, 246, 182, 8706, 8869, 981, 43, 120161, 163, 10935, 8826, 10933, 8719, 8733, 120005, 10764, 120162, 120006, 34, 8658, 10596, -85, 10217, 187, 8594, 125, 10551, 8627, 8476, 9645, 1009, 730, 120163, 41, 120007, 93, 9657, 10936, 10934, 8901, 167, 59, 10038, 1096, 8771, 10910, 10909, 8739, 10924, 10692, 120164, 8741, 9642, 120008, 9734, 10949, 8838, 8827, 9834, 185, 178, 179, 10950, 8839, 223, 9140, 8411, 254, 215, 8749, 10536, 120165, 10537, 8796, 120009, 1094, 8657, 10595, 8593, 251, 120166, 965, 120010, 9653, 252, 8661, 10984, 8597, 124, 120167, 120011, 120168, 120012, 8898, 8899, 10236, 8955, 120169, 120013, 8897, 1103, 1111, 120170, 120014, 1102, 255, 380, 950, 1078, 120171, 120015, 8204, 38, 1040, 120068, 10835, 196, 1041, 120069, 169, 8914, 8493, 935, 8915, 1044, 8711, 120071, 168, 330, 208, 1069, 120072, 919, 203, 1060, 120073, 1043, 120074, 94, 8460, 1048, 8465, 8748, 207, 1049, 120077, 1050, 120078, 1051, 120079, 8624, 10501, 1052, 120080, 1053, 120081, 10988, 1054, 120082, 214, 1055, 120083, 934, 936, 34, 120084, 174, 1056, 8476, 929, 8625, 1057, 120086, 8912, 8721, 8913, 9, 932, 1058, 120087, 1059, 120088, 220, 1042, 8897, 120089, 120090, 120091, 1067, 120092, 1047, 8488, -86, 8767, 1072, 120094, 38, 8743, 8736, 10864, 8778, 42, 228, 1073, 120095, -87, 8869, 8745, 162, 120096, 967, 9675, 169, 8746, 1076, 176, 120097, 168, 247, 729, 1101, 120098, 10902, 8467, 10901, 331, 951, 240, 235, 1092, 120099, 10892, 10886, 1075, 8923, 8805, 10878, 120100, 8921, 10898, 10917, 10916, 8809, 10888, 120101, 1080, 8660, 120102, 8747, 239, 1081, 120103, 1082, 120104, 10891, 10885, 10923, 1083, 8922, 8804, 10877, 120105, 10897, 8808, 10887, 9674, 8206, 8624, 175, 8614, 1084, 120106, 8487, 8739, -88, -89, -90, -91, 8777, 160, 1085, 120107, -92, 8817, 8815, 8956, 8715, -93, 8816, 8814, 172, 8832, 8833, 35, 1086, 120108, 10689, 937, 10688, 10845, 170, 186, 10843, 246, 8741, 182, 1087, 120109, 966, 982, 10931, 10927, 968, 120110, 34, 1088, 174, 120111, 961, 8207, 8625, 10932, 10928, 1089, 167, 120112, 173, 8764, 10922, 47, 9633, 8834, 8721, 185, 178, 179, 8835, 964, 1090, 120113, 8868, 1091, 120114, 168, 252, 1074, 8744, 120115, 120116, 120117, 1099, 165, 120118, 255, 1079, 120119, 8205, 38, 8517, 208, 62, 8921, 8811, 8465, 60, 8920, 8810, 924, 925, 10836, 928, 10939, 174, 8476, 10940, 926, 8766, 8289, 38, 8776, 8518, 176, 8519, 10906, 10905, 240, 8807, 8805, 8811, 8823, 62, 8291, 8520, 8712, 8290, 8806, 8804, 8822, 8810, 60, 8723, 956, 8800, 8715, 172, 957, 9416, 8744, 960, 177, 8826, 174, 8478, 8827, 173, 168, 8472, 8768, 958, 165, 62, 60, 62, 60};
	private static int[] entityDoubles=new int[]{10914, 824, 10878, 824, 8807, 824, 10704, 824, 10703, 824, 8811, 824, 10877, 824, 10913, 824, 8848, 824, 10927, 824, 10928, 824, 8831, 824, 8782, 824, 8847, 824, 8770, 824, 10955, 65024, 10956, 65024, 8783, 824, 8842, 65024, 8843, 65024, 8810, 824, 8835, 8402, 8287, 8202, 10949, 824, 10950, 824, 8834, 8402, 8809, 65024, 8808, 65024, 10878, 824, 10877, 824, 10861, 824, 8949, 824, 8801, 8421, 10927, 824, 8834, 8402, 10928, 824, 8835, 8402, 8884, 8402, 8885, 8402, 8783, 824, 8953, 824, 11005, 8421, 10547, 824, 8605, 824, 8851, 65024, 8852, 65024, 10955, 65024, 8842, 65024, 10956, 65024, 8843, 65024, 102, 106, 10925, 65024, 8779, 824, 8782, 824, 8784, 824, 8770, 824, 8807, 824, 8806, 824, 8706, 824, 10949, 824, 10950, 824, 8764, 8402, 10924, 65024, 8834, 8402, 8835, 8402, 8745, 65024, 8746, 65024, 8923, 65024, 8809, 65024, 8922, 65024, 8808, 65024, 8811, 824, 8810, 824, 8736, 8402, 10864, 824, 10878, 824, 10877, 824, 10927, 824, 10928, 824, 8781, 8402, 8805, 8402, 62, 8402, 8804, 8402, 60, 8402, 8765, 817, 8766, 819, 61, 8421, 8921, 824, 8811, 8402, 8920, 824, 8810, 8402, 8807, 824, 8806, 824};



	public static readonly string MATHML_NAMESPACE = "http://www.w3.org/1998/Math/MathML";

	public static readonly string SVG_NAMESPACE = "http://www.w3.org/2000/svg";


	internal static int TOKEN_EOF= 0x10000000;

	internal static int TOKEN_START_TAG= 0x20000000;

	internal static int TOKEN_END_TAG= 0x30000000;

	internal static int TOKEN_COMMENT=0x40000000;

	internal static int TOKEN_DOCTYPE=0x50000000;
	internal static int TOKEN_TYPE_MASK=unchecked((int)0xF0000000);
	internal static int TOKEN_CHARACTER=0x00000000;
	private static int TOKEN_INDEX_MASK=0x0FFFFFFF;
	public static string HTML_NAMESPACE="http://www.w3.org/1999/xhtml";

	private static string[] quirksModePublicIdPrefixes=new string[]{
		"+//silmaril//dtd html pro v0r11 19970101//",
		"-//advasoft ltd//dtd html 3.0 aswedit + extensions//",
		"-//as//dtd html 3.0 aswedit + extensions//",
		"-//ietf//dtd html 2.0 level 1//",
		"-//ietf//dtd html 2.0 level 2//",
		"-//ietf//dtd html 2.0 strict level 1//",
		"-//ietf//dtd html 2.0 strict level 2//",
		"-//ietf//dtd html 2.0 strict//",
		"-//ietf//dtd html 2.0//",
		"-//ietf//dtd html 2.1e//",
		"-//ietf//dtd html 3.0//",
		"-//ietf//dtd html 3.2 final//",
		"-//ietf//dtd html 3.2//",
		"-//ietf//dtd html 3//",
		"-//ietf//dtd html level 0//",
		"-//ietf//dtd html level 1//",
		"-//ietf//dtd html level 2//",
		"-//ietf//dtd html level 3//",
		"-//ietf//dtd html strict level 0//",
		"-//ietf//dtd html strict level 1//",
		"-//ietf//dtd html strict level 2//",
		"-//ietf//dtd html strict level 3//",
		"-//ietf//dtd html strict//",
		"-//ietf//dtd html//",
		"-//metrius//dtd metrius presentational//",
		"-//microsoft//dtd internet explorer 2.0 html strict//",
		"-//microsoft//dtd internet explorer 2.0 html//",
		"-//microsoft//dtd internet explorer 2.0 tables//",
		"-//microsoft//dtd internet explorer 3.0 html strict//",
		"-//microsoft//dtd internet explorer 3.0 html//",
		"-//microsoft//dtd internet explorer 3.0 tables//",
		"-//netscape comm. corp.//dtd html//",
		"-//netscape comm. corp.//dtd strict html//",
		"-//o'reilly and associates//dtd html 2.0//",
		"-//o'reilly and associates//dtd html extended 1.0//",
		"-//o'reilly and associates//dtd html extended relaxed 1.0//",
		"-//softquad software//dtd hotmetal pro 6.0::19990601::extensions to html 4.0//",
		"-//softquad//dtd hotmetal pro 4.0::19971010::extensions to html 4.0//",
		"-//spyglass//dtd html 2.0 extended//",
		"-//sq//dtd html 2.0 hotmetal + extensions//",
		"-//sun microsystems corp.//dtd hotjava html//",
		"-//sun microsystems corp.//dtd hotjava strict html//",
		"-//w3c//dtd html 3 1995-03-24//",
		"-//w3c//dtd html 3.2 draft//",
		"-//w3c//dtd html 3.2 final//",
		"-//w3c//dtd html 3.2//",
		"-//w3c//dtd html 3.2s draft//",
		"-//w3c//dtd html 4.0 frameset//",
		"-//w3c//dtd html 4.0 transitional//",
		"-//w3c//dtd html experimental 19960712//",
		"-//w3c//dtd html experimental 970421//",
		"-//w3c//dtd w3 html//",
		"-//w3o//dtd w3 html 3.0//",
		"-//webtechs//dtd mozilla html 2.0//",
		"-//webtechs//dtd mozilla html//"
	};


	private ConditionalBufferInputStream inputStream;
	private IMarkableCharacterInput charInput=null;
	private EncodingConfidence encoding=null;


	private bool error=false;
	private TokenizerState lastState=TokenizerState.Data;
	private CommentToken lastComment;
	private DocTypeToken docTypeToken;
	private IList<Element> integrationElements=new List<Element>();
	private IList<IToken> tokens=new List<IToken>();
	private TagToken lastStartTag=null;
	private Html5Decoder decoder=null;
	private TagToken currentEndTag=null;
	private TagToken currentTag=null;
	private Attrib currentAttribute=null;
	private int bogusCommentCharacter=0;
	private IntList tempBuffer=new IntList();
	private TokenizerState state=TokenizerState.Data;
	private bool framesetOk=true;
	private IList<int> tokenQueue=new List<int>();
	private InsertionMode insertionMode=InsertionMode.Initial;
	private InsertionMode originalInsertionMode=InsertionMode.Initial;
	private IList<Element> openElements=new List<Element>();
	private IList<FormattingElement> formattingElements=new List<FormattingElement>();
	private Element headElement=null;
	private Element formElement=null;
	private Element inputElement=null;
	private string baseurl=null;
	private bool hasForeignContent=false;
	internal Document document=null;
	private bool done=false;

	private IntList pendingTableCharacters=new IntList();
	private bool doFosterParent;
	private Element context;
	private bool noforeign;
	private string address;

	private void initialize(){
		noforeign=false;
		document=new Document();
		document.baseurl=address;
		context=null;
		openElements.Clear();
		error=false;
		baseurl=null;
		hasForeignContent=false; // performance optimization
		lastState=TokenizerState.Data;
		lastComment=null;
		docTypeToken=null;
		tokens.Clear();
		lastStartTag=null;
		currentEndTag=null;
		currentTag=null;
		currentAttribute=null;
		bogusCommentCharacter=0;
		tempBuffer.clearAll();
		state=TokenizerState.Data;
		framesetOk=true;
		integrationElements.Clear();
		tokenQueue.Clear();
		insertionMode=InsertionMode.Initial;
		originalInsertionMode=InsertionMode.Initial;
		formattingElements.Clear();
		doFosterParent=false;
		headElement=null;
		formElement=null;
		inputElement=null;
		done=false;
		pendingTableCharacters.clearAll();
	}

	public HtmlParser(PeterO.Support.InputStream source, string address, string charset) {
		if(source==null)throw new ArgumentException();
		if(address!=null && address.Length>0){
			URL url=URL.parse(address);
			if(url==null || url.getScheme().Length==0)
				throw new ArgumentException();
		}
		this.address=address;
		initialize();
		inputStream=new ConditionalBufferInputStream(source);
		encoding=CharsetSniffer.sniffEncoding(inputStream,charset);
		inputStream.rewind();
		decoder=new Html5Decoder(TextEncoding.getDecoder(encoding.getEncoding()));
		charInput=new StackableCharacterInput(new DecoderCharacterInput(inputStream,decoder));
	}
	public HtmlParser(PeterO.Support.InputStream s, string _string) : this(s,_string,null) {
	}


	private static T removeAtIndex<T>(IList<T> array, int index){
		T ret=array[index];
		array.RemoveAt(index);
		return ret;
	}

	private bool isMathMLTextIntegrationPoint(Element element) {
		string name=element.getLocalName();
		return MATHML_NAMESPACE.Equals(element.getNamespaceURI()) && (
				name.Equals("mi") ||
				name.Equals("mo") ||
				name.Equals("mn") ||
				name.Equals("ms") ||
				name.Equals("mtext"));
	}

	private bool isHtmlIntegrationPoint(Element element) {
		if(integrationElements.Contains(element))
			return true;
		string name=element.getLocalName();
		return SVG_NAMESPACE.Equals(element.getNamespaceURI()) && (
				name.Equals("foreignObject") ||
				name.Equals("desc") ||
				name.Equals("title"));
	}



	private bool isForeignContext(int token){
		if(hasForeignContent && token!=TOKEN_EOF){
			Element element=(context!=null && openElements.Count==1) ?
					context : getCurrentNode(); // adjusted current node
			if(element==null)return false;
			if(element.getNamespaceURI().Equals(HTML_NAMESPACE))
				return false;
			if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=element.getLocalName();
				if(isMathMLTextIntegrationPoint(element)){
					string tokenName=tag.getName();
					if(!"mglyph".Equals(tokenName) &&
							!"malignmark".Equals(tokenName))
						return false;
				}
				if(MATHML_NAMESPACE.Equals(element.getNamespaceURI()) && (
						name.Equals("annotation-xml")) &&
						"svg".Equals(tag.getName()))
					return false;
				if(isHtmlIntegrationPoint(element))
					return false;
				return true;
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				if(isMathMLTextIntegrationPoint(element) ||
						isHtmlIntegrationPoint(element))
					return false;
				return true;
			} else
				return true;
		}
		return false;
	}


	private Text getFosterParentedTextNode() {
		if(openElements.Count==0)return null;
		Node fosterParent=openElements[0];
		IList<Node> childNodes;
		for(int i=openElements.Count-1;i>=0;i--){
			Element e=openElements[i];
			if(e.getLocalName().Equals("table")){
				Node parent=(Node) e.getParentNode();
				bool isElement=(parent!=null && parent.getNodeType()==NodeType.ELEMENT_NODE);
				if(!isElement){ // the parent is not an element
					if(i<=1)
						// This usually won't happen
						throw new InvalidOperationException();
					// append to the element before this table
					fosterParent=openElements[i-1];
					break;
				} else {
					// Parent of the table, insert before the table
					childNodes=parent.getChildNodesInternal();
					if(childNodes.Count==0)
						throw new InvalidOperationException();
					for(int j=0;j<childNodes.Count;j++){
						if(childNodes[j].Equals(e)){
							if(j>0 && childNodes[j-1].getNodeType()==NodeType.TEXT_NODE)
								return (Text)childNodes[j-1];
							else {
								Text textNode=new Text();
								parent.insertBefore(textNode, e);
								return textNode;
							}
						}
					}
					throw new InvalidOperationException();
				}
			}
		}
		childNodes=fosterParent.getChildNodesInternal();
		Node lastChild=(childNodes.Count==0) ? null : childNodes[childNodes.Count-1];
		if(lastChild==null || lastChild.getNodeType()!=NodeType.TEXT_NODE){
			Text textNode=new Text();
			fosterParent.appendChild(textNode);
			return textNode;
		} else
			return ((Text)lastChild);
	}

	private Text getTextNodeToInsert(Node node){
		if(doFosterParent && node.Equals(getCurrentNode())){
			string name=((Element)node).getLocalName();
			if("table".Equals(name) ||
					"tbody".Equals(name) ||
					"tfoot".Equals(name) ||
					"thead".Equals(name) ||
					"tr".Equals(name))
				return getFosterParentedTextNode();
		}
		IList<Node> childNodes=node.getChildNodesInternal();
		Node lastChild=(childNodes.Count==0) ? null : childNodes[childNodes.Count-1];
		if(lastChild==null || lastChild.getNodeType()!=NodeType.TEXT_NODE){
			Text textNode=new Text();
			node.appendChild(textNode);
			return textNode;
		} else
			return ((Text)lastChild);
	}

	private void insertInCurrentNode(Node element){
		Element node=getCurrentNode();
		if(doFosterParent){
			string name=node.getLocalName();
			if("table".Equals(name) ||
					"tbody".Equals(name) ||
					"tfoot".Equals(name) ||
					"thead".Equals(name) ||
					"tr".Equals(name)){
				fosterParent(element);
			} else {
				node.appendChild(element);
			}
		} else {
			node.appendChild(element);
		}
	}

	private Comment createCommentNode(int token){
		CommentToken comment=(CommentToken)getToken(token);
		Comment node=new Comment();
		node.setData(comment.getValue());
		return node;
	}

	private void addCommentNodeToCurrentNode(int token){
		insertInCurrentNode(createCommentNode(token));
	}

	private void addCommentNodeToDocument(int token){
		document.appendChild(createCommentNode(token));
	}


	private void addCommentNodeToFirst(int token){
		openElements[0].appendChild(createCommentNode(token));
	}


	private Element addHtmlElement(StartTagToken tag){
		Element element=Element.fromToken(tag);
		Element currentNode=getCurrentNode();
		if(currentNode!=null) {
			insertInCurrentNode(element);
		} else {
			document.appendChild(element);
		}
		openElements.Add(element);
		return element;
	}

	private Element insertForeignElement(StartTagToken tag, string _namespace) {
		Element element=Element.fromToken(tag,_namespace);
		string xmlns=element.getAttributeNS(XMLNS_NAMESPACE,"xmlns");
		string xlink=element.getAttributeNS(XMLNS_NAMESPACE,"xlink");
		if(xmlns!=null && !xmlns.Equals(_namespace)){
			error=true;
		}
		if(xlink!=null && !xlink.Equals(XLINK_NAMESPACE)){
			error=true;
		}
		Element currentNode=getCurrentNode();
		if(currentNode!=null) {
			insertInCurrentNode(element);
		} else {
			document.appendChild(element);
		}
		openElements.Add(element);
		return element;
	}

	private Element addHtmlElementNoPush(StartTagToken tag){
		Element element=Element.fromToken(tag);
		Element currentNode=getCurrentNode();
		if(currentNode!=null) {
			insertInCurrentNode(element);
		}
		return element;
	}

	private void insertCharacter(Node node, int ch){
		Text textNode=getTextNodeToInsert(node);
		if(textNode!=null) {
			textNode.text.appendInt(ch);
		}
	}

	private void insertString(Node node, string str){
		Text textNode=getTextNodeToInsert(node);
		if(textNode!=null) {
			textNode.text.appendString(str);
		}
	}

	private bool applyEndTag(string name, InsertionMode? insMode) {
		return applyInsertionMode(getArtificialToken(TOKEN_END_TAG,name),insMode);
	}

	private bool applyForeignContext(int token) {
		if(token==0){
			error=true;
			insertCharacter(getCurrentNode(),0xFFFD);
			return true;
		} else if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
			insertCharacter(getCurrentNode(),token);
			if(token!=0x09 && token!=0x0c && token!=0x0a &&
					token!=0x0d && token!=0x20){
				framesetOk=false;
			}
			return true;
		} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
			addCommentNodeToCurrentNode(token);
		} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
			error=true;
			return false;
		} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
			StartTagToken tag=(StartTagToken)getToken(token);
			string name=tag.getName();
			if(name.Equals("font")){
				if(tag.getAttribute("color")!=null ||
						tag.getAttribute("size")!=null ||
						tag.getAttribute("face")!=null){
					error=true;
					while(true){
						popCurrentNode();
						Element node=getCurrentNode();
						if(node.getNamespaceURI().Equals(HTML_NAMESPACE) ||
								isMathMLTextIntegrationPoint(node) ||
								isHtmlIntegrationPoint(node)){
							break;
						}
					}
					return applyInsertionMode(token,null);
				}
			} else if(name.Equals("b") ||
					name.Equals("big") || name.Equals("blockquote") || name.Equals("body") || name.Equals("br") ||
					name.Equals("center") || name.Equals("code") || name.Equals("dd") || name.Equals("div") ||
					name.Equals("dl") || name.Equals("dt") || name.Equals("em") || name.Equals("embed") ||
					name.Equals("h1") || name.Equals("h2") || name.Equals("h3") || name.Equals("h4") ||
					name.Equals("h5") || name.Equals("h6") || name.Equals("head") || name.Equals("hr") ||
					name.Equals("i") || name.Equals("img") || name.Equals("li") || name.Equals("listing") ||
					name.Equals("menu") || name.Equals("meta") || name.Equals("nobr") || name.Equals("ol") ||
					name.Equals("p") || name.Equals("pre") || name.Equals("ruby") || name.Equals("s") ||
					name.Equals("small") || name.Equals("span") || name.Equals("strong") || name.Equals("strike") ||
					name.Equals("sub") || name.Equals("sup") || name.Equals("table") || name.Equals("tt") ||
					name.Equals("u") || name.Equals("ul") || name.Equals("var")){
				error=true;
				if(context!=null && !hasNativeElementInScope()){
					noforeign=true;
					bool ret=applyInsertionMode(token,InsertionMode.InBody);
					noforeign=false;
					return ret;
				}
				while(true){
					popCurrentNode();
					Element node=getCurrentNode();
					if(node.getNamespaceURI().Equals(HTML_NAMESPACE) ||
							isMathMLTextIntegrationPoint(node) ||
							isHtmlIntegrationPoint(node)){
						break;
					}
				}
				return applyInsertionMode(token,null);
			} else {
				string _namespace=getCurrentNode().getNamespaceURI();
				bool mathml=false;
				if(SVG_NAMESPACE.Equals(_namespace)){
					if(name.Equals("altglyph")) {
						tag.setName("altGlyph");
					} else if(name.Equals("altglyphdef")) {
						tag.setName("altGlyphDef");
					} else if(name.Equals("altglyphitem")) {
						tag.setName("altGlyphItem");
					} else if(name.Equals("animatecolor")) {
						tag.setName("animateColor");
					} else if(name.Equals("animatemotion")) {
						tag.setName("animateMotion");
					} else if(name.Equals("animatetransform")) {
						tag.setName("animateTransform");
					} else if(name.Equals("clippath")) {
						tag.setName("clipPath");
					} else if(name.Equals("feblend")) {
						tag.setName("feBlend");
					} else if(name.Equals("fecolormatrix")) {
						tag.setName("feColorMatrix");
					} else if(name.Equals("fecomponenttransfer")) {
						tag.setName("feComponentTransfer");
					} else if(name.Equals("fecomposite")) {
						tag.setName("feComposite");
					} else if(name.Equals("feconvolvematrix")) {
						tag.setName("feConvolveMatrix");
					} else if(name.Equals("fediffuselighting")) {
						tag.setName("feDiffuseLighting");
					} else if(name.Equals("fedisplacementmap")) {
						tag.setName("feDisplacementMap");
					} else if(name.Equals("fedistantlight")) {
						tag.setName("feDistantLight");
					} else if(name.Equals("feflood")) {
						tag.setName("feFlood");
					} else if(name.Equals("fefunca")) {
						tag.setName("feFuncA");
					} else if(name.Equals("fefuncb")) {
						tag.setName("feFuncB");
					} else if(name.Equals("fefuncg")) {
						tag.setName("feFuncG");
					} else if(name.Equals("fefuncr")) {
						tag.setName("feFuncR");
					} else if(name.Equals("fegaussianblur")) {
						tag.setName("feGaussianBlur");
					} else if(name.Equals("feimage")) {
						tag.setName("feImage");
					} else if(name.Equals("femerge")) {
						tag.setName("feMerge");
					} else if(name.Equals("femergenode")) {
						tag.setName("feMergeNode");
					} else if(name.Equals("femorphology")) {
						tag.setName("feMorphology");
					} else if(name.Equals("feoffset")) {
						tag.setName("feOffset");
					} else if(name.Equals("fepointlight")) {
						tag.setName("fePointLight");
					} else if(name.Equals("fespecularlighting")) {
						tag.setName("feSpecularLighting");
					} else if(name.Equals("fespotlight")) {
						tag.setName("feSpotLight");
					} else if(name.Equals("fetile")) {
						tag.setName("feTile");
					} else if(name.Equals("feturbulence")) {
						tag.setName("feTurbulence");
					} else if(name.Equals("foreignobject")) {
						tag.setName("foreignObject");
					} else if(name.Equals("glyphref")) {
						tag.setName("glyphRef");
					} else if(name.Equals("lineargradient")) {
						tag.setName("linearGradient");
					} else if(name.Equals("radialgradient")) {
						tag.setName("radialGradient");
					} else if(name.Equals("textpath")) {
						tag.setName("textPath");
					}
					adjustSvgAttributes(tag);
				} else if(MATHML_NAMESPACE.Equals(_namespace)){
					adjustMathMLAttributes(tag);
					mathml=true;
				}
				adjustForeignAttributes(tag);
				Element e=insertForeignElement(tag,_namespace);
				if(mathml && tag.getName().Equals("annotation-xml")){
					string encoding=tag.getAttribute("encoding");
					if(encoding!=null){
						encoding=StringUtility.toLowerCaseAscii(encoding);
						if(encoding.Equals("text/html") ||
								encoding.Equals("application/xhtml+xml")){
							integrationElements.Add(e);
						}
					}
				}
				if(tag.isSelfClosing()){
					if(name.Equals("script")){
						tag.ackSelfClosing();
						applyEndTag("script",null);
					} else {
						popCurrentNode();
						tag.ackSelfClosing();
					}
				}
				return true;
			}
			return false;
		} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
			EndTagToken tag=(EndTagToken)getToken(token);
			string name=tag.getName();
			if(name.Equals("script") &&
					getCurrentNode().getLocalName().Equals("script") &&
					SVG_NAMESPACE.Equals(getCurrentNode().getNamespaceURI())){
				popCurrentNode();
			} else {
				// NOTE: The HTML spec here is unfortunately too strict
				// in that it doesn't allow an ASCII case-insensitive
				// comparison (for example, with SVG foreignObject)
				if(!getCurrentNode().getLocalName().Equals(name)) {
					error=true;
				}
				int originalSize=openElements.Count;
				for(int i1=originalSize-1;i1>=0;i1--){
					if(i1==0)
						return true;
					Element node=openElements[i1];
					if(i1<originalSize-1 &&
							HTML_NAMESPACE.Equals(node.getNamespaceURI())){
						noforeign=true;
						return applyInsertionMode(token,null);
					}
					string nodeName=StringUtility.toLowerCaseAscii(node.getLocalName());
					if(name.Equals(nodeName)){
						while(true){
							Element node2=popCurrentNode();
							if(node2.Equals(node)) {
								break;
							}
						}
						break;
					}
				}
			}
			return false;
		} else if(token==TOKEN_EOF)
			return applyInsertionMode(token,null);
		return true;
	}

	private bool hasNativeElementInScope() {
		for(int i=openElements.Count-1;i>=0;i--){
			Element e=openElements[i];
			//Console.WriteLine("%s %s",e.getLocalName(),e.getNamespaceURI());
			if(e.getNamespaceURI().Equals(HTML_NAMESPACE) ||
					isMathMLTextIntegrationPoint(e) ||
					isHtmlIntegrationPoint(e))
				return true;
			if(e.isHtmlElement("applet")||
					e.isHtmlElement("caption")||
					e.isHtmlElement("html")||
					e.isHtmlElement("table")||
					e.isHtmlElement("td")||
					e.isHtmlElement("th")||
					e.isHtmlElement("marquee")||
					e.isHtmlElement("object")||
					e.isMathMLElement("mi")||
					e.isMathMLElement("mo")||
					e.isMathMLElement("mn")||
					e.isMathMLElement("ms")||
					e.isMathMLElement("mtext")||
					e.isMathMLElement("annotation-xml")||
					e.isSvgElement("foreignObject")||
					e.isSvgElement("desc")||
					e.isSvgElement("title")
					)
				return false;
		}
		return false;
	}

	private void skipLineFeed()  {
		int mark=charInput.setSoftMark();
		int nextToken=charInput.read();
		if(nextToken==0x0a)
			return; // ignore the token if it's 0x0A
		else if(nextToken==0x26){ // start of character reference
			int charref=parseCharacterReference(-1);
			if(charref<0){
				// more than one character in this reference
				int index=Math.Abs(charref+1);
				tokenQueue.Add(entityDoubles[index*2]);
				tokenQueue.Add(entityDoubles[index*2+1]);
			} else if(charref==0x0a)
				return; // ignore the token
			else {
				tokenQueue.Add(charref);
			}
		} else {
			// anything else; reset the input stream
			charInput.setMarkPosition(mark);
		}
	}

	private bool applyInsertionMode(int token, InsertionMode? insMode) {
		//Console.WriteLine("[[%08X %s %s %s(%s)",token,getToken(token),insMode==null ? insertionMode :
		//insMode,isForeignContext(token),noforeign);
		if(!noforeign && isForeignContext(token))
			return applyForeignContext(token);
		noforeign=false;
		if(insMode==null) {
			insMode=insertionMode;
		}
		switch(insMode){
		case InsertionMode.Initial:{
			if(token==0x09 || token==0x0a ||
					token==0x0c || token==0x0d ||
					token==0x20)
				return false;
			if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				DocTypeToken doctype=(DocTypeToken)getToken(token);
				string doctypeName=(doctype.name==null) ? "" : doctype.name.ToString();
				string doctypePublic=(doctype.publicID==null) ? null : doctype.publicID.ToString();
				string doctypeSystem=(doctype.systemID==null) ? null : doctype.systemID.ToString();
				bool matchesHtml="html".Equals(doctypeName);
				bool hasSystemId=(doctype.systemID!=null);
				if(!matchesHtml || doctypePublic!=null ||
						(doctypeSystem!=null && !"about:legacy-compat".Equals(doctypeSystem))){
					bool html4=(matchesHtml && "-//W3C//DTD HTML 4.0//EN".Equals(doctypePublic) &&
							(doctypeSystem==null || "http://www.w3.org/TR/REC-html40/strict.dtd".Equals(doctypeSystem)));
					bool html401=(matchesHtml && "-//W3C//DTD HTML 4.01//EN".Equals(doctypePublic) &&
							(doctypeSystem==null || "http://www.w3.org/TR/html4/strict.dtd".Equals(doctypeSystem)));
					bool xhtml=(matchesHtml && "-//W3C//DTD XHTML 1.0 Strict//EN".Equals(doctypePublic) &&
							("http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd".Equals(doctypeSystem)));
					bool xhtml11=(matchesHtml && "-//W3C//DTD XHTML 1.1//EN".Equals(doctypePublic) &&
							("http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd".Equals(doctypeSystem)));
					if(!html4 && !html401 && !xhtml && !xhtml11){
						error=true;
					}
				}
				if(doctypePublic==null) {
					doctypePublic="";
				}
				if(doctypeSystem==null) {
					doctypeSystem="";
				}
				DocumentType doctypeNode=new DocumentType();
				doctypeNode.name=doctypeName;
				doctypeNode.publicId=doctypePublic;
				doctypeNode.systemId=doctypeSystem;
				document.doctype=doctypeNode;
				document.appendChild(doctypeNode);
				string doctypePublicLC=null;
				if(!matchesHtml||doctype.forceQuirks){
					document.setMode(DocumentMode.QuirksMode);
				}
				else {
					doctypePublicLC=StringUtility.toLowerCaseAscii(doctypePublic);
					if("html".Equals(doctypePublicLC) ||
							"-//w3o//dtd w3 html strict 3.0//en//".Equals(doctypePublicLC) ||
							"-/w3c/dtd html 4.0 transitional/en".Equals(doctypePublicLC)
							){
						document.setMode(DocumentMode.QuirksMode);
					}
					else if(doctypePublic.Length>0){
						foreach(string id in quirksModePublicIdPrefixes){
							if(doctypePublicLC.StartsWith(id,StringComparison.Ordinal)){
								document.setMode(DocumentMode.QuirksMode);
								break;
							}
						}
					}
				}
				if(document.getMode()!=DocumentMode.QuirksMode){
					if(doctypePublicLC==null) {
						doctypePublicLC=StringUtility.toLowerCaseAscii(doctypePublic);
					}
					if("http://www.ibm.com/data/dtd/v11/ibmxhtml1-transitional.dtd".Equals(
							StringUtility.toLowerCaseAscii(doctypeSystem)) ||
							(!hasSystemId && doctypePublicLC.StartsWith("-//w3c//dtd html 4.01 frameset//",StringComparison.Ordinal)) ||
							(!hasSystemId && doctypePublicLC.StartsWith("-//w3c//dtd html 4.01 transitional//",StringComparison.Ordinal))){
						document.setMode(DocumentMode.QuirksMode);
					}
				}
				if(document.getMode()!=DocumentMode.QuirksMode){
					if(doctypePublicLC==null) {
						doctypePublicLC=StringUtility.toLowerCaseAscii(doctypePublic);
					}
					if(doctypePublicLC.StartsWith("-//w3c//dtd xhtml 1.0 frameset//",StringComparison.Ordinal) ||
							doctypePublicLC.StartsWith("-//w3c//dtd xhtml 1.0 transitional//",StringComparison.Ordinal) ||
							(hasSystemId && doctypePublicLC.StartsWith("-//w3c//dtd html 4.01 frameset//",StringComparison.Ordinal)) ||
							(hasSystemId && doctypePublicLC.StartsWith("-//w3c//dtd html 4.01 transitional//",StringComparison.Ordinal))){
						document.setMode(DocumentMode.LimitedQuirksMode);
					}
				}
				insertionMode=InsertionMode.BeforeHtml;
				return true;
			}
			if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToDocument(token);

				return true;
			}
			if(!"about:srcdoc".Equals(address)){
				error=true;
				document.setMode(DocumentMode.QuirksMode);
			}
			insertionMode=InsertionMode.BeforeHtml;
			return applyInsertionMode(token,null);
		}
		case InsertionMode.BeforeHtml:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				error=true;
				return false;
			}
			if(token==0x09 || token==0x0a ||
					token==0x0c || token==0x0d ||
					token==0x20)
				return false;
			if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToDocument(token);

				return true;
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if("html".Equals(name)){
					addHtmlElement(tag);
					insertionMode=InsertionMode.BeforeHead;
					return true;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				TagToken tag=(TagToken)getToken(token);
				string name=tag.getName();
				if(!"html".Equals(name) && !"br".Equals(name) &&
						!"head".Equals(name) && !"body".Equals(name)){
					error=true;
					return false;
				}
			}
			Element element=new Element();
			element.setName("html");
			element.setNamespace(HTML_NAMESPACE);
			document.appendChild(element);
			openElements.Add(element);
			insertionMode=InsertionMode.BeforeHead;
			return applyInsertionMode(token,null);
		}
		case InsertionMode.BeforeHead:{
			if(token==0x09 || token==0x0a ||
					token==0x0c || token==0x0d ||
					token==0x20)
				return false;
			if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToCurrentNode(token);
				return true;
			}
			if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				error=true;
				return false;
			}
			if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if("html".Equals(name)){
					applyInsertionMode(token,InsertionMode.InBody);
					return true;
				} else if("head".Equals(name)){
					Element element=addHtmlElement(tag);
					headElement=element;
					insertionMode=InsertionMode.InHead;
					return true;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				TagToken tag=(TagToken)getToken(token);
				string name=tag.getName();
				if("head".Equals(name) ||
						"br".Equals(name) ||
						"body".Equals(name) ||
						"html".Equals(name)){
					applyStartTag("head",insMode);
					return applyInsertionMode(token,null);
				} else {
					error=true;
					return false;
				}
			}
			applyStartTag("head",insMode);
			return applyInsertionMode(token,null);
		}
		case InsertionMode.InHead:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToCurrentNode(token);
				return true;
			}
			if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				error=true;
				return false;
			}
			if(token==0x09 || token==0x0a ||
					token==0x0c || token==0x0d ||
					token==0x20){
				insertCharacter(getCurrentNode(),token);
				return true;
			}
			if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if("html".Equals(name)){
					applyInsertionMode(token,InsertionMode.InBody);
					return true;
				} else if("base".Equals(name)||
						"bgsound".Equals(name)||
						"basefont".Equals(name)||
						"link".Equals(name)){
					Element e=addHtmlElementNoPush(tag);
					if(baseurl==null && "base".Equals(name)){
						// Get the document _base URL
						baseurl=e.getAttribute("href");
					}
					tag.ackSelfClosing();
					return true;
				} else if("meta".Equals(name)){
					Element element=addHtmlElementNoPush(tag);
					tag.ackSelfClosing();
					if(encoding.getConfidence()==EncodingConfidence.Tentative){
						string charset=element.getAttribute("charset");
						if(charset!=null){
							charset=TextEncoding.resolveEncoding(charset);
							if(TextEncoding.isAsciiCompatible(charset) ||
									"utf-16be".Equals(charset) ||
									"utf-16le".Equals(charset)){
								changeEncoding(charset);
								if(encoding.getConfidence()==EncodingConfidence.Certain){
									inputStream.disableBuffer();
								}
								return true;
							}
						}
						string value=element.getAttribute("http-equiv");
						if(value!=null && StringUtility.toLowerCaseAscii(value).Equals("content-type")){
							value=element.getAttribute("content");
							if(value!=null){
								value=StringUtility.toLowerCaseAscii(value);
								charset=CharsetSniffer.extractCharsetFromMeta(value);
								if(TextEncoding.isAsciiCompatible(charset) ||
										"utf-16be".Equals(charset) ||
										"utf-16le".Equals(charset)){
									changeEncoding(charset);
									if(encoding.getConfidence()==EncodingConfidence.Certain){
										inputStream.disableBuffer();
									}
									return true;
								}
							}
						}
					}
					if(encoding.getConfidence()==EncodingConfidence.Certain){
						inputStream.disableBuffer();
					}
					return true;
				} else if("title".Equals(name)){
					addHtmlElement(tag);
					state=TokenizerState.RcData;
					originalInsertionMode=insertionMode;
					insertionMode=InsertionMode.Text;
					return true;
				} else if("noframes".Equals(name) ||
						"style".Equals(name)){
					addHtmlElement(tag);
					state=TokenizerState.RawText;
					originalInsertionMode=insertionMode;
					insertionMode=InsertionMode.Text;
					return true;
				} else if("noscript".Equals(name)){
					addHtmlElement(tag);
					insertionMode=InsertionMode.InHeadNoscript;
					return true;
				} else if("script".Equals(name)){
					addHtmlElement(tag);
					state=TokenizerState.ScriptData;
					originalInsertionMode=insertionMode;
					insertionMode=InsertionMode.Text;
					return true;
				} else if("head".Equals(name)){
					error=true;
					return false;
				} else {
					applyEndTag("head",insMode);
					return applyInsertionMode(token,null);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				TagToken tag=(TagToken)getToken(token);
				string name=tag.getName();
				if("head".Equals(name)){
					openElements.RemoveAt(openElements.Count-1);
					insertionMode=InsertionMode.AfterHead;
					return true;
				} else if(!(
						"br".Equals(name) ||
						"body".Equals(name) ||
						"html".Equals(name))){
					error=true;
					return false;
				}
				applyEndTag("head",insMode);
				return applyInsertionMode(token,null);
			} else {
				applyEndTag("head",insMode);
				return applyInsertionMode(token,null);
			}
		}
		case InsertionMode.AfterHead:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				if(token==0x20 || token==0x09 || token==0x0a ||
						token==0x0c || token==0x0d){
					insertCharacter(getCurrentNode(),token);
				} else {
					applyStartTag("body",insMode);
					framesetOk=true;
					return applyInsertionMode(token,null);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				error=true;
				return false;
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("html")){
					applyInsertionMode(token,InsertionMode.InBody);
					return true;
				} else if(name.Equals("body")){
					addHtmlElement(tag);
					framesetOk=false;
					insertionMode=InsertionMode.InBody;
					return true;
				} else if(name.Equals("frameset")){
					addHtmlElement(tag);
					insertionMode=InsertionMode.InFrameset;
					return true;
				} else if("base".Equals(name)||
						"bgsound".Equals(name)||
						"basefont".Equals(name)||
						"link".Equals(name)||
						"noframes".Equals(name)||
						"script".Equals(name)||
						"style".Equals(name)||
						"title".Equals(name)||
						"meta".Equals(name)){
					error=true;
					openElements.Add(headElement);
					applyInsertionMode(token,InsertionMode.InHead);
					openElements.Remove(headElement);
					return true;
				} else if("head".Equals(name)){
					error=true;
					return false;
				} else {
					applyStartTag("body",insMode);
					framesetOk=true;
					return applyInsertionMode(token,null);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("body") || name.Equals("html")||
						name.Equals("br")){
					applyStartTag("body",insMode);
					framesetOk=true;
					return applyInsertionMode(token,null);
				} else {
					error=true;
					return false;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToCurrentNode(token);

				return true;
			} else if(token==TOKEN_EOF){
				applyStartTag("body",insMode);
				framesetOk=true;
				return applyInsertionMode(token,null);
			}
			return true;
		}
		case InsertionMode.Text:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				if(insMode!=insertionMode){
					insertCharacter(getCurrentNode(),token);
				} else {
					Text textNode=getTextNodeToInsert(getCurrentNode());
					int ch=token;
					if(textNode==null)
						throw new InvalidOperationException();
					while(true){
						textNode.text.appendInt(ch);
						token=parserRead();
						if((token&TOKEN_TYPE_MASK)!=TOKEN_CHARACTER){
							tokenQueue.Insert(0,token);
							break;
						}
						ch=token;
					}
				}
				return true;
			} else if(token==TOKEN_EOF){
				error=true;
				openElements.RemoveAt(openElements.Count-1);
				insertionMode=originalInsertionMode;
				return applyInsertionMode(token,null);
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				openElements.RemoveAt(openElements.Count-1);
				insertionMode=originalInsertionMode;
			}
			return true;
		}
		case InsertionMode.InBody:{
			if(token==0){
				error=true;
				return true;
			}
			if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToCurrentNode(token);

				return true;
			}
			if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				error=true;
				return true;
			}
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				//Console.WriteLine("%c %s",token,getCurrentNode().getTagName());
				reconstructFormatting();
				Text textNode=getTextNodeToInsert(getCurrentNode());
				int ch=token;
				if(textNode==null)
					throw new InvalidOperationException();
				while(true){
					// Read multiple characters at once
					if(ch==0){
						error=true;
					} else {
						textNode.text.appendInt(ch);
					}
					if(framesetOk && token!=0x20 && token!=0x09 &&
							token!=0x0a && token!=0x0c && token!=0x0d){
						framesetOk=false;
					}
					// If we're only processing under a different
					// insertion mode then break
					if(insMode!=insertionMode) {
						break;
					}
					token=parserRead();
					if((token&TOKEN_TYPE_MASK)!=TOKEN_CHARACTER){
						tokenQueue.Insert(0,token);
						break;
					}
					ch=token;
				}
				return true;
			} else if(token==TOKEN_EOF){
				foreach(Element e in openElements){
					string name=e.getLocalName();
					if(!"dd".Equals(name) &&
							!"dt".Equals(name) &&
							!"li".Equals(name) &&
							!"p".Equals(name) &&
							!"tbody".Equals(name) &&
							!"td".Equals(name) &&
							!"tfoot".Equals(name) &&
							!"th".Equals(name) &&
							!"tr".Equals(name) &&
							!"thead".Equals(name) &&
							!"body".Equals(name) &&
							!"html".Equals(name)){
						error=true;
					}
				}
				stopParsing();
			}
			if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				//
				//  START TAGS
				//
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if("html".Equals(name)){
					error=true;
					openElements[0].mergeAttributes(tag);
					return true;
				} else if("base".Equals(name)||
						"bgsound".Equals(name)||
						"basefont".Equals(name)||
						"link".Equals(name)||
						"menuitem".Equals(name)||
						"noframes".Equals(name)||
						"script".Equals(name)||
						"style".Equals(name)||
						"title".Equals(name)||
						"meta".Equals(name)){
					applyInsertionMode(token,InsertionMode.InHead);
					return true;
				} else if("body".Equals(name)){
					error=true;
					if(openElements.Count<=1 ||
							!openElements[1].isHtmlElement("body"))
						return false;
					framesetOk=false;
					openElements[1].mergeAttributes(tag);
					return true;
				} else if("frameset".Equals(name)){
					error=true;
					if(!framesetOk ||
							openElements.Count<=1 ||
							!openElements[1].isHtmlElement("body"))
						return false;
					Node parent=(Node) openElements[1].getParentNode();
					if(parent!=null){
						parent.removeChild(openElements[1]);
					}
					while(openElements.Count>1){
						popCurrentNode();
					}
					addHtmlElement(tag);
					insertionMode=InsertionMode.InFrameset;
					return true;
				} else if("address".Equals(name) ||
						"article".Equals(name) ||
						"aside".Equals(name) ||
						"blockquote".Equals(name) ||
						"center".Equals(name) ||
						"details".Equals(name) ||
						"dialog".Equals(name) ||
						"dir".Equals(name) ||
						"div".Equals(name) ||
						"dl".Equals(name) ||
						"fieldset".Equals(name) ||
						"figcaption".Equals(name) ||
						"figure".Equals(name) ||
						"footer".Equals(name) ||
						"header".Equals(name) ||
						"hgroup".Equals(name) ||
						"menu".Equals(name) ||
						"nav".Equals(name) ||
						"ol".Equals(name) ||
						"p".Equals(name) ||
						"section".Equals(name) ||
						"summary".Equals(name) ||
						"ul".Equals(name)
						){
					closeParagraph(insMode);
					addHtmlElement(tag);
					return true;
				} else if("h1".Equals(name) ||
						"h2".Equals(name) ||
						"h3".Equals(name) ||
						"h4".Equals(name) ||
						"h5".Equals(name) ||
						"h6".Equals(name)
						){
					closeParagraph(insMode);
					Element node=getCurrentNode();
					string name1=node.getLocalName();
					if("h1".Equals(name1) ||
							"h2".Equals(name1) ||
							"h3".Equals(name1) ||
							"h4".Equals(name1) ||
							"h5".Equals(name1) ||
							"h6".Equals(name1)
							){
						error=true;
						openElements.RemoveAt(openElements.Count-1);
					}
					addHtmlElement(tag);
					return true;
				} else if("pre".Equals(name)||
						"listing".Equals(name)){
					closeParagraph(insMode);
					addHtmlElement(tag);
					skipLineFeed();
					framesetOk=false;
					return true;
				} else if("form".Equals(name)){
					if(formElement!=null){
						error=true;
						return true;
					}
					closeParagraph(insMode);
					formElement=addHtmlElement(tag);
					return true;
				} else if("li".Equals(name)){
					framesetOk=false;
					for(int i=openElements.Count-1;i>=0;i--){
						Element node=openElements[i];
						string nodeName=node.getLocalName();
						if(nodeName.Equals("li")){
							applyInsertionMode(
									getArtificialToken(TOKEN_END_TAG,"li"),
									insMode);
							break;
						}
						if(isSpecialElement(node) &&
								!"address".Equals(nodeName) &&
								!"div".Equals(nodeName) &&
								!"p".Equals(nodeName)){
							break;
						}
					}
					closeParagraph(insMode);
					addHtmlElement(tag);
					return true;
				} else if("dd".Equals(name) || "dt".Equals(name)){
					framesetOk=false;
					for(int i=openElements.Count-1;i>=0;i--){
						Element node=openElements[i];
						string nodeName=node.getLocalName();
						//Console.WriteLine("looping through %s",nodeName);
						if(nodeName.Equals("dd") || nodeName.Equals("dt")){
							applyEndTag(nodeName,insMode);
							break;
						}
						if(isSpecialElement(node) &&
								!"address".Equals(nodeName) &&
								!"div".Equals(nodeName) &&
								!"p".Equals(nodeName)){
							break;
						}
					}
					closeParagraph(insMode);
					addHtmlElement(tag);
					return true;
				} else if("plaintext".Equals(name)){
					closeParagraph(insMode);
					addHtmlElement(tag);
					state=TokenizerState.PlainText;
					return true;
				} else if("button".Equals(name)){
					if(hasHtmlElementInScope("button")){
						error=true;
						applyEndTag("button",insMode);
						return applyInsertionMode(token,null);
					}
					reconstructFormatting();
					addHtmlElement(tag);
					framesetOk=false;
					return true;
				} else if("a".Equals(name)){
					while(true){
						Element node=null;
						for(int i=formattingElements.Count-1; i>=0; i--){
							FormattingElement fe=formattingElements[i];
							if(fe.isMarker()) {
								break;
							}
							if(fe.element.getLocalName().Equals("a")){
								node=fe.element;
								break;
							}
						}
						if(node!=null){
							error=true;
							applyEndTag("a",insMode);
							removeFormattingElement(node);
							openElements.Remove(node);
						} else {
							break;
						}
					}
					reconstructFormatting();
					pushFormattingElement(tag);
				} else if("b".Equals(name) ||
						"big".Equals(name)||
						"code".Equals(name)||
						"em".Equals(name)||
						"font".Equals(name)||
						"i".Equals(name)||
						"s".Equals(name)||
						"small".Equals(name)||
						"strike".Equals(name)||
						"strong".Equals(name)||
						"tt".Equals(name)||
						"u".Equals(name)){
					reconstructFormatting();
					pushFormattingElement(tag);
				} else if("nobr".Equals(name)){
					reconstructFormatting();
					if(hasHtmlElementInScope("nobr")){
						error=true;
						applyEndTag("nobr",insMode);
						reconstructFormatting();
					}
					pushFormattingElement(tag);
				} else if("table".Equals(name)){
					if(document.getMode()!=DocumentMode.QuirksMode) {
						closeParagraph(insMode);
					}
					addHtmlElement(tag);
					framesetOk=false;
					insertionMode=InsertionMode.InTable;
					return true;
				} else if("area".Equals(name)||
						"br".Equals(name)||
						"embed".Equals(name)||
						"img".Equals(name)||
						"keygen".Equals(name)||
						"wbr".Equals(name)
						){
					reconstructFormatting();
					addHtmlElementNoPush(tag);
					tag.ackSelfClosing();
					framesetOk=false;
				} else if("input".Equals(name)){
					reconstructFormatting();
					inputElement=addHtmlElementNoPush(tag);
					tag.ackSelfClosing();
					string attr=inputElement.getAttribute("type");
					if(attr==null || !"hidden".Equals(StringUtility.toLowerCaseAscii(attr))){
						framesetOk=false;
					}
				} else if("param".Equals(name)||
						"source".Equals(name)||
						"track".Equals(name)
						){
					addHtmlElementNoPush(tag);
					tag.ackSelfClosing();
				} else if("hr".Equals(name)){
					closeParagraph(insMode);
					addHtmlElementNoPush(tag);
					tag.ackSelfClosing();
					framesetOk=false;
				} else if("image".Equals(name)){
					error=true;
					tag.setName("img");
					return applyInsertionMode(token,null);
				} else if("isindex".Equals(name)){
					error=true;
					if(formElement!=null)return false;
					tag.ackSelfClosing();
					applyStartTag("form",insMode);
					string action=tag.getAttribute("action");
					if(action!=null) {
						formElement.setAttribute("action",action);
					}
					applyStartTag("hr",insMode);
					applyStartTag("label",insMode);
					StartTagToken isindex=new StartTagToken("input");
					foreach(Attrib attr in tag.getAttributes()){
						string attrname=attr.getName();
						if(!"name".Equals(attrname) &&
								!"action".Equals(attrname) &&
								!"prompt".Equals(attrname)){
							isindex.setAttribute(attrname,attr.getValue());
						}
					}
					string prompt=tag.getAttribute("prompt");
					// NOTE: Because of the inserted hr elements,
					// the frameset-ok flag should have been set
					// to not-ok already, so we don't need to check
					// for whitespace here
					if(prompt!=null){
						reconstructFormatting();
						insertString(getCurrentNode(),prompt);
					} else {
						reconstructFormatting();
						insertString(getCurrentNode(),"Enter search keywords:");
					}
					int isindexToken=tokens.Count|isindex.getType();
					tokens.Add(isindex);
					applyInsertionMode(isindexToken,insMode);
					inputElement.setAttribute("name","isindex");
					applyEndTag("label",insMode);
					applyStartTag("hr",insMode);
					applyEndTag("form",insMode);
				} else if("textarea".Equals(name)){
					addHtmlElement(tag);
					skipLineFeed();
					state=TokenizerState.RcData;
					originalInsertionMode=insertionMode;
					framesetOk=false;
					insertionMode=InsertionMode.Text;
				} else if("xmp".Equals(name)){
					closeParagraph(insMode);
					reconstructFormatting();
					framesetOk=false;
					addHtmlElement(tag);
					state=TokenizerState.RawText;
					originalInsertionMode=insertionMode;
					insertionMode=InsertionMode.Text;
				} else if("iframe".Equals(name)){
					framesetOk=false;
					addHtmlElement(tag);
					state=TokenizerState.RawText;
					originalInsertionMode=insertionMode;
					insertionMode=InsertionMode.Text;
				} else if("noembed".Equals(name)){
					addHtmlElement(tag);
					state=TokenizerState.RawText;
					originalInsertionMode=insertionMode;
					insertionMode=InsertionMode.Text;
				} else if("select".Equals(name)){
					reconstructFormatting();
					addHtmlElement(tag);
					framesetOk=false;
					if(insertionMode==InsertionMode.InTable ||
							insertionMode==InsertionMode.InCaption ||
							insertionMode==InsertionMode.InTableBody ||
							insertionMode==InsertionMode.InRow ||
							insertionMode==InsertionMode.InCell ) {
						insertionMode=InsertionMode.InSelectInTable;
					} else {
						insertionMode=InsertionMode.InSelect;
					}
				} else if("option".Equals(name) || "optgroup".Equals(name)){
					if(getCurrentNode().getLocalName().Equals("option")){
						applyEndTag("option",insMode);
					}
					reconstructFormatting();
					addHtmlElement(tag);
				} else if("rp".Equals(name) || "rt".Equals(name)){
					if(hasHtmlElementInScope("ruby")){
						generateImpliedEndTags();
						if(!getCurrentNode().getLocalName().Equals("ruby")){
							error=true;
						}
					}
					addHtmlElement(tag);
				} else if("applet".Equals(name) ||
						"marquee".Equals(name) ||
						"object".Equals(name)){
					reconstructFormatting();
					Element e=addHtmlElement(tag);
					insertFormattingMarker(tag,e);
					framesetOk=false;
				} else if("math".Equals(name)){
					reconstructFormatting();
					adjustMathMLAttributes(tag);
					adjustForeignAttributes(tag);
					insertForeignElement(tag,MATHML_NAMESPACE);
					if(tag.isSelfClosing()){
						tag.ackSelfClosing();
						popCurrentNode();
					} else {
						hasForeignContent=true;
					}
				} else if("svg".Equals(name)){
					reconstructFormatting();
					adjustSvgAttributes(tag);
					adjustForeignAttributes(tag);
					insertForeignElement(tag,SVG_NAMESPACE);
					if(tag.isSelfClosing()){
						tag.ackSelfClosing();
						popCurrentNode();
					} else {
						hasForeignContent=true;
					}
				} else if("caption".Equals(name) ||
						"col".Equals(name) ||
						"colgroup".Equals(name) ||
						"frame".Equals(name) ||
						"head".Equals(name) ||
						"tbody".Equals(name) ||
						"td".Equals(name) ||
						"tfoot".Equals(name) ||
						"th".Equals(name) ||
						"thead".Equals(name) ||
						"tr".Equals(name)
						){
					error=true;
					return false;
				} else {
					//Console.WriteLine("ordinary: %s",tag);
					reconstructFormatting();
					addHtmlElement(tag);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				//
				//  END TAGS
				// NOTE: Have all cases
				//
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("body")){
					if(!hasHtmlElementInScope("body")){
						error=true;
						return false;
					}
					foreach(Element e in openElements){
						string name2=e.getLocalName();
						if(!"dd".Equals(name2) &&
								!"dt".Equals(name2) &&
								!"li".Equals(name2) &&
								!"option".Equals(name2) &&
								!"optgroup".Equals(name2) &&
								!"p".Equals(name2) &&
								!"rb".Equals(name2) &&
								!"tbody".Equals(name2) &&
								!"td".Equals(name2) &&
								!"tfoot".Equals(name2) &&
								!"th".Equals(name2) &&
								!"tr".Equals(name2) &&
								!"thead".Equals(name2) &&
								!"body".Equals(name2) &&
								!"html".Equals(name2)){
							error=true;
							// token not ignored here
						}
					}
					insertionMode=InsertionMode.AfterBody;
				} else if(name.Equals("a") ||
						name.Equals("b") ||
						name.Equals("big") ||
						name.Equals("code") ||
						name.Equals("em") ||
						name.Equals("b") ||
						name.Equals("font") ||
						name.Equals("i") ||
						name.Equals("nobr") ||
						name.Equals("s") ||
						name.Equals("small") ||
						name.Equals("strike") ||
						name.Equals("strong") ||
						name.Equals("tt") ||
						name.Equals("u")
						){
					for(int i=0;i<8;i++){
						FormattingElement formatting=null;
						for(int j=formattingElements.Count-1; j>=0; j--){
							FormattingElement fe=formattingElements[j];
							if(fe.isMarker()) {
								break;
							}
							if(fe.element.getLocalName().Equals(name)){
								formatting=fe;
								break;
							}
						}
						if(formatting==null){
							// NOTE: Steps for "any other end tag"
							//	Console.WriteLine("no such formatting element");
							for(int i1=openElements.Count-1;i1>=0;i1--){
								Element node=openElements[i1];
								if(name.Equals(node.getLocalName())){
									generateImpliedEndTagsExcept(name);
									if(!name.Equals(getCurrentNode().getLocalName())){
										error=true;
									}
									while(true){
										Element node2=popCurrentNode();
										if(node2.Equals(node)) {
											break;
										}
									}
									break;
								} else if(isSpecialElement(node)){
									error=true;
									return false;
								}
							}
							break;
						}
						int formattingElementPos=openElements.IndexOf(formatting.element);
						if(formattingElementPos<0){ // not found
							error=true;
							//	Console.WriteLine("Not in stack of open elements");
							formattingElements.Remove(formatting);
							break;
						}
						//	Console.WriteLine("Open elements[%s]:",i);
						//	Console.WriteLine(openElements);
						//	Console.WriteLine("Formatting elements:");
						//	Console.WriteLine(formattingElements);
						if(!hasHtmlElementInScope(formatting.element)){
							error=true;
							return false;
						}
						if(!formatting.element.Equals(getCurrentNode())){
							error=true;
						}
						Element furthestBlock=null;
						int furthestBlockPos=-1;
						for(int j=openElements.Count-1;j>formattingElementPos;j--){
							Element e=openElements[j];
							if(isSpecialElement(e)){
								furthestBlock=e;
								furthestBlockPos=j;
							}
						}
						//	Console.WriteLine("furthest block: %s",furthestBlock);
						if(furthestBlock==null){
							// Pop up to and including the
							// formatting element
							while(openElements.Count>formattingElementPos){
								popCurrentNode();
							}
							formattingElements.Remove(formatting);
							//Console.WriteLine("Open elements now [%s]:",i);
							//Console.WriteLine(openElements);
							//Console.WriteLine("Formatting elements now:");
							//Console.WriteLine(formattingElements);
							break;
						}
						Element commonAncestor=openElements[formattingElementPos-1];
						//	Console.WriteLine("common ancestor: %s",commonAncestor);
						int bookmark=formattingElements.IndexOf(formatting);
						//	Console.WriteLine("bookmark=%d",bookmark);
						Element myNode=furthestBlock;
						Element superiorNode=openElements[furthestBlockPos-1];
						Element lastNode=furthestBlock;
						for(int j=0;j<3;j++){
							myNode=superiorNode;
							FormattingElement nodeFE=getFormattingElement(myNode);
							if(nodeFE==null){
								//	Console.WriteLine("node not a formatting element");
								superiorNode=openElements[openElements.IndexOf(myNode)-1];
								openElements.Remove(myNode);
								continue;
							} else if(myNode.Equals(formatting.element)){
								//	Console.WriteLine("node is the formatting element");
								break;
							}
							Element e=Element.fromToken(nodeFE.token);
							nodeFE.element=e;
							int io=openElements.IndexOf(myNode);
							superiorNode=openElements[io-1];
							openElements[io]=e;
							myNode=e;
							if(lastNode.Equals(furthestBlock)){
								bookmark=formattingElements.IndexOf(nodeFE)+1;
							}
							// NOTE: Because 'node' can only be a formatting
							// element, the foster parenting rule doesn't
							// apply here
							if(lastNode.getParentNode()!=null) {
								((Node) lastNode.getParentNode()).removeChild(lastNode);
							}
							myNode.appendChild(lastNode);
							lastNode=myNode;
						}
						//	Console.WriteLine("node: %s",node);
						//	Console.WriteLine("lastNode: %s",lastNode);
						if(commonAncestor.getLocalName().Equals("table") ||
								commonAncestor.getLocalName().Equals("tr") ||
								commonAncestor.getLocalName().Equals("tbody") ||
								commonAncestor.getLocalName().Equals("thead") ||
								commonAncestor.getLocalName().Equals("tfoot")
								){
							if(lastNode.getParentNode()!=null) {
								((Node) lastNode.getParentNode()).removeChild(lastNode);
							}
							fosterParent(lastNode);
						} else {
							if(lastNode.getParentNode()!=null) {
								((Node) lastNode.getParentNode()).removeChild(lastNode);
							}
							commonAncestor.appendChild(lastNode);
						}
						Element e2=Element.fromToken(formatting.token);
						foreach(Node child in new List<Node>(furthestBlock.getChildNodesInternal())){
							furthestBlock.removeChild(child);
							// NOTE: Because 'e' can only be a formatting
							// element, the foster parenting rule doesn't
							// apply here
							e2.appendChild(child);
						}
						// NOTE: Because intervening elements, including
						// formatting elements, are cleared between table
						// and tbody/thead/tfoot and between those three
						// elements and tr, the foster parenting rule
						// doesn't apply here
						furthestBlock.appendChild(e2);
						FormattingElement newFE=new FormattingElement();
						newFE.marker=false;
						newFE.element=e2;
						newFE.token=formatting.token;
						//	Console.WriteLine("Adding formatting element at %d",bookmark);
						formattingElements.Insert(bookmark,newFE);
						formattingElements.Remove(formatting);
						//	Console.WriteLine("Replacing open element at %d",openElements.IndexOf(furthestBlock)+1);
						int idx=openElements.IndexOf(furthestBlock)+1;
						openElements.Insert(idx,e2);
						openElements.Remove(formatting.element);
					}
				} else if("applet".Equals(name) ||
						"marquee".Equals(name) ||
						"object".Equals(name)){
					if(!hasHtmlElementInScope(name)){
						error=true;
						return false;
					} else {
						generateImpliedEndTags();
						if(!getCurrentNode().getLocalName().Equals(name)){
							error=true;
						}
						while(true){
							Element node=popCurrentNode();
							if(node.getLocalName().Equals(name)) {
								break;
							}
						}
						clearFormattingToMarker();

					}
				} else if(name.Equals("html")){
					if(applyEndTag("body",insMode))
						return applyInsertionMode(token,null);
					return false;
				} else if("address".Equals(name) ||
						"article".Equals(name) ||
						"aside".Equals(name) ||
						"blockquote".Equals(name) ||
						"button".Equals(name) ||
						"center".Equals(name) ||
						"details".Equals(name) ||
						"dialog".Equals(name) ||
						"dir".Equals(name) ||
						"div".Equals(name) ||
						"dl".Equals(name) ||
						"fieldset".Equals(name) ||
						"figcaption".Equals(name) ||
						"figure".Equals(name) ||
						"footer".Equals(name) ||
						"header".Equals(name) ||
						"hgroup".Equals(name) ||
						"listing".Equals(name) ||
						"main".Equals(name) ||
						"menu".Equals(name) ||
						"nav".Equals(name) ||
						"ol".Equals(name) ||
						"pre".Equals(name) ||
						"section".Equals(name) ||
						"summary".Equals(name) ||
						"ul".Equals(name)
						){
					if(!hasHtmlElementInScope(name)){
						error=true;
						return true;
					} else {
						generateImpliedEndTags();
						if(!getCurrentNode().getLocalName().Equals(name)){
							error=true;
						}
						while(true){
							Element node=popCurrentNode();
							if(node.getLocalName().Equals(name)) {
								break;
							}
						}
					}
				} else if(name.Equals("form")){
					Element node=formElement;
					formElement=null;
					if(node==null || hasHtmlElementInScope(node)){
						error=true;
						return true;
					}
					generateImpliedEndTags();
					if(getCurrentNode()!=node){
						error=true;
					}
					openElements.Remove(node);
				} else if(name.Equals("p")){
					if(!hasHtmlElementInButtonScope(name)){
						error=true;
						applyStartTag("p",insMode);
						return applyInsertionMode(token,null);
					}
					generateImpliedEndTagsExcept(name);
					if(!getCurrentNode().getLocalName().Equals(name)){
						error=true;
					}
					while(true){
						Element node=popCurrentNode();
						if(node.getLocalName().Equals(name)) {
							break;
						}
					}
				} else if(name.Equals("li")){
					if(!hasHtmlElementInListItemScope(name)){
						error=true;
						return false;
					}
					generateImpliedEndTagsExcept(name);
					if(!getCurrentNode().getLocalName().Equals(name)){
						error=true;
					}
					while(true){
						Element node=popCurrentNode();
						if(node.getLocalName().Equals(name)) {
							break;
						}
					}
				} else if(name.Equals("h1") || name.Equals("h2") ||
						name.Equals("h3") || name.Equals("h4") ||
						name.Equals("h5") || name.Equals("h6")){
					if(!hasHtmlHeaderElementInScope()){
						error=true;
						return false;
					}
					generateImpliedEndTags();
					if(!getCurrentNode().getLocalName().Equals(name)){
						error=true;
					}
					while(true){
						Element node=popCurrentNode();
						string name2=node.getLocalName();
						if(name2.Equals("h1") ||
								name2.Equals("h2") ||
								name2.Equals("h3") ||
								name2.Equals("h4") ||
								name2.Equals("h5") ||
								name2.Equals("h6")) {
							break;
						}
					}
					return true;
				} else if(name.Equals("dd") || name.Equals("dt")){
					if(!hasHtmlElementInScope(name)){
						error=true;
						return false;
					}
					generateImpliedEndTagsExcept(name);
					if(!getCurrentNode().getLocalName().Equals(name)){
						error=true;
					}
					while(true){
						Element node=popCurrentNode();
						if(node.getLocalName().Equals(name)) {
							break;
						}
					}
				} else if("br".Equals(name)){
					error=true;
					applyStartTag("br",insMode);
					return false;
				} else {
					for(int i=openElements.Count-1;i>=0;i--){
						Element node=openElements[i];
						if(name.Equals(node.getLocalName())){
							generateImpliedEndTagsExcept(name);
							if(!name.Equals(getCurrentNode().getLocalName())){
								error=true;
							}
							while(true){
								Element node2=popCurrentNode();
								if(node2.Equals(node)) {
									break;
								}
							}
							break;
						} else if(isSpecialElement(node)){
							error=true;
							return false;
						}
					}
				}
			}
			return true;
		}
		case InsertionMode.InHeadNoscript:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				if(token==0x09 || token==0x0a || token==0x0c ||
						token==0x0d || token==0x20)
					return applyInsertionMode(token,InsertionMode.InBody);
				else {
					error=true;
					applyEndTag("noscript",insMode);
					return applyInsertionMode(token,null);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				error=true;
				return false;
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("html"))
					return applyInsertionMode(token,InsertionMode.InBody);
				else if(name.Equals("basefont") ||
						name.Equals("bgsound") ||
						name.Equals("link") ||
						name.Equals("meta") ||
						name.Equals("noframes") ||
						name.Equals("style")
						)
					return applyInsertionMode(token,InsertionMode.InHead);
				else if(name.Equals("head") ||
						name.Equals("noscript")){
					error=true;
					return false;
				} else {
					error=true;
					applyEndTag("noscript",insMode);
					return applyInsertionMode(token,null);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("noscript")){
					popCurrentNode();
					insertionMode=InsertionMode.InHead;
				} else if(name.Equals("br")){
					error=true;
					applyEndTag("noscript",insMode);
					return applyInsertionMode(token,null);
				} else {
					error=true;
					return false;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT)
				return applyInsertionMode(token,InsertionMode.InHead);
			else if(token==TOKEN_EOF){
				error=true;
				applyEndTag("noscript",insMode);
				return applyInsertionMode(token,null);
			}
			return true;
		}
		case InsertionMode.InTable:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				Element currentNode=getCurrentNode();
				if(currentNode.getLocalName().Equals("table") ||
						currentNode.getLocalName().Equals("tbody") ||
						currentNode.getLocalName().Equals("tfoot") ||
						currentNode.getLocalName().Equals("thead") ||
						currentNode.getLocalName().Equals("tr")
						){
					pendingTableCharacters.clearAll();
					originalInsertionMode=insertionMode;
					insertionMode=InsertionMode.InTableText;
					return applyInsertionMode(token,null);
				} else {
					// NOTE: Foster parenting rules don't apply here, since
					// the current node isn't table, tbody, tfoot, thead, or
					// tr and won't change while In Body is being applied
					error=true;
					return applyInsertionMode(token,InsertionMode.InBody);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				error=true;
				return false;
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("table")){
					error=true;
					if(applyEndTag("table",insMode))
						return applyInsertionMode(token,null);
					return false;
				} else if(name.Equals("caption")){
					while(true){
						Element node=getCurrentNode();
						if(node==null ||
								node.getLocalName().Equals("table") ||
								node.getLocalName().Equals("html")) {
							break;
						}
						popCurrentNode();
					}
					insertFormattingMarker(tag,addHtmlElement(tag));
					insertionMode=InsertionMode.InCaption;
					return true;
				} else if(name.Equals("colgroup")){
					while(true){
						Element node=getCurrentNode();
						if(node==null ||
								node.getLocalName().Equals("table") ||
								node.getLocalName().Equals("html")) {
							break;
						}
						popCurrentNode();
					}
					addHtmlElement(tag);
					insertionMode=InsertionMode.InColumnGroup;
					return true;
				} else if(name.Equals("col")){
					applyStartTag("colgroup",insMode);
					return applyInsertionMode(token,null);
				} else if(name.Equals("tbody") ||
						name.Equals("tfoot") ||
						name.Equals("thead")){
					while(true){
						Element node=getCurrentNode();
						if(node==null ||
								node.getLocalName().Equals("table") ||
								node.getLocalName().Equals("html")) {
							break;
						}
						popCurrentNode();
					}
					addHtmlElement(tag);
					insertionMode=InsertionMode.InTableBody;
				} else if(name.Equals("td") ||
						name.Equals("th") ||
						name.Equals("tr")){
					applyStartTag("tbody",insMode);
					return applyInsertionMode(token,null);
				} else if(name.Equals("style") ||
						name.Equals("script")){
					applyInsertionMode(token,InsertionMode.InHead);
				} else if(name.Equals("input")){
					string attr=tag.getAttribute("type");
					if(attr==null || !"hidden".Equals(StringUtility.toLowerCaseAscii(attr))){
						error=true;
						doFosterParent=true;
						applyInsertionMode(token,InsertionMode.InBody);
						doFosterParent=false;
					} else {
						error=true;
						addHtmlElementNoPush(tag);
						tag.ackSelfClosing();
					}
				} else if(name.Equals("form")){
					error=true;
					if(formElement!=null)return false;
					formElement=addHtmlElementNoPush(tag);
				} else {
					error=true;
					doFosterParent=true;
					applyInsertionMode(token,InsertionMode.InBody);
					doFosterParent=false;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("table")){
					if(!hasHtmlElementInTableScope(name)){
						error=true;
						return false;
					} else {
						while(true){
							Element node=popCurrentNode();
							if(node.getLocalName().Equals(name)) {
								break;
							}
						}
						resetInsertionMode();
					}
				} else if(name.Equals("body") ||
						name.Equals("caption") ||
						name.Equals("col") ||
						name.Equals("colgroup") ||
						name.Equals("html") ||
						name.Equals("tbody") ||
						name.Equals("td") ||
						name.Equals("tfoot") ||
						name.Equals("th") ||
						name.Equals("thead") ||
						name.Equals("tr")){
					error=true;
					return false;
				} else {
					doFosterParent=true;
					applyInsertionMode(token,InsertionMode.InBody);
					doFosterParent=false;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToCurrentNode(token);
				return true;
			} else if(token==TOKEN_EOF){
				if(getCurrentNode()==null || !getCurrentNode().getLocalName().Equals("html")){
					error=true;
				}
				stopParsing();
			}
			return true;
		}
		case InsertionMode.InTableText:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				if(token==0){
					error=true;
					return false;
				} else {
					pendingTableCharacters.appendInt(token);
				}
			} else {
				bool nonspace=false;
				int[] array=pendingTableCharacters.array();
				int size=pendingTableCharacters.Count;
				for(int i=0;i<size;i++){
					int c=array[i];
					if(c!=0x9 && c!=0xa && c!=0xc && c!=0xd && c!=0x20){
						nonspace=true;
						break;
					}
				}
				if(nonspace){
					// See 'anything else' for 'in table'
					error=true;
					doFosterParent=true;
					for(int i=0;i<size;i++){
						int c=array[i];
						applyInsertionMode(c,InsertionMode.InBody);
					}
					doFosterParent=false;
				} else {
					insertString(getCurrentNode(),pendingTableCharacters.ToString());
				}
				insertionMode=originalInsertionMode;
				return applyInsertionMode(token,null);
			}
			return true;
		}
		case InsertionMode.InCaption:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("caption") ||
						name.Equals("col") ||
						name.Equals("colgroup") ||
						name.Equals("tbody") ||
						name.Equals("thead") ||
						name.Equals("td") ||
						name.Equals("tfoot") ||
						name.Equals("th") ||
						name.Equals("tr")
						){
					error=true;
					if(applyEndTag("caption",insMode))
						return applyInsertionMode(token,null);
				} else
					return applyInsertionMode(token,InsertionMode.InBody);
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("caption")){
					if(!hasHtmlElementInScope(name)){
						error=true;
						return false;
					}
					generateImpliedEndTags();
					if(!getCurrentNode().getLocalName().Equals("caption")){
						error=true;
					}
					while(true){
						Element node=popCurrentNode();
						if(node.getLocalName().Equals("caption")){
							break;
						}
					}
					clearFormattingToMarker();
					insertionMode=InsertionMode.InTable;
				} else if(name.Equals("table")){
					error=true;
					if(applyEndTag("caption",insMode))
						return applyInsertionMode(token,null);
				} else if(name.Equals("body") ||
						name.Equals("col") ||
						name.Equals("colgroup") ||
						name.Equals("tbody") ||
						name.Equals("thead") ||
						name.Equals("td") ||
						name.Equals("tfoot") ||
						name.Equals("th") ||
						name.Equals("tr") ||
						name.Equals("html")
						){
					error=true;
					return false;
				} else
					return applyInsertionMode(token,InsertionMode.InBody);
			} else
				return applyInsertionMode(token,InsertionMode.InBody);
			return true;
		}
		case InsertionMode.InColumnGroup:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				if(token==0x20 || token==0x0c || token==0x0a || token==0x0d || token==0x09){
					insertCharacter(getCurrentNode(),token);
				} else {
					if(applyEndTag("colgroup",insMode))
						return applyInsertionMode(token,null);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				error=true;
				return false;
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("html"))
					return applyInsertionMode(token,InsertionMode.InBody);
				else if(name.Equals("col")){
					addHtmlElementNoPush(tag);
					tag.ackSelfClosing();
				} else {
					if(applyEndTag("colgroup",insMode))
						return applyInsertionMode(token,null);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("colgroup")){
					if(getCurrentNode().getLocalName().Equals("html")){
						error=true;
						return false;
					}
					popCurrentNode();
					insertionMode=InsertionMode.InTable;
				} else if(name.Equals("col")){
					error=true;
					return false;
				} else {
					if(applyEndTag("colgroup",insMode))
						return applyInsertionMode(token,null);
				}

			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				if(applyEndTag("colgroup",insMode))
					return applyInsertionMode(token,null);

			} else if(token==TOKEN_EOF){
				if(getCurrentNode().getLocalName().Equals("html")){
					stopParsing();
					return true;
				}
				if(applyEndTag("colgroup",insMode))
					return applyInsertionMode(token,null);
			}
			return true;
		}
		case InsertionMode.InTableBody:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("tr")){
					while(true){
						Element node=getCurrentNode();
						if(node==null ||
								node.getLocalName().Equals("tbody") ||
								node.getLocalName().Equals("tfoot") ||
								node.getLocalName().Equals("thead") ||
								node.getLocalName().Equals("html")) {
							break;
						}
						popCurrentNode();
					}
					addHtmlElement(tag);
					insertionMode=InsertionMode.InRow;
				} else if(name.Equals("th") || name.Equals("td")){
					error=true;
					applyStartTag("tr",insMode);
					return applyInsertionMode(token,null);
				} else if(name.Equals("caption") ||
						name.Equals("col") ||
						name.Equals("colgroup") ||
						name.Equals("tbody") ||
						name.Equals("tfoot") ||
						name.Equals("thead")){
					if(!hasHtmlElementInTableScope("tbody") &&
							!hasHtmlElementInTableScope("thead") &&
							!hasHtmlElementInTableScope("tfoot")
							){
						error=true;
						return false;
					}
					while(true){
						Element node=getCurrentNode();
						if(node==null ||
								node.getLocalName().Equals("tbody") ||
								node.getLocalName().Equals("tfoot") ||
								node.getLocalName().Equals("thead") ||
								node.getLocalName().Equals("html")) {
							break;
						}
						popCurrentNode();
					}
					applyEndTag(getCurrentNode().getLocalName(),insMode);
					return applyInsertionMode(token,null);
				} else
					return applyInsertionMode(token,InsertionMode.InTable);
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("tbody") ||
						name.Equals("tfoot") ||
						name.Equals("thead")){
					if(!hasHtmlElementInScope(name)){
						error=true;
						return false;
					}
					while(true){
						Element node=getCurrentNode();
						if(node==null ||
								node.getLocalName().Equals("tbody") ||
								node.getLocalName().Equals("tfoot") ||
								node.getLocalName().Equals("thead") ||
								node.getLocalName().Equals("html")) {
							break;
						}
						popCurrentNode();
					}
					popCurrentNode();
					insertionMode=InsertionMode.InTable;
				} else if(name.Equals("table")){
					if(!hasHtmlElementInTableScope("tbody") &&
							!hasHtmlElementInTableScope("thead") &&
							!hasHtmlElementInTableScope("tfoot")
							){
						error=true;
						return false;
					}
					while(true){
						Element node=getCurrentNode();
						if(node==null ||
								node.getLocalName().Equals("tbody") ||
								node.getLocalName().Equals("tfoot") ||
								node.getLocalName().Equals("thead") ||
								node.getLocalName().Equals("html")) {
							break;
						}
						popCurrentNode();
					}
					applyEndTag(getCurrentNode().getLocalName(),insMode);
					return applyInsertionMode(token,null);
				} else if(name.Equals("body") ||
						name.Equals("caption") ||
						name.Equals("col") ||
						name.Equals("colgroup") ||
						name.Equals("html") ||
						name.Equals("td") ||
						name.Equals("th") ||
						name.Equals("tr")){
					error=true;
					return false;
				} else
					return applyInsertionMode(token,InsertionMode.InTable);
			} else
				return applyInsertionMode(token,InsertionMode.InTable);
			return true;
		}
		case InsertionMode.InRow:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				applyInsertionMode(token,InsertionMode.InTable);

			} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				applyInsertionMode(token,InsertionMode.InTable);

			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("th")||name.Equals("td")){
					while(!getCurrentNode().getLocalName().Equals("tr") &&
							!getCurrentNode().getLocalName().Equals("html")){
						popCurrentNode();
					}
					insertionMode=InsertionMode.InCell;
					insertFormattingMarker(tag,addHtmlElement(tag));
				} else if(name.Equals("caption")||
						name.Equals("col")||
						name.Equals("colgroup")||
						name.Equals("tbody")||
						name.Equals("tfoot")||
						name.Equals("thead")||
						name.Equals("tr")){
					if(applyEndTag("tr",insMode))
						return applyInsertionMode(token,null);
				} else {
					applyInsertionMode(token,InsertionMode.InTable);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("tr")){
					if(!hasHtmlElementInTableScope(name)){
						error=true;
						return false;
					}
					while(!getCurrentNode().getLocalName().Equals("tr") &&
							!getCurrentNode().getLocalName().Equals("html")){
						popCurrentNode();
					}
					popCurrentNode();
					insertionMode=InsertionMode.InTableBody;
				} else if(name.Equals("tbody") || name.Equals("tfoot") ||
						name.Equals("thead")){
					if(!hasHtmlElementInTableScope(name)){
						error=true;
						return false;
					}
					applyEndTag("tr",insMode);
					return applyInsertionMode(token,null);
				} else if(name.Equals("caption")||
						name.Equals("col")||
						name.Equals("colgroup")||
						name.Equals("html")||
						name.Equals("body")||
						name.Equals("td")||
						name.Equals("th")){
					error=true;
				} else {
					applyInsertionMode(token,InsertionMode.InTable);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				applyInsertionMode(token,InsertionMode.InTable);

			} else if(token==TOKEN_EOF){
				applyInsertionMode(token,InsertionMode.InTable);

			}
			return true;
		}
		case InsertionMode.InCell:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				applyInsertionMode(token,InsertionMode.InBody);

			} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				applyInsertionMode(token,InsertionMode.InBody);

			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("caption")||
						name.Equals("col")||
						name.Equals("colgroup")||
						name.Equals("tbody")||
						name.Equals("td")||
						name.Equals("tfoot")||
						name.Equals("th")||
						name.Equals("thead")||
						name.Equals("tr")){
					if(!hasHtmlElementInTableScope("td") &&
							!hasHtmlElementInTableScope("th")){
						error=true;
						return false;
					}
					applyEndTag(hasHtmlElementInTableScope("td") ? "td" : "th",insMode);
					return applyInsertionMode(token,null);
				} else {
					applyInsertionMode(token,InsertionMode.InBody);
				}

			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("td") || name.Equals("th")){
					if(!hasHtmlElementInTableScope(name)){
						error=true;
						return false;
					}
					generateImpliedEndTags();
					if(!getCurrentNode().getLocalName().Equals(name)){
						error=true;
					}
					while(true){
						Element node=popCurrentNode();
						if(node.getLocalName().Equals(name)) {
							break;
						}
					}
					clearFormattingToMarker();
					insertionMode=InsertionMode.InRow;
				} else if(name.Equals("caption")||
						name.Equals("col")||
						name.Equals("colgroup")||
						name.Equals("body")||
						name.Equals("html")){
					error=true;
					return false;
				} else if(name.Equals("table")||
						name.Equals("tbody")||
						name.Equals("tfoot")||
						name.Equals("thead")||
						name.Equals("tr")){
					if(!hasHtmlElementInTableScope(name)){
						error=true;
						return false;
					}
					applyEndTag(hasHtmlElementInTableScope("td") ? "td" : "th",insMode);
					return applyInsertionMode(token,null);
				} else {
					applyInsertionMode(token,InsertionMode.InBody);

				}

			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				applyInsertionMode(token,InsertionMode.InBody);

			} else if(token==TOKEN_EOF){
				applyInsertionMode(token,InsertionMode.InBody);

			}
			return true;
		}
		case InsertionMode.InSelect:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				if(token==0){
					error=true; return false;
				} else {
					insertCharacter(getCurrentNode(),token);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				error=true; return false;

			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("html")){
					applyInsertionMode(token,InsertionMode.InBody);
				} else if(name.Equals("option")){
					if(getCurrentNode().getLocalName().Equals("option")){
						applyEndTag("option",insMode);
					}
					addHtmlElement(tag);
				} else if(name.Equals("optgroup")){
					if(getCurrentNode().getLocalName().Equals("option")){
						applyEndTag("option",insMode);
					}
					if(getCurrentNode().getLocalName().Equals("optgroup")){
						applyEndTag("optgroup",insMode);
					}
					addHtmlElement(tag);
				} else if(name.Equals("select")){
					error=true;
					return applyEndTag("select",insMode);
				} else if(name.Equals("input") || name.Equals("keygen") ||
						name.Equals("textarea")){
					error=true;
					if(!hasHtmlElementInSelectScope("select"))
						return false;
					applyEndTag("select",insMode);
					return applyInsertionMode(token,null);
				} else if(name.Equals("script"))
					return applyInsertionMode(token,InsertionMode.InHead);
				else {
					error=true; return false;

				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("optgroup")){
					if(getCurrentNode().getLocalName().Equals("option") &&
							openElements.Count>=2 &&
							openElements[openElements.Count-2].getLocalName().Equals("optgroup")){
						applyEndTag("option",insMode);
					}
					if(getCurrentNode().getLocalName().Equals("optgroup")){
						popCurrentNode();
					} else {
						error=true;
						return false;
					}
				} else if(name.Equals("option")){
					if(getCurrentNode().getLocalName().Equals("option")){
						popCurrentNode();
					} else {
						error=true;
						return false;
					}
				} else if(name.Equals("select")){
					if(!hasHtmlElementInScope(name)){
						error=true;
						return false;
					}
					while(true){
						Element node=popCurrentNode();
						if(node.getLocalName().Equals(name)) {
							break;
						}
					}
					resetInsertionMode();
				} else {
					error=true; return false;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToCurrentNode(token);
			} else if(token==TOKEN_EOF){
				if(getCurrentNode()==null || !getCurrentNode().getLocalName().Equals("html")){
					error=true;
				}
				stopParsing();
			}
			return true;
		}
		case InsertionMode.InSelectInTable:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER)
				return applyInsertionMode(token,InsertionMode.InSelect);
			else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE)
				return applyInsertionMode(token,InsertionMode.InSelect);
			else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("caption") ||
						name.Equals("table") ||
						name.Equals("tbody") ||
						name.Equals("tfoot") ||
						name.Equals("thead") ||
						name.Equals("tr") ||
						name.Equals("td") ||
						name.Equals("th")
						){
					error=true;
					applyEndTag("select",insMode);
					return applyInsertionMode(token,null);
				}
				return applyInsertionMode(token,InsertionMode.InSelect);
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("caption") ||
						name.Equals("table") ||
						name.Equals("tbody") ||
						name.Equals("tfoot") ||
						name.Equals("thead") ||
						name.Equals("tr") ||
						name.Equals("td") ||
						name.Equals("th")
						){
					error=true;
					if(!hasHtmlElementInTableScope(name))
						return false;
					applyEndTag("select",insMode);
					return applyInsertionMode(token,null);
				}
				return applyInsertionMode(token,InsertionMode.InSelect);
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT)
				return applyInsertionMode(token,InsertionMode.InSelect);
			else if(token==TOKEN_EOF)
				return applyInsertionMode(token,InsertionMode.InSelect);
			return true;
		}
		case InsertionMode.AfterBody:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				if(token==0x09 || token==0x0a || token==0x0c || token==0x0d || token==0x20){
					applyInsertionMode(token,InsertionMode.InBody);
				} else {
					error=true;
					insertionMode=InsertionMode.InBody;
					return applyInsertionMode(token,null);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				error=true;
				return true;
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("html")){
					applyInsertionMode(token,InsertionMode.InBody);
				} else {
					error=true;
					insertionMode=InsertionMode.InBody;
					return applyInsertionMode(token,null);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("html")){
					if(context!=null){
						error=true;
						return false;
					}
					insertionMode=InsertionMode.AfterAfterBody;
				} else {
					error=true;
					insertionMode=InsertionMode.InBody;
					return applyInsertionMode(token,null);
				}

			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToFirst(token);


			} else if(token==TOKEN_EOF){
				stopParsing();

				return true;
			}
			return true;
		}
		case InsertionMode.InFrameset:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				if(token==0x09 || token==0x0a || token==0x0c ||
						token==0x0d || token==0x20) {
					insertCharacter(getCurrentNode(),token);
				} else {
					error=true;
					return false;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				error=true;
				return false;
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("html")){
					applyInsertionMode(token,InsertionMode.InBody);
				} else if(name.Equals("frameset")){
					addHtmlElement(tag);
				} else if(name.Equals("frame")){
					addHtmlElementNoPush(tag);
					tag.ackSelfClosing();
				} else if(name.Equals("noframes")){
					applyInsertionMode(token,InsertionMode.InHead);
				} else {
					error=true;
				}

			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				if(getCurrentNode().getLocalName().Equals("html")){
					error=true;
					return false;
				}
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("frameset")){
					popCurrentNode();
					if(context==null &&
							!getCurrentNode().isHtmlElement("frameset")){
						insertionMode=InsertionMode.AfterFrameset;
					}
				} else {
					error=true;
				}

			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToCurrentNode(token);


			} else if(token==TOKEN_EOF){
				if(!getCurrentNode().isHtmlElement("html")) {
					error=true;
				}
				stopParsing();

			}
			return true;
		}
		case InsertionMode.AfterFrameset:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				if(token==0x09 || token==0x0a || token==0x0c || token==0x0d || token==0x20){
					insertCharacter(getCurrentNode(),token);
				} else {
					error=true;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				error=true;
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("html"))
					return applyInsertionMode(token,InsertionMode.InBody);
				else if(name.Equals("noframes"))
					return applyInsertionMode(token,InsertionMode.InHead);
				else {
					error=true;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				EndTagToken tag=(EndTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("html")){
					insertionMode=InsertionMode.AfterAfterFrameset;
				} else {
					error=true;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToCurrentNode(token);

			} else if(token==TOKEN_EOF){
				stopParsing();

			}
			return true;
		}
		case InsertionMode.AfterAfterBody:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				if(token==0x09 || token==0x0a || token==0x0c || token==0x0d || token==0x20){
					applyInsertionMode(token,InsertionMode.InBody);
				} else {
					error=true;
					insertionMode=InsertionMode.InBody;
					return applyInsertionMode(token,null);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				applyInsertionMode(token,InsertionMode.InBody);
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if(name.Equals("html")){
					applyInsertionMode(token,InsertionMode.InBody);
				} else {
					error=true;
					insertionMode=InsertionMode.InBody;
					return applyInsertionMode(token,null);
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				error=true;
				insertionMode=InsertionMode.InBody;
				return applyInsertionMode(token,null);

			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToDocument(token);

			} else if(token==TOKEN_EOF){
				stopParsing();

			}
			return true;
		}
		case InsertionMode.AfterAfterFrameset:{
			if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER){
				if(token==0x09 || token==0x0a || token==0x0c || token==0x0d || token==0x20){
					applyInsertionMode(token,InsertionMode.InBody);
				} else {
					error=true;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_DOCTYPE){
				applyInsertionMode(token,InsertionMode.InBody);
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				string name=tag.getName();
				if("html".Equals(name)){
					applyInsertionMode(token,InsertionMode.InBody);
				} else if("noframes".Equals(name)){
					applyInsertionMode(token,InsertionMode.InHead);
				} else {
					error=true;
				}
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_END_TAG){
				error=true;
			} else if((token&TOKEN_TYPE_MASK)==TOKEN_COMMENT){
				addCommentNodeToDocument(token);

			} else if(token==TOKEN_EOF){
				stopParsing();

			}
			return true;
		}
		default:
			throw new InvalidOperationException();
		}
	}


	private bool applyStartTag(string name, InsertionMode? insMode)  {
		return applyInsertionMode(getArtificialToken(TOKEN_START_TAG,name),insMode);
	}

	private void changeEncoding(string charset)  {
		string currentEncoding=encoding.getEncoding();
		if(currentEncoding.Equals("utf-16le") ||
				currentEncoding.Equals("utf-16be")){
			encoding=new EncodingConfidence(currentEncoding,EncodingConfidence.Certain);
			return;
		}
		if(charset.Equals("utf-16le")) {
			charset="utf-8";
		} else if(charset.Equals("utf-16be")) {
			charset="utf-8";
		}
		if(charset.Equals(currentEncoding)){
			encoding=new EncodingConfidence(currentEncoding,EncodingConfidence.Certain);
			return;
		}
		// Reinitialize all parser state
		initialize();
		// Rewind the input stream and set the new encoding
		inputStream.rewind();
		encoding=new EncodingConfidence(charset,EncodingConfidence.Certain);
		decoder=new Html5Decoder(TextEncoding.getDecoder(encoding.getEncoding()));
		charInput=new StackableCharacterInput(new DecoderCharacterInput(inputStream,decoder));
	}

	private void clearFormattingToMarker() {
		while(formattingElements.Count>0){
			FormattingElement fe=removeAtIndex(formattingElements,formattingElements.Count-1);
			if(fe.isMarker()) {
				break;
			}
		}
	}

	private void closeParagraph(InsertionMode? insMode) {
		if(hasHtmlElementInButtonScope("p")){
			applyEndTag("p",insMode);
		}
	}

	private void fosterParent(Node element) {
		if(openElements.Count==0)return;
		Node fosterParent=openElements[0];
		for(int i=openElements.Count-1;i>=0;i--){
			Element e=openElements[i];
			if(e.getLocalName().Equals("table")){
				Node parent=(Node) e.getParentNode();
				bool isElement=(parent!=null && parent.getNodeType()==NodeType.ELEMENT_NODE);
				if(!isElement){ // the parent is not an element
					if(i<=1)
						// This usually won't happen
						throw new InvalidOperationException();
					// append to the element before this table
					fosterParent=openElements[i-1];
					break;
				} else {
					// Parent of the table, insert before the table
					parent.insertBefore(element,e);
					return;
				}
			}
		}
		fosterParent.appendChild(element);
	}

	private void generateImpliedEndTags(){
		while(true){
			Element node=getCurrentNode();
			string name=node.getLocalName();
			if("dd".Equals(name)||
					"dd".Equals(name)||
					"dt".Equals(name)||
					"li".Equals(name)||
					"option".Equals(name)||
					"optgroup".Equals(name)||
					"p".Equals(name)||
					"rp".Equals(name)||
					"rt".Equals(name)){
				popCurrentNode();
			} else {
				break;
			}
		}
	}


	private void generateImpliedEndTagsExcept(string _string) {
		while(true){
			Element node=getCurrentNode();
			string name=node.getLocalName();
			if(_string.Equals(name)) {
				break;
			}
			if("dd".Equals(name)||
					"dd".Equals(name)||
					"dt".Equals(name)||
					"li".Equals(name)||
					"option".Equals(name)||
					"optgroup".Equals(name)||
					"p".Equals(name)||
					"rp".Equals(name)||
					"rt".Equals(name)){
				popCurrentNode();
			} else {
				break;
			}
		}
	}
	private int getArtificialToken(int type, string name){
		if(type==TOKEN_END_TAG){
			EndTagToken token=new EndTagToken(name);
			int ret=tokens.Count|type;
			tokens.Add(token);
			return ret;
		}
		if(type==TOKEN_START_TAG){
			StartTagToken token=new StartTagToken(name);
			int ret=tokens.Count|type;
			tokens.Add(token);
			return ret;
		}
		throw new ArgumentException();
	}
	private Element getCurrentNode(){
		if(openElements.Count==0)return null;
		return openElements[openElements.Count-1];
	}
	private FormattingElement getFormattingElement(Element node) {
		foreach(FormattingElement fe in formattingElements){
			if(!fe.isMarker() && node.Equals(fe.element))
				return fe;
		}
		return null;
	}
	internal IToken getToken(int token){
		if((token&TOKEN_TYPE_MASK)==TOKEN_CHARACTER ||
				(token&TOKEN_TYPE_MASK)==TOKEN_EOF)
			return null;
		else
			return tokens[token&TOKEN_INDEX_MASK];
	}
	private bool hasHtmlElementInButtonScope(string name){
		bool found=false;
		foreach(Element e in openElements){
			if(e.getLocalName().Equals(name)){
				found=true;
			}
		}
		if(!found)return false;
		for(int i=openElements.Count-1;i>=0;i--){
			Element e=openElements[i];
			string _namespace=e.getNamespaceURI();
			string thisName=e.getLocalName();
			if(HTML_NAMESPACE.Equals(_namespace)){
				if(thisName.Equals(name))
					return true;
				if(thisName.Equals("applet")||
						thisName.Equals("caption")||
						thisName.Equals("html")||
						thisName.Equals("table")||
						thisName.Equals("td")||
						thisName.Equals("th")||
						thisName.Equals("marquee")||
						thisName.Equals("object")||
						thisName.Equals("button"))
					//Console.WriteLine("not in scope: %s",thisName);
					return false;
			}
			if(MATHML_NAMESPACE.Equals(_namespace)){
				if(thisName.Equals("mi")||
						thisName.Equals("mo")||
						thisName.Equals("mn")||
						thisName.Equals("ms")||
						thisName.Equals("mtext")||
						thisName.Equals("annotation-xml"))
					return false;
			}
			if(SVG_NAMESPACE.Equals(_namespace)){
				if(thisName.Equals("foreignObject")||
						thisName.Equals("desc")||
						thisName.Equals("title"))
					return false;
			}
		}
		return false;
	}
	private bool hasHtmlElementInListItemScope(string name) {
		for(int i=openElements.Count-1;i>=0;i--){
			Element e=openElements[i];
			if(e.isHtmlElement(name))
				return true;
			if(e.isHtmlElement("applet")||
					e.isHtmlElement("caption")||
					e.isHtmlElement("html")||
					e.isHtmlElement("table")||
					e.isHtmlElement("td")||
					e.isHtmlElement("th")||
					e.isHtmlElement("ol")||
					e.isHtmlElement("ul")||
					e.isHtmlElement("marquee")||
					e.isHtmlElement("object")||
					e.isMathMLElement("mi")||
					e.isMathMLElement("mo")||
					e.isMathMLElement("mn")||
					e.isMathMLElement("ms")||
					e.isMathMLElement("mtext")||
					e.isMathMLElement("annotation-xml")||
					e.isSvgElement("foreignObject")||
					e.isSvgElement("desc")||
					e.isSvgElement("title")
					)
				return false;
		}
		return false;
	}
	private bool hasHtmlElementInScope(Element node) {
		for(int i=openElements.Count-1;i>=0;i--){
			Element e=openElements[i];
			if(e==node)
				return true;
			if(e.isHtmlElement("applet")||
					e.isHtmlElement("caption")||
					e.isHtmlElement("html")||
					e.isHtmlElement("table")||
					e.isHtmlElement("td")||
					e.isHtmlElement("th")||
					e.isHtmlElement("marquee")||
					e.isHtmlElement("object")||
					e.isMathMLElement("mi")||
					e.isMathMLElement("mo")||
					e.isMathMLElement("mn")||
					e.isMathMLElement("ms")||
					e.isMathMLElement("mtext")||
					e.isMathMLElement("annotation-xml")||
					e.isSvgElement("foreignObject")||
					e.isSvgElement("desc")||
					e.isSvgElement("title")
					)
				return false;
		}
		return false;
	}

	private bool hasHtmlElementInScope(string name){
		for(int i=openElements.Count-1;i>=0;i--){
			Element e=openElements[i];
			if(e.isHtmlElement(name))
				return true;
			if(e.isHtmlElement("applet")||
					e.isHtmlElement("caption")||
					e.isHtmlElement("html")||
					e.isHtmlElement("table")||
					e.isHtmlElement("td")||
					e.isHtmlElement("th")||
					e.isHtmlElement("marquee")||
					e.isHtmlElement("object")||
					e.isMathMLElement("mi")||
					e.isMathMLElement("mo")||
					e.isMathMLElement("mn")||
					e.isMathMLElement("ms")||
					e.isMathMLElement("mtext")||
					e.isMathMLElement("annotation-xml")||
					e.isSvgElement("foreignObject")||
					e.isSvgElement("desc")||
					e.isSvgElement("title")
					)
				return false;
		}
		return false;
	}



	private bool hasHtmlElementInSelectScope(string name) {
		for(int i=openElements.Count-1;i>=0;i--){
			Element e=openElements[i];
			if(e.isHtmlElement(name))
				return true;
			if(!e.isHtmlElement("optgroup") && !e.isHtmlElement("option"))
				return false;
		}
		return false;
	}

	private bool hasHtmlElementInTableScope(string name) {
		for(int i=openElements.Count-1;i>=0;i--){
			Element e=openElements[i];
			if(e.isHtmlElement(name))
				return true;
			if(e.isHtmlElement("html")||
					e.isHtmlElement("table")
					)
				return false;
		}
		return false;

	}

	private bool hasHtmlHeaderElementInScope() {
		for(int i=openElements.Count-1;i>=0;i--){
			Element e=openElements[i];
			if(e.isHtmlElement("h1")||
					e.isHtmlElement("h2")||
					e.isHtmlElement("h3")||
					e.isHtmlElement("h4")||
					e.isHtmlElement("h5")||
					e.isHtmlElement("h6"))
				return true;
			if(e.isHtmlElement("applet")||
					e.isHtmlElement("caption")||
					e.isHtmlElement("html")||
					e.isHtmlElement("table")||
					e.isHtmlElement("td")||
					e.isHtmlElement("th")||
					e.isHtmlElement("marquee")||
					e.isHtmlElement("object")||
					e.isMathMLElement("mi")||
					e.isMathMLElement("mo")||
					e.isMathMLElement("mn")||
					e.isMathMLElement("ms")||
					e.isMathMLElement("mtext")||
					e.isMathMLElement("annotation-xml")||
					e.isSvgElement("foreignObject")||
					e.isSvgElement("desc")||
					e.isSvgElement("title")
					)
				return false;
		}
		return false;
	}

	private void insertFormattingMarker(StartTagToken tag,
			Element addHtmlElement) {
		FormattingElement fe=new FormattingElement();
		fe.marker=true;
		fe.element=addHtmlElement;
		fe.token=tag;
		formattingElements.Add(fe);
	}

	private bool isAppropriateEndTag(){
		if(lastStartTag==null || currentEndTag==null)
			return false;
		//Console.WriteLine("lastStartTag=%s",lastStartTag.getName());
		//Console.WriteLine("currentEndTag=%s",currentEndTag.getName());
		return currentEndTag.getName().Equals(lastStartTag.getName());
	}

	private bool isSpecialElement(Element node) {
		if(node.isHtmlElement("address") || node.isHtmlElement("applet") || node.isHtmlElement("area") || node.isHtmlElement("article") || node.isHtmlElement("aside") || node.isHtmlElement("base") || node.isHtmlElement("basefont") || node.isHtmlElement("bgsound") || node.isHtmlElement("blockquote") || node.isHtmlElement("body") || node.isHtmlElement("br") || node.isHtmlElement("button") || node.isHtmlElement("caption") || node.isHtmlElement("center") || node.isHtmlElement("col") || node.isHtmlElement("colgroup") || node.isHtmlElement("dd") || node.isHtmlElement("details") || node.isHtmlElement("dir") || node.isHtmlElement("div") || node.isHtmlElement("dl") || node.isHtmlElement("dt") || node.isHtmlElement("embed") || node.isHtmlElement("fieldset") || node.isHtmlElement("figcaption") || node.isHtmlElement("figure")
				|| node.isHtmlElement("footer") || node.isHtmlElement("form") || node.isHtmlElement("frame") || node.isHtmlElement("frameset") || node.isHtmlElement("h1") || node.isHtmlElement("h2") || node.isHtmlElement("h3") || node.isHtmlElement("h4") || node.isHtmlElement("h5") || node.isHtmlElement("h6") || node.isHtmlElement("head") || node.isHtmlElement("header") || node.isHtmlElement("hgroup") || node.isHtmlElement("hr") || node.isHtmlElement("html") || node.isHtmlElement("iframe") || node.isHtmlElement("img") || node.isHtmlElement("input") || node.isHtmlElement("isindex") || node.isHtmlElement("li") || node.isHtmlElement("link") ||
				node.isHtmlElement("listing") || node.isHtmlElement("main") || node.isHtmlElement("marquee") || node.isHtmlElement("menu") || node.isHtmlElement("menuitem") || node.isHtmlElement("meta") || node.isHtmlElement("nav") || node.isHtmlElement("noembed") || node.isHtmlElement("noframes") || node.isHtmlElement("noscript") || node.isHtmlElement("object") || node.isHtmlElement("ol") || node.isHtmlElement("p") || node.isHtmlElement("param") || node.isHtmlElement("plaintext") || node.isHtmlElement("pre") || node.isHtmlElement("script") || node.isHtmlElement("section") ||
				node.isHtmlElement("select") || node.isHtmlElement("source") || node.isHtmlElement("style") || node.isHtmlElement("summary") || node.isHtmlElement("table") || node.isHtmlElement("tbody") || node.isHtmlElement("td") || node.isHtmlElement("textarea") || node.isHtmlElement("tfoot") || node.isHtmlElement("th") || node.isHtmlElement("thead") || node.isHtmlElement("title") || node.isHtmlElement("tr") || node.isHtmlElement("track") || node.isHtmlElement("ul") || node.isHtmlElement("wbr") || node.isHtmlElement("xmp"))
			return true;
		if(node.isMathMLElement("mi") || node.isMathMLElement("mo") || node.isMathMLElement("mn") || node.isMathMLElement("ms") || node.isMathMLElement("mtext") || node.isMathMLElement("annotation-xml"))
			return true;
		if(node.isSvgElement("foreignObject") || node.isSvgElement("desc") || node.isSvgElement("title"))
			return true;

		return false;
	}

	private int parseCharacterReference(int allowedCharacter) {
		int markStart=charInput.setSoftMark();
		int c1=charInput.read();
		if(c1<0 || c1==0x09 || c1==0x0a || c1==0x0c ||
				c1==0x20 || c1==0x3c || c1==0x26 || (allowedCharacter>=0 && c1==allowedCharacter)){
			charInput.setMarkPosition(markStart);
			return 0x26; // emit ampersand
		} else if(c1==0x23){
			c1=charInput.read();
			int value=0;
			bool haveHex=false;
			if(c1==0x78 || c1==0x58){
				// Hex number
				while(true){ // skip zeros
					int c=charInput.read();
					if(c!='0'){
						if(c>=0) {
							charInput.moveBack(1);
						}
						break;
					}
					haveHex=true;
				}
				bool overflow=false;
				while(true){
					int number=charInput.read();
					if(number>='0' && number<='9'){
						if(!overflow) {
							value=(value<<4)+(number-'0');
						}
						haveHex=true;
					} else if(number>='a' && number<='f'){
						if(!overflow) {
							value=(value<<4)+(number-'a')+10;
						}
						haveHex=true;
					} else if(number>='A' && number<='F'){
						if(!overflow) {
							value=(value<<4)+(number-'A')+10;
						}
						haveHex=true;
					} else {
						if(number>=0) {
							// move back character (except if it's EOF)
							charInput.moveBack(1);
						}
						break;
					}
					if(value>0x10FFFF){
						value=0x110000; overflow=true;
					}
				}
			} else {
				if(c1>0) {
					charInput.moveBack(1);
				}
				// Digits
				while(true){ // skip zeros
					int c=charInput.read();
					if(c!='0'){
						if(c>=0) {
							charInput.moveBack(1);
						}
						break;
					}
					haveHex=true;
				}
				bool overflow=false;
				while(true){
					int number=charInput.read();
					if(number>='0' && number<='9'){
						if(!overflow) {
							value=(value*10)+(number-'0');
						}
						haveHex=true;
					} else {
						if(number>=0) {
							// move back character (except if it's EOF)
							charInput.moveBack(1);
						}
						break;
					}
					if(value>0x10FFFF){
						value=0x110000; overflow=true;
					}
				}
			}
			if(!haveHex){
				// No digits: parse error
				error=true;
				charInput.setMarkPosition(markStart);
				return 0x26; // emit ampersand
			}
			c1=charInput.read();
			if(c1!=0x3B){ // semicolon
				error=true;
				if(c1>=0)
				{
					charInput.moveBack(1); // parse error
				}
			}
			if(value>0x10FFFF || (value>=0xD800 && value<=0xDFFF)){
				error=true;
				value=0xFFFD; // parse error
			} else if(value>=0x80 && value<0xA0){
				error=true;
				// parse error
				int[] replacements=new int[]{
						0x20ac,0x81,0x201a,0x192,0x201e,
						0x2026,0x2020,0x2021,0x2c6,0x2030,
						0x160,0x2039,0x152,0x8d,0x17d,
						0x8f,0x90,0x2018,0x2019,0x201c,0x201d,
						0x2022,0x2013,0x2014,0x2dc,0x2122,
						0x161,0x203a,0x153,0x9d,0x17e,0x178
				};
				value=replacements[value-0x80];
			} else if(value==0x0D){
				// parse error
				error=true;
			} else if(value==0x00){
				// parse error
				error=true;
				value=0xFFFD;
			}
			if(value==0x08 || value==0x0B ||
					(value&0xFFFE)==0xFFFE ||
					(value>=0x0e && value<=0x1f) ||
					value==0x7F || (value>=0xFDD0 && value<=0xFDEF)){
				// parse error
				error=true;
			}
			return value;
		} else if((c1>='A' && c1<='Z') ||
				(c1>='a' && c1<='z') ||
				(c1>='0' && c1<='9')){
			int[] data=null;
			// check for certain well-known entities
			if(c1=='g'){
				if(charInput.read()=='t' && charInput.read()==';')
					return '>';
				charInput.setMarkPosition(markStart+1);
			} else if(c1=='l'){
				if(charInput.read()=='t' && charInput.read()==';')
					return '<';
				charInput.setMarkPosition(markStart+1);
			} else if(c1=='a'){
				if(charInput.read()=='m' && charInput.read()=='p' && charInput.read()==';')
					return '&';
				charInput.setMarkPosition(markStart+1);
			} else if(c1=='n'){
				if(charInput.read()=='b' && charInput.read()=='s' && charInput.read()=='p' && charInput.read()==';')
					return 0xa0;
				charInput.setMarkPosition(markStart+1);
			}
			int count=0;
			for(int index=0;index<entities.Length;index++){
				string entity=entities[index];
				if(entity[0]==c1){
					if(data==null){
						// Read the rest of the character reference
						// (the entities are sorted by length, so
						// we get the maximum length possible starting
						// with the first matching character)
						data=new int[entity.Length-1];
						count=charInput.read(data,0,data.Length);
						//Console.WriteLine("markposch=%c",(char)data[0]);
					}
					// if fewer bytes were read than the
					// entity's remaining length, this
					// can't match
					//Console.WriteLine("data count=%s %s",count,stream.getMarkPosition());
					if(count<entity.Length-1) {
						continue;
					}
					bool matched=true;
					for(int i=1;i<entity.Length;i++){
						//Console.WriteLine("%c %c | markpos=%d",
						//	(char)data[i-1],entity[i],stream.getMarkPosition());
						if(data[i-1]!=entity[i]){
							matched=false;
							break;
						}
					}
					if(matched){
						// Move back the difference between the
						// number of bytes actually read and
						// this entity's length
						charInput.moveBack(count-(entity.Length-1));
						//Console.WriteLine("lastchar=%c",entity[entity.Length-1]);
						if(allowedCharacter>=0 &&
								entity[entity.Length-1]!=';'){
							// Get the next character after the entity
							int ch2=charInput.read();
							if(ch2=='=' || (ch2>='A' && ch2<='Z') ||
									(ch2>='a' && ch2<='z') ||
									(ch2>='0' && ch2<='9')){
								if(ch2=='=') {
									error=true;
								}
								charInput.setMarkPosition(markStart);
								return 0x26; // return ampersand rather than entity
							} else {
								if(ch2>=0) {
									charInput.moveBack(1);
								}
								if(entity[entity.Length-1]!=';'){
									error=true;
								}
							}
						} else {
							if(entity[entity.Length-1]!=';'){
								error=true;
							}
						}
						return entityValues[index];
					}
				}
			}
			// no match
			charInput.setMarkPosition(markStart);
			while(true){
				int ch2=charInput.read();
				if(ch2==';'){
					error=true;
					break;
				} else if(!((ch2>='A' && ch2<='Z') ||
						(ch2>='a' && ch2<='z') ||
						(ch2>='0' && ch2<='9'))){
					break;
				}
			}
			charInput.setMarkPosition(markStart);
			return 0x26;
		} else {
			// not a character reference
			charInput.setMarkPosition(markStart);
			return 0x26; // emit ampersand
		}
	}


	private void adjustSvgAttributes(StartTagToken token){
		IList<Attrib> attributes=token.getAttributes();
		foreach(Attrib attr in attributes){
			string name=attr.getName();
			if(name.Equals("attributename")){ attr.setName("attributeName"); }
			else if(name.Equals("attributetype")){ attr.setName("attributeType");  }
			else if(name.Equals("basefrequency")){ attr.setName("baseFrequency");  }
			else if(name.Equals("baseprofile")){ attr.setName("baseProfile");  }
			else if(name.Equals("calcmode")){ attr.setName("calcMode");  }
			else if(name.Equals("clippathunits")){ attr.setName("clipPathUnits");  }
			else if(name.Equals("contentscripttype")){ attr.setName("contentScriptType");  }
			else if(name.Equals("contentstyletype")){ attr.setName("contentStyleType");  }
			else if(name.Equals("diffuseconstant")){ attr.setName("diffuseConstant");  }
			else if(name.Equals("edgemode")){ attr.setName("edgeMode");  }
			else if(name.Equals("externalresourcesrequired")){ attr.setName("externalResourcesRequired");  }
			else if(name.Equals("filterres")){ attr.setName("filterRes");  }
			else if(name.Equals("filterunits")){ attr.setName("filterUnits");  }
			else if(name.Equals("glyphref")){ attr.setName("glyphRef");  }
			else if(name.Equals("gradienttransform")){ attr.setName("gradientTransform");  }
			else if(name.Equals("gradientunits")){ attr.setName("gradientUnits");  }
			else if(name.Equals("kernelmatrix")){ attr.setName("kernelMatrix");  }
			else if(name.Equals("kernelunitlength")){ attr.setName("kernelUnitLength");  }
			else if(name.Equals("keypoints")){ attr.setName("keyPoints");  }
			else if(name.Equals("keysplines")){ attr.setName("keySplines");  }
			else if(name.Equals("keytimes")){ attr.setName("keyTimes");  }
			else if(name.Equals("lengthadjust")){ attr.setName("lengthAdjust");  }
			else if(name.Equals("limitingconeangle")){ attr.setName("limitingConeAngle");  }
			else if(name.Equals("markerheight")){ attr.setName("markerHeight");  }
			else if(name.Equals("markerunits")){ attr.setName("markerUnits");  }
			else if(name.Equals("markerwidth")){ attr.setName("markerWidth");  }
			else if(name.Equals("maskcontentunits")){ attr.setName("maskContentUnits");  }
			else if(name.Equals("maskunits")){ attr.setName("maskUnits");  }
			else if(name.Equals("numoctaves")){ attr.setName("numOctaves");  }
			else if(name.Equals("pathlength")){ attr.setName("pathLength");  }
			else if(name.Equals("patterncontentunits")){ attr.setName("patternContentUnits");  }
			else if(name.Equals("patterntransform")){ attr.setName("patternTransform");  }
			else if(name.Equals("patternunits")){ attr.setName("patternUnits");  }
			else if(name.Equals("pointsatx")){ attr.setName("pointsAtX");  }
			else if(name.Equals("pointsaty")){ attr.setName("pointsAtY");  }
			else if(name.Equals("pointsatz")){ attr.setName("pointsAtZ");  }
			else if(name.Equals("preservealpha")){ attr.setName("preserveAlpha");  }
			else if(name.Equals("preserveaspectratio")){ attr.setName("preserveAspectRatio");  }
			else if(name.Equals("primitiveunits")){ attr.setName("primitiveUnits");  }
			else if(name.Equals("refx")){ attr.setName("refX");  }
			else if(name.Equals("refy")){ attr.setName("refY");  }
			else if(name.Equals("repeatcount")){ attr.setName("repeatCount");  }
			else if(name.Equals("repeatdur")){ attr.setName("repeatDur");  }
			else if(name.Equals("requiredextensions")){ attr.setName("requiredExtensions");  }
			else if(name.Equals("requiredfeatures")){ attr.setName("requiredFeatures");  }
			else if(name.Equals("specularconstant")){ attr.setName("specularConstant");  }
			else if(name.Equals("specularexponent")){ attr.setName("specularExponent");  }
			else if(name.Equals("spreadmethod")){ attr.setName("spreadMethod");  }
			else if(name.Equals("startoffset")){ attr.setName("startOffset");  }
			else if(name.Equals("stddeviation")){ attr.setName("stdDeviation");  }
			else if(name.Equals("stitchtiles")){ attr.setName("stitchTiles");  }
			else if(name.Equals("surfacescale")){ attr.setName("surfaceScale");  }
			else if(name.Equals("systemlanguage")){ attr.setName("systemLanguage");  }
			else if(name.Equals("tablevalues")){ attr.setName("tableValues");  }
			else if(name.Equals("targetx")){ attr.setName("targetX");  }
			else if(name.Equals("targety")){ attr.setName("targetY");  }
			else if(name.Equals("textlength")){ attr.setName("textLength");  }
			else if(name.Equals("viewbox")){ attr.setName("viewBox");  }
			else if(name.Equals("viewtarget")){ attr.setName("viewTarget");  }
			else if(name.Equals("xchannelselector")){ attr.setName("xChannelSelector");  }
			else if(name.Equals("ychannelselector")){ attr.setName("yChannelSelector");  }
			else if(name.Equals("zoomandpan")){ attr.setName("zoomAndPan");  }
		}
	}
	private void adjustMathMLAttributes(StartTagToken token){
		IList<Attrib> attributes=token.getAttributes();
		foreach(Attrib attr in attributes){
			if(attr.getName().Equals("definitionurl")){
				attr.setName("definitionURL");
			}
		}
	}

	public static readonly string XLINK_NAMESPACE="http://www.w3.org/1999/xlink";
	public static readonly string XML_NAMESPACE="http://www.w3.org/XML/1998/_namespace";
	private static readonly string XMLNS_NAMESPACE="http://www.w3.org/2000/xmlns/";

	private void adjustForeignAttributes(StartTagToken token){
		IList<Attrib> attributes=token.getAttributes();
		foreach(Attrib attr in attributes){
			string name=attr.getName();
			if(name.Equals("xlink:actuate") ||
					name.Equals("xlink:arcrole") ||
					name.Equals("xlink:href") ||
					name.Equals("xlink:role") ||
					name.Equals("xlink:show") ||
					name.Equals("xlink:title") ||
					name.Equals("xlink:type")
					){
				attr.setNamespace(XLINK_NAMESPACE);
			}
			else if(name.Equals("xml:base") ||
					name.Equals("xml:lang") ||
					name.Equals("xml:space")
					){
				attr.setNamespace(XML_NAMESPACE);
			}
			else if(name.Equals("xmlns") ||
					name.Equals("xmlns:xlink")){
				attr.setNamespace(XMLNS_NAMESPACE);
			}
		}
	}

	public IDocument parse()  {
		while(true){
			int token=parserRead();
			applyInsertionMode(token,null);
			if((token&TOKEN_TYPE_MASK)==TOKEN_START_TAG){
				StartTagToken tag=(StartTagToken)getToken(token);
				//	Console.WriteLine(tag);
				if(!tag.isAckSelfClosing()){
					error=true;
				}
			}
			//	Console.WriteLine("token=%08X, insertionMode=%s, error=%s",token,insertionMode,error);
			if(done){
				break;
			}
		}
		return document;
	}
	internal int parserRead() {
		int token=parserReadInternal();
		//Console.WriteLine("token=%08X [%c]",token,token&0xFF);
		if(decoder.isError()) {
			error=true;
		}
		return token;
	}
	private int parserReadInternal() {
		if(tokenQueue.Count>0)
			return removeAtIndex(tokenQueue,0);
		while(true){
			//Console.WriteLine(state);
			switch(state){
			case TokenizerState.Data:
				int c=charInput.read();
				if(c==0x26){
					state=TokenizerState.CharacterRefInData;
				} else if(c==0x3c){
					state=TokenizerState.TagOpen;
				} else if(c==0){
					error=true;
					return c;
				} else if(c<0)
					return TOKEN_EOF;
				else {
					int ret=c;
					// Keep reading characters to
					// reduce the need to re-call
					// this method
					int mark=charInput.setSoftMark();
					for(int i=0;i<100;i++){
						c=charInput.read();
						if(c>0 && c!=0x26 && c!=0x3c){
							tokenQueue.Add(c);
						} else {
							charInput.setMarkPosition(mark+i);
							break;
						}
					}
					return ret;
				}
				break;
			case TokenizerState.CharacterRefInData:{
				state=TokenizerState.Data;
				int charref=parseCharacterReference(-1);
				if(charref<0){
					// more than one character in this reference
					int index=Math.Abs(charref+1);
					tokenQueue.Add(entityDoubles[index*2+1]);
					return entityDoubles[index*2];
				}
				return charref;
			}
			case TokenizerState.CharacterRefInRcData:{
				state=TokenizerState.RcData;
				int charref=parseCharacterReference(-1);
				if(charref<0){
					// more than one character in this reference
					int index=Math.Abs(charref+1);
					tokenQueue.Add(entityDoubles[index*2+1]);
					return entityDoubles[index*2];
				}
				return charref;
			}
			case TokenizerState.RcData:
				int c1=charInput.read();
				if(c1==0x26) {
					state=TokenizerState.CharacterRefInRcData;
				} else if(c1==0x3c) {
					state=TokenizerState.RcDataLessThan;
				} else if(c1==0){
					error=true;
					return 0xFFFD;
				}
				else if(c1<0)
					return TOKEN_EOF;
				else
					return c1;
				break;
			case TokenizerState.RawText:
			case TokenizerState.ScriptData:{
				int c11=charInput.read();
				if(c11==0x3c) {
					state=(state==TokenizerState.RawText) ?
							TokenizerState.RawTextLessThan :
								TokenizerState.ScriptDataLessThan;
				} else if(c11==0){
					error=true;
					return 0xFFFD;
				}
				else if(c11<0)
					return TOKEN_EOF;
				else
					return c11;
				break;
			}
			case TokenizerState.ScriptDataLessThan:{
				charInput.setHardMark();
				int c11=charInput.read();
				if(c11==0x2f){
					tempBuffer.clearAll();
					state=TokenizerState.ScriptDataEndTagOpen;
				} else if(c11==0x21){
					state=TokenizerState.ScriptDataEscapeStart;
					tokenQueue.Add(0x21);
					return '<';
				} else {
					state=TokenizerState.ScriptData;
					if(c11>=0) {
						charInput.moveBack(1);
					}
					return 0x3c;
				}
				break;
			}
			case TokenizerState.ScriptDataEndTagOpen:
			case TokenizerState.ScriptDataEscapedEndTagOpen:{
				charInput.setHardMark();
				int ch=charInput.read();
				if(ch>='A' && ch<='Z'){
					EndTagToken token=new EndTagToken((char) (ch+0x20));
					tempBuffer.appendInt(ch);
					currentTag=token;
					currentEndTag=token;
					if(state==TokenizerState.ScriptDataEndTagOpen) {
						state=TokenizerState.ScriptDataEndTagName;
					} else {
						state=TokenizerState.ScriptDataEscapedEndTagName;
					}
				} else if(ch>='a' && ch<='z'){
					EndTagToken token=new EndTagToken((char)ch);
					tempBuffer.appendInt(ch);
					currentTag=token;
					currentEndTag=token;
					if(state==TokenizerState.ScriptDataEndTagOpen) {
						state=TokenizerState.ScriptDataEndTagName;
					} else {
						state=TokenizerState.ScriptDataEscapedEndTagName;
					}
				} else {
					if(state==TokenizerState.ScriptDataEndTagOpen) {
						state=TokenizerState.ScriptData;
					} else {
						state=TokenizerState.ScriptDataEscaped;
					}
					tokenQueue.Add(0x2f);
					if(ch>=0) {
						charInput.moveBack(1);
					}
					return 0x3c;
				}
				break;
			}
			case TokenizerState.ScriptDataEndTagName:
			case TokenizerState.ScriptDataEscapedEndTagName:{
				charInput.setHardMark();
				int ch=charInput.read();
				if((ch==0x09 || ch==0x0a || ch==0x0c || ch==0x20) &&
						isAppropriateEndTag()){
					state=TokenizerState.BeforeAttributeName;
				} else if(ch==0x2f && isAppropriateEndTag()){
					state=TokenizerState.SelfClosingStartTag;
				} else if(ch==0x3e && isAppropriateEndTag()){
					state=TokenizerState.Data;
					return emitCurrentTag();
				} else if(ch>='A' && ch<='Z'){
					currentTag.appendChar((char) (ch+0x20));
					tempBuffer.appendInt(ch);
				} else if(ch>='a' && ch<='z'){
					currentTag.appendChar((char)ch);
					tempBuffer.appendInt(ch);
				} else {
					if(state==TokenizerState.ScriptDataEndTagName) {
						state=TokenizerState.ScriptData;
					} else {
						state=TokenizerState.ScriptDataEscaped;
					}
					tokenQueue.Add(0x2f);
					int[] array=tempBuffer.array();
					for(int i=0;i<tempBuffer.Count;i++){
						tokenQueue.Add(array[i]);
					}
					if(ch>=0) {
						charInput.moveBack(1);
					}
					return '<';
				}
				break;
			}
			case TokenizerState.ScriptDataDoubleEscapeStart:{
				charInput.setHardMark();
				int ch=charInput.read();
				if(ch==0x09 || ch==0x0a || ch==0x0c || ch==0x20 ||
						ch==0x2f || ch==0x3e){
					string bufferString=tempBuffer.ToString();
					if(bufferString.Equals("script")){
						state=TokenizerState.ScriptDataDoubleEscaped;
					} else {
						state=TokenizerState.ScriptDataEscaped;
					}
					return ch;
				} else if(ch>='A' && ch<='Z'){
					tempBuffer.appendInt(ch+0x20);
					return ch;
				} else if(ch>='a' && ch<='z'){
					tempBuffer.appendInt(ch);
					return ch;
				} else {
					state=TokenizerState.ScriptDataEscaped;
					if(ch>=0) {
						charInput.moveBack(1);
					}
				}
				break;
			}
			case TokenizerState.ScriptDataDoubleEscapeEnd:{
				charInput.setHardMark();
				int ch=charInput.read();
				if(ch==0x09 || ch==0x0a || ch==0x0c || ch==0x20 ||
						ch==0x2f || ch==0x3e){
					string bufferString=tempBuffer.ToString();
					if(bufferString.Equals("script")){
						state=TokenizerState.ScriptDataEscaped;
					} else {
						state=TokenizerState.ScriptDataDoubleEscaped;
					}
					return ch;
				} else if(ch>='A' && ch<='Z'){
					tempBuffer.appendInt(ch+0x20);
					return ch;
				} else if(ch>='a' && ch<='z'){
					tempBuffer.appendInt(ch);
					return ch;
				} else {
					state=TokenizerState.ScriptDataDoubleEscaped;
					if(ch>=0) {
						charInput.moveBack(1);
					}
				}
				break;
			}
			case TokenizerState.ScriptDataEscapeStart:
			case TokenizerState.ScriptDataEscapeStartDash:{
				charInput.setHardMark();
				int ch=charInput.read();
				if(ch==0x2d){
					if(state==TokenizerState.ScriptDataEscapeStart) {
						state=TokenizerState.ScriptDataEscapeStartDash;
					} else {
						state=TokenizerState.ScriptDataEscapedDashDash;
					}
					return '-';
				} else {
					if(ch>=0) {
						charInput.moveBack(1);
					}
					state=TokenizerState.ScriptData;
				}
				break;
			}
			case TokenizerState.ScriptDataEscaped:{
				int ch=charInput.read();
				if(ch==0x2d){
					state=TokenizerState.ScriptDataEscapedDash;
					return '-';
				} else if(ch==0x3c){
					state=TokenizerState.ScriptDataEscapedLessThan;
				} else if(ch==0){
					error=true;
					return 0xFFFD;
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else
					return ch;
				break;
			}
			case TokenizerState.ScriptDataDoubleEscaped:{
				int ch=charInput.read();
				if(ch==0x2d){
					state=TokenizerState.ScriptDataDoubleEscapedDash;
					return '-';
				} else if(ch==0x3c){
					state=TokenizerState.ScriptDataDoubleEscapedLessThan;
					return '<';
				} else if(ch==0){
					error=true;
					return 0xFFFD;
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else
					return ch;
				break;
			}
			case TokenizerState.ScriptDataEscapedDash:{
				int ch=charInput.read();
				if(ch==0x2d){
					state=TokenizerState.ScriptDataEscapedDashDash;
					return '-';
				} else if(ch==0x3c){
					state=TokenizerState.ScriptDataEscapedLessThan;
				} else if(ch==0){
					error=true;
					state=TokenizerState.ScriptDataEscaped;
					return 0xFFFD;
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					state=TokenizerState.ScriptDataEscaped;
					return ch;
				}
				break;
			}
			case TokenizerState.ScriptDataDoubleEscapedDash:{
				int ch=charInput.read();
				if(ch==0x2d){
					state=TokenizerState.ScriptDataDoubleEscapedDashDash;
					return '-';
				} else if(ch==0x3c){
					state=TokenizerState.ScriptDataDoubleEscapedLessThan;
					return '<';
				} else if(ch==0){
					error=true;
					state=TokenizerState.ScriptDataDoubleEscaped;
					return 0xFFFD;
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					state=TokenizerState.ScriptDataDoubleEscaped;
					return ch;
				}
				break;
			}
			case TokenizerState.ScriptDataEscapedDashDash:{
				int ch=charInput.read();
				if(ch==0x2d)
					return '-';
				else if(ch==0x3c){
					state=TokenizerState.ScriptDataEscapedLessThan;
				} else if(ch==0x3e){
					state=TokenizerState.ScriptData;
					return '>';
				} else if(ch==0){
					error=true;
					state=TokenizerState.ScriptDataEscaped;
					return 0xFFFD;
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					state=TokenizerState.ScriptDataEscaped;
					return ch;
				}
				break;
			}
			case TokenizerState.ScriptDataDoubleEscapedDashDash:{
				int ch=charInput.read();
				if(ch==0x2d)
					return '-';
				else if(ch==0x3c){
					state=TokenizerState.ScriptDataDoubleEscapedLessThan;
					return '<';
				} else if(ch==0x3e){
					state=TokenizerState.ScriptData;
					return '>';
				} else if(ch==0){
					error=true;
					state=TokenizerState.ScriptDataDoubleEscaped;
					return 0xFFFD;
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					state=TokenizerState.ScriptDataDoubleEscaped;
					return ch;
				}
				break;
			}
			case TokenizerState.ScriptDataDoubleEscapedLessThan:{
				charInput.setHardMark();
				int ch=charInput.read();
				if(ch==0x2f){
					tempBuffer.clearAll();
					state=TokenizerState.ScriptDataDoubleEscapeEnd;
					return 0x2f;
				} else {
					state=TokenizerState.ScriptDataDoubleEscaped;
					if(ch>=0) {
						charInput.moveBack(1);
					}
				}
				break;
			}
			case TokenizerState.ScriptDataEscapedLessThan:{
				charInput.setHardMark();
				int ch=charInput.read();
				if(ch==0x2f){
					tempBuffer.clearAll();
					state=TokenizerState.ScriptDataEscapedEndTagOpen;
				} else if(ch>='A' && ch<='Z'){
					tempBuffer.clearAll();
					tempBuffer.appendInt(ch+0x20);
					state=TokenizerState.ScriptDataDoubleEscapeStart;
					tokenQueue.Add(ch);
					return 0x3c;
				} else if(ch>='a' && ch<='z'){
					tempBuffer.clearAll();
					tempBuffer.appendInt(ch);
					state=TokenizerState.ScriptDataDoubleEscapeStart;
					tokenQueue.Add(ch);
					return 0x3c;
				} else {
					state=TokenizerState.ScriptDataEscaped;
					if(ch>=0) {
						charInput.moveBack(1);
					}
					return 0x3c;
				}
				break;
			}
			case TokenizerState.PlainText:{
				int c11=charInput.read();
				if(c11==0){
					error=true;
					return 0xFFFD;
				}
				else if(c11<0)
					return TOKEN_EOF;
				else
					return c11;
			}
			case TokenizerState.TagOpen:{
				charInput.setHardMark();
				int c11=charInput.read();
				if(c11==0x21) {
					state=TokenizerState.MarkupDeclarationOpen;
				} else if(c11==0x2F) {
					state=TokenizerState.EndTagOpen;
				} else if(c11>='A' && c11<='Z'){
					TagToken token=new StartTagToken((char) (c11+0x20));
					currentTag=token;
					state=TokenizerState.TagName;
				}
				else if(c11>='a' && c11<='z'){
					TagToken token=new StartTagToken((char) (c11));
					currentTag=token;
					state=TokenizerState.TagName;
				}
				else if(c11==0x3F){
					error=true;
					bogusCommentCharacter=c11;
					state=TokenizerState.BogusComment;
				} else {
					error=true;
					state=TokenizerState.Data;
					if(c11>=0) {
						charInput.moveBack(1);
					}
					return '<';
				}
				break;
			}
			case TokenizerState.EndTagOpen:{
				int ch=charInput.read();
				if(ch>='A' && ch<='Z'){
					TagToken token=new EndTagToken((char) (ch+0x20));
					currentEndTag=token;
					currentTag=token;
					state=TokenizerState.TagName;
				}
				else if(ch>='a' && ch<='z'){
					TagToken token=new EndTagToken((char) (ch));
					currentEndTag=token;
					currentTag=token;
					state=TokenizerState.TagName;
				}
				else if(ch==0x3e){
					error=true;
					state=TokenizerState.Data;
				}
				else if(ch<0){
					error=true;
					state=TokenizerState.Data;
					tokenQueue.Add(0x2F); // solidus
					return 0x3C; // Less than
				}
				else {
					error=true;
					bogusCommentCharacter=ch;
					state=TokenizerState.BogusComment;
				}
				break;
			}
			case TokenizerState.RcDataEndTagOpen:
			case TokenizerState.RawTextEndTagOpen:{
				charInput.setHardMark();
				int ch=charInput.read();
				if(ch>='A' && ch<='Z'){
					TagToken token=new EndTagToken((char) (ch+0x20));
					tempBuffer.appendInt(ch);
					currentEndTag=token;
					currentTag=token;
					state=(state==TokenizerState.RcDataEndTagOpen) ?
							TokenizerState.RcDataEndTagName :
								TokenizerState.RawTextEndTagName;
				}
				else if(ch>='a' && ch<='z'){
					TagToken token=new EndTagToken((char) (ch));
					tempBuffer.appendInt(ch);
					currentEndTag=token;
					currentTag=token;
					state=(state==TokenizerState.RcDataEndTagOpen) ?
							TokenizerState.RcDataEndTagName :
								TokenizerState.RawTextEndTagName;
				}
				else {
					if(ch>=0) {
						charInput.moveBack(1);
					}
					state=TokenizerState.RcData;
					tokenQueue.Add(0x2F); // solidus
					return 0x3C; // Less than
				}
				break;
			}
			case TokenizerState.RcDataEndTagName:
			case TokenizerState.RawTextEndTagName:{
				charInput.setHardMark();
				int ch=charInput.read();
				if((ch==0x09 || ch==0x0a || ch==0x0c || ch==0x20) && isAppropriateEndTag()){
					state=TokenizerState.BeforeAttributeName;
				} else if(ch==0x2f && isAppropriateEndTag()){
					state=TokenizerState.SelfClosingStartTag;
				} else if(ch==0x3e && isAppropriateEndTag()){
					state=TokenizerState.Data;
					return emitCurrentTag();
				} else if(ch>='A' && ch<='Z'){
					currentTag.append(ch+0x20);
					tempBuffer.appendInt(ch+0x20);
				} else if(ch>='a' && ch<='z'){
					currentTag.append(ch);
					tempBuffer.appendInt(ch);
				} else {
					if(ch>=0) {
						charInput.moveBack(1);
					}
					state=(state==TokenizerState.RcDataEndTagName) ?
							TokenizerState.RcData :
								TokenizerState.RawText;
					tokenQueue.Add(0x2F); // solidus
					int[] array=tempBuffer.array();
					for(int i=0;i<tempBuffer.Count;i++){
						tokenQueue.Add(array[i]);
					}
					return 0x3C; // Less than
				}
				break;
			}
			case TokenizerState.BeforeAttributeName:{
				int ch=charInput.read();
				if(ch==0x09 || ch==0x0a || ch==0x0c || ch==0x20){
					// ignored
				} else if(ch==0x2f){
					state=TokenizerState.SelfClosingStartTag;
				} else if(ch==0x3e){
					state=TokenizerState.Data;
					return emitCurrentTag();
				} else if(ch>='A' && ch<='Z'){
					currentAttribute=currentTag.addAttribute((char)(ch+0x20));
					state=TokenizerState.AttributeName;
				} else if(ch==0){
					error=true;
					currentAttribute=currentTag.addAttribute((char)(0xFFFD));
					state=TokenizerState.AttributeName;
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					if(ch==0x22 || ch==0x27 || ch==0x3c || ch==0x3d){
						error=true;
					}
					currentAttribute=currentTag.addAttribute(ch);
					state=TokenizerState.AttributeName;
				}
				break;
			}
			case TokenizerState.AttributeName:{
				int ch=charInput.read();
				if(ch==0x09 || ch==0x0a || ch==0x0c || ch==0x20){
					if(!currentTag.checkAttributeName()) {
						error=true;
					}
					state=TokenizerState.AfterAttributeName;
				} else if(ch==0x2f){
					if(!currentTag.checkAttributeName()) {
						error=true;
					}
					state=TokenizerState.SelfClosingStartTag;
				} else if(ch==0x3d){
					if(!currentTag.checkAttributeName()) {
						error=true;
					}
					state=TokenizerState.BeforeAttributeValue;
				} else if(ch==0x3e){
					if(!currentTag.checkAttributeName()) {
						error=true;
					}
					state=TokenizerState.Data;
					return emitCurrentTag();

				} else if(ch>='A' && ch<='Z'){
					currentAttribute.appendToName(ch+0x20);
				} else if(ch==0){
					error=true;
					currentAttribute.appendToName(0xfffd);
				} else if(ch<0){
					error=true;
					if(!currentTag.checkAttributeName()) {
						error=true;
					}
					state=TokenizerState.Data;
				} else if(ch==0x22 || ch==0x27 || ch==0x3c){
					error=true;
					currentAttribute.appendToName(ch);
				} else {
					currentAttribute.appendToName(ch);
				}
				break;
			}
			case TokenizerState.AfterAttributeName:{
				int ch=charInput.read();
				while(ch==0x09 || ch==0x0a || ch==0x0c || ch==0x20){
					ch=charInput.read();
				}
				if(ch==0x2f){
					state=TokenizerState.SelfClosingStartTag;
				} else if(ch=='='){
					state=TokenizerState.BeforeAttributeValue;
				} else if(ch=='>'){
					state=TokenizerState.Data;
					return emitCurrentTag();

				} else if(ch>='A' && ch<='Z'){
					currentAttribute=currentTag.addAttribute((char)(ch+0x20));
					state=TokenizerState.AttributeName;
				} else if(ch==0){
					error=true;
					currentAttribute=currentTag.addAttribute((char)(0xFFFD));
					state=TokenizerState.AttributeName;
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					if(ch==0x22 || ch==0x27 || ch==0x3c){
						error=true;
					}
					currentAttribute=currentTag.addAttribute(ch);
					state=TokenizerState.AttributeName;
				}
				break;
			}
			case TokenizerState.BeforeAttributeValue:{
				charInput.setHardMark();
				int ch=charInput.read();
				while(ch==0x09 || ch==0x0a || ch==0x0c || ch==0x20){
					ch=charInput.read();
				}
				if(ch==0x22){
					state=TokenizerState.AttributeValueDoubleQuoted;
				} else if(ch==0x26){
					charInput.moveBack(1);
					state=TokenizerState.AttributeValueUnquoted;
				} else if(ch==0x27){
					state=TokenizerState.AttributeValueSingleQuoted;
				} else if(ch==0){
					error=true;
					currentAttribute.appendToValue(0xFFFD);
					state=TokenizerState.AttributeValueUnquoted;
				} else if(ch==0x3e){
					error=true;
					state=TokenizerState.Data;
					return emitCurrentTag();
				} else if(ch==0x3c || ch==0x3d || ch==0x60){
					error=true;
					currentAttribute.appendToValue(ch);
					state=TokenizerState.AttributeValueUnquoted;
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					currentAttribute.appendToValue(ch);
					state=TokenizerState.AttributeValueUnquoted;
				}
				break;
			}
			case TokenizerState.AttributeValueDoubleQuoted:{
				int ch=charInput.read();
				if(ch==0x22){
					currentAttribute.commitValue();
					state=TokenizerState.AfterAttributeValueQuoted;
				} else if(ch==0x26){
					lastState=state;
					state=TokenizerState.CharacterRefInAttributeValue;
				} else if(ch==0){
					error=true;
					currentAttribute.appendToValue(0xfffd);
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					currentAttribute.appendToValue(ch);
					// Keep reading characters to
					// reduce the need to re-call
					// this method
					int mark=charInput.setSoftMark();
					for(int i=0;i<100;i++){
						ch=charInput.read();
						if(ch>0 && ch!=0x26 && ch!=0x22){
							currentAttribute.appendToValue(ch);
						} else if(ch==0x22){
							currentAttribute.commitValue();
							state=TokenizerState.AfterAttributeValueQuoted;
							break;
						} else {
							charInput.setMarkPosition(mark+i);
							break;
						}
					}
				}
				break;
			}
			case TokenizerState.AttributeValueSingleQuoted:{
				int ch=charInput.read();
				if(ch==0x27){
					currentAttribute.commitValue();
					state=TokenizerState.AfterAttributeValueQuoted;
				} else if(ch==0x26){
					lastState=state;
					state=TokenizerState.CharacterRefInAttributeValue;
				} else if(ch==0){
					error=true;
					currentAttribute.appendToValue(0xfffd);
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					currentAttribute.appendToValue(ch);
					// Keep reading characters to
					// reduce the need to re-call
					// this method
					int mark=charInput.setSoftMark();
					for(int i=0;i<100;i++){
						ch=charInput.read();
						if(ch>0 && ch!=0x26 && ch!=0x27){
							currentAttribute.appendToValue(ch);
						} else if(ch==0x27){
							currentAttribute.commitValue();
							state=TokenizerState.AfterAttributeValueQuoted;
							break;
						} else {
							charInput.setMarkPosition(mark+i);
							break;
						}
					}
				}
				break;
			}
			case TokenizerState.AttributeValueUnquoted:{
				int ch=charInput.read();
				if(ch==0x09 || ch==0x0a || ch==0x0c || ch==0x20){
					currentAttribute.commitValue();
					state=TokenizerState.BeforeAttributeName;
				} else if(ch==0x26){
					lastState=state;
					state=TokenizerState.CharacterRefInAttributeValue;
				} else if(ch==0x3e){
					currentAttribute.commitValue();
					state=TokenizerState.Data;
					return emitCurrentTag();

				} else if(ch==0){
					error=true;
					currentAttribute.appendToValue(0xfffd);
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					if(ch==0x22||ch==0x27||ch==0x3c||ch==0x3d||ch==0x60){
						error=true;
					}
					currentAttribute.appendToValue(ch);
				}
				break;
			}
			case TokenizerState.AfterAttributeValueQuoted:{
				int mark=charInput.setSoftMark();
				int ch=charInput.read();
				if(ch==0x09 || ch==0x0a || ch==0x0c || ch==0x20){
					state=TokenizerState.BeforeAttributeName;
				} else if(ch==0x2f){
					state=TokenizerState.SelfClosingStartTag;
				} else if(ch==0x3e){
					state=TokenizerState.Data;
					return emitCurrentTag();

				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					error=true;
					state=TokenizerState.BeforeAttributeName;
					charInput.setMarkPosition(mark);
				}
				break;
			}
			case TokenizerState.SelfClosingStartTag:{
				int mark=charInput.setSoftMark();
				int ch=charInput.read();
				if(ch==0x3e){
					currentTag.setSelfClosing(true);
					state=TokenizerState.Data;
					return emitCurrentTag();
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					error=true;
					state=TokenizerState.BeforeAttributeName;
					charInput.setMarkPosition(mark);
				}
				break;
			}
			case TokenizerState.MarkupDeclarationOpen:{
				int mark=charInput.setSoftMark();
				int ch=charInput.read();
				if(ch=='-' && charInput.read()=='-'){
					CommentToken token=new CommentToken();
					lastComment=token;
					state=TokenizerState.CommentStart;
					break;
				} else if(ch=='D' || ch=='d'){
					if(((ch=charInput.read())=='o' || ch=='O') &&
							((ch=charInput.read())=='c' || ch=='C') &&
							((ch=charInput.read())=='t' || ch=='T') &&
							((ch=charInput.read())=='y' || ch=='Y') &&
							((ch=charInput.read())=='p' || ch=='P') &&
							((ch=charInput.read())=='e' || ch=='E')){
						state=TokenizerState.DocType;
						break;
					}
				} else if(ch=='[' && true){
					if(charInput.read()=='C' &&
							charInput.read()=='D' &&
							charInput.read()=='A' &&
							charInput.read()=='T' &&
							charInput.read()=='A' &&
							charInput.read()=='[' &&
							getCurrentNode()!=null &&
							!HTML_NAMESPACE.Equals(getCurrentNode().getNamespaceURI())
							){
						state=TokenizerState.CData;
						break;
					}
				}
				error=true;
				charInput.setMarkPosition(mark);
				bogusCommentCharacter=-1;
				state=TokenizerState.BogusComment;
				break;
			}
			case TokenizerState.CommentStart:{
				int ch=charInput.read();
				if(ch=='-'){
					state=TokenizerState.CommentStartDash;
				} else if(ch==0){
					error=true;
					lastComment.appendChar(0xFFFD);
					state=TokenizerState.Comment;
				} else if(ch==0x3e || ch<0){
					error=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|lastComment.getType();
					tokens.Add(lastComment);
					return ret;
				} else {
					lastComment.appendChar(ch);
					state=TokenizerState.Comment;
				}
				break;
			}
			case TokenizerState.CommentStartDash:{
				int ch=charInput.read();
				if(ch=='-'){
					state=TokenizerState.CommentEnd;
				} else if(ch==0){
					error=true;
					lastComment.appendChar('-');
					lastComment.appendChar(0xFFFD);
					state=TokenizerState.Comment;
				} else if(ch==0x3e || ch<0){
					error=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|lastComment.getType();
					tokens.Add(lastComment);
					return ret;
				} else {
					lastComment.appendChar('-');
					lastComment.appendChar(ch);
					state=TokenizerState.Comment;
				}
				break;
			}
			case TokenizerState.Comment:{
				int ch=charInput.read();
				if(ch=='-'){
					state=TokenizerState.CommentEndDash;
				} else if(ch==0){
					error=true;
					lastComment.appendChar(0xFFFD);
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|lastComment.getType();
					tokens.Add(lastComment);
					return ret;
				} else {
					lastComment.appendChar(ch);
				}
				break;
			}
			case TokenizerState.CommentEndDash:{
				int ch=charInput.read();
				if(ch=='-'){
					state=TokenizerState.CommentEnd;
				} else if(ch==0){
					error=true;
					lastComment.appendChar('-');
					lastComment.appendChar(0xFFFD);
					state=TokenizerState.Comment;
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|lastComment.getType();
					tokens.Add(lastComment);
					return ret;
				} else {
					lastComment.appendChar('-');
					lastComment.appendChar(ch);
					state=TokenizerState.Comment;
				}
				break;
			}
			case TokenizerState.CommentEnd:{
				int ch=charInput.read();
				if(ch==0x3e){
					state=TokenizerState.Data;
					int ret=tokens.Count|lastComment.getType();
					tokens.Add(lastComment);
					return ret;
				} else if(ch==0){
					error=true;
					lastComment.appendChar('-');
					lastComment.appendChar('-');
					lastComment.appendChar(0xFFFD);
					state=TokenizerState.Comment;
				} else if(ch==0x21){ // --!>
					error=true;
					state=TokenizerState.CommentEndBang;
				} else if(ch==0x2D){
					error=true;
					lastComment.appendChar('-');
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|lastComment.getType();
					tokens.Add(lastComment);
					return ret;
				} else {
					error=true;
					lastComment.appendChar('-');
					lastComment.appendChar('-');
					lastComment.appendChar(ch);
					state=TokenizerState.Comment;
				}
				break;
			}
			case TokenizerState.CommentEndBang:{
				int ch=charInput.read();
				if(ch==0x3e){
					state=TokenizerState.Data;
					int ret=tokens.Count|lastComment.getType();
					tokens.Add(lastComment);
					return ret;
				} else if(ch==0){
					error=true;
					lastComment.appendChar('-');
					lastComment.appendChar('-');
					lastComment.appendChar('!');
					lastComment.appendChar(0xFFFD);
					state=TokenizerState.Comment;
				} else if(ch==0x2D){
					lastComment.appendChar('-');
					lastComment.appendChar('-');
					lastComment.appendChar('!');
					state=TokenizerState.CommentEndDash;
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|lastComment.getType();
					tokens.Add(lastComment);
					return ret;
				} else {
					error=true;
					lastComment.appendChar('-');
					lastComment.appendChar('-');
					lastComment.appendChar('!');
					lastComment.appendChar(ch);
					state=TokenizerState.Comment;
				}
				break;
			}
			case TokenizerState.CharacterRefInAttributeValue:{
				int allowed=0x3E;
				if(lastState==TokenizerState.AttributeValueDoubleQuoted) {
					allowed='"';
				}
				if(lastState==TokenizerState.AttributeValueSingleQuoted) {
					allowed='\'';
				}
				int ch=parseCharacterReference(allowed);
				if(ch<0){
					// more than one character in this reference
					int index=Math.Abs(ch+1);
					currentAttribute.appendToValue(entityDoubles[index*2]);
					currentAttribute.appendToValue(entityDoubles[index*2+1]);
				} else {
					currentAttribute.appendToValue(ch);
				}
				state=lastState;
				break;
			}
			case TokenizerState.TagName:{
				int ch=charInput.read();
				if(ch==0x09 || ch==0x0a || ch==0x0c || ch==0x20){
					state=TokenizerState.BeforeAttributeName;
				} else if(ch==0x2f){
					state=TokenizerState.SelfClosingStartTag;
				} else if(ch==0x3e){
					state=TokenizerState.Data;
					return emitCurrentTag();

				} else if(ch>='A' && ch<='Z'){
					currentTag.appendChar((char)(ch+0x20));
				} else if(ch==0){
					error=true;
					currentTag.appendChar((char)0xFFFD);
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
				} else {
					currentTag.append(ch);
				}
				break;
			}
			case TokenizerState.RawTextLessThan:{
				charInput.setHardMark();
				int ch=charInput.read();
				if(ch==0x2f){
					tempBuffer.clearAll();
					state=TokenizerState.RawTextEndTagOpen;
				} else {
					state=TokenizerState.RawText;
					if(ch>=0) {
						charInput.moveBack(1);
					}
					return 0x3c;
				}
				break;
			}
			case TokenizerState.BogusComment:{
				CommentToken comment=new CommentToken();
				if(bogusCommentCharacter>=0) {
					comment.appendChar(bogusCommentCharacter==0 ? 0xFFFD : bogusCommentCharacter);
				}
				while(true){
					int ch=charInput.read();
					if(ch<0 || ch=='>') {
						break;
					}
					if(ch==0) {
						ch=0xFFFD;
					}
					comment.appendChar(ch);
				}
				int ret=tokens.Count|comment.getType();
				tokens.Add(comment);
				state=TokenizerState.Data;
				return ret;
			}
			case TokenizerState.DocType:{
				charInput.setHardMark();
				int ch=charInput.read();
				if(ch==0x09||ch==0x0a||ch==0x0c||ch==0x20){
					state=TokenizerState.BeforeDocTypeName;
				} else if(ch<0){
					error=true;
					state=TokenizerState.Data;
					DocTypeToken token=new DocTypeToken();
					token.forceQuirks=true;
					int ret=tokens.Count|token.getType();
					tokens.Add(token);
					return ret;
				} else {
					error=true;
					charInput.moveBack(1);
					state=TokenizerState.BeforeDocTypeName;
				}
				break;
			}
			case TokenizerState.BeforeDocTypeName:{
				int ch=charInput.read();
				if(ch==0x09||ch==0x0a||ch==0x0c||ch==0x20){
					break;
				} else if(ch>='A' && ch<='Z'){
					docTypeToken=new DocTypeToken();
					docTypeToken.name=new IntList();
					docTypeToken.name.appendInt(ch+0x20);
					state=TokenizerState.DocTypeName;
				} else if(ch==0){
					error=true;
					docTypeToken=new DocTypeToken();
					docTypeToken.name=new IntList();
					docTypeToken.name.appendInt(0xFFFD);
					state=TokenizerState.DocTypeName;
				} else if(ch==0x3e || ch<0){
					error=true;
					state=TokenizerState.Data;
					DocTypeToken token=new DocTypeToken();
					token.forceQuirks=true;
					int ret=tokens.Count|token.getType();
					tokens.Add(token);
					return ret;
				} else {
					docTypeToken=new DocTypeToken();
					docTypeToken.name=new IntList();
					docTypeToken.name.appendInt(ch);
					state=TokenizerState.DocTypeName;
				}
				break;
			}
			case TokenizerState.DocTypeName:{
				int ch=charInput.read();
				if(ch==0x09||ch==0x0a||ch==0x0c||ch==0x20){
					state=TokenizerState.AfterDocTypeName;
				} else if(ch==0x3e){
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				} else if(ch>='A' && ch<='Z'){
					docTypeToken.name.appendInt(ch+0x20);
				} else if(ch==0){
					error=true;
					docTypeToken.name.appendInt(0xfffd);
				} else if(ch<0){
					error=true;
					docTypeToken.forceQuirks=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				} else {
					docTypeToken.name.appendInt(ch);
				}
				break;
			}
			case TokenizerState.AfterDocTypeName:{
				int ch=charInput.read();
				if(ch==0x09||ch==0x0a||ch==0x0c||ch==0x20){
					break;
				} else if(ch==0x3e){
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				} else if(ch<0){
					error=true;
					docTypeToken.forceQuirks=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				} else {
					int ch2=0;
					int pos=charInput.setSoftMark();
					if(ch=='P' || ch=='p'){
						if(((ch2=charInput.read())=='u' || ch2=='U') &&
								((ch2=charInput.read())=='b' || ch2=='B') &&
								((ch2=charInput.read())=='l' || ch2=='L') &&
								((ch2=charInput.read())=='i' || ch2=='I') &&
								((ch2=charInput.read())=='c' || ch2=='C')
								){
							state=TokenizerState.AfterDocTypePublic;
						} else {
							error=true;
							charInput.setMarkPosition(pos);
							docTypeToken.forceQuirks=true;
							state=TokenizerState.BogusDocType;
						}
					} else if(ch=='S' || ch=='s'){
						if(((ch2=charInput.read())=='y' || ch2=='Y') &&
								((ch2=charInput.read())=='s' || ch2=='S') &&
								((ch2=charInput.read())=='t' || ch2=='T') &&
								((ch2=charInput.read())=='e' || ch2=='E') &&
								((ch2=charInput.read())=='m' || ch2=='M')
								){
							state=TokenizerState.AfterDocTypeSystem;
						} else {
							error=true;
							charInput.setMarkPosition(pos);
							docTypeToken.forceQuirks=true;
							state=TokenizerState.BogusDocType;
						}
					} else {
						error=true;
						charInput.setMarkPosition(pos);
						docTypeToken.forceQuirks=true;
						state=TokenizerState.BogusDocType;
					}
				}
				break;
			}
			case TokenizerState.AfterDocTypePublic:
			case TokenizerState.BeforeDocTypePublicID:{
				int ch=charInput.read();
				if(ch==0x09||ch==0x0a||ch==0x0c||ch==0x20){
					if(state==TokenizerState.AfterDocTypePublic) {
						state=TokenizerState.BeforeDocTypePublicID;
					}
				} else if(ch==0x22){
					docTypeToken.publicID=new IntList();
					if(state==TokenizerState.AfterDocTypePublic) {
						error=true;
					}
					state=TokenizerState.DocTypePublicIDDoubleQuoted;
				} else if(ch==0x27){
					docTypeToken.publicID=new IntList();
					if(state==TokenizerState.AfterDocTypePublic) {
						error=true;
					}
					state=TokenizerState.DocTypePublicIDSingleQuoted;
				} else if(ch==0x3e || ch<0){
					error=true;
					docTypeToken.forceQuirks=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				} else {
					error=true;
					docTypeToken.forceQuirks=true;
					state=TokenizerState.BogusDocType;
				}
				break;
			}
			case TokenizerState.AfterDocTypeSystem:
			case TokenizerState.BeforeDocTypeSystemID:{
				int ch=charInput.read();
				if(ch==0x09||ch==0x0a||ch==0x0c||ch==0x20){
					if(state==TokenizerState.AfterDocTypeSystem) {
						state=TokenizerState.BeforeDocTypeSystemID;
					}
				} else if(ch==0x22){
					docTypeToken.systemID=new IntList();
					if(state==TokenizerState.AfterDocTypeSystem) {
						error=true;
					}
					state=TokenizerState.DocTypeSystemIDDoubleQuoted;
				} else if(ch==0x27){
					docTypeToken.systemID=new IntList();
					if(state==TokenizerState.AfterDocTypeSystem) {
						error=true;
					}
					state=TokenizerState.DocTypeSystemIDSingleQuoted;
				} else if(ch==0x3e || ch<0){
					error=true;
					docTypeToken.forceQuirks=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				} else {
					error=true;
					docTypeToken.forceQuirks=true;
					state=TokenizerState.BogusDocType;
				}
				break;
			}
			case TokenizerState.DocTypePublicIDDoubleQuoted:
			case TokenizerState.DocTypePublicIDSingleQuoted:{
				int ch=charInput.read();
				if(ch==(state==TokenizerState.DocTypePublicIDDoubleQuoted ? 0x22 : 0x27)){
					state=TokenizerState.AfterDocTypePublicID;
				} else if(ch==0){
					error=true;
					docTypeToken.publicID.appendInt(0xFFFD);
				} else if(ch==0x3e || ch<0){
					error=true;
					docTypeToken.forceQuirks=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				} else {
					docTypeToken.publicID.appendInt(ch);
				}
				break;
			}
			case TokenizerState.DocTypeSystemIDDoubleQuoted:
			case TokenizerState.DocTypeSystemIDSingleQuoted:{
				int ch=charInput.read();
				if(ch==(state==TokenizerState.DocTypeSystemIDDoubleQuoted ? 0x22 : 0x27)){
					state=TokenizerState.AfterDocTypeSystemID;
				} else if(ch==0){
					error=true;
					docTypeToken.systemID.appendInt(0xFFFD);
				} else if(ch==0x3e || ch<0){
					error=true;
					docTypeToken.forceQuirks=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				} else {
					docTypeToken.systemID.appendInt(ch);
				}
				break;
			}
			case TokenizerState.AfterDocTypePublicID:
			case TokenizerState.BetweenDocTypePublicAndSystem:{
				int ch=charInput.read();
				if(ch==0x09||ch==0x0a||ch==0x0c||ch==0x20){
					if(state==TokenizerState.AfterDocTypePublicID) {
						state=TokenizerState.BetweenDocTypePublicAndSystem;
					}
				} else if(ch==0x3e){
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				} else if(ch==0x22){
					docTypeToken.systemID=new IntList();
					if(state==TokenizerState.AfterDocTypePublicID) {
						error=true;
					}
					state=TokenizerState.DocTypeSystemIDDoubleQuoted;
				} else if(ch==0x27){
					docTypeToken.systemID=new IntList();
					if(state==TokenizerState.AfterDocTypePublicID) {
						error=true;
					}
					state=TokenizerState.DocTypeSystemIDSingleQuoted;
				} else if(ch<0){
					error=true;
					docTypeToken.forceQuirks=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				} else {
					error=true;
					docTypeToken.forceQuirks=true;
					state=TokenizerState.BogusDocType;
				}
				break;
			}
			case TokenizerState.AfterDocTypeSystemID:{
				int ch=charInput.read();
				if(ch==0x09||ch==0x0a||ch==0x0c||ch==0x20){
					break;
				} else if(ch==0x3e){
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				} else if(ch<0){
					error=true;
					docTypeToken.forceQuirks=true;
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				} else {
					error=true;
					state=TokenizerState.BogusDocType;
				}
				break;
			}
			case TokenizerState.BogusDocType:{
				int ch=charInput.read();
				if(ch==0x3e || ch<0){
					state=TokenizerState.Data;
					int ret=tokens.Count|docTypeToken.getType();
					tokens.Add(docTypeToken);
					return ret;
				}
				break;
			}
			case TokenizerState.CData:{
				IntList buffer=new IntList();
				int phase=0;
				state=TokenizerState.Data;
				while(true){
					int ch=charInput.read();
					if(ch<0) {
						break;
					}
					buffer.appendInt(ch);
					if(phase==0){
						if(ch==']') {
							phase++;
						} else {
							phase=0;
						}
					} else if(phase==1) {
						if(ch==']'){
							phase++;
						} else {
							phase=0;
						}
					} else if(phase==2) {
						if(ch=='>'){
							phase++;
							break;
						} else if(ch==']'){
							phase=2;
						} else {
							phase=0;
						}
					}
				}
				int[] arr=buffer.array();
				int size=buffer.Count;
				if(phase==3)
				{
					size-=3; // don't count the ']]>'
				}
				if(size>0){
					// Emit the tokens
					int ret1=arr[0];
					for(int i=1;i<size;i++){
						tokenQueue.Add(arr[i]);
					}
					return ret1;
				}
				break;
			}
			case TokenizerState.RcDataLessThan:{
				charInput.setHardMark();
				int ch=charInput.read();
				if(ch==0x2f){
					tempBuffer.clearAll();
					state=TokenizerState.RcDataEndTagOpen;
				} else {
					state=TokenizerState.RcData;
					if(ch>=0) {
						charInput.moveBack(1);
					}
					return 0x3c;
				}
				break;
			}
			default:
				throw new InvalidOperationException();
			}
		}
	}

	private int emitCurrentTag() {
		int ret=tokens.Count|currentTag.getType();
		tokens.Add(currentTag);
		if(currentTag.getType()==TOKEN_START_TAG) {
			lastStartTag=currentTag;
		} else {
			if(currentTag.getAttributes().Count>0 ||
					currentTag.isSelfClosing()){
				error=true;
			}
		}
		currentTag=null;
		return ret;
	}

	private Element popCurrentNode(){
		if(openElements.Count>0)
			return removeAtIndex(openElements,openElements.Count-1);
		return null;
	}

	private void pushFormattingElement(StartTagToken tag) {
		Element element=addHtmlElement(tag);
		int matchingElements=0;
		int lastMatchingElement=-1;
		string name=element.getLocalName();
		for(int i=formattingElements.Count-1;i>=0;i--){
			FormattingElement fe=formattingElements[i];
			if(fe.isMarker()) {
				break;
			}
			if(fe.element.getLocalName().Equals(name) &&
					fe.element.getNamespaceURI().Equals(element.getNamespaceURI())){
				IList<Attrib> attribs=fe.element.getAttributes();
				IList<Attrib> myAttribs=element.getAttributes();
				if(attribs.Count==myAttribs.Count){
					bool match=true;
					for(int j=0;j<myAttribs.Count;j++){
						string name1=myAttribs[j].getName();
						string _namespace=myAttribs[j].getNamespace();
						string value=myAttribs[j].getValue();
						string otherValue=fe.element.getAttributeNS(_namespace,name1);
						if(otherValue==null || !otherValue.Equals(value)){
							match=false;
						}
					}
					if(match){
						matchingElements++;
						lastMatchingElement=i;
					}
				}
			}
		}
		if(matchingElements>=3){
			formattingElements.RemoveAt(lastMatchingElement);
		}
		FormattingElement fe2=new FormattingElement();
		fe2.marker=false;
		fe2.token=tag;
		fe2.element=element;
		formattingElements.Add(fe2);
	}

	private void reconstructFormatting(){
		if(formattingElements.Count==0)return;
		//Console.WriteLine("reconstructing elements");
		//Console.WriteLine(formattingElements);
		FormattingElement fe=formattingElements[formattingElements.Count-1];
		if(fe.isMarker() || openElements.Contains(fe.element))
			return;
		int i=formattingElements.Count-1;
		while(i>0){
			fe=formattingElements[i-1];
			i--;
			if(!fe.isMarker() && !openElements.Contains(fe.element)){
				continue;
			}
			i++;
			break;
		}
		for(int j=i;j<formattingElements.Count;j++){
			fe=formattingElements[j];
			Element element=addHtmlElement(fe.token);
			fe.element=element;
			fe.marker=false;
		}
	}

	private void removeFormattingElement(Element aElement) {
		FormattingElement f=null;
		foreach(FormattingElement fe in formattingElements){
			if(!fe.isMarker() && aElement.Equals(fe.element)){
				f=fe;
				break;
			}
		}
		if(f!=null) {
			formattingElements.Remove(f);
		}
	}

	private void resetInsertionMode(){
		bool last=false;
		for(int i=openElements.Count-1;i>=0;i--){
			Element e=openElements[i];
			if(context!=null && i==0){
				e=context;
				last=true;
			}
			string name=e.getLocalName();
			if(!last && (name.Equals("th") || name.Equals("td"))){
				insertionMode=InsertionMode.InCell;
				break;
			}
			if((name.Equals("select"))){
				insertionMode=InsertionMode.InSelect;
				break;
			}
			if((name.Equals("colgroup"))){
				insertionMode=InsertionMode.InColumnGroup;
				break;
			}
			if((name.Equals("tr"))){
				insertionMode=InsertionMode.InRow;
				break;
			}
			if((name.Equals("caption"))){
				insertionMode=InsertionMode.InCaption;
				break;
			}
			if((name.Equals("table"))){
				insertionMode=InsertionMode.InTable;
				break;
			}
			if((name.Equals("frameset"))){
				insertionMode=InsertionMode.InFrameset;
				break;
			}
			if((name.Equals("html"))){
				insertionMode=InsertionMode.BeforeHead;
				break;
			}
			if((name.Equals("head") || name.Equals("body"))){
				insertionMode=InsertionMode.InBody;
				break;
			}
			if((name.Equals("thead")||name.Equals("tbody")||name.Equals("tfoot"))){
				insertionMode=InsertionMode.InTableBody;
				break;
			}
			if(last){
				insertionMode=InsertionMode.InBody;
				break;
			}
		}
	}

	internal void setRcData(){
		state=TokenizerState.RcData;
	}
	internal void setPlainText(){
		state=TokenizerState.PlainText;
	}
	internal void setRawText(){
		state=TokenizerState.RawText;
	}
	internal void setCData(){
		state=TokenizerState.CData;
	}

	public static string resolveURL(INode node, string url, string _base){
		string encoding=(node is IDocument) ?
				((IDocument)node).getCharacterSet() :
					node.getOwnerDocument().getCharacterSet();
				if("utf-16be".Equals(encoding) ||
						"utf-16le".Equals(encoding)){
					encoding="utf-8";
				}
				if(_base==null){
					_base=node.getBaseURI();
				}
				URL resolved=URL.parse(url,URL.parse(_base),encoding,true);
				if(resolved==null)
					return _base;
				return resolved.ToString();
	}

	private void stopParsing() {
		done=true;
		document.encoding=encoding.getEncoding();
		if(document.baseurl==null || document.baseurl.Length==0){
			document.baseurl=baseurl;
		} else {
			if(baseurl!=null && baseurl.Length>0){
				document.baseurl=resolveURL(document,baseurl,document.baseurl);
			}
		}
		openElements.Clear();
		formattingElements.Clear();
	}

	public bool isError() {
		return error;
	}

	////////////////////////////////////////////////////

	internal string nodesToDebugString(IList<Node> nodes){
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		foreach(Node node in nodes){
			string str=node.toDebugString();
			string[] strarray=StringUtility.splitAt(str,"\n");
			foreach(string el in strarray){
				builder.Append("| ");
				builder.Append(el.Replace("~~~~","\n"));
				builder.Append("\n");
			}
		}
		return builder.ToString();
	}

	public IList<Node> parseFragment(string contextName) {
		Element element=new Element();
		element.setName(contextName);
		element.setNamespace(HTML_NAMESPACE);
		return parseFragment(element);
	}

	public IList<Node> parseFragment(Element context) {
		if(context==null)
			throw new ArgumentException();
		initialize();
		document=new Document();
		INode ownerDocument=context;
		INode lastForm=null;
		while(ownerDocument!=null){
			if(lastForm==null && ownerDocument.getNodeType()==NodeType.ELEMENT_NODE){
				string name=((Element)ownerDocument).getLocalName();
				if(name.Equals("form")){
					lastForm=ownerDocument;
				}
			}
			ownerDocument=ownerDocument.getParentNode();
			if(ownerDocument==null ||
					ownerDocument.getNodeType()==NodeType.DOCUMENT_NODE){
				break;
			}
		}
		Document ownerDoc=null;
		if(ownerDocument!=null && ownerDocument.getNodeType()==NodeType.DOCUMENT_NODE){
			ownerDoc=(Document)ownerDocument;
			document.setMode(ownerDoc.getMode());
		}
		string name2=context.getLocalName();
		state=TokenizerState.Data;
		if(name2.Equals("title")||name2.Equals("textarea")){
			state=TokenizerState.RcData;
		} else if(name2.Equals("style") || name2.Equals("xmp") ||
				name2.Equals("iframe") || name2.Equals("noembed") ||
				name2.Equals("noframes")){
			state=TokenizerState.RawText;
		} else if(name2.Equals("script")){
			state=TokenizerState.ScriptData;
		} else if(name2.Equals("noscript")){
			state=TokenizerState.Data;
		} else if(name2.Equals("plaintext")){
			state=TokenizerState.PlainText;
		}
		Element element=new Element();
		element.setName("html");
		element.setNamespace(HTML_NAMESPACE);
		document.appendChild(element);
		done=false;
		openElements.Clear();
		openElements.Add(element);
		this.context=context;
		resetInsertionMode();
		formElement=(lastForm==null) ? null : ((Element)lastForm);
		if(encoding.getConfidence()!=EncodingConfidence.Irrelevant){
			encoding=new EncodingConfidence(encoding.getEncoding(),
					EncodingConfidence.Irrelevant);
		}
		parse();
		return new List<Node>(element.getChildNodesInternal());
	}


}

}
