﻿using System;
using System.Numerics;
using System.Reflection;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using Xunit;
using Xunit.Abstractions;
// ReSharper disable InconsistentNaming

namespace SixLabors.ImageSharp.Tests.Processing.Transforms
{
    using SixLabors.ImageSharp.Helpers;

    public class TransformTests
    {
        private readonly ITestOutputHelper Output;

        public static readonly TheoryData<float, float, float, float, float> TransformValues
            = new TheoryData<float, float, float, float, float>
                  {
                      { 0, 1, 1, 0, 0 },
                      { 50, 1, 1, 0, 0 },
                      { 0, 1, 1, 20, 10 },
                      { 50, 1, 1, 20, 10 },
                      { 0, 1, 1, -20, -10 },
                      { 50, 1, 1, -20, -10 },
                      { 50, 1.5f, 1.5f, 0, 0 },
                      { 50, 1.1F, 1.2F, 20, 10 },
                      { 0, 2f, 1f, 0, 0 },
                      { 0, 1f, 2f, 0, 0 },
                  };

        public static readonly TheoryData<string> ResamplerNames =
            new TheoryData<string>
                  {
                      nameof(KnownResamplers.Bicubic),
                      nameof(KnownResamplers.Box),
                      nameof(KnownResamplers.CatmullRom),
                      nameof(KnownResamplers.Hermite),
                      nameof(KnownResamplers.Lanczos2),
                      nameof(KnownResamplers.Lanczos3),
                      nameof(KnownResamplers.Lanczos5),
                      nameof(KnownResamplers.Lanczos8),
                      nameof(KnownResamplers.MitchellNetravali),
                      nameof(KnownResamplers.NearestNeighbor),
                      nameof(KnownResamplers.Robidoux),
                      nameof(KnownResamplers.RobidouxSharp),
                      nameof(KnownResamplers.Spline),
                      nameof(KnownResamplers.Triangle),
                      nameof(KnownResamplers.Welch),
                  };

        public TransformTests(ITestOutputHelper output)
        {
            this.Output = output;
        }

        /// <summary>
        /// The output of an "all white" image should be "all white" or transparent, regardless of the transformation and the resampler.
        /// </summary>
        [Theory]
        [WithSolidFilledImages(5, 5, 255, 255, 255, 255, PixelTypes.Rgba32, nameof(KnownResamplers.NearestNeighbor))]
        [WithSolidFilledImages(5, 5, 255, 255, 255, 255, PixelTypes.Rgba32, nameof(KnownResamplers.Triangle))]
        [WithSolidFilledImages(5, 5, 255, 255, 255, 255, PixelTypes.Rgba32, nameof(KnownResamplers.Bicubic))]
        [WithSolidFilledImages(5, 5, 255, 255, 255, 255, PixelTypes.Rgba32, nameof(KnownResamplers.Lanczos8))]
        public void Transform_DoesNotCreateEdgeArtifacts<TPixel>(TestImageProvider<TPixel> provider, string resamplerName)
            where TPixel : struct, IPixel<TPixel>
        {
            IResampler resampler = GetResampler(resamplerName);
            using (Image<TPixel> image = provider.GetImage())
            {
                var rotate = Matrix3x2.CreateRotation((float)Math.PI / 4F, new Vector2(5 / 2F, 5 / 2F));
                var translate = Matrix3x2.CreateTranslation((7 - 5) / 2F, (7 - 5) / 2F);

                image.Mutate(c => c.Transform(rotate * translate, resampler));
                image.DebugSave(provider, resamplerName);

                VerifyAllPixelsAreWhiteOrTransparent(image);
            }
        }

        [Theory]
        [WithTestPatternImages(nameof(TransformValues), 100, 50, PixelTypes.Rgba32)]
        public void Transform_RotateScaleTranslate_AutoDestRectangle<TPixel>(
            TestImageProvider<TPixel> provider,
            float angleDeg,
            float sx, float sy,
            float tx, float ty)
            where TPixel : struct, IPixel<TPixel>
        {
            using (Image<TPixel> image = provider.GetImage())
            {
                Matrix3x2 rotate = Matrix3x2Extensions.CreateRotationDegrees(angleDeg);
                var translate = Matrix3x2.CreateTranslation(tx, ty);
                var scale = Matrix3x2.CreateScale(sx, sy);
                Matrix3x2 m = rotate * scale * translate;

                this.PrintMatrix(m);
                
                image.Mutate(i => i.Transform(m, KnownResamplers.Bicubic));
                image.DebugSave(provider, $"R({angleDeg})_S({sx},{sy})_T({tx},{ty})");
            }
        }

        [Theory]
        [WithTestPatternImages(nameof(TransformValues), 100, 50, PixelTypes.Rgba32)]
        public void Transform_RotateScaleTranslate_SameDestRectangle<TPixel>(
            TestImageProvider<TPixel> provider,
            float angleDeg,
            float sx, float sy,
            float tx, float ty)
            where TPixel : struct, IPixel<TPixel>
        {
            using (Image<TPixel> image = provider.GetImage())
            {
                Matrix3x2 rotate = Matrix3x2Extensions.CreateRotationDegrees(angleDeg);
                var translate = Matrix3x2.CreateTranslation(tx, ty);
                var scale = Matrix3x2.CreateScale(sx, sy);
                Matrix3x2 m = rotate * scale * translate;

                this.PrintMatrix(m);

                Rectangle destBounds = image.Bounds();
                image.Mutate(i => i.Transform(m, KnownResamplers.Bicubic, destBounds));
                image.DebugSave(provider, $"R({angleDeg})_S({sx},{sy})_T({tx},{ty})");
            }
        }

        [Theory]
        [WithTestPatternImages(96, 96, PixelTypes.Rgba32, 50, 0.8f)]
        public void Transform_RotateScale_ManuallyCentered<TPixel>(TestImageProvider<TPixel> provider, float angleDeg, float s)
            where TPixel : struct, IPixel<TPixel>
        {
            using (Image<TPixel> image = provider.GetImage())
            {
                Matrix3x2 rotate = Matrix3x2Extensions.CreateRotationDegrees(angleDeg);
                Vector2 toCenter = 0.5f * new Vector2(image.Width, image.Height);
                var translate = Matrix3x2.CreateTranslation(-toCenter);
                var translateBack = Matrix3x2.CreateTranslation(toCenter);
                var scale = Matrix3x2.CreateScale(s);

                Matrix3x2 m = translate * rotate * scale * translateBack;

                this.PrintMatrix(m);

                Rectangle destBounds = image.Bounds();
                image.Mutate(i => i.Transform(m, KnownResamplers.Bicubic, destBounds));
                image.DebugSave(provider, $"R({angleDeg})_S({s})");
            }
        }

        [Theory]
        [WithTestPatternImages(nameof(ResamplerNames), 100, 200, PixelTypes.Rgba32)]
        public void Transform_WithSampler<TPixel>(TestImageProvider<TPixel> provider, string resamplerName)
            where TPixel : struct, IPixel<TPixel>
        {
            IResampler sampler = GetResampler(resamplerName);
            using (Image<TPixel> image = provider.GetImage())
            {
                Matrix3x2 rotate = Matrix3x2Extensions.CreateRotationDegrees(50);
                Matrix3x2 scale = Matrix3x2Extensions.CreateScale(new SizeF(.5F, .5F));
                var translate = Matrix3x2.CreateTranslation(75, 0);


                image.Mutate(i => i.Transform(rotate * scale * translate, sampler));
                image.DebugSave(provider, resamplerName);
            }
        }

        private static IResampler GetResampler(string name)
        {
            PropertyInfo property = typeof(KnownResamplers).GetTypeInfo().GetProperty(name);

            if (property == null)
            {
                throw new Exception("Invalid property name!");
            }

            return (IResampler)property.GetValue(null);
        }

        private static void VerifyAllPixelsAreWhiteOrTransparent<TPixel>(Image<TPixel> image)
            where TPixel : struct, IPixel<TPixel>
        {
            TPixel[] data = new TPixel[image.Width * image.Height];
            image.Frames.RootFrame.SavePixelData(data);
            var rgba = default(Rgba32);
            var white = new Rgb24(255, 255, 255);
            foreach (TPixel pixel in data)
            {
                pixel.ToRgba32(ref rgba);
                if (rgba.A == 0) continue;

                Assert.Equal(white, rgba.Rgb);
            }
        }

        private void PrintMatrix(Matrix3x2 a)
        {
            string s = $"{a.M11:F10},{a.M12:F10},{a.M21:F10},{a.M22:F10},{a.M31:F10},{a.M32:F10}";
            this.Output.WriteLine(s);
        }
    }
}