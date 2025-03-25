using System;

// TODO: Write or find a tool to convert this code
// (or pseudocode therefor) to various programming
// languages, such as Java, Python, Ruby, PHP,
  // and Rust, without artificial intelligence.
// This code is simple enough for such a tool.
namespace PeterO {
  /// <summary>Helper functions for rectangles. In this class, a
  /// rectangle is an array of four numbers that give the rectangle's
  /// left edge, upper edge, right edge, and lower edge, in that
  /// order.</summary>
  public static class RectangleUtility {
    /// <summary>Normalizes a rectangle so that its width is never less
    /// than zero.</summary>
    /// <param name='rect'>A rectangle.</param>
    /// <returns>A rectangle in its normalized form. The returned array
    /// gives the rectangle's left edge, upper edge, right edge, and lower
    /// edge, in that order.</returns>
    public static double[] Normalize(double[] rect) {
      double left = rect[0];
      double right = rect[2];
      double top = rect[1];
      double bottom = rect[3];
      if (left > right) {
        left = rect[2];
        right = rect[0];
      }
      if (top > bottom) {
        top = rect[3];
        bottom = rect[1];
      }
      return new double[] { left, top, right, bottom};
    }

    /// <summary>Adjusts a rectangle so its contents fit within a specified
    /// screen rectangle.</summary>
    /// <param name='source'>A rectangle to be moved.</param>
    /// <param name='screen'>A rectangle that specifies the screen. The
    /// rectangle will be resized and moved as necessary to fit the
    /// screen.</param>
    /// <returns>A rectangle. The returned array gives the rectangle's left
    /// edge, upper edge, right edge, and lower edge, in that
    /// order.</returns>
    public static double[] FitRectangleToScreen(double[] source, double[]
      screen) {
      double left, top, right, bottom;
      double cSrc, cScreen, diff;
      source = Normalize(source);
      screen = Normalize(screen);
      cSrc = source[2] - source[0]; // width
      cScreen = screen[2] - screen[0]; // width
      if (cSrc > cScreen) {
        left = screen[0];
        right = screen[2];
      } else if (source[2] > screen[2]) {
        diff = source[2] - screen[2];
        left = source[0] - diff;
        right = screen[2];
      } else if (source[0] < screen[0]) {
        diff = screen[0] - source[0];
        left = screen[0];
        right = source[2] + diff;
      } else {
        left = source[0];
        right = source[2];
      }
      // y-axis
      cSrc = source[3] - source[1]; // height
      cScreen = screen[3] - screen[1]; // height
      if (cSrc > cScreen) {
        top = screen[1];
        bottom = screen[3];
      } else if (source[3] > screen[3]) {
        diff = source[3] - screen[3];
        top = source[1] - diff;
        bottom = screen[3];
      } else if (source[1] < screen[1]) {
        diff = screen[1] - source[1];
        top = screen[1];
        bottom = source[3] + diff;
      } else {
        top = source[1];
        bottom = source[3];
      }
      return new double[] { left, top, right, bottom};
    }

    /// <summary>Centers a rectangle to the screen. Its size will be
    /// adjusted to fit the screen.</summary>
    /// <param name='source'>A rectangle to be centered.</param>
    /// <param name='screen'>A rectangle that specifies the screen.</param>
    /// <returns>The centered rectangle. The returned array gives the
    /// rectangle's left edge, upper edge, right edge, and lower edge, in
    /// that order.</returns>
    public static double[] CenterRectangle(double[] source, double[]
      screen) {
      return CenterRectangle(source, screen, screen);
    }

    /// <summary>Centers a rectangle to a reference point. Its size and
    /// position will be adjusted to fit the screen.</summary>
    /// <param name='source'>A rectangle to be centered.</param>
    /// <param name='screen'>A rectangle that specifies the screen
    /// rectangle.</param>
    /// <param name='referenceX'>A number that specifies the reference
    /// point's x-coordinate.</param>
    /// <param name='referenceY'>A number that specifies the reference
    /// point's y-coordinate.</param>
    /// <returns>The centered rectangle. The returned array gives the
    /// rectangle's left edge, upper edge, right edge, and lower edge, in
    /// that order.</returns>
    public static double[] CenterRectangle(
      double[] source,
      double[] screen,
      double referenceX,
      double referenceY) {
      screen = Normalize(screen);
      source = FitRectangleToScreen(source, screen);
      double cx = source[2] - source[0]; // width
      double cy = source[3] - source[1]; // height
      double xLeft = referenceX - (cx / 2);
      double yTop = referenceY - (cy / 2);
      if (xLeft < 0) {
        xLeft = 0;
      } else if (xLeft + cx > screen[2]) {
        xLeft = screen[2] - cx;
      }
      if (yTop < 0) {
        yTop = 0;
      } else if (yTop + cy > screen[3]) {
        yTop = screen[3] - cy;
      }
      return new double[] { xLeft, yTop, xLeft + cx, yTop + cy};
    }

    /// <summary>Centers a rectangle to the center of a reference
    /// rectangle. Its size and position will be adjusted to fit the
    /// screen.</summary>
    /// <param name='source'>A rectangle to be centered.</param>
    /// <param name='screen'>A rectangle that specifies the screen
    /// rectangle.</param>
    /// <param name='reference'>Specifies the reference rectangle where the
    /// source will be placed. This rectangle need not fit the
    /// screen.</param>
    /// <returns>The centered rectangle. The returned array gives the
    /// rectangle's left edge, upper edge, right edge, and lower edge, in
    /// that order.</returns>
    public static double[] CenterRectangle(
      double[] source,
      double[] screen,
      double[] reference) {
      reference = Normalize(reference);
      double xMid = (reference[0] + reference[2]) / 2;
      double yMid = (reference[1] + reference[3]) / 2;
      return CenterRectangle(source, screen, xMid, yMid);
    }
  }
}
