# Server

This is at the moment a simple implementation of a generic Http server in F# it is a plug and play server where you feed it a handler function that takes a tuple of a Request and a Response (like in js)


## Current state of project
There is a simple main server handler and a proxi server handler in the project, the later does not cache anything but simply pass requests and responses between another server and a client. This can be changed but by altering the handler.

## NOTE
The current version use a self made concurrent workforce (threadpool) an channel, at this stage it is highly unlikely that this are anywhere near as fast as async or any other concurrent struct in .net libs.

