using System;
using System.Text;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using Android.Widget;
using Android.OS;
using RadiusNetworks.IBeaconAndroid;
using Android.Support.V4.App;
using Android.Bluetooth;

using Color = Android.Graphics.Color;

namespace BeaconScanner.Droid
{
	[Activity(Label = "BeaconScanner", MainLauncher = true, LaunchMode = LaunchMode.SingleTask)]
	public class MainActivity : Activity, IBeaconConsumer
	{
        private const string UUID = "699EBC80-E1F3-11E3-9A0F-0CF3EE3BC012"; // emBeacon COiN UUID
		private const string tokenId = "COiN";

		bool isPaused;
		View view;
		IBeaconManager iBeaconManager;
		MonitorNotifier monitorNotifier;
		RangeNotifier rangeNotifier;
		Region monitoringRegion;
		Region rangingRegion;
		TextView textViewMessage;

		int _previousProximity;

		public MainActivity()
		{
			iBeaconManager = IBeaconManager.GetInstanceForApplication(this);

			monitorNotifier = new MonitorNotifier();
			rangeNotifier = new RangeNotifier();

            //monitoringRegion = new Region(tokenId, UUID, null, null);
            //rangingRegion = new Region(tokenId, UUID, null, null);
            monitoringRegion = new Region(tokenId, null, null, null);
            rangingRegion = new Region(tokenId, null, null, null);
		}

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			SetContentView(Resource.Layout.Main);

			view = FindViewById<RelativeLayout>(Resource.Id.findTheTokenView);
			textViewMessage = FindViewById<TextView>(Resource.Id.tokenStatusLabel);

			iBeaconManager.Bind(this);

			monitorNotifier.EnterRegionComplete += EnteredRegion;
			monitorNotifier.ExitRegionComplete += ExitedRegion;

			rangeNotifier.DidRangeBeaconsInRegionComplete += RangingBeaconsInRegion;

            SwitchOnBT();
		}

		protected override void OnResume()
		{
			base.OnResume();
			isPaused = false;
		}

		protected override void OnPause()
		{
			base.OnPause();
			isPaused = true;
		}

		void EnteredRegion(object sender, MonitorEventArgs e)
		{
			if(isPaused)
			{
				ShowNotification();
			}
		}

		void ExitedRegion(object sender, MonitorEventArgs e)
		{
			RunOnUiThread(() => Toast.MakeText(this, "No tokens visible", ToastLength.Short).Show());
            UpdateDisplay("No tokens found", Color.Black, Color.Gray);
		}

		void RangingBeaconsInRegion(object sender, RangeEventArgs e)
		{
			if (e.Beacons.Count > 0)
			{
				var beacon = e.Beacons.FirstOrDefault();

                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                
                sb2.Append("Accuracy: " + Math.Round(beacon.Accuracy, 2).ToString() + "\n");
                sb2.Append("UUID: " + beacon.ProximityUuid.ToString() + "\n");
                sb2.Append("Major: " + beacon.Major.ToString() + "\n");
                sb2.Append("Minor: " + beacon.Minor.ToString() + "\n");
                sb2.Append("Proximity: " + beacon.Proximity.ToString() + "\n");
                sb2.Append("RSSI: " + beacon.Rssi.ToString() + "\n");                                

				switch((ProximityType)beacon.Proximity)
				{
					case ProximityType.Immediate:
                        sb1.Append("Token found\n").Append(sb2);
						UpdateDisplay(sb1.ToString(), Color.Green, Color.White);
						break;
					case ProximityType.Near:
                        sb1.Append("Token close\n").Append(sb2);
						UpdateDisplay(sb1.ToString(), Color.Yellow, Color.Blue);
						break;
					case ProximityType.Far:
                        sb1.Append("Token far\n").Append(sb2);
						UpdateDisplay(sb1.ToString(), Color.Blue, Color.White);
						break;
					case ProximityType.Unknown:
                        sb1.Append("Token proxiximity unknown\n").Append(sb2);
						UpdateDisplay(sb1.ToString(), Color.Red, Color.Black);
						break;
				}

				_previousProximity = beacon.Proximity;
			}
		}

		#region IBeaconConsumer impl
		public void OnIBeaconServiceConnect()
		{
			iBeaconManager.SetMonitorNotifier(monitorNotifier);
			iBeaconManager.SetRangeNotifier(rangeNotifier);

			iBeaconManager.StartMonitoringBeaconsInRegion(monitoringRegion);
			iBeaconManager.StartRangingBeaconsInRegion(rangingRegion);
		}
		#endregion

        private void UpdateDisplay(string message, Color bgColor, Color textColor)
		{
			RunOnUiThread(() =>
			{
				textViewMessage.Text = message;
                textViewMessage.SetTextColor(textColor);
				view.SetBackgroundColor(bgColor);
			});
		}

		private void ShowNotification()
		{
			var resultIntent = new Intent(this, typeof(MainActivity));
			resultIntent.AddFlags(ActivityFlags.ReorderToFront);
			var pendingIntent = PendingIntent.GetActivity(this, 0, resultIntent, PendingIntentFlags.UpdateCurrent);
			var notificationId = Resource.String.token_notification;

			var builder = new NotificationCompat.Builder(this)
				.SetSmallIcon(Resource.Drawable.Icon)
				.SetContentTitle(this.GetText(Resource.String.app_label))
                .SetContentText(this.GetText(Resource.String.token_notification))
				.SetContentIntent(pendingIntent)
				.SetAutoCancel(true);

			var notification = builder.Build();

			var notificationManager = (NotificationManager)GetSystemService(NotificationService);
			notificationManager.Notify(notificationId, notification);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			monitorNotifier.EnterRegionComplete -= EnteredRegion;
			monitorNotifier.ExitRegionComplete -= ExitedRegion;

			rangeNotifier.DidRangeBeaconsInRegionComplete -= RangingBeaconsInRegion;

			iBeaconManager.StopMonitoringBeaconsInRegion(monitoringRegion);
			iBeaconManager.StopRangingBeaconsInRegion(rangingRegion);
			iBeaconManager.UnBind(this);
		}

        void SwitchOnBT()
        {
            Intent enableBluetoothIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, enableBluetoothIntent, PendingIntentFlags.UpdateCurrent);
        }
	}
}