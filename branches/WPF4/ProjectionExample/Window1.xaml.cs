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

using System.Configuration;

namespace ProjectionExample
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Rect SurfaceArea = Rect.Empty;
        
        public Window1()
        {
            InitializeComponent();

            double resolutionX = SystemParameters.PrimaryScreenWidth;
            double resolutionY = SystemParameters.PrimaryScreenHeight;

            // Put your usersettings.xml path in here
            string path = @"Config\UserSettings.xml";
            ProjectionConfig config = ProjectionConfig.Load(path);
            switchFullScreen(config, resolutionX, resolutionY);
        }

        void switchFullScreen(ProjectionConfig proj, double resolutionX, double resolutionY)
        {
            SurfaceArea = GetScreenDimensions(proj.OffsetX,
                proj.OffsetY,
                proj.ScaleX,
                proj.ScaleY,
                resolutionX,
                resolutionY);

            this.Width = SurfaceArea.Width;
            this.Height = SurfaceArea.Height;
            this.Top = SurfaceArea.Top;
            this.Left = SurfaceArea.Left;

            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStyle = WindowStyle.None;
            this.Topmost = true;
        }

        public Rect GetScreenDimensions(float offsetX, float offsetY, float scaleX, float scaleY, double fullscreenWidth, double fullscreenHeight)
        {
            // Screen is scaled by an inverse value, so calculate the screen pixel dimensions
            int sW = (int)(fullscreenWidth / scaleX);
            int sH = (int)(fullscreenHeight / scaleY);

            // Screen offset is a value from -1 to +1 so convert to 0 to 1 and base it on the center
            float offsetX2d = 0.5f - (offsetX / 2f + 0.5f);
            float offsetY2d = 0.5f - (offsetY / 2f + 0.5f);

            // Calculate the pixel offset by the full screen resolution
            int offsetPixX = (int)(offsetX2d * fullscreenWidth);
            int offsetPixY = (int)(offsetY2d * fullscreenHeight);

            // Offset the windows based on the center and set the window dimensions
            double left = (fullscreenWidth / 2) - (sW / 2) - offsetPixX;
            double top = (fullscreenHeight / 2) - (sH / 2) - offsetPixY;

            // Return the dimensions
            Rect r = new Rect(left, top, sW, sH);
            return r;
        }

    }
}
