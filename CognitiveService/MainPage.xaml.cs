using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using Windows.UI.Core;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CognitiveService
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private GpioHelper gpioHelper;

        private bool gpioAvailable;
        private bool doorbellJustPressed = false;
        public MainPage()
        {
            this.InitializeComponent();

            if (gpioAvailable == false)
            {
                // If GPIO is not available, attempt to initialize it
                InitializeGpio();
            }
            this.cameraControl.ImageCaptured += CameraControl_ImageCaptured;
            this.cameraControl.CameraRestarted += CameraControl_CameraRestarted;

            StartWebCameraAsync();
        }
        private async Task DoorbellPressed()
        {
            if (imageFromCameraWithFaces.MediaPlayer.Source != null)
            {
                imageFromCameraWithFaces.StopFunction();
                imageFromCameraWithFaces.MediaPlayer.Source = null;
                cameraControl.CameraControlButton_image();

            }
            else
            {
                cameraControl.CameraControlButton_image2();
            }
            doorbellJustPressed = false;
            //FaceQuery(file);
        }
        public void InitializeGpio()
        {
            try
            {
                // Attempts to initialize application GPIO. 
                gpioHelper = new GpioHelper();
                gpioAvailable = gpioHelper.Initialize();
            }
            catch
            {
                // This can fail if application is run on a device, such as a laptop, that does not have a GPIO controller
                gpioAvailable = false;
                Debug.WriteLine("GPIO controller not available.");
            }

            // If initialization was successfull, attach doorbell pressed event handler
            if (gpioAvailable)
            {
                gpioHelper.GetDoorBellPin().ValueChanged += DoorBellPressed;
            }
        }
        private async void DoorBellPressed(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (!doorbellJustPressed)
            {
                // Checks to see if even was triggered from a press or release of button
                if (args.Edge == GpioPinEdge.FallingEdge)
                {
                    //Doorbell was just pressed
                    doorbellJustPressed = true;

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await DoorbellPressed();
                    });

                }
            }
        }
        private async void CameraControl_CameraRestarted(object sender, EventArgs e)
        {
            await Task.Delay(500);
            this.imageFromCameraWithFaces.Visibility = Visibility.Collapsed;
        }

        private async void CameraControl_ImageCaptured(object sender, ServiceHelpers.ImageAnalyzer e)
        {
            this.imageFromCameraWithFaces.DataContext = e;
            this.imageFromCameraWithFaces.Visibility = Visibility.Visible;

            await this.cameraControl.StopStreamAsync();

            
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private async Task StartWebCameraAsync()
        {
            webCamHostGrid.Visibility = Visibility.Visible;
            imageWithFacesControl.Visibility = Visibility.Collapsed;

            await this.cameraControl.StartStreamAsync();
            await Task.Delay(250);
            this.imageFromCameraWithFaces.Visibility = Visibility.Collapsed;
            
            UpdateWebCamHostGridSize();
        }

        private void UpdateWebCamHostGridSize()
        {
            this.webCamHostGrid.Width = this.webCamHostGrid.ActualHeight * (this.cameraControl.CameraAspectRatio != 0 ? this.cameraControl.CameraAspectRatio : 1.777777777777);
        }

        private void mainbuttonclick(object sender, RoutedEventArgs e)
        {
            if(imageFromCameraWithFaces.MediaPlayer.Source != null)
            {
                imageFromCameraWithFaces.StopFunction();
                imageFromCameraWithFaces.MediaPlayer.Source = null;
                cameraControl.CameraControlButton_image();
                
            }
            else
            {
                cameraControl.CameraControlButton_image2();
            }

        }
        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
    }
}
