﻿using System;

namespace SkiaSharp.Components
{
    public class ColorBrush : IBrush
    {
        public ColorBrush(SKColor color)
        {
            this.Color = color;
        }

        public SKColor Color { get; }

        public IDisposable Apply(SKPaint paint)
        {
            paint.Color = this.Color;
            return null;
        }

     
    }
}
