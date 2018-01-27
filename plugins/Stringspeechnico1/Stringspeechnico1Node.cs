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
				Help = "Basic template with one string in/out",
				Tags = "c#")]

	/* CLASS **************************************************************/
	public class Stringspeechnico1Node : IPluginEvaluate
	{
		/* Pins definition ************************************************/
		[Input("Choices", DefaultString = "input strings")]
		public IDiffSpread<string> FInput;

		[Input("BBang!", IsBang=true)]
		public IDiffSpread<bool> FBang;

		[Output("Guess", DefaultString = "")]
		public ISpread<string> FOutput;

		[Output("Confidence")]
		public ISpread<double> Confidence;

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
    static bool done = false;
    // this locker is used for thread safety
    static readonly object locker = new object();


		/* CONSTRUCTOR ********************************************************/
		public Stringspeechnico1Node()
		{
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
			sre.SetInputToDefaultAudioDevice();

			/* add the event handlers */
			sre.SpeechRecognized   += new EventHandler<SpeechRecognizedEventArgs>(sr);
			sre.RecognizeCompleted += new EventHandler<RecognizeCompletedEventArgs>(rc);
			//sre.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(srr);

			th = new Thread( recon );
			th.Name = "Recon";
			th.Start();
		}


		/* RECON THREAD *******************************************************/
		public void recon()
		{
			while( 1 != 2 )
			{
				FLogger.Log( LogType.Debug, "recon: Thread ({0} - {1}): {2}, Priority {3}",
											Thread.CurrentThread.ManagedThreadId,
											Thread.CurrentThread.Name,
											Thread.CurrentThread.ThreadState,
											Thread.CurrentThread.Priority);
				sre.Recognize();
				FLogger.Log( LogType.Debug, "fine del riconoscimento?!");
			}
		}


		/* CALLBACKS **********************************************************/
		// speech recognized
		public void sr(object sender, SpeechRecognizedEventArgs e)
		{
			lock(locker)
			{
				confidence = e.Result.Confidence;
				guess = e.Result.Text;
			}
			FLogger.Log( LogType.Debug, "Guess: {0}, Confidence {1}.", guess, confidence.ToString() );
			//th.Join();
		}

		// recognize complete
		public void rc(object sender, RecognizeCompletedEventArgs e)
		{
			// who is the thread which is running this piece of code
			FLogger.Log( LogType.Debug, "rc: Thread ({0} - {1}): {2}, Priority {3}",
											Thread.CurrentThread.ManagedThreadId,
											Thread.CurrentThread.Name,
											Thread.CurrentThread.ThreadState,
											Thread.CurrentThread.Priority);
			FLogger.Log( LogType.Debug, "completed");
			//done = true;
		}

		/* EVALUATE ***********************************************************/
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			// do something only if input is changed
			if( FInput.IsChanged )
			{
				FLogger.Log(LogType.Debug, "## Input changed ##");

				// generate grammar from input strings
				GrammarBuilder gb = new GrammarBuilder();
				gb.Culture = sre.RecognizerInfo.Culture;

				// fill the list of alternative words
				choices = new Choices();
				for(int i=0; i< SpreadMax; i++)
				{
					FLogger.Log(LogType.Debug, "slice: "+FInput[i] );
					choices.Add( FInput[i] );
				}
				// and put it inside the Grammar Builder
				gb.Append(choices);
				// use the grammar builder the build the grammar
				Grammar g = new Grammar( gb );
				// load the grammar inside the engine
				// Beware: access to shared data
				//lock( locker ) {
					sre.LoadGrammar( g );
				//}

				//th = new Thread( recon );
				//th.Name = "Recon";
				//th.Start();
			} // end of if( FInput.IsChanged )

			// replicate the string for all
			FOutput[0] = guess;
			/*
			for (int i = 0; i < SpreadMax; i++)
			{
				// Beware: access to shared data
				//lock( locker) {
					FOutput[0] = guess;
			  //}
				*/
			}


		} // end of Evaluate(int SpreadMax)
	}
}
