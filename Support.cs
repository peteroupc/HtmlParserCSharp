using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using com.upokecenter.util;

namespace PeterO.Support {
    /// <summary>Not documented yet.</summary>
  public interface InputStream : PeterO.IReader {
    /// <summary>Not documented yet.</summary>
    /// <param name='limit'>The parameter <paramref name='limit'/> is not
    /// documented yet.</param>
    void mark(int limit);

    /// <summary>Not documented yet.</summary>
    void reset();
  }
}
