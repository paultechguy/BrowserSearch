**Browser Search**

This is a quick and simple program I wrote to perform batch searches against the Bing search engine via a browser of choice.  I created it for fun to search Bing and accumulate reward points. There are a lot of command-line options to tweak things like:

* Search engine type
* Number of searches
* Pause duration between searches
* Min/Max words to include in a single search
* Execute a OS command before and/or after the search process


I have several scheduled tasks that perform searches using Edge and Chrome (Chrome is set to use a mobile User Agent to obtain mobile reward points).

If you want to search using another engine, that can easily be changed in the source; see the *FormatSearchEngineUrl* method.

**Example**
The following example performs 50 searches in intervals of 1 second, using 2-4 random words. The Chrome browser is used. After the search is completed, a 5 second pause occurs, then instances of chrome.exe are killed to fully close the Chrome browser.

	BrowserSearch.exe" -c 50 -d 1000 --minwords 2 --maxwords 4 -b chrome --commandAfter "taskkill::/F /IM chrome.exe" --commandAfterPause 5000

You can see usage for the program  using:

	BrowserSearch --help

Have fun.