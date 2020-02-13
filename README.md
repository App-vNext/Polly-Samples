# Polly-Samples
![](https://raw.github.com/App-vNext/Polly/master/Polly.png)

Provides sample implementations using the [Polly library](https://www.github.com/App-vNext/Polly). The intent of this project is to help newcomers kick-start the use of [Polly](https://www.github.com/App-vNext/Polly) within their own projects.  The samples demonstrate the policies in action, against faulting endpoints

## About the numbered demos

### Background

+ The demos run against an example 'faulting server' (also within the solution as `PollyTestWebApi`).  To simulate failure, the dummy server rejects more than 3 calls from the same IP in any five-second period (bulkhead demos excepted).
+ Be sure to read the `<summary/>` at the top of each demo: this explains the intent of that demo, and what resilience it adds to its handling of the calls to the 'faulting server'.  
+ Sometimes the `<summary/>` also highlights what this demo _doesn't_ achieve - often picked up in the following demo. Explore the demos in sequence, for best understanding.
+ All demos exist in both sync and async forms. 

### Demo sequence

+ Demo 00 shows behaviour calling the faulting server every half-second, with _no_ Polly policies protecting the call. 
+ Demos 01-04 show various flavors of Polly retry.
+ Demos 06-07 show retry combined with Circuit-Breaker.  
+ Demo 07 shows the Polly v5.0 `PolicyWrap` for combining policies.
+ Demo 08 adds Polly v5.0 `Fallback`, making the call protected (in a PolicyWrap) by a Fallback, Retry, Circuitbreaker. 
+ Demo 09 shows the Polly v5.0 `Timeout` policy for an overall call timeout, in combination with `Fallback` and `WaitAndRetry`.

## Bulkhead isolation demos

### Background

The bulkhead isolation demos place calls against two different endpoints on a downstream server:

+ The **good** endpoint returns results in a timely manner
+ The **faulting** endpoint simulates a faulting downstream system: it does respond, but only after a long delay.
+ (_Note_: Unlike the other demos, there is no rate-limiting rejection of the caller.)

### Demo sequence

In both bulkhead demos, the upstream system makes a random mixture of calls to the **good** and **faulting** endpoints.

+ In Demo 00 there is **no isolation**: calls to both the **good** and **faulting** endpoints share resources.  
  + Sooner or later, the **faulting stream of calls saturates** all resource in the caller, starving the calls to the **good** endpoint of resource too.   
  + Watch how the the calls to the **good** endpoint eventually start backing up (watch the 'pending' or 'faulting' counts climb), because the faulting stream of calls is starving the whole system of resource.
+ In Demo 01, the calls to **faulting** and **good** endpoints are **separated by bulkhead isolation**.  
  + The faulting stream of calls still backs up and fails.
  + But **the calls to the good endpoint are unaffected - they consistently succeed**, because they are isolated in a separate bulkhead.   

# Running the demos

## To start the dummy faulting server (PollyTestApp)

If there are problems with https and ssl trust, try running
```
dotnet dev-certs https --trust
```

+ Start the dummy server, by starting `PollyTestWebApi`.  
```
dotnet run --project PollyTestWebApi/PollyTestWebApi.csproj
```
+ Be sure the port number for the dummy server in `PollyDemos\Configuration.cs` matches the port on which `PollyTestApp` has started on _your_ machine (in the previous step).

Then ...

## To run the demos - WPF (PollyTestClientWPF - only on Windows)

+ Start the PollyTestClientWPF application.
+ **Start** button starts a demo; **Stop** button stops it; **Clear** button clears the output. 
+ Many Polly policies are about handling exceptions.  If running the demos in debug mode out of Visual Studio and flow is interrupted by Visual Studio breaking on exceptions, uncheck the box "Break when this exception type is user-unhandled" in the dialog shown when Visual Studio breaks on an exception.  Or simply run without debugging.   

## To run the demos - Console (PollyTestClientConsole - works on Mac/Linux/Windows)

```
dotnet run --project PollyTestClientConsole/PollyTestClientConsole.csproj
```

+ To run a demo, uncomment the demo you wish to run in `PollyTestClientConsole\program.cs`.  Then start `PollyTestClientConsole`.  
+ Many Polly policies are about handling exceptions.  If running the demos in debug mode out of Visual Studio and flow is interrupted by Visual Studio breaking on exceptions, uncheck the box "Break when this exception type is user-unhandled" in the dialog shown when Visual Studio breaks on an exception.  Or simply run without debugging.  

## Want further information?

+ Any questions about the operation of the demos, ask on this repo; any questions about Polly, ask at [Polly](https://www.github.com/App-vNext/Polly).
+ For full Polly syntax, see [Polly](https://www.github.com/App-vNext/Polly).  
+ For deeper discussions of transient fault-handling and further Polly patterns, see the [Polly wiki](https://github.com/App-vNext/Polly/wiki)

## Slide decks

View the [slides presented](AppvNext-DotNetFoundation-Polly-DemoSlides-Nov-2019-generic.pptx) at NDC, DevIntersections and other conferences.  You are welcome to use and adapt this presentation for not-for-profit presentations of Polly to co-workers, user groups and similar, subject to the condition that references to the .NET Foundation, App-vNext and the individual members of the Polly team are retained.
