using Plugin.Geolocator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Xaml;

namespace WorkingWithMaps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        CustomMap map;
        Geocoder geoCoder;
        String navAdd;
        String nearestAdd;
        CustomPin pin;
        Position nearestPos;
        Position testPos;

        public MainPage()
        {
            InitializeComponent();

            var maplocator = CrossGeolocator.Current;
            maplocator.DesiredAccuracy = 1;
            geoCoder = new Geocoder();

            map = new CustomMap
            {
                HeightRequest = 100,
                WidthRequest = 960,
                VerticalOptions = LayoutOptions.FillAndExpand,
                IsShowingUser = true
            };

            map.MapType = MapType.Street;
            map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(55.237208, 10.479160), Distance.FromMeters(500)));
            map.IsShowingUser = true;

            var street = new Button { Text = "Street" };
            var hybrid = new Button { Text = "Hybrid" };
            var satellite = new Button { Text = "Satellite" };
            street.Clicked += HandleClickedAsync;
            hybrid.Clicked += HandleClickedAsync;
            satellite.Clicked += NavigateClicked;
            var segments = new StackLayout
            {
                Spacing = 30,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Orientation = StackOrientation.Horizontal,
                Children = { street, hybrid, satellite }
            };

            Content = new StackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                Children = { map, segments }
            };

            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {

                    //var currentpos = await maplocator.GetPositionAsync(1000);
                    //map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(currentpos.Latitude, currentpos.Longitude), Distance.FromMeters(500)));

                    if (!maplocator.IsListening)
                    {
                        await maplocator.StartListeningAsync(10000, 50, true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Fail" + ex);
                }
            });

            testPos = new Position(54.928877, 10.828930);

            pin = new CustomPin
            {
                Id = "Xamarin",
                Pin = new Pin
                {
                    Type = PinType.Place,
                    Position = testPos,
                    Label = string.Format("Nærmeste adresse:")
                },
            };

            map.CustomPins = new List<CustomPin> { pin };
            map.Pins.Add(pin.Pin);

            //Device.BeginInvokeOnMainThread(async () =>
            //{
            //    await RandomMethodAsync(testPos);
            //});



            map.PropertyChanged += (sender, e) =>
            {
                Debug.WriteLine(e.PropertyName + " just changed!");
                if (e.PropertyName == "VisibleRegion" && map.VisibleRegion != null)
                    CalculateBoundingCoordinates(map.VisibleRegion);
            };

            maplocator.PositionChanged += (sender, e) =>
            {
                var position = e.Position;

                map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(position.Latitude, position.Longitude), Distance.FromKilometers(2)));
            };
        }

        //async Task RandomMethodAsync(Position randomPos)
        //{
        //    var possibleAddresses = await geoCoder.GetAddressesForPositionAsync(randomPos);
        //    nearestAdd += possibleAddresses.ElementAt(0) + "\n";          

        //    pin = new CustomPin
        //    {
        //        Id = "Xamarin",
        //        Pin = new Pin
        //        {
        //            Type = PinType.Place,
        //            Position = testPos,
        //            Label = string.Format("Nærmeste adresse: {0}", nearestAdd)
        //        },
        //    };

        //    map.CustomPins = new List<CustomPin> { pin };
        //    map.Pins.Add(pin.Pin);
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void NavigateClicked(object sender, EventArgs e)
        {
            string posLatitude = pin.Pin.Position.Latitude.ToString();
            string posLongitude = pin.Pin.Position.Longitude.ToString();

            navAdd = posLatitude + "," + posLongitude;

            nearestPos = new Position(54.928877, 10.828930);
            var possibleAddresses = await geoCoder.GetAddressesForPositionAsync(nearestPos);
            nearestAdd += possibleAddresses.ElementAt(0) + "\n";

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    Device.OpenUri(new Uri(string.Format("http://maps.apple.com/?q={0}", WebUtility.UrlEncode(navAdd))));
                    break;
                case Device.Android:
                    Device.OpenUri(new Uri(string.Format("geo:0,0?q={0}({1})", WebUtility.UrlEncode(navAdd), pin.Pin.Label)));
                    break;
                case Device.Windows:
                case Device.WinPhone:
                    Device.OpenUri(new Uri(string.Format("bingmaps:?where={0}", Uri.EscapeDataString(navAdd))));
                    break;
            }
        }

        /// <summary>
        /// Håndterer click events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HandleClickedAsync(object sender, EventArgs e)
        {
            var b = sender as Button;
            switch (b.Text)
            {
                case "Street":
                    map.MapType = MapType.Street;
                    break;
                case "Hybrid":
                    map.MapType = MapType.Hybrid;
                    break;
                case "Satellite":
                    map.MapType = MapType.Satellite;
                    break;
            }
        }

        static void CalculateBoundingCoordinates(MapSpan region)
        {
            var center = region.Center;
            var halfheightDegrees = region.LatitudeDegrees / 2;
            var halfwidthDegrees = region.LongitudeDegrees / 2;

            var left = center.Longitude - halfwidthDegrees;
            var right = center.Longitude + halfwidthDegrees;
            var top = center.Latitude + halfheightDegrees;
            var bottom = center.Latitude - halfheightDegrees;

            if (left < -180) left = 180 + (180 + left);
            if (right > 180) right = (right - 180) - 180;

            Debug.WriteLine("Bounding box:");
            Debug.WriteLine("                    " + top);
            Debug.WriteLine("  " + left + "                " + right);
            Debug.WriteLine("                    " + bottom);
        }

    }
}