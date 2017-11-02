Orleans. Current and vNext.

Orleans have to complete with raw in memory dicti0nary with fine granularity across farms , thus ensuring local consistency  -
// orleans provides good model with sometimes unacceptable performance implications
-- movingffrom 1 to 2 machines - 2x perf drop. _ There should not be it. Also this drop essentially means that  multi-node cluster performs at best at 50% of its capability, (grain directory cache_)

chineses in repo with strange issues
//  e.g. orleans already built bbetween clients and db ( asp.net mvc with in memory cache with ttl expiration policies )  - really simple simplicity, har





Widen orleans community

assumptions: 
		- gaining community reach is of priority for the team
	- distrivuted actors is future of cloud computatios (taking most optimistic branch) 
	-- yes, the idea is nice, but we need people to use it. 
Statements about of asp.net mvc integration should be main focus 

- assuming that gaining tracion is of any importance to current directive
- asp.net mvc integration should be main focus 
= (this on is rather gihht priority) spend team effort to fit Orleans into https://github.com/dotnet-architecture/eShopOnContainers (Orleans team have some weight compared to ousider conbvutor) as it being main example of mvc design for whole .net community

- architecture slide with fit into default mvc model is needed as example (mvc + EF)
- mvc focus should be empatised as being main (this is assumption - move to assumptions)  need of users which look at orleans

 - benefist should be stated from perspective of aveage web dev, with comparison to cache


  - http://dotnet.github.io/orleans/Documentation/Introduction.html - great in terms of absrtactions, but another view with abstacrtions concretized as asp mvc should be next to it.  - action item - team, check whats really needs to be done


- currenlty mvc interactions is under some cryptic http://dotnet.github.io/orleans/Tutorials/Front-Ends-for-Orleans-Services.html 
It couldn;t be even seen from left menu http://dotnet.github.io/orleans/index.html, requiring navigation to tutorials? - is it a problem?

- there's no clear 	obstacles on path of making Orleans just reasonably slower than in memory dictionary access insted of being orders of magnitudes slower
for it to become standart - it have some great perf implications, currently reasoned as price for distribution, but this price can be turned into pay for what use

- actions: 
- make single host usage cases as optimized as possible ( also colocating with client in same process + dynamic actor repartition with aggresively optimizedSingleHostCase should result in both throughput and latency being comparable to in-memory dictionary access)

 - price for distribution should not be payed upfront -  perf gains from move from A to B nodes count should reasebmle the ones from 0 to A nodes

 - from prysm of asp mvc user
 - users neither dont need nor want to pay the costs of Orleans upfront, as most users's  cases being solved with simple caches with,

 distributed part of orleans is usually being unused , as most users dont have such (>1 node) scale.
 \
  - create 2 prism of view, extremums - stantard asp.mvc TDefaultMvc
   - high scale app. THighScale
  
  - ACores MMemory currenlty giving single Orleans server 2x cpus and mem - without client app 
  bottlenecks should result in ~2x perf increase and to be ~ 3x more perfomant than 2 nodes with half of cpus (due to IO + serialization costs),but its not due to locks  
  
 And the ones who have - don't want to pay the price.
 . The ones 
 
  
  
  -- main theme - orleans as future of distributed computing, and integrational part of .Net framework
  
   - prysm of 95% use cases - model of cache is nice, though costs..
   - prysm of high scale remainign ones - they usually have power to hand craft something similar on top of Akka in order not to pay the costs.
   
   -- Akka is strong competitor , wih only lacking one feature  - seamless fit into languaqe model. 
   (Also, if performance was comparable with Akka's - there could be some thougths on joing efforts and creating unified model for .Net ecosystem.
  - perf related question -  Maybe bringing orleans model to akka with clustering would be easier effort than optimizing Orleans) 
   )
   
	 
	  -- Akka.Net does not bring costs, being almost as performant as Dataflow blocks\ hand coded threading - laks language fit and Microsoft's power behind
	  
	  
   - microsevises for .Net - https://github.com/gigya/microdot - it's success to some certain depends on Orleans perf
   
    - -- user standpoint: I want my objects to be highly scalabe, but still look alike ordinary .Net objects
	
	
	 -- some slight efforts(- - threading rework reference) resulted in up to 89% perf increase, making Orleans remotely comparable to Akka.Net in terms of single host execution thgoughput 
	 
	 
	 
	 
	 - assumption - everithing is ASP.MVC
	 
	 - focus on out of box expirience - asp net dev should be able to use Orleans after package installation, without any preconfiguration
	 
	  - head of PR
	  Orleans nodel while theoretically fitting nicely into  95% mvc users cases - at practice neither clearly states how to do that, nor provides acceptable perf.
	  
	  - MS can not currently state that Orleans is way to go and PR Orleans alongside the MVC due to it's perf implications
	  - single host in process perf - critical.
	  - in order to gain traction and get uses - following  could be done: .....
     - introduce notion of akkas  distributed data (https://doc.akka.io/docs/akka/2.5.4/scala/distributed-data.html)
	 - with view of (Dictionary<K,V> ) - basically a cache - as default in mem caching for distributed apps  - with distributed transactions should be easy
 --  yhat cache should be quite configurable in order to cover cases from InMemoryDict to multicluster wide cache ]	   (some inspiration from https://hazelcast.com/use-cases/caching/)
 
 
 
	 - convinient streams DSL, Orleans should handle that part, basically becoming distributed DataFlow. as 95% of workload have static dataflow processing plans, thus leading to success of tools with ablility to heavily optimize this kind of plans,
	 such as Spark.   Dynamically reconfigurable streams could also be covered by that DSL.  -
	 -
	 --- orthogonal, but nice to have geatures: Distr data, streams DSL. (with reasonings and real life examples ))
	 
	 - in current world of Distributed multicore processors MS has to offer default  solution to be used on premises for this matter similar on how the ThreadPool for node cores (Orleans for nodes as .Net TaskScheduler for cores)
	  -- average use case req - take mvc app in vacuum,  analyze - for Orleans, Distr data, Streams DSl. -(expandable part OF Pr )
	  - the main rev - perf.
	  -- plan - become a future (de-facto in .Net world)(
	  add reasoing  - simplicity of use, whith uncopromised at low scale performance, i).

	  requirements - Asp.MVC focus, PR from Microsoft wit and performance.
	  
	   -- it should be as easy to use as ordinary cahce ( it already is) 
	   - it shouldn't bring pow(10, n) perf drop. - thats the problem part
	      -- become part of channels api
	    - if that sounds good to team - I'll take some actions?

		
		- Orleans team, could you please share your thoughts on this matter? - as separate comment
		
		 == is it possible to have MS' greater support after perf increase?  - direct message after PR
		 
		  -- recenlty encountered just another case of orleans fit
		  
					
 - problem: .Net lacks supported by MS cloud independent solution for cluster processing.
  - most obvious candidate could be guessed pretty easily by starring at https://github.com/dotnet
 - though stars count is high - actual usage which can be seen at nuget downloads is two times lower than Akka.Net counterpart. - Community likes the idea,  but contunues to use Akka \ in house solutions due to Orleans' abstractions costs. This costs can be removed.
		- all it to be true needs just some belief in Orleans
		
		 assuming that Asp.net mvc developer is the largest part current .Net community
		--everythung that is going to be said boils down to simple questions: why Asp.net mvc developer doesn't use Orleans and how to fix it.
		using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Runtime;

namespace Orleans
{
    internal static class InternerConstants
    {
        /* Recommended cache sizes, based on expansion policy of ConcurrentDictionary
        // Internal implementation of ConcurrentDictionary resizes to prime numbers (not divisible by 3 or 5 or 7)
        31
        67
        137
        277
        557
        1,117
        2,237
        4,477
        8,957
        17,917
        35,837
        71,677
        143,357
        286,717
        573,437
        1,146,877
        2,293,757
        4,587,517
        9,175,037
        18,350,077
        36,700,157
        */
        public const int SIZE_SMALL = 67;
        public const int SIZE_MEDIUM = 1117;
        public const int SIZE_LARGE = 143357;
        public const int SIZE_X_LARGE = 2293757;

        public static readonly TimeSpan DefaultCacheCleanupFreq = TimeSpan.FromMinutes(10);
    }

    /// <summary>
    /// Provide a weakly-referenced cache of interned objects.
    /// Interner is used to optimise garbage collection.
    /// We use it to store objects that are allocated frequently and may have long timelife. 
    /// This means those object may quickly fill gen 2 and cause frequent costly full heap collections.
    /// Specificaly, a message that arrives to a silo and all the headers and ids inside it may stay alive long enough to reach gen 2.
    /// Therefore, we store all ids in interner to re-use their memory accros different messages.
    /// </summary>
    /// <typeparam name="K">Type of objects to be used for intern keys</typeparam>
    /// <typeparam name="T">Type of objects to be interned / cached</typeparam>
    internal class Interner<K, T> : IDisposable where T : class
    {
        private readonly TimeSpan cacheCleanupInterval;
        private readonly SafeTimer cacheCleanupTimer;

        [NonSerialized]
        private readonly ConcurrentDictionary<K, WeakReference<T>> internCache;

        public Interner()
            : this(InternerConstants.SIZE_SMALL)
        {
        }
        public Interner(int initialSize)
            : this(initialSize, Timeout.InfiniteTimeSpan)
        {
        }
        public Interner(int initialSize, TimeSpan cleanupFreq)
        {
            if (initialSize <= 0) initialSize = InternerConstants.SIZE_MEDIUM;
            int concurrencyLevel = Environment.ProcessorCount * 4; // Default from ConcurrentDictionary class in .NET 4.0

            this.internCache = new ConcurrentDictionary<K, WeakReference<T>>(concurrencyLevel, initialSize);

            this.cacheCleanupInterval = (cleanupFreq <= TimeSpan.Zero) ? Timeout.InfiniteTimeSpan : cleanupFreq;
            if (Timeout.InfiniteTimeSpan != cacheCleanupInterval)
            {
                cacheCleanupTimer = new SafeTimer(NullLogger.Instance, InternCacheCleanupTimerCallback, null, cacheCleanupInterval, cacheCleanupInterval);
            }
#if DEBUG_INTERNER
            StringValueStatistic.FindOrCreate(internCacheName, () => String.Format("Size={0}, Content=" + Environment.NewLine + "{1}", internCache.Count, PrintInternerContent()));
#endif
        }

        /// <summary>
        /// Find cached copy of object with specified key, otherwise create new one using the supplied creator-function.
        /// </summary>
        /// <param name="key">key to find</param>
        /// <param name="creatorFunc">function to create new object and store for this key if no cached copy exists</param>
        /// <returns>Object with specified key - either previous cached copy or newly created</returns>
        public T FindOrCreate(K key, Func<K, T> creatorFunc)
        {
            T result;
            WeakReference<T> cacheEntry;

            // Attempt to get the existing value from cache.
            internCache.TryGetValue(key, out cacheEntry);

            // If no cache entry exists, create and insert a new one using the creator function.
            if (cacheEntry == null)
            {
                result = creatorFunc(key);
                cacheEntry = new WeakReference<T>(result);
                internCache[key] = cacheEntry;
                return result;
            }

            // If a cache entry did exist, determine if it still holds a valid value.
            cacheEntry.TryGetTarget(out result);
            if (result == null)
            {
                // Create new object and ensure the entry is still valid by re-inserting it into the cache.
                result = creatorFunc(key);
                cacheEntry.SetTarget(result);
                internCache[key] = cacheEntry;
            }

            return result;
        }

        /// <summary>
        /// Find cached copy of object with specified key, otherwise create new one using the supplied creator-function.
        /// </summary>
        /// <param name="key">key to find</param>
        /// <param name="obj">The existing value if the key is found</param>
        public bool TryFind(K key, out T obj)
        {
            obj = null;
            WeakReference<T> cacheEntry;
            return internCache.TryGetValue(key, out cacheEntry) && cacheEntry != null && cacheEntry.TryGetTarget(out obj);
        }

        /// <summary>
        /// Find cached copy of object with specified key, otherwise store the supplied one. 
        /// </summary>
        /// <param name="key">key to find</param>
        /// <param name="obj">The new object to store for this key if no cached copy exists</param>
        /// <returns>Object with specified key - either previous cached copy or justed passed in</returns>
        public T Intern(K key, T obj)
        {
            return FindOrCreate(key, _ => obj);
        }

        public void StopAndClear()
        {
            internCache.Clear();
            cacheCleanupTimer?.Dispose();
        }

        public List<T> AllValues()
        {
            List<T> values = new List<T>();
            foreach (var e in internCache)
            {
                T value;
                if (e.Value != null && e.Value.TryGetTarget(out value))
                {
                    values.Add(value);
                }
            }
            return values;
        }

        private void InternCacheCleanupTimerCallback(object state)
        {
            foreach (var e in internCache)
            {
                T ignored;
                if (e.Value == null || e.Value.TryGetTarget(out ignored) == false)
                {
                    WeakReference<T> weak;
                    internCache.TryRemove(e.Key, out weak);
                }
            }
        }

        public void Dispose()
        {
            cacheCleanupTimer?.Dispose();
        }
    }
}
