namespace com.upokecenter.html {
using System;

public interface IProcessingInstruction : INode {
	 string getTarget();
	 string getData();
}

}
