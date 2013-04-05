// Written by Peter Occil, 2013. In the public domain.
// Public domain dedication: http://creativecommons.org/publicdomain/zero/1.0/
namespace com.upokecenter.rdf {
using System;

sealed class UriResolver {
	private UriResolver(){}
	private static int[] splitUri(string refValue){
		int index=0;
		int[] ret=new int[]{-1,-1,-1,-1,-1,-1,-1,-1,-1,-1};
		if(refValue==null || refValue.Length==0)return ret;
		bool scheme=false;
		// scheme
		while(index<refValue.Length){
			int c=refValue[index];
			if(index>0 && c==':'){
				scheme=true;
				ret[0]=0;
				ret[1]=index;
				index++;
				break;
			}
			if(index==0 && !((c>='a' && c<='z') || (c>='A' && c<='Z'))){
				index++;
				break;
			}
			else if(index>0 && !((c>='a' && c<='z') || (c>='A' && c<='Z') || (c>='0' && c<='9') ||
					c=='+' && c=='-' && c=='.')){
				index++;
				break;
			}

			index++;
		}
		if(!scheme) {
			index=0;
		}
		if(index+2<=refValue.Length && refValue[index]=='/' && refValue[index+1]=='/'){
			// authority
			ret[2]=index+2;
			ret[3]=refValue.Length;
			index+=2;
			while(index<refValue.Length){
				int c=refValue[index];
				if(c=='/' || c=='?' || c=='#'){
					ret[3]=index;
					break;
				}
				index++;
			}
		}
		// path
		ret[4]=index;
		ret[5]=refValue.Length;
		while(index<refValue.Length){
			int c=refValue[index];
			if(c=='?' || c=='#'){
				ret[5]=index;
				break;
			}
			index++;
		}
		if(ret[4]>ret[5]){
			ret[4]=0;
			ret[5]=0;
		}
		// query
		ret[6]=index+1;
		ret[7]=refValue.Length;
		while(index<refValue.Length){
			int c=refValue[index];
			if(c=='#'){
				ret[7]=index;
				break;
			}
			index++;
		}
		// fragment
		ret[8]=index+1;
		ret[9]=refValue.Length;
		if(ret[6]>ret[7]){
			ret[6]=-1;
			ret[7]=-1;
		}
		if(ret[8]>ret[9]){
			ret[8]=-1;
			ret[9]=-1;
		}
		return ret;
	}

	private static void appendScheme(
			System.Text.StringBuilder builder, string refValue, int[] segments){
		if(segments[0]>=0){
			builder.Append(refValue.Substring(segments[0],(segments[1])-(segments[0])));
			builder.Append(':');
		}
	}
	private static void appendAuthority(
			System.Text.StringBuilder builder, string refValue, int[] segments){
		if(segments[2]>=0){
			builder.Append("//");
			builder.Append(refValue.Substring(segments[2],(segments[3])-(segments[2])));
		}
	}
	private static void appendPath(
			System.Text.StringBuilder builder, string refValue, int[] segments){
		builder.Append(refValue.Substring(segments[4],(segments[5])-(segments[4])));
	}
	private static void appendQuery(
			System.Text.StringBuilder builder, string refValue, int[] segments){
		if(segments[6]>=0){
			builder.Append('?');
			builder.Append(refValue.Substring(segments[6],(segments[7])-(segments[6])));
		}
	}
	private static void appendFragment(
			System.Text.StringBuilder builder, string refValue, int[] segments){
		if(segments[8]>=0){
			builder.Append('#');
			builder.Append(refValue.Substring(segments[8],(segments[9])-(segments[8])));
		}
	}

	private static string pathParent(string refValue, int startIndex, int endIndex){
		if(startIndex>endIndex)return "";
		endIndex--;
		while(endIndex>=startIndex){
			if(refValue[endIndex]=='/')
				return refValue.Substring(startIndex,(endIndex+1)-(startIndex));
			endIndex--;
		}
		return "";
	}

	private static string normalizePath(string path){
		int len=path.Length;
		if(len==0 || path.Equals("..") || path.Equals("."))
			return "";
		if(path.IndexOf("/.",StringComparison.Ordinal)<0 && path.IndexOf("./",StringComparison.Ordinal)<0)
			return path;
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		int index=0;
		while(index<len){
			char c=path[index];
			if((index+3<=len && c=='/' &&
					path[index+1]=='.' &&
					path[index+2]=='/') ||
					(index+2==len && c=='.' &&
					path[index+1]=='.')){
				// begins with "/./" or is "..";
				// move index by 2
				index+=2;
				continue;
			} else if((index+3<=len && c=='.' &&
					path[index+1]=='.' &&
					path[index+2]=='/')){
				// begins with "../";
				// move index by 3
				index+=3;
				continue;
			} else if((index+2<=len && c=='.' &&
					path[index+1]=='/') ||
					(index+1==len && c=='.')){
				// begins with "./" or is ".";
				// move index by 1
				index+=1;
				continue;
			} else if(index+2==len && c=='/' &&
					path[index+1]=='.'){
				// is "/."; append '/' and break
				builder.Append('/');
				break;
			} else if((index+3==len && c=='/' &&
					path[index+1]=='.' &&
					path[index+2]=='.')){
				// is "/.."; remove last segment,
				// append "/" and return
				int index2=builder.Length-1;
				while(index2>=0){
					if(builder[index2]=='/'){
						break;
					}
					index2--;
				}
				if(index2<0) {
					index2=0;
				}
				builder.Length=(index2);
				builder.Append('/');
				break;
			} else if((index+4<=len && c=='/' &&
					path[index+1]=='.' &&
					path[index+2]=='.' &&
					path[index+3]=='/')){
				// begins with "/../"; remove last segment
				int index2=builder.Length-1;
				while(index2>=0){
					if(builder[index2]=='/'){
						break;
					}
					index2--;
				}
				if(index2<0) {
					index2=0;
				}
				builder.Length=(index2);
				index+=3;
				continue;
			} else {
				builder.Append(c);
				index++;
			}
		}
		return builder.ToString();
	}
	private static void appendNormalizedPath(
			System.Text.StringBuilder builder, string refValue, int[] segments){
		builder.Append(normalizePath(refValue.Substring(segments[4],(segments[5])-(segments[4]))));
	}

	public static bool hasScheme(string refValue){
		int[] segments=splitUri(refValue);
		return segments[0]>=0;
	}

	public static string relativeResolve(string refValue, string _base){
		int[] segments=splitUri(refValue);
		int[] segmentsBase=splitUri(_base);
		System.Text.StringBuilder builder=new System.Text.StringBuilder();
		if(segments[0]>=0){
			appendScheme(builder,refValue,segments);
			appendAuthority(builder,refValue,segments);
			appendNormalizedPath(builder,refValue,segments);
			appendQuery(builder,refValue,segments);
			appendFragment(builder,refValue,segments);
		} else if(segments[2]>=0){
			appendScheme(builder,_base,segmentsBase);
			appendAuthority(builder,refValue,segments);
			appendNormalizedPath(builder,refValue,segments);
			appendQuery(builder,refValue,segments);
			appendFragment(builder,refValue,segments);
		} else if(segments[4]==segments[5]){
			appendScheme(builder,_base,segmentsBase);
			appendAuthority(builder,_base,segmentsBase);
			appendPath(builder,_base,segmentsBase);
			if(segments[6]>=0){
				appendQuery(builder,refValue,segments);
			} else {
				appendQuery(builder,_base,segmentsBase);
			}
			appendFragment(builder,refValue,segments);
		} else {
			appendScheme(builder,_base,segmentsBase);
			appendAuthority(builder,_base,segmentsBase);
			if(segments[4]<segments[5] && refValue[segments[4]]=='/'){
				appendNormalizedPath(builder,refValue,segments);
			} else {
				System.Text.StringBuilder merged=new System.Text.StringBuilder();
				if(segmentsBase[2]>=0 && segmentsBase[4]==segments[5]){
					merged.Append('/');
					appendPath(merged,refValue,segments);
					builder.Append(normalizePath(merged.ToString()));
				} else {
					merged.Append(pathParent(_base,segmentsBase[4],segmentsBase[5]));
					appendPath(merged,refValue,segments);
					builder.Append(normalizePath(merged.ToString()));
				}
			}
			appendQuery(builder,refValue,segments);
			appendFragment(builder,refValue,segments);
		}
		return builder.ToString();
	}
}

}
