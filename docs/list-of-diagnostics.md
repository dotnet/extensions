# Experiments

As new functionality is introduced to this repo, new in-development APIs are marked as being experimental. Experimental APIs offer no
compatibility guarantees and can change without notice. They are usually published in order to gather feedback before finalizing
a design.

You may use experimental APIs in your application, but we advise against using these APIs in production scenarios as they may not be
fully tested nor fully reliable. Additionally, we strongly recommend that library authors do not publish versions of their libraries
that depend on experimental APIs as this will quite possibly lead to future breaking changes and diamond problems.

If you use experimental APIs, you will get one of the diagnostic shown below. The diagnostic is there to let you know you're
using such an API so that you can avoid accidentally depending on experimental features. You may suppress these diagnostics
if desired.


| Diagnostic ID     | Description |
| :---------------- | :---------- |
| `EXTEXP0001` | Resilience experiments |
| `EXTEXP0002` | Compliance experiments  |
| `EXTEXP0003` | Telemetry experiments |
| `EXTEXP0004` | TimeProvider experiments |
| `EXTEXP0005` | AutoClient experiments |
| `EXTEXP0006` | AsyncState experiments |
| `EXTEXP0007` | Health check experiments |
| `EXTEXP0008` | Resource monitoring experiments |
| `EXTEXP0009` | Hosting experiments |
| `EXTEXP0010` | Object pool experiments |

