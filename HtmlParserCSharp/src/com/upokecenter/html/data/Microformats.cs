using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Com.Upokecenter.Html;
using Com.Upokecenter.Util;
using PeterO;
using PeterO.Cbor;

namespace Com.Upokecenter.Html.Data {
  /// <summary>Not documented yet.</summary>
  public sealed class Microformats {
    private static IDictionary<string, string[]> complexLegacyMap = new
    Dictionary<string, string[]>();

    static Microformats() {
      string[] strarr;
      complexLegacyMap.Add("adr", new string[] { "p-adr", "h-adr" });
      strarr = new string[] {
        "p-affiliation",
        "h-card",
      };
      complexLegacyMap.Add(
        "affiliation",
        strarr);
      complexLegacyMap.Add("author", new string[] { "p-author", "h-card" });
      complexLegacyMap.Add("contact", new string[] { "p-contact", "h-card" });
      strarr = new string[] {
        "p-education",
        "h-event",
      };
      complexLegacyMap.Add(
        "education",
        strarr);
      strarr = new string[] {
        "p-experience",
        "h-event",
      };
      complexLegacyMap.Add(
        "experience",
        strarr);
      complexLegacyMap.Add("fn", new string[] {
        "p-item", "h-item",
        "p-name",
      });
      complexLegacyMap.Add("geo", new string[] { "p-geo", "h-geo" });
      strarr = new string[] {
        "p-location",
        "h-card",
        "h-adr",
      };

      complexLegacyMap.Add(
        "location",
        strarr);
      strarr = new string[] {
        "p-item",
        "h-item",
        "u-photo",
      };
      complexLegacyMap.Add(
        "photo",
        strarr);
      complexLegacyMap.Add("review", new string[] { "p-review", "h-review" });
      complexLegacyMap.Add("reviewer", new string[] {
        "p-reviewer",
        "h-card",
      });
      complexLegacyMap.Add("url", new string[] {
        "p-item", "h-item",
        "u-url",
      });
    }

    private static readonly string[] ValueLegacyLabels = new string[] {
      "additional-name", "p-additional-name", "adr", "h-adr", "bday",
      "dt-bday", "best", "p-best", "brand", "p-brand", "category",
      "p-category", "count", "p-count", "country-name", "p-country-name",
      "description", "e-description", "dtend", "dt-end", "dtreviewed",
      "dt-dtreviewed", "dtstart", "dt-start", "duration",
      "dt-duration", "e-entry-summary", "e-summary", "email", "u-email",
      "entry-content", "e-content", "entry-summary", "p-summary",
      "entry-title",
      "p-name", "extended-address", "p-extended-address", "family-name",
      "p-family-name", "fn", "p-name", "geo", "h-geo", "given-name",
      "p-given-name", "hentry", "h-entry", "honorific-prefix",
      "p-honorific-prefix", "honorific-suffix", "p-honorific-suffix",
      "hproduct", "h-product", "hrecipe", "h-recipe", "hresume",
      "h-resume", "hreview", "h-review", "hreview-aggregate",
      "h-review-aggregate", "identifier", "u-identifier", "ingredient",
      "p-ingredient",
      "instructions", "e-instructions", "key", "u-key", "label",
      "p-label", "latitude", "p-latitude", "locality", "p-locality",
      "logo", "u-logo", "longitude", "p-longitude", "nickname",
      "p-nickname", "note", "p-note", "nutrition", "p-nutrition", "org",
      "p-org", "organization-name", "p-organization-name",
      "organization-unit", "p-organization-unit", "p-entry-summary",
      "p-summary", "p-entry-title", "p-name", "photo", "u-photo",
      "post-office-box", "p-post-office-box",
      "postal-code", "p-postal-code", "price", "p-price", "published",
      "dt-published", "rating", "p-rating", "region", "p-region", "rev",
      "dt-rev", "skill", "p-skill", "street-address",
      "p-street-address", "summary", "p-name", "tel", "p-tel", "tz",
      "p-tz", "uid", "u-uid", "updated", "dt-updated", "url", "p-url",
      "vcard", "h-card", "vevent", "h-event", "votes", "p-votes",
      "worst", "p-worst", "yield", "p-yield",
    };

    private static readonly IDictionary<string, string>
    ValueLegacyLabelsMap = CreateLegacyLabelsMap();

    private static readonly int[] ValueNormalDays = {
      0, 31, 28, 31, 30, 31, 30, 31, 31, 30,
      31, 30, 31,
    };

    private static readonly int[] ValueLeapDays = {
      0, 31, 29, 31, 30, 31, 30, 31, 31, 30,
      31, 30, 31,
    };

    private static void AccumulateValue(
      CBORObject obj,
      string key,
      Object value) {
      CBORObject arr = null;
      if (obj.ContainsKey(key)) {
        arr = obj[key];
      } else {
        arr = CBORObject.NewArray();
        obj.Add(key, arr);
      }
      arr.Add(value);
    }

    private static void Append2d(StringBuilder builder, int value) {
      value = Math.Abs(value);
      builder.Append((char)('0' + ((value / 10) % 10)));
      builder.Append((char)('0' + (value % 10)));
    }

    private static void Append3d(StringBuilder builder, int value) {
      value = Math.Abs(value);
      builder.Append((char)('0' + ((value / 100) % 10)));
      builder.Append((char)('0' + ((value / 10) % 10)));
      builder.Append((char)('0' + (value % 10)));
    }

    private static void Append4d(StringBuilder builder, int value) {
      value = Math.Abs(value);
      builder.Append((char)('0' + ((value / 1000) % 10)));
      builder.Append((char)('0' + ((value / 100) % 10)));
      builder.Append((char)('0' + ((value / 10) % 10)));
      builder.Append((char)('0' + (value % 10)));
    }

    private static void CopyComponents(
      int[] src,
      int[] components,
      bool useDate,
      bool useTime,
      bool useTimezone) {
      if (useDate) {
        if (src[0] != Int32.MinValue) {
          components[0] = src[0];
        }
        if (src[1] != Int32.MinValue) {
          components[1] = src[1];
        }
        if (src[2] != Int32.MinValue) {
          components[2] = src[2];
        }
      }
      if (useTime) {
        if (src[3] != Int32.MinValue) {
          components[3] = src[3];
        }
        if (src[4] != Int32.MinValue) {
          components[4] = src[4];
        }
        if (src[5] != Int32.MinValue) {
          components[5] = src[5];
        }
      }
      if (useTimezone) {
        if (src[6] != Int32.MinValue) {
          components[6] = src[6];
        }
        if (src[7] != Int32.MinValue) {
          components[7] = src[7];
        }
      }
    }

    private static CBORObject CopyJson(CBORObject obj) {
      return CBORObject.FromJSONString(obj.ToJSONString());
    }

    private static IDictionary<string, string> CreateLegacyLabelsMap() {
      IDictionary<string, string> map = new Dictionary<string, string>();
      for (int i = 0; i < ValueLegacyLabels.Length; i += 2) {
        map.Add(ValueLegacyLabels[i], ValueLegacyLabels[i + 1]);
      }
      return map;
    }

    private static string ElementName(IElement element) {
      return DataUtilities.ToLowerCaseAscii(element.GetLocalName());
    }

    private static IList<IElement> GetChildElements(INode e) {
      IList<IElement> elements = new List<IElement>();
      foreach (var child in e.GetChildNodes()) {
        if (child is IElement) {
          elements.Add((IElement)child);
        }
      }
      return elements;
    }

    private static string[] GetClassNames(IElement element) {
      string[] ret = StringUtility.SplitAtSpTabCrLfFf(element.GetAttribute(
        "class"));
      string[] rel = ParseLegacyRel(element.GetAttribute("rel"));
      if (ret.Length == 0 && rel.Length == 0) {
        return ret;
      }
      // Replace old microformats class names with
      // their modern versions
      IList<string> retList = new List<string>();
      foreach (var element2 in rel) {
        retList.Add(element2);
      }
      foreach (var element2 in ret) {
        string legacyLabel = ValueLegacyLabelsMap[element2];
        if (complexLegacyMap.ContainsKey(element2)) {
          foreach (var item in complexLegacyMap[element2]) {
            retList.Add(item);
          }
        } else if (legacyLabel != null) {
          retList.Add(legacyLabel);
        } else {
          retList.Add(element2);
        }
      }
      if (retList.Count >= 2) {
        var stringSet = new HashSet<string>(retList);
        retList = new List<string>((ICollection<string>)stringSet);
      }
      return retList.ToArray();
    }

    private static readonly string[] DatePatterns = new string[] {
      "%Y-%M-%d",
      "%Y-%D",
    };

    private static readonly string[] TimePatterns = new string[] {
      "%H:%m:%s",
      "%H:%m",
      "%H:%m:%s%Z:%z",
      "%H:%m:%s%Z%z", "%H:%m:%s%G",
      "%H:%m%Z:%z", "%H:%m%Z%z",
      "%H:%m%G",
    };

    private static string GetDTValue(IElement root, int[] source) {
      IList<IElement> valueElements = GetValueClasses(root);
      bool haveDate = false, haveTime = false, haveTimeZone = false;
      var components = new int[] {
        Int32.MinValue,
        Int32.MinValue,
        Int32.MinValue,
        Int32.MinValue,
        Int32.MinValue,
        Int32.MinValue,
        Int32.MinValue,
        Int32.MinValue,
      };
      if (source != null) {
        CopyComponents(source, components, true, true, true);
      }
      if (valueElements.Count == 0) {
        // No value elements, get the text content
        return GetDTValueContent(root);
      }
      foreach (var valueElement in valueElements) {
        string text = GetDTValueContent(valueElement);
        if (
          MatchDateTimePattern(
            text, // check date or date + time
            DatePatterns,
            TimePatterns,
            components,
            !haveDate,
            !haveTime,
            !haveTimeZone)) {
          // check if components are defined
          if (components[0] != Int32.MinValue) {
            haveDate = true;
          }
          if (components[3] != Int32.MinValue) {
            haveTime = true;
          }
          if (components[6] != Int32.MinValue) {
            haveTimeZone = true;
          }
        } else if (
          MatchDateTimePattern(
            text, // check time-only formats
            null,
            new string[] {
              "%H:%m:%s", "%H:%m",
              "%H:%m:%s%Z:%z",
              "%H:%m:%s%Z%z", "%H:%m:%s%G",
              "%H:%m%Z:%z", "%H:%m%Z%z",
              "%H:%m%G",
            },
            components,
            false,
            !haveTime,
            !haveTimeZone)) {
          // check if components are defined
          if (components[3] != Int32.MinValue) {
            haveTime = true;
          }
          if (components[6] != Int32.MinValue) {
            haveTimeZone = true;
          }
        } else if (
          MatchDateTimePattern(
            text,
            null,
            new string[] {
              "%Z:%z",
              "%Z%z",
              "%Z",
              "%G",
            },
            components,
            false,
            false,
            !haveTimeZone)) { // check timezone
          // formats
          if (components[6] != Int32.MinValue) {
            haveTimeZone = true;
          }
        } else if (
          MatchDateTimePattern(
            DataUtilities.ToLowerCaseAscii(text),
            null,
            new string[] {
              "%h:%m:%sa.m.",
              // AM clock values
              "%h:%m:%sam", "%h:%ma.m.", "%h:%mam",
              "%ha.m.", "%ham",
            },
            components,
            false,
            !haveTime,
            false)) { // check AM time formats
          if (components[3] != Int32.MinValue) {
            haveTime = true;
            // convert AM hour to 24-hour clock
            if (components[3] == 12) {
              components[3] = 0;
            }
          }
        } else if (
          MatchDateTimePattern(
            DataUtilities.ToLowerCaseAscii(text),
            null,
            new string[] {
              "%h:%m:%sp.m.",
              // PM clock values
              "%h:%m:%spm", "%h:%mp.m.", "%h:%mpm", "%hp.m.", "%hpm",
            },
            components,
            false,
            !haveTime,
            false)) { // check PM time formats
          if (components[3] != Int32.MinValue) {
            haveTime = true;
            // convert PM hour to 24-hour clock
            if (components[3] < 12) {
              components[3] += 12;
            }
          }
        }
      }
      return (components[0] != Int32.MinValue) ?
        ToDateTimeString(components) : GetDTValueContent(root);
    }

    private static string GetDTValueContent(IElement valueElement) {
      string elname = ElementName(valueElement);
      string text = String.Empty;
      if (HasClassName(valueElement, "value-title")) {
        return OrEmpty(valueElement.GetAttribute("title"));
      } else if (elname.Equals("img", StringComparison.Ordinal) ||
        elname.Equals("area", StringComparison.Ordinal)) {
        string s = valueElement.GetAttribute("alt");
        text = (s == null) ? String.Empty : s;
      } else if (elname.Equals("Data", StringComparison.Ordinal)) {
        string s = valueElement.GetAttribute("value");
        text = (s == null) ? GetTrimmedTextContent(valueElement) : s;
      } else if (elname.Equals("abbr", StringComparison.Ordinal)) {
        string s = valueElement.GetAttribute("title");
        text = (s == null) ? GetTrimmedTextContent(valueElement) : s;
      } else if (elname.Equals("del", StringComparison.Ordinal) ||
        elname.Equals("ins", StringComparison.Ordinal) ||
        elname.Equals("time", StringComparison.Ordinal)) {
        string s = valueElement.GetAttribute("datetime");
        if (StringUtility.IsNullOrSpaces(s)) {
          s = valueElement.GetAttribute("title");
        }
        text = (s == null) ? GetTrimmedTextContent(valueElement) : s;
      } else {
        text = GetTrimmedTextContent(valueElement);
      }
      return text;
    }

    private static string GetEValue(IElement root) {
      return root.GetInnerHTML();
    }

    private static IElement GetFirstChildElement(INode e) {
      foreach (var child in e.GetChildNodes()) {
        if (child is IElement) {
          return (IElement)child;
        }
      }
      return null;
    }

    private static string GetHref(IElement node) {
      string name = DataUtilities.ToLowerCaseAscii(node.GetLocalName());
      string href = String.Empty;
      if ("a".Equals(name, StringComparison.Ordinal) ||
        "link".Equals(name, StringComparison.Ordinal) ||
        "area".Equals(name, StringComparison.Ordinal)) {
        href = node.GetAttribute("href");
      } else if ("object".Equals(name, StringComparison.Ordinal)) {
        href = node.GetAttribute("Data");
      } else if ("img".Equals(name, StringComparison.Ordinal) ||
        "source".Equals(name, StringComparison.Ordinal) ||
        "track".Equals(name, StringComparison.Ordinal) ||
        "iframe".Equals(name, StringComparison.Ordinal) ||
        "audio".Equals(name, StringComparison.Ordinal) ||
        "video".Equals(name, StringComparison.Ordinal) ||
        "embed".Equals(name, StringComparison.Ordinal)) {
        href = node.GetAttribute("src");
      } else {
        return null;
      }
      if (String.IsNullOrEmpty(href)) {
        return String.Empty;
      }
      href = HtmlCommon.ResolveURL(node, href, null);
      return String.IsNullOrEmpty(href) ? String.Empty : href;
    }

    private static int[] GetLastKnownTime(CBORObject obj) {
      if (obj.ContainsKey("start")) {
        CBORObject arr = obj["start"];
        // Console.WriteLine("start %s",arr);
        Object result = arr[arr.Count - 1];
        if (result is string) {
          var components = new int[] {
            Int32.MinValue,
            Int32.MinValue,
            Int32.MinValue,
            Int32.MinValue,
            Int32.MinValue,
            Int32.MinValue,
            Int32.MinValue,
            Int32.MinValue,
          };
          if (
            MatchDateTimePattern(
              (string)result,
              new string[] { "%Y-%M-%d", "%Y-%D" },
              new string[] {
                "%H:%m:%s", "%H:%m",
                "%H:%m:%s%Z:%z",
                "%H:%m:%s%Z%z", "%H:%m:%s%G",
                "%H:%m%Z:%z", "%H:%m%Z%z", "%H:%m%G",
              },
              components,
              true,
              true,
              true)) {
            // reset the time components
            components[3] = Int32.MinValue;
            components[4] = Int32.MinValue;
            components[5] = Int32.MinValue;
            components[6] = Int32.MinValue;
            components[7] = Int32.MinValue;
            // Console.WriteLine("match %s",Arrays.ToString(components));
            return components;
          } else {
            // Console.WriteLine("no match");
          }
        }
      }
      return null;
    }

    /// <summary>Scans an HTML document for Microformats.org metadata. The
    /// resulting object will contain an "items" property, an array of all
    /// Microformats items. Each item will have a "type" and "properties"
    /// properties.</summary>
    /// <param name='root'>The document to scan.</param>
    /// <returns>A JSON object containing Microformats metadata.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='root'/> is null.</exception>
    public static CBORObject GetMicroformatsJSON(IDocument root) {
      if (root == null) {
        throw new ArgumentNullException(nameof(root));
      }
      return GetMicroformatsJSON(root.GetDocumentElement());
    }

    /// <summary>Scans an HTML element for Microformats.org metadata. The
    /// resulting object will contain an "items" property, an array of all
    /// Microformats items. Each item will have a "type" and "properties"
    /// properties.</summary>
    /// <param name='root'>The element to scan.</param>
    /// <returns>A JSON object containing Microformats metadata.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='root'/> is null.</exception>
    public static CBORObject GetMicroformatsJSON(IElement root) {
      if (root == null) {
        throw new ArgumentNullException(nameof(root));
      }
      var obj = CBORObject.NewMap();
      var items = CBORObject.NewArray();
      PropertyWalk(root, null, items);
      obj.Add("items", items);
      return obj;
    }

    private static int[] GetMonthAndDay(int year, int day) {
      #if DEBUG
      if (!(day >= 0)) {
        throw new InvalidOperationException("doesn't satisfy day>= 0");
      }
      #endif
      int[] dayArray = ((year & 3) != 0 || (year % 100 == 0 && year % 400 !=
        0)) ? ValueNormalDays : ValueLeapDays;
      var month = 1;
      while (day <= 0 || day > dayArray[month]) {
        if (day > dayArray[month]) {
          day -= dayArray[month];
          ++month;
          if (month > 12) {
            return null;
          }
        }
        if (day <= 0) {
          --month;
          if (month < 1) {
            return null;
          }
          day += dayArray[month];
        }
      }
      #if DEBUG
      if (!(month >= 1)) {
        throw new InvalidOperationException(month + " " + day);
      }
      if (!(month <= 12)) {
        throw new InvalidOperationException(month + " " + day);
      }
      if (!(day >= 1)) {
        throw new InvalidOperationException(month + " " + day);
      }
      if (!(day <= 31)) {
        throw new InvalidOperationException(month + " " + day);
      }
      #endif
      return new int[] { month, day };
    }

    private static string GetPValue(IElement root) {
      if (root.GetAttribute("title") != null) {
        return root.GetAttribute("title");
      }
      return (DataUtilities.ToLowerCaseAscii(root.GetLocalName()).Equals(
        "img",
        StringComparison.Ordinal) &&
        !StringUtility.IsNullOrSpaces(root.GetAttribute("alt"))) ?
        root.GetAttribute("alt") : GetValueContent(root, false);
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='root'>The parameter <paramref name='root'/> is
    /// a.Upokecenter.Html.IDocument object.</param>
    /// <returns>A CBORObject object.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='root'/> is null.</exception>
    public static CBORObject GetRelJSON(IDocument root) {
      if (root == null) {
        throw new ArgumentNullException(nameof(root));
      }
      return GetRelJSON(root.GetDocumentElement());
    }

    /// <summary>Not documented yet.</summary>
    /// <param name='root'>The parameter <paramref name='root'/> is
    /// a.Upokecenter.Html.IElement object.</param>
    /// <returns>A CBORObject object.</returns>
    /// <exception cref='ArgumentNullException'>The parameter <paramref
    /// name='root'/> is null.</exception>
    public static CBORObject GetRelJSON(IElement root) {
      if (root == null) {
        throw new ArgumentNullException(nameof(root));
      }
      var obj = CBORObject.NewMap();
      var items = CBORObject.NewArray();
      var item = CBORObject.NewMap();
      AccumulateValue(item, "type", "rel");
      var props = CBORObject.NewMap();
      RelWalk(root, props);
      item.Add("properties", props);
      items.Add(item);
      obj.Add("items", items);
      return obj;
    }

    private static string[] GetRelNames(IElement element) {
      string[] ret = StringUtility.SplitAtSpTabCrLfFf(
          DataUtilities.ToLowerCaseAscii(element.GetAttribute("rel")));
      if (ret.Length == 0) {
        return ret;
      }
      IList<string> retList = new List<string>();
      foreach (var element2 in ret) {
        retList.Add(element2);
      }
      if (retList.Count >= 2) {
        var stringSet = new HashSet<string>(retList);
        retList = new List<string>((ICollection<string>)stringSet);
      }
      return retList.ToArray();
    }

    private static string TrimAndCollapseSpaces(string str) {
      if (String.IsNullOrEmpty(str)) {
        return str;
      }
      var index = 0;
      int valueSLength = str.Length;
      var state = 0;
      StringBuilder sb = null;
      if (str.Length < 512) {
        while (index < valueSLength) {
          char c = str[index];
          if (c > 0x20 || (c != 0x09 && c != 0x20 && c != 0x0d && c != 0x0a &&
            c != 0x0c)) {
            ++index;
          } else {
            break;
          }
        }
        if (index == valueSLength) {
          return str;
        }
        sb = new StringBuilder();
        sb.Append(str, 0, index);
      } else {
        sb = new StringBuilder();
      }
      while (index < valueSLength) {
        char c = str[index];
        if (c > 0x20 || (c != 0x09 && c != 0x20 && c != 0x0d && c != 0x0a &&
          c != 0x0c)) {
          if (state > 0) {
            sb.Append(' ');
            sb.Append(c);
          } else {
            sb.Append(c);
          }
          state = 1;
        }
        ++index;
      }
      return sb.ToString();
    }

    private static string GetTrimmedTextContent(IElement element) {
      return TrimAndCollapseSpaces(element.GetTextContent());
    }

    /// <summary>Gets a Microformats "u-*" value from an HTML element. It
    /// tries to find the URL from the element's attributes, if possible;
    /// otherwise from the element's text.</summary>
    /// <param name='e'>An HTML element.</param>
    /// <returns>A URL, or the empty stringValue if none was
    /// found.</returns>
    private static string GetUValue(IElement e) {
      string url = GetHref(e);
      if (String.IsNullOrEmpty(url)) {
        url = GetTrimmedTextContent(e);
        if (URIUtility.IsValidIRI(url)) {
          return url;
        } else {
          return String.Empty;
        }
      }
      return url;
    }

    private static IList<IElement> GetValueClasses(IElement root) {
      IList<IElement> elements = new List<IElement>();
      foreach (var element in GetChildElements(root)) {
        GetValueClassInner(element, elements);
      }
      return elements;
    }

    private static void GetValueClassInner(
      IElement root,
      IList<IElement> elements) {
      string[] cls = GetClassNames(root);
      // Check if this is a value
      foreach (var c in cls) {
        if (c.Equals("value", StringComparison.Ordinal)) {
          elements.Add(root);
          return;
        } else if (c.Equals("value-title", StringComparison.Ordinal)) {
          elements.Add(root);
          return;
        }
      }
      // Not a value; check if this is a property
      foreach (var c in cls) {
        if (c.StartsWith("p-", StringComparison.Ordinal) ||
          c.StartsWith("e-", StringComparison.Ordinal) ||
          c.StartsWith("dt-", StringComparison.Ordinal) ||
          c.StartsWith("u-", StringComparison.Ordinal)) {
          // don't traverse
          return;
        }
      }
      foreach (var element in GetChildElements(root)) {
        GetValueClassInner(element, elements);
      }
    }

    private static string GetValueContent(IElement root, bool dt) {
      IList<IElement> elements = GetValueClasses(root);
      if (elements.Count == 0) {
        // No value elements, get the text content
        return GetValueElementContent(root);
      } else if (elements.Count == 1) {
        // One value element
        IElement valueElement = elements[0];
        return GetValueElementContent(valueElement);
      } else {
        var builder = new StringBuilder();
        var first = true;
        foreach (var element in elements) {
          if (!first) {
            builder.Append(' ');
          }
          first = false;
          builder.Append(GetValueElementContent(element));
        }
        return builder.ToString();
      }
    }

    private static string GetValueElementContent(IElement valueElement) {
      if (HasClassName(valueElement, "value-title")) {
        // If element has the value-title class, use
        // the title instead
        return OrEmpty(valueElement.GetAttribute("title"));
      } else if (ElementName(valueElement).Equals("img",
        StringComparison.Ordinal) || ElementName(valueElement).Equals(
          "area",
          StringComparison.Ordinal)) {
        string s = valueElement.GetAttribute("alt");
        return (s == null) ? String.Empty : s;
      } else if (ElementName(valueElement).Equals("Data",
        StringComparison.Ordinal)) {
        string s = valueElement.GetAttribute("value");
        return (s == null) ? GetTrimmedTextContent(valueElement) : s;
      } else if (ElementName(valueElement).Equals("abbr",
        StringComparison.Ordinal)) {
        string s = valueElement.GetAttribute("title");
        return (s == null) ? GetTrimmedTextContent(valueElement) : s;
      } else {
        return GetTrimmedTextContent(valueElement);
      }
    }

    private static bool HasClassName(IElement e, string className) {
      string attr = e.GetAttribute("class");
      if (attr == null || attr.Length < className.Length) {
        return false;
      }
      string[] cls = StringUtility.SplitAtSpTabCrLfFf(attr);
      foreach (var c in cls) {
        if (c.Equals(className, StringComparison.Ordinal)) {
          return true;
        }
      }
      return false;
    }

    private static bool HasSingleChildElementNamed(INode e, string name) {
      var seen = false;
      foreach (var child in e.GetChildNodes()) {
        if (child is IElement) {
          if (seen) {
            return false;
          }
          if (!DataUtilities.ToLowerCaseAscii(((IElement)child).GetLocalName())
            .Equals(name)) {
            return false;
          }
          seen = true;
        }
      }
      return seen;
    }

    private static bool ImplyForLink(
      IElement root,
      CBORObject subProperties) {
      if (DataUtilities.ToLowerCaseAscii(root.GetLocalName()).Equals("a",
        StringComparison.Ordinal) && root.GetAttribute("href") != null) {
        // get the link's URL
        SetValueIfAbsent(subProperties, "url", GetUValue(root));
        IList<IElement> elements = GetChildElements(root);
        if (elements.Count == 1 &&
          DataUtilities.ToLowerCaseAscii(elements[0].GetLocalName()).Equals(
            "img",
            StringComparison.Ordinal)) {
          // try to get the ALT/TITLE
          // from the image
          string valuePValue = GetPValue(elements[0]);
          if (StringUtility.IsNullOrSpaces(valuePValue)) {
            valuePValue = GetPValue(root); // if empty, get text from link
            // instead
          }
          SetValueIfAbsent(subProperties, "name", valuePValue);
          // get the SRC of the image
          SetValueIfAbsent(subProperties, "photo", GetUValue(elements[0]));
        } else {
          // get the text content
          string pvalue = GetPValue(root);
          if (!StringUtility.IsNullOrSpaces(pvalue)) {
            SetValueIfAbsent(subProperties, "name", pvalue);
          }
        }
        return true;
      }
      return false;
    }

    private static int IsDatePattern(
      string value,
      int index,
      string pattern,
      int[] components) {
      int[] c = components;
      c[0] = c[1] = c[2] = c[3] = c[4] = c[5] = c[6] = c[7] = Int32.MinValue;
      if (pattern == null) {
        throw new ArgumentNullException(nameof(pattern));
      }
      if (value == null) {
        return -1;
      }
      var patternValue = 0;
      int valueIndex = index;
      for (int patternIndex = 0; patternIndex < pattern.Length;
        patternIndex++) {
        if (valueIndex >= value.Length) {
          return -1;
        }
        char vc;
        char pc = pattern[patternIndex];
        if (pc == '%') {
          ++patternIndex;
          if (patternIndex >= pattern.Length) {
            return -1;
          }
          pc = pattern[patternIndex];
          if (pc == 'D') {
            // day of year -- expect three digits
            if (valueIndex + 3 > value.Length) {
              return -1;
            }
            vc = value[valueIndex++];
            if (vc < '0' || vc > '9') {
              return -1;
            }
            patternValue = vc - '0';
            vc = value[valueIndex++];
            if (vc < '0' || vc > '9') {
              return -1;
            }
            patternValue = (patternValue * 10) + (vc - '0');
            vc = value[valueIndex++];
            if (vc < '0' || vc > '9') {
              return -1;
            }
            patternValue = (patternValue * 10) + (vc - '0');
            if (patternValue > 366) {
              return -1;
            }
            components[2] = patternValue;
          } else if (pc == 'Y') {
            // year -- expect four digits
            if (valueIndex + 4 > value.Length) {
              return -1;
            }
            vc = value[valueIndex++];
            if (vc < '0' || vc > '9') {
              return -1;
            }
            patternValue = vc - '0';
            vc = value[valueIndex++];
            if (vc < '0' || vc > '9') {
              return -1;
            }
            patternValue = (patternValue * 10) + (vc - '0');
            vc = value[valueIndex++];
            if (vc < '0' || vc > '9') {
              return -1;
            }
            patternValue = (patternValue * 10) + (vc - '0');
            vc = value[valueIndex++];
            if (vc < '0' || vc > '9') {
              return -1;
            }
            patternValue = (patternValue * 10) + (vc - '0');
            components[0] = patternValue;
          } else if (pc == 'G') { // expect 'Z'
            if (valueIndex + 1 > value.Length) {
              return -1;
            }
            vc = value[valueIndex++];
            if (vc != 'Z') {
              return -1;
            }
            components[6] = 0; // time zone offset is 0
            components[7] = 0;
          } else if (pc == '%') { // expect 'Z'
            if (valueIndex + 1 > value.Length) {
              return -1;
            }
            vc = value[valueIndex++];
            if (vc != '%') {
              return -1;
            }
          } else if (pc == 'Z') { // expect plus or minus, then two digits
            if (valueIndex + 3 > value.Length) {
              return -1;
            }
            var negative = false;
            vc = value[valueIndex++];
            if (vc != '+' && vc != '-') {
              return -1;
            }
            negative = vc == '-';
            vc = value[valueIndex++];
            if (vc < '0' || vc > '9') {
              return -1;
            }
            patternValue = vc - '0';
            vc = value[valueIndex++];
            if (vc < '0' || vc > '9') {
              return -1;
            }
            patternValue = (patternValue * 10) + (vc - '0');
            if (pc == 'Z' && patternValue > 12) {
              return -1; // time zone offset hour
            }
            if (negative) {
              patternValue = -patternValue;
            }
            components[6] = patternValue;
          } else if (pc == 'M' || pc == 'd' || pc == 'H' || pc == 'h' ||
            pc == 'm' || pc == 's' || pc == 'z') { // expect two digits
            if (valueIndex + 2 > value.Length) {
              return -1;
            }
            vc = value[valueIndex++];
            if (vc < '0' || vc > '9') {
              return -1;
            }
            patternValue = vc - '0';
            vc = value[valueIndex++];
            if (vc < '0' || vc > '9') {
              return -1;
            }
            patternValue = (patternValue * 10) + (vc - '0');
            if (pc == 'M' && patternValue > 12) {
              return -1;
            } else if (pc == 'M') {
              components[1] = patternValue; // month
            } else if (pc == 'd' && patternValue > 31) {
              return -1;
            } else if (pc == 'd') {
              components[2] = patternValue; // day
            } else if (pc == 'H' && patternValue >= 24) {
              return -1;
            } else if (pc == 'H') {
              components[3] = patternValue; // hour
            } else if (pc == 'h' && patternValue >= 12 && patternValue != 0) {
              return -1;
            } else if (pc == 'h') {
              components[3] = patternValue; // hour (12-hour clock)
            } else if (pc == 'm' && patternValue >= 60) {
              return -1;
            } else if (pc == 'm') {
              components[4] = patternValue; // minute
            } else if (pc == 's' && patternValue > 60) {
              return -1;
            } else if (pc == 's') {
              components[5] = patternValue; // second
            } else if (pc == 'z' && patternValue >= 60) {
              return -1;
            } else if (pc == 'z') {
              components[7] = patternValue; // timezone offset minute
            }
          } else {
            return -1;
          }
        } else {
          vc = value[valueIndex++];
          if (vc != pc) {
            return -1;
          }
        }
      }
      // Special case: day of year
      if (components[2] != Int32.MinValue && components[0] != Int32.MinValue &&
        components[1] == Int32.MinValue) {
        int[] monthDay = GetMonthAndDay(components[0], components[2]);
        // Console.WriteLine("monthday %d->%d %d"
        // , components[2], monthDay[0], monthDay[1]);
        if (monthDay == null) {
          return -1;
        }
        components[1] = monthDay[0];
        components[2] = monthDay[1];
      }
      if (components[3] != Int32.MinValue && components[4] == Int32.MinValue) {
        components[4] = 0;
      }
      if (components[4] != Int32.MinValue && components[5] == Int32.MinValue) {
        components[5] = 0;
      }
      // Special case: time zone offset
      if (components[6] != Int32.MinValue && components[7] == Int32.MinValue) {
        // Console.WriteLine("spcase");
        components[7] = 0;
      }
      return valueIndex;
    }

    private static bool MatchDateTimePattern(
      string value,
      string[] datePatterns,
      string[] timePatterns,
      int[] components,
      bool useDate,
      bool useTime,
      bool useTimezone) {
      // year, month, day, hour, minute, second, zone offset,
      // zone offset minutes
      if (!useDate && !useTime && !useTimezone) {
        return false;
      }
      var c = new int[8];
      var c2 = new int[8];
      var index = 0;
      int oldIndex = index;
      if (datePatterns != null) {
        // match the date patterns, if any
        foreach (var pattern in datePatterns) {
          // reset components
          int endIndex = IsDatePattern(value, index, pattern, c);
          if (endIndex >= 0) {
            // copy any matching components
            if (endIndex >= value.Length) {
              CopyComponents(
                c,
                components,
                useDate,
                useTime,
                useTimezone);
              // we have just a date
              return true;
            }
            // match the T
            if (value[endIndex] != 'T') {
              return false;
            }
            index = endIndex + 1;
            break;
          }
        }
        if (index == oldIndex) {
          return false;
        }
      } else {
        // Won't match date patterns, so reset all components
        // instead
        c[0] = c[1] = c[2] = c[3] = c[4] = c[5] = c[6] = c[7] = Int32.MinValue;
      }
      // match the time pattern
      foreach (var pattern in timePatterns) {
        // reset components
        int endIndex = IsDatePattern(value, index, pattern, c2);
        if (endIndex == value.Length) {
          // copy any matching components
          CopyComponents(
            c,
            components,
            useDate,
            useTime,
            useTimezone);
          CopyComponents(
            c2,
            components,
            useDate,
            useTime,
            useTimezone);
          return true;
        }
      }
      return false;
    }

    private static string[] ParseLegacyRel(string str) {
      string[] ret = StringUtility.SplitAtSpTabCrLfFf(
          DataUtilities.ToLowerCaseAscii(str));
      if (ret.Length == 0) {
        return ret;
      }
      IList<string> relList = new List<string>();
      var hasTag = false;
      var hasSelf = false;
      var hasBookmark = false;
      foreach (var element in ret) {
        if (!hasTag && "tag".Equals(element, StringComparison.Ordinal)) {
          relList.Add("p-category");
          hasTag = true;
        } else if (!hasSelf && "self".Equals(element,
          StringComparison.Ordinal)) {
          if (hasBookmark) {
            relList.Add("u-url");
          }
          hasSelf = true;
        } else if (!hasBookmark && "bookmark".Equals(element,
          StringComparison.Ordinal)) {
          if (hasSelf) {
            relList.Add("u-url");
          }
          hasBookmark = true;
        }
      }
      return relList.ToArray();
    }

    private static void PropertyWalk(
      IElement root,
      CBORObject properties,
      CBORObject children) {
      string[] classNameArray = GetClassNames(root);
      if (classNameArray.Length > 0) {
        IList<string> types = new List<string>();
        var hasProperties = false;
        foreach (var cls in classNameArray) {
          if (cls.StartsWith("p-", StringComparison.Ordinal) && properties !=
            null) {
            hasProperties = true;
          } else if (cls.StartsWith("u-", StringComparison.Ordinal) &&
            properties != null) {
            hasProperties = true;
          } else if (cls.StartsWith("dt-", StringComparison.Ordinal) &&
            properties != null) {
            hasProperties = true;
          } else if (cls.StartsWith("e-", StringComparison.Ordinal) &&
            properties != null) {
            hasProperties = true;
          } else if (cls.StartsWith("h-", StringComparison.Ordinal)) {
            types.Add(cls);
          }
        }
        if (types.Count == 0 && hasProperties) {
          // has properties and isn't a microformat
          // root
          foreach (var cls in classNameArray) {
            if (cls.StartsWith("p-", StringComparison.Ordinal)) {
              string value = GetPValue(root);
              if (!StringUtility.IsNullOrSpaces(value)) {
                AccumulateValue(properties, cls.Substring(2), value);
              }
            } else if (cls.StartsWith("u-", StringComparison.Ordinal)) {
              AccumulateValue(
                properties,
                cls.Substring(2),
                GetUValue(root));
            } else if (cls.StartsWith("dt-", StringComparison.Ordinal)) {
              AccumulateValue(
                properties,
                cls.Substring(3),
                GetDTValue(root, GetLastKnownTime(properties)));
            } else if (cls.StartsWith("e-", StringComparison.Ordinal)) {
              AccumulateValue(
                properties,
                cls.Substring(2),
                GetEValue(root));
            }
          }
        } else if (types.Count > 0) {
          // this is a child microformat
          // with no properties
          var obj = CBORObject.NewMap();
          obj["type"] = CBORObject.FromObject(types);
          // for holding child elements with
          // properties
          var subProperties = CBORObject.NewMap();
          // for holding child microformats with no
          // property class
          var subChildren = CBORObject.NewArray();
          foreach (var child in root.GetChildNodes()) {
            if (child is IElement) {
              PropertyWalk(
                (IElement)child,
                subProperties,
                subChildren);
            }
          }
          if (subChildren.Count > 0) {
            obj.Add("children", subChildren);
          }
          if (types.Count > 0) {
            // we imply missing properties here
            // Imply p-name and p-url
            if (!ImplyForLink(root, subProperties)) {
              if (HasSingleChildElementNamed(root, "a")) {
                ImplyForLink(GetFirstChildElement(root), subProperties);
              } else {
                string pvalue = GetPValue(root);
                if (!StringUtility.IsNullOrSpaces(pvalue)) {
                  SetValueIfAbsent(subProperties, "name", pvalue);
                }
              }
            }
            // Also imply u-photo
            if (DataUtilities.ToLowerCaseAscii(root.GetLocalName()).Equals(
              "img",
              StringComparison.Ordinal) && root.GetAttribute("src") !=
              null) {
              SetValueIfAbsent(subProperties, "photo", GetUValue(root));
            }
            if (!subProperties.ContainsKey("photo")) {
              IList<IElement> images = root.GetElementsByTagName("img");
              // If there is only one descendant image, imply
              // u-photo
              if (images.Count == 1) {
                SetValueIfAbsent(
                  subProperties,
                  "photo",
                  GetUValue(images[0]));
              }
            }
          }
          obj.Add("properties", subProperties);
          if (hasProperties) {
            foreach (var cls in classNameArray) {
              if (cls.StartsWith("p-", StringComparison.Ordinal)) { // property
                CBORObject clone = CopyJson(obj);
                clone.Add("value", GetPValue(root));
                AccumulateValue(properties, cls.Substring(2), clone);
              } else if (cls.StartsWith("u-", StringComparison.Ordinal)) {
                // URL
                CBORObject clone = CopyJson(obj);
                clone.Add("value", GetUValue(root));
                AccumulateValue(properties, cls.Substring(2), clone);
              } else if (cls.StartsWith("dt-", StringComparison.Ordinal)) {
                // date/time
                CBORObject clone = CopyJson(obj);
                {
                  object objectTemp = "value";
                  object objectTemp2 = GetDTValue(
                      root,
                      GetLastKnownTime(properties));
                  clone.Add(objectTemp, objectTemp2);
                }
                AccumulateValue(properties, cls.Substring(3), clone);
              } else if (cls.StartsWith("e-", StringComparison.Ordinal)) {
                // date/time
                CBORObject clone = CopyJson(obj);
                clone.Add("value", GetEValue(root));
                AccumulateValue(properties, cls.Substring(2), clone);
              }
            }
          } else {
            children.Add(obj);
          }
          return;
        }
      }
      foreach (var child in root.GetChildNodes()) {
        if (child is IElement) {
          PropertyWalk((IElement)child, properties, children);
        }
      }
    }

    private static void RelWalk(
      IElement root,
      CBORObject properties) {
      string[] classNameArray = GetRelNames(root);
      if (classNameArray.Length > 0) {
        string href = GetHref(root);
        if (!StringUtility.IsNullOrSpaces(href)) {
          foreach (var cls in classNameArray) {
            AccumulateValue(properties, cls, href);
          }
        }
      }
      foreach (var child in root.GetChildNodes()) {
        if (child is IElement) {
          RelWalk((IElement)child, properties);
        }
      }
    }

    private static void SetValueIfAbsent(
      CBORObject obj,
      string key,
      Object value) {
      if (!obj.ContainsKey(key)) {
        CBORObject arr = null;
        arr = CBORObject.NewArray();
        obj.Add(key, arr);
        arr.Add(value);
      }
    }

    private static string ToDateTimeString(int[] components) {
      var builder = new StringBuilder();
      if (components[0] != Int32.MinValue) { // has a date
        // add year
        Append4d(builder, components[0]);
        builder.Append('-');
        if (components[1] == Int32.MinValue) {
          Append3d(builder, components[2]); // year and day of year
        } else { // has month
          // add month and day
          Append2d(builder, components[1]);
          builder.Append('-');
          Append2d(builder, components[2]);
        }
        // add T if there is a time
        if (components[3] != Int32.MinValue) {
          builder.Append('T');
        }
      }
      if (components[3] != Int32.MinValue) {
        Append2d(builder, components[3]);
        builder.Append(':');
        Append2d(builder, components[4]);
        builder.Append(':');
        Append2d(builder, components[5]);
      }
      if (components[6] != Int32.MinValue) {
        if (components[6] == 0 && components[7] == 0) {
          builder.Append('Z');
        } else if (components[6] < 0) { // negative time zone offset
          builder.Append('-');
          Append2d(builder, components[6]);
          Append2d(builder, components[7]);
        } else { // positive time zone offset
          builder.Append('+');
          Append2d(builder, components[6]);
          Append2d(builder, components[7]);
        }
      }
      return builder.ToString();
    }

    private static string OrEmpty(string s) {
      return s == null ? String.Empty : s;
    }

    private Microformats() {
    }
  }
}
