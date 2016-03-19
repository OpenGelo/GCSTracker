using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;
using GMap.NET.Projections;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace GCSTracker
{
    public partial class Form1 : Form
    {
        double latitude = -6.977865167;
        double longitude = 107.630188833;
        PointLatLng current_pos;
        PointLatLng home_pos;
        public Form1() 
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialize map:
            MainMap.MapProvider = GMap.NET.MapProviders.BingSatelliteMapProvider.Instance;
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            MainMap.Position = new PointLatLng(-6.976865167, 107.630188833);
            
            //MainMap.Position = new PointLatLng(-25.971684, 32.589759);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            serialPort1.PortName = "COM3";
            serialPort1.BaudRate = 57600;
            serialPort1.StopBits = StopBits.One;
            serialPort1.DataBits = 8;
            //serialPort1.Handshake = Handshake.None;
            serialPort1.Parity = Parity.None;
            serialPort1.Open();
            GMapOverlay homeOverlay = new GMapOverlay("markers");
            GMarkerGoogle home = new GMarkerGoogle(new PointLatLng(latitude, longitude), GMarkerGoogleType.blue);
            homeOverlay.Markers.Clear();
            MainMap.Overlays.Add(homeOverlay);
            homeOverlay.Markers.Add(home);
            serialPort1.Write("1");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            serialPort2.PortName = "COM6";
            serialPort2.BaudRate = 57600;
            serialPort2.StopBits = StopBits.One;
            serialPort2.DataBits = 8;
            //serialPort1.Handshake = Handshake.None;
            serialPort2.Parity = Parity.None;
            serialPort2.Open();
        }

        string kata;
        string[] words;
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            kata = serialPort1.ReadLine();
            words = Regex.Split(kata, " ");
            if (words[0] == "005" && words.Length == 12)
            {
                richTextBox1.BeginInvoke(new myDelegate(updatetextbox));
            }
        }

        public delegate void myDelegate();

        double lat,lng;
        void updatetextbox()
        {
            lat = Convert.ToDouble(words[10]);
            lng = Convert.ToDouble(words[11]);
            
            richTextBox1.Text = kata;

            GMapOverlay markersOverlay = new GMapOverlay("markers");
            GMarkerGoogle marker = new GMarkerGoogle(new PointLatLng(lat, lng), GMarkerGoogleType.green);
            markersOverlay.Markers.Clear();
            MainMap.Overlays.Add(markersOverlay);
            markersOverlay.Markers.Add(marker);
            MainMap.Invalidate(false);

            textBox2.Text = words[10];
            textBox3.Text = words[11];
            textBox4.Text = Convert.ToString(BearingTo(lat, lng));
            textBox5.Text = Convert.ToString(RhumbBearingTo(lat,lng));
            
            home_pos.Lat = latitude;
            home_pos.Lng = longitude;
            
            current_pos.Lat = lat;
            current_pos.Lng = lng;
            double bear = MainMap.MapProvider.Projection.GetBearing(home_pos, current_pos);
            textBox1.Text = Convert.ToString(bear);
        }

        public enum DistanceType : int
        {
            Miles = 0,
            Kilometers = 1
        }

        public double DegreeToRadian(double angle) { return Math.PI * angle / 180.0; }

        public const double EarthRadiusInMiles = 3956.0;
        public const double EarthRadiusInKilometers = 6367.0;

        public double RadianToDegree(double angle) { return 180.0 * angle / Math.PI; }

        public double DistanceTo(double lat, double lng, DistanceType dType)
        {
            double R = (dType == DistanceType.Miles) ? EarthRadiusInMiles : EarthRadiusInKilometers;
            double dLat = DegreeToRadian(lat) - DegreeToRadian(this.latitude);
            double dLon = DegreeToRadian(lng) - DegreeToRadian(this.longitude);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(DegreeToRadian(this.latitude)) * Math.Cos(DegreeToRadian(lat)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = c * R;

            return Math.Round(distance, 2);
        } // end DistanceTo

        public double RhumbDistanceTo(double lat, double lng, DistanceType dType)
        {
            double R = (dType == DistanceType.Miles) ? EarthRadiusInMiles : EarthRadiusInKilometers;
            double lat1 = DegreeToRadian(this.latitude);
            double lat2 = DegreeToRadian(lat);
            double dLat = DegreeToRadian(lat - this.latitude);
            double dLon = DegreeToRadian(Math.Abs(lng - this.longitude));

            double dPhi = Math.Log(Math.Tan(lat2 / 2 + Math.PI / 4) / Math.Tan(lat1 / 2 + Math.PI / 4));
            double q = Math.Cos(lat1);
            if (dPhi != 0) q = dLat / dPhi;  // E-W line gives dPhi=0
            // if dLon over 180° take shorter rhumb across 180° meridian:
            if (dLon > Math.PI) dLon = 2 * Math.PI - dLon;
            double dist = Math.Sqrt(dLat * dLat + q * q * dLon * dLon) * R;

            return dist;
        } // end RhumbDistanceTo

        public double RhumbBearingTo(double lat, double lng)
        {
            double lat1 = DegreeToRadian(this.latitude);
            double lat2 = DegreeToRadian(lat);
            double dLon = DegreeToRadian(lng - this.longitude);

            double dPhi = Math.Log(Math.Tan(lat2 / 2 + Math.PI / 4) / Math.Tan(lat1 / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI) dLon = (dLon > 0) ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
            double brng = Math.Atan2(dLon, dPhi);

            return (RadianToDegree(brng) + 360) % 360;
        } // end RhumbBearingTo

        public double BearingTo(double lat, double lng)
        {
            double lat1 = DegreeToRadian(this.latitude);
            double lat2 = DegreeToRadian(lat);
            double dLon = DegreeToRadian(lng) - DegreeToRadian(this.longitude);

            double y = Math.Sin(dLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
            double brng = Math.Atan2(y, x);

            return (RadianToDegree(brng) + 360) % 360;
        } // end BearingTo
    }
}
