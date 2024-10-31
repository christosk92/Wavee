// Decompiled with JetBrains decompiler
// Type: Avalonia.WaveeSize
// Assembly: Avalonia.Base, Version=11.1.0.0, Culture=neutral, PublicKeyToken=c8d484a7012f9a8b
// MVID: CEB1C14A-2CAD-4D38-A6C5-BF90E72339C8
// Assembly location: C:\Users\ckara\.nuget\packages\avalonia\11.1.0\ref\net8.0\Avalonia.Base.dll
// XML documentation location: C:\Users\ckara\.nuget\packages\avalonia\11.1.0\ref\net8.0\Avalonia.Base.xml

#nullable enable
using System.Globalization;
using System.Numerics;

namespace Wavee.ViewModels.Models.UI;

/// <summary>Defines a WaveeSize.</summary>
public readonly struct WaveeSize : IEquatable<WaveeSize>
{
  /// <summary>A WaveeSize representing infinity.</summary>
  public static readonly WaveeSize Infinity = new WaveeSize(double.PositiveInfinity, double.PositiveInfinity);
  /// <summary>The width.</summary>
  private readonly double _width;
  /// <summary>The height.</summary>
  private readonly double _height;

  /// <summary>
  /// Initializes a new instance of the <see cref="T:Wavee.ViewModels.Models.UI.WaveeSize" /> structure.
  /// </summary>
  /// <param name="width">The width.</param>
  /// <param name="height">The height.</param>
  public WaveeSize(double width, double height)
  {
    this._width = width;
    this._height = height;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="T:Wavee.ViewModels.Models.UI.WaveeSize" /> structure.
  /// </summary>
  /// <param name="vector2">The vector to take values from.</param>
  public WaveeSize(Vector2 vector2)
    : this((double) vector2.X, (double) vector2.Y)
  {
  }

  /// <summary>Gets the aspect ratio of the WaveeSize.</summary>
  public double AspectRatio => this._width / this._height;

  /// <summary>Gets the width.</summary>
  public double Width => this._width;

  /// <summary>Gets the height.</summary>
  public double Height => this._height;

  /// <summary>
  /// Checks for equality between two <see cref="T:Wavee.ViewModels.Models.UI.WaveeSize" />s.
  /// </summary>
  /// <param name="left">The first WaveeSize.</param>
  /// <param name="right">The second WaveeSize.</param>
  /// <returns>True if the WaveeSizes are equal; otherwise false.</returns>
  public static bool operator ==(WaveeSize left, WaveeSize right) => left.Equals(right);

  /// <summary>
  /// Checks for inequality between two <see cref="T:Wavee.ViewModels.Models.UI.WaveeSize" />s.
  /// </summary>
  /// <param name="left">The first WaveeSize.</param>
  /// <param name="right">The second WaveeSize.</param>
  /// <returns>True if the WaveeSizes are unequal; otherwise false.</returns>
  public static bool operator !=(WaveeSize left, WaveeSize right) => !(left == right);

  /// <summary>Scales a WaveeSize.</summary>
  /// <param name="WaveeSize">The WaveeSize</param>
  /// <param name="scale">The scaling factor.</param>
  /// <returns>The scaled WaveeSize.</returns>
  public static WaveeSize operator *(WaveeSize WaveeSize, WaveeVector scale)
  {
    return new WaveeSize(WaveeSize._width * scale.X, WaveeSize._height * scale.Y);
  }

  /// <summary>Scales a WaveeSize.</summary>
  /// <param name="WaveeSize">The WaveeSize</param>
  /// <param name="scale">The scaling factor.</param>
  /// <returns>The scaled WaveeSize.</returns>
  public static WaveeSize operator /(WaveeSize WaveeSize, WaveeVector scale)
  {
    return new WaveeSize(WaveeSize._width / scale.X, WaveeSize._height / scale.Y);
  }

  /// <summary>
  /// Divides a WaveeSize by another WaveeSize to produce a scaling factor.
  /// </summary>
  /// <param name="left">The first WaveeSize</param>
  /// <param name="right">The second WaveeSize.</param>
  /// <returns>The scaled WaveeSize.</returns>
  public static WaveeVector operator /(WaveeSize left, WaveeSize right)
  {
    return new WaveeVector(left._width / right._width, left._height / right._height);
  }

  /// <summary>Scales a WaveeSize.</summary>
  /// <param name="WaveeSize">The WaveeSize</param>
  /// <param name="scale">The scaling factor.</param>
  /// <returns>The scaled WaveeSize.</returns>
  public static WaveeSize operator *(WaveeSize WaveeSize, double scale)
  {
    return new WaveeSize(WaveeSize._width * scale, WaveeSize._height * scale);
  }

  /// <summary>Scales a WaveeSize.</summary>
  /// <param name="WaveeSize">The WaveeSize</param>
  /// <param name="scale">The scaling factor.</param>
  /// <returns>The scaled WaveeSize.</returns>
  public static WaveeSize operator /(WaveeSize WaveeSize, double scale)
  {
    return new WaveeSize(WaveeSize._width / scale, WaveeSize._height / scale);
  }

  public static WaveeSize operator +(WaveeSize WaveeSize, WaveeSize toAdd)
  {
    return new WaveeSize(WaveeSize._width + toAdd._width, WaveeSize._height + toAdd._height);
  }

  public static WaveeSize operator -(WaveeSize WaveeSize, WaveeSize toSubtract)
  {
    return new WaveeSize(WaveeSize._width - toSubtract._width, WaveeSize._height - toSubtract._height);
  }

  //TOOD:
  // /// <summary>
  // /// Parses a <see cref="T:Wavee.ViewModels.Models.UI.WaveeSize" /> string.
  // /// </summary>
  // /// <param name="s">The string.</param>
  // /// <returns>The <see cref="T:Wavee.ViewModels.Models.UI.WaveeSize" />.</returns>
  // public static WaveeSize Parse(string s)
  // {
  //   using (StringTokenizer stringTokenizer = new StringTokenizer(s, (IFormatProvider) CultureInfo.InvariantCulture, "Invalid WaveeSize."))
  //     return new WaveeSize(stringTokenizer.ReadDouble(), stringTokenizer.ReadDouble());
  // }

  /// <summary>Constrains the WaveeSize.</summary>
  /// <param name="constraint">The WaveeSize to constrain to.</param>
  /// <returns>The constrained WaveeSize.</returns>
  public WaveeSize Constrain(WaveeSize constraint)
  {
    return new WaveeSize(Math.Min(this._width, constraint._width), Math.Min(this._height, constraint._height));
  }

  // /// <summary>
  // /// Deflates the WaveeSize by a <see cref="T:Avalonia.Thickness" />.
  // /// </summary>
  // /// <param name="thickness">The thickness.</param>
  // /// <returns>The deflated WaveeSize.</returns>
  // /// <remarks>The deflated WaveeSize cannot be less than 0.</remarks>
  // public WaveeSize Deflate(WaveeThickness thickness)
  // {
  //   return new WaveeSize(Math.Max(0.0, this._width - thickness.Left - thickness.Right), Math.Max(0.0, this._height - thickness.Top - thickness.Bottom));
  // }

  /// <summary>
  /// Returns a boolean indicating whether the WaveeSize is equal to the other given WaveeSize (bitwise).
  /// </summary>
  /// <param name="other">The other WaveeSize to test equality against.</param>
  /// <returns>True if this WaveeSize is equal to other; False otherwise.</returns>
  public bool Equals(WaveeSize other) => this._width == other._width && this._height == other._height;

  // /// <summary>
  // /// Returns a boolean indicating whether the WaveeSize is equal to the other given WaveeSize (numerically).
  // /// </summary>
  // /// <param name="other">The other WaveeSize to test equality against.</param>
  // /// <returns>True if this WaveeSize is equal to other; False otherwise.</returns>
  // public bool NearlyEquals(WaveeSize other)
  // {
  //   return MathUtilities.AreClose(this._width, other._width) && MathUtilities.AreClose(this._height, other._height);
  // }

  /// <summary>Checks for equality between a WaveeSize and an object.</summary>
  /// <param name="obj">The object.</param>
  /// <returns>
  /// True if <paramref name="obj" /> is a WaveeSize that equals the current WaveeSize.
  /// </returns>
  public override bool Equals(object? obj) => obj is WaveeSize other && this.Equals(other);

  /// <summary>
  /// Returns a hash code for a <see cref="T:Wavee.ViewModels.Models.UI.WaveeSize" />.
  /// </summary>
  /// <returns>The hash code.</returns>
  public override int GetHashCode()
  {
    return (17 * 23 + this.Width.GetHashCode()) * 23 + this.Height.GetHashCode();
  }

  // /// <summary>
  // /// Inflates the WaveeSize by a <see cref="T:Avalonia.Thickness" />.
  // /// </summary>
  // /// <param name="thickness">The thickness.</param>
  // /// <returns>The inflated WaveeSize.</returns>
  // public WaveeSize Inflate(Thickness thickness)
  // {
  //   return new WaveeSize(this._width + thickness.Left + thickness.Right, this._height + thickness.Top + thickness.Bottom);
  // }

  /// <summary>
  /// Returns a new <see cref="T:Wavee.ViewModels.Models.UI.WaveeSize" /> with the same height and the specified width.
  /// </summary>
  /// <param name="width">The width.</param>
  /// <returns>The new <see cref="T:Wavee.ViewModels.Models.UI.WaveeSize" />.</returns>
  public WaveeSize WithWidth(double width) => new WaveeSize(width, this._height);

  /// <summary>
  /// Returns a new <see cref="T:Wavee.ViewModels.Models.UI.WaveeSize" /> with the same width and the specified height.
  /// </summary>
  /// <param name="height">The height.</param>
  /// <returns>The new <see cref="T:Wavee.ViewModels.Models.UI.WaveeSize" />.</returns>
  public WaveeSize WithHeight(double height) => new WaveeSize(this._width, height);

  /// <summary>Returns the string representation of the WaveeSize.</summary>
  /// <returns>The string representation of the WaveeSize.</returns>
  public override string ToString()
  {
    return string.Format((IFormatProvider) CultureInfo.InvariantCulture, "{0}, {1}", (object) this._width, (object) this._height);
  }

  /// <summary>
  /// Deconstructs the WaveeSize into its Width and Height values.
  /// </summary>
  /// <param name="width">The width.</param>
  /// <param name="height">The height.</param>
  public void Deconstruct(out double width, out double height)
  {
    width = this._width;
    height = this._height;
  }
}