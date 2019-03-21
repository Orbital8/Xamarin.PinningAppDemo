using System;
using Android.App;
using Android.Runtime;

namespace Xamarin.PinningAppDemo.Droid
{
#if DEBUG
    [Application(Label = "Pinning Demo", Debuggable = true)]
#else
    [Application(Label = "Pinning Demo", Debuggable = false)]
#endif
    public class AndroidApp : Application
    {
        public AndroidApp()
        {
        }

        public AndroidApp(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }
    }
}