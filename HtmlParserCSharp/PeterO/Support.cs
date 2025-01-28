using System;
using System.Collections.Generic;
using System.IO;

namespace PeterO.Support {
  /// <summary>Not documented yet.</summary>
  public interface InputStream : PeterO.IReader {
    /// <summary>Not documented yet.</summary>
    /// <param name='limit'>The parameter <paramref name='limit'/> is not
    /// documented yet.</param>
    void Mark(int limit);

    /// <summary>Not documented yet.</summary>
    void Reset();
  }
}
