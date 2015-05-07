﻿using System;
using Android.App;using Android.OS;
using Android.Widget;
using Com.Google.Android.Maps;
using Dot42;
using Dot42.Manifest;
using Java.Lang;

[assembly: Application("dot42 Maps Regression Test")]
[assembly: UsesPermission("android.permission.INTERNET")]

namespace MapsTest
{
    [Activity]
    public class MainActivity : MapActivity
    {
        protected override void OnCreate(Bundle savedInstance)
        {
            base.OnCreate(savedInstance);
            SetContentView(R.Layout.MainLayout);

            var mapView = FindViewById<MapView>(R.Id.theMap);
        }

        protected internal override bool IsRouteDisplayed()
        {
            return true;
        }
    }
}
