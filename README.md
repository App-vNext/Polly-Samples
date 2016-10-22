# Polly-Samples
![](https://raw.github.com/App-vNext/Polly/master/Polly.png)

Provides sample implementations of the [Polly library](https://www.github.com/App-vNext/Polly). The intent of this project is to help newcomers kick-start their use of [Polly](https://www.github.com/App-vNext/Polly) within their own projects.

## About the numbered demos

+ The demos show different flavors of retry; then retry combined with Circuit-Breaker.  
+ The demos run against an example 'faulting server' (also within the solution).  To simulate failure, the dummy server rejects more than 3 calls from the same IP in any five-second period.
+ Be sure to read the `<summary/>` at the top of each demo: this explains the intent of each demo, what it achieves (in relation to the dummy 'faulting server'), what it doesn't yet achieve.
+ Explore the demos in sequence for best understanding: each solves a problem which the previous didn't.

## To run the demos

+ To start the dummy server, start `PollyTestApp`.  
+ Be sure the port number for the dummy server in `PollyTestClient\Configuration.cs` matches the port on which `PollyTestApp` starts on your machine.
+ To run a demo, uncomment the demo you wish to run in `PollyTestClient\program.cs`.  Then start `PollyTestClient`.  

## Want further information?

+ Any questions about the operation of the demos, ask on this repo; any questions about Polly, ask at [Polly](https://www.github.com/App-vNext/Polly).
+ For full Polly syntax, see [Polly](https://www.github.com/App-vNext/Polly).  
+ For deeper discussions of transient fault-handling and further Polly patterns, see the [Polly wiki](https://github.com/App-vNext/Polly/wiki)

## Slide deck from DevIntersections presentation

View the [slides presented](https://github.com/App-vNext/Polly-Samples/blob/master/Demo-Slides.pdf) at the April 2016 DevIntersections Polly demo by Carl Franklin.
