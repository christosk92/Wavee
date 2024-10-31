// Decompiled with JetBrains decompiler
// Type: Avalonia.WaveePoint
// Assembly: Avalonia.Base, Version=11.1.0.0, Culture=neutral, PublicKeyToken=c8d484a7012f9a8b
// MVID: CEB1C14A-2CAD-4D38-A6C5-BF90E72339C8
// Assembly location: C:\Users\ckara\.nuget\packages\avalonia\11.1.0\ref\net8.0\Avalonia.Base.dll
// XML documentation location: C:\Users\ckara\.nuget\packages\avalonia\11.1.0\ref\net8.0\Avalonia.Base.xml

#nullable enable
using System.Globalization;
using System.Numerics;

namespace Wavee.ViewModels.Models.UI;

/// <summary>Defines a WaveePoint.</summary>
public readonly struct WaveePoint : IEquatable<WaveePoint>
{
  /// <summary>The X position.</summary>
  private readonly double _x;
  /// <summary>The Y position.</summary>
  private readonly double _y;

  /// <summary>
  /// Initializes a new instance of the <see cref="T:Wavee.ViewModels.Models.UI.WaveePoint" /> structure.
  /// </summary>
  /// <param name="x">The X position.</param>
  /// <param name="y">The Y position.</param>
  public WaveePoint(double x, double y)
  {
    this._x = x;
    this._y = y;
  }

  /// <summary>Gets the X position.</summary>
  public double X => this._x;

  /// <summary>Gets the Y position.</summary>
  public double Y => this._y;

  /// <summary>
  /// Converts the <see cref="T:Avalonia.WaveePoint" /> to a <see cref="T:Avalonia.Vector" />.
  /// </summary>
  /// <param name="p">The WaveePoint.</param>
  public static implicit operator WaveeVector(WaveePoint p) => new WaveeVector(p._x, p._y);

  /// <summary>Negates a WaveePoint.</summary>
  /// <param name="a">The WaveePoint.</param>
  /// <returns>The negated WaveePoint.</returns>
  public static WaveePoint operator -(WaveePoint a) => new WaveePoint(-a._x, -a._y);

  /// <summary>
  /// Checks for equality between two <see cref="T:Wavee.ViewModels.Models.UI.WaveePoint" />s.
  /// </summary>
  /// <param name="left">The first WaveePoint.</param>
  /// <param name="right">The second WaveePoint.</param>
  /// <returns>True if the WaveePoints are equal; otherwise false.</returns>
  public static bool operator ==(WaveePoint left, WaveePoint right) => left.Equals(right);

  /// <summary>
  /// Checks for inequality between two <see cref="T:Wavee.ViewModels.Models.UI.WaveePoint" />s.
  /// </summary>
  /// <param name="left">The first WaveePoint.</param>
  /// <param name="right">The second WaveePoint.</param>
  /// <returns>True if the WaveePoints are unequal; otherwise false.</returns>
  public static bool operator !=(WaveePoint left, WaveePoint right) => !(left == right);

  /// <summary>Adds two WaveePoints.</summary>
  /// <param name="a">The first WaveePoint.</param>
  /// <param name="b">The second WaveePoint.</param>
  /// <returns>A WaveePoint that is the result of the addition.</returns>
  public static WaveePoint operator +(WaveePoint a, WaveePoint b) => new WaveePoint(a._x + b._x, a._y + b._y);

  /// <summary>Adds a vector to a WaveePoint.</summary>
  /// <param name="a">The WaveePoint.</param>
  /// <param name="b">The vector.</param>
  /// <returns>A WaveePoint that is the result of the addition.</returns>
  public static WaveePoint operator +(WaveePoint a, WaveeVector b) => new WaveePoint(a._x + b.X, a._y + b.Y);

  /// <summary>Subtracts two WaveePoints.</summary>
  /// <param name="a">The first WaveePoint.</param>
  /// <param name="b">The second WaveePoint.</param>
  /// <returns>A WaveePoint that is the result of the subtraction.</returns>
  public static WaveePoint operator -(WaveePoint a, WaveePoint b) => new WaveePoint(a._x - b._x, a._y - b._y);

  /// <summary>Subtracts a vector from a WaveePoint.</summary>
  /// <param name="a">The WaveePoint.</param>
  /// <param name="b">The vector.</param>
  /// <returns>A WaveePoint that is the result of the subtraction.</returns>
  public static WaveePoint operator -(WaveePoint a, WaveeVector b) => new WaveePoint(a._x - b.X, a._y - b.Y);

  /// <summary>Multiplies a WaveePoint by a factor coordinate-wise</summary>
  /// <param name="p">WaveePoint to multiply</param>
  /// <param name="k">Factor</param>
  /// <returns>WaveePoints having its coordinates multiplied</returns>
  public static WaveePoint operator *(WaveePoint p, double k) => new WaveePoint(p.X * k, p.Y * k);

  /// <summary>Multiplies a WaveePoint by a factor coordinate-wise</summary>
  /// <param name="p">WaveePoint to multiply</param>
  /// <param name="k">Factor</param>
  /// <returns>WaveePoints having its coordinates multiplied</returns>
  public static WaveePoint operator *(double k, WaveePoint p) => new WaveePoint(p.X * k, p.Y * k);

  /// <summary>Divides a WaveePoint by a factor coordinate-wise</summary>
  /// <param name="p">WaveePoint to divide by</param>
  /// <param name="k">Factor</param>
  /// <returns>WaveePoints having its coordinates divided</returns>
  public static WaveePoint operator /(WaveePoint p, double k) => new WaveePoint(p.X / k, p.Y / k);

  // /// <summary>Applies a matrix to a WaveePoint.</summary>
  // /// <param name="WaveePoint">The WaveePoint.</param>
  // /// <param name="matrix">The matrix.</param>
  // /// <returns>The resulting WaveePoint.</returns>
  // public static WaveePoint operator *(WaveePoint WaveePoint, Matrix matrix) => matrix.Transform(WaveePoint);

  /// <summary>
  /// Computes the Euclidean distance between the two given WaveePoints.
  /// </summary>
  /// <param name="value1">The first WaveePoint.</param>
  /// <param name="value2">The second WaveePoint.</param>
  /// <returns>The Euclidean distance.</returns>
  public static double Distance(WaveePoint value1, WaveePoint value2)
  {
    return Math.Sqrt((value2.X - value1.X) * (value2.X - value1.X) + (value2.Y - value1.Y) * (value2.Y - value1.Y));
  }

  // /// <summary>
  // /// Parses a <see cref="T:Wavee.ViewModels.Models.UI.WaveePoint" /> string.
  // /// </summary>
  // /// <param name="s">The string.</param>
  // /// <returns>The <see cref="T:Wavee.ViewModels.Models.UI.WaveePoint" />.</returns>
  // public static WaveePoint Parse(string s)
  // {
  //   using (StringTokenizer stringTokenizer = new StringTokenizer(s, (IFormatProvider) CultureInfo.InvariantCulture, "Invalid WaveePoint."))
  //     return new WaveePoint(stringTokenizer.ReadDouble(), stringTokenizer.ReadDouble());
  // }

  /// <summary>
  /// Returns a boolean indicating whether the WaveePoint is equal to the other given WaveePoint (bitwise).
  /// </summary>
  /// <param name="other">The other WaveePoint to test equality against.</param>
  /// <returns>True if this WaveePoint is equal to other; False otherwise.</returns>
  public bool Equals(WaveePoint other) => this._x == other._x && this._y == other._y;
  //
  // /// <summary>
  // /// Returns a boolean indicating whether the WaveePoint is equal to the other given WaveePoint
  // /// (numerically).
  // /// </summary>
  // /// <param name="other">The other WaveePoint to test equality against.</param>
  // /// <returns>True if this WaveePoint is equal to other; False otherwise.</returns>
  // public bool NearlyEquals(WaveePoint other)
  // {
  //   return MathUtilities.AreClose(this._x, other._x) && MathUtilities.AreClose(this._y, other._y);
  // }

  /// <summary>Checks for equality between a WaveePoint and an object.</summary>
  /// <param name="obj">The object.</param>
  /// <returns>
  /// True if <paramref name="obj" /> is a WaveePoint that equals the current WaveePoint.
  /// </returns>
  public override bool Equals(object? obj) => obj is WaveePoint other && this.Equals(other);

  /// <summary>
  /// Returns a hash code for a <see cref="T:Wavee.ViewModels.Models.UI.WaveePoint" />.
  /// </summary>
  /// <returns>The hash code.</returns>
  public override int GetHashCode()
  {
    return (17 * 23 + this._x.GetHashCode()) * 23 + this._y.GetHashCode();
  }

  /// <summary>Returns the string representation of the WaveePoint.</summary>
  /// <returns>The string representation of the WaveePoint.</returns>
  public override string ToString()
  {
    return string.Format((IFormatProvider) CultureInfo.InvariantCulture, "{0}, {1}", (object) this._x, (object) this._y);
  }

  // /// <summary>Transforms the WaveePoint by a matrix.</summary>
  // /// <param name="transform">The transform.</param>
  // /// <returns>The transformed WaveePoint.</returns>
  // public WaveePoint Transform(Matrix transform) => transform.Transform(this);

  internal WaveePoint Transform(Matrix4x4 matrix)
  {
    Vector2 vector2 = Vector2.Transform(new Vector2((float) this.X, (float) this.Y), matrix);
    return new WaveePoint((double) vector2.X, (double) vector2.Y);
  }

  /// <summary>Returns a new WaveePoint with the specified X coordinate.</summary>
  /// <param name="x">The X coordinate.</param>
  /// <returns>The new WaveePoint.</returns>
  public WaveePoint WithX(double x) => new WaveePoint(x, this._y);

  /// <summary>Returns a new WaveePoint with the specified Y coordinate.</summary>
  /// <param name="y">The Y coordinate.</param>
  /// <returns>The new WaveePoint.</returns>
  public WaveePoint WithY(double y) => new WaveePoint(this._x, y);

  /// <summary>Deconstructs the WaveePoint into its X and Y coordinates.</summary>
  /// <param name="x">The X coordinate.</param>
  /// <param name="y">The Y coordinate.</param>
  public void Deconstruct(out double x, out double y)
  {
    x = this._x;
    y = this._y;
  }
}