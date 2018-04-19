MOTD (Message Of The Day)
=========================

Module for retrieving release updates and important messages from a remote server.

```plantuml
'skinparam monochrome reverse
skinparam backgroundColor transparent

participant "360Player" as app
participant "MOTD Server" as motd
participant "A website" as website


note right of website
This can also be an MOTD Server endpoint
end note


app -> motd: send HTTP request with app details
motd --> app: json with an action

alt type=none

... nothing happens ...
else type=notification

... a toast is displayed with an optional link ...

app -> app: the user clicks on a link
app ->  : link launched by a notification's url

else type=popup

... a popup with a website is displayed ....

app -> website: embedded webpage in a popup
website -> website: additional resources are loaded
website -> app: optionally, a callback might happen

else an error occured with communication to the MOTD server

... nothing happens, error is logged ...

else an error is reported by the MOTD server

... nothing happens, error is logged ...

end
```


API reference
-------------

## Endpoint `motd`
URL: `https://tools.bivrost360.com/motd-server/?action=motd`

Main communication endpoint, the only one that the client uses.

### Parameters

* POST param `product`:string  
  The product name, example: `360player-windows`.
* POST param `install-id`:string  
  Unique identificator of this installation, example: `13bf5143-c542-4126-91db-60ded9f74926`
* POST param `version`:string  
  The version of the product used for informing about changes (displaying changelog).  
  Format of the version string is `([0-9]+)(.([0-9]+))*` (example. `1.0.0.123`).  
  Version numbers are compared starting from the first element, that is `1.0` is the same as `1.0.0.0`, and `2.0` is before `2.0.0.1`.  
  The `version` can be also null when the version is not known (for example a development build).

Example request:

```bash
curl --http1.1 -v                                               \
     -Fproduct="360player-windows"                              \
     -Finstall-id="13bf5143-c542-4126-91db-60ded9f74926"        \
     -Fversion="1.0.0.196"                                      \
     "https://tools.bivrost360.com/motd-server/?action=motd"    ;
```


### Return value

A JSON with one of the following structures determined by the `type` field.
All fields returned are required, unless marked as optional.

Common fields returned:
* `motd-server-version`:string - The version of the server, uses the `([0-9]+)(.([0-9]+))*` version format.
* `type`:string - one of the types below

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

The pair of values `link` and `uri` are optional (they can both be absent or both be set, not just one). If both are set, then a link will appear after the text of the notification. If only one is set, it is ignored.

```json
{
	"motd-server-version": "1.0",
	"type": "notification",
	"text": "the text of the notification",
	"link": "optional: title of the link at the end of the notification",
	"uri": "http://example.com/the/link/for/url#optional"
}
```

Additional fields returned:
* `text`:string - Text that will be displayed as plain text.
* `link`:string (optional) - Text that will be displayed as the link title.
* `uri`:string (optional) - The address that will be opened when the link is opened. 
  This is a full URI, not just an URL, so it doesn't have to be a HTTP/HTTPS protocol  
  The special value `::update::` will trigger the auto-update system.


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
* `height`:number (optional, default `400`) - The height of the window (with decoration).

The embedded web view can bind additional Javascript callbacks to the main application. For example an update button or privileged UI actions.


#### A server error (`error`)

Returned when the server encounters an issue.

```json
{
	"motd-server-version": "1.0",
	"type": "error",
	"message": "the message that will be logged",
}
```


## Endpoint `upgrade`
URL: `https://tools.bivrost360.com/motd-server/?action=upgrade`

Used to get a greeting message after an upgrade.

### Parameters

* POST param `product`:string  
  The product name, example: `360player-windows`.
* POST param `install-id`:string  
  Unique identificator of this installation, example: `13bf5143-c542-4126-91db-60ded9f74926`
* POST param `version-previous`:string  
  The version of the product, used for informing about the changes (displaying changelog).  
  Format of the version string is `([0-9]+)(.([0-9]+))*` (example. `1.0.0.123`).  
  Version numbers are compared starting from the first element, that is `1.0` is the same as `1.0.0.0`, and `2.0` is before `2.0.0.1`.  
  The `version-previous` can also be also null when the version is not known (for example a development build).
* POST param `version-current`:string  
  The version of the product, used for informing about the changes (displaying changelog).  
  Format of the version string is `([0-9]+)(.([0-9]+))*` (example. `1.0.0.123`).  
  Version numbers are compared starting from the first element, that is `1.0` is the same as `1.0.0.0`, and `2.0` is before `2.0.0.1`.  
  The `version` can also be null when the version is not known (for example a development build).

Version numbers in a request should differ.

Example request:

```bash
curl --http1.1 -v                                                  \
     -Fproduct="360player-windows"                                 \
     -Finstall-id="13bf5143-c542-4126-91db-60ded9f74926"           \
     -Fversion-current="1.0.0.196"                                 \
     -Fversion-previous="1.0.0.194"                                \
     "https://tools.bivrost360.com/motd-server/?action=upgrade"    ;
```


### Return values

The same as in `motd`.



Network issue behavior
----------------------

HTTPS certificates are verified in case of API calls and will fail the request when an issue occurs.

When a network request to the MOTD server fails, it is ignored and logged.

When the main request for the HTML file fails, the popup is not displayed.
Requests from that HTML file can fail.

Requests from the `url` of an `notification` are done by the operating system and are not checked for success or validity.