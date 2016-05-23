using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SuperPing
{
    class ObservablePingInfo
    {
        public string ip { get; set; }
        public string dns { get; set; }
        public string ping { get; set; }
        public string packetinfo { get; set; }
        public string status { get; set; }
    }

    class DrawPoint
    {
        public int intresting;
        public long value;
        public DrawPoint(long val, int type)
        {
            intresting = type;
            value = val;
        }
    }
    class PingGraphInfo
    {
        static Ping p;
        public long highest;
        public long lowest;
        public long avg;
        public long packages;
        public long lost;
        public long ttl;
        public long scope;
        public long maxpoints;
        public IPStatus error;
        public List<DrawPoint> points;
        public string address;
        public string dns;
        public List<PingGraphInfo> route;
        public bool dontbother;
        public Window owner;
        private int last;

        public void ReTracert()
        {
            if (p == null)
                p = new Ping();

            if (this.error != IPStatus.Success)
                return;

            byte[] test = Encoding.Default.GetBytes("tracert " + DateTime.UtcNow.Ticks.ToString());

            List<PingGraphInfo> temp = new List<PingGraphInfo>(); ;
            PingOptions po = new PingOptions();
            po.Ttl = 1;
            PingReply pr = p.Send(address, 1000, test, po);
            while (pr.Status != IPStatus.Success)
            {

                PingGraphInfo subpgi = new PingGraphInfo();
                subpgi.address = pr.Address == null ? null : pr.Address.ToString();
                subpgi.maxpoints = this.maxpoints;
                temp.Add(subpgi);

                if (po.Ttl++ >= 255)
                    break;
                pr = p.Send(address, 1000, test, po);
            }  

            if(route==null || route.Count<=0)
            {
                route = temp;
                this.points.Add(new DrawPoint(this.points[this.points.Count - 1].value, 3));
                return;
            }
            else if (temp.Count == route.Count)
            {
                for (int n = 0; n < temp.Count; n++)
                {
                    if (temp[n].address != route[n].address)
                    {
                        route = temp;
                        this.points.Add(new DrawPoint(this.points[this.points.Count - 1].value, 3));
                        return;
                    }
                }
            }
            else
            {               
                route = temp;
                this.points.Add(new DrawPoint(this.points[this.points.Count - 1].value, 3));
                return;              
            }

        }

        public void SingleRoutePing()
        {
            if (route == null || route.Count <= 0)
            {
                return;
            }

            if (last >= route.Count)
            {
                if (owner != null)
                    owner.Title = address + " tracerouting";        
                ReTracert();
                last = 0;
                if (owner != null)
                    owner.Title = address + " pinging";
                SingleRoutePing();
                return;
            }

            route[last++].DoPing();
        }
        public string ReverseDNS()
        {
            if (!string.IsNullOrEmpty(dns))
                return dns;

            try
            {
                dns = Dns.GetHostEntry(address).HostName;
            }
            catch{
                dns = address;
            }
            return dns;
        }

        public List<ObservablePingInfo> GetRoute()
        {
            if (route == null || route.Count <= 0)
                return null;

            List<ObservablePingInfo> opi = new List<ObservablePingInfo>();
            long pingg;
            foreach(PingGraphInfo pgi in route)
            {
                pingg = pgi.GetLastPing();
                ObservablePingInfo pi = new ObservablePingInfo();
                pi.ip = string.IsNullOrEmpty(pgi.address) ? "*" : pgi.address;
                pi.dns = pi.ip == "*" ? "*" : pgi.ReverseDNS();
                pi.ping = pingg == 0 && pgi.error!=IPStatus.Success ? "*" : pingg.ToString();
                pi.packetinfo = pgi.packages+"/"+pgi.lost+" ("+pgi.ttl+")";
                pi.status = pgi.error.ToString();
                opi.Add(pi);
            }
            return opi;
        }

        public long GetLastPing()
        {
            if (points == null || points.Count <= 0)
            {
                return 0;
            }
            return points[points.Count - 1].value;
        }

        private PingGraphInfo()
        {
            scope = 0;
            if (p == null)
                p = new Ping();
            points = new List<DrawPoint>();
            route = null;          
        }
        public PingGraphInfo(string addr, long max)
        {
                            
            if (p == null)
                p = new Ping();
            scope = 0;
            points = new List<DrawPoint>();
            route = new List<PingGraphInfo>();
            address = addr;
            maxpoints = max;

            this.DoPing();

            if (this.error != IPStatus.Success)
                return;

            byte[] test = Encoding.Default.GetBytes("tracert "+DateTime.UtcNow.Ticks.ToString());
            
            PingOptions po = new PingOptions();
            po.Ttl = 1;
            PingReply pr = p.Send(addr, 1000, test, po);
            while (pr.Status != IPStatus.Success)
            {

                PingGraphInfo subpgi = new PingGraphInfo();
                subpgi.address = pr.Address == null ? null : pr.Address.ToString();
                subpgi.maxpoints = this.maxpoints;
                subpgi.DoPing();
                route.Add(subpgi);

                if (po.Ttl++ >= 255)
                    break;
                pr = p.Send(addr, 1000, test, po);
            }        
        }
        public IPStatus DoPing()
        {
            if (string.IsNullOrEmpty(address))
            {
                this.error = IPStatus.Unknown;
                dontbother = true;
                return this.error;
            }
            else if (dontbother)
                return this.error;
            
            if (p == null)
                p = new Ping();

            PingReply pr = null;
            try
            {
                pr = p.Send(this.address, 1000);
            }
            catch
            {
                pr = null;
            }

            if (pr != null && pr.Status == IPStatus.Success)
            {
                if (this.points.Count >= this.maxpoints)
                {
                    this.points.RemoveAt(0);
                }

                long tm = pr.RoundtripTime;

                this.points.Add(new DrawPoint(tm,(tm>this.highest&&this.highest>0) ? 1 : 0));
                this.packages++;
                this.ttl = pr.Options.Ttl;
            }
            else
            {
                if (this.points.Count > 0)
                {
                    this.points.Add(new DrawPoint(this.points[this.points.Count - 1].value,2));
                }
                this.lost++;
            }

            this.error = pr==null?IPStatus.Unknown: pr.Status;

            if (this.error != IPStatus.Success && this.packages == 0 && this.lost >= 5)
                dontbother = true;

            this.highest = 0;
            this.lowest = this.points.Count == 0 ? 0 : this.points[0].value;

            long total = 0;

            foreach (DrawPoint l in this.points)
            {
                if (l.value > 0)
                {
                    if (l.value > this.highest)
                    {
                        this.highest = l.value;
                    }
                    if (l.value < this.lowest)
                    {
                        this.lowest = l.value;
                    }
                }
                total += l.value;
            }

            this.avg = this.points.Count==0? 0: total / this.points.Count;
            if(this.packages!=0&&this.packages%2==0)
                SingleRoutePing();
            return this.error;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string addr;
        public MainWindow()
        {               
            Start start = new Start();
            start.Tag = this;
            start.ShowDialog();
            this.Loaded += WindowLoaded;
            InitializeComponent();
        }

        private PointCollection Circle(double x, double y, double radius, long resolution,PointCollection existing=null)
        {
            PointCollection points = null;

            if (existing == null)
                points = new PointCollection();
            else
                points = existing;

            for (int n = 0; n < resolution; n++)
            {
                points.Add(new Point((radius * Math.Cos(n)+x), (radius * Math.Sin(n))+y));
            }

            return points;
        }

        private Polyline Dot(double x, double y, double radius, long resolution, Brush color)
        {
            Polyline polyline = new Polyline();
            polyline.StrokeThickness = 1;
            polyline.Stroke = color;
            polyline.Points = Circle(x, y, radius, resolution);

            polyline.Fill = polyline.Stroke;
            polyline.FillRule = FillRule.Nonzero;
            return polyline;
        }

        private Point GetPoint(long val, long nth, long resolution, PingGraphInfo pgi)
        {
            double w = NetGraph.ActualWidth;
            double h = NetGraph.ActualHeight;

            double x = (w / (resolution - 1)) * nth;
            double y = 0;

            long lowest = pgi.lowest;
            long highest = pgi.highest;

            if (pgi.scope > 0)
            {
                highest = pgi.scope;
                lowest = 0;
            }

            if (val < lowest)
                val = lowest;
            else if (val >= highest)
                val = highest;

            val = val - lowest;
            highest = highest - lowest;

            double mod = (double)val / (double)highest;

            y = h * mod;

            return new Point(x, h-y);
        }

        private void ReDrawGraph(PingGraphInfo pgi)
        {
            NetGraph.Children.RemoveRange(0, NetGraph.Children.Count);

            PointCollection pcmid = new PointCollection();

            if (pgi.scope <= 0)
            {
                pcmid.Add(new Point(0, NetGraph.ActualHeight / 2));
                pcmid.Add(new Point(NetGraph.ActualWidth, NetGraph.ActualHeight / 2));
            }
            else
            {
                double mod = (double)pgi.avg / (double)pgi.scope;

                pcmid.Add(new Point(0, NetGraph.ActualHeight - (NetGraph.ActualHeight * mod)));
                pcmid.Add(new Point(NetGraph.ActualWidth, NetGraph.ActualHeight-(NetGraph.ActualHeight * mod)));
            }

            Polyline midline = new Polyline();
            midline.StrokeThickness = 1;
            midline.Stroke = Brushes.Blue;
            midline.Points = pcmid;

            NetGraph.Children.Add(midline);

            PointCollection pc = new PointCollection();
            Point pt;

            for (int n = 0; n < pgi.points.Count; n++)
            {
                pt = GetPoint(pgi.points[n].value, n, pgi.maxpoints, pgi);
                pc.Add(pt);
                if (pgi.points[n].intresting==1)
                {
                    PointCollection circle = Circle(pt.X, pt.Y, 5, 50);
                    Polyline plcircle = new Polyline();
                    plcircle.StrokeThickness = 2;
                    plcircle.Stroke = Brushes.Red;
                    plcircle.Points = circle;

                    NetGraph.Children.Add(plcircle);
                }
                else if (pgi.points[n].intresting == 2)
                {
                    NetGraph.Children.Add(Dot(pt.X, pt.Y, 5, 50, Brushes.HotPink));
                }
                else if (pgi.points[n].intresting == 3)
                {
                    PointCollection cross = new PointCollection();

                    cross.Add(new Point(pt.X, NetGraph.ActualHeight));
                    cross.Add(new Point(pt.X, 0));

                    Polyline crossline = new Polyline();
                    crossline.StrokeThickness = 3;
                    crossline.Stroke = Brushes.LightBlue;
                    crossline.Points = cross;

                    NetGraph.Children.Add(crossline);
                }
            }

            Polyline polyline = new Polyline();
            polyline.StrokeThickness = 2;
            polyline.Stroke = Brushes.Red;
            polyline.Points = pc;

            NetGraph.Children.Add(polyline);
        }

        private Vector GetCenter(Canvas cv)
        {
            return new Vector(cv.ActualWidth / 2, cv.ActualHeight / 2);
        }

        private void UpdateStatusbar(PingGraphInfo pgi)
        {
            double percent = 0;
            if(pgi.lost>0)
            {
                percent = (double)pgi.lost/(double)pgi.packages;
                percent *= 100;
            }

            long last = pgi.points[pgi.points.Count - 1].value;
            CurrentPing.Content = "Ping: " + last.ToString();
            AvgPing.Content = "Average Ping (ttl): " + pgi.avg+" ("+pgi.ttl+")";
            HiLoPing.Content = "Highest/Lowest ping: " + pgi.highest + "/" + pgi.lowest;
            Packets.Content = "Packets Sent/Lost (%): " + pgi.packages + "/" + pgi.lost + " (" + percent + "%)";
        }

        private void UpdateRoute(PingGraphInfo pgi)
        {
            if (tracert.HasItems)
            {
                tracert.Items.Clear();
            }

            List<ObservablePingInfo> opi = pgi.GetRoute();
            if (opi != null)
            {
                foreach (ObservablePingInfo pi in opi)
                {
                    tracert.Items.Add(pi);
                }
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(addr))
                {
                    MessageBox.Show("You gotta enter an address");
                    this.Close();
                    return;
                }

                this.Title = this.addr + " loading";
                PingGraphInfo pgi = new PingGraphInfo(this.addr, 100);

                if (pgi.error != IPStatus.Success)
                {
                    MessageBox.Show("Couldnt find: "+this.addr);
                    this.Close();
                    return;
                }

                this.Tag = pgi;
                pgi.owner = this;
                UpdateRoute(pgi);

                this.SizeChanged += WindowSizeChanged;

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(500);
                timer.Tag = this;
                timer.Tick += WindowTick;
                timer.Start();
                this.Title = this.addr + " pinging";
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: "+ex.Message+"\n"+ex.StackTrace);
            }
        }

        private void WindowTick(object sender, EventArgs e)
        {
            try
            {
                Task.Factory.StartNew(() =>
                {
                    PingGraphInfo pgi = ((Window)((DispatcherTimer)sender).Tag).Tag as PingGraphInfo;

                    pgi.DoPing();
                    ReDrawGraph(pgi);
                    UpdateStatusbar(pgi);
                    UpdateRoute(pgi);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                PingGraphInfo pgi = ((Window)sender).Tag as PingGraphInfo;
                ReDrawGraph(pgi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
