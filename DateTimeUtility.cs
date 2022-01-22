using System;

namespace Com.Upokecenter.Util {
  // C# version of DateTimeUtility

  /// <summary>Not documented yet.</summary>
  public static class DateTimeUtility {
    /// <summary>Not documented yet.</summary>
    /// <returns>An array of 32-bit unsigned integers.</returns>
    public static int[] GetCurrentGmtDateComponents() {
      DateTime dt = DateTime.UtcNow;
      var ret = new int[8];
      ret[0] = dt.Year;
      ret[1] = dt.Month;
      ret[2] = dt.Day;
      ret[3] = dt.Hour;
      ret[4] = dt.Minute;
      ret[5] = dt.Second;
      ret[6] = dt.Millisecond;
      ret[8] = 0; // time zone offset is 0 for GMT
      DayOfWeek dow = dt.DayOfWeek;
      if (dow == DayOfWeek.Sunday) {
        ret[7] = 1;
      } else if (dow == DayOfWeek.Monday) {
        ret[7] = 2;
      } else if (dow == DayOfWeek.Tuesday) {
        ret[7] = 3;
      } else if (dow == DayOfWeek.Wednesday) {
        ret[7] = 4;
      } else if (dow == DayOfWeek.Thursday) {
        ret[7] = 5;
      } else if (dow == DayOfWeek.Friday) {
        ret[7] = 6;
      } else if (dow == DayOfWeek.Saturday) {
        ret[7] = 7;
      } else {
        ret[7] = 0;
      }
      return ret;
    }
  }
}
