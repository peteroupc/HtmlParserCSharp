namespace com.upokecenter.util {
using System;

using System.IO;


public sealed class IntListCharacterInput : ICharacterInput {

	int pos;
	IntList ilist;

	public IntListCharacterInput(IntList ilist){
		this.ilist=ilist;
	}

	public int read(int[] buf, int offset, int unitCount)  {
		if(offset<0 || unitCount<0 || offset+unitCount>buf.Length)
			throw new ArgumentOutOfRangeException();
		if(unitCount==0)return 0;
		int[] arr=this.ilist.array();
		int size=this.ilist.Count;
		int count=0;
		while(pos<size && unitCount>0){
			buf[offset]=arr[pos];
			offset++;
			count++;
			unitCount--;
			pos++;
		}
		return count==0 ? -1 : count;
	}

	public int read()  {
		int[] arr=this.ilist.array();
		if(pos<this.ilist.Count)
			return arr[pos++];
		return -1;
	}

}

}
