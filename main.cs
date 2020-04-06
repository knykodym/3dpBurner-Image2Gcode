/*  3dpBurner Image2Gcode. A Image to GCODE converter for GRBL based devices.
    This file is part of 3dpBurner Image2Gcode application.
   
    Copyright (C) 2015  Adrian V. J. (villamany) contact: villamany@gmail.com
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
//Form 1 (Main form)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Reflection;

namespace _3dpBurnerImage2Gcode
{
    public partial class main : Form
    {
        //const string ver = "v0.1";
        BurnGCode bgCode;
        float lastRatioValue;//Aux for apply processing to image only when a new value is detected
        public main()
        {
            InitializeComponent();
        }
        float ratio; //Used to lock the aspect ratio when the option is selected
        //Save settings
        private void SaveSettings()
        {
            try
            {
                string set;
                Properties.Settings.Default.autoZoom=autoZoomToolStripMenuItem.Checked;
                if (imperialinToolStripMenuItem.Checked) set = "imperial";
                    else set = "metric";
                Properties.Settings.Default.units =set;
                Properties.Settings.Default.width = tbWidth.Text;
                Properties.Settings.Default.height = tbHeight.Text;
                Properties.Settings.Default.resolution = tbRes.Text;
                Properties.Settings.Default.minPower = tbLaserMin.Text;
                Properties.Settings.Default.maxPower = tbLaserMax.Text;
                Properties.Settings.Default.header = rtbPreGcode.Text;
                Properties.Settings.Default.footer = rtbPostGcode.Text;
                Properties.Settings.Default.feedrate = tbFeedRate.Text;
                if (rbUseZ.Checked) set = "Z";
                else set = "S";
                Properties.Settings.Default.powerCommand = set;
                Properties.Settings.Default.pattern = cbEngravingPattern.Text;
                Properties.Settings.Default.edgeLines = cbEdgeLines.Checked;

                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error saving config: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }
        //Load settings
        private void LoadSettings()
        {
            try
            {
                autoZoomToolStripMenuItem.Checked=Properties.Settings.Default.autoZoom;
                autoZoomToolStripMenuItem_Click(this, null);

                if (Properties.Settings.Default.units=="imperial")
                {
                    imperialinToolStripMenuItem.Checked=true;
                    imperialinToolStripMenuItem_Click(this,null);
                }
                else 
                {
                    metricmmToolStripMenuItem.Checked=true;
                    metricmmToolStripMenuItem_Click(this,null);
                }
                tbWidth.Text=Properties.Settings.Default.width;
                tbHeight.Text=Properties.Settings.Default.height;
                tbRes.Text=Properties.Settings.Default.resolution;
                tbLaserMin.Text=Properties.Settings.Default.minPower;
                tbLaserMax.Text=Properties.Settings.Default.maxPower;
                rtbPreGcode.Text=Properties.Settings.Default.header;
                rtbPostGcode.Text=Properties.Settings.Default.footer;
                tbFeedRate.Text=Properties.Settings.Default.feedrate;
                if (Properties.Settings.Default.powerCommand == "Z")
                    rbUseZ.Checked = true;
                        else rbUseS.Checked = true;
                cbEngravingPattern.Text=Properties.Settings.Default.pattern;
                cbEdgeLines.Checked=Properties.Settings.Default.edgeLines;

                bgCode = new BurnGCode
                {
                    Brightness=0,
                    Contrast=0,
                    Gamma =100
                };
                

            }
            catch (Exception e)
            {
                MessageBox.Show("Error saving config: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }
        
        //Interpolate a 8 bit gray scale value (0-255) between min,max
        //private Int32 Interpolate(Int32 grayValue, Int32 min, Int32 max)
        //{
        //    Int32 dif=max-min;
        //    return (min + ((grayValue * dif) / 255));
        //}

        //Return true if char is a valid float digit, show error message is not and return false
        private bool CheckDigitFloat(char ch )
        {
            if ((ch != '.') & (ch != '0') & (ch != '1') & (ch != '2') & (ch != '3') & (ch != '4') & (ch != '5') & (ch != '6') & (ch != '7') & (ch != '8') & (ch != '9') & (Convert.ToByte(ch) != 8) & (Convert.ToByte(ch) != 13))//is a 0-9 numbre or . decimal separator, backspace or enter
            {
                MessageBox.Show("Allowed chars are '0'-'9' and '.' as decimal separator", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }
        //Return true if char is a valid integer digit, show eror message is not and return false
        private bool CheckDigitInteger(char ch)
        {
            if ((ch != '0') & (ch != '1') & (ch != '2') & (ch != '3') & (ch != '4') & (ch != '5') & (ch != '6') & (ch != '7') & (ch != '8') & (ch != '9') & (Convert.ToByte(ch) != 8) & (Convert.ToByte(ch) != 13))//is a 0-9 numbre or . decimal separator, backspace or enter
            {
                MessageBox.Show("Allowed chars are '0'-'9'", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }
     
        //Invoked when the user input any value for image adjust
        private void UserAdjusted()
        {
            try
            {
                Refresh();
                if (bgCode.OrgImage == null) return;//if no image, do nothing
                //Apply resize to original image
                bgCode.XSize = Convert.ToInt32(float.Parse(tbWidth.Text, CultureInfo.InvariantCulture.NumberFormat) / float.Parse(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat));
                bgCode.YSize = Convert.ToInt32(float.Parse(tbHeight.Text, CultureInfo.InvariantCulture.NumberFormat) / float.Parse(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat));
                SetStatus("Adjust Image...");
                //Display image
                UpdateImage(false);
                //Reset dirthering to adjusted (resized and balanced) image
                cbDirthering.Text = bgCode.Dirthering;
                //Set preview
                autoZoomToolStripMenuItem_Click(this, null);
                SetStatus("Done");
            }
            catch (Exception e)
            {
                MessageBox.Show("Error resizing/balancing image: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //Contrast adjusted by user
        private void tBarContrast_Scroll(object sender, EventArgs e)
        {
            lblContrast.Text = Convert.ToString(tBarContrast.Value);
            UserAdjusted(); 
        }
        //Brightness adjusted by user
        private void tBarBrightness_Scroll(object sender, EventArgs e)
        {
            lblBrightness.Text = Convert.ToString(tBarBrightness.Value);
            UserAdjusted();          
        }
        //Gamma adjusted by user
        private void tBarGamma_Scroll(object sender, EventArgs e)
        {
            lblGamma.Text = Convert.ToString(tBarGamma.Value/100.0f);
            UserAdjusted(); 
        }
        private void ResetImage()
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            bgCode.OrgImage = bgCode.ResetImage; 
            SetStatus("Loading original image...");
            UpdateImage(false);
            SetStatus("Done");
        }
        //Quick preview of the original image. Todo: use a new image container for fas return to processed image
        private void btnCheckOrig_MouseDown(object sender, MouseEventArgs e)
        {
            ResetImage();
        }
        //Reload the processed image after temporal preview of the original image
        private void btnCheckOrig_MouseUp(object sender, MouseEventArgs e)
        {
            ResetImage();
        }
        //Check if a new image width has been confirmed by user, process it.
        private void WidthChangedCheck()
        {
            try
            {
                if (bgCode.OrgImage == null) return;//if no image, do nothing
                float newValue = Convert.ToSingle(tbWidth.Text, CultureInfo.InvariantCulture.NumberFormat);//Get the user input value           
                if (newValue == lastRatioValue) return;//if not is a new value do nothing     
                lastRatioValue = Convert.ToSingle(tbWidth.Text, CultureInfo.InvariantCulture.NumberFormat);
                if (cbLockRatio.Checked)
                {
                    tbHeight.Text = Convert.ToString((newValue / ratio), CultureInfo.InvariantCulture.NumberFormat);
                }
                UserAdjusted();
            }
            catch 
            {
                MessageBox.Show("Check width value.", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //Check if a new image height has been confirmed by user, process it.
        private void HeightChangedCheck()
        {
            try
            {
                if (bgCode.OrgImage == null) return;//if no image, do nothing
                float newValue = Convert.ToSingle(tbHeight.Text, CultureInfo.InvariantCulture.NumberFormat);//Get the user input value   
                if (newValue == lastRatioValue) return;//if not is a new value do nothing
                lastRatioValue = Convert.ToSingle(tbHeight.Text, CultureInfo.InvariantCulture.NumberFormat);
                if (cbLockRatio.Checked)
                {
                    tbWidth.Text = Convert.ToString((newValue * ratio), CultureInfo.InvariantCulture.NumberFormat);
                }
                UserAdjusted();
            }
            catch {
                MessageBox.Show("Check height value.", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //Check if a new image resolution has been confirmed by user, process it.
        private void ResolutionChangedCheck()
        {
            try
            {
                if (bgCode.OrgImage == null) return;//if no image, do nothing
                float newValue = Convert.ToSingle(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat);//Get the user input value
                if (newValue == lastRatioValue) return;//if not is a new value do nothing
                lastRatioValue = Convert.ToSingle(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat);
                UserAdjusted();
            }
            catch {
                MessageBox.Show("Check resolution value.", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //CheckBox lockAspectRatio checked. Set as mandatory the user settled width and calculate the height by using the original aspect ratio
        private void cbLockRatio_CheckedChanged(object sender, EventArgs e)
        {
            if (cbLockRatio.Checked)
            {
                tbHeight.Text = Convert.ToString((Convert.ToSingle(tbWidth.Text, CultureInfo.InvariantCulture.NumberFormat) / ratio), CultureInfo.InvariantCulture.NumberFormat);//Initialize y size
                if (bgCode.OrgImage == null) return;//if no image, do nothing
                UserAdjusted();
            }
        }
        //On form load
        private void Form1_Load(object sender, EventArgs e)
        {
            Text = "3dpBurner Image2Gcode " + Assembly.GetEntryAssembly().GetName().Version ;
            SetStatus("Done");
            LoadSettings();

            autoZoomToolStripMenuItem_Click(this, null);//Set preview zoom mode
        }
        //Width confirmed by user by the enter key
        private void tbWidth_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent any not allowed char
            if (!CheckDigitFloat(e.KeyChar))
            {
                e.Handled = true;//Stop the character from being entered into the control since it is non-numerical.
                return;
            }

            if (e.KeyChar==Convert.ToChar(13))
            {              
                WidthChangedCheck();
            }
        }
        //Height confirmed by user by the enter key
        private void tbHeight_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent any not allowed char
            if (!CheckDigitFloat(e.KeyChar))
            {
                e.Handled = true;//Stop the character from being entered into the control since it is non-numerical.
                return;
            }
            if (e.KeyChar == Convert.ToChar(13))
            {             
                HeightChangedCheck();
            }
        }
        //Resolution confirmed by user by the enter key
        private void tbRes_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent any not allowed char
            if (!CheckDigitFloat(e.KeyChar))
            {
                e.Handled = true;//Stop the character from being entered into the control since it is non-numerical.
                return;
            }
            if (e.KeyChar == Convert.ToChar(13))
            {
                ResolutionChangedCheck();
            }
        }
        //Width control leave focus. Check if new value
        private void tbWidth_Leave(object sender, EventArgs e)
        {
            WidthChangedCheck();
        }
        //Height control leave focus. Check if new value
        private void tbHeight_Leave(object sender, EventArgs e)
        {
            HeightChangedCheck();
        }
        //Resolution control leave focus. Check if new value
        private void tbRes_Leave(object sender, EventArgs e)
        {
            ResolutionChangedCheck();
        }
        //Width control get focusv
        private void tbWidth_Enter(object sender, EventArgs e)
        {
            try
            {
                lastRatioValue = Convert.ToSingle(tbWidth.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception ex) { Console.WriteLine(ex);}
        }
        //Height control get focus. Backup actual value for check changes.
        private void tbHeight_Enter(object sender, EventArgs e)
        {
            try
            {
                lastRatioValue = Convert.ToSingle(tbHeight.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception ex) { Console.WriteLine(ex);}
        }
        //Resolution control get focus. Backup actual value for check changes.
        private void tbRes_Enter(object sender, EventArgs e)
        {
            try
            {
                lastRatioValue = Convert.ToSingle(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception ex) { Console.WriteLine(ex);}
        }
        
        //Generate button click
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            float resol = Convert.ToSingle(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat);//Resolution (or laser spot size)
            float w = Convert.ToSingle(tbWidth.Text, CultureInfo.InvariantCulture.NumberFormat);//Get the user input value only for check for cancel if not valid         
            float h = Convert.ToSingle(tbHeight.Text, CultureInfo.InvariantCulture.NumberFormat);//Get the user input value only for check for cancel if not valid              

            if ((resol <= 0) | (bgCode.XSize < 1) | (bgCode.YSize < 1) | (w < 1) | (h < 1))
            {
                MessageBox.Show("Check width, height and resolution values.", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (Convert.ToInt32(tbFeedRate.Text) < 1)
            {
                MessageBox.Show("Check feedrate value.", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel) return;

            //Int32 lin;//top/bottom pixel
            //Int32 col;//Left/right pixel

            SetStatus("Generating file...");

            char szChar;//Use 'S' or 'Z' for test laser power
            if (rbUseS.Checked) szChar = 'S'; else szChar = 'Z';

            GenerateGCode gc = new GenerateGCode();
            GenerateGCodeResults gcResults = gc.GenerateFileData(bgCode.AdjImage, szChar, imperialinToolStripMenuItem.Checked,
                rtbPreGcode, tbFeedRate.Text, cbEngravingPattern.Text, resol, tbLaserMin.Text, tbLaserMax.Text, cbEdgeLines.Checked,
                tbRes.Text);
            //fileLines = new List<string>();
            ////S or Z use as power command
            //if (rbUseS.Checked) szChar = 'S'; else szChar = 'Z';

            //WriteHeaderInfo(fileLines);

            //foreach(string s in rtbPreGcode.Lines)
            //{
            //    fileLines.Add(s);
            //}

            //fileLines.Add("G90\r");//Absolute coordinates

            //WriteMeasurementInfo(fileLines, imperialinToolStripMenuItem.Checked);

            //fileLines.Add("F" + tbFeedRate.Text + "\r");//Feed rate

            //float lastX;//Last x/y  coords for compare
            //float lastY;
            //Int32 lastSz;//last 'S' value for compare
            //float coordY;

            ////Add the pre-Gcode lines
            //lastX = -1;//reset last positions
            //lastY = -1;
            //lastSz = -1;
            ////Generate picture Gcode
            //Int32 pixTot = bgCode.XSize * bgCode.YSize;
            //Int32 pixBurned = 0;
            ////////////////////////////////////////////////
            //// Generate Gcode lines by Horizontal scanning
            ////////////////////////////////////////////////
            //if (cbEngravingPattern.Text == "Horizontal scanning")
            //{
            //    //Goto rapid move to left top corner
            //    fileLines.Add("G0X0Y" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", bgCode.YSize * Convert.ToSingle(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat)));
            //    fileLines.Add("G1\r");//G1 mode
            //    fileLines.Add("M3\r");//Switch laser on

            //    //Start image
            //    lin = bgCode.YSize - 1;//top tile
            //    col = 0;//Left pixel
            //    while (lin >= 0)
            //    {
            //        //Y coordinate
            //        coordY = resol * (float)lin;
            //        while (col < bgCode.XSize)//From left to right
            //        {
            //            GenerateXLineData(fileLines, coordY, lastX, lastY, lastSz, szChar, col, resol, lin, pixBurned);
            //            col++;
            //        }
            //        col--;
            //        lin--;
            //        coordY = resol * (float)lin;
            //        while ((col >= 0) & (lin >= 0))//From right to left
            //        {
            //            GenerateXLineData(fileLines, coordY, lastX, lastY, lastSz, szChar, col, resol, lin, pixBurned);
            //            col--;
            //        }
            //        col++;
            //        lin--;
            //        SetStatus("Generating file... " + Convert.ToString((pixBurned*100)/pixTot ) + "%");
            //    }

            //}
            ////////////////////////////////////////////////
            //// Generate Gcode lines by Diagonal scanning
            ////////////////////////////////////////////////
            //else
            //{
            //    //Goto rapid move to left top corner
            //    fileLines.Add("G0X0Y0");
            //    fileLines.Add("G1\r");//G1 mode
            //    fileLines.Add("M3\r");//Switch laser on

            //    //Start image
            //    col = 0;
            //    lin = 0;
            //    while ((col < bgCode.XSize)|(lin<bgCode.YSize))          
            //    {
            //        while ((col < bgCode.XSize)&(lin>=0))
            //        {
            //            //Y coordinate
            //            coordY = resol * (float)lin;
            //            GenerateXLineData(fileLines, coordY, lastX, lastY, lastSz, szChar, col, resol, lin, pixBurned);
            //            col++;
            //            lin--;
            //        }
            //        col--;
            //        lin++;

            //        if (col >= bgCode.XSize-1) lin++;
            //        else col++;
            //        while ((col >=0)&(lin<bgCode.YSize))
            //        {
            //            //Y coordinate
            //            coordY = resol * (float)lin;
            //            GenerateXLineData(fileLines, coordY, lastX, lastY, lastSz, szChar, col, resol, lin, pixBurned);
            //            col--;
            //            lin++;
            //        }
            //        col++;
            //        lin--;
            //        if (lin >= bgCode.YSize-1) col++;
            //        else lin++;
            //        SetStatus("Generating file... " + Convert.ToString((pixBurned * 100) / pixTot) + "%"); 
            //    }
            //}
            ////Edge lines
            //WriteEdgeLines(fileLines, cbEdgeLines.Checked, resol);

            ////Switch laser off
            //fileLines.Add("M5\r");//G1 mode

            //WritePostGCode(fileLines);

            SetStatus("Saving file...");
            //Save file
            File.WriteAllLines(saveFileDialog1.FileName , gcResults.FileLines);
            SetStatus("Done (" + Convert.ToString(gcResults.PixBurned) + "/" + Convert.ToString(gcResults.PixTotal) +")");
        }

        //private void WriteEdgeLines(List<string> fileLines, bool edgeLine, float resol)
        //{
        //    if (edgeLine)
        //    {
        //        fileLines.Add("M5\r");
        //        fileLines.Add("G0X0Y0\r");
        //        fileLines.Add("M3S" + tbLaserMax.Text + "\r");
        //        fileLines.Add("G1X0Y" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", (bgCode.YSize - 1) * resol) + "\r");
        //        fileLines.Add("G1X" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", (bgCode.XSize - 1) * resol) + "Y" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", (bgCode.YSize - 1) * resol) + "\r");
        //        fileLines.Add("G1X" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", (bgCode.XSize - 1) * resol) + "Y0\r");
        //        fileLines.Add("G1X0Y0\r");
        //    }
        //}

        //Horizontal mirroring
        private void btnHorizMirror_Click(object sender, EventArgs e)
        {
            RotateImage(RotateFlipType.RotateNoneFlipX, false);
        }
        //Vertical mirroring
        private void btnVertMirror_Click(object sender, EventArgs e)
        {
            RotateImage(RotateFlipType.RotateNoneFlipY, false);
        }
        //Rotate right
        private void btnRotateRight_Click(object sender, EventArgs e)
        {
            RotateImage(RotateFlipType.Rotate90FlipNone, true);
            autoZoomToolStripMenuItem_Click(this, null);
        }
        //Rotate left
        private void btnRotateLeft_Click(object sender, EventArgs e)
        {
            RotateImage(RotateFlipType.Rotate270FlipNone, true);
            autoZoomToolStripMenuItem_Click(this, null);
        }
        private void RotateImage(RotateFlipType r, Boolean changeRatio)
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            SetStatus("Rotating...");
            bgCode.RotateFlip(r);
            UpdateImage(changeRatio);
            SetStatus("Done");
        }
        //Invert image color
        private void btnInvert_Click(object sender, EventArgs e)
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            SetStatus("Invert Image...");
            pictureBox1.Image = bgCode.InvertImage();
            SetStatus("Done");
            UpdateImage(false);
        }

        private void cbDirthering_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            if (cbDirthering.Text == "Dirthering FS 1 bit")
            {
                SetStatus("Dirthering...");
                pictureBox1.Image = bgCode.DirtherImage();
                SetStatus("Done");
            }
            else
                UserAdjusted();
        }
        //Feed rate Text changed
        private void tbFeedRate_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent any not allowed char
            if (!CheckDigitInteger(e.KeyChar))
            {
                e.Handled = true;//Stop the character from being entered into the control since it is non-numerical.
                return;
            }
        }
        //Metric units selected
        private void metricmmToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imperialinToolStripMenuItem.Checked = false;
            gbDimensions.Text = "Output (mm)";
            lblFeedRateUnits.Text = "mm/min";
        }
        //Imperial unitsSelected
        private void imperialinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            metricmmToolStripMenuItem.Checked = false;
            gbDimensions.Text = "Output (in)";
            lblFeedRateUnits.Text = "in/min";
        }
        //About dialog
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            about frmAbout = new about();
            frmAbout.ShowDialog();
        }
        //Preview AutoZoom
        private void autoZoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (autoZoomToolStripMenuItem.Checked)
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Width = panel1.Width;
                pictureBox1.Height = panel1.Height;
                pictureBox1.Top = 0;
                pictureBox1.Left = 0;
            }
            else
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                if (pictureBox1.Width > panel1.Width) pictureBox1.Left = 0; else pictureBox1.Left = (panel1.Width / 2) - (pictureBox1.Width / 2);
                if (pictureBox1.Height > panel1.Height) pictureBox1.Top = 0; else pictureBox1.Top = (panel1.Height / 2) - (pictureBox1.Height / 2);
            }
        }
        //Laser Min keyPress
        private void tbLaserMin_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent any not allowed char
            if (!CheckDigitInteger(e.KeyChar))
            {
                e.Handled = true;//Stop the character from being entered into the control since it is non-numerical.
                return;
            }
        }
        //Laser Max keyPress
        private void tbLaserMax_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent any not allowed char
            if (!CheckDigitInteger(e.KeyChar))
            {
                e.Handled = true;//Stop the character from being entered into the control since it is non-numerical.
                return;
            }
        }
        //OpenFile, save picture gray scaled to originalImage and save the original aspect ratio to ratio
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.Cancel) return;//if no image, do nothing
                if (!File.Exists(openFileDialog1.FileName)) return;
                SetStatus( "Opening file...");
                if (bgCode.ResetImage != null)
                {
                    bgCode.ResetImage = null;
                }
                bgCode.OrgImage = new Bitmap(Image.FromFile(openFileDialog1.FileName));
                tBarBrightness.Value = bgCode.Brightness;
                tBarContrast.Value = bgCode.Contrast;
                tBarGamma.Value = bgCode.Gamma;
                lblBrightness.Text = bgCode.BrightnessText ;
                lblContrast.Text = bgCode.ContrastText ;
                lblGamma.Text = bgCode.GammaText ;
                ratio = (bgCode.XSize + 0.0f) / bgCode.YSize;//Save ratio for future use if needled
                if (cbLockRatio.Checked) tbHeight.Text = Convert.ToString((Convert.ToSingle(tbWidth.Text) / ratio), CultureInfo.InvariantCulture.NumberFormat);//Initialize y size
                UserAdjusted();
                SetStatus("Done");
            }
            catch (Exception err)
            {
                MessageBox.Show("Error opening file: " + err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //Exit Menu
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        //On form closing
        private void main_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }

        private void SetStatus(string message)
        {
            lblStatus.Text = message;
            Refresh();
        }
        private void UpdateImage(Boolean changeRatio)
        {
            pictureBox1.Image = bgCode.AdjImage;
            if (changeRatio)
            {
                ratio = 1 / ratio;
                string s = tbHeight.Text;
                tbHeight.Text = tbWidth.Text;
                tbWidth.Text = s;
            }
        }
















    }
}
