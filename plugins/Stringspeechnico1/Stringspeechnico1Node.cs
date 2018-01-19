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
	[PluginInfo(Name = "speechnico1", Category = "String", Help = "Basic template with one string in/out", Tags = "c#")]

	/* CLASS **************************************************************/
	public class Stringspeechnico1Node : IPluginEvaluate
	{
		/* Pins definition ************************************************/
		[Input("Input", DefaultString = "input strings")]
		public IDiffSpread<string> FInput;

		[Output("Output")]
		public ISpread<string> FOutput;
		
		//[OutputAttribute ("Bang")]
		//public ISpread<bang> 

		[Import()]
		public ILogger FLogger;

		/* class fields *******************************************************/
		public string guess = "";
		public RecognizerInfo info = null;
		public SpeechRecognitionEngine sre = null;
		public bool done = true;
		public Thread th;
		public int nSlices = 0;
		public double confidence = 0.0;

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
			
			/* lets load default grammar */
			GrammarBuilder gb = new GrammarBuilder();
			gb.Culture = sre.RecognizerInfo.Culture;
			
			Choices words = new Choices();
			words.Add(new string[]{
				"cane",
				"abbaiare",
				"cotoletta",
				"milano",
				"freccia nera",
				"luigi",
				"gianni",
				"perfavore",
				"passami",
				"il",
				"sale"
			});
			
			gb.Append(words);
			Grammar g = new Grammar( gb );
			// carica la grammatica nell'sre
			sre.LoadGrammar( g );
			
			th = new Thread( recon );
			th.Start();
		}

	
		/* RECON THREAD *******************************************************/
		public void recon() 
		{
			while( 1 != 2 )
			{
				FLogger.Log( LogType.Debug, "Thread ({0}): {1}, Priority {2}", 
											Thread.CurrentThread.ManagedThreadId,
											Thread.CurrentThread.ThreadState,
											Thread.CurrentThread.Priority
				);
				
				sre.Recognize();
				Thread.Sleep( 1000 );
			}
		}
		
		
		/* CALLBACKS **********************************************************/
		// speech recognized
		public void sr(object sender, SpeechRecognizedEventArgs e)
		{
			confidence = e.Result.Confidence;
			if( confidence > 0.95 ) 
			{
				FLogger.Log( LogType.Debug, "confidence: " + confidence );
				guess = e.Result.Text;
				FLogger.Log( LogType.Debug, "recognized: " + guess );
			}
		}
		
		
		
		// recognize complete
		public void rc(object sender, RecognizeCompletedEventArgs e)
		{
			FLogger.Log( LogType.Debug, "completed");
			//done = true;
		}
		
		/* recognize complete
		public void srr(object sender, SpeechRecognitionRejectedEventArgs e)
		{
			FLogger.Log( LogType.Debug, "rejected");
		}
		*/


		/* EVALUATE ***********************************************************/
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			/*
			FLogger.Log( LogType.Debug, "Main Thread ({0}): {1}, Priority {2}",
											Thread.CurrentThread.ManagedThreadId,
											Thread.CurrentThread.ThreadState,
											Thread.CurrentThread.Priority
			);
			*/
			nSlices = SpreadMax;
			FOutput.SliceCount = SpreadMax;
					
			// do something only if input is changed
			if( FInput.IsChanged )
			{
				FLogger.Log(LogType.Debug, "## Input changed ##");
				/*
				done = false;

				// generate grammar from input strings 
				GrammarBuilder gb = new GrammarBuilder();
				gb.Culture = sre.RecognizerInfo.Culture;
				
				for(int i=0; i< SpreadMax; i++) 
				{
					FLogger.Log(LogType.Debug, "slice: "+FInput[i] );
					gb.Append( FInput[i] );
				}
				Grammar g = new Grammar( gb );
				// carica la grammatica nell'sre
				sre.LoadGrammar( g );
				*/
				
				//th.Start();
			} // end of if( FInput.IsChanged )
			
						// replicate the string for all
			for (int i = 0; i < nSlices; i++)
			{
				FOutput[i] = guess;				
			}
			
			
		} // end of Evaluate(int SpreadMax)
	}
}
