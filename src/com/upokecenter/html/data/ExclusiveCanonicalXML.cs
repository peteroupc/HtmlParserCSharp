namespace com.upokecenter.html.data {
using System;
using System.Text;
using System.Collections.Generic;
using com.upokecenter.html;
using com.upokecenter.util;


/**
 * 
 * Implements Exclusive XML Canonicalization
 * as specified at:
 * http://www.w3.org/TR/xml-exc-c14n/
 * 
 * @author Peter
 *
 */
sealed class ExclusiveCanonicalXML {
	private ExclusiveCanonicalXML(){}
	private sealed class NamespaceAttrComparer : IComparer<IAttr> {
		public int Compare(IAttr arg0, IAttr arg1) {
			return StringUtility.codePointCompare(arg0.getName(),arg1.getName());
		}
	}

	private sealed class AttrComparer : IComparer<IAttr> {
		public int Compare(IAttr arg0, IAttr arg1) {
			string namespace1=string.IsNullOrEmpty(arg0.getPrefix()) ?
					"" : arg0.getNamespaceURI();
			string namespace2=string.IsNullOrEmpty(arg1.getPrefix()) ?
					"" : arg1.getNamespaceURI();
			// compare _namespace URIs (attributes without a prefix
			// are considered to have no _namespace URI)
			int cmp=StringUtility.codePointCompare(namespace1,namespace2);
			if(cmp==0){
				// then compare their local names
				cmp=StringUtility.codePointCompare(arg0.getLocalName(),
						arg1.getLocalName());
			}
			return cmp;
		}
	}

	private static readonly IComparer<IAttr> attrComparer=new AttrComparer();
	private static readonly IComparer<IAttr> attrNamespaceComparer=new NamespaceAttrComparer();

	public static string canonicalize(
			INode node,
			bool includeRoot,
			IDictionary<string,string> prefixList
			){
		return canonicalize(node,includeRoot,prefixList,false);
	}
	public static string canonicalize(
			INode node,
			bool includeRoot,
			IDictionary<string,string> prefixList,
			bool withComments
			){
		if((node)==null)throw new ArgumentNullException("node");
		StringBuilder builder=new StringBuilder();
		IList<IDictionary<string,string>> stack=new List<IDictionary<string,string>>();
		if(prefixList==null) {
			prefixList=new PeterO.Support.LenientDictionary<string,string>();
		} else {
      foreach(string prefix in prefixList.Keys){
        string nsvalue=prefixList[prefix];
        if(!string.IsNullOrEmpty(nsvalue)){
					if(!URIUtility.hasSchemeForURI(nsvalue))
						throw new ArgumentException(nsvalue+" is not a valid _namespace URI.");
				}
      }
    }
    PeterO.Support.LenientDictionary<string,string> item=new PeterO.Support.LenientDictionary<string,string>(); 
		stack.Add(item);
		if(node is IDocument){
			bool beforeElement=true;
			foreach(INode child in node.getChildNodes()){
				if(child is IElement){
					beforeElement=false;
					canonicalize(child,builder,stack,prefixList,true,withComments);
				} else if(withComments || child.getNodeType()!=NodeType.COMMENT_NODE){
					canonicalizeOutsideElement(child,builder,beforeElement);
				}
			}
		} else if(includeRoot){
			canonicalize(node,builder,stack,prefixList,true,withComments);
		} else {
			foreach(INode child in node.getChildNodes()){
				canonicalize(child,builder,stack,prefixList,true,withComments);
			}
		}
		return builder.ToString();
	}

	private static bool isVisiblyUtilized(IElement element, string s){
		string prefix=element.getPrefix();
		if(prefix==null) {
			prefix="";
		}
		if(s.Equals(prefix))return true;
		if(s.Length>0){
			foreach(IAttr attr in element.getAttributes()){
				prefix=attr.getPrefix();
				if(prefix==null) {
					continue;
				}
				if(s.Equals(prefix))return true;
			}
		}
		return false;
	}

	private static void renderAttribute(StringBuilder builder,
			string prefix, string name, string value){
		builder.Append(' ');
		if(!string.IsNullOrEmpty(prefix)){
			builder.Append(prefix);
			builder.Append(":");
		}
		builder.Append(name);
		builder.Append("=\"");
		for(int i=0;i<value.Length;i++){
			char c=value[i];
			if(c==0x0d) {
				builder.Append("&#xD;");
			} else if(c==0x09) {
				builder.Append("&#x9;");
			} else if(c==0x0a) {
				builder.Append("&#xA;");
			} else if(c=='"') {
				builder.Append("&quot;");
			} else if(c=='<') {
				builder.Append("&lt;");
			} else if(c=='&') {
				builder.Append("&amp;");
			} else {
				builder.Append(c);
			}
		}
		builder.Append('"');
	}

	private static void canonicalizeOutsideElement(
			INode node, StringBuilder builder, bool beforeDocument){
		int nodeType=node.getNodeType();
		if(nodeType==NodeType.COMMENT_NODE){
			if(!beforeDocument) {
				builder.Append('\n');
			}
			builder.Append("<!--");
			builder.Append(((IComment)node).getData());
			builder.Append("-->");
			if(beforeDocument) {
				builder.Append('\n');
			}
		} else if(nodeType==NodeType.PROCESSING_INSTRUCTION_NODE){
			if(!beforeDocument) {
				builder.Append('\n');
			}
			builder.Append("<?");
			builder.Append(((IProcessingInstruction)node).getTarget());
			string data=((IProcessingInstruction)node).getData();
			if(data.Length>0){
				builder.Append(' ');
				builder.Append(data);
			}
			builder.Append("?>");
			if(beforeDocument) {
				builder.Append('\n');
			}
		}
	}

	private sealed class NamespaceAttr : IAttr {
		string prefix;
		string localName;
		string value;
		string name;
		public NamespaceAttr(string prefix, string value){
			if(prefix.Length==0){
				this.prefix="";
				this.localName="xmlns";
				this.value=value;
				this.name="xmlns";
			} else {
				this.prefix="xmlns";
				this.localName=prefix;
				this.name="xmlns:"+value;
				this.value=value;
			}
		}
		public string getPrefix() {
			return prefix;
		}
		public string getLocalName() {
			return localName;
		}
		public string getName() {
			return name;
		}
		public string getNamespaceURI() {
			return "http://www.w3.org/2000/xmlns/";
		}
		public string getValue() {
			return value;
		}
	}

	private static void canonicalize(
			INode node,
			StringBuilder builder,
			IList<IDictionary<string,string>> namespaceStack,
			IDictionary<string,string> prefixList,
			bool addPrefixes,
			bool withComments
			){
		int nodeType=node.getNodeType();
		if(nodeType==NodeType.COMMENT_NODE){
			if(withComments){
        builder.Append("<!--");
		  	builder.Append(((IComment)node).getData());
			  builder.Append("-->");
      }
		} else if(nodeType==NodeType.PROCESSING_INSTRUCTION_NODE){
			builder.Append("<?");
			builder.Append(((IProcessingInstruction)node).getTarget());
			string data=((IProcessingInstruction)node).getData();
			if(data.Length>0){
				builder.Append(' ');
				builder.Append(data);
			}
			builder.Append("?>");
		} else if(nodeType==NodeType.ELEMENT_NODE){
			IElement e=((IElement)node);
			IDictionary<string,string> nsRendered=namespaceStack[namespaceStack.Count-1];
			bool copied=false;
			builder.Append('<');
			if(e.getPrefix()!=null && e.getPrefix().Length>0){
				builder.Append(e.getPrefix());
				builder.Append(':');
			}
			builder.Append(e.getLocalName());
			List<IAttr> attrs=new List<IAttr>();
			ISet<string> declaredNames=null;
			if(addPrefixes && prefixList.Count>0){
				declaredNames=new HashSet<string>();
			}
			foreach(IAttr attr in e.getAttributes()){
				string name=attr.getName();
				string nsvalue=null;
				if("xmlns".Equals(name)){
					attrs.Add(attr); // add default _namespace
					if(declaredNames!=null) {
						declaredNames.Add("");
					}
					nsvalue=attr.getValue();
				} else if(name.StartsWith("xmlns:",StringComparison.Ordinal) && name.Length>6){
					attrs.Add(attr); // add prefix _namespace
					if(declaredNames!=null) {
						declaredNames.Add(attr.getLocalName());
					}
					nsvalue=attr.getValue();
				}
				if(!string.IsNullOrEmpty(nsvalue)){
					if(!URIUtility.hasSchemeForURI(nsvalue))
						throw new ArgumentException(nsvalue+" is not a valid _namespace URI.");
				}
			}
			if(declaredNames!=null){
				// add declared prefixes to list
				foreach(string prefix in prefixList.Keys){
					if(prefix==null || declaredNames.Contains(prefix)) {
						continue;
					}
					string value=prefixList[prefix];
					if(value==null) {
						value="";
					}
					attrs.Add(new NamespaceAttr(prefix,value));
				}
			}
			attrs.Sort(attrNamespaceComparer);
			foreach(IAttr attr in attrs){
				string prefix=attr.getLocalName();
				if(attr.getPrefix().Length==0){
					prefix="";
				}
				string value=attr.getValue();
				bool isEmpty=string.IsNullOrEmpty(prefix);
				bool isEmptyDefault=(isEmpty && string.IsNullOrEmpty(value));
				bool renderNamespace=false;
				if(isEmptyDefault){
					///
					// condition used for Canonical XML
					//renderNamespace=(
					//		(e.getParentNode() is IElement) &&
					//		!string.IsNullOrEmpty(((IElement)e.getParentNode()).getAttribute("xmlns"))
					//		);
					///
					// changed condition for Exclusive XML Canonicalization
					renderNamespace=(isVisiblyUtilized(e,"") ||
							prefixList.ContainsKey("")) &&
							nsRendered.ContainsKey("");
				} else {
					string renderedValue=nsRendered[prefix];
					renderNamespace=(renderedValue==null ||
							!renderedValue.Equals(value));
					// added condition for Exclusive XML Canonicalization
					renderNamespace=renderNamespace && (isVisiblyUtilized(e,prefix) ||
							prefixList.ContainsKey(prefix));
				}
				if(renderNamespace){
					renderAttribute(builder,
							(isEmpty ? null : "xmlns"),
							(isEmpty ? "xmlns" : prefix),
							value);
					if(!copied){
						copied=true;
						nsRendered=new PeterO.Support.LenientDictionary<string,string>(nsRendered);
					}
					nsRendered.Add(prefix, value);
				}
			}
			namespaceStack.Add(nsRendered);
			attrs.Clear();
			// All other attributes
			foreach(IAttr attr in e.getAttributes()){
				string name=attr.getName();
				if(!("xmlns".Equals(name) ||
						(name.StartsWith("xmlns:",StringComparison.Ordinal) && name.Length>6))){
					// non-_namespace node
					attrs.Add(attr);
				}
			}
			attrs.Sort(attrComparer);
			foreach(IAttr attr in attrs){
				renderAttribute(builder,
						attr.getPrefix(),attr.getLocalName(),attr.getValue());
			}
			builder.Append('>');
			foreach(INode child in node.getChildNodes()){
				canonicalize(child,builder,namespaceStack,prefixList,false,withComments);
			}
			namespaceStack.RemoveAt(namespaceStack.Count-1);
			builder.Append("</");
			if(e.getPrefix()!=null && e.getPrefix().Length>0){
				builder.Append(e.getPrefix());
				builder.Append(':');
			}
			builder.Append(e.getLocalName());
			builder.Append('>');
		} else if(nodeType==NodeType.TEXT_NODE){
			string comment=((IText)node).getData();
			for(int i=0;i<comment.Length;i++){
				char c=comment[i];
				if(c==0x0d) {
					builder.Append("&#xD;");
				} else if(c=='>') {
					builder.Append("&gt;");
				} else if(c=='<') {
					builder.Append("&lt;");
				} else if(c=='&') {
					builder.Append("&amp;");
				} else {
					builder.Append(c);
				}
			}
		}
	}

}

}
