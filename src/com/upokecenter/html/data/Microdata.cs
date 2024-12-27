using System;
using System.Collections.Generic;
using Com.Upokecenter.Html;
using Com.Upokecenter.util;
using PeterO;
using PeterO.Cbor;

namespace Com.Upokecenter.Html.Data {
  /// <summary>Not documented yet.</summary>
  public sealed class Microdata {
    private class ElementAndIndex {
      internal int Index {
        get;
        set;
      }

      internal IElement Element {
        get;
        set;
      }
    }

    private sealed class SortInTreeOrderComparer : IComparer<ElementAndIndex> {
      public int Compare (ElementAndIndex arg0, ElementAndIndex arg1) {
        return (arg0.Index == arg1.Index) ? 0 : ((arg0.Index < arg1.Index) ?
-1 :
            1);
      }
    }

    private static int GetElementIndex(
      INode root,
      IElement e,
      int startIndex) {
      var runningIndex = new int[] { startIndex };
      return GetElementIndex (root, e, runningIndex);
    }

    private static int GetElementIndex(
      INode root,
      IElement e,
      int[] runningIndex) {
      int valueIndex = runningIndex[0];
      if (root.Equals (e)) {
        return valueIndex;
      }
      ++valueIndex;
      foreach (var child in root.getChildNodes()) {
        int idx = GetElementIndex (child, e, runningIndex);
        if (idx >= 0) {
          return idx;
        }
      }
      runningIndex[0] = valueIndex;
      return -1;
    }

    private static string GetHref (IElement node) {
      string name = DataUtilities.ToLowerCaseAscii (node.getLocalName());
      string href = String.Empty;
      if ("a".Equals (name, StringComparison.Ordinal) ||
        "link".Equals (name, StringComparison.Ordinal) ||
        "area".Equals (name, StringComparison.Ordinal)) {
        href = node.getAttribute ("href");
      } else if ("object".Equals (name, StringComparison.Ordinal)) {
        href = node.getAttribute ("Data");
      } else if ("img".Equals (name, StringComparison.Ordinal) ||
        "source".Equals (name, StringComparison.Ordinal) ||
        "track".Equals (name, StringComparison.Ordinal) ||
        "iframe".Equals (name, StringComparison.Ordinal) ||
        "audio".Equals (name, StringComparison.Ordinal) ||
        "video".Equals (name, StringComparison.Ordinal) ||
        "embed".Equals (name, StringComparison.Ordinal)) {
        href = node.getAttribute ("src");
      } else {
        return null;
      }
      if (href == null || href.Length == 0) {
        return String.Empty;
      }
      href = HtmlCommon.resolveURL (node, href, null);
      return (href == null || href.Length == 0) ? String.Empty : href;
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='document'>The parameter <paramref name='document'/> is
    /// a.Upokecenter.Html.IDocument object.</param>
    /// <returns>The return value is not documented yet.</returns>
    public static PeterO.Cbor.CBORObject GetMicrodataJSON (IDocument document) {
      if (document == null) {
        throw new ArgumentNullException(nameof(document));
      }
      PeterO.Cbor.CBORObject result = PeterO.Cbor.CBORObject.NewMap();
      var items = CBORObject.NewArray();
      foreach (var node in document.getElementsByTagName ("*")) {
        if (node.getAttribute ("itemscope") != null &&
          node.getAttribute ("itemprop") == null) {
          IList<IElement> memory = new List<IElement>();
          items.Add (GetMicrodataObject (node, memory));
        }
      }
      result.Add ("items", items);
      return result;
    }

    private static PeterO.Cbor.CBORObject GetMicrodataObject(
      IElement item,
      IList<IElement> memory) {
      string[] itemtypes = StringUtility.SplitAtSpTabCrLfFf (item.getAttribute(
            "itemtype"));
      PeterO.Cbor.CBORObject result = PeterO.Cbor.CBORObject.NewMap();
      memory.Add (item);
      if (itemtypes.Length > 0) {
        var array = CBORObject.NewArray();
        foreach (var itemtype in itemtypes) {
          array.Add (itemtype);
        }
        result.Add ("type", array);
      }
      string globalid = item.getAttribute ("itemid");
      if (globalid != null) {
        globalid = HtmlCommon.resolveURL(
            item,
            globalid,
            item.getBaseURI());
        result.Add ("id", globalid);
      }
      PeterO.Cbor.CBORObject properties = PeterO.Cbor.CBORObject.NewMap();
      foreach (var valueElement in GetMicrodataProperties (item)) {
        string[] names = StringUtility.SplitAtSpTabCrLfFf(
            valueElement.getAttribute(
              "itemprop"));
        Object obj = null;
        if (valueElement.getAttribute ("itemscope") != null) {
          obj = memory.Contains (valueElement) ? (object)"ERROR" :
            (object)GetMicrodataObject (valueElement, new
List<IElement>(memory));
          } else {
          obj = GetPropertyValue (valueElement);
        }
        foreach (var name in names) {
          if (properties.ContainsKey (name)) {
            properties[name].Add (obj);
          } else {
            var arr = CBORObject.NewArray();
            arr.Add (obj);
            properties.Add (name, arr);
          }
        }
      }
      result.Add ("properties", properties);
      return result;
    }

    private static IList<IElement> GetMicrodataProperties (IElement root) {
      IList<IElement> results = new List<IElement>();
      IList<IElement> memory = new List<IElement>();
      IList<IElement> pending = new List<IElement>();
      memory.Add (root);
      IDocument document = root.getOwnerDocument();
      foreach (var child in root.getChildNodes()) {
        if (child is IElement) {
          pending.Add ((IElement)child);
        }
      }
      string[] itemref = StringUtility.SplitAtSpTabCrLfFf (root.getAttribute(
            "itemref"));
      foreach (var item in itemref) {
        IElement valueElement = document.getElementById (item);
        if (valueElement != null) {
          pending.Add (valueElement);
        }
      }
      while (pending.Count > 0) {
        IElement current = pending[0];
        pending.RemoveAt (0);
        if (memory.Contains (current)) {
          continue;
        }
        memory.Add (current);
        if (current.getAttribute ("itemscope") == null) {
          foreach (var child in current.getChildNodes()) {
            if (child is IElement) {
              pending.Add ((IElement)child);
            }
          }
        }
        if (!StringUtility.isNullOrSpaces (current.getAttribute ("itemprop"))) {
          results.Add (current);
        }
      }
      return SortInTreeOrder (results, document);
    }

    private static string GetPropertyValue (IElement e) {
      if (IsHtmlElement (e)) {
        if (IsHtmlElement (e, "meta")) {
          string attr = e.getAttribute ("content");
          return (attr == null) ? String.Empty : attr;
        }
        string href = GetHref (e);
        if (href != null) {
          return href;
        }
        if (IsHtmlElement (e, "Data")) {
          string attr = e.getAttribute ("value");
          return (attr == null) ? String.Empty : attr;
        }
        if (IsHtmlElement (e, "time")) {
          string attr = e.getAttribute ("datetime");
          if (attr != null) {
            return attr;
          }
        }
      }
      return e.getTextContent();
    }

    private static bool IsHtmlElement (IElement valueElement) {
      return "http://www.w3.org/1999/xhtml"
        .Equals (valueElement.getNamespaceURI());
    }

    private static bool IsHtmlElement (IElement e, string name) {
      return e.getLocalName().Equals (name, StringComparison.Ordinal) &&
        IsHtmlElement (e);
    }

    private static IList<IElement> SortInTreeOrder(
      IList<IElement> elements,
      INode root) {
      if (elements == null || elements.Count < 2) {
        return elements;
      }
      var elems = new List<ElementAndIndex>();
      foreach (var valueElement in elements) {
        var el = new ElementAndIndex();
        el.Element = valueElement;
        el.Index = GetElementIndex (root, valueElement, 0);
        elems.Add (el);
      }
      elems.Sort (new SortInTreeOrderComparer());
      IList<IElement> ret = new List<IElement>();
      foreach (var el in elems) {
        ret.Add (el.Element);
      }
      return ret;
    }

    private Microdata() {
    }
  }
}
