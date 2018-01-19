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
		[Input("Input", DefaultString = "cane")]
		public IDiffSpread<string> FInput;

		[Output("Output")]
		public ISpread<string> FOutput;

		[Import()]
		public ILogger FLogger;

		/* class fields *******************************************************/
		public string guess = "";
		public RecognizerInfo info = null;
		public SpeechRecognitionEngine sre = null;
		public bool done = true;

		
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
			gb.Append("supercallifragilistichespiralidoso");
			Grammar g = new Grammar( gb );
			/* carica la grammatica nell'sre */
			sre.LoadGrammar( g );
		}

		/* speech recognized */
		public void sr(object sender, SpeechRecognizedEventArgs e)
		{
			FLogger.Log( LogType.Debug, "recognized");
			done = true;
		}

		/* recognize complete */
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
			FOutput.SliceCount = SpreadMax;
			
			//sre.Recognize();
			
			//if( !done )
			//{
			//	sre.Recognize();
			//	FLogger.Log(LogType.Debug, "trying to recognize");
			//}
		
			// do something only if input is changed
			if( FInput.IsChanged )
			{
				FLogger.Log(LogType.Debug, "## Input changed ##");
				done = false;
				
				/*
				for(int i=0; i< SpreadMax; i++)
				{
					FLogger.Log(LogType.Debug, "slice: "+FInput[i] );
				}
				FLogger.Log(LogType.Debug, "");
				*/

				/* generate grammar from input strings */
				GrammarBuilder gb = new GrammarBuilder();
				gb.Culture = sre.RecognizerInfo.Culture;
				for(int i=0; i< SpreadMax; i++) {
					FLogger.Log(LogType.Debug, "slice: "+FInput[i] );
					gb.Append( FInput[i] );
				}
				Grammar g = new Grammar( gb );
				/* carica la grammatica nell'sre */
				sre.LoadGrammar( g );

				//FLogger.Log( LogType.Debug, "start recognizing: " );
				//sre.Recognize();
				
				// replicate the string for all
				for (int i = 0; i < SpreadMax; i++)
				{
					FOutput[i] = guess;
				}
			} // end of if( FInput.IsChanged )
		} // end of Evaluate(int SpreadMax)
	}
}
