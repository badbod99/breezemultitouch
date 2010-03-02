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

using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;
using System.Xml;

using System.ComponentModel;
using System.Windows.Threading;
using System.Threading;

namespace TouchFramework.ControlHandlers
{
    /// <summary>
    /// Interaction logic for RssList.xaml
    /// </summary>
    public partial class RssList : UserControl, IDisposable
    {
        const int DEFAULT_MINS = 2;

        DispatcherTimer dispatcherTimer = null;
        SyndicationFeed feed = null;
        
        string feedUrl = string.Empty;
        
        bool refresh = false;
        bool running = false;

        object syncLock = new object();

        delegate void InvokeDelegate();

        public RssList()
        {
            InitializeComponent();
        }

        public ListBox InternalList
        {
            get
            {
                return listBox1;
            }
        }

        public void Read(string url)
        {
            Read(url, DEFAULT_MINS);
        }

        public void Read(string url, int refreshMins)
        {
            feedUrl = url;

            Thread t = new Thread(new ThreadStart(WaitRefresh));
            t.Start();

            refresh = true;

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, refreshMins, 0);
            dispatcherTimer.Start();
        }

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            refresh = true;
        }

        void updateList()
        {
            lock (syncLock)
            {
                label1.Content = feed.Title.Text;
                listBox1.ItemsSource = feed.Items;
                Console.WriteLine("UPDATED:{0}", DateTime.Now);
            }
        }

        void WaitRefresh()
        {
            running = true;
            while (running)
            {
                if (refresh)
                {
                    // Set so we don't keep running
                    refresh = false;

                    // Change the value of the feed query string to 'atom' to use Atom format.
                    try
                    {
                        XmlReader reader = XmlReader.Create(feedUrl,
                              new XmlReaderSettings()
                              {
                                  //MaxCharactersInDocument can be used to control the maximum amount of data 
                                  //read from the reader and helps prevent OutOfMemoryException
                                  //MaxCharactersInDocument = 1024 * 64
                              });

                        lock (syncLock) feed = SyndicationFeed.Load(reader);
                    }
                    catch
                    {
                        // We don't really want nasty exceptions from dodgy feeds
                    }

                    this.Dispatcher.BeginInvoke((InvokeDelegate)delegate() { this.updateList(); });
                }
                Thread.Sleep(1000);
            }
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
            running = false;
        }

        #endregion

        ~RssList()
        {
            Dispose(false);
        }
    }
}
