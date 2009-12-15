﻿/*
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
using System.Threading;
using System.ComponentModel;

namespace TouchFramework.ControlHandlers
{
    /// <summary>
    /// Interaction logic for VideoControl.xaml
    /// </summary>
    public partial class VideoControl : UserControl
    {
        delegate void InvokeDelegate();     

        MediaPlayer player = new MediaPlayer();
        bool playing = false;
        bool inPreview = false;
        bool loaded = false;
        double dispHeight = 0.0;
        double dispWidth = 0.0;

        public VideoControl()
        {
            player.MediaOpened += new EventHandler(player_MediaOpened);
            InitializeComponent();
        }

        public System.Windows.Shapes.Rectangle SetVideo(string path)
        {
            player.Open(new Uri(path, UriKind.RelativeOrAbsolute));
            return rectangle1;
        }

        void CalcDisplay()
        {
            double width = Convert.ToDouble(player.NaturalVideoWidth);
            double height = Convert.ToDouble(player.NaturalVideoHeight);

            double ratio = height / width;

            dispHeight = rectangle1.Width * ratio;
            dispWidth = rectangle1.Width;
        }

        void player_MediaOpened(object sender, EventArgs e)
        {
            CalcDisplay();
            rectangle1.Height = dispHeight;

            StartPlayer();

            BackgroundWorker wk = new BackgroundWorker();
            wk.DoWork += new DoWorkEventHandler(wk_DoWork);
            wk.RunWorkerCompleted += new RunWorkerCompletedEventHandler(wk_RunWorkerCompleted);
            wk.RunWorkerAsync();
        }

        void wk_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            RenderPreview();
            StopPlayer();

            loaded = true;
            inPreview = true;
        }

        void wk_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1000);
        }

        void RenderVideo()
        {
            DrawingVisual rend = new DrawingVisual();
            VideoDrawing aVideoDrawing = new VideoDrawing();
            aVideoDrawing.Rect = new Rect(0, 0, dispWidth, dispHeight);
            aVideoDrawing.Player = player;

            Brush brush = new DrawingBrush(aVideoDrawing);
            rectangle1.Fill = brush;

            RenderOptions.SetCachingHint(brush, CachingHint.Cache);
            RenderOptions.SetCacheInvalidationThresholdMinimum(brush, 0.5);
            RenderOptions.SetCacheInvalidationThresholdMaximum(brush, 2.0);
        }

        void StopPlayer()
        {
            player.Stop();
        }

        void StartPlayer()
        {
            player.Position = TimeSpan.FromSeconds(1);
            player.Play();
        }

        BitmapImage GetPlayImage()
        {
            BitmapImage bi = new BitmapImage(new Uri(@"pack://application:,,,/TouchControls;component/play.png"));
            return bi;
        }

        void RenderPreview()
        {
            DrawingVisual visual = new DrawingVisual();
            DrawingContext context = visual.RenderOpen();
            context.DrawVideo(player, new Rect(0, 0, dispWidth, dispHeight));

            BitmapImage playIcon = GetPlayImage();
            double xpos = (dispWidth - playIcon.Width) / 2;
            double ypos = (dispHeight - playIcon.Height) / 2;

            context.DrawImage(playIcon, new Rect(xpos, ypos, playIcon.Width, playIcon.Height));
            context.Close();

            RenderTargetBitmap target = new RenderTargetBitmap((int)dispWidth, (int)dispHeight, 1 / 100, 1 / 100, PixelFormats.Pbgra32);
            target.Render(visual);
            BitmapFrame frame = BitmapFrame.Create(target).GetAsFrozen() as BitmapFrame;

            Image img = new Image();
            img.Source = frame;
            Brush brush = new VisualBrush(img);
            rectangle1.Fill = brush;
        }

        public void PlayVideo()
        {
            if (!loaded) return;
            if (inPreview) RenderVideo();
            playing = !playing;
            if (playing) player.Play(); else player.Pause();
        }
    }
}
