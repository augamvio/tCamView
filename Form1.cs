using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using AForge.Video;
using AForge.Video.DirectShow;

namespace tCamView
{

    public partial class Form1 : Form
    {
        // 타원형 윈도우를 만들기 위함.
        // https://stackoverflow.com/questions/5092216/c-sharp-form-with-custom-border-and-rounded-edges
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,     // x-coordinate of upper-left corner
            int nTopRect,      // y-coordinate of upper-left corner
            int nRightRect,    // x-coordinate of lower-right corner
            int nBottomRect,   // y-coordinate of lower-right corner
            int nWidthEllipse, // height of ellipse
            int nHeightEllipse // width of ellipse
        );

        private FilterInfoCollection webcam;
        private VideoCaptureDevice cam;
        List<string> videoCaptureDevicesList = new List<string>();
        List<string> videoCapabilitiesList = new List<string>();

        bool state_flip_vertical = false;
        bool state_flip_horizontal = false;
        int currCamID = -1;
        int currSizeID = -1;
        int cropSize = 0;

        //private object lockObject = new object();

        MenuItem Menu_VideoCaptureDevices = new MenuItem("Video Capture Devices");
        MenuItem Menu_VideoCapabilities = new MenuItem("Video Resolutions");

        bool firstimage_captured = false;

        int videoCaptureDevicesListCount = 0;
        int videoCapabilitiesListCount = 0;
        int currMenuItem0VideoCaptureDevices = -1;
        int currMenuItem1VideoCapabilities = -1;
        
        int currMenuItem3WindowStyles = 0;
        const int currMenuItem3WindowStylesCount = 5;

        bool stretchKeepAspectRatio = true;  // Alt.Stretch, UniformToFill

        public Form1()
        {
            InitializeComponent();

            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.ControlBox = true;
            this.Icon = Properties.Resources.webcam;
            this.Opacity = 1.0;

            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            stretchKeepAspectRatio = true;
            this.Text = "tCamView (alt.stretch)";

            // DPI aware 
            // https://help.syncfusion.com/windowsforms/highdpi-support
            // https://stackoverrun.com/ko/q/8936019
            // see app.manifest
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                webcam = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                foreach (FilterInfo VideoCaptureDevice in webcam)
                {
                    videoCaptureDevicesList.Add(VideoCaptureDevice.Name);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
            Focus();

            for (var i = 0; i < videoCaptureDevicesList.Count; i++) 
            {
                MenuItem mi = new MenuItem(videoCaptureDevicesList[i]);
                mi.Click += new EventHandler(MenuItem_VideoCaptureDevices_Click);
                mi.Tag = new object[] { i };
                Menu_VideoCaptureDevices.MenuItems.Add(mi);
            }

            videoCaptureDevicesListCount = videoCaptureDevicesList.Count;
            for (int i = 0; i < videoCaptureDevicesList.Count; i++)
            {
                if (openVideoCaptureDevice(i, 0) == 1)
                {
                    currMenuItem0VideoCaptureDevices = i;
                    currMenuItem1VideoCapabilities = 0;
                    break;
                }
            }

            // Update the Checked mark...
            UpdateMenuItemsChecked();

            // Always on Top
            this.TopMost = true;
        }

        private int openVideoCaptureDevice(int deviceID, int sizeID)
        {
            if (cam != null)
            {
                closeVideoCaptureDevice();
            }
            cropSize = 0;

            cam = new VideoCaptureDevice(webcam[deviceID].MonikerString);
            cam.NewFrame += new NewFrameEventHandler(cam_NewFrame);
            //cam.DisplayPropertyPage(new IntPtr(0));

            var videoCapabilities = cam.VideoCapabilities;
            videoCapabilitiesList.Clear();
            foreach (var video in videoCapabilities)
            {
                videoCapabilitiesList.Add(video.FrameSize.Width + "x" + video.FrameSize.Height);
            }

            currCamID = deviceID;
            currSizeID = (sizeID < 0) ? 0 : sizeID;
            if (videoCapabilities.Count() > 0)
                cam.VideoResolution = cam.VideoCapabilities[currSizeID];

            videoCapabilitiesListCount = videoCapabilities.Count();

            //cam.SetCameraProperty(AForge.Video.DirectShow.CameraControlProperty.Exposure, -2, AForge.Video.DirectShow.CameraControlFlags.Manual);
            //cam.SetCameraProperty(AForge.Video.DirectShow.CameraControlProperty.Focus, 0, AForge.Video.DirectShow.CameraControlFlags.Manual);
            //cam.SetCameraProperty(AForge.Video.DirectShow.CameraControlProperty.Zoom, 100, AForge.Video.DirectShow.CameraControlFlags.Manual);
            //videoSource.SetVideoProperty(AForge.Video.DirectShow.VideoProcAmpProperty.BacklightCompensation, 0, AForge.Video.DirectShow.VideoProcAmpFlags.Manual);
            //videoSource.SetVideoProperty(AForge.Video.DirectShow.VideoProcAmpProperty.Contrast, 128, AForge.Video.DirectShow.VideoProcAmpFlags.Manual);
            //videoSource.SetVideoProperty(AForge.Video.DirectShow.VideoProcAmpProperty.Gain, 160, AForge.Video.DirectShow.VideoProcAmpFlags.Manual);
            //videoSource.SetVideoProperty(AForge.Video.DirectShow.VideoProcAmpProperty.WhiteBalance, 5920, AForge.Video.DirectShow.VideoProcAmpFlags.Manual);
            //videoSource.SetVideoProperty(AForge.Video.DirectShow.VideoProcAmpProperty.Sharpness, 128, AForge.Video.DirectShow.VideoProcAmpFlags.Manual);
            //videoSource.SetVideoProperty(AForge.Video.DirectShow.VideoProcAmpProperty.Saturation, 128, AForge.Video.DirectShow.VideoProcAmpFlags.Manual);

            firstimage_captured = false;
            cam.Start();
            
            ContextMenu cm = new ContextMenu();
            Menu_VideoCapabilities.MenuItems.Clear();
            for (var i = 0; i < videoCapabilitiesList.Count; i++)
            {
                MenuItem mi = new MenuItem(videoCapabilitiesList[i]);
                mi.Click += new EventHandler(MenuItem_VideoCapabilties_Click);
                mi.Tag = new object[] { i };
                Menu_VideoCapabilities.MenuItems.Add(mi);
            }

            MenuItem pictureMode = new MenuItem("PictureSize Mode");
            pictureMode.MenuItems.Add(new MenuItem("Zoom [Z]", MenuItem_Zoom_Click));
            pictureMode.MenuItems.Add(new MenuItem("Stretch [X]", MenuItem_Stretch_Click));
            pictureMode.MenuItems.Add(new MenuItem("Center [C]", MenuItem_Center_Click));
            pictureMode.MenuItems.Add(new MenuItem("Alt.Stretch [A]", MenuItem_AltStretch_Click));
            MenuItem borderOptions = new MenuItem("Window Styles");
            borderOptions.MenuItems.Add(new MenuItem("Normal Border (Resizable) [N or Esc]", MenuItem_Normal_Border_Click));
            borderOptions.MenuItems.Add(new MenuItem("Borderless Ellipse (FixedSize) [E]", MenuItem_Borderless_Ellipse_Click));
            borderOptions.MenuItems.Add(new MenuItem("Borderless Rectangle (FixedSize) [R]", MenuItem_Borderless_Rectangle_Click));
            borderOptions.MenuItems.Add(new MenuItem("Borderless Rounded Rectangle (FixedSize) [W]", MenuItem_Borderless_RoundedRectangle_Click));
            borderOptions.MenuItems.Add(new MenuItem("Full Screen [F]", MenuItem_FullScreen_Click));
            MenuItem imageFlipping = new MenuItem("Image Flipping");
            imageFlipping.MenuItems.Add(new MenuItem("Horizontal Flipping [H]", MenuItem_Horizontal_Flipping_Click));
            imageFlipping.MenuItems.Add(new MenuItem("Vertical Flipping [V]", MenuItem_Vertical_Flipping_Click));
            MenuItem opacityControl = new MenuItem("Opacity Control");
            opacityControl.MenuItems.Add(new MenuItem("Opacity Increase [Up Arrow]", MenuItem_Opacity_Increase_Click));
            opacityControl.MenuItems.Add(new MenuItem("Opacity Decrease [Down Arrow]", MenuItem_Opacity_Decrease_Click));
            opacityControl.MenuItems.Add(new MenuItem("Opacity 100% (max) [Right Arrow]", MenuItem_Opacity_100_Click));
            opacityControl.MenuItems.Add(new MenuItem("Opacity 80%", MenuItem_Opacity_80_Click));
            opacityControl.MenuItems.Add(new MenuItem("Opacity 60%", MenuItem_Opacity_60_Click));
            opacityControl.MenuItems.Add(new MenuItem("Opacity 40%", MenuItem_Opacity_40_Click));
            opacityControl.MenuItems.Add(new MenuItem("Opacity 20% (min) [Left Arrow]", MenuItem_Opacity_20_Click));
            MenuItem addFeatures = new MenuItem("Additional Features");
            addFeatures.MenuItems.Add(new MenuItem("GetImage From Clipboard [G or ^V]", MenuItem_GetImageFromClipboard_Click));
            addFeatures.MenuItems.Add(new MenuItem("SetImage To Clipboard [I or ^C]", MenuItem_SetImageToClipboard_Click));
            addFeatures.MenuItems.Add(new MenuItem("SetImage To Clipboard After 5 Sec. [D]", MenuItem_SetImageToClipboardAfter5Sec_Click));
            addFeatures.MenuItems.Add(new MenuItem("CopyScreen To Clipboard [S]", MenuItem_CopyScreenToClipboard_Click));
            addFeatures.MenuItems.Add(new MenuItem("Resume CameraPreview (From ClipboardView) [Space]", MenuItem_ResumeCameraPreviewFromClipboard_Click));
            addFeatures.MenuItems.Add("-");
            addFeatures.MenuItems.Add(new MenuItem("CropImage(ZoomIn) [Page Up]", MenuItem_CropImageZoomIn_Click));
            addFeatures.MenuItems.Add(new MenuItem("CropImage(ZoomOut) [Page Down]", MenuItem_CropImageZoomOut_Click));
            addFeatures.MenuItems.Add("-");
            addFeatures.MenuItems.Add(new MenuItem("Increase the Window Size [P]", MenuItem_IncreaseWindowSize_Click));
            addFeatures.MenuItems.Add(new MenuItem("Decrease the Window Size [M]", MenuItem_DecreaseWindowSize_Click));
            MenuItem onTop = new MenuItem("Always on Top [T]", MenuItem_AlwaysOnTop_Click);
            MenuItem minimizeApp = new MenuItem("Minimize [L]", MenuItem_Minimize_Click);
            MenuItem quitApp = new MenuItem("Quit [Q]", MenuItem_Quit_Click);
            MenuItem aboutApp = new MenuItem("About...", MenuItem_About_Click);

            cm.MenuItems.AddRange(new MenuItem[] { Menu_VideoCaptureDevices, Menu_VideoCapabilities,
                pictureMode, borderOptions, imageFlipping, opacityControl, addFeatures, onTop, minimizeApp, quitApp, aboutApp});
            pictureBox1.ContextMenu = cm;

            // 카메라가 다른 프로그램에 의해 사용중인지 체크하기 위한 트릭
            for (int i = 0; i < 10; i++)
            {
                if (firstimage_captured == true)
                    return 1;
                System.Threading.Thread.Sleep(100);
            }
            return 0;
        }

        private void UpdateMenuItemsChecked()
        {
            for (int i = 0; i < videoCaptureDevicesListCount; i++)
            {
                pictureBox1.ContextMenu.MenuItems[0].MenuItems[i].Checked = false;
            }
            if (currMenuItem0VideoCaptureDevices >= 0 && currMenuItem0VideoCaptureDevices < videoCaptureDevicesListCount)
            {
                pictureBox1.ContextMenu.MenuItems[0].MenuItems[currMenuItem0VideoCaptureDevices].Checked = true;
            }

            for (int i = 0; i < videoCapabilitiesListCount; i++)
            {
                pictureBox1.ContextMenu.MenuItems[1].MenuItems[i].Checked = false;
            }
            if (currMenuItem1VideoCapabilities >= 0 && currMenuItem1VideoCapabilities < videoCapabilitiesListCount)
            {
                pictureBox1.ContextMenu.MenuItems[1].MenuItems[currMenuItem1VideoCapabilities].Checked = true;
            }

            if (pictureBox1.SizeMode == PictureBoxSizeMode.Zoom)
            {
                pictureBox1.ContextMenu.MenuItems[2].MenuItems[0].Checked = true;
                pictureBox1.ContextMenu.MenuItems[2].MenuItems[1].Checked = false;
                pictureBox1.ContextMenu.MenuItems[2].MenuItems[2].Checked = false;
                pictureBox1.ContextMenu.MenuItems[2].MenuItems[3].Checked = false;
            }
            else if (pictureBox1.SizeMode == PictureBoxSizeMode.StretchImage)
            {
                if (stretchKeepAspectRatio == false)
                {
                    pictureBox1.ContextMenu.MenuItems[2].MenuItems[0].Checked = false;
                    pictureBox1.ContextMenu.MenuItems[2].MenuItems[1].Checked = true;
                    pictureBox1.ContextMenu.MenuItems[2].MenuItems[2].Checked = false;
                    pictureBox1.ContextMenu.MenuItems[2].MenuItems[3].Checked = false;
                }
                else
                {
                    pictureBox1.ContextMenu.MenuItems[2].MenuItems[0].Checked = false;
                    pictureBox1.ContextMenu.MenuItems[2].MenuItems[1].Checked = false;
                    pictureBox1.ContextMenu.MenuItems[2].MenuItems[2].Checked = false;
                    pictureBox1.ContextMenu.MenuItems[2].MenuItems[3].Checked = true;
                }
            }
            else if (pictureBox1.SizeMode == PictureBoxSizeMode.CenterImage)
            {
                pictureBox1.ContextMenu.MenuItems[2].MenuItems[0].Checked = false;
                pictureBox1.ContextMenu.MenuItems[2].MenuItems[1].Checked = false;
                pictureBox1.ContextMenu.MenuItems[2].MenuItems[2].Checked = true;
                pictureBox1.ContextMenu.MenuItems[2].MenuItems[3].Checked = false;
            }

            for (int i = 0; i < currMenuItem3WindowStylesCount; i++)
            {
                pictureBox1.ContextMenu.MenuItems[3].MenuItems[i].Checked = false;
            }
            pictureBox1.ContextMenu.MenuItems[3].MenuItems[currMenuItem3WindowStyles].Checked = true;

            pictureBox1.ContextMenu.MenuItems[4].MenuItems[1].Checked = state_flip_vertical;
            pictureBox1.ContextMenu.MenuItems[4].MenuItems[0].Checked = state_flip_horizontal;

            pictureBox1.ContextMenu.MenuItems[7].Checked = this.TopMost;
        }

        private static DateTime Delay(int MS)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);

            while (AfterWards >= ThisMoment)
            {
                System.Windows.Forms.Application.DoEvents();
                ThisMoment = DateTime.Now;
            }

            return DateTime.Now;
        }

        private void MenuItem_SetImageToClipboard_Click(object sender, EventArgs e)
        {
            Clipboard.SetImage(pictureBox1.Image);
        }

        private void MenuItem_SetImageToClipboardAfter5Sec_Click(object sender, EventArgs e)
        {
            if (label1.Visible == true) return;

            label1.Visible = true;
            label1.Text = "5";
            Delay(1000);
            label1.Text = "4";
            Delay(1000);
            label1.Text = "3";
            Delay(1000);
            label1.Text = "2";
            Delay(1000);
            label1.Text = "1";
            Delay(1000);
            label1.Visible = false;

            Clipboard.SetImage(pictureBox1.Image);
        }

        private void MenuItem_CopyScreenToClipboard_Click(object sender, EventArgs e)
        {
            // app.manifest에서 DPI aware를 사용한 경우 
            Delay(300); // 메뉴가 사라지는 시간동안 약간 기다림. 
            Image img = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Graphics g = Graphics.FromImage(img);
            g.CopyFromScreen(PointToScreen(pictureBox1.Location), new System.Drawing.Point(0, 0), new System.Drawing.Size(pictureBox1.Width, pictureBox1.Height));
            Clipboard.SetImage(img);
            g.Dispose();
        }

        private void MenuItem_GetImageFromClipboard_Click(object sender, EventArgs e)
        {
            /*
                if (Clipboard.ContainsImage())
                {
                    closeVideoCaptureDevice();
                    pictureBox1.Image = Clipboard.GetImage();
                }
            */

            // Transparent image 처리
            //http://csharphelper.com/blog/2014/09/paste-a-png-format-image-with-a-transparent-background-from-the-clipboard-in-c/
            //https://stackoverflow.com/questions/11273669/how-to-paste-a-transparent-image-from-the-clipboard-in-a-c-sharp-winforms-app
            //https://www12.lunapic.com/editor/?action=transparent
            using (var bmp = GetImageFromClipboard())
            {
                if (bmp != null)
                {
                    closeVideoCaptureDevice();
                    pictureBox1.Image = new Bitmap(bmp.Width, bmp.Height);
                    using (Graphics gr = Graphics.FromImage(pictureBox1.Image))
                    {
                        gr.DrawImage(bmp, 0, 0);
                        gr.Dispose();
                    }
                    pictureBox1.Refresh();
                    GC.Collect();

                    //this.Text = "tCamview (clipboard)";
                }
            }
        }

        private void MenuItem_About_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "tCamView 1.3.5\n" +
                "Copyright © 2020-2021, Sung Deuk Kim\n" +
                "All rights reserved.\n" +
                "--------------------------------\n" +
                "https://github.com/augamvio/tCamView\n" +
                "Published under the GNU GPLv3 license.\n" +
                "(For details, see license.txt)\n" +
                "--------------------------------\n" +
                "Credits:\nAForge.NET http://www.aforgenet.com/"
                );
        }

        private void MenuItem_VideoCaptureDevices_Click(object sender, EventArgs e)
        {
            int deviceID = (int)((object[])((MenuItem)sender).Tag)[0];
            //MessageBox.Show("VideoCaptureDevice:" + index);

            openVideoCaptureDevice(deviceID, 0);
            currMenuItem0VideoCaptureDevices = deviceID;
            currMenuItem1VideoCapabilities = 0;
            UpdateMenuItemsChecked();
        }

        private void MenuItem_VideoCapabilties_Click(object sender, EventArgs e)
        {
            int sizeID = (int)((object[])((MenuItem)sender).Tag)[0];

            openVideoCaptureDevice(currCamID, sizeID);
            currMenuItem0VideoCaptureDevices = currCamID;
            currMenuItem1VideoCapabilities = sizeID;
            UpdateMenuItemsChecked();
        }

        // [test] additioal feature: frame skipping to reduce CPU usage
        //int frame_number_mod = -1;

        private void cam_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            firstimage_captured = true;

            /*
            frame_number_mod++;
            frame_number_mod %= 2;  // 15Hz
            //frame_number_mod %= 3;  // 10Hz
            if (frame_number_mod != 0) return;
            */

            //lock (lockObject)
            {

                //https://stackoverflow.com/questions/9484935/how-to-cut-a-part-of-image-in-c-sharp
                //var image = (Bitmap)eventArgs.Frame.Clone();

                // 가로세로 비율을 유지하면서 영상 크기를 줄임.
                cropSize = Math.Max(0,Math.Min(cropSize, Math.Min(eventArgs.Frame.Width / 2, eventArgs.Frame.Height / 2)));
                float ratio = (float) eventArgs.Frame.Height / eventArgs.Frame.Width;
                int vcropSize = (int)(cropSize * ratio);
                var image = (Bitmap)eventArgs.Frame.Clone(new System.Drawing.Rectangle(cropSize, vcropSize, 
                    eventArgs.Frame.Width - 2*cropSize, eventArgs.Frame.Height - 2*vcropSize), eventArgs.Frame.PixelFormat);

                if (pictureBox1.SizeMode == PictureBoxSizeMode.StretchImage && stretchKeepAspectRatio == true)
                {
                    // 가로세로 비율을 유지하면서 Stretch수행  (UniformToFill) 
                    float ratioImage = (float)eventArgs.Frame.Width / eventArgs.Frame.Height;
                    float ratioPictureBox = (float)pictureBox1.ClientSize.Width / pictureBox1.ClientSize.Height;
                    if (ratioImage >= ratioPictureBox)
                    {
                        int newWidth = (int)(image.Height * ratioPictureBox);
                        int wcrop = (int)((image.Width - newWidth) / 2);
                        image = (Bitmap)image.Clone(new System.Drawing.Rectangle(wcrop, 0, image.Width - 2 * wcrop, image.Height), image.PixelFormat);
                    }
                    else
                    {
                        int newHeight = (int)(image.Width / ratioPictureBox);
                        int hcrop = (int)((image.Height - newHeight) / 2);
                        image = (Bitmap)image.Clone(new System.Drawing.Rectangle(0, hcrop, image.Width, image.Height - 2 * hcrop), image.PixelFormat);
                    }
                }
                else if ((pictureBox1.SizeMode == PictureBoxSizeMode.CenterImage) && (cropSize != 0))
                {
                    // PictureBoxSizeMode.CenterImage인 경우 cropping이 발생하면 영상크기가 줄어듬.
                    // 줄어든 영상을 원본영상크기로 재조정해서 보여주면 윈도우의 크기는 변화되지 않고 영상만 확대되는 느낌이 듦.
                    image = new Bitmap(image, new System.Drawing.Size(eventArgs.Frame.Width, eventArgs.Frame.Height));
                }

                if (state_flip_horizontal == true && state_flip_vertical == true)
                {
                    image.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                }
                else if (state_flip_horizontal == true)
                {
                    image.RotateFlip(RotateFlipType.RotateNoneFlipX);
                }
                else if (state_flip_vertical == true)
                {
                    image.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }

                pictureBox1.Image = image;
                GC.Collect();
            }
        }

        private void closeVideoCaptureDevice()
        {
            if (cam != null)
            {
                cam.SignalToStop();

                // wait ~ 3 seconds
                for (int i = 0; i < 30; i++)
                {
                    if (!cam.IsRunning)
                        break;
                    System.Threading.Thread.Sleep(100);
                }

                if (cam.IsRunning)
                {
                    cam.Stop();
                }

                cam = null;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            closeVideoCaptureDevice();
        }

        private void MenuItem_Minimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void MenuItem_Quit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MenuItem_AlwaysOnTop_Click(object sender, EventArgs e)
        {
            this.TopMost = !this.TopMost;
            UpdateMenuItemsChecked();
        }

        private void MenuItem_Opacity_20_Click(object sender, EventArgs e)
        {
            this.Opacity = 0.2;
        }

        private void MenuItem_Opacity_40_Click(object sender, EventArgs e)
        {
            this.Opacity = 0.4;
        }

        private void MenuItem_Opacity_60_Click(object sender, EventArgs e)
        {
            this.Opacity = 0.6;
        }

        private void MenuItem_Opacity_80_Click(object sender, EventArgs e)
        {
            this.Opacity = 0.8;
        }

        private void MenuItem_Opacity_100_Click(object sender, EventArgs e)
        {
            this.Opacity = 1.0;
        }

        private void MenuItem_Opacity_Decrease_Click(object sender, EventArgs e)
        {
            double opacityStep = 0.02;
            if (this.Opacity <= (0.2 + opacityStep))
            {
                this.Opacity = 0.2;
            }
            else
            {
                this.Opacity -= opacityStep;
            }
        }

        private void MenuItem_Opacity_Increase_Click(object sender, EventArgs e)
        {
            double opacityStep = 0.02;
            if (this.Opacity >= (1.0 - opacityStep))
            {
                this.Opacity = 1.0;
            }
            else
            {
                this.Opacity += opacityStep;
            }
        }

        private void MenuItem_Vertical_Flipping_Click(object sender, EventArgs e)
        {
            state_flip_vertical = !state_flip_vertical;
            UpdateMenuItemsChecked();
        }

        private void MenuItem_Horizontal_Flipping_Click(object sender, EventArgs e)
        {
            state_flip_horizontal = !state_flip_horizontal;
            UpdateMenuItemsChecked();
        }

        private void MenuItem_Normal_Border_Click(object sender, EventArgs e)
        {
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            currMenuItem3WindowStyles = 0;
            UpdateMenuItemsChecked();
        }

        private void MenuItem_FullScreen_Click(object sender, EventArgs e)
        {
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;

            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            //Bounds = Screen.PrimaryScreen.Bounds;
            currMenuItem3WindowStyles = 4;
            UpdateMenuItemsChecked();
        }

        private void MenuItem_Borderless_Rectangle_Click(object sender, EventArgs e)
        {
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 0, 0));
            currMenuItem3WindowStyles = 2;
            UpdateMenuItemsChecked();
        }

        private void MenuItem_Borderless_RoundedRectangle_Click(object sender, EventArgs e)
        {
            int m = Math.Min(Width, Height) / 4;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, m, m));
            currMenuItem3WindowStyles = 3;
            UpdateMenuItemsChecked();
        }

        private void MenuItem_Borderless_Ellipse_Click(object sender, EventArgs e)
        {
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, Width, Height));
            currMenuItem3WindowStyles = 1;
            UpdateMenuItemsChecked();
        }

        private void MenuItem_Center_Click(object sender, EventArgs e)
        {
            pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
            this.Text = "tCamView (center)";
            UpdateMenuItemsChecked();
        }

        private void MenuItem_Stretch_Click(object sender, EventArgs e)
        {
            stretchKeepAspectRatio = false;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            this.Text = "tCamView (stretch)";
            UpdateMenuItemsChecked();
        }

        private void MenuItem_AltStretch_Click(object sender, EventArgs e)
        {
            stretchKeepAspectRatio = true;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            this.Text = "tCamView (alt.stretch)";
            UpdateMenuItemsChecked();
        }

        private void MenuItem_Zoom_Click(object sender, EventArgs e)
        {
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            this.Text = "tCamView (zoom)";
            UpdateMenuItemsChecked();
        }

        private void MenuItem_CropImageZoomIn_Click(object sender, EventArgs e)
        {
            cropSize += 5;
        }

        private void MenuItem_CropImageZoomOut_Click(object sender, EventArgs e)
        {
            cropSize -= 5;
        }

        private void MenuItem_ResumeCameraPreviewFromClipboard_Click(object sender, EventArgs e)
        {
            if (cam == null)
            {
                openVideoCaptureDevice(currCamID, currSizeID);
                currMenuItem0VideoCaptureDevices = currCamID;
                currMenuItem1VideoCapabilities = currSizeID;
                UpdateMenuItemsChecked();
            }
        }

        private void MenuItem_IncreaseWindowSize_Click(object sender, EventArgs e)
        {
            if (Width > 4000 || Height > 2000) return;
            float ratio = (float)Height / Width;
            Size = new Size(Width + 5, (int)(Height + 5 * ratio + 0.5));
            if (currMenuItem3WindowStyles == 1) // Borderless_Ellipse
            {
                Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, Width, Height));
            }
            else if (currMenuItem3WindowStyles == 2) // Borderless_Rectangle
            {
                Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 0, 0));
            }
            else if (currMenuItem3WindowStyles == 3) // Borderless_RoundedRectangle
            {
                int m = Math.Min(Width, Height) / 4;
                Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, m, m));
            }
        }

        private void MenuItem_DecreaseWindowSize_Click(object sender, EventArgs e)
        {
            if (Width < 50 || Height < 50) return;
            float ratio = (float)Height / Width;
            Size = new Size(Width - 5, (int)(Height - 5 * ratio + 0.5));
            if (currMenuItem3WindowStyles == 1) // Borderless_Ellipse
            {
                Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, Width, Height));
            }
            else if (currMenuItem3WindowStyles == 2) // Borderless_Rectangle
            {
                Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 0, 0));
            }
            else if (currMenuItem3WindowStyles == 3) // Borderless_RoundedRectangle
            {
                int m = Math.Min(Width, Height) / 4;
                Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, m, m));
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //////////////////////////////////////////////
            /// Opacity
            double opacityStep = 0.02;
            if (keyData == Keys.Up)
            {
                if (this.Opacity >= (1.0 - opacityStep))
                {
                    this.Opacity = 1.0;
                }
                else
                {
                    this.Opacity += opacityStep;
                }
                return true;
            }
            else if (keyData == Keys.Down)
            {
                if (this.Opacity <= (0.2 + opacityStep))
                {
                    this.Opacity = 0.2;
                }
                else
                {
                    this.Opacity -= opacityStep;
                }
                return true;
            }
            else if (keyData == Keys.Right)
            {
                this.Opacity = 1.0;
                return true;
            }
            else if (keyData == Keys.Left)
            {
                this.Opacity = 0.2;
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.C))
            {
                MenuItem_SetImageToClipboard_Click(null, null);
                //e.SuppressKeyPress = true;
                return;
            }
            else if (e.KeyData == (Keys.Control | Keys.V))
            {
                MenuItem_GetImageFromClipboard_Click(null, null);
                //e.SuppressKeyPress = true;
                return;
            }


            switch (e.KeyData)
            {
                //////////////////////////////////////////////
                /// Picture Mode
                case Keys.Z:
                    MenuItem_Zoom_Click(null, null);
                    return;
                case Keys.X:
                    MenuItem_Stretch_Click(null, null);
                    return;
                case Keys.C:
                    MenuItem_Center_Click(null, null);
                    return;
                case Keys.A:
                    MenuItem_AltStretch_Click(null, null);
                    return;

                //////////////////////////////////////////////
                /// Borderless Mode
                case Keys.N: // Normal Border (Resizable)
                case Keys.Escape:
                    MenuItem_Normal_Border_Click(null, null);
                    return;
                case Keys.E: // Borderless Ellipse (FixedSize)
                    MenuItem_Borderless_Ellipse_Click(null, null);
                    return;
                case Keys.R: // Borderless Rectangle (FixedSize)
                    MenuItem_Borderless_Rectangle_Click(null, null);
                    return;
                case Keys.W: // Borderless Rounded Rectangle (FixedSize)
                    MenuItem_Borderless_RoundedRectangle_Click(null, null);
                    return;
                case Keys.F: // Full Screen
                    MenuItem_FullScreen_Click(null, null);
                    return;

                //////////////////////////////////////////////
                /// Image Flipping
                case Keys.H: // Horizontal Flipping (toggle)
                    MenuItem_Horizontal_Flipping_Click(null, null);
                    return;
                case Keys.V: // Vertical Flipping (toggle)
                    MenuItem_Vertical_Flipping_Click(null, null);
                    return;

                //////////////////////////////////////////////
                /// Window Control
                case Keys.T: // Always on Top (toggle)
                    MenuItem_AlwaysOnTop_Click(null, null);
                    return;
                case Keys.Q: // Quit
                    Application.Exit();
                    return;

                case Keys.L: // Minimize
                    this.WindowState = FormWindowState.Minimized;
                    return;
                /*
                    case Keys.U: // Maximize
                        if (WindowState == FormWindowState.Normal)
                        {
                            this.WindowState = FormWindowState.Maximized;
                        }
                        else
                        {
                            this.WindowState = FormWindowState.Normal;
                        }
                        return;
                */

                case Keys.Space: // Resume CameraPreview (escaping from the clipboard view state)
                    MenuItem_ResumeCameraPreviewFromClipboard_Click(null, null);
                    return;

                case Keys.P: // Increase the window size
                    MenuItem_IncreaseWindowSize_Click(null, null);
                    return;

                case Keys.M: // Descrease the window size
                    MenuItem_DecreaseWindowSize_Click(null, null);
                    return;

                //////////////////////////////////////////////
                /// Additional Features
                case Keys.G: // GetImageFromClipboard
                    MenuItem_GetImageFromClipboard_Click(null, null);
                    return;

                case Keys.I: // SetImageToClipboard
                    MenuItem_SetImageToClipboard_Click(null, null);
                    return;

                case Keys.D: // SetImageToClipboardAfter5Sec
                    MenuItem_SetImageToClipboardAfter5Sec_Click(null, null);
                    return;

                case Keys.S: // CopyScreenToClipboard
                    MenuItem_CopyScreenToClipboard_Click(null, null);
                    return;

                //////////////////////////////////////////////
                /// Digital Zoom Effect
                case Keys.PageUp:
                    MenuItem_CropImageZoomIn_Click(null, null);
                    return;

                case Keys.PageDown:
                    MenuItem_CropImageZoomOut_Click(null, null);
                    return;

            }
        }

        // Form 내부 영역을 왼쪽 마우스 클릭해서 옮길 수 있도록 함.
        // https://www.codeproject.com/Articles/11114/Move-window-form-without-Titlebar-in-C
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        //https://stackoverflow.com/questions/11273669/how-to-paste-a-transparent-image-from-the-clipboard-in-a-c-sharp-winforms-app
        // Sample usage:
        // protected override void OnPaint(PaintEventArgs e)
        // {
        //    using (var bmp = GetImageFromClipboard())
        //    {
        //        if (bmp != null) e.Graphics.DrawImage(bmp, 0, 0);
        //    }
        // }
        private Image GetImageFromClipboard()
        {
            if (Clipboard.GetDataObject() == null) return null;
            if (Clipboard.GetDataObject().GetDataPresent(DataFormats.Dib))
            {
                var dib = ((System.IO.MemoryStream)Clipboard.GetData(DataFormats.Dib)).ToArray();
                var width = BitConverter.ToInt32(dib, 4);
                var height = BitConverter.ToInt32(dib, 8);
                var bpp = BitConverter.ToInt16(dib, 14);
                if (bpp == 32)
                {
                    var gch = GCHandle.Alloc(dib, GCHandleType.Pinned);
                    Bitmap bmp = null;
                    try
                    {
                        var ptr = new IntPtr((long)gch.AddrOfPinnedObject() + 52);  // 40 ? 52 ? => 52가 맞았음.
                        bmp = new Bitmap(width, height, width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, ptr);
                        bmp.RotateFlip(RotateFlipType.RotateNoneFlipY); // flipping이 필요했음.
                        return new Bitmap(bmp);
                    }
                    finally
                    {
                        gch.Free();
                        if (bmp != null) bmp.Dispose();
                    }
                }
            }
            return Clipboard.ContainsImage() ? Clipboard.GetImage() : null;
        }

    }
}
