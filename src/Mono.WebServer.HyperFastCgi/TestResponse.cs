//
// TestResponse.cs: Simple HTTP responses.
//
// Author:
//   Sergey Zhukov
//
// Copyright (C) 2013 Sergey Zhukov
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace Mono.WebServer.HyperFastCgi
{
	public class TestResponse
	{
		public TestResponse ()
		{
		}

		public static string Response1=
@"HTTP/1.1 200 OK
Date: Fri, 15 Nov 2013 00:29:02 GMT
Content-Type: text/html; charset=utf-8
X-AspNet-Version: 4.0.30319
Cache-Control: private
Set-Cookie: ASP.NET_SessionId=7226F67056A58F7572E98BDB; path=/
Content-Length: 19

<p>Hello, World</p>


";
		public static string Header="Status: 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: 20\r\n\r\n";

		public static string Response="<p>Hello, world!</p>";

		public static string Response2 = "Content-type: text/html\r\n\r\n<html>\n<p>Hello, World</p>\n</html>\n\n";

	}
}

