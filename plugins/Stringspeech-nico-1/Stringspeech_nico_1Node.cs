#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "speech-nico-1", Category = "String", Help = "Basic template with one string in/out", Tags = "c#")]
	#endregion PluginInfo
	public class speech-nico-1 : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input", DefaultString = "hello c#")]
		public IDiffSpread<string> FInput;

		[Output("Output")]
		public ISpread<string> FOutput;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = SpreadMax;

			if( FInput.IsChanged )
			{
				//FLogger.Log( LogType.Debug, "cambiato: "+FOutput.SliceCount.ToString() );
				FLogger.Log( LogType.Debug, "cambiato: "+SpreadMax );
				//FLogger.Log( LogType.Debug, FOutput.SliceCount.ToString() );
				
				// replicate the string for all
				for (int i = 0; i < SpreadMax; i++)
					FOutput[i] = FInput[0];
			}
			
			//for (int i = 0; i < SpreadMax; i++)
			//	FOutput[i] = FInput[i].Replace("c#", "vvvv");
			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
