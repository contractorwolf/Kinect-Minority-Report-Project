/////////////////////////////////////////////////////////////////////////
//
// This module contains code to do Kinect NUI initialization and
// processing and also to display NUI streams on screen.
//
// Copyright © Microsoft Corporation.  All rights reserved.  
// This code is licensed under the terms of the 
// Microsoft Kinect for Windows SDK (Beta) from Microsoft Research 
// License Agreement: http://research.microsoft.com/KinectSDK-ToU
//
/////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;
using System.Windows.Controls.Primitives;

namespace SkeletalViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Runtime nui;
        DateTime lastTime = DateTime.MaxValue;
        Popup imageMain = new Popup();
        Image myImage = new Image();
        private bool isVideo = false;

 
        double lastRightXPosition = -1;
        double lastLeftXPosition = -1;

        double lastRightYPosition = -1;

        double lastHandDiff = -1;

        bool leftOn = false;
        bool rightOn = false;

        double diff;


        double changeX = 0;
        double changeY = 0;

        private KinectListener kl;
        private string ListenMode;
        private bool testMode = false;

        
        Dictionary<JointID,Brush> jointColors = new Dictionary<JointID,Brush>() { 
            {JointID.HipCenter, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointID.Spine, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointID.ShoulderCenter, new SolidColorBrush(Color.FromRgb(168, 230, 29))},
            {JointID.Head, new SolidColorBrush(Color.FromRgb(200, 0,   0))},
            {JointID.ShoulderLeft, new SolidColorBrush(Color.FromRgb(79,  84,  33))},
            {JointID.ElbowLeft, new SolidColorBrush(Color.FromRgb(84,  33,  42))},
            {JointID.WristLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointID.HandLeft, new SolidColorBrush(Color.FromRgb(215,  86, 0))},
            {JointID.ShoulderRight, new SolidColorBrush(Color.FromRgb(33,  79,  84))},
            {JointID.ElbowRight, new SolidColorBrush(Color.FromRgb(33,  33,  84))},
            {JointID.WristRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointID.HandRight, new SolidColorBrush(Color.FromRgb(37,   69, 243))},
            {JointID.HipLeft, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointID.KneeLeft, new SolidColorBrush(Color.FromRgb(69,  33,  84))},
            {JointID.AnkleLeft, new SolidColorBrush(Color.FromRgb(229, 170, 122))},
            {JointID.FootLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointID.HipRight, new SolidColorBrush(Color.FromRgb(181, 165, 213))},
            {JointID.KneeRight, new SolidColorBrush(Color.FromRgb(71, 222,  76))},
            {JointID.AnkleRight, new SolidColorBrush(Color.FromRgb(245, 228, 156))},
            {JointID.FootRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))}
        };

        private void Window_Loaded(object sender, EventArgs e)
        {
            nui = new Runtime();
            kl = new KinectListener();

            kl.PassMessage += new EventHandler(MessageReceived);


            List<string> wordList = new List<string>();
            wordList.Add("move");
            wordList.Add("scale");
            wordList.Add("rotate");
            wordList.Add("stop");
            wordList.Add("picture");
            wordList.Add("video");
            kl.StartListening(wordList);





            try
            {
                nui.Initialize(RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);
                //Must set to true and set after call to Initialize     
                nui.SkeletonEngine.TransformSmooth = true;       
                //Use to transform and reduce jitter     
                var parameters = new TransformSmoothParameters{         
                    Smoothing = 0.75f,         
                    Correction = 0.0f,         
                    Prediction = 0.0f,         
                    JitterRadius = 0.05f,         
                    MaxDeviationRadius = 0.04f     
                };       
                
                nui.SkeletonEngine.SmoothParameters = parameters; 

            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Runtime initialization failed. Please make sure Kinect device is plugged in.");
                return;
            }





            try
            {
                nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution1280x1024, ImageType.Color);

            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Failed to open stream. Please make sure to specify a supported image type and resolution.");
                return;
            }

            lastTime = DateTime.Now;


            nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
            nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_ColorFrameReady);








            image.RenderTransform = GetImageTransformGroup();




        }

        public void MessageReceived(object sender, EventArgs e)
        {

            ProgessResponse response = kl.Message;




            if (response.Message == "move")
            {
                if (rightOn || testMode)
                {
                    txtMode.Background = Brushes.Green;
                    modeIndicator.Background = Brushes.Green;
                    ListenMode = response.Message;
                    txtMode.Text = ListenMode;
                }
                else
                {                    
                    txtMode.Background = Brushes.Red;
                    modeIndicator.Background = Brushes.Red;
                    txtMode.Text = "must have right hands up to move image";
                }
            }
            else if (response.Message == "scale")
            {
                if ((rightOn && leftOn) || testMode)
                {
                    txtMode.Background = Brushes.Green;
                    ListenMode = response.Message;
                    txtMode.Text = ListenMode;
                    modeIndicator.Background = Brushes.Green;
                }
                else
                {
                    txtMode.Text = "must have both hands up to scale";
                    txtMode.Background = Brushes.Red;
                    modeIndicator.Background = Brushes.Red;
                }
            }
            else if (response.Message == "rotate")
            {
                if ((rightOn && leftOn) || testMode)
                {
                    txtMode.Background = Brushes.Green;
                    modeIndicator.Background = Brushes.Green;
                    ListenMode = response.Message;                    
                    txtMode.Text = ListenMode;
                }
                else
                {
                    txtMode.Text = "must have both hands up to rotate";
                    txtMode.Background = Brushes.Red;
                    modeIndicator.Background = Brushes.Red;
                }
            }

            else if (response.Message == "picture")
            {
                isVideo = false;
                    image.Source = video.Source;
                    txtMode.Text = "waiting on instructions: (" + response.Confidence + ")";
                    ListenMode = "stop";
                    txtMode.Background = Brushes.Blue;
modeIndicator.Background = Brushes.Blue;
            }


            else if (response.Message == "video")
            {
                    //image.Source = video.Source;
                    txtMode.Text = "waiting on instructions: (" + response.Confidence + ")";
                    ListenMode = "stop";
                    txtMode.Background = Brushes.Blue;
                isVideo = true;
                modeIndicator.Background = Brushes.Blue;
            }



            else
            {
                    txtMode.Text = "waiting on instructions: (" + response.Confidence + ")";
                    ListenMode = "stop";
                    txtMode.Background = Brushes.Blue;
                modeIndicator.Background = Brushes.Blue;
            }
            
        }


        private Point getDisplayPosition(Joint joint)
        {
            float depthX, depthY;
            nui.SkeletonEngine.SkeletonToDepthImage(joint.Position, out depthX, out depthY);
            depthX = Math.Max(0, Math.Min(depthX * 320, 320));  //convert to 320, 240 space
            depthY = Math.Max(0, Math.Min(depthY * 240, 240));  //convert to 320, 240 space
            int colorX, colorY;
            ImageViewArea iv = new ImageViewArea();
            // only ImageResolution.Resolution640x480 is supported at this point
            nui.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(ImageResolution.Resolution640x480, iv, (int)depthX, (int)depthY, (short)0, out colorX, out colorY);

            // map back to skeleton.Width & skeleton.Height
            return new Point((int)(skeleton.Width * colorX / 640.0), (int)(skeleton.Height * colorY / 480));
        }

        Polyline getBodySegment(JointsCollection joints, Brush brush, params JointID[] ids)
        {
            PointCollection points = new PointCollection(ids.Length);
            for (int i = 0; i < ids.Length; ++i )
            {
                points.Add(getDisplayPosition(joints[ids[i]]));
            }

            Polyline polyline = new Polyline();
            polyline.Points = points;
            polyline.Stroke = brush;
            polyline.StrokeThickness = 5;
            return polyline;
        }

        void nui_ColorFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            // 32-bit per pixel, RGBA image
            PlanarImage Image = e.ImageFrame.Image;
            video.Source = BitmapSource.Create(Image.Width, Image.Height, 96, 96, PixelFormats.Bgr32, null, Image.Bits, Image.Width * Image.BytesPerPixel);

            if (isVideo)
            {
                image.Source = video.Source;
            }
        }



        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            SkeletonFrame skeletonFrame = e.SkeletonFrame;
            int iSkeleton = 0;
            Brush[] brushes = new Brush[6];
            brushes[0] = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            brushes[1] = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            brushes[2] = new SolidColorBrush(Color.FromRgb(64, 255, 255));
            brushes[3] = new SolidColorBrush(Color.FromRgb(255, 255, 64));
            brushes[4] = new SolidColorBrush(Color.FromRgb(255, 64, 255));
            brushes[5] = new SolidColorBrush(Color.FromRgb(128, 128, 255));

            skeleton.Children.Clear();
            foreach (SkeletonData data in skeletonFrame.Skeletons)
            {
                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    // Draw bones
                    Brush brush = brushes[iSkeleton % brushes.Length];
                    //skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.Spine, JointID.ShoulderCenter, JointID.Head));
                    //skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.ShoulderCenter, JointID.ShoulderLeft, JointID.ElbowLeft, JointID.WristLeft, JointID.HandLeft));
                    //skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.ShoulderCenter, JointID.ShoulderRight, JointID.ElbowRight, JointID.WristRight, JointID.HandRight));
                    //skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.HipLeft, JointID.KneeLeft, JointID.AnkleLeft, JointID.FootLeft));
                    //skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.HipRight, JointID.KneeRight, JointID.AnkleRight, JointID.FootRight));

                    // Draw joints
                    foreach (Joint joint in data.Joints)
                    {
                        Point jointPos = getDisplayPosition(joint);
                        Line jointLine = new Line();
                        jointLine.X1 = jointPos.X - 3;
                        jointLine.X2 = jointLine.X1 + 6;
                        jointLine.Y1 = jointLine.Y2 = jointPos.Y;
                        jointLine.Stroke = jointColors[joint.ID];
                        jointLine.StrokeThickness = 6;
                        skeleton.Children.Add(jointLine);
                    }


                    Joint rightWrist = data.Joints[JointID.WristRight];
                    Joint rightHand = data.Joints[JointID.HandRight];
                    Point rightWristPos = getDisplayPosition(rightWrist);
                    Point rightHandPos = getDisplayPosition(rightHand);

                    Joint leftWrist = data.Joints[JointID.WristLeft];
                    Joint leftHand = data.Joints[JointID.HandLeft];
                    Point leftWristPos = getDisplayPosition(leftWrist);
                    Point leftHandPos = getDisplayPosition(leftHand);

                    double xDiff = rightHandPos.X - leftHandPos.X;
                    double yDiff = rightHandPos.Y - leftHandPos.Y;



                    if (ListenMode == "scale")
                    {
                        if (leftOn && rightOn)
                        {
                            if (lastHandDiff != -1)
                            {
                                diff = xDiff - lastHandDiff;
                                ScaleImage(image, diff);
                            }
                        }
                    }else if(ListenMode == "move"){

                        if (lastRightXPosition != -1)
                        {
                            changeX = lastRightXPosition - rightHandPos.X;
                            changeY = lastRightYPosition - rightHandPos.Y;

                            MoveImage(image, changeX, changeY);
                        }

                    }else if(ListenMode == "rotate"){
                        double radians = Math.Atan(yDiff/xDiff);
                        double angle = radians * (180 / Math.PI);

                        RotateImage(image, angle);
                        txtAngle.Text = angle.ToString();
                    }


                    //0 is at the top of Y, gets larger as it moves down
                    // higher Y means lower on the screen
                    if (rightHandPos.Y < rightWristPos.Y)
                    {
                        rightMarker.Background = new SolidColorBrush(Colors.Green);
                        rightOn = true;
                    }
                    else
                    {
                        rightMarker.Background = new SolidColorBrush(Colors.Red);
                        rightOn = false;
                    }


                    //0 is at the top of Y, gets larger as it moves down
                    // higher Y means lower on the screen
                    if (leftHandPos.Y < leftWristPos.Y)
                    {
                        leftMarker.Background = new SolidColorBrush(Colors.Green);
                        leftOn = true;
                    }
                    else
                    {
                        leftMarker.Background = new SolidColorBrush(Colors.Red);
                        leftOn = false;
                    }






                    lastHandDiff = xDiff;

                    lastRightYPosition = rightHandPos.Y;
                    lastRightXPosition = rightHandPos.X;
                    lastLeftXPosition = leftHandPos.X;

                    iSkeleton++;
                } // for each skeleton
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            nui.Uninitialize();
            Environment.Exit(0);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void RotateImage(Image img, double angle)
        {
            RotateTransform RT = GetImageRotater(img);
            RT.Angle = angle * 3;
            RT.CenterX = 10;
            RT.CenterY = 10;
        }

        private void ScaleImage(Image img, double amount)
        {
            ScaleTransform STF = GetImageScaler(img);




            double zoom = amount * .1;
            STF.ScaleX = STF.ScaleX + zoom;
            STF.ScaleY = STF.ScaleY + zoom;

            imageMain.Width = imageMain.Width * (1 + zoom);
            imageMain.Height = imageMain.Height * (1 + zoom);



            //shift left for more centered resizing
            TranslateTransform TT = GetImageMover(img);

            int hmultiplier = 100;
            int vmultiplier = 50;

            TT.X = TT.X - (hmultiplier * zoom);
            TT.Y = TT.Y - (vmultiplier * zoom);

        }

        private void MoveImage(Image img, double diffX, double diffY)
        {
            TranslateTransform TT = GetImageMover(img);

            int multiplier = 10;


            TT.X = TT.X - (multiplier*diffX);
            TT.Y = TT.Y - (multiplier*diffY);
        }




        #region TransformGroup
        //all methods that use transform groups to perform work
        private TransformGroup GetImageTransformGroup()
        {
            //lets create a transform group which will handle all transformations
            TransformGroup TFG = new TransformGroup();
            ScaleTransform STF = new ScaleTransform();
            TFG.Children.Add(STF);
            TranslateTransform TTF = new TranslateTransform();
            TFG.Children.Add(TTF);
            RotateTransform RTF = new RotateTransform();
            TFG.Children.Add(RTF);

            return (TFG);
        }

        private TranslateTransform GetImageMover(Image img)
        {
            TransformGroup TFG = (TransformGroup)img.RenderTransform;
            return (TranslateTransform)TFG.Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetImageScaler(Image img)
        {
            TransformGroup TFG = (TransformGroup)img.RenderTransform;
            return (ScaleTransform)TFG.Children.First(tr => tr is ScaleTransform);
        }

        private RotateTransform GetImageRotater(Image img)
        {
            TransformGroup TFG = (TransformGroup)img.RenderTransform;
            return (RotateTransform)TFG.Children.First(tr => tr is RotateTransform);
        }

        #endregion

        private void btnCapture_Click(object sender, RoutedEventArgs e)
        {

            image.Source = video.Source;

            MessageBox.Show("image catured");
        }

        private void btnSetMode_Click(object sender, RoutedEventArgs e)
        {

            testMode = true;
            ComboBoxItem selected = (ComboBoxItem)cbMode.SelectedItem;
            //MessageBox.Show(selected.Content.ToString());

            kl.CallTestingMessage(selected.Content.ToString());
        }


    }
}
