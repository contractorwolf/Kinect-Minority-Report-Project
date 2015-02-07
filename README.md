# Kinect-Minority-Report-Project
Manipulate an image with the Kinect V1 using hand movement and voice instructions

video of the code in action:
https://www.youtube.com/watch?v=eseKFOjGbf0


more info on the blog:
http://contractorwolf.com/kinect-minority-report-image-viewer/



Kinect Minority Report Image Viewer

You MUST install all of these prior to attemping to run this project:
Visual Studio Express
http://www.microsoft.com/visualstudio/en-us/products/2010-editions/express

.NET Framework 4.0
http://msdn.microsoft.com/en-us/netframework/aa569263

Kinect SDK download
http://research.microsoft.com/en-us/um/redmond/projects/kinectsdk/download.aspx


You will also need to have a kinect hooked up (duh) and should probably verify that it is setup correctly by running the "skeletal tracker" project that is included with the SDK first before running my app.  This will verify that you have everything setup right before you contact me telling me my app doesnt work.

This project allows you to manipulate images using your hands and voice commands. When the project loads you will see the main 
image (my rockstar pose).  To manipulate this image you need to say clear commands of "scale", "rotate" or "move".  With the 
scale and rotate commands you must say the commands after both hands are in the vertical position (palm out and fingers up). 
You will see the red hand indicators light up green when you have your hands in the correct position. The move command only needs 
the right hand (your right) in the verticle position.  The other commands are "picture" (replaces the rockstar image with a 
picture taken by the kinect of you) and "video" (replaces the image with a video feed coming from the kinect). To quit
manipulating the image (say after you have correctly resized) you only need to say "stop" and the software will go back to its 
waiting for a command state.

If you want to add commands please take a look at how the main program is calling methods on the KinectListener object.  Good Luck and post videos on youtube of any of the cool stuff you do under my video.  Thanks!
