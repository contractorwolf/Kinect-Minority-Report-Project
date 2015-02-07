using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Microsoft.Research.Kinect.Audio;
using Microsoft.Speech.Recognition;
using System.IO;
using Microsoft.Speech.AudioFormat;
using System.Threading;


namespace SkeletalViewer
{
    class KinectListener
    {

        public BackgroundWorker backgroundWorker1;
        private const string RecognizerId = "SR_MS_en-US_Kinect_10.0";
        private const double minimumConfidence = .95;
        public ProgessResponse Message { get; set; }
        public string Certainty { get; set; }

        public event EventHandler PassMessage;

        private bool testingMode = false;

        public KinectListener()
        {
            backgroundWorker1 = new BackgroundWorker();

            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.DoWork += new DoWorkEventHandler(this.backgroundWorker1_DoWork);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);

        }

        public void StartListening(List<string> words)
        {
            backgroundWorker1.RunWorkerAsync(words);
        }


        public void StopListening()
        {
            backgroundWorker1.CancelAsync();
        }

        void OnPassMessage(EventArgs e)
        {
            if (PassMessage != null)
            {
                PassMessage(this, e);
            }
        }



        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            List<string> words = (List<string>)e.Argument;
            ProgessResponse response = new ProgessResponse()
            {

            };
            using (var source = new KinectAudioSource())
            {
                source.FeatureMode = true;
                source.AutomaticGainControl = false; //Important to turn this off for speech recognition
                source.SystemMode = SystemMode.OptibeamArrayOnly; //No AEC for this sample

                RecognizerInfo ri = SpeechRecognitionEngine.InstalledRecognizers().Where(r => r.Id == RecognizerId).FirstOrDefault();

                if (ri == null)
                {
                    response.Message = "Could not find speech recognizer: {0}. Please refer to the sample requirements."+ RecognizerId;
                    backgroundWorker1.ReportProgress(0,response);
                    return;
                }

                response.Message = "Using: {0}" + ri.Name;
                backgroundWorker1.ReportProgress(0, response);

                using (var sre = new SpeechRecognitionEngine(ri.Id))
                {
                    var wordChoices = new Choices();

                    foreach (string word in words)
                    {
                        wordChoices.Add(word);
                        //backgroundWorker1.ReportProgress(0, "added: " + word);
                    }


                    response.Message = "speech listener started";
                    backgroundWorker1.ReportProgress(0, response);



                    var gb = new GrammarBuilder();
                    //Specify the culture to match the recognizer in case we are running in a different culture.                                 
                    gb.Culture = ri.Culture;
                    gb.Append(wordChoices);

                    // Create the actual Grammar instance, and then load it into the speech recognizer.
                    var g = new Grammar(gb);

                    sre.LoadGrammar(g);
                    sre.SpeechRecognized += SreSpeechRecognized;
                    sre.SpeechHypothesized += SreSpeechHypothesized;
                    sre.SpeechRecognitionRejected += SreSpeechRecognitionRejected;

                    using (Stream s = source.Start())
                    {
                        sre.SetInputToAudioStream(s, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                        sre.RecognizeAsync(RecognizeMode.Multiple);

                        bool cancel = false;
                        while (cancel == false)
                        {
                            if (backgroundWorker1.CancellationPending)
                            {
                                // Set the e.Cancel flag so that the WorkerCompleted event
                                // knows that the process was cancelled.
                                e.Cancel = true;
                                cancel = true;
                                break;
                            }

                            Thread.Sleep(10);
                        }
                        sre.RecognizeAsyncStop();
                    }
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.Message = (ProgessResponse)e.UserState;
            this.OnPassMessage(e);
        }


        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Message.Message = "stopped";
        }



        private void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            //backgroundWorker1.ReportProgress(0,String.Format("Speech Rejected"));
            //if (e.Result != null)
            //DumpRecordedAudio(e.Result.Audio);
        }

        private void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            //backgroundWorker1.ReportProgress(0, String.Format("word: {0}", e.Result.Text));
        }

        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //This first release of the Kinect language pack doesn't have a reliable confidence model, so 
            //we don't use e.Result.Confidence here.


            ProgessResponse pr = new ProgessResponse()
            {
                Confidence = e.Result.Confidence,
                Message = e.Result.Text

            };

            //if (e.Result.Confidence > minimumConfidence)
            //{
            //    //backgroundWorker1.ReportProgress(0, String.Format("Speech Recognized: {0} confidence:{1}", e.Result.Text, e.Result.Confidence));



            if (!testingMode)
            {
                backgroundWorker1.ReportProgress(0, pr);
            }

            //}
            //else
            //{
            //    //backgroundWorker1.ReportProgress(0, String.Format("Speech NOT Recognized: {0} confidence:{1}", e.Result.Text, e.Result.Confidence));

            //}

        }


        public void CallTestingMessage(string message)
        {// fake a particular message coming int, to facilitate testing

            testingMode = true;
            ProgessResponse pr = new ProgessResponse()
            {
                Confidence = 1,
                Message = message
            };

            backgroundWorker1.ReportProgress(0, pr);
        }





        private static void DumpRecordedAudio(RecognizedAudio audio)
        {
            if (audio == null) return;

            int fileId = 0;
            string filename;
            while (File.Exists((filename = "RetainedAudio_" + fileId + ".wav")))
                fileId++;

            Console.WriteLine("\nWriting file: {0}", filename);
            using (var file = new FileStream(filename, System.IO.FileMode.CreateNew))
                audio.WriteToWaveStream(file);
        }


    }

}