﻿using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Dot42;
using Dot42.Manifest;

namespace $rootnamespace$
{
    [Activity(Label = "$safeitemrootname$", VisibleInLauncher = false)]
    public class $safeitemrootname$ : Activity
    {
        protected override void OnCreate(Bundle savedInstance) 
        {
            base.OnCreate(savedInstance);
            SetContentView(R.Layout.$safeitemrootname$_Layout);
        }
   }
}
