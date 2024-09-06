**Browser Search**

This is a simple program I wrote to perform batch searches against the Bing search engine via a browser of choice.  The program will perform x cycles of y searches, pausing z1 milliseconds between each cycle and z2 milliseconds between each search, using a specific browser.

For example, if you want to perform one cycle of 10 searches, pausing two seconds between each search, and use the Microsoft Edge browser:

	> BrowserSearch.exe --cycles 1 --count 10 --pause 2000 --browser msedge ...

By default, each search will a random prefix phrase, followed by between 2 and 4 words.

Bing often has a cool down period of n minutes between a set (i.e. cycle) of searches, you can pause n milliseconds between three cycles of the 10 searches:

	> BrowserSearch.exe --cycles 3 --cyclePause 1200000 --count 10 --pause 2000 --browser msedge ...

If you want to adjust the number of words per search:

	> BrowserSearch.exe --cycles 3 --cyclePause 1200000 --count 10 -pause 2000 --browser msedge --minWords 3 --maxWords 5 ...

There are a lot of command-line options to tweak things. You can see usage for the program using:

	> BrowserSearch.exe --help

* --StartAllCyclesPauseMs (ms pause before all cycles begin)
* --browser (search engine type)
* --cycles (number of search cycles)
* --cyclePause (ms pause duration between cycles)
* --count (number of searches per cycle)
* --pause (ms pause duration between searches)
* --minWords (minimum words to include in a search)
* --maxWords (maximum words to include in a search)
* --cmdBefore (execute an OS command before a search cycle)
* --cmdBeforeWaitToComplete (ms wait for "before" command to complete)
* --cmdBeforePause (ms pause before a search cycle)
* --cmdAfterCycle (execute an OS command after a search cycle)
* --cmdAfterWaitToComplete (ms wait for ""after" command to complete)
* --cmdAfterCyclePause (ms pause after a search cycle)


I have several scheduled tasks that perform searches using Edge and Chrome (a Chrome plugin is installed to simulate a mobile User Agent).

If you want to search using another engine rather than Bing, that can easily be changed in the source; see the *FormatSearchEngineUrl* method.

**Example**
The following example performs five cycles of 10 searches in intervals of 18 seconds, using 2-4 random words. A pause of 20 minutes (1200000ms) occurs after each cycle.  The Edge browser is used. After each individual search is completed, an 18 second pause occurs (18000ms), then instances of msedge.exe are killed to fully close the  Edge browser.  Finally, a 5 second pause occurs after the kill command.

	> BrowserSearch.exe --cycles 5 --cyclePause 1200000 --count 10 --pause 18000 -b msedge --minWords 2 --maxWords 4 --cmdAfterCycle "taskkill::/F /IM msedge.exe" --cmdAfterCyclePause 5000

The random search words used are in the serachwords.txt file.  The prefix phrases for each search are found in the searchprefixes.txt file.

Have fun.