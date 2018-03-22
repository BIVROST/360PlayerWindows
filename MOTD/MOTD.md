MOTD (Message Of The Day)
=========================

Module for retrieving release updates and important messages from a remote server.

```plantuml
skinparam monochrome reverse
skinparam backgroundColor transparent

"360Player" -> "MOTD Server": send GET HTTP request with product name, current version and installId
"MOTD Server" --> "360Player": json with an action

alt type=none

... nothing happens ...
else type=notification

... a toast is displayed with an optional link ...

"360Player" -> "360Player": the user clicks on a link
"360Player" ->  : link launched by a notification's url

else type=popup

... a popup with a website is displayed ....

"360Player" -> "A website": embedded webpage in a popup
"A website" -> "A website": additional resources are loaded
"A website" -> "360Player": optionally, a callback might happen

else an error occured with the MOTD server

... nothing happens ...

end
```


API reference
-------------

## Endpoint `https://tools.bivrost360.com/motd-server/v1/`

Main communication endpoint, the only one that the client uses.

### Parameters

* GET param `product`:string  
  The product name, example: `360Player`.
* GET param `installId`:string  
  Unique identificator of this installation, example: `13bf5143-c542-4126-91db-60ded9f74926`
* GET param `version`:string  
  The version of the product, used for informing about changes (displaying changelog).  
  Format of the version string is `([0-9]+)(.([0-9]+))*` (example. `1.0.0.123`).  
  Version numbers are compared starting from the first element, that is `1.0` is the same as `1.0.0.0`, and `2.0` is before `2.0.0.1`.  
  The `version` can be also null when the version is not known (for example a development build).


### Return value

A JSON with one of the following structures determined by the `type` field.
All fields returned are required, unless marked as optional.

Common fields returned:
* `motd-server-version`:string - The version of the server, uses the `([0-9]+)(.([0-9]+))*` version format.
* `type`:string - one of the below types below

#### Nothing (`none`)

Most of the time, MOTD server will not return anything. This should be the case when the version installed is the most recent one and there is no other important message to be delivered.

```json
{
	"motd-server-version": "1.0",
	"type": "none"
}
```


#### A notification (`notification`)

A notification in the corner (a toast).

The pair of values `link` and `url` are optional (they can both be absent or both be set, not just one). If both are set, then a link will appear after the text of the notification. If only one is set, it is ignored.

```json
{
	"motd-server-version": "1.0",
	"type": "notification",
	"text": "the text of the notification",
	"link": "optional: title of the link at the end of the notification",
	"url": "http://example.com/the/link/for/url#optional"
}
```

Additional fields returned:
* `text`:string - Text that will be displayed as plain text.
* `link`:string (optional) - Text that will be displayed as the link title.
* `url`:string (optional) - The address that will be opened when the link is opened. 


#### A HTML popup (`popup`)

A full HTML window. 

```json
{ 
	"motd-server-version": "1.0",
	"type": "popup",
	"title": "the title of the popup",
	"url": "http://tools.bivrost360.com/motd-server/v1/some-message.html",
	"width": 600,
	"height": 400
}
```

Additional fields returned:
* `title`:string - The title of the window with the webview embedded.
* `url`:string - The address that will be opened in the window.  
  It can be any HTTP or HTTPS URL from any domain.
* `width`:number (optional, default `600`) - The width of the window (with decoration).
* `height`:number (optional, default `400`) - The height of the window (with decoration.

The embedded web view can bind additional Javascript callbacks to the main application. For example an update button or privileged UI actions.



Network issue behavior
----------------------

HTTPS certificates are verified in case of API calls and will fail the request when having an issue.

When a network request to the MOTD server fails, it is ignored and logged.

When the main request for the HTML file fails, the popup is not displayed.
Requests from that HTML file can fail.

Requests from the `url` of an `notification` are done by the operating system and are not checked for success or validity.