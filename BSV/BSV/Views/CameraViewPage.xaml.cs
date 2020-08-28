using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BSV.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CameraViewPage : ContentPage
    {
        public CameraViewPage()
        {
            InitializeComponent();
        }
        private void Start_Clicked(object sender, EventArgs e)
        {
            MessagingCenter.Send<Object>(this, "start_recording");
        }

        private void Stop_Clicked(object sender, EventArgs e)
        {
            MessagingCenter.Send<Object>(this, "stop_recording");
        }
    }
}