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
using System.Threading;
using System.ComponentModel;

namespace TouchFramework.ControlHandlers
{
    /// <summary>
    /// Interaction logic for VideoControl.xaml
    /// </summary>
    public partial class VideoControl : UserControl, IDisposable
    {
        delegate void InvokeDelegate();     

        MediaPlayer player = new MediaPlayer();
        DrawingVisual videoVisual = new DrawingVisual();
        bool playing = false;
        bool inPreview = false;
        bool loaded = false;
        double dispHeight = 0.0;
        double dispWidth = 0.0;

        public VideoControl()
        {
            player.MediaOpened += new EventHandler(player_MediaOpened);
            player.MediaEnded += new EventHandler(player_MediaEnded);
            InitializeComponent();
        }
        
        public System.Windows.Shapes.Rectangle SetVideo(string path)
        {
            player.Open(new Uri(path, UriKind.RelativeOrAbsolute));
            
            return rectangle1;
        }

        public void PlayVideo()
        {
            if (!loaded) return;
            if (inPreview) { renderVideo(); inPreview = false; }
            playing = !playing;
            if (playing) player.Play(); else player.Pause();
        }

        void player_MediaOpened(object sender, EventArgs e)
        {
            initVideo();
        }

        void player_MediaEnded(object sender, EventArgs e)
        {
            playing = false;
            initVideo();
        }

        void initVideo()
        {
            calcDisplay();
            rectangle1.Height = dispHeight;

            startPreviewPlayer();

            BackgroundWorker wk = new BackgroundWorker();
            wk.DoWork += new DoWorkEventHandler(wk_DoWork);
            wk.RunWorkerCompleted += new RunWorkerCompletedEventHandler(wk_RunWorkerCompleted);
            wk.RunWorkerAsync();
        }

        void wk_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            renderPreview();
            stopPlayer();

            loaded = true;
            inPreview = true;
        }

        void wk_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1000);
        }

        void renderVideo()
        {
            DrawingContext context = videoVisual.RenderOpen();
            context.DrawVideo(player, new Rect(0, 0, dispWidth, dispHeight));
            context.Close();

            Brush brush = new VisualBrush(videoVisual);
            rectangle1.Fill = brush;

            RenderOptions.SetCachingHint(brush, CachingHint.Cache);
            RenderOptions.SetCacheInvalidationThresholdMinimum(brush, 0.5);
            RenderOptions.SetCacheInvalidationThresholdMaximum(brush, 2.0);
        }

        void renderPreview()
        {
            BitmapImage playIcon = getPlayIcon();
            Rect iconPos = calcIconCenterPosition(playIcon);

            DrawingContext context = videoVisual.RenderOpen();
            context.DrawVideo(player, new Rect(0, 0, dispWidth, dispHeight));
            context.DrawImage(playIcon, iconPos);
            context.Close();

            RenderTargetBitmap target = new RenderTargetBitmap((int)dispWidth, (int)dispHeight, 1 / 100, 1 / 100, PixelFormats.Pbgra32);
            target.Render(videoVisual);
            BitmapFrame frame = BitmapFrame.Create(target).GetAsFrozen() as BitmapFrame;

            Image img = new Image();
            img.Source = frame;
            Brush brush = new VisualBrush(img);
            rectangle1.Fill = brush;
        }

        void stopPlayer()
        {
            player.Stop();
        }

        void startPreviewPlayer()
        {
            player.Position = TimeSpan.FromSeconds(1);
            player.Play();
        }

        BitmapImage getPlayIcon()
        {
            BitmapImage bi = new BitmapImage(new Uri(@"pack://application:,,,/TouchControls;component/play.png"));
            return bi;
        }

        Rect calcIconCenterPosition(BitmapImage icon)
        {
            double xpos = (dispWidth - icon.Width) / 2;
            double ypos = (dispHeight - icon.Height) / 2;
            return new Rect(xpos, ypos, icon.Width, icon.Height);
        }
        
        void calcDisplay()
        {
            double width = Convert.ToDouble(player.NaturalVideoWidth);
            double height = Convert.ToDouble(player.NaturalVideoHeight);

            double ratio = height / width;

            dispHeight = rectangle1.Width * ratio;
            dispWidth = rectangle1.Width;
        }

        #region IDisposable Members

        protected bool disposed = false;

        public void Dispose()
        {
            Dispose(true);            
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    Cleanup();
                }
                disposed = true;
            }
        }

        private void Cleanup()
        {
            player.Close();
        }

        #endregion

        ~VideoControl()
        {
            Dispose(false);
        }
        
    }
}
