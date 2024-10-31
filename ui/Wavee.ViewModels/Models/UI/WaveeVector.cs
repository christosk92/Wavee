// Decompiled with JetBrains decompiler
// Type: Avalonia.WaveeVector
// Assembly: Avalonia.Base, Version=11.1.0.0, Culture=neutral, PublicKeyToken=c8d484a7012f9a8b
// MVID: CEB1C14A-2CAD-4D38-A6C5-BF90E72339C8
// Assembly location: C:\Users\ckara\.nuget\packages\avalonia\11.1.0\ref\net8.0\Avalonia.Base.dll
// XML documentation location: C:\Users\ckara\.nuget\packages\avalonia\11.1.0\ref\net8.0\Avalonia.Base.xml

#nullable enable
using System.Globalization;
using System.Numerics;

namespace Wavee.ViewModels.Models.UI;

/// <summary>Defines a WaveeVector.</summary>
public readonly struct WaveeVector : IEquatable<WaveeVector>
{
  /// <summary>The X component.</summary>
  private readonly double _x;
  /// <summary>The Y component.</summary>
  private readonly double _y;

  /// <summary>
  /// Initializes a new instance of the <see cref="T:Wavee.ViewModels.Models.UI.WaveeVector" /> structure.
  /// </summary>
  /// <param name="x">The X component.</param>
  /// <param name="y">The Y component.</param>
  public WaveeVector(double x, double y)
  {
    this._x = x;
    this._y = y;
  }

  /// <summary>Gets the X component.</summary>
  public double X => this._x;

  /// <summary>Gets the Y component.</summary>
  public double Y => this._y;

  /// <summary>
  /// Converts the <see cref="T:Avalonia.WaveeVector" /> to a <see cref="T:Avalonia.Point" />.
  /// </summary>
  /// <param name="a">The WaveeVector.</param>
  public static explicit operator WaveePoint(WaveeVector a) => new WaveePoint(a._x, a._y);

  /// <summary>Calculates the dot product of two WaveeVectors.</summary>
  /// <param name="a">First WaveeVector.</param>
  /// <param name="b">Second WaveeVector.</param>
  /// <returns>The dot product.</returns>
  public static double operator *(WaveeVector a, WaveeVector b) => WaveeVector.Dot(a, b);

  /// <summary>Scales a WaveeVector.</summary>
  /// <param name="WaveeVector">The WaveeVector.</param>
  /// <param name="scale">The scaling factor.</param>
  /// <returns>The scaled WaveeVector.</returns>
  public static WaveeVector operator *(WaveeVector WaveeVector, double scale) => WaveeVector.Multiply(WaveeVector, scale);

  /// <summary>Scales a WaveeVector.</summary>
  /// <param name="WaveeVector">The WaveeVector.</param>
  /// <param name="scale">The scaling factor.</param>
  /// <returns>The scaled WaveeVector.</returns>
  public static WaveeVector operator *(double scale, WaveeVector WaveeVector) => WaveeVector.Multiply(WaveeVector, scale);

  /// <summary>Scales a WaveeVector.</summary>
  /// <param name="WaveeVector">The WaveeVector.</param>
  /// <param name="scale">The divisor.</param>
  /// <returns>The scaled WaveeVector.</returns>
  public static WaveeVector operator /(WaveeVector WaveeVector, double scale) => WaveeVector.Divide(WaveeVector, scale);

  //TODO:
  // /// <summary>
  // /// Parses a <see cref="T:Wavee.ViewModels.Models.UI.WaveeVector" /> string.
  // /// </summary>
  // /// <param name="s">The string.</param>
  // /// <returns>The <see cref="T:Wavee.ViewModels.Models.UI.WaveeVector" />.</returns>
  // public static WaveeVector Parse(string s)
  // {
  //   using (StringTokenizer stringTokenizer = new StringTokenizer(s, (IFormatProvider) CultureInfo.InvariantCulture, "Invalid WaveeVector."))
  //     return new WaveeVector(stringTokenizer.ReadDouble(), stringTokenizer.ReadDouble());
  // }

  /// <summary>Length of the WaveeVector.</summary>
  public double Length => Math.Sqrt(this.SquaredLength);

  /// <summary>Squared Length of the WaveeVector.</summary>
  public double SquaredLength => this._x * this._x + this._y * this._y;

  /// <summary>Negates a WaveeVector.</summary>
  /// <param name="a">The WaveeVector.</param>
  /// <returns>The negated WaveeVector.</returns>
  public static WaveeVector operator -(WaveeVector a) => WaveeVector.Negate(a);

  /// <summary>Adds two WaveeVectors.</summary>
  /// <param name="a">The first WaveeVector.</param>
  /// <param name="b">The second WaveeVector.</param>
  /// <returns>A WaveeVector that is the result of the addition.</returns>
  public static WaveeVector operator +(WaveeVector a, WaveeVector b) => WaveeVector.Add(a, b);

  /// <summary>Subtracts two WaveeVectors.</summary>
  /// <param name="a">The first WaveeVector.</param>
  /// <param name="b">The second WaveeVector.</param>
  /// <returns>A WaveeVector that is the result of the subtraction.</returns>
  public static WaveeVector operator -(WaveeVector a, WaveeVector b) => WaveeVector.Subtract(a, b);

  /// <summary>Check if two WaveeVectors are equal (bitwise).</summary>
  /// <param name="other"></param>
  /// <returns></returns>
  public bool Equals(WaveeVector other) => this._x == other._x && this._y == other._y;

  // /// <summary>Check if two WaveeVectors are nearly equal (numerically).</summary>
  // /// <param name="other">The other WaveeVector.</param>
  // /// <returns>True if WaveeVectors are nearly equal.</returns>
  // public bool NearlyEquals(WaveeVector other)
  // {
  //   return MathUtilities.AreClose(this._x, other._x) && MathUtilities.AreClose(this._y, other._y);
  // }

  public override bool Equals(object? obj) => obj is WaveeVector other && this.Equals(other);

  public override int GetHashCode() => this._x.GetHashCode() * 397 ^ this._y.GetHashCode();

  public static bool operator ==(WaveeVector left, WaveeVector right) => left.Equals(right);

  public static bool operator !=(WaveeVector left, WaveeVector right) => !left.Equals(right);

  /// <summary>Returns the string representation of the WaveeVector.</summary>
  /// <returns>The string representation of the WaveeVector.</returns>
  public override string ToString()
  {
    return string.Format((IFormatProvider) CultureInfo.InvariantCulture, "{0}, {1}", (object) this._x, (object) this._y);
  }

  /// <summary>Returns a new WaveeVector with the specified X component.</summary>
  /// <param name="x">The X component.</param>
  /// <returns>The new WaveeVector.</returns>
  public WaveeVector WithX(double x) => new WaveeVector(x, this._y);

  /// <summary>Returns a new WaveeVector with the specified Y component.</summary>
  /// <param name="y">The Y component.</param>
  /// <returns>The new WaveeVector.</returns>
  public WaveeVector WithY(double y) => new WaveeVector(this._x, y);

  /// <summary>Returns a normalized version of this WaveeVector.</summary>
  /// <returns>The normalized WaveeVector.</returns>
  public WaveeVector Normalize() => WaveeVector.Normalize(this);

  /// <summary>Returns a negated version of this WaveeVector.</summary>
  /// <returns>The negated WaveeVector.</returns>
  public WaveeVector Negate() => WaveeVector.Negate(this);

  /// <summary>Returns the dot product of two WaveeVectors.</summary>
  /// <param name="a">The first WaveeVector.</param>
  /// <param name="b">The second WaveeVector.</param>
  /// <returns>The dot product.</returns>
  public static double Dot(WaveeVector a, WaveeVector b) => a._x * b._x + a._y * b._y;

  /// <summary>Returns the cross product of two WaveeVectors.</summary>
  /// <param name="a">The first WaveeVector.</param>
  /// <param name="b">The second WaveeVector.</param>
  /// <returns>The cross product.</returns>
  public static double Cross(WaveeVector a, WaveeVector b) => a._x * b._y - a._y * b._x;

  /// <summary>Normalizes the given WaveeVector.</summary>
  /// <param name="WaveeVector">The WaveeVector</param>
  /// <returns>The normalized WaveeVector.</returns>
  public static WaveeVector Normalize(WaveeVector WaveeVector) => WaveeVector.Divide(WaveeVector, WaveeVector.Length);

  /// <summary>Divides the first WaveeVector by the second.</summary>
  /// <param name="a">The first WaveeVector.</param>
  /// <param name="b">The second WaveeVector.</param>
  /// <returns>The scaled WaveeVector.</returns>
  public static WaveeVector Divide(WaveeVector a, WaveeVector b) => new WaveeVector(a._x / b._x, a._y / b._y);

  /// <summary>Divides the WaveeVector by the given scalar.</summary>
  /// <param name="WaveeVector">The WaveeVector</param>
  /// <param name="scalar">The scalar value</param>
  /// <returns>The scaled WaveeVector.</returns>
  public static WaveeVector Divide(WaveeVector WaveeVector, double scalar)
  {
    return new WaveeVector(WaveeVector._x / scalar, WaveeVector._y / scalar);
  }

  /// <summary>Multiplies the first WaveeVector by the second.</summary>
  /// <param name="a">The first WaveeVector.</param>
  /// <param name="b">The second WaveeVector.</param>
  /// <returns>The scaled WaveeVector.</returns>
  public static WaveeVector Multiply(WaveeVector a, WaveeVector b) => new WaveeVector(a._x * b._x, a._y * b._y);

  /// <summary>Multiplies the WaveeVector by the given scalar.</summary>
  /// <param name="WaveeVector">The WaveeVector</param>
  /// <param name="scalar">The scalar value</param>
  /// <returns>The scaled WaveeVector.</returns>
  public static WaveeVector Multiply(WaveeVector WaveeVector, double scalar)
  {
    return new WaveeVector(WaveeVector._x * scalar, WaveeVector._y * scalar);
  }

  /// <summary>Adds the second to the first WaveeVector</summary>
  /// <param name="a">The first WaveeVector.</param>
  /// <param name="b">The second WaveeVector.</param>
  /// <returns>The summed WaveeVector.</returns>
  public static WaveeVector Add(WaveeVector a, WaveeVector b) => new WaveeVector(a._x + b._x, a._y + b._y);

  /// <summary>Subtracts the second from the first WaveeVector</summary>
  /// <param name="a">The first WaveeVector.</param>
  /// <param name="b">The second WaveeVector.</param>
  /// <returns>The difference WaveeVector.</returns>
  public static WaveeVector Subtract(WaveeVector a, WaveeVector b) => new WaveeVector(a._x - b._x, a._y - b._y);

  /// <summary>Negates the WaveeVector</summary>
  /// <param name="WaveeVector">The WaveeVector to negate.</param>
  /// <returns>The scaled WaveeVector.</returns>
  public static WaveeVector Negate(WaveeVector WaveeVector) => new WaveeVector(-WaveeVector._x, -WaveeVector._y);

  /// <summary>Returns the WaveeVector (0.0, 0.0).</summary>
  public static WaveeVector Zero => new WaveeVector(0.0, 0.0);

  /// <summary>Returns the WaveeVector (1.0, 1.0).</summary>
  public static WaveeVector One => new WaveeVector(1.0, 1.0);

  /// <summary>Returns the WaveeVector (1.0, 0.0).</summary>
  public static WaveeVector UnitX => new WaveeVector(1.0, 0.0);

  /// <summary>Returns the WaveeVector (0.0, 1.0).</summary>
  public static WaveeVector UnitY => new WaveeVector(0.0, 1.0);

  /// <summary>Deconstructs the WaveeVector into its X and Y components.</summary>
  /// <param name="x">The X component.</param>
  /// <param name="y">The Y component.</param>
  public void Deconstruct(out double x, out double y)
  {
    x = this._x;
    y = this._y;
  }

  internal Vector2 ToVector2() => new Vector2((float) this.X, (float) this.Y);

  internal WaveeVector(Vector2 v)
    : this((double) v.X, (double) v.Y)
  {
  }

  /// <summary>
  /// Returns a WaveeVector whose elements are the absolute values of each of the specified WaveeVector's elements.
  /// </summary>
  /// <returns></returns>
  public WaveeVector Abs() => new WaveeVector(Math.Abs(this.X), Math.Abs(this.Y));

  /// <summary>
  /// Restricts a WaveeVector between a minimum and a maximum value.
  /// </summary>
  public static WaveeVector Clamp(WaveeVector value, WaveeVector min, WaveeVector max)
  {
    return WaveeVector.Min(WaveeVector.Max(value, min), max);
  }

  /// <summary>
  /// Returns a WaveeVector whose elements are the maximum of each of the pairs of elements in two specified WaveeVectors
  /// </summary>
  public static WaveeVector Max(WaveeVector left, WaveeVector right)
  {
    return new WaveeVector(Math.Max(left.X, right.X), Math.Max(left.Y, right.Y));
  }

  /// <summary>
  /// Returns a WaveeVector whose elements are the minimum of each of the pairs of elements in two specified WaveeVectors
  /// </summary>
  public static WaveeVector Min(WaveeVector left, WaveeVector right)
  {
    return new WaveeVector(Math.Min(left.X, right.X), Math.Min(left.Y, right.Y));
  }

  /// <summary>
  /// Computes the Euclidean distance between the two given points.
  /// </summary>
  /// <param name="value1">The first point.</param>
  /// <param name="value2">The second point.</param>
  /// <returns>The Euclidean distance.</returns>
  public static double Distance(WaveeVector value1, WaveeVector value2)
  {
    return Math.Sqrt(WaveeVector.DistanceSquared(value1, value2));
  }

  /// <summary>
  /// Returns the Euclidean distance squared between two specified points
  /// </summary>
  /// <param name="value1">The first point.</param>
  /// <param name="value2">The second point.</param>
  /// <returns>The Euclidean distance squared.</returns>
  public static double DistanceSquared(WaveeVector value1, WaveeVector value2)
  {
    WaveeVector WaveeVector = value1 - value2;
    return WaveeVector.Dot(WaveeVector, WaveeVector);
  }

  public static implicit operator WaveeVector(Vector2 v) => new(v);
}