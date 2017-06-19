namespace com.upokecenter.html.data {
using System;
using System.Collections.Generic;
using com.upokecenter.html;
using com.upokecenter.json;
using com.upokecenter.util;

public sealed class Microdata {
  private class ElementAndIndex {
    public int index;
    public IElement element;
  }

  private sealed class SortInTreeOrderComparer : IComparer<ElementAndIndex> {
    public int Compare(ElementAndIndex arg0, ElementAndIndex arg1) {
    return (arg0.index == arg1.index) ? (0) : ((arg0.index<arg1.index) ? -1 :
        1);
    }
  }

  private static int getElementIndex(
      INode root,
      IElement e,
      int startIndex) {
    int[] runningIndex = new int[] { startIndex };
    return getElementIndex(root, e, runningIndex);
  }
  private static int getElementIndex(
      INode root,
      IElement e,
      int[] runningIndex) {
    int index = runningIndex[0];
    if (root.Equals(e)) {
 return index;
}
    ++index;
    foreach (var child in root.getChildNodes()) {
      int idx = getElementIndex(child, e, runningIndex);
      if (idx >= 0) {
 return idx;
}
    }
    runningIndex[0]=index;
    return -1;
  }

  private static string getHref(IElement node) {
    string name = StringUtility.toLowerCaseAscii(node.getLocalName());
    string href="";
    if ("a".Equals(name) || "link".Equals(name) || "area".Equals(name)) {
      href=node.getAttribute("href");
    } else if ("object".Equals(name)) {
      href=node.getAttribute("data");
    } else if ("img".Equals(name) || "source".Equals(name) ||
        "track".Equals(name) || "iframe".Equals(name) ||
        "audio".Equals(name) || "video".Equals(name) ||
        "embed".Equals(name)) {
      href=node.getAttribute("src");
    } else {
 return null;
}
    if (href == null || href.Length == 0) {
 return "";
}
    href = HtmlDocument.resolveURL(node, href, null);
    return (href==null || href.Length==0) ? ("") : (href);
  }

  public static PeterO.Cbor.CBORObject getMicrodataJSON(IDocument document) {
    if ((document) == null) {
 throw new ArgumentNullException("document");
}
    PeterO.Cbor.CBORObject result = PeterO.Cbor.CBORObject.NewMap();
    JSONArray items = new JSONArray();
    foreach (var node in document.getElementsByTagName("*")) {
      if (node.getAttribute("itemscope")!=null &&
          node.getAttribute("itemprop")==null) {
        IList<IElement> memory = new List<IElement>();
        items.put(getMicrodataObject(node, memory));
      }
    }
    result.put("items", items);
    return result;
  }

  private static PeterO.Cbor.CBORObject getMicrodataObject(IElement item,
    IList<IElement> memory) {
 string[] itemtypes=StringUtility.splitAtSpaces(item.getAttribute("itemtype"
));
    PeterO.Cbor.CBORObject result = PeterO.Cbor.CBORObject.NewMap();
    memory.Add(item);
    if (itemtypes.Length>0) {
      JSONArray array = new JSONArray();
      foreach (var itemtype in itemtypes) {
        array.put(itemtype);
      }
      result.put("type",array);
    }
    string globalid=item.getAttribute("itemid");
    if (globalid != null) {
      globalid = HtmlDocument.resolveURL(item, globalid,
          item.getBaseURI());
      result.put("id", globalid);
    }
    PeterO.Cbor.CBORObject properties = PeterO.Cbor.CBORObject.NewMap();
    foreach (var element in getMicrodataProperties(item)) {
  string[] names=StringUtility.splitAtSpaces(element.getAttribute("itemprop"
));
      Object obj = null;
      if (element.getAttribute("itemscope")!=null) {
        if (memory.Contains(element)) {
          obj="ERROR";
        } else {
          obj = getMicrodataObject(element, new List<IElement>(memory));
        }
      } else {
        obj = getPropertyValue(element);
      }
      foreach (var name in names) {
        if (properties.has(name)) {
          properties.getJSONArray(name).put(obj);
        } else {
          JSONArray arr = new JSONArray();
          arr.put(obj);
          properties.put(name, arr);
        }
      }
    }
    result.put("properties",properties);
    return result;
  }

  private static IList<IElement> getMicrodataProperties(IElement root) {
    IList<IElement> results = new List<IElement>();
    IList<IElement> memory = new List<IElement>();
    IList<IElement> pending = new List<IElement>();
    memory.Add(root);
    IDocument document = root.getOwnerDocument();
    foreach (var child in root.getChildNodes()) {
      if (child is IElement) {
        pending.Add((IElement)child);
      }
    }
    string[] itemref=StringUtility.splitAtSpaces(root.getAttribute("itemref"));
    foreach (var item in itemref) {
      IElement element = document.getElementById(item);
      if (element != null) {
        pending.Add(element);
      }
    }
    while (pending.Count>0) {
      IElement current = pending[0];
      pending.RemoveAt(0);
      if (memory.Contains(current)) {
        continue;
      }
      memory.Add(current);
      if (current.getAttribute("itemscope")==null) {
        foreach (var child in current.getChildNodes()) {
          if (child is IElement) {
            pending.Add((IElement)child);
          }
        }
      }
      if (!StringUtility.isNullOrSpaces(current.getAttribute("itemprop"))) {
        results.Add(current);
      }
    }
    return sortInTreeOrder(results, document);
  }

  private static string getPropertyValue(IElement e) {
    if (isHtmlElement(e)) {
      if (isHtmlElement(e,"meta")) {
        string attr=e.getAttribute("content");
        return (attr==null) ? "" : attr;
      }
      string href = getHref(e);
      if (href != null) {
 return href;
}
      if (isHtmlElement(e,"data")) {
        string attr=e.getAttribute("value");
        return (attr==null) ? "" : attr;
      }
      if (isHtmlElement(e,"time")) {
        string attr=e.getAttribute("datetime");
        if (attr != null) {
 return attr;
}
      }
    }
    return e.getTextContent();
  }

  private static bool isHtmlElement(IElement element) {
    return "http://www.w3.org/1999/xhtml".Equals(element.getNamespaceURI());
  }

  private static bool isHtmlElement(IElement e, string name) {
    return e.getLocalName().Equals(name) && isHtmlElement(e);
  }

  private static IList<IElement> sortInTreeOrder(
      IList<IElement> elements,
      INode root) {
    if (elements == null || elements.Count< 2) {
 return elements;
}
    List<ElementAndIndex> elems = new List<ElementAndIndex>();
    foreach (var element in elements) {
      ElementAndIndex el = new ElementAndIndex();
      el.element = element;
      el.index = getElementIndex(root, element, 0);
      elems.Add(el);
    }
    elems.Sort(new SortInTreeOrderComparer());
    IList<IElement> ret = new List<IElement>();
    foreach (var el in elems) {
      ret.Add(el.element);
    }
    return ret;
  }

  private Microdata() {}
}
}
