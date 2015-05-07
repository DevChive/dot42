﻿using System;
using Android.App;
using Android.OS;
using Android.Test;
using Android.Widget;
using Java.Util;
using Dot42;
using Dot42.Manifest;
using Com.Google.Gson;

namespace ImportJar.Sources.Gson
{
    public class Test : ActivityInstrumentationTestCase2<MainActivity>
    {
		public Test() : base(typeof(MainActivity)) 
		{
		}
		
		public void test1() 
		{
			var activity = Activity;
			AssertNotNull(activity);
		}
		
		public void test2() 
		{
			var json = new JsonObject();
			json.Add("name", JsonNull.INSTANCE);
            //AssertEquals(JsonNull.INSTANCE, json.).A
		}
		
		public void test3() 
		{
            // How is this supposed to work with gson, java, and type erasure???
			ArrayList<Fill> pList = GetValue(new ArrayList<Fill>());
            AssertEquals(1, pList.Count);
            //AssertEquals(5, pList[0].Name); // see above.
		}
		
		public T GetValue<T>(T xDefaultValue)
        {
            const string JsonString = "[{\"Name\":\"5\"}]";
            var GSonObj = new Com.Google.Gson.Gson();
            var LastParseResult = GSonObj.FromJson<T>(JsonString, xDefaultValue.GetType());
            if (LastParseResult != null)
                return (T)LastParseResult;
            return xDefaultValue;
        }
		
		public class Fill
		{
			public string Name { get; set; }
		}
	}
}
