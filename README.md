
<p align="center"><a href="https://github.com/dotnet/orleans/blob/gh-pages/assets/logo_full.png" target="_blank"><img src="https://github.com/dotnet/orleans/raw/gh-pages/assets/logo_full.png" alt="Orleans logo" width="600px"></a> 
</p>
=======


<p><a href="http://dotnet-ci.cloudapp.net/job/dotnet_orleans/job/innerloop"><img src="https://camo.githubusercontent.com/07c1fa6eb271a99cfec719d75862a227de0fe51f/687474703a2f2f646f746e65742d63692e636c6f75646170702e6e65742f6a6f622f646f746e65745f6f726c65616e732f6a6f622f696e6e65726c6f6f702f62616467652f69636f6e" alt="Build status" data-canonical-src="http://dotnet-ci.cloudapp.net/job/dotnet_orleans/job/innerloop/badge/icon" style="max-width:100%;"></a>
<a href="http://www.nuget.org/profiles/Orleans"><img src="https://camo.githubusercontent.com/8d0f99f652fa768cbf32577c33af1d5d3061b0ad/68747470733a2f2f696d672e736869656c64732e696f2f6e756765742f762f4d6963726f736f66742e4f726c65616e732e436f72652e7376673f7374796c653d666c6174" alt="NuGet" data-canonical-src="https://img.shields.io/nuget/v/Microsoft.Orleans.Core.svg?style=flat" style="max-width:100%;"></a>
<a href="http://www.issuestats.com/github/dotnet/orleans"><img src="https://camo.githubusercontent.com/5359fb309a3c2555d9d824133da783137ff24099/687474703a2f2f7777772e697373756573746174732e636f6d2f6769746875622f646f746e65742f6f726c65616e732f62616467652f7072" alt="Issue Stats" data-canonical-src="http://www.issuestats.com/github/dotnet/orleans/badge/pr" style="max-width:100%;"></a>
<a href="http://www.issuestats.com/github/dotnet/orleans"><img src="https://camo.githubusercontent.com/ed0e065ad89a8c1155df509c439eee306c10d8b1/687474703a2f2f7777772e697373756573746174732e636f6d2f6769746875622f646f746e65742f6f726c65616e732f62616467652f6973737565" alt="Issue Stats" data-canonical-src="http://www.issuestats.com/github/dotnet/orleans/badge/issue" style="max-width:100%;"></a></p>

<p><a href="https://gitter.im/dotnet/orleans?utm_source=badge&amp;utm_medium=badge&amp;utm_campaign=pr-badge"><img src="https://camo.githubusercontent.com/da2edb525cde1455a622c58c0effc3a90b9a181c/68747470733a2f2f6261646765732e6769747465722e696d2f4a6f696e253230436861742e737667" alt="Gitter" data-canonical-src="https://badges.gitter.im/Join%20Chat.svg" style="max-width:100%;"></a></p>
<p><a href="http://waffle.io/dotnet/orleans"><img src="https://camo.githubusercontent.com/3ee99c1c37097b807ae7e4bfa90b2cd6e353d48d/68747470733a2f2f62616467652e776166666c652e696f2f646f746e65742f6f726c65616e732e7376673f6c6162656c3d75702d666f722d6772616273267469746c653d48656c7025323057616e746564253230497373756573" alt="Help Wanted Issues" data-canonical-src="https://badge.waffle.io/dotnet/orleans.svg?label=up-for-grabs&amp;title=Help%20Wanted%20Issues" style="max-width:100%;"></a></p>


Orleans is a framework that provides a straight-forward approach to building distributed high-scale computing applications, without the need to learn and apply complex concurrency or other scaling patterns. 
It was created by [Microsoft Research][MSR-ProjectOrleans] and designed for use in the cloud. 
Orleans has been used extensively running in Microsoft Azure by several Microsoft product groups, most notably by 343 Industries as a platform for all of Halo 4 and Halo 5 cloud services, as well as by [a number of other projects and companies](http://dotnet.github.io/orleans/Who-Is-Using-Orleans).

Installation
=======

Installation is performed via NuGet. There are several packages, one for each different project type (interfaces, grains, silo, and client).

In the grain interfaces project:
```
PM> Install-Package Microsoft.Orleans.Templates.Interfaces
```
In the grain implementations project:
```
PM> Install-Package Microsoft.Orleans.Templates.Grains
```
In the server (silo) project:
```
PM> Install-Package Microsoft.Orleans.Server
```
In the client project:
```
PM> Install-Package Microsoft.Orleans.Client
```

### Official Builds
The stable production-quality release is located [here](https://github.com/dotnet/orleans/releases/latest).

The latest clean development branch build from CI is located: [here](http://dotnet-ci.cloudapp.net/job/dotnet_orleans/job/innerloop/lastStableBuild/artifact/)

### Building From Source
Clone the sources and run the `Build.cmd` script to build the binaries locally.

Then reference the required assemblies from `Binaries\Release\*` or the NuGet packages from `Binaries\NuGet.Packages\*`.

[Documentation][Orleans Documentation]
=======
Documentation is located [here][Orleans Documentation]

Example
=======

Create an interface for your grain:
```c#
public interface IHello : Orleans.IGrainWithIntegerKey
{
  Task<string> SayHello(string greeting);
}
```

Provide an implementation of that interface:
```c#
public class HelloGrain : Orleans.Grain, IHello
{
  Task<string> SayHello(string greeting)
  {
    return Task.FromResult($"You said: '{greeting}', I say: Hello!");
  }
}
```

Call the grain from your Web service (or anywhere else):
```c#
// Get a reference to the IHello grain with id '0'.
var friend = GrainClient.GrainFactory.GetGrain<IHello>(0);

// Send a greeting to the grain an await the response.
Console.WriteLine(await friend.SayHello("Good morning, my friend!"));
```

Contributing To This Project
=======

* List of [Ideas for Contributions]

* [Contributing Guide]

* [CLA - Contribution License Agreement][CLA]

* The coding standards / style guide used for Orleans code is the [.NET Framework Design Guidelines][DotNet Framework Design Guidelines]

* [Orleans Community - Repository of community add-ons to Orleans](https://github.com/OrleansContrib/) Various community projects, including Orleans Monitoring, Design Patterns, Storage Provider, etc.

You are also encouraged to start a discussion by filing an issue.

License
=======
This project is licensed under the [MIT license](https://github.com/dotnet/orleans/blob/master/LICENSE).


[MSR-ProjectOrleans]: http://research.microsoft.com/projects/orleans/
[Orleans Documentation]: http://dotnet.github.io/orleans/
[Ideas for Contributions]: http://dotnet.github.io/orleans/Ideas-for-Contributions
[Contributing Guide]: https://github.com/dotnet/corefx/wiki/Contributing
[CLA]: https://github.com/dotnet/corefx/wiki/Contribution-License-Agreement-%28CLA%29
[DotNet Framework Design Guidelines]: https://github.com/dotnet/corefx/wiki/Framework-Design-Guidelines-Digest
[Download Link]: http://orleans.codeplex.com/releases/view/144111
