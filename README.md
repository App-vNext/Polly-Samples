# Polly-Samples

![Polly logo](https://raw.github.com/App-vNext/Polly/main/Polly-Logo.png)

This repository provides sample implementations of using the [Polly library](https://www.github.com/App-vNext/Polly) in a .NET application.

The intent of this project is to help newcomers kick-start the use of Polly within their own projects.

The samples demonstrate the policies in action, against faulting endpoints.

## Projects

The solution contains three applications and one class library:
- `PollyTestWebApi`: This application is a web API with three endpoints. ([Further information](/PollyTestWebApi/README.md))
- `PollyDemos`: This library contains the Polly demos. ([Further information](/PollyDemos/README.md))
- `PollyTestClientConsole`: This application provides a CLI to test a demo. ([Further information](/PollyTestClientConsole/README.md))
- `PollyTestClientWpf`: This application provides a GUI to walk through the demos. ([Further information](/PollyTestClientWpf/README.md))

```mermaid
flowchart LR
    console{{PollyTestClientConsole}}
    wpf{{PollyTestClientWPF}}
    lib>PollyDemos]
    api[/PollyTestWebApi\]

    console -- uses --> lib
    wpf -- uses --> lib
    lib -- invokes --> api
```

## Demos

### General information

- The demos run against an example 'faulting server'.
  - To simulate failure, the dummy server rejects more than 3 calls in any five-second period.
- Be sure to read the `<summary>` at the top of each demo.
  - This explains the intent of that demo, and what resilience it adds to its handling of the calls to the 'faulting server'.
- Sometimes the `<summary>` also highlights what this demo _doesn't_ achieve, which is often picked up in the following demo.
- Explore the demos in sequence for best understanding.
- All demos exist in both sync and async forms.

### Sequence

| # | Description | Sync link | Async link |
| :-: | -- | :-: | :-: |
| 00 | No strategy | [Code](PollyDemos/Sync/Demo00_NoStrategy.cs) | [Code](PollyDemos/Async/AsyncDemo00_NoStrategy.cs) |
| 01 | Retry N times | [Code](PollyDemos/Sync/Demo01_RetryNTimes.cs) | [Code](PollyDemos/Async/AsyncDemo01_RetryNTimes.cs) |
| 02 | Wait and retry N times | [Code](PollyDemos/Sync/Demo02_WaitAndRetryNTimes.cs) | [Code](PollyDemos/Async/AsyncDemo02_WaitAndRetryNTimes.cs) |
| 03 | Wait and retry N times, N big enough to guarantee success | [Code](PollyDemos/Sync/Demo03_WaitAndRetryNTimes_WithEnoughRetries.cs) | [Code](PollyDemos/Async/AsyncDemo03_WaitAndRetryNTimes_WithEnoughRetries.cs) |
| 04 | Wait and retry forever | [Code](PollyDemos/Sync/Demo04_WaitAndRetryForever.cs) | [Code](PollyDemos/Async/AsyncDemo04_WaitAndRetryForever.cs) |
| 05 | Wait and retry with exponential back-off | [Code](PollyDemos/Sync/Demo05_WaitAndRetryWithExponentialBackoff.cs) | [Code](PollyDemos/Async/AsyncDemo05_WaitAndRetryWithExponentialBackoff.cs) |
| 06 | Wait and retry nesting circuit breaker | [Code](PollyDemos/Sync/Demo06_WaitAndRetryNestingCircuitBreaker.cs) | [Code](PollyDemos/Async/AsyncDemo06_WaitAndRetryNestingCircuitBreaker.cs) |
| 07 | Wait and retry chaining with circuit breaker by using Pipeline | [Code](PollyDemos/Sync/Demo07_WaitAndRetryNestingCircuitBreakerUsingPipeline.cs) | [Code](PollyDemos/Async/AsyncDemo07_WaitAndRetryNestingCircuitBreakerUsingPipeline.cs) |
| 08 | Fallback, Retry, and CircuitBreaker in a Pipeline | [Code](PollyDemos/Sync/Demo08_Pipeline-Fallback-WaitAndRetry-CircuitBreaker.cs) | [Code](PollyDemos/Async/AsyncDemo08_Pipeline-Fallback-WaitAndRetry-CircuitBreaker.cs) |
| 09 | Fallback, Timeout, and Retry in a Pipeline | [Code](PollyDemos/Sync/Demo09_Pipeline-Fallback-Timeout-WaitAndRetry.cs) | [Code](PollyDemos/Async/AsyncDemo09_Pipeline-Fallback-Timeout-WaitAndRetry.cs) |
| 10 | Without isolation: Faulting calls swamp resources, <br/>also prevent good calls | - | [Code](PollyDemos/Async/AsyncDemo10_SharedConcurrencyLimiter.cs) |
| 11 | With isolation: Faulting calls separated, <br/>do not swamp resources, good calls still succeed | - | [Code](PollyDemos/Async/AsyncDemo11_MultipleConcurrencyLimiters.cs) |


## Want further information?

- For any questions about the operation of the demos, ask on this repo [with an issue](https://github.com/App-vNext/Polly-Samples/issues/new/choose)).
- For any questions about Polly, ask in the [Polly repository](https://www.github.com/App-vNext/Polly/issues/new/choose).
- For full Polly syntax, see [Polly repository]((https://github.com/App-vNext/Polly#readme)) and the [Polly documentation](https://www.pollydocs.org/).

## Slide decks

View the [slides presented](./slides/AppvNext-DotNetFoundation-Polly-DemoSlides-Nov-2019-generic.pptx) at NDC, DevIntersections and other conferences.

You are welcome to use and adapt this presentation for not-for-profit presentations of Polly to co-workers, user groups and similar, subject to the condition that references to the .NET Foundation, App-vNext and the individual members of the Polly team are retained.
