# sg.webbrowsercall
Simple dotnet utility library to open URL in different installed web browsers.
Provides functionality to query installed web browsers, and to open an URL in specifically selected web browser.

## Use Case
Use this library if:
* Your application has URL(s) which the user wants to open, and
* The user should be able to choose which installed browser to open the URL in.

For example:

A user might have configured FireFox as his/her default web browser.
But the URL from your program, opens a web frontend, which works best with Chrome.

## Details
TODO

## License
This library is provided under the 3-Clause BSD License.

For further details, see the [LICENSE](./LICENSE) file.

## Alternatives
This library is not for every use case to open URLs in web browsers.
If you have a different use case one of these alternative solutions might be easier for you:

### Default Browser
Don't use this, if you are just interested in opening an URL in the users default web browser.
If that is your scenario, then simply let the OS open the URL:

```
Process.Start(new ProcessStartInfo()
	{
		FileName = url,
		UseShellExecute = true
	});
```

### Specific Browser
If you want to have in-depth control over a specific browser, have a look at their API.
