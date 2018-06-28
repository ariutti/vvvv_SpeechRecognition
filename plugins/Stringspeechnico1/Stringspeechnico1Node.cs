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
	[PluginInfo(Name = "speechnico1", Category = "String", Help = "An italian speech recognition plugin", Author = "Nicola Ariutti", Tags = "c#")]

	/* CLASS **************************************************************/
	public class Stringspeechnico1Node : IPluginEvaluate, IDisposable
	{
		/* Pins definition ************************************************/
		[Input("Choices", DefaultString = "input strings")]
		public IDiffSpread<string> FInChoices;

		[Input("Confidence Threshold!", IsSingle=true)]
		public IDiffSpread<double> FInConfidenceThreshold;

		[Output("Guess", DefaultString = "")]
		public ISpread<string> FOutGuess;

		[Output("Confidence")]
		public ISpread<double> FOutConfidence;

		[Output("Completed", IsBang=true)]
		public ISpread<bool> FOutBangCompleted;

		[Output("Rejected", IsBang=true)]
		public ISpread<bool> FOutBangRejected;

		[Import()]
		public ILogger FLogger;

		/* class fields *******************************************************/
		// TODO: public? static? public static?
		public static RecognizerInfo info = null;
		public static SpeechRecognitionEngine sre = null;
		public static Choices choices;
		public Thread th;
		public int nSlices = 0;

		// shared fields between threads
		public string guess = "";
		public double confidence = 0.0;
		public bool bCompleted, bPrevCompleted   = false;
		public bool bRejected, bPrevRejected     = false;

		// this locker is used for thread safety
		static readonly object locker = new object();

		/* CONSTRUCTOR ********************************************************/
		public Stringspeechnico1Node()
		{
			// TODO: maybe there's a better way to initialize the sre language
			foreach (RecognizerInfo ri in SpeechRecognitionEngine.InstalledRecognizers())
			{
				if(ri.Culture.TwoLetterISOLanguageName.Equals("it"))
				{
					info = ri;
					break;
				}
			}
			if(info == null)
			{
				// TODO: find a way to manage this error?
			}

			sre = new SpeechRecognitionEngine(info);

			// Speech recognition Engine Settings //
			sre.SetInputToDefaultAudioDevice();
			// timing
			sre.InitialSilenceTimeout = TimeSpan.FromSeconds(3);
			sre.BabbleTimeout = TimeSpan.FromSeconds(2);
			sre.EndSilenceTimeout = TimeSpan.FromSeconds(1);
			sre.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(1.5);
			// handlers
			sre.SpeechDetected += new EventHandler<SpeechDetectedEventArgs>(DetectedCB);
			sre.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(HypothesizedCB);
			sre.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(RejectedCB);
			sre.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(RecognizedCB);
			sre.RecognizeCompleted += new EventHandler<RecognizeCompletedEventArgs>(CompletedCB);
		}

		/* DESTRUCTOR *********************************************************/
		public void Dispose()
		{
			FLogger.Log(LogType.Debug, "destructor;");
		}

		/* RECON THREAD *******************************************************/
		public void Recon()
		{
			FLogger.Log(LogType.Debug, "Th ({0} {1}) - recon: {2}, Priority {3}",
										Thread.CurrentThread.ManagedThreadId,
										Thread.CurrentThread.Name,
										Thread.CurrentThread.ThreadState,
										Thread.CurrentThread.Priority);
			sre.RecognizeAsync(RecognizeMode.Single);
		}


		/* EVALUATE ***********************************************************/
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{

			/*
			// reset all the bangs
			lock(locker)
			{
				if(bCompleted) {
					bCompleted = false;
					FOutBangCompleted[0] = bCompleted;
				}
				if(bRejected) {
					bRejected = false;
					FOutBangRejected[0] = bRejected;
				}
			}
			*/
			

			if(FInConfidenceThreshold.IsChanged)
			{
				var c = (Int32)FInConfidenceThreshold[0];
				if(c > 100)
				{
					c = 100;
				}
				else if(c < 0)
				{
					c = 0;
				}
				//FLogger.Log(LogType.Debug, sre.QueryRecognizerSetting("CFGConfidenceRejectionThreshold").ToString() );
				sre.UpdateRecognizerSetting("CFGConfidenceRejectionThreshold", c);
				FLogger.Log(LogType.Debug, "Changed Threshold: {0}", sre.QueryRecognizerSetting("CFGConfidenceRejectionThreshold").ToString() );
			}

			// do something only if input is changed
			if(FInChoices.IsChanged)
			{
				FLogger.Log(LogType.Debug, "## Input changed ##");

				// cancle the current recognition operation (if any)
				sre.RecognizeAsyncCancel();

				FLogger.Log(LogType.Debug, "Current culture: {0};", sre.RecognizerInfo.Culture.Name);
				FLogger.Log(LogType.Debug, "Current confidence: {0}%;",
											sre.QueryRecognizerSetting("CFGConfidenceRejectionThreshold"));

				// clear the current grammar
				sre.UnloadAllGrammars();

				// generate grammar from input strings
				GrammarBuilder gb = new GrammarBuilder();
				gb.Culture = sre.RecognizerInfo.Culture;

				// fill the list of alternative words
				choices = new Choices();
				for(int i=0; i< SpreadMax; i++)
				{
					FLogger.Log(LogType.Debug, "  slice: '" + FInChoices[i] + "';");
					choices.Add(FInChoices[i]);
				}
				// and put it inside the Grammar Builder
				gb.Append(choices);
				// use the grammar builder to build the grammar
				Grammar g = new Grammar(gb);
				// load the grammar inside the engine
				sre.LoadGrammar(g);

				th = new Thread(Recon);
				th.Name = "Recon";
				th.Start();
			} // end of if(FInput.IsChanged)

			//FLogger.Log(LogType.Debug, bCompleted.ToString());

			// replicate the string for all
			lock(locker) 
			{
				FOutGuess[0] = guess;
				FOutConfidence[0] = confidence;
				
				if(bCompleted != bPrevCompleted) {
					FOutBangCompleted[0] = bCompleted;
					bPrevCompleted = bCompleted;
					if(bCompleted)
						bCompleted = false;
				}
				
				if(bRejected != bPrevRejected)
				{
					FOutBangRejected[0] = bRejected;
					bPrevRejected = bRejected;
					if(bRejected)
						bRejected = false;				
				}
			}
		}


		/* HANDLERS ***********************************************************/
		public void DetectedCB(object sender, SpeechDetectedEventArgs e)
		{
			FLogger.Log(LogType.Debug, "Th {0} -     detected;",
																	Thread.CurrentThread.ManagedThreadId);
		}


		public void HypothesizedCB(object sender, SpeechHypothesizedEventArgs e)
		{
			FLogger.Log(LogType.Debug, "Th {0} - hypothesized - {2:0.0000} - '{1}';",
																	Thread.CurrentThread.ManagedThreadId,
																	e.Result.Text,
																	e.Result.Confidence);
		}


		public void RejectedCB(object sender, SpeechRecognitionRejectedEventArgs e)
		{
			FLogger.Log(LogType.Debug, "Th {0} -     rejected:",
																	Thread.CurrentThread.ManagedThreadId);

			foreach (RecognizedPhrase phrase in e.Result.Alternates)
			{
				FLogger.Log(LogType.Debug, "    rejected phrase - {1:0.0000} - '{0}'; ",
																	phrase.Text,
																	phrase.Confidence);
			}
			lock(locker)
			{
				bRejected = true;
				//FOutBangRejected[0] = bRejected; // does it work?
			}
		}


		public void RecognizedCB(object sender, SpeechRecognizedEventArgs e)
		{
			FLogger.Log(LogType.Debug, "Th {0} -   recognized - {2:0.0000} - '{1}';",
																	Thread.CurrentThread.ManagedThreadId,
																	e.Result.Text,
																	e.Result.Confidence);
			lock(locker)
			{
				confidence = e.Result.Confidence;
				guess = e.Result.Text;
			}
		}


		public void CompletedCB(object sender, RecognizeCompletedEventArgs e)
		{
			if(e.Result != null)
			{
				FLogger.Log(LogType.Debug, "Th {0} -    completed - {2:0.0000} - '{1}';",
																	Thread.CurrentThread.ManagedThreadId,
																	e.Result.Text,
																	e.Result.Confidence);
				lock(locker)
				{
					bCompleted = true;
					//FOutBangCompleted[0] = bCompleted; // does it work?
				}
			}
			else
			{
				FLogger.Log(LogType.Debug, "Th {0} -    completed;",
																	Thread.CurrentThread.ManagedThreadId);
			}
			FLogger.Log(LogType.Debug, "");
		}
	}
}
