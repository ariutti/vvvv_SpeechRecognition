using System;
using System.ComponentModel.Composition;
using Microsoft.Speech.Recognition;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

namespace VVVV.Nodes
{
	/* PLUGIN INFO ********************************************************/
	[PluginInfo(Name = "speechnico1",
				Category = "String",
				Help = "An italian speech recognition plugin",
				Tags = "c#")]

	/* CLASS **************************************************************/
	public class Stringspeechnico1Node : IPluginEvaluate
	{
		/* Pins definition ************************************************/
		[Input("Choices", DefaultString = "input strings")]
		public IDiffSpread<string> InChoices;

		[Input("Confidence Threshold!")]
		public IDiffSpread<double> InConfidenceThreshold;

		[Output("Guess", DefaultString = "")]
		public ISpread<string> OutGuess;

		[Output("Confidence")]
		public ISpread<double> OutConfidence;
		
		[Output("Completed", IsBang=true)]
		public ISpread<bool> OutBangCompleted;

		[Import()]
		public ILogger FLogger;

		/* class fields *******************************************************/
		public RecognizerInfo info = null;
		public SpeechRecognitionEngine sre = null;
		public static Choices choices;
		public Thread th;
		public int nSlices = 0;

    	// shared fields between threads
		public string guess = "";
		public double confidence = 0.0;
		public bool bCompleted = false;
    	// this locker is used for thread safety
   		static readonly object locker = new object();


		/* CONSTRUCTOR ********************************************************/
		public Stringspeechnico1Node()
		{
			// maybe there's a better way to initialize the sre language
			foreach( RecognizerInfo ri in SpeechRecognitionEngine.InstalledRecognizers() ) {
				if( ri.Culture.TwoLetterISOLanguageName.Equals("it")) {
					info = ri;
					break;
				}
			}
			if( info == null ) {
				//error: find a way to manage it?
			}

			sre = new SpeechRecognitionEngine( info );
			
			// Speech recognition Engine Settings //
			sre.SetInputToDefaultAudioDevice();
			// timing
			sre.InitialSilenceTimeout = TimeSpan.FromSeconds(3);
            sre.BabbleTimeout = TimeSpan.FromSeconds(2);
            sre.EndSilenceTimeout = TimeSpan.FromSeconds(1);
            sre.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(1.5);
			// handlers
            sre.SpeechDetected += new EventHandler<SpeechDetectedEventArgs>(h_detected);
            sre.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(h_hypothesized);
            sre.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(h_rejected);
            sre.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(h_recognized);
            sre.RecognizeCompleted += new EventHandler<RecognizeCompletedEventArgs>(h_completed);
		}

		/* RECON THREAD *******************************************************/
		public void recon() {
			FLogger.Log( LogType.Debug, "Th ({0} {1}) - recon: {2}, Priority {3}",
										Thread.CurrentThread.ManagedThreadId,
										Thread.CurrentThread.Name,
										Thread.CurrentThread.ThreadState,
										Thread.CurrentThread.Priority);
			sre.RecognizeAsync(RecognizeMode.Single);
		}

		/* EVALUATE ***********************************************************/
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax) {
			
			if( InConfidenceThreshold.IsChanged ) {
				var c = (Int32)InConfidenceThreshold[0];
				if(c > 100)
					c=100;
				else if( c < 0)
					c = 0;
				//FLogger.Log(LogType.Debug, sre.QueryRecognizerSetting("CFGConfidenceRejectionThreshold").ToString() );
				sre.UpdateRecognizerSetting("CFGConfidenceRejectionThreshold", c);
				//FLogger.Log(LogType.Debug, sre.QueryRecognizerSetting("CFGConfidenceRejectionThreshold").ToString() );
			}
			
			// do something only if input is changed
			if( InChoices.IsChanged ) {
				FLogger.Log(LogType.Debug, "## Input changed ##");
				
				// cancle the current recognition operation (if any)
				sre.RecognizeAsyncCancel();
				
				FLogger.Log(LogType.Debug, "Current Confidence: {0}", sre.QueryRecognizerSetting("CFGConfidenceRejectionThreshold"));
				
				// clear the current grammar
				sre.UnloadAllGrammars();
				
				// generate grammar from input strings
				GrammarBuilder gb = new GrammarBuilder();
				gb.Culture = sre.RecognizerInfo.Culture;

				// fill the list of alternative words
				choices = new Choices();
				for(int i=0; i< SpreadMax; i++) {
					FLogger.Log(LogType.Debug, "  slice: "+InChoices[i] );
					choices.Add( InChoices[i] );
				}
				// and put it inside the Grammar Builder
				gb.Append(choices);
				// use the grammar builder to build the grammar
				Grammar g = new Grammar( gb );
				// load the grammar inside the engine
				sre.LoadGrammar( g );
				
				FLogger.Log( LogType.Debug, g.ToString());
				
				th = new Thread( recon );
				th.Name = "Recon";
				th.Start();
			} // end of if( FInput.IsChanged )

			//FLogger.Log(LogType.Debug, bCompleted.ToString() );
		
			// replicate the string for all
            lock(locker) {
                OutGuess[0] = guess;
            	OutConfidence[0] = confidence;
            }

			OutBangCompleted[0] = bCompleted;
			bCompleted = false;		
		}

		
		/* HANDLERS ***********************************************************/
		public void h_detected(object sender, SpeechDetectedEventArgs e) {
			FLogger.Log( LogType.Debug, "Th {0} - detected;", Thread.CurrentThread.ManagedThreadId);
		}
		
        public void h_hypothesized(object sender, SpeechHypothesizedEventArgs e) {
			FLogger.Log( LogType.Debug, "Th {0} - hyp {1} - score {2};", Thread.CurrentThread.ManagedThreadId, 
                                                    e.Result.Text,
                                                    e.Result.Confidence);
		}
		
		public void h_rejected(object sender, SpeechRecognitionRejectedEventArgs e) {
			FLogger.Log( LogType.Debug, "Th {0} - rejected;", Thread.CurrentThread.ManagedThreadId);
            foreach (RecognizedPhrase phrase in e.Result.Alternates) {
                FLogger.Log( LogType.Debug, "  Rejected phrase; {0} - score: {1}: ", phrase.Text, phrase.Confidence);
            }
		}

		public void h_recognized(object sender, SpeechRecognizedEventArgs e) {
			
			lock(locker) {
				confidence = e.Result.Confidence;
				guess = e.Result.Text;
			}
			FLogger.Log( LogType.Debug, "Th {0} - recon {1} - {2};", 
                                            Thread.CurrentThread.ManagedThreadId, 
                                            e.Result.Text,
                                            e.Result.Confidence);
		}

		public void h_completed(object sender, RecognizeCompletedEventArgs e) {		
			if( e.Result != null ) {
                FLogger.Log( LogType.Debug, "Th {0} - completed '{1}' - {2};", 
											Thread.CurrentThread.ManagedThreadId, 
											e.Result.Text,
                                            e.Result.Confidence);
				bCompleted = true;				
            } else {
                FLogger.Log( LogType.Debug, "Th {0} - completed;", 
            								Thread.CurrentThread.ManagedThreadId);
            }
				
			FLogger.Log( LogType.Debug, "");
		}
	}
}
