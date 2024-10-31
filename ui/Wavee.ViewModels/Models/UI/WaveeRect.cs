// Decompiled with JetBrains decompiler
// Type: Avalonia.WaveeRect
// Assembly: Avalonia.Base, Version=11.1.0.0, Culture=neutral, PublicKeyToken=c8d484a7012f9a8b
// MVID: CEB1C14A-2CAD-4D38-A6C5-BF90E72339C8
// Assembly location: C:\Users\ckara\.nuget\packages\avalonia\11.1.0\ref\net8.0\Avalonia.Base.dll
// XML documentation location: C:\Users\ckara\.nuget\packages\avalonia\11.1.0\ref\net8.0\Avalonia.Base.xml

#nullable enable
using System.Globalization;

namespace Wavee.ViewModels.Models.UI;

/// <summary>Defines a WaveeRectangle.</summary>
public readonly struct WaveeRect : IEquatable<WaveeRect>
{
    /// <summary>The X position.</summary>
    private readonly double _x;

    /// <summary>The Y position.</summary>
    private readonly double _y;

    /// <summary>The width.</summary>
    private readonly double _width;

    /// <summary>The height.</summary>
    private readonly double _height;

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Avalonia.WaveeRect" /> structure.
    /// </summary>
    /// <param name="x">The X position.</param>
    /// <param name="y">The Y position.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public WaveeRect(double x, double y, double width, double height)
    {
        this._x = x;
        this._y = y;
        this._width = width;
        this._height = height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Avalonia.WaveeRect" /> structure.
    /// </summary>
    /// <param name="WaveeSize">The WaveeSize of the WaveeRectangle.</param>
    public WaveeRect(WaveeSize WaveeSize)
    {
        this._x = 0.0;
        this._y = 0.0;
        this._width = WaveeSize.Width;
        this._height = WaveeSize.Height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Avalonia.WaveeRect" /> structure.
    /// </summary>
    /// <param name="position">The position of the WaveeRectangle.</param>
    /// <param name="WaveeSize">The WaveeSize of the WaveeRectangle.</param>
    public WaveeRect(WaveePoint position, WaveeSize WaveeSize)
    {
        this._x = position.X;
        this._y = position.Y;
        this._width = WaveeSize.Width;
        this._height = WaveeSize.Height;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Avalonia.WaveeRect" /> structure.
    /// </summary>
    /// <param name="topLeft">The top left position of the WaveeRectangle.</param>
    /// <param name="bottomRight">The bottom right position of the WaveeRectangle.</param>
    public WaveeRect(WaveePoint topLeft, WaveePoint bottomRight)
    {
        this._x = topLeft.X;
        this._y = topLeft.Y;
        this._width = bottomRight.X - topLeft.X;
        this._height = bottomRight.Y - topLeft.Y;
    }

    /// <summary>Gets the X position.</summary>
    public double X => this._x;

    /// <summary>Gets the Y position.</summary>
    public double Y => this._y;

    /// <summary>Gets the width.</summary>
    public double Width => this._width;

    /// <summary>Gets the height.</summary>
    public double Height => this._height;

    /// <summary>Gets the position of the WaveeRectangle.</summary>
    public WaveePoint Position => new WaveePoint(this._x, this._y);

    /// <summary>Gets the WaveeSize of the WaveeRectangle.</summary>
    public WaveeSize WaveeSize => new WaveeSize(this._width, this._height);

    /// <summary>Gets the right position of the WaveeRectangle.</summary>
    public double Right => this._x + this._width;

    /// <summary>Gets the bottom position of the WaveeRectangle.</summary>
    public double Bottom => this._y + this._height;

    /// <summary>Gets the left position.</summary>
    public double Left => this._x;

    /// <summary>Gets the top position.</summary>
    public double Top => this._y;

    /// <summary>Gets the top left WaveePoint of the WaveeRectangle.</summary>
    public WaveePoint TopLeft => new WaveePoint(this._x, this._y);

    /// <summary>Gets the top right WaveePoint of the WaveeRectangle.</summary>
    public WaveePoint TopRight => new WaveePoint(this.Right, this._y);

    /// <summary>Gets the bottom left WaveePoint of the WaveeRectangle.</summary>
    public WaveePoint BottomLeft => new WaveePoint(this._x, this.Bottom);

    /// <summary>Gets the bottom right WaveePoint of the WaveeRectangle.</summary>
    public WaveePoint BottomRight => new WaveePoint(this.Right, this.Bottom);

    /// <summary>Gets the center WaveePoint of the WaveeRectangle.</summary>
    public WaveePoint Center => new WaveePoint(this._x + this._width / 2.0, this._y + this._height / 2.0);

    /// <summary>
    /// Checks for equality between two <see cref="T:Avalonia.WaveeRect" />s.
    /// </summary>
    /// <param name="left">The first WaveeRect.</param>
    /// <param name="right">The second WaveeRect.</param>
    /// <returns>True if the WaveeRects are equal; otherwise false.</returns>
    public static bool operator ==(WaveeRect left, WaveeRect right) => left.Equals(right);

    /// <summary>
    /// Checks for inequality between two <see cref="T:Avalonia.WaveeRect" />s.
    /// </summary>
    /// <param name="left">The first WaveeRect.</param>
    /// <param name="right">The second WaveeRect.</param>
    /// <returns>True if the WaveeRects are unequal; otherwise false.</returns>
    public static bool operator !=(WaveeRect left, WaveeRect right) => !(left == right);

    /// <summary>Multiplies a WaveeRectangle by a scaling WaveeVector.</summary>
    /// <param name="WaveeRect">The WaveeRectangle.</param>
    /// <param name="scale">The WaveeVector scale.</param>
    /// <returns>The scaled WaveeRectangle.</returns>
    public static WaveeRect operator *(WaveeRect WaveeRect, WaveeVector scale)
    {
        return new WaveeRect(WaveeRect.X * scale.X, WaveeRect.Y * scale.Y, WaveeRect.Width * scale.X,
            WaveeRect.Height * scale.Y);
    }

    /// <summary>Multiplies a WaveeRectangle by a scale.</summary>
    /// <param name="WaveeRect">The WaveeRectangle.</param>
    /// <param name="scale">The scale.</param>
    /// <returns>The scaled WaveeRectangle.</returns>
    public static WaveeRect operator *(WaveeRect WaveeRect, double scale)
    {
        return new WaveeRect(WaveeRect.X * scale, WaveeRect.Y * scale, WaveeRect.Width * scale,
            WaveeRect.Height * scale);
    }

    /// <summary>Divides a WaveeRectangle by a WaveeVector.</summary>
    /// <param name="WaveeRect">The WaveeRectangle.</param>
    /// <param name="scale">The WaveeVector scale.</param>
    /// <returns>The scaled WaveeRectangle.</returns>
    public static WaveeRect operator /(WaveeRect WaveeRect, WaveeVector scale)
    {
        return new WaveeRect(WaveeRect.X / scale.X, WaveeRect.Y / scale.Y, WaveeRect.Width / scale.X,
            WaveeRect.Height / scale.Y);
    }

    /// <summary>
    /// Determines whether a WaveePoint is in the bounds of the WaveeRectangle.
    /// </summary>
    /// <param name="p">The WaveePoint.</param>
    /// <returns>true if the WaveePoint is in the bounds of the WaveeRectangle; otherwise false.</returns>
    public bool Contains(WaveePoint p)
    {
        return p.X >= this._x && p.X <= this._x + this._width && p.Y >= this._y && p.Y <= this._y + this._height;
    }

    /// <summary>
    /// Determines whether a WaveePoint is in the bounds of the WaveeRectangle, exclusive of the
    /// WaveeRectangle's bottom/right edge.
    /// </summary>
    /// <param name="p">The WaveePoint.</param>
    /// <returns>true if the WaveePoint is in the bounds of the WaveeRectangle; otherwise false.</returns>
    public bool ContainsExclusive(WaveePoint p)
    {
        return p.X >= this._x && p.X < this._x + this._width && p.Y >= this._y && p.Y < this._y + this._height;
    }

    /// <summary>
    /// Determines whether the WaveeRectangle fully contains another WaveeRectangle.
    /// </summary>
    /// <param name="r">The WaveeRectangle.</param>
    /// <returns>true if the WaveeRectangle is fully contained; otherwise false.</returns>
    public bool Contains(WaveeRect r) => this.Contains(r.TopLeft) && this.Contains(r.BottomRight);

    /// <summary>Centers another WaveeRectangle in this WaveeRectangle.</summary>
    /// <param name="WaveeRect">The WaveeRectangle to center.</param>
    /// <returns>The centered WaveeRectangle.</returns>
    public WaveeRect CenterWaveeRect(WaveeRect WaveeRect)
    {
        return new WaveeRect(this._x + (this._width - WaveeRect._width) / 2.0,
            this._y + (this._height - WaveeRect._height) / 2.0, WaveeRect._width, WaveeRect._height);
    }

    // /// <summary>Inflates the WaveeRectangle.</summary>
    // /// <param name="thickness">The thickness to be subtracted for each side of the WaveeRectangle.</param>
    // /// <returns>The inflated WaveeRectangle.</returns>
    // public WaveeRect Inflate(double thickness) => this.Inflate(new Thickness(thickness));
    //
    // /// <summary>Inflates the WaveeRectangle.</summary>
    // /// <param name="thickness">The thickness to be subtracted for each side of the WaveeRectangle.</param>
    // /// <returns>The inflated WaveeRectangle.</returns>
    // public WaveeRect Inflate(Thickness thickness)
    // {
    //   return new WaveeRect(new WaveePoint(this._x - thickness.Left, this._y - thickness.Top), this.WaveeSize.Inflate(thickness));
    // }

    // /// <summary>Deflates the WaveeRectangle.</summary>
    // /// <param name="thickness">The thickness to be subtracted for each side of the WaveeRectangle.</param>
    // /// <returns>The deflated WaveeRectangle.</returns>
    // public WaveeRect Deflate(double thickness) => this.Deflate(new Thickness(thickness));
    //
    // /// <summary>
    // /// Deflates the WaveeRectangle by a <see cref="T:Avalonia.Thickness" />.
    // /// </summary>
    // /// <param name="thickness">The thickness to be subtracted for each side of the WaveeRectangle.</param>
    // /// <returns>The deflated WaveeRectangle.</returns>
    // public WaveeRect Deflate(Thickness thickness)
    // {
    //   return new WaveeRect(new WaveePoint(this._x + thickness.Left, this._y + thickness.Top), this.WaveeSize.Deflate(thickness));
    // }

    /// <summary>
    /// Returns a boolean indicating whether the WaveeRect is equal to the other given WaveeRect.
    /// </summary>
    /// <param name="other">The other WaveeRect to test equality against.</param>
    /// <returns>True if this WaveeRect is equal to other; False otherwise.</returns>
    public bool Equals(WaveeRect other)
    {
        return this._x == other._x && this._y == other._y && this._width == other._width &&
               this._height == other._height;
    }

    /// <summary>
    /// Returns a boolean indicating whether the given object is equal to this WaveeRectangle.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>True if the object is equal to this WaveeRectangle; false otherwise.</returns>
    public override bool Equals(object? obj) => obj is WaveeRect other && this.Equals(other);

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return (((17 * 23 + this.X.GetHashCode()) * 23 + this.Y.GetHashCode()) * 23 + this.Width.GetHashCode()) * 23 +
               this.Height.GetHashCode();
    }

    /// <summary>Gets the intersection of two WaveeRectangles.</summary>
    /// <param name="WaveeRect">The other WaveeRectangle.</param>
    /// <returns>The intersection.</returns>
    public WaveeRect Intersect(WaveeRect WaveeRect)
    {
        double x = WaveeRect.X > this.X ? WaveeRect.X : this.X;
        double y = WaveeRect.Y > this.Y ? WaveeRect.Y : this.Y;
        double num1 = WaveeRect.Right < this.Right ? WaveeRect.Right : this.Right;
        double num2 = WaveeRect.Bottom < this.Bottom ? WaveeRect.Bottom : this.Bottom;
        return num1 > x && num2 > y ? new WaveeRect(x, y, num1 - x, num2 - y) : new WaveeRect();
    }

    /// <summary>
    /// Determines whether a WaveeRectangle intersects with this WaveeRectangle.
    /// </summary>
    /// <param name="WaveeRect">The other WaveeRectangle.</param>
    /// <returns>
    /// True if the specified WaveeRectangle intersects with this one; otherwise false.
    /// </returns>
    public bool Intersects(WaveeRect WaveeRect)
    {
        return WaveeRect.X < this.Right && this.X < WaveeRect.Right && WaveeRect.Y < this.Bottom &&
               this.Y < WaveeRect.Bottom;
    }

    // /// <summary>
    // /// Returns the axis-aligned bounding box of a transformed WaveeRectangle.
    // /// </summary>
    // /// <param name="matrix">The transform.</param>
    // /// <returns>The bounding box</returns>
    // public WaveeRect TransformToAABB(Matrix matrix)
    // {
    //   ReadOnlySpan<WaveePoint> readOnlySpan1 = stackalloc WaveePoint[4]
    //   {
    //     this.TopLeft.Transform(matrix),
    //     this.TopRight.Transform(matrix),
    //     this.BottomRight.Transform(matrix),
    //     this.BottomLeft.Transform(matrix)
    //   };
    //   double x1 = double.MaxValue;
    //   double x2 = double.MinValue;
    //   double y1 = double.MaxValue;
    //   double y2 = double.MinValue;
    //   ReadOnlySpan<WaveePoint> readOnlySpan2 = readOnlySpan1;
    //   for (int index = 0; index < readOnlySpan2.Length; ++index)
    //   {
    //     WaveePoint WaveePoint = readOnlySpan2[index];
    //     if (WaveePoint.X < x1)
    //       x1 = WaveePoint.X;
    //     if (WaveePoint.X > x2)
    //       x2 = WaveePoint.X;
    //     if (WaveePoint.Y < y1)
    //       y1 = WaveePoint.Y;
    //     if (WaveePoint.Y > y2)
    //       y2 = WaveePoint.Y;
    //   }
    //   return new WaveeRect(new WaveePoint(x1, y1), new WaveePoint(x2, y2));
    // }
    //
    // internal WaveeRect TransformToAABB(Matrix4x4 matrix)
    // {
    //   ReadOnlySpan<WaveePoint> readOnlySpan1 = stackalloc WaveePoint[4]
    //   {
    //     this.TopLeft.Transform(matrix),
    //     this.TopRight.Transform(matrix),
    //     this.BottomRight.Transform(matrix),
    //     this.BottomLeft.Transform(matrix)
    //   };
    //   double x1 = double.MaxValue;
    //   double x2 = double.MinValue;
    //   double y1 = double.MaxValue;
    //   double y2 = double.MinValue;
    //   ReadOnlySpan<WaveePoint> readOnlySpan2 = readOnlySpan1;
    //   for (int index = 0; index < readOnlySpan2.Length; ++index)
    //   {
    //     WaveePoint WaveePoint = readOnlySpan2[index];
    //     if (WaveePoint.X < x1)
    //       x1 = WaveePoint.X;
    //     if (WaveePoint.X > x2)
    //       x2 = WaveePoint.X;
    //     if (WaveePoint.Y < y1)
    //       y1 = WaveePoint.Y;
    //     if (WaveePoint.Y > y2)
    //       y2 = WaveePoint.Y;
    //   }
    //   return new WaveeRect(new WaveePoint(x1, y1), new WaveePoint(x2, y2));
    // }

    // /// <summary>Translates the WaveeRectangle by an offset.</summary>
    // /// <param name="offset">The offset.</param>
    // /// <returns>The translated WaveeRectangle.</returns>
    // public WaveeRect Translate(WaveeVector offset) => new WaveeRect(this.Position + offset, this.WaveeSize);

    /// <summary>
    /// Normalizes the WaveeRectangle so both the <see cref="P:Avalonia.WaveeRect.Width" /> and <see cref="P:Avalonia.WaveeRect.Height" /> are positive, without changing the location of the WaveeRectangle
    /// </summary>
    /// <returns>Normalized WaveeRect</returns>
    /// <remarks>
    /// Empty WaveeRect will be return when WaveeRect contains invalid values. Like NaN.
    /// </remarks>
    public WaveeRect Normalize()
    {
        WaveeRect WaveeRect = this;
        if (double.IsNaN(WaveeRect.Right) || double.IsNaN(WaveeRect.Bottom) || double.IsNaN(WaveeRect.X) ||
            double.IsNaN(WaveeRect.Y) || double.IsNaN(this.Height) || double.IsNaN(this.Width))
            return new WaveeRect();
        if (WaveeRect.Width < 0.0)
        {
            double x = this.X + this.Width;
            double width = this.X - x;
            WaveeRect = WaveeRect.WithX(x).WithWidth(width);
        }

        if (WaveeRect.Height < 0.0)
        {
            double y = this.Y + this.Height;
            double height = this.Y - y;
            WaveeRect = WaveeRect.WithY(y).WithHeight(height);
        }

        return WaveeRect;
    }

    /// <summary>Gets the union of two WaveeRectangles.</summary>
    /// <param name="WaveeRect">The other WaveeRectangle.</param>
    /// <returns>The union.</returns>
    public WaveeRect Union(WaveeRect WaveeRect)
    {
        if (this.Width == 0.0 && this.Height == 0.0)
            return WaveeRect;
        if (WaveeRect.Width == 0.0 && WaveeRect.Height == 0.0)
            return this;
        double x1 = Math.Min(this.X, WaveeRect.X);
        double x2 = Math.Max(this.Right, WaveeRect.Right);
        double num = Math.Min(this.Y, WaveeRect.Y);
        double y1 = Math.Max(this.Bottom, WaveeRect.Bottom);
        double y2 = num;
        return new WaveeRect(new WaveePoint(x1, y2), new WaveePoint(x2, y1));
    }

    internal static WaveeRect? Union(WaveeRect? left, WaveeRect? right)
    {
        if (!left.HasValue)
            return right;
        return !right.HasValue ? left : new WaveeRect?(left.Value.Union(right.Value));
    }

    /// <summary>
    /// Returns a new <see cref="T:Avalonia.WaveeRect" /> with the specified X position.
    /// </summary>
    /// <param name="x">The x position.</param>
    /// <returns>The new <see cref="T:Avalonia.WaveeRect" />.</returns>
    public WaveeRect WithX(double x) => new WaveeRect(x, this._y, this._width, this._height);

    /// <summary>
    /// Returns a new <see cref="T:Avalonia.WaveeRect" /> with the specified Y position.
    /// </summary>
    /// <param name="y">The y position.</param>
    /// <returns>The new <see cref="T:Avalonia.WaveeRect" />.</returns>
    public WaveeRect WithY(double y) => new WaveeRect(this._x, y, this._width, this._height);

    /// <summary>
    /// Returns a new <see cref="T:Avalonia.WaveeRect" /> with the specified width.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <returns>The new <see cref="T:Avalonia.WaveeRect" />.</returns>
    public WaveeRect WithWidth(double width) => new WaveeRect(this._x, this._y, width, this._height);

    /// <summary>
    /// Returns a new <see cref="T:Avalonia.WaveeRect" /> with the specified height.
    /// </summary>
    /// <param name="height">The height.</param>
    /// <returns>The new <see cref="T:Avalonia.WaveeRect" />.</returns>
    public WaveeRect WithHeight(double height) => new WaveeRect(this._x, this._y, this._width, height);

    /// <summary>Returns the string representation of the WaveeRectangle.</summary>
    /// <returns>The string representation of the WaveeRectangle.</returns>
    public override string ToString()
    {
        return string.Format((IFormatProvider)CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", (object)this._x,
            (object)this._y, (object)this._width, (object)this._height);
    }


    /// <summary>
    /// This method should be used internally to check for the WaveeRect emptiness
    /// Once we add support for WPF-like empty WaveeRects, there will be an actual implementation
    /// For now it's internal to keep some loud community members happy about the API being pretty
    /// </summary>
    internal bool IsEmpty() => this == new WaveeRect();
}