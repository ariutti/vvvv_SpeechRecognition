# vvvv_SpeechRecognition

This repository contains the code of an Italian speech recognition plugin for VVVV.

The repository contains also a patch in order for you to test it a see how it works.

This is still a WIP so there will be more updates and features in future commits.

## Installation

In order for the plugin to work you need to install the Windows Speech Platform 11, see the instruction below:
* download and install the [Microsoft Speech Platform SDK v.11](https://www.microsoft.com/en-us/download/details.aspx?id=27226);
* download and install the [Microsoft Speech Platform Runtime v.11](https://www.microsoft.com/en-us/download/details.aspx?id=27225);
* download and install the Italian [Redistributable Language Pack v.11](https://www.microsoft.com/en-us/download/details.aspx?id=27224): the file name is _MSSpeech_SR_it-IT_TELE.msi_

## How it works

![screenshot](/docs/screenshot.png)

The leftmost input pin of the node (called _Choices_) accepts multi-sliced spreads of strings. These strings are used to build up grammars of alternative words or phrases for the speech recognition engine (from now on SRE).

Every time this input changes, a new grammar is created and provided to the SRE. The SRE starts processing the audio coming from the system default input (mic or line-in): it will check this audio in order to find phrases corresponding to one of the alternatives inside the grammar.

If it detects a match with a confidence greater than the selected threshold (via the second input pin, called _Confidence Threshold_), the node will output the string via the first output (called _Guess_).
The second output (_Confidence_) is the confidence level the SRE had in recognizing the phrase while the third output (_Completed_) will fire a bang on recognition completion.

You can experiment with different levels of confidence threshold!

NOTE: for the moment the plugin works only with the italian language but you can modify this behaviour if you want by changing a single line within the plugin source code (replacing _it_ with _en_ or _fr_ for instance):
```
if(ri.Culture.TwoLetterISOLanguageName.Equals("it"))
```
Clearly you need to install the appropriate [Redistributable Language Pack](https://www.microsoft.com/en-us/download/details.aspx?id=27224) in order for the selected language to be used.

## Development

The plugin has been developed and tested on a _Windows 7 Professional_ machine using:
* VVVV version 50beta35.8_x64;
* Visual Studio Community 2017;

## Credits

Thanks to [mrvux](https://github.com/mrvux) for his [inspirational code](https://sourceforge.net/p/vvvv/code/HEAD/tree/plugins/c%23/String/Speech/) (another [link](https://discourse.vvvv.org/t/speech-recognition/3476));

Thanks to [leavittx](https://github.com/leavittx) for his [inspirational code](https://github.com/leavittx/vvvv-SpeechRecognition);

Thanks to [id144](https://github.com/id144) (Andrej Boleslavsky) for the precious VVVV tutorial.

## References

[Microsoft Speech Platform Documentation](https://msdn.microsoft.com/en-us/library/office/hh361572(v=office.14).aspx)
