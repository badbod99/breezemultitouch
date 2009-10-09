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

using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

using TouchFramework.Helpers;
using TouchFramework.Events;

namespace TouchFramework.ControlHandlers
{
    public class ElementHandler
    {
        public static ElementHandler GetHandler(FrameworkElement source, Panel cont)
        {
            ElementHandler handler = null;
            if (source is TextBox)
            {
                handler = new TextBoxHandler() as ElementHandler;
            }
            else if (source is Button)
            {
                handler = new ButtonHandler() as ElementHandler;
            }
            else if (source is CheckBox)
            {
                handler = new CheckBoxHandler() as ElementHandler;
            }
            else if (source is Slider)
            {
                handler = new SliderHandler() as ElementHandler;
            }
            else if (source is ListBox)
            {
                handler = new ListBoxHandler() as ElementHandler;
            }                
            else
            {
                handler = new ButtonHandler() as ElementHandler;
            }
            handler.Source = source;
            handler.Container = cont;
            return handler;
        }

        public FrameworkElement Source = null;
        public Panel Container = null;

        public virtual void Tap(PointF p)
        {
            this.Source.RaiseEvent(new RoutedEventArgs(MTEvents.TapEvent, Source));
        }

        public virtual void Scroll(float x, float y)
        {
            // This sets it so that we scroll by pixels rather than by lines
            if (ScrollViewer.GetCanContentScroll(Source)) ScrollViewer.SetCanContentScroll(Source, false);
            
            // Find the scrollviewer and scroll it if there is one
            ScrollViewer scroll = Source.FindChild<ScrollViewer>() as ScrollViewer;
            if (scroll != null)
            {
                scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset + (x * -1));
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset + (y * -1));
            }

            Source.RaiseEvent(new RoutedEventArgs(MTEvents.ScrollEvent, Source));
        }

        public virtual void Drag(float x, float y)
        {
            Source.RaiseEvent(new RoutedEventArgs(MTEvents.DragEvent, Source));
        }

        public virtual void Slide(float x, float y)
        {
            Source.RaiseEvent(new RoutedEventArgs(MTEvents.SlideEvent, Source));
        }

        public virtual void TouchDown(PointF p)
        {
            Source.RaiseEvent(new RoutedEventArgs(MTEvents.TouchDownEvent, Source));
        }

        public virtual void TouchUp(PointF p)
        {
            Source.RaiseEvent(new RoutedEventArgs(MTEvents.TouchUpEvent, Source));
        }
    }
}
