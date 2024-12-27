HTML5 parser for C#, translated from the original Java.

Takes an input stream or a file and returns an HTML document tree.
The API is currently only a subset of the DOM.  Example:

    IDocument doc=HtmlDocument.parseFile(filename);
    for(IElement element : doc.getElementsByTagName("img")){
        Console.WriteLine(element.getAttribute("src"));
    }

Copyright (C) 2013 Peter Occil.  Licensed under the Expat License.

Sample code on this README file is dedicated to the public domain under CC0:
[https://creativecommons.org/publicdomain/zero/1.0/](https://creativecommons.org/publicdomain/zero/1.0/)
