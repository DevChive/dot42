﻿using System;
using System.Runtime.InteropServices;
using Android.App;using Android.OS;
using Android.Widget;
using Dot42;
using Dot42.Manifest;

[assembly: Application("DllImportTest")]

namespace DllImportTest
{
    [Activity(Label ="DllImport MainActivity")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstance)
        {
            base.OnCreate(savedInstance);
            SetContentView(R.Layout.MainActivityLayout);
			//Java.Lang.System.LoadLibrary("dllImportTest");
            var textView = FindViewById<TextView>(R.Id.label);
            textView.Text = Foo();
        }
        
        [DllImport("dllImportTest")]
        public static extern string Foo();
    }
}
