using System;
using System.Collections.Generic;
namespace com.upokecenter.html {
  internal interface INameAndAttributes {
    string getName();
    IList<Attr> getAttributes();
  }
}
