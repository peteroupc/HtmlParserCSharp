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
using System;
using System.Collections.Generic;

namespace Com.Upokecenter.Net {
  /// <summary>Not documented yet.</summary>
  public interface IHttpHeaders {
    /// <summary>Not documented yet.</summary>
    /// <param name='name'>The parameter <paramref name='name'/> is a
    /// 32-bit signed integer.</param>
    /// <returns>The return value is not documented yet.</returns>
    string GetHeaderField(int name);

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>The parameter <paramref name='name'/> is a text
    /// string.</param>
    /// <returns>The return value is not documented yet.</returns>
    string GetHeaderField(string name);

    /// <summary>Not documented yet.</summary>
    /// <returns>The return value is not documented yet.</returns>
    long GetHeaderFieldDate(string field, long defaultValue);

    /// <summary>Not documented yet.</summary>
    /// <param name='name'>The parameter <paramref name='name'/> is a
    /// 32-bit signed integer.</param>
    /// <returns>The return value is not documented yet.</returns>
    string GetHeaderFieldKey(int name);

    /// <summary>Not documented yet.</summary>
    /// <returns>The return value is not documented yet.</returns>
    IDictionary<string, IList<string>> GetHeaderFields();

    /// <summary>Not documented yet.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetRequestMethod();

    /// <summary>Not documented yet.</summary>
    /// <returns>The return value is not documented yet.</returns>
    int GetResponseCode();

    /// <summary>Not documented yet.</summary>
    /// <returns>The return value is not documented yet.</returns>
    string GetUrl();
  }
}
