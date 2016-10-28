# Polly-Samples
![](https://raw.github.com/App-vNext/Polly/master/Polly.png)

Provides sample implementations using the [Polly library](https://www.github.com/App-vNext/Polly). The intent of this project is to help newcomers kick-start their use of [Polly](https://www.github.com/App-vNext/Polly) within their own projects.

## About the numbered demos

### Background

+ The demos run against an example 'faulting server' (also within the solution).  To simulate failure, this dummy server rejects more than 3 calls from the same IP in any five-second period.
+ Be sure to read the `<summary/>` at the top of each demo: this explains the intent of that demo, and what it achieves (in relation to the dummy 'faulting server').  
+ Sometimes the `<summary/>` also highlights what this demo _doesn't_ achieve - often picked up in the following demo. Explore the demos in sequence, for best understanding.

### Sequence

+ Demo 00 shows behaviour calling the faulting server every half-second, with _no_ Polly policies protecting the call. 
+ Demos 01-04 show various flavors of Polly retry.
+ Demos 05-06 show retry combined with Circuit-Breaker.  
+ Demo 07 shows the Polly v5.0 `PolicyWrap` for combining Retry and CircuitBreaker.
+ Demo 08 adds Polly v5.0 `Fallback`, making the call protected (in a PolicyWrap) by a Fallback, Retry, Circuitbreaker. 
+ Demo 09 shows the Polly v5.0 `Timeout` policy for an overall call timeout, in combination with `Fallback` and `WaitAndRetry`.

## Bulkhead isolation demos

### Background

The bulkhead isolation demos place calls against two different imaginary endpoints on a downstream server:

+ The **good** endpoint returns results in a timely manner
+ The **faulting** endpoint simulates a faulting downstream system: it does respond, but only after a long delay.
+ (_Note_: Unlike the other demos, there is no throttling rejection of the caller.)

### Sequence

In all bulkhead demos, the upstream system makes a random mixture of calls to the **good** and **faulting** endpoints.

+ In Demo 00 there is **no bulkhead isolation**.  
  + Sooner or later, the **faulting stream of calls saturates **all resource in the caller, starving the calls to the **good** endpoint of resource too.   
  + Watch how the the calls to the **good** endpoint eventually start backing up too (watch the 'pending' count climb), as the faulting stream starves the whole system of resource.
+ In demo 01, the calls to **faulting** and **good** endpoints are **isolated by bulkhead isolation**.  
  + The faulting stream of calls still backs up.
  + But **the calls to the good endpoint are unaffected - they consistently succeed**, because they are isolated in a separate bulkhead.   

## To run the demos

+ To start the dummy server, start `PollyTestApp`.  
+ Be sure the port number for the dummy server in `PollyTestClient\Configuration.cs` matches the port on which `PollyTestApp` has started on your machine (in the previous step).
+ To run a demo, uncomment the demo you wish to run in `PollyTestClient\program.cs`.  Then start `PollyTestClient`.  

## Want further information?

+ Any questions about the operation of the demos, ask on this repo; any questions about Polly, ask at [Polly](https://www.github.com/App-vNext/Polly).
+ For full Polly syntax, see [Polly](https://www.github.com/App-vNext/Polly).  
+ For deeper discussions of transient fault-handling and further Polly patterns, see the [Polly wiki](https://github.com/App-vNext/Polly/wiki)

## Slide deck from DevIntersections presentation

View the [slides presented](https://github.com/App-vNext/Polly-Samples/blob/master/Demo-Slides.pdf) at the April 2016 DevIntersections Polly demo by Carl Franklin.
