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

using System.Xml;
using System.Xml.XPath;

using System.Configuration;
using System.IO;

using TouchFramework;
using TouchFramework.Tracking;
using TouchFramework.Events;

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

        /// <summary>
        /// This sets the tracking mode between all available modes from TouchFrameworkTracking.
        /// NOTE: If you use Traal (Mindstorm's tracking system) you need to copy ALL the dependencies
        /// from the Dependencies folder into the Bin\Debug or Bin\Release folder.  These are the DLLs used for the
        /// Traal tracking system.
        /// </summary>
        TrackingHelper.TrackingType currentTrackingType = TrackingHelper.TrackingType.TUIO;

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

            // Containers are used to provide multi-touch functionality to any existing WPF control
            MTContainer cont;
            
            // Element properties defines what features a multi-touch container supports
            ElementProperties prop = new ElementProperties();
            prop.ElementSupport.AddSupport(TouchAction.Tap);

            // We use direct containers to help performance on object that don't move
            cont = new MTDirectContainer(button1, canvas1, prop);
            button1.AddHandler(MTEvents.TapEvent, new RoutedEventHandler(contbut_Tap));
            framework.RegisterElement(cont);
            cont.StartX = (int)Canvas.GetLeft(button1);
            cont.StartY = (int)Canvas.GetTop(button1);

            cont = new MTDirectContainer(checkBox1, canvas1, prop);
            checkBox1.AddHandler(MTEvents.TapEvent, new RoutedEventHandler(check_Tap));
            framework.RegisterElement(cont);
            cont.StartX = (int)Canvas.GetLeft(checkBox1);
            cont.StartY = (int)Canvas.GetTop(checkBox1);

            prop = new ElementProperties();
            prop.ElementSupport.AddSupport(TouchAction.Tap | TouchAction.ScrollY);

            cont = new MTDirectContainer(textBox1, canvas1, prop);
            textBox1.AddHandler(MTEvents.TapEvent, new RoutedEventHandler(textbox_Tap));
            framework.RegisterElement(cont);
            cont.StartX = (int)Canvas.GetLeft(textBox1);
            cont.StartY = (int)Canvas.GetTop(textBox1);

            prop = new ElementProperties();
            // The add support function allows for bitwise enum passing to provide support for multiple options easily
            prop.ElementSupport.AddSupport(TouchAction.Tap | TouchAction.Slide | TouchAction.Resize);

            cont = new MTDirectContainer(slider1, canvas1, prop);
            slider1.AddHandler(MTEvents.TapEvent, new RoutedEventHandler(slider_Tap));
            slider1.AddHandler(MTEvents.SlideEvent, new RoutedEventHandler(slider_Slide));
            framework.RegisterElement(cont);
            cont.StartX = (int)Canvas.GetLeft(slider1);
            cont.StartY = (int)Canvas.GetTop(slider1);

            prop = new ElementProperties();
            prop.ElementSupport.AddSupport(TouchAction.Tap | TouchAction.ScrollY | TouchAction.Resize | TouchAction.Drag);

            // We use smooth containers on object which move about, this filters their movement using a linear filter
            cont = new MTSmoothContainer(listBox1, canvas1, prop);
            listBox1.AddHandler(MTEvents.TapEvent, new RoutedEventHandler(listbox_Tap));
            listBox1.AddHandler(MTEvents.ScrollEvent, new RoutedEventHandler(listbox_Scroll));
            framework.RegisterElement(cont);
            cont.StartX = (int)Canvas.GetLeft(listBox1);
            cont.StartY = (int)Canvas.GetTop(listBox1);

            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            LoadAllImages(path);
        }

        void listbox_Tap(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("ListboxTapped");
        }

        void listbox_Scroll(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("ListboxScroll");
        }

        void slider_Tap(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("SliderTapped");
        }

        void slider_Slide(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("SliderSlide");
        }

        void check_Tap(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("CheckTapped");
        }

        void contbut_Tap(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("TappedButton");
        }

        void textbox_Tap(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("TappedTextbox");
        }

        void contimg_Tap(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine("TappedImage");
        }

        /// <summary>
        /// Displays all points from the collection of points on the screen as elipses.
        /// </summary>
        void DisplayPoints()
        {
            RemovePoints();
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
            }

            if (e == null)
            {
                e = new Ellipse();

                e.Stroke = new SolidColorBrush(brushColor);
                e.Height = 15.0;
                e.Width = 15.0;
                e.StrokeThickness = 2.0;

                canvas1.Children.Add(e);
                int eZ = this.framework.MaxZIndex + 100;
                Panel.SetZIndex(e, eZ);
                points.Add(id, e);
            }

            Canvas.SetLeft(e, p.X);
            Canvas.SetTop(e, p.Y);
        }
        
        /// <summary>
        /// Loads all images within a specified folder.
        /// </summary>
        /// <param name="folderName">Folder to load all images from.</param>
        void LoadAllImages(string folderName)
        {
            string[] fileNames = Directory.GetFiles(folderName);
            DirectoryInfo newDir = Directory.CreateDirectory(System.IO.Path.Combine(folderName, "small"));
            foreach (string fileName in fileNames)
            {
                if (System.IO.Path.GetExtension(fileName).ToLower() == ".jpg")
                {                   
                    AddPhoto(fileName);
                }
            }
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

            MTContainer cont = new MTSmoothContainer(p, canvas1, prop);
            framework.RegisterElement(cont);

            canvas1.Children.Add(p);

            int x = randomGen.Next(0, (int)screen_width - (int)p.ActualWidth);
            int y = randomGen.Next(0, (int)screen_height - (int)p.ActualHeight);
            int a = randomGen.Next(0, 360);

            cont.StartX = x;
            cont.StartY = y;
            cont.Rotate(a, new PointF(x, y));
            cont.ApplyTransforms();
            cont.MaxX = (int)(this.screen_height);
            cont.MaxY = (int)(this.screen_width);
            cont.MinX = (int)(this.screen_height / 10);
            cont.MinY = (int)(this.screen_width / 10);

            //cont.Tap += new RoutedEventHandler(cont_Tap);
            p.AddHandler(MTEvents.TapEvent, new RoutedEventHandler(contimg_Tap));

            Canvas.SetLeft(p, x);
            Canvas.SetTop(p, y);
        }

        /// <summary>
        /// Handles key presses to clear the background etc...
        /// B = clear background
        /// E = Stop the tracking engine
        /// Return = Full screen toggle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.B)
            {
                framework.ForceRefresh();
            }
            else if (e.Key == Key.E)
            {
                framework.Stop();
            }
            else if (e.Key == Key.Return)
            {

                fullscreen = !fullscreen;
                if (fullscreen)
                {
                    window_left = this.Left;
                    window_top = this.Top;

                    window_width = this.Width;
                    window_height = this.Height;

                    this.ResizeMode = ResizeMode.NoResize;
                    this.WindowStyle = WindowStyle.None;
                    this.Left = 0;
                    this.Top = 0;
                    this.Width = screen_width;
                    this.Height = screen_height;
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.Left = window_left;
                    this.Top = window_top;
                    this.Width = window_width;
                    this.Height = window_height;
                }
            }
        }

        void Window_Closed(object sender, EventArgs e)
        {
            framework.Stop();
        }
    }
}
