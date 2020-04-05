using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace _3dpBurnerImage2Gcode
{
    public class BurnGCode
    {
        public int Brightness { get; set; }
        public string BrightnessText => Convert.ToString(Brightness);
        public int Contrast { get; set; }
        public string ContrastText => Convert.ToString(Contrast);
        public int Gamma { get; set; }
        public string GammaText => Convert.ToString(Gamma / 100.0f);
        public string Dirthering { get; set; }
        public int xSize { get; set; }
        public int ySize { get; set; }
        private Bitmap orgImage;
        public Bitmap OrgImage
        {
            get => orgImage;
            set => SetImage(value); 
        }
        private Bitmap resetImage;
        public Bitmap ResetImage
        {
            get => resetImage;
            set => resetImage = value;
        }
        public Bitmap AdjImage => AdjustImage();
        private void SetImage(Bitmap value)
        {
            orgImage = value;
            xSize = orgImage.Width;
            ySize = orgImage.Height;
            if(resetImage == null)
            {
                resetImage = value;
            }
        }
        public Bitmap DirtherImage()
        {
            if(orgImage==null) return null;
            var masks = new byte[] { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };
            var output = new Bitmap(orgImage.Width, orgImage.Height, PixelFormat.Format1bppIndexed);
            var data = new sbyte[orgImage.Width, orgImage.Height];
            var inputData = orgImage.LockBits(new Rectangle(0, 0, orgImage.Width, orgImage.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                var scanLine = inputData.Scan0;
                var line = new byte[inputData.Stride];
                for (var y = 0; y < inputData.Height; y++, scanLine += inputData.Stride)
                {
                    System.Runtime.InteropServices.Marshal.Copy(scanLine, line, 0, line.Length);
                    for (var x = 0; x < orgImage.Width; x++)
                    {
                        data[x, y] = (sbyte)(64 * (GetGreyLevel(line[x * 3 + 2], line[x * 3 + 1], line[x * 3 + 0]) - 0.5));
                    }
                }
            }
            finally
            {
                orgImage.UnlockBits(inputData);
            }
            var outputData = output.LockBits(new Rectangle(0, 0, output.Width, output.Height), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
            try
            {
                var scanLine = outputData.Scan0;
                for (var y = 0; y < outputData.Height; y++, scanLine += outputData.Stride)
                {
                    var line = new byte[outputData.Stride];
                    for (var x = 0; x < orgImage.Width; x++)
                    {
                        var j = data[x, y] > 0;
                        if (j) line[x / 8] |= masks[x % 8];
                        var error = (sbyte)(data[x, y] - (j ? 32 : -32));
                        if (x < orgImage.Width - 1) data[x + 1, y] += (sbyte)(7 * error / 16);
                        if (y < orgImage.Height - 1)
                        {
                            if (x > 0) data[x - 1, y + 1] += (sbyte)(3 * error / 16);
                            data[x, y + 1] += (sbyte)(5 * error / 16);
                            if (x < orgImage.Width - 1) data[x + 1, y + 1] += (sbyte)(1 * error / 16);
                        }
                    }
                    System.Runtime.InteropServices.Marshal.Copy(line, 0, scanLine, outputData.Stride);
                }
            }
            finally
            {
                output.UnlockBits(outputData);
            }
            orgImage = output;
            return output;
        }
        private static double GetGreyLevel(byte r, byte g, byte b)//aux for dirthering
        {
            return (r * 0.299 + g * 0.587 + b * 0.114) / 255;
        }
        //Return a gray scale version of an image
        public Bitmap GrayscaleImage()
        {
            if (orgImage == null) return null;
            Bitmap newBitmap = new Bitmap(xSize, ySize);//create a blank bitmap the same size as original
            Graphics g = Graphics.FromImage(newBitmap);//get a graphics object from the new image
            //create the gray scale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
                {
                    new float[] {.299f, .299f, .299f, 0, 0},
                    new float[] {.587f, .587f, .587f, 0, 0},
                    new float[] {.114f, .114f, .114f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
                });
            ImageAttributes attributes = new ImageAttributes();//create some image attributes
            attributes.SetColorMatrix(colorMatrix);//set the color matrix attribute

            //draw the original image on the new image using the gray scale color matrix
            g.DrawImage(orgImage, new Rectangle(0, 0, xSize, ySize),
               0, 0, xSize, ySize, GraphicsUnit.Pixel, attributes);
            g.Dispose();//dispose the Graphics object
            orgImage = newBitmap;
            return (newBitmap);
        }
        public Bitmap InvertImage()
        {
            if (orgImage == null) return null;
            Bitmap newBitmap = new Bitmap(xSize, ySize);//create a blank bitmap the same size as original
            Graphics g = Graphics.FromImage(newBitmap);//get a graphics object from the new image
            //create the gray scale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
                {
                    new float[] {-1, 0, 0, 0, 0},
                    new float[] {0, -1, 0, 0, 0},
                    new float[] {0, 0, -1, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {1, 1, 1, 0, 1}
                });
            ImageAttributes attributes = new ImageAttributes();//create some image attributes
            attributes.SetColorMatrix(colorMatrix);//set the color matrix attribute
            
            //draw the original image on the new image using the gray scale color matrix
            g.DrawImage(orgImage, new Rectangle(0, 0, xSize, ySize),
               0, 0, xSize, ySize, GraphicsUnit.Pixel, attributes);
            g.Dispose();//dispose the Graphics object
            orgImage = newBitmap;
            return newBitmap;
        }
        //Adjust brightness contrast and gamma of an image      
        public Bitmap BalanceImage()
        {
            if (orgImage == null) return null;
            ImageAttributes imageAttributes;
            float brightness = (Brightness/ 100.0f) + 1.0f;
            float contrast = (Contrast / 100.0f) + 1.0f;
            float gamma = 1 / (Gamma / 100.0f);
            float adjustedBrightness = brightness - 1.0f;
            Bitmap output;
            // create matrix that will brighten and contrast the image
            float[][] ptsArray ={
            new float[] {contrast, 0, 0, 0, 0}, // scale red
            new float[] {0, contrast, 0, 0, 0}, // scale green
            new float[] {0, 0, contrast, 0, 0}, // scale blue
            new float[] {0, 0, 0, 1.0f, 0}, // don't scale alpha
            new float[] {adjustedBrightness, adjustedBrightness, adjustedBrightness, 0, 1}};

            output = new Bitmap(orgImage);
            imageAttributes = new ImageAttributes();
            imageAttributes.ClearColorMatrix();
            imageAttributes.SetColorMatrix(new ColorMatrix(ptsArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            imageAttributes.SetGamma(gamma, ColorAdjustType.Bitmap);
            Graphics g = Graphics.FromImage(output);
            g.DrawImage(output, new Rectangle(0, 0, output.Width, output.Height)
            , 0, 0, output.Width, output.Height,
            GraphicsUnit.Pixel, imageAttributes);
            orgImage = output;
            return output;
        }
        //Resize image to desired width/height for gcode generation
        public Bitmap ResizeImage()
        {
            if (orgImage == null) return null;
            //Resize
            Bitmap output;
            output = new Bitmap(orgImage, new Size(xSize, ySize));
            orgImage = output;
            return output;
        }
        //Invoked when the user input any value for image adjust
        private Bitmap AdjustImage()
        {           
            if (orgImage == null) return null;//if no image, do nothing
            Bitmap adjustedImage = ResizeImage();
            //Apply balance to adjusted (resized) image
            adjustedImage = BalanceImage();
            //Reset dirthering to adjusted (resized and balanced) image
            Dirthering = "GrayScale 8 bit";
            //Display image
            return adjustedImage;
        }

        public void RotateFlip(RotateFlipType r)
        {
            if (orgImage == null) return;
            //RotateFlip
            orgImage.RotateFlip(r);      
        }
    }

}
