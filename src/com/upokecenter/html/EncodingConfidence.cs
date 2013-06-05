/*
If you like this, you should donate to Peter O.
at: http://upokecenter.com/d/



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

sealed class EncodingConfidence {
  int confidence;
  string encoding;
  public static readonly int Irrelevant=0;
  public static readonly int Tentative=1;
  public static readonly int Certain=2;
  public static readonly EncodingConfidence UTF16BE=
      new EncodingConfidence("utf-16be",Certain);
  public static readonly EncodingConfidence UTF16LE=
      new EncodingConfidence("utf-16le",Certain);
  public static readonly EncodingConfidence UTF8=
      new EncodingConfidence("utf-8",Certain);

  public static readonly EncodingConfidence UTF8_TENTATIVE=
      new EncodingConfidence("utf-8",Tentative);
  public EncodingConfidence(string e){
    encoding=e;
    confidence=Tentative;
  }
  public EncodingConfidence(string e, int c){
    encoding=e;
    confidence=c;
  }
  public int getConfidence() {
    return confidence;
  }
  public string getEncoding() {
    return encoding;
  }
  public override sealed string ToString() {
    return "EncodingConfidence [confidence=" + confidence + ", encoding="
        + encoding + "]";
  }
}
}
