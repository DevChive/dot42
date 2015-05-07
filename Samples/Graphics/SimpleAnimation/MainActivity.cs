﻿using Android.App;using Android.OS;
using Dot42;
using Dot42.Manifest;

[assembly: Application("dot42 Simple Animation")]

namespace SimpleAnimation
{
    [Activity(Icon = "Icon", Label = "dot42 Simple Animation Sample")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstance)
        {
            base.OnCreate(savedInstance);
            RequestWindowFeature(Android.Views.Window.FEATURE_NO_TITLE);
            SetContentView(new Panel(this));
        }
    }
}
