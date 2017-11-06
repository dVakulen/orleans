## Orleans. Current and vNext
## Orleans place in .NET ecosystem
Currently .NET lacks supported by Microsoft cloud independent solution for building distributed applications. Most obvious candidate on this role could be guessed pretty easily from looking at [ .NET Foundation home page](https://github.com/dotnet). But even though its stars count is high - actual usage numbers can be seen in [nuget package statistics](https://www.nuget.org/stats/packages/Microsoft.Orleans.Core?groupby=Version), it's downloads count is two times lower than [Akka.NET counterpart]( https://www.nuget.org/stats/packages/Akka?groupby=Version), and both of them seems to be used only in rare, rather specific scenarios.

Broad community likes the idea, but continues to use in-house solutions or plain old caches.
Lack of high-scale users could be observed at [case studies page](http://dotnet.github.io/orleans/Community/Who-Is-Using-Orleans.html) ([JVM Akka's for unfair comparison](https://www.lightbend.com/case-studies)).

Everything that is going to be said next boils down to the following questions: why Orleans isn't being used neither in ASP.NET, nor in high-scale solutions, and how to make it viable option to be integrated into wide spectrum of .NET applications.

## Assumptions
 - Cloud enabled (distributed)  by default solutions will become standard in near future.
 - Actors is one of the most optimal ways of implementing distributed systems. 
 - ASP.NET users is the largest part of .NET community.
 - Almost every ASP.NET solution could benefit from using Orleans.

## View of Orleans from ASP.NET developer standpoint
 <details><summary>Notion of ASP.NET is missing from online documentation thus creating in mind 
 of passing by visitor impression that Orleans is meant to be used only in high-scale apps.</summary>
<p>
<br/>
 First place where curious potential user goes is documentation.  Orleans' first [page](http://dotnet.github.io/orleans/Documentation/Introduction.html) - great in terms of abstractions, but completely missing even a notion of ASP.NET, and so does it's left menu. Getting at least some ASP.NET reference requires navigation to tutorials, where it can be located under entry which name again, like on purpose, avoids mentioning ASP.NET (http://dotnet.github.io/orleans/Tutorials/Front-Ends-for-Orleans-Services.html).
Thus such visitor have quite high chances of getting impression that Orleans is for some high scale apps, and for his ASP.NET app with 2 - 5 servers web farm (one of most high loaded ASP.NET deployments architecture - https://nickcraver.com/blog/2016/02/17/stack-overflow-the-architecture-2016-edition uses 11 web and 2 Redis servers) it would be overkill, and will just continue to use in-memory or external caches.
</p>
</details>

<br/>
<details><summary>Integration with existing applications is not quite easy while lacking any guidelines. </summary>
<p>
<br/>Problems of such integration: in addition to complexity of awaits introducing into previously synchronous entities, it also usually brings in need in application architecture re-think or re-design
as it's entities in many cases are being tightly coupled, making straightforward rewrite for Orleans to result in chatty interactions with obvious outcomes with current Orleans performance. Recommended coordinator pattern lacks guidelines on how to design it,  and it's creation requires more thinking on existing system design in terms of DDD than in theoretically possible "every object - actor" approach, thus resulting in harder fit into existing design with reasonable compared to possible outcome costs. 
And as additional exercise for the ones wanting to incorporate Orleans into their apps - each entity contract needs to be duplicated in marker interface (While usually being right approach - for newcomers it could be a bit too much trouble for ghostly benefits).
</p>
</details>


<br/>
<details><summary>Examples with ASP.NET are almost non-existent.</summary>
<p>
<br/>
There's no ASP.NET templates besides few without "ASP.NET " in name in examples folder. So for new ASP.NET project question whether to add Orleans to it is usually being rhetorical. 
</p>
</details>


## Ways of attracting ASP.NET community
 - Focus on tight integration with ASP.NET should be clearly stated in first pages of documentation as well as normality of single server deployments with silo co-hosted in same process with web app as it being a way of ensuring easy scaling later (that indirectly would require supporting TLS though). 

 - In addition and expanding [already existing issue](https://github.com/dotnet/orleans/issues/3190) - simplify out of box experience as much as possible:  Orleans should be ready to use immediately after package installation and converting classes to grains, requiring as little configuration as possible,  ideally - just adding ``UseOrleans()`` to ASP.NET host builder. Probably allow grains resolving by class type instead of interface, as entities contracts duplication in marker interfaces could be a bit too much for curious newcomers.
Also dependency free reliable deployment would be really nice to have for this matter (it'll require built in distributed coordination solution, but that (Paxos and Raft algorithms) was already implemented on top of Orleans as experiment, so while needing thorough verification with [Jepsen](https://github.com/jepsen-io/jepsen)  - overall remaining effort shouldn't be too big, and resulting solution could be used as .NET alternative to ZooKeeper for those who doesn't want to bring additional dependencies into their apps). 

 - Document use cases of common ASP.NET apps, with Orleans being useful addition to old way caching.

 - Integrate Orleans  with main ASP.NET examples: [ASP.NET Boilerplate](https://github.com/aspnetboilerplate/aspnetboilerplate), [eShopOnContainers](https://github.com/dotnet-architecture/eShopOnContainers) , [Music Store](https://github.com/aspnet/MusicStore), etc. This one is rather high priority, and when (if) Orleans will be ready to be used in any ASP.NET application -  probably Orleans core team effort should be spent on fitting Orleans into that examples in order to have it done as soon as possible. 

- Even though existing performance usually should be enough - sometimes it still might be deciding factor. 

- Theoretically Orleans could relate to ASP.NET a bit similar to how [Akka relates to Play Framework](https://www.playframework.com/documentation/2.6.x/JavaAkka). 
 
## Comparison with requirements of high-scale application
- Fault tolerance, scalability, performance and programming model are required for framework which targets to solve highload problems.  Orleans performance seems to be the main reason of it being rare pick for such app as every additional percent of CPU resulting in increased spending on servers, and every ms of latency influences user experience. Thus potential users just don't want to pay high price of Orleans abstractions.

- Lack of enterprise support ([like Akka.Net does](https://petabridge.com/services/consulting/)) plays its part too, even though Orleans team and community does great job of helping in gitter. 
- Recently made usable .NET Core support is nice addition, but it still in beta and that makes enterprise customers to refrain from using it yet.
 
## Path of making Orleans viable choice to be base building block of high-scale application
- Performance. There's no clear obstacles on path of making Orleans as fast as its competitors instead of being multiple times slower. A bit of technical details: [Some slight efforts](https://github.com/dotnet/orleans/issues/2060) resulted in up to 90% throughput increase, making Orleans remotely comparable to Akka.NET in terms of single host execution throughput, even on 16+ core machines. And with dynamic actor re-partition, co-locating silos with clients and integration with client's threads - theoretically even thread switch cost could be removed from average grain call, leaving only relatively small await overhead. 

- While performance is being the most critical part, there's some nice to have but a bit orthogonal features, such as convenient graph-resembling streams DSL. As large part of workloads have static data flow processing plans, demand for stream processing is increasing every day, thus leading to success of tools with expressive syntax for building such pipelines ([Akka streams](https://doc.akka.io/docs/akka/2.5/scala/stream/index.html), [Kafka](https://docs.confluent.io/current/streams/index.html) to name a few lightweight ones, and [Spark's](https://spark.apache.org/streaming/ ) being the most outstanding batch-oriented one.) Having such DSL   could be deciding factor for many due to lack of such in .NET world (not counting Akka.NET).


## Relations with Akka.NET
- In addition to [existing comparison](https://github.com/akka/akka-meta/blob/master/ComparisonWithOrleans.md): Akka.NET, while being strong competitor, lacks two important features - seamless integration with programming model and official Microsoft support.
 
- There could be some thoughts on joining efforts and creating unified model for .Net ecosystem. Cost of bringing Orleans model to Akka with clustering (essentially typed actors) could be compared be to effort on Orleans optimizing.

## Conclusion
- For increasing broad ASP.NET community usages: Orleans currently missing two quite important features for it to be able to be promoted in ASP.NET examples alongside with Entity Framework: ease of deployment and performance. After fixing of that issues core team effort could be spent on increasing ASP.NET presence in documentation and making Orleans integral part of popular ASP.NET examples.

- In order to get high-scale customers - performance should be uncompromising.  Enterprise support would be nice to have, as well as Streams DSL.

<br/>
<br/>

