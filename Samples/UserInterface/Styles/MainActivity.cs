﻿using Android.App;using Android.OS;
using Dot42;
using Dot42.Manifest;

[assembly: Application("dot42 Simple Style Sample")]

namespace SimpleStyle
{
    [Activity(Icon = "Icon", Label = "dot42 Simple Style!")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstance)
        {
            base.OnCreate(savedInstance);
            SetContentView(R.Layout.MainLayout);
        }
    }
}
