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


namespace _3dpBurnerImage2Gcode
{
    public partial class main : Form
    {
        const string ver = "v0.1";
        BurnGCode bgCode;
        float lastValue;//Aux for apply processing to image only when a new value is detected
        public main()
        {
            InitializeComponent();
        }
        float ratio; //Used to lock the aspect ratio when the option is selected
        //Save settings
        private void saveSettings()
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
        private void loadSettings()
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
        private Int32 interpolate(Int32 grayValue, Int32 min, Int32 max)
        {
            Int32 dif=max-min;
            return (min + ((grayValue * dif) / 255));
        }

        //Return true if char is a valid float digit, show error message is not and return false
        private bool checkDigitFloat(char ch )
        {
            if ((ch != '.') & (ch != '0') & (ch != '1') & (ch != '2') & (ch != '3') & (ch != '4') & (ch != '5') & (ch != '6') & (ch != '7') & (ch != '8') & (ch != '9') & (Convert.ToByte(ch) != 8) & (Convert.ToByte(ch) != 13))//is a 0-9 numbre or . decimal separator, backspace or enter
            {
                MessageBox.Show("Allowed chars are '0'-'9' and '.' as decimal separator", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }
        //Return true if char is a valid integer digit, show eror message is not and return false
        private bool checkDigitInteger(char ch)
        {
            if ((ch != '0') & (ch != '1') & (ch != '2') & (ch != '3') & (ch != '4') & (ch != '5') & (ch != '6') & (ch != '7') & (ch != '8') & (ch != '9') & (Convert.ToByte(ch) != 8) & (Convert.ToByte(ch) != 13))//is a 0-9 numbre or . decimal separator, backspace or enter
            {
                MessageBox.Show("Allowed chars are '0'-'9'", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }
     
        //Invoked when the user input any value for image adjust
        private void userAdjust()
        {
            try
            {
                if (bgCode.OrgImage == null) return;//if no image, do nothing
                //Apply resize to original image
                bgCode.xSize = Convert.ToInt32(float.Parse(tbWidth.Text, CultureInfo.InvariantCulture.NumberFormat) / float.Parse(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat));
                bgCode.ySize = Convert.ToInt32(float.Parse(tbHeight.Text, CultureInfo.InvariantCulture.NumberFormat) / float.Parse(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat));
                lblStatus.Text = "Adjust Image...";
                Refresh();
                //Display image
                pictureBox1.Image = bgCode.AdjImage;
                //Reset dirthering to adjusted (resized and balanced) image
                cbDirthering.Text = bgCode.Dirthering;
                //Set preview
                autoZoomToolStripMenuItem_Click(this, null);
                lblStatus.Text = "Done...";
                Refresh();
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
            Refresh();
            userAdjust(); 
        }
        //Brightness adjusted by user
        private void tBarBrightness_Scroll(object sender, EventArgs e)
        {
            lblBrightness.Text = Convert.ToString(tBarBrightness.Value);
            Refresh();
            userAdjust();          
        }
        //Gamma adjusted by user
        private void tBarGamma_Scroll(object sender, EventArgs e)
        {
            lblGamma.Text = Convert.ToString(tBarGamma.Value/100.0f);
            Refresh();
            userAdjust(); 
        }
        private void ResetImage()
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            bgCode.OrgImage = bgCode.ResetImage; 
            lblStatus.Text = "Loading original image...";
            Refresh();
            pictureBox1.Image = bgCode.AdjImage;
            lblStatus.Text = "Done";
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
        private void widthChangedCheck()
        {
            try
            {
                if (bgCode.OrgImage == null) return;//if no image, do nothing
                float newValue = Convert.ToSingle(tbWidth.Text, CultureInfo.InvariantCulture.NumberFormat);//Get the user input value           
                if (newValue == lastValue) return;//if not is a new value do nothing     
                lastValue = Convert.ToSingle(tbWidth.Text, CultureInfo.InvariantCulture.NumberFormat);
                if (cbLockRatio.Checked)
                {
                    tbHeight.Text = Convert.ToString((newValue / ratio), CultureInfo.InvariantCulture.NumberFormat);
                }
                userAdjust();
            }
            catch 
            {
                MessageBox.Show("Check width value.", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //Check if a new image height has been confirmed by user, process it.
        private void heightChangedCheck()
        {
            try
            {
                if (bgCode.OrgImage == null) return;//if no image, do nothing
                float newValue = Convert.ToSingle(tbHeight.Text, CultureInfo.InvariantCulture.NumberFormat);//Get the user input value   
                if (newValue == lastValue) return;//if not is a new value do nothing
                lastValue = Convert.ToSingle(tbHeight.Text, CultureInfo.InvariantCulture.NumberFormat);
                if (cbLockRatio.Checked)
                {
                    tbWidth.Text = Convert.ToString((newValue * ratio), CultureInfo.InvariantCulture.NumberFormat);
                }
                userAdjust();
            }
            catch {
                MessageBox.Show("Check height value.", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //Check if a new image resolution has been confirmed by user, process it.
        private void resolutionChangedCheck()
        {
            try
            {
                if (bgCode.OrgImage == null) return;//if no image, do nothing
                float newValue = Convert.ToSingle(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat);//Get the user input value
                if (newValue == lastValue) return;//if not is a new value do nothing
                lastValue = Convert.ToSingle(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat);
                userAdjust();
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
                userAdjust();
            }
        }
        //On form load
        private void Form1_Load(object sender, EventArgs e)
        {
            Text = "3dpBurner Image2Gcode " + ver;
            lblStatus.Text = "Done";
            loadSettings();

            autoZoomToolStripMenuItem_Click(this, null);//Set preview zoom mode
        }
        //Width confirmed by user by the enter key
        private void tbWidth_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent any not allowed char
            if (!checkDigitFloat(e.KeyChar))
            {
                e.Handled = true;//Stop the character from being entered into the control since it is non-numerical.
                return;
            }

            if (e.KeyChar==Convert.ToChar(13))
            {              
                widthChangedCheck();
            }
        }
        //Height confirmed by user by the enter key
        private void tbHeight_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent any not allowed char
            if (!checkDigitFloat(e.KeyChar))
            {
                e.Handled = true;//Stop the character from being entered into the control since it is non-numerical.
                return;
            }
            if (e.KeyChar == Convert.ToChar(13))
            {             
                heightChangedCheck();
            }
        }
        //Resolution confirmed by user by the enter key
        private void tbRes_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent any not allowed char
            if (!checkDigitFloat(e.KeyChar))
            {
                e.Handled = true;//Stop the character from being entered into the control since it is non-numerical.
                return;
            }
            if (e.KeyChar == Convert.ToChar(13))
            {
                resolutionChangedCheck();
            }
        }
        //Width control leave focus. Check if new value
        private void tbWidth_Leave(object sender, EventArgs e)
        {
            widthChangedCheck();
        }
        //Height control leave focus. Check if new value
        private void tbHeight_Leave(object sender, EventArgs e)
        {
            heightChangedCheck();
        }
        //Resolution control leave focus. Check if new value
        private void tbRes_Leave(object sender, EventArgs e)
        {
            resolutionChangedCheck();
        }
        //Width control get focusv
        private void tbWidth_Enter(object sender, EventArgs e)
        {
            try
            {
                lastValue = Convert.ToSingle(tbWidth.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch { }
        }
        //Height control get focus. Backup actual value for check changes.
        private void tbHeight_Enter(object sender, EventArgs e)
        {
            try
            {
                lastValue = Convert.ToSingle(tbHeight.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch { }
        }
        //Resolution control get focus. Backup actual value for check changes.
        private void tbRes_Enter(object sender, EventArgs e)
        {
            try
            {
                lastValue = Convert.ToSingle(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch { }
        }
        //Generate a "minimalist" gcode line based on the actual and last coordinates and laser power
        string line;
        float coordX;//X
        float coordY;//Y
        Int32 sz;//S (or Z)
        float lastX;//Last x/y  coords for compare
        float lastY;
        Int32 lastSz;//last 'S' value for compare
        char szChar;//Use 'S' or 'Z' for test laser power
        string coordXStr;//String formated X
        string coordYStr;////String formated Y
        string szStr;////String formated S
        private void generateLine()
        {
            //Generate Gcode line
            line = "";
            if (coordX != lastX)//Add X coord to line if is different from previous             
            {
                coordXStr = string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", coordX);
                line += 'X' + coordXStr;
            }
            if (coordY != lastY)//Add Y coord to line if is different from previous
            {
                coordYStr = string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", coordY);
                line += 'Y' + coordYStr;
            }
            if (sz != lastSz)//Add power value to line if is different from previous
            {
                szStr = szChar + Convert.ToString(sz);
                line += szStr;
            }
        }
        //Generate button click
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            float resol = Convert.ToSingle(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat);//Resolution (or laser spot size)
            float w = Convert.ToSingle(tbWidth.Text, CultureInfo.InvariantCulture.NumberFormat);//Get the user input value only for check for cancel if not valid         
            float h = Convert.ToSingle(tbHeight.Text, CultureInfo.InvariantCulture.NumberFormat);//Get the user input value only for check for cancel if not valid              

            if ((resol <= 0) | (bgCode.xSize < 1) | (bgCode.ySize < 1) | (w < 1) | (h < 1))
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

            Int32 lin;//top/botom pixel
            Int32 col;//Left/right pixel

            lblStatus.Text="Generating file...";
            Refresh();

            List<string> fileLines;
            fileLines = new List<string>();
            //S or Z use as power command
            if (rbUseS.Checked) szChar = 'S'; else szChar = 'Z';

            //first Gcode line
            line = "(Generated by 3dpBurner Image2Gcode " + ver+")";
            fileLines.Add(line);
            line="(@"+DateTime.Now.ToString("MMM/dd/yyy HH:mm:ss)");
            fileLines.Add(line);



            line = "M5\r";//Make sure laser off
            fileLines.Add(line);

            //Add the pre-Gcode lines
            lastX = -1;//reset last positions
            lastY = -1;
            lastSz = -1;
            foreach(string s in rtbPreGcode.Lines)
            {
                fileLines.Add(s);
            }
            line = "G90\r";//Absolute coordinates
            fileLines.Add(line);

            if (imperialinToolStripMenuItem.Checked) line = "G20\r";//Imperial units
                else line = "G21\r";//Metric units
            fileLines.Add(line);
            line = "F" + tbFeedRate.Text + "\r";//Feed rate
            fileLines.Add(line);

            //Generate picture Gcode
            Int32 pixTot = bgCode.xSize * bgCode.ySize;
            Int32 pixBurned = 0;
            //////////////////////////////////////////////
            // Generate Gcode lines by Horizontal scanning
            //////////////////////////////////////////////
            if (cbEngravingPattern.Text == "Horizontal scanning")
            {
                //Goto rapid move to left top corner
                line = "G0X0Y" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", bgCode.ySize * Convert.ToSingle(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat));
                fileLines.Add(line);
                line = "G1\r";//G1 mode
                fileLines.Add(line);
                line = "M3\r";//Switch laser on
                fileLines.Add(line);

                //Start image
                lin = bgCode.ySize - 1;//top tile
                col = 0;//Left pixel
                while (lin >= 0)
                {
                    //Y coordinate
                    coordY = resol * (float)lin;
                    while (col < bgCode.xSize)//From left to right
                    {
                        //X coordinate
                        coordX = resol * (float)col;
                        //Power value
                        Color cl = bgCode.AdjImage.GetPixel(col, (bgCode.ySize - 1) - lin);//Get pixel color
                        sz = 255 - cl.R;
                        sz = interpolate(sz, Convert.ToInt32(tbLaserMin.Text), Convert.ToInt32(tbLaserMax.Text));
                        generateLine();
                        pixBurned++;
                        //adjustedImage.SetPixel(col, (adjustedImage.Height-1)-lin, Color.Red);
                        //pictureBox1.Image = adjustedImage;
                        //Refresh();

                        if (!string.IsNullOrEmpty(line)) fileLines.Add(line);
                        lastX = coordX;
                        lastY = coordY;
                        lastSz = sz;
                        col++;
                    }
                    col--;
                    lin--;
                    coordY = resol * (float)lin;
                    while ((col >= 0) & (lin >= 0))//From right to left
                    {
                        //X coordinate
                        coordX = resol * (float)col;
                        //Power value
                        Color cl = bgCode.AdjImage.GetPixel(col, (bgCode.ySize - 1) - lin);//Get pixel color
                        sz = 255 - cl.R;
                        sz = interpolate(sz, Convert.ToInt32(tbLaserMin.Text), Convert.ToInt32(tbLaserMax.Text));
                        generateLine();
                        pixBurned++;
                        //adjustedImage.SetPixel(col, (adjustedImage.Height-1)-lin, Color.Red);
                        //pictureBox1.Image = adjustedImage;
                        //Refresh();

                        if (!string.IsNullOrEmpty(line)) fileLines.Add(line);
                        lastX = coordX;
                        lastY = coordY;
                        lastSz = sz;
                        col--;
                    }
                    col++;
                    lin--;
                    lblStatus.Text = "Generating file... " + Convert.ToString((pixBurned*100)/pixTot ) + "%";
                    Refresh();
                }

            }
            //////////////////////////////////////////////
            // Generate Gcode lines by Diagonal scanning
            //////////////////////////////////////////////
            else
            {
                //Goto rapid move to left top corner
                line = "G0X0Y0";
                fileLines.Add(line);
                line = "G1\r";//G1 mode
                fileLines.Add(line);
                line = "M3\r";//Switch laser on
                fileLines.Add(line);

                //Start image
                col = 0;
                lin = 0;
                while ((col < bgCode.xSize)|(lin<bgCode.ySize))          
                {
                    while ((col < bgCode.xSize)&(lin>=0))
                    {
                        //Y coordinate
                        coordY = resol * (float)lin;
                        //X coordinate
                        coordX = resol * (float)col;

                        //Power value
                        Color cl = bgCode.AdjImage.GetPixel(col, (bgCode.ySize - 1) - lin);//Get pixel color
                        sz = 255 - cl.R;
                        sz = interpolate(sz, Convert.ToInt32(tbLaserMin.Text), Convert.ToInt32(tbLaserMax.Text));

                        generateLine();
                        pixBurned++;

                        //adjustedImage.SetPixel(col, (adjustedImage.Height-1)-lin, Color.Red);
                        //pictureBox1.Image = adjustedImage;
                        //Refresh();

                        if (!string.IsNullOrEmpty(line)) fileLines.Add(line);
                        lastX = coordX;
                        lastY = coordY;
                        lastSz = sz;

                        col++;
                        lin--;
                    }
                    col--;
                    lin++;

                    if (col >= bgCode.xSize-1) lin++;
                    else col++;
                    while ((col >=0)&(lin<bgCode.ySize))
                    {
                        //Y coordinate
                        coordY = resol * (float)lin;
                        //X coordinate
                        coordX = resol * (float)col;

                        //Power value
                        Color cl = bgCode.AdjImage.GetPixel(col, (bgCode.ySize - 1) - lin);//Get pixel color
                        sz = 255 - cl.R;
                        sz = interpolate(sz, Convert.ToInt32(tbLaserMin.Text), Convert.ToInt32(tbLaserMax.Text));

                        generateLine();
                        pixBurned++;
                    
                        //adjustedImage.SetPixel(col, (adjustedImage.Height-1)-lin, Color.Red);
                        //pictureBox1.Image = adjustedImage;
                       // Refresh();

                        if (!string.IsNullOrEmpty(line)) fileLines.Add(line);
                        lastX = coordX;
                        lastY = coordY;
                        lastSz = sz;

                        col--;
                        lin++;
                    }
                    col++;
                    lin--;
                    if (lin >= bgCode.ySize-1) col++;
                    else lin++;
                    lblStatus.Text = "Generating file... " + Convert.ToString((pixBurned * 100) / pixTot) + "%"; 
                    Refresh();
                }
            }
            //Edge lines
            if (cbEdgeLines.Checked)
            {
                line = "M5\r";
                fileLines.Add(line);
                line = "G0X0Y0\r";
                fileLines.Add(line);
                line = "M3S" + tbLaserMax.Text + "\r";
                fileLines.Add(line);
                line = "G1X0Y"+string.Format(CultureInfo.InvariantCulture.NumberFormat,"{0:0.###}",(bgCode.ySize-1)*resol)+"\r";
                fileLines.Add(line);
                line = "G1X" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}", (bgCode.xSize - 1) * resol) + "Y" +string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}",(bgCode.ySize- 1) * resol) + "\r";
                fileLines.Add(line);
                line = "G1X" + string.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:0.###}",(bgCode.xSize - 1)*resol) + "Y0\r";
                fileLines.Add(line);
                line = "G1X0Y0\r";
                fileLines.Add(line);
            }
            //Switch laser off
            line = "M5\r";//G1 mode
            fileLines.Add(line);

            //Add the post-Gcode 
            foreach (string s in rtbPostGcode.Lines)
            {
                fileLines.Add(s);
            }
            lblStatus.Text="Saving file...";
            Refresh();
            //Save file
            File.WriteAllLines(saveFileDialog1.FileName , fileLines);
            lblStatus.Text = "Done (" + Convert.ToString(pixBurned) + "/" + Convert.ToString(pixTot)+")";
        }
        //Horizontal mirroring
        private void btnHorizMirror_Click(object sender, EventArgs e)
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            lblStatus.Text = "Mirroring...";
            Refresh();
            bgCode.OrgImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
            pictureBox1.Image = bgCode.AdjImage;
            lblStatus.Text = "Done";
        }
        //Vertical mirroring
        private void btnVertMirror_Click(object sender, EventArgs e)
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            lblStatus.Text = "Mirroring...";
            Refresh();
            bgCode.OrgImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
            pictureBox1.Image = bgCode.AdjImage;
            lblStatus.Text = "Done";
        }
        //Rotate right
        private void btnRotateRight_Click(object sender, EventArgs e)
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            lblStatus.Text = "Rotating...";
            Refresh();
            bgCode.OrgImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
            ratio = 1 / ratio;
            string s = tbHeight.Text;
            tbHeight.Text = tbWidth.Text;
            tbWidth.Text = s;
            pictureBox1.Image = bgCode.AdjImage;
            autoZoomToolStripMenuItem_Click(this, null);
            lblStatus.Text = "Done";
        }
        //Rotate left
        private void btnRotateLeft_Click(object sender, EventArgs e)
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            lblStatus.Text = "Rotating...";
            Refresh();
            bgCode.OrgImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
            ratio = 1 / ratio;
            string s = tbHeight.Text;
            tbHeight.Text = tbWidth.Text;
            tbWidth.Text = s;
            pictureBox1.Image = bgCode.AdjImage ;
            autoZoomToolStripMenuItem_Click(this, null);
            lblStatus.Text = "Done";
        }
        //Invert image color
        private void btnInvert_Click(object sender, EventArgs e)
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            pictureBox1.Image = bgCode.InvertImage();
        }

        private void cbDirthering_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (bgCode.OrgImage == null) return;//if no image, do nothing
            if (cbDirthering.Text == "Dirthering FS 1 bit")
            {
                lblStatus.Text = "Dirthering...";
                pictureBox1.Image = bgCode.DirtherImage();
                lblStatus.Text = "Done";
            }
            else
                userAdjust();
        }
        //Feed rate Text changed
        private void tbFeedRate_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent any not allowed char
            if (!checkDigitInteger(e.KeyChar))
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
            if (!checkDigitInteger(e.KeyChar))
            {
                e.Handled = true;//Stop the character from being entered into the control since it is non-numerical.
                return;
            }
        }
        //Laser Max keyPress
        private void tbLaserMax_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent any not allowed char
            if (!checkDigitInteger(e.KeyChar))
            {
                e.Handled = true;//Stop the character from being entered into the control since it is non-numerical.
                return;
            }
        }
        //OpenFile, save picture grayscaled to originalImage and save the original aspect ratio to ratio
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.Cancel) return;//if no image, do nothing
                if (!File.Exists(openFileDialog1.FileName)) return;
                lblStatus.Text = "Opening file...";
                Refresh();
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
                ratio = (bgCode.xSize + 0.0f) / bgCode.ySize;//Save ratio for future use if needled
                if (cbLockRatio.Checked) tbHeight.Text = Convert.ToString((Convert.ToSingle(tbWidth.Text) / ratio), CultureInfo.InvariantCulture.NumberFormat);//Initialize y size
                userAdjust();
                lblStatus.Text = "Done";
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
            saveSettings();
        }




















    }
}
