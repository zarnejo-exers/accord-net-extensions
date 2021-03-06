#region Licence and Terms
// Accord.NET Extensions Framework
// https://github.com/dajuric/accord-net-extensions
//
// Copyright © Darko Jurić, 2014 
// darko.juric2@gmail.com
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU Lesser General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU Lesser General Public License for more details.
// 
//   You should have received a copy of the GNU Lesser General Public License
//   along with this program.  If not, see <https://www.gnu.org/licenses/lgpl.txt>.
//
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Extensions;
using Accord.Extensions.Imaging;

namespace Test
{
    public static class ImagePatchProcessor
    {
        private static void makePatches(Size imgSize, int minPatchHeight, out List<Rectangle> patches)
        {
            int patchHeight, verticalPatches;
            getPatchInfo(imgSize, out patchHeight, out verticalPatches);
            minPatchHeight = System.Math.Max(minPatchHeight, patchHeight);

            patches = new List<Rectangle>();

            for (int y = 0; y < imgSize.Height; )
            {
                int h = System.Math.Min(patchHeight, imgSize.Height - y);

                Rectangle patch = new Rectangle(0, y, imgSize.Width, h);
                patches.Add(patch);

                y += h;
            }

            //ensure minPatchSize (merge last two patches if necessary)
            if (patches.Last().Height < minPatchHeight)
            {
                var penultimate = patches[patches.Count - 1 - 1];
                var last = patches[patches.Count - 1];

                var mergedPatch = new Rectangle
                {
                    X = penultimate.X,
                    Y = penultimate.Y,
                    Width = penultimate.Width,
                    Height = penultimate.Height + last.Height
                };

                patches.RemoveRange(patches.Count - 1 - 1, 2);
                patches.Add(mergedPatch);
            }
        }

        private static void getPatchInfo(Size imgSize, out int patchHeight, out int verticalPatches)
        {
            int numOfCores = System.Environment.ProcessorCount;
            int minNumOfPatches = numOfCores * 2;

            float avgNumPatchElements = (float)(imgSize.Width * imgSize.Height) / minNumOfPatches;

            //make patch look like a long stripe (it is probably more efficient to process than a square patch)
            patchHeight = (int)System.Math.Floor(avgNumPatchElements / imgSize.Width);

            //get number of patches
            verticalPatches = (int)System.Math.Ceiling((float)imgSize.Height / patchHeight);
        }

        public static unsafe Image<TColor, TDepthDst> ProcessPatch<TColor, TDepthSrc, TDepthDst>(this Image<TColor, TDepthSrc> image, Action<Int32, Int32> action)
            where TColor: IColor
            where TDepthSrc: struct
            where TDepthDst: struct
        {
            List<Rectangle> patches;
            makePatches(image.Size, 0, out patches);

            int nChannels = image.ColorInfo.NumberOfChannels;

            var dst = new Image<TColor, TDepthDst>(image.Size);
            int dstOffset = dst.Stride - nChannels * dst.Width;

            int srcOffset = image.Stride - nChannels * image.Width;

            int width = image.Width;
            int height = image.Height;
       

            Parallel.ForEach(patches, (rect) => 
            {
                int patchHeight = rect.Height;

                byte* srcPtr = (byte*)image.GetData(rect.Y);
                byte* dstPtr = (byte*)dst.GetData(rect.Y);

                while (patchHeight-- > 0)
                {
                    int _width = width;
                    while(_width-- > 0)
                    {
                        action((Int32)(srcPtr), (Int32)(dstPtr));
                        srcPtr += nChannels;
                        dstPtr += nChannels;
                    }

                    srcPtr = (byte*)((byte*)srcPtr + srcOffset);
                    dstPtr = (byte*)((byte*)dstPtr + dstOffset);
                }

                /*for (int r = 0; r < patchHeight; r++)
                {
                    for (int c = 0; c < width; c++)
                    {
                        action((Int32)(srcPtr), (Int32)(dstPtr));
                        srcPtr += nChannels;
                        dstPtr += nChannels;
                    }

                    srcPtr = (byte*)((byte*)srcPtr + srcOffset);
                    dstPtr = (byte*)((byte*)dstPtr + dstOffset);
                }*/
            });

            return dst;
        }



    }

    public unsafe struct IntPtr<T>
    {
        void* value;

        public IntPtr(void* value)
        {
            this.value = value;
        }

        public static implicit operator int(IntPtr<T> ptr)
        {
            return (int)ptr.value;
        }

        public unsafe static implicit operator void*(IntPtr<T> ptr)
        {
            return (void*)ptr.value;
        }

        public unsafe static implicit operator IntPtr<T>(void* value)
        {
            return new IntPtr<T>(value);
        }
    }
}
