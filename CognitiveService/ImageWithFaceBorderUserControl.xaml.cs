// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: http://www.microsoft.com/cognitive
// 
// Microsoft Cognitive Services Github:
// https://github.com/Microsoft/Cognitive
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using System.IO;
using ServiceHelpers;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using Windows.Devices.Gpio;
using System.Diagnostics;
using Windows.UI.Core;
// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CognitiveService
{
    

    public class EmotionFeedback
    {
       
        public string Text { get; set; }
        public SolidColorBrush AccentColor { get; set; }
        public string ImageFileName { get; set; }
    }

    public interface IEmotionFeedbackDataProvider
    {
        EmotionFeedback GetEmotionFeedback(Emotion emotion);
    }

    public sealed partial class ImageWithFaceBorderUserControl : UserControl
    {
        //bae
        public List<int> ages = new List<int>();
        public List<string> genders = new List<string>();
        private static string blobStorageConnectionString =
            "======intsert your blob storage connection string======";
        private Uri urlToBePlayed_female = null;
        private Uri urlToBePlayed_male = null;
        private Uri urlToBePlayed_kid = null;
        private GpioHelper gpioHelper;
        private bool gpioAvailable;
        
        public List<int> AgeList()
        {
            return ages;
        }
        public List<string> GenderList()
        {
            return genders;
        }
        public ImageWithFaceBorderUserControl()
        {
            this.InitializeComponent();
            if (gpioAvailable == false)
            {
                // If GPIO is not available, attempt to initialize it
                InitializeGpio();
            }
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
            if (args.Edge == GpioPinEdge.FallingEdge)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    MediaPlayer.Stop();
                    MediaPlayer.Visibility = Visibility.Collapsed;
                });

            }
            
        }

        public static readonly DependencyProperty DetectFacesOnLoadProperty =
            DependencyProperty.Register(
            "DetectFacesOnLoad",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty ShowMultipleFacesProperty =
            DependencyProperty.Register(
            "ShowMultipleFaces",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty PerformRecognitionProperty =
            DependencyProperty.Register(
            "PerformRecognition",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty DetectFaceAttributesProperty =
            DependencyProperty.Register(
            "DetectFaceAttributes",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty ShowRecognitionResultsProperty =
            DependencyProperty.Register(
            "ShowRecognitionResults",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty ShowDialogOnApiErrorsProperty =
            DependencyProperty.Register(
            "ShowDialogOnApiErrors",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty ShowEmotionRecognitionProperty =
            DependencyProperty.Register(
            "ShowEmotionRecognition",
            typeof(bool),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(false)
            );

        public static readonly DependencyProperty BalloonBackgroundProperty =
            DependencyProperty.Register(
            "BalloonBackground",
            typeof(SolidColorBrush),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty BalloonForegroundProperty =
            DependencyProperty.Register(
            "BalloonForeground",
            typeof(SolidColorBrush),
            typeof(ImageWithFaceBorderUserControl),
            new PropertyMetadata(null)
            );

        public SolidColorBrush BalloonBackground
        {
            get { return (SolidColorBrush)GetValue(BalloonBackgroundProperty); }
            set { SetValue(BalloonBackgroundProperty, (SolidColorBrush)value); }
        }

        public SolidColorBrush BalloonForeground
        {
            get { return (SolidColorBrush)GetValue(BalloonForegroundProperty); }
            set { SetValue(BalloonForegroundProperty, (SolidColorBrush)value); }
        }

        public bool ShowEmotionRecognition
        {
            get { return (bool)GetValue(ShowEmotionRecognitionProperty); }
            set { SetValue(ShowEmotionRecognitionProperty, (bool)value); }
        }

        public bool ShowMultipleFaces
        {
            get { return (bool)GetValue(ShowMultipleFacesProperty); }
            set { SetValue(ShowMultipleFacesProperty, (bool)value); }
        }

        public bool DetectFacesOnLoad
        {
            get { return (bool)GetValue(DetectFacesOnLoadProperty); }
            set { SetValue(DetectFacesOnLoadProperty, (bool)value); }
        }

        public bool DetectFaceAttributes
        {
            get { return (bool)GetValue(DetectFaceAttributesProperty); }
            set { SetValue(DetectFaceAttributesProperty, (bool)value); }
        }

        public bool PerformRecognition
        {
            get { return (bool)GetValue(PerformRecognitionProperty); }
            set { SetValue(PerformRecognitionProperty, (bool)value); }
        }

        public bool ShowRecognitionResults
        {
            get { return (bool)GetValue(ShowRecognitionResultsProperty); }
            set { SetValue(ShowRecognitionResultsProperty, (bool)value);
            }

        }

        public bool ShowDialogOnApiErrors
        {
            get { return (bool)GetValue(ShowDialogOnApiErrorsProperty); }
            set { SetValue(ShowDialogOnApiErrorsProperty, (bool)value); }
        }

        private async void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ImageAnalyzer dataContext = this.DataContext as ImageAnalyzer;

            foreach (var child in this.hostGrid.Children.Where(c => !(c is Image)).ToArray())
            {
                this.hostGrid.Children.Remove(child);
            }

            // remove the current source
            this.bitmapImage.UriSource = null;

            if (dataContext != null)
            {
                try
                {
                    if (dataContext.ImageUrl != null)
                    {
                        this.bitmapImage.UriSource = new Uri(dataContext.ImageUrl);
                    }
                    else if (dataContext.GetImageStreamCallback != null)
                    {
                        await this.bitmapImage.SetSourceAsync((await dataContext.GetImageStreamCallback()).AsRandomAccessStream());
                    }
                }
                catch (Exception ex)
                {
                    // If we fail to load the image we will just not display it
                    this.bitmapImage.UriSource = null;
                    if (this.ShowDialogOnApiErrors)
                    {
                        await Util.GenericApiCallExceptionHandler(ex, "Error loading captured image.");
                    }
                }
            }
        }

        private async Task DetectAndShowFaceBorders()
        {           
            this.progressIndicator.IsActive = true;

            foreach (var child in this.hostGrid.Children.Where(c => !(c is Image)).ToArray())
            {
                this.hostGrid.Children.Remove(child);
            }

            ImageAnalyzer imageWithFace = this.DataContext as ImageAnalyzer;
            if (imageWithFace != null)
            {
                if (imageWithFace.DetectedFaces == null)
                {
                    await imageWithFace.DetectFacesAsync(detectFaceAttributes: this.DetectFaceAttributes);
                }

                double renderedImageXTransform = this.imageControl.RenderSize.Width / this.bitmapImage.PixelWidth;
                double renderedImageYTransform = this.imageControl.RenderSize.Height / this.bitmapImage.PixelHeight;

                foreach (Face face in imageWithFace.DetectedFaces)
                {
                    FaceIdentificationBorder faceUI = new FaceIdentificationBorder()
                    {
                        Tag = face.FaceId,
                    };

                    faceUI.Margin = new Thickness((face.FaceRectangle.Left * renderedImageXTransform) + ((this.ActualWidth - this.imageControl.RenderSize.Width) / 2),
                                                  (face.FaceRectangle.Top * renderedImageYTransform) + ((this.ActualHeight - this.imageControl.RenderSize.Height) / 2), 0, 0);

                    faceUI.BalloonBackground = this.BalloonBackground;
                    faceUI.BalloonForeground = this.BalloonForeground;
                    faceUI.ShowFaceRectangle(face.FaceRectangle.Width * renderedImageXTransform, face.FaceRectangle.Height * renderedImageYTransform);

                    this.hostGrid.Children.Add(faceUI);

                    if (!this.ShowMultipleFaces)
                    {
                        break;
                    }
                }

                if (this.PerformRecognition)
                {
                    if (imageWithFace.IdentifiedPersons == null)
                    {
                        await imageWithFace.IdentifyFacesAsync();
                    }

                    if (this.ShowRecognitionResults)
                    {
                        foreach (Face face in imageWithFace.DetectedFaces)
                        {
                            // Get the border for the associated face id
                            FaceIdentificationBorder faceUI = (FaceIdentificationBorder)this.hostGrid.Children.FirstOrDefault(e => e is FaceIdentificationBorder && (Guid)(e as FaceIdentificationBorder).Tag == face.FaceId);

                            if (faceUI != null)
                            {
                                IdentifiedPerson faceIdIdentification = imageWithFace.IdentifiedPersons.FirstOrDefault(p => p.FaceId == face.FaceId);

                                string name = this.DetectFaceAttributes && faceIdIdentification != null ? faceIdIdentification.Person.Name : null;
                                string gender = this.DetectFaceAttributes ? face.FaceAttributes.Gender : null;
                                double age = this.DetectFaceAttributes ? face.FaceAttributes.Age : 0;
                                double confidence = this.DetectFaceAttributes && faceIdIdentification != null ? faceIdIdentification.Confidence : 0;
                                //bae
                                int roundedAge = (int)Math.Round(age);
                                ages.Add(roundedAge);
                                genders.Add(gender);

                                faceUI.ShowIdentificationData(age, gender, (uint)Math.Round(confidence * 100), name);                                
                            }
                        }                        
                    }
                }
            }

            this.progressIndicator.IsActive = false;
        }
        private async Task DetectAndShowEmotion()
        {
            this.progressIndicator.IsActive = true;

            foreach (var child in this.hostGrid.Children.Where(c => !(c is Image)).ToArray())
            {
                this.hostGrid.Children.Remove(child);
            }

            ImageAnalyzer imageWithFace = this.DataContext as ImageAnalyzer;
            if (imageWithFace != null)
            {
                if (imageWithFace.DetectedEmotion == null)
                {
                    await imageWithFace.DetectEmotionAsync();
                }

                double renderedImageXTransform = this.imageControl.RenderSize.Width / this.bitmapImage.PixelWidth;
                double renderedImageYTransform = this.imageControl.RenderSize.Height / this.bitmapImage.PixelHeight;

                foreach (Emotion emotion in imageWithFace.DetectedEmotion)
                {
                    FaceIdentificationBorder faceUI = new FaceIdentificationBorder();

                    faceUI.Margin = new Thickness((emotion.FaceRectangle.Left * renderedImageXTransform) + ((this.ActualWidth - this.imageControl.RenderSize.Width) / 2),
                                                    (emotion.FaceRectangle.Top * renderedImageYTransform) + ((this.ActualHeight - this.imageControl.RenderSize.Height) / 2), 0, 0);

                    faceUI.BalloonBackground = this.BalloonBackground;
                    faceUI.BalloonForeground = this.BalloonForeground;

                    faceUI.ShowFaceRectangle(emotion.FaceRectangle.Width * renderedImageXTransform, emotion.FaceRectangle.Height * renderedImageYTransform);

                    //faceUI.ShowEmotionData(emotion);

                    this.hostGrid.Children.Add(faceUI);

                    if (!this.ShowMultipleFaces)
                    {
                        break;
                    }
                }
            }

            this.progressIndicator.IsActive = false;
        }

        private async Task PreviewImageFaces()
        {
            if (!this.DetectFacesOnLoad || this.progressIndicator.IsActive)
            {
                return;
            }

            ImageAnalyzer img = this.DataContext as ImageAnalyzer;
            if (img != null)
            {
                img.UpdateDecodedImageSize(this.bitmapImage.PixelHeight, this.bitmapImage.PixelWidth);
            }

            if (this.ShowEmotionRecognition)
            {
                await this.DetectAndShowEmotion();
            }
            else if (this.DetectFaceAttributes)
            {
                await this.DetectAndShowFaceBorders();
                await Task.Delay(2000);
                //bae
                if (ages.Count != 0 && genders.Count != 0)
                {
                          
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(blobStorageConnectionString);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = blobClient.GetContainerReference("video");
                    await container.SetPermissionsAsync(new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    });                    
                    bool istherekid = ages.Exists(i => i <= 10);
                    bool istherewomen = genders.Contains("female");

                    if (istherekid == true)
                    {
                        ages.Clear();
                        genders.Clear();
                        CloudBlockBlob blockBlob = container.GetBlockBlobReference("kid.mp4");
                        urlToBePlayed_kid = blockBlob.Uri;
                        MediaPlayer.Source = urlToBePlayed_kid;
                        MediaPlayer.Visibility = Visibility.Visible;
                        MediaPlayer.Play();                        
                    }
                    else if (istherewomen == true)
                    {
                        //bae
                        ages.Clear();
                        genders.Clear();
                        CloudBlockBlob blockBlob = container.GetBlockBlobReference("female.mp4");
                        urlToBePlayed_female = blockBlob.Uri;
                        MediaPlayer.Source = urlToBePlayed_female;
                        MediaPlayer.Visibility = Visibility.Visible;
                        MediaPlayer.Play();
                    }
                    else
                    {
                        //bae
                        ages.Clear();
                        genders.Clear();
                        CloudBlockBlob blockBlob = container.GetBlockBlobReference("male.mp4");
                        urlToBePlayed_male = blockBlob.Uri;
                        MediaPlayer.Source = urlToBePlayed_male;
                        MediaPlayer.Visibility = Visibility.Visible;
                        MediaPlayer.Play();
                    }
                }
            }
        }
        
        public void StopFunction()
        {
            MediaPlayer.Stop();
            MediaPlayer.Visibility = Visibility.Collapsed;
            
            //var cameracont = new CameraControl();
            //cameracont.CameraControlButton_image(null,null);
        }

        private async void OnBitmapImageOpened(object sender, RoutedEventArgs e)
        {
            await PreviewImageFaces();            
        }

        private async void OnImageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            await this.PreviewImageFaces();
        }

        private void Media_Ended(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Position = TimeSpan.FromSeconds(0);
            MediaPlayer.Play();
        }
    }
}
