/*
TouchFramework connects touch tracking from a tracking engine to WPF controls 
allow scaling, rotation, movement and other multi-touch behaviours.

Copyright 2009 - Mindstorm Limited (reg. 05071596)

Author - Simon Lerpiniere

This file is part of TouchFramework.

TouchFramework is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

TouchFramework is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser Public License for more details.

You should have received a copy of the GNU Lesser Public License
along with TouchFramework.  If not, see <http://www.gnu.org/licenses/>.

If you have any questions regarding this library, or would like to purchase 
a commercial licence, please contact Mindstorm via www.mindstorm.com.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.ComponentModel;

using System.Xml;
using System.Xml.XPath;

using System.Configuration;
using System.IO;

using TouchFramework;
using TouchFramework.Tracking;
using TouchFramework.Events;
using TouchFramework.ControlHandlers;

namespace TouchExample
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        double screen_width = SystemParameters.PrimaryScreenWidth;
        double screen_height = SystemParameters.PrimaryScreenHeight;   
        double window_width = 640;
        double window_height = 480;
        double window_left = 0;
        double window_top = 0;
        int numItems = 0;
        const int MAX_ITEMS = 40;

        /// <summary>
        /// This sets the tracking mode between all available modes from TouchFrameworkTracking.
        /// NOTE: If you use Traal (Mindstorm's tracking system) you need to copy ALL the dependencies
        /// from the Dependencies folder into the Bin\Debug or Bin\Release folder.  These are the DLLs used for the
        /// Lightning tracking system.
        /// </summary>
        TrackingHelper.TrackingType currentTrackingType = TrackingHelper.TrackingType.Mouse;

        bool fullscreen = false;
        static System.Random randomGen = new System.Random();
        Dictionary<int, UIElement> points = new Dictionary<int, UIElement>();
        FrameworkControl framework = null;

        public Window1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Inits everything and starts the tracking engine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            framework = TrackingHelper.GetTracking(this, currentTrackingType);
            framework.OnProcessUpdates += new FrameworkControl.ProcessUpdatesDelegate(this.DisplayPoints);
            framework.Start();

            if (AppConfig.StartFullscreen) toggleFullscreen();

            takeBackground();

            LoadMyPictures();
            LoadMyVideos();

            nonBlockDelay = 4000;
            AnimateStartPos();
        }

        /// <summary>
        /// Displays all points from the collection of points on the screen as elipses.
        /// </summary>
        void DisplayPoints()
        {
            foreach (int i in points.Keys)
            {
                if (!framework.AllTouches.Keys.Contains(i)) canvas1.Children.Remove(points[i]);
            }
            foreach (Touch te in framework.AllTouches.Values)
            {
                DisplayPoint(te.TouchId, te.TouchPoint);
            }
        }
        /// <summary>
        /// Goes through and removes all points from the screen.  I.e. all elipses created to represent touch points.
        /// </summary>
        void RemovePoints()
        {
            foreach (UIElement e in points.Values)
            {
                canvas1.Children.Remove(e);
            }
            points = new Dictionary<int, UIElement>();
        }
        /// <summary>
        /// Displays a point on the screen in the specified location, with the specified colour.
        /// </summary>
        /// <param name="id">Id of the point.</param>
        /// <param name="p">Position of the point in screen coordinates.</param>
        void DisplayPoint(int id, PointF p)
        {
            DisplayPoint(id, p, Colors.White);
        }
        /// <summary>
        /// Displays a point on the screen in the specified location, with the specified colour.
        /// </summary>
        /// <param name="id">Id of the point.</param>
        /// <param name="p">Position of the point in screen coordinates.</param>
        /// <param name="brushColor">The brush to use for the elipse.</param>
        void DisplayPoint(int id, PointF p, System.Windows.Media.Color brushColor)
        {
            Ellipse e = null;
            if (points.ContainsKey(id))
            {
                e = points[id] as Ellipse;
                e.RenderTransform = new TranslateTransform(p.X - 13, p.Y - 13);
            }

            if (e == null)
            {
                e = new Ellipse();

                RadialGradientBrush radialGradient = new RadialGradientBrush();
                radialGradient.GradientOrigin = new System.Windows.Point(0.5, 0.5);
                radialGradient.Center = new System.Windows.Point(0.5, 0.5);
                radialGradient.RadiusX = 0.5;
                radialGradient.RadiusY = 0.5;
                
                System.Windows.Media.Color shadow = Colors.Black;
                shadow.A = 30;
                radialGradient.GradientStops.Add(new GradientStop(shadow, 0.9));
                brushColor.A = 60;
                radialGradient.GradientStops.Add(new GradientStop(brushColor, 0.8));
                brushColor.A = 150;
                radialGradient.GradientStops.Add(new GradientStop(brushColor, 0.1));

                radialGradient.Freeze();

                e.Height = 26.0;
                e.Width = 26.0;
                e.Fill = radialGradient;                

                int eZ = this.framework.MaxZIndex + 100;
                e.IsHitTestVisible = false;
                e.RenderTransform = new TranslateTransform(p.X - 13, p.Y - 13);
                canvas1.Children.Add(e);
                Panel.SetZIndex(e, eZ);
                points.Add(id, e);
            }
        }

        /// <summary>
        /// Loads all images within My Pictures special folder.
        /// </summary>
        void LoadMyPictures()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            LoadAllImages(path);
        }

        /// <summary>
        /// Loads all images within a specified folder.
        /// </summary>
        /// <param name="folderName">Folder to load all images from.</param>
        void LoadAllImages(string folderName)
        {
            string[] fileNames = Directory.GetFiles(folderName);
            
            foreach (string fileName in fileNames)
            {
                if (IsImageExt(System.IO.Path.GetExtension(fileName)))
                {
                    if (numItems > MAX_ITEMS) break;
                    AddPhoto(fileName);
                    numItems++;
                }
            }
        }

        /// <summary>
        /// Unregisters all containers and clears the whole canvas then recreates the touch points. 
        /// </summary>
        void ClearAll()
        {
            this.framework.UnregisterAllElements();
            this.RemovePoints();
            canvas1.Children.Clear();
            DisplayPoints();
            numItems = 0;
        }

        /// <summary>
        /// Loads all videos within My Pictures special folder.
        /// </summary>
        void LoadMyVideos()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            LoadAllVideos(path);
        }

        /// <summary>
        /// Loads all images within a specified folder.
        /// </summary>
        /// <param name="folderName">Folder to load all images from.</param>
        void LoadAllVideos(string folderName)
        {
            string[] fileNames = Directory.GetFiles(folderName);
            foreach (string fileName in fileNames)
            {
                if (IsVideoExt(System.IO.Path.GetExtension(fileName)))
                {
                    if (numItems > MAX_ITEMS) break;
                    AddVideo(fileName);
                    numItems++;
                }
            }
        }

        /// <summary>
        /// Checks if a file extension is a valid image file extension
        /// </summary>
        /// <param name="ext">Extension to check if it's valid</param>
        /// <returns>True if valid false if not.</returns>
        bool IsImageExt(string ext)
        {
            string[] exts = { ".jpg", ".png", ".gif", ".tiff", ".bmp", ".jpeg" };
            return exts.Contains(ext.ToLower());
        }

        /// <summary>
        /// Checks if a file extension is a valid video file extension
        /// </summary>
        /// <param name="ext">Extension to check if it's valid</param>
        /// <returns>True if valid false if not.</returns>
        bool IsVideoExt(string ext)
        {
            string[] exts = { ".wmv", ".mpeg", ".mpg", ".avi" };
            return exts.Contains(ext.ToLower());
        }        

        /// <summary>
        /// Checks if a file has the specified extension
        /// </summary>
        /// <param name="filename">Name or path to the file</param>
        /// <param name="ext">Extension to compare for</param>
        /// <returns>Whether or not the filename has the extension</returns>
        bool CheckExtension(string filename, string ext)
        {
            return (System.IO.Path.GetExtension(filename).ToLower() == ext);
        }

        /// <summary>
        /// Creates a new photo and adds it as a touch managed object to the MTElementDictionary withing the framework.
        /// Randomly positions and rotates the photo within the screen area.
        /// </summary>
        /// <param name="filePath">Full path to the image.</param>
        void AddPhoto(string filePath)
        {
            BitmapImage bi = new BitmapImage(new Uri(filePath));
            Photo p = new Photo();
            System.Windows.Controls.Image i = p.SetPicture(filePath);
            
            RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.HighQuality);

            ElementProperties prop = new ElementProperties();
            prop.ElementSupport.AddSupportForAll();
            // prop.ElementSupport.AddSupport(TouchAction.Move | TouchAction.Rotate | TouchAction.Resize);

            MTContainer cont = new MTSmoothContainer(p, canvas1, prop);
            framework.RegisterElement(cont);

            canvas1.Children.Add(p);

            cont.MaxX = (int)(this.screen_height);
            cont.MaxY = (int)(this.screen_width);
            cont.MinX = (int)(this.screen_height / 10);
            cont.MinY = (int)(this.screen_width / 10);
        }

        int nonBlockDelay = 1000;

        void NonBlockingDelay(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(nonBlockDelay);
        } 

        void AnimateStartPos()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(NonBlockingDelay);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(AnimateStartPos);
            bw.RunWorkerAsync();
        }
        
        /// <summary>
        /// Creates a non-blocking delay using a callback to delay the animation of the items to their intial start position.
        /// </summary>
        void AnimateStartPos(object sender, RunWorkerCompletedEventArgs e)
        {
            // Move then wait a bit and rotate
            RotateAll();
            Move();
            
        }

        /// <summary>
        /// Positions all elements registered with the framework randomly within the window area.
        /// Uses SetTop and SetLeft of the Canvas for positioning.  The timer behaviour in the MTSmoothContainer
        /// means that Move is not suitable to set initial positioning.
        /// </summary>
        void Move()
        {
            foreach (var cont in this.framework.Assigner.Elements.Values)
            {
                // Just incase canvas is too small
                int difX = canvas1.ActualWidth > 200 ? 200 : 0;
                int difY = canvas1.ActualWidth > 200 ? 200 : 0;

                // Get random position and rotation
                int x = randomGen.Next(0, (int)canvas1.ActualWidth - difX);
                int y = randomGen.Next(0, (int)canvas1.ActualHeight - difY);
                Rect curPos = cont.GetElementBounds();
                x -= (int)curPos.Left;
                y -= (int)curPos.Top;

                cont.Move(x, y);
            }
        }

        /// <summary>
        /// Rotates all elements registered in the framework by a random angle between -90 and 90 degrees
        /// </summary>
        void RotateAll()
        {
            foreach (var cont in this.framework.Assigner.Elements.Values)
            {
                int a = randomGen.Next(-90, 90);
                PointF p = cont.GetElementCenter();
                cont.Rotate(a, p);
            }
        }

        /// <summary>
        /// Rotates all elements registered in the framework by the specified angle
        /// </summary>
        void RotateAll(int angle)
        {
            foreach (var cont in this.framework.Assigner.Elements.Values)
            {
                PointF p = cont.GetElementCenter();
                cont.Rotate(angle, p);
            }
        }


        /// <summary>
        /// Creates a new video and adds it as a touch managed object to the MTElementDictionary withing the framework.
        /// Randomly positions and rotates the photo within the screen area.
        /// </summary>
        /// <param name="filePath">Full path to the image.</param>
        void AddVideo(string filePath)
        {
            VideoControl p = new VideoControl();
            System.Windows.Shapes.Rectangle i = p.SetVideo(filePath);

            ElementProperties prop = new ElementProperties();
            prop.ElementSupport.AddSupportForAll();

            MTContainer cont = new MTSmoothContainer(p, canvas1, prop);
            framework.RegisterElement(cont);

            canvas1.Children.Add(p);

            cont.MaxX = (int)(this.screen_height);
            cont.MaxY = (int)(this.screen_width);
            cont.MinX = (int)(this.screen_height / 10);
            cont.MinY = (int)(this.screen_width / 10);
        }


        /// <summary>
        /// Handles key presses to clear the background etc...
        /// B = clear background
        /// Return = Full screen toggle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.B)
            {
                takeBackground();
            }
            else if (e.Key == Key.R)
            {
                ClearAll();
                LoadMyPictures();
                LoadMyVideos();

                nonBlockDelay = 4000;
                AnimateStartPos();
            }
            else if (e.Key == Key.Space)
            {
                RotateAll();
                Move();
            }
            else if (e.Key == Key.S)
            {
                RotateAll(10);
            }
            else if (e.Key == Key.Return)
            {
                toggleFullscreen();
            }
        }

        void takeBackground()
        {
            framework.ForceRefresh();
        }

        void toggleFullscreen()
        {
            if (!fullscreen) switchFullScreen(); else switchWindowed();
        }

        void switchWindowed()
        {
            this.WindowState = WindowState.Normal;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.Left = window_left;
            this.Top = window_top;
            this.Width = window_width;
            this.Height = window_height;

            fullscreen = false;
        }

        void switchFullScreen()
        {
            window_left = this.Left;
            window_top = this.Top;

            window_width = this.Width;
            window_height = this.Height;

            this.Left = 0;
            this.Top = 0;
            this.Width = screen_width;
            this.Height = screen_height;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStyle = WindowStyle.None;
            this.Topmost = true;

            fullscreen = true;
        }

        void Window_Closed(object sender, EventArgs e)
        {
            framework.Stop();
        }
    }
}
