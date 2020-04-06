using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Drawing;
using System.Security;
using System.Reflection;
using System.Windows.Forms;

namespace _3dpBurnerImage2Gcode
{
    public class GenerateGCode
    {
        public GenerateGCodeResults GenerateFileData(Bitmap img, char szChar, Boolean imperial, System.Windows.Forms.RichTextBox rtbGCode, string fRate,
            string engravePattern, float resol, string laserMin, string laserMax, Boolean edgeLines, string res)
        {

            Int32 lin;//top/bottom pixel
            Int32 col;//Left/right pixel

            List<string> fileLines;
            fileLines = new List<string>();

            WriteHeaderInfo(fileLines);

            foreach (string s in rtbGCode.Lines)
            {
                fileLines.Add(s);
            }

            fileLines.Add("G90\r");//Absolute coordinates

            WriteMeasurementInfo(fileLines, imperial);

            fileLines.Add("F" + fRate + "\r");//Feed rate

            float lastX;//Last x/y  coords for compare
            float lastY;
            Int32 lastSz;//last 'S' value for compare
            float coordY;

            //Add the pre-Gcode lines
            lastX = -1;//reset last positions
            lastY = -1;
            lastSz = -1;
            //Generate picture Gcode
            Int32 pixTot = img.Width * img.Height;
            Int32 pixBurned = 0;
            //////////////////////////////////////////////
            // Generate Gcode lines by Horizontal scanning
            //////////////////////////////////////////////
            if (engravePattern == "Horizontal scanning")
            {
                //Goto rapid move to left top corner
                fileLines.Add("G0X0Y" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", img.Height * Convert.ToSingle(res, CultureInfo.InvariantCulture.NumberFormat)));
                fileLines.Add("G1\r");//G1 mode
                fileLines.Add("M3\r");//Switch laser on

                //Start image
                lin = img.Height - 1;//top tile
                col = 0;//Left pixel
                while (lin >= 0)
                {
                    //Y coordinate
                    coordY = resol * (float)lin;
                    while (col < img.Width)//From left to right
                    {
                        GenerateXLineData(fileLines, coordY, lastX, lastY, lastSz, szChar, col, resol, lin, pixBurned, img, laserMin, laserMax);
                        col++;
                    }
                    col--;
                    lin--;
                    coordY = resol * (float)lin;
                    while ((col >= 0) & (lin >= 0))//From right to left
                    {
                        GenerateXLineData(fileLines, coordY, lastX, lastY, lastSz, szChar, col, resol, lin, pixBurned, img, laserMin, laserMax);
                        col--;
                    }
                    col++;
                    lin--;
                    //SetStatus("Generating file... " + Convert.ToString((pixBurned * 100) / pixTot) + "%");
                }

            }
            //////////////////////////////////////////////
            // Generate Gcode lines by Diagonal scanning
            //////////////////////////////////////////////
            else
            {
                //Goto rapid move to left top corner
                fileLines.Add("G0X0Y0");
                fileLines.Add("G1\r");//G1 mode
                fileLines.Add("M3\r");//Switch laser on

                //Start image
                col = 0;
                lin = 0;
                while ((col < img.Width) | (lin < img.Height))
                {
                    while ((col < img.Width) & (lin >= 0))
                    {
                        //Y coordinate
                        coordY = resol * (float)lin;
                        GenerateXLineData(fileLines, coordY, lastX, lastY, lastSz, szChar, col, resol, lin, pixBurned, img, laserMin, laserMax);
                        col++;
                        lin--;
                    }
                    col--;
                    lin++;

                    if (col >= img.Width - 1) lin++;
                    else col++;
                    while ((col >= 0) & (lin < img.Height))
                    {
                        //Y coordinate
                        coordY = resol * (float)lin;
                        GenerateXLineData(fileLines, coordY, lastX, lastY, lastSz, szChar, col, resol, lin, pixBurned, img, laserMin, laserMax);
                        col--;
                        lin++;
                    }
                    col++;
                    lin--;
                    if (lin >= img.Height - 1) col++;
                    else lin++;
                    //SetStatus("Generating file... " + Convert.ToString((pixBurned * 100) / pixTot) + "%");
                }
            }
            //Edge lines
            WriteEdgeLines(fileLines, edgeLines, resol, img, laserMax);

            //Switch laser off
            fileLines.Add("M5\r");//G1 mode

            WritePostGCode(fileLines, rtbGCode);

            return new GenerateGCodeResults()
            {
                FileLines = fileLines,
                PixBurned = pixBurned,
                PixTotal = pixTot
            };
        }
        ////Generate a "minimalist" gcode line based on the actual and last coordinates and laser power
        private string GenerateLine(float coordX, float coordY, float lastX, float lastY, float lastSz, Int32 sz, char szChar)
        {
            //Generate Gcode line
            string line = "";
            if (coordX != lastX)//Add X coord to line if is different from previous             
            {
                line += 'X' + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", coordX);
            }
            if (coordY != lastY)//Add Y coord to line if is different from previous
            {
                line += 'Y' + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", coordY);
            }
            if (sz != lastSz)//Add power value to line if is different from previous
            {
                line += szChar + Convert.ToString(sz);
            }
            return line;
        }
        private void GenerateXLineData(List<string> fLines, float coordY, float lastX, float lastY, float lastSz, char szChar, int col, float resol, int lin, int pixBurned, Bitmap img, string laserMin,string laserMax)
        {

            float coordX;
            Int32 sz;
            //X coordinate
            coordX = resol * (float)col;

            //Power value
            Color cl = img.GetPixel(col, (img.Height - 1) - lin);//Get pixel color
            sz = 255 - cl.R;
            sz = Interpolate(sz, Convert.ToInt32(laserMin), Convert.ToInt32(laserMax));

            string l = GenerateLine(coordX, coordY, lastX, lastY, lastSz, sz, szChar);
            pixBurned++;

            if (!string.IsNullOrEmpty(l)) fLines.Add(l);
            lastX = coordX;
            lastY = coordY;
            lastSz = sz;
        }
        private void WriteHeaderInfo(List<string> fLines)
        {
            //first Gcode line
            fLines.Add("(Generated by 3dpBurner Image2Gcode " + Assembly.GetEntryAssembly().GetName().Version + ")");
            //line="(@"+DateTime.Now.ToString("MMM/dd/yyy HH:mm:ss)");
            fLines.Add("(@" + DateTime.Now.ToString("MMM/dd/yyy HH:mm:ss)"));

            fLines.Add("M5\r");//Make sure laser off
        }
        private void WriteMeasurementInfo(List<string> fLines, Boolean imperial)
        {
            if (imperial) fLines.Add("G20\r");//Imperial units
            else fLines.Add("G21\r");//Metric units
        }
        private void WritePostGCode(List<string> fLines, System.Windows.Forms.RichTextBox rtbGCode)
        {
            //Add the post-Gcode 
            foreach (string s in rtbGCode.Lines)
            {
                fLines.Add(s);
            }
        }
        private void WriteEdgeLines(List<string> fileLines, bool edgeLine, float resol, Bitmap img, string laserMax)
        {
            if (edgeLine)
            {
                fileLines.Add("M5\r");
                fileLines.Add("G0X0Y0\r");
                fileLines.Add("M3S" + laserMax + "\r");
                fileLines.Add("G1X0Y" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", (img.Height - 1) * resol) + "\r");
                fileLines.Add("G1X" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", (img.Width - 1) * resol) + "Y" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", (img.Height - 1) * resol) + "\r");
                fileLines.Add("G1X" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", (img.Width - 1) * resol) + "Y0\r");
                fileLines.Add("G1X0Y0\r");
            }
        }
        private Int32 Interpolate(Int32 grayValue, Int32 min, Int32 max)
        {
            Int32 dif = max - min;
            return (min + ((grayValue * dif) / 255));
        }
    }
}
