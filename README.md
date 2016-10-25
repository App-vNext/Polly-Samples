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
