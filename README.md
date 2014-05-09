SimpleWebServer
===============

A simple web server implemented in the computer network course

It simply listens to a certain port using the *HttpListener* class in C#.

Request from different ports are bound to different directories.

## TODO: ##

Implementing complete HTTP methods including POST, GET, PUT, etc.

Performance issues.

## DEMO USE ##

	WebServer server = new WebServer();
	server.CreateListeners("127.0.0.1");
	
	if (!server.AddPort("8000","D:\\", 5))
		return;