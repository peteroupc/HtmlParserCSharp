using System;
using System.Collections.Generic;

namespace Com.Upokecenter.Html {
  internal interface INameAndAttributes {
    string GetName();

    IList<Attr> GetAttributes();
  }
}
